# GameDevPartner Unity SDK

SDK для интеграции партнёрского трекинга в Unity-игры. Отслеживает установки, покупки, рекламный доход и атрибуцию инфлюенсеров.

**Unity 2020.3 LTS+** | **v2.3.0**

---

## Установка (Unity Package Manager)

1. Откройте Unity-проект
2. **Window → Package Manager**
3. Нажмите **+** → **Add package from git URL**
4. Вставьте:

```
https://github.com/nonstop1899/gamedevpartner-unity-sdk.git
```

5. Нажмите **Add**

---

## Быстрый старт

### Шаг 1. Настройка SDK

**Вариант А — через Editor (рекомендуется):**

1. **Window → GameDevPartner → Settings**
2. Вставьте API Key (из личного кабинета gamedevpartner.ru → Мои игры → SDK)
3. Выберите регион (RU / World)
4. Включите Debug Mode для тестирования
5. Сохраните

SDK инициализируется **автоматически** при запуске игры. Никакого кода для инициализации не нужно.

**Вариант Б — из кода (опционально):**

```csharp
using GameDevPartner.SDK;

// Вызывается автоматически, но можно и вручную:
GameDevPartnerSDK.Init(new SDKConfig {
    ApiKey = "YOUR_LIVE_KEY",
    Region = SDKRegion.RU,
    DebugMode = true
});
```

### Шаг 2. Идентификация игрока

Вызовите после логина/регистрации игрока:

```csharp
GameDevPartnerSDK.IdentifyPlayer(playerId);
```

> Если включён `AutoIdentify`, SDK автоматически идентифицирует игрока по `deviceUniqueIdentifier`. Но рекомендуется вызывать `IdentifyPlayer` явно с вашим game-specific player ID.

### Шаг 3. Трекинг покупок

Вызовите после каждой успешной покупки:

```csharp
GameDevPartnerSDK.TrackPurchase(new PurchaseEvent {
    PlayerId = playerId,           // ID игрока
    ProductId = "gem_pack_100",    // ID продукта
    Amount = 299f,                 // Сумма (gross, до комиссии магазина)
    Currency = "RUB",              // Валюта
    Source = PaymentSource.GooglePlay, // Источник платежа
    TransactionId = txId,          // Уникальный ID транзакции от провайдера
    ReceiptData = receipt          // Чек для валидации (опционально)
});
```

**Доступные источники платежа (`PaymentSource`):**
| Значение | Описание |
|----------|----------|
| `GooglePlay` | Google Play Billing |
| `Apple` | Apple App Store |
| `YooKassa` | ЮKassa |
| `RuStore` | RuStore |
| `TBank` | T-Bank |
| `Web` | Веб-платежи |
| `Other` | Другое |

### Шаг 4. Трекинг рекламного дохода

SDK трекает impression-level ad revenue (ILAR) — доход с каждого показа рекламы. Поддерживаются все популярные медиаторы.

#### AdMob (Google)

1. Добавьте в **Player Settings → Scripting Define Symbols**: `GDP_ADMOB`

2. После загрузки рекламы подключите трекинг:

```csharp
using GameDevPartner.SDK.Adapters;

// Rewarded
var rewardedAd = new RewardedAd(adUnitId);
rewardedAd.LoadAd(request, (ad, error) => {
    if (ad != null) {
        GDPAdMobAdapter.TrackRewarded(ad, adUnitId);
    }
});

// Interstitial
var interstitialAd = new InterstitialAd(adUnitId);
interstitialAd.LoadAd(request, (ad, error) => {
    if (ad != null) {
        GDPAdMobAdapter.TrackInterstitial(ad, adUnitId);
    }
});

// Banner
var bannerView = new BannerView(adUnitId, adSize, position);
GDPAdMobAdapter.TrackBanner(bannerView, adUnitId);
```

> AdMob передает revenue в микро-единицах (1 000 000 = $1). Адаптер автоматически конвертирует.

#### IronSource / LevelPlay

1. Define: `GDP_IRONSOURCE`

2. Одна строка после инициализации:

```csharp
using GameDevPartner.SDK.Adapters;

IronSource.Agent.init(appKey);
GDPIronSourceAdapter.Enable(); // Автоматически трекает ВСЕ форматы
```

> Автоматически подписывается на `ImpressionDataReady` и трекает rewarded, interstitial, banner.

#### AppLovin MAX

1. Define: `GDP_APPLOVIN`

2. Одна строка после инициализации:

