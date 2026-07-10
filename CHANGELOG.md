# Changelog

[English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

All notable changes to this project will be documented in this file.

Release notes are grouped by version and date. The latest release is summarized first for package-manager checks and Git tag verification.

| Latest | Date | Focus |
| --- | --- | --- |
| `v1.0.16` | 2026-07-10 | Stat System Editor list display and asset icon editing improvements. |

## [Unreleased]

No pending changes.

## [v1.0.16] - 2026-07-10

### Added
- Added an editor-only sprite icon field to `SWIdentifiedObject` and displayed it in inherited object lists such as the Stat System Editor.
- Added quick sort buttons and selected asset renaming to `SWTools/Utils/Data/Stat System Editor`.
- Added Stat System Editor display options and a preview for list row height, icon size, delete button size, and text size.

### Changed
- Split `SWTools/Debug` and `SWTools/Utils` editor windows into more specific submenu groups.
- Reworked the Stat System Editor list rows so the delete button keeps a fixed visible area.
- Updated package metadata and README installation URLs to `v1.0.16`.

## [v1.0.15] - 2026-07-08

### Added
- Added debug console settings for open-key modifiers, startup overlay display, overlay metrics, and optional Input System input.

### Changed
- Reworked the debug console settings window into focused tabs for status, input, overlay, and play-mode controls.
- Removed the mandatory Input System package dependency while keeping optional Input System console input through cached reflection.
- Updated package metadata and README installation URLs to `v1.0.15`.

## [v1.0.14] - 2026-07-07

### Added
- Added `SWCondition` handling to `SWSubClassSelector` fields so conditional disabled and hidden states work on `SerializeReference` fields and collections.

### Changed
- Grouped inherited `SWIdentifiedObject` definition fields under a `데이터 정의` foldout using the existing `SWGroup` inspector design.
- Updated package metadata and README installation URLs to `v1.0.14`.

## [v1.0.13] - 2026-07-07

### Changed
- Updated package metadata and README installation URLs to `v1.0.13`.

## [v1.0.12] - 2026-07-07

### Changed
- Renamed conflict-prone namespaces to clearer feature namespace names: `SW.Attributes`, `SW.Coroutines`, `SW.Debugging`, `SW.ScreenResolution`, and `SW.EditorTools.Attributes`.
- Updated package metadata and README installation URLs to `v1.0.12`.

## [v1.0.11] - 2026-07-06

### Changed
- Reorganized runtime namespaces into feature-oriented `SW.*` namespaces and aligned Runtime and Editor folders with those namespaces.
- Renamed remaining public `SWUtils...` types to the consistent `SW...` form while preserving saved-data keys.
- Standardized Korean XML documentation and corrected malformed attribute examples.
- Added English and Korean README and Changelog documents with language navigation links.
- Updated the package version and README installation URL to `v1.0.11`.

## [v1.0.10] - 2026-07-02

### Changed
- Updated the package version and README installation URL to `v1.0.10`.

### Fixed
- Corrected the folder `.meta` file names for `Runtime/SWMonoBehaviour`, `Runtime/SWScriptableObject`, `Editor/SWMonobehaviour`, and `Editor/SWScriptableObject` so immutable package installations can import them.

## [v1.0.9] - 2026-07-02

### Added
- Added `SWScriptableObject` with the same grouped fields, inspector buttons, and repaint attributes provided by `SWMonoBehaviour`.
- Added a `Unity PlayerPrefs` tab to `SWTools/Debug/PlayerPrefs Viewer` for viewing, editing, and deleting standard Unity PlayerPrefs.
- Added search-target filters and an option to delete all standard Unity PlayerPrefs from `SWTools/Debug/PlayerPrefs Viewer`.
- Added `long` and `double` support to `SWEncrypt<T>`.
- Added `SetLong`, `GetLong`, `SetDouble`, and `GetDouble` to `SWPlayerPrefs`.
- Added `SWTime.ToDateTime` for converting saved time strings to `DateTime` values.

### Changed
- Updated the Entries list in `SWTools/Debug/PlayerPrefs Viewer` to support inline value editing and saving.
- Added `IsLogOutputEnabled` to control log output from `SWEventBus`.
- Added the `shouldOutputLog` parameter to `SWEventBus.Publish` for per-publication log control.
- Renamed `SWUtilsRefillTimer` to `SWRefillTimer` while preserving its saved-data keys.
- Extended `SWTableSheet` importing to support ordinary class fields in addition to lists and arrays.
- Added a vertical field-and-value layout to `SWTools/Utils/Excel Table Importer`.
- Renamed the MonoBehaviour and ScriptableObject implementation folders to match their SWUtils type names.
- Updated the package version to `v1.0.9`.
- Expanded the README with setup steps and practical examples for the major runtime and editor workflows.

## [v1.0.8] - 2026-06-16

### Added
- Added `SWAmountFormat` for formatting large numbers with suffixes such as K, M, B, and T.
- Added `SWAmountFormatProfile` for managing number suffixes and decimal settings in a Resources preset asset.
- Added `SWTools/Utils/Amount Format Window` for automatically creating, editing, and previewing preset assets.
- Added `SWRectDummy`, a mesh-free UI Graphic that provides a raycast area without an Image.
- Added the `GameObject/UI/SW Rect Dummy` menu item and a dedicated Inspector.

### Changed
- Updated the package version to `v1.0.8`.
- Added number format preset and Rect Dummy usage instructions to the README.

## [v1.0.7] - 2026-06-08

### Added
- Added `SWTools/Debug/EventBus Debugger Window`.
- Added per-event listener counts, publication counts, latest publication times, and latest data snapshots to `SWEventBus`.
- Added `SWTools/Debug/Pool Monitor Window`.
- Added per-prefab snapshots for created, active, inactive, spawned, returned, destroyed, and delayed-return counts to `SWPool`.

### Changed
- Separated EditorWindow menu paths into `SWTools/Debug` and `SWTools/Utils`.
- Documented the actual EditorWindow menu paths in the README.
- Updated the package version to `v1.0.7`.

### Fixed
- Guarded the Steamworks.NET branch with `!UNITY_EDITOR` to prevent Unity build issues in runtime scripts.
- Removed an unused `UnityEditor` reference from `SWConditionAttribute`.

## [v1.0.6] - 2026-06-02

### Added
- Added `SWSubClassSelector`.
- Added a searchable dropdown for selecting implementations of abstract classes or interfaces on `SerializeReference` fields.
- Added `SWAddTypeMenu` for defining type selection menu paths.
- Added `SWHideInTypeMenu` for hiding specific types from the type selection menu.
- Added the `SWSubClassSelectorExample` sample with single-field, array, and `List<T>` examples.

### Changed
- Updated the package version to `v1.0.6`.
- Applied the `SWExample` namespace to sample scripts.

## [v1.0.5] - 2026-05-29

### Added
- Added a TextMeshPro font asset performance tab to `SWTools/TMP Font Asset Manager`.
- Added inspection of atlas memory, glyphs, characters, fallback chains, dynamic atlases, and material presets.
- Added a TextMeshPro font asset performance guide to the README.

### Changed
- Updated README and Changelog version references to v1.0.5.

## [1.0.0] - 2026-03-19

### Added
- Initial SWUtils release.
