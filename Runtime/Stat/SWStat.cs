using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using SW.Base;

using SW.Util;

namespace SW.Stat
{
    /// <summary>
    /// 기본값과 보너스 값을 조합하여 최종 능력치를 계산하는 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWStat_", menuName = "SWStat/Stat")]
    public class SWStat : SWIdentifiedObject
    {
        #region 이벤트
        /// <summary>
        /// 능력치 값이 변경될 때 호출되는 이벤트 처리자입니다.
        /// </summary>
        /// <param name="stat">값이 변경된 능력치입니다.</param>
        /// <param name="currentValue">변경된 현재 값입니다.</param>
        /// <param name="prevValue">변경 전 값입니다.</param>
        public delegate void ValueChangedHandler(SWStat stat, float currentValue, float prevValue);
        #endregion // 이벤트

        #region 필드
        [Tooltip("% 타입 여부 (1 = 100%, 0 = 0%)")]
        [SerializeField] private bool isPercentType;
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private float minValue;
        [SerializeField] private float defaultValue;

        /// <summary>
        /// 보너스를 제공한 대상과 세부 구분 키별 값을 저장합니다.
        /// </summary>
        private readonly Dictionary<object, Dictionary<object, float>> bonusValuesByKey = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>런타임 복제본이 참조하는 원본 에셋입니다.</summary>
        public SWStat OriginStat { get; private set; }

        /// <summary>저장된 모든 보너스 값의 합입니다.</summary>
        public float BonusValue { get; private set; }
        /// <summary>기본값과 보너스 값의 합을 최솟값과 최댓값 사이로 제한한 현재 값입니다.</summary>
        public float Value => Mathf.Clamp(defaultValue + BonusValue, MinValue, MaxValue);
        /// <summary>허용되는 최댓값입니다.</summary>
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }
        /// <summary>허용되는 최솟값입니다.</summary>
        public float MinValue
        {
            get => minValue;
            set => minValue = value;
        }
        /// <summary>보너스를 적용하기 전의 기본값입니다.</summary>
        public float DefaultValue
        {
            get => defaultValue;
            set
            {
                float prevValue = Value;
                defaultValue = Mathf.Clamp(value, MinValue, MaxValue);
                TryInvokeValueChangedEvent(Value, prevValue);
            }
        }

        /// <summary>값을 백분율로 표시할지 여부입니다.</summary>
        public bool IsPercentType => isPercentType;
        /// <summary>현재 값이 최댓값인지 여부입니다.</summary>
        public bool IsMax => Mathf.Approximately(Value, maxValue);
        /// <summary>현재 값이 최솟값인지 여부입니다.</summary>
        public bool IsMin => Mathf.Approximately(Value, minValue);

        /// <summary>현재 값이 변경될 때 발생합니다.</summary>
        public event ValueChangedHandler OnValueChanged;
        /// <summary>현재 값이 최댓값에 도달했을 때 발생합니다.</summary>
        public event ValueChangedHandler OnValueMax;
        /// <summary>현재 값이 최솟값에 도달했을 때 발생합니다.</summary>
        public event ValueChangedHandler OnValueMin;
        #endregion // 프로퍼티

        #region 복사
        /// <summary>
        /// 현재 능력치의 런타임 복제본을 생성합니다.
        /// </summary>
        /// <returns>생성된 런타임 복제본입니다.</returns>
        public override object Clone()
            => CreateRuntimeClone();

        /// <summary>
        /// 원본 에셋 정보를 유지하는 런타임 복제본을 생성합니다.
        /// </summary>
        /// <returns>생성된 런타임 복제본입니다.</returns>
        public SWStat CreateRuntimeClone()
        {
            SWStat clone = Instantiate(this);
            clone.name = name;
            clone.OriginStat = OriginStat != null ? OriginStat : this;
            return clone;
        }
        #endregion // 복사

        #region 비교
        /// <summary>
        /// 두 능력치가 같은 원본 에셋에서 생성되었는지 확인합니다.
        /// </summary>
        /// <param name="other">비교할 능력치입니다.</param>
        /// <returns>같은 능력치 정의이면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool IsSameStat(SWStat other)
        {
            if (other == null)
            {
                return false;
            }

            if (other == this)
            {
                return true;
            }

            if (ID != 0 && ID == other.ID)
            {
                return true;
            }

            SWStat selfOrigin = OriginStat != null ? OriginStat : this;
            SWStat otherOrigin = other.OriginStat != null ? other.OriginStat : other;
            return selfOrigin == otherOrigin;
        }
        #endregion // 비교

        #region 보너스값
        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값을 설정합니다.
        /// 같은 키 조합이 이미 있으면 기존 값을 교체합니다.
        /// </summary>
        /// <param name="key">보너스를 제공한 대상입니다.</param>
        /// <param name="subKey">같은 대상의 보너스를 구분하는 키입니다.</param>
        /// <param name="value">설정할 보너스 값입니다.</param>
        public void SetBonusValue(object key, object subKey, float value)
        {
            if (key == null || subKey == null)
            {
                SWLog.LogError($"[SWStat] SetBonusValue 실패 : key 또는 subKey가 null입니다. Stat: {DisplayName}");
                return;
            }

            float prevValue = Value;

            if (!bonusValuesByKey.TryGetValue(key, out Dictionary<object, float> bonusValuesBySubKey))
            {
                bonusValuesBySubKey = new();
                bonusValuesByKey[key] = bonusValuesBySubKey;
            }
            else if (bonusValuesBySubKey.TryGetValue(subKey, out float previousBonus))
            {
                BonusValue -= previousBonus;
            }

            bonusValuesBySubKey[subKey] = value;
            BonusValue += value;

            TryInvokeValueChangedEvent(Value, prevValue);
        }

