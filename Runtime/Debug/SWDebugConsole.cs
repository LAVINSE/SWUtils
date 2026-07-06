using System.Diagnostics;

#if SW_DEBUG_MODE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#endif

using SW.Attribute;

namespace SW.Debug
{
    /// <summary>
    /// 빌드에서 로그 확인, [SWCommand] 명령 실행, 상태 HUD를 제공하는 인게임 디버그 콘솔입니다.
    /// </summary>
    /// <remarks>
    /// - SW_DEBUG_MODE 심볼이 있을 때만 동작하며, 없으면 이 클래스의 호출 코드 자체가
    ///   Conditional 어트리뷰트로 컴파일에서 제거됩니다. (SWLog와 동일한 방식)
    /// - UI는 IMGUI(OnGUI)로 코드가 직접 그리므로 프리팹, Canvas, 폰트 등 별도 에셋 제작이 필요 없습니다.
    /// - 열기: PC/에디터는 백쿼트(`) 키, 모바일은 3손가락 동시 터치.
    /// - SW_DEBUG_MODE가 정의되면 씬 로드 후 자동으로 생성되므로 별도 배치도 필요 없습니다.
    /// </remarks>
    public static class SWDebugConsole
    {
        #region 프로퍼티
        /// <summary>콘솔이 현재 열려 있는지 여부입니다. 심볼이 없으면 항상 false입니다.</summary>
        public static bool IsOpen
        {
            get
            {
#if SW_DEBUG_MODE
                return SWDebugConsoleBehaviour.HasInstance && SWDebugConsoleBehaviour.Instance.IsOpen;
#else
                return false;
#endif
            }
        }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 콘솔을 엽니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void Show()
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.SetOpen(true);
#endif
        }

        /// <summary>
        /// 콘솔을 닫습니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void Hide()
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.SetOpen(false);
#endif
        }

        /// <summary>
        /// 콘솔 열림 상태를 토글합니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void Toggle()
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.SetOpen(!SWDebugConsoleBehaviour.Instance.IsOpen);
#endif
        }

        /// <summary>
        /// 인스턴스 메서드의 [SWCommand]를 콘솔에 등록합니다. 보통 Awake에서 호출합니다.
        /// </summary>
        /// <param name="target">명령을 가진 오브젝트입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void RegisterInstance(object target)
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.RegisterInstanceCommands(target);
#endif
        }

        /// <summary>
        /// 등록한 인스턴스 명령을 해제합니다. 보통 OnDestroy에서 호출합니다.
        /// </summary>
        /// <param name="target">해제할 오브젝트입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void UnregisterInstance(object target)
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.UnregisterInstanceCommands(target);
#endif
        }

        /// <summary>
        /// 상태 탭에 매 프레임 갱신되는 감시 값을 등록합니다.
        /// <code>SWDebugConsole.Watch("Gold", () => player.Gold.ToString());</code>
        /// </summary>
        /// <param name="name">표시 이름입니다. 같은 이름은 덮어씁니다.</param>
        /// <param name="valueGetter">표시할 값을 반환하는 함수입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void Watch(string name, System.Func<string> valueGetter)
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.AddWatch(name, valueGetter);
#endif
        }

        /// <summary>
        /// 감시 값을 제거합니다.
        /// </summary>
        /// <param name="name">제거할 감시 이름입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void Unwatch(string name)
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.RemoveWatch(name);
#endif
        }

        /// <summary>
        /// 명령 문자열을 코드에서 직접 실행합니다.
        /// <code>SWDebugConsole.Execute("gold 5000");</code>
        /// </summary>
        /// <param name="commandLine">명령과 인자를 포함한 한 줄 문자열입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void Execute(string commandLine)
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.ExecuteCommandLine(commandLine);
#endif
        }
        #endregion // 함수
    }

