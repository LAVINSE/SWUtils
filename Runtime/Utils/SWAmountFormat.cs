using System;
using System.Globalization;

namespace SWUtils
{
    /// <summary>
    /// 숫자를 K, M, B, T 같은 축약 단위 문자열로 변환하는 유틸리티입니다.
    /// </summary>
    public static class SWAmountFormat
    {
        #region 필드
        private const int DefaultDecimalPlaces = 1;
        private static readonly string[] DecimalFormats =
        {
            "0",
            "0.#",
            "0.##",
            "0.###",
            "0.####",
            "0.#####",
            "0.######",
        };
        private static readonly string[] FixedDecimalFormats =
        {
            "0",
            "0.0",
            "0.00",
            "0.000",
            "0.0000",
            "0.00000",
            "0.000000",
        };

        /// <summary>기본 숫자 단위 목록입니다.</summary>
        public static readonly SWAmountFormatUnit[] DefaultUnits =
        {
            new SWAmountFormatUnit(1_000_000_000_000m, "T"),
            new SWAmountFormatUnit(1_000_000_000m, "B"),
            new SWAmountFormatUnit(1_000_000m, "M"),
            new SWAmountFormatUnit(1_000m, "K"),
        };
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 숫자를 기본 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(long amount)
        {
            bool isNegative = amount < 0;
            decimal absoluteAmount = amount == long.MinValue
                ? (decimal)long.MaxValue + 1m
                : Math.Abs(amount);

            string formattedAmount = FormatAbsolute(
                absoluteAmount,
                DefaultUnits,
                DefaultDecimalPlaces,
                false,
                false,
                SWAmountFormatRoundingMode.Truncate,
                CultureInfo.InvariantCulture);

            return isNegative ? $"-{formattedAmount}" : formattedAmount;
        }

        /// <summary>
        /// 숫자를 기본 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(decimal amount)
        {
            return Format(amount, DefaultUnits);
        }

        /// <summary>
        /// 숫자를 지정한 프리셋 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <param name="profile">사용할 포맷 프리셋입니다. null이면 기본 설정을 사용합니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(long amount, SWAmountFormatProfile profile)
        {
            return profile != null ? profile.Format(amount) : Format(amount);
        }

        /// <summary>
        /// 숫자를 지정한 프리셋 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <param name="profile">사용할 포맷 프리셋입니다. null이면 기본 설정을 사용합니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(decimal amount, SWAmountFormatProfile profile)
        {
            return profile != null ? profile.Format(amount) : Format(amount);
        }

        /// <summary>
        /// 숫자를 지정한 프리셋 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <param name="profile">사용할 포맷 프리셋입니다. null이면 기본 설정을 사용합니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(double amount, SWAmountFormatProfile profile)
        {
            return profile != null ? profile.Format(amount) : Format(amount, DefaultUnits);
        }

        /// <summary>
        /// 숫자를 지정한 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <param name="units">사용할 단위 목록입니다. 기준값이 0보다 큰 항목만 사용합니다.</param>
        /// <param name="decimalPlaces">소수점 자리수입니다.</param>
        /// <param name="keepTrailingZeros">남는 0을 유지할지 여부입니다.</param>
        /// <param name="useGroupSeparator">단위가 적용되지 않은 숫자에 천 단위 구분자를 사용할지 여부입니다.</param>
        /// <param name="roundingMode">소수점 처리 방식입니다.</param>
        /// <param name="formatProvider">숫자 표시에 사용할 문화권 정보입니다. null이면 InvariantCulture를 사용합니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(
            decimal amount,
            SWAmountFormatUnit[] units,
            int decimalPlaces = DefaultDecimalPlaces,
            bool keepTrailingZeros = false,
            bool useGroupSeparator = false,
            SWAmountFormatRoundingMode roundingMode = SWAmountFormatRoundingMode.Truncate,
            IFormatProvider formatProvider = null)
        {
            bool isNegative = amount < 0m;
            decimal absoluteAmount = Math.Abs(amount);
            string formattedAmount = FormatAbsolute(
                absoluteAmount,
                units,
                decimalPlaces,
                keepTrailingZeros,
                useGroupSeparator,
                roundingMode,
                formatProvider ?? CultureInfo.InvariantCulture);

            return isNegative ? $"-{formattedAmount}" : formattedAmount;
        }

        /// <summary>
        /// double 숫자를 지정한 설정으로 단위 문자열로 변환합니다.
        /// </summary>
        /// <param name="amount">변환할 숫자입니다.</param>
        /// <param name="units">사용할 단위 목록입니다.</param>
        /// <param name="decimalPlaces">소수점 자리수입니다.</param>
        /// <param name="keepTrailingZeros">남는 0을 유지할지 여부입니다.</param>
        /// <param name="useGroupSeparator">단위가 적용되지 않은 숫자에 천 단위 구분자를 사용할지 여부입니다.</param>
        /// <param name="roundingMode">소수점 처리 방식입니다.</param>
        /// <param name="formatProvider">숫자 표시에 사용할 문화권 정보입니다.</param>
        /// <returns>화면에 표시할 단위 문자열입니다.</returns>
        public static string Format(
            double amount,
            SWAmountFormatUnit[] units,
            int decimalPlaces = DefaultDecimalPlaces,
            bool keepTrailingZeros = false,
            bool useGroupSeparator = false,
            SWAmountFormatRoundingMode roundingMode = SWAmountFormatRoundingMode.Truncate,
            IFormatProvider formatProvider = null)
        {
            return Format(
                Convert.ToDecimal(amount),
                units,
                decimalPlaces,
                keepTrailingZeros,
                useGroupSeparator,
                roundingMode,
                formatProvider);
        }

