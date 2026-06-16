using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 숫자 단위 포맷 설정을 저장하는 프리셋 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWAmountFormatProfile", menuName = "SWUtils/Amount Format Profile")]
    public class SWAmountFormatProfile : ScriptableObject
    {
        #region 필드
        /// <summary>Resources 폴더에서 찾는 설정 에셋 이름입니다.</summary>
        public const string ResourceAssetName = "SWAmountFormatProfile";

        [SerializeField, Min(0)] private int decimalPlaces = 1;
        [SerializeField] private bool keepTrailingZeros;
        [SerializeField] private bool useGroupSeparator;
        [SerializeField] private SWAmountFormatRoundingMode roundingMode = SWAmountFormatRoundingMode.Truncate;
        [SerializeField] private List<UnitData> units = new List<UnitData>
        {
            new UnitData("1000000000000", "T"),
            new UnitData("1000000000", "B"),
            new UnitData("1000000", "M"),
            new UnitData("1000", "K"),
        };

        [NonSerialized] private bool isCacheDirty = true;
        [NonSerialized] private SWAmountFormatUnit[] cachedUnits;
        private static SWAmountFormatProfile defaultProfileCache;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>소수점 자리수입니다.</summary>
        public int DecimalPlaces => decimalPlaces;

        /// <summary>남는 0을 유지할지 여부입니다.</summary>
        public bool KeepTrailingZeros => keepTrailingZeros;

        /// <summary>단위가 적용되지 않은 숫자에 천 단위 구분자를 사용할지 여부입니다.</summary>
        public bool UseGroupSeparator => useGroupSeparator;

        /// <summary>소수점 처리 방식입니다.</summary>
        public SWAmountFormatRoundingMode RoundingMode => roundingMode;
        #endregion // 프로퍼티

        #region 포맷
        /// <summary>
        /// Resources 폴더의 기본 숫자 포맷 프리셋을 불러옵니다.
        /// 없으면 기본값을 가진 임시 인스턴스를 반환합니다.
        /// </summary>
        /// <returns>기본 숫자 포맷 프리셋입니다.</returns>
        public static SWAmountFormatProfile LoadDefault()
        {
            if (defaultProfileCache != null) return defaultProfileCache;

            defaultProfileCache = Resources.Load<SWAmountFormatProfile>(ResourceAssetName);
            if (defaultProfileCache != null) return defaultProfileCache;

            defaultProfileCache = CreateInstance<SWAmountFormatProfile>();
            defaultProfileCache.ResetProfile();
            return defaultProfileCache;
        }

        /// <summary>
        /// 숫자를 현재 프리셋 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public string Format(long amount)
        {
            return Format((decimal)amount);
        }

        /// <summary>
        /// 숫자를 현재 프리셋 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public string Format(decimal amount)
        {
            return SWAmountFormat.Format(
                amount,
                GetUnits(),
                decimalPlaces,
                keepTrailingZeros,
                useGroupSeparator,
                roundingMode,
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 숫자를 현재 프리셋 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public string Format(double amount)
        {
            return SWAmountFormat.Format(
                amount,
                GetUnits(),
                decimalPlaces,
                keepTrailingZeros,
                useGroupSeparator,
                roundingMode,
                CultureInfo.InvariantCulture);
        }
        #endregion // 포맷

        #region 설정
        /// <summary>
        /// 기본 단위와 설정으로 되돌립니다.
        /// </summary>
        public void ResetProfile()
        {
            decimalPlaces = 1;
            keepTrailingZeros = false;
            useGroupSeparator = false;
            roundingMode = SWAmountFormatRoundingMode.Truncate;
            units = new List<UnitData>
            {
                new UnitData("1000000000000", "T"),
                new UnitData("1000000000", "B"),
                new UnitData("1000000", "M"),
                new UnitData("1000", "K"),
            };
            MarkDirty();
        }

        /// <summary>
        /// 캐시된 단위 목록을 갱신 대상으로 표시합니다.
        /// </summary>
        public void MarkDirty()
        {
            isCacheDirty = true;
        }

        private SWAmountFormatUnit[] GetUnits()
        {
            if (!isCacheDirty && cachedUnits != null) return cachedUnits;

            if (units == null || units.Count == 0)
            {
                cachedUnits = SWAmountFormat.DefaultUnits;
                isCacheDirty = false;
                return cachedUnits;
            }

            cachedUnits = new SWAmountFormatUnit[units.Count];
            for (int i = 0; i < units.Count; i++)
            {
                UnitData unit = units[i];
                decimal threshold = ParseThreshold(unit != null ? unit.ThresholdText : string.Empty);
                string suffix = unit != null ? unit.Suffix : string.Empty;
                cachedUnits[i] = new SWAmountFormatUnit(threshold, suffix);
            }

            isCacheDirty = false;
            return cachedUnits;
        }

        private static decimal ParseThreshold(string thresholdText)
        {
            if (decimal.TryParse(
                thresholdText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal threshold))
            {
                return threshold;
            }

            return 0m;
        }

        private void Reset()
        {
            ResetProfile();
        }

        private void OnValidate()
        {
            decimalPlaces = Mathf.Max(0, decimalPlaces);
            MarkDirty();
        }
        #endregion // 설정

        #region 내부 타입
        /// <summary>
        /// Unity 직렬화를 위한 숫자 단위 데이터입니다.
        /// </summary>
        [Serializable]
        private class UnitData
        {
            #region 필드
            [SerializeField] private string thresholdText;
            [SerializeField] private string suffix;
            #endregion // 필드

            #region 프로퍼티
            /// <summary>단위가 적용되는 기준값 문자열입니다.</summary>
            public string ThresholdText => thresholdText;

            /// <summary>숫자 뒤에 붙일 단위 문자열입니다.</summary>
            public string Suffix => suffix;
            #endregion // 프로퍼티

            #region 생성자
            public UnitData()
            {
                thresholdText = "1000";
                suffix = "K";
            }

            public UnitData(string thresholdText, string suffix)
            {
                this.thresholdText = thresholdText;
                this.suffix = suffix;
            }
            #endregion // 생성자
        }
        #endregion // 내부 타입
    }
}
