using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>제한 시간 안에 끝나지 않은 자식을 중단하고 실패를 반환합니다.</summary>
    [Serializable]
    public sealed class SWBehaviourTimeoutNode : SWBehaviourDecoratorNode
    {
        [SerializeField, Min(0f)] private float duration = 1f;
        [NonSerialized] private float elapsedTime;

        protected override void OnStart(SWBehaviourContext context) => elapsedTime = 0f;

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            elapsedTime += context.DeltaTime;
            if (elapsedTime < duration)
                return TickChild(context, tree);
            if (ChildIdentifiers.Count == 1 &&
                tree.TryGetNode(ChildIdentifiers[0], out SWBehaviourNode child))
                child.Abort(context, tree);
            return SWBehaviourStatus.Failure;
        }
    }
}
