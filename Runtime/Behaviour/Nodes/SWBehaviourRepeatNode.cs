using System;

namespace SW.BehaviourTree
{
    /// <summary>자식을 지정 횟수만큼 반복하는 Decorator 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourRepeatNode : SWBehaviourDecoratorNode
    {
        [UnityEngine.SerializeField, UnityEngine.Min(-1)] private int repeatCount = -1;
        [NonSerialized] private int completedCount;

        protected override void OnStart(SWBehaviourContext context) => completedCount = 0;

        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            SWBehaviourStatus childStatus = TickChild(context, tree);
            if (childStatus == SWBehaviourStatus.Running)
                return SWBehaviourStatus.Running;
            if (childStatus == SWBehaviourStatus.Failure)
                return SWBehaviourStatus.Failure;

            completedCount++;
            return repeatCount >= 0 && completedCount >= repeatCount
                ? SWBehaviourStatus.Success
                : SWBehaviourStatus.Running;
        }
    }
}
