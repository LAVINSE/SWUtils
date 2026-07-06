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
            set => allowSceneActivation = value;
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
        /// <inheritdoc/>
        /// <inheritdoc />
        public override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            CancelCurrentLoad();
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
        /// 현재 진행 중인 로딩 코루틴을 취소합니다.
        /// </summary>
        public void CancelCurrentLoad()
        {
            if (loadingRoutine != null)
            {
                StopCoroutine(loadingRoutine);
                SWLog.LogWarning($"[SWSceneLoader] Load canceled: {LoadingSceneName}");
            }

            ResetLoadingState();
        }
        #endregion // 로드

        #region 내부
        /// <summary>
        /// 씬 이름 기반 로딩을 시작합니다.
        /// </summary>
        private void StartLoading(string sceneName, LoadSceneMode mode,
            Action<float> onProgress, Action onComplete)
        {
            if (IsLoading)
            {
                SWLog.LogWarning($"[SWSceneLoader] Already loading: {LoadingSceneName}");
                return;
            }

            loadingRoutine = StartCoroutine(LoadSceneRoutine(sceneName, mode, onProgress, onComplete));
        }

        /// <summary>
        /// 빌드 인덱스 기반 로딩을 시작합니다.
        /// </summary>
        private void StartLoading(int sceneBuildIndex, LoadSceneMode mode,
            Action<float> onProgress, Action onComplete)
        {
            if (IsLoading)
            {
                SWLog.LogWarning($"[SWSceneLoader] Already loading: {LoadingSceneName}");
                return;
            }

            loadingRoutine = StartCoroutine(LoadSceneRoutine(sceneBuildIndex, mode, onProgress, onComplete));
        }

        /// <summary>
        /// 씬 언로드를 시작합니다.
        /// </summary>
        private void StartUnloading(string sceneName, Action<float> onProgress, Action onComplete)
        {
            if (IsLoading)
            {
                SWLog.LogWarning($"[SWSceneLoader] Already loading: {LoadingSceneName}");
                return;
            }

            loadingRoutine = StartCoroutine(UnloadSceneRoutine(sceneName, onProgress, onComplete));
        }

        /// <summary>
        /// 씬 이름 기반 로딩 코루틴.
        /// </summary>
        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode,
            Action<float> onProgress, Action onComplete)
        {
            BeginLoad(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                FailLoad(sceneName);
                yield break;
            }

            yield return TrackOperation(sceneName, operation, onProgress);
            CompleteLoad(sceneName, onComplete);
        }

        /// <summary>
        /// 빌드 인덱스 기반 로딩 코루틴.
        /// </summary>
        private IEnumerator LoadSceneRoutine(int sceneBuildIndex, LoadSceneMode mode,
            Action<float> onProgress, Action onComplete)
        {
            string sceneLabel = sceneBuildIndex.ToString();
            BeginLoad(sceneLabel);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneBuildIndex, mode);
            if (operation == null)
            {
                FailLoad(sceneLabel);
                yield break;
            }

            yield return TrackOperation(sceneLabel, operation, onProgress);
            CompleteLoad(sceneLabel, onComplete);
        }

        /// <summary>
        /// 씬 언로드 코루틴.
        /// </summary>
        private IEnumerator UnloadSceneRoutine(string sceneName, Action<float> onProgress, Action onComplete)
        {
            BeginLoad(sceneName);

            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
            {
                FailLoad(sceneName);
                yield break;
            }

            yield return TrackOperation(sceneName, operation, onProgress);
            CompleteLoad(sceneName, onComplete);
        }

        /// <summary>
        /// AsyncOperation 진행률을 추적합니다.
        /// </summary>
        private IEnumerator TrackOperation(string sceneName, AsyncOperation operation, Action<float> onProgress)
        {
            operation.allowSceneActivation = allowSceneActivation;

            while (!operation.isDone)
            {
                operation.allowSceneActivation = allowSceneActivation;
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(Progress);
                LoadProgressChanged?.Invoke(sceneName, Progress);
                yield return null;
            }

            Progress = 1f;
            onProgress?.Invoke(Progress);
            LoadProgressChanged?.Invoke(sceneName, Progress);
        }

        /// <summary>
        /// 로딩 상태를 시작 상태로 설정합니다.
        /// </summary>
        private void BeginLoad(string sceneName)
        {
            IsLoading = true;
            LoadingSceneName = sceneName;
            Progress = 0f;
            LoadStarted?.Invoke(sceneName);
            SWLog.Log($"[SWSceneLoader] Load started: {sceneName}");
        }

        /// <summary>
        /// 로딩 완료 처리를 수행합니다.
        /// </summary>
        private void CompleteLoad(string sceneName, Action onComplete)
        {
            onComplete?.Invoke();
            LoadCompleted?.Invoke(sceneName);
            SWLog.Log($"[SWSceneLoader] Load completed: {sceneName}");
            ResetLoadingState();
        }

        /// <summary>
        /// 로딩 실패 처리를 수행합니다.
        /// </summary>
        private void FailLoad(string sceneName)
        {
            SWLog.LogWarning($"[SWSceneLoader] Load failed: {sceneName}");
            LoadFailed?.Invoke(sceneName);
            ResetLoadingState();
        }

        /// <summary>
        /// 로딩 상태 값을 초기화합니다.
        /// </summary>
        private void ResetLoadingState()
        {
            IsLoading = false;
            LoadingSceneName = string.Empty;
            Progress = 0f;
            loadingRoutine = null;
        }
        #endregion // 내부
    }
}
