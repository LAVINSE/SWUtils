using System;
using UnityEngine;

using SW.Util;

namespace SW.Data
{
    /// <summary>클라우드와 기기 저장이 충돌한 이유입니다.</summary>
    public enum ESaveConflictReason
    {
        /// <summary>마지막으로 맞춘 뒤 기기와 클라우드가 둘 다 바뀌었습니다(여러 기기에서 번갈아 플레이).</summary>
        BothChanged,
        /// <summary>마지막으로 맞춘 계정과 지금 계정이 다릅니다.</summary>
        AccountChanged,
        /// <summary>맞춘 기록이 없는데(이전 버전 저장, 기록 삭제) 두 저장이 다릅니다.</summary>
        NoSyncRecord
    }

    /// <summary>
    /// 충돌한 저장 한쪽(기기 또는 클라우드)입니다. 게임은 내용을 읽어 진행 정도를 보여 주고 플레이어에게 고르게 할 수 있습니다.
    /// </summary>
    public sealed class SWSaveSnapshot
    {
        #region 프로퍼티
        /// <summary>저장 데이터 JSON(평문)입니다. 없으면 빈 문자열입니다.</summary>
        public string DataJson { get; }
        /// <summary>저장한 시각(UTC, ISO 8601)입니다. 모르면 빈 문자열입니다.</summary>
        public string SavedAtUtc { get; }
        /// <summary>그 기기에서 저장한 횟수입니다. 모르면 0입니다.</summary>
        public int Revision { get; }
        #endregion // 프로퍼티

        #region 생성
        internal SWSaveSnapshot(string dataJson, string savedAtUtc, int revision)
        {
            DataJson = dataJson ?? string.Empty;
            SavedAtUtc = savedAtUtc ?? string.Empty;
            Revision = revision;
        }
        #endregion // 생성

        #region 읽기
        /// <summary>저장 데이터를 T로 읽습니다. 읽지 못하면 null입니다.</summary>
        /// <typeparam name="T">SetData에 등록한 저장 데이터 타입</typeparam>
        public T ReadData<T>() where T : class
        {
            if (string.IsNullOrEmpty(DataJson))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(DataJson);
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveSnapshot] 저장 데이터 읽기 실패: {exception.Message}");
                return null;
            }
        }
        #endregion // 읽기
    }

    /// <summary>
    /// SWSaveDataManager.LoadAll이 찾은 충돌입니다. ConflictHandler에서 받아 UseCloud 또는 KeepLocal 중 하나를 한 번 부릅니다.
    /// 부를 때까지 LoadAll은 끝나지 않습니다(팝업으로 물어볼 수 있음).
    /// </summary>
    public sealed class SWSaveConflict
    {
        #region 필드
        private Action<bool> resolve;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>충돌한 슬롯 이름입니다.</summary>
        public string Slot { get; }
        /// <summary>충돌 이유입니다.</summary>
        public ESaveConflictReason Reason { get; }
        /// <summary>이 기기의 저장입니다.</summary>
        public SWSaveSnapshot Local { get; }
        /// <summary>클라우드의 저장입니다.</summary>
        public SWSaveSnapshot Cloud { get; }
        /// <summary>UseCloud 또는 KeepLocal을 이미 불렀는지 여부입니다.</summary>
        public bool IsResolved => resolve == null;
        #endregion // 프로퍼티

        #region 생성
        internal SWSaveConflict(string slot, ESaveConflictReason reason, SWSaveSnapshot local, SWSaveSnapshot cloud, Action<bool> resolve)
        {
            Slot = slot;
            Reason = reason;
            Local = local;
            Cloud = cloud;
            this.resolve = resolve;
        }
        #endregion // 생성

        #region 해결
        /// <summary>클라우드 저장을 이 기기에 씁니다. 이 기기의 이전 저장은 백업 파일(.bak)로 남습니다.</summary>
        public void UseCloud()
        {
            Resolve(true);
        }

        /// <summary>이 기기의 저장을 유지하고 클라우드에 올립니다.</summary>
        public void KeepLocal()
        {
            Resolve(false);
        }

        /// <summary>한 번만 결과를 넘깁니다. 두 번째 호출은 경고만 남깁니다.</summary>
        private void Resolve(bool useCloud)
        {
            if (resolve == null)
            {
                SWLog.LogWarning("[SWSaveConflict] 이미 해결한 충돌입니다.");
                return;
            }

            Action<bool> callback = resolve;
            resolve = null;
            callback(useCloud);
        }
        #endregion // 해결
    }
}
