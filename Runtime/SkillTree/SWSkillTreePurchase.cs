using System;
using System.Collections.Generic;

namespace SW.SkillTree
{
    /// <summary>구매 미리보기 또는 실행 결과입니다. 견적은 실행 시 다시 계산합니다.</summary>
    public sealed class SWSkillTreePurchase
    {
        /// <summary>구매할 수 있거나 요청한 거래가 성공했는지 나타냅니다.</summary>
        public bool Success { get; }
        /// <summary>실패 사유 또는 일괄 구매 제한 안내입니다.</summary>
        public string Reason { get; }
        /// <summary>이번 요청에서 구매하는 레벨 수입니다.</summary>
        public int Levels { get; }
        /// <summary>이번 요청의 전체 재화 비용입니다.</summary>
        public IReadOnlyList<SWSkillTreeAmount> Costs { get; }
        internal SWSkillTreePurchase(bool success, string reason, int levels = 0, SWSkillTreeAmount[] costs = null)
        {
            Success = success;
            Reason = reason;
            Levels = levels;
            Costs = Array.AsReadOnly(costs ?? Array.Empty<SWSkillTreeAmount>());
        }
    }
}
