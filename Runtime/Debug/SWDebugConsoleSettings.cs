using UnityEngine;

namespace SW.Debugging
{
    /// <summary>
    /// 디버그 콘솔을 여는 키보드 키입니다.
    /// </summary>
    public enum SWConsoleOpenKey
    {
        /// <summary>키보드로 열지 않습니다.</summary>
        None = 0,
        /// <summary>백쿼트(`) 키입니다.</summary>
        BackQuote,
        /// <summary>Tab 키입니다.</summary>
        Tab,
        F1, F2, F3, F4, F5, F6,
        F7, F8, F9, F10, F11, F12,
    }

    /// <summary>
    /// 성능 오버레이를 표시할 화면 모서리입니다.
    /// </summary>
    public enum SWOverlayAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    /// <summary>
    /// SWDebugConsole과 성능 오버레이의 동작 설정입니다.
    /// </summary>
    /// <remarks>
    /// Resources 폴더의 <c>SWDebugConsoleSettings</c> 에셋을 자동으로 불러옵니다.
    /// 에셋이 없으면 기본값 인스턴스로 동작하므로 별도 배치 없이도 콘솔이 동작합니다.
    /// 에셋은 <c>SWTools &gt; Debug &gt; Debug Console Settings</c> 창에서 생성하고 편집합니다.
    /// </remarks>
    public class SWDebugConsoleSettings : ScriptableObject
    {
        #region 상수
        /// <summary>Resources.Load에 사용하는 에셋 이름입니다.</summary>
        public const string ResourceName = "SWDebugConsoleSettings";
        #endregion // 상수

        #region 필드
        [Header("=====> 콘솔 열기 <=====")]
        [Tooltip("씬 로드 후 콘솔 오브젝트를 자동으로 생성할지 여부입니다.")]
        [SerializeField] private bool autoCreateOnLoad = true;

        [Tooltip("콘솔을 여닫는 키보드 키입니다. None이면 키보드로 열지 않습니다.")]
        [SerializeField] private SWConsoleOpenKey openKey = SWConsoleOpenKey.BackQuote;

        [Tooltip("콘솔 열기 키와 함께 Control 키를 눌러야 하는지 여부입니다.")]
        [SerializeField] private bool requireControlKey;

        [Tooltip("콘솔 열기 키와 함께 Shift 키를 눌러야 하는지 여부입니다.")]
        [SerializeField] private bool requireShiftKey;

        [Tooltip("콘솔 열기 키와 함께 Alt 키를 눌러야 하는지 여부입니다.")]
        [SerializeField] private bool requireAltKey;

        [Tooltip("모바일에서 콘솔을 여닫는 동시 터치 손가락 개수입니다.")]
        [SerializeField, Range(2, 5)] private int touchCountToOpen = 3;

        [Tooltip("Input System 패키지가 설치된 프로젝트에서 Input System 입력을 먼저 확인합니다. 패키지가 없으면 기본 입력으로 처리합니다.")]
        [SerializeField] private bool enableInputSystem = true;

        [Header("=====> 성능 오버레이 <=====")]
        [Tooltip("씬 로드 후 성능 오버레이를 자동으로 표시할지 여부입니다.")]
        [SerializeField] private bool overlayEnabledOnStart;

        [Tooltip("오버레이를 표시할 화면 모서리입니다.")]
        [SerializeField] private SWOverlayAnchor overlayAnchor = SWOverlayAnchor.TopRight;

        [Tooltip("오버레이 글자 크기 배율입니다.")]
        [SerializeField, Range(0.5f, 3f)] private float overlayScale = 1f;

        [Tooltip("오버레이 표시값 갱신 간격(초)입니다.")]
        [SerializeField, Range(0.1f, 2f)] private float overlayUpdateInterval = 0.5f;

        [Tooltip("FPS와 프레임 시간을 표시합니다.")]
        [SerializeField] private bool showFps = true;

        [Tooltip("갱신 구간의 최소/최대 FPS 기록을 표시합니다.")]
        [SerializeField] private bool showMinMax = true;

        [Tooltip("Mono 힙과 총 할당 메모리를 표시합니다.")]
        [SerializeField] private bool showMemory = true;

        [Header("=====> FPS 경고 색상 <=====")]
        [Tooltip("이 값 미만이면 오버레이가 노란색으로 표시됩니다.")]
        [SerializeField] private int fpsWarningThreshold = 45;

        [Tooltip("이 값 미만이면 오버레이가 빨간색으로 표시됩니다.")]
        [SerializeField] private int fpsDangerThreshold = 25;

        private static SWDebugConsoleSettings cached;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>씬 로드 후 콘솔 오브젝트를 자동으로 생성할지 여부입니다.</summary>
        public bool AutoCreateOnLoad => autoCreateOnLoad;

        /// <summary>콘솔을 여닫는 키보드 키입니다.</summary>
        public SWConsoleOpenKey OpenKey => openKey;

        /// <summary>콘솔 열기 키와 함께 Control 키를 눌러야 하는지 여부입니다.</summary>
        public bool RequireControlKey => requireControlKey;

        /// <summary>콘솔 열기 키와 함께 Shift 키를 눌러야 하는지 여부입니다.</summary>
        public bool RequireShiftKey => requireShiftKey;

