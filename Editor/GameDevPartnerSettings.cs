using UnityEditor;
using UnityEngine;

namespace GameDevPartner.SDK.Editor
{
    /// <summary>
    /// Editor window for configuring GameDevPartner SDK settings.
    /// Access via menu: Window > GameDevPartner > Settings
    /// </summary>
    public class GameDevPartnerSettings : EditorWindow
    {
        private GameDevPartnerConfig _config;

        [MenuItem("Window/GameDevPartner/Settings")]
        public static void ShowWindow()
        {
            GetWindow<GameDevPartnerSettings>("GameDevPartner SDK");
        }

        private void OnEnable()
        {
            _config = GameDevPartnerConfig.GetOrCreate();
        }

        private void OnGUI()
        {
            if (_config == null)
                _config = GameDevPartnerConfig.GetOrCreate();

            GUILayout.Label("GameDevPartner SDK v2.5.0", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // === API Key ===
            _config.ApiKey = EditorGUILayout.TextField("API Key", _config.ApiKey);
            EditorGUILayout.HelpBox(
                "Скопируйте API Key из личного кабинета:\ngamedevpartner.ru -> Мои игры -> API-ключ",
                MessageType.None);

            EditorGUILayout.Space();
            _config.Region = (SDKRegion)EditorGUILayout.EnumPopup("Region", _config.Region);
            _config.DebugMode = EditorGUILayout.Toggle("Debug Mode", _config.DebugMode);
            _config.AutoIdentify = EditorGUILayout.Toggle("Auto-Identify Player", _config.AutoIdentify);

            // === Advanced ===
            EditorGUILayout.Space(10);
            GUILayout.Label("Advanced", EditorStyles.boldLabel);
            _config.CustomBaseUrl = EditorGUILayout.TextField("Custom Base URL", _config.CustomBaseUrl);

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Settings"))
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                Debug.Log("[GameDevPartner] Settings saved");
            }

            EditorGUILayout.Space();

            // === Status & Quick Reference ===
            bool ready = !string.IsNullOrEmpty(_config.ApiKey);
            if (ready)
            {
                EditorGUILayout.HelpBox(
                    "SDK ready!\n\n" +
                    "Purchases:\n  GameDevPartnerSDK.TrackPurchase(event)\n\n" +
                    "Ad Revenue (1 line in your ad callback):\n  GameDevPartnerSDK.TrackAdRevenue(revenue, \"USD\", \"rewarded\", \"yandex_ads\")\n\n" +
                    "Supported ad networks: admob, ironsource, applovin, unity_ads, yandex_ads, other",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Enter API Key to activate SDK.\ngamedevpartner.ru -> My Games -> API Key",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Select Config Asset"))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }
        }

        private void OnDisable()
        {
            if (_config != null)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
