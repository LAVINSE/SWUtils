using System;
using UnityEngine;

using SW.Base;

namespace SW.Util
{
    /// <summary>
    /// 싱글톤의 앱 종료 감지와 Domain Reload 비활성화 대응을 담당하는 내부 가드입니다.
    /// </summary>
    /// <remarks>
    /// 제네릭 클래스(와 그 중첩 타입)에는 <see cref="RuntimeInitializeOnLoadMethodAttribute"/>가
    /// 호출되지 않으므로 비제네릭 클래스가 반드시 필요합니다.
    /// 별도 파일 대신 SWSingleton과 같은 파일에 두어 싱글톤 관련 코드를 한 곳에서 관리합니다.
    /// </remarks>
    internal static class SWSingletonGuard
    {
        #region 필드
        private static event Action resetActions;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>앱이 종료 중인지 여부입니다. 종료 중에는 싱글톤 인스턴스를 새로 만들지 않습니다.</summary>
        internal static bool IsQuitting { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 플레이 진입 시 정적 상태를 초기화합니다. Domain Reload가 꺼져 있어도 항상 호출됩니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            IsQuitting = false;

            resetActions?.Invoke();
            resetActions = null;

            Application.quitting -= HandleQuitting;
            Application.quitting += HandleQuitting;
        }

        /// <summary>
        /// 플레이 진입 시 실행할 static 리셋 액션을 등록합니다. 중복 등록은 무시됩니다.
        /// </summary>
        /// <param name="resetAction">다음 플레이 진입 때 실행할 리셋 액션입니다.</param>
        internal static void RegisterReset(Action resetAction)
        {
            if (resetAction == null) return;

            resetActions -= resetAction;
            resetActions += resetAction;
        }

        /// <summary>
        /// 앱 종료 시 호출되어 종료 상태를 표시합니다.
        /// </summary>
        private static void HandleQuitting()
        {
            IsQuitting = true;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 씬 전환 후에도 유지되는 전역 싱글톤 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 애플리케이션 종료 중에는 새 인스턴스를 만들지 않으며, 도메인 다시 불러오기가
    /// 비활성화된 환경에서도 플레이 진입 시 정적 상태를 초기화합니다.
    /// </remarks>
    /// <typeparam name="T">싱글톤으로 관리할 컴포넌트 타입입니다.</typeparam>
    public class SWSingleton<T> : SWMonoBehaviour where T : Component
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
                    SWLog.LogWarning($"[SWSingleton] 앱 종료 중이라 인스턴스를 생성하지 않습니다: {typeof(T).Name}");
                    return null;
                }

                SetupInstance();
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
        private static void SetupInstance()
        {
            instance = FindAnyObjectByType<T>();

            if (instance == null)
            {
                GameObject gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
                DontDestroyOnLoad(gameObject);
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
        /// Awake 시 중복 인스턴스를 제거합니다.
        /// </summary>
        public virtual void Awake()
        {
            RemoveDuplicates();
        }

        /// <summary>
        /// 파괴 시 인스턴스 참조를 해제합니다.
        /// </summary>
        public virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 중복 인스턴스를 검사하여 제거합니다.
        /// 이미 인스턴스가 존재하면 자신을 파괴합니다.
        /// </summary>
        private void RemoveDuplicates()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
                RegisterGuard();
            }
            else if (instance == this)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion // 함수
    }
}
