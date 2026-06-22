# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- `SWTools/Debug/PlayerPrefs Viewer`에 기본 Unity PlayerPrefs를 조회, 수정, 삭제할 수 있는 `Unity PlayerPrefs` 탭을 추가했습니다.

### Changed
- `SWTools/Debug/PlayerPrefs Viewer`의 Entries 목록에서 값을 바로 수정하고 저장할 수 있도록 개선했습니다.
- `SWEventBus`에 로그 출력 여부를 제어하는 `IsLogOutputEnabled` 설정을 추가했습니다.
- `SWEventBus.Publish`에 발행 단위 로그 출력 여부를 정하는 `shouldOutputLog` 매개변수를 추가했습니다.

## [v1.0.8] - 2026-06-16

### Added
- `SWAmountFormat`을 추가했습니다. 큰 숫자를 K, M, B, T 같은 단위 문자열로 변환합니다.
- `SWAmountFormatProfile`을 추가했습니다. 숫자 포맷 단위와 소수점 설정을 Resources 프리셋 에셋으로 관리합니다.
- `SWTools/Utils/Amount Format Window`를 추가했습니다. 프리셋 에셋을 자동 생성하고 에디터 창에서 바로 수정 및 미리보기할 수 있습니다.
- `SWRectDummy`를 추가했습니다. 메시를 생성하지 않는 UI Graphic으로 Image 없이 레이캐스트 영역을 만들 수 있습니다.
- `GameObject/UI/SW Rect Dummy` 메뉴와 전용 인스펙터를 추가했습니다.

### Changed
- 패키지 버전을 `v1.0.8`로 갱신했습니다.
- README에 숫자 포맷 프리셋과 Rect Dummy 사용법을 추가했습니다.

## [v1.0.7] - 2026-06-08

### Added
- `SWTools/Debug/EventBus Debugger Window`를 추가했습니다.
- `SWEventBus`에 이벤트 타입별 리스너 수, 발행 횟수, 마지막 발행 시간, 마지막 발행 데이터 스냅샷 기능을 추가했습니다.
- `SWTools/Debug/Pool Monitor Window`를 추가했습니다.
- `SWPool`에 프리팹별 생성 수, 활성 수, 대기 수, 스폰 횟수, 반환 횟수, 파괴 수, 지연 반환 수 스냅샷 기능을 추가했습니다.

### Changed
- EditorWindow 메뉴 경로를 `SWTools/Debug`와 `SWTools/Utils`로 구분했습니다.
- README의 EditorWindow 설명에 실제 메뉴 경로를 표기했습니다.
- 패키지 버전을 `v1.0.7`로 갱신했습니다.

### Fixed
- Runtime 스크립트에서 Unity 빌드 시 문제가 생기지 않도록 Steamworks.NET 분기를 `!UNITY_EDITOR` 조건으로 보호했습니다.
- `SWConditionAttribute`에서 사용하지 않는 `UnityEditor` 참조를 제거했습니다.

## [v1.0.6] - 2026-06-02

### Added
- `SWSubClassSelector`를 추가했습니다.
- `SerializeReference` 필드에서 추상 클래스 또는 인터페이스의 구현 타입을 검색 가능한 드롭다운으로 선택할 수 있습니다.
- `SWAddTypeMenu`로 타입 선택 메뉴 경로를 지정할 수 있습니다.
- `SWHideInTypeMenu`로 타입 선택 메뉴에서 특정 타입을 숨길 수 있습니다.
- 단일 필드, 배열, `List<T>` 컬렉션 예시를 포함한 `SWSubClassSelectorExample` 샘플을 추가했습니다.

### Changed
- 패키지 버전을 `v1.0.6`으로 갱신했습니다.
- 샘플 스크립트에 `SWExample` 네임스페이스를 적용했습니다.

## [v1.0.5] - 2026-05-29

### Added
- `SWTools/TMP Font Asset Manager`에 TextMeshPro 폰트 에셋 성능 탭 추가
- 성능 탭에서 아틀라스 메모리, 글리프, 문자, 폴백 체인, 동적 아틀라스, 머티리얼 프리셋 점검 지원
- README에 TextMeshPro 폰트 에셋 성능 확인 가이드 추가

### Changed
- README 및 Changelog 버전 표기를 v1.0.5로 갱신

## [1.0.0] - 2026-03-19

### Added
- SWUtils 최초 릴리스
