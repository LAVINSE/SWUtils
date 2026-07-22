using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>다른 Behaviour Tree를 현재 노드의 하위 트리로 실행합니다.</summary>
    [Serializable]
    public sealed class SWBehaviourSubTreeNode : SWBehaviourActionNode
    {
        [SerializeField] private SWBehaviourTreeAsset subTreeAsset;
        [SerializeField] private bool shareBlackboard = true;
        [NonSerialized] private SWBehaviourTreeAsset runtimeSubTree;

        protected override void OnStart(SWBehaviourContext context)
        {
            if (subTreeAsset != null)
            {
                runtimeSubTree = subTreeAsset.CreateRuntimeInstance(
                    context.Owner,
                    shareBlackboard ? context.Blackboard : null);
            }
        }

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            return runtimeSubTree == null
                ? SWBehaviourStatus.Failure
                : runtimeSubTree.Tick(context.DeltaTime);
        }

        protected override void OnStop(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree,
            SWBehaviourStatus status)
        {
            DestroyRuntimeSubTree();
        }

        protected override void OnAbort(SWBehaviourContext context)
        {
            DestroyRuntimeSubTree();
        }

        private void DestroyRuntimeSubTree()
        {
            if (runtimeSubTree == null)
                return;
            runtimeSubTree.Abort();
            UnityEngine.Object.Destroy(runtimeSubTree);
            runtimeSubTree = null;
        }
    }
}
