# SWUtils

SWUtils is a collection of commonly used runtime features and editor tools for Unity projects.

## Install from a Git URL

Add the package through Unity Package Manager:

1. Open `Window > Package Manager` from the Unity menu.
2. Select the `+` button in the upper-left corner.
3. Select `Add package from git URL...`.
4. Enter the Git URL for this repository.

Append `#branch-name` or `#tag-name` to the URL to install a specific branch or tag.

```text
https://github.com/LAVINSE/SWUtils.git#v1.0.9
```

## Dependencies

The following Unity packages are installed automatically through `package.json`:

- Input System
- Localization
- TextMeshPro
- Unity UI

The following external libraries may not be available directly through Unity Package Manager. Install them before using the related features:

- DOTween: Used for popup show and hide animations.
- Google Play Games: Used for Android cloud saves.
- Steamworks.NET: Used for standalone cloud saves.

## Optional Define Symbols

Add the following define symbols when using external libraries for cloud saves:

- `SW_GOOGLEPLAY_ENABLE`: Enables Google Play Games saves on Android.
- `SW_STEAMWORKS_NET`: Enables Steamworks.NET saves on standalone platforms.

## Runtime Features

### `Runtime/Attribute`

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
using SWTools;
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
using SWTools;
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

### `Runtime/Coroutine`

Separates coroutine execution behind an interface so runtime code does not depend tightly on a specific MonoBehaviour.

- `ICoroutineRunner`: Defines operations for coroutines, delayed actions, and simple interpolation.
- `SWCoroutineRunner`: Executes the coroutines.

Example:

```csharp
using System.Collections;
using SWCoroutine;
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

### `Runtime/Data`

Provides save data, PlayerPrefs, encryption, and cloud save features.

- `SWUtilsPlayerPrefs`: A PlayerPrefs wrapper with slot selection, encryption, and JSON import and export.
- `SWUtilsPlayerPrefsSettings`: A settings asset that stores project-specific salt and initialization-vector salt values.
- `SWSaveDataManager`: Saves and loads multiple data types in a file.
- `SWUtilsCloud`: A unified entry point for Google Play Games, iCloud, Steamworks.NET, and local fallback saves.
- `SWEncrypt<T>`: Stores `int`, `long`, `float`, `double`, `bool`, and `string` values as encrypted PlayerPrefs data.
- `SWSaveSlot`: Provides default save-slot constants.

Example:

```csharp
using SWUtils;

SWUtilsPlayerPrefs.SetSlot("player_01");
SWUtilsPlayerPrefs.SetInt("coin", 100);
SWUtilsPlayerPrefs.Save();

int coin = SWUtilsPlayerPrefs.GetInt("coin", 0);
```

Create and edit salt settings from `SWTools/Utils/PlayerPrefs Salt Settings`. The settings asset is created at `Assets/Resources/SWUtilsPlayerPrefsSettings.asset` and loaded automatically through Resources at runtime. Data saved with a previous salt cannot be read after the salt changes, so delete or migrate existing data first.

Save manager example:

```csharp
using System;
using SWUtils;

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

### `Runtime/MonoBehaviour`

Provides the `SWMonoBehaviour` base class for use with the attribute-driven `SWTools` custom Inspector.

Example:

```csharp
using SWTools;
using UnityEngine;

public class PlayerController : SWMonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
}
```

### `Runtime/ScriptableObject`

Provides the `SWScriptableObject` base class for ScriptableObject assets that use the same attribute-driven custom Inspector as `SWMonoBehaviour`.

Fields and methods can use Inspector features such as `SWGroup`, `SWButton`, `SWCondition`, `SWReadOnly`, and the constant repaint attributes.

Example:

```csharp
using SWTools;
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

### `Runtime/Pooling`

Provides GameObject pooling and group-based spawning.

- `SWPool`: A singleton-based object pool.
- `IPool`: Defines the pooling contract.
- `IPoolable`: Defines callbacks invoked when an object is spawned from or returned to a pool.
- `SWPoolCatalog`: A ScriptableObject containing pool registrations.
- `SWPoolRegistry`: Registers pools and groups when a scene starts.
- `SWPoolGroupSelectionMode`: Defines how an object is selected from a group.
- `SWPoolSnapshot`: A read-only pool state snapshot for `SWTools/Debug/Pool Monitor Window`.

Example:

```csharp
using SWPooling;
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

Pool callback example:

```csharp
using SWPooling;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    private IPool pool;

    public void SetPool(IPool pool)
    {
        this.pool = pool;
    }

    public void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
    }

    public void OnReturnToPool()
    {
        gameObject.SetActive(false);
    }
}
```

