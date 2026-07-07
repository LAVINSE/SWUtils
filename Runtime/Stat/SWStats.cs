using System.Collections.Generic;
using System.Text;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Stat
{
    /// <summary>
    /// 게임 오브젝트가 보유한 런타임 능력치 목록을 관리합니다.
    /// </summary>
    public class SWStats : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("설정")]
        [Tooltip("Awake에서 자동으로 런타임 스탯을 생성할지 여부")]
        [SerializeField] private bool setupOnAwake = true;

        [SWGroup("스탯 목록")]
        [SerializeField] private SWStatOverride[] statOverrides;

        private SWStat[] stats;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>런타임 능력치가 생성되었는지 여부입니다.</summary>
        public bool IsSetup => stats != null;

        /// <summary>생성된 모든 런타임 능력치입니다.</summary>
        public SWStat[] All => stats ?? System.Array.Empty<SWStat>();
        #endregion // 프로퍼티

        #region 초기화
        private void Awake()
        {
            if (setupOnAwake)
            {
                Setup();
            }
        }

        private void OnDestroy()
        {
            if (stats == null)
            {
                return;
            }

            for (int i = 0; i < stats.Length; i++)
            {
                if (stats[i] != null)
                {
                    Destroy(stats[i]);
                }
            }

            stats = null;
        }

        /// <summary>
        /// 등록된 재정의 설정으로 런타임 능력치 복제본을 생성합니다.
        /// </summary>
        public void Setup()
        {
            if (IsSetup)
            {
                SWLog.LogWarning($"[SWStats] 이미 Setup 되었습니다: {name}");
                return;
            }

            int count = statOverrides != null ? statOverrides.Length : 0;
            List<SWStat> statList = new(count);

            for (int i = 0; i < count; i++)
            {
                SWStat createdStat = statOverrides[i]?.CreateStat();

                if (createdStat != null)
                {
                    statList.Add(createdStat);
                }
            }

            stats = statList.ToArray();
        }
        #endregion // 초기화

        #region 조회
        /// <summary>
        /// 능력치 에셋에 해당하는 런타임 능력치를 반환합니다.
        /// </summary>
        /// <param name="stat">찾을 능력치 에셋입니다.</param>
        /// <returns>찾은 런타임 능력치이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWStat GetStat(SWStat stat)
        {
            if (stat == null)
            {
                SWLog.LogError($"[SWStats] GetStat 실패: stat이 null입니다. Owner: {name}");
                return null;
            }

            if (stats == null)
            {
                SWLog.LogError($"[SWStats] GetStat 실패: Setup되지 않았습니다. Owner: {name}");
                return null;
            }

            for (int i = 0; i < stats.Length; i++)
            {
                if (stats[i] != null && stats[i].IsSameStat(stat))
                {
                    return stats[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 능력치 에셋에 해당하는 런타임 능력치를 찾습니다.
        /// </summary>
        /// <param name="stat">찾을 능력치입니다.</param>
        /// <param name="outStat">찾은 런타임 능력치입니다.</param>
        /// <returns>능력치를 찾았으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool TryGetStat(SWStat stat, out SWStat outStat)
        {
            outStat = GetStat(stat);
            return outStat != null;
        }

        /// <summary>
        /// 지정한 능력치를 보유하고 있는지 확인합니다.
        /// </summary>
        /// <param name="stat">확인할 능력치입니다.</param>
        /// <returns>능력치를 보유하고 있으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool HasStat(SWStat stat)
            => GetStat(stat) != null;

        /// <summary>
        /// 능력치의 현재 최종 값을 반환합니다.
        /// </summary>
        /// <param name="stat">조회할 능력치 에셋입니다.</param>
        /// <returns>현재 값이며, 능력치가 없으면 0입니다.</returns>
        public float GetValue(SWStat stat)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null ? runtimeStat.Value : 0f;
        }
        #endregion // 조회

        #region 기본값
        /// <summary>
        /// 능력치의 기본값을 설정합니다.
        /// </summary>
        /// <param name="stat">설정할 능력치입니다.</param>
        /// <param name="value">설정할 기본값입니다.</param>
        public void SetDefaultValue(SWStat stat, float value)
        {
            SWStat runtimeStat = GetStat(stat);

            if (runtimeStat != null)
            {
                runtimeStat.DefaultValue = value;
            }
        }

        /// <summary>
        /// 능력치의 기본값을 반환합니다.
        /// </summary>
        /// <param name="stat">조회할 능력치입니다.</param>
        /// <returns>기본값이며, 능력치가 없으면 0입니다.</returns>
        public float GetDefaultValue(SWStat stat)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null ? runtimeStat.DefaultValue : 0f;
        }

        /// <summary>
        /// 능력치의 기본값을 지정한 값만큼 변경합니다.
        /// </summary>
        /// <param name="stat">변경할 능력치입니다.</param>
        /// <param name="value">적용할 변화량입니다. 음수이면 값이 감소합니다.</param>
        public void IncreaseDefaultValue(SWStat stat, float value)
        {
            SWStat runtimeStat = GetStat(stat);

            if (runtimeStat != null)
            {
                runtimeStat.DefaultValue += value;
            }
        }
        #endregion // 기본값

        #region 보너스값
        /// <summary>
        /// 능력치에 대상별 보너스 값을 설정합니다.
        /// </summary>
        /// <param name="stat">보너스를 적용할 능력치입니다.</param>
        /// <param name="key">보너스를 제공한 대상입니다.</param>
        /// <param name="value">설정할 보너스 값입니다.</param>
        public void SetBonusValue(SWStat stat, object key, float value)
            => GetStat(stat)?.SetBonusValue(key, value);

        /// <summary>
        /// 능력치에 대상과 세부 구분 키별 보너스 값을 설정합니다.
        /// </summary>
        /// <param name="stat">보너스를 적용할 능력치입니다.</param>
        /// <param name="key">보너스를 제공한 대상입니다.</param>
        /// <param name="subKey">같은 대상의 보너스를 구분하는 키입니다.</param>
        /// <param name="value">설정할 보너스 값입니다.</param>
        public void SetBonusValue(SWStat stat, object key, object subKey, float value)
            => GetStat(stat)?.SetBonusValue(key, subKey, value);

        /// <summary>
        /// 능력치에 적용된 모든 보너스 값의 합을 반환합니다.
        /// </summary>
        /// <param name="stat">조회할 능력치입니다.</param>
        /// <returns>보너스 값의 합이며, 능력치가 없으면 0입니다.</returns>
        public float GetBonusValue(SWStat stat)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null ? runtimeStat.BonusValue : 0f;
        }

        /// <summary>
        /// 대상이 능력치에 제공한 모든 보너스 값의 합을 반환합니다.
        /// </summary>
        /// <param name="stat">조회할 능력치입니다.</param>
        /// <param name="key">조회할 대상입니다.</param>
        /// <returns>대상이 제공한 보너스 값의 합이며, 능력치가 없으면 0입니다.</returns>
        public float GetBonusValue(SWStat stat, object key)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null ? runtimeStat.GetBonusValue(key) : 0f;
        }

        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값을 반환합니다.
        /// </summary>
        /// <param name="stat">조회할 능력치입니다.</param>
        /// <param name="key">조회할 대상입니다.</param>
        /// <param name="subKey">조회할 세부 구분 키입니다.</param>
        /// <returns>저장된 보너스 값이며, 값이 없으면 0입니다.</returns>
        public float GetBonusValue(SWStat stat, object key, object subKey)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null ? runtimeStat.GetBonusValue(key, subKey) : 0f;
        }

        /// <summary>
        /// 대상이 능력치에 제공한 모든 보너스 값을 제거합니다.
        /// </summary>
        /// <param name="stat">보너스를 제거할 능력치입니다.</param>
        /// <param name="key">제거할 대상입니다.</param>
        public void RemoveBonusValue(SWStat stat, object key)
            => GetStat(stat)?.RemoveBonusValue(key);

        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값을 제거합니다.
        /// </summary>
        /// <param name="stat">보너스를 제거할 능력치입니다.</param>
        /// <param name="key">제거할 대상입니다.</param>
        /// <param name="subKey">제거할 세부 구분 키입니다.</param>
        public void RemoveBonusValue(SWStat stat, object key, object subKey)
            => GetStat(stat)?.RemoveBonusValue(key, subKey);

        /// <summary>
        /// 대상이 제공한 보너스 값이 있는지 확인합니다.
        /// </summary>
        /// <param name="stat">확인할 능력치입니다.</param>
        /// <param name="key">확인할 대상입니다.</param>
        /// <returns>보너스 값이 있으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool ContainsBonusValue(SWStat stat, object key)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null && runtimeStat.ContainsBonusValue(key);
        }

        /// <summary>
        /// 대상과 세부 구분 키에 해당하는 보너스 값이 있는지 확인합니다.
        /// </summary>
        /// <param name="stat">확인할 능력치입니다.</param>
        /// <param name="key">확인할 대상입니다.</param>
        /// <param name="subKey">확인할 세부 구분 키입니다.</param>
        /// <returns>보너스 값이 있으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool ContainsBonusValue(SWStat stat, object key, object subKey)
        {
            SWStat runtimeStat = GetStat(stat);
            return runtimeStat != null && runtimeStat.ContainsBonusValue(key, subKey);
        }
        #endregion // 보너스값

        #region 디버그
        /// <summary>
        /// 현재 능력치 상태를 로그로 출력합니다.
        /// </summary>
        [SWButton("스탯 로그 출력")]
        private void LogStats()
        {
            if (!IsSetup)
            {
                SWLog.Log($"[SWStats] {name}: 아직 Setup되지 않았습니다");
                return;
            }

            StringBuilder stringBuilder = new($"[SWStats] {name} 스탯 목록\n");

            for (int i = 0; i < stats.Length; i++)
            {
                SWStat stat = stats[i];

                if (stat == null)
                {
                    continue;
                }

                stringBuilder
                .Append(stat.DisplayName)
                .Append(": ")
                .Append(stat.GetDisplayValue())
                .Append(" (기본 ")
                .Append(stat.DefaultValue.ToString("0.##"))
                .Append(" + 보너스 ")
                .Append(stat.BonusValue.ToString("0.##"))
                .AppendLine(")");
            }

            SWLog.Log(stringBuilder.ToString());
        }
        #endregion // 디버그
    }
}
