using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Main SDK entry point. Auto-initializes from GameDevPartnerConfig (Resources).
    /// No manual Init() call needed — just configure via Window > GameDevPartner > Settings.
    /// </summary>
    public class GameDevPartnerSDK : MonoBehaviour
    {
        private static GameDevPartnerSDK _instance;
        private static SDKConfig _config;
        private static bool _initialized;
        private static string _currentPlayerId;
        private static bool _identified;
        private static bool _autoIdentify;


        // Purchase offline queue
        private readonly Queue<PurchaseEvent> _offlineQueue = new Queue<PurchaseEvent>();
        private const int MaxQueueSize = 100;
        private const string QueueKey = "gdp_offline_queue";
        private const string AttributedKey = "gdp_attributed";
        private const string PlayerIdKey = "gdp_player_id";

        // Ad impression batching
        private readonly List<AdImpressionItem> _adBatch = new List<AdImpressionItem>();
        private readonly List<AdImpressionItem> _adOfflineQueue = new List<AdImpressionItem>();
        private const int AdBatchSize = 50;
        private const float AdBatchIntervalSec = 30f;
        private const int AdMaxOfflineQueue = 500;
        private const string AdQueueKey = "gdp_ad_offline_queue";
        private Coroutine _adFlushCoroutine;
        private static string _sessionId;
        private static int _adImpressionCounter;

        /// <summary>
        /// Auto-initialize SDK from ScriptableObject config in Resources.
        /// Called automatically before any scene loads.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            var asset = GameDevPartnerConfig.Load();
            if (asset == null)
            {
                // No config asset — SDK not configured, skip silently
                return;
            }

            if (string.IsNullOrEmpty(asset.ApiKey))
            {
                Debug.LogWarning("[GameDevPartner] API Key not set. Configure via Window > GameDevPartner > Settings");
                return;
            }

            _autoIdentify = asset.AutoIdentify;

            Init(asset.ToSDKConfig());
        }

        /// <summary>
        /// Initialize the SDK manually (optional — SDK auto-inits from config asset).
        /// </summary>
        public static void Init(SDKConfig config)
        {
            if (_initialized)
            {
                Log("SDK already initialized");
                return;
            }

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                Debug.LogError("[GameDevPartner] ApiKey is required");
                return;
            }

            _config = config;
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _adImpressionCounter = 0;

            // Create persistent singleton
            if (_instance == null)
            {
                var go = new GameObject("GameDevPartnerSDK");
                _instance = go.AddComponent<GameDevPartnerSDK>();
                DontDestroyOnLoad(go);
            }

            _initialized = true;
            _identified = false; // Always re-identify — server handles dedup
            _currentPlayerId = PlayerPrefs.GetString(PlayerIdKey, "");

            Log($"SDK initialized. Region={config.Region}, Debug={config.DebugMode}");

            // Flush offline queues
            _instance.StartCoroutine(_instance.FlushOfflineQueue());
            _instance.StartCoroutine(_instance.FlushAdOfflineQueue());

            // Fetch install referrer in background (Google Play / RuStore)
            GDPInstallReferrer.FetchReferrer((referrer) =>
            {
                if (!string.IsNullOrEmpty(referrer))
                    Log($"Install referrer obtained: {referrer}");

                // Auto-identify player using device unique ID + referrer
                if (_autoIdentify && string.IsNullOrEmpty(_currentPlayerId))
                {
                    string deviceId = SystemInfo.deviceUniqueIdentifier;
                    if (deviceId != SystemInfo.unsupportedIdentifier)
                    {
                        Log($"Auto-identifying player with device ID");
                        IdentifyPlayer(deviceId, referrer);
                    }
                }
                else if (_autoIdentify && !string.IsNullOrEmpty(_currentPlayerId))
                {
                    Log($"Re-identifying previously known player");
                    IdentifyPlayer(_currentPlayerId, referrer);
                }
            });
        }

        /// <summary>
        /// Identify a player for attribution. Call after player login/registration.
        /// </summary>
        public static void IdentifyPlayer(string playerId, string referrer = null)
        {
            if (!EnsureInitialized()) return;

            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("[GameDevPartner] playerId is required");
                return;
            }

            // Auto-fill referrer from Install Referrer API if not provided
            if (string.IsNullOrEmpty(referrer))
                referrer = GDPInstallReferrer.GetCachedReferrer();

            // Cache player ID
            _currentPlayerId = playerId;
            PlayerPrefs.SetString(PlayerIdKey, playerId);

            _instance.StartCoroutine(_instance.DoIdentify(playerId, referrer));
        }

        /// <summary>
        /// Track a purchase event. Call after every successful in-app purchase.
        /// </summary>
        public static void TrackPurchase(PurchaseEvent purchase)
        {
            if (!EnsureInitialized()) return;

            if (string.IsNullOrEmpty(purchase.PlayerId))
                purchase.PlayerId = _currentPlayerId;

            if (string.IsNullOrEmpty(purchase.PlayerId))
            {
                Debug.LogError("[GameDevPartner] PlayerId is required. Call IdentifyPlayer first.");
                return;
            }

            if (purchase.Amount <= 0)
            {
                Debug.LogError("[GameDevPartner] Amount must be positive");
                return;
            }

            _instance.StartCoroutine(_instance.DoTrackPurchase(purchase));
        }

        /// <summary>
        /// Track an ad impression with ILAR (impression-level ad revenue) data.
        /// Events are batched and sent every 30 seconds or when batch reaches 50 events.
        /// Supports all major ad networks: AdMob, IronSource, AppLovin, Unity Ads, Yandex Ads.
        /// </summary>
        public static void TrackAdImpression(AdImpressionEvent impression)
        {
            if (!EnsureInitialized()) return;

            if (string.IsNullOrEmpty(impression.PlayerId))
                impression.PlayerId = _currentPlayerId;

            if (string.IsNullOrEmpty(impression.PlayerId))
            {
                Debug.LogError("[GameDevPartner] PlayerId is required. Call IdentifyPlayer first.");
                return;
            }

            if (impression.Revenue < 0)
            {
                Debug.LogError("[GameDevPartner] Ad revenue cannot be negative");
                return;
            }

            // Auto-generate impression ID if not provided
            if (string.IsNullOrEmpty(impression.ImpressionId))
            {
                impression.ImpressionId = $"{_sessionId}_{ToAdNetworkString(impression.AdNetwork)}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{_adImpressionCounter++}";
            }

            var item = new AdImpressionItem
            {
                player_id = impression.PlayerId,
                ad_type = ToAdTypeString(impression.AdType),
                ad_network = ToAdNetworkString(impression.AdNetwork),
                ad_unit_id = impression.AdUnitId ?? "",
                revenue = impression.Revenue,
                currency = impression.Currency ?? "USD",
                impression_id = impression.ImpressionId,
            };

            lock (_instance._adBatch)
            {
                _instance._adBatch.Add(item);
            }

            Log($"Ad impression queued: {item.ad_network}/{item.ad_type} rev={item.revenue} {item.currency}");

            // Flush immediately if batch is full
            if (_instance._adBatch.Count >= AdBatchSize)
            {
                _instance.FlushAdBatchNow();
            }
            else if (_instance._adFlushCoroutine == null)
            {
                // Start delayed flush timer
                _instance._adFlushCoroutine = _instance.StartCoroutine(_instance.DelayedAdFlush());
            }
        }

        /// <summary>
        /// Simplified ad revenue tracking — no adapter needed, no Define Symbols.
        /// Works with any ad network, including Yandex mediation via code.
        ///
        /// Usage (one line):
        ///   GameDevPartnerSDK.TrackAdRevenue(revenue, "USD", "rewarded", "yandex_ads", "ad_unit_123");
        ///
        /// Valid adType: "rewarded", "interstitial", "banner"
        /// Valid adNetwork: "admob", "ironsource", "applovin", "unity_ads", "yandex_ads", "other"
        /// </summary>
        public static void TrackAdRevenue(double revenue, string currency = "USD",
            string adType = "rewarded", string adNetwork = "other", string adUnitId = "")
        {
            if (!EnsureInitialized()) return;
            if (revenue <= 0) return;

            AdType parsedType;
            switch (adType?.ToLower())
            {
                case "rewarded": parsedType = AdType.Rewarded; break;
                case "interstitial": parsedType = AdType.Interstitial; break;
                case "banner": parsedType = AdType.Banner; break;
                default: parsedType = AdType.Rewarded; break;
            }

            AdNetwork parsedNetwork;
            switch (adNetwork?.ToLower())
            {
                case "admob": parsedNetwork = AdNetwork.AdMob; break;
                case "ironsource": parsedNetwork = AdNetwork.IronSource; break;
                case "applovin": parsedNetwork = AdNetwork.AppLovin; break;
                case "unity_ads": parsedNetwork = AdNetwork.UnityAds; break;
                case "yandex_ads": case "yandex": parsedNetwork = AdNetwork.YandexAds; break;
                default: parsedNetwork = AdNetwork.Other; break;
            }

            TrackAdImpression(new AdImpressionEvent
            {
                AdType = parsedType,
                AdNetwork = parsedNetwork,
                AdUnitId = adUnitId,
                Revenue = revenue,
                Currency = currency,
            });
        }

        #region Internal Coroutines

        /// <summary>
        /// Track app session (app_open) — sent automatically after identify.
        /// Server records one entry per player per day for retention analytics.
        /// </summary>
        private IEnumerator DoTrackSession()
        {
            if (string.IsNullOrEmpty(_currentPlayerId)) yield break;

            var body = new SessionRequest
            {
                player_id = _currentPlayerId,
                platform = GetPlatform()
            };

            string json = JsonUtility.ToJson(body);
            Log($"Session: {json}");

            using var request = CreatePost("/sdk/session", json);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Log("Session tracked");
            }
            else
            {
                Log($"Session tracking failed (non-critical): {request.error}");
            }
        }

        private IEnumerator DoIdentify(string playerId, string referrer, int retryAttempt = 0)
        {
            var body = new IdentifyRequest
            {
                player_id = playerId,
                platform = GetPlatform(),
                gaid = GetGAID(),
                idfv = GetIDFV(),
                referrer = referrer,
                device_fingerprint = SystemInfo.deviceModel + "|" + SystemInfo.operatingSystem,
                country = null
            };

            string json = JsonUtility.ToJson(body);
            Log($"Identify: {json}");

            using var request = CreatePost("/sdk/identify", json);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<IdentifyResponse>(request.downloadHandler.text);
                if (response?.data != null && response.data.attributed)
                {
                    _identified = true;
                    PlayerPrefs.SetInt(AttributedKey, 1);
                    Log($"Player attributed! match_type={response.data.match_type}");
                }
                else
                {
                    Log("Player not attributed (organic)");
                }

                // Track session (app open) for retention analytics
                StartCoroutine(DoTrackSession());
            }
            else
            {
                Debug.LogWarning($"[GameDevPartner] Identify failed: {request.error}");
                // Retry up to 3 times with exponential backoff
                if (retryAttempt < 3)
                {
                    float delay = Mathf.Pow(2, retryAttempt + 1); // 2s, 4s, 8s
                    Log($"Retrying identify in {delay}s (attempt {retryAttempt + 1}/3)");
                    yield return new WaitForSeconds(delay);
                    StartCoroutine(DoIdentify(playerId, referrer, retryAttempt + 1));
                }
            }
        }

        private IEnumerator DoTrackPurchase(PurchaseEvent purchase)
        {
            var body = new PurchaseRequest
            {
                player_id = purchase.PlayerId,
                product_id = purchase.ProductId,
                gross_amount = purchase.Amount,
                currency = purchase.Currency,
                source = ToSourceString(purchase.Source),
                external_tx_id = purchase.TransactionId,
                receipt_data = purchase.ReceiptData
            };

            string json = JsonUtility.ToJson(body);
            Log($"TrackPurchase: {json}");

            using var request = CreatePost("/sdk/purchase", json);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Log($"Purchase tracked: {request.downloadHandler.text}");
            }
            else if (IsRetryableError(request) && purchase._retryCount < 2)
            {
                purchase._retryCount++;
                float delay = Mathf.Pow(2, purchase._retryCount); // 2s, 4s
                Log($"Purchase tracking failed, retrying in {delay}s (attempt {purchase._retryCount}/2)");
                yield return new WaitForSeconds(delay);
                StartCoroutine(DoTrackPurchase(purchase));
            }
            else
            {
                Debug.LogWarning($"[GameDevPartner] Purchase tracking failed: {request.error}, queuing offline");
                EnqueueOffline(purchase);
            }
        }

        private IEnumerator FlushOfflineQueue()
        {
            LoadOfflineQueue();

            while (_offlineQueue.Count > 0)
            {
                var purchase = _offlineQueue.Peek();
                Log($"Retrying offline purchase: {purchase.TransactionId}");

                var body = new PurchaseRequest
                {
                    player_id = purchase.PlayerId,
                    product_id = purchase.ProductId,
                    gross_amount = purchase.Amount,
                    currency = purchase.Currency,
                    source = ToSourceString(purchase.Source),
                    external_tx_id = purchase.TransactionId,
                    receipt_data = purchase.ReceiptData
                };

                string json = JsonUtility.ToJson(body);
                using var request = CreatePost("/sdk/purchase", json);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _offlineQueue.Dequeue();
                    SaveOfflineQueue();
                    Log($"Offline purchase sent: {purchase.TransactionId}");
                }
                else
                {
                    Log("Offline flush failed, will retry later");
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }

        // --- Ad Impression Batch Flush ---

        private IEnumerator DelayedAdFlush()
        {
            yield return new WaitForSeconds(AdBatchIntervalSec);
            FlushAdBatchNow();
        }

        private void FlushAdBatchNow()
        {
            if (_adFlushCoroutine != null)
            {
                StopCoroutine(_adFlushCoroutine);
                _adFlushCoroutine = null;
            }

            List<AdImpressionItem> batch;
            lock (_adBatch)
            {
                if (_adBatch.Count == 0) return;
                batch = new List<AdImpressionItem>(_adBatch);
                _adBatch.Clear();
            }

            StartCoroutine(DoSendAdBatch(batch));
        }

        private IEnumerator DoSendAdBatch(List<AdImpressionItem> batch)
        {
            var request_body = new AdRevenueBatchRequest
            {
                impressions = batch.ToArray()
            };

            string json = JsonUtility.ToJson(request_body);
            Log($"Sending ad batch: {batch.Count} impressions");

            using var request = CreatePost("/sdk/ad-revenue", json);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AdRevenueResponse>(request.downloadHandler.text);
                if (response?.data != null)
                    Log($"Ad batch sent: stored={response.data.stored}, skipped={response.data.skipped}");
                else
                    Log($"Ad batch sent (response: {request.downloadHandler.text})");
            }
            else
            {
                Debug.LogWarning($"[GameDevPartner] Ad revenue batch failed: {request.error}, queuing offline");
                EnqueueAdOffline(batch);
            }
        }

        private IEnumerator FlushAdOfflineQueue()
        {
            LoadAdOfflineQueue();

            if (_adOfflineQueue.Count == 0) yield break;

            // Send in batches of AdBatchSize
            while (_adOfflineQueue.Count > 0)
            {
                var batch = _adOfflineQueue.Take(AdBatchSize).ToList();

                var request_body = new AdRevenueBatchRequest
                {
                    impressions = batch.ToArray()
                };

                string json = JsonUtility.ToJson(request_body);
                Log($"Retrying offline ad batch: {batch.Count} impressions");

                using var request = CreatePost("/sdk/ad-revenue", json);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _adOfflineQueue.RemoveRange(0, batch.Count);
                    SaveAdOfflineQueue();
                    Log($"Offline ad batch sent: {batch.Count}");
                }
                else
                {
                    Log("Offline ad flush failed, will retry later");
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _adBatch.Count > 0)
            {
                // App going to background — flush any pending ad impressions
                FlushAdBatchNow();
            }
        }

        private void OnApplicationQuit()
        {
            // Save any pending ad impressions to offline queue before exit
            if (_adBatch.Count > 0)
            {
                lock (_adBatch)
                {
                    _adOfflineQueue.AddRange(_adBatch);
                    _adBatch.Clear();
                    SaveAdOfflineQueue();
                }
            }
        }

        #endregion

        #region HTTP Helpers

        /// <summary>Check if the error is retryable (network error or 5xx server error)</summary>
        private static bool IsRetryableError(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError) return true;
            if (request.responseCode >= 500 && request.responseCode < 600) return true;
            if (request.responseCode == 429) return true; // rate limited
            return false;
        }

        private UnityWebRequest CreatePost(string path, string json)
        {
            string url = _config.BaseUrl.TrimEnd('/') + path;
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Api-Key", _config.ApiKey);

            // HMAC signature
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string payload = timestamp + "." + json;
            string signature = HmacHelper.ComputeHmacSha256(payload, _config.ApiKey);
            request.SetRequestHeader("X-Timestamp", timestamp);
            request.SetRequestHeader("X-Signature", signature);

            return request;
        }

        #endregion

        #region Platform Helpers

        private static string GetPlatform()
        {
#if UNITY_ANDROID
            return "android";
#elif UNITY_IOS
            return "ios";
#elif UNITY_WEBGL
            return "web";
#else
            return "other";
#endif
        }

        private static string GetGAID()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var adIdClass = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");
                var adInfo = adIdClass.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity);
                return adInfo.Call<string>("getId");
            }
            catch
            {
                return null;
            }