### `Runtime/Popup`

Manages popup creation, display, hiding, caching, and animation.

- `SWPopupBase`: The base component for all popups.
- `SWPopupManager`: Handles popup display, hiding, key-based registration, and caching.
- `SWPopupCatalog`: A ScriptableObject that maps keys to popup prefabs.
- `SWPopupShowEffect`, `SWPopupHideEffect`: Abstract classes for show and hide effects.
- `SWPopupScaleShowEffect`, `SWPopupScaleHideEffect`: Default DOTween-based scale effects.
- `SWPopupLifecycle`: Connects popup lifecycle events.

Example:

```csharp
using SWUtils;
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

Key-based example:

```csharp
using SWUtils;

SWPopupManager.Instance.Register("option", optionPopupPrefab);
SWPopupManager.Instance.Show("option");
SWPopupManager.Instance.Hide("option");
```

### `Runtime/Resolution`

Provides resolution, safe area, and CanvasScaler adjustments.

- `SWSafeArea`: Automatically adjusts RectTransform anchors for notches, Dynamic Islands, and punch-hole areas.
- `SWCanvasResolution`: Adjusts CanvasScaler `matchWidthOrHeight` according to the screen aspect ratio.

Usage:

1. Add `SWSafeArea` to the user interface object that requires safe-area handling.
2. Add `SWCanvasResolution` to a Canvas that requires CanvasScaler adjustment.
3. Configure the directions and ratios in the Inspector.

### `Runtime/Utils`

A collection of small, general-purpose game utilities.

- `SWAudioLibrary`, `SWAudioManager`: Manage key-based music and sound-effect playback.
- `SWCooldown`: Calculates cooldown progress, remaining time, and availability.
- `SWEventBus`: Handles type-based event subscription, publication, and unsubscription.
- `SWEventBusEventSnapshot`: A read-only event state snapshot for `SWTools/Debug/EventBus Debugger Window`.
- `SWSceneLoader`: Handles scene loading, additive loading, unloading, and reloading.
- `SWSingleton`, `SWSingletonScene`: Singleton MonoBehaviour base classes.
- `SWTimer`, `SWRefillTimer`: Provide elapsed-time and refill timer behavior.
- `SWUtilsExtension`: Extension methods for Transform, GameObject, and other types.
- `SWUtilsFactory`: Assists with runtime object creation.
- `SWUtilsLog`: A logging wrapper.
- `SWUtilsResolution`: Assists with resolution calculations.
- `SWUtilsString`: Provides string helpers such as extracting numbers.
- `SWUtilsTime`: Provides time formatting and calculation helpers.
- `SWUtilsTriggerDispatcher`: Delegates trigger events.
- `SWUtilsUtility`: Provides shared helpers such as user interface gauge updates.
- `SWVibration`: Invokes vibration on Android and iOS.
- `SWAmountFormat`: Formats large numbers with suffixes such as K, M, B, and T.
- `SWAmountFormatProfile`: Stores number suffixes, decimal places, and decimal handling in a Resources preset asset.
- `SWRectDummy`: A mesh-free Graphic that creates a rectangular user interface raycast area without an Image.

Event bus example:

```csharp
using SWUtils;

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
using SWUtils;

private readonly SWCooldown skillCooldown = new(3f);

private void TryUseSkill()
{
    if (!skillCooldown.TryUse()) return;

    // Execute the skill.
}
```

Number format preset example:

```csharp
using SWUtils;
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

Create and edit the default number format preset from `SWTools/Utils/Amount Format Window`. The settings asset is created at `Assets/Resources/SWAmountFormatProfile.asset` and can be loaded through Resources at runtime.

Rect Dummy usage:

1. Add `SWRectDummy` to a user interface object that only needs a clickable area.
2. Enable Raycast Target on `SWRectDummy` to use it as an input area without an Image component.
3. Use the `Fit Parent` Inspector button or context menu to match the parent RectTransform.
4. Select `GameObject > UI > SW Rect Dummy` to create one from the menu.

## Editor Features

### `Editor/Attribute`

A collection of PropertyDrawers that render the Inspector features defined in `Runtime/Attribute`.

### `Editor/EditorWindow`

Editor windows available from the `SWTools` menu. Debugging tools are under `SWTools/Debug`, while general utilities are under `SWTools/Utils`.