```csharp
using GameDevPartner.SDK.Adapters;

MaxSdk.InitializeSdk();
GDPAppLovinAdapter.Enable(); // Автоматически трекает ВСЕ форматы
```

> Автоматически подписывается на `OnAdRevenuePaidEvent` для всех форматов включая MREC.

#### Unity Ads

1. Define: `GDP_UNITY_ADS`

2. Вызывайте после показа рекламы:

```csharp
using GameDevPartner.SDK.Adapters;

// В вашем IUnityAdsShowListener:
public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state) {
    if (state == UnityAdsShowCompletionState.COMPLETED) {
        GDPUnityAdsAdapter.TrackShowComplete(placementId, AdType.Rewarded);
    }
}

// Опционально: настроить eCPM для более точных оценок
GDPUnityAdsAdapter.SetEcpm(
    rewardedEcpm: 12.0,      // $12 за 1000 показов (по умолчанию $10)
    interstitialEcpm: 6.0,   // $6 (по умолчанию $5)
    bannerEcpm: 1.5           // $1.5 (по умолчанию $1)
);
```

> Unity Ads не предоставляет ILAR данные, поэтому используется оценка на основе eCPM.

#### Yandex Ads

1. Define: `GDP_YANDEX_ADS`

2. В колбеке показа:

```csharp
using GameDevPartner.SDK.Adapters;

// Вариант 1: Из ImpressionData JSON
rewardedAd.OnImpression += (sender, args) => {
    GDPYandexAdsAdapter.TrackFromImpressionData(
        AdType.Rewarded, adUnitId, args.Data
    );
};

// Вариант 2: Вручную
GDPYandexAdsAdapter.TrackImpression(AdType.Interstitial, adUnitId, 0.0045, "USD");
```

#### Ручной трекинг (любая рекламная сеть)

Если ваш медиатор не в списке, можно трекать вручную:

```csharp
GameDevPartnerSDK.TrackAdImpression(new AdImpressionEvent {
    AdType = AdType.Rewarded,
    AdNetwork = AdNetwork.Other,
    AdUnitId = "my_rewarded_unit",
    Revenue = 0.005,         // Доход за показ в валюте
    Currency = "USD",
});
```

---

## Автоматический трекинг покупок

### Unity IAP

Замените одну строку инициализации:

```csharp
// БЫЛО:
UnityPurchasing.Initialize(listener, builder);

// СТАЛО:
GDPUnityIAP.Initialize(listener, builder);
```

Все покупки через Unity IAP будут автоматически отправляться в GameDevPartner.

### YooKassa

```csharp
GDPYooKassaTracker.Attach(yookassaSDK, (item) => {
    // Вернуть цену и валюту для товара
    return (price: 299f, currency: "RUB");
});
```

---

## Как это работает

### Атрибуция
1. Инфлюенсер делится ссылкой на вашу игру
2. Игрок переходит по ссылке и устанавливает игру
3. SDK автоматически определяет, от какого инфлюенсера пришел игрок (через install referrer / GAID / device fingerprint)
4. Все покупки и рекламный доход этого игрока атрибутируются инфлюенсеру

### Покупки
- SDK отправляет каждую покупку на сервер в реальном времени
- Сервер вычитает комиссию магазина (30% Google/Apple, 15% RuStore и т.д.)
- Инфлюенсер получает свой % от чистой суммы

### Рекламный доход
- SDK батчит impression-level данные (каждые 30 сек или 50 событий)
- Данные хранятся на сервере с привязкой к игроку
- Ежедневно (в 1:30 AM) сервер агрегирует рекламный доход и начисляет инфлюенсерам их %
- Рекламный доход уже net (сеть забрала свою долю), комиссия магазина НЕ вычитается

### Offline Queue
- Покупки: до 100 событий, хранятся 7 дней
- Реклама: до 500 событий
- При восстановлении сети — автоматическая отправка
- При закрытии приложения — сохранение в PlayerPrefs

---

## Безопасность

- Все запросы подписываются HMAC-SHA256
- Timestamp-валидация (окно 5 минут)
- Защита от replay-атак (nonce через Redis)
- Отдельные ключи для test/live режимов

---

## Требования

- Unity 2020.3 LTS или новее
- .NET Standard 2.1 или .NET 4.x
- Для Google Play referrer: `com.android.installreferrer:installreferrer:2.2` в gradle

## Поддержка

- Документация: [gamedevpartner.ru/developer/sdk](https://gamedevpartner.ru/developer/sdk)
- Telegram: [@gamedevpartner](https://t.me/gamedevpartner)

## Лицензия

MIT
