using System;
using UnityEngine;
using SW.Stat;

namespace SW.SkillTree
{
    /// <summary>SWUtils 능력치에 출처별 가산 보너스를 연결합니다.</summary>
    [CreateAssetMenu(fileName = "SWSkillStatEffect_", menuName = "SWUtils/Skill Tree/Stat Effect")]
    public sealed class SWSkillTreeStatEffect : SWSkillTreeEffect
    {
        [SerializeField] private SWStat stat;
        [SerializeField] private float amountPerLevel = 1;
        /// <inheritdoc />
        public override void Apply(object context, object source, int level)
        {
            SWStats stats = context as SWStats ?? (context as SWSkillTreeGameContext)?.Stats;
            if (level == 0 && (stat == null || stats == null || !stats.IsSetup)) return;
            if (stat == null || stats == null || !stats.IsSetup)
                throw new InvalidOperationException("초기화된 SWStats와 대상 능력치를 연결하세요.");
            SWStat runtimeStat = stats.GetStat(stat);
            if (runtimeStat == null && level == 0) return;
            if (runtimeStat == null) throw new InvalidOperationException("대상 런타임 능력치를 연결하세요.");
            double amount = (double)amountPerLevel * level;
            if (double.IsNaN(amount) || Math.Abs(amount) > float.MaxValue) throw new OverflowException("능력치 효과 범위를 초과했습니다.");
            if (level == 0) runtimeStat.RemoveBonusValue(source);
            else runtimeStat.SetBonusValue(source, (float)amount);
        }
        /// <inheritdoc />
        public override string Describe(int level) => $"{(stat != null ? stat.DisplayName : "No stat assigned")} +{(double)amountPerLevel * level:0.##}";
    }
}
