using System;
using UnityEngine;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Automatically retrieves Install Referrer from Google Play and RuStore on Android.
    /// No manual setup needed — SDK calls this internally during IdentifyPlayer.
    ///
    /// Google Play: requires com.android.installreferrer:installreferrer library in gradle.
    /// RuStore: uses RuStore Unity SDK InstallReferrerClient if present (via reflection).
    /// Both are optional — SDK gracefully falls back if libraries are not present.
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

            // Check PlayerPrefs first (referrer doesn't change after install)
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

        // =====================================================
        // Google Play Install Referrer API
        // Docs: https://developer.android.com/google/play/installreferrer/library
        // Requires gradle: com.android.installreferrer:installreferrer:2.2
        // =====================================================

        private static void TryGooglePlayReferrer(Action<string> onComplete)
        {
            try
            {
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");

                // InstallReferrerClient.newBuilder(context).build()
                var clientClass = new AndroidJavaClass("com.android.installreferrer.api.InstallReferrerClient");
                var client = clientClass.CallStatic<AndroidJavaObject>("newBuilder", activity)
                    .Call<AndroidJavaObject>("build");

                client.Call("startConnection", new GooglePlayReferrerListener(client, onComplete));
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameDevPartner] Google Play Install Referrer not available: {ex.Message}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// AndroidJavaProxy implementing com.android.installreferrer.api.InstallReferrerStateListener
        /// </summary>
        private class GooglePlayReferrerListener : AndroidJavaProxy
        {
            private readonly AndroidJavaObject _client;
            private readonly Action<string> _onComplete;
            private bool _completed;

            public GooglePlayReferrerListener(AndroidJavaObject client, Action<string> onComplete)
                : base("com.android.installreferrer.api.InstallReferrerStateListener")
            {
                _client = client;
                _onComplete = onComplete;
            }

            // InstallReferrerStateListener.onInstallReferrerSetupFinished(int responseCode)
            // responseCode: 0=OK, 1=FEATURE_NOT_SUPPORTED, 2=SERVICE_UNAVAILABLE
            void onInstallReferrerSetupFinished(int responseCode)
            {
                if (_completed) return;
                _completed = true;

                try
                {
                    if (responseCode == 0) // InstallReferrerResponse.OK
                    {
                        // ReferrerDetails details = client.getInstallReferrer()
                        var details = _client.Call<AndroidJavaObject>("getInstallReferrer");
                        // String referrer = details.getInstallReferrer()
                        string referrer = details.Call<string>("getInstallReferrer");
                        Debug.Log($"[GameDevPartner] Google Play referrer: {referrer}");

                        string parsed = ParseReferrerCode(referrer);
                        _onComplete?.Invoke(parsed);
                    }
                    else
                    {
                        Debug.Log($"[GameDevPartner] Google Play referrer responseCode={responseCode}");
                        _onComplete?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"[GameDevPartner] Google Play referrer error: {ex.Message}");
                    _onComplete?.Invoke(null);
                }
                finally
                {
                    try { _client.Call("endConnection"); } catch { }
                }
            }

            void onInstallReferrerServiceDisconnected()
            {
                // Service disconnected — no retry needed, one-time fetch
                if (!_completed)
                {
                    _completed = true;
                    _onComplete?.Invoke(null);
                }
            }
        }

        // =====================================================
        // RuStore Install Referrer SDK (Unity C# API)
        // Docs: https://www.rustore.ru/help/en/sdk/install-referrer/unity/9-0-2
        // Uses reflection to avoid hard dependency on RuStore SDK package.
        // API: InstallReferrerClient.Instance.Init()
        //      InstallReferrerClient.Instance.GetInstallReferrer(onFailure, onSuccess)
        //      result.referrerId -> the referrer string
        // =====================================================

        private static void TryRuStoreReferrer(Action<string> onComplete)
        {
            try
            {
                // Find RuStore InstallReferrerClient type via reflection
                System.Type clientType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    clientType = asm.GetType("RuStore.InstallReferrer.InstallReferrerClient");
                    if (clientType != null) break;
                    // Try alternative namespace
                    clientType = asm.GetType("RuStoreSdk.InstallReferrerClient");
                    if (clientType != null) break;
                }

                if (clientType == null)
                {
                    Debug.Log("[GameDevPartner] RuStore Install Referrer SDK not found");
                    onComplete?.Invoke(null);
                    return;
                }

                // Get singleton: InstallReferrerClient.Instance
                var instanceProp = clientType.GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp == null)
                {
                    Debug.Log("[GameDevPartner] RuStore InstallReferrerClient.Instance not found");
                    onComplete?.Invoke(null);
                    return;
                }

                var instance = instanceProp.GetValue(null);
                if (instance == null)
                {
                    Debug.Log("[GameDevPartner] RuStore InstallReferrerClient.Instance is null");
                    onComplete?.Invoke(null);
                    return;
                }

                // Call Init()
                var initMethod = clientType.GetMethod("Init");
                if (initMethod != null)
                    initMethod.Invoke(instance, null);

                // Call GetInstallReferrer(onFailure, onSuccess)
                var getMethod = clientType.GetMethod("GetInstallReferrer");
                if (getMethod == null)
                {
                    Debug.Log("[GameDevPartner] RuStore GetInstallReferrer method not found");
                    onComplete?.Invoke(null);
                    return;
                }

                // Build callbacks using reflection to match delegate types
                var methodParams = getMethod.GetParameters();
                if (methodParams.Length >= 2)
                {
                    // onFailure callback
                    var failureType = methodParams[0].ParameterType;
                    var failureDelegate = CreateDelegateForAction(failureType, (object error) =>
                    {
                        Debug.Log($"[GameDevPartner] RuStore referrer error: {error}");
                        onComplete?.Invoke(null);
                    });

                    // onSuccess callback
                    var successType = methodParams[1].ParameterType;
                    var successDelegate = CreateDelegateForAction(successType, (object result) =>
                    {
                        try
                        {
                            // result.referrerId
                            var referrerProp = result.GetType().GetProperty("referrerId");
                            string referrerId = referrerProp?.GetValue(result) as string;
                            Debug.Log($"[GameDevPartner] RuStore referrer: {referrerId}");
                            onComplete?.Invoke(referrerId);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"[GameDevPartner] RuStore referrer parse error: {ex.Message}");
                            onComplete?.Invoke(null);
                        }
                    });

                    getMethod.Invoke(instance, new object[] { failureDelegate, successDelegate });
                }
                else
                {
                    Debug.Log("[GameDevPartner] RuStore GetInstallReferrer unexpected signature");
                    onComplete?.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameDevPartner] RuStore Install Referrer not available: {ex.Message}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// Create a delegate matching the target Action-like type via reflection.
        /// </summary>
        private static Delegate CreateDelegateForAction(System.Type delegateType, Action<object> handler)
        {
            // Get the Invoke method to understand the delegate signature
            var invokeMethod = delegateType.GetMethod("Invoke");
            if (invokeMethod == null) return null;

            var invokeParams = invokeMethod.GetParameters();
            if (invokeParams.Length == 1)
            {
                // Create Action<T> where T is the parameter type
                var paramType = invokeParams[0].ParameterType;
                var actionType = typeof(Action<>).MakeGenericType(paramType);

                // Use a wrapper that boxes the parameter
                var wrapperMethod = typeof(GDPInstallReferrer)
                    .GetMethod(nameof(InvokeObjectAction), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                // Store the handler in a closure-like approach
                _tempActionHandler = handler;
                return Delegate.CreateDelegate(delegateType, wrapperMethod.MakeGenericMethod(paramType));
            }

            return null;
        }

        private static Action<object> _tempActionHandler;

        private static void InvokeObjectAction<T>(T value)
        {
            _tempActionHandler?.Invoke(value);
        }

#endif

        /// <summary>
        /// Parse referrer string to extract GameDevPartner link code.
        /// Google Play format: "utm_source=gamedevpartner&amp;utm_content=aBcD1f2&amp;utm_medium=influencer"
        /// RuStore format: just the referrerId string directly.
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

            // If short code (no key=value format), return as-is (RuStore referrerId)
            if (referrer.Length <= 16 && !referrer.Contains("=") && !referrer.Contains("&"))
                return referrer;

            // Return full string as fallback (server can parse it)
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
