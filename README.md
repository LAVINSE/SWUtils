# SWUtils 1.4.1

[한국어](README.md) | [English](README.en.md)

Unity 6용 공통 런타임과 편집기 도구 모음입니다. 저장 슬롯, 상태 머신, 행동 트리, 퀘스트, 스킬트리, 풀링과 팝업을 구성하고 인스펙터와 전용 편집기에서 데이터를 편집합니다.

현재 테스트 단계의 패키지입니다. 퀘스트·업적을 포함한 각 기능은 적용할 프로젝트에서 동작과 저장 호환성을 확인해야 합니다.

`SWTools > SWUtils Data Editor`에서 ScriptableObject를 검색하고 분류·즐겨찾기·다중 탭으로 편집합니다. 편집기 창과 창 내부의 편집 영역에 공통 디자인을 적용하며, Unity 기본 Inspector 창은 기존 SWUtils 스타일을 유지합니다. [사용법과 디자인 분석](Documentation~/SWUtilsEditor.ko.md)을 확인하세요.

## 설치

Unity Package Manager에서 `+ > Add package from git URL...`을 선택하고 배포 태그를 입력합니다.

```text
https://github.com/LAVINSE/SWUtils.git#v1.4.1
```

위 주소는 원격 저장소에 `v1.4.1` 태그가 등록된 후 사용할 수 있습니다. 태그 등록 전에는 `+ > Add package from disk...`에서 로컬 `package.json`을 선택합니다. 개발 중인 코드를 받을 때는 원하는 브랜치 또는 커밋을 지정합니다. [버전별 변경 기록](CHANGELOG.ko.md)을 확인하세요.

필수 패키지와 모듈은 `package.json`으로 연결됩니다. Localization, Unity UI 2.0에 포함된 TextMeshPro, Audio, Android JNI, IMGUI, JSON Serialize, Physics, Physics 2D를 사용합니다. Input System은 프로젝트에 설치되어 있을 때 선택적으로 사용합니다.

클라우드 연동은 별도 설정이 필요합니다. Google Play Games는 `SW_GOOGLEPLAY_ENABLE`, Steamworks.NET은 `SW_STEAMWORKS_NET`, 프로젝트에서 제공하는 iCloud 네이티브 연결은 `SW_ICLOUD_ENABLE`로 활성화합니다. [필요한 구현과 저장 규칙](Documentation~/Reliability.ko.md)을 확인하세요.

## 첫 사용

1. 컴포넌트는 `SW.Base.SWMonoBehaviour`, 데이터 에셋은 `SW.Base.SWScriptableObject`를 상속합니다.
2. 인스펙터 확장에는 `SW.Attributes`를 사용합니다.
3. 프로젝트가 어셈블리 정의 파일을 사용한다면 `SWUtils.Runtime`을 참조합니다.
4. 예제는 프로젝트 창의 `Packages > SWUtils > Samples`에서 확인합니다.

아래 컴포넌트를 게임 오브젝트에 추가하면 인스펙터에서 값을 편집하고 버튼으로 저장할 수 있습니다.

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
    [SWButton("시작 점수 저장")]
    private void SaveStartingScore()
    {
        SWPlayerPrefs.SetInt("StartingScore", startingScore);
        SWPlayerPrefs.Save();
    }
}
```

관리자 컴포넌트의 씬 유지 여부는 해당 컴포넌트 설정으로 지정합니다. 저장 슬롯은 게임의 저장 흐름에서 정합니다.

## 전체 기능 찾아보기

아래 링크는 이 README의 기능 설명으로 이동합니다. 각 절에서 제공 기능, 설정 방법과 사용 예제를 확인할 수 있습니다.

| 기능 | README 본문 |
| --- | --- |
| 공통 기반·인스펙터 | [기본 타입](#runtime-base) · [어트리뷰트](#runtime-attributes) |
| 코루틴·저장 | [코루틴](#runtime-coroutines) · [데이터와 저장](#runtime-storage) |
| 게임 로직 | [스킬트리](#runtime-skilltree) · [퀘스트와 업적](#runtime-quests) · [능력치](#runtime-stats) |
| 실행 흐름 | [상태 머신](#runtime-states) · [행동 트리](#runtime-behaviour) |
| 오브젝트·화면 | [풀링](#runtime-pooling) · [팝업](#runtime-popups) · [해상도](#runtime-resolution) |
| 시간·입력·오디오·씬·이벤트 | [공통 유틸리티](#runtime-utilities) |
| 디버그·제작 도구 | [런타임 콘솔](#runtime-debug) · [편집기 기능과 메뉴](#editor-tools) |

[네임스페이스](#네임스페이스-구조) · [샘플](#샘플) · [어셈블리](#조립체-정의) · [변경 기록](CHANGELOG.ko.md)

## 화면 미리보기

인스펙터에서 그룹과 조건 표시, 메서드 실행 버튼을 구성합니다.

<img src="Documentation~/Media/SWAttribute.gif" alt="SWUtils 인스펙터 속성 예제" width="460">

상태 머신과 행동 트리는 그래프에서 연결하고 실행 상태를 확인합니다.

![상태 머신 편집기](Documentation~/Media/SWStateMachine.gif)
![행동 트리 편집기](Documentation~/Media/SWBehaviourTree.gif)

[에셋 팔레트](Documentation~/Media/SWAssetPalette.png) · [참조 검색](Documentation~/Media/ReferenceFinder.png) · [표 가져오기](Documentation~/Media/ExcelTable.png) · [숫자 표시 설정](Documentation~/Media/AmountFormat.png)

## 네임스페이스 구조

Runtime과 Editor 코드는 기능별 폴더와 같은 네임스페이스를 사용합니다.

| 폴더 | 네임스페이스 |
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
| `Editor/<기능>` | `SW.EditorTools.<기능>` |

## 런타임 기능

<a id="runtime-skilltree"></a>

### 스킬트리

인크리멘탈 강화, 연구 해금과 특성 선택을 위한 스킬트리를 제공합니다. 노드별 선행 레벨, 모든 조건·하나 이상 조건, 선택 분기, 반복 강화, 여러 재화 구매, 환불, 저장 복원과 영구 노드를 유지하는 초기화를 구성할 수 있습니다. 정의와 소유자별 진행을 분리하며 조건, 비용, 지속 효과와 프로젝트 재화를 확장할 수 있습니다.

- 제작 편집기: `SWTools > Utils > Data > Skill Tree Editor`
- 기본 게임 화면: `Samples/Prefab/SWSkillTreeExample.prefab`
- 화면 위치 편집: 예제 인스펙터의 `노드 생성 및 자동 배치`, `현재 위치 저장`, `저장 위치 불러오기`
- [스킬트리 사용법과 확장 계약](Documentation~/SkillTree.md)

MiningSkillTree 예제는 **81개 노드와 6개 확장 경로**로 구성되어 있습니다. 기본 배율로 시작해 드래그와 휠로 화면을 탐색하고, `Start`, `Selected`, `Fit All` 버튼으로 시작 지점·선택 노드·현재 표시 중인 노드를 확인합니다. 노드 선택 상세 정보와 구매·환불 조작도 포함합니다.

TreeView의 **노드 배치 → 노드 크기**에서 크기를 직접 설정합니다. 별도의 ViewStyle 에셋은 사용하지 않으며, TextMeshProUGUI와 프로젝트의 기본 글꼴을 사용합니다. 별도 글꼴 데이터는 포함하지 않습니다.

인스펙터의 `SWButton`으로 노드를 생성하며, 자동 배치도 저장 좌표를 우선 사용합니다. 직접 수정한 위치를 저장하면 Skill Tree Editor에도 반영되고, 연결선의 길이와 각도는 실제 노드 위치에 맞춰 갱신됩니다. **편집 미리보기 → 전체 노드 보기**로 숨겨진 노드도 배치할 수 있습니다. 실행 중에는 선행 진행에 따른 숨김·정보 가리기를 적용하며, 플레이어 진행 저장에 화면 배치 데이터를 추가하지 않습니다. 예제 생성과 재생성은 SWTools 메뉴에 노출하지 않습니다.

`SWSkillTreeSystem`과 `ISWSkillTreeWallet`으로 플레이어별 진행과 재화를 연결합니다. `PreviewPurchase`는 구매 가능 여부와 비용을 확인하고, `Purchase`는 비용을 일괄 차감합니다. `Refund`는 마지막 레벨의 실제 지불 비용을 돌려주며 후속 조건이 깨지는 환불은 거절합니다. `Reset`에서 영구 노드 유지와 환급 여부를 지정합니다.

조건·비용·효과는 `SWSkillTreeCondition`, `SWSkillTreeCost`, `SWSkillTreeEffect`로 확장합니다. 지속 효과는 `SWSkillTreeEffectBinding`으로 연결하고 현재 레벨 기준으로 적용합니다. `CaptureSaveData`와 `Restore`는 진행과 지불 기록을 다루며 지갑 잔액도 같은 게임 저장에 포함해야 합니다.

<a id="runtime-attributes"></a>

### 어트리뷰트

- `SWButton`: 인스펙터 버튼으로 메서드를 실행합니다.
- `SWButtonBar`: 여러 메서드 실행 버튼을 한 줄에 표시합니다.
- `SWCondition`: Boolean 필드 값에 따라 필드를 표시하거나 숨깁니다.
- `SWEnumCondition`: 열거형 값에 따라 필드를 표시하거나 숨깁니다.
- `SWDropdown`: 미리 정의한 값을 드롭다운으로 표시합니다.
- `SWGroup`: 인스펙터 필드를 접을 수 있는 그룹으로 묶습니다.
- `SWReadOnly`: 필드를 읽기 전용으로 표시합니다.
- `SWSubClassSelector`: `SerializeReference` 필드의 구현 타입을 검색하여 선택합니다.
- `SWAddTypeMenu`, `SWHideInTypeMenu`: 구현 타입 선택 메뉴의 경로와 숨김 여부를 지정합니다.
- `SWRequiresConstantRepaint`, `SWRequiresConstantRepaintOnlyWhenPlaying`: 인스펙터를 계속 갱신하거나 실행 중에만 갱신하도록 지정합니다.
- `SWTable`, `SWTableSheet`: 표 데이터를 직렬화 필드에 연결합니다.
- `SWCommand`: 메서드를 런타임 디버그 콘솔 명령으로 노출합니다.

<a id="runtime-base"></a>

### 기본 타입

- `SWMonoBehaviour`: SWUtils 인스펙터 기능을 사용하는 컴포넌트 기본 클래스입니다.
- `SWScriptableObject`: 같은 인스펙터 기능을 사용하는 데이터 에셋 기본 클래스입니다.
- `SWIdentifiedObject`: 식별자, 코드명, 표시명, 설명, 카테고리와 에디터 전용 스프라이트 아이콘을 가진 데이터 에셋입니다.
- `SWIODatabase`: `SWIdentifiedObject` 목록을 관리하고 빠르게 조회합니다.
- `SWCategory`: 데이터 에셋 분류에 사용합니다.

`SWIdentifiedObject`의 공통 필드는 기본으로 접힌 **기본 정의** 폴드아웃에 모아 표시합니다. 기존 에셋과 파생 타입에도 자동 적용하며, 사용자가 변경한 펼침 상태를 유지합니다.

<a id="runtime-coroutines"></a>

### 코루틴

- `ICoroutineRunner`: 코루틴 실행 기능의 추상 인터페이스입니다.
- `SWCoroutineRunner`: 지연 호출, 다음 프레임 호출, 조건 대기와 반복 실행을 제공합니다.

```csharp
using SW.Coroutines;
using UnityEngine;

