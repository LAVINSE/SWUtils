using System.Collections.Generic;
using UnityEngine;

namespace SWPooling
{
    /// <summary>
    /// 풀에 미리 생성할 프리팹 목록을 에셋으로 관리하는 카탈로그입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWPoolCatalog", menuName = "SWUtils/Pooling/Pool Catalog")]
    public class SWPoolCatalog : ScriptableObject
    {
        #region 데이터
        /// <summary>
        /// 미리 생성할 프리팹과 개수를 저장하는 풀 등록 정보입니다.
        /// </summary>
        [System.Serializable]
        public class PoolEntry
        {
            /// <summary>이름으로 풀을 찾을 때 사용할 키입니다. 비어 있으면 프리팹 이름을 사용합니다.</summary>
            public string poolName;
            /// <summary>그룹 선택 기능에서 사용할 그룹 이름입니다.</summary>
            public string groupName;
            /// <summary>풀링할 프리팹입니다.</summary>
            public GameObject prefab;
            /// <summary>미리 생성할 개수입니다.</summary>
            public int prewarmCount = 1;

            /// <summary>실제로 등록에 사용할 풀 이름입니다.</summary>
            public string PoolName => string.IsNullOrWhiteSpace(poolName) && prefab != null ? prefab.name : poolName;
            /// <summary>실제로 등록에 사용할 그룹 이름입니다.</summary>
            public string GroupName => groupName;
        }
        #endregion // 데이터

        #region 필드
        [SerializeField] private PoolEntry[] poolEntries = new PoolEntry[0];
        #endregion // 필드

        #region 프로퍼티
        /// <summary>미리 생성할 풀 등록 정보 목록입니다.</summary>
        public IReadOnlyList<PoolEntry> PoolEntries => poolEntries;
        #endregion // 프로퍼티
    }
}
