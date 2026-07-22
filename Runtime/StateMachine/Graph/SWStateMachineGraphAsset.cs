using System;
using System.Collections.Generic;
using UnityEngine;

namespace SW.StateMachine
{
    /// <summary>
    /// 그래프가 구성하는 상태 머신 종류입니다.
    /// </summary>
    public enum SWStateMachineGraphType
    {
        /// <summary>여러 계층과 상태 전이를 사용하는 유한 상태 머신입니다.</summary>
        Layered,

        /// <summary>상태를 위에 쌓고 이전 상태로 복귀하는 스택 상태 머신입니다.</summary>
        Stack,
    }

    /// <summary>
    /// 그래프에 표시되는 노드 종류입니다.
    /// </summary>
    public enum SWStateMachineNodeKind
    {
        /// <summary>실제로 실행되는 상태 노드입니다.</summary>
        State,

        /// <summary>현재 상태와 관계없이 전이하는 시작 노드입니다.</summary>
        AnyState,

        /// <summary>스택에서 이전 상태로 복귀하는 대상 노드입니다.</summary>
        Return,
    }

    /// <summary>
    /// 그래프 연결이 실행할 상태 변경 종류입니다.
    /// </summary>
    public enum SWStateMachineTransitionOperation
    {
        /// <summary>유한 상태 머신의 일반 상태 전이입니다.</summary>
        Transition,

        /// <summary>현재 상태 위에 새 상태를 추가합니다.</summary>
        Push,

        /// <summary>현재 상태를 새 상태로 교체합니다.</summary>
        Replace,

        /// <summary>현재 상태를 제거하고 이전 상태로 복귀합니다.</summary>
        Pop,
    }

    /// <summary>
    /// 상태 머신 그래프의 상태 노드 데이터를 저장합니다.
    /// </summary>
    [Serializable]
    public sealed class SWStateMachineNodeData
    {
        #region 필드
        [SerializeField] private string identifier;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string stateTypeName;
        [SerializeField] private Rect position;
        [SerializeField] private int layer;
        [SerializeField] private bool isInitialState;
        [SerializeField] private SWStateMachineNodeKind kind;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>노드를 구분하는 고유 식별자입니다.</summary>
        public string Identifier => identifier;

        /// <summary>그래프에 표시할 노드 이름입니다.</summary>
        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        /// <summary>노드 아래에 표시할 상태 설명입니다.</summary>
        public string Description
        {
            get => description;
            set => description = value;
        }

        /// <summary>상태 구현 타입의 조립체 한정 이름입니다.</summary>
        public string StateTypeName
        {
            get => stateTypeName;
            set => stateTypeName = value;
        }

        /// <summary>그래프에서 노드가 표시되는 위치와 크기입니다.</summary>
        public Rect Position
        {
            get => position;
            set => position = value;
        }

        /// <summary>유한 상태 머신에서 노드가 속한 계층 번호입니다.</summary>
        public int Layer
        {
            get => layer;
            set => layer = value;
        }

        /// <summary>상태 머신이 시작할 때 진입하는 상태인지 여부입니다.</summary>
        public bool IsInitialState
        {
            get => isInitialState;
            set => isInitialState = value;
        }

        /// <summary>노드 종류입니다.</summary>
        public SWStateMachineNodeKind Kind => kind;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 상태 머신 노드 데이터를 생성합니다.
        /// </summary>
        /// <param name="kind">생성할 노드 종류입니다.</param>
        /// <param name="displayName">그래프에 표시할 이름입니다.</param>
        /// <param name="stateTypeName">상태 구현 타입의 조립체 한정 이름입니다.</param>
        /// <param name="position">그래프에 표시할 위치와 크기입니다.</param>
        public SWStateMachineNodeData(
            SWStateMachineNodeKind kind,
            string displayName,
            string stateTypeName,
            Rect position)
        {
            identifier = Guid.NewGuid().ToString("N");
            this.kind = kind;
            this.displayName = displayName;
            description = string.Empty;
            this.stateTypeName = stateTypeName;
            this.position = position;
            layer = 0;
            isInitialState = false;
        }
        #endregion // 생성자

        #region 내부 함수
        /// <summary>식별자가 비어 있으면 새 식별자를 생성합니다.</summary>
        internal void EnsureIdentifier()
        {
            if (string.IsNullOrWhiteSpace(identifier))
                identifier = Guid.NewGuid().ToString("N");
        }
        #endregion // 내부 함수
    }

