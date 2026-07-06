using UnityEngine;

using SW.Base;

namespace SW.Util
{
    /// <summary>
    /// 현재 씬에서만 유지되는 싱글톤 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 애플리케이션 종료 중에는 새 인스턴스를 만들지 않으며, 씬이 전환되면 파괴됩니다.
    /// </remarks>
    /// <typeparam name="T">싱글톤으로 관리할 컴포넌트 타입입니다.</typeparam>
    public class SWSingletonScene<T> : SWMonoBehaviour where T : Component
    {
        #region 변수
        /// <summary>싱글톤 인스턴스.</summary>
        private static T instance;
        #endregion // 변수

        #region 프로퍼티
        /// <summary>
        /// 싱글톤 인스턴스에 접근합니다. 없으면 자동으로 생성합니다.
        /// 앱 종료 중에는 새로 생성하지 않고 null을 반환할 수 있습니다.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance != null) return instance;

                if (SWSingletonGuard.IsQuitting)
                {
                    SWLog.LogWarning($"[SWSingletonScene] 앱 종료 중이라 인스턴스를 생성하지 않습니다: {typeof(T).Name}");
                    return null;
                }

                CreateInstance();
                return instance;
            }
        }

        /// <summary>
        /// 인스턴스가 이미 존재하는지 여부. 생성 없이 확인만 할 때 사용합니다.
        /// </summary>
        public static bool HasInstance => instance != null;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 씬에서 인스턴스를 찾거나 새로 생성합니다.
        /// </summary>
        private static void CreateInstance()
        {
            instance = FindAnyObjectByType<T>();
            if (instance == null)
            {
                var gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
            }

            RegisterGuard();
        }

        /// <summary>
        /// Domain Reload 비활성화 대응을 위해 static 리셋 액션을 가드에 등록합니다.
        /// </summary>
        private static void RegisterGuard()
        {
            SWSingletonGuard.RegisterReset(ResetStaticState);
        }

        /// <summary>
        /// 플레이 진입 시 정적 인스턴스 참조를 초기화합니다.
        /// </summary>
        private static void ResetStaticState()
        {
            instance = null;
        }

        /// <summary>
        /// Awake 시 인스턴스를 등록하거나 중복을 제거합니다.
        /// </summary>
        public virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                RegisterGuard();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 파괴 시 인스턴스 참조를 해제합니다.
        /// </summary>
        public virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
        #endregion // 함수
    }
}
