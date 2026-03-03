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
        private string _gameId = "";
        private string _apiKey = "";
        private int _regionIndex = 0;
        private bool _debugMode = true;
        private string _customBaseUrl = "";

        private readonly string[] _regionOptions = { "RU", "World" };

        private const string PrefGameId = "GDP_GameId";
        private const string PrefApiKey = "GDP_ApiKey";
        private const string PrefRegion = "GDP_Region";
        private const string PrefDebug = "GDP_Debug";
        private const string PrefCustomUrl = "GDP_CustomUrl";

        [MenuItem("Window/GameDevPartner/Settings")]
        public static void ShowWindow()
        {
            GetWindow<GameDevPartnerSettings>("GameDevPartner SDK");
        }

        private void OnEnable()
        {
            _gameId = EditorPrefs.GetString(PrefGameId, "");
            _apiKey = EditorPrefs.GetString(PrefApiKey, "");
            _regionIndex = EditorPrefs.GetInt(PrefRegion, 0);
            _debugMode = EditorPrefs.GetBool(PrefDebug, true);
            _customBaseUrl = EditorPrefs.GetString(PrefCustomUrl, "");
        }

        private void OnGUI()
        {
            GUILayout.Label("GameDevPartner SDK Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _gameId = EditorGUILayout.TextField("Game ID", _gameId);
            _apiKey = EditorGUILayout.TextField("API Key", _apiKey);
            _regionIndex = EditorGUILayout.Popup("Region", _regionIndex, _regionOptions);
            _debugMode = EditorGUILayout.Toggle("Debug Mode", _debugMode);

            EditorGUILayout.Space();
            GUILayout.Label("Advanced", EditorStyles.boldLabel);
            _customBaseUrl = EditorGUILayout.TextField("Custom Base URL", _customBaseUrl);

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Settings"))
            {
                EditorPrefs.SetString(PrefGameId, _gameId);
                EditorPrefs.SetString(PrefApiKey, _apiKey);
                EditorPrefs.SetInt(PrefRegion, _regionIndex);
                EditorPrefs.SetBool(PrefDebug, _debugMode);
                EditorPrefs.SetString(PrefCustomUrl, _customBaseUrl);
                Debug.Log("[GameDevPartner] Settings saved");
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Usage:\n" +
                "1. Enter your Game ID and API Key from the developer dashboard\n" +
                "2. Select your region\n" +
                "3. In your game code, call:\n\n" +
                "   GameDevPartnerSDK.Init(new SDKConfig {\n" +
                "       GameId = \"your_game_id\",\n" +
                "       ApiKey = \"sk_live_xxx\",\n" +
                "       Region = SDKRegion.RU,\n" +
                "       DebugMode = true\n" +
                "   });\n\n" +
                "4. After player login:\n" +
                "   GameDevPartnerSDK.IdentifyPlayer(playerId);\n\n" +
                "5. After each purchase:\n" +
                "   GameDevPartnerSDK.TrackPurchase(new PurchaseEvent { ... });",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Init Code"))
            {
                var region = _regionIndex == 0 ? "SDKRegion.RU" : "SDKRegion.World";
                var code = $@"GameDevPartnerSDK.Init(new SDKConfig {{
    GameId = ""{_gameId}"",
    ApiKey = ""{_apiKey}"",
    Region = {region},
    DebugMode = {_debugMode.ToString().ToLower()}{(_customBaseUrl != "" ? $",\n    CustomBaseUrl = \"{_customBaseUrl}\"" : "")}
}});";
                EditorGUIUtility.systemCopyBuffer = code;
                Debug.Log("[GameDevPartner] Init code copied to clipboard:\n" + code);
            }
        }
    }
}
