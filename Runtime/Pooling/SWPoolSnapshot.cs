using System.Collections.Generic;
using UnityEngine;

namespace SWPooling
{
    /// <summary>
    /// SWPool에 등록된 프리팹 풀 하나의 현재 상태를 표시하기 위한 읽기 전용 스냅샷입니다.
    /// </summary>
    public sealed class SWPoolSnapshot
    {
        /// <summary>
        /// 풀 상태 값을 지정해 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="prefab">풀의 원본 프리팹입니다.</param>
        /// <param name="poolNames">프리팹에 연결된 풀 이름 목록입니다.</param>
        /// <param name="groupNames">프리팹이 포함된 그룹 이름 목록입니다.</param>
        /// <param name="createdCount">생성된 인스턴스 수입니다.</param>
        /// <param name="activeCount">현재 사용 중인 인스턴스 수입니다.</param>
        /// <param name="inactiveCount">풀 안에 대기 중인 인스턴스 수입니다.</param>
        /// <param name="spawnCount">풀에서 꺼낸 총 횟수입니다.</param>
        /// <param name="releaseCount">풀로 반환한 총 횟수입니다.</param>
        /// <param name="destroyedCount">풀 크기 제한 또는 정리로 파괴된 총 횟수입니다.</param>
        /// <param name="delayedReleaseCount">지연 반환 예약 수입니다.</param>
        public SWPoolSnapshot(GameObject prefab, IReadOnlyList<string> poolNames, IReadOnlyList<string> groupNames,
            int createdCount, int activeCount, int inactiveCount, int spawnCount, int releaseCount,
            int destroyedCount, int delayedReleaseCount)
        {
            Prefab = prefab;
            PoolNames = poolNames;
            GroupNames = groupNames;
            CreatedCount = createdCount;
            ActiveCount = activeCount;
            InactiveCount = inactiveCount;
            SpawnCount = spawnCount;
            ReleaseCount = releaseCount;
            DestroyedCount = destroyedCount;
            DelayedReleaseCount = delayedReleaseCount;
        }

        /// <summary>풀의 원본 프리팹입니다.</summary>
        public GameObject Prefab { get; }

        /// <summary>프리팹에 연결된 풀 이름 목록입니다.</summary>
        public IReadOnlyList<string> PoolNames { get; }

        /// <summary>프리팹이 포함된 그룹 이름 목록입니다.</summary>
        public IReadOnlyList<string> GroupNames { get; }

        /// <summary>생성된 인스턴스 수입니다.</summary>
        public int CreatedCount { get; }

        /// <summary>현재 사용 중인 인스턴스 수입니다.</summary>
        public int ActiveCount { get; }

        /// <summary>풀 안에 대기 중인 인스턴스 수입니다.</summary>
        public int InactiveCount { get; }

        /// <summary>풀에서 꺼낸 총 횟수입니다.</summary>
        public int SpawnCount { get; }

        /// <summary>풀로 반환한 총 횟수입니다.</summary>
        public int ReleaseCount { get; }

        /// <summary>풀 크기 제한 또는 정리로 파괴된 총 횟수입니다.</summary>
        public int DestroyedCount { get; }

        /// <summary>지연 반환 예약 수입니다.</summary>
        public int DelayedReleaseCount { get; }
    }
}
