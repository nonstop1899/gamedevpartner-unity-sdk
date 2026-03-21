#if GDP_IRONSOURCE
using System;
using UnityEngine;

namespace GameDevPartner.SDK.Adapters
{
    /// <summary>
    /// IronSource / LevelPlay adapter for automatic ad revenue tracking.
    /// Hooks into the ImpressionDataReady event.
    ///
    /// Usage:
    ///   1. Add GDP_IRONSOURCE to Scripting Define Symbols
    ///   2. Call GDPIronSourceAdapter.Enable() after IronSource SDK init
    /// </summary>
    public static class GDPIronSourceAdapter
    {
        private static bool _enabled;

        /// <summary>
        /// Enable automatic ad revenue tracking from IronSource.
        /// Call once after IronSource.Agent.init().
        /// </summary>
        public static void Enable()
        {
            if (_enabled) return;
            _enabled = true;

            IronSourceEvents.onImpressionDataReadyEvent += OnImpressionData;
            Debug.Log("[GameDevPartner] IronSource ad revenue adapter enabled");
        }

        /// <summary>
        /// Disable tracking and unsubscribe from events.
        /// </summary>
        public static void Disable()
        {
            if (!_enabled) return;
            _enabled = false;
            IronSourceEvents.onImpressionDataReadyEvent -= OnImpressionData;
        }

        private static void OnImpressionData(IronSourceImpressionData data)
        {
            if (data == null) return;

            double revenue = data.revenue ?? 0;
            if (revenue <= 0) return;

            AdType adType;
            string adInstance = data.instanceName ?? data.instanceId ?? "";

            switch (data.adUnit?.ToLower())
            {
                case "rewarded_video":
                    adType = AdType.Rewarded;
                    break;
                case "interstitial":
                    adType = AdType.Interstitial;
                    break;
                case "banner":
                    adType = AdType.Banner;
                    break;
                default:
                    adType = AdType.Interstitial;
                    break;
            }

            GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent
            {
                AdType = adType,
                AdNetwork = AdNetwork.IronSource,
                AdUnitId = adInstance,
                Revenue = revenue,
                Currency = "USD", // IronSource always reports in USD
            });
        }
    }
}
#endif
