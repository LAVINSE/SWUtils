# SWUtils State Machine Graph 사용 방법

State Machine Graph는 Unity 6 전용이며 `Layered`와 `Stack` 그래프를 편집합니다.

## 시작하기

1. `SWTools > Utils > State Machine > Graph Editor`를 엽니다.
2. 첫 화면에서 `Create New Graph` 또는 `Open Existing Graph`를 선택합니다.
3. Blackboard의 `Graph Type`에서 `Layered` 또는 `Stack`을 선택합니다.
4. `Create Node`, 빈 공간의 오른쪽 클릭 또는 `Space` 키로 상태, Any State, Return State를 생성하고 `Out`과 `In`을 연결합니다.
5. Graph Inspector에서 상태 타입, Layer, 초기 상태와 전이 명령·조건·우선순위를 편집합니다.

## 편집 기능

- `Auto Layout`: Layer와 초기 상태를 기준으로 노드를 정렬합니다.
- `Assets`: 프로젝트의 그래프 에셋을 빠르게 전환합니다.
- `New Script`: Layered State, Stack State, Transition Condition 스크립트를 생성합니다.
- 복사, 붙여넣기, 복제와 키보드 노드 탐색을 지원합니다.
- Graph List 접힘 상태와 Blackboard, Graph Inspector 크기 및 위치를 저장합니다.
- 그래프 이동 위치와 확대 비율은 에셋별로 자동 복원됩니다.
- 아래 `Graph Validation`을 펼치면 모든 검사 내용을 확인할 수 있습니다.

`Project Settings > SWUtils > State Machine Graph`에서 노드 크기, 패널, 전이 요약과 격자 맞춤을 설정할 수 있습니다.

## 스크립트 템플릿

생성 템플릿은 `Editor/StateMachine/Templates`에 있습니다. 기본 문맥 타입은 `GameObject`이며 프로젝트가 별도 문맥 타입을 사용한다면 생성된 스크립트의 제네릭 타입을 해당 타입으로 바꿉니다.

- `SWLayeredStateTemplate.txt`
- `SWStackStateTemplate.txt`
- `SWTransitionConditionTemplate.txt`

## 사용자 정의 상태 카테고리

상태 또는 전이 조건 클래스에 `SWStateMachineNodeCategory`를 지정하면 노드 검색과 조건 선택 메뉴를 원하는 구조로 나눌 수 있습니다. 슬래시(`/`)는 하위 카테고리를 만듭니다.

```csharp
[SWStateMachineNodeCategory("Combat/Movement")]
public sealed class ChaseState : SWState<GameObject>
{
}

[SWStateMachineNodeCategory("Combat/Conditions")]
public sealed class CanChaseCondition : SWStateMachineGraphCondition<GameObject>
{
    public override bool Evaluate(GameObject context)
    {
        return context != null;
    }
}
```

카테고리 속성이 없는 상태는 `States`, 조건은 `Conditions`에 표시됩니다. `New Script`로 생성한 타입은 기본적으로 `Custom` 아래에 배치되며 속성의 경로 문자열을 수정해 자유롭게 분류할 수 있습니다.

## 실행

Layered 그래프는 `SWStateMachineGraphFactory.CreateLayered`, Stack 그래프는 `SWStateMachineGraphFactory.CreateStack`으로 실행 인스턴스를 생성합니다. Play Mode에서 문맥 게임 오브젝트를 선택하면 활성 상태, 실행 시간과 최근 전이를 Runtime Inspector에서 확인할 수 있습니다.
