using SWTools;
using SWUtils;
using UnityEngine;

namespace SWPooling
{
    /// <summary>
    /// 시작 때 지정한 프리팹들을 대상 풀에 미리 등록하고 생성하는 컴포넌트입니다.
    /// </summary>
    public class SWPoolRegistry : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private SWPool targetPool;
        [SerializeField] private SWPoolCatalog poolCatalog;
        [SerializeField] private bool unregisterOnDestroy = true;
        #endregion // 필드

        #region 초기화
        private void Awake()
        {
            SWPool resolvedPool = ResolveTargetPool();
            if (resolvedPool == null)
            {
                SWUtilsLog.LogError("[SWPoolRegistry] SWPool을 찾을 수 없습니다.");
                return;
            }

            if (poolCatalog == null)
            {
                SWUtilsLog.LogWarning("[SWPoolRegistry] SWPoolCatalog가 없어 풀 등록을 건너뜁니다.");
                return;
            }

            RegisterCatalog(resolvedPool, poolCatalog);
        }

        private void OnDestroy()
        {
            if (!unregisterOnDestroy || targetPool == null || poolCatalog == null)
                return;

            UnregisterCatalog(targetPool, poolCatalog);
        }
        #endregion // 초기화

        #region 내부
        /// <summary>
        /// 등록 대상 풀을 반환합니다. 지정된 풀이 없으면 전역 풀을 사용합니다.
        /// </summary>
        /// <returns>등록에 사용할 풀입니다.</returns>
        private SWPool ResolveTargetPool()
        {
            if (targetPool != null)
                return targetPool;

            targetPool = SWPool.Instance;
            return targetPool;
        }

        /// <summary>
        /// 카탈로그에 등록된 프리팹들을 대상 풀에 미리 생성합니다.
        /// </summary>
        /// <param name="pool">등록할 대상 풀입니다.</param>
        /// <param name="catalog">등록 정보가 담긴 카탈로그입니다.</param>
        private void RegisterCatalog(SWPool pool, SWPoolCatalog catalog)
        {
            for (int index = 0; index < catalog.PoolEntries.Count; ++index)
            {
                SWPoolCatalog.PoolEntry poolEntry = catalog.PoolEntries[index];
                if (poolEntry?.prefab == null)
                    continue;

                pool.RegisterPrefab(poolEntry.PoolName, poolEntry.prefab);
                pool.RegisterGroup(poolEntry.GroupName, poolEntry.prefab);

                if (poolEntry.prewarmCount > 0)
                    pool.Prewarm(poolEntry.prefab, poolEntry.prewarmCount);
            }
        }

        /// <summary>
        /// 카탈로그에 등록된 이름과 그룹 정보를 대상 풀에서 해제합니다.
        /// </summary>
        /// <param name="pool">해제할 대상 풀입니다.</param>
        /// <param name="catalog">해제 정보가 담긴 카탈로그입니다.</param>
        private void UnregisterCatalog(SWPool pool, SWPoolCatalog catalog)
        {
            for (int index = 0; index < catalog.PoolEntries.Count; ++index)
            {
                SWPoolCatalog.PoolEntry poolEntry = catalog.PoolEntries[index];
                if (poolEntry?.prefab == null)
                    continue;

                pool.UnregisterPrefab(poolEntry.PoolName, poolEntry.prefab);
                pool.UnregisterGroup(poolEntry.GroupName, poolEntry.prefab);
            }
        }
        #endregion // 내부
    }
}
