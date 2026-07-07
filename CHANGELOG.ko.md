# 변경 기록

[English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

이 프로젝트의 주요 변경사항을 기록합니다.

## [v1.0.12] - 2026-07-07

### 변경
- Unity 및 .NET 타입 이름과 충돌하기 쉬운 네임스페이스를 `SW.Attributes`, `SW.Coroutines`, `SW.Debugging`, `SW.ScreenResolution`, `SW.Editor.Attributes`로 변경했습니다.
- 패키지 메타데이터와 README 설치 주소를 `v1.0.12`로 변경했습니다.

## [v1.0.11] - 2026-07-06

### 변경
- Runtime 네임스페이스를 기능별 `SW.*` 구조로 재편하고 Runtime 및 Editor 폴더를 네임스페이스와 일치시켰습니다.
- 남아 있던 공개 `SWUtils...` 타입을 `SW...` 형식으로 변경하고 기존 저장 키는 유지했습니다.
- 한글 XML 문서 주석을 통일하고 잘못된 어트리뷰트 예제를 수정했습니다.
- 영문·한글 README와 변경 기록 문서를 추가하고 언어 전환 링크를 연결했습니다.
- 패키지 버전과 README 설치 주소를 `v1.0.11`로 변경했습니다.

## [v1.0.10] - 2026-07-02

### 변경
- 패키지 버전과 README 설치 주소를 `v1.0.10`으로 변경했습니다.

### 수정
- 변경된 폴더와 Unity 메타 파일 이름을 일치시켜 읽기 전용 패키지 설치 오류를 수정했습니다.

## [v1.0.9] - 2026-07-02

### 추가
- `SWMonoBehaviour`와 같은 그룹, 버튼, 다시 그리기 어트리뷰트를 지원하는 `SWScriptableObject`를 추가했습니다.
- 기본 Unity PlayerPrefs를 조회, 수정, 삭제할 수 있는 탭을 PlayerPrefs Viewer에 추가했습니다.
- 기본 Unity PlayerPrefs 검색 필터와 전체 삭제 기능을 추가했습니다.
- `SWEncrypt<T>`에 `long`, `double` 지원을 추가했습니다.
- `SWPlayerPrefs`에 `SetLong`, `GetLong`, `SetDouble`, `GetDouble`을 추가했습니다.
- `SWTime.ToDateTime`을 추가했습니다.

### 변경
- PlayerPrefs Viewer 목록에서 값을 직접 수정하고 저장할 수 있게 변경했습니다.
- `SWEventBus`에 전체 로그와 발행별 로그 제어 기능을 추가했습니다.
- `SWUtilsRefillTimer`를 `SWRefillTimer`로 변경하고 기존 저장 키는 유지했습니다.
- `SWTableSheet`가 리스트와 배열 외에 일반 클래스 필드도 지원하도록 확장했습니다.
- Excel Table Importer에 세로형 필드·값 구조를 추가했습니다.
- 주요 Runtime 및 Editor 사용법을 README에 보강했습니다.

## [v1.0.8] - 2026-06-16

### 추가
- 큰 숫자 단위 표시를 위한 `SWAmountFormat`을 추가했습니다.
- 숫자 단위와 소수점 설정을 저장하는 `SWAmountFormatProfile`을 추가했습니다.
- 프리셋 생성, 편집, 미리보기를 제공하는 Amount Format Window를 추가했습니다.
- 이미지 없이 레이캐스트 영역을 제공하는 `SWRectDummy`와 전용 인스펙터를 추가했습니다.

### 변경
- 패키지 버전을 `v1.0.8`로 변경했습니다.
- 숫자 표시 프리셋과 `SWRectDummy` 사용법을 README에 추가했습니다.

## [v1.0.7] - 2026-06-08

### 추가
- EventBus Debugger Window를 추가했습니다.
- `SWEventBus`에 리스너 수, 발행 수, 마지막 발행 시각과 데이터 스냅샷을 추가했습니다.
- Pool Monitor Window를 추가했습니다.
- `SWPool`에 프리팹별 생성, 활성, 대기, 반환과 지연 반환 상태를 추가했습니다.

### 변경
- 에디터 창 메뉴를 `SWTools/Debug`와 `SWTools/Utils`로 분리했습니다.
- 실제 에디터 메뉴 경로를 README에 기록했습니다.
- 패키지 버전을 `v1.0.7`로 변경했습니다.

### 수정
- Steamworks.NET 분기를 에디터에서 제외하여 빌드 오류를 방지했습니다.
- `SWConditionAttribute`의 사용하지 않는 UnityEditor 참조를 제거했습니다.

## [v1.0.6] - 2026-06-02

### 추가
- `SWSubClassSelector`를 추가했습니다.
- `SerializeReference` 필드에서 추상 클래스나 인터페이스 구현 타입을 검색하여 선택할 수 있게 했습니다.
- 타입 선택 경로를 설정하는 `SWAddTypeMenu`를 추가했습니다.
- 특정 타입을 선택 메뉴에서 숨기는 `SWHideInTypeMenu`를 추가했습니다.
- 단일 필드, 배열과 `List<T>` 사용법을 보여주는 샘플을 추가했습니다.

### 변경
- 패키지 버전을 `v1.0.6`으로 변경했습니다.
- 샘플 스크립트에 `SWExample` 네임스페이스를 적용했습니다.

## [v1.0.5] - 2026-05-29

### 추가
- TextMeshPro Font Asset Manager에 성능 탭을 추가했습니다.
- 아틀라스 메모리, 글리프, 문자, 대체 폰트 연결, 동적 아틀라스와 머티리얼 프리셋 검사를 추가했습니다.
- README에 TextMeshPro 폰트 에셋 성능 안내를 추가했습니다.

### 변경
- README와 변경 기록의 버전 표기를 `v1.0.5`로 변경했습니다.

## [1.0.0] - 2026-03-19

### 추가
- SWUtils 첫 버전을 배포했습니다.
