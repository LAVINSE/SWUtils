using UnityEngine;

namespace SWPooling
{
    /// <summary>
    /// 프리팹 기반 오브젝트 풀의 공통 기능을 정의하는 인터페이스입니다.
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// 지정 프리팹을 미리 생성해둔다.
        /// </summary>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="count">미리 생성할 개수</param>
        public void Prewarm(GameObject prefab, int count);

        /// <summary>
        /// 이름으로 지정 프리팹을 미리 생성해둔다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름</param>
        /// <param name="count">미리 생성할 개수</param>
        public void Prewarm(string poolName, int count);

        /// <summary>
        /// 풀에서 오브젝트를 꺼낸다.
        /// </summary>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트</returns>
        public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// 이름으로 풀에서 오브젝트를 꺼낸다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트</returns>
        public GameObject Spawn(string poolName, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// 풀에서 오브젝트를 꺼낸다 (제네릭).
        /// </summary>
        /// <typeparam name="T">반환받을 컴포넌트 타입</typeparam>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트의 T 컴포넌트</returns>
        public T Spawn<T>(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

        /// <summary>
        /// 이름으로 풀에서 오브젝트를 꺼낸다 (제네릭).
        /// </summary>
        /// <typeparam name="T">반환받을 컴포넌트 타입</typeparam>
        /// <param name="poolName">등록된 풀 이름</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트의 T 컴포넌트</returns>
        public T Spawn<T>(string poolName, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

        /// <summary>
        /// 그룹에서 선택한 프리팹으로 오브젝트를 꺼낸다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름</param>
        /// <param name="selectionMode">그룹 프리팹 선택 방식</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트</returns>
        public GameObject SpawnFromGroup(string groupName, SWPoolGroupSelectionMode selectionMode = SWPoolGroupSelectionMode.Random,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// 그룹 안에서 이름이 일치하는 프리팹으로 오브젝트를 꺼낸다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름</param>
        /// <param name="poolName">등록된 풀 이름</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트</returns>
        public GameObject SpawnFromGroup(string groupName, string poolName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// 그룹에서 선택한 프리팹으로 오브젝트를 꺼낸다 (제네릭).
        /// </summary>
        /// <typeparam name="T">반환받을 컴포넌트 타입</typeparam>
        /// <param name="groupName">등록된 그룹 이름</param>
        /// <param name="selectionMode">그룹 프리팹 선택 방식</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트의 T 컴포넌트</returns>
        public T SpawnFromGroup<T>(string groupName, SWPoolGroupSelectionMode selectionMode = SWPoolGroupSelectionMode.Random,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

        /// <summary>
        /// 그룹 안에서 이름이 일치하는 프리팹으로 오브젝트를 꺼낸다 (제네릭).
        /// </summary>
        /// <typeparam name="T">반환받을 컴포넌트 타입</typeparam>
        /// <param name="groupName">등록된 그룹 이름</param>
        /// <param name="poolName">등록된 풀 이름</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트의 T 컴포넌트</returns>
        public T SpawnFromGroup<T>(string groupName, string poolName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

        /// <summary>
        /// 오브젝트를 풀로 반환한다.
        /// </summary>
        /// <param name="instance">반납할 오브젝트</param>
        public void Release(GameObject instance);

        /// <summary>
        /// 일정 시간 후 오브젝트를 풀로 반환한다.
        /// </summary>
        /// <param name="instance">반납할 오브젝트</param>
        /// <param name="delay">지연 시간(초)</param>
        public void Release(GameObject instance, float delay);

        /// <summary>
        /// 특정 프리팹의 풀을 비운다.
        /// </summary>
        /// <param name="prefab">비울 대상 프리팹</param>
        public void Clear(GameObject prefab);

        /// <summary>
        /// 모든 풀을 비운다.
        /// </summary>
        public void ClearAll();

        /// <summary>
        /// 특정 프리팹의 현재 대기 중 오브젝트 수를 반환한다.
        /// </summary>
        /// <param name="prefab">대상 프리팹</param>
        /// <returns>대기 중 오브젝트 수</returns>
        public int CountInPool(GameObject prefab);

        /// <summary>
        /// 이름으로 특정 프리팹의 현재 대기 중 오브젝트 수를 반환한다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름</param>
        /// <returns>대기 중 오브젝트 수</returns>
        public int CountInPool(string poolName);
    }
}
