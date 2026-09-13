namespace SW.Pooling
{
    /// <summary>
    /// 풀에서 생성, 재사용, 반납되는 오브젝트가 구현하는 생명주기 인터페이스입니다.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 자신을 관리하는 풀의 참조를 설정합니다. Spawn 시 자동으로 호출됩니다.
        /// </summary>
        /// <param name="pool">자신을 관리하는 풀</param>
        public void SetPool(IPool pool);

        /// <summary>
        /// 부모, 위치와 풀 참조를 설정한 뒤 활성화 전에 호출합니다. 재사용 상태를 초기화합니다.
        /// 미리 생성하는 과정에서는 호출하지 않습니다.
        /// </summary>
        public void OnSpawnFromPool();

        /// <summary>
        /// 풀로 반납되기 전 호출됩니다. 리소스 정리에 사용합니다.
        /// </summary>
        public void OnReturnToPool();
    }
}