public class DelayExample : MonoBehaviour
{
    [SerializeField] private SWCoroutineRunner coroutineRunner;

    private void Start()
    {
        coroutineRunner.DelayedCall(1f, () => Debug.Log("완료"));
    }
}
```

`Wait(seconds)`는 같은 길이의 일반 시간 대기를 최대 128개까지 캐시합니다. `WaitRealtime(seconds)`는 호출마다 독립적인 실제 시간 대기 객체를 생성합니다. 실행 핸들을 보관하면 `Stop`으로 개별 코루틴을 멈출 수 있습니다.

<a id="runtime-storage"></a>

### 데이터와 저장

- `SWEncrypt<T>`: `SWPlayerPrefs`를 사용하는 암호화 값 래퍼입니다.
- `SWPlayerPrefs`: 키 이름은 해시로 바꾸고 값은 암호화하여 Unity PlayerPrefs에 저장합니다.
- `SWPlayerPrefsSettings`: 암호화 솔트 설정을 관리합니다.
- `SWSaveDataManager`: 슬롯별 저장, 불러오기, 백업과 복원을 관리합니다.
- `SWSaveSlot`: 기본 저장 슬롯 이름을 제공합니다.
- `SWCloud`: 플랫폼별 클라우드 저장소와 로컬 대체 저장소를 통합합니다.

`SWPlayerPrefs`는 내부적으로 Unity PlayerPrefs를 사용합니다. 따라서 Unity PlayerPrefs 전체 삭제는 SWUtils 암호화 데이터도 함께 삭제합니다.

```csharp
using SW.Data;

