using System;
using System.Collections;
using System.Collections.Generic;
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

        private readonly Queue<PurchaseEvent> _offlineQueue = new Queue<PurchaseEvent>();
        private const int MaxQueueSize = 100;
        private const string QueueKey = "gdp_offline_queue";
        private const string AttributedKey = "gdp_attributed";
        private const string PlayerIdKey = "gdp_player_id";

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

            // Flush offline queue
            _instance.StartCoroutine(_instance.FlushOfflineQueue());

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

        #region Internal Coroutines

        private IEnumerator DoIdentify(string playerId, string referrer)
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
                if (response.data.attributed)
                {
                    _identified = true;
                    PlayerPrefs.SetInt(AttributedKey, 1);
                    Log($"Player attributed! match_type={response.data.match_type}");
                }
                else
                {
                    Log("Player not attributed (organic)");
                }
            }
            else
            {
                Debug.LogWarning($"[GameDevPartner] Identify failed: {request.error}");
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

        #endregion

        #region HTTP Helpers

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

        #region Offline Queue

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
