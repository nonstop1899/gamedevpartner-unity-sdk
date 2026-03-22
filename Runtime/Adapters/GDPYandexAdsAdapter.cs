#if GDP_YANDEX_ADS
using System;
using UnityEngine;
using YandexMobileAds;
using YandexMobileAds.Base;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Yandex Mobile Ads adapter — automatic ad revenue tracking.
    ///
    /// EASIEST integration (1 line after ad load):
    ///   rewardedAd = args.RewardedAd;
    ///   GDPYandexAdsAdapter.AttachTo(rewardedAd, "R-M-XXXXXX-X");
    ///
    /// That's it! Revenue from every impression will be tracked automatically.
    /// Works with RewardedAd, Interstitial, and Banner.
    /// </summary>
    public static class GDPYandexAdsAdapter
    {
        private static bool _autoEnabled;

        /// <summary>
        /// Called automatically by SDK. Marks adapter as active.
        /// </summary>
        public static void EnableAutoTracking()
        {
            if (_autoEnabled) return;
            _autoEnabled = true;
            Debug.Log("[GameDevPartner] Yandex Ads tracking enabled. " +
                      "Use GDPYandexAdsAdapter.AttachTo(ad, adUnitId) after loading ads.");
        }

        /// <summary>
        /// Attach to a RewardedAd — automatically tracks revenue from OnAdImpression.
        /// Call once after ad is loaded:
        ///   rewardedAd = args.RewardedAd;
        ///   GDPYandexAdsAdapter.AttachTo(rewardedAd, "R-M-XXXXXX-X");
        /// </summary>
        public static void AttachTo(RewardedAd ad, string adUnitId = "")
        {
            if (ad == null) return;
            ad.OnAdImpression += (sender, data) =>
                TrackFromImpressionData(AdType.Rewarded, adUnitId, data);
        }

        /// <summary>
        /// Attach to an Interstitial — automatically tracks revenue from OnAdImpression.
        /// </summary>
        public static void AttachTo(Interstitial ad, string adUnitId = "")
        {
            if (ad == null) return;
            ad.OnAdImpression += (sender, data) =>
                TrackFromImpressionData(AdType.Interstitial, adUnitId, data);
        }

        /// <summary>
        /// Attach to a Banner — automatically tracks revenue from OnImpression.
        /// </summary>
        public static void AttachTo(Banner ad, string adUnitId = "")
        {
            if (ad == null) return;
            ad.OnImpression += (sender, data) =>
                TrackFromImpressionData(AdType.Banner, adUnitId, data);
        }

        /// <summary>
        /// Track impression from Yandex ImpressionData object.
        /// Called automatically by AttachTo, but can also be used directly.
        /// </summary>
        public static void TrackFromImpressionData(AdType adType, string adUnitId, ImpressionData impressionData)
        {
            if (impressionData == null || string.IsNullOrEmpty(impressionData.rawData)) return;

            try
            {
                var json = JsonUtility.FromJson<YandexImpressionJson>(impressionData.rawData);
                if (json.revenue > 0)
                {
                    GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent
                    {
                        AdType = adType,
                        AdNetwork = AdNetwork.YandexAds,
                        AdUnitId = adUnitId,
                        Revenue = json.revenue,
                        Currency = string.IsNullOrEmpty(json.currency) ? "USD" : json.currency,
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameDevPartner] Failed to parse Yandex impression data: {e.Message}");
            }
        }

        /// <summary>
        /// Track impression with known revenue (manual).
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

        [Serializable]
        private class YandexImpressionJson
        {
            public double revenue;
            public string currency;
        }
    }
}
#endif
