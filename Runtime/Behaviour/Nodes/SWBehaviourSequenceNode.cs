using System;

namespace SW.BehaviourTree
{
    /// <summary>자식을 순서대로 실행하며 하나라도 실패하면 실패하는 Composite 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourSequenceNode : SWBehaviourCompositeNode
    {
        [NonSerialized] private int currentChildIndex;

        protected override void OnStart(SWBehaviourContext context) => currentChildIndex = 0;

        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            while (currentChildIndex < ChildIdentifiers.Count)
            {
                if (!tree.TryGetNode(ChildIdentifiers[currentChildIndex], out SWBehaviourNode child))
                    return SWBehaviourStatus.Failure;
                SWBehaviourStatus childStatus = child.Tick(context, tree);
                if (childStatus == SWBehaviourStatus.Running || childStatus == SWBehaviourStatus.Failure)
                    return childStatus;
                currentChildIndex++;
            }
            return SWBehaviourStatus.Success;
        }
    }
}
