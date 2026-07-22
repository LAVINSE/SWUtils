using System;
using System.Collections.Generic;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Behaviour 노드, 연결과 Blackboard를 저장하고 실행하는 에셋입니다.</summary>
    [CreateAssetMenu(fileName = "SWBehaviourTree", menuName = "SWTools/Behaviour Tree")]
    public sealed class SWBehaviourTreeAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeReference] private List<SWBehaviourNode> nodes = new();
        [SerializeField] private string rootNodeIdentifier;
        [SerializeField] private SWBehaviourBlackboard blackboard = new();
        [NonSerialized] private Dictionary<string, SWBehaviourNode> nodesByIdentifier;
        [NonSerialized] private SWBehaviourContext context;

        /// <summary>그래프에 저장된 모든 노드입니다.</summary>
        public IReadOnlyList<SWBehaviourNode> Nodes => nodes;

        /// <summary>Behaviour Tree의 공유 Blackboard입니다.</summary>
        public SWBehaviourBlackboard Blackboard => blackboard;

        /// <summary>Root 노드 식별자입니다.</summary>
        public string RootNodeIdentifier => rootNodeIdentifier;

        /// <summary>지정한 타입의 Behaviour 노드를 추가합니다.</summary>
        public SWBehaviourNode AddNode(Type nodeType, Vector2 position)
        {
            if (nodeType == null || nodeType.IsAbstract ||
                !typeof(SWBehaviourNode).IsAssignableFrom(nodeType))
                return null;

            SWBehaviourNode node = Activator.CreateInstance(nodeType) as SWBehaviourNode;
            if (node == null)
                return null;

            node.Position = new Rect(position, new Vector2(220f, 110f));
            node.DisplayName = nodeType.Name
                .Replace("SWBehaviour", string.Empty)
                .Replace("Node", string.Empty);
            nodes.Add(node);
            RebuildLookup();
            if (string.IsNullOrWhiteSpace(rootNodeIdentifier))
                rootNodeIdentifier = node.Identifier;
            return node;
        }

        /// <summary>직렬화된 노드 복사본을 새 식별자로 그래프에 추가합니다.</summary>
        public SWBehaviourNode AddNodeCopy(Type nodeType, string serializedNode, Vector2 position)
        {
            if (nodeType == null || string.IsNullOrWhiteSpace(serializedNode) ||
                !typeof(SWBehaviourNode).IsAssignableFrom(nodeType))
                return null;
            SWBehaviourNode node = JsonUtility.FromJson(serializedNode, nodeType) as SWBehaviourNode;
            if (node == null)
                return null;
            Vector2 positionOffset = position - node.Position.position;
            node.PrepareDuplicate(positionOffset);
            nodes.Add(node);
            RebuildLookup();
            return node;
        }

        /// <summary>노드와 해당 노드로 들어오고 나가는 연결을 제거합니다.</summary>
        public bool RemoveNode(string identifier)
        {
            int index = nodes.FindIndex(node => node.Identifier == identifier);
            if (index < 0)
                return false;

            nodes.RemoveAt(index);
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
                nodes[nodeIndex].RemoveChild(identifier);
            if (rootNodeIdentifier == identifier)
                rootNodeIdentifier = nodes.Count > 0 ? nodes[0].Identifier : string.Empty;
            RebuildLookup();
            return true;
        }

        /// <summary>부모 노드와 자식 노드를 연결합니다.</summary>
        public bool Connect(string parentIdentifier, string childIdentifier)
        {
            if (parentIdentifier == childIdentifier ||
                !TryGetNode(parentIdentifier, out SWBehaviourNode parent) ||
                !TryGetNode(childIdentifier, out _ ) ||
                childIdentifier == rootNodeIdentifier ||
                HasParent(childIdentifier) || WouldCreateCycle(parentIdentifier, childIdentifier))
                return false;

            bool connected = parent.AddChild(childIdentifier);
            if (connected)
                SortChildrenByPosition();
            return connected;
        }

        /// <summary>부모와 자식 사이의 연결을 제거합니다.</summary>
        public bool Disconnect(string parentIdentifier, string childIdentifier)
        {
            return TryGetNode(parentIdentifier, out SWBehaviourNode parent) &&
                parent.RemoveChild(childIdentifier);
        }

        /// <summary>지정한 노드를 Root로 설정합니다.</summary>
        public void SetRoot(string identifier)
        {
            if (TryGetNode(identifier, out _))
            {
                for (int index = 0; index < nodes.Count; index++)
                    nodes[index].RemoveChild(identifier);
                rootNodeIdentifier = identifier;
            }
        }

        /// <summary>Composite 자식 실행 순서를 그래프의 가로 위치에 맞춰 정렬합니다.</summary>
        public void SortChildrenByPosition()
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                nodes[index].SortChildren((leftIdentifier, rightIdentifier) =>
                {
                    float leftPosition = TryGetNode(leftIdentifier, out SWBehaviourNode left)
                        ? left.Position.x
                        : 0f;
                    float rightPosition = TryGetNode(rightIdentifier, out SWBehaviourNode right)
                        ? right.Position.x
                        : 0f;
                    return leftPosition.CompareTo(rightPosition);
                });
            }
        }

        /// <summary>식별자로 노드를 찾습니다.</summary>
        public bool TryGetNode(string identifier, out SWBehaviourNode node)
        {
            EnsureLookup();
            return nodesByIdentifier.TryGetValue(identifier, out node);
        }

        /// <summary>런타임 실행에 사용할 독립 복제본을 생성합니다.</summary>
        public SWBehaviourTreeAsset CreateRuntimeInstance(GameObject owner)
        {
            return CreateRuntimeInstance(owner, null);
        }

        /// <summary>선택적으로 부모 Blackboard를 공유하는 독립 실행 복제본을 생성합니다.</summary>
        public SWBehaviourTreeAsset CreateRuntimeInstance(
            GameObject owner,
            SWBehaviourBlackboard sharedBlackboard)
        {
            SWBehaviourTreeAsset instance = Instantiate(this);
            instance.hideFlags = HideFlags.DontSave;
            instance.InitializeRuntime(owner, sharedBlackboard);
            return instance;
        }

        /// <summary>Root에서 Behaviour Tree를 한 번 실행합니다.</summary>
        public SWBehaviourStatus Tick(float deltaTime)
        {
            if (context == null)
                InitializeRuntime(null, null);
            context.DeltaTime = deltaTime;
            return TryGetNode(rootNodeIdentifier, out SWBehaviourNode root)
                ? root.Tick(context, this)
                : SWBehaviourStatus.Failure;
        }

        /// <summary>진행 중인 모든 노드를 중단합니다.</summary>
        public void Abort()
        {
            if (context != null && TryGetNode(rootNodeIdentifier, out SWBehaviourNode root))
                root.Abort(context, this);
        }

        /// <summary>빠른 노드 조회 테이블을 다시 만듭니다.</summary>
        public void RebuildLookup()
        {
            nodesByIdentifier = new Dictionary<string, SWBehaviourNode>(StringComparer.Ordinal);
            for (int index = 0; index < nodes.Count; index++)
            {
                SWBehaviourNode node = nodes[index];
                if (node == null)
                    continue;
                node.EnsureIdentifier();
                nodesByIdentifier[node.Identifier] = node;
            }
            blackboard?.RebuildLookup();
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            nodesByIdentifier = null;
            context = null;
        }

        private void InitializeRuntime(GameObject owner, SWBehaviourBlackboard sharedBlackboard)
        {
            RebuildLookup();
            context = new SWBehaviourContext
            {
                Owner = owner,
                Blackboard = sharedBlackboard ?? blackboard,
            };
        }

        private void EnsureLookup()
        {
            if (nodesByIdentifier == null)
                RebuildLookup();
        }

        private bool HasParent(string childIdentifier)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                IReadOnlyList<string> children = nodes[index].ChildIdentifiers;
                for (int childIndex = 0; childIndex < children.Count; childIndex++)
                {
                    if (children[childIndex] == childIdentifier)
                        return true;
                }
            }
            return false;
        }

        private bool WouldCreateCycle(string parentIdentifier, string childIdentifier)
        {
            return IsDescendant(childIdentifier, parentIdentifier, new HashSet<string>());
        }

        private bool IsDescendant(string currentIdentifier, string targetIdentifier, HashSet<string> visited)
        {
            if (!visited.Add(currentIdentifier) || !TryGetNode(currentIdentifier, out SWBehaviourNode current))
                return false;
            if (currentIdentifier == targetIdentifier)
                return true;
            for (int index = 0; index < current.ChildIdentifiers.Count; index++)
            {
                if (IsDescendant(current.ChildIdentifiers[index], targetIdentifier, visited))
                    return true;
            }
            return false;
        }
    }
}
