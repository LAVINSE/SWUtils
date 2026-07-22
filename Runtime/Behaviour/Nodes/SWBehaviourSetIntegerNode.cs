using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>고정값 또는 Blackboard 값을 다른 Integer 속성에 기록합니다.</summary>
    [Serializable]
    public sealed class SWBehaviourSetIntegerNode : SWBehaviourActionNode
    {
        [SerializeField] private SWBehaviourNodeProperty<int> target = new();
        [SerializeField] private SWBehaviourNodeProperty<int> source = new();

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            return target.SetValue(context, source.GetValue(context))
                ? SWBehaviourStatus.Success
                : SWBehaviourStatus.Failure;
        }
    }
}