SWPlayerPrefs.SetInt("coin", 100);
int coin = SWPlayerPrefs.GetInt("coin");
SWPlayerPrefs.Save();
```

저장 슬롯은 `SetSlot`으로 선택하고, 비동기 작업에서는 `SetString`, `GetString`, `ImportFromJson`, `MergeFromJson`의 슬롯 인수를 명시합니다. `ExportSlotToJson`은 현재 선택을 바꾸지 않고 지정한 슬롯을 내보냅니다. 정수·실수·문자열·논리값과 `long`·`double`을 저장할 수 있습니다.

`ImportFromJson`은 전체 입력을 검증한 뒤 슬롯 내용을 교체하며 `MergeFromJson`은 같은 키만 덮어씁니다. 빈 문자열과 구분자가 포함된 키도 보존합니다. 파일 저장은 임시 기록 후 교체하고, 등록된 데이터를 읽을 때 본 파일이 없거나 해석되지 않으면 백업을 확인합니다.

`SWCloud`는 Google Play Games, Steam Cloud, 프로젝트에서 제공하는 iCloud 연결과 로컬 대체 저장을 제공합니다. 실제 연동에는 해당 라이브러리·인증·정의 심볼이 필요합니다. 파일과 PlayerPrefs를 함께 다루는 복원의 범위와 이전 캐시 호환은 [저장·실행 규칙](Documentation~/Reliability.ko.md)에 설명되어 있습니다.

<a id="runtime-debug"></a>

### 디버그

- `SWDebugConsole`: 로그 확인, 명령 실행과 상태 감시를 제공하는 런타임 콘솔입니다.
- `SWDebugConsoleSettings`: 콘솔 열기 입력, 선택적 Input System 확인, 성능 오버레이 표시값을 저장하는 설정 에셋입니다.
- `SWCommand`: 콘솔에서 실행할 메서드를 등록합니다.
- `SWLog`: `SW_DEBUG_MODE` 정의 심볼이 있을 때 로그를 출력합니다.

`SWTools/Debug/Console/Debug Console Settings`에서 현재 빌드 타겟에 `SW_DEBUG_MODE`를 추가한 뒤 사용합니다. 심볼이 없으면 콘솔 호출은 조건부 메서드로 컴파일에서 제거됩니다.

디버그 콘솔 설정 순서:

1. `SWTools/Debug/Console/Debug Console Settings`를 엽니다.
2. `상태` 탭에서 현재 빌드 타겟에 `SW_DEBUG_MODE`를 추가합니다.
3. 프로젝트별 값을 저장하려면 설정 에셋을 생성합니다.
4. `입력` 탭에서 열기 키와 `Control`, `Shift`, `Alt` 조합키를 선택합니다.
5. 모바일에서 콘솔을 여는 동시 터치 개수를 지정합니다.

Input System 패키지는 필수 의존성이 아닙니다. `Input System 확인`을 켜고 프로젝트에 Input System 패키지가 있으면 캐시된 리플렉션으로 먼저 입력을 확인합니다. 패키지가 없으면 Unity 기본 `Input` API로 처리하므로 컴파일 오류가 발생하지 않습니다.

성능 오버레이 설정 순서:

1. `SWTools/Debug/Console/Debug Console Settings`를 엽니다.
2. `오버레이` 탭에서 시작 시 표시 여부, 표시 위치, 크기 배율, 갱신 간격을 설정합니다.
3. FPS, 최소/최대 FPS, 메모리 표시 여부와 FPS 경고 기준을 선택합니다.

런타임 제어 예시:

```csharp
using SW.Debugging;

SWDebugConsole.Show();
SWDebugConsole.ToggleOverlay();
SWDebugConsole.ResetOverlayStats();
```

명령 등록 예시:

```csharp
using SW.Attributes;