    /// <summary>
    /// 상태 머신 그래프에서 두 노드를 연결하는 전이 데이터를 저장합니다.
    /// </summary>
    [Serializable]
    public sealed class SWStateMachineTransitionData
    {
        #region 필드
        [SerializeField] private string identifier;
        [SerializeField] private string fromNodeIdentifier;
        [SerializeField] private string toNodeIdentifier;
        [SerializeField] private SWStateMachineTransitionOperation operation;
        [SerializeField] private bool usesCommand;
        [SerializeField] private int command;
        [SerializeField] private string conditionTypeName;
        [SerializeField] private bool canReenter;
        [SerializeField] private int priority;
        [SerializeField] private Vector2 summaryOffset;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>전이를 구분하는 고유 식별자입니다.</summary>
        public string Identifier => identifier;

        /// <summary>전이가 시작되는 노드의 식별자입니다.</summary>
        public string FromNodeIdentifier => fromNodeIdentifier;

        /// <summary>전이가 도착하는 노드의 식별자입니다.</summary>
        public string ToNodeIdentifier => toNodeIdentifier;

        /// <summary>전이가 실행할 상태 변경 종류입니다.</summary>
        public SWStateMachineTransitionOperation Operation
        {
            get => operation;
            set => operation = value;
        }

        /// <summary>전이가 명령 번호를 사용하는지 여부입니다.</summary>
        public bool UsesCommand
        {
            get => usesCommand;
            set => usesCommand = value;
        }

        /// <summary>전이를 실행할 명령 번호입니다.</summary>
        public int Command
        {
            get => command;
            set => command = value;
        }

        /// <summary>전이 조건 구현 타입의 조립체 한정 이름입니다.</summary>
        public string ConditionTypeName
        {
            get => conditionTypeName;
            set => conditionTypeName = value;
        }

        /// <summary>현재 상태와 같은 상태로 다시 진입할 수 있는지 여부입니다.</summary>
        public bool CanReenter
        {
            get => canReenter;
            set => canReenter = value;
        }

        /// <summary>전이를 검사할 우선순위입니다. 값이 낮을수록 먼저 검사합니다.</summary>
        public int Priority
        {
            get => priority;
            set => priority = value;
        }

        /// <summary>전이 선의 중앙을 기준으로 전이 요약을 이동한 거리입니다.</summary>
        public Vector2 SummaryOffset
        {
            get => summaryOffset;
            set => summaryOffset = value;
        }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 두 노드를 연결하는 전이 데이터를 생성합니다.
        /// </summary>
        /// <param name="fromNodeIdentifier">전이가 시작되는 노드 식별자입니다.</param>
        /// <param name="toNodeIdentifier">전이가 도착하는 노드 식별자입니다.</param>
        /// <param name="operation">전이가 실행할 상태 변경 종류입니다.</param>
        public SWStateMachineTransitionData(
            string fromNodeIdentifier,
            string toNodeIdentifier,
            SWStateMachineTransitionOperation operation)
        {
            identifier = Guid.NewGuid().ToString("N");
            this.fromNodeIdentifier = fromNodeIdentifier;
            this.toNodeIdentifier = toNodeIdentifier;
            this.operation = operation;
            usesCommand = false;
            command = 0;
            conditionTypeName = string.Empty;
            canReenter = false;
            priority = 0;
            summaryOffset = Vector2.zero;
        }
        #endregion // 생성자

        #region 내부 함수
        /// <summary>식별자가 비어 있으면 새 식별자를 생성합니다.</summary>
        internal void EnsureIdentifier()
        {
            if (string.IsNullOrWhiteSpace(identifier))
                identifier = Guid.NewGuid().ToString("N");
        }
        #endregion // 내부 함수
    }

    /// <summary>
    /// 상태 노드와 연결 정보를 직렬화하여 저장하는 상태 머신 그래프 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWStateMachineGraph_", menuName = "SWTools/State Machine Graph")]
    public sealed class SWStateMachineGraphAsset : ScriptableObject
    {
        #region 필드
        [SerializeField] private SWStateMachineGraphType graphType;
        [SerializeField] private List<SWStateMachineNodeData> nodes = new List<SWStateMachineNodeData>();
        [SerializeField] private List<SWStateMachineTransitionData> transitions =
            new List<SWStateMachineTransitionData>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>그래프가 구성하는 상태 머신 종류입니다.</summary>
        public SWStateMachineGraphType GraphType
        {
            get => graphType;
            set => graphType = value;
        }

