using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>메시지를 Unity Console에 출력하고 성공하는 Action 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourLogNode : SWBehaviourActionNode
    {
        [SerializeField] private string message = "Behaviour Tree";

        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            Debug.Log(message, context.Owner);
            return SWBehaviourStatus.Success;
        }
    }
}
