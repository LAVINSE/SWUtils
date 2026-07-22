using System.Collections.Generic;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프의 노드와 연결 구성을 검사합니다.
    /// </summary>
    internal static class SWStateMachineGraphValidator
    {
        #region 함수
        /// <summary>
        /// 그래프에서 실행을 방해할 수 있는 구성 오류를 찾습니다.
        /// </summary>
        /// <param name="graphAsset">검사할 그래프 에셋입니다.</param>
        /// <returns>발견된 문제 설명 목록입니다.</returns>
        public static IReadOnlyList<string> Validate(SWStateMachineGraphAsset graphAsset)
        {
            List<string> messages = new List<string>();
            if (graphAsset == null)
            {
                messages.Add("검사할 그래프 에셋이 없습니다.");
                return messages;
            }

            if (graphAsset.Nodes.Count == 0)
            {
                messages.Add("상태 노드가 하나 이상 필요합니다.");
                return messages;
            }

            Dictionary<string, SWStateMachineNodeData> nodesByIdentifier =
                new Dictionary<string, SWStateMachineNodeData>();
            HashSet<string> registeredStateScopes = new HashSet<string>();
            HashSet<int> anyStateLayers = new HashSet<int>();
            int returnStateCount = 0;

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Identifier))
                {
                    messages.Add("식별자가 비어 있는 노드가 있습니다.");
                    continue;
                }

                if (!nodesByIdentifier.TryAdd(node.Identifier, node))
                    messages.Add($"중복된 노드 식별자가 있습니다: {node.Identifier}");

                if (node.Kind == SWStateMachineNodeKind.State &&
                    string.IsNullOrWhiteSpace(node.StateTypeName))
                    messages.Add($"{node.DisplayName} 노드에 상태 타입이 지정되지 않았습니다.");

                if (node.Kind == SWStateMachineNodeKind.State &&
                    !string.IsNullOrWhiteSpace(node.StateTypeName))
                {
                    string stateScope = graphAsset.GraphType == SWStateMachineGraphType.Stack
                        ? node.StateTypeName
                        : $"{node.Layer}:{node.StateTypeName}";
                    if (!registeredStateScopes.Add(stateScope))
                        messages.Add($"같은 범위에 상태 타입이 중복되어 있습니다: {node.DisplayName}");
                }

                if (graphAsset.GraphType == SWStateMachineGraphType.Layered &&
                    node.Kind == SWStateMachineNodeKind.Return)
                    messages.Add("Layered 그래프에는 Return State 노드를 사용할 수 없습니다.");

                if (graphAsset.GraphType == SWStateMachineGraphType.Stack &&
                    node.Kind == SWStateMachineNodeKind.AnyState)
                    messages.Add("Stack 그래프에는 Any State 노드를 사용할 수 없습니다.");

                if (node.Kind == SWStateMachineNodeKind.AnyState &&
                    !anyStateLayers.Add(node.Layer))
                    messages.Add($"Layer {node.Layer}에 Any State 노드가 여러 개 있습니다.");
                if (node.Kind == SWStateMachineNodeKind.Return)
                    returnStateCount++;
            }

            if (returnStateCount > 1)
                messages.Add("Stack 그래프에는 Return State 노드를 하나만 사용하는 것이 허용됩니다.");

            ValidateInitialStates(graphAsset, messages);

            HashSet<string> nodesWithIncomingTransition = new HashSet<string>();
            foreach (SWStateMachineTransitionData transition in graphAsset.Transitions)
            {
                if (!transition.UsesCommand && string.IsNullOrWhiteSpace(transition.ConditionTypeName))
                    messages.Add($"명령이나 조건이 지정되지 않은 연결이 있습니다: {transition.Identifier}");

                if (!string.IsNullOrWhiteSpace(transition.ConditionTypeName) &&
                    SWStateMachineGraphTypeResolver.Resolve(transition.ConditionTypeName) == null)
                    messages.Add($"조건 타입을 찾을 수 없습니다: {transition.ConditionTypeName}");

                if (!nodesByIdentifier.TryGetValue(
                    transition.FromNodeIdentifier,
                    out SWStateMachineNodeData fromNode))
                {
                    messages.Add($"출발 노드를 찾을 수 없는 연결이 있습니다: {transition.Identifier}");
                    continue;
                }

                if (!nodesByIdentifier.TryGetValue(
                    transition.ToNodeIdentifier,
                    out SWStateMachineNodeData toNode))
                {
                    messages.Add($"도착 노드를 찾을 수 없는 연결이 있습니다: {transition.Identifier}");
                    continue;
                }

                nodesWithIncomingTransition.Add(toNode.Identifier);

                if (graphAsset.GraphType == SWStateMachineGraphType.Layered)
                    ValidateLayeredTransition(transition, fromNode, toNode, messages);
                else
                    ValidateStackTransition(transition, fromNode, toNode, messages);
            }

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (node.Kind == SWStateMachineNodeKind.State &&
                    !node.IsInitialState &&
                    !nodesWithIncomingTransition.Contains(node.Identifier))
                {
                    messages.Add($"Root 흐름에서 진입할 수 없는 상태입니다: {node.DisplayName}");
                }
            }

            return messages;
        }

        /// <summary>그래프 종류에 맞는 초기 상태 구성을 검사합니다.</summary>
        private static void ValidateInitialStates(
            SWStateMachineGraphAsset graphAsset,
            List<string> messages)
        {
            if (graphAsset.GraphType == SWStateMachineGraphType.Stack)
            {
                int initialStateCount = 0;
                foreach (SWStateMachineNodeData node in graphAsset.Nodes)
                {
                    if (node.Kind == SWStateMachineNodeKind.State && node.IsInitialState)
                        initialStateCount++;
                }

                if (initialStateCount != 1)
                    messages.Add("스택 상태 머신에는 초기 상태가 정확히 하나 필요합니다.");

                return;
            }

            Dictionary<int, int> stateCountsByLayer = new Dictionary<int, int>();
            Dictionary<int, int> initialCountsByLayer = new Dictionary<int, int>();

            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (node.Kind != SWStateMachineNodeKind.State)
                    continue;

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
                    messages.Add($"Layer {layer}에는 초기 상태가 정확히 하나 필요합니다.");
            }
        }

        /// <summary>유한 상태 머신 연결 규칙을 검사합니다.</summary>
        private static void ValidateLayeredTransition(
            SWStateMachineTransitionData transition,
            SWStateMachineNodeData fromNode,
            SWStateMachineNodeData toNode,
            List<string> messages)
        {
            if (transition.Operation != SWStateMachineTransitionOperation.Transition)
                messages.Add($"유한 상태 머신 연결은 Transition 동작만 사용할 수 있습니다: {transition.Identifier}");

            if (toNode.Kind != SWStateMachineNodeKind.State)
                messages.Add($"상태 전이는 상태 노드로만 연결할 수 있습니다: {transition.Identifier}");

            if (fromNode.Layer != toNode.Layer)
                messages.Add($"서로 다른 Layer의 노드를 연결할 수 없습니다: {transition.Identifier}");
        }

        /// <summary>스택 상태 머신 연결 규칙을 검사합니다.</summary>
        private static void ValidateStackTransition(
            SWStateMachineTransitionData transition,
            SWStateMachineNodeData fromNode,
            SWStateMachineNodeData toNode,
            List<string> messages)
        {
            if (fromNode.Kind != SWStateMachineNodeKind.State)
                messages.Add($"스택 연결은 상태 노드에서 시작해야 합니다: {transition.Identifier}");

            if (transition.Operation == SWStateMachineTransitionOperation.Pop)
            {
                if (toNode.Kind != SWStateMachineNodeKind.Return)
                    messages.Add($"Pop 전이는 Return State 노드에 연결해야 합니다: {transition.Identifier}");
            }
            else if (toNode.Kind != SWStateMachineNodeKind.State)
            {
                messages.Add($"Push 또는 Replace 연결은 상태 노드로 연결해야 합니다: {transition.Identifier}");
            }

            if (transition.Operation == SWStateMachineTransitionOperation.Push &&
                fromNode.Identifier == toNode.Identifier)
                messages.Add($"현재 스택 상태를 자기 자신 위에 다시 추가할 수 없습니다: {transition.Identifier}");
        }
        #endregion // 함수
    }
}