        private static string FormatAbsolute(
            decimal absoluteAmount,
            SWAmountFormatUnit[] units,
            int decimalPlaces,
            bool keepTrailingZeros,
            bool useGroupSeparator,
            SWAmountFormatRoundingMode roundingMode,
            IFormatProvider formatProvider)
        {
            if (TryGetBestUnit(absoluteAmount, units, out SWAmountFormatUnit unit))
            {
                decimal unitAmount = absoluteAmount / unit.Threshold;
                decimal adjustedAmount = ApplyDecimalPlaces(unitAmount, decimalPlaces, roundingMode);
                return $"{adjustedAmount.ToString(CreateDecimalFormat(decimalPlaces, keepTrailingZeros), formatProvider)}{unit.Suffix}";
            }

            return absoluteAmount.ToString(useGroupSeparator ? "#,0" : "0", formatProvider);
        }

        private static bool TryGetBestUnit(decimal absoluteAmount, SWAmountFormatUnit[] units, out SWAmountFormatUnit bestUnit)
        {
            bestUnit = default(SWAmountFormatUnit);
            if (units == null || units.Length == 0) return false;

            decimal bestThreshold = 0m;
            for (int i = 0; i < units.Length; i++)
            {
                SWAmountFormatUnit unit = units[i];
                if (unit.Threshold <= 0m) continue;
                if (absoluteAmount < unit.Threshold) continue;
                if (unit.Threshold <= bestThreshold) continue;

                bestThreshold = unit.Threshold;
                bestUnit = unit;
            }

            return bestThreshold > 0m;
        }

        private static decimal ApplyDecimalPlaces(decimal amount, int decimalPlaces, SWAmountFormatRoundingMode roundingMode)
        {
            int safeDecimalPlaces = Math.Max(0, decimalPlaces);
            decimal multiplier = Pow10(safeDecimalPlaces);

            switch (roundingMode)
            {
                case SWAmountFormatRoundingMode.Round:
                    return Math.Round(amount, safeDecimalPlaces, MidpointRounding.AwayFromZero);
                case SWAmountFormatRoundingMode.Floor:
                    return Math.Floor(amount * multiplier) / multiplier;
                case SWAmountFormatRoundingMode.Ceiling:
                    return Math.Ceiling(amount * multiplier) / multiplier;
                default:
                    return Math.Truncate(amount * multiplier) / multiplier;
            }
        }

        private static decimal Pow10(int decimalPlaces)
        {
            decimal result = 1m;
            for (int i = 0; i < decimalPlaces; i++)
            {
                result *= 10m;
            }
            return result;
        }

        private static string CreateDecimalFormat(int decimalPlaces, bool keepTrailingZeros)
        {
            int safeDecimalPlaces = Math.Max(0, decimalPlaces);
            if (safeDecimalPlaces == 0) return "0";
            if (safeDecimalPlaces < DecimalFormats.Length)
            {
                return keepTrailingZeros
                    ? FixedDecimalFormats[safeDecimalPlaces]
                    : DecimalFormats[safeDecimalPlaces];
            }

            char decimalPlaceChar = keepTrailingZeros ? '0' : '#';
            return "0." + new string(decimalPlaceChar, safeDecimalPlaces);
        }
        #endregion // 함수
    }

    /// <summary>
    /// 숫자 축약에 사용할 단위 정보입니다.
    /// </summary>
    [Serializable]
    public struct SWAmountFormatUnit
    {
        #region 필드
        /// <summary>단위가 적용되는 기준값입니다.</summary>
        public decimal Threshold;
        /// <summary>숫자 뒤에 붙일 단위 문자열입니다.</summary>
        public string Suffix;
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 숫자 단위 정보를 생성합니다.
        /// </summary>
        /// <param name="threshold">단위가 적용되는 기준값입니다.</param>
        /// <param name="suffix">숫자 뒤에 붙일 단위 문자열입니다.</param>
        public SWAmountFormatUnit(decimal threshold, string suffix)
        {
            Threshold = threshold;
            Suffix = suffix ?? string.Empty;
        }
        #endregion // 생성자
    }

    /// <summary>
    /// 소수점 처리 방식입니다.
    /// </summary>
    public enum SWAmountFormatRoundingMode
    {
        /// <summary>버림 처리합니다.</summary>
        Truncate,
        /// <summary>반올림 처리합니다.</summary>
        Round,
        /// <summary>내림 처리합니다.</summary>
        Floor,
        /// <summary>올림 처리합니다.</summary>
        Ceiling,
    }
}
