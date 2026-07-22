using UnityEditor;
using UnityEngine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프 편집기의 표시와 편집 동작 설정을 관리합니다.
    /// </summary>
    internal sealed class SWStateMachineGraphEditorSettings
    {
        #region 상수
        private const string PreferencePrefix = "SWUtils.StateMachineGraph.";
        private const float DefaultNodeWidth = 220f;
        private const float DefaultGraphPanelWidth = 300f;
        private const float DefaultGraphPanelHeight = 430f;
        private const float DefaultInspectorPanelWidth = 320f;
        private const float DefaultInspectorPanelHeight = 460f;
        private const float DefaultGridSnapSize = 16f;
        private const float DefaultHorizontalSpacing = 60f;
        private const float DefaultVerticalSpacing = 190f;
        private const int DefaultRuntimeRefreshMilliseconds = 200;
        #endregion // 상수

        #region 프로퍼티
        /// <summary>그래프 정보 패널을 표시할지 여부입니다.</summary>
        public bool ShowGraphPanel { get; set; }

        /// <summary>선택 항목 상세 패널을 표시할지 여부입니다.</summary>
        public bool ShowInspectorPanel { get; set; }

        /// <summary>전이 연결선 위에 요약을 표시할지 여부입니다.</summary>
        public bool ShowTransitionSummary { get; set; }

        /// <summary>상태 노드의 설명을 표시할지 여부입니다.</summary>
        public bool ShowDescriptions { get; set; }

        /// <summary>노드를 이동할 때 격자에 맞출지 여부입니다.</summary>
        public bool SnapToGrid { get; set; }

        /// <summary>상태 노드의 공통 너비입니다.</summary>
        public float NodeWidth { get; set; }

        /// <summary>Blackboard 패널의 너비입니다.</summary>
        public float GraphPanelWidth { get; set; }

        /// <summary>Blackboard 패널의 높이입니다.</summary>
        public float GraphPanelHeight { get; set; }

        /// <summary>Graph Inspector 패널의 너비입니다.</summary>
        public float InspectorPanelWidth { get; set; }

        /// <summary>Graph Inspector 패널의 높이입니다.</summary>
        public float InspectorPanelHeight { get; set; }

        /// <summary>Blackboard의 States 목록을 펼칠지 여부입니다.</summary>
        public bool StatesExpanded { get; set; }

        /// <summary>Blackboard의 Transitions 목록을 펼칠지 여부입니다.</summary>
        public bool TransitionsExpanded { get; set; }

        /// <summary>노드 위치를 맞출 격자 간격입니다.</summary>
        public float GridSnapSize { get; set; }

        /// <summary>자동 배치에서 사용할 상태 노드의 가로 간격입니다.</summary>
        public float HorizontalSpacing { get; set; }

        /// <summary>자동 배치에서 사용할 Layer 사이의 세로 간격입니다.</summary>
        public float VerticalSpacing { get; set; }

        /// <summary>Play Mode 실행 정보를 갱신할 시간 간격입니다.</summary>
        public int RuntimeRefreshMilliseconds { get; set; }
        #endregion // 프로퍼티

        #region 생성
        /// <summary>저장된 사용자 설정을 불러옵니다.</summary>
        public static SWStateMachineGraphEditorSettings Load()
        {
            return new SWStateMachineGraphEditorSettings
            {
                ShowGraphPanel = EditorPrefs.GetBool(PreferencePrefix + nameof(ShowGraphPanel), true),
                ShowInspectorPanel = EditorPrefs.GetBool(PreferencePrefix + nameof(ShowInspectorPanel), true),
                ShowTransitionSummary = EditorPrefs.GetBool(
                    PreferencePrefix + nameof(ShowTransitionSummary), true),
                ShowDescriptions = EditorPrefs.GetBool(
                    PreferencePrefix + nameof(ShowDescriptions), true),
                SnapToGrid = EditorPrefs.GetBool(PreferencePrefix + nameof(SnapToGrid), false),
                NodeWidth = EditorPrefs.GetFloat(PreferencePrefix + nameof(NodeWidth), DefaultNodeWidth),
                GraphPanelWidth = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(GraphPanelWidth), DefaultGraphPanelWidth),
                GraphPanelHeight = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(GraphPanelHeight), DefaultGraphPanelHeight),
                InspectorPanelWidth = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(InspectorPanelWidth), DefaultInspectorPanelWidth),
                InspectorPanelHeight = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(InspectorPanelHeight), DefaultInspectorPanelHeight),
                StatesExpanded = EditorPrefs.GetBool(
                    PreferencePrefix + nameof(StatesExpanded), true),
                TransitionsExpanded = EditorPrefs.GetBool(
                    PreferencePrefix + nameof(TransitionsExpanded), true),
                GridSnapSize = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(GridSnapSize), DefaultGridSnapSize),
                HorizontalSpacing = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(HorizontalSpacing), DefaultHorizontalSpacing),
                VerticalSpacing = EditorPrefs.GetFloat(
                    PreferencePrefix + nameof(VerticalSpacing), DefaultVerticalSpacing),
                RuntimeRefreshMilliseconds = EditorPrefs.GetInt(
                    PreferencePrefix + nameof(RuntimeRefreshMilliseconds),
                    DefaultRuntimeRefreshMilliseconds),
            };
        }

        /// <summary>모든 값을 기본 설정으로 되돌립니다.</summary>
        public void Reset()
        {
            ShowGraphPanel = true;
            ShowInspectorPanel = true;
            ShowTransitionSummary = true;
            ShowDescriptions = true;
            SnapToGrid = false;
            NodeWidth = DefaultNodeWidth;
            GraphPanelWidth = DefaultGraphPanelWidth;
            GraphPanelHeight = DefaultGraphPanelHeight;
            InspectorPanelWidth = DefaultInspectorPanelWidth;
            InspectorPanelHeight = DefaultInspectorPanelHeight;
            StatesExpanded = true;
            TransitionsExpanded = true;
            GridSnapSize = DefaultGridSnapSize;
            HorizontalSpacing = DefaultHorizontalSpacing;
            VerticalSpacing = DefaultVerticalSpacing;
            RuntimeRefreshMilliseconds = DefaultRuntimeRefreshMilliseconds;
            Save();
        }
        #endregion // 생성

        #region 저장
        /// <summary>현재 사용자 설정을 Unity 편집기 환경 설정에 저장합니다.</summary>
        public void Save()
        {
            NodeWidth = Mathf.Clamp(NodeWidth, 180f, 360f);
            GraphPanelWidth = Mathf.Clamp(GraphPanelWidth, 240f, 560f);
            GraphPanelHeight = Mathf.Clamp(GraphPanelHeight, 220f, 800f);
            InspectorPanelWidth = Mathf.Clamp(InspectorPanelWidth, 260f, 560f);
            InspectorPanelHeight = Mathf.Clamp(InspectorPanelHeight, 240f, 800f);
            GridSnapSize = Mathf.Clamp(GridSnapSize, 8f, 64f);
            HorizontalSpacing = Mathf.Clamp(HorizontalSpacing, 20f, 180f);
            VerticalSpacing = Mathf.Clamp(VerticalSpacing, 120f, 320f);
            RuntimeRefreshMilliseconds = Mathf.Clamp(RuntimeRefreshMilliseconds, 50, 1000);

            EditorPrefs.SetBool(PreferencePrefix + nameof(ShowGraphPanel), ShowGraphPanel);
            EditorPrefs.SetBool(PreferencePrefix + nameof(ShowInspectorPanel), ShowInspectorPanel);
            EditorPrefs.SetBool(PreferencePrefix + nameof(ShowTransitionSummary), ShowTransitionSummary);
            EditorPrefs.SetBool(PreferencePrefix + nameof(ShowDescriptions), ShowDescriptions);
            EditorPrefs.SetBool(PreferencePrefix + nameof(SnapToGrid), SnapToGrid);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(NodeWidth), NodeWidth);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(GraphPanelWidth), GraphPanelWidth);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(GraphPanelHeight), GraphPanelHeight);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(InspectorPanelWidth), InspectorPanelWidth);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(InspectorPanelHeight), InspectorPanelHeight);
            EditorPrefs.SetBool(PreferencePrefix + nameof(StatesExpanded), StatesExpanded);
            EditorPrefs.SetBool(PreferencePrefix + nameof(TransitionsExpanded), TransitionsExpanded);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(GridSnapSize), GridSnapSize);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(HorizontalSpacing), HorizontalSpacing);
            EditorPrefs.SetFloat(PreferencePrefix + nameof(VerticalSpacing), VerticalSpacing);
            EditorPrefs.SetInt(
                PreferencePrefix + nameof(RuntimeRefreshMilliseconds),
                RuntimeRefreshMilliseconds);
        }

        /// <summary>Unity Project Settings에 상태 머신 그래프 설정 화면을 등록합니다.</summary>
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/SWUtils/State Machine Graph", SettingsScope.Project)
            {
                label = "SW State Machine Graph",
                guiHandler = _ => DrawProjectSettings(),
                keywords = new System.Collections.Generic.HashSet<string>
                {
                    "State", "Machine", "Graph", "Node", "Blackboard",
                },
            };
        }

        private static void DrawProjectSettings()
        {
            SWStateMachineGraphEditorSettings settings = Load();
            EditorGUI.BeginChangeCheck();
            settings.ShowGraphPanel = EditorGUILayout.Toggle("Show Blackboard", settings.ShowGraphPanel);
            settings.ShowInspectorPanel = EditorGUILayout.Toggle(
                "Show Graph Inspector", settings.ShowInspectorPanel);
            settings.ShowTransitionSummary = EditorGUILayout.Toggle(
                "Show Transition Summary", settings.ShowTransitionSummary);
            settings.ShowDescriptions = EditorGUILayout.Toggle(
                "Show Descriptions", settings.ShowDescriptions);
            settings.SnapToGrid = EditorGUILayout.Toggle("Snap To Grid", settings.SnapToGrid);
            settings.NodeWidth = EditorGUILayout.Slider("Node Width", settings.NodeWidth, 180f, 360f);
            settings.GridSnapSize = EditorGUILayout.Slider(
                "Grid Snap Size", settings.GridSnapSize, 8f, 64f);
            settings.HorizontalSpacing = EditorGUILayout.Slider(
                "Horizontal Spacing", settings.HorizontalSpacing, 20f, 180f);
            settings.VerticalSpacing = EditorGUILayout.Slider(
                "Vertical Spacing", settings.VerticalSpacing, 120f, 320f);
            settings.GraphPanelWidth = EditorGUILayout.Slider(
                "Blackboard Width", settings.GraphPanelWidth, 240f, 560f);
            settings.GraphPanelHeight = EditorGUILayout.Slider(
                "Blackboard Height", settings.GraphPanelHeight, 220f, 800f);
            settings.InspectorPanelWidth = EditorGUILayout.Slider(
                "Inspector Width", settings.InspectorPanelWidth, 260f, 560f);
            settings.InspectorPanelHeight = EditorGUILayout.Slider(
                "Inspector Height", settings.InspectorPanelHeight, 240f, 800f);
            settings.RuntimeRefreshMilliseconds = EditorGUILayout.IntSlider(
                "Runtime Refresh", settings.RuntimeRefreshMilliseconds, 50, 1000);
            if (EditorGUI.EndChangeCheck())
                settings.Save();
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Reset Settings", GUILayout.Width(130f)))
                settings.Reset();
        }
        #endregion // 저장
    }
}
