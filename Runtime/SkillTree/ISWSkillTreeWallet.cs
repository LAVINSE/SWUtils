using System;
using System.Collections.Generic;

namespace SW.SkillTree
{
    /// <summary>프로젝트 재화를 연결합니다. 변경은 전체 성공 또는 전체 실패여야 하며 실패 시 잔액을 보존해야 합니다.</summary>
    public interface ISWSkillTreeWallet
    {
        /// <summary>외부 지급이나 차감으로 잔액이 바뀐 뒤 발생합니다.</summary>
        event Action Changed;
        /// <summary>현재 재화 잔액을 반환합니다.</summary>
        double GetBalance(string currency);
        /// <summary>여러 재화를 한 번에 차감하거나 환급합니다. 콜백 예외가 거래 결과를 바꾸면 안 됩니다.</summary>
        bool TryExchange(IReadOnlyList<SWSkillTreeAmount> amounts, bool credit);
    }
}
