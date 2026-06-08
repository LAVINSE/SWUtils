# SWUtils

Unity 프로젝트에서 자주 사용하는 런타임 기능과 에디터 도구를 모아둔 패키지입니다.

## Git URL 설치

Unity Package Manager에서 다음 순서로 추가합니다.

1. Unity 상단 메뉴에서 `Window > Package Manager`를 엽니다.
2. 좌측 상단 `+` 버튼을 누릅니다.
3. `Add package from git URL...`을 선택합니다.
4. 이 저장소의 Git URL을 입력합니다.

브랜치 또는 태그를 고정해서 설치하려면 URL 뒤에 `#브랜치명` 또는 `#태그명`을 붙입니다.

```text
https://github.com/LAVINSE/SwUtils.git#v1.0.7
```

## 의존 패키지

`package.json`에서 다음 Unity 패키지를 자동으로 설치합니다.

- Input System
- Localization
- TextMeshPro
- Unity UI

다음 라이브러리는 Unity Package Manager에서 직접 설치할 수 없는 외부 라이브러리일 수 있으므로, 해당 기능을 사용하는 프로젝트에서 먼저 설치해야 합니다.

- DOTween: 팝업 표시 및 숨김 연출에서 사용합니다.
- Google Play Games: Android 클라우드 저장 기능에서 사용합니다.
- Steamworks.NET: Standalone 클라우드 저장 기능에서 사용합니다.

## 선택 정의 심볼

클라우드 저장 기능에서 외부 라이브러리를 사용할 때 다음 정의 심볼을 프로젝트에 추가합니다.

- `SW_GOOGLEPLAY_ENABLE`: Android에서 Google Play Games 저장 기능을 사용합니다.
- `SW_STEAMWORKS_NET`: Standalone에서 Steamworks.NET 저장 기능을 사용합니다.

## Runtime 폴더 기능

### `Runtime/Attribute`

인스펙터 표시를 확장하는 어트리뷰트 모음입니다.

- `SWButton`: 인스펙터에서 메서드를 버튼으로 실행합니다.
- `SWButtonBar`: 여러 메서드를 버튼 묶음으로 실행합니다.
- `SWCondition`: Boolean 필드 값에 따라 필드를 표시하거나 숨깁니다.
- `SWEnumCondition`: enum 값에 따라 필드를 표시하거나 숨깁니다.
- `SWDropdown`: 지정한 값 목록을 드롭다운으로 표시합니다.
- `SWGroup`: 인스펙터 필드를 그룹으로 묶습니다.
- `SWReadOnly`: 필드를 읽기 전용으로 표시합니다.
- `SWSubClassSelector`: `SerializeReference` 필드에서 추상 클래스 또는 인터페이스의 구현 타입을 검색 가능한 드롭다운으로 선택합니다.
- `SWAddTypeMenu`: `SWSubClassSelector` 드롭다운에 표시되는 타입 메뉴 경로를 지정합니다.
- `SWHideInTypeMenu`: `SWSubClassSelector` 드롭다운에서 특정 타입을 숨깁니다.
- `SWRequiresConstantRepaint`, `SWRequiresConstantRepaintOnlyWhenPlaying`: 커스텀 인스펙터 갱신 조건을 지정합니다.
- `SWTable`, `SWTableSheet`: 표 데이터를 ScriptableObject 필드와 연결할 때 사용합니다.

사용 예시:

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

    [SWButton("값 갱신")]
    private void RefreshValue()
    {
        currentLevel++;
    }
}
```

`SWSubClassSelector` 사용 예시:

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

`SkillAction` 같은 추상 클래스 또는 인터페이스를 기준 타입으로 두면 Inspector에서 `HealSkillAction` 같은 직렬화 가능한 구현 타입이 드롭다운에 표시됩니다. `SWAddTypeMenu`로 메뉴 경로를 지정할 수 있고, `SWHideInTypeMenu`가 붙은 타입은 표시되지 않습니다.

### `Runtime/Coroutine`

코루틴 실행을 인터페이스로 분리해서 런타임 코드가 특정 MonoBehaviour에 강하게 묶이지 않도록 돕습니다.

- `ICoroutineRunner`: 코루틴 실행, 지연 실행, 간단한 보간 실행 계약입니다.
- `SWCoroutineRunner`: 실제 코루틴 실행 컴포넌트입니다.

사용 예시:

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
        Debug.Log("완료");
    }
}
```

### `Runtime/Data`

저장 데이터, PlayerPrefs, 암호화, 클라우드 저장을 다룹니다.

