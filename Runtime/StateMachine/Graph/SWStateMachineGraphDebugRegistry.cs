using System;
using System.Collections.Generic;
using UnityEngine;

namespace SW.StateMachine
{
    /// <summary>실행 중인 그래프 상태 하나의 디버그 정보입니다.</summary>
    public sealed class SWStateMachineGraphActiveNodeDebugData
    {
        public string NodeIdentifier { get; internal set; }
        public float ActiveDuration { get; internal set; }
    }

    /// <summary>최근 상태 전이 하나의 디버그 정보입니다.</summary>
    public sealed class SWStateMachineGraphTransitionDebugData
    {
        public string TransitionIdentifier { get; internal set; }
        public string FromNodeIdentifier { get; internal set; }
        public string ToNodeIdentifier { get; internal set; }
        public double Time { get; internal set; }
    }

    /// <summary>그래프 편집기에 전달되는 상태 머신 실행 스냅샷입니다.</summary>
    public sealed class SWStateMachineGraphDebugSnapshot
    {
        internal readonly List<SWStateMachineGraphActiveNodeDebugData> activeNodes = new();
        internal readonly List<SWStateMachineGraphTransitionDebugData> transitionHistory = new();

        public UnityEngine.Object Owner { get; internal set; }
        public IReadOnlyList<SWStateMachineGraphActiveNodeDebugData> ActiveNodes => activeNodes;
        public IReadOnlyList<SWStateMachineGraphTransitionDebugData> TransitionHistory => transitionHistory;
    }

    /// <summary>실행 중인 그래프 상태 머신을 편집기 디버거와 연결합니다.</summary>
    public static class SWStateMachineGraphDebugRegistry
    {
        private const int MaximumHistoryCount = 32;
        private static readonly List<IDebugProvider> providers = new();

        /// <summary>선택한 Owner와 그래프에 대응하는 최신 실행 스냅샷을 반환합니다.</summary>
        public static bool TryGetSnapshot(
            SWStateMachineGraphAsset graphAsset,
            GameObject selectedGameObject,
            out SWStateMachineGraphDebugSnapshot snapshot)
        {
            snapshot = null;
            for (int index = providers.Count - 1; index >= 0; index--)
            {
                IDebugProvider provider = providers[index];
                if (!provider.IsAlive)
                {
                    providers.RemoveAt(index);
                    continue;
                }
                if (provider.GraphAsset != graphAsset ||
                    (selectedGameObject != null && provider.OwnerGameObject != selectedGameObject))
                    continue;
                snapshot = provider.CreateSnapshot();
                return snapshot != null;
            }
            return false;
        }

        internal static void RegisterLayered<TContext>(
            SWStateMachineGraphAsset graphAsset,
            TContext context,
            SWStateMachine<TContext> stateMachine)
        {
            LayeredDebugProvider<TContext> provider = new(graphAsset, context, stateMachine);
            stateMachine.StateChanged += provider.OnStateChanged;
            providers.Add(provider);
        }

        internal static void RegisterStack<TContext>(
            SWStateMachineGraphAsset graphAsset,
            TContext context,
            SWStackStateMachine<TContext> stateMachine)
        {
            StackDebugProvider<TContext> provider = new(graphAsset, context, stateMachine);
            stateMachine.StateChanged += provider.OnStateChanged;
            providers.Add(provider);
        }

        private interface IDebugProvider
        {
            SWStateMachineGraphAsset GraphAsset { get; }
            GameObject OwnerGameObject { get; }
            bool IsAlive { get; }
            SWStateMachineGraphDebugSnapshot CreateSnapshot();
        }

        private abstract class DebugProviderBase : IDebugProvider
        {
            private readonly List<SWStateMachineGraphTransitionDebugData> history = new();
            private readonly Dictionary<string, double> activeStartTimes = new(StringComparer.Ordinal);

            public SWStateMachineGraphAsset GraphAsset { get; }
            public UnityEngine.Object Owner { get; }
            public GameObject OwnerGameObject { get; }
            public abstract bool IsAlive { get; }

            protected DebugProviderBase(SWStateMachineGraphAsset graphAsset, object context)
            {
                GraphAsset = graphAsset;
                Owner = context as UnityEngine.Object;
                OwnerGameObject = context switch
                {
                    GameObject gameObject => gameObject,
                    Component component => component.gameObject,
                    _ => null,
                };
            }

            public SWStateMachineGraphDebugSnapshot CreateSnapshot()
            {
                List<string> activeIdentifiers = GetActiveNodeIdentifiers();
                double now = Time.realtimeSinceStartupAsDouble;
                HashSet<string> activeSet = new(activeIdentifiers, StringComparer.Ordinal);
                List<string> removedIdentifiers = new();
                foreach (string identifier in activeStartTimes.Keys)
                {
                    if (!activeSet.Contains(identifier))
                        removedIdentifiers.Add(identifier);
                }
                for (int index = 0; index < removedIdentifiers.Count; index++)
                    activeStartTimes.Remove(removedIdentifiers[index]);

                SWStateMachineGraphDebugSnapshot snapshot = new() { Owner = Owner };
                for (int index = 0; index < activeIdentifiers.Count; index++)
                {
                    string identifier = activeIdentifiers[index];
                    if (!activeStartTimes.TryGetValue(identifier, out double startedAt))
                    {
                        startedAt = now;
                        activeStartTimes[identifier] = startedAt;
                    }
                    snapshot.activeNodes.Add(new SWStateMachineGraphActiveNodeDebugData
                    {
                        NodeIdentifier = identifier,
                        ActiveDuration = (float)(now - startedAt),
                    });
                }
                snapshot.transitionHistory.AddRange(history);
                return snapshot;
            }

