using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>지정 확률로 실패하고 나머지 경우 성공하는 Action 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourRandomFailureNode : SWBehaviourActionNode
    {
        [SerializeField, Range(0f, 1f)] private float failureProbability = 0.5f;

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            return UnityEngine.Random.value < failureProbability
                ? SWBehaviourStatus.Failure
                : SWBehaviourStatus.Success;
        }
    }
}
