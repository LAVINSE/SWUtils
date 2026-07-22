using System;

namespace SW.BehaviourTree
{
    /// <summary>자식의 성공과 실패 결과를 반대로 바꾸는 Decorator 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourInverterNode : SWBehaviourDecoratorNode
    {
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            return TickChild(context, tree) switch
            {
                SWBehaviourStatus.Success => SWBehaviourStatus.Failure,
                SWBehaviourStatus.Failure => SWBehaviourStatus.Success,
                SWBehaviourStatus.Running => SWBehaviourStatus.Running,
                _ => SWBehaviourStatus.Failure,
            };
        }
    }
}