        /// <summary>콘솔 열기 키와 함께 Alt 키를 눌러야 하는지 여부입니다.</summary>
        public bool RequireAltKey => requireAltKey;

        /// <summary>모바일에서 콘솔을 여닫는 동시 터치 손가락 개수입니다.</summary>
        public int TouchCountToOpen => touchCountToOpen;

        /// <summary>Input System 입력을 우선 확인할지 여부입니다.</summary>
        public bool EnableInputSystem => enableInputSystem;

        /// <summary>씬 로드 후 성능 오버레이를 자동으로 표시할지 여부입니다.</summary>
        public bool OverlayEnabledOnStart => overlayEnabledOnStart;

        /// <summary>오버레이를 표시할 화면 모서리입니다.</summary>
        public SWOverlayAnchor OverlayAnchor => overlayAnchor;

        /// <summary>오버레이 글자 크기 배율입니다.</summary>
        public float OverlayScale => overlayScale;

        /// <summary>오버레이 표시값 갱신 간격(초)입니다.</summary>
        public float OverlayUpdateInterval => overlayUpdateInterval;

        /// <summary>FPS와 프레임 시간을 표시할지 여부입니다.</summary>
        public bool ShowFps => showFps;

        /// <summary>최소/최대 FPS 기록을 표시할지 여부입니다.</summary>
        public bool ShowMinMax => showMinMax;

        /// <summary>메모리 사용량을 표시할지 여부입니다.</summary>
        public bool ShowMemory => showMemory;

        /// <summary>이 값 미만이면 노란색으로 표시하는 FPS 경계입니다.</summary>
        public int FpsWarningThreshold => fpsWarningThreshold;

        /// <summary>이 값 미만이면 빨간색으로 표시하는 FPS 경계입니다.</summary>
        public int FpsDangerThreshold => fpsDangerThreshold;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 플레이 진입 시 정적 캐시를 초기화합니다. Domain Reload가 꺼져 있어도 항상 호출됩니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            cached = null;
        }
        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 설정 에셋을 불러옵니다. Resources에 에셋이 없으면 기본값 인스턴스를 반환합니다.
        /// </summary>
        /// <returns>설정 인스턴스입니다. null을 반환하지 않습니다.</returns>
        public static SWDebugConsoleSettings Load()
        {
            if (cached != null) return cached;

            cached = Resources.Load<SWDebugConsoleSettings>(ResourceName);
            if (cached == null)
            {
                cached = CreateInstance<SWDebugConsoleSettings>();
                cached.hideFlags = HideFlags.HideAndDontSave;
            }

            return cached;
        }

        /// <summary>
        /// 열기 키에 대응하는 Input System Keyboard 프로퍼티 이름을 반환합니다.
        /// </summary>
        /// <returns>Keyboard 프로퍼티 이름입니다. 대응하지 않으면 빈 문자열을 반환합니다.</returns>
        public string GetInputSystemKeyPropertyName()
        {
            return openKey switch
            {
                SWConsoleOpenKey.BackQuote => "backquoteKey",
                SWConsoleOpenKey.Tab => "tabKey",
                SWConsoleOpenKey.F1 => "f1Key",
                SWConsoleOpenKey.F2 => "f2Key",
                SWConsoleOpenKey.F3 => "f3Key",
                SWConsoleOpenKey.F4 => "f4Key",
                SWConsoleOpenKey.F5 => "f5Key",
                SWConsoleOpenKey.F6 => "f6Key",
                SWConsoleOpenKey.F7 => "f7Key",
                SWConsoleOpenKey.F8 => "f8Key",
                SWConsoleOpenKey.F9 => "f9Key",
                SWConsoleOpenKey.F10 => "f10Key",
                SWConsoleOpenKey.F11 => "f11Key",
                SWConsoleOpenKey.F12 => "f12Key",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 열기 키를 레거시 Input의 KeyCode 값으로 변환합니다.
        /// </summary>
        /// <returns>대응하는 KeyCode입니다. None이면 KeyCode.None을 반환합니다.</returns>
        public KeyCode GetLegacyKeyCode()
        {
            return openKey switch
            {
                SWConsoleOpenKey.BackQuote => KeyCode.BackQuote,
                SWConsoleOpenKey.Tab => KeyCode.Tab,
                SWConsoleOpenKey.F1 => KeyCode.F1,
                SWConsoleOpenKey.F2 => KeyCode.F2,
                SWConsoleOpenKey.F3 => KeyCode.F3,
                SWConsoleOpenKey.F4 => KeyCode.F4,
                SWConsoleOpenKey.F5 => KeyCode.F5,
                SWConsoleOpenKey.F6 => KeyCode.F6,
                SWConsoleOpenKey.F7 => KeyCode.F7,
                SWConsoleOpenKey.F8 => KeyCode.F8,
                SWConsoleOpenKey.F9 => KeyCode.F9,
                SWConsoleOpenKey.F10 => KeyCode.F10,
                SWConsoleOpenKey.F11 => KeyCode.F11,
                SWConsoleOpenKey.F12 => KeyCode.F12,
                _ => KeyCode.None,
            };
        }
        #endregion // 함수
    }
}
