using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevPartner.SDK.Editor
{
    /// <summary>
    /// Manages Scripting Define Symbols for ad SDK adapters.
    /// Provides methods to add/remove defines via the Settings UI.
    /// Does NOT auto-add symbols — developer controls which adapters are active.
    /// </summary>
    public static class GDPDefineSymbolsManager
    {
        public static readonly string[] AdDefineSymbols = {
            "GDP_ADMOB",
            "GDP_IRONSOURCE",
            "GDP_APPLOVIN",
            "GDP_UNITY_ADS",
            "GDP_YANDEX_ADS",
        };

        public static readonly Dictionary<string, string> AdSdkLabels = new Dictionary<string, string>
        {
            { "GDP_ADMOB",       "AdMob (Google Mobile Ads)" },
            { "GDP_IRONSOURCE",  "IronSource / LevelPlay" },
            { "GDP_APPLOVIN",    "AppLovin MAX" },
            { "GDP_UNITY_ADS",   "Unity Ads" },
            { "GDP_YANDEX_ADS",  "Yandex Mobile Ads" },
        };

        public static readonly Dictionary<string, string> AdSdkHints = new Dictionary<string, string>
        {
            { "GDP_ADMOB",       "После загрузки рекламы: GDPAdMobAdapter.TrackRewarded(ad)" },
            { "GDP_IRONSOURCE",  "Полностью автоматически — ноль кода" },
            { "GDP_APPLOVIN",    "Полностью автоматически — ноль кода" },
            { "GDP_UNITY_ADS",   "В callback: GDPUnityAdsAdapter.TrackShowComplete(id)" },
            { "GDP_YANDEX_ADS",  "В callback: GDPYandexAdsAdapter.TrackFromImpressionData(...)" },
        };

        public static bool HasDefine(string symbol)
        {
            return GetCurrentDefines().Contains(symbol);
        }

        public static void SetDefine(string symbol, bool enabled)
        {
            var defines = GetCurrentDefines();
            bool has = defines.Contains(symbol);

            if (enabled && !has)
            {
                defines.Add(symbol);
                ApplyDefines(defines);
                Debug.Log($"[GameDevPartner] Включён адаптер {symbol}");
            }
            else if (!enabled && has)
            {
                defines.Remove(symbol);
                ApplyDefines(defines);
                Debug.Log($"[GameDevPartner] Выключен адаптер {symbol}");
            }
        }

        private static HashSet<string> GetCurrentDefines()
        {
#if UNITY_2021_2_OR_NEWER
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string current = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
            return new HashSet<string>(current.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private static void ApplyDefines(HashSet<string> defines)
        {
            string result = string.Join(";", defines);
#if UNITY_2021_2_OR_NEWER
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, result);
#else
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, result);
#endif
        }
    }
}