        /// <summary>그래프에 저장된 상태 노드 목록입니다.</summary>
        public IReadOnlyList<SWStateMachineNodeData> Nodes => nodes;

        /// <summary>그래프에 저장된 전이 목록입니다.</summary>
        public IReadOnlyList<SWStateMachineTransitionData> Transitions => transitions;
        #endregion // 프로퍼티

        #region 노드 관리
        /// <summary>
        /// 그래프에 새 노드를 추가합니다.
        /// </summary>
        public SWStateMachineNodeData AddNode(
            SWStateMachineNodeKind kind,
            string displayName,
            string stateTypeName,
            Rect position)
        {
            SWStateMachineNodeData node = new SWStateMachineNodeData(
                kind, displayName, stateTypeName, position);
            nodes.Add(node);
            return node;
        }

        /// <summary>
        /// 노드와 노드에 연결된 모든 전이를 제거합니다.
        /// </summary>
        /// <param name="identifier">제거할 노드 식별자입니다.</param>
        /// <returns>노드를 제거했으면 true입니다.</returns>
        public bool RemoveNode(string identifier)
        {
            int removedCount = nodes.RemoveAll(node => node.Identifier == identifier);
            transitions.RemoveAll(transition =>
                transition.FromNodeIdentifier == identifier ||
                transition.ToNodeIdentifier == identifier);
            return removedCount > 0;
        }

        /// <summary>
        /// 지정한 노드를 초기 상태로 설정하고 같은 범위의 다른 초기 상태를 해제합니다.
        /// </summary>
        /// <param name="identifier">초기 상태로 설정할 노드 식별자입니다.</param>
        public void SetInitialNode(string identifier)
        {
            if (!TryGetNode(identifier, out SWStateMachineNodeData selectedNode) ||
                selectedNode.Kind != SWStateMachineNodeKind.State)
                return;

            foreach (SWStateMachineNodeData node in nodes)
            {
                if (node.Kind != SWStateMachineNodeKind.State)
                    continue;

                bool isSameScope = graphType == SWStateMachineGraphType.Stack ||
                    node.Layer == selectedNode.Layer;
                if (isSameScope)
                    node.IsInitialState = node.Identifier == identifier;
            }
        }

        /// <summary>
        /// 식별자로 노드를 찾습니다.
        /// </summary>
        public bool TryGetNode(string identifier, out SWStateMachineNodeData node)
        {
            node = nodes.Find(item => item.Identifier == identifier);
            return node != null;
        }
        #endregion // 노드 관리

        #region 전이 관리
        /// <summary>
        /// 두 노드를 연결하는 전이를 추가합니다.
        /// </summary>
        public SWStateMachineTransitionData AddTransition(
            string fromNodeIdentifier,
            string toNodeIdentifier,
            SWStateMachineTransitionOperation operation)
        {
            SWStateMachineTransitionData transition = new SWStateMachineTransitionData(
                fromNodeIdentifier, toNodeIdentifier, operation);
            transitions.Add(transition);
            return transition;
        }

        /// <summary>
        /// 지정한 전이를 제거합니다.
        /// </summary>
        /// <param name="identifier">제거할 전이 식별자입니다.</param>
        /// <returns>전이를 제거했으면 true입니다.</returns>
        public bool RemoveTransition(string identifier)
        {
            return transitions.RemoveAll(transition => transition.Identifier == identifier) > 0;
        }

        /// <summary>
        /// 식별자로 전이를 찾습니다.
        /// </summary>
        public bool TryGetTransition(string identifier, out SWStateMachineTransitionData transition)
        {
            transition = transitions.Find(item => item.Identifier == identifier);
            return transition != null;
        }
        #endregion // 전이 관리

        #region Unity 생명주기
        /// <summary>에셋의 직렬화 데이터가 유효한지 정리합니다.</summary>
        private void OnValidate()
        {
            nodes ??= new List<SWStateMachineNodeData>();
            transitions ??= new List<SWStateMachineTransitionData>();

            nodes.RemoveAll(node => node == null);
            transitions.RemoveAll(transition => transition == null);

            foreach (SWStateMachineNodeData node in nodes)
            {
                node.EnsureIdentifier();
            }

            foreach (SWStateMachineTransitionData transition in transitions)
            {
                transition.EnsureIdentifier();
            }
        }
        #endregion // Unity 생명주기
    }
}
