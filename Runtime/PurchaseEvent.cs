using System;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Purchase event data to send to GameDevPartner.
    /// </summary>
    [Serializable]
    public class PurchaseEvent
    {
        /// <summary>Player ID in your game</summary>
        public string PlayerId;

        /// <summary>Product identifier (e.g., "gems_pack_500")</summary>
        public string ProductId;

        /// <summary>Gross purchase amount</summary>
        public float Amount;

        /// <summary>Currency code (RUB, USD, EUR)</summary>
        public string Currency = "RUB";

        /// <summary>Payment source</summary>
        public PaymentSource Source = PaymentSource.GooglePlay;

        /// <summary>Unique transaction ID from payment provider</summary>
        public string TransactionId;

        /// <summary>Raw receipt data for server-side validation (optional)</summary>
        public string ReceiptData;

        /// <summary>Timestamp (auto-set on creation)</summary>
        public string Timestamp = DateTime.UtcNow.ToString("o");

        /// <summary>Internal retry counter (not serialized to JSON)</summary>
        [NonSerialized] internal int _retryCount;
    }

    public enum PaymentSource
    {
        GooglePlay,
        Apple,
        YooKassa,
        RuStore,
        TBank,
        Web,
        Other
    }
}
