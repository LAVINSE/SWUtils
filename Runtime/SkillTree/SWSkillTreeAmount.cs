using System;
using System.Globalization;

namespace SW.SkillTree
{
    /// <summary>재화 이름과 유한한 비음수 수량입니다. 기본 수치 범위는 double입니다.</summary>
    [Serializable]
    public struct SWSkillTreeAmount
    {
        /// <summary>프로젝트 재화와 연결하는 이름입니다.</summary>
        public string currency;
        /// <summary>재화 수량입니다.</summary>
        public double value;
        /// <summary>재화와 수량을 지정합니다.</summary>
        public SWSkillTreeAmount(string currency, double value) { this.currency = currency; this.value = value; }
        /// <summary>구매와 환불에 사용할 수 있는 수량인지 확인합니다.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(currency) && value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
        /// <summary>정밀도 손실로 실제 수량이 감소하지 않는 차감을 차단합니다.</summary>
        public static bool CanSubtract(double balance, double amount)
            => amount >= 0 && balance >= amount && (amount == 0 || balance - amount < balance);
        /// <summary>기본 화면에서 사용하는 재화 표시입니다.</summary>
        public override string ToString() => $"{currency} {value.ToString(value >= 1e12 ? "0.###E+0" : "0.##", CultureInfo.InvariantCulture)}";
    }
}
