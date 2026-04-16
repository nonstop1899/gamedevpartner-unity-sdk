# Changelog

## [2.6.2] - 2026-04-17
### Fixed
- `GDPUnityIAP.cs` moved into its own sub-assembly `GameDevPartner.SDK.IAP` under `Runtime/IAP/` with `defineConstraints: ["UNITY_PURCHASING"]`. The sub-asmdef is completely skipped by Unity when Unity IAP is not installed, eliminating `Failed to resolve assembly` errors that prevented the Editor menu from registering in 2.6.1.
- `#if UNITY_PURCHASING` guard removed from the source file — no longer needed since compilation is now gated at the asmdef level.

## [2.6.1] - 2026-04-17
### Fixed
- Runtime asmdef no longer hard-references `Unity.Purchasing`. Previously, projects without Unity IAP installed failed to compile the entire SDK assembly (CS0246 on `ConfigurationBuilder`, `IStoreListener`, etc.), which also prevented the `Window → GameDevPartner` editor menu from registering. `GDPUnityIAP.cs` is already guarded by `#if UNITY_PURCHASING`, so the reference is only needed transparently when the package is installed — `Unity.Purchasing` is auto-referenced.

## [2.6.0] - 2026-04-15
### Added
- `GameDevPartnerSDK.TrackAdRevenue(revenue, currency, adType, adNetwork, adUnitId)` — universal ad impression tracker
- Supports zero-revenue impressions for Unity Ads standard SDK (revenue imported server-side via Reporting API)
- Works with any ad network: Unity Ads, Yandex Ads, AppLovin MAX, IronSource, AdMob, other

### Changed
- For Unity Ads standard (without LevelPlay), developers now connect Reporting API in the GameDevPartner dashboard
  → platform nightly imports exact revenue in USD → converts to RUB via ЦБ rate → distributes across SDK impressions
- SDK only needs `TrackAdRevenue(0, ...)` call to link impressions to players/influencers

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
