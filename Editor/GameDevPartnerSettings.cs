using UnityEditor;
using UnityEngine;

namespace GameDevPartner.SDK.Editor
{
    /// <summary>
    /// Editor window for configuring GameDevPartner SDK settings.
    /// Access via menu: Window > GameDevPartner > Settings
    /// Settings are saved as a ScriptableObject in Assets/Resources/ and included in builds.
    /// </summary>
    public class GameDevPartnerSettings : EditorWindow
    {
        private GameDevPartnerConfig _config;
        private UnityEditor.Editor _configEditor;

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
            {
                _config = GameDevPartnerConfig.GetOrCreate();
            }

            GUILayout.Label("GameDevPartner SDK", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Draw config fields
            _config.ApiKey = EditorGUILayout.TextField("API Key", _config.ApiKey);
            EditorGUILayout.HelpBox(
                "Скопируйте API Key (Live или Test) из личного кабинета:\n" +
                "gamedevpartner.ru -> Мои игры -> API-ключ",
                MessageType.None);

            EditorGUILayout.Space();
            _config.Region = (SDKRegion)EditorGUILayout.EnumPopup("Region", _config.Region);
            _config.DebugMode = EditorGUILayout.Toggle("Debug Mode", _config.DebugMode);
            _config.AutoIdentify = EditorGUILayout.Toggle("Auto-Identify Player", _config.AutoIdentify);
            EditorGUILayout.HelpBox(
                "Auto-Identify: SDK автоматически идентифицирует игрока по device ID.\n" +
                "Если у вас есть свой player ID — выключите и вызовите GameDevPartnerSDK.IdentifyPlayer() вручную.",
                MessageType.None);

            EditorGUILayout.Space();
            GUILayout.Label("Advanced", EditorStyles.boldLabel);
            _config.CustomBaseUrl = EditorGUILayout.TextField("Custom Base URL", _config.CustomBaseUrl);

            EditorGUILayout.Space();

            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                Debug.Log("[GameDevPartner] Settings saved to Assets/Resources/GameDevPartnerConfig.asset");
            }

            EditorGUILayout.Space();

            // Status
            var statusStyle = new GUIStyle(EditorStyles.helpBox);
            bool ready = !string.IsNullOrEmpty(_config.ApiKey);
            if (ready)
            {
                EditorGUILayout.HelpBox(
                    "SDK готов к работе!\n\n" +
                    "SDK автоматически инициализируется при запуске игры.\n" +
                    "Никакого кода писать не нужно.\n\n" +
                    "Единственное, что нужно вызвать вручную — отправка покупки:\n\n" +
                    "   GameDevPartnerSDK.TrackPurchase(new PurchaseEvent {\n" +
                    "       ProductId = \"gems_100\",\n" +
                    "       Amount = 99,\n" +
                    "       Currency = \"RUB\",\n" +
                    "       TransactionId = txId\n" +
                    "   });",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Введите API Key для активации SDK.\n" +
                    "Получить ключ: gamedevpartner.ru -> Мои игры -> API-ключ",
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
            // Auto-save on close
            if (_config != null)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
