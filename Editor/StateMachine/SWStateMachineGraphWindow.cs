using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프 에셋을 시각적으로 생성하고 편집하는 에디터 창입니다.
    /// </summary>
    public sealed class SWStateMachineGraphWindow : EditorWindow
    {
        #region 상수
        private const string MenuPath = "SWTools/Utils/State Machine/Graph Editor";
        #endregion // 상수

        #region 필드
        private SWStateMachineGraphAsset graphAsset;
        private SWStateMachineGraphView graphView;
        private SWStateMachineGraphEditorSettings editorSettings;
        private VisualElement graphHost;
        private ScrollView libraryPanel;
        private VisualElement inspectorPanel;
        private DropdownField graphTypeField;
        private VisualElement validationPanel;
        private Button validationHeaderButton;
        private ScrollView validationMessageList;
        private Label graphTitleLabel;
        private Label stateCountLabel;
        private Label transitionCountLabel;
        private ListView stateListView;
        private ListView transitionListView;
        private ToolbarSearchField graphDataSearchField;
        private string graphDataSearchText = string.Empty;
        private readonly List<SWStateMachineNodeData> stateListItems =
            new List<SWStateMachineNodeData>();
        private readonly List<SWStateMachineTransitionData> transitionListItems =
            new List<SWStateMachineTransitionData>();
        private ToolbarButton addStateButton;
        private SWStateMachineNodeView selectedNodeView;
        private SWStateMachineEdgeView selectedEdgeView;
        private bool validationExpanded;
        private bool runtimeInspectorVisible;
        private SWStateMachineGraphDebugSnapshot latestRuntimeSnapshot;
        private VisualElement welcomeOverlay;
        private SWGraphAssetListPanel graphAssetListPanel;
        #endregion // 필드

        #region 메뉴
        /// <summary>상태 머신 그래프 편집기 창을 엽니다.</summary>
        [MenuItem(MenuPath)]
        public static SWStateMachineGraphWindow OpenWindow()
        {
            SWStateMachineGraphWindow window = GetWindow<SWStateMachineGraphWindow>();
            window.titleContent = new GUIContent("SW State Machine Graph");
            window.minSize = new Vector2(1000f, 600f);
            return window;
        }

        /// <summary>
        /// 지정한 그래프 에셋을 편집기에서 엽니다.
        /// </summary>
        /// <param name="asset">열어 편집할 그래프 에셋입니다.</param>
        public static void OpenGraph(SWStateMachineGraphAsset asset)
        {
            SWStateMachineGraphWindow window = OpenWindow();
            window.SetGraphAsset(asset);
            window.Focus();
        }
        #endregion // 메뉴

        #region Unity 생명주기
        /// <summary>에디터 창의 사용자 인터페이스를 구성합니다.</summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            editorSettings = SWStateMachineGraphEditorSettings.Load();
            StyleSheet sharedStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                AssetDatabase.GUIDToAssetPath("c1902963a6ec47b49e068b3713d1464d"));
            if (sharedStyleSheet != null)
                rootVisualElement.styleSheets.Add(sharedStyleSheet);
            rootVisualElement.AddToClassList("sw-behaviour-window");
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            SWStateMachineGraphStyles.ApplyWindow(rootVisualElement);
            CreateToolbar();
            CreateGraphArea();
            SetGraphAsset(graphAsset);
            rootVisualElement.schedule.Execute(RefreshRuntimeDebug)
                .Every(editorSettings.RuntimeRefreshMilliseconds);
        }

        /// <summary>실행 취소나 다시 실행 후 그래프 화면을 갱신합니다.</summary>
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        /// <summary>창이 비활성화될 때 실행 취소 이벤트 연결을 해제합니다.</summary>
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }
        #endregion // Unity 생명주기

        #region 화면 구성
        /// <summary>그래프 선택과 주요 작업 버튼을 제공하는 도구 모음을 생성합니다.</summary>
        private void CreateToolbar()
        {
            Toolbar toolbar = new Toolbar();
            toolbar.AddToClassList("sw-behaviour-toolbar");
            toolbar.style.flexDirection = FlexDirection.Row;
            SWStateMachineGraphStyles.ApplyToolbar(toolbar);

            graphTitleLabel = new Label("상태 머신 그래프");
            SWGraphEditorVisualUtility.ApplyToolbarTitle(graphTitleLabel);
            toolbar.Add(graphTitleLabel);

            addStateButton = SWGraphEditorVisualUtility.CreateToolbarButton(
                "Create Node", () => graphView?.OpenNodeSearchAtCenter());
            addStateButton.tooltip = "상태 또는 흐름 제어 노드를 검색해 생성합니다.";
            toolbar.Add(addStateButton);

            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Frame All", () => graphView?.FrameAllNodes()));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Auto Layout", () => graphView?.AutoLayout()));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "New Script", ShowScriptTemplateMenu));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Validate", ValidateGraph));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Save Asset", SaveGraph));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Settings", ShowEditorSettings));
            rootVisualElement.Add(toolbar);
        }

        /// <summary>그래프 보기와 선택 상세 편집 영역을 생성합니다.</summary>
        private void CreateGraphArea()
        {
            TwoPaneSplitView splitView = new TwoPaneSplitView(
                0, 260f, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;
            graphAssetListPanel = new SWGraphAssetListPanel(
                "Graph List",
                "New State Machine",
                typeof(SWStateMachineGraphAsset),
                CreateGraphAsset,
                asset => SetGraphAsset(asset as SWStateMachineGraphAsset));
            splitView.Add(graphAssetListPanel);

            graphHost = new VisualElement();
            graphHost.style.flexGrow = 1f;
            graphHost.style.position = Position.Relative;
            graphHost.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                ApplyFloatingPanelLayout();
                graphView?.ApplyEditorSettings();
            });

            graphView = new SWStateMachineGraphView(
                ShowNodeInspector,
                ShowTransitionInspector,
                OnGraphChanged,
                editorSettings);
            graphHost.Add(graphView);

            libraryPanel = new ScrollView();
            libraryPanel.AddToClassList("sw-behaviour-panel");
            SWStateMachineGraphStyles.ApplyFloatingPanel(libraryPanel);
            libraryPanel.style.left = 12f;
            libraryPanel.style.top = 42f;
            graphHost.Add(libraryPanel);
            AddResizeHandle(libraryPanel, false, size =>
            {
                editorSettings.GraphPanelWidth = size.x;
                editorSettings.GraphPanelHeight = size.y;
                ApplyEditorSettings();
            });

            inspectorPanel = new ScrollView();
            inspectorPanel.AddToClassList("sw-behaviour-panel");
            SWStateMachineGraphStyles.ApplyFloatingPanel(inspectorPanel);
            inspectorPanel.style.right = 12f;
            inspectorPanel.style.top = 12f;
            graphHost.Add(inspectorPanel);
            AddResizeHandle(inspectorPanel, true, size =>
            {
                editorSettings.InspectorPanelWidth = size.x;
                editorSettings.InspectorPanelHeight = size.y;
                ApplyEditorSettings();
            });

            welcomeOverlay = CreateWelcomeOverlay();
            graphHost.Add(welcomeOverlay);
            graphHost.Add(graphAssetListPanel.CreateCollapseButton(splitView));

            splitView.Add(graphHost);
            rootVisualElement.Add(splitView);

            CreateValidationPanel();
            ApplyEditorSettings();
        }

        /// <summary>그래프 검증 결과를 콘솔처럼 펼쳐 볼 수 있는 아래 패널을 생성합니다.</summary>
        private void CreateValidationPanel()
        {
            validationPanel = new VisualElement();
            validationPanel.style.flexShrink = 0f;
            validationPanel.style.backgroundColor = new Color(0.095f, 0.1f, 0.11f);
            validationPanel.style.borderTopWidth = 1f;
            validationPanel.style.borderTopColor = new Color(0.25f, 0.27f, 0.29f);

            validationHeaderButton = new Button(() =>
            {
                SetValidationExpanded(!validationExpanded);
                ValidateGraph();
            });
            validationHeaderButton.style.height = 26f;
            validationHeaderButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            validationHeaderButton.style.marginLeft = 0f;
            validationHeaderButton.style.marginRight = 0f;
            validationHeaderButton.style.marginTop = 0f;
            validationHeaderButton.style.marginBottom = 0f;
            validationPanel.Add(validationHeaderButton);

            validationMessageList = new ScrollView();
            validationMessageList.style.height = 150f;
            validationMessageList.style.paddingLeft = 8f;
            validationMessageList.style.paddingRight = 8f;
            validationMessageList.style.paddingTop = 5f;
            validationMessageList.style.paddingBottom = 5f;
            validationPanel.Add(validationMessageList);
            rootVisualElement.Add(validationPanel);
            SetValidationExpanded(false);
        }

        /// <summary>아래 검증 결과 목록의 펼침 상태를 변경합니다.</summary>
        private void SetValidationExpanded(bool isExpanded)
        {
            validationExpanded = isExpanded;
            if (validationMessageList != null)
            {
                validationMessageList.style.display = validationExpanded
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        /// <summary>떠 있는 패널 아래 모서리에 크기 조절 손잡이를 추가합니다.</summary>
        private static void AddResizeHandle(
            VisualElement panel,
            bool resizeFromLeft,
            Action<Vector2> resizeCompleted)
        {
            Label handle = new Label(resizeFromLeft ? "◣" : "◢");
            handle.tooltip = "끌어서 패널 크기 조절";
            handle.style.position = Position.Absolute;
            handle.style.bottom = 1f;
            handle.style.width = 18f;
            handle.style.height = 18f;
            handle.style.unityTextAlign = TextAnchor.MiddleCenter;
            handle.style.color = new Color(0.55f, 0.58f, 0.62f);
            if (resizeFromLeft)
                handle.style.left = 1f;
            else
                handle.style.right = 1f;
            panel.hierarchy.Add(handle);
            handle.AddManipulator(new SWPanelResizeManipulator(
                panel,
                resizeFromLeft,
                resizeCompleted));
        }
        #endregion // 화면 구성

        #region 편집기 설정
        /// <summary>저장된 편집기 설정을 그래프와 떠 있는 패널에 적용합니다.</summary>
        private void ApplyEditorSettings()
        {
            if (editorSettings == null)
                return;

            editorSettings.Save();
            ApplyFloatingPanelLayout();
            graphView?.ApplyEditorSettings();
        }

        /// <summary>현재 창 크기에 맞춰 떠 있는 패널의 표시와 크기를 갱신합니다.</summary>
        private void ApplyFloatingPanelLayout()
        {
            if (editorSettings == null)
                return;

            if (libraryPanel != null)
            {
                libraryPanel.style.display = editorSettings.ShowGraphPanel
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                libraryPanel.style.width = editorSettings.GraphPanelWidth;
                float availableHeight = graphHost == null
                    ? editorSettings.GraphPanelHeight
                    : Mathf.Max(220f, graphHost.resolvedStyle.height - 24f);
                libraryPanel.style.height = Mathf.Min(
                    editorSettings.GraphPanelHeight,
                    availableHeight);
            }

            if (inspectorPanel != null)
            {
                inspectorPanel.style.display = editorSettings.ShowInspectorPanel
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                inspectorPanel.style.width = editorSettings.InspectorPanelWidth;
                float availableHeight = graphHost == null
                    ? editorSettings.InspectorPanelHeight
                    : Mathf.Max(240f, graphHost.resolvedStyle.height - 24f);
                inspectorPanel.style.height = Mathf.Min(
                    editorSettings.InspectorPanelHeight,
                    availableHeight);
            }
        }

        /// <summary>오른쪽 그래프 인스펙터에 편집기 설정 탭을 표시합니다.</summary>
        private void ShowEditorSettings()
        {
            runtimeInspectorVisible = false;
            editorSettings.ShowInspectorPanel = true;
            ApplyEditorSettings();

            inspectorPanel.Clear();
            AddInspectorTabs(true);
            inspectorPanel.Add(SWStateMachineGraphStyles.CreatePanelTitle("편집기 설정"));
            Label description = new Label(
                "이 설정은 프로젝트 에셋이 아니라 현재 Unity 편집기 사용자 환경에 저장됩니다.");
            SWStateMachineGraphStyles.ApplyMutedText(description);
            inspectorPanel.Add(description);

            VisualElement visibilityCard = SWStateMachineGraphStyles.CreateCard("표시");
            inspectorPanel.Add(visibilityCard);
            AddSettingsToggle(
                visibilityCard,
                "그래프 정보 패널",
                editorSettings.ShowGraphPanel,
                value => editorSettings.ShowGraphPanel = value);
            AddSettingsToggle(
                visibilityCard,
                "그래프 인스펙터",
                editorSettings.ShowInspectorPanel,
                value => editorSettings.ShowInspectorPanel = value);
            AddSettingsToggle(
                visibilityCard,
                "전이 요약",
                editorSettings.ShowTransitionSummary,
                value => editorSettings.ShowTransitionSummary = value);
            AddSettingsToggle(
                visibilityCard,
                "노드 설명",
                editorSettings.ShowDescriptions,
                value => editorSettings.ShowDescriptions = value);

            VisualElement sizeCard = SWStateMachineGraphStyles.CreateCard("크기");
            inspectorPanel.Add(sizeCard);
            AddSettingsSlider(sizeCard, "노드 너비", 180f, 360f, editorSettings.NodeWidth,
                value => editorSettings.NodeWidth = value);
            AddSettingsSlider(sizeCard, "Blackboard 너비", 240f, 560f, editorSettings.GraphPanelWidth,
                value => editorSettings.GraphPanelWidth = value);
            AddSettingsSlider(sizeCard, "Blackboard 높이", 220f, 800f, editorSettings.GraphPanelHeight,
                value => editorSettings.GraphPanelHeight = value);
            AddSettingsSlider(sizeCard, "Graph Inspector 너비", 260f, 560f, editorSettings.InspectorPanelWidth,
                value => editorSettings.InspectorPanelWidth = value);
            AddSettingsSlider(sizeCard, "Graph Inspector 높이", 240f, 800f, editorSettings.InspectorPanelHeight,
                value => editorSettings.InspectorPanelHeight = value);
            AddSettingsSlider(sizeCard, "자동 배치 가로 간격", 20f, 180f,
                editorSettings.HorizontalSpacing,
                value => editorSettings.HorizontalSpacing = value);
            AddSettingsSlider(sizeCard, "자동 배치 세로 간격", 120f, 320f,
                editorSettings.VerticalSpacing,
                value => editorSettings.VerticalSpacing = value);

            VisualElement controlCard = SWStateMachineGraphStyles.CreateCard("조작");
            inspectorPanel.Add(controlCard);
            AddSettingsToggle(
                controlCard,
                "노드를 격자에 맞춤",
                editorSettings.SnapToGrid,
                value => editorSettings.SnapToGrid = value);
            AddSettingsSlider(controlCard, "격자 간격", 8f, 64f, editorSettings.GridSnapSize,
                value => editorSettings.GridSnapSize = value);

            Button resetButton = new Button(() =>
            {
                editorSettings.Reset();
                ApplyEditorSettings();
                ShowEditorSettings();
            })
            {
                text = "기본 설정으로 복원",
            };
            resetButton.style.marginTop = 8f;
            inspectorPanel.Add(resetButton);
        }

        /// <summary>그래프 인스펙터의 선택 항목과 편집기 설정 탭을 추가합니다.</summary>
        private void AddInspectorTabs(bool isSettingsTab, bool isRuntimeTab = false)
        {
            VisualElement tabBar = new VisualElement();
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.marginBottom = 10f;

            Button selectionTab = new Button(ShowSelectionInspector) { text = "선택 항목" };
            Button settingsTab = new Button(ShowEditorSettings) { text = "편집기 설정" };
            Button runtimeTab = new Button(ShowRuntimeInspector) { text = "Runtime" };
            selectionTab.style.flexGrow = 1f;
            settingsTab.style.flexGrow = 1f;
            runtimeTab.style.flexGrow = 1f;
            SWStateMachineGraphStyles.ApplyTabButton(selectionTab, !isSettingsTab && !isRuntimeTab);
            SWStateMachineGraphStyles.ApplyTabButton(settingsTab, isSettingsTab);
            SWStateMachineGraphStyles.ApplyTabButton(runtimeTab, isRuntimeTab);
            tabBar.Add(selectionTab);
            tabBar.Add(settingsTab);
            tabBar.Add(runtimeTab);
            inspectorPanel.Add(tabBar);
        }

        /// <summary>마지막으로 선택한 노드나 전이의 인스펙터로 돌아갑니다.</summary>
        private void ShowSelectionInspector()
        {
            if (selectedNodeView != null && selectedNodeView.panel != null)
            {
                ShowNodeInspector(selectedNodeView);
                return;
            }

            if (selectedEdgeView != null && selectedEdgeView.panel != null)
            {
                ShowTransitionInspector(selectedEdgeView);
                return;
            }

            ShowEmptyInspector();
        }

        /// <summary>선택된 그래프 요소가 없을 때 기본 인스펙터 내용을 표시합니다.</summary>
        private void ShowEmptyInspector()
        {
            runtimeInspectorVisible = false;
            inspectorPanel.Clear();
            AddInspectorTabs(false);
            inspectorPanel.Add(SWStateMachineGraphStyles.CreatePanelTitle("그래프 인스펙터"));
            Label selectionLabel = new Label("상태 노드 또는 전이 연결선을 선택하세요.");
            SWStateMachineGraphStyles.ApplyMutedText(selectionLabel);
            inspectorPanel.Add(selectionLabel);
        }

        /// <summary>실행 중인 상태와 최근 전이 이력을 Graph Inspector에 표시합니다.</summary>
        private void ShowRuntimeInspector()
        {
            runtimeInspectorVisible = true;
            PopulateRuntimeInspector(latestRuntimeSnapshot);
        }

        /// <summary>지정한 실행 스냅샷으로 Runtime Inspector 내용을 다시 구성합니다.</summary>
        private void PopulateRuntimeInspector(SWStateMachineGraphDebugSnapshot snapshot)
        {
            if (inspectorPanel == null)
                return;
            inspectorPanel.Clear();
            AddInspectorTabs(false, true);
            inspectorPanel.Add(SWStateMachineGraphStyles.CreatePanelTitle("Runtime Debug"));
            if (!EditorApplication.isPlaying)
            {
                inspectorPanel.Add(new HelpBox(
                    "Play Mode에서 그래프 상태 머신을 실행하면 상태 정보를 확인할 수 있습니다.",
                    HelpBoxMessageType.Info));
                return;
            }
            if (snapshot == null)
            {
                inspectorPanel.Add(new HelpBox(
                    "선택한 게임 오브젝트에서 이 그래프의 실행 인스턴스를 찾지 못했습니다.",
                    HelpBoxMessageType.Warning));
                return;
            }

            VisualElement activeCard = SWStateMachineGraphStyles.CreateCard("Active States");
            for (int index = 0; index < snapshot.ActiveNodes.Count; index++)
            {
                SWStateMachineGraphActiveNodeDebugData activeNode = snapshot.ActiveNodes[index];
                string stateName = graphAsset.TryGetNode(
                    activeNode.NodeIdentifier, out SWStateMachineNodeData node)
                    ? GetNodeListName(node).Trim()
                    : activeNode.NodeIdentifier;
                activeCard.Add(new Label($"{stateName}  ·  {activeNode.ActiveDuration:0.0}s"));
            }
            if (snapshot.ActiveNodes.Count == 0)
                activeCard.Add(new Label("활성 상태 없음"));
            inspectorPanel.Add(activeCard);

            VisualElement historyCard = SWStateMachineGraphStyles.CreateCard("Transition History");
            int historyCount = Mathf.Min(16, snapshot.TransitionHistory.Count);
            for (int index = 0; index < historyCount; index++)
            {
                SWStateMachineGraphTransitionDebugData history = snapshot.TransitionHistory[index];
                string fromName = graphAsset.TryGetNode(
                    history.FromNodeIdentifier, out SWStateMachineNodeData fromNode)
                    ? GetNodeListName(fromNode).Trim()
                    : "Start";
                string toName = graphAsset.TryGetNode(
                    history.ToNodeIdentifier, out SWStateMachineNodeData toNode)
                    ? GetNodeListName(toNode).Trim()
                    : "Return";
                historyCard.Add(new Label($"{index + 1}. {fromName} → {toName}"));
            }
            if (historyCount == 0)
                historyCard.Add(new Label("기록된 전이 없음"));
            inspectorPanel.Add(historyCard);
        }

        /// <summary>설정 카드에 켜기와 끄기 항목을 추가합니다.</summary>
        private void AddSettingsToggle(
            VisualElement parent,
            string label,
            bool value,
            Action<bool> valueChanged)
        {
            Toggle toggle = new Toggle(label) { value = value };
            toggle.RegisterValueChangedCallback(changeEvent =>
            {
                valueChanged(changeEvent.newValue);
                ApplyEditorSettings();
            });
            parent.Add(toggle);
        }

        /// <summary>설정 카드에 숫자 범위 항목을 추가합니다.</summary>
        private void AddSettingsSlider(
            VisualElement parent,
            string label,
            float minimum,
            float maximum,
            float value,
            Action<float> valueChanged)
        {
            Slider slider = new Slider(label, minimum, maximum)
            {
                value = value,
                showInputField = true,
            };
            slider.RegisterValueChangedCallback(changeEvent =>
            {
                valueChanged(changeEvent.newValue);
                ApplyEditorSettings();
            });
            parent.Add(slider);
        }
        #endregion // 편집기 설정

        #region 그래프 에셋
        /// <summary>새 상태 머신 그래프 에셋을 생성합니다.</summary>
        private void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "상태 머신 그래프 생성",
                "SWStateMachineGraph",
                "asset",
                "그래프 에셋을 저장할 위치를 선택하세요.");
            if (string.IsNullOrWhiteSpace(path))
                return;

            SWStateMachineGraphAsset newAsset = CreateInstance<SWStateMachineGraphAsset>();
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();
            SetGraphAsset(newAsset);
            graphAssetListPanel?.Refresh();
            Selection.activeObject = newAsset;
        }

        /// <summary>프로젝트의 상태 머신 그래프 에셋을 빠르게 전환하는 메뉴를 표시합니다.</summary>
        private void ShowGraphAssetMenu()
        {
            GenericMenu menu = new GenericMenu();
            string[] assetIdentifiers = AssetDatabase.FindAssets("t:SWStateMachineGraphAsset");
            Array.Sort(assetIdentifiers, (left, right) => string.Compare(
                AssetDatabase.GUIDToAssetPath(left),
                AssetDatabase.GUIDToAssetPath(right),
                StringComparison.Ordinal));
            for (int index = 0; index < assetIdentifiers.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetIdentifiers[index]);
                SWStateMachineGraphAsset asset =
                    AssetDatabase.LoadAssetAtPath<SWStateMachineGraphAsset>(path);
                if (asset == null)
                    continue;
                SWStateMachineGraphAsset capturedAsset = asset;
                menu.AddItem(
                    new GUIContent($"Open/{asset.name}"),
                    asset == graphAsset,
                    () => SetGraphAsset(capturedAsset));
            }
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Create New..."), false, CreateGraphAsset);
            menu.ShowAsContext();
        }

        /// <summary>편집할 그래프 에셋을 변경합니다.</summary>
        private void SetGraphAsset(SWStateMachineGraphAsset asset)
        {
            graphAsset = asset;
            selectedNodeView = null;
            selectedEdgeView = null;

            graphAssetListPanel?.SelectAsset(asset);

            if (graphTypeField != null)
            {
                int graphTypeIndex = asset != null && asset.GraphType == SWStateMachineGraphType.Stack
                    ? 1
                    : 0;
                graphTypeField.SetValueWithoutNotify(graphTypeField.choices[graphTypeIndex]);
            }

            graphView?.SetGraphAsset(asset);
            if (welcomeOverlay != null)
                welcomeOverlay.style.display = asset == null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            SetGraphControlsEnabled(asset != null);
            if (graphTitleLabel != null)
                graphTitleLabel.text = asset == null ? "상태 머신 그래프" : asset.name;
            ShowGraphSummary();
            ValidateGraph();
        }

        /// <summary>그래프 에셋 유무에 따라 편집 버튼 활성 상태를 변경합니다.</summary>
        private void SetGraphControlsEnabled(bool isEnabled)
        {
            graphTypeField?.SetEnabled(isEnabled);
            addStateButton?.SetEnabled(isEnabled);
        }

        /// <summary>그래프 종류 변경을 에셋과 화면에 반영합니다.</summary>
        private void OnGraphTypeChanged(ChangeEvent<string> changeEvent)
        {
            if (graphAsset == null)
                return;

            Undo.RecordObject(graphAsset, "상태 머신 그래프 종류 변경");
            graphAsset.GraphType = graphTypeField.index == 1
                ? SWStateMachineGraphType.Stack
                : SWStateMachineGraphType.Layered;
            EditorUtility.SetDirty(graphAsset);
            graphView.Reload();
            ShowGraphSummary();
            ValidateGraph();
        }

        /// <summary>그래프 에셋을 디스크에 저장합니다.</summary>
        private void SaveGraph()
        {
            if (graphAsset == null)
                return;

            EditorUtility.SetDirty(graphAsset);
            AssetDatabase.SaveAssetIfDirty(graphAsset);
            ValidateGraph();
        }

        /// <summary>상태와 전이 조건 스크립트 생성 메뉴를 표시합니다.</summary>
        private void ShowScriptTemplateMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Layered State"), false,
                () => CreateScriptFromTemplate(SWStateMachineScriptKind.LayeredState));
            menu.AddItem(new GUIContent("Stack State"), false,
                () => CreateScriptFromTemplate(SWStateMachineScriptKind.StackState));
            menu.AddItem(new GUIContent("Transition Condition"), false,
                () => CreateScriptFromTemplate(SWStateMachineScriptKind.TransitionCondition));
            menu.ShowAsContext();
        }

        private static void CreateScriptFromTemplate(SWStateMachineScriptKind scriptKind)
        {
            string defaultName = scriptKind switch
            {
                SWStateMachineScriptKind.StackState => "NewStackState",
                SWStateMachineScriptKind.TransitionCondition => "NewTransitionCondition",
                _ => "NewState",
            };
            string path = EditorUtility.SaveFilePanelInProject(
                "State Machine Script 생성", defaultName, "cs", "저장 위치를 선택하세요.");
            if (string.IsNullOrWhiteSpace(path))
                return;
            string className = Path.GetFileNameWithoutExtension(path);
            if (!IsValidClassName(className))
            {
                EditorUtility.DisplayDialog("스크립트 생성 실패",
                    "파일 이름은 유효한 C# 클래스 이름이어야 합니다.", "확인");
                return;
            }
            string templatePath = AssetDatabase.GUIDToAssetPath(scriptKind switch
            {
                SWStateMachineScriptKind.StackState => "bbd305568a6f43b898fab7a07df572fa",
                SWStateMachineScriptKind.TransitionCondition => "fb2a9acdb926410ea1b55a61ad6c4f52",
                _ => "404f53fa3c2f4872944b313f1ee8e267",
            });
            if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
            {
                EditorUtility.DisplayDialog("스크립트 생성 실패",
                    "State Machine 스크립트 템플릿을 찾을 수 없습니다.", "확인");
                return;
            }
            File.WriteAllText(Path.GetFullPath(path),
                File.ReadAllText(templatePath).Replace("#SCRIPTNAME#", className));
            AssetDatabase.ImportAsset(path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        }

        private static bool IsValidClassName(string className)
        {
            if (string.IsNullOrWhiteSpace(className) ||
                !(char.IsLetter(className[0]) || className[0] == '_'))
                return false;
            for (int index = 1; index < className.Length; index++)
            {
                if (!(char.IsLetterOrDigit(className[index]) || className[index] == '_'))
                    return false;
            }
            return true;
        }

        private VisualElement CreateWelcomeOverlay()
        {
            VisualElement overlay = new VisualElement();
            overlay.AddToClassList("sw-behaviour-welcome-overlay");
            VisualElement card = new VisualElement();
            card.AddToClassList("sw-behaviour-welcome-card");
            Label title = new Label("State Machine");
            title.AddToClassList("sw-graph-welcome-title");
            card.Add(title);
            Label description = new Label("Create a new graph or open an existing asset.");
            description.AddToClassList("sw-behaviour-welcome-description");
            card.Add(description);
            Button createButton = new Button(CreateGraphAsset) { text = "Create New Graph" };
            card.Add(createButton);
            Button openButton = new Button(ShowGraphAssetMenu) { text = "Open Existing Graph" };
            card.Add(openButton);
            overlay.Add(card);
            return overlay;
        }

        /// <summary>선택한 게임 오브젝트의 상태 머신 실행 정보를 그래프에 표시합니다.</summary>
        private void RefreshRuntimeDebug()
        {
            if (graphAsset == null || !EditorApplication.isPlaying)
            {
                latestRuntimeSnapshot = null;
                graphView?.ApplyRuntimeSnapshot(null);
                if (runtimeInspectorVisible)
                    PopulateRuntimeInspector(null);
                return;
            }

            GameObject selectedGameObject = Selection.activeGameObject;
            bool found = SWStateMachineGraphDebugRegistry.TryGetSnapshot(
                graphAsset,
                selectedGameObject,
                out SWStateMachineGraphDebugSnapshot snapshot);
            latestRuntimeSnapshot = found ? snapshot : null;
            graphView?.ApplyRuntimeSnapshot(found ? snapshot : null);
            if (runtimeInspectorVisible)
                PopulateRuntimeInspector(latestRuntimeSnapshot);
        }
        #endregion // 그래프 에셋

        #region 상세 편집
        /// <summary>선택한 노드의 속성 편집 화면을 표시합니다.</summary>
        private void ShowNodeInspector(SWStateMachineNodeView nodeView)
        {
            runtimeInspectorVisible = false;
            selectedNodeView = nodeView;
            selectedEdgeView = null;
            inspectorPanel.Clear();
            AddInspectorTabs(false);
            SWStateMachineNodeData nodeData = nodeView.Data;
            inspectorPanel.Add(SWStateMachineGraphStyles.CreatePanelTitle("상태 인스펙터"));
            VisualElement propertyCard = SWStateMachineGraphStyles.CreateCard("상태 속성");
            inspectorPanel.Add(propertyCard);

            TextField displayNameField = new TextField("표시 이름") { value = nodeData.DisplayName };
            displayNameField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("상태 노드 이름 변경");
                nodeData.DisplayName = changeEvent.newValue;
                nodeView.RefreshVisuals();
            });
            propertyCard.Add(displayNameField);

            TextField descriptionField = new TextField("Description")
            {
                value = nodeData.Description,
                multiline = true,
            };
            descriptionField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("상태 노드 설명 변경");
                nodeData.Description = changeEvent.newValue;
                nodeView.RefreshVisuals();
            });
            propertyCard.Add(descriptionField);

            if (nodeData.Kind == SWStateMachineNodeKind.State)
            {
                TextField typeField = new TextField("상태 타입") { value = nodeData.StateTypeName };
                typeField.isReadOnly = true;
                propertyCard.Add(typeField);

                Toggle initialStateToggle = new Toggle("시작 상태로 사용") { value = nodeData.IsInitialState };
                initialStateToggle.RegisterValueChangedCallback(changeEvent =>
                {
                    RecordGraphChange("초기 상태 변경");
                    if (changeEvent.newValue)
                        graphAsset.SetInitialNode(nodeData.Identifier);
                    else
                        nodeData.IsInitialState = false;

                    graphView.Reload();
                    ShowGraphSummary();
                    ValidateGraph();
                });
                propertyCard.Add(initialStateToggle);
            }

            if (graphAsset.GraphType == SWStateMachineGraphType.Layered &&
                nodeData.Kind != SWStateMachineNodeKind.Return)
            {
                IntegerField layerField = new IntegerField("Layer") { value = nodeData.Layer };
                layerField.RegisterValueChangedCallback(changeEvent =>
                {
                    RecordGraphChange("상태 노드 계층 변경");
                    nodeData.Layer = changeEvent.newValue;
                    nodeData.IsInitialState = false;
                    graphView.Reload();
                    ShowGraphSummary();
                    ValidateGraph();
                });
                propertyCard.Add(layerField);
            }

            AddDeleteButton(nodeView);
        }

        /// <summary>선택한 상태 전이의 속성 편집 화면을 표시합니다.</summary>
        private void ShowTransitionInspector(SWStateMachineEdgeView edgeView)
        {
            runtimeInspectorVisible = false;
            selectedNodeView = null;
            selectedEdgeView = edgeView;
            inspectorPanel.Clear();
            AddInspectorTabs(false);
            SWStateMachineTransitionData transitionData = edgeView.Data;
            inspectorPanel.Add(SWStateMachineGraphStyles.CreatePanelTitle("전이 인스펙터"));
            VisualElement propertyCard = SWStateMachineGraphStyles.CreateCard("전이 조건");
            inspectorPanel.Add(propertyCard);

            if (graphAsset.GraphType == SWStateMachineGraphType.Stack)
            {
                graphAsset.TryGetNode(
                    transitionData.ToNodeIdentifier,
                    out SWStateMachineNodeData targetNode);
                bool isReturnTarget = targetNode?.Kind == SWStateMachineNodeKind.Return;
                int operationIndex = transitionData.Operation ==
                    SWStateMachineTransitionOperation.Replace
                    ? 1
                    : 0;
                List<string> operationChoices = isReturnTarget
                    ? new List<string> { "Pop" }
                    : new List<string> { "Push", "Replace" };
                DropdownField operationField = new DropdownField(
                    "Stack Operation",
                    operationChoices,
                    operationIndex);
                operationField.SetEnabled(!isReturnTarget);
                operationField.tooltip = isReturnTarget
                    ? "Return State 연결은 항상 Pop으로 실행됩니다."
                    : "Push는 현재 상태 위에 쌓고 Replace는 현재 상태를 교체합니다.";
                operationField.RegisterValueChangedCallback(changeEvent =>
                {
                    RecordGraphChange("스택 상태 전이 동작 변경");
                    transitionData.Operation = operationField.index == 1
                        ? SWStateMachineTransitionOperation.Replace
                        : SWStateMachineTransitionOperation.Push;
                    graphView.RefreshTransition(edgeView);
                    ValidateGraph();
                });
                propertyCard.Add(operationField);
            }

            Toggle commandToggle = new Toggle("명령으로 전이") { value = transitionData.UsesCommand };
            IntegerField commandField = new IntegerField("전이 명령") { value = transitionData.Command };
            commandField.SetEnabled(transitionData.UsesCommand);
            commandToggle.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("상태 전이 명령 사용 변경");
                transitionData.UsesCommand = changeEvent.newValue;
                commandField.SetEnabled(changeEvent.newValue);
                graphView.RefreshTransition(edgeView);
            });
            commandField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("상태 전이 명령 변경");
                transitionData.Command = changeEvent.newValue;
                graphView.RefreshTransition(edgeView);
            });
            propertyCard.Add(commandToggle);
            propertyCard.Add(commandField);

            TextField conditionTypeField = new TextField("전이 조건 타입")
            {
                value = transitionData.ConditionTypeName,
            };
            conditionTypeField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("상태 전이 조건 변경");
                transitionData.ConditionTypeName = changeEvent.newValue;
                graphView.RefreshTransition(edgeView);
                ValidateGraph();
            });
            propertyCard.Add(conditionTypeField);

            Button selectConditionButton = new Button(() =>
                ShowConditionTypeMenu(edgeView, conditionTypeField))
            {
                text = "전이 조건 선택",
            };
            propertyCard.Add(selectConditionButton);

            Toggle reenterToggle = new Toggle("같은 상태 재진입 허용")
            {
                value = transitionData.CanReenter,
            };
            reenterToggle.SetEnabled(graphAsset.GraphType == SWStateMachineGraphType.Layered);
            reenterToggle.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("자기 상태 재진입 변경");
                transitionData.CanReenter = changeEvent.newValue;
            });
            propertyCard.Add(reenterToggle);

            IntegerField priorityField = new IntegerField("우선순위")
            {
                value = transitionData.Priority,
            };
            priorityField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordGraphChange("상태 전이 우선순위 변경");
                transitionData.Priority = changeEvent.newValue;
                graphView.RefreshTransition(edgeView);
            });
            propertyCard.Add(priorityField);

            AddDeleteButton(edgeView);
        }

        /// <summary>선택한 그래프 요소를 삭제하는 버튼을 상세 편집기에 추가합니다.</summary>
        private void AddDeleteButton(UnityEditor.Experimental.GraphView.GraphElement element)
        {
            Button deleteButton = new Button(() => graphView.DeleteElements(new[] { element }))
            {
                text = "선택 항목 삭제",
            };
            deleteButton.style.marginTop = 12f;
            deleteButton.style.height = 28f;
            deleteButton.style.backgroundColor = new Color(0.48f, 0.18f, 0.18f);
            inspectorPanel.Add(deleteButton);
        }

        /// <summary>그래프 에셋 변경을 실행 취소 기록과 변경 상태에 반영합니다.</summary>
        private void RecordGraphChange(string operationName)
        {
            if (graphAsset == null)
                return;

            Undo.RecordObject(graphAsset, operationName);
            EditorUtility.SetDirty(graphAsset);
            rootVisualElement.schedule.Execute(OnGraphChanged);
        }

        /// <summary>사용 가능한 그래프 조건 타입 선택 메뉴를 표시합니다.</summary>
        private void ShowConditionTypeMenu(
            SWStateMachineEdgeView edgeView,
            TextField conditionTypeField)
        {
            SWStateMachineTransitionData transitionData = edgeView.Data;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("조건 없음"), false, () =>
            {
                RecordGraphChange("상태 전이 조건 제거");
                transitionData.ConditionTypeName = string.Empty;
                conditionTypeField.SetValueWithoutNotify(string.Empty);
                graphView.RefreshTransition(edgeView);
                ValidateGraph();
            });

            IReadOnlyList<Type> conditionTypes = SWStateMachineTypeUtility.GetConditionTypes();
            foreach (Type conditionType in conditionTypes)
            {
                Type capturedType = conditionType;
                string categoryPath = SWStateMachineTypeUtility.GetCategoryPath(
                    conditionType,
                    "Conditions");
                string menuName = $"{categoryPath}/{SWStateMachineTypeUtility.GetDisplayName(conditionType)}";
                menu.AddItem(new GUIContent(menuName), false, () =>
                {
                    RecordGraphChange("상태 전이 조건 선택");
                    transitionData.ConditionTypeName = capturedType.AssemblyQualifiedName;
                    conditionTypeField.SetValueWithoutNotify(transitionData.ConditionTypeName);
                    graphView.RefreshTransition(edgeView);
                    ValidateGraph();
                });
            }

            menu.ShowAsContext();
        }
        #endregion // 상세 편집

        #region 상태 표시
        /// <summary>현재 그래프의 기본 정보를 상세 편집기에 표시합니다.</summary>
        private void ShowGraphSummary(bool resetInspector = true)
        {
            if (libraryPanel == null || inspectorPanel == null)
                return;

            libraryPanel.Clear();
            libraryPanel.Add(SWStateMachineGraphStyles.CreatePanelTitle("Blackboard"));

            if (graphAsset == null)
            {
                libraryPanel.Add(new HelpBox(
                    "새 그래프를 만들거나 기존 그래프 에셋을 선택하세요.",
                    HelpBoxMessageType.Info));
                if (resetInspector)
                    ShowEmptyInspector();
                return;
            }

            VisualElement summaryCard = SWStateMachineGraphStyles.CreateCard(graphAsset.name);
            int graphTypeIndex = graphAsset.GraphType == SWStateMachineGraphType.Stack ? 1 : 0;
            graphTypeField = new DropdownField(
                "Graph Type",
                new List<string> { "Layered", "Stack" },
                graphTypeIndex);
            graphTypeField.tooltip = "Layered 또는 Stack 상태 머신을 선택합니다.";
            graphTypeField.RegisterValueChangedCallback(OnGraphTypeChanged);
            summaryCard.Add(graphTypeField);
            stateCountLabel = new Label();
            transitionCountLabel = new Label();
            summaryCard.Add(stateCountLabel);
            summaryCard.Add(transitionCountLabel);
            UpdateGraphSummaryCounts();
            libraryPanel.Add(summaryCard);

            graphDataSearchField = new ToolbarSearchField();
            graphDataSearchField.value = graphDataSearchText;
            graphDataSearchField.tooltip = "상태와 전이 목록 검색";
            graphDataSearchField.style.marginTop = 6f;
            graphDataSearchField.RegisterValueChangedCallback(changeEvent =>
            {
                graphDataSearchText = changeEvent.newValue ?? string.Empty;
                RefreshGraphDataLists();
            });
            libraryPanel.Add(graphDataSearchField);

            Foldout statesFoldout = CreateBlackboardSection(
                "States",
                editorSettings.StatesExpanded,
                value =>
                {
                    editorSettings.StatesExpanded = value;
                    editorSettings.Save();
                },
                () => graphView.OpenNodeSearchAtCenter(),
                () =>
                {
                    if (stateListView?.selectedItem is SWStateMachineNodeData node)
                        graphView.DeleteNode(node.Identifier);
                });
            stateListView = CreateStateListView();
            statesFoldout.Add(stateListView);
            libraryPanel.Add(statesFoldout);

            Foldout transitionsFoldout = CreateBlackboardSection(
                "Transitions",
                editorSettings.TransitionsExpanded,
                value =>
                {
                    editorSettings.TransitionsExpanded = value;
                    editorSettings.Save();
                },
                null,
                () =>
                {
                    if (transitionListView?.selectedItem is SWStateMachineTransitionData transition)
                        graphView.DeleteTransition(transition.Identifier);
                });
            transitionListView = CreateTransitionListView();
            transitionsFoldout.Add(transitionListView);
            libraryPanel.Add(transitionsFoldout);
            RefreshGraphDataLists();

            if (resetInspector)
                ShowEmptyInspector();
        }

        /// <summary>Blackboard 목록 영역의 제목과 추가 버튼을 생성합니다.</summary>
        private static Foldout CreateBlackboardSection(
            string title,
            bool isExpanded,
            Action<bool> expandedChanged,
            Action addAction,
            Action removeAction)
        {
            Foldout foldout = new Foldout
            {
                text = title,
                value = isExpanded,
            };
            foldout.style.marginTop = 8f;
            foldout.RegisterValueChangedCallback(changeEvent =>
                expandedChanged?.Invoke(changeEvent.newValue));

            VisualElement header = new VisualElement();
            header.style.position = Position.Absolute;
            header.style.top = 0f;
            header.style.right = 0f;
            header.style.height = 28f;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.FlexEnd;
            if (addAction != null)
            {
                Button addButton = new Button(addAction) { text = "+" };
                addButton.tooltip = "Create Node";
                addButton.style.width = 26f;
                addButton.style.height = 22f;
                header.Add(addButton);
            }
            if (removeAction != null)
            {
                Button removeButton = new Button(removeAction) { text = "−" };
                removeButton.tooltip = "Remove Selected";
                removeButton.style.width = 26f;
                removeButton.style.height = 22f;
                header.Add(removeButton);
            }

            foldout.hierarchy.Add(header);
            return foldout;
        }

        /// <summary>상태 노드를 선택하고 찾아갈 수 있는 Blackboard 목록을 생성합니다.</summary>
        private ListView CreateStateListView()
        {
            ListView listView = new ListView
            {
                fixedItemHeight = 24f,
                selectionType = SelectionType.Single,
                makeItem = () => new Label(),
                bindItem = (element, index) =>
                {
                    SWStateMachineNodeData node = stateListItems[index];
                    Label label = (Label)element;
                    label.text = GetNodeListName(node);
                    label.tooltip = node.StateTypeName;
                    label.style.paddingLeft = 6f;
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                },
            };
            listView.selectionChanged += selectedItems =>
            {
                foreach (object selectedItem in selectedItems)
                {
                    if (selectedItem is SWStateMachineNodeData node)
                        graphView.SelectNode(node.Identifier);
                    break;
                }
            };
            return listView;
        }

        /// <summary>상태 전이를 선택하고 찾아갈 수 있는 Blackboard 목록을 생성합니다.</summary>
        private ListView CreateTransitionListView()
        {
            ListView listView = new ListView
            {
                fixedItemHeight = 24f,
                selectionType = SelectionType.Single,
                makeItem = () => new Label(),
                bindItem = (element, index) =>
                {
                    SWStateMachineTransitionData transition = transitionListItems[index];
                    Label label = (Label)element;
                    label.text = GetTransitionListName(transition);
                    label.tooltip = transition.Identifier;
                    label.style.paddingLeft = 6f;
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                },
            };
            listView.selectionChanged += selectedItems =>
            {
                foreach (object selectedItem in selectedItems)
                {
                    if (selectedItem is SWStateMachineTransitionData transition)
                        graphView.SelectTransition(transition.Identifier);
                    break;
                }
            };
            return listView;
        }

        /// <summary>그래프 에셋의 노드와 전이를 Blackboard 목록에 반영합니다.</summary>
        private void RefreshGraphDataLists()
        {
            if (graphAsset == null || stateListView == null || transitionListView == null)
                return;

            stateListItems.Clear();
            foreach (SWStateMachineNodeData node in graphAsset.Nodes)
            {
                if (MatchesGraphDataSearch(GetNodeListName(node), node.StateTypeName))
                    stateListItems.Add(node);
            }
            transitionListItems.Clear();
            foreach (SWStateMachineTransitionData transition in graphAsset.Transitions)
            {
                if (MatchesGraphDataSearch(
                    GetTransitionListName(transition),
                    transition.ConditionTypeName))
                {
                    transitionListItems.Add(transition);
                }
            }
            stateListView.itemsSource = stateListItems;
            transitionListView.itemsSource = transitionListItems;
            stateListView.style.height = Mathf.Clamp(stateListItems.Count * 24f + 2f, 50f, 190f);
            transitionListView.style.height = Mathf.Clamp(
                transitionListItems.Count * 24f + 2f,
                50f,
                190f);
            stateListView.Rebuild();
            transitionListView.Rebuild();
        }

        /// <summary>Blackboard 검색어가 하나 이상의 표시 문자열과 일치하는지 확인합니다.</summary>
        private bool MatchesGraphDataSearch(params string[] values)
        {
            if (string.IsNullOrWhiteSpace(graphDataSearchText))
                return true;

            for (int index = 0; index < values.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(values[index]) &&
                    values[index].IndexOf(
                        graphDataSearchText,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Blackboard에 표시할 상태 노드 이름을 반환합니다.</summary>
        private static string GetNodeListName(SWStateMachineNodeData node)
        {
            string prefix = node.IsInitialState ? "● " : "  ";
            Type stateType = SWStateMachineGraphTypeResolver.Resolve(node.StateTypeName);
            string stateName = string.IsNullOrWhiteSpace(node.DisplayName)
                ? stateType?.Name ?? "Unnamed State"
                : node.DisplayName;
            return node.Kind switch
            {
                SWStateMachineNodeKind.AnyState => prefix + "Any State",
                SWStateMachineNodeKind.Return => prefix + "Return State",
                _ => prefix + stateName,
            };
        }

        /// <summary>Blackboard에 표시할 전이 출발점과 도착점 이름을 반환합니다.</summary>
        private string GetTransitionListName(SWStateMachineTransitionData transition)
        {
            graphAsset.TryGetNode(transition.FromNodeIdentifier, out SWStateMachineNodeData fromNode);
            graphAsset.TryGetNode(transition.ToNodeIdentifier, out SWStateMachineNodeData toNode);
            string fromName = fromNode == null ? "Missing" : GetNodeListName(fromNode).TrimStart(' ', '●');
            string toName = toNode == null ? "Missing" : GetNodeListName(toNode).TrimStart(' ', '●');
            return $"{fromName}  →  {toName}";
        }

        /// <summary>그래프 종류를 사용자에게 표시할 한글 이름으로 변환합니다.</summary>
        private static string GetGraphTypeName(SWStateMachineGraphType graphType)
        {
            return graphType == SWStateMachineGraphType.Layered
                ? "Layered State Machine"
                : "Stack State Machine";
        }

        /// <summary>그래프 정보 패널의 상태와 전이 개수만 가볍게 갱신합니다.</summary>
        private void UpdateGraphSummaryCounts()
        {
            if (graphAsset == null)
                return;

            if (stateCountLabel != null)
                stateCountLabel.text = $"상태  {graphAsset.Nodes.Count}";
            if (transitionCountLabel != null)
                transitionCountLabel.text = $"전이  {graphAsset.Transitions.Count}";
        }

        /// <summary>그래프 검사 결과를 도구 모음에 표시합니다.</summary>
        private void ValidateGraph()
        {
            if (validationHeaderButton == null || validationMessageList == null)
                return;

            validationMessageList.Clear();
            if (graphAsset == null)
            {
                validationHeaderButton.text = "  Graph Validation · 그래프 에셋을 선택하세요.";
                validationHeaderButton.style.color = new Color(0.62f, 0.65f, 0.68f);
                SetValidationExpanded(false);
                return;
            }

            IReadOnlyList<string> messages = SWStateMachineGraphValidator.Validate(graphAsset);
            validationHeaderButton.text = messages.Count == 0
                ? "▶  Graph Validation · No Issues"
                : $"{(validationExpanded ? "▼" : "▶")}  Graph Validation · {messages.Count} Issues";
            validationHeaderButton.style.color = messages.Count == 0
                ? new Color(0.34f, 0.78f, 0.52f)
                : new Color(1f, 0.7f, 0.28f);
            validationHeaderButton.tooltip = messages.Count == 0
                ? "그래프 구성에서 문제가 발견되지 않았습니다."
                : "클릭하여 검증 결과를 펼치거나 접습니다.";

            foreach (string message in messages)
            {
                Label messageLabel = new Label($"⚠  {message}");
                messageLabel.style.whiteSpace = WhiteSpace.Normal;
                messageLabel.style.paddingLeft = 6f;
                messageLabel.style.paddingRight = 6f;
                messageLabel.style.paddingTop = 5f;
                messageLabel.style.paddingBottom = 5f;
                messageLabel.style.marginBottom = 2f;
                messageLabel.style.backgroundColor = new Color(0.16f, 0.13f, 0.08f);
                messageLabel.style.color = new Color(1f, 0.78f, 0.38f);
                validationMessageList.Add(messageLabel);
            }

            if (messages.Count == 0)
                SetValidationExpanded(false);
        }

        /// <summary>그래프 데이터가 변경되면 검사 결과를 갱신합니다.</summary>
        private void OnGraphChanged()
        {
            UpdateGraphSummaryCounts();
            RefreshGraphDataLists();
            ValidateGraph();
            rootVisualElement.schedule.Execute(() =>
            {
                bool nodeWasRemoved = selectedNodeView != null && selectedNodeView.panel == null;
                bool edgeWasRemoved = selectedEdgeView != null && selectedEdgeView.panel == null;
                if (nodeWasRemoved || edgeWasRemoved)
                {
                    selectedNodeView = null;
                    selectedEdgeView = null;
                    ShowEmptyInspector();
                }
            });
        }

        /// <summary>실행 취소 또는 다시 실행된 그래프 내용을 화면에 반영합니다.</summary>
        private void OnUndoRedoPerformed()
        {
            graphView?.Reload();
            ShowGraphSummary();
            ValidateGraph();
            Repaint();
        }

        private enum SWStateMachineScriptKind
        {
            LayeredState,
            StackState,
            TransitionCondition,
        }
        #endregion // 상태 표시
    }
}
