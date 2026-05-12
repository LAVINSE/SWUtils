using System.Collections;
using System.Collections.Generic;
using SWTools;
using SWUtils;
using UnityEngine;
using UnityEngine.Pool;

namespace SWPooling
{
    /// <summary>
    /// Unity ObjectPool 기반으로 프리팹별 오브젝트 풀을 전역 관리하는 컴포넌트입니다.
    /// </summary>
    public class SWPool : SWSingleton<SWPool>, IPool
    {
        #region 필드
        [SWGroup("=====> 설정 <=====")]
        [SerializeField] private bool collectionCheck = true;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 1000;

        private readonly Dictionary<GameObject, ObjectPool<GameObject>> poolDictionary = new();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefabDictionary = new();
        private readonly Dictionary<string, GameObject> nameToPrefabDictionary = new();
        private readonly Dictionary<string, List<GameObject>> groupToPrefabListDictionary = new();
        private readonly Dictionary<string, int> groupSequenceIndexDictionary = new();
        /// <summary>지연 반환 예약 중인 코루틴을 저장하여 조기 반환 때 취소합니다.</summary>
        private readonly Dictionary<GameObject, Coroutine> delayedReleaseDictionary = new();
        /// <summary>WaitForSeconds 캐시입니다. 반복 생성에 따른 메모리 할당을 줄입니다.</summary>
        private readonly Dictionary<float, WaitForSeconds> waitCacheDictionary = new();
        #endregion // 필드

        #region 초기화
        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
        }

        private void OnDestroy()
        {
            ClearAll();
        }
        #endregion // 초기화

        #region 풀 기능
        /// <summary>
        /// 이름으로 프리팹을 찾을 수 있도록 등록합니다.
        /// </summary>
        /// <param name="poolName">등록할 풀 이름입니다.</param>
        /// <param name="prefab">등록할 프리팹입니다.</param>
        public void RegisterPrefab(string poolName, GameObject prefab)
        {
            if (prefab == null)
            {
                SWUtilsLog.LogWarning("[SWPool] 이름 등록 실패: 프리팹이 null입니다.");
                return;
            }

            string normalizedPoolName = NormalizeKey(poolName);
            if (string.IsNullOrEmpty(normalizedPoolName))
            {
                SWUtilsLog.LogWarning("[SWPool] 이름 등록 실패: 풀 이름이 비어 있습니다.");
                return;
            }

            if (nameToPrefabDictionary.TryGetValue(normalizedPoolName, out GameObject registeredPrefab)
                && registeredPrefab != prefab)
            {
                SWUtilsLog.LogWarning($"[SWPool] 같은 이름의 프리팹 등록을 교체합니다. Name: {normalizedPoolName}");
            }

            nameToPrefabDictionary[normalizedPoolName] = prefab;
        }

