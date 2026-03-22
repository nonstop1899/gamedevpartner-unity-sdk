#if GDP_IRONSOURCE
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// IronSource / LevelPlay helper for ad revenue tracking.
    /// Does NOT reference IronSource types (may be in Assembly-CSharp).
    ///
    /// Integration (1 line in your impression callback):
    ///   IronSourceEvents.onImpressionDataReadyEvent += (data) =>
    ///       GDPIronSourceAdapter.TrackImpression(data.revenue ?? 0, data.adUnit, data.instanceName);
    ///
    /// Or universal:
    ///   GameDevPartnerSDK.TrackAdRevenue(data.revenue.Value, "USD", "rewarded", "ironsource");
    /// </summary>
    public static class GDPIronSourceAdapter
    {
        private static bool _enabled;

        public static void Enable()
        {
            if (_enabled) return;
            _enabled = true;
            Debug.Log("[GameDevPartner] IronSource tracking enabled. " +
                      "Add 1 line in your impression callback:\n" +
                      "  IronSourceEvents.onImpressionDataReadyEvent += (data) =>\n" +
                      "      GDPIronSourceAdapter.TrackImpression(data.revenue ?? 0, data.adUnit, data.instanceName);");
        }

        /// <summary>
        /// Track impression from IronSource ImpressionData.
        /// </summary>
        /// <param name="revenue">data.revenue value</param>
        /// <param name="adUnit">"rewarded_video", "interstitial", "banner"</param>
        /// <param name="instanceId">data.instanceName or data.instanceId</param>
        public static void TrackImpression(double revenue, string adUnit, string instanceId = "")
        {
            if (revenue <= 0) return;

            string adType;
            switch (adUnit?.ToLower())
            {
                case "rewarded_video": adType = "rewarded"; break;
                case "interstitial": adType = "interstitial"; break;
                case "banner": adType = "banner"; break;
                default: adType = "interstitial"; break;
            }

            GameDevPartnerSDK.TrackAdRevenue(revenue, "USD", adType, "ironsource", instanceId ?? "");
        }
    }
}
#endif
