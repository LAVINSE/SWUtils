# SWUtils

[한국어](README.md) | [English](README.en.md)

Shared runtime systems and editor tools for Unity 6. SWUtils provides save slots, state machines, behaviour trees, quests, skill trees, pooling and popups, with inspectors and editors for authoring their data.

The package is in testing. Validate the features you use, including quest and achievement persistence, in your project before release.

## Installation

In Unity Package Manager, choose `+ > Add package from git URL...` and enter the release tag:

```text
https://github.com/LAVINSE/SWUtils.git#v1.3.1
```

The URL above requires the `v1.3.1` tag in the remote repository. Before the tag is published, use `+ > Add package from disk...` and select the local `package.json`. To fetch development code, specify the branch or commit you need. See the [version history](CHANGELOG.md).

Required packages and modules are declared in `package.json`: Localization, TextMeshPro included in Unity UI 2.0, Audio, Android JNI, IMGUI, JSON Serialize, Physics and Physics 2D. Input System support is optional and uses the package already installed in your project.

Cloud integrations require separate setup: `SW_GOOGLEPLAY_ENABLE` for Google Play Games, `SW_STEAMWORKS_NET` for Steamworks.NET and `SW_ICLOUD_ENABLE` for an iCloud native bridge supplied by your project. See the [integration and persistence requirements (Korean)](Documentation~/Reliability.ko.md).

## First use

1. Derive components from `SW.Base.SWMonoBehaviour` and data assets from `SW.Base.SWScriptableObject`.
2. Import `SW.Attributes` for inspector attributes.
3. Reference `SWUtils.Runtime` if your project uses assembly definition files.
4. Find examples under `Packages > SWUtils > Samples` in the Project window.

Attach this component to a game object to edit a score and save it with an inspector button:

```csharp
using UnityEngine;
using SW.Attributes;
using SW.Base;
using SW.Data;

/// <summary>인스펙터에서 설정한 시작 점수를 저장하는 예제입니다.</summary>
public sealed class ScoreSettings : SWMonoBehaviour
{
    [SerializeField] private int startingScore = 100;

    /// <summary>현재 저장 슬롯에 시작 점수를 기록합니다.</summary>
    [SWButton("Save starting score")]
    private void SaveStartingScore()
    {
        SWPlayerPrefs.SetInt("StartingScore", startingScore);
        SWPlayerPrefs.Save();
    }
}
```

Configure scene persistence on each manager component. Choose save slots as part of your game's save flow.

## Find a feature

These links lead to the feature descriptions in this README, including setup instructions and usage examples.