#if SW_DEBUG_MODE
    /// <summary>
    /// SWDebugConsole의 실제 구현 MonoBehaviour입니다. SW_DEBUG_MODE에서만 컴파일됩니다.
    /// </summary>
    internal sealed class SWDebugConsoleBehaviour : MonoBehaviour
    {
        #region 데이터
        /// <summary>수집된 로그 한 건입니다.</summary>
        private sealed class LogEntry
        {
            /// <summary>로그 메시지입니다.</summary>
            public string message;
            /// <summary>로그가 발생한 호출 스택입니다.</summary>
            public string stackTrace;
            /// <summary>로그의 심각도입니다.</summary>
            public LogType type;
            /// <summary>같은 로그가 반복된 횟수입니다.</summary>
            public int count = 1;
            /// <summary>로그가 마지막으로 발생한 시각입니다.</summary>
            public string timeText;
        }

        /// <summary>등록된 콘솔 명령 한 건입니다.</summary>
        private sealed class CommandInfo
        {
            /// <summary>콘솔에서 입력할 명령 이름입니다.</summary>
            public string name;
            /// <summary>명령 사용 목적입니다.</summary>
            public string description;
            /// <summary>명령을 표시할 그룹입니다.</summary>
            public string group;
            /// <summary>명령이 실행할 메서드입니다.</summary>
            public MethodInfo method;
            /// <summary>인스턴스 메서드를 호출할 대상입니다.</summary>
            public object target;
            /// <summary>명령이 받는 매개변수입니다.</summary>
            public ParameterInfo[] parameters;
            /// <summary>사용자가 입력한 인수 문자열입니다.</summary>
            public string argsInput = string.Empty;

            /// <summary>help와 치트 패널에 표시할 시그니처 문자열입니다.</summary>
            public string Signature
            {
                get
                {
                    if (parameters.Length == 0) return name;

                    StringBuilder builder = new(name);
                    for (int index = 0; index < parameters.Length; index++)
                        builder.Append(' ').Append('<').Append(parameters[index].Name).Append(':')
                            .Append(parameters[index].ParameterType.Name).Append('>');
                    return builder.ToString();
                }
            }
        }

        /// <summary>상태 탭 감시 항목입니다.</summary>
        private sealed class WatchEntry
        {
            /// <summary>감시 항목 이름입니다.</summary>
            public string name;
            /// <summary>현재 표시값을 반환하는 함수입니다.</summary>
            public Func<string> valueGetter;
        }
        #endregion // 데이터

        #region 필드
        private const int MaxLogCount = 500;
        private const int MaxHistoryCount = 30;
        private static readonly string[] TabNames = { "로그", "명령", "상태" };

        private static SWDebugConsoleBehaviour instance;

        private readonly object logLock = new();
        private readonly List<LogEntry> logEntries = new();
        private readonly List<CommandInfo> commands = new();
        private readonly List<WatchEntry> watches = new();
        private readonly List<string> inputHistory = new();

        private bool isOpen;
        private int selectedTab;
        private int selectedLogIndex = -1;
        private int historyIndex = -1;
        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;
        private bool gestureLatched;
        private bool staticCommandsScanned;

        private string searchText = string.Empty;
        private string commandInput = string.Empty;

        private Vector2 logScrollPosition;
        private Vector2 stackScrollPosition;
        private Vector2 commandScrollPosition;
        private Vector2 statusScrollPosition;

        private float smoothedDeltaTime;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>구현 인스턴스입니다.</summary>
        internal static SWDebugConsoleBehaviour Instance => instance;

        /// <summary>인스턴스가 존재하는지 여부입니다.</summary>
        internal static bool HasInstance => instance != null;

        /// <summary>콘솔이 열려 있는지 여부입니다.</summary>
        internal bool IsOpen => isOpen;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 플레이 진입 시 정적 상태를 초기화합니다. Domain Reload가 꺼져 있어도 항상 호출됩니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        /// <summary>
        /// 씬 로드 후 콘솔을 자동 생성합니다. 배치 없이 열기 제스처가 동작하게 합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            EnsureCreated();
        }

        /// <summary>
        /// 콘솔 오브젝트가 없으면 생성합니다.
        /// </summary>
        internal static void EnsureCreated()
        {
            if (instance != null) return;
            if (!Application.isPlaying) return;

            GameObject consoleObject = new("[SWDebugConsole]");
            consoleObject.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(consoleObject);
            instance = consoleObject.AddComponent<SWDebugConsoleBehaviour>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.logMessageReceivedThreaded += HandleLogMessage;
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= HandleLogMessage;
            if (instance == this)
                instance = null;
        }
        #endregion // 초기화

        #region 열기/닫기
        /// <summary>
        /// 콘솔 열림 상태를 설정합니다.
        /// </summary>
        /// <param name="open">열림 여부입니다.</param>
        internal void SetOpen(bool open)
        {
            if (isOpen == open) return;

            isOpen = open;
            if (isOpen)
                ScanStaticCommandsIfNeeded();
        }

        private void Update()
        {
            smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, 0.1f);
            CheckOpenGesture();
        }

        /// <summary>
        /// 백쿼트 키 또는 3손가락 터치로 콘솔을 토글합니다.
        /// </summary>
        private void CheckOpenGesture()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                SetOpen(!isOpen);
                return;
            }

            if (Touchscreen.current != null)
            {
                int touchCount = 0;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed)
                        touchCount++;
                }

                if (touchCount >= 3)
                {
                    if (!gestureLatched)
                    {
                        gestureLatched = true;
                        SetOpen(!isOpen);
                    }
                }
                else
                {
                    gestureLatched = false;
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                SetOpen(!isOpen);
                return;
            }

            if (Input.touchCount >= 3)
            {
                if (!gestureLatched)
                {
                    gestureLatched = true;
                    SetOpen(!isOpen);
                }
            }
            else
            {
                gestureLatched = false;
            }