        /// <summary>
        /// 그룹으로 프리팹을 선택할 수 있도록 등록합니다.
        /// </summary>
        /// <param name="groupName">등록할 그룹 이름입니다.</param>
        /// <param name="prefab">등록할 프리팹입니다.</param>
        public void RegisterGroup(string groupName, GameObject prefab)
        {
            if (prefab == null)
            {
                SWUtilsLog.LogWarning("[SWPool] 그룹 등록 실패: 프리팹이 null입니다.");
                return;
            }

            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName))
                return;

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList))
            {
                prefabList = new List<GameObject>();
                groupToPrefabListDictionary[normalizedGroupName] = prefabList;
            }

            if (!prefabList.Contains(prefab))
                prefabList.Add(prefab);
        }

        /// <summary>
        /// 이름으로 등록된 프리팹을 해제합니다.
        /// </summary>
        /// <param name="poolName">해제할 풀 이름입니다.</param>
        /// <param name="prefab">해제할 프리팹입니다.</param>
        public void UnregisterPrefab(string poolName, GameObject prefab)
        {
            string normalizedPoolName = NormalizeKey(poolName);
            if (string.IsNullOrEmpty(normalizedPoolName) || prefab == null)
                return;

            if (nameToPrefabDictionary.TryGetValue(normalizedPoolName, out GameObject registeredPrefab)
                && registeredPrefab == prefab)
            {
                nameToPrefabDictionary.Remove(normalizedPoolName);
            }
        }

        /// <summary>
        /// 그룹으로 등록된 프리팹을 해제합니다.
        /// </summary>
        /// <param name="groupName">해제할 그룹 이름입니다.</param>
        /// <param name="prefab">해제할 프리팹입니다.</param>
        public void UnregisterGroup(string groupName, GameObject prefab)
        {
            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName) || prefab == null)
                return;

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList))
                return;

            prefabList.Remove(prefab);
            if (prefabList.Count > 0) return;

            groupToPrefabListDictionary.Remove(normalizedGroupName);
            groupSequenceIndexDictionary.Remove(normalizedGroupName);
        }

        /// <inheritdoc/>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                SWUtilsLog.LogWarning("[SWPool] 프리웜 실패: 프리팹이 null입니다.");
                return;
            }

            if (count <= 0)
            {
                SWUtilsLog.LogWarning($"[SWPool] 프리웜 실패: 생성 수가 0 이하입니다. Count: {count}");
                return;
            }

            ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
            GameObject[] temporaryInstances = new GameObject[count];

            for (int index = 0; index < count; index++)
                temporaryInstances[index] = pool.Get();

            for (int index = 0; index < count; index++)
                pool.Release(temporaryInstances[index]);
        }

        /// <inheritdoc/>
        public void Prewarm(string poolName, int count)
        {
            if (!TryGetPrefab(poolName, out GameObject prefab))
                return;

            Prewarm(prefab, count);
        }

        /// <inheritdoc/>
        public GameObject Spawn(GameObject prefab, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null)
        {
            if (prefab == null)
            {
                SWUtilsLog.LogWarning("[SWPool] Spawn 실패: 프리팹이 null입니다.");
                return null;
            }

            ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
            GameObject instance = pool.Get();

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent != null ? parent : transform, false);
            instanceTransform.SetPositionAndRotation(position, rotation);

            NotifyPoolables(instance);

            return instance;
        }

        /// <inheritdoc/>
        public GameObject Spawn(string poolName, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null)
        {
            if (!TryGetPrefab(poolName, out GameObject prefab))
                return null;

            return Spawn(prefab, position, rotation, parent);
        }

        /// <inheritdoc/>
        public T Spawn<T>(GameObject prefab, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = Spawn(prefab, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public T Spawn<T>(string poolName, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = Spawn(poolName, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public GameObject SpawnFromGroup(string groupName, SWPoolGroupSelectionMode selectionMode = SWPoolGroupSelectionMode.Random,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (!TryGetPrefabFromGroup(groupName, selectionMode, out GameObject prefab))
                return null;

            return Spawn(prefab, position, rotation, parent);
        }

        /// <inheritdoc/>
        public GameObject SpawnFromGroup(string groupName, string poolName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (!TryGetPrefabFromGroup(groupName, poolName, out GameObject prefab))
                return null;

            return Spawn(prefab, position, rotation, parent);
        }

        /// <inheritdoc/>
        public T SpawnFromGroup<T>(string groupName, SWPoolGroupSelectionMode selectionMode = SWPoolGroupSelectionMode.Random,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = SpawnFromGroup(groupName, selectionMode, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public T SpawnFromGroup<T>(string groupName, string poolName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = SpawnFromGroup(groupName, poolName, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                SWUtilsLog.LogWarning("[SWPool] Release 실패: 반환할 인스턴스가 null입니다.");
                return;
            }

            CancelDelayedRelease(instance);

            if (!instanceToPrefabDictionary.TryGetValue(instance, out GameObject prefab))
            {
                SWUtilsLog.LogWarning($"[SWPool] 등록되지 않은 인스턴스라 파괴합니다: {instance.name}");
                Destroy(instance);
                return;
            }

            if (poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(instance);
                return;
            }

            SWUtilsLog.LogWarning($"[SWPool] 연결된 풀이 없어 인스턴스를 파괴합니다: {instance.name}");
            Destroy(instance);
        }

        /// <inheritdoc/>
        public void Release(GameObject instance, float delay)
        {
            if (instance == null) return;

            if (delay <= 0f)
            {
                Release(instance);
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                Release(instance);
                return;
            }

            CancelDelayedRelease(instance);

            Coroutine releaseCoroutine = StartCoroutine(DelayedReleaseRoutine(instance, delay));
            delayedReleaseDictionary[instance] = releaseCoroutine;
        }

        /// <inheritdoc/>
        public void Clear(GameObject prefab)
        {
            if (prefab == null)
            {
                SWUtilsLog.LogWarning("[SWPool] Clear 실패: 프리팹이 null입니다.");
                return;
            }

            if (!poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool)) return;

            CancelDelayedReleases(prefab);

            pool.Clear();
            poolDictionary.Remove(prefab);
            RemovePrefabInstances(prefab);
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            StopAllCoroutines();
            delayedReleaseDictionary.Clear();

            foreach (ObjectPool<GameObject> pool in poolDictionary.Values)
                pool.Clear();

            poolDictionary.Clear();
            instanceToPrefabDictionary.Clear();
        }

        /// <inheritdoc/>
        public int CountInPool(GameObject prefab)
        {
            return poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool) ? pool.CountInactive : 0;
        }

        /// <summary>
        /// 이름으로 특정 프리팹의 현재 대기 중 오브젝트 수를 반환합니다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름입니다.</param>
        /// <returns>대기 중 오브젝트 수입니다.</returns>
        public int CountInPool(string poolName)
        {
            return TryGetPrefab(poolName, out GameObject prefab) ? CountInPool(prefab) : 0;
        }
        #endregion // 풀 기능

        #region 내부
        /// <summary>
        /// 검색 키를 비교 가능한 형태로 정리합니다.
        /// </summary>
        /// <param name="key">정리할 키입니다.</param>
        /// <returns>앞뒤 공백이 제거된 키입니다.</returns>
        private string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }

        /// <summary>
        /// 등록된 이름으로 프리팹을 찾습니다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름입니다.</param>
        /// <param name="prefab">찾은 프리팹입니다.</param>
        /// <returns>프리팹을 찾았으면 true입니다.</returns>
        private bool TryGetPrefab(string poolName, out GameObject prefab)
        {
            string normalizedPoolName = NormalizeKey(poolName);
            if (string.IsNullOrEmpty(normalizedPoolName))
            {
                prefab = null;
                SWUtilsLog.LogWarning("[SWPool] 이름 검색 실패: 풀 이름이 비어 있습니다.");
                return false;
            }

            if (nameToPrefabDictionary.TryGetValue(normalizedPoolName, out prefab))
                return true;

            SWUtilsLog.LogWarning($"[SWPool] 이름 검색 실패: 등록되지 않은 풀 이름입니다. Name: {normalizedPoolName}");
            return false;
        }

        /// <summary>
        /// 그룹에서 선택 방식에 맞는 프리팹을 찾습니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="selectionMode">프리팹 선택 방식입니다.</param>
        /// <param name="prefab">선택된 프리팹입니다.</param>
        /// <returns>프리팹을 찾았으면 true입니다.</returns>
        private bool TryGetPrefabFromGroup(string groupName, SWPoolGroupSelectionMode selectionMode, out GameObject prefab)
        {
            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName))
            {
                prefab = null;
                SWUtilsLog.LogWarning("[SWPool] 그룹 검색 실패: 그룹 이름이 비어 있습니다.");
                return false;
            }

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList)
                || prefabList.Count <= 0)
            {
                prefab = null;
                SWUtilsLog.LogWarning($"[SWPool] 그룹 검색 실패: 등록되지 않은 그룹입니다. Group: {normalizedGroupName}");
                return false;
            }

            prefab = SelectPrefabFromGroup(normalizedGroupName, prefabList, selectionMode);
            return prefab != null;
        }

        /// <summary>
        /// 그룹 안에서 이름이 일치하는 프리팹을 찾습니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="poolName">등록된 풀 이름입니다.</param>
        /// <param name="prefab">찾은 프리팹입니다.</param>
        /// <returns>프리팹을 찾았으면 true입니다.</returns>
        private bool TryGetPrefabFromGroup(string groupName, string poolName, out GameObject prefab)
        {
            prefab = null;
            if (!TryGetPrefab(poolName, out GameObject namedPrefab))
                return false;

            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName))
            {
                SWUtilsLog.LogWarning("[SWPool] 그룹 이름 검색 실패: 그룹 이름이 비어 있습니다.");
                return false;
            }

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList)
                || prefabList.Count <= 0)
            {
                SWUtilsLog.LogWarning($"[SWPool] 그룹 이름 검색 실패: 등록되지 않은 그룹입니다. Group: {normalizedGroupName}");
                return false;
            }

            if (!prefabList.Contains(namedPrefab))
            {
                SWUtilsLog.LogWarning($"[SWPool] 그룹 이름 검색 실패: 해당 그룹에 등록되지 않은 풀 이름입니다. Group: {normalizedGroupName}, Name: {poolName}");
                return false;
            }

            prefab = namedPrefab;
            return true;
        }

        /// <summary>
        /// 그룹 목록에서 선택 방식에 맞는 프리팹을 반환합니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="prefabList">선택 대상 프리팹 목록입니다.</param>
        /// <param name="selectionMode">프리팹 선택 방식입니다.</param>
        /// <returns>선택된 프리팹입니다.</returns>
        private GameObject SelectPrefabFromGroup(string groupName, List<GameObject> prefabList, SWPoolGroupSelectionMode selectionMode)
        {
            switch (selectionMode)
            {
                case SWPoolGroupSelectionMode.Sequence:
                    int sequenceIndex = GetGroupSequenceIndex(groupName, prefabList.Count);
                    return prefabList[sequenceIndex];
                case SWPoolGroupSelectionMode.Random:
                default:
                    return prefabList[Random.Range(0, prefabList.Count)];
            }
        }

        /// <summary>
        /// 그룹의 순차 선택 인덱스를 반환하고 다음 인덱스로 갱신합니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="prefabCount">그룹에 등록된 프리팹 수입니다.</param>
        /// <returns>이번에 사용할 인덱스입니다.</returns>
        private int GetGroupSequenceIndex(string groupName, int prefabCount)
        {
            if (!groupSequenceIndexDictionary.TryGetValue(groupName, out int sequenceIndex))
                sequenceIndex = 0;

            int selectedIndex = Mathf.Abs(sequenceIndex) % prefabCount;
            groupSequenceIndexDictionary[groupName] = (selectedIndex + 1) % prefabCount;
            return selectedIndex;
        }

        /// <summary>
        /// 지연 반환 예약을 취소합니다.
        /// </summary>
        /// <param name="instance">예약을 취소할 인스턴스입니다.</param>
        private void CancelDelayedRelease(GameObject instance)
        {
            if (!delayedReleaseDictionary.TryGetValue(instance, out Coroutine reservedCoroutine)) return;

            if (reservedCoroutine != null)
                StopCoroutine(reservedCoroutine);

            delayedReleaseDictionary.Remove(instance);
        }

        /// <summary>
        /// 지정한 프리팹에 연결된 모든 지연 반환 예약을 취소합니다.
        /// </summary>
        /// <param name="prefab">예약을 취소할 프리팹입니다.</param>
        private void CancelDelayedReleases(GameObject prefab)
        {
            List<GameObject> instancesToCancel = new();
            foreach (KeyValuePair<GameObject, Coroutine> keyValue in delayedReleaseDictionary)
            {
                if (instanceToPrefabDictionary.TryGetValue(keyValue.Key, out GameObject mappedPrefab) && mappedPrefab == prefab)
                    instancesToCancel.Add(keyValue.Key);
            }

            for (int index = 0; index < instancesToCancel.Count; index++)
                CancelDelayedRelease(instancesToCancel[index]);
        }

        /// <summary>
        /// 지정한 프리팹에 연결된 인스턴스 등록 정보를 제거합니다.
        /// </summary>
        /// <param name="prefab">등록 정보를 제거할 프리팹입니다.</param>
        private void RemovePrefabInstances(GameObject prefab)
        {
            List<GameObject> instancesToRemove = new();
            foreach (KeyValuePair<GameObject, GameObject> keyValue in instanceToPrefabDictionary)
            {
                if (keyValue.Value == prefab)
                    instancesToRemove.Add(keyValue.Key);
            }

            for (int index = 0; index < instancesToRemove.Count; index++)
                instanceToPrefabDictionary.Remove(instancesToRemove[index]);
        }

        /// <summary>
        /// 지연 반환을 처리합니다.
        /// </summary>
        /// <param name="instance">반환할 오브젝트입니다.</param>
        /// <param name="delay">지연 시간(초)입니다.</param>
        /// <returns>IEnumerator입니다.</returns>
        private IEnumerator DelayedReleaseRoutine(GameObject instance, float delay)
        {
            yield return GetWait(delay);

            delayedReleaseDictionary.Remove(instance);
            Release(instance);
        }

        /// <summary>
        /// 캐시된 WaitForSeconds를 반환합니다. 없으면 생성하여 캐싱합니다.
        /// </summary>
        /// <param name="seconds">대기 시간(초)입니다.</param>
        /// <returns>캐시된 WaitForSeconds 인스턴스입니다.</returns>
        private WaitForSeconds GetWait(float seconds)
        {
            if (!waitCacheDictionary.TryGetValue(seconds, out WaitForSeconds waitForSeconds))
            {
                waitForSeconds = new WaitForSeconds(seconds);
                waitCacheDictionary[seconds] = waitForSeconds;
            }

            return waitForSeconds;
        }

        /// <summary>
        /// 프리팹에 대한 ObjectPool을 가져오거나 생성합니다.
        /// </summary>
        /// <param name="prefab">풀링할 프리팹입니다.</param>
        /// <returns>프리팹에 매핑된 ObjectPool입니다.</returns>
        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> existingPool))
                return existingPool;

            GameObject capturedPrefab = prefab;

            ObjectPool<GameObject> pool = new(
                createFunc: () => CreatePooled(capturedPrefab),
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPooled,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );

            poolDictionary[prefab] = pool;
            SWUtilsLog.Log($"[SWPool] 풀 생성 완료: {prefab.name}");
            return pool;
        }

        /// <summary>
        /// 새 인스턴스를 생성하고 등록합니다.
        /// </summary>
        /// <param name="prefab">생성할 원본 프리팹입니다.</param>
        /// <returns>생성된 인스턴스입니다.</returns>
        private GameObject CreatePooled(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = prefab.name;
            instanceToPrefabDictionary[instance] = prefab;
            return instance;
        }

        /// <summary>
        /// 풀링 대상 컴포넌트에 풀 참조를 전달하고 Spawn 콜백을 호출합니다.
        /// </summary>
        /// <param name="instance">풀에서 꺼낸 오브젝트입니다.</param>
        private void NotifyPoolables(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int index = 0; index < poolables.Length; index++)
            {
                poolables[index].SetPool(this);
                poolables[index].OnSpawnFromPool();
            }
        }

        /// <summary>
        /// ObjectPool의 Get 호출 때 자동으로 실행됩니다.
        /// </summary>
        /// <param name="instance">꺼내진 오브젝트입니다.</param>
        private void OnGetFromPool(GameObject instance)
        {
            instance.SetActive(true);
        }

        /// <summary>
        /// ObjectPool의 Release 호출 때 자동으로 실행됩니다.
        /// </summary>
        /// <param name="instance">반환되는 오브젝트입니다.</param>
        private void OnReleaseToPool(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int index = 0; index < poolables.Length; index++)
                poolables[index].OnReturnToPool();

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
        }

        /// <summary>
        /// ObjectPool의 최대 크기 초과 때 자동으로 실행됩니다.
        /// </summary>
        /// <param name="instance">파괴할 오브젝트입니다.</param>
        private void OnDestroyPooled(GameObject instance)
        {
            if (instance == null) return;

            instanceToPrefabDictionary.Remove(instance);
            Destroy(instance);
        }
        #endregion // 내부
    }
}
