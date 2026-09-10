using System;
using UnityEngine;

namespace SW.SkillTree
{
    /// <summary>지정 레벨을 구매할 때 필요한 단일 재화 비용을 계산합니다.</summary>
    [Serializable]
    public abstract class SWSkillTreeCost
    {
        /// <summary>현재 레벨에서 다음 한 레벨로 성장하는 비용을 반환합니다.</summary>
        public abstract SWSkillTreeAmount Evaluate(int currentLevel);
    }

    /// <summary>비용의 기본 증가 방식입니다.</summary>
    public enum SWSkillTreeCostGrowth { Fixed, Linear, Exponential, Table }

    /// <summary>고정, 선형, 지수 또는 레벨별 표로 비용을 계산하는 기본 구현입니다.</summary>
    [Serializable]
    public sealed class SWSkillTreeFormulaCost : SWSkillTreeCost
    {
        [SerializeField] private string currency = "Gold";
        [SerializeField] private SWSkillTreeCostGrowth growth = SWSkillTreeCostGrowth.Exponential;
        [SerializeField] private double initialCost = 10;
        [SerializeField] private double growthValue = 1.15;
        [SerializeField] private double[] levelCosts = Array.Empty<double>();

        /// <inheritdoc />
        public override SWSkillTreeAmount Evaluate(int currentLevel)
        {
            double value = growth switch
            {
                SWSkillTreeCostGrowth.Fixed => initialCost,
                SWSkillTreeCostGrowth.Linear => initialCost + growthValue * currentLevel,
                SWSkillTreeCostGrowth.Exponential => initialCost * Math.Pow(growthValue, currentLevel),
                SWSkillTreeCostGrowth.Table => levelCosts != null && currentLevel >= 0 && currentLevel < levelCosts.Length
                    ? levelCosts[currentLevel] : double.NaN,
                _ => double.NaN
            };
            return new SWSkillTreeAmount(currency, value);
        }
    }
}
