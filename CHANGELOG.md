# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-02-27

### Added
- First-launch "Command Briefing" overlay with updated gameplay instructions.
- Right-click rotation support on fleet cells and ship tray cards.
- Richer sound cue sequences for miss/new game/placement events.

### Changed
- Reworked main screen into a board-focused command-center layout with a fixed top control bar.
- Increased board cell sizing to improve ship-grid alignment and readability.
- Clarified stat wording to distinguish lifetime turns from current mission stats.
- Added cinematic turn transition messaging to keep turn pacing readable.

### Fixed
- Removed scroll-container behavior that caused board clipping and awkward ship displacement.
- Updated settings persistence schema and tests for command-briefing visibility state.

## [1.2.0] - 2026-02-25

### Added
- New ship and combat image assets for gameplay polish.

### Changed
- Standardized image file naming in app resources.
- Updated release metadata to use a consistent semantic version (`1.2.0`).

### Fixed
- CI workflow reliability for MAUI workload setup and test execution.

## [1.1.0] - 2026-02-25

### Added
- Persistent stats panel with win/loss/draw and shot metrics.
- Expanded ship UI polish and feedback mechanics during play.

### Changed
- Improved turn flow, game settings behavior, and post-game UX.

## [1.0.0] - 2026-02-25

### Added
- First public playable release of Battleship MAUI.
- Manual fleet placement with rotation.
- Player-vs-CPU turn loop with hunt/target attack logic.
- End-game reveal behavior and core gameplay tests.
