using System;

namespace SW.BehaviourTree
{
    /// <summary>자식이 끝나면 결과와 관계없이 실패를 반환합니다.</summary>
    [Serializable]
    public sealed class SWBehaviourFailureNode : SWBehaviourDecoratorNode
    {
        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            return TickChild(context, tree) == SWBehaviourStatus.Running
                ? SWBehaviourStatus.Running
                : SWBehaviourStatus.Failure;
        }
    }
}
