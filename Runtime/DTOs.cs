using System;
using System.Collections.Generic;

namespace GameDevPartner.SDK
{
    // --- Request DTOs (serialized to JSON) ---

    [Serializable]
    internal class IdentifyRequest
    {
        public string player_id;
        public string platform;
        public string device_fingerprint;
        public string gaid;
        public string idfv;
        public string referrer;
        public string country;
    }

    [Serializable]
    internal class PurchaseRequest
    {
        public string player_id;
        public string product_id;
        public float gross_amount;
        public string currency;
        public string source;
        public string external_tx_id;
        public string receipt_data;
    }

    [Serializable]
    internal class SessionRequest
    {
        public string player_id;
        public string platform;
    }

    // --- Response DTOs (deserialized from JSON) ---

    [Serializable]
    internal class IdentifyResponse
    {
        public bool success;
        public IdentifyData data;
    }

    [Serializable]
    internal class IdentifyData
    {
        public bool attributed;
        public string install_id;
        public string match_type;
    }

    [Serializable]
    internal class PurchaseResponse
    {
        public bool success;
        public PurchaseData data;
    }

    [Serializable]
    internal class PurchaseData
    {
        public string transaction_id;
        public bool attributed;
        public string influencer_id;
        public float net_amount;
    }

    // --- Ad Revenue DTOs ---

    [Serializable]
    internal class AdImpressionItem
    {
        public string player_id;
        public string ad_type;
        public string ad_network;
        public string ad_unit_id;
        public double revenue;
        public string currency;
        public string impression_id;
    }

    [Serializable]
    internal class AdRevenueBatchRequest
    {
        public AdImpressionItem[] impressions;
    }

    [Serializable]
    internal class AdRevenueResponse
    {
        public bool success;
        public AdRevenueData data;
    }

    [Serializable]
    internal class AdRevenueData
    {
        public int stored;
        public int skipped;
    }

    // --- Offline Queue Wrappers ---

    [Serializable]
    internal class QueueWrapper
    {
        public List<PurchaseEvent> items;
    }

    [Serializable]
    internal class AdQueueWrapper
    {
        public List<AdImpressionItem> items;
    }
}
