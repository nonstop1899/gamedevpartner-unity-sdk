using UnityEngine;
using GameDevPartner.SDK;

/// <summary>
/// Example integration of GameDevPartner SDK.
/// Attach this script to a persistent GameObject in your first scene.
/// </summary>
public class GamePartnerExample : MonoBehaviour
{
    [Header("GameDevPartner Settings")]
    [SerializeField] private string gameId = "your_game_slug";
    [SerializeField] private string apiKey = "sk_live_your_key_here";
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        // Step 1: Initialize SDK at game startup
        GameDevPartnerSDK.Init(new SDKConfig
        {
            GameId = gameId,
            ApiKey = apiKey,
            Region = SDKRegion.RU,
            DebugMode = debugMode
        });
    }

    /// <summary>
    /// Call this after your player logs in or registers.
    /// </summary>
    public void OnPlayerLogin(string playerId)
    {
        // Step 2: Identify player for attribution
        GameDevPartnerSDK.IdentifyPlayer(playerId);
    }

    /// <summary>
    /// Call this after a successful in-app purchase.
    /// </summary>
    public void OnPurchaseComplete(string playerId, string productId, float amount, string txId, string receipt)
    {
        // Step 3: Track the purchase
        GameDevPartnerSDK.TrackPurchase(new PurchaseEvent
        {
            PlayerId = playerId,
            ProductId = productId,
            Amount = amount,
            Currency = "RUB",
            Source = PaymentSource.YooKassa,
            TransactionId = txId,
            ReceiptData = receipt
        });
    }
}
