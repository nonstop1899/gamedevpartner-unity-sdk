using System.Collections.Generic;
using System.Linq;
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

            GUILayout.Label("GameDevPartner SDK v2.3.0", EditorStyles.boldLabel);
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

            // === Ad Revenue Section ===
            GUILayout.Label("Рекламная монетизация", EditorStyles.boldLabel);
            _config.EnableAdRevenueTracking = EditorGUILayout.Toggle("Трекинг рекламного дохода", _config.EnableAdRevenueTracking);

            if (_config.EnableAdRevenueTracking)
            {
                EditorGUILayout.HelpBox(
                    "SDK автоматически определяет установленные рекламные SDK и подключает трекинг.\n" +
                    "Define Symbols добавляются автоматически при импорте SDK рекламных сетей.",
                    MessageType.Info);

                // Show detected ad SDKs
                EditorGUI.indentLevel++;
                DrawAdSdkStatus("AdMob", "GDP_ADMOB", "Google Mobile Ads",
                    "IronSource/AppLovin — полностью автоматически.\n" +
                    "AdMob — вызовите GDPAdMobAdapter.TrackRewarded(ad) после загрузки рекламы.");
                DrawAdSdkStatus("IronSource", "GDP_IRONSOURCE", "IronSource/LevelPlay",
                    "Полностью автоматически — SDK сам подписывается на ImpressionDataReady.");
                DrawAdSdkStatus("AppLovin MAX", "GDP_APPLOVIN", "AppLovin MAX",
                    "Полностью автоматически — SDK сам подписывается на OnAdRevenuePaid.");
                DrawAdSdkStatus("Unity Ads", "GDP_UNITY_ADS", "Unity Ads",
                    "Вызовите GDPUnityAdsAdapter.TrackShowComplete() из OnUnityAdsShowComplete().");
                DrawAdSdkStatus("Yandex Ads", "GDP_YANDEX_ADS", "Yandex Mobile Ads",
                    "Вызовите GDPYandexAdsAdapter.TrackFromImpressionData() из OnImpression.");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);
                if (GUILayout.Button("Обновить определение SDK"))
                {
                    GDPDefineSymbolsManager.SyncDefineSymbols();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Трекинг рекламного дохода отключён.\n" +
                    "Включите, чтобы видеть доход от рекламы в аналитике GameDevPartner.",
                    MessageType.None);
            }

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
            bool ready = !string.IsNullOrEmpty(_config.ApiKey);
            if (ready)
            {
                EditorGUILayout.HelpBox(
                    "SDK готов к работе!\n\n" +
                    "SDK автоматически инициализируется при запуске игры.\n" +
                    "Покупки: вызовите GameDevPartnerSDK.TrackPurchase().\n" +
                    "Реклама: " + (_config.EnableAdRevenueTracking ? "трекается автоматически." : "отключено."),
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

        private void DrawAdSdkStatus(string name, string defineSymbol, string sdkName, string hint)
        {
            bool detected = HasDefineSymbol(defineSymbol);
            var icon = detected ? "✅" : "⬜";
            var status = detected ? "обнаружен" : "не установлен";
            var color = detected ? "green" : "gray";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{icon} {name}", GUILayout.Width(180));
            EditorGUILayout.LabelField(status, detected ? EditorStyles.boldLabel : EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (detected)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(hint, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
        }

        private bool HasDefineSymbol(string symbol)
        {
#if UNITY_2021_2_OR_NEWER
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            return defines.Split(';').Any(d => d.Trim() == symbol);
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
