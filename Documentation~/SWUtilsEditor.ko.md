# SWUtils Data Editor

`SWTools > SWUtils Data Editor`에서 엽니다. 처음에는 `Search folders`에서 탐색 폴더를 확인하고 관리할 유형을 선택한 뒤 `Start browsing`을 누릅니다. 기존 도구의 기능은 유지하며 공통 테마와 탐색 배치를 적용합니다.

## 탐색 폴더와 로딩

1. 최초 설정의 `Search folders` 또는 `Settings > Asset browser > Search folders`를 엽니다.
2. SWUtils에 포함된 ScriptableObject 에셋 폴더인 `Samples/Data`가 기본값입니다. 하위 `SkillTree` 폴더도 포함합니다. 패키지로 설치하면 `Packages/com.swtools.swutils/Samples/Data`를 사용하며, Assets 안에 설치한 경우에는 해당 위치를 사용합니다.
3. 추가할 `Assets/...` 또는 `Packages/...` 경로를 입력하거나 옆의 폴더 선택 필드에 Project 창의 폴더를 지정한 뒤 `Add`를 누릅니다. 여러 폴더를 등록할 수 있고 각 경로를 수정하거나 `Remove`로 제거할 수 있습니다.
4. 일부 하위 폴더는 `Excluded folders`에 등록해 제외합니다. 변경 내용은 프로젝트 설정에 자동 저장됩니다. `Add SWUtils data folders`는 사용자 폴더를 유지하면서 기본 데이터 폴더를 다시 추가합니다.

등록한 폴더와 하위 폴더의 활성 유형만 검색합니다. 중복 경로와 상위 폴더에 이미 포함된 경로는 검색 시 정리하며, 제외 폴더 안의 파일은 불러오기 전에 거릅니다. 비어 있거나 사라진 경로를 전체 프로젝트 검색으로 대체하지 않습니다. 대형 에셋 패키지가 있는 프로젝트에서는 `Assets` 전체 대신 관리할 데이터 폴더를 지정하세요.

1.4.1 이하의 설정을 처음 불러오면 기본 탐색 폴더를 추가하고 기존 유형·분류 설정은 유지합니다. 이전에 보이던 프로젝트 에셋이나 다른 위치로 가져온 샘플은 해당 폴더를 탐색 범위에 추가해야 표시됩니다. 탐색 범위 밖에 새로 만든 에셋은 파일로 저장되며, 폴더를 추가한 뒤 목록에서 볼 수 있습니다.

유형 설정을 열 때에는 에셋 파일을 미리 불러오지 않습니다. 에셋 수를 아직 조회하지 않은 유형은 `Not scanned`로 표시합니다. 검색은 편집기 갱신마다 로딩을 나누고 진행 상태를 표시하며, `취소` 또는 로딩 실패 시 이전 목록과 열린 에셋 상태를 유지합니다. 완료한 검색 결과만 목록에 반영합니다. 격자와 목록은 화면에 보이는 행만 생성하고 스크롤에 따라 교체합니다.

Unity의 단일 검색 호출이나 단일 파일 로딩이 끝나기 전에는 취소를 처리할 수 없습니다. 무거운 데이터 파일 자체가 탐색 범위에 있다면 그 파일을 불러오는 동안 대기가 발생할 수 있습니다.

## 분석 기준

