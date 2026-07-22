using System;

namespace SW.BehaviourTree
{
    /// <summary>성공하는 자식을 찾을 때까지 순서대로 실행하는 Composite 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourSelectorNode : SWBehaviourCompositeNode
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
                if (childStatus == SWBehaviourStatus.Running || childStatus == SWBehaviourStatus.Success)
                    return childStatus;
                currentChildIndex++;
            }
            return SWBehaviourStatus.Failure;
        }
    }
}