- `SWUtilsPlayerPrefs`: 슬롯, 암호화, JSON 내보내기와 가져오기를 지원하는 PlayerPrefs 래퍼입니다.
- `SWUtilsPlayerPrefsSettings`: 프로젝트별 salt와 IV salt를 보관하는 설정 에셋입니다.
- `SWSaveDataManager`: 여러 데이터 타입을 모아 파일로 저장하고 불러오는 저장 관리자입니다.
- `SWUtilsCloud`: Google Play Games, iCloud, Steamworks.NET, 로컬 fallback을 통합한 클라우드 저장 진입점입니다.
- `SWEncrypt`: 문자열 암호화 보조 기능입니다.
- `SWSaveSlot`: 기본 저장 슬롯 상수입니다.

사용 예시:

```csharp
using SWUtils;

SWUtilsPlayerPrefs.SetSlot("player_01");
SWUtilsPlayerPrefs.SetInt("coin", 100);
SWUtilsPlayerPrefs.Save();

int coin = SWUtilsPlayerPrefs.GetInt("coin", 0);
```

salt 설정은 `SWTools/Utils/PlayerPrefs Salt Settings`에서 생성하고 수정합니다. 설정 에셋은 `Assets/Resources/SWUtilsPlayerPrefsSettings.asset`에 생성되며, 빌드 런타임에서는 Resources를 통해 자동으로 읽습니다. salt 값을 변경하면 이전 salt로 저장된 데이터는 읽을 수 없으므로 변경 전에 데이터를 삭제하거나 마이그레이션을 준비해야 합니다.

저장 관리자 사용 예시:

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

`SWMonoBehaviour` 기본 클래스를 제공합니다. `SWTools` 어트리뷰트 기반 커스텀 인스펙터와 함께 사용할 때 편합니다.

사용 예시:

```csharp
using SWTools;
using UnityEngine;

public class PlayerController : SWMonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
}
```

### `Runtime/Pooling`

GameObject 풀링과 그룹 기반 생성 기능을 제공합니다.

- `SWPool`: 싱글톤 기반 오브젝트 풀입니다.
- `IPool`: 풀 기능 계약입니다.
- `IPoolable`: 풀에서 생성되거나 반환될 때 호출되는 콜백 계약입니다.
- `SWPoolCatalog`: 풀 등록 정보를 담는 ScriptableObject입니다.
- `SWPoolRegistry`: 씬 시작 시 풀과 그룹을 등록하는 컴포넌트입니다.
- `SWPoolGroupSelectionMode`: 그룹 생성 방식입니다.
- `SWPoolSnapshot`: `SWTools/Debug/Pool Monitor Window`에서 풀 상태를 표시하기 위한 읽기 전용 스냅샷입니다.

사용 예시:

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

풀 콜백 사용 예시:

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

팝업 생성, 표시, 숨김, 캐싱, 연출을 관리합니다.

- `SWPopupBase`: 모든 팝업이 상속하는 기본 컴포넌트입니다.
- `SWPopupManager`: 팝업 표시, 숨김, 키 기반 등록, 캐싱을 처리합니다.
- `SWPopupCatalog`: 키와 팝업 prefab을 연결하는 ScriptableObject입니다.
- `SWPopupShowEffect`, `SWPopupHideEffect`: 표시 및 숨김 연출의 추상 클래스입니다.
- `SWPopupScaleShowEffect`, `SWPopupScaleHideEffect`: DOTween 기반 기본 scale 연출입니다.
- `SWPopupLifecycle`: 팝업 생명주기 연결 컴포넌트입니다.

사용 예시:

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

키 기반 사용 예시:

```csharp
using SWUtils;

SWPopupManager.Instance.Register("option", optionPopupPrefab);
SWPopupManager.Instance.Show("option");
SWPopupManager.Instance.Hide("option");
```

### `Runtime/Resolution`

해상도, SafeArea, CanvasScaler 보정 기능을 제공합니다.

- `SWSafeArea`: 노치, 다이내믹 아일랜드, 펀치홀 영역에 맞춰 RectTransform anchor를 자동 보정합니다.
- `SWCanvasResolution`: 화면 비율에 따라 CanvasScaler의 `matchWidthOrHeight` 값을 조정합니다.

사용법:

1. SafeArea를 적용할 UI 오브젝트에 `SWSafeArea`를 추가합니다.
2. CanvasScaler 보정이 필요한 Canvas에 `SWCanvasResolution`을 추가합니다.
3. 인스펙터에서 방향별 적용 여부와 비율 설정을 조정합니다.

### `Runtime/Utils`

게임 전반에서 쓰는 작은 유틸리티 모음입니다.

