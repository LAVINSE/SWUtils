using UnityEngine;

namespace SW.Stat
{
    /// <summary>
    /// 지정한 능력치 값에 비례하여 최종 값을 계산하는 설정입니다.
    /// </summary>
    [System.Serializable]
    public struct SWStatScaleFloat
    {
        /// <summary>능력치 비율을 적용하기 전의 기본값입니다.</summary>
        public float defaultValue;

        /// <summary>비례 기준이 되는 능력치입니다. 비어 있으면 기본값을 그대로 사용합니다.</summary>
        public SWStat scaleStat;

        /// <summary>
        /// 능력치를 적용한 최종 값을 계산합니다.
        /// </summary>
        /// <param name="stats">능력치를 조회할 대상입니다.</param>
        /// <returns>기본값에 능력치 비율을 적용한 값이며, 능력치가 없으면 기본값입니다.</returns>
        public float GetValue(SWStats stats)
        {
            if (scaleStat != null && stats != null && stats.TryGetStat(scaleStat, out SWStat stat))
            {
                return defaultValue * (1f + stat.Value);
            }

            return defaultValue;
        }
    }
}
