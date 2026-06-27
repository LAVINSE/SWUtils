# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Added a `Unity PlayerPrefs` tab to `SWTools/Debug/PlayerPrefs Viewer` for viewing, editing, and deleting standard Unity PlayerPrefs.
- Added search-target filters and an option to delete all standard Unity PlayerPrefs from `SWTools/Debug/PlayerPrefs Viewer`.
- Added `long` and `double` support to `SWEncrypt<T>`.
- Added `SetLong`, `GetLong`, `SetDouble`, and `GetDouble` to `SWUtilsPlayerPrefs`.

### Changed
- Updated the Entries list in `SWTools/Debug/PlayerPrefs Viewer` to support inline value editing and saving.
- Added `IsLogOutputEnabled` to control log output from `SWEventBus`.
- Added the `shouldOutputLog` parameter to `SWEventBus.Publish` for per-publication log control.

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
