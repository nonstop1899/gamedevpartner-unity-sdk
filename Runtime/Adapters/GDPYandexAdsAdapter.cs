#if GDP_YANDEX_ADS
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Yandex Mobile Ads adapter for automatic ad revenue tracking.
    /// Hooks into impression data callbacks from Yandex Ads SDK.
    ///
    /// Usage:
    ///   1. Add GDP_YANDEX_ADS to Scripting Define Symbols
    ///   2. Call GDPYandexAdsAdapter.TrackRewarded(rewardedAd) after creating the ad
    ///   3. Call GDPYandexAdsAdapter.TrackInterstitial(interstitialAd)
    ///   4. Call GDPYandexAdsAdapter.TrackBanner(bannerAd)
    /// </summary>
    public static class GDPYandexAdsAdapter
    {
        /// <summary>
        /// Track rewarded ad revenue from Yandex Ads.
        /// Pass the ad object's OnAdImpression event data.
        /// </summary>
        public static void TrackImpression(AdType adType, string adUnitId, double revenue, string currency = "USD")
        {
            if (revenue <= 0) return;

            GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent
            {
                AdType = adType,
                AdNetwork = AdNetwork.YandexAds,
                AdUnitId = adUnitId,
                Revenue = revenue,
                Currency = currency,
            });
        }

        /// <summary>
        /// Track impression from Yandex ImpressionData JSON.
        /// Call this from your ad's OnImpression callback.
        /// </summary>
        /// <param name="adType">Type of ad</param>
        /// <param name="adUnitId">Ad unit ID</param>
        /// <param name="impressionDataJson">Raw JSON from Yandex ImpressionData</param>
        public static void TrackFromImpressionData(AdType adType, string adUnitId, string impressionDataJson)
        {
            if (string.IsNullOrEmpty(impressionDataJson)) return;

            try
            {
                // Parse minimal fields from Yandex impression data JSON
                // Format: {"revenue": 0.001234, "currency": "USD", ...}
                var data = JsonUtility.FromJson<YandexImpressionData>(impressionDataJson);
                if (data.revenue > 0)
                {
                    TrackImpression(adType, adUnitId, data.revenue, data.currency ?? "USD");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameDevPartner] Failed to parse Yandex impression data: {e.Message}");
            }
        }

        [Serializable]
        private class YandexImpressionData
        {
            public double revenue;
            public string currency;
        }
    }
}
#endif
