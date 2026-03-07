#if UNITY_PURCHASING
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Drop-in wrapper for Unity IAP that auto-tracks purchases to GameDevPartner.
    ///
    /// BEFORE (manual):
    ///   builder.Build(this);
    ///   // + manual TrackPurchase in ProcessPurchase
    ///
    /// AFTER (automatic):
    ///   GDPUnityIAP.Initialize(this, builder);
    ///   // Done — SDK auto-tracks every successful purchase
    /// </summary>
    public static class GDPUnityIAP
    {
        /// <summary>
        /// Initialize Unity IAP with automatic GameDevPartner purchase tracking.
        /// Drop-in replacement for UnityPurchasing.Initialize(listener, builder).
        /// </summary>
        public static void Initialize(IStoreListener listener, ConfigurationBuilder builder)
        {
            UnityPurchasing.Initialize(new GDPIAPListener(listener), builder);
        }
    }

    /// <summary>
    /// IStoreListener wrapper that intercepts ProcessPurchase to auto-track purchases.
    /// Passes all calls through to the original listener unchanged.
    /// </summary>
    internal class GDPIAPListener : IStoreListener
    {
        private readonly IStoreListener _inner;

        internal GDPIAPListener(IStoreListener inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _inner.OnInitialized(controller, extensions);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            _inner.OnInitializeFailed(error);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // Auto-track to GameDevPartner before passing to game code
            try
            {
                var product = args.purchasedProduct;
#if UNITY_IOS
                var source = PaymentSource.Apple;
#else
                var source = PaymentSource.GooglePlay;
#endif
                GameDevPartnerSDK.TrackPurchase(new PurchaseEvent
                {
                    ProductId = product.definition.id,
                    Amount = (float)product.metadata.localizedPrice,
                    Currency = product.metadata.isoCurrencyCode,
                    TransactionId = product.transactionID,
                    Source = source,
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameDevPartner] Auto-track failed: {ex.Message}");
            }

            return _inner.ProcessPurchase(args);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            _inner.OnPurchaseFailed(product, failureReason);
        }
    }
}
#endif
