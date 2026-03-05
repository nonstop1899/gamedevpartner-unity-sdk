# GameDevPartner Unity SDK

SDK for integrating affiliate tracking into Unity games. Tracks installs, purchases, and influencer attribution.

**Unity 2020.3 LTS+** | **v1.0.0**

## Installation (Unity Package Manager)

1. Open your Unity project
2. Go to **Window > Package Manager**
3. Click **+** > **Add package from git URL**
4. Paste:

```
https://github.com/nonstop1899/gamedevpartner-unity-sdk.git
```

5. Click **Add**

## Quick Start

### 1. Initialize SDK

```csharp
using GameDevPartner.SDK;

void Awake() {
    GameDevPartnerSDK.Init(new SDKConfig {
        GameId = "your_game_slug",
        ApiKey = "YOUR_LIVE_KEY",
        Region = SDKRegion.RU,
        DebugMode = true
    });
}
```

### 2. Identify Player

```csharp
GameDevPartnerSDK.IdentifyPlayer(playerId);
```

### 3. Track Purchase

```csharp
GameDevPartnerSDK.TrackPurchase(new PurchaseEvent {
    PlayerId = playerId,
    ProductId = "gem_pack_100",
    Amount = 299f,
    Currency = "RUB",
    Source = PaymentSource.YooKassa,
    TransactionId = txId,
    ReceiptData = receipt
});
```

## Features

- Offline queue (up to 100 events, 7-day retention)
- HMAC-SHA256 request signing
- Automatic platform/GAID/IDFV detection
- Editor settings window (Window > GameDevPartner > Settings)

## Documentation

Full documentation: [gamedevpartner.ru/developer/sdk](https://gamedevpartner.ru/developer/sdk)

## License

MIT