            protected void RecordTransition(string fromIdentifier, string toIdentifier)
            {
                string transitionIdentifier = FindTransitionIdentifier(fromIdentifier, toIdentifier);
                history.Insert(0, new SWStateMachineGraphTransitionDebugData
                {
                    TransitionIdentifier = transitionIdentifier,
                    FromNodeIdentifier = fromIdentifier,
                    ToNodeIdentifier = toIdentifier,
                    Time = Time.realtimeSinceStartupAsDouble,
                });
                if (history.Count > MaximumHistoryCount)
                    history.RemoveRange(MaximumHistoryCount, history.Count - MaximumHistoryCount);
            }

            protected string FindNodeIdentifier(Type stateType, int? layer = null)
            {
                if (stateType == null)
                    return string.Empty;
                for (int index = 0; index < GraphAsset.Nodes.Count; index++)
                {
                    SWStateMachineNodeData node = GraphAsset.Nodes[index];
                    if (node.Kind == SWStateMachineNodeKind.State &&
                        SWStateMachineGraphTypeResolver.Resolve(node.StateTypeName) == stateType &&
                        (!layer.HasValue || node.Layer == layer.Value))
                        return node.Identifier;
                }
                return string.Empty;
            }

            protected abstract List<string> GetActiveNodeIdentifiers();

            private string FindTransitionIdentifier(string fromIdentifier, string toIdentifier)
            {
                for (int index = 0; index < GraphAsset.Transitions.Count; index++)
                {
                    SWStateMachineTransitionData transition = GraphAsset.Transitions[index];
                    if (transition.FromNodeIdentifier == fromIdentifier &&
                        transition.ToNodeIdentifier == toIdentifier)
                        return transition.Identifier;
                }
                for (int index = 0; index < GraphAsset.Transitions.Count; index++)
                {
                    SWStateMachineTransitionData transition = GraphAsset.Transitions[index];
                    bool isAnyStateTransition = transition.ToNodeIdentifier == toIdentifier &&
                        GraphAsset.TryGetNode(
                            transition.FromNodeIdentifier, out SWStateMachineNodeData fromNode) &&
                        fromNode.Kind == SWStateMachineNodeKind.AnyState;
                    bool isReturnTransition = transition.FromNodeIdentifier == fromIdentifier &&
                        GraphAsset.TryGetNode(
                            transition.ToNodeIdentifier, out SWStateMachineNodeData toNode) &&
                        toNode.Kind == SWStateMachineNodeKind.Return;
                    if (isAnyStateTransition || isReturnTransition)
                        return transition.Identifier;
                }
                return string.Empty;
            }
        }

        private sealed class LayeredDebugProvider<TContext> : DebugProviderBase
        {
            private readonly WeakReference<SWStateMachine<TContext>> stateMachineReference;

            public override bool IsAlive => stateMachineReference.TryGetTarget(out _);

            public LayeredDebugProvider(
                SWStateMachineGraphAsset graphAsset,
                TContext context,
                SWStateMachine<TContext> stateMachine) : base(graphAsset, context)
            {
                stateMachineReference = new WeakReference<SWStateMachine<TContext>>(stateMachine);
            }

            public void OnStateChanged(
                SWStateMachine<TContext> stateMachine,
                SWState<TContext> currentState,
                SWState<TContext> previousState,
                int layer)
            {
                RecordTransition(
                    FindNodeIdentifier(previousState?.GetType(), layer),
                    FindNodeIdentifier(currentState?.GetType(), layer));
            }

            protected override List<string> GetActiveNodeIdentifiers()
            {
                List<string> identifiers = new();
                if (!stateMachineReference.TryGetTarget(out SWStateMachine<TContext> stateMachine) ||
                    !stateMachine.IsRunning)
                    return identifiers;
                HashSet<int> layers = new();
                for (int index = 0; index < GraphAsset.Nodes.Count; index++)
                {
                    SWStateMachineNodeData node = GraphAsset.Nodes[index];
                    if (node.Kind == SWStateMachineNodeKind.State)
                        layers.Add(node.Layer);
                }
                foreach (int layer in layers)
                {
                    string identifier = FindNodeIdentifier(stateMachine.GetCurrentStateType(layer), layer);
                    if (!string.IsNullOrEmpty(identifier)) identifiers.Add(identifier);
                }
                return identifiers;
            }
        }

        private sealed class StackDebugProvider<TContext> : DebugProviderBase
        {
            private readonly WeakReference<SWStackStateMachine<TContext>> stateMachineReference;

            public override bool IsAlive => stateMachineReference.TryGetTarget(out _);

            public StackDebugProvider(
                SWStateMachineGraphAsset graphAsset,
                TContext context,
                SWStackStateMachine<TContext> stateMachine) : base(graphAsset, context)
            {
                stateMachineReference = new WeakReference<SWStackStateMachine<TContext>>(stateMachine);
            }

            public void OnStateChanged(
                SWStackStateMachine<TContext> stateMachine,
                SWStackState<TContext> currentState,
                SWStackState<TContext> previousState,
                SWStackStateOperation operation)
            {
                RecordTransition(
                    FindNodeIdentifier(previousState?.GetType()),
                    FindNodeIdentifier(currentState?.GetType()));
            }

            protected override List<string> GetActiveNodeIdentifiers()
            {
                List<string> identifiers = new();
                if (!stateMachineReference.TryGetTarget(out SWStackStateMachine<TContext> stateMachine) ||
                    !stateMachine.IsRunning || stateMachine.CurrentState == null)
                    return identifiers;
                string identifier = FindNodeIdentifier(stateMachine.CurrentState.GetType());
                if (!string.IsNullOrEmpty(identifier)) identifiers.Add(identifier);
                return identifiers;
            }
        }
    }
}
