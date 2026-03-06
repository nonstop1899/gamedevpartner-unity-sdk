# Changelog

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
