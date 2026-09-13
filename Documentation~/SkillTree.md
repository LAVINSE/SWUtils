# 스킬트리 사용법

[README](../README.md) · [예제 코드](../Samples/Scripts/SWSkillTreeExample.cs)

`SW.SkillTree`는 공용 정의와 플레이어별 진행을 분리합니다. 선행 레벨, 모든 조건·하나 이상 조건, 선택 분기, 반복 강화, 여러 재화 비용, 환불과 진행 초기화를 구성할 수 있습니다.

## 제작

1. `SWTools > Utils > Data > Skill Tree Editor`를 엽니다.
2. 트리 정의에 노드를 추가하고 식별자, 스킬 정의, 최대 레벨과 비용을 지정합니다.
3. 선행 연결과 요구 레벨을 설정합니다. 표시 여부와 상세 정보 공개 조건도 별도로 지정할 수 있습니다.
4. 초기화 후 남겨야 할 노드는 **진행 초기화 시 유지**를 켭니다.
5. 예제 프리팹 `Samples/Prefab/SWSkillTreeExample.prefab`을 씬에 배치하고 트리 정의를 연결합니다.

실행 중에는 정의를 변경하지 않습니다. 플레이어 진행을 저장한 뒤에도 노드 식별자를 유지해야 기존 데이터가 같은 노드를 가리킵니다.

## 실행

```csharp
using SW.SkillTree;

SWSkillTreeWallet wallet = new SWSkillTreeWallet();
wallet.SetBalance("Gold", 250);

SWSkillTreeSystem tree = new SWSkillTreeSystem(definition, wallet);
SWSkillTreePurchase preview = tree.PreviewPurchase(nodeIdentifier);
SWSkillTreePurchase purchase = tree.Purchase(nodeIdentifier);
bool refunded = tree.Refund(nodeIdentifier, out string reason);

tree.Dispose();
```

`definition`은 제작한 트리 에셋이고 `nodeIdentifier`는 그 안의 노드 식별자입니다. 구매는 실행 시점의 조건과 잔액을 다시 확인하고 요청한 비용을 한 번에 차감합니다. 최대 구매는 한 요청에서 최대 10,000레벨까지 처리합니다. 환불은 실제 지불한 마지막 레벨 비용을 기준으로 하며, 후속 노드의 조건을 깨뜨리면 거절합니다.

`Changed`는 구매 외에 잔액 변경, 복원과 초기화에서도 발생합니다. `Purchased`는 실제 구매가 성공했을 때만 발생하므로 일회성 구매 알림에 사용할 수 있습니다.

## 확장

| 계약 | 구현할 내용 |
| --- | --- |
| `ISWSkillTreeWallet` | 잔액 조회, 여러 재화의 일괄 차감·환급과 변경 알림 |
| `SWSkillTreeCondition` | 게임 문맥을 사용하는 습득·표시 조건 |
| `SWSkillTreeCost` | 현재 레벨에서 다음 레벨로 가는 비용 |
| `SWSkillTreeEffect` | 현재 레벨의 절댓값으로 다시 적용할 수 있는 지속 효과 |
| `ISWSkillTreeSaveStore` | 저장 문자열을 보관할 위치 |

지갑의 교환이 실패하면 어떤 재화도 바뀌면 안 됩니다. 변경 알림 구독자의 예외도 거래 결과를 바꾸면 안 됩니다. 기본 `SWSkillTreeWallet` 구현을 참고하세요.

지속 효과는 `SWSkillTreeEffectBinding`으로 연결합니다. 효과는 출처별로 관리하며 반복 동기화 시 누적 지급되지 않도록 현재 레벨 기준으로 적용합니다. 사용이 끝나면 효과 연결의 `Dispose()`를 먼저 호출하고 시스템을 정리합니다. 실패한 사용자 효과는 `Failed` 이벤트로 알리며 다음 동기화에서 재시도합니다.

## 저장과 초기화

`CaptureSaveData()`는 진행과 실제 지불 기록의 사본을 만듭니다. `Restore(data, out reason)`는 전체 내용을 검증한 뒤 진행을 교체합니다. 잔액을 바꾸거나 구매 이벤트를 다시 발생시키지 않으므로 지갑 데이터도 같은 게임 저장에 포함해야 합니다.

`Reset(retainPermanentNodes, refundPayments, out reason)`로 영구 노드 유지와 비용 환급 여부를 지정합니다. 환생에서 기존 비용을 돌려주지 않으려면 `refundPayments`를 `false`로 설정합니다.

## 화면과 배치

MiningSkillTree 예제는 81개 노드와 6개 경로로 구성되어 있습니다. 드래그와 휠로 이동·확대하고 `Start`, `Selected`, `Fit All`로 시작점·선택 노드·전체 표시 노드를 확인합니다.

`SWSkillTreeView`에 시스템을 `Bind`한 뒤 노드를 생성합니다. 인스펙터의 **노드 생성 및 자동 배치**, **현재 위치 저장**, **저장 위치 불러오기**로 배치를 편집합니다. 자동 배치는 저장 위치를 우선 사용하고 연결선은 실제 위치에 맞춰 갱신합니다.

노드 크기는 TreeView에서 지정하고 글꼴은 프로젝트의 TextMeshPro 기본 글꼴을 사용합니다. **편집 미리보기 → 전체 노드 보기**로 숨겨진 노드도 배치할 수 있습니다. 화면 좌표는 공용 정의에 저장하며 플레이어 진행 저장에는 포함하지 않습니다.
