using System;
using System.Collections.Generic;
using UnityEngine;
using SW.Attributes;
using SW.Base;

namespace SW.SkillTree
{
    /// <summary>선행 노드의 요구 레벨을 판정하는 방식입니다.</summary>
    public enum SWSkillTreeRequirementMode { All, Any }

    /// <summary>선행 노드 진행에 따라 정보를 공개하는 정책입니다.</summary>
    public enum SWSkillTreeRevealPolicy
    {
        [InspectorName("항상 공개")] Always,
        [InspectorName("선행 노드 습득 시 공개")] PrerequisiteLearned,
        [InspectorName("선행 요구 레벨 충족 시 공개")] PrerequisiteLevelReached
    }

    /// <summary>아직 공개되지 않은 노드의 표시 방식입니다.</summary>
    public enum SWSkillTreeConcealment
    {
        [InspectorName("완전히 숨기기")] Hidden,
        [InspectorName("정보 가리기")] Masked
    }

    /// <summary>선행 노드와 필요한 습득 레벨입니다.</summary>
    [Serializable]
    public sealed class SWSkillTreeRequirement
    {
        [SerializeField] private string nodeIdentifier;
        [SerializeField, Min(1)] private int requiredLevel = 1;
        /// <summary>선행 노드의 영구 식별자입니다.</summary>
        public string NodeIdentifier => nodeIdentifier;
        /// <summary>선행 노드에 필요한 레벨입니다.</summary>
        public int RequiredLevel => requiredLevel;
        /// <summary>연결할 선행 노드와 요구 레벨을 지정합니다.</summary>
        public SWSkillTreeRequirement(string nodeIdentifier, int requiredLevel = 1)
        {
            this.nodeIdentifier = nodeIdentifier;
            this.requiredLevel = requiredLevel;
        }
    }

    /// <summary>트리에 배치한 스킬과 해당 위치의 해금 규칙입니다.</summary>
    [Serializable]
    public sealed class SWSkillTreeNode
    {
        [SerializeField] private string identifier = Guid.NewGuid().ToString("N");
        [SerializeField] private SWSkillDefinition skill;
        [SerializeField] private Vector2 position;
        [SerializeField] private bool hasSavedPosition = true;
        [SerializeField] private SWSkillTreeRequirementMode requirementMode;
        [SerializeField] private List<SWSkillTreeRequirement> requirements = new();
        [SerializeField] private string exclusiveGroup;
        [SerializeField] private bool retainOnReset;
        [SerializeField] private SWSkillTreeRevealPolicy revealPolicy;
        [SerializeField] private SWSkillTreeConcealment concealment;
        [SerializeReference, SWSubClassSelector] private SWSkillTreeCondition[] visibilityConditions = Array.Empty<SWSkillTreeCondition>();
        [SerializeReference, SWSubClassSelector] private SWSkillTreeCondition[] purchaseConditions = Array.Empty<SWSkillTreeCondition>();

        /// <summary>배치와 목록 순서가 바뀌어도 유지하는 저장 식별자입니다.</summary>
        public string Identifier => identifier;
        /// <summary>노드에 연결한 스킬 정의입니다.</summary>
        public SWSkillDefinition Skill => skill;
        /// <summary>오른쪽과 아래 방향으로 증가하는 트리 배치 좌표입니다.</summary>
        public Vector2 Position => position;
        /// <summary>자동 배치보다 우선해서 사용할 저장 좌표가 있는지 반환합니다.</summary>
        public bool HasSavedPosition => hasSavedPosition;
        /// <summary>선행 진행에 따른 정보 공개 정책입니다.</summary>
        public SWSkillTreeRevealPolicy RevealPolicy => revealPolicy;
        /// <summary>공개 전 노드를 숨기거나 정보를 가리는 방식입니다.</summary>
        public SWSkillTreeConcealment Concealment => concealment;
        /// <summary>선행 조건을 모두 또는 하나 이상 만족해야 하는지 지정합니다.</summary>
        public SWSkillTreeRequirementMode RequirementMode => requirementMode;
        /// <summary>선행 노드 조건 목록입니다.</summary>
        public IReadOnlyList<SWSkillTreeRequirement> Requirements => requirements != null ? requirements : Array.Empty<SWSkillTreeRequirement>();
        /// <summary>같은 그룹의 다른 노드와 동시에 습득할 수 없습니다. 빈 값이면 제한하지 않습니다.</summary>
        public string ExclusiveGroup => exclusiveGroup;
        /// <summary>영구 노드를 유지하는 초기화에서 이 노드를 보존합니다.</summary>
        public bool RetainOnReset => retainOnReset;
        /// <summary>미습득 노드를 화면에 표시하는 조건입니다.</summary>
        public IReadOnlyList<SWSkillTreeCondition> VisibilityConditions => visibilityConditions ?? Array.Empty<SWSkillTreeCondition>();
        /// <summary>구매 시점에 검사하는 프로젝트별 추가 조건입니다.</summary>
        public IReadOnlyList<SWSkillTreeCondition> PurchaseConditions => purchaseConditions ?? Array.Empty<SWSkillTreeCondition>();

        /// <summary>새 배치 식별자를 가진 노드를 생성합니다.</summary>
        public SWSkillTreeNode(SWSkillDefinition skill, Vector2 position)
        {
            this.skill = skill;
            this.position = position;
        }
    }

    /// <summary>플레이어 진행 상태를 포함하지 않는 스킬트리 정의 에셋입니다.</summary>
    [CreateAssetMenu(fileName = "SWSkillTree_", menuName = "SWUtils/Skill Tree/Tree")]
    public sealed class SWSkillTreeDefinition : SWScriptableObject
    {
        [SerializeField] private string identifier = Guid.NewGuid().ToString("N");
        [SerializeField] private List<SWSkillTreeNode> nodes = new();
        /// <summary>저장 데이터가 속한 트리를 식별합니다.</summary>
        public string Identifier => identifier;
        /// <summary>트리에 배치한 노드입니다.</summary>
        public IReadOnlyList<SWSkillTreeNode> Nodes => nodes != null ? nodes : Array.Empty<SWSkillTreeNode>();
    }
}
