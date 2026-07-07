# SWUtils

[English](README.md) | [한국어](README.ko.md)

SWUtils는 Unity 프로젝트에서 반복적으로 사용하는 런타임 기능과 에디터 도구를 모은 패키지입니다.

## Git 주소로 설치

Unity Package Manager에서 다음 순서로 설치합니다.

1. Unity 메뉴에서 `Window > Package Manager`를 엽니다.
2. 왼쪽 위의 `+` 버튼을 누릅니다.
3. `Add package from git URL...`을 선택합니다.
4. 다음 주소를 입력합니다.

```text
https://github.com/LAVINSE/SWUtils.git#v1.0.13
```

특정 브랜치나 태그를 설치하려면 주소 뒤에 `#브랜치이름` 또는 `#태그이름`을 붙입니다.

## 의존성

다음 Unity 패키지는 `package.json`을 통해 자동으로 설치됩니다.

- Input System
- Localization
- TextMeshPro
- Unity UI

다음 외부 라이브러리는 사용하는 기능에 따라 별도로 설치해야 합니다.

- DOTween: 팝업 표시 및 숨김 연출
- Google Play Games: Android 클라우드 저장
- Steamworks.NET: 데스크톱 클라우드 저장

클라우드 저장 기능에 외부 라이브러리를 사용할 때는 다음 정의 심볼을 추가합니다.

- `SW_GOOGLEPLAY_ENABLE`: Android에서 Google Play Games 저장 기능을 활성화합니다.
- `SW_STEAMWORKS_NET`: 데스크톱에서 Steamworks.NET 저장 기능을 활성화합니다.

## 빠른 시작

1. 예제가 필요하면 Unity Package Manager의 `Samples` 탭에서 샘플을 가져옵니다.
2. `SWMonoBehaviour`와 `SWScriptableObject`를 사용할 때 `using SW.Base;`를 추가합니다.
3. 인스펙터 어트리뷰트를 사용할 때 `using SW.Attributes;`를 추가합니다.
4. 데이터, 팝업, 해상도, 유틸리티 기능은 각각 `SW.Data`, `SW.Popup`, `SW.ScreenResolution`, `SW.Util`을 사용합니다.
5. 코루틴 실행기는 `using SW.Coroutines;`을 사용합니다.
6. 조립체 정의 파일을 사용하는 프로젝트는 `SWUtils.Runtime` 참조를 추가합니다.

관리자 컴포넌트는 기본적으로 씬이 소유합니다. 씬 전환 후에도 필요한 관리자는 시작 씬에 배치하고 유지되도록 구성합니다.

## 네임스페이스 구조

Runtime과 Editor 코드는 기능별 폴더와 같은 네임스페이스를 사용합니다.

| 폴더 | 네임스페이스 |
| --- | --- |
| `Runtime/Attribute` | `SW.Attributes` |
| `Runtime/Base` | `SW.Base` |
| `Runtime/Coroutine` | `SW.Coroutines` |
| `Runtime/Data` | `SW.Data` |
| `Runtime/Debug` | `SW.Debugging` |
| `Runtime/Pooling` | `SW.Pooling` |
| `Runtime/Popup` | `SW.Popup` |
| `Runtime/Resolution` | `SW.ScreenResolution` |
| `Runtime/Stat` | `SW.Stat` |
| `Runtime/Util` | `SW.Util` |
| `Editor/Attribute` | `SW.EditorTools.Attributes` |
| `Editor/<기능>` | `SW.EditorTools.<기능>` |

## 런타임 기능

### 어트리뷰트

- `SWButton`: 인스펙터 버튼으로 메서드를 실행합니다.
- `SWButtonBar`: 여러 메서드 실행 버튼을 한 줄에 표시합니다.
- `SWCondition`: Boolean 필드 값에 따라 필드를 표시하거나 숨깁니다.
- `SWEnumCondition`: 열거형 값에 따라 필드를 표시하거나 숨깁니다.
- `SWDropdown`: 미리 정의한 값을 드롭다운으로 표시합니다.
- `SWGroup`: 인스펙터 필드를 접을 수 있는 그룹으로 묶습니다.
- `SWReadOnly`: 필드를 읽기 전용으로 표시합니다.
- `SWSubClassSelector`: `SerializeReference` 필드의 구현 타입을 검색하여 선택합니다.
- `SWTable`, `SWTableSheet`: 표 데이터를 직렬화 필드에 연결합니다.
- `SWCommand`: 메서드를 런타임 디버그 콘솔 명령으로 노출합니다.

### 기본 타입

- `SWMonoBehaviour`: SWUtils 인스펙터 기능을 사용하는 컴포넌트 기본 클래스입니다.
- `SWScriptableObject`: 같은 인스펙터 기능을 사용하는 데이터 에셋 기본 클래스입니다.
- `SWIdentifiedObject`: 식별자, 코드명, 표시명, 설명과 카테고리를 가진 데이터 에셋입니다.
- `SWIODatabase`: `SWIdentifiedObject` 목록을 관리하고 빠르게 조회합니다.
- `SWCategory`: 데이터 에셋 분류에 사용합니다.

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