- `SWAudioLibrary`, `SWAudioManager`: 음악과 효과음 키 기반 재생을 관리합니다.
- `SWCooldown`: 쿨다운 진행률, 남은 시간, 사용 가능 여부를 계산합니다.
- `SWEventBus`: 타입 기반 이벤트 구독, 발행, 해제를 처리합니다.
- `SWEventBusEventSnapshot`: `SWTools/Debug/EventBus Debugger Window`에서 이벤트 상태를 표시하기 위한 읽기 전용 스냅샷입니다.
- `SWSceneLoader`: 씬 로드, additive 로드, 언로드, 재로드를 처리합니다.
- `SWSingleton`, `SWSingletonScene`: 싱글톤 MonoBehaviour 기반 클래스입니다.
- `SWTimer`, `SWUtilsRefillTimer`: 시간 경과와 충전형 타이머를 다룹니다.
- `SWUtilsExtension`: Transform, GameObject 등 확장 메서드입니다.
- `SWUtilsFactory`: 런타임 오브젝트 생성 보조 기능입니다.
- `SWUtilsLog`: 로그 출력 래퍼입니다.
- `SWUtilsResolution`: 해상도 계산 보조 기능입니다.
- `SWUtilsString`: 문자열에서 숫자 추출 같은 문자열 보조 기능입니다.
- `SWUtilsTime`: 시간 포맷과 계산 보조 기능입니다.
- `SWUtilsTriggerDispatcher`: Trigger 이벤트를 위임해서 처리합니다.
- `SWUtilsUtility`: UI gauge 설정 같은 공통 보조 기능입니다.
- `SWVibration`: Android, iOS 진동 호출을 처리합니다.

이벤트 버스 사용 예시:

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
SWEventBus.Unsubscribe<CoinChangedEvent>(OnCoinChanged);
```

쿨다운 사용 예시:

```csharp
using SWUtils;

private readonly SWCooldown skillCooldown = new(3f);

