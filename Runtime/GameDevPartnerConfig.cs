using UnityEngine;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// ScriptableObject that stores SDK settings.
    /// Created automatically by the Editor settings window.
    /// Loaded at runtime from Resources/ for auto-initialization.
    /// </summary>
    public class GameDevPartnerConfig : ScriptableObject
    {
        private const string ResourcePath = "GameDevPartnerConfig";

        [Header("API Key (Live or Test) — gamedevpartner.ru -> Мои игры -> API-ключ")]
        public string ApiKey = "";

        [Header("Region")]
        public SDKRegion Region = SDKRegion.RU;

        [Header("Debug Logging")]
        public bool DebugMode = true;

        [Header("Custom Base URL (leave empty for default)")]
        public string CustomBaseUrl = "";

        [Header("Auto-identify player using device ID")]
        public bool AutoIdentify = true;

        /// <summary>Load config from Resources at runtime.</summary>
        public static GameDevPartnerConfig Load()
        {
            return Resources.Load<GameDevPartnerConfig>(ResourcePath);
        }

        public SDKConfig ToSDKConfig()
        {
            return new SDKConfig
            {
                ApiKey = ApiKey,
                Region = Region,
                DebugMode = DebugMode,
                CustomBaseUrl = CustomBaseUrl
            };
        }

#if UNITY_EDITOR
        private const string AssetDir = "Assets/Resources";
        private const string AssetPath = AssetDir + "/GameDevPartnerConfig.asset";

        /// <summary>Get or create the config asset in Editor.</summary>
        public static GameDevPartnerConfig GetOrCreate()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDevPartnerConfig>(AssetPath);
            if (config != null) return config;

            // Ensure Resources folder exists
            if (!UnityEditor.AssetDatabase.IsValidFolder(AssetDir))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            config = CreateInstance<GameDevPartnerConfig>();
            UnityEditor.AssetDatabase.CreateAsset(config, AssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[GameDevPartner] Created config asset at " + AssetPath);
            return config;
        }
#endif
    }
}