public class DebugCommands
{
    [SWCommand("give_gold", "테스트 골드를 추가합니다", "Test")]
    private static void GiveGold(int amount)
    {
    }
}
```

<a id="runtime-pooling"></a>

### 오브젝트 풀

- `IPool`, `IPoolable`: 풀 구현과 풀링 대상의 계약입니다.
- `SWPool`: 프리팹별 생성, 예열, 반환, 지연 반환과 유휴 풀 정리를 관리합니다.
- `SWPoolCatalog`: 풀 이름, 그룹과 예열 수량을 데이터로 관리합니다.
- `SWPoolRegistry`: 카탈로그를 실제 풀에 등록합니다.
- `SWPoolSnapshot`: 에디터 모니터에서 사용하는 읽기 전용 상태입니다.
- `SWPoolGroupSelectionMode`: 같은 그룹에 등록된 프리팹을 선택하는 방식을 지정합니다.

시작 씬에 `SWPool`과 `SWPoolRegistry`를 배치하고 카탈로그를 연결합니다. 프리팹 참조 또는 등록된 이름·그룹으로 `Spawn`하고 `Release`로 즉시 또는 지연 반납합니다. `Prewarm`은 사용할 인스턴스를 미리 만들며 `TrimIdlePools`는 활성 객체가 없는 유휴 풀을 정리합니다.

풀은 비활성 상태에서 부모·위치와 풀 참조를 설정하고 `OnSpawnFromPool`을 호출한 뒤 객체를 활성화합니다. `IPoolable` 구현은 재사용 상태를 초기화하고 `OnReturnToPool`에서 정리합니다. 미리 생성만 하는 동안에는 이 두 콜백을 호출하지 않습니다.

<a id="runtime-popups"></a>

### 팝업

- `SWPopupBase`: 팝업 생명주기와 표시 상태를 관리합니다.
- `SWPopupManager`: 팝업 생성, 표시, 숨김, 캐시와 카탈로그 조회를 관리합니다.
- `SWPopupCatalog`: 문자열 키와 팝업 프리팹을 연결합니다.
- `SWPopupShowEffect`, `SWPopupHideEffect`: 표시 및 숨김 연출 기본 타입입니다.
- `SWPopupScaleShowEffect`, `SWPopupScaleHideEffect`: 크기 변경 기반 기본 연출입니다.
- `SWPopupEffectHandle`: 실행 중인 팝업 연출을 제어합니다.
- `SWPopupLifecycle`: 외부 비활성화·파괴를 감지하여 관리자에게 닫힘을 알립니다.

프리팹, 기존 인스턴스 또는 카탈로그 키로 팝업을 표시할 수 있습니다. `Show<T>`의 초기화 콜백으로 표시할 데이터를 전달하고 `Hide`로 닫습니다. `ShowAsync`는 해당 팝업이 닫힐 때까지 기다리므로 확인창과 보상창을 순서대로 표시할 때 사용할 수 있습니다.

기본 부모·Canvas와 표시 순서를 관리하며 카탈로그 설정으로 인스턴스를 캐시할 수 있습니다. 다시 표시한 팝업에는 이전 숨김 콜백이 적용되지 않습니다. 표시·숨김 연출은 기본 크기 변경 연출을 사용하거나 프로젝트에서 확장할 수 있습니다.

<a id="runtime-resolution"></a>

### 해상도

- `SWCanvasResolution`: 화면 비율에 따라 CanvasScaler 설정을 조정합니다.
- `SWSafeArea`: 노치와 화면 안전 구역에 맞춰 사용자 인터페이스를 배치합니다.
- `SWResolution`: 화면 크기, 비율, 좌표 변환과 카메라 계산을 제공합니다.

<a id="runtime-stats"></a>

### 능력치

- `SWStat`: 기본값과 보너스 값을 조합하는 능력치 데이터입니다.
- `SWStatOverride`: 개체별 기본값 재정의 설정입니다.
- `SWStats`: 게임 오브젝트의 런타임 능력치 목록을 관리합니다.
- `SWStatScaleFloat`: 능력치 비율을 적용한 값을 계산합니다.

`SWStats`는 정의 에셋에서 개체별 실행 값을 준비합니다. `SWStat`의 보너스는 출처와 세부 키로 등록·교체·제거하므로 장비나 효과가 만든 값만 해제할 수 있습니다. 최종 값은 최소·최대 범위로 제한되며 값 변경과 상한·하한 도달을 이벤트로 알립니다.

`SetRange(minimumValue, maximumValue)`로 최소·최대 값을 함께 설정합니다. 잘못된 범위를 거절하며, 범위 변경으로 최종 값이 달라졌을 때도 알림이 발생합니다.

<a id="runtime-quests"></a>

### 퀘스트와 업적

> [!WARNING]
> 퀘스트와 업적은 아직 전체 설계 검토와 실제 프로젝트 검증이 끝나지 않은 실험적 시스템입니다. 저장 데이터 형식과 공개 기능이 변경될 수 있으므로 실제 서비스 적용 전 충분히 검증하세요.

- `SWQuest`: 순서가 있는 작업 묶음, 수락·취소 조건과 완료 보상을 조합하는 정의 에셋입니다.
- `SWAchievement`: 자동 완료, 항상 저장, 취소 금지 규칙을 적용한 업적 정의입니다.
- `SWQuestTask`, `SWQuestTaskGroup`: 동시에 진행할 작업과 순서대로 진행할 작업 묶음을 구성합니다.
- `SWQuestTaskAction`: 보고 변화량을 진행량으로 바꾸는 전략입니다. 더하기, 값 교체, 양수·음수 전용, 연속 진행 전략을 기본 제공합니다.
- `SWQuestTarget`: 문자열 또는 Unity 오브젝트 보고 대상을 비교합니다.
- `SWQuestCondition`, `SWQuestReward`: 프로젝트별 수락 조건, 취소 조건과 보상을 확장하는 기본 타입입니다.
- `SWQuestDatabase`, `SWAchievementDatabase`: 일반 퀘스트와 업적 정의를 각각 조회하고 검증합니다.
- `SWQuestSystem`: 런타임 복제, 중복 등록 방지, 진행 보고, 완료·취소, 업적 자동 등록과 저장 복원을 관리합니다.
- `SWQuestSystemWindow`: 관련 에셋 생성·복제·삭제·검색·편집과 데이터베이스 동기화·검증을 한 창에서 제공합니다.
- `ISWQuestSaveStore`: 퀘스트 저장 위치를 교체하는 계약이며 기본 구현은 암호화된 `SWPlayerPrefs`를 사용합니다.
- `SWQuestGiver`, `SWQuestReporter`: 시작 시 퀘스트를 지급하거나 직접 호출 및 물리 트리거에서 진행을 보고합니다.

구성 순서:

1. `Assets > Create > SWBase > Category`에서 진행 보고를 구분할 카테고리를 만듭니다.
2. `Assets > Create > SWUtils > Quest > Target`에서 선택적인 문자열 또는 Unity 오브젝트 대상을 만듭니다.
3. `Assets > Create > SWUtils > Quest > Task`에서 카테고리, 대상, 필요 진행량과 계산 전략을 연결합니다. 계산 전략이 비어 있으면 보고 변화량을 현재 진행량에 더합니다.
4. `Quest` 또는 `Achievement` 에셋에 작업 묶음, 조건과 보상을 연결하고 묶음마다 퀘스트 안에서 고유한 코드명을 지정합니다.
5. `SWTools > Utils > Data > Quest System Editor`를 열어 퀘스트와 업적 데이터베이스를 각각 만들고 `프로젝트 정의 동기화` 후 검증합니다.
6. 시작 씬의 `SWQuestSystem`에 두 데이터베이스를 연결하고 게임 플레이에서 진행을 보고합니다.

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

업적은 업적 데이터베이스 초기화 시 자동 등록되고 같은 진행 보고를 받습니다. 일반 퀘스트와 업적은 별도 데이터베이스이므로 서로 같은 코드명을 사용할 수 있지만, 각 데이터베이스 안에서는 코드명이 고유해야 합니다.

기본 저장은 암호화된 `SWPlayerPrefs`를 사용합니다. 다른 저장소가 필요하면 자동 불러오기를 끄고 `SetSaveStore(ISWQuestSaveStore)`로 구현을 연결할 수 있습니다. 게임 전체 저장 데이터와 합칠 때는 `CreateSaveData()`가 반환하는 `SWQuestSystemSaveData`를 저장 루트에 포함하고, 불러온 뒤 `RestoreSaveData()`로 적용합니다. 완료 상태를 복원할 때 보상은 다시 지급하지 않습니다.

프로젝트별 조건과 보상은 각각 `SWQuestCondition`, `SWQuestReward`를 상속합니다. 외부 게임 서비스가 필요하면 시작할 때 `SetContext`로 문맥을 연결하고 구현 안에서 `TryGetContext<TContext>`로 가져옵니다. 전체 예제는 `Samples/Scripts/SWQuestExample.cs`와 `SWQuestScoreRewardExample.cs`에 있습니다.

에셋 구성, 확장 지점, 이벤트와 저장 구조는 [퀘스트와 업적 상세 문서](Documentation~/Quest.ko.md)를 참고하세요.

완료 처리는 모든 보상 지급이 성공한 뒤 확정합니다. 예를 들어 골드 지급 후 아이템 지급이 실패하면 `WaitingForCompletion` 상태를 유지합니다. 다시 `Complete()`를 호출하면 성공 기록이 있는 골드는 건너뛰고 실패한 보상부터 재시도합니다. 일부 보상을 지급한 퀘스트는 취소할 수 없습니다.

보상 구현의 `Grant`는 지급할 수 없을 때 데이터를 변경하기 전에 예외를 발생시켜야 합니다. 지급 성공 알림은 화면·효과음에 사용하고 실제 지급은 `Grant`에서 처리합니다. 재화·아이템과 퀘스트 지급 기록은 같은 게임 저장 단위에 포함해야 합니다.

<a id="runtime-states"></a>

### 상태 머신

- `SWStateMachine<TContext>`: Unity 컴포넌트에 의존하지 않는 다중 계층 범용 유한 상태 머신입니다.
- `SWState<TContext>`: 초기화, 진입, 갱신, 종료와 메시지 처리를 정의하는 상태 기본 타입입니다.
- `SWMonoStateMachine<TContext>`: 일반 프레임, 물리 프레임 또는 수동 방식으로 상태 머신을 갱신하는 Unity 컴포넌트 기본 타입입니다.
- `SWStackStateMachine<TContext>`: 이전 상태를 유지하면서 새 상태를 쌓고 제거하는 범용 스택 상태 머신입니다.
- `SWStackState<TContext>`: 진입, 일시 정지, 복귀, 갱신, 종료와 메시지 처리를 정의하는 스택 상태 기본 타입입니다.
- `SWMonoStackStateMachine<TContext>`: 스택 상태 머신을 Unity 갱신 생명주기와 연결하는 컴포넌트 기본 타입입니다.

각 계층은 하나의 현재 상태를 가지며 낮은 계층 번호부터 독립적으로 실행됩니다. 모든 상태 전이는 일반 상태 전이보다 먼저 확인하고, 같은 우선순위에서는 등록 순서가 빠른 전이를 먼저 실행합니다.

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

그래프 기반 다중 계층 상태의 통합 예제는 `SWGraphAssetsExample`에서 확인할 수 있습니다.

스택 상태 머신에서는 최상단 상태만 갱신됩니다. 새 상태를 추가하면 기존 상태가 일시 정지되고, 최상단 상태를 제거하면 아래 상태가 종료되지 않은 채 다시 활성화됩니다.

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

그래프 기반 스택 상태와 실행 제어도 `SWGraphAssetsExample`에서 함께 확인할 수 있습니다.

#### 상태 머신 그래프 편집기

- `SWStateMachineGraphAsset`: 상태 노드와 전이 정보를 저장하는 `ScriptableObject` 그래프 에셋입니다.
- `Layered`: 여러 계층을 독립적으로 실행하며 같은 계층의 상태와 `Any State`를 연결하는 그래프 형식입니다.
- `Stack`: 상태를 쌓고 제거하는 그래프 형식입니다. 입력 전용 `Return State`로 현재 상태를 제거하고 이전 상태에 복귀합니다.
- `SWStateMachineGraphFactory`: 그래프 에셋으로 다중 계층 상태 머신 또는 스택 상태 머신 제어기를 생성합니다.
- `SWStateMachineGraphCondition<TContext>`: 프로젝트 문맥에 맞는 전이 조건을 구현하는 기본 타입입니다.
- `SWStackStateMachineGraphController<TContext>`: 스택 그래프의 갱신, 명령, 메시지, 중지와 상태 전이를 제어합니다.

##### 편집 방법

1. `Assets > Create > SWTools > State Machine Graph`에서 그래프 에셋을 생성합니다.
2. 그래프 에셋을 두 번 클릭하거나 인스펙터의 `상태 머신 그래프 편집` 버튼을 누릅니다.
3. `SWTools > Utils > State Machine > Graph Editor`에서도 편집기 창을 열 수 있습니다.
4. 왼쪽 `Blackboard`의 `Graph Type`에서 `Layered` 또는 `Stack`을 선택합니다.
5. 가운데 빈 공간을 마우스 오른쪽 버튼으로 누른 뒤 `Create Node...`를 선택합니다. 상단 `Create Node` 버튼과 `Space` 키도 같은 검색 창을 엽니다.
6. 검색 창의 `States`에서 `SWState<TContext>` 또는 `SWStackState<TContext>` 구현을 선택합니다. `Flow Control`에서는 `Any State` 또는 `Return State`를 생성할 수 있습니다. 이름이 같은 중첩 상태는 선언 클래스 이름으로 구분됩니다.
7. 노드의 `Out` 연결점에서 다른 노드의 `In` 연결점까지 끌어 전이를 만듭니다. 연결선 위 표식에서 동작, 명령과 우선순위를 바로 확인할 수 있습니다.
8. 노드를 선택하면 오른쪽에서 표시 이름, 초기 상태와 `Layer`를 편집합니다. 연결선을 선택하면 동작, 명령, 조건, 재진입과 우선순위를 편집합니다.
9. 노드 또는 연결선을 선택하고 `Delete` 키를 누르면 삭제됩니다. `Blackboard`의 검색 가능한 `States`와 `Transitions` 목록에서도 선택·이동·추가·삭제할 수 있고, 각 목록은 접을 수 있습니다.
10. 아래 `Graph Validation`을 펼쳐 구체적인 오류 내용을 확인한 뒤 `Save Asset`으로 저장합니다.

##### 편집 및 디버깅 기능

- `Graph List`: 그래프 에셋을 검색하고 빠르게 전환하며 패널을 왼쪽으로 접을 수 있습니다.
- `Blackboard`: 그래프 형식, 상태와 전이 목록을 관리하고 각 목록을 검색하거나 접을 수 있습니다. 아래 모서리를 끌어 패널 크기를 조절합니다.
- `Graph Inspector`: 선택한 상태의 표시 이름, 초기 상태와 계층 또는 전이의 동작, 명령, 조건, 재진입과 우선순위를 편집합니다. 아래 모서리를 끌어 패널 크기를 조절합니다.
- `Graph Validation`: 저장 전에 잘못된 노드 구성과 전이 연결을 검사합니다.
- `Auto Layout`: 계층과 초기 상태를 기준으로 노드를 자동 정렬합니다.
- `New Script`: 수정 가능한 텍스트 템플릿으로 다중 계층 상태, 스택 상태와 전이 조건 스크립트를 생성합니다.
- `전이 요약`: 노드 이동을 따라가며 동작, 명령과 우선순위를 표시합니다. `Alt` 키를 누른 채 끌면 사용자 지정 위치를 에셋에 저장합니다.
- `Settings`: 전이 요약 표시, 노드와 패널 크기, 격자 맞춤과 간격을 설정합니다. 프로젝트 공통 설정은 `Project Settings > SWUtils > State Machine Graph`에서 변경합니다.
- `Runtime`: 그래프 팩터리로 만든 상태 머신을 Play Mode 디버거에 자동 등록합니다. 문맥 게임 오브젝트를 선택하면 활성 상태, 실행 시간과 최근 전이 이력을 표시합니다.
- `단축키`: `Ctrl+C` 복사, `Ctrl+V` 붙여넣기, `Ctrl+D` 복제, `A` 전체 보기, `O` 원점 보기, `Space` 노드 검색을 지원합니다.

일반 상태는 초록색, `Any State`는 보라색, `Return State`는 청록색 카드로 구분됩니다. 복사한 노드 사이의 전이와 전이 설정도 함께 복사되며 같은 그래프 형식의 다른 에셋에도 붙여넣을 수 있습니다. 그래프 화면 위치와 확대 비율은 에셋별로, 편집기 배치는 사용자별로 저장됩니다. 자세한 설명은 `Documentation~/StateMachineGraph.ko.md`를 참고하세요.

그래프 에셋으로 런타임 상태 머신을 생성할 수 있습니다.

```csharp
SWStateMachine<Player> stateMachine =
    SWStateMachineGraphFactory.CreateLayered(graphAsset, player);

