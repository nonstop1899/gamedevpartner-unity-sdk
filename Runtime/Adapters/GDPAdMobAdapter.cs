#if GDP_ADMOB
using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// AdMob adapter for automatic ad revenue tracking.
    /// Hooks into OnAdPaid callbacks on RewardedAd, InterstitialAd, and BannerView.
    ///
    /// Usage:
    ///   1. Add GDP_ADMOB to Scripting Define Symbols
    ///   2. Call GDPAdMobAdapter.TrackRewarded(rewardedAd, adUnitId) after loading
    ///   3. Call GDPAdMobAdapter.TrackInterstitial(interstitialAd, adUnitId) after loading
    ///   4. Call GDPAdMobAdapter.TrackBanner(bannerView, adUnitId) after loading
    /// </summary>
    public static class GDPAdMobAdapter
    {
        /// <summary>
        /// Attach ad revenue tracking to a RewardedAd.
        /// Call this after the ad is loaded.
        /// </summary>
        public static void TrackRewarded(RewardedAd ad, string adUnitId = null)
        {
            if (ad == null) return;
            ad.OnAdPaid += (AdValue adValue) =>
            {
                SendImpression(AdType.Rewarded, adUnitId, adValue);
            };
        }

        /// <summary>
        /// Attach ad revenue tracking to an InterstitialAd.
        /// Call this after the ad is loaded.
        /// </summary>
        public static void TrackInterstitial(InterstitialAd ad, string adUnitId = null)
        {
            if (ad == null) return;
            ad.OnAdPaid += (AdValue adValue) =>
            {
                SendImpression(AdType.Interstitial, adUnitId, adValue);
            };
        }

        /// <summary>
        /// Attach ad revenue tracking to a BannerView.
        /// Call this after the ad is loaded.
        /// </summary>
        public static void TrackBanner(BannerView banner, string adUnitId = null)
        {
            if (banner == null) return;
            banner.OnAdPaid += (AdValue adValue) =>
            {
                SendImpression(AdType.Banner, adUnitId, adValue);
            };
        }

        private static void SendImpression(AdType adType, string adUnitId, AdValue adValue)
        {
            // AdMob reports value in micros (1,000,000 micros = 1 unit of currency)
            double revenue = adValue.Value / 1_000_000.0;
            string currency = adValue.CurrencyCode ?? "USD";

            GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent
            {
                AdType = adType,
                AdNetwork = AdNetwork.AdMob,
                AdUnitId = adUnitId,
                Revenue = revenue,
                Currency = currency,
            });
        }
    }
}
#endif
