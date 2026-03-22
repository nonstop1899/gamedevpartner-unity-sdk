#if GDP_YANDEX_ADS
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// Yandex Ads helper for ad revenue tracking.
    /// Does NOT reference YandexMobileAds types (they live in Assembly-CSharp).
    ///
    /// Integration (1 line in your OnAdImpression callback):
    ///   rewardedAd.OnAdImpression += (sender, data) =>
    ///       GDPYandexAdsAdapter.TrackImpression("rewarded", adUnitId, data.rawData);
    ///
    /// Or use the universal method (no adapter needed):
    ///   GameDevPartnerSDK.TrackAdRevenue(revenue, "USD", "rewarded", "yandex_ads");
    /// </summary>
    public static class GDPYandexAdsAdapter
    {
        private static bool _autoEnabled;

        public static void EnableAutoTracking()
        {
            if (_autoEnabled) return;
            _autoEnabled = true;
            Debug.Log("[GameDevPartner] Yandex Ads tracking enabled. " +
                      "Add 1 line in your OnAdImpression callback:\n" +
                      "  GDPYandexAdsAdapter.TrackImpression(\"rewarded\", adUnitId, data.rawData);");
        }

        /// <summary>
        /// Track impression from Yandex ImpressionData.rawData JSON string.
        /// Call from your ad's OnAdImpression callback:
        ///   rewardedAd.OnAdImpression += (sender, data) =>
        ///       GDPYandexAdsAdapter.TrackImpression("rewarded", adUnitId, data.rawData);
        /// </summary>
        /// <param name="adType">"rewarded", "interstitial", or "banner"</param>
        /// <param name="adUnitId">Your Yandex ad unit ID (e.g. "R-M-XXXXXX-X")</param>
        /// <param name="impressionRawData">ImpressionData.rawData JSON string</param>
        public static void TrackImpression(string adType, string adUnitId, string impressionRawData)
        {
            if (string.IsNullOrEmpty(impressionRawData)) return;

            try
            {
                var json = JsonUtility.FromJson<YandexImpressionJson>(impressionRawData);
                if (json.revenue > 0)
                {
                    GameDevPartnerSDK.TrackAdRevenue(
                        json.revenue,
                        string.IsNullOrEmpty(json.currency) ? "USD" : json.currency,
                        adType,
                        "yandex_ads",
                        adUnitId
                    );
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
        public static void TrackManual(string adType, string adUnitId, double revenue, string currency = "USD")
        {
            if (revenue <= 0) return;
            GameDevPartnerSDK.TrackAdRevenue(revenue, currency, adType, "yandex_ads", adUnitId);
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
