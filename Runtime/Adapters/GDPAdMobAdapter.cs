#if GDP_ADMOB
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// AdMob helper for ad revenue tracking.
    /// Does NOT reference GoogleMobileAds types (may be in Assembly-CSharp).
    ///
    /// Integration (1 line in your OnAdPaid callback):
    ///   rewardedAd.OnAdPaid += (adValue) =>
    ///       GDPAdMobAdapter.TrackImpression("rewarded", adUnitId, adValue.Value, adValue.CurrencyCode);
    ///
    /// adValue.Value is in micros (1,000,000 = 1 currency unit) — adapter converts automatically.
    /// </summary>
    public static class GDPAdMobAdapter
    {
        private static bool _autoEnabled;

        public static void EnableAutoTracking()
        {
            if (_autoEnabled) return;
            _autoEnabled = true;
            Debug.Log("[GameDevPartner] AdMob tracking enabled. " +
                      "Add 1 line in your OnAdPaid callback:\n" +
                      "  GDPAdMobAdapter.TrackImpression(\"rewarded\", adUnitId, adValue.Value, adValue.CurrencyCode);");
        }

        /// <summary>
        /// Track ad impression from AdMob OnAdPaid callback.
        /// Usage:
        ///   rewardedAd.OnAdPaid += (adValue) =>
        ///       GDPAdMobAdapter.TrackImpression("rewarded", adUnitId, adValue.Value, adValue.CurrencyCode);
        /// </summary>
        /// <param name="adType">"rewarded", "interstitial", or "banner"</param>
        /// <param name="adUnitId">Your AdMob ad unit ID</param>
        /// <param name="valueMicros">AdValue.Value (in micros: 1,000,000 = 1 currency unit)</param>
        /// <param name="currencyCode">AdValue.CurrencyCode (e.g. "USD")</param>
        public static void TrackImpression(string adType, string adUnitId, long valueMicros, string currencyCode = "USD")
        {
            double revenue = valueMicros / 1_000_000.0;
            if (revenue <= 0) return;

            GameDevPartnerSDK.TrackAdRevenue(
                revenue,
                string.IsNullOrEmpty(currencyCode) ? "USD" : currencyCode,
                adType,
                "admob",
                adUnitId
            );
        }
    }
}
#endif
