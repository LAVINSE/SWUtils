using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>지정한 시간 동안 Running을 반환한 뒤 성공하는 Action 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourWaitNode : SWBehaviourActionNode
    {
        [SerializeField, Min(0f)] private float duration = 1f;
        [NonSerialized] private float elapsedTime;

        protected override void OnStart(SWBehaviourContext context) => elapsedTime = 0f;

        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            elapsedTime += context.DeltaTime;
            return elapsedTime >= duration ? SWBehaviourStatus.Success : SWBehaviourStatus.Running;
        }
    }
}
