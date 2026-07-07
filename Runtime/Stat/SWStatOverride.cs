using UnityEngine;

using SW.Attributes;

using SW.Util;

namespace SW.Stat
{
    /// <summary>
    /// 능력치 에셋의 기본값을 개체별로 재정의하는 설정입니다.
    /// </summary>
    [System.Serializable]
    public class SWStatOverride
    {
        #region 필드
        [SerializeField] private SWStat stat;
        [SerializeField] private bool isUseOverride;
        [SerializeField, SWCondition("isUseOverride", true)] private float overrideDefaultValue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>재정의할 능력치 에셋입니다.</summary>
        public SWStat Stat => stat;

        /// <summary>기본값 재정의를 사용할지 여부입니다.</summary>
        public bool IsUseOverride => isUseOverride;
        public float OverrideDefaultValue => overrideDefaultValue;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 능력치 에셋을 지정하여 재정의 설정을 생성합니다.
        /// </summary>
        /// <param name="stat">대상 능력치 에셋입니다.</param>
        public SWStatOverride(SWStat stat)
            => this.stat = stat;
        #endregion // 생성자

        /// <summary>
        /// 재정의 설정을 적용한 런타임 능력치 복제본을 생성합니다.
        /// </summary>
        /// <returns>생성된 런타임 능력치이며, 능력치 에셋이 없으면 <see langword="null"/>입니다.</returns>
        public SWStat CreateStat()
        {
            if (stat == null)
            {
                SWLog.LogError("[SWStatOverride] CreateStat 실패: 스탯 에셋이 비어 있습니다");
                return null;
            }

            SWStat newStat = stat.CreateRuntimeClone();

            if (isUseOverride)
            {
                newStat.DefaultValue = overrideDefaultValue;
            }

            return newStat;
        }
    }
}
