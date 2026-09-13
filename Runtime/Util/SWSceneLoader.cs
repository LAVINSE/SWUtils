using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SW.Util
{
    /// <summary>
    /// 씬 로드/언로드, 진행률 콜백, 중복 로딩 방지를 처리하는 씬 로더.
    /// SWSceneLoader.Instance로 전역 접근하거나 씬에 직접 배치해서 사용합니다.
    /// </summary>
    public class SWSceneLoader : SWSingleton<SWSceneLoader>
    {
        #region 필드
        [Header("=====> 설정 <=====")]
        [SerializeField] private bool allowSceneActivation = true;

        private Coroutine loadingRoutine;
        private AsyncOperation activeOperation;
        private int requestVersion;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 씬 로딩 중인지 여부.</summary>
        public bool IsLoading { get; private set; }
        /// <summary>현재 로딩 중인 씬 이름 또는 빌드 인덱스 문자열.</summary>
        public string LoadingSceneName { get; private set; }
        /// <summary>현재 로딩 진행률. 0~1 사이 값.</summary>
        public float Progress { get; private set; }

        /// <summary>씬 활성화 허용 여부.</summary>
        public bool AllowSceneActivation
        {
            get => allowSceneActivation;
            set
            {
                allowSceneActivation = value;
                if (activeOperation != null) activeOperation.allowSceneActivation = value;
            }
        }
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>씬 로드 시작 이벤트.</summary>
        public event Action<string> LoadStarted;
        /// <summary>씬 로드 진행률 변경 이벤트.</summary>
        public event Action<string, float> LoadProgressChanged;
        /// <summary>씬 로드 완료 이벤트.</summary>
        public event Action<string> LoadCompleted;
        /// <summary>씬 로드 실패 이벤트.</summary>
        public event Action<string> LoadFailed;
        #endregion // 이벤트

        #region 초기화
        /// <inheritdoc />
        public override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            ReleaseOperation();
            base.OnDestroy();
        }
        #endregion // 초기화

        #region 로드
        /// <summary>
        /// 씬 이름으로 씬을 로드합니다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름.</param>
        /// <param name="mode">씬 로드 방식.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null, Action onComplete = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                SWLog.LogWarning("[SWSceneLoader] LoadScene failed. Scene name is empty.");
                return;
            }

            StartLoading(sceneName, mode, onProgress, onComplete);
        }

        /// <summary>
        /// 빌드 인덱스로 씬을 로드합니다.
        /// </summary>
        /// <param name="sceneBuildIndex">로드할 씬 빌드 인덱스.</param>
        /// <param name="mode">씬 로드 방식.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void LoadScene(int sceneBuildIndex, LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null, Action onComplete = null)
        {
            if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                SWLog.LogWarning($"[SWSceneLoader] LoadScene failed. Invalid build index: {sceneBuildIndex}");
                return;
            }

            StartLoading(sceneBuildIndex, mode, onProgress, onComplete);
        }

        /// <summary>
        /// 씬을 Additive 방식으로 로드합니다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void LoadAdditive(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            LoadScene(sceneName, LoadSceneMode.Additive, onProgress, onComplete);
        }

        /// <summary>
        /// 현재 활성 씬을 다시 로드합니다.
        /// </summary>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void ReloadActiveScene(Action<float> onProgress = null, Action onComplete = null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            LoadScene(activeScene.name, LoadSceneMode.Single, onProgress, onComplete);
        }

        /// <summary>
        /// 로드된 씬을 언로드합니다.
        /// </summary>
        /// <param name="sceneName">언로드할 씬 이름.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void UnloadScene(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                SWLog.LogWarning("[SWSceneLoader] UnloadScene failed. Scene name is empty.");
                return;
            }

            StartUnloading(sceneName, onProgress, onComplete);
        }

        /// <summary>
        /// 로드되어 있는 씬을 활성 씬으로 설정합니다.
        /// </summary>
        /// <param name="sceneName">활성화할 씬 이름.</param>
        public void SetActiveScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
                SWLog.Log($"[SWSceneLoader] Set active scene: {sceneName}");
                return;
            }

            SWLog.LogWarning($"[SWSceneLoader] SetActiveScene failed. Scene is not loaded: {sceneName}");
        }

        /// <summary>
        /// 엔진 작업이 시작되기 전의 요청을 취소합니다. 이미 시작한 작업은 유지하고 경고를 남깁니다.
        /// </summary>
        public void CancelCurrentLoad()
        {
            if (!TryCancelCurrentLoad())
                SWLog.LogWarning("[SWSceneLoader] 엔진에서 진행 중인 씬 작업은 취소할 수 없습니다.");
        }

        /// <summary>대기 요청을 취소합니다. 엔진 작업이 시작되었으면 false를 반환합니다.</summary>
        public bool TryCancelCurrentLoad()
        {
            if (activeOperation != null) return false;
            if (loadingRoutine != null) StopCoroutine(loadingRoutine);
            ResetLoadingState();
            return true;
        }
        #endregion // 로드

        #region 내부
        /// <summary>씬 이름으로 엔진 작업을 예약합니다.</summary>
        private void StartLoading(string sceneName, LoadSceneMode mode, Action<float> onProgress, Action onComplete)
            => StartOperation(sceneName, () => SceneManager.LoadSceneAsync(sceneName, mode), onProgress, onComplete);

        /// <summary>빌드 번호로 엔진 작업을 예약합니다.</summary>
        private void StartLoading(int sceneBuildIndex, LoadSceneMode mode, Action<float> onProgress, Action onComplete)
            => StartOperation(sceneBuildIndex.ToString(), () => SceneManager.LoadSceneAsync(sceneBuildIndex, mode), onProgress, onComplete);

        /// <summary>씬 언로드를 예약합니다.</summary>
        private void StartUnloading(string sceneName, Action<float> onProgress, Action onComplete)
            => StartOperation(sceneName, () => SceneManager.UnloadSceneAsync(sceneName), onProgress, onComplete);

        /// <summary>다음 프레임에 시작할 작업을 등록합니다. 엔진 작업 시작 전까지 취소할 수 있습니다.</summary>
        private void StartOperation(string sceneName, Func<AsyncOperation> createOperation,
            Action<float> onProgress, Action onComplete)
        {
            if (IsLoading || !isActiveAndEnabled)
            {
                SWLog.LogWarning("[SWSceneLoader] 로더가 비활성 상태이거나 다른 작업이 진행 중입니다.");
                return;
            }
            IsLoading = true;
            LoadingSceneName = sceneName;
            Progress = 0f;
            int version = ++requestVersion;
            loadingRoutine = StartCoroutine(RunOperation(sceneName, createOperation, onProgress, onComplete, version));
        }

        /// <summary>작업 생성 실패와 외부 알림 예외를 분리하고 완료 전까지 엔진 작업을 소유합니다.</summary>
        private IEnumerator RunOperation(string sceneName, Func<AsyncOperation> createOperation,
            Action<float> onProgress, Action onComplete, int version)
        {
            yield return null;
            if (version != requestVersion) yield break;
            try
            {
                activeOperation = createOperation();
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSceneLoader] 씬 작업 시작 실패: {sceneName}, {exception.Message}");
            }
            if (activeOperation == null)
            {
                ResetLoadingState();
                SWSafeEvent.Invoke(LoadFailed, handler => handler(sceneName));
                yield break;
            }
            AsyncOperation operation = activeOperation;
            operation.allowSceneActivation = allowSceneActivation;
            SWSafeEvent.Invoke(LoadStarted, handler => handler(sceneName));
            while (!operation.isDone)
            {
                if (version != requestVersion) yield break;
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                SWSafeEvent.Invoke(onProgress, handler => handler(Progress));
                SWSafeEvent.Invoke(LoadProgressChanged, handler => handler(sceneName, Progress));
                yield return null;
            }
            if (version != requestVersion) yield break;
            Progress = 1f;
            SWSafeEvent.Invoke(onProgress, handler => handler(1f));
            SWSafeEvent.Invoke(LoadProgressChanged, handler => handler(sceneName, 1f));
            if (version != requestVersion) yield break;
            ResetLoadingState();
            SWSafeEvent.Invoke(onComplete, handler => handler());
            SWSafeEvent.Invoke(LoadCompleted, handler => handler(sceneName));
        }

        /// <summary>로더를 비활성화해도 엔진 작업이 활성화 대기로 대기열을 막지 않도록 합니다.</summary>
        private void OnDisable() => ReleaseOperation();

        /// <summary>추적을 중단하기 전에 엔진 작업의 활성화 보류를 해제합니다.</summary>
        private void ReleaseOperation()
        {
            if (activeOperation != null && !activeOperation.isDone)
                activeOperation.allowSceneActivation = true;
            if (loadingRoutine != null) StopCoroutine(loadingRoutine);
            ResetLoadingState();
        }

        /// <summary>이전 요청을 무효화하고 다음 요청을 받을 수 있는 상태로 돌립니다.</summary>
        private void ResetLoadingState()
        {
            requestVersion++;
            IsLoading = false;
            LoadingSceneName = string.Empty;
            Progress = 0f;
            loadingRoutine = null;
            activeOperation = null;
        }
        #endregion // 내부
    }
}