SWStackStateMachineGraphController<GameFlow> stackController =
    SWStateMachineGraphFactory.CreateStack(graphAsset, gameFlow);
```

조건 연결은 문맥 타입에 맞는 `SWStateMachineGraphCondition<TContext>`를 구현하고 연결선 상세 영역의 `조건 타입 선택`에서 선택합니다.

```csharp
public sealed class IsMovingCondition : SWStateMachineGraphCondition<Player>
{
    public override bool Evaluate(Player context)
    {
        return context.IsMoving;
    }
}
```

다중 계층 그래프 팩터리는 완성된 `SWStateMachine<TContext>`를 반환합니다. 스택 그래프 제어기는 `Tick`, `ExecuteCommand`, `SendMessage`, `Stop`을 제공하며 그래프에 지정된 상태 추가, 교체와 이전 상태 복귀 연결을 실행합니다.

상태 전환 콜백에서 다시 전환을 요청하면 현재 전환을 마친 뒤 요청 순서대로 실행합니다. 전환 중 호출한 `Pop`과 `ExecuteCommand`의 `true`는 요청 접수를 뜻합니다. 실행 결과와 상태는 전환 알림에서 확인합니다. 한 호출에서 연속 작업이 1,024회를 넘으면 순환 요청으로 중단합니다.

[상태 머신 제작과 전환 규칙](Documentation~/StateMachineGraph.ko.md)

<a id="runtime-behaviour"></a>

### Behaviour Tree

- `SWBehaviourTreeAsset`: 노드와 연결, Blackboard 기본값을 저장하고 `Running`, `Success`, `Failure`, `Aborted` 실행 결과를 관리합니다.
- `SWBehaviourTreeRunner`: 에셋의 독립 실행 복제본을 생성하고 게임 오브젝트의 생명주기에 맞춰 실행합니다.
- `SWBehaviourActionNode`: 실제 작업을 수행하며 자식을 가지지 않는 노드 기본 타입입니다.
- `SWBehaviourCompositeNode`: 여러 자식을 가지며 실행 순서를 정의하는 노드 기본 타입입니다.
- `SWBehaviourDecoratorNode`: 하나의 자식을 감싸 실행 결과나 실행 방식을 바꾸는 노드 기본 타입입니다.
- `SWBehaviourBlackboard`: 트리에서 공유하는 타입별 값을 키로 관리합니다.
- `SWBehaviourNodeProperty<T>`: 고정값 또는 같은 타입의 Blackboard 키를 노드 필드에 연결합니다.
- `SWBehaviourSubTreeNode`: 다른 Behaviour Tree 에셋을 현재 트리의 일부로 실행합니다.

#### 편집 방법

1. `Assets > Create > SWTools > Behaviour Tree`에서 에셋을 생성합니다.
2. 에셋 Inspector의 `Behaviour Tree 편집` 또는 `SWTools > Utils > Behaviour > Tree Editor`를 엽니다.
3. 빈 공간을 마우스 오른쪽 버튼으로 누르거나 `Create Node`를 눌러 Action, Composite, Decorator 노드를 검색합니다.
4. 부모의 `Out`에서 자식의 `In`으로 연결합니다. Composite는 여러 자식, Decorator는 하나의 자식, Action은 자식을 가질 수 없습니다.
5. 자식은 그래프의 왼쪽에서 오른쪽 순서로 실행됩니다. 노드를 가로로 이동하면 실행 순서도 자동 정렬됩니다.
6. 왼쪽 Blackboard에서 값 타입을 선택해 키를 추가하고 이름과 기본값을 편집합니다.
7. 오른쪽 Node Inspector에서 표시 이름, 설명, 노드별 값을 편집하고 `Set as Root`로 시작 노드를 지정합니다.
8. `SWBehaviourTreeRunner`에 에셋을 연결하면 활성화 시 독립 복제본을 실행합니다. Play Mode에서 해당 게임 오브젝트를 선택하면 Running은 노란색, Success는 초록색, Failure는 빨간색으로 표시됩니다.

#### 편집 및 실행 기능

- `Graph List`: Behaviour Tree 에셋을 검색하고 빠르게 전환하며 패널을 접을 수 있습니다.
- `Blackboard`: 기본 타입과 사용자 정의 타입의 키, 기본값과 Runner별 재정의 값을 관리합니다.
- `Node Inspector`: 표시 이름, 설명, 노드별 값과 시작 노드를 편집합니다.
- `Auto Layout`: 시작 노드를 기준으로 트리 구조를 자동 정렬합니다.
- `SubTree`: 다른 트리를 선택해 재사용하고 부모 Blackboard의 공유 여부를 설정합니다.
- `New Script`: `Editor/Behaviour/Templates`의 수정 가능한 텍스트 템플릿으로 Action, Composite와 Decorator 노드 스크립트를 생성합니다.
- `Settings`: 노드 크기, 간격, 패널 크기와 실행 상태 갱신 간격을 설정합니다. 프로젝트 공통 설정은 `Project Settings > SWUtils > Behaviour Tree`에서 변경합니다.
- `Runtime Debug`: Runner, 노드 상태와 Blackboard 값을 확인합니다. 실행 중은 노란색, 성공은 초록색, 실패는 빨간색, 중단은 회색으로 표시됩니다.
- `단축키`: 복사, 붙여넣기, 복제, 키보드 탐색, 전체 보기, 원점 보기와 노드 검색을 지원합니다.

`Set Property`와 `Compare Property`는 기본 타입과 사용자 정의 Blackboard 값을 처리합니다. `SWBehaviourTreeRunner`는 외부 `MonoBehaviour`에서 사용하는 `GetBlackboardValue`, `SetBlackboardValue`, `FindBlackboardKey`를 제공하며 사용자 정의 키도 Runner별 재정의에서 선택할 수 있습니다. 자세한 설명은 `Documentation~/BehaviourTree.ko.md`를 참고하세요.

#### 사용자 정의 분류

- `SWStateMachineNodeCategory`: 상태와 전이 조건을 슬래시 경로 기반 생성 메뉴 카테고리로 분류합니다.
- `SWBehaviourNodeCategory`: Behaviour 노드를 슬래시 경로 기반 생성 메뉴 카테고리로 분류합니다.

예를 들어 `SWStateMachineNodeCategory("Combat/Movement")` 또는 `SWBehaviourNodeCategory("Combat/Actions")`처럼 경로를 지정할 수 있습니다.

사용자 노드는 다음처럼 추가합니다. 별도 등록 없이 노드 검색 창에 나타납니다.

```csharp
using SW.BehaviourTree;

