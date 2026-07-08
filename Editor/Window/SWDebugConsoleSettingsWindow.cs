using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

using SW.Debugging;

using SW.EditorTools.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// SWDebugConsole과 성능 오버레이 설정을 편집하는 에디터 창입니다.
    /// </summary>
    /// <remarks>
    /// - 설정 에셋(<see cref="SWDebugConsoleSettings"/>)을 찾거나 Resources 폴더에 생성합니다.
    /// - 열기 키, 터치 개수, 오버레이 표시 항목과 위치를 설정합니다.
    /// - SW_DEBUG_MODE 정의 심볼을 현재 빌드 타겟에 추가하거나 제거합니다.
    /// - 플레이 중에는 콘솔과 오버레이를 직접 열고 닫을 수 있습니다.
    /// </remarks>
    public class SWDebugConsoleSettingsWindow : EditorWindow
    {
        #region 필드
        private const string DebugSymbol = "SW_DEBUG_MODE";
        private const string ResourcesFolder = "Assets/Resources";
        private const string DefaultAssetPath = ResourcesFolder + "/" + SWDebugConsoleSettings.ResourceName + ".asset";
        private static readonly string[] TabNames = { "상태", "입력", "오버레이", "플레이" };

        private SWDebugConsoleSettings settings;
        private SerializedObject serializedSettings;
        private Vector2 scrollPosition;
        private int selectedTab;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// Debug Console Settings 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/Debug Console Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<SWDebugConsoleSettingsWindow>();
            SWEditorUtils.SetupWindow(window, "SW Debug Console", "d_UnityEditor.ConsoleWindow.png", 400, 560);
            window.Show();
        }

        private void OnEnable()
        {
            FindSettingsAsset();
        }
        #endregion // 초기화

        #region 그리기
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            selectedTab = SWEditorUtils.DrawTabBar(selectedTab, TabNames);
            DrawSelectedTab();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 현재 선택된 탭 내용을 그립니다.
        /// </summary>
        private void DrawSelectedTab()
        {
            switch (selectedTab)
            {
                case 0:
                    DrawStatusTab();
                    break;
                case 1:
                    DrawSettingsTab(DrawOpenSection);
                    break;
                case 2:
                    DrawSettingsTab(DrawOverlaySection);
                    break;
                case 3:
                    DrawPlayModeSection();
                    break;
            }
        }

        /// <summary>
        /// 정의 심볼과 설정 에셋 상태 탭을 그립니다.
        /// </summary>
        private void DrawStatusTab()
        {
            DrawSymbolSection();
            EditorGUILayout.Space(8f);
            DrawAssetSection();
        }

        /// <summary>
        /// 설정 에셋이 필요한 탭을 그립니다.
        /// </summary>
        /// <param name="drawSettings">설정 내용을 그리는 함수입니다.</param>
        private void DrawSettingsTab(Action drawSettings)
        {
            DrawAssetSection();
            EditorGUILayout.Space(8f);

            if (settings == null) return;

            EnsureSerializedObject();
            serializedSettings.Update();

            drawSettings.Invoke();

            if (serializedSettings.ApplyModifiedProperties())
                EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// SW_DEBUG_MODE 정의 심볼 상태와 추가/제거 버튼을 그립니다.
        /// </summary>
        private void DrawSymbolSection()
        {
            SWEditorUtils.DrawHeader("정의 심볼");

            NamedBuildTarget buildTarget = GetCurrentBuildTarget();
            bool hasSymbol = HasDebugSymbol(buildTarget);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(DebugSymbol, hasSymbol ? "● 활성" : "○ 비활성");

            if (GUILayout.Button(hasSymbol ? "제거" : "추가", GUILayout.Width(56f)))
            {
                SetDebugSymbol(buildTarget, !hasSymbol);
            }
            EditorGUILayout.EndHorizontal();

            if (!hasSymbol)
            {
                EditorGUILayout.HelpBox(
                    $"{DebugSymbol} 심볼이 없으면 콘솔과 오버레이가 컴파일에서 제거됩니다.\n" +
                    $"현재 빌드 타겟: {buildTarget.TargetName}", MessageType.Info);
            }
        }

        /// <summary>
        /// 설정 에셋 연결 상태와 생성 버튼을 그립니다.
        /// </summary>
        private void DrawAssetSection()
        {
            SWEditorUtils.DrawHeader("설정 에셋");

            EditorGUI.BeginChangeCheck();
            settings = (SWDebugConsoleSettings)EditorGUILayout.ObjectField(
                "Settings", settings, typeof(SWDebugConsoleSettings), false);
            if (EditorGUI.EndChangeCheck())
                serializedSettings = null;

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "설정 에셋이 없으면 런타임에서 기본값으로 동작합니다.\n" +
                    "값을 변경하려면 Resources 폴더에 에셋을 생성하세요.", MessageType.Info);

                if (GUILayout.Button("Resources에 설정 에셋 생성"))
                    CreateSettingsAsset();
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(settings);
            if (!assetPath.Contains("/Resources/"))
            {
                EditorGUILayout.HelpBox(
                    "에셋이 Resources 폴더 밖에 있어 런타임에서 불러올 수 없습니다.\n" +
                    $"경로: {assetPath}", MessageType.Warning);
            }
        }

        /// <summary>
        /// 콘솔 열기 설정을 그립니다.
        /// </summary>
        private void DrawOpenSection()
        {
            SWEditorUtils.DrawHeader("콘솔 열기");

            EditorGUILayout.PropertyField(serializedSettings.FindProperty("autoCreateOnLoad"),
                new GUIContent("자동 생성", "씬 로드 후 콘솔 오브젝트를 자동으로 생성합니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("openKey"),
                new GUIContent("열기 키", "콘솔을 여닫는 키보드 키입니다."));
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("조합키", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("requireControlKey"),
                new GUIContent("Control 필요", "콘솔 열기 키와 함께 Control 키를 눌러야 합니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("requireShiftKey"),
                new GUIContent("Shift 필요", "콘솔 열기 키와 함께 Shift 키를 눌러야 합니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("requireAltKey"),
                new GUIContent("Alt 필요", "콘솔 열기 키와 함께 Alt 키를 눌러야 합니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("touchCountToOpen"),
                new GUIContent("터치 개수", "모바일에서 콘솔을 여닫는 동시 터치 손가락 개수입니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableInputSystem"),
                new GUIContent("Input System 확인", "Input System 패키지가 있으면 해당 입력을 먼저 확인합니다. 패키지가 없어도 컴파일 오류가 발생하지 않습니다."));
        }

        /// <summary>
        /// 성능 오버레이 설정을 그립니다.
        /// </summary>
        private void DrawOverlaySection()
        {
            SWEditorUtils.DrawHeader("성능 오버레이");

            EditorGUILayout.PropertyField(serializedSettings.FindProperty("overlayEnabledOnStart"),
                new GUIContent("시작 시 표시", "씬 로드 후 오버레이를 자동으로 표시합니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("overlayAnchor"),
                new GUIContent("표시 위치"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("overlayScale"),
                new GUIContent("크기 배율"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("overlayUpdateInterval"),
                new GUIContent("갱신 간격(초)"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("showFps"),
                new GUIContent("FPS 표시"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("showMinMax"),
                new GUIContent("최소/최대 표시"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("showMemory"),
                new GUIContent("메모리 표시"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("fpsWarningThreshold"),
                new GUIContent("경고 FPS", "이 값 미만이면 노란색으로 표시합니다."));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("fpsDangerThreshold"),
                new GUIContent("위험 FPS", "이 값 미만이면 빨간색으로 표시합니다."));
        }

        /// <summary>
        /// 플레이 중 콘솔과 오버레이 제어 버튼을 그립니다.
        /// </summary>
        private void DrawPlayModeSection()
        {
            SWEditorUtils.DrawHeader("플레이 중 제어");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 콘솔과 오버레이를 제어할 수 있습니다.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("콘솔", SWDebugConsole.IsOpen ? "● 열림" : "○ 닫힘");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("콘솔 열기")) SWDebugConsole.Show();
            if (GUILayout.Button("콘솔 닫기")) SWDebugConsole.Hide();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("오버레이", SWDebugConsole.IsOverlayVisible ? "● 표시 중" : "○ 숨김");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("오버레이 토글")) SWDebugConsole.ToggleOverlay();
            if (GUILayout.Button("기록 초기화")) SWDebugConsole.ResetOverlayStats();
            EditorGUILayout.EndHorizontal();

            Repaint();
        }
        #endregion // 그리기

        #region 에셋 관리
        /// <summary>
        /// 프로젝트에서 설정 에셋을 찾아 연결합니다.
        /// </summary>
        private void FindSettingsAsset()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SWDebugConsoleSettings)}");
            if (guids.Length == 0) return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<SWDebugConsoleSettings>(path);
            serializedSettings = null;
        }

        /// <summary>
        /// Resources 폴더에 설정 에셋을 생성합니다.
        /// </summary>
        private void CreateSettingsAsset()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<SWDebugConsoleSettings>();
            AssetDatabase.CreateAsset(asset, DefaultAssetPath);
            AssetDatabase.SaveAssets();

            settings = asset;
            serializedSettings = null;

            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// SerializedObject를 준비합니다. 에셋이 바뀌면 다시 생성합니다.
        /// </summary>
        private void EnsureSerializedObject()
        {
            if (serializedSettings == null || serializedSettings.targetObject != settings)
                serializedSettings = new SerializedObject(settings);
        }
        #endregion // 에셋 관리

        #region 정의 심볼
        /// <summary>
        /// 현재 활성 빌드 타겟을 NamedBuildTarget으로 반환합니다.
        /// </summary>
        private static NamedBuildTarget GetCurrentBuildTarget()
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }

        /// <summary>
        /// 현재 빌드 타겟에 SW_DEBUG_MODE 심볼이 있는지 확인합니다.
        /// </summary>
        private static bool HasDebugSymbol(NamedBuildTarget buildTarget)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            return defines.Split(';').Any(symbol => symbol.Trim() == DebugSymbol);
        }

        /// <summary>
        /// 현재 빌드 타겟에 SW_DEBUG_MODE 심볼을 추가하거나 제거합니다.
        /// </summary>
        /// <param name="buildTarget">대상 빌드 타겟입니다.</param>
        /// <param name="enable">true면 추가, false면 제거합니다.</param>
        private static void SetDebugSymbol(NamedBuildTarget buildTarget, bool enable)
        {
            string[] symbols = PlayerSettings.GetScriptingDefineSymbols(buildTarget)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(symbol => symbol.Trim())
                .Where(symbol => symbol.Length > 0 && symbol != DebugSymbol)
                .ToArray();

            if (enable)
                symbols = symbols.Append(DebugSymbol).ToArray();

            PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Join(";", symbols));
        }
        #endregion // 정의 심볼
    }
}