### 데이터와 저장

- `SWEncrypt<T>`: `SWPlayerPrefs`를 사용하는 암호화 값 래퍼입니다.
- `SWPlayerPrefs`: 키와 값을 암호화하여 Unity PlayerPrefs에 저장합니다.
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

### 디버그

- `SWDebugConsole`: 로그 확인, 명령 실행과 상태 감시를 제공하는 런타임 콘솔입니다.
- `SWCommand`: 콘솔에서 실행할 메서드를 등록합니다.
- `SWLog`: `SW_DEBUG_MODE` 정의 심볼이 있을 때 로그를 출력합니다.

### 오브젝트 풀

- `IPool`, `IPoolable`: 풀 구현과 풀링 대상의 계약입니다.
- `SWPool`: 프리팹별 생성, 예열, 반환, 지연 반환과 유휴 풀 정리를 관리합니다.
- `SWPoolCatalog`: 풀 이름, 그룹과 예열 수량을 데이터로 관리합니다.
- `SWPoolRegistry`: 카탈로그를 실제 풀에 등록합니다.
- `SWPoolSnapshot`: 에디터 모니터에서 사용하는 읽기 전용 상태입니다.

### 팝업

- `SWPopupBase`: 팝업 생명주기와 표시 상태를 관리합니다.
- `SWPopupManager`: 팝업 생성, 표시, 숨김, 캐시와 카탈로그 조회를 관리합니다.
- `SWPopupCatalog`: 문자열 키와 팝업 프리팹을 연결합니다.
- `SWPopupShowEffect`, `SWPopupHideEffect`: 표시 및 숨김 연출 기본 타입입니다.
- `SWPopupScaleShowEffect`, `SWPopupScaleHideEffect`: 크기 변경 기반 기본 연출입니다.
- `SWPopupEffectHandle`: 실행 중인 팝업 연출을 제어합니다.

### 해상도

- `SWCanvasResolution`: 화면 비율에 따라 CanvasScaler 설정을 조정합니다.
- `SWSafeArea`: 노치와 화면 안전 구역에 맞춰 사용자 인터페이스를 배치합니다.
- `SWResolution`: 화면 크기, 비율, 좌표 변환과 카메라 계산을 제공합니다.

### 능력치

- `SWStat`: 기본값과 보너스 값을 조합하는 능력치 데이터입니다.
- `SWStatOverride`: 개체별 기본값 재정의 설정입니다.
- `SWStats`: 게임 오브젝트의 런타임 능력치 목록을 관리합니다.
- `SWStatScaleFloat`: 능력치 비율을 적용한 값을 계산합니다.

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

## 에디터 기능

### 인스펙터

Runtime 어트리뷰트에 대응하는 프로퍼티 서랍과 `SWMonoBehaviour`, `SWScriptableObject` 사용자 지정 인스펙터를 제공합니다.

### 에디터 창

디버깅 도구는 `SWTools/Debug`, 일반 도구는 `SWTools/Utils` 메뉴에서 엽니다.

- Build Report Viewer
- EventBus Debugger Window
- Input Debugger Window
- PlayerPrefs Viewer
- Pool Monitor Window
- Test Tools Window
- Define Symbol Window
- Excel Table Importer
- Hierarchy Tools
- Localization Tools
- Amount Format Window
- PlayerPrefs Salt Settings
- Quick Asset Palette
- Random Simulator
- Reference Finder
- Stat System Window
- TextMeshPro Font Asset Manager
- Resolution Window

### 엑셀 표 가져오기

`SWTableSheet`가 적용된 리스트, 배열 또는 일반 클래스 필드에 탭으로 구분된 데이터를 적용합니다.

1. 대상 `ScriptableObject` 필드에 `SWTableSheet`를 추가합니다.
2. 행 데이터 타입의 필드에 `SWTable`을 추가합니다.
3. `SWTools > Utils > Excel Table Importer`를 엽니다.
4. 표 데이터를 붙여 넣고 미리보기 후 적용합니다.

### 하이어라키 도구

게임 오브젝트의 배경색, 아이콘, 활성 상태와 누락된 컴포넌트 경고를 하이어라키에 표시합니다.

## 샘플

- `SWAttributeExample`: 인스펙터 어트리뷰트 사용 예제
- `SWSubClassSelectorExample`: `SerializeReference` 구현 타입 선택 예제
- `SWPool`, `SWPoolRegistry`, `SWPopupManager`, `SWStats` 프리팹

Unity Package Manager의 `Samples` 탭에서 샘플을 가져올 수 있습니다.

## 조립체 정의

- `SWUtils.Runtime`: 런타임 코드
- `SWUtils.Editor`: 에디터 코드
- `SWUtils.Samples`: 샘플 코드

스크립트 파일을 이동하거나 이름을 변경할 때는 Unity 메타 식별자를 유지해야 기존 씬과 프리팹 참조가 보존됩니다.