[Serializable]
public sealed class HasTargetNode : SWBehaviourActionNode
{
    protected override SWBehaviourStatus OnUpdate(
        SWBehaviourContext context,
        SWBehaviourTreeAsset tree)
    {
        return context.Blackboard.GetValue<GameObject>("Target") != null
            ? SWBehaviourStatus.Success
            : SWBehaviourStatus.Failure;
    }
}
```

블랙보드의 참조 값은 `null`로 지울 수 있습니다. 키 이름을 바꾸면 조회 캐시도 갱신됩니다. `Rename(identifier, newName)`은 빈 이름과 중복 이름을 검사하며 노드에 저장된 문자열 참조는 별도로 갱신해야 합니다.

하위 트리는 실행 복제 전에 순환 연결과 64단계 중첩 한도를 검사합니다. 오류가 있으면 인스펙터에 표시하고 `CreateRuntimeInstance`가 `null`을 반환합니다. 코드에서는 `ValidateSubTrees(out error)`로 먼저 검사할 수 있습니다.

<a id="runtime-utilities"></a>

### 유틸리티

- `SWAmountFormat`, `SWAmountFormatProfile`: 큰 숫자의 단위와 소수점 표시를 관리합니다.
- `SWAudioLibrary`, `SWAudioManager`: 음악과 효과음 등록, 재생과 볼륨을 관리합니다.
- `SWButtonExtension`: 연타 방지, 길게 누르기, 반복 실행과 클릭 효과음을 제공합니다.
- `SWCooldown`, `SWTimer`, `SWRefillTimer`, `SWTime`: 시간과 타이머 기능을 제공합니다.
- `SWEventBus`: 타입 기반 이벤트 발행과 구독을 제공합니다.
- `SWRandom`, `SWShuffleBag`: 가중치 선택, 섞기와 반복 없는 무작위 선택을 제공합니다.
- `SWSceneLoader`: 씬 로딩 진행률과 전환 상태를 관리합니다.
- `SWSingleton`, `SWSingletonScene`: 전역 또는 씬 단위 싱글톤을 제공합니다.
- `SWFactory`, `SWExtension`, `SWString`, `SWUtility`: 생성, 확장 메서드, 문자열과 공통 기능을 제공합니다.
- `SWRectDummy`: 메시 없는 사용자 인터페이스 레이캐스트 영역을 제공합니다.
- `SWTriggerDispatcher`: 2차원 및 3차원 트리거 이벤트를 외부로 전달합니다.
- `SWVibration`: 플랫폼 진동 기능을 제공합니다.

#### 오디오

`SWAudioLibrary`에 키와 클립을 등록하고 `SWAudioManager`에서 음악·효과음을 재생합니다. 음악 페이드, 효과음 소스 재사용, 클립별 재생 간격 제한과 전체·음악·효과음 음량을 관리합니다. 전체·효과음 음량 변경은 재생 중인 효과음에도 적용하며 개별 재생 요청의 배율을 유지합니다.

#### 시간과 타이머

`SWTimer`는 `Tick`을 호출하여 갱신하고 시작·일시 정지·재개·반복·시간 기준을 지정합니다. 길이가 0이거나 실행 중 길이를 경과 시간 이하로 줄이면 다음 갱신에서 완료를 알립니다. `SWCooldown.TryUse`는 대기 시간이 지난 경우에만 사용을 허용합니다. `SWRefillTimer`는 사용 횟수와 오프라인 경과에 따른 회복을 저장하며 `SWTime`은 시간 표시와 날짜 변환을 제공합니다.

#### 이벤트

`SWEventBus.Subscribe<T>`, `Publish<T>`, `Unsubscribe<T>`로 같은 이벤트 타입을 주고받습니다. 리스너 수, 발행 횟수, 마지막 발행과 데이터 요약은 `SWEventBusEventSnapshot` 및 디버그 창에서 확인합니다. `IsDiagnosticsEnabled`는 진단 기록을, `IsLogOutputEnabled`는 로그 출력을 제어합니다. 진단용 문자열 변환 실패가 이벤트 발행자에게 전파되지는 않습니다.

#### 씬 로딩

`SWSceneLoader`는 이름·빌드 번호로 씬을 읽고 추가 로드, 언로드, 현재 씬 재로드와 활성 씬 설정을 제공합니다. 진행률·시작·완료·실패 알림을 받을 수 있습니다. `TryCancelCurrentLoad`는 다음 프레임의 엔진 작업 시작 전까지만 취소를 허용합니다. 활성화를 보류한 경우 `AllowSceneActivation = true`로 해제합니다. 완료 콜백에서 다음 씬을 요청할 수 있습니다.

#### 숫자 표시와 무작위 선택

`SWAmountFormatProfile`에 숫자 단위·소수점·반올림 방식을 저장하고 `SWAmountFormat`으로 표시합니다. `SWRandom`은 가중치 선택과 섞기를, `SWShuffleBag<T>`는 준비한 항목을 소진할 때까지 반복하지 않는 선택을 제공합니다. Amount Format Window와 Random Simulator에서 설정 결과를 확인할 수 있습니다.

#### 입력과 공통 도구

`SWButtonExtension`은 연타 방지, 길게 누르기, 누르는 동안 반복과 효과음을 구성합니다. 키보드·게임패드의 확정 입력도 지원합니다. `SWRectDummy`는 이미지를 그리지 않고 입력 영역을 만들며 `SWTriggerDispatcher`는 물리 트리거 진입·유지·종료를 이벤트로 전달합니다.

`SWSingleton<T>`와 `SWSingletonScene<T>`는 각각 전역·씬 단위의 컴포넌트 조회를 제공합니다. `SWFactory`는 오브젝트 생성을, `SWExtension`·`SWString`·`SWUtility`는 공통 계산·확장 함수·문자열 처리를 제공합니다. `SWUtility.SetGaugeText`는 현재 값과 최대 값을 텍스트로 표시하며 기존 `SetGauge`도 같은 동작을 유지합니다. `SWVibration`은 지원 플랫폼의 진동을 호출합니다.

<a id="editor-tools"></a>

## 에디터 기능

### SWUtils Data Editor

`SWTools > SWUtils Data Editor`에서 ScriptableObject를 검색하고 생성·복제·이름 변경·분류·즐겨찾기와 다중 탭으로 관리합니다. 인스펙터를 잠가 편집 대상을 유지하거나 참조 에셋을 다른 탭에서 열 수 있습니다.

1. `Settings > Categories`에서 분류를 추가하거나 이름·순서를 변경합니다. 왼쪽 목록과 유형 선택창은 이 설정만 사용하며 삭제한 분류는 재검색으로 복구되지 않습니다.
2. `Configure asset types`에서 표시할 유형을 활성화하고 기본 분류를 선택합니다. 변경은 자동 저장되며 `Back to settings`로 설정에, `Done`으로 탐색기에 돌아갑니다. 최초 설정에서는 `Start browsing`을 표시합니다.
3. 생성 메뉴와 필터는 `SWUtils / Other assets → 카테고리 → 유형` 순서입니다. 검색어, 그룹 펼침 상태와 스크롤 위치를 복원합니다.

기본 분류는 `SWUtility`, `SWSamples`, `SWSkillTree`, `SWStat`, `SWBehaviour Tree`, `SWStateMachine`, `SWQuest`, `Other`입니다. 최초 한 번만 추가되며 이후 수정과 삭제를 유지합니다. SWUtils의 Samples 폴더에 있는 에셋은 유형 기본값보다 우선해 `SWSamples`에 표시합니다. 패키지 설치 경로와 가져온 샘플 경로도 인식하며, 해당 유형은 활성화되어 있어야 합니다.

설정은 프로젝트의 `ProjectSettings/SWUtilsEditorSettings.asset`에 저장합니다. 편집기 창과 내장 인스펙터는 공통 테마를 사용하며 어트리뷰트에 직접 지정한 그룹 색상은 유지합니다. Unity 기본 Inspector 창에는 기존 그룹·버튼·입력 필드 스타일을 적용합니다. [분류 추가와 확장 방법](Documentation~/SWUtilsEditor.ko.md)을 확인하세요.

### 인스펙터

Runtime 어트리뷰트에 대응하는 프로퍼티 서랍과 `SWMonoBehaviour`, `SWScriptableObject` 사용자 지정 인스펙터를 제공합니다.

### 에디터 창

디버깅 도구는 `SWTools/Debug`, 일반 도구는 `SWTools/Utils` 메뉴에서 엽니다.

- `SWTools/SWUtils Data Editor`: ScriptableObject 탐색, 분류, 생성, 유형 필터와 인스펙터 탭을 제공합니다.
- `SWTools/Debug/Build/Build Report Viewer`: 빌드 결과와 포함된 에셋 크기를 확인합니다.
- `SWTools/Debug/Console/Debug Console Settings`: 콘솔 입력, 성능 오버레이와 디버그 심볼을 설정합니다.
- `SWTools/Debug/Event/EventBus Debugger Window`: 이벤트 타입별 구독자와 발행 기록을 확인합니다.
- `SWTools/Debug/Input/Input Debugger Window`: EventSystem, 포인터, 레이캐스트와 입력 상태를 확인합니다.
- `SWTools/Debug/PlayerPrefs/PlayerPrefs Viewer`: SWUtils 저장값과 일반 Unity 저장값을 조회·수정·삭제합니다.
- `SWTools/Debug/Pool/Pool Monitor Window`: 프리팹별 풀 생성·활성·대기·반납 상태를 확인합니다.
- `SWTools/Debug/Test/Test Tools Window`: 씬 이동과 실행 중 테스트 도구를 제공합니다.
- `SWTools/Utils/Asset/Quick Asset Palette`: 자주 사용하는 에셋·폴더를 등록하고 열기·선택·생성을 실행합니다.
- `SWTools/Utils/Behaviour/Tree Editor`: 행동 트리의 노드, 블랙보드와 하위 트리를 편집합니다.
- `SWTools/Utils/Asset/Reference Finder`: 에셋 참조, 의존성과 미사용 후보를 검색합니다.
- `SWTools/Utils/Asset/TMP Font Asset Manager`: TextMeshPro 글꼴 연결·교체와 메모리 사용을 확인합니다.
- `SWTools/Utils/Data/Amount Format Window`: 숫자 단위·소수점·반올림 프리셋을 만들고 결과를 확인합니다.
- `SWTools/Utils/Data/Excel Table Importer`: 표를 미리 확인하고 데이터 에셋에 적용합니다.
- `SWTools/Utils/Data/Quest System Editor`: 퀘스트·업적·조건·보상과 데이터베이스를 관리합니다.
- `SWTools/Utils/Data/Localization Tools`: 번역 컬렉션의 언어별 내보내기와 TSV 가져오기를 처리합니다.
- `SWTools/Utils/Data/Skill Tree Editor`: 스킬트리의 노드, 선행 연결, 공개 조건과 저장 위치를 편집합니다.
- `SWTools/Utils/Data/Stat System Editor`: `SWCategory`, `SWStat` 같은 `SWIdentifiedObject` 에셋을 생성, 편집, 정렬, 이름 변경하고 목록 아이콘과 표시 크기를 조정합니다.
- `SWTools/Utils/Hierarchy/Hierarchy Tools`: 하이어라키의 배경색, 아이콘과 표시 방식을 설정합니다.
- `SWTools/Utils/Project/Define Symbol Window`: 빌드 대상별 스크립트 정의 심볼을 관리합니다.
- `SWTools/Utils/Project/PlayerPrefs Salt Settings`: SWPlayerPrefs 암호화 솔트 설정 에셋을 만들고 편집합니다.
- `SWTools/Utils/Screen/Resolution Window`: 화면 크기와 비율을 확인합니다.
- `SWTools/Utils/Simulation/Random Simulator`: 가중치·섞기 기반 선택 결과를 시뮬레이션합니다.
- `SWTools/Utils/State Machine/Graph Editor`: 다중 계층·스택 상태 머신의 노드와 전이를 편집합니다.

#### `SWTools/Debug/Console/Debug Console Settings`

디버그 콘솔 설정 창은 탭으로 필요한 항목만 보여줍니다.

- `상태`: Resources 설정 에셋을 연결하거나 생성하고 현재 빌드 타겟의 `SW_DEBUG_MODE`를 추가 또는 제거합니다.
- `입력`: 자동 생성, 열기 키, 조합키, 터치 개수, 선택적 Input System 확인을 설정합니다.
- `오버레이`: 시작 시 표시, 표시 위치, 크기 배율, 갱신 간격, 표시 항목, FPS 경고 색상을 설정합니다.
- `플레이`: 플레이 중 콘솔 열기와 닫기, 오버레이 토글, 오버레이 기록 초기화를 실행합니다.

### 엑셀 표 가져오기

`SWTableSheet`가 적용된 리스트, 배열 또는 일반 클래스 필드에 탭으로 구분된 데이터를 적용합니다.

1. 대상 `ScriptableObject` 필드에 `SWTableSheet`를 추가합니다.
2. 행 데이터 타입의 필드에 `SWTable`을 추가합니다.
3. `SWTools > Utils > Data > Excel Table Importer`를 엽니다.
4. 표 데이터를 붙여 넣고 미리보기 후 적용합니다.

리스트·배열은 모든 행을 받고 일반 클래스 필드는 첫 행을 받습니다. 일반 클래스에는 필드명과 값을 세로로 나열하는 입력도 지원합니다. 필수 열 누락, 중복 헤더, 잘못된 논리값과 닫히지 않은 따옴표가 있으면 적용을 거절합니다. 인용된 셀의 탭·줄바꿈·따옴표를 보존합니다.

### 번역 관리

`SWTools > Utils > Data > Localization Tools`에서 Localization 문자열 테이블을 관리합니다. 선택한 언어를 CSV·TSV·JSON으로 내보내고 TSV를 미리 본 뒤 가져올 수 있습니다. 새 컬렉션 생성·기존 컬렉션 업데이트, 키 접두사, Smart String 사용과 빈 항목 내보내기를 설정하며 인용된 번역의 탭·줄바꿈·따옴표를 보존합니다.

기존 컬렉션을 업데이트하면 입력에 없는 키는 삭제됩니다. **모든 기존 키 삭제 후 교체**를 켜면 기존 키를 먼저 전부 지우고 새 입력으로 구성합니다. 적용 전에 미리보기와 선택한 컬렉션을 확인합니다.

### 에셋과 글꼴 도구

Quick Asset Palette는 자주 사용하는 에셋·폴더를 등록해 열기·선택·생성을 지원합니다. Reference Finder는 선택 에셋의 참조·의존성과 미사용 후보를 찾습니다. TMP Font Asset Manager는 글꼴 연결과 교체, 아틀라스 메모리, 글리프·문자 수, 대체 글꼴 연결과 머티리얼 비용을 확인합니다.

### 진단 창

EventBus Debugger는 이벤트 리스너·발행 기록을, Pool Monitor는 풀별 생성·활성·대기·반납 수와 지연 반납을 표시합니다. PlayerPrefs Viewer는 암호화된 SWUtils 데이터와 일반 Unity 데이터를 별도 탭에서 조회·수정·삭제합니다. Build Report Viewer는 빌드 결과와 포함된 에셋 크기를, Input Debugger는 EventSystem·포인터·레이캐스트 상태를 보여줍니다.

### 편집기 확장

공유 인스펙터는 `SWMonoBehaviour`와 `SWScriptableObject`의 그룹·버튼·조건 표시·다시 그리기를 처리합니다. `Editor/StyleSheet`는 편집기 UI Toolkit 스타일을, `Editor/Util`은 창·인스펙터에서 사용하는 드래그, 아이콘, 설정 저장, 스타일 캐시와 에셋 선택 도구를 제공합니다.

### 하이어라키 도구

게임 오브젝트의 배경색, 아이콘, 활성 상태와 누락된 컴포넌트 경고를 하이어라키에 표시합니다.

표시 설정은 `SWHierarchyToolsWindow`에서 편집하고 프로젝트의 편집기 도구에 적용합니다.

## 샘플

- `SWAttributeExample`: 인스펙터 어트리뷰트 사용 예제
- `SWSubClassSelectorExample`: `SerializeReference` 구현 타입 선택 예제
- `SWGraphAssetsExample`: Behaviour Tree, 다중 계층 상태 머신, 스택 상태 머신과 사용자 정의 노드 카테고리를 한 파일에서 보여주는 통합 예제
- `SWQuestExample`, `SWQuestScoreRewardExample`: 퀘스트 초기화, 진행 보고, 완료·업적 이벤트와 프로젝트별 보상 구현 예제
- `SWSkillTreeExample`, `SWSkillTreeNode` 프리팹과 `MiningSkillTree`: 81개 노드의 화면 탐색, 구매·환불, 저장·복원과 배치 편집 예제
- `SWExampleBehaviourTree`: 실행 가능한 Behaviour Tree 예제 에셋
- `SWExampleStateMachine`: 실행 가능한 다중 계층 State Machine 예제 에셋
- `SWExampleStackStateMachine`: Gameplay, Pause와 Return State를 사용하는 Stack State Machine 예제 에셋
- `SWPool`, `SWPoolRegistry`, `SWPopupManager`, `SWStats` 프리팹

예제는 프로젝트 창의 `Packages > SWUtils > Samples` 폴더에서 바로 확인할 수 있습니다.

## 조립체 정의

- `SWUtils.Runtime`: 런타임 코드
- `SWUtils.Editor`: 에디터 코드
- `SWUtils.Samples`: 샘플 코드
- `SWUtils.SkillTree.Samples.Editor`: 편집기 전용 스킬트리 예제 생성 코드
- `SWUtils.SkillTree.Tests`: 스킬트리와 실행 안정성의 편집 모드 테스트

스크립트 파일을 이동하거나 이름을 변경할 때는 Unity 메타 식별자를 유지해야 기존 씬과 프리팹 참조가 보존됩니다.
