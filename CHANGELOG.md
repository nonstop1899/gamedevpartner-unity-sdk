# Changelog

## [2.4.0] - 2026-03-22
### Added
- HTTP retry with exponential backoff for identify, purchase, and ad batch requests (up to 3 retries on network/5xx/429 errors)
- `IsRetryableError()` helper for smart retry decisions
- `TrackAdRevenue()` — simplified one-line API for any ad network (no defines needed)
- Universal ad revenue hint in Settings UI

### Fixed
- Null reference crash in `DoIdentify()` and `DoSendAdBatch()` when server returns unexpected response
- Removed aggressive `CleanupStaleDefines` that was resetting user's ad SDK choices (especially Yandex Ads via code integration)
- Removed `[InitializeOnLoad]` auto-detection — developer has full manual control via Settings

### Changed
- Ad SDK management is now fully manual via `Window > GameDevPartner > Settings`
- Yandex Ads label updated to "Yandex Ads (любая интеграция)" to clarify it works with code-based integration too

## [2.3.0] - 2026-03-21
### Added
- Ad revenue tracking with impression-level data (ILAR)
- Adapters for AdMob, IronSource, AppLovin MAX, Unity Ads, Yandex Ads
- `AdImpressionEvent`, `AdType`, `AdNetwork` enums
- Ad impression batching (50 events or 30 seconds)
- Ad offline queue (up to 500 events) with PlayerPrefs persistence
- Auto-detect ad SDKs via `GDPDefineSymbolsManager`
- Settings UI for enabling/disabling ad network adapters

## [2.2.0] - 2026-03-15
### Added
- RuStore Install Referrer support via reflection
- Install referrer caching in PlayerPrefs (7-day retention)
- `GDPInstallReferrer` with Google Play and RuStore detection

## [2.1.0] - 2026-03-10
### Added
- YooKassa auto-tracking via `GDPYooKassaTracker.Attach()`
- Unity IAP wrapper `GDPUnityIAP.Initialize()` for automatic purchase tracking
- Session tracking for retention analytics
- `DoTrackSession()` called after successful identify

## [2.0.0] - 2026-03-06
### Changed
- SDK auto-initializes at game startup — no Init() call needed
- Settings stored as ScriptableObject (Assets/Resources/GameDevPartnerConfig.asset)
- Auto-identify player by device ID (configurable)
- Only TrackPurchase() call required in game code

### Removed
- Manual Init() no longer required (still available for advanced use)
- EditorPrefs replaced by ScriptableObject config

## [1.0.1] - 2026-03-06
### Fixed
- Added .meta files for all SDK assets (required for Unity UPM packages)

## [1.0.0] - 2026-03-06
### Added
- Initial release
- Install tracking with device fingerprint attribution
- Purchase event reporting with HMAC signing
- Editor settings window for API key configuration
- Unity 2020.3+ support
