# 저장·실행 규칙

[README 전체 기능](../README.md#전체-기능-찾아보기)

이 문서는 현재 개발본의 실패 처리와 기존 데이터 호환 범위를 설명합니다.

## 설정값과 파일 저장

`SWPlayerPrefs`는 빈 문자열을 저장값으로 보존하고, 키 목록을 문자열 배열로 기록해 `|` 같은 문자를 포함한 키도 관리합니다. 새 목록이 없으면 기존 구분자 형식을 읽습니다. 이전 형식에서 이미 구분자로 나뉜 키 이름은 자동 복구할 수 없습니다.

`ImportFromJson`은 항목과 암호화 결과를 모두 검사한 뒤 해당 슬롯의 값을 교체합니다. `MergeFromJson`은 기존 값에 입력을 합칩니다. 둘 다 적용 중 예외가 발생하면 이전 값을 복구하려고 시도하며, 저장소 자체가 계속 실패하면 복구도 실패할 수 있습니다. `CanImportJson`은 저장소를 변경하지 않고 입력만 검사합니다.

비동기 작업에서는 요청한 슬롯을 명시합니다. 아래 호출은 현재 선택된 슬롯을 바꾸지 않습니다.

```csharp
using SW.Data;

SWPlayerPrefs.SetString("DisplayName", "", "PlayerA");
string displayName = SWPlayerPrefs.GetString("DisplayName", "Guest", "PlayerA");
string snapshot = SWPlayerPrefs.ExportSlotToJson("PlayerA");
bool restored = SWPlayerPrefs.ImportFromJson(snapshot, "PlayerA");
SWPlayerPrefs.Save();
```

`SWSaveDataManager`는 같은 디렉터리에 임시 파일을 끝까지 쓴 뒤 기존 파일을 교체합니다. 기존 파일을 먼저 삭제하지 않습니다. 교체 기능을 지원하지 않는 파일 시스템에서는 저장이 실패하고 이전 파일을 유지합니다. 등록된 데이터를 읽을 때 본 파일이 없거나 해석되지 않으면 `.bak` 파일을 확인하고 복구합니다.

클라우드 복원은 요청 당시 슬롯에 파일과 설정값을 적용합니다. 파일 저장 실패 시 설정값을 이전 내용으로 돌립니다. 파일과 PlayerPrefs는 서로 다른 저장소이므로 프로세스가 강제 종료되는 순간까지 하나의 원자적 저장을 보장하지는 않습니다.

암호화 솔트와 기존 저장 키를 바꾸면 이전 데이터에 접근하지 못할 수 있습니다. 암호화 저장은 게임 서버의 지급 검증을 대신하지 않습니다.

## 클라우드 연결

| 환경 | 활성화 조건 | 프로젝트에서 준비할 내용 |
| --- | --- | --- |
| Android | `SW_GOOGLEPLAY_ENABLE` | Google Play Games 패키지, 저장 기능 설정과 인증 |
| 데스크톱 | `SW_STEAMWORKS_NET` | Steamworks.NET, 초기화 상태를 제공하는 `SteamManager.Initialized`, Steam Cloud 설정 |
| iOS | `SW_ICLOUD_ENABLE` | 아래 네이티브 함수 구현, iCloud 권한과 동기화 설정 |
| 편집기 또는 연동 비활성 | 별도 심볼 없음 | 슬롯별 로컬 PlayerPrefs 대체 저장 |

iCloud 네이티브 함수 `_GetiCloudData`, `_SetiCloudData`, `_ForceSynciCloudData`의 구현은 이 패키지에 포함되어 있지 않습니다. 기존 iCloud 연결을 사용하던 프로젝트는 구현을 유지하고 `SW_ICLOUD_ENABLE`을 추가해야 합니다. 심볼이 없으면 로컬 저장을 사용합니다.

로컬 대체 저장은 다른 기기로 동기화되지 않습니다. 실제 클라우드 연결과 플랫폼 빌드는 각각의 프로젝트에서 확인해야 합니다.

이전 버전의 로컬 캐시는 현재 선택한 슬롯과 기본 슬롯에서 요청 이름에 해당하는 키를 찾아 새 슬롯으로 옮깁니다. 다른 슬롯에 보관했던 캐시는 그 슬롯을 선택한 뒤 불러오세요. 삭제한 캐시에는 빈 값을 남겨 이전 사본이 다시 나타나지 않게 합니다.

## 퀘스트와 상태 전환

- 퀘스트는 보상이 모두 지급되어야 완료합니다. 실패한 보상부터 다시 시도하고 성공 기록은 저장 데이터에 포함합니다. 재화와 지급 기록을 함께 저장하는 방법은 [퀘스트 문서](Quest.ko.md)에 있습니다.
- 상태 전환 중 들어온 요청은 현재 전환을 마친 뒤 순서대로 실행합니다. [반환값과 콜백 규칙](StateMachineGraph.ko.md)을 확인하세요.
- 행동 트리는 하위 트리의 순환 연결과 중첩 한도를 검사한 뒤 실행 복제본을 만듭니다. [블랙보드와 하위 트리](BehaviourTree.ko.md)를 참고하세요.

## 씬 로딩

`SWSceneLoader`는 요청을 받은 다음 프레임에 엔진 작업을 시작합니다. 그 전에는 `TryCancelCurrentLoad()`로 취소할 수 있습니다. 엔진 작업이 시작된 뒤에는 `false`를 반환하고 작업을 유지합니다. 기존 `CancelCurrentLoad()`는 같은 규칙을 사용하고 취소가 거절되면 경고를 남깁니다.

`AllowSceneActivation`을 `false`로 설정했다면 진행률이 준비 단계에 도달한 뒤 `true`로 바꿔 활성화를 허용해야 합니다. Unity는 활성화를 보류한 작업 뒤의 비동기 작업도 대기시킬 수 있습니다. [Unity 활성화 제어 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html)

완료 콜백을 호출하기 전에 로더 상태를 초기화하므로 콜백에서 다음 씬을 요청할 수 있습니다. 로더를 비활성화하거나 파괴하면 추적 중인 작업의 활성화 보류를 해제합니다. 이 경우 엔진 로드는 계속될 수 있으며 취소로 처리하지 않습니다.

## 풀·팝업·오디오

- `SWPool`은 비활성 상태에서 부모와 위치, 풀 참조를 설정하고 `OnSpawnFromPool`을 호출한 뒤 활성화합니다. 미리 생성만 하는 동안에는 생성·반납 콜백을 호출하지 않습니다. 처음 생성되는 비활성 객체는 `Awake` 시점을 가정하지 말고 필요한 초기화를 콜백에서 준비하세요.
- 팝업을 다시 표시하면 이전 숨김 완료 콜백은 새 표시를 닫지 않습니다. 닫힘 대기와 알림 전에 관리 목록과 캐시 처리를 끝냅니다.
- 전체·효과음 음량을 바꾸면 재생 중인 효과음에도 반영합니다. 각 재생 요청의 음량 배율은 유지합니다.

## 표와 진단

표와 번역 데이터의 인용된 셀은 탭, 줄바꿈과 이중 따옴표를 보존합니다. 따옴표가 닫히지 않은 입력은 거절합니다. 표의 필수 열 누락, 중복 헤더와 잘못된 논리값도 적용 오류입니다. 논리값은 `true/false`, `yes/no`, `y/n`, `1/0`을 대소문자 구분 없이 사용합니다.

`SWEventBus.IsDiagnosticsEnabled`는 진단 기록을 제어하고 `IsLogOutputEnabled`는 로그 출력만 제어합니다. 데이터의 `ToString()`이 실패해도 발행자에게 예외를 전파하지 않습니다.

길이가 0이거나 실행 중 길이를 이미 경과한 시간 이하로 줄인 타이머는 다음 `Tick`에서 완료를 알립니다. `SWStat.SetRange`는 최소·최대 값을 함께 검증하고 최종 값이 달라지면 변경 이벤트를 발생시킵니다. `SWUtility.SetGaugeText`는 게이지의 텍스트만 갱신하며 기존 `SetGauge`도 같은 동작을 유지합니다.
