using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Unity Editor를 일시 정지하고 성공을 반환하는 디버그 Action 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourBreakpointNode : SWBehaviourActionNode
    {
        [SerializeField] private string message = "Behaviour Tree Breakpoint";

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            Debug.Log(message, context.Owner);
#if UNITY_EDITOR
            Debug.Break();
#endif
            return SWBehaviourStatus.Success;
        }
    }
}
