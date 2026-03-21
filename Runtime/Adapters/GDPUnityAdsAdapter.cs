#if GDP_UNITY_ADS
using System;
using UnityEngine;
using UnityEngine.Advertisements;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Unity Ads adapter for ad revenue tracking.
    /// Unity Ads does not provide impression-level revenue data directly,
    /// so this adapter tracks ad show completions with an estimated eCPM.
    ///
    /// Usage:
    ///   1. Add GDP_UNITY_ADS to Scripting Define Symbols
    ///   2. Set eCPM estimates via GDPUnityAdsAdapter.SetEcpm()
    ///   3. Call GDPUnityAdsAdapter.TrackShowComplete() in your IUnityAdsShowListener
    /// </summary>
    public static class GDPUnityAdsAdapter
    {
        // Default estimated eCPM values (USD per 1000 impressions)
        private static double _rewardedEcpm = 10.0;    // $10 per 1000 impressions
        private static double _interstitialEcpm = 5.0;  // $5 per 1000 impressions
        private static double _bannerEcpm = 1.0;        // $1 per 1000 impressions

        /// <summary>
        /// Set estimated eCPM values for revenue calculation.
        /// Since Unity Ads doesn't provide ILAR, we estimate based on eCPM.
        /// </summary>
        /// <param name="rewardedEcpm">eCPM for rewarded ads in USD (default: 10.0)</param>
        /// <param name="interstitialEcpm">eCPM for interstitial ads in USD (default: 5.0)</param>
        /// <param name="bannerEcpm">eCPM for banner ads in USD (default: 1.0)</param>
        public static void SetEcpm(double rewardedEcpm = 10.0, double interstitialEcpm = 5.0, double bannerEcpm = 1.0)
        {
            _rewardedEcpm = rewardedEcpm;
            _interstitialEcpm = interstitialEcpm;
            _bannerEcpm = bannerEcpm;
        }

        /// <summary>
        /// Track a completed ad show. Call from your IUnityAdsShowListener.OnUnityAdsShowComplete().
        /// </summary>
        /// <param name="adUnitId">Unity Ads placement ID</param>
        /// <param name="adType">Type of ad shown</param>
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

            // Revenue per impression = eCPM / 1000
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
