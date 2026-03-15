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

    // --- Offline Queue Wrapper ---

    [Serializable]
    internal class QueueWrapper
    {
        public List<PurchaseEvent> items;
    }
}
