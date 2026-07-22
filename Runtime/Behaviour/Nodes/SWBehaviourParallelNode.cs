using System;
using System.Collections.Generic;

namespace SW.BehaviourTree
{
    /// <summary>모든 자식을 함께 실행하고 지정한 수가 성공하면 성공하는 Composite 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourParallelNode : SWBehaviourCompositeNode
    {
        [UnityEngine.SerializeField, UnityEngine.Min(1)] private int requiredSuccessCount = 1;
        [NonSerialized] private Dictionary<string, SWBehaviourStatus> completedStatuses;

        protected override void OnStart(SWBehaviourContext context)
        {
            completedStatuses ??= new Dictionary<string, SWBehaviourStatus>(StringComparer.Ordinal);
            completedStatuses.Clear();
        }

        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (ChildIdentifiers.Count == 0)
                return SWBehaviourStatus.Failure;

            int successCount = 0;
            int failureCount = 0;
            for (int index = 0; index < ChildIdentifiers.Count; index++)
            {
                if (completedStatuses.TryGetValue(
                    ChildIdentifiers[index], out SWBehaviourStatus completedStatus))
                {
                    if (completedStatus == SWBehaviourStatus.Success) successCount++;
                    else failureCount++;
                    continue;
                }
                if (!tree.TryGetNode(ChildIdentifiers[index], out SWBehaviourNode child))
                {
                    failureCount++;
                    completedStatuses[ChildIdentifiers[index]] = SWBehaviourStatus.Failure;
                    continue;
                }
                SWBehaviourStatus childStatus = child.Tick(context, tree);
                if (childStatus == SWBehaviourStatus.Success)
                {
                    successCount++;
                    completedStatuses[ChildIdentifiers[index]] = childStatus;
                }
                else if (childStatus == SWBehaviourStatus.Failure)
                {
                    failureCount++;
                    completedStatuses[ChildIdentifiers[index]] = childStatus;
                }
            }

            int required = Math.Min(Math.Max(1, requiredSuccessCount), ChildIdentifiers.Count);
            if (successCount >= required) return SWBehaviourStatus.Success;
            if (failureCount > ChildIdentifiers.Count - required) return SWBehaviourStatus.Failure;
            return SWBehaviourStatus.Running;
        }

        protected override void OnStop(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree,
            SWBehaviourStatus status)
        {
            for (int index = 0; index < ChildIdentifiers.Count; index++)
            {
                if (!completedStatuses.ContainsKey(ChildIdentifiers[index]) &&
                    tree.TryGetNode(ChildIdentifiers[index], out SWBehaviourNode child))
                {
                    child.Abort(context, tree);
                }
            }
        }
    }
}
