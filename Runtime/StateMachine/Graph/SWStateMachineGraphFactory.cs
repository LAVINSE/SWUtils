using System;
using System.Collections.Generic;

namespace SW.StateMachine
{
    /// <summary>
    /// 그래프 에셋의 상태 타입과 전이를 런타임 상태 머신으로 구성합니다.
    /// </summary>
    public static class SWStateMachineGraphFactory
    {
        #region 다중 계층 상태 머신
        /// <summary>
        /// 그래프 에셋으로 다중 계층 유한 상태 머신을 생성하고 시작합니다.
        /// </summary>
        /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
        /// <param name="graphAsset">구성에 사용할 다중 계층 그래프 에셋입니다.</param>
        /// <param name="context">상태들이 제어할 문맥입니다.</param>
        /// <returns>그래프 구성으로 시작된 상태 머신입니다.</returns>
        public static SWStateMachine<TContext> CreateLayered<TContext>(
            SWStateMachineGraphAsset graphAsset,
            TContext context)
        {
            EnsureGraphType(graphAsset, SWStateMachineGraphType.Layered);
            SWStateMachine<TContext> stateMachine = new SWStateMachine<TContext>(context);
            Dictionary<string, SWStateMachineNodeData> nodesByIdentifier = CreateNodeLookup(graphAsset);
            Dictionary<int, int> stateCountsByLayer = new Dictionary<int, int>();
            Dictionary<int, int> initialCountsByLayer = new Dictionary<int, int>();

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (node.Kind != SWStateMachineNodeKind.State)
                    continue;

                Type stateType = ResolveStateType<SWState<TContext>>(node);
                stateMachine.AddState(stateType, node.Layer);
                stateCountsByLayer.TryGetValue(node.Layer, out int stateCount);
                stateCountsByLayer[node.Layer] = stateCount + 1;

                if (node.IsInitialState)
                {
                    initialCountsByLayer.TryGetValue(node.Layer, out int initialCount);
                    initialCountsByLayer[node.Layer] = initialCount + 1;
                }
            }

            foreach (int layer in stateCountsByLayer.Keys)
            {
                initialCountsByLayer.TryGetValue(layer, out int initialCount);
                if (initialCount != 1)
                    throw new InvalidOperationException($"{layer}번 계층에는 초기 상태가 정확히 하나 필요합니다.");
            }

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (!node.IsInitialState || node.Kind != SWStateMachineNodeKind.State)
                    continue;

                stateMachine.SetInitialState(
                    ResolveStateType<SWState<TContext>>(node),
                    node.Layer);
            }

            List<SWStateMachineTransitionData> orderedTransitions = CreateOrderedTransitions(graphAsset);
            foreach (SWStateMachineTransitionData transition in orderedTransitions)
            {
                AddLayeredTransition(stateMachine, transition, nodesByIdentifier);
            }

