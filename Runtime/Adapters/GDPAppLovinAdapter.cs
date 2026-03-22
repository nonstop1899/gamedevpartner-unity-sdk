#if GDP_APPLOVIN
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// AppLovin MAX helper for ad revenue tracking.
    /// Does NOT reference MaxSdk types (may be in Assembly-CSharp).
    ///
    /// Integration (1 line per ad type in OnAdRevenuePaidEvent):
    ///   MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitId, adInfo) =>
    ///       GDPAppLovinAdapter.TrackImpression("rewarded", adUnitId, adInfo.Revenue);
    ///
    /// Or universal:
    ///   GameDevPartnerSDK.TrackAdRevenue(adInfo.Revenue, "USD", "rewarded", "applovin", adUnitId);
    /// </summary>
    public static class GDPAppLovinAdapter
    {
        private static bool _enabled;

        public static void Enable()
        {
            if (_enabled) return;
            _enabled = true;
            Debug.Log("[GameDevPartner] AppLovin MAX tracking enabled. " +
                      "Add in your ad callbacks:\n" +
                      "  MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (id, info) =>\n" +
                      "      GDPAppLovinAdapter.TrackImpression(\"rewarded\", id, info.Revenue);");
        }

        /// <summary>
        /// Track impression from AppLovin MAX OnAdRevenuePaidEvent.
        /// </summary>
        /// <param name="adType">"rewarded", "interstitial", or "banner"</param>
        /// <param name="adUnitId">MAX ad unit ID</param>
        /// <param name="revenue">adInfo.Revenue (in USD)</param>
        public static void TrackImpression(string adType, string adUnitId, double revenue)
        {
            if (revenue <= 0) return;
            GameDevPartnerSDK.TrackAdRevenue(revenue, "USD", adType, "applovin", adUnitId);
        }
    }
}
#endif
