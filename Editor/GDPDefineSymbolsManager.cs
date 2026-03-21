using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevPartner.SDK.Editor
{
    /// <summary>
    /// Automatically detects installed ad SDKs and manages Scripting Define Symbols.
    /// Runs on every script recompilation (when assemblies are reloaded).
    /// Developer doesn't need to manually add GDP_ADMOB etc. — it's automatic.
    /// </summary>
    [InitializeOnLoad]
    public static class GDPDefineSymbolsManager
    {
        // Map: define symbol → type to check for (fully qualified)
        private static readonly Dictionary<string, string> AdSdkDetection = new Dictionary<string, string>
        {
            { "GDP_ADMOB",       "GoogleMobileAds.Api.MobileAds" },
            { "GDP_IRONSOURCE",  "IronSourceEvents" },
            { "GDP_APPLOVIN",    "MaxSdkCallbacks" },
            { "GDP_UNITY_ADS",   "UnityEngine.Advertisements.Advertisement" },
            { "GDP_YANDEX_ADS",  "YandexMobileAds.Base.AdValue" },
        };

        static GDPDefineSymbolsManager()
        {
            // Run after compilation finishes
            EditorApplication.delayCall += SyncDefineSymbols;
        }

        /// <summary>
        /// Check which ad SDKs are installed and add/remove define symbols.
        /// </summary>
        public static void SyncDefineSymbols()
        {
            var config = GameDevPartnerConfig.GetOrCreate();
            if (!config.EnableAdRevenueTracking) return;

#if UNITY_2021_2_OR_NEWER
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

            var defines = new HashSet<string>(currentDefines.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)));
            bool changed = false;

            foreach (var kvp in AdSdkDetection)
            {
                bool sdkInstalled = IsTypeAvailable(kvp.Value);

                if (sdkInstalled && !defines.Contains(kvp.Key))
                {
                    defines.Add(kvp.Key);
                    changed = true;
                    Debug.Log($"[GameDevPartner] Auto-detected {kvp.Key} — ad SDK found ({kvp.Value})");
                }
                else if (!sdkInstalled && defines.Contains(kvp.Key))
                {
                    defines.Remove(kvp.Key);
                    changed = true;
                    Debug.Log($"[GameDevPartner] Removed {kvp.Key} — ad SDK not found");
                }
            }

            if (changed)
            {
                string newDefines = string.Join(";", defines);
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
#endif
                Debug.Log($"[GameDevPartner] Updated Scripting Define Symbols: {newDefines}");
            }
        }

        private static bool IsTypeAvailable(string typeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetType(typeName) != null)
                        return true;
                }
                catch
                {
                    // Some assemblies may throw on reflection — skip
                }
            }
            return false;
        }
    }
}
