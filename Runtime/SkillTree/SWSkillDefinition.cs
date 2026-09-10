using System;
using System.Collections.Generic;
using UnityEngine;
using SW.Attributes;
using SW.Base;

namespace SW.SkillTree
{
    /// <summary>여러 트리에서 재사용하는 스킬의 표시 정보, 성장 비용과 지속 효과 정의입니다.</summary>
    [CreateAssetMenu(fileName = "SWSkill_", menuName = "SWUtils/Skill Tree/Skill")]
    public class SWSkillDefinition : SWIdentifiedObject
    {
        [SWGroup("표시")]
        [SerializeField] private Sprite icon;
        [SWGroup("성장")]
        [SerializeField, Min(1)] private int maximumLevel = 1;
        [SerializeReference, SWSubClassSelector] private SWSkillTreeCost[] costs = Array.Empty<SWSkillTreeCost>();
        [SWGroup("효과")]
        [SerializeField] private SWSkillTreeEffect[] effects = Array.Empty<SWSkillTreeEffect>();

        /// <summary>게임 화면에 표시하는 아이콘입니다.</summary>
        public Sprite Icon => icon;
        /// <summary>구매할 수 있는 최대 레벨입니다.</summary>
        public int MaximumLevel => maximumLevel;
        /// <summary>다음 레벨의 비용 계산 목록입니다. 같은 재화는 합산합니다.</summary>
        public IReadOnlyList<SWSkillTreeCost> Costs => costs ?? Array.Empty<SWSkillTreeCost>();
        /// <summary>습득 레벨에 따라 다시 구성하는 지속 효과입니다.</summary>
        public IReadOnlyList<SWSkillTreeEffect> Effects => effects ?? Array.Empty<SWSkillTreeEffect>();
    }
}
