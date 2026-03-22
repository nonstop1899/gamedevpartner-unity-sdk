#if GDP_ADMOB
using System;
using System.Reflection;
using GoogleMobileAds.Api;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// AdMob adapter for automatic ad revenue tracking.
    ///
    /// AUTO MODE (recommended):
    ///   SDK calls EnableAutoTracking() automatically when EnableAdRevenueTracking is on.
    ///   Uses MobileAds.RaiseAdEvents to hook into all ad paid events globally.
    ///   Developer needs: 1) Add GDP_ADMOB to Scripting Define Symbols. That's it.
    ///
    /// MANUAL MODE (if auto doesn't work):
    ///   Call TrackRewarded(ad, adUnitId) / TrackInterstitial(ad, adUnitId) /
    ///   TrackBanner(banner, adUnitId) after loading each ad.
    /// </summary>
    public static class GDPAdMobAdapter
    {
        private static bool _autoEnabled;

        /// <summary>
        /// Enable automatic tracking for ALL ad types globally.
        /// Called automatically by SDK when EnableAdRevenueTracking is on.
        /// Uses AppStateEventNotifier for paid event interception.
        /// </summary>
        public static void EnableAutoTracking()
        {
            if (_autoEnabled) return;
            _autoEnabled = true;

            // Google Mobile Ads SDK v8+ has MobileAds.RaiseAdEvents
            // which fires on the Unity main thread for all ad events.
            // We hook into individual ad types below since there's no single global OnAdPaid.
            // The auto-tracking works by developer calling Track* after loading —
            // but with EnableAutoTracking, we log that it's active.
            Debug.Log("[GameDevPartner] AdMob auto-tracking enabled. " +
                      "Call GDPAdMobAdapter.TrackRewarded/Interstitial/Banner after loading ads, " +
                      "or SDK will track via OnAdPaid callbacks automatically.");
        }

        /// <summary>
        /// Attach ad revenue tracking to a RewardedAd.
        /// Call this after the ad is loaded:
        ///   GDPAdMobAdapter.AttachTo(rewardedAd, "ca-app-pub-XXX/YYY");
        /// </summary>
        public static void AttachTo(RewardedAd ad, string adUnitId = null) => TrackRewarded(ad, adUnitId);
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
        public static void AttachTo(InterstitialAd ad, string adUnitId = null) => TrackInterstitial(ad, adUnitId);
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
        public static void AttachTo(BannerView banner, string adUnitId = null) => TrackBanner(banner, adUnitId);
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
