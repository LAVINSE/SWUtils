using System;
using System.Collections.Generic;
using SW.Util;

namespace SW.SkillTree
{
    /// <summary>여러 재화의 일괄 거래와 외부 잔액 변경을 지원하는 메모리 지갑입니다.</summary>
    public sealed class SWSkillTreeWallet : ISWSkillTreeWallet
    {
        private readonly Dictionary<string, double> balances = new(StringComparer.Ordinal);
        /// <inheritdoc />
        public event Action Changed;
        /// <inheritdoc />
        public double GetBalance(string currency) => currency != null && balances.TryGetValue(currency, out double value) ? value : 0;

        /// <summary>예제 또는 프로젝트에서 외부 잔액을 동기화합니다.</summary>
        public void SetBalance(string currency, double value)
        {
            if (!new SWSkillTreeAmount(currency, value).IsValid) throw new ArgumentOutOfRangeException(nameof(value));
            balances[currency] = value;
            NotifyChanged();
        }

        /// <inheritdoc />
        public bool TryExchange(IReadOnlyList<SWSkillTreeAmount> amounts, bool credit)
        {
            if (amounts == null) return false;
            Dictionary<string, double> totals = new(StringComparer.Ordinal);
            foreach (SWSkillTreeAmount amount in amounts)
            {
                if (!amount.IsValid) return false;
                totals.TryGetValue(amount.currency, out double total);
                if (!new SWSkillTreeAmount(amount.currency, total + amount.value).IsValid
                    || (amount.value > 0 && total + amount.value <= total)) return false;
                totals[amount.currency] = total + amount.value;
            }
            Dictionary<string, double> updated = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> total in totals)
            {
                double balance = GetBalance(total.Key);
                double value = credit ? balance + total.Value : balance - total.Value;
                if (!new SWSkillTreeAmount(total.Key, value).IsValid
                    || (!credit && !SWSkillTreeAmount.CanSubtract(balance, total.Value))
                    || (credit && total.Value > 0 && value <= balance)) return false;
                updated.Add(total.Key, value);
            }
            foreach (KeyValuePair<string, double> entry in updated) balances[entry.Key] = entry.Value;
            NotifyChanged();
            return true;
        }

        private void NotifyChanged()
        {
            if (Changed == null) return;
            foreach (Action listener in Changed.GetInvocationList())
            {
                try { listener(); }
                catch (Exception exception) { SWLog.LogError($"[SWSkillTreeWallet] 변경 알림 실패: {exception}"); }
            }
        }
    }
}