- `SWTools/Debug/Build Report Viewer`: Inspects build reports and included asset sizes.
- `SWTools/Debug/EventBus Debugger Window`: Inspects registered `SWEventBus` event types, listener counts, publication counts, and the latest published data.
- `SWTools/Debug/Input Debugger Window`: Inspects Input System devices and input states.
- `SWTools/Debug/PlayerPrefs Viewer`: Views, edits, and deletes SWUtils PlayerPrefs and standard Unity PlayerPrefs data in separate tabs.
- `SWTools/Debug/Pool Monitor Window`: Inspects created, active, inactive, spawned, returned, and delayed-return counts for each `SWPool` prefab.
- `SWTools/Debug/Test Tools Window`: Assists with play-mode testing and scene navigation.
- `SWTools/Utils/Define Symbol Window`: Manages Scripting Define Symbols.
- `SWTools/Utils/Excel Table Importer`: Applies tabular text to ScriptableObject data.
- `SWTools/Utils/Hierarchy Tools`: Configures Hierarchy object colors, icons, and styles.
- `SWTools/Utils/Localization Tools`: Assists with Localization table workflows.
- `SWTools/Utils/Amount Format Window`: Creates and edits number format presets.
- `SWTools/Utils/PlayerPrefs Salt Settings`: Creates and edits the SWUtilsPlayerPrefs encryption salt asset.
- `SWTools/Utils/Quick Asset Palette`: Provides quick access to frequently used assets.
- `SWTools/Utils/Reference Finder`: Finds project references to the selected asset.
- `SWTools/Utils/TMP Font Asset Manager`: Manages TextMeshPro font asset assignment and performance inspection.
- `SWTools/Utils/Resolution Window`: Displays resolution test values.

#### `SWTools/Debug/EventBus Debugger Window`

Displays the current `SWEventBus` state in play mode or edit mode.

Displayed information:

- Event type name and full name
- Current registered listener count
- Event publication count
- Latest publication time
- Summary of the latest published data

Use `Clear Publish History` to reset publication counts and latest publication records without removing listeners.

#### `SWTools/Debug/Pool Monitor Window`

Displays the state of each prefab managed by `SWPool`.

Displayed information:

- Registered prefab
- Pool and group names
- Created, active, and inactive counts
- Spawn, return, and destruction counts
- Scheduled delayed-return count

The window uses the `SWPool` in the scene. Registered prefabs whose underlying ObjectPool has not been created yet are also shown with zero counts.

#### `SWTools/Utils/TMP Font Asset Manager` Performance Tab

Inspects atlas memory, glyphs, characters, fallback chains, and material preset costs for a TextMeshPro font asset.

Usage:

1. Open `SWTools > Utils > TMP Font Asset Manager` from the Unity menu.
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

### `Editor/ExcelTable`

Parses tabular text and applies it to ScriptableObject fields marked with `SWTable` or `SWTableSheet`.
`SWTableSheet` supports `List<T>`, arrays, and ordinary class fields. Collections receive every
data row, while an ordinary class field receives only the first data row.
For an ordinary class field, the importer also provides a vertical layout where each row contains
a field name and value, such as `InitCoinValue    1000`. Select the input layout in the editor window.

### `Editor/HierarchyTools`

Stores and applies Hierarchy display styles and icons. Used with `SWHierarchyToolsWindow`.

### `Editor/Monobehaviour`

Builds custom Inspectors for components derived from `SWMonoBehaviour`, including groups, buttons, conditional display, and constant repaint behavior. The shared Inspector implementation is also used by `SWScriptableObject`.

### `Editor/ScriptableObject`

Applies the shared SWUtils custom Inspector to assets derived from `SWScriptableObject`.

### `Editor/StyleSheet`

Stylesheets used by editor UI Toolkit views.

### `Editor/Utils`

Shared editor-window and custom-Inspector utilities for graphical user interfaces, drag and drop, icons, EditorPrefs, style caching, selection, and pinging assets.

## Samples

Provides sample prefabs and example scripts.

- `Samples/Example/SWAttributeExample.cs`: Attribute examples.
- `Samples/Example/SWSubClassSelectorExample.cs`: Examples for `SWSubClassSelector`, `SWAddTypeMenu`, and `SWHideInTypeMenu`.
- `Samples/Prefab/AtrributeExample.prefab`: Attribute example prefab.
- `Samples/Prefab/SWPool.prefab`: Pool manager prefab.
- `Samples/Prefab/SWPoolRegistry.prefab`: Pool registry prefab.

After installing the package through Unity Package Manager, import the sample into your project to inspect the example prefabs.

## Assembly Definitions

- `SWUtils.Runtime`: Runtime code assembly.
- `SWUtils.Editor`: Editor code assembly.
- `SWUtils.Samples`: Sample code assembly.

Sample prefabs are serialized using the assembly name that contains each script.
