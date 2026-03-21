#if GDP_APPLOVIN
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// AppLovin MAX adapter for automatic ad revenue tracking.
    /// Hooks into OnAdRevenuePaidEvent callbacks.
    ///
    /// Usage:
    ///   1. Add GDP_APPLOVIN to Scripting Define Symbols
    ///   2. Call GDPAppLovinAdapter.Enable() after MAX SDK init
    /// </summary>
    public static class GDPAppLovinAdapter
    {
        private static bool _enabled;

        /// <summary>
        /// Enable automatic ad revenue tracking from AppLovin MAX.
        /// Call once after MaxSdk.InitializeSdk().
        /// </summary>
        public static void Enable()
        {
            if (_enabled) return;
            _enabled = true;

            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenue;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedRevenue;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerRevenue;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnBannerRevenue;

            Debug.Log("[GameDevPartner] AppLovin MAX ad revenue adapter enabled");
        }

        /// <summary>
        /// Disable tracking and unsubscribe from events.
        /// </summary>
        public static void Disable()
        {
            if (!_enabled) return;
            _enabled = false;

            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialRevenue;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedRevenue;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerRevenue;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnBannerRevenue;
        }

        private static void OnRewardedRevenue(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SendImpression(AdType.Rewarded, adUnitId, adInfo);
        }

        private static void OnInterstitialRevenue(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SendImpression(AdType.Interstitial, adUnitId, adInfo);
        }

        private static void OnBannerRevenue(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SendImpression(AdType.Banner, adUnitId, adInfo);
        }

        private static void SendImpression(AdType adType, string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double revenue = adInfo.Revenue; // Already in currency units (USD)
            if (revenue <= 0) return;

            GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent
            {
                AdType = adType,
                AdNetwork = AdNetwork.AppLovin,
                AdUnitId = adUnitId,
                Revenue = revenue,
                Currency = "USD", // MAX always reports in USD
            });
        }
    }
}
#endif
