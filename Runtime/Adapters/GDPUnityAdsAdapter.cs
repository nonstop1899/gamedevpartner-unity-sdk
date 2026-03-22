#if GDP_UNITY_ADS
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Unity Ads helper for ad revenue tracking.
    /// Unity Ads does not provide ILAR, so uses estimated eCPM.
    ///
    /// Integration (1 line in OnUnityAdsShowComplete):
    ///   GDPUnityAdsAdapter.TrackShowComplete(placementId, "rewarded");
    /// </summary>
    public static class GDPUnityAdsAdapter
    {
        private static bool _autoEnabled;

        private static double _rewardedEcpm = 10.0;
        private static double _interstitialEcpm = 5.0;
        private static double _bannerEcpm = 1.0;

        public static void EnableAutoTracking()
        {
            if (_autoEnabled) return;
            _autoEnabled = true;
            Debug.Log("[GameDevPartner] Unity Ads tracking enabled. " +
                      "Add 1 line in OnUnityAdsShowComplete:\n" +
                      "  GDPUnityAdsAdapter.TrackShowComplete(placementId, \"rewarded\");");
        }

        /// <summary>
        /// Set estimated eCPM values (USD per 1000 impressions).
        /// </summary>
        public static void SetEcpm(double rewardedEcpm = 10.0, double interstitialEcpm = 5.0, double bannerEcpm = 1.0)
        {
            _rewardedEcpm = rewardedEcpm;
            _interstitialEcpm = interstitialEcpm;
            _bannerEcpm = bannerEcpm;
        }

        /// <summary>
        /// Track a completed ad show.
        /// Call from OnUnityAdsShowComplete when state == COMPLETED.
        /// </summary>
        /// <param name="adUnitId">Placement ID</param>
        /// <param name="adType">"rewarded", "interstitial", or "banner"</param>
        public static void TrackShowComplete(string adUnitId, string adType = "rewarded")
        {
            double ecpm;
            switch (adType?.ToLower())
            {
                case "rewarded": ecpm = _rewardedEcpm; break;
                case "interstitial": ecpm = _interstitialEcpm; break;
                case "banner": ecpm = _bannerEcpm; break;
                default: ecpm = _interstitialEcpm; break;
            }

            double revenue = ecpm / 1000.0;
            GameDevPartnerSDK.TrackAdRevenue(revenue, "USD", adType, "unity_ads", adUnitId);
        }
    }
}
#endif
