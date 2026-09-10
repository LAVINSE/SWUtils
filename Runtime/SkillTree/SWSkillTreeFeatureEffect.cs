using System;
using UnityEngine;

namespace SW.SkillTree
{
    /// <summary>게임이 조회할 수 있는 기능 이름과 해금 레벨을 등록합니다.</summary>
    [CreateAssetMenu(fileName = "SWSkillFeatureEffect_", menuName = "SWUtils/Skill Tree/Feature Effect")]
    public sealed class SWSkillTreeFeatureEffect : SWSkillTreeEffect
    {
        [SerializeField] private string feature = "AutomaticMining";
        [SerializeField] private string displayName = "Auto Mining";
        /// <inheritdoc />
        public override void Apply(object context, object source, int level)
        {
            if (context is not SWSkillTreeGameContext gameContext) throw new InvalidOperationException("SWSkillTreeGameContext를 연결하세요.");
            gameContext.SetFeatureLevel(feature, source, level);
        }
        /// <inheritdoc />
        public override string Describe(int level) => level == 0 ? $"{displayName}: Locked" : $"{displayName}: Level {level}";
    }
}