#endif
        }
        #endregion // 열기/닫기

        #region 로그 수집
        /// <summary>
        /// Unity 로그 콜백입니다. 다른 스레드에서도 호출될 수 있어 lock으로 보호합니다.
        /// </summary>
        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (logLock)
            {
                // 직전 로그와 동일하면 개수만 증가 (collapse)
                if (logEntries.Count > 0)
                {
                    LogEntry lastEntry = logEntries[logEntries.Count - 1];
                    if (lastEntry.type == type && lastEntry.message == condition)
                    {
                        lastEntry.count++;
                        return;
                    }
                }

                logEntries.Add(new LogEntry
                {
                    message = condition,
                    stackTrace = stackTrace,
                    type = type,
                    timeText = DateTime.Now.ToString("HH:mm:ss")
                });

                if (logEntries.Count > MaxLogCount)
                {
                    logEntries.RemoveAt(0);
                    if (selectedLogIndex >= 0)
                        selectedLogIndex--;
                }
            }
        }
        #endregion // 로그 수집

        #region 명령 등록
        /// <summary>
        /// 모든 어셈블리에서 static [SWCommand] 메서드를 1회 수집합니다.
        /// </summary>
        private void ScanStaticCommandsIfNeeded()
        {
            if (staticCommandsScanned) return;
            staticCommandsScanned = true;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                string assemblyName = assemblies[index].GetName().Name;
                if (ShouldSkipAssembly(assemblyName)) continue;

                try
                {
                    Type[] types = assemblies[index].GetTypes();
                    for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                        CollectCommands(types[typeIndex], null, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch (ReflectionTypeLoadException)
                {
                    // 로드 실패한 타입이 있는 어셈블리는 건너뜁니다.
                }
            }
        }

        /// <summary>
        /// 명령 수집에서 제외할 어셈블리인지 확인합니다.
        /// </summary>
        private static bool ShouldSkipAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("Unity", StringComparison.Ordinal)
                || assemblyName.StartsWith("System", StringComparison.Ordinal)
                || assemblyName.StartsWith("Mono.", StringComparison.Ordinal)
                || assemblyName.StartsWith("Microsoft", StringComparison.Ordinal)
                || assemblyName == "mscorlib"
                || assemblyName == "netstandard";
        }

        /// <summary>
        /// 인스턴스의 [SWCommand] 메서드를 등록합니다.
        /// </summary>
        /// <param name="target">명령을 가진 오브젝트입니다.</param>
        internal void RegisterInstanceCommands(object target)
        {
            if (target == null) return;

            UnregisterInstanceCommands(target);
            CollectCommands(target.GetType(), target,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// 인스턴스의 명령 등록을 해제합니다.
        /// </summary>
        /// <param name="target">해제할 오브젝트입니다.</param>
        internal void UnregisterInstanceCommands(object target)
        {
            if (target == null) return;

            for (int index = commands.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(commands[index].target, target))
                    commands.RemoveAt(index);
            }
        }

        /// <summary>
        /// 타입에서 [SWCommand] 메서드를 찾아 등록합니다. (상속 계층 포함)
        /// </summary>
        private void CollectCommands(Type type, object target, BindingFlags bindingFlags)
        {
            MethodInfo[] methods = type.GetMethods(bindingFlags | BindingFlags.FlattenHierarchy);
            for (int index = 0; index < methods.Length; index++)
            {
                SWCommandAttribute commandAttribute = methods[index].GetCustomAttribute<SWCommandAttribute>();
                if (commandAttribute == null) continue;

                string commandName = string.IsNullOrEmpty(commandAttribute.Name)
                    ? methods[index].Name
                    : commandAttribute.Name;

                commands.Add(new CommandInfo
                {
                    name = commandName,
                    description = commandAttribute.Description,
                    group = string.IsNullOrEmpty(commandAttribute.Group) ? "기본" : commandAttribute.Group,
                    method = methods[index],
                    target = target,
                    parameters = methods[index].GetParameters()
                });
            }
        }
        #endregion // 명령 등록

        #region 명령 실행
        /// <summary>
        /// 명령 한 줄을 파싱해서 실행합니다.
        /// </summary>
        /// <param name="commandLine">명령과 인자를 포함한 문자열입니다.</param>
        internal void ExecuteCommandLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return;

            ScanStaticCommandsIfNeeded();
            AddHistory(commandLine);

            List<string> tokens = Tokenize(commandLine);
            if (tokens.Count == 0) return;

            string commandName = tokens[0];

            // 내장 명령
            if (string.Equals(commandName, "help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return;
            }

            if (string.Equals(commandName, "clear", StringComparison.OrdinalIgnoreCase))
            {
                lock (logLock) { logEntries.Clear(); }
                selectedLogIndex = -1;
                return;
            }

            CommandInfo command = FindCommand(commandName, tokens.Count - 1);
            if (command == null)
            {
                UnityEngine.Debug.LogWarning($"[SWDebugConsole] 알 수 없는 명령: {commandName} (help로 목록 확인)");
                return;
            }

            InvokeCommand(command, tokens);
        }

        /// <summary>
        /// 이름과 인자 개수로 명령을 찾습니다. 파괴된 인스턴스 명령은 건너뜁니다.
        /// </summary>
        private CommandInfo FindCommand(string commandName, int argumentCount)
        {
            CommandInfo nameMatched = null;

            for (int index = 0; index < commands.Count; index++)
            {
                CommandInfo command = commands[index];
                if (!string.Equals(command.name, commandName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (command.target is UnityEngine.Object unityTarget && unityTarget == null)
                    continue;

                nameMatched = command;
                if (command.parameters.Length == argumentCount)
                    return command;
            }

            return nameMatched;
        }

        /// <summary>
        /// 인자를 변환해 명령 메서드를 호출합니다.
        /// </summary>
        private void InvokeCommand(CommandInfo command, List<string> tokens)
        {
            int argumentCount = tokens.Count - 1;
            if (command.parameters.Length != argumentCount)
            {
                UnityEngine.Debug.LogWarning($"[SWDebugConsole] 인자 개수가 맞지 않습니다. 사용법: {command.Signature}");
                return;
            }

            object[] arguments = new object[argumentCount];
            for (int index = 0; index < argumentCount; index++)
            {
                if (!TryConvertArgument(tokens[index + 1], command.parameters[index].ParameterType, out arguments[index]))
                {
                    UnityEngine.Debug.LogWarning($"[SWDebugConsole] 인자 변환 실패: '{tokens[index + 1]}' → {command.parameters[index].ParameterType.Name}");
                    return;
                }
            }

            try
            {
                object result = command.method.Invoke(command.target, arguments);
                UnityEngine.Debug.Log(result != null
                    ? $"[SWDebugConsole] {command.name} → {result}"
                    : $"[SWDebugConsole] {command.name} 실행 완료");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[SWDebugConsole] {command.name} 실행 오류: {exception.InnerException?.Message ?? exception.Message}");
            }
        }

        /// <summary>
        /// 문자열 인자를 파라미터 타입으로 변환합니다. int, float, bool, string, enum을 지원합니다.
        /// </summary>
        private static bool TryConvertArgument(string token, Type parameterType, out object value)
        {
            value = null;

            if (parameterType == typeof(string)) { value = token; return true; }
            if (parameterType == typeof(int)) { bool ok = int.TryParse(token, out int intValue); value = intValue; return ok; }
            if (parameterType == typeof(float))
            {
                bool ok = float.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float floatValue);
                value = floatValue;
                return ok;
            }
            if (parameterType == typeof(bool))
            {
                if (token == "1" || string.Equals(token, "on", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
                if (token == "0" || string.Equals(token, "off", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
                bool ok = bool.TryParse(token, out bool boolValue);
                value = boolValue;
                return ok;
            }
            if (parameterType.IsEnum)
            {
                try { value = Enum.Parse(parameterType, token, true); return true; }
                catch { return false; }
            }

            return false;
        }

        /// <summary>
        /// 명령 줄을 토큰으로 분리합니다. 큰따옴표로 공백 포함 문자열을 지원합니다.
        /// </summary>
        private static List<string> Tokenize(string commandLine)
        {
            List<string> tokens = new();
            StringBuilder builder = new();
            bool insideQuotes = false;

            for (int index = 0; index < commandLine.Length; index++)
            {
                char character = commandLine[index];

                if (character == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (character == ' ' && !insideQuotes)
                {
                    if (builder.Length > 0)
                    {
                        tokens.Add(builder.ToString());
                        builder.Clear();
                    }
                    continue;
                }

                builder.Append(character);
            }

            if (builder.Length > 0)
                tokens.Add(builder.ToString());

            return tokens;
        }

        /// <summary>
        /// 등록된 명령 목록을 로그로 출력합니다.
        /// </summary>
        private void PrintHelp()
        {
            StringBuilder builder = new("[SWDebugConsole] 명령 목록\n");
            builder.AppendLine("help - 명령 목록 출력");
            builder.AppendLine("clear - 로그 비우기");

            for (int index = 0; index < commands.Count; index++)
            {
                builder.Append(commands[index].Signature);
                if (!string.IsNullOrEmpty(commands[index].description))
                    builder.Append(" - ").Append(commands[index].description);
                builder.AppendLine();
            }

            UnityEngine.Debug.Log(builder.ToString());
        }

        /// <summary>
        /// 입력 히스토리에 명령을 추가합니다.
        /// </summary>
        private void AddHistory(string commandLine)
        {
            inputHistory.Remove(commandLine);
            inputHistory.Add(commandLine);
            if (inputHistory.Count > MaxHistoryCount)
                inputHistory.RemoveAt(0);

            historyIndex = -1;
        }
        #endregion // 명령 실행

        #region 감시
        /// <summary>
        /// 상태 탭 감시 항목을 추가하거나 덮어씁니다.
        /// </summary>
        internal void AddWatch(string watchName, Func<string> valueGetter)
        {
            if (string.IsNullOrEmpty(watchName) || valueGetter == null) return;

            RemoveWatch(watchName);
            watches.Add(new WatchEntry { name = watchName, valueGetter = valueGetter });
        }

        /// <summary>
        /// 감시 항목을 제거합니다.
        /// </summary>
        internal void RemoveWatch(string watchName)
        {
            for (int index = watches.Count - 1; index >= 0; index--)
            {
                if (watches[index].name == watchName)
                    watches.RemoveAt(index);
            }
        }
        #endregion // 감시

        #region GUI
        private void OnGUI()
        {
            if (!isOpen) return;

            // DPI 기반 스케일: 모바일 고해상도에서도 읽을 수 있는 크기로 확대합니다.
            float uiScale = Mathf.Max(1f, Screen.dpi > 0f ? Screen.dpi / 150f : 1f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

            float panelWidth = Screen.width / uiScale;
            float panelHeight = Screen.height / uiScale * 0.65f;

            GUILayout.BeginArea(new Rect(0f, 0f, panelWidth, panelHeight), GUI.skin.box);

            DrawHeaderBar();

            switch (selectedTab)
            {
                case 0: DrawLogTab(panelHeight); break;
                case 1: DrawCommandTab(); break;
                case 2: DrawStatusTab(); break;
            }

            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// 탭과 닫기 버튼을 그립니다.
        /// </summary>
        private void DrawHeaderBar()
        {
            GUILayout.BeginHorizontal();
            selectedTab = GUILayout.Toolbar(selectedTab, TabNames, GUILayout.Height(28f));
            if (GUILayout.Button("X", GUILayout.Width(32f), GUILayout.Height(28f)))
                SetOpen(false);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 로그 탭을 그립니다.
        /// </summary>
        private void DrawLogTab(float panelHeight)
        {
            GUILayout.BeginHorizontal();
            showLog = GUILayout.Toggle(showLog, "Log", GUILayout.Width(56f));
            showWarning = GUILayout.Toggle(showWarning, "Warn", GUILayout.Width(60f));
            showError = GUILayout.Toggle(showError, "Error", GUILayout.Width(60f));
            searchText = GUILayout.TextField(searchText);

            if (GUILayout.Button("복사", GUILayout.Width(48f)))
                CopyLogsToClipboard();

            if (GUILayout.Button("비우기", GUILayout.Width(56f)))
            {
                lock (logLock) { logEntries.Clear(); }
                selectedLogIndex = -1;
            }
            GUILayout.EndHorizontal();

            logScrollPosition = GUILayout.BeginScrollView(logScrollPosition,
                GUILayout.Height(panelHeight * (selectedLogIndex >= 0 ? 0.45f : 0.72f)));

            lock (logLock)
            {
                for (int index = 0; index < logEntries.Count; index++)
                {
                    LogEntry entry = logEntries[index];
                    if (!IsLogVisible(entry)) continue;

                    GUI.contentColor = GetLogColor(entry.type);
                    string countSuffix = entry.count > 1 ? $" (x{entry.count})" : string.Empty;
                    string line = $"[{entry.timeText}] {entry.message}{countSuffix}";

                    if (GUILayout.Button(line, GUI.skin.label))
                        selectedLogIndex = selectedLogIndex == index ? -1 : index;
                }
            }

            GUI.contentColor = Color.white;
            GUILayout.EndScrollView();

            // 선택된 로그의 스택 트레이스
            if (selectedLogIndex >= 0)
            {
                string stackText;
                lock (logLock)
                {
                    stackText = selectedLogIndex < logEntries.Count
                        ? $"{logEntries[selectedLogIndex].message}\n{logEntries[selectedLogIndex].stackTrace}"
                        : string.Empty;
                }

                stackScrollPosition = GUILayout.BeginScrollView(stackScrollPosition, GUI.skin.box);
                GUILayout.TextArea(stackText, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 로그가 필터/검색을 통과하는지 확인합니다.
        /// </summary>
        private bool IsLogVisible(LogEntry entry)
        {
            bool typeVisible = entry.type switch
            {
                LogType.Warning => showWarning,
                LogType.Error or LogType.Exception or LogType.Assert => showError,
                _ => showLog
            };

            if (!typeVisible) return false;
            if (string.IsNullOrEmpty(searchText)) return true;

            return entry.message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 로그 타입별 표시 색을 반환합니다.
        /// </summary>
        private static Color GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Warning => Color.yellow,
                LogType.Error or LogType.Exception or LogType.Assert => new Color(1f, 0.45f, 0.45f),
                _ => Color.white
            };
        }

        /// <summary>
        /// 표시 중인 로그를 클립보드에 복사합니다.
        /// </summary>
        private void CopyLogsToClipboard()
        {
            StringBuilder builder = new();
            lock (logLock)
            {
                for (int index = 0; index < logEntries.Count; index++)
                {
                    LogEntry entry = logEntries[index];
                    if (!IsLogVisible(entry)) continue;

                    builder.Append('[').Append(entry.timeText).Append("] ").AppendLine(entry.message);
                    if (entry.type != LogType.Log && !string.IsNullOrEmpty(entry.stackTrace))
                        builder.AppendLine(entry.stackTrace);
                }
            }

            GUIUtility.systemCopyBuffer = builder.ToString();
        }

        /// <summary>
        /// 명령 탭을 그립니다. 입력창 + 등록된 명령의 버튼 목록입니다.
        /// </summary>
        private void DrawCommandTab()
        {
            ScanStaticCommandsIfNeeded();
            HandleCommandInputKeys();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("SWConsoleInput");
            commandInput = GUILayout.TextField(commandInput, GUILayout.Height(28f));

            if (GUILayout.Button("실행", GUILayout.Width(56f), GUILayout.Height(28f)))
                SubmitCommandInput();

            if (GUILayout.Button("help", GUILayout.Width(52f), GUILayout.Height(28f)))
                ExecuteCommandLine("help");
            GUILayout.EndHorizontal();

            commandScrollPosition = GUILayout.BeginScrollView(commandScrollPosition);

            string currentGroup = null;
            for (int index = 0; index < commands.Count; index++)
            {
                CommandInfo command = commands[index];

                if (command.target is UnityEngine.Object unityTarget && unityTarget == null)
                    continue;

                if (command.group != currentGroup)
                {
                    currentGroup = command.group;
                    GUILayout.Space(4f);
                    GUILayout.Label($"── {currentGroup} ──");
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(string.IsNullOrEmpty(command.description)
                    ? command.Signature
                    : $"{command.Signature}  ({command.description})");
                GUILayout.FlexibleSpace();

                if (command.parameters.Length > 0)
                    command.argsInput = GUILayout.TextField(command.argsInput, GUILayout.Width(140f));

                if (GUILayout.Button("실행", GUILayout.Width(56f)))
                {
                    string line = command.parameters.Length > 0
                        ? $"{command.name} {command.argsInput}"
                        : command.name;
                    ExecuteCommandLine(line);
                }
                GUILayout.EndHorizontal();
            }

            if (commands.Count == 0)
                GUILayout.Label("등록된 명령이 없습니다. [SWCommand] 어트리뷰트를 메서드에 붙여주세요.");

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 입력창 포커스 상태에서 Enter 실행, 위/아래 방향키 히스토리를 처리합니다.
        /// </summary>
        private void HandleCommandInputKeys()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown) return;
            if (GUI.GetNameOfFocusedControl() != "SWConsoleInput") return;

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                SubmitCommandInput();
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.UpArrow && inputHistory.Count > 0)
            {
                historyIndex = historyIndex < 0 ? inputHistory.Count - 1 : Mathf.Max(0, historyIndex - 1);
                commandInput = inputHistory[historyIndex];
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.DownArrow && historyIndex >= 0)
            {
                historyIndex = Mathf.Min(inputHistory.Count - 1, historyIndex + 1);
                commandInput = inputHistory[historyIndex];
                currentEvent.Use();
            }
        }

        /// <summary>
        /// 입력창의 명령을 실행하고 입력을 비웁니다.
        /// </summary>
        private void SubmitCommandInput()
        {
            if (string.IsNullOrWhiteSpace(commandInput)) return;

            ExecuteCommandLine(commandInput);
            commandInput = string.Empty;
        }

        /// <summary>
        /// 상태 탭을 그립니다. FPS, 메모리, 등록된 감시 값을 표시합니다.
        /// </summary>
        private void DrawStatusTab()
        {
            statusScrollPosition = GUILayout.BeginScrollView(statusScrollPosition);

            float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
            GUILayout.Label($"FPS: {fps:F0} ({smoothedDeltaTime * 1000f:F1} ms)");
            GUILayout.Label($"TimeScale: {Time.timeScale:F2}");
            GUILayout.Label($"메모리(할당): {Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
            GUILayout.Label($"메모리(예약): {Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)} MB");
            GUILayout.Label($"해상도: {Screen.width}x{Screen.height} @{Screen.dpi:F0}dpi");

            if (watches.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("── 감시 ──");

                for (int index = 0; index < watches.Count; index++)
                {
                    string valueText;
                    try
                    {
                        valueText = watches[index].valueGetter();
                    }
                    catch (Exception exception)
                    {
                        valueText = $"(오류: {exception.Message})";
                    }

                    GUILayout.Label($"{watches[index].name}: {valueText}");
                }
            }

            GUILayout.EndScrollView();
        }
        #endregion // GUI
    }
#endif // SW_DEBUG_MODE
}
