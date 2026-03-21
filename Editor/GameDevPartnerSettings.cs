using System.Linq;
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

            GUILayout.Label("GameDevPartner SDK v2.4.0", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // === API Key ===
            _config.ApiKey = EditorGUILayout.TextField("API Key", _config.ApiKey);
            EditorGUILayout.HelpBox(
                "Скопируйте API Key из личного кабинета:\ngamedevpartner.ru → Мои игры → API-ключ",
                MessageType.None);

            EditorGUILayout.Space();
            _config.Region = (SDKRegion)EditorGUILayout.EnumPopup("Region", _config.Region);
            _config.DebugMode = EditorGUILayout.Toggle("Debug Mode", _config.DebugMode);
            _config.AutoIdentify = EditorGUILayout.Toggle("Auto-Identify Player", _config.AutoIdentify);

            // === Ad Revenue ===
            EditorGUILayout.Space(10);
            GUILayout.Label("📺 Рекламная монетизация", EditorStyles.boldLabel);
            _config.EnableAdRevenueTracking = EditorGUILayout.Toggle("Трекинг рекламного дохода", _config.EnableAdRevenueTracking);

            if (_config.EnableAdRevenueTracking)
            {
                EditorGUILayout.HelpBox(
                    "Включите рекламные сети, которые используются в игре.\n" +
                    "IronSource и AppLovin — полностью автоматически (ноль кода).\n" +
                    "Остальные — одна строка кода в callback.\n\n" +
                    "Универсальный способ (без галочек, для любой сети):\n" +
                    "GameDevPartnerSDK.TrackAdRevenue(revenue, \"USD\", \"rewarded\", \"yandex_ads\");",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                foreach (var symbol in GDPDefineSymbolsManager.AdDefineSymbols)
                {
                    bool current = GDPDefineSymbolsManager.HasDefine(symbol);
                    string label = GDPDefineSymbolsManager.AdSdkLabels[symbol];
                    string hint = GDPDefineSymbolsManager.AdSdkHints[symbol];

                    EditorGUILayout.BeginVertical("box");

                    bool newValue = EditorGUILayout.ToggleLeft(label, current, EditorStyles.boldLabel);
                    if (newValue != current)
                    {
                        GDPDefineSymbolsManager.SetDefine(symbol, newValue);
                    }

                    if (current)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("✅ " + hint, EditorStyles.wordWrappedMiniLabel);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "⚠️ Включайте только те сети, SDK которых установлен в проекте!\n" +
                    "Иначе будут ошибки компиляции.",
                    MessageType.Warning);
            }

            // === Advanced ===
            EditorGUILayout.Space(10);
            GUILayout.Label("Advanced", EditorStyles.boldLabel);
            _config.CustomBaseUrl = EditorGUILayout.TextField("Custom Base URL", _config.CustomBaseUrl);

            EditorGUILayout.Space();

            if (GUILayout.Button("💾 Сохранить настройки"))
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                Debug.Log("[GameDevPartner] Настройки сохранены");
            }

            EditorGUILayout.Space();

            // === Status ===
            bool ready = !string.IsNullOrEmpty(_config.ApiKey);
            if (ready)
            {
                int adCount = GDPDefineSymbolsManager.AdDefineSymbols.Count(GDPDefineSymbolsManager.HasDefine);
                string adStatus = adCount > 0
                    ? $"Рекламных сетей: {adCount}"
                    : "Рекламные сети не подключены";

                EditorGUILayout.HelpBox(
                    $"✅ SDK готов к работе!\n\n" +
                    $"Покупки: GameDevPartnerSDK.TrackPurchase()\n" +
                    $"Реклама: {adStatus}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Введите API Key для активации SDK.\ngamedevpartner.ru → Мои игры → API-ключ",
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
