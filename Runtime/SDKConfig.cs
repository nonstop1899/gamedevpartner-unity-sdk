namespace GameDevPartner.SDK
{
    /// <summary>
    /// Configuration for GameDevPartner SDK initialization.
    /// </summary>
    [System.Serializable]
    public class SDKConfig
    {
        /// <summary>Game identifier (slug from developer dashboard)</summary>
        public string GameId;

        /// <summary>API key (sk_live_xxx for production, sk_test_xxx for testing)</summary>
        public string ApiKey;

        /// <summary>Server region</summary>
        public SDKRegion Region = SDKRegion.RU;

        /// <summary>Enable debug logging</summary>
        public bool DebugMode;

        /// <summary>Override base URL (optional, default derived from Region)</summary>
        public string CustomBaseUrl;

        public string BaseUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomBaseUrl))
                    return CustomBaseUrl;

                return Region == SDKRegion.RU
                    ? "https://api.gamedevpartner.ru/api/v1"
                    : "https://api.gamedevpartner.com/api/v1";
            }
        }
    }

    public enum SDKRegion
    {
        RU,
        World
    }
}
