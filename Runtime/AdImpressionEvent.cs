using System;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Ad impression event with ILAR (impression-level ad revenue) data.
    /// Use TrackAdImpression() to send this to GameDevPartner.
    /// </summary>
    [Serializable]
    public class AdImpressionEvent
    {
        /// <summary>Player ID in your game (auto-filled from IdentifyPlayer if empty)</summary>
        public string PlayerId;

        /// <summary>Ad format type</summary>
        public AdType AdType;

        /// <summary>Ad mediation network</summary>
        public AdNetwork AdNetwork;

        /// <summary>Ad unit identifier (e.g., "reward_level_complete")</summary>
        public string AdUnitId;

        /// <summary>Revenue for this impression (in currency units, NOT micros)</summary>
        public double Revenue;

        /// <summary>Revenue currency (usually "USD" for ad revenue)</summary>
        public string Currency = "USD";

        /// <summary>Unique impression ID (auto-generated if empty)</summary>
        public string ImpressionId;

        /// <summary>Timestamp (auto-set on creation)</summary>
        public string Timestamp = DateTime.UtcNow.ToString("o");
    }

    public enum AdType
    {
        Rewarded,
        Interstitial,
        Banner
    }

    public enum AdNetwork
    {
        AdMob,
        IronSource,
        AppLovin,
        UnityAds,
        YandexAds,
        Other
    }
}
