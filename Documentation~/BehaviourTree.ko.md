# SWUtils Behaviour Tree 사용 방법

SWUtils Behaviour Tree는 Unity 6 전용 그래프 편집기와 런타임 실행기를 함께 제공합니다. 노드는 Action, Composite, Decorator 세 종류이며 각 실행 결과는 `Running`, `Success`, `Failure`, `Aborted`입니다.

## 빠른 시작

1. `Assets > Create > SWTools > Behaviour Tree`에서 Tree Asset을 생성합니다.
2. 에셋을 두 번 누르거나 `SWTools > Utils > Behaviour > Tree Editor`를 엽니다.
3. 그래프 빈 공간에서 마우스 오른쪽 버튼을 누른 뒤 `Create Node`를 선택합니다.
4. 부모 노드의 `Out`을 자식 노드의 `In`에 연결합니다.
5. 시작할 노드를 선택하고 오른쪽 Node Inspector에서 `Set as Root`를 누릅니다.
6. 게임 오브젝트에 `SWBehaviourTreeRunner`를 추가하고 Tree Asset을 연결합니다.

Composite 노드는 여러 자식을 가지며 왼쪽부터 실행합니다. Decorator 노드는 자식 하나의 실행 결과나 실행 시간을 바꿉니다. Action 노드는 실제 작업을 수행하고 자식을 가질 수 없습니다.

## 편집기 조작

- `Space` 또는 빈 공간의 `Create Node`: 노드 검색
- `Ctrl+C`, `Ctrl+V`, `Ctrl+D`: 복사, 붙여넣기, 복제
- `A`: 전체 노드 보기
- `O`: 그래프 원점 보기
- `[`, `]`: 부모 또는 첫 번째 자식 선택
- 노드를 Command 또는 Ctrl 키로 두 번 누르기: 해당 SubTree 선택
- `Auto Layout`: Root를 기준으로 자동 정렬
- `Open`: 프로젝트의 Tree Asset 빠른 전환
- `New Script`: Action, Composite, Decorator 스크립트 생성
- `Settings`: 노드 크기, 간격, 패널 크기, 설명 표시와 실행 상태 갱신 간격 설정

그래프 위치와 확대 비율은 Tree Asset별로 자동 저장됩니다. Blackboard와 Node Inspector 패널은 아래 모서리를 끌어 크기를 바꿀 수 있으며 크기는 프로젝트 설정에 보존됩니다.

## Blackboard와 NodeProperty

왼쪽 Blackboard에서 기본 Key를 추가합니다. 노드 필드가 `SWBehaviourNodeProperty<T>`이면 고정값 또는 같은 타입의 Blackboard Key를 선택할 수 있습니다.

```csharp
[Serializable]
public sealed class AddScoreNode : SWBehaviourActionNode
{
    [SerializeField] private SWBehaviourNodeProperty<int> score = new();
    [SerializeField] private SWBehaviourNodeProperty<int> amount = new();

    protected override SWBehaviourStatus OnUpdate(
        SWBehaviourContext context,
        SWBehaviourTreeAsset tree)
    {
        return score.SetValue(context, score.GetValue(context) + amount.GetValue(context))
            ? SWBehaviourStatus.Success
            : SWBehaviourStatus.Failure;
    }
}
```

사용자 Key 타입은 `SWBehaviourBlackboardEntry<T>`를 상속하고 Blackboard의 `Custom` 메뉴에서 추가합니다.

```csharp
[Serializable]
public sealed class TargetKey : SWBehaviourBlackboardEntry<GameObject>
{
}
```

## Runner별 값 변경

`SWBehaviourTreeRunner`의 `Blackboard Overrides`에서 Key를 선택하면 원본 Tree Asset을 수정하지 않고 해당 게임 오브젝트의 시작값만 변경할 수 있습니다. 기본 타입과 사용자 정의 Key를 모두 지원합니다. Play Mode에서는 `Start Tree`, `Stop Tree`로 실행을 제어할 수 있습니다.

MonoBehaviour에서는 Runner의 공개 API로 실행 복제본의 값을 읽고 씁니다. 반복해서 접근하는 값은 `FindBlackboardKey<T>` 결과를 캐시하면 이름 사전 조회를 생략할 수 있습니다.

```csharp
SWBehaviourBlackboardKey<int> healthKey = runner.FindBlackboardKey<int>("Health");
healthKey.Value -= 10;

runner.SetBlackboardValue("TargetVisible", true);
bool visible = runner.GetBlackboardValue("TargetVisible", false);
```

## 범용 Property 노드

- `Set Property`: 선택한 Blackboard Key에 고정값을 기록합니다.
- `Compare Property`: `Equal`, `Not Equal`, 대소 비교 결과를 성공 또는 실패로 반환합니다.

고정값은 Boolean, Integer, Float, String, Vector2, Vector3, Object와 사용자 정의 Key 타입을 지원합니다.

## SubTree

`Sub Tree` 노드에 다른 Tree Asset을 연결하면 재사용 가능한 트리를 실행합니다. `Share Blackboard`를 켜면 부모와 같은 Blackboard를 사용하고, 끄면 SubTree에 저장된 기본값을 독립적으로 사용합니다.

## 실행 상태 확인

Play Mode에서 `SWBehaviourTreeRunner`가 있는 게임 오브젝트를 선택합니다. 실행 중인 노드는 노란색, 성공은 초록색, 실패는 빨간색, 중단은 회색으로 표시되며 활성 연결도 같은 색으로 표시됩니다.

## 기본 제공 노드

- Composite: Sequence, Selector, Parallel
- Decorator: Inverter, Repeat, Timeout, Succeeder, Failure
- Action: Wait, Log, Set Integer, Set Property, Compare Property, Random Failure, Breakpoint, Sub Tree

`Breakpoint`는 Unity Editor에서 실행 중 해당 노드에 도달했을 때 일시 정지하는 디버깅 노드입니다.

## 스크립트 템플릿

`New Script` 메뉴는 `Editor/Behaviour/Templates`의 텍스트 템플릿을 읽습니다. 팀 코드 스타일에 맞게 템플릿 내용을 수정하면 이후 생성되는 Action, Composite, Decorator 스크립트에 그대로 반영됩니다.

## 사용자 정의 노드 카테고리

노드 클래스에 `SWBehaviourNodeCategory`를 지정하면 `Create Node` 검색 창을 원하는 구조로 나눌 수 있습니다. 슬래시(`/`)는 하위 카테고리를 만듭니다. 속성이 없으면 노드 기반 타입에 따라 `Actions`, `Composites`, `Decorators`에 자동 배치됩니다.

```csharp
[Serializable]
[SWBehaviourNodeCategory("Combat/Movement")]
public sealed class ChaseTargetNode : SWBehaviourActionNode
{
    protected override SWBehaviourStatus OnUpdate(
        SWBehaviourContext context,
        SWBehaviourTreeAsset tree)
    {
        return SWBehaviourStatus.Success;
    }
}
```

`New Script`로 생성한 노드는 기본적으로 `Custom` 아래에 표시됩니다. 생성된 속성의 문자열만 프로젝트 구조에 맞게 변경하면 됩니다.
