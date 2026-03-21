#if GDP_UNITY_ADS
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Unity Ads adapter for ad revenue tracking.
    /// Unity Ads does not provide impression-level revenue data (ILAR),
    /// so this adapter tracks ad show completions with estimated eCPM.
    ///
    /// AUTO MODE (recommended):
    ///   SDK calls EnableAutoTracking() automatically.
    ///   Developer only needs to call TrackShowComplete() in their
    ///   IUnityAdsShowListener.OnUnityAdsShowComplete() callback.
    ///
    /// Minimal integration (1 line):
    ///   GDPUnityAdsAdapter.TrackShowComplete(placementId, AdType.Rewarded);
    /// </summary>
    public static class GDPUnityAdsAdapter
    {
        private static bool _autoEnabled;

        // Default estimated eCPM values (USD per 1000 impressions)
        private static double _rewardedEcpm = 10.0;
        private static double _interstitialEcpm = 5.0;
        private static double _bannerEcpm = 1.0;

        /// <summary>
        /// Called automatically by SDK. Marks adapter as active.
        /// </summary>
        public static void EnableAutoTracking()
        {
            if (_autoEnabled) return;
            _autoEnabled = true;
            Debug.Log("[GameDevPartner] Unity Ads tracking enabled. " +
                      "Call TrackShowComplete() from OnUnityAdsShowComplete().");
        }

        /// <summary>
        /// Set estimated eCPM values for revenue calculation.
        /// </summary>
        public static void SetEcpm(double rewardedEcpm = 10.0, double interstitialEcpm = 5.0, double bannerEcpm = 1.0)
        {
            _rewardedEcpm = rewardedEcpm;
            _interstitialEcpm = interstitialEcpm;
            _bannerEcpm = bannerEcpm;
        }

        /// <summary>
        /// Track a completed ad show. Call from your IUnityAdsShowListener.OnUnityAdsShowComplete().
        /// </summary>
        public static void TrackShowComplete(string adUnitId, AdType adType = AdType.Rewarded)
        {
            double ecpm;
            switch (adType)
            {
                case AdType.Rewarded: ecpm = _rewardedEcpm; break;
                case AdType.Interstitial: ecpm = _interstitialEcpm; break;
                case AdType.Banner: ecpm = _bannerEcpm; break;
                default: ecpm = _interstitialEcpm; break;
            }

            double revenue = ecpm / 1000.0;

            GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent
            {
                AdType = adType,
                AdNetwork = AdNetwork.UnityAds,
                AdUnitId = adUnitId,
                Revenue = revenue,
                Currency = "USD",
            });
        }
    }
}
#endif
