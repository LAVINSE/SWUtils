using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Blackboard Key에 타입 호환 고정값을 기록하는 범용 Action 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourSetPropertyNode : SWBehaviourActionNode
    {
        [SerializeField, SWBehaviourBlackboardKeySelector] private string targetKeyName;
        [SerializeField] private SWBehaviourBlackboardValue value = new();

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            return context.Blackboard.TryGetEntry(targetKeyName, out SWBehaviourBlackboardEntry entry) &&
                entry.TrySetBoxedValue(value.GetBoxedValue())
                ? SWBehaviourStatus.Success
                : SWBehaviourStatus.Failure;
        }
    }
}
