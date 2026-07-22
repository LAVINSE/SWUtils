using System;
using System.Collections.Generic;

namespace SW.StateMachine
{
    /// <summary>
    /// 스택 그래프의 연결 조건과 명령을 평가하여 스택 상태 머신을 제어합니다.
    /// </summary>
    /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
    public sealed class SWStackStateMachineGraphController<TContext>
    {
        #region 필드
        private readonly SWStateMachineGraphAsset graphAsset;
        private readonly Dictionary<string, SWStateMachineNodeData> nodesByIdentifier;
        private readonly Dictionary<Type, SWStateMachineNodeData> nodesByStateType =
            new Dictionary<Type, SWStateMachineNodeData>();
        private readonly Dictionary<string, SWStateMachineGraphCondition<TContext>> conditionsByTransition =
            new Dictionary<string, SWStateMachineGraphCondition<TContext>>();
        private readonly List<SWStateMachineTransitionData> orderedTransitions;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>그래프 구성으로 실행되는 스택 상태 머신입니다.</summary>
        public SWStackStateMachine<TContext> StateMachine { get; }

        /// <summary>상태 머신이 제어하는 문맥입니다.</summary>
        public TContext Context => StateMachine.Context;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>그래프 에셋으로 스택 상태 머신 제어기를 구성하고 시작합니다.</summary>
        internal SWStackStateMachineGraphController(
            SWStateMachineGraphAsset graphAsset,
            TContext context)
        {
            this.graphAsset = graphAsset;
            nodesByIdentifier = SWStateMachineGraphFactory.CreateNodeLookup(graphAsset);
            orderedTransitions = SWStateMachineGraphFactory.CreateOrderedTransitions(graphAsset);
            StateMachine = new SWStackStateMachine<TContext>(context);

            Type initialStateType = null;

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (node.Kind != SWStateMachineNodeKind.State)
                    continue;

                Type stateType = SWStateMachineGraphFactory.ResolveStateType<SWStackState<TContext>>(node);
                StateMachine.AddState(stateType);

                if (!nodesByStateType.TryAdd(stateType, node))
                    throw new InvalidOperationException($"같은 스택 상태 타입을 여러 노드에 사용할 수 없습니다: {stateType.FullName}");

                if (node.IsInitialState)
                {
                    if (initialStateType != null)
                        throw new InvalidOperationException("스택 상태 머신 그래프에는 초기 상태가 하나만 있어야 합니다.");

                    initialStateType = stateType;
                }
            }

            if (initialStateType == null)
                throw new InvalidOperationException("스택 상태 머신 그래프에 초기 상태가 없습니다.");

            foreach (SWStateMachineTransitionData transition in orderedTransitions)
            {
                SWStateMachineGraphCondition<TContext> condition =
                    SWStateMachineGraphFactory.CreateCondition<TContext>(transition.ConditionTypeName);
                if (!transition.UsesCommand && condition == null)
                    throw new InvalidOperationException($"명령이나 조건이 없는 스택 연결입니다: {transition.Identifier}");

                conditionsByTransition[transition.Identifier] = condition;
            }

            StateMachine.Start(initialStateType);
        }
        #endregion // 생성자

        #region 실행
        /// <summary>
        /// 자동 연결을 확인하고 전이하지 않았다면 최상단 상태를 갱신합니다.
        /// </summary>
        /// <param name="deltaTime">이전 갱신 이후 흐른 시간입니다.</param>
        public void Tick(float deltaTime)
        {
            if (!TryExecuteTransition(null))
                StateMachine.Tick(deltaTime);
        }

        /// <summary>
        /// 현재 상태에서 명령과 일치하는 첫 연결을 실행합니다.
        /// </summary>
        /// <param name="command">실행할 명령 번호입니다.</param>
        /// <returns>연결을 실행했으면 true입니다.</returns>
        public bool ExecuteCommand(int command)
        {
            return TryExecuteTransition(command);
        }

        /// <summary>
        /// 현재 상태에서 열거형 명령과 일치하는 첫 연결을 실행합니다.
        /// </summary>
        public bool ExecuteCommand(Enum command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return ExecuteCommand(Convert.ToInt32(command));
        }

        /// <summary>스택 상태 머신을 정지합니다.</summary>
        public void Stop()
        {
            StateMachine.Stop();
        }

        /// <summary>현재 상태와 조건 또는 명령이 일치하는 연결을 찾아 실행합니다.</summary>
        private bool TryExecuteTransition(int? command)
        {
            SWStackState<TContext> currentState = StateMachine.CurrentState;
            if (currentState == null || !nodesByStateType.TryGetValue(
                currentState.GetType(),
                out SWStateMachineNodeData currentNode))
                return false;

            foreach (SWStateMachineTransitionData transition in orderedTransitions)
            {
                if (transition.FromNodeIdentifier != currentNode.Identifier)
                    continue;

                if (command.HasValue != transition.UsesCommand)
                    continue;

                if (command.HasValue && transition.Command != command.Value)
                    continue;

                SWStateMachineGraphCondition<TContext> condition = conditionsByTransition[transition.Identifier];
                if (condition != null && !condition.Evaluate(Context))
                    continue;

                ExecuteTransition(transition);
                return true;
            }

            return false;
        }

        /// <summary>그래프 연결에 지정된 스택 변경 동작을 실행합니다.</summary>
        private void ExecuteTransition(SWStateMachineTransitionData transition)
        {
            if (transition.Operation == SWStateMachineTransitionOperation.Pop)
            {
                StateMachine.Pop();
                return;
            }

            SWStateMachineNodeData toNode = SWStateMachineGraphFactory.GetNode(
                nodesByIdentifier,
                transition.ToNodeIdentifier);
            Type stateType = SWStateMachineGraphFactory.ResolveStateType<SWStackState<TContext>>(toNode);

            if (transition.Operation == SWStateMachineTransitionOperation.Replace)
                StateMachine.Replace(stateType);
            else if (transition.Operation == SWStateMachineTransitionOperation.Push)
                StateMachine.Push(stateType);
            else
                throw new InvalidOperationException($"스택 그래프에서 지원하지 않는 연결 동작입니다: {transition.Operation}");
        }
        #endregion // 실행

        #region 메시지
        /// <summary>현재 스택 상태에 메시지를 전달합니다.</summary>
        public bool SendMessage(int message, object data = null)
        {
            return StateMachine.SendMessage(message, data);
        }

        /// <summary>현재 스택 상태에 열거형 메시지를 전달합니다.</summary>
        public bool SendMessage(Enum message, object data = null)
        {
            return StateMachine.SendMessage(message, data);
        }
        #endregion // 메시지
    }
}