private void TryUseSkill()
{
    if (!skillCooldown.TryUse()) return;

    // 스킬 실행
}
```

## Editor 폴더 기능

### `Editor/Attribute`

`Runtime/Attribute`의 인스펙터 표시를 실제 Unity Editor에서 그려주는 PropertyDrawer 모음입니다.

### `Editor/EditorWindow`

상단 메뉴 `SWTools`에서 열 수 있는 에디터 창 모음입니다. 디버그 용도는 `SWTools/Debug`, 일반 편의 기능은 `SWTools/Utils` 아래에 배치되어 있습니다.

- `SWTools/Debug/Build Report Viewer`: 빌드 리포트와 포함 에셋 크기를 확인합니다.
- `SWTools/Debug/EventBus Debugger Window`: `SWEventBus`에 등록된 이벤트 타입, 리스너 수, 발행 횟수, 마지막 발행 데이터를 확인합니다.
- `SWTools/Debug/Input Debugger Window`: Input System 장치와 입력 상태를 확인합니다.
- `SWTools/Debug/PlayerPrefs Viewer`: PlayerPrefs 데이터를 조회, 수정, 삭제합니다.
- `SWTools/Debug/Pool Monitor Window`: `SWPool`의 프리팹별 생성 수, 활성 수, 대기 수, 스폰 횟수, 반환 횟수, 지연 반환 수를 확인합니다.
- `SWTools/Debug/Test Tools Window`: 플레이 중 테스트와 씬 이동 작업을 보조합니다.
- `SWTools/Utils/Define Symbol Window`: Scripting Define Symbols를 관리합니다.
- `SWTools/Utils/Excel Table Importer`: 표 텍스트를 ScriptableObject 데이터로 적용합니다.
- `SWTools/Utils/Hierarchy Tools`: Hierarchy 오브젝트 색상, 아이콘, 스타일을 설정합니다.
- `SWTools/Utils/Localization Tools`: Localization 테이블 작업을 보조합니다.
- `SWTools/Utils/PlayerPrefs Salt Settings`: SWUtilsPlayerPrefs 암호화 salt 설정 에셋을 생성하고 수정합니다.
- `SWTools/Utils/Quick Asset Palette`: 자주 쓰는 에셋을 빠르게 선택합니다.
- `SWTools/Utils/Reference Finder`: 선택한 에셋의 프로젝트 참조를 찾습니다.
- `SWTools/Utils/TMP Font Asset Manager`: TextMeshPro 폰트 에셋 적용과 성능 확인을 관리합니다.
- `SWTools/Utils/Resolution Window`: 해상도 테스트 값을 확인합니다.

#### `SWTools/Debug/EventBus Debugger Window`

`SWEventBus`의 현재 상태를 플레이 중 또는 에디터에서 확인하는 디버그 창입니다.

확인 항목:

- 이벤트 타입 이름과 전체 이름
- 현재 등록된 리스너 수
- 이벤트 발행 횟수
- 마지막 발행 시간
- 마지막 발행 데이터 요약

`발행 기록 초기화` 버튼으로 리스너는 유지한 채 발행 횟수와 마지막 발행 기록만 초기화할 수 있습니다.

#### `SWTools/Debug/Pool Monitor Window`

`SWPool`의 프리팹별 상태를 확인하는 디버그 창입니다.

확인 항목:

- 풀에 등록된 프리팹
- 풀 이름과 그룹 이름
- 생성 수, 활성 수, 대기 수
- 스폰 횟수, 반환 횟수, 파괴 수
- 지연 반환 예약 수

씬에 존재하는 `SWPool`을 기준으로 표시하며, 등록만 되고 아직 실제 ObjectPool이 생성되지 않은 프리팹도 0 상태로 확인할 수 있습니다.

#### `SWTools/Utils/TMP Font Asset Manager` 성능 탭

TextMeshPro 폰트 에셋의 아틀라스 메모리, 글리프, 문자, 폴백 체인, 머티리얼 프리셋 비용을 한 화면에서 확인합니다.

사용 순서:

1. Unity 상단 메뉴에서 `SWTools > Utils > TMP Font Asset Manager`를 엽니다.
2. `성능` 탭을 선택합니다.
3. 확인할 `TMP_FontAsset`을 드래그 앤 드롭하거나 Object Field에 지정합니다.
4. 선택 중인 폰트 에셋 또는 TextMeshPro 오브젝트의 폰트를 확인하려면 `선택 에셋 사용`을 누릅니다.
5. Quick Swap 탭에 지정한 기본 폰트를 확인하려면 `기본 폰트 사용`을 누릅니다.
6. 폴백 폰트 전체 체인을 포함해서 계산하려면 `폴백 체인 포함`을 켭니다.

확인 항목:

- 아틀라스 텍스처 개수, 전체 픽셀 면적, 런타임 텍스처 메모리, 저장 텍스처 메모리, RGBA32 기준 예상 메모리
- 글리프 개수와 문자 개수
- 직접 폴백 폰트 개수, 전체 폴백 폰트 개수, 폴백 깊이
- 동적 아틀라스 사용 여부
- 같은 폴더에서 찾은 TextMeshPro 머티리얼 프리셋 개수

점검 항목에서는 모바일 대상에서 주의가 필요한 큰 아틀라스 메모리, 많은 글리프, 긴 폴백 체인, 동적 아틀라스, 많은 머티리얼 프리셋을 경고로 표시합니다.

### `Editor/ExcelTable`

표 텍스트를 파싱하고 `SWTable`, `SWTableSheet` 어트리뷰트가 붙은 ScriptableObject 필드에 적용하는 기능입니다.

### `Editor/HierarchyTools`

Hierarchy 표시 스타일과 아이콘을 저장하고 적용합니다. `SWHierarchyToolsWindow`와 함께 사용됩니다.

### `Editor/Monobehaviour`

`SWMonoBehaviour`를 상속한 컴포넌트의 커스텀 인스펙터를 구성합니다. 그룹, 버튼, 조건부 표시, 상시 repaint 처리 등이 이곳에서 연결됩니다.

### `Editor/StyleSheet`

에디터 UI Toolkit 화면에서 사용하는 스타일시트입니다.

### `Editor/Utils`

에디터 창과 커스텀 인스펙터에서 공통으로 쓰는 GUI, 드래그 앤 드롭, 아이콘, EditorPrefs, 스타일 캐시, 선택 및 ping 유틸리티입니다.

## Samples 폴더

샘플 prefab과 예제 스크립트를 제공합니다.

- `Samples/Example/SWAttributeExample.cs`: 어트리뷰트 사용 예시입니다.
- `Samples/Example/SWSubClassSelectorExample.cs`: `SWSubClassSelector`, `SWAddTypeMenu`, `SWHideInTypeMenu` 사용 예시입니다.
- `Samples/Prefab/AtrributeExample.prefab`: 어트리뷰트 예제 prefab입니다.
- `Samples/Prefab/SWPool.prefab`: 풀 매니저 prefab입니다.
- `Samples/Prefab/SWPoolRegistry.prefab`: 풀 등록 prefab입니다.

Unity Package Manager에서 패키지를 설치한 뒤 Sample을 프로젝트로 가져오면 예제 prefab을 직접 확인할 수 있습니다.

## assembly definition

- `SWUtils.Runtime`: 런타임 코드 assembly입니다.
- `SWUtils.Editor`: 에디터 코드 assembly입니다.
- `SWUtils.Samples`: 샘플 코드 assembly입니다.

샘플 prefab은 각 스크립트가 들어 있는 assembly 이름을 기준으로 직렬화되어 있습니다.