2026-09-14에 [제품 소개](https://fullscreen.no/editor-pro-2/), [시작 안내](https://fullscreen.no/docs/editor-pro-2/getting-started/), [기능](https://fullscreen.no/docs/editor-pro-2/features/), [확장 문서](https://fullscreen.no/docs/editor-pro-2/api/), [문제 해결](https://fullscreen.no/docs/editor-pro-2/troubleshooting/), [Asset Store](https://assetstore.unity.com/packages/tools/utilities/editor-pro-2-398680)를 확인했습니다. 공개된 Window, Grid, List 이미지도 비교했습니다. 원본 패키지 없이 공개 자료를 기준으로 독립 구현했습니다.

공식 기능을 아래 항목으로 나누어 구현했습니다.

| 영역 | 대응 기능 |
| --- | --- |
| 탐색 | 지정 폴더 검색, 유형 발견, 이름·유형·확장 검색어 검색, 복수 유형 필터, 정렬, 화면에 보이는 격자·목록 행만 생성 |
| 구성 | 그룹별 유형 선택, 기본 분류, 개별 에셋 분류, 즐겨찾기, 아이콘, 분류 순서 |
| 작업 | 생성, 복제, 이름 변경, 이동, 삭제, Project 창에서 위치 확인 |
| 선택 | 일반·Control·Shift 선택, 다중 탭, 탭 닫기, 인스펙터 잠금, 참조 탐색 |
| 상태 | 검색어, 분류, 탭, 활성 에셋, 크기, 스크롤 위치 복원 |
| 정보 | 분류별 에셋 수, 유형 수, 최빈 유형, 유형 분포 |
| 설정 | 여러 탐색 폴더, 제외 폴더, 보기 방식, 카드 크기 72–156, 탭 표시, 배치·전체 초기화 |

공식 확장 문서는 분류·초기화·생성 흐름·아이콘·작업·배지·검증·검색·유형 정책·인스펙터 탭·머리글·부가 정보 및 이벤트를 정의합니다. SWUtils에서는 `SW.EditorTools.Workspace`의 `SWEditorRegistry`와 `SWEditorEvents`로 제공합니다. 식별자가 같은 등록은 교체하고, 부가 정보 공급자는 추가합니다. 가까운 기본 유형과 우선순위에 따라 확장을 선택하고 콜백 예외를 격리합니다.

자동 업데이트 확인은 사용자 결정에 따라 제외했습니다.

## 디자인과 기존 화면 적용

공개 이미지에서 관찰한 기본값입니다. 원본 스타일 소스나 공개되지 않은 상호작용을 검증한 값은 아닙니다.

| 용도 | 값 |
| --- | --- |
| 편집기 창 내장 인스펙터 배경 | `#13161A` |
| 탐색 영역 | `#1B1F24` |
| 왼쪽 탐색 | `#222831` |
| 도구 모음 | `#1F242B` |
| 카드 | `#272D35` |
| 테두리 | `#424E5B` |
| 본문 | `#D5DFE5` |
| 보조 글자 | `#8DA6B4` |
| 선택 배경 | `#2A5B89` |
| 강조 | `#80BFFF` |
| 탐색 패널·도구 모음 | 246·55 픽셀 |

실제 적용에서는 가독성을 위해 본문을 `#E7EEF5`, 보조 글자를 `#A8B9C8`, 입력 영역을 `#242C35`로 조정했습니다. 작업 공간 안의 어트리뷰트는 그룹 제목과 버튼 간격을 통일하고 중첩 여백을 줄였습니다. 기본 그룹 강조색은 파란색으로 맞추며, 어트리뷰트에 직접 지정한 색상은 유지합니다.

`Editor/Theme/SWEditorTheme.uss`와 `SWEditorTheme.cs`가 편집기 창의 공통 디자인을 담당합니다. UI Toolkit은 창의 루트에서 스타일을 적용하고, IMGUI는 창 또는 내장 인스펙터를 그리는 범위를 `SWEditorThemeScope`로 감싼 뒤 원래 스타일을 복원합니다. 공통 인스펙터와 속성 편집기는 스스로 테마를 적용하지 않으므로 Unity 기본 Inspector 창에서는 기존 SWUtils 그룹·버튼·입력 필드 스타일을 유지합니다.

기존 창 23개, 커스텀 인스펙터 7개, 속성 편집기 소스 10개를 점검 대상으로 삼았습니다. 스탯과 퀘스트 창은 분류–목록–편집 배치, 그래프는 목록–캔버스–인스펙터 배치입니다. 일반 도구는 왼쪽 기능 탐색과 작업 영역을 사용합니다. 620픽셀보다 좁으면 왼쪽 탐색을 접고 기존 탭을 표시합니다. 그래프의 실행·성공·실패 등 의미 있는 상태 색상은 유지합니다.

## 사용 방법

- 클릭하면 현재 선택을 교체하고, Control을 누르면 탭에 추가합니다. Shift로 보이는 범위를 선택합니다.
- 에셋을 두 번 클릭하면 Project 창에서 위치를 표시합니다. 탭을 두 번 클릭하면 해당 분류로 이동합니다.
- 열린 에셋을 드래그하면 함께 열린 에셋들도 분류에 배정합니다. 즐겨찾기 드롭도 지원합니다.
- 인스펙터 이름 필드는 실제 파일 이름을 바꿉니다. 삭제는 확인 대화상자를 표시합니다.
- 설정의 분류 손잡이를 드래그하면 순서를 바꿉니다. Edit에서 이름과 Texture2D 아이콘을 지정합니다.
- 폴더 제외는 Assets 하위 폴더에만 적용합니다. 파일은 삭제하지 않습니다.

설정은 `ProjectSettings/SWUtilsEditorSettings.asset`에 저장합니다. 에셋 식별자는 파일 식별자와 파일 내부 식별자를 함께 사용하여 하위 에셋 충돌을 피합니다. 하위 에셋에는 파일 전체 이름 변경·이동·삭제를 제공하지 않습니다.

## 생성과 확장

생성 메뉴는 먼저 **SWUtils / Other assets**로 나누고, 그 아래에 탐색기와 같은 카테고리로 유형을 묶습니다. `SWUtils` 소속 어셈블리, `SW` 네임스페이스, `SWUtils/...` 생성 메뉴 중 하나에 해당하면 SWUtils에 표시합니다. 접두사 뒤의 구분자까지 확인하므로 `SWUtilsCopy`처럼 이름만 비슷한 모듈은 포함하지 않습니다. 네임스페이스가 없는 `SWQuestScoreRewardExample`도 `SWUtils.Samples` 소속이므로 SWUtils에 포함됩니다. 나머지 패키지·프로젝트 유형은 Other assets에 표시합니다. 카테고리 이름을 바꾸더라도 이 구분은 유지됩니다.

현재 선택한 분류를 먼저 펼치며 나머지 분류는 접고 펼칠 수 있습니다. 검색은 최상위 구분, 분류명, 유형명과 Unity 생성 메뉴 경로를 함께 확인하고 일치하는 분류를 펼칩니다. 최근 생성한 유형은 해당 분류 안에서 먼저 표시합니다.

### 설정 화면에서 분류 추가

왼쪽 분류와 유형 설정의 선택지는 **Settings > Categories에 저장된 목록만** 사용합니다. 기본 분류는 `SWUtility`, `SWSamples`, `SWSkillTree`, `SWStat`, `SWBehaviour Tree`, `SWStateMachine`, `SWQuest`, `Other` 순서로 한 번 추가합니다. 이후 이름 변경, 순서 변경과 삭제가 유지되며 코드 등록이나 재검색으로 다시 나타나지 않습니다. `All assets`와 `Favourites`는 분류 편집과 별개의 탐색 기능입니다.

`Assets/SWUtils/Samples`, `Packages/com.swtools.swutils/Samples`, 가져온 `Assets/Samples/SWUtils/<버전>` 아래의 에셋은 유형 설정과 개별 분류보다 우선하여 `SWSamples`에 표시합니다. 해당 폴더가 탐색 범위에 포함되어야 하며 유형 표시 여부와 제외 폴더 설정도 적용합니다. `SWSamples` 자체를 삭제하면 폴더 우선 규칙도 해제됩니다. 다른 분류를 삭제한 에셋은 탐색 범위 안에 있을 때 미지정 상태로 `All assets`에서 볼 수 있습니다.

유형 설정, 생성 메뉴와 필터의 검색·펼침·스크롤 상태를 저장합니다. 유형 설정의 `Back to settings`는 변경 사항을 저장하고 설정으로 돌아가며, `Done`은 탐색으로 돌아갑니다. 처음 설정할 때만 `Start browsing`을 표시합니다. 필터는 `SWUtils / Other assets → 설정 카테고리 → 유형` 순서이며 그룹 선택도 지원합니다. 탐색 필터에서 선택한 유형이 없으면 탐색 범위 안의 활성 유형 전체를 표시합니다. 유형 설정에서 모든 유형을 비활성화한 경우에는 검색하지 않습니다.

1. 오른쪽 위 톱니바퀴를 눌러 `Settings`를 엽니다.
2. `Categories`의 이름 입력란에 새 이름을 입력하고 `Add`를 누릅니다. `Edit`에서 이름과 아이콘을 바꿀 수 있습니다.
3. `Maintenance > Configure asset types`에서 해당 유형을 검색하고, 오른쪽 카테고리를 새 분류로 지정합니다. 필요하면 유형을 활성화합니다.
4. 변경 사항은 자동 저장됩니다. `Done`으로 탐색하거나 `Back to settings`로 설정에 돌아갑니다. 생성 가능한 활성 유형이 없는 분류는 생성 메뉴에서 생략됩니다.

기존 에셋 하나를 카테고리로 드래그하는 동작은 그 에셋만 재분류합니다. 앞으로 만들 유형 전체의 기본 분류를 변경하려면 3번의 유형 설정을 사용합니다.

### 코드로 분류 추가와 유형 기본값 지정

분류 추가는 명시적으로 실행하는 메뉴에서 하고, 유형 기본 정책만 에디터 로드 시 등록합니다. 이렇게 하면 사용자가 삭제한 분류가 다시 생기지 않습니다. 식별자는 고유하게 유지하고 표시 순서는 Settings에서 변경합니다. 정책의 `priority`가 높은 조건을 우선 적용합니다. `SWEditorRegistry.RegisterCategory`는 외부 등록 정보만 보관하며 화면 목록에 직접 추가하지 않습니다.

생성은 사용자 흐름, 확인 가능한 Unity 생성 메뉴, 직접 생성 순서입니다. 기본 메뉴가 동작할 때에는 Unity의 이름 편집을 완료해야 합니다. 취소하려면 생성 감시 취소 버튼을 누릅니다. 생성 감시는 120초 후 해제합니다. `CreateAssetMenu`와 별도로 작성한 임의의 메뉴에서 대상 유형을 추론할 수 없는 경우 생성 흐름을 등록합니다.

```csharp
using UnityEditor;
using SW.EditorTools.Workspace;

/// <summary>
/// 게임 전용 분류 추가와 새 유형의 기본값을 제공합니다.
/// </summary>
public static class GameEditorSetup
{
    /// <summary>
    /// 사용자가 메뉴를 실행할 때만 분류를 추가합니다.
    /// </summary>
    [MenuItem("SWTools/Add Game Data Category")]
    public static void AddCategory()
    {
        SWEditorWorkspaceSettings settings = SWEditorWorkspaceSettings.instance;
        settings.InitializeCategories();
        if (!settings.HasCategory("my-game.data"))
        {
            settings.Categories.Add(new SWEditorCategorySettings
            {
                Identifier = "my-game.data",
                DisplayName = "Game Data"
            });
            settings.Persist();
        }
    }

    /// <summary>
    /// 새로 발견한 게임 유형의 기본 분류를 지정합니다.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void RegisterTypePolicy()
    {
        SWEditorRegistry.RegisterTypePolicy(new SWEditorTypePolicy(
            "my-game.data.policy",
            type => type.Namespace == "MyGame.Data",
            defaultCategoryIdentifier: "my-game.data",
            enabled: true,
            priority: 100));
    }
}
```

위 예제는 `Other assets > Game Data`에 표시됩니다. SWUtils 확장 모듈이라면 조건을 실제 `SW.모듈명` 네임스페이스에 맞추면 `SWUtils` 아래에 표시됩니다. 하위 네임스페이스도 포함하려면 일치 조건에 `StartsWith("MyGame.Data.", System.StringComparison.Ordinal)`을 추가합니다.

정책은 새로 발견한 유형의 기본값을 제공합니다. 이미 설정에 저장된 유형의 사용자 선택은 덮어쓰지 않으므로, 기존 유형에 새 정책을 적용하려면 `Configure asset types`에서 분류를 한 번 변경하세요. 직접 생성할 유형에는 `[CreateAssetMenu]`를 붙이거나 생성 흐름을 등록해야 합니다.

`SWIdentifiedObject`의 코드명·표시명·설명·식별자가 검색어에 포함됩니다. 해당 에셋의 스프라이트도 재사용합니다. Game Creator가 설치되면 공개 Sprite 멤버를 읽어 아이콘으로 사용하고, 읽을 수 없으면 Unity 에셋 아이콘으로 돌아갑니다.

UI Toolkit 참조 필드는 참조 에셋을 새 탭으로 열며 오브젝트 선택기 버튼은 구분합니다. 외부 IMGUI 인스펙터는 해당 인스펙터의 참조 필드 구현에 따라 동작이 달라질 수 있습니다. 외부 패키지의 비공개 검사·생성 구현까지 동일하다고 보장하지 않습니다.

## 검증

### v1.4.2 검증 범위

Unity 6000.3.11f1 참조 어셈블리로 Runtime과 Editor 코드를 컴파일해 오류가 없는지 확인했습니다. 실제 검색·폴더 규칙·목록·설정 소스에 Unity 서비스 대역을 연결한 자동 검사 29개도 통과했습니다. 기본 폴더 초기화, 여러 경로와 폴더 경계, 빈 경로 처리, 비활성 유형 제외, 하위 에셋 식별, 검색 취소·실패 시 상태 보존을 검사했습니다. 탐색 범위 밖에 에셋 85,000개를 둔 모의 검사에서 해당 파일을 불러오지 않는 것도 확인했습니다.

이번 변경의 실제 Unity 화면 동작, 대형 프로젝트에서의 멈춤과 창 핸들 오류 해소 여부는 별도 실행 검증이 필요합니다. 검증용 소스와 결과물은 패키지에 추가하지 않았으며, 기존 `Temp`·`Tests`와 테스트 메타 파일은 배포 구성에서 제거했습니다.

### v1.4.0 검증 기록

아래는 v1.4.0 개발 당시의 실행 기록입니다. 당시 사용한 테스트 소스는 v1.4.2 패키지에 포함되지 않습니다.

Unity 6000.3.11f1과 UnityCLI 1.0.0-beta.6에서 검사했습니다. `SWEditorWorkspaceTests`의 16개 검사는 분류 우선순위, 폴더 경계, 하위 에셋 식별, 재검색 시 사용자 선택 유지, 등록 교체, 생성 흐름 우선순위, 검색 공급자 오류 격리, 복합 검색, 복제·이름 변경 이벤트와 인스펙터 잠금, 네임스페이스 없는 샘플의 소속과 생성 메뉴 경계, 삭제한 기본 분류의 유지, 설정 선택지 일치, 샘플 폴더 우선 분류, 유형 설정의 펼침·스크롤 복원과 뒤로가기를 검증하며 모두 통과했습니다.

프로젝트의 샘플 에셋 94개가 SWSamples로 분류되는 것을 확인했습니다. 카드 크기 72·100·156에서 긴 배지가 있는 에셋도 이름과 아이콘이 겹치지 않으며, 필터의 그룹과 유형이 팝업 너비 안에 배치되는지 검사했습니다.

샘플 소속 검사 추가 전 전체 실행에서는 73개 중 72개가 통과했습니다. 기존 `SWReliabilityTests.InvalidImportPreservesExistingValues("{}")` 한 개는 빈 객체 가져오기 반환값이 예상과 달라 실패했습니다. 관련 없는 런타임 저장 코드는 수정하지 않았습니다. 기존 에디터 창 23개는 실제로 열어 화면 생성 오류가 없는지 확인했습니다. 좁은 창의 영역 경계, 스크롤 손잡이의 트랙 포함 여부, 이미지 아이콘의 중앙 오차도 실제 배치값으로 확인했습니다.

당시 분석 이미지, 창 캡처와 실행 결과는 검증 프로젝트의 `Library/SWUtilsEditorResearch`에 보관했습니다. 수정 전 에디터 및 테스트 소스는 그 폴더의 `BeforeImplementation`에 보관했습니다. 이 자료는 배포 패키지에 포함되지 않습니다.
