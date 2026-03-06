using UnityEngine;
using GameDevPartner.SDK;

/// <summary>
/// Example: tracking purchases with GameDevPartner SDK.
///
/// SDK auto-initializes from settings (Window > GameDevPartner > Settings).
/// You only need to call TrackPurchase() after each purchase.
/// </summary>
public class GamePartnerExample : MonoBehaviour
{
    /// <summary>
    /// Call this after a successful in-app purchase.
    /// This is the ONLY SDK call you need in your code.
    /// </summary>
    public void OnPurchaseComplete(string productId, float amount, string txId, string receipt)
    {
        GameDevPartnerSDK.TrackPurchase(new PurchaseEvent
        {
            ProductId = productId,
            Amount = amount,
            Currency = "RUB",
            Source = PaymentSource.YooKassa,
            TransactionId = txId,
            ReceiptData = receipt
        });
    }
}
