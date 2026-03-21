#if GDP_YANDEX_ADS
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Yandex Mobile Ads adapter for ad revenue tracking.
    ///
    /// AUTO MODE (recommended):
    ///   SDK calls EnableAutoTracking() automatically. Logs that tracking is active.
    ///   Developer calls TrackFromImpressionData() from their ad OnImpression callback.
    ///
    /// Minimal integration (1 line per ad type):
    ///   rewardedAd.OnImpression += (_, data) =>
    ///       GDPYandexAdsAdapter.TrackFromImpressionData(AdType.Rewarded, adUnitId, data);
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
                      "Call TrackFromImpressionData() from your ad OnImpression callbacks.");
        }

        /// <summary>
        /// Track ad impression with known revenue.
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
        public static void TrackFromImpressionData(AdType adType, string adUnitId, string impressionDataJson)
        {
            if (string.IsNullOrEmpty(impressionDataJson)) return;

            try
            {
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