        /// <summary>
        /// 별도의 세부 구분 키 없이 대상의 보너스 값을 설정합니다.
        /// </summary>
        /// <param name="key">보너스를 제공한 대상입니다.</param>
        /// <param name="value">설정할 보너스 값입니다.</param>
        public void SetBonusValue(object key, float value)
            => SetBonusValue(key, string.Empty, value);

        /// <summary>
        /// 대상이 제공한 모든 보너스 값의 합을 반환합니다.
        /// </summary>
        /// <param name="key">조회할 대상입니다.</param>
        /// <returns>대상이 제공한 보너스 값의 합입니다.</returns>
        public float GetBonusValue(object key)
            => bonusValuesByKey.TryGetValue(key, out Dictionary<object, float> bonusValuesBySubKey) ? bonusValuesBySubKey.Sum(x => x.Value) : 0f;

        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값을 반환합니다.
        /// </summary>
        /// <param name="key">조회할 대상입니다.</param>
        /// <param name="subKey">조회할 세부 구분 키입니다.</param>
        /// <returns>저장된 보너스 값이며, 값이 없으면 0입니다.</returns>
        public float GetBonusValue(object key, object subKey)
        {
            if (bonusValuesByKey.TryGetValue(key, out Dictionary<object, float> bonusValuesBySubKey) && bonusValuesBySubKey.TryGetValue(subKey, out float value))
            {
                return value;
            }

            return 0f;
        }

        /// <summary>
        /// 대상이 제공한 모든 보너스 값을 제거합니다.
        /// </summary>
        /// <param name="key">제거할 대상입니다.</param>
        /// <returns>값을 제거했으면 <see langword="true"/>이고, 값이 없으면 <see langword="false"/>입니다.</returns>
        public bool RemoveBonusValue(object key)
        {
            if (bonusValuesByKey.TryGetValue(key, out Dictionary<object, float> bonusValuesBySubKey))
            {
                float prevValue = Value;
                BonusValue -= bonusValuesBySubKey.Values.Sum();
                bonusValuesByKey.Remove(key);

                TryInvokeValueChangedEvent(Value, prevValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값을 제거합니다.
        /// </summary>
        /// <param name="key">제거할 대상입니다.</param>
        /// <param name="subKey">제거할 세부 구분 키입니다.</param>
        /// <returns>값을 제거했으면 <see langword="true"/>이고, 값이 없으면 <see langword="false"/>입니다.</returns>
        public bool RemoveBonusValue(object key, object subKey)
        {
            if (bonusValuesByKey.TryGetValue(key, out Dictionary<object, float> bonusValuesBySubKey) && bonusValuesBySubKey.Remove(subKey, out float value))
            {
                float prevValue = Value;
                BonusValue -= value;
                TryInvokeValueChangedEvent(Value, prevValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 모든 보너스 값을 제거합니다.
        /// </summary>
        public void ClearBonusValues()
        {
            if (bonusValuesByKey.Count == 0)
            {
                return;
            }

            float prevValue = Value;
            bonusValuesByKey.Clear();
            BonusValue = 0;
            TryInvokeValueChangedEvent(Value, prevValue);
        }

        /// <summary>
        /// 대상이 제공한 보너스 값이 있는지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 대상입니다.</param>
        /// <returns>보너스 값이 있으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool ContainsBonusValue(object key)
            => bonusValuesByKey.ContainsKey(key);

        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값이 있는지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 대상입니다.</param>
        /// <param name="subKey">확인할 세부 구분 키입니다.</param>
        /// <returns>보너스 값이 있으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool ContainsBonusValue(object key, object subKey)
            => bonusValuesByKey.TryGetValue(key, out Dictionary<object, float> bonusValuesBySubKey) && bonusValuesBySubKey.ContainsKey(subKey);
        #endregion // 보너스값

        #region 표시
        /// <summary>
        /// 현재 값을 표시용 문자열로 반환합니다.
        /// 백분율 타입이면 0에서 100 사이의 비율로 변환합니다.
        /// </summary>
        /// <returns>현재 값을 나타내는 문자열입니다.</returns>
        public string GetDisplayValue()
        {
            return isPercentType
            ? $"{Value * 100f:0.##;-0.##}%"
            : Value.ToString("0.##;-0.##");
        }
        #endregion // 표시
        
        /// <summary>
        /// 값이 실제로 변경되었을 때 변경, 최댓값, 최솟값 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="currentValue">현재 값입니다.</param>
        /// <param name="prevValue">변경 전 값입니다.</param>
        private void TryInvokeValueChangedEvent(float currentValue, float prevValue)
        {
            if (Mathf.Approximately(currentValue, prevValue))
            {
                return;
            }

            OnValueChanged?.Invoke(this, currentValue, prevValue);

            if (Mathf.Approximately(currentValue, MaxValue))
            {
                OnValueMax?.Invoke(this, MaxValue, prevValue);
            }
            else if (Mathf.Approximately(currentValue, MinValue))
            {
                OnValueMin?.Invoke(this, MinValue, prevValue);
            }
        }
    }
}