#else
            return null;
#endif
        }

        private static string GetIDFV()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return UnityEngine.iOS.Device.vendorIdentifier;
#else
            return null;
#endif
        }

        #endregion

        #region Offline Queue (Purchases)

        private void EnqueueOffline(PurchaseEvent purchase)
        {
            if (_offlineQueue.Count >= MaxQueueSize)
            {
                _offlineQueue.Dequeue(); // drop oldest
            }
            _offlineQueue.Enqueue(purchase);
            SaveOfflineQueue();
        }

        private void SaveOfflineQueue()
        {
            var wrapper = new QueueWrapper { items = new List<PurchaseEvent>(_offlineQueue) };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(QueueKey, json);
            PlayerPrefs.Save();
        }

        private void LoadOfflineQueue()
        {
            string json = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<QueueWrapper>(json);
                if (wrapper?.items == null) return;

                var cutoff = DateTime.UtcNow.AddDays(-7);
                foreach (var item in wrapper.items)
                {
                    if (DateTime.TryParse(item.Timestamp, out var ts) && ts > cutoff)
                    {
                        _offlineQueue.Enqueue(item);
                    }
                }
            }
            catch { /* corrupted data, ignore */ }
        }

        #endregion

        #region Offline Queue (Ad Impressions)

        private void EnqueueAdOffline(List<AdImpressionItem> items)
        {
            _adOfflineQueue.AddRange(items);
            // Trim to max size (drop oldest)
            while (_adOfflineQueue.Count > AdMaxOfflineQueue)
            {
                _adOfflineQueue.RemoveAt(0);
            }
            SaveAdOfflineQueue();
        }

        private void SaveAdOfflineQueue()
        {
            var wrapper = new AdQueueWrapper { items = new List<AdImpressionItem>(_adOfflineQueue) };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(AdQueueKey, json);
            PlayerPrefs.Save();
        }

        private void LoadAdOfflineQueue()
        {
            string json = PlayerPrefs.GetString(AdQueueKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<AdQueueWrapper>(json);
                if (wrapper?.items != null)
                {
                    _adOfflineQueue.AddRange(wrapper.items);
                }
            }
            catch { /* corrupted data, ignore */ }
        }

        #endregion

        #region Utility

        private static string ToSourceString(PaymentSource source)
        {
            switch (source)
            {
                case PaymentSource.GooglePlay: return "google_play";
                case PaymentSource.Apple: return "apple";
                case PaymentSource.YooKassa: return "yookassa";
                case PaymentSource.RuStore: return "rustore";
                case PaymentSource.TBank: return "tbank";
                case PaymentSource.Web: return "web";
                default: return "other";
            }
        }

        private static string ToAdTypeString(AdType adType)
        {
            switch (adType)
            {
                case AdType.Rewarded: return "rewarded";
                case AdType.Interstitial: return "interstitial";
                case AdType.Banner: return "banner";
                default: return "banner";
            }
        }

        private static string ToAdNetworkString(AdNetwork network)
        {
            switch (network)
            {
                case AdNetwork.AdMob: return "admob";
                case AdNetwork.IronSource: return "ironsource";
                case AdNetwork.AppLovin: return "applovin";
                case AdNetwork.UnityAds: return "unity_ads";
                case AdNetwork.YandexAds: return "yandex_ads";
                default: return "other";
            }
        }

        private static bool EnsureInitialized()
        {
            if (!_initialized)
            {
                Debug.LogError("[GameDevPartner] SDK not initialized. Call GameDevPartnerSDK.Init() first.");
                return false;
            }
            return true;
        }

        private static void Log(string message)
        {
            if (_config != null && _config.DebugMode)
                Debug.Log($"[GameDevPartner] {message}");
        }

        #endregion
    }
}
