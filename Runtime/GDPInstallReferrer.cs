using System;
using UnityEngine;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Automatically retrieves Install Referrer from Google Play and RuStore on Android.
    /// No manual setup needed — SDK calls this internally during IdentifyPlayer.
    /// Requires install referrer libraries in gradle (added automatically via mainTemplate).
    /// </summary>
    internal static class GDPInstallReferrer
    {
        private static string _cachedReferrer;
        private static bool _fetched;

        /// <summary>
        /// Get cached install referrer string, or null if not available.
        /// Call FetchReferrer() first to populate.
        /// </summary>
        internal static string GetCachedReferrer()
        {
            return _cachedReferrer;
        }

        /// <summary>
        /// Fetch install referrer from Google Play or RuStore.
        /// Stores result in cache and PlayerPrefs for persistence.
        /// </summary>
        internal static void FetchReferrer(Action<string> onComplete = null)
        {
            // Return cached value if already fetched
            if (_fetched)
            {
                onComplete?.Invoke(_cachedReferrer);
                return;
            }

            // Check PlayerPrefs first (referrer doesn't change)
            string saved = PlayerPrefs.GetString("gdp_install_referrer", "");
            if (!string.IsNullOrEmpty(saved))
            {
                _cachedReferrer = saved;
                _fetched = true;
                onComplete?.Invoke(_cachedReferrer);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // Try Google Play Install Referrer first, then RuStore
            TryGooglePlayReferrer((gpRef) =>
            {
                if (!string.IsNullOrEmpty(gpRef))
                {
                    CacheReferrer(gpRef);
                    onComplete?.Invoke(gpRef);
                }
                else
                {
                    TryRuStoreReferrer((rsRef) =>
                    {
                        CacheReferrer(rsRef);
                        onComplete?.Invoke(rsRef);
                    });
                }
            });
#else
            _fetched = true;
            onComplete?.Invoke(null);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void TryGooglePlayReferrer(Action<string> onComplete)
        {
            try
            {
                // com.android.installreferrer.api.InstallReferrerClient
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");

                var clientClass = new AndroidJavaClass("com.android.installreferrer.api.InstallReferrerClient");
                var client = clientClass.CallStatic<AndroidJavaObject>("newBuilder", activity)
                    .Call<AndroidJavaObject>("build");

                client.Call("startConnection", new InstallReferrerStateListener(client, onComplete));
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameDevPartner] Google Play Install Referrer not available: {ex.Message}");
                onComplete?.Invoke(null);
            }
        }

        private static void TryRuStoreReferrer(Action<string> onComplete)
        {
            try
            {
                // ru.rustore.sdk.installreferrer.RuStoreInstallReferrerClient
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");

                var clientClass = new AndroidJavaClass("ru.rustore.sdk.installreferrer.RuStoreInstallReferrerClient");
                var client = clientClass.CallStatic<AndroidJavaObject>("create", activity);

                client.Call("startConnection", new RuStoreReferrerStateListener(client, onComplete));
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameDevPartner] RuStore Install Referrer not available: {ex.Message}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// Callback listener for Google Play Install Referrer API.
        /// </summary>
        private class InstallReferrerStateListener : AndroidJavaProxy
        {
            private readonly AndroidJavaObject _client;
            private readonly Action<string> _onComplete;

            public InstallReferrerStateListener(AndroidJavaObject client, Action<string> onComplete)
                : base("com.android.installreferrer.api.InstallReferrerStateListener")
            {
                _client = client;
                _onComplete = onComplete;
            }

            // Called on connection established
            void onInstallReferrerSetupFinished(int responseCode)
            {
                try
                {
                    if (responseCode == 0) // OK
                    {
                        var details = _client.Call<AndroidJavaObject>("getInstallReferrer");
                        string referrer = details.Call<string>("getInstallReferrer");
                        Debug.Log($"[GameDevPartner] Google Play referrer: {referrer}");

                        // Parse utm_content or gdp_ref from referrer string
                        string parsed = ParseReferrerCode(referrer);
                        _onComplete?.Invoke(parsed);
                    }
                    else
                    {
                        Debug.Log($"[GameDevPartner] Google Play referrer response code: {responseCode}");
                        _onComplete?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"[GameDevPartner] Google Play referrer parse error: {ex.Message}");
                    _onComplete?.Invoke(null);
                }
                finally
                {
                    try { _client.Call("endConnection"); } catch { }
                }
            }

            void onInstallReferrerServiceDisconnected()
            {
                // Connection lost, no-op
            }
        }

        /// <summary>
        /// Callback listener for RuStore Install Referrer API.
        /// </summary>
        private class RuStoreReferrerStateListener : AndroidJavaProxy
        {
            private readonly AndroidJavaObject _client;
            private readonly Action<string> _onComplete;

            public RuStoreReferrerStateListener(AndroidJavaObject client, Action<string> onComplete)
                : base("ru.rustore.sdk.installreferrer.RuStoreInstallReferrerStateListener")
            {
                _client = client;
                _onComplete = onComplete;
            }

            void onInstallReferrerSetupFinished(int responseCode)
            {
                try
                {
                    if (responseCode == 0) // OK
                    {
                        var details = _client.Call<AndroidJavaObject>("getInstallReferrer");
                        string referrer = details.Call<string>("getInstallReferrer");
                        Debug.Log($"[GameDevPartner] RuStore referrer: {referrer}");

                        string parsed = ParseReferrerCode(referrer);
                        _onComplete?.Invoke(parsed);
                    }
                    else
                    {
                        _onComplete?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"[GameDevPartner] RuStore referrer parse error: {ex.Message}");
                    _onComplete?.Invoke(null);
                }
                finally
                {
                    try { _client.Call("endConnection"); } catch { }
                }
            }

            void onInstallReferrerServiceDisconnected()
            {
                // Connection lost, no-op
            }
        }
#endif

        /// <summary>
        /// Parse referrer string to extract GameDevPartner link code.
        /// Referrer format from store: "utm_source=gamedevpartner&utm_content=aBcD1f2&utm_medium=influencer"
        /// or just the raw link code.
        /// </summary>
        internal static string ParseReferrerCode(string referrer)
        {
            if (string.IsNullOrEmpty(referrer)) return null;

            // Try to find utm_content=CODE (our tracking link code)
            if (referrer.Contains("utm_content="))
            {
                foreach (var pair in referrer.Split('&'))
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2 && kv[0] == "utm_content")
                        return Uri.UnescapeDataString(kv[1]);
                }
            }

            // Try gdp_ref=CODE
            if (referrer.Contains("gdp_ref="))
            {
                foreach (var pair in referrer.Split('&'))
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2 && kv[0] == "gdp_ref")
                        return Uri.UnescapeDataString(kv[1]);
                }
            }

            // If it looks like a short code (base64url, 8 chars), return as-is
            if (referrer.Length <= 12 && !referrer.Contains("=") && !referrer.Contains("&"))
                return referrer;

            return referrer;
        }

        private static void CacheReferrer(string referrer)
        {
            _cachedReferrer = referrer;
            _fetched = true;
            if (!string.IsNullOrEmpty(referrer))
            {
                PlayerPrefs.SetString("gdp_install_referrer", referrer);
                PlayerPrefs.Save();
            }
        }
    }
}