| Area | README sections |
| --- | --- |
| Components and inspectors | [Base types](#runtime-base) · [Attributes](#runtime-attributes) |
| Scheduling and persistence | [Coroutines](#runtime-coroutines) · [Storage](#runtime-storage) |
| Game systems | [Skill trees](#runtime-skilltree) · [Quests and achievements](#runtime-quests) · [Stats](#runtime-stats) |
| Control flow | [State machines](#runtime-states) · [Behaviour trees](#runtime-behaviour) |
| Objects and views | [Pooling](#runtime-pooling) · [Popups](#runtime-popups) · [Resolution](#runtime-resolution) |
| Time, input, audio, scenes and events | [Utilities](#runtime-utilities) |
| Debugging and authoring | [Runtime console](#runtime-debug) · [Editor features and menus](#editor-tools) |

[Namespaces](#namespace-layout) · [Samples](#samples) · [Assemblies](#assembly-definitions) · [Changelog](CHANGELOG.md)

## Preview

Configure inspector groups, conditional fields and method buttons:

<img src="Documentation~/Media/SWAttribute.gif" alt="SWUtils inspector attribute example" width="460">

Connect state and behaviour nodes in graph editors and inspect their runtime state:

![State machine editor](Documentation~/Media/SWStateMachine.gif)
![Behaviour tree editor](Documentation~/Media/SWBehaviourTree.gif)

[Asset palette](Documentation~/Media/SWAssetPalette.png) · [Reference finder](Documentation~/Media/ReferenceFinder.png) · [Table importer](Documentation~/Media/ExcelTable.png) · [Number formatting](Documentation~/Media/AmountFormat.png)

## Namespace Layout

Runtime and Editor code use feature-oriented namespaces that match their folders:

| Folder | Namespace |
| --- | --- |
| `Runtime/Attribute` | `SW.Attributes` |
| `Runtime/Base` | `SW.Base` |
| `Runtime/Behaviour` | `SW.BehaviourTree` |
| `Runtime/Coroutine` | `SW.Coroutines` |
| `Runtime/Data` | `SW.Data` |
| `Runtime/Debug` | `SW.Debugging` |
| `Runtime/Pooling` | `SW.Pooling` |
| `Runtime/Popup` | `SW.Popup` |
| `Runtime/Quest` | `SW.Quest` |
| `Runtime/SkillTree` | `SW.SkillTree` |
| `Runtime/Resolution` | `SW.ScreenResolution` |
| `Runtime/Stat` | `SW.Stat` |
| `Runtime/StateMachine` | `SW.StateMachine` |
| `Runtime/Util` | `SW.Util` |
| `Editor/Attribute` | `SW.EditorTools.Attributes` |
| `Editor/<Feature>` | `SW.EditorTools.<Feature>` |

## Runtime Features

<a id="runtime-skilltree"></a>

### Skill trees

Provides incremental upgrades, research unlocks, and talent choices through reusable definitions and independent progress for each owner. Supports prerequisite levels, all/any requirements, exclusive branches, repeated upgrades, multiple currencies, refunds, save restoration, and progress resets that retain permanent nodes. Costs, conditions, effects, wallets, and save stores can be extended for each project.

- Editor: `SWTools > Utils > Data > Skill Tree Editor`.
- Example: `Samples/Prefab/SWSkillTreeExample.prefab`, with **81 nodes and six expansion paths** in MiningSkillTree.
- Navigation: drag to pan, scroll to zoom around the pointer, and use `Start`, `Selected`, and `Fit All` to focus the first revealed node, selected node, or currently displayed nodes.
- Layout: Inspector buttons generate nodes, save edited positions to the shared tree definition, and reload saved positions. Automatic layout gives saved coordinates priority; Skill Tree Editor shares those coordinates. Connections follow the actual node rectangles.
- Editing preview: **편집 미리보기 → 전체 노드 보기** displays every node for placement without changing gameplay reveal rules. Hidden and masked nodes still follow prerequisite progress during play.
- Node size: set it directly under **노드 배치 → 노드 크기** in TreeView. No separate ViewStyle asset is required.

The default view uses TextMeshProUGUI with the project's default font. No font data is bundled. Player save data does not include view layout. Sample creation and rebuilding are available through editor code and Inspector workflows rather than SWTools menu entries.

See the [skill tree guide, extension contracts, (Korean)](Documentation~/SkillTree.md).

Use `SWSkillTreeSystem` with an `ISWSkillTreeWallet` implementation to create independent player progress. `PreviewPurchase` checks a purchase, `Purchase` charges its full cost, and `Refund` returns the actual payment for the last level when prerequisites allow it. `Reset` controls permanent-node retention and whether payments are refunded. `CaptureSaveData` and `Restore` handle progress; save wallet balances in the same game snapshot.

Extend `SWSkillTreeCondition`, `SWSkillTreeCost` and `SWSkillTreeEffect` for project rules. `SWSkillTreeEffectBinding` applies ongoing effects from absolute levels; dispose the binding before the system. Use the `Purchased` event for one-time purchase notifications and `Changed` for progress, restore and balance changes.

<a id="runtime-attributes"></a>

### Inspector attributes

Attributes that extend Inspector presentation and behavior.

- `SWButton`: Runs a method from an Inspector button.
- `SWButtonBar`: Displays multiple methods as a group of buttons.
- `SWCondition`: Shows or hides a field based on a Boolean field.
- `SWEnumCondition`: Shows or hides a field based on an enumeration value.
- `SWDropdown`: Displays a predefined list of values as a dropdown.
- `SWGroup`: Groups Inspector fields.
- `SWReadOnly`: Displays a field as read-only.
- `SWSubClassSelector`: Selects an implementation of an abstract class or interface from a searchable dropdown on a `SerializeReference` field.
- `SWAddTypeMenu`: Defines the type menu path shown in the `SWSubClassSelector` dropdown.
- `SWHideInTypeMenu`: Hides a type from the `SWSubClassSelector` dropdown.
- `SWRequiresConstantRepaint`, `SWRequiresConstantRepaintOnlyWhenPlaying`: Define custom Inspector repaint conditions.
- `SWTable`, `SWTableSheet`: Connect tabular data to ScriptableObject fields.

Example:

```csharp
using SW.Attributes;
using SW.Base;
using UnityEngine;

public class ExampleComponent : SWMonoBehaviour
{
    [SWGroup("Status")]
    [SWReadOnly]
    [SerializeField] private int currentLevel;

    [SerializeField] private bool useOption;

    [SWCondition("useOption", true)]
    [SerializeField] private int optionValue;

    [SWButton("Refresh Value")]
    private void RefreshValue()
    {
        currentLevel++;
    }
}
```

`SWSubClassSelector` example:

```csharp
using System;
using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using UnityEngine;

public class SubClassSelectorExample : SWMonoBehaviour
{
    [SerializeReference]
    [SWSubClassSelector]
    [SerializeField] private SkillAction skillAction;

    [SerializeReference]
    [SWSubClassSelector]
    [SerializeField] private List<SkillAction> skillActions = new List<SkillAction>();
}

[Serializable]
public abstract class SkillAction
{
    public abstract void Execute();
}

[Serializable]
[SWAddTypeMenu("Skill/Heal")]
public class HealSkillAction : SkillAction
{
    [SerializeField] private int healAmount = 10;

    public override void Execute()
    {
    }
}

[Serializable]
[SWHideInTypeMenu]
public class HiddenSkillAction : SkillAction
{
    public override void Execute()
    {
    }
}
```

When an abstract class or interface such as `SkillAction` is used as the base type, serializable implementations such as `HealSkillAction` appear in an Inspector dropdown. Use `SWAddTypeMenu` to define a menu path and `SWHideInTypeMenu` to exclude a type.

<a id="runtime-coroutines"></a>

### Coroutines

Separates coroutine execution behind an interface so runtime code does not depend tightly on a specific MonoBehaviour.

- `ICoroutineRunner`: Defines operations for coroutines, delayed actions, and simple interpolation.
- `SWCoroutineRunner`: Executes the coroutines.

Example:

```csharp
using System.Collections;
using SW.Coroutines;
using UnityEngine;

public class CoroutineExample : MonoBehaviour
{
    [SerializeField] private SWCoroutineRunner coroutineRunner;

    private void Start()
    {
        coroutineRunner.Run(DelayLog());
    }

    private IEnumerator DelayLog()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Complete");
    }
}
```

`SWCoroutineRunner.Wait` caches up to 128 durations. `WaitRealtime` creates an independent wait object for each call. Scheduling operations include:

```csharp
coroutineRunner.DelayedCall(1f, () => Debug.Log("One second later"));
coroutineRunner.NextFrame(() => Debug.Log("Next frame"));
coroutineRunner.Repeat(0.5f, 3, index => Debug.Log($"Repeat {index}"));
coroutineRunner.Tween(
    0.25f,
    progress => transform.localScale = Vector3.one * progress,
    () => Debug.Log("Tween complete"),
    true);
```

Keep the returned `Coroutine` when an individual operation may need to be cancelled with `Stop`.

<a id="runtime-storage"></a>

### Storage

Provides save data, PlayerPrefs, encryption, and cloud save features.

- `SWPlayerPrefs`: A PlayerPrefs wrapper with slot selection, encryption, and JSON import and export.
- `SWPlayerPrefsSettings`: A settings asset that stores project-specific salt and initialization-vector salt values.
- `SWSaveDataManager`: Saves and loads multiple data types in a file.
- `SWCloud`: A unified entry point for Google Play Games, iCloud, Steamworks.NET, and local fallback saves.
- `SWEncrypt<T>`: Stores `int`, `long`, `float`, `double`, `bool`, and `string` values as encrypted PlayerPrefs data.
- `SWSaveSlot`: Provides default save-slot constants.

Example:

```csharp
using SW.Data;
using SW.Util;

SWPlayerPrefs.SetSlot("player_01");
SWPlayerPrefs.SetInt("coin", 100);
SWPlayerPrefs.Save();

int coin = SWPlayerPrefs.GetInt("coin", 0);
```

The selected slot is included in the stored key space. Select the slot before every read or write flow that can run before your bootstrap initialization.

```csharp
SWPlayerPrefs.SetSlot("player_01");

SWPlayerPrefs.SetLong("total_score", 125000L);
SWPlayerPrefs.SetDouble("play_time", 42.5d);
SWPlayerPrefs.SetBool("tutorial_complete", true);
SWPlayerPrefs.Save();

string exportedJson = SWPlayerPrefs.ExportToJson();
bool imported = SWPlayerPrefs.ImportFromJson(exportedJson);
```

`ImportFromJson` replaces the entire selected slot after validation, while `MergeFromJson` preserves existing keys and overwrites matching incoming keys. Use `HasKey`, `DeleteKey`, and `DeleteAll` for key-level or slot-level cleanup.

Create and edit salt settings from `SWTools/Utils/Project/PlayerPrefs Salt Settings`. The settings asset is created at `Assets/Resources/SWPlayerPrefsSettings.asset` and loaded automatically through Resources at runtime. Data saved with a previous salt cannot be read after the salt changes, so delete or migrate existing data first.

Save manager example:

```csharp
using System;
using SW.Data;
using SW.Util;

[Serializable]
public class PlayerSaveData
{
    public int level;
    public int coin;
}

SWSaveDataManager.SetData(new PlayerSaveData { level = 3, coin = 100 });
SWSaveDataManager.SaveAll();

SWSaveDataManager.LoadAll();
PlayerSaveData data = SWSaveDataManager.GetData<PlayerSaveData>();
```

Use `TryGetData` when a save may not exist, and use the asynchronous methods when cloud synchronization is enabled:

```csharp
SWSaveDataManager.SetSlot("player_01");

if (SWSaveDataManager.HasSave())
{
    await SWSaveDataManager.LoadAllAsync();

    if (SWSaveDataManager.TryGetData(out PlayerSaveData loadedData))
        Debug.Log($"Loaded level: {loadedData.level}");
}

await SWSaveDataManager.SaveAllAsync();
```

`ListSaves`, `CopySlot`, `Delete`, and `GetSaveInfo` provide save-slot management. `SaveAll` writes the registered data and the current SWUtils PlayerPrefs slot together; changing the save-manager slot also aligns the PlayerPrefs slot.

Slot overloads of `SetString`, `GetString`, `ImportFromJson` and `MergeFromJson` operate on a specified slot without changing the current selection. `ExportSlotToJson` exports that slot. Empty strings and keys containing delimiters are preserved. Imports validate their input before applying it and attempt to restore previous values on failure.

File saves replace the target after a temporary write completes. Registered-data loads check a backup when the main file is missing or cannot be parsed. Cloud restore uses the requested slot. File and PlayerPrefs updates do not form an atomic transaction across process termination. See the [persistence contracts](Documentation~/Reliability.ko.md) for provider setup and legacy local-cache behavior.

<a id="runtime-debug"></a>

### Debug console

Provides a runtime debug console, command registration, watch values, logging helpers, and a lightweight performance overlay.

- `SWDebugConsole`: Shows runtime logs, executes registered commands, manages watch values, and draws the performance overlay.
- `SWDebugConsoleSettings`: Stores console open input, optional Input System handling, and overlay display settings.
- `SWCommand`: Registers static or instance methods as console commands.
- `SWLog`: Writes logs only when `SW_DEBUG_MODE` is enabled.

Add `SW_DEBUG_MODE` from `SWTools/Debug/Console/Debug Console Settings` before using the console in a build. When the symbol is missing, console calls are compiled out through conditional methods.

Debug console setup:

1. Open `SWTools/Debug/Console/Debug Console Settings`.
2. Select `상태` and add `SW_DEBUG_MODE` for the current build target.
3. Select `입력` and create the settings asset if you want project-specific input values.
4. Choose the open key and optional `Control`, `Shift`, or `Alt` modifiers.
5. Set the mobile touch count used to open the console on touch devices.

Input System is optional. If `Input System 확인` is enabled and the package is installed, the console reads input through cached reflection. Otherwise it uses Unity's built-in `Input` API.

Performance overlay setup:

1. Open `SWTools/Debug/Console/Debug Console Settings`.
2. Select `오버레이`.
3. Enable `시작 시 표시` if the overlay should appear automatically after scene load.
4. Choose the screen corner, scale, update interval, visible metrics, and FPS warning thresholds.

Runtime control:

```csharp
using SW.Debugging;

SWDebugConsole.Show();
SWDebugConsole.ToggleOverlay();
SWDebugConsole.ResetOverlayStats();
```

Register a command:

```csharp
using SW.Attributes;

public class DebugCommands
{
    [SWCommand("give_gold", "Adds gold for testing", "Test")]
    private static void GiveGold(int amount)
    {
    }
}
```

<a id="runtime-base"></a>

### Base components

Provides the `SWMonoBehaviour` base class for use with the attribute-driven `SWTools` custom Inspector.

Example:

```csharp
using SW.Attributes;
using SW.Base;
using UnityEngine;

public class PlayerController : SWMonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
}
```

### Identified assets

`SWIODatabase` stores and looks up `SWIdentifiedObject` assets, while `SWCategory` groups related definitions.

Shared definition fields are grouped under **기본 정의**, collapsed by default. This applies automatically to existing assets and derived types, while preserving the user's foldout preference.

### Data assets

Provides the `SWScriptableObject` base class for ScriptableObject assets that use the same attribute-driven custom Inspector as `SWMonoBehaviour`.

Fields and methods can use Inspector features such as `SWGroup`, `SWButton`, `SWCondition`, `SWReadOnly`, and the constant repaint attributes.

Example:

```csharp
using SW.Attributes;
using SW.Base;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterData",
    menuName = "Game Data/Character Data")]
public class CharacterData : SWScriptableObject
{
    [SWGroup("Status")]
    [SerializeField] private int maxHp = 100;

    [SWGroup("Status")]
    [SWReadOnly]
    [SerializeField] private int calculatedPower;

    [SWButton("Recalculate Power")]
    private void RecalculatePower()
    {
        calculatedPower = maxHp * 2;
    }
}
```

<a id="runtime-pooling"></a>

### Pooling

Provides GameObject pooling and group-based spawning.

- `SWPool`: A singleton-based object pool.
- `IPool`: Defines the pooling contract.
- `IPoolable`: Defines callbacks invoked when an object is spawned from or returned to a pool.
- `SWPoolCatalog`: A ScriptableObject containing pool registrations.
- `SWPoolRegistry`: Registers pools and groups when a scene starts.
- `SWPoolGroupSelectionMode`: Defines how an object is selected from a group.
- `SWPoolSnapshot`: A read-only pool state snapshot for `SWTools/Debug/Pool/Pool Monitor Window`.

Example:

```csharp
using SW.Pooling;
using UnityEngine;

public class SpawnExample : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;

    private void Start()
    {
        SWPool.Instance.Prewarm(bulletPrefab, 20);
    }

    private void Fire()
    {
        GameObject bullet = SWPool.Instance.Spawn(bulletPrefab, transform.position, transform.rotation);
        SWPool.Instance.Release(bullet, 2f);
    }
}
```

Setup:

1. Create an `SWPoolCatalog` asset and register each prefab with its prewarm count and maximum size.
2. Add `SWPool` and `SWPoolRegistry` to the bootstrap scene.
3. Assign the catalog to `SWPoolRegistry`.
4. Spawn by prefab reference or by a configured group, then return instances through `Release`.

Pool callback example:

```csharp
using SW.Pooling;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    private IPool pool;
    private float remainingLifetime;

    /// <summary>반납할 풀을 연결합니다.</summary>
    public void SetPool(IPool pool)
    {
        this.pool = pool;
    }

    /// <summary>활성화 전에 재사용할 탄환의 수명을 초기화합니다.</summary>
    public void OnSpawnFromPool()
    {
        remainingLifetime = 2f;
    }

    /// <summary>반납 시 남은 수명을 비웁니다.</summary>
    public void OnReturnToPool()
    {
        remainingLifetime = 0f;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f) pool.Release(gameObject);
    }
}
```

The pool sets the parent, position and pool reference before `OnSpawnFromPool`, then activates the instance. Prewarming keeps objects inactive and does not invoke spawn or return callbacks.

<a id="runtime-popups"></a>

### Popups

Manages popup creation, display, hiding, caching, and animation.

- `SWPopupBase`: The base component for all popups.
- `SWPopupManager`: Handles popup display, hiding, key-based registration, and caching.
- `SWPopupCatalog`: A ScriptableObject that maps keys to popup prefabs.
- `SWPopupShowEffect`, `SWPopupHideEffect`: Abstract classes for show and hide effects.
- `SWPopupScaleShowEffect`, `SWPopupScaleHideEffect`: Default coroutine-based scale effects.
- `SWPopupLifecycle`: Connects popup lifecycle events.
- `SWPopupEffectHandle`: Controls an active popup effect.

Example:

```csharp
using SW.Popup;
using SW.Util;
using UnityEngine;

public class PopupExample : MonoBehaviour
{
    [SerializeField] private SWPopupBase optionPopupPrefab;

    public void OpenOption()
    {
        SWPopupManager.Instance.Show(optionPopupPrefab);
    }
}
```

Setup:

1. Add `SWPopupManager` to the bootstrap scene.
2. Create popup prefabs derived from `SWPopupBase`.
3. Assign optional show and hide effect assets on each popup.
4. Use prefab-based calls directly, or create an `SWPopupCatalog` and register string keys for key-based calls.

Key-based example:

```csharp
using SW.Popup;
using SW.Util;

SWPopupManager.Instance.Register("option", optionPopupPrefab);
SWPopupManager.Instance.Show("option");
SWPopupManager.Instance.Hide("option");
```

<a id="runtime-resolution"></a>

### Resolution

Provides resolution, safe area, and CanvasScaler adjustments.

- `SWSafeArea`: Automatically adjusts RectTransform anchors for notches, Dynamic Islands, and punch-hole areas.
- `SWCanvasResolution`: Adjusts CanvasScaler `matchWidthOrHeight` according to the screen aspect ratio.

Usage:

1. Add `SWSafeArea` to the user interface object that requires safe-area handling.
2. Add `SWCanvasResolution` to a Canvas that requires CanvasScaler adjustment.
3. Configure the directions and ratios in the Inspector.

<a id="runtime-stats"></a>

### Stats

`SWStat` combines a base value and bonuses, clamped to a minimum and maximum. `SWStats` prepares per-object runtime stats, `SWStatOverride` configures base-value overrides, and `SWStatScaleFloat` derives scaled values. Track bonuses by source and subkey so equipment or effects can remove only their own contribution.

Value-change and minimum/maximum events notify consumers of the final value. `SetRange(minimumValue, maximumValue)` validates both bounds together and also notifies consumers when a range change affects the final value.

<a id="runtime-quests"></a>

### Quests and achievements

`SWQuestTaskGroup` combines tasks that run together and sequences them with later groups. `SWQuestTarget` matches reports against a string or Unity object target.

Provides a data-driven quest and achievement runtime built on `SWIdentifiedObject`, `SWSingleton`, encrypted `SWPlayerPrefs`, and `SWEventBus`.

> [!WARNING]
> The quest and achievement system is experimental. Its complete design review and real-project validation are not finished, so its save-data format and public API may change. Validate it thoroughly before using it in production.

- `SWQuest` combines sequential task groups, acceptance and cancellation conditions, and rewards.
- `SWAchievement` is always saved, cannot be canceled, and completes automatically.
- `SWQuestTask` filters reports by `SWCategory` and optional string or Unity object targets.
- `SWQuestTaskAction` provides replace, additive, positive-only, negative-only, and continuous progress strategies. A task with no action uses additive progress.
- `SWQuestCondition` and `SWQuestReward` are extension points for project rules and reward services.
- `SWQuestDatabase` and `SWAchievementDatabase` separately store, query, collect, and validate quest and achievement definitions.
- `SWQuestSystem` owns isolated runtime clones, duplicate prevention, reports, completion, cancellation, automatic achievement registration, and save restoration.
- `SWQuestSystemWindow` creates, duplicates, deletes, searches, and edits related assets, and synchronizes and validates both databases.
- `ISWQuestSaveStore` allows projects to replace persistence; the default implementation uses encrypted `SWPlayerPrefs`.
- `SWQuestGiver` and `SWQuestReporter` connect scene interactions to the runtime without coupling presentation code to quest internals.

Open `SWTools > Utils > Data > Quest System Editor` to create and manage the related assets. Create a quest database and an achievement database, synchronize each one with the project definitions, validate them, and assign both to a bootstrap-scene `SWQuestSystem`. Give every task group a code name that is unique within its quest, then report gameplay progress:

```csharp
using SW.Quest;

SWQuestSystem questSystem = SWQuestSystem.Instance;
questSystem.Initialize(questDatabase, achievementDatabase);
SWQuest runtimeQuest = questSystem.Register(questDefinition);
questSystem.ReceiveReport(killCategory, slimeTarget.Value, 1);

if (runtimeQuest != null && runtimeQuest.IsWaitingForCompletion)
{
    runtimeQuest.Complete();
}
```

`Save()` and `Load()` use encrypted `SWPlayerPrefs`. Disable automatic loading and call `SetSaveStore(ISWQuestSaveStore)` before initialization to use another store. To include quest state in a larger save root, store the `SWQuestSystemSaveData` returned by `CreateSaveData()` and pass it back to `RestoreSaveData()`. Task groups and tasks restore by code name, so their ordering may change without assigning progress to the wrong definition. Restoring completed entries never grants their rewards again. Project conditions and rewards can receive external services through `SetContext` and `TryGetContext<TContext>`. See `Samples/Scripts/SWQuestExample.cs` and `SWQuestScoreRewardExample.cs`.

Completion is confirmed after all rewards succeed. If gold is granted but an item reward fails, the quest remains `WaitingForCompletion`. A later `Complete()` skips recorded rewards and retries the failed item. A partially rewarded quest cannot be canceled.

A reward's `Grant` implementation must throw before changing data when it cannot pay. Reward events are notifications after payment; grant currency in `Grant`. Persist wallet or inventory data together with quest reward records to avoid duplicates or omissions across saves.

<a id="runtime-states"></a>

### State machines

Provides a general-purpose finite state machine that supports independent layers without depending on a Unity component.

- `SWStateMachine<TContext>`: Owns states, transitions, layers, commands, and messages.
- `SWState<TContext>`: Defines initialization, enter, tick, exit, and message callbacks.
- `SWMonoStateMachine<TContext>`: Drives a state machine from regular, physics, or manual Unity updates.
- `SWStackStateMachine<TContext>`: Pushes and removes states while preserving the states below them.
- `SWStackState<TContext>`: Defines enter, pause, resume, tick, exit, and message callbacks for stack states.
- `SWMonoStackStateMachine<TContext>`: Drives a stack state machine from the Unity update lifecycle.

Each layer owns one current state and runs independently in ascending layer order. Any-state transitions are evaluated before transitions from the current state. Within the same priority, transitions use registration order.

```csharp
using SW.StateMachine;

SWStateMachine<Player> stateMachine = new SWStateMachine<Player>(player);
stateMachine.AddState<IdleState>(0);
stateMachine.AddState<MovingState>(0);
stateMachine.SetInitialState<IdleState>(0);

stateMachine.AddTransition<IdleState, MovingState>(
    state => state.Context.IsMoving,
    0);
stateMachine.AddAnyTransition<IdleState>(PlayerStateCommand.ReturnToIdle, layer: 0);

stateMachine.Start();
stateMachine.Tick(deltaTime);
```

See `Samples/Example/SWGraphAssetsExample.cs` for the consolidated graph-based layered state example.

Only the top stack state receives ticks. Pushing a state pauses the previous top state, and popping it resumes the state below without re-entering it.

```csharp
SWStackStateMachine<GameFlow> stackStateMachine =
    new SWStackStateMachine<GameFlow>(gameFlow);

stackStateMachine.AddState<GameplayState>();
stackStateMachine.AddState<PauseState>();
stackStateMachine.AddState<SettingsState>();
stackStateMachine.Start<GameplayState>();

stackStateMachine.Push<PauseState>();
stackStateMachine.Push<SettingsState>();
stackStateMachine.Pop();
```

The same `SWGraphAssetsExample` file also contains graph-compatible stack states and runtime control.

#### State Machine Graph Editor

`SWStateMachineGraphAsset` stores the state nodes, layers and transitions used by the runtime graph factory.

The Unity 6-only graph editor stores state nodes and connections in a `ScriptableObject` asset. Its Shader Graph-inspired layout uses a full-window canvas with a collapsible Graph List, floating Blackboard and Graph Inspector panels, and a bottom validation console.

1. Create an asset from `Assets > Create > SWTools > State Machine Graph`.
2. Double-click the asset or select `Edit State Machine Graph` in its Inspector.
3. You can also open `SWTools > Utils > State Machine > Graph Editor`.
4. Choose `Layered` or `Stack` from `Graph Type` in the Blackboard.
5. Right-click empty canvas space and choose `Create Node...`. The toolbar action and `Space` key open the same searchable finder.
6. Select an implementation of `SWState<TContext>` or `SWStackState<TContext>` from States. Flow Control contains Any State and Return nodes. Nodes are created at the graph position where the finder was opened.
7. Drag from a node's `Out` connector to another node's `In` connector. The edge badge shows its operation, command, and priority directly on the canvas.
8. Select a node to edit its name, initial-state flag, and layer. Select an edge to edit its operation, command, condition, reentry, and priority.
9. Press `Delete` to remove selected elements. The searchable States and Transitions lists in the Blackboard support selection, creation, deletion, and independent folding.
10. Check the bottom status bar or select `Validate`, then save the asset.

Keyboard shortcuts are `Ctrl+C` to copy, `Ctrl+V` to paste, `Ctrl+D` to duplicate, `A` to frame all, `O` to frame the origin, and `Space` to open node search. Connections and transition settings between copied nodes are preserved and can be pasted into another asset with the same Graph Type.

`Auto Layout` arranges nodes by layer and initial-state priority, while the canvas position and zoom are restored per graph asset. `New Script` creates Layered State, Stack State, and Transition Condition scripts from editable templates. Shared options are also available under `Project Settings > SWUtils > State Machine Graph`.

Layered graphs connect states in the same layer and support an Any State node. Stack graphs support push and replace connections, while a connection to the Return node represents popping the current state and resuming the covered state.

Regular states use green cards, Any State uses violet, and Return State uses cyan. The Graph List collapses to the left, while the Blackboard lists and focuses States and Transitions and Graph Validation expands into a console-like issue list. Blackboard and Graph Inspector can be resized independently from their lower corners. Transition summaries continue to follow their nodes; hold `Alt` and drag a summary to save a custom offset in the graph asset. Return State is an input-only Pop target in Stack graphs. The Settings tab configures transition summaries, node and panel sizes, grid snapping, and grid spacing. Editor layout preferences are saved per Unity Editor user.

State machines created by the graph factory register automatically with the Play Mode debugger. Select the context GameObject to inspect active states, active duration, and recent transition history in the Runtime tab. Active nodes display a yellow LIVE badge and the latest transition is highlighted.

Create runtime state machines directly from graph assets:

```csharp
SWStateMachine<Player> stateMachine =
    SWStateMachineGraphFactory.CreateLayered(graphAsset, player);

SWStackStateMachineGraphController<GameFlow> stackController =
    SWStateMachineGraphFactory.CreateStack(graphAsset, gameFlow);
```

Implement `SWStateMachineGraphCondition<TContext>` for a project condition and select it from `Select Condition Type` in the edge details panel.

```csharp
public sealed class IsMovingCondition : SWStateMachineGraphCondition<Player>
{
    public override bool Evaluate(Player context)
    {
        return context.IsMoving;
    }
}
```

The layered graph factory returns a configured and started `SWStateMachine<TContext>`. The stack graph controller provides `Tick`, `ExecuteCommand`, `SendMessage`, and `Stop`, and executes graph-authored push, replace, and pop connections.

Transitions requested from transition callbacks are queued until the current transition finishes, then processed in order. A `true` result from a nested `Pop` or `ExecuteCommand` means the request was accepted. Observe the state-change notification for the resulting state. More than 1,024 consecutive operations in one call raises an error and clears pending requests.

<a id="runtime-behaviour"></a>

### Behaviour Tree

`SWBehaviourActionNode`, `SWBehaviourCompositeNode` and `SWBehaviourDecoratorNode` are the three node base types. Nodes receive an `SWBehaviourContext` and return an `SWBehaviourStatus`. `SWBehaviourBlackboard` holds shared values, `SWBehaviourNodeProperty<T>` connects fields to those values or constants, and `SWBehaviourSubTreeNode` runs a referenced tree.

`SWBehaviourTreeAsset` in the `SW.BehaviourTree` namespace provides a Behaviour Tree runtime based on `Running`, `Success`, `Failure`, and `Aborted`. It includes Composite, Decorator, Action, SubTree, typed Blackboard, NodeProperty, and per-Runner override support. Project-defined node and custom Blackboard entry types appear automatically in the editor.

Create an asset from `Assets > Create > SWTools > Behaviour Tree`, then open `SWTools > Utils > Behaviour > Tree Editor`. Connect a parent's `Out` port to a child's `In` port. Composite nodes accept multiple children, Decorators accept one, and Actions accept none. Children execute from left to right and are reordered automatically when moved. The floating Blackboard edits shared typed values, while Node Inspector edits descriptions and node-specific fields. Add `SWBehaviourTreeRunner` to execute an isolated runtime clone. In Play Mode, select its GameObject to see Running, Success, and Failure colors in the graph.

The graph supports copy, paste, duplicate, SubTree selection, keyboard navigation, automatic layout, quick asset switching, node script generation, per-asset view persistence, and colored runtime paths. Configure node and panel layout under `Project Settings > SWUtils > Behaviour Tree`.

Both graph editors provide a collapsible shared Graph List and Runtime Debug. Use slash-delimited attributes such as `SWStateMachineNodeCategory("Combat/Movement")` and `SWBehaviourNodeCategory("Combat/Actions")` to organize custom states, conditions, and Behaviour nodes in creation menus.

Generic Set Property and Compare Property nodes support built-in and custom Blackboard values. `SWBehaviourTreeRunner` exposes external get, set, and cached key APIs, and custom keys can be overridden per Runner. Node scripts are generated from editable text templates under `Editor/Behaviour/Templates`.

Reference values in the blackboard can be cleared with `null`. Renaming a key invalidates its lookup cache. `Rename(identifier, newName)` validates empty and duplicate names; update string-based references in nodes separately.

Subtree cycles and nesting beyond 64 levels are rejected before runtime cloning. The inspector displays the problem and `CreateRuntimeInstance` returns `null`. Call `ValidateSubTrees(out error)` to inspect the definition from code.

<a id="runtime-utilities"></a>

### Utilities

A collection of small, general-purpose game utilities.

- `SWAudioLibrary`, `SWAudioManager`: Manage key-based music and sound-effect playback.
- `SWCooldown`: Calculates cooldown progress, remaining time, and availability.
- `SWEventBus`: Handles type-based event subscription, publication, and unsubscription.
- `SWEventBusEventSnapshot`: A read-only event state snapshot for `SWTools/Debug/Event/EventBus Debugger Window`.
- `SWSceneLoader`: Handles scene loading, additive loading, unloading, and reloading.
- `SWSingleton`, `SWSingletonScene`: Singleton MonoBehaviour base classes.
- `SWTimer`, `SWRefillTimer`: Provide elapsed-time and refill timer behavior.
- `SWExtension`: Extension methods for Transform, GameObject, and other types.
- `SWFactory`: Assists with runtime object creation.
- `SWLog`: A logging wrapper.
- `SWResolution`: Assists with resolution calculations.
- `SWString`: Provides string helpers such as extracting numbers.
- `SWTime`: Provides time formatting and calculation helpers.
- `SWTriggerDispatcher`: Delegates trigger events.
- `SWUtility`: Provides shared helpers such as user interface gauge updates.
- `SWVibration`: Invokes vibration on Android and iOS.
- `SWAmountFormat`: Formats large numbers with suffixes such as K, M, B, and T.
- `SWAmountFormatProfile`: Stores number suffixes, decimal places, and decimal handling in a Resources preset asset.
- `SWRectDummy`: A mesh-free Graphic that creates a rectangular user interface raycast area without an Image.
- `SWButtonExtension`: Configures cooldowns, long presses, held-button repetition, submit input and click sounds.
- `SWRandom`, `SWShuffleBag<T>`: Provide weighted selection, shuffling and draws without repetition until the current bag is exhausted.

Configure `SWButtonExtension` on the button and use its events for held or repeated actions. Keyboard and gamepad submit input is handled independently of previous pointer holds. Compare random-selection settings in `SWTools > Utils > Simulation > Random Simulator`.

#### Audio

1. Create an audio library from `Assets > Create > SWUtils > Audio Library`.
2. Register music and sound-effect clips with unique string keys.
3. Add `SWAudioManager` to the bootstrap scene and assign the library.
4. Optionally assign dedicated music and sound-effect `AudioSource` components. Missing sources are created automatically.

```csharp
using SW.Util;
using UnityEngine;

public class AudioExample : MonoBehaviour
{
    public void PlayLobbyMusic()
    {
        SWAudioManager.Instance.PlayMusic("lobby", true, 0.5f);
    }

    public void PlayButtonSound()
    {
        SWAudioManager.Instance.PlaySfx("button");
    }

    public void ApplyVolume(float volume)
    {
        SWAudioManager.Instance.SetMasterVolume(volume);
        SWAudioManager.Instance.SaveVolumes();
    }
}
```

Call `LoadVolumes` during initialization if volume settings were saved previously.

Event bus example:

```csharp
using SW.Util;

public readonly struct CoinChangedEvent
{
    public readonly int Coin;

    public CoinChangedEvent(int coin)
    {
        Coin = coin;
    }
}

SWEventBus.Subscribe<CoinChangedEvent>(OnCoinChanged);
SWEventBus.Publish(new CoinChangedEvent(100));
SWEventBus.Publish(new CoinChangedEvent(200), false); // Suppresses the publish log.
SWEventBus.Unsubscribe<CoinChangedEvent>(OnCoinChanged);

SWEventBus.IsLogOutputEnabled = false; // Suppresses all event bus logs.
```

Cooldown example:

```csharp
using SW.Util;

private readonly SWCooldown skillCooldown = new(3f);

private void TryUseSkill()
{
    if (!skillCooldown.TryUse()) return;

    // Execute the skill.
}
```

Timer example:

```csharp
using SW.Util;
using UnityEngine;

public class RoundTimerExample : MonoBehaviour
{
    private readonly SWTimer roundTimer = new SWTimer(60f);

    private void Start()
    {
        roundTimer.Start();
    }

    private void Update()
    {
        if (roundTimer.Tick())
            Debug.Log("Round complete");
    }
}
```

`SWTimer.Tick` must be called by an update owner. Use `Pause`, `Resume`, `Restart`, and `SetDuration` to control it. `SWRefillTimer` is intended for count recovery across sessions; construct it with a stable save key, call `Use` when spending a count, and call `RecoverOffline` after loading.

Scene-loading example:

```csharp
using SW.Util;
using UnityEngine;

public class SceneTransitionExample : MonoBehaviour
{
    public void LoadLobby()
    {
        SWSceneLoader.Instance.LoadScene(
            "Lobby",
            onProgress: progress => Debug.Log($"Loading: {progress:P0}"),
            onComplete: () => Debug.Log("Lobby loaded"));
    }
}
```

Add all target scenes to Build Settings before loading them. `LoadAdditive`, `UnloadScene`, `ReloadActiveScene`, and `SetActiveScene` cover multi-scene flows. Set `AllowSceneActivation` when a loading screen must hold activation after loading reaches the ready state.

`TryCancelCurrentLoad()` accepts cancellation before the engine operation starts and returns `false` afterward. Set `AllowSceneActivation = true` to release a held load. Completion callbacks run after the loader resets its request state.

Number format preset example:

```csharp
using SW.Util;
using TMPro;
using UnityEngine;

public class GoldTextExample : MonoBehaviour
{
    [SerializeField] private SWAmountFormatProfile amountFormatProfile;
    [SerializeField] private TMP_Text goldText;

    public void SetGold(long goldAmount)
    {
        SWAmountFormatProfile profile = amountFormatProfile != null
            ? amountFormatProfile
            : SWAmountFormatProfile.LoadDefault();

        goldText.text = profile.Format(goldAmount);
    }
}
```

Create and edit the default number format preset from `SWTools/Utils/Data/Amount Format Window`. The settings asset is created at `Assets/Resources/SWAmountFormatProfile.asset` and can be loaded through Resources at runtime.

Rect Dummy usage:

1. Add `SWRectDummy` to a user interface object that only needs a clickable area.
2. Enable Raycast Target on `SWRectDummy` to use it as an input area without an Image component.
3. Use the `Fit Parent` Inspector button or context menu to match the parent RectTransform.
4. Select `GameObject > UI > SW Rect Dummy` to create one from the menu.

Volume changes also update currently playing sound effects while preserving each playback's volume scale. `SWEventBus.IsDiagnosticsEnabled` controls snapshot recording separately from `IsLogOutputEnabled`; failures in diagnostic string conversion do not interrupt publishing. Zero-length timers and timers shortened below elapsed time complete on the next `Tick`.

`SWUtility.SetGaugeText` displays the current and maximum values as text. The existing `SetGauge` method keeps this behavior; its image argument is retained for compatibility.

<a id="editor-tools"></a>

## Editor Features

### Inspector drawers

A collection of PropertyDrawers that render the Inspector features defined in `Runtime/Attribute`.

### Editor windows

Editor windows available from the `SWTools` menu. Debugging tools are under `SWTools/Debug`, while general utilities are under `SWTools/Utils`.

- `SWTools/Debug/Build/Build Report Viewer`: Inspects build reports and included asset sizes.
- `SWTools/Debug/Console/Debug Console Settings`: Configures the runtime debug console, performance overlay, debug define symbol, and play-mode controls.
- `SWTools/Debug/Event/EventBus Debugger Window`: Inspects registered `SWEventBus` event types, listener counts, publication counts, and the latest published data.
- `SWTools/Debug/Input/Input Debugger Window`: Inspects EventSystem, pointer, raycast, and input states.
- `SWTools/Debug/PlayerPrefs/PlayerPrefs Viewer`: Views, edits, and deletes SWUtils PlayerPrefs and standard Unity PlayerPrefs data in separate tabs.
- `SWTools/Debug/Pool/Pool Monitor Window`: Inspects created, active, inactive, spawned, returned, and delayed-return counts for each `SWPool` prefab.
- `SWTools/Debug/Test/Test Tools Window`: Assists with play-mode testing and scene navigation.
- `SWTools/Utils/Asset/Quick Asset Palette`: Provides quick access to frequently used assets.
- `SWTools/Utils/Asset/Reference Finder`: Finds project references to the selected asset.
- `SWTools/Utils/Asset/TMP Font Asset Manager`: Manages TextMeshPro font asset assignment and performance inspection.
- `SWTools/Utils/Data/Amount Format Window`: Creates and edits number format presets.
- `SWTools/Utils/Data/Excel Table Importer`: Applies tabular text to ScriptableObject data.
- `SWTools/Utils/Behaviour/Tree Editor`: Authors behaviour trees, blackboard keys and subtrees.
- `SWTools/Utils/Data/Quest System Editor`: Manages quests, achievements, rewards, conditions and their databases.
- `SWTools/Utils/Data/Localization Tools`: Assists with Localization table workflows.
- `SWTools/Utils/Data/Skill Tree Editor`: Edits skill nodes, prerequisite connections, reveal rules, and shared layout coordinates.
- `SWTools/Utils/Data/Stat System Editor`: Creates, edits, sorts, renames, previews icons, and adjusts list display sizes for `SWIdentifiedObject` assets such as categories and stats.
- `SWTools/Utils/Hierarchy/Hierarchy Tools`: Configures Hierarchy object colors, icons, and styles.
- `SWTools/Utils/Project/Define Symbol Window`: Manages Scripting Define Symbols.
- `SWTools/Utils/Project/PlayerPrefs Salt Settings`: Creates and edits the SWPlayerPrefs encryption salt asset.
- `SWTools/Utils/Screen/Resolution Window`: Displays resolution test values.
- `SWTools/Utils/Simulation/Random Simulator`: Simulates weighted and shuffled random selection.
- `SWTools/Utils/State Machine/Graph Editor`: Creates and edits layered and stack state machine graph assets.

#### `SWTools/Debug/Console/Debug Console Settings`

Uses focused tabs to keep debug console configuration compact:

- `상태`: Connects or creates the Resources settings asset and adds or removes `SW_DEBUG_MODE` for the active build target.
- `입력`: Sets auto creation, open key, optional modifier keys, touch count, and optional Input System checking.
- `오버레이`: Sets startup visibility, anchor, scale, refresh interval, shown metrics, and FPS threshold colors.
- `플레이`: Opens, closes, toggles the overlay, and resets overlay statistics while the Editor is in play mode.

#### `SWTools/Debug/Event/EventBus Debugger Window`

Displays the current `SWEventBus` state in play mode or edit mode.

Displayed information:

- Event type name and full name
- Current registered listener count
- Event publication count
- Latest publication time
- Summary of the latest published data

Use `Clear Publish History` to reset publication counts and latest publication records without removing listeners.

#### `SWTools/Debug/Pool/Pool Monitor Window`

Displays the state of each prefab managed by `SWPool`.

Displayed information:

- Registered prefab
- Pool and group names
- Created, active, and inactive counts
- Spawn, return, and destruction counts
- Scheduled delayed-return count

The window uses the `SWPool` in the scene. Registered prefabs whose underlying ObjectPool has not been created yet are also shown with zero counts.

#### `SWTools/Debug/PlayerPrefs/PlayerPrefs Viewer`

Use the `SWUtils PlayerPrefs` tab to inspect decrypted logical keys for the selected slot. Use the `Unity PlayerPrefs` tab to inspect ordinary Unity PlayerPrefs entries that are not internal SWUtils storage keys.

Values can be edited and saved from the entries list. Deleting all Unity PlayerPrefs also deletes the encrypted backend used by SWUtils, so reserve that action for development and testing.

#### `SWTools/Utils/Asset/Reference Finder`

Select an asset and open `Assets > SWTools > Find References In Project`, or open the window from `SWTools > Utils > Asset > Reference Finder`. The search scans project assets for references to the selected object and lets you select or ping each result.

#### `SWTools/Utils/Asset/TMP Font Asset Manager` Performance Tab

Inspects atlas memory, glyphs, characters, fallback chains, and material preset costs for a TextMeshPro font asset.

Usage:

1. Open `SWTools > Utils > Asset > TMP Font Asset Manager` from the Unity menu.
2. Select the `Performance` tab.
3. Drag a `TMP_FontAsset` into the window or assign it through the Object Field.
4. Select `Use Selected Asset` to inspect the selected font asset or the font used by a selected TextMeshPro object.
5. Select `Use Default Font` to inspect the default font assigned in the Quick Swap tab.
6. Enable `Include Fallback Chain` to include the entire fallback font chain.

Displayed information:

- Atlas texture count, total pixel area, runtime texture memory, stored texture memory, and estimated RGBA32 memory
- Glyph and character counts
- Direct and total fallback font counts, plus fallback depth
- Dynamic atlas usage
- TextMeshPro material preset count in the same folder

The inspection warns about large atlas memory, high glyph counts, long fallback chains, dynamic atlases, and numerous material presets that may require attention on mobile targets.

### Table importer

Parses tabular text and applies it to ScriptableObject fields marked with `SWTable` or `SWTableSheet`.
`SWTableSheet` supports `List<T>`, arrays, and ordinary class fields. Collections receive every
data row, while an ordinary class field receives only the first data row.
For an ordinary class field, the importer also provides a vertical layout where each row contains
a field name and value, such as `InitCoinValue    1000`. Select the input layout in the editor window.

Usage:

1. Derive the target asset from `SWScriptableObject` or another ScriptableObject type.
2. Apply `SWTable` or `SWTableSheet` to the destination field.
3. Open `SWTools > Utils > Data > Excel Table Importer`.
4. Assign the target asset and paste tab-separated spreadsheet data.
5. Select the table layout that matches the pasted data.
6. Preview parsing errors, then apply the imported values and save the asset.

Collection fields receive every data row. An ordinary class field receives the first row in horizontal layout, or matching field-and-value rows in vertical layout. Field names must match serialized field names.

Missing required columns, duplicate headers, unknown boolean values and unclosed quotes are rejected before application. Quoted cells preserve tabs, newlines and quotes.

### Localization tools

Open `SWTools > Utils > Data > Localization Tools` to work with string-table collections. Export selected locales as CSV, TSV or JSON, and preview TSV before importing it. Configure new-collection creation or existing-collection updates, key prefixes, Smart String usage and empty-entry export.

Updating an existing collection removes keys absent from the incoming table. Enabling **모든 기존 키 삭제 후 교체** clears all existing keys before rebuilding them from the input. Review the preview and target collection before applying the import.

### Hierarchy tools

Stores and applies Hierarchy display styles and icons. Used with `SWHierarchyToolsWindow`.

### Component inspectors

Builds custom Inspectors for components derived from `SWMonoBehaviour`, including groups, buttons, conditional display, and constant repaint behavior. The shared Inspector implementation is also used by `SWScriptableObject`.

### Asset inspectors

Applies the shared SWUtils custom Inspector to assets derived from `SWScriptableObject`.

### Editor stylesheets

Stylesheets used by editor UI Toolkit views.

### Shared editor utilities

Shared editor-window and custom-Inspector utilities for graphical user interfaces, drag and drop, icons, EditorPrefs, style caching, selection, and pinging assets.

## Samples

Provides sample prefabs and example scripts.

- `Samples/Example/SWAttributeExample.cs`: Attribute examples.
- `Samples/Example/SWSubClassSelectorExample.cs`: Examples for `SWSubClassSelector`, `SWAddTypeMenu`, and `SWHideInTypeMenu`.
- `Samples/Example/SWGraphAssetsExample.cs`: Consolidated Behaviour Tree, layered state machine, stack state machine, and custom node-category example.
- `Samples/Scripts/SWQuestExample.cs`, `SWQuestScoreRewardExample.cs`: Quest initialization, progress reporting, completion and achievement events, and a project reward example.
- `Samples/Prefab/SWSkillTreeExample.prefab`, `SWSkillTreeNode.prefab`, and `Samples/Data/SkillTree/MiningSkillTree.asset`: An 81-node example with navigation, purchases, refunds, persistence, and layout editing.
- `Samples/Example/SWExampleBehaviourTree.asset`: Ready-to-run Behaviour Tree graph.
- `Samples/Example/SWExampleStateMachine.asset`: Ready-to-run layered State Machine graph.
- `Samples/Example/SWExampleStackStateMachine.asset`: Ready-to-run Stack State Machine graph using Gameplay, Pause, and Return State.
- `Samples/Prefab/AtrributeExample.prefab`: Attribute example prefab.
- `Samples/Prefab/SWPool.prefab`: Pool manager prefab.
- `Samples/Prefab/SWPoolRegistry.prefab`: Pool registry prefab.

After installation, inspect the examples directly in `Packages > SWUtils > Samples` in the Project window.

## Assembly Definitions

- `SWUtils.Runtime`: Runtime code assembly.
- `SWUtils.Editor`: Editor code assembly.
- `SWUtils.Samples`: Sample code assembly.
- `SWUtils.SkillTree.Samples.Editor`: Editor-only skill tree sample generation.
- `SWUtils.SkillTree.Tests`: Edit Mode skill tree and reliability tests.

Sample prefabs are serialized using the assembly name that contains each script.