            stateMachine.Start();
            SWStateMachineGraphDebugRegistry.RegisterLayered(graphAsset, context, stateMachine);
            return stateMachine;
        }

        /// <summary>그래프 연결 하나를 다중 계층 상태 머신에 등록합니다.</summary>
        private static void AddLayeredTransition<TContext>(
            SWStateMachine<TContext> stateMachine,
            SWStateMachineTransitionData transition,
            Dictionary<string, SWStateMachineNodeData> nodesByIdentifier)
        {
            if (transition.Operation != SWStateMachineTransitionOperation.Transition)
                throw new InvalidOperationException("다중 계층 그래프에서는 일반 상태 전이 동작만 사용할 수 있습니다.");

            SWStateMachineNodeData fromNode = GetNode(nodesByIdentifier, transition.FromNodeIdentifier);
            SWStateMachineNodeData toNode = GetNode(nodesByIdentifier, transition.ToNodeIdentifier);

            if (toNode.Kind != SWStateMachineNodeKind.State)
                throw new InvalidOperationException("다중 계층 그래프 전이의 도착 노드는 상태 노드여야 합니다.");

            Type toStateType = ResolveStateType<SWState<TContext>>(toNode);
            int? command = transition.UsesCommand ? transition.Command : null;
            SWStateMachineGraphCondition<TContext> graphCondition =
                CreateCondition<TContext>(transition.ConditionTypeName);
            Func<SWState<TContext>, bool> condition = graphCondition == null
                ? null
                : state => graphCondition.Evaluate(state.Context);

            EnsureTransitionTrigger(transition, condition);

            if (fromNode.Kind == SWStateMachineNodeKind.AnyState)
            {
                stateMachine.AddGraphAnyTransition(
                    toStateType,
                    command,
                    condition,
                    toNode.Layer,
                    transition.CanReenter);
                return;
            }

            Type fromStateType = ResolveStateType<SWState<TContext>>(fromNode);
            if (fromNode.Layer != toNode.Layer)
                throw new InvalidOperationException("서로 다른 계층의 상태를 연결할 수 없습니다.");

            stateMachine.AddGraphTransition(
                fromStateType,
                toStateType,
                command,
                condition,
                fromNode.Layer,
                transition.CanReenter);
        }
        #endregion // 다중 계층 상태 머신

        #region 스택 상태 머신
        /// <summary>
        /// 그래프 에셋으로 스택 상태 머신 제어기를 생성하고 시작합니다.
        /// </summary>
        /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
        /// <param name="graphAsset">구성에 사용할 스택 그래프 에셋입니다.</param>
        /// <param name="context">상태들이 제어할 문맥입니다.</param>
        /// <returns>그래프 구성으로 시작된 스택 상태 머신 제어기입니다.</returns>
        public static SWStackStateMachineGraphController<TContext> CreateStack<TContext>(
            SWStateMachineGraphAsset graphAsset,
            TContext context)
        {
            EnsureGraphType(graphAsset, SWStateMachineGraphType.Stack);
            SWStackStateMachineGraphController<TContext> controller =
                new SWStackStateMachineGraphController<TContext>(graphAsset, context);
            SWStateMachineGraphDebugRegistry.RegisterStack(
                graphAsset,
                context,
                controller.StateMachine);
            return controller;
        }
        #endregion // 스택 상태 머신

        #region 공통 구성
        /// <summary>그래프 에셋 종류가 요청한 상태 머신 종류와 같은지 확인합니다.</summary>
        private static void EnsureGraphType(
            SWStateMachineGraphAsset graphAsset,
            SWStateMachineGraphType expectedType)
        {
            if (graphAsset == null)
                throw new ArgumentNullException(nameof(graphAsset));

            if (graphAsset.GraphType != expectedType)
                throw new ArgumentException($"{expectedType} 종류의 상태 머신 그래프가 필요합니다.", nameof(graphAsset));
        }

        /// <summary>그래프 노드를 식별자로 조회하는 사전을 생성합니다.</summary>
        internal static Dictionary<string, SWStateMachineNodeData> CreateNodeLookup(
            SWStateMachineGraphAsset graphAsset)
        {
            Dictionary<string, SWStateMachineNodeData> nodesByIdentifier =
                new Dictionary<string, SWStateMachineNodeData>();

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (!nodesByIdentifier.TryAdd(node.Identifier, node))
                    throw new InvalidOperationException($"중복된 그래프 노드 식별자가 있습니다: {node.Identifier}");
            }

            return nodesByIdentifier;
        }

        /// <summary>우선순위가 낮은 값부터 정렬한 그래프 전이 목록을 생성합니다.</summary>
        internal static List<SWStateMachineTransitionData> CreateOrderedTransitions(
            SWStateMachineGraphAsset graphAsset)
        {
            List<SWStateMachineTransitionData> transitions =
                new List<SWStateMachineTransitionData>(graphAsset.Transitions);
            transitions.Sort((left, right) => left.Priority.CompareTo(right.Priority));
            return transitions;
        }

        /// <summary>식별자로 그래프 노드를 반환합니다.</summary>
        internal static SWStateMachineNodeData GetNode(
            Dictionary<string, SWStateMachineNodeData> nodesByIdentifier,
            string identifier)
        {
            if (!nodesByIdentifier.TryGetValue(identifier, out SWStateMachineNodeData node))
                throw new KeyNotFoundException($"그래프 노드를 찾을 수 없습니다: {identifier}");

            return node;
        }

        /// <summary>그래프 노드에 저장된 런타임 상태 타입을 확인하여 반환합니다.</summary>
        internal static Type ResolveStateType<TState>(SWStateMachineNodeData node)
        {
            Type stateType = SWStateMachineGraphTypeResolver.Resolve(node.StateTypeName);
            if (stateType == null || stateType.IsAbstract || !typeof(TState).IsAssignableFrom(stateType))
                throw new InvalidOperationException($"그래프 상태 타입을 사용할 수 없습니다: {node.StateTypeName}");

            return stateType;
        }

        /// <summary>전이 데이터에 지정된 조건 타입을 생성합니다.</summary>
        internal static SWStateMachineGraphCondition<TContext> CreateCondition<TContext>(string conditionTypeName)
        {
            if (string.IsNullOrWhiteSpace(conditionTypeName))
                return null;

            Type conditionType = SWStateMachineGraphTypeResolver.Resolve(conditionTypeName);
            if (conditionType == null ||
                conditionType.IsAbstract ||
                !typeof(SWStateMachineGraphCondition<TContext>).IsAssignableFrom(conditionType))
                throw new InvalidOperationException($"그래프 조건 타입을 사용할 수 없습니다: {conditionTypeName}");

            SWStateMachineGraphCondition<TContext> condition =
                Activator.CreateInstance(conditionType) as SWStateMachineGraphCondition<TContext>;
            if (condition == null)
                throw new InvalidOperationException($"그래프 조건을 생성할 수 없습니다: {conditionTypeName}");

            return condition;
        }

        /// <summary>전이에 명령이나 조건 중 하나 이상이 있는지 확인합니다.</summary>
        internal static void EnsureTransitionTrigger<TState>(
            SWStateMachineTransitionData transition,
            Func<TState, bool> condition)
        {
            if (!transition.UsesCommand && condition == null)
                throw new InvalidOperationException($"명령이나 조건이 없는 그래프 전이입니다: {transition.Identifier}");
        }
        #endregion // 공통 구성
    }
}
