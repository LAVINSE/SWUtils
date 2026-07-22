using System;
using System.Collections.Generic;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Behaviour Tree를 구성하는 모든 노드의 실행 생명주기를 정의합니다.</summary>
    [Serializable]
    public abstract class SWBehaviourNode
    {
        [SerializeField] private string identifier = Guid.NewGuid().ToString("N");
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Rect position = new(0f, 0f, 220f, 110f);
        [SerializeField] private List<string> childIdentifiers = new();
        [NonSerialized] private bool isStarted;
        [NonSerialized] private SWBehaviourStatus status;

        /// <summary>노드의 고유 식별자입니다.</summary>
        public string Identifier => identifier;

        /// <summary>그래프에 표시할 이름입니다.</summary>
        public string DisplayName { get => displayName; set => displayName = value; }

        /// <summary>노드 동작을 설명하는 문장입니다.</summary>
        public string Description { get => description; set => description = value; }

        /// <summary>그래프에 저장된 노드 위치입니다.</summary>
        public Rect Position { get => position; set => position = value; }

        /// <summary>자식 노드 식별자를 실행 순서대로 반환합니다.</summary>
        public IReadOnlyList<string> ChildIdentifiers => childIdentifiers;

        /// <summary>노드의 마지막 실행 결과입니다.</summary>
        public SWBehaviourStatus Status => status;

        /// <summary>노드가 허용하는 최대 자식 수입니다.</summary>
        public abstract int MaximumChildCount { get; }

        /// <summary>노드를 한 번 실행하고 결과를 반환합니다.</summary>
        public SWBehaviourStatus Tick(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (!isStarted)
            {
                isStarted = true;
                OnStart(context);
            }

            status = OnUpdate(context, tree);
            if (status != SWBehaviourStatus.Running)
            {
                OnStop(context, tree, status);
                isStarted = false;
            }

            return status;
        }

        /// <summary>진행 중인 노드와 모든 자식을 즉시 중단합니다.</summary>
        public void Abort(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            for (int index = 0; index < childIdentifiers.Count; index++)
            {
                if (tree.TryGetNode(childIdentifiers[index], out SWBehaviourNode child))
                    child.Abort(context, tree);
            }

            if (isStarted)
                OnAbort(context);
            isStarted = false;
            status = SWBehaviourStatus.Aborted;
        }

        /// <summary>노드 실행이 시작될 때 한 번 호출됩니다.</summary>
        protected virtual void OnStart(SWBehaviourContext context) { }

        /// <summary>노드의 현재 실행 결과를 계산합니다.</summary>
        protected abstract SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree);

        /// <summary>노드가 성공 또는 실패로 끝났을 때 호출됩니다.</summary>
        protected virtual void OnStop(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree,
            SWBehaviourStatus status) { }

        /// <summary>진행 중인 노드가 외부에서 중단될 때 호출됩니다.</summary>
        protected virtual void OnAbort(SWBehaviourContext context) { }

        internal bool AddChild(string childIdentifier)
        {
            if (string.IsNullOrWhiteSpace(childIdentifier) ||
                childIdentifiers.Contains(childIdentifier) ||
                (MaximumChildCount >= 0 && childIdentifiers.Count >= MaximumChildCount))
                return false;

            childIdentifiers.Add(childIdentifier);
            return true;
        }

        internal bool RemoveChild(string childIdentifier) => childIdentifiers.Remove(childIdentifier);

        internal void SortChildren(Comparison<string> comparison) => childIdentifiers.Sort(comparison);

        internal void EnsureIdentifier()
        {
            if (string.IsNullOrWhiteSpace(identifier))
                identifier = Guid.NewGuid().ToString("N");
        }

        internal void PrepareDuplicate(Vector2 positionOffset)
        {
            identifier = Guid.NewGuid().ToString("N");
            childIdentifiers.Clear();
            position.position += positionOffset;
            isStarted = false;
            status = SWBehaviourStatus.Inactive;
        }
    }

    /// <summary>자식이 없는 실제 행동 노드입니다.</summary>
    [Serializable]
    public abstract class SWBehaviourActionNode : SWBehaviourNode
    {
        public sealed override int MaximumChildCount => 0;
    }

    /// <summary>여러 자식의 실행 순서를 제어하는 노드입니다.</summary>
    [Serializable]
    public abstract class SWBehaviourCompositeNode : SWBehaviourNode
    {
        public sealed override int MaximumChildCount => -1;
    }

    /// <summary>하나의 자식 실행 결과를 변경하거나 반복하는 노드입니다.</summary>
    [Serializable]
    public abstract class SWBehaviourDecoratorNode : SWBehaviourNode
    {
        public sealed override int MaximumChildCount => 1;

        /// <summary>자식 노드를 실행하며 연결되지 않았으면 실패를 반환합니다.</summary>
        protected SWBehaviourStatus TickChild(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            return ChildIdentifiers.Count == 1 &&
                tree.TryGetNode(ChildIdentifiers[0], out SWBehaviourNode child)
                ? child.Tick(context, tree)
                : SWBehaviourStatus.Failure;
        }
    }
}
