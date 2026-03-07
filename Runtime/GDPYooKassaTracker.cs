using System;
using UnityEngine;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// Auto-tracker for YooKassa / SenomeYookassaSDK purchases.
    ///
    /// Usage — call once after YooKassaSDK initializes:
    ///   GDPYooKassaTracker.Attach(yookassaSDK, getPriceCallback);
    ///
    /// Example:
    ///   GDPYooKassaTracker.Attach(_yookassaSDK, (item) =>
    ///   {
    ///       var product = _yookassaSDK.GetProduct(item);
    ///       return (product?.Price ?? 0f, "RUB");
    ///   });
    ///
    /// That's it. Every successful YooKassa transaction is automatically
    /// reported to GameDevPartner with Source = PaymentSource.YooKassa.
    /// </summary>
    public static class GDPYooKassaTracker
    {
        private static bool _attached;
        private static Func<string, (float price, string currency)> _getPrice;

        /// <summary>
        /// Attach auto-tracking to a YooKassaSDK instance.
        /// getPriceCallback: given product SKU (item), returns (price, currency).
        /// </summary>
        public static void Attach(object yookassaSDK, Func<string, (float price, string currency)> getPriceCallback)
        {
            if (_attached) return;
            if (yookassaSDK == null)
            {
                Debug.LogWarning("[GameDevPartner] GDPYooKassaTracker.Attach: yookassaSDK is null");
                return;
            }

            _getPrice = getPriceCallback;

            try
            {
                var sdkType = yookassaSDK.GetType();
                var observableProp = sdkType.GetProperty("TransactionsObservable");
                if (observableProp == null)
                {
                    Debug.LogWarning("[GameDevPartner] GDPYooKassaTracker: TransactionsObservable not found.");
                    return;
                }

                var observable = observableProp.GetValue(yookassaSDK);
                var observableType = observable.GetType();
                var eventInfo = observableType.GetEvent("TransactionSucceededEvent");
                if (eventInfo == null)
                {
                    Debug.LogWarning("[GameDevPartner] GDPYooKassaTracker: TransactionSucceededEvent not found.");
                    return;
                }

                var handlerType = eventInfo.EventHandlerType;
                var method = typeof(GDPYooKassaTracker).GetMethod(
                    nameof(OnTransactionSucceeded),
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                );
                var handler = System.Delegate.CreateDelegate(handlerType, null, method);
                eventInfo.AddEventHandler(observable, handler);

                _attached = true;
                Debug.Log("[GameDevPartner] YooKassa auto-tracking attached.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameDevPartner] GDPYooKassaTracker.Attach failed: {ex.Message}");
            }
        }

        private static void OnTransactionSucceeded(object transaction)
        {
            if (transaction == null) return;
            try
            {
                var txType = transaction.GetType();
                var txId = txType.GetProperty("ID")?.GetValue(transaction) as string ?? "";
                var item = txType.GetProperty("Item")?.GetValue(transaction) as string ?? "";

                if (string.IsNullOrEmpty(txId)) return;

                float price = 0f;
                string currency = "RUB";
                if (_getPrice != null)
                {
                    var result = _getPrice(item);
                    price = result.price;
                    currency = result.currency;
                }

                if (price <= 0f)
                {
                    Debug.LogWarning($"[GameDevPartner] YooKassa: price is 0 for item '{item}', skipping TrackPurchase.");
                    return;
                }

                GameDevPartnerSDK.TrackPurchase(new PurchaseEvent
                {
                    ProductId = item,
                    Amount = price,
                    Currency = currency,
                    TransactionId = txId,
                    Source = PaymentSource.YooKassa,
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameDevPartner] YooKassa auto-track error: {ex.Message}");
            }
        }
    }
}
