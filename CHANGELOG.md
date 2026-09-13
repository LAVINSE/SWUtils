# Changelog

[한국어](CHANGELOG.ko.md) | [English](CHANGELOG.md)

## [v1.3.1] - 2026-09-13

- Validate save input before applying it and restore preferences on failure. Replace files after temporary writes, check backups when loading fails, and restore asynchronous responses to the requested slot.
- Complete quests after all rewards succeed, persist granted reward records for retries, and isolate notification exceptions from completion.
- Queue nested state transitions in request order. Allow scene cancellation only before the engine operation starts, and clean up request state around failures and callbacks.
- Fixed shared realtime waits, popup redisplay, pool activation order and volume updates for playing sound effects.
- Validate required table columns, boolean values and quoted localization fields. Fixed subtree cycles, blackboard updates, timers, event diagnostics, submit input, stat range notifications and conditional-field caching.
- Use TextMeshPro from Unity UI 2.0 and declare required engine modules. Enable iCloud integration only with a native bridge and `SW_ICLOUD_ENABLE`.
- Organized all feature descriptions, usage examples, editor menus and navigation in the README, condensed release notes and duplicate comments, and added `SetGaugeText` while retaining `SetGauge`.

## [v1.3.0] - 2026-09-10

- Added skill tree authoring, runtime and views, with prerequisites, repeat upgrades, multiple currencies, refunds, persistence and resets that retain permanent nodes.
- Added the MiningSkillTree example with 81 nodes across 6 paths, saved layouts, dragging, zooming and node navigation.
- Views use the project font and node size configured on TreeView. Example generation and layout tools run from the inspector.
- Grouped shared `SWIdentifiedObject` fields into a collapsed base-definition section that remembers its expanded state.

## [v1.2.2] - 2026-09-07

- Updated object identifiers and hierarchy handling for Unity 6.4+, and graph dropdowns and object searches for Unity 6.6.
- Validated Runtime, Editor and Samples compilation against Unity 6000.6.0f1 and 6000.3.11f1 reference assemblies for this release.

## [v1.2.1] - 2026-09-07

- Standardized Korean code regions and documentation comments, removed repeated explanations and clarified `SWTimer.Reset`. Runtime behavior was unchanged.

## [v1.2.0] - 2026-09-07

- Added the experimental quest and achievement module, editor, separate databases, progress reports, extensible conditions and rewards, and replaceable save stores.
- Fixed negative progress, restored groups and tasks by code name after reordering, and prevented completed rewards from being granted again on restore.

## [v1.1.1] - 2026-08-27

- Reorganized Korean and English documentation and screenshots, and declared Physics and Physics 2D dependencies.
- Fixed editor references in player builds and platform-specific vibration branches.

## [v1.1.0] - 2026-07-22

- Added layered and stack state machines, graph assets, factories, editors and runtime inspection.
- Added behaviour trees, blackboards, subtrees, custom nodes and runners.
- Added graph lists, automatic layout, node search and categories, script templates and saved editor settings.
- Raised the minimum version to Unity 6 and fixed graph selection, type lookup and transition summary positioning.

## [v1.0.16] - 2026-07-10

- Added identified-asset icons and Stat System Editor list sizing, sorting and display settings; reorganized related menus.

## [v1.0.15] - 2026-07-08

- Added debug console keys, modifiers, touch input, optional Input System support and performance overlay settings.

## [v1.0.14] - 2026-07-07

- Improved conditional fields, subclass selection and base-class inspector groups.

## [v1.0.13] - 2026-07-07

- Updated package metadata and installation URLs.

## [v1.0.12] - 2026-07-07

- Renamed namespaces to avoid Unity and .NET type collisions: `SW.Attributes`, `SW.Coroutines`, `SW.Debugging`, `SW.ScreenResolution` and `SW.EditorTools.Attributes`.

## [v1.0.11] - 2026-07-06

- Reorganized namespaces under `SW.*` and renamed public `SWUtils...` types to `SW...`, preserving existing save keys.
- Updated Korean XML comments, attribute examples and Korean and English documentation.

## [v1.0.10] - 2026-07-02

- Fixed read-only package installation by matching folder and metadata filenames.

## [v1.0.9] - 2026-07-02

- Added `SWScriptableObject`, editing of native PlayerPrefs, `long` and `double` persistence, and `SWTime.ToDateTime`.
- Added event log controls, class-field table import and vertical tables. Renamed `SWUtilsRefillTimer` to `SWRefillTimer`, preserving save keys.

## [v1.0.8] - 2026-06-16

- Added large-number formatting and its preset editor, plus `SWRectDummy` for raycast areas without a mesh.

## [v1.0.7] - 2026-06-08

- Added event publish and subscription diagnostics, and pool creation, activity and return monitoring.
- Split menus into `SWTools/Debug` and `SWTools/Utils` and fixed Steam branch compilation in the editor.

## [v1.0.6] - 2026-06-02

- Added `SWSubClassSelector`, `SWAddTypeMenu`, `SWHideInTypeMenu` and examples of implementation type selection in lists and arrays.

## [v1.0.5] - 2026-05-29

- Added the TextMeshPro font manager performance tab and checks for atlases, glyphs, fallback fonts and materials.

## [1.0.0] - 2026-03-19

- Initial release of runtime utilities and editor tools.
