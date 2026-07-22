using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 노드 생성, 이동, 삭제와 연결 편집을 제공하는 그래프 보기입니다.
    /// </summary>
    internal sealed class SWStateMachineGraphView : GraphView
    {
        #region 필드
        private readonly Dictionary<string, SWStateMachineNodeView> nodeViewsByIdentifier =
            new Dictionary<string, SWStateMachineNodeView>();
        private readonly Action<SWStateMachineNodeView> nodeSelected;
        private readonly Action<SWStateMachineEdgeView> edgeSelected;
        private readonly Action graphChanged;
        private readonly SWStateMachineSearchProvider searchProvider;
        private readonly SWStateMachineGraphEditorSettings editorSettings;
        private SWStateMachineGraphAsset graphAsset;
        private Vector2 lastPointerPanelPosition;
        private bool isReloading;
        private bool isApplyingSnap;
        private static SWStateMachineGraphClipboard clipboard;
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 상태 머신 그래프 보기를 생성합니다.
        /// </summary>
        public SWStateMachineGraphView(
            Action<SWStateMachineNodeView> nodeSelected,
            Action<SWStateMachineEdgeView> edgeSelected,
            Action graphChanged,
            SWStateMachineGraphEditorSettings editorSettings)
        {
            this.nodeSelected = nodeSelected;
            this.edgeSelected = edgeSelected;
            this.graphChanged = graphChanged;
            this.editorSettings = editorSettings;

            style.flexGrow = 1f;
            AddToClassList("sw-state-graph-view");
            SWStateMachineGraphStyles.ApplyGraphView(this);

            GridBackground gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            searchProvider = ScriptableObject.CreateInstance<SWStateMachineSearchProvider>();
            nodeCreationRequest = context => OpenNodeSearchFromRequest(context.screenMousePosition);
            focusable = true;
            RegisterCallback<MouseDownEvent>(OnGraphMouseDown);
            RegisterCallback<KeyDownEvent>(OnGraphKeyDown);
            RegisterCallback<MouseMoveEvent>(mouseEvent =>
                lastPointerPanelPosition = GetPanelPosition(mouseEvent.localMousePosition));
            graphViewChanged = OnGraphViewChanged;
            viewTransformChanged = _ => SaveViewTransform();
            ApplyEditorSettings();
        }
        #endregion // 생성자

        #region 그래프 연결
        /// <summary>
        /// 편집할 그래프 에셋을 연결하고 화면을 다시 구성합니다.
        /// </summary>
        public void SetGraphAsset(SWStateMachineGraphAsset asset)
        {
            graphAsset = asset;
            Reload();
            schedule.Execute(RestoreViewTransform);
        }

        /// <summary>현재 그래프 에셋의 모든 노드와 연결선을 다시 생성합니다.</summary>
        public void Reload()
        {
            isReloading = true;

            foreach (GraphElement element in graphElements.ToList())
                RemoveElement(element);

            nodeViewsByIdentifier.Clear();

            if (graphAsset != null)
            {
                foreach (SWStateMachineNodeData nodeData in graphAsset.Nodes)
                {
                    AddNodeView(nodeData);
                }

                foreach (SWStateMachineTransitionData transitionData in graphAsset.Transitions)
                {
                    AddEdgeView(transitionData);
                }
            }

            isReloading = false;
        }

        /// <summary>그래프의 모든 노드가 화면에 보이도록 위치와 확대 비율을 조정합니다.</summary>
        public void FrameAllNodes()
        {
            FrameAll();
        }

        /// <summary>Layer와 노드 종류를 기준으로 상태 노드를 자동 배치합니다.</summary>
        public void AutoLayout()
        {
            if (graphAsset == null || graphAsset.Nodes.Count == 0)
                return;
            Undo.RecordObject(graphAsset, "상태 머신 그래프 자동 배치");
            float horizontalSpacing = editorSettings.NodeWidth + editorSettings.HorizontalSpacing;
            float verticalSpacing = editorSettings.VerticalSpacing;
            List<SWStateMachineNodeData> flowNodes = graphAsset.Nodes
                .Where(node => node.Kind != SWStateMachineNodeKind.State)
                .OrderBy(node => node.Kind)
                .ToList();
            isReloading = true;
            for (int index = 0; index < flowNodes.Count; index++)
                SetNodeLayoutPosition(flowNodes[index], new Vector2(index * horizontalSpacing, -170f));

            IEnumerable<IGrouping<int, SWStateMachineNodeData>> layers = graphAsset.Nodes
                .Where(node => node.Kind == SWStateMachineNodeKind.State)
                .GroupBy(node => node.Layer)
                .OrderBy(group => group.Key);
            foreach (IGrouping<int, SWStateMachineNodeData> layer in layers)
            {
                List<SWStateMachineNodeData> layerNodes = layer
                    .OrderByDescending(node => node.IsInitialState)
                    .ThenBy(node => node.DisplayName, StringComparer.Ordinal)
                    .ToList();
                for (int index = 0; index < layerNodes.Count; index++)
                {
                    SetNodeLayoutPosition(layerNodes[index], new Vector2(
                        index * horizontalSpacing,
                        layer.Key * verticalSpacing));
                }
            }
            isReloading = false;
            EditorUtility.SetDirty(graphAsset);
            graphChanged?.Invoke();
            FrameAll();
        }

        private void SetNodeLayoutPosition(SWStateMachineNodeData nodeData, Vector2 position)
        {
            Rect nodePosition = nodeData.Position;
            nodePosition.position = position;
            nodeData.Position = nodePosition;
            if (nodeViewsByIdentifier.TryGetValue(nodeData.Identifier, out SWStateMachineNodeView nodeView))
                nodeView.SetPosition(nodePosition);
        }

        private void SaveViewTransform()
        {
            string key = GetViewTransformKey();
            if (string.IsNullOrEmpty(key))
                return;
            Translate translate = contentViewContainer.resolvedStyle.translate;
            Scale scale = contentViewContainer.resolvedStyle.scale;
            EditorPrefs.SetFloat(key + ".PositionX", translate.x.value);
            EditorPrefs.SetFloat(key + ".PositionY", translate.y.value);
            EditorPrefs.SetFloat(key + ".Scale", scale.value.x);
        }

        private void RestoreViewTransform()
        {
            string key = GetViewTransformKey();
            if (string.IsNullOrEmpty(key) || !EditorPrefs.HasKey(key + ".Scale"))
                return;
            Vector3 position = new Vector3(
                EditorPrefs.GetFloat(key + ".PositionX"),
                EditorPrefs.GetFloat(key + ".PositionY"),
                0f);
            float scaleValue = EditorPrefs.GetFloat(key + ".Scale", 1f);
            UpdateViewTransform(position, Vector3.one * scaleValue);
        }

        private string GetViewTransformKey()
        {
            if (graphAsset == null)
                return string.Empty;
            string identifier = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graphAsset));
            return string.IsNullOrWhiteSpace(identifier)
                ? string.Empty
                : "SWUtils.StateMachineGraph.View." + identifier;
        }

        /// <summary>Blackboard에서 선택한 상태 노드를 그래프에서 선택하고 화면에 표시합니다.</summary>
        public void SelectNode(string nodeIdentifier)
        {
            if (!nodeViewsByIdentifier.TryGetValue(nodeIdentifier, out SWStateMachineNodeView nodeView))
                return;

            ClearSelection();
            AddToSelection(nodeView);
            nodeSelected?.Invoke(nodeView);
            FrameSelection();
        }

        /// <summary>Blackboard에서 선택한 전이를 그래프에서 선택하고 화면에 표시합니다.</summary>
        public void SelectTransition(string transitionIdentifier)
        {
            SWStateMachineEdgeView edgeView = edges
                .OfType<SWStateMachineEdgeView>()
                .FirstOrDefault(edge => edge.Data?.Identifier == transitionIdentifier);
            if (edgeView == null)
                return;

            ClearSelection();
            AddToSelection(edgeView);
            edgeSelected?.Invoke(edgeView);
            FrameSelection();
        }

        /// <summary>Blackboard에서 지정한 상태 노드와 연결된 전이를 함께 삭제합니다.</summary>
        public void DeleteNode(string nodeIdentifier)
        {
            if (nodeViewsByIdentifier.TryGetValue(nodeIdentifier, out SWStateMachineNodeView nodeView))
                DeleteElements(new[] { nodeView });
        }

        /// <summary>Blackboard에서 지정한 상태 전이를 삭제합니다.</summary>
        public void DeleteTransition(string transitionIdentifier)
        {
            SWStateMachineEdgeView edgeView = edges
                .OfType<SWStateMachineEdgeView>()
                .FirstOrDefault(edge => edge.Data?.Identifier == transitionIdentifier);
            if (edgeView != null)
                DeleteElements(new[] { edgeView });
        }

        /// <summary>편집기 표시 설정을 그래프와 현재 요소에 적용합니다.</summary>
        public void ApplyEditorSettings()
        {
            foreach (SWStateMachineNodeView nodeView in nodes.ToList().OfType<SWStateMachineNodeView>())
            {
                SWStateMachineGraphStyles.ApplyNode(nodeView, editorSettings.NodeWidth);
                nodeView.SetDescriptionVisible(editorSettings.ShowDescriptions);
            }

            foreach (SWStateMachineEdgeView edgeView in edges.ToList().OfType<SWStateMachineEdgeView>())
            {
                edgeView.SetSummaryVisible(editorSettings.ShowTransitionSummary);
            }
        }

        /// <summary>런타임 상태 스냅샷을 노드와 전이 강조 표시에 반영합니다.</summary>
        public void ApplyRuntimeSnapshot(SWStateMachineGraphDebugSnapshot snapshot)
        {
            foreach (SWStateMachineNodeView nodeView in nodeViewsByIdentifier.Values)
                nodeView.SetRuntimeStatus(false, 0f);
            foreach (SWStateMachineEdgeView edgeView in edges.OfType<SWStateMachineEdgeView>())
                edgeView.SetRuntimeTriggered(false);
            if (snapshot == null)
                return;

            for (int index = 0; index < snapshot.ActiveNodes.Count; index++)
            {
                SWStateMachineGraphActiveNodeDebugData activeNode = snapshot.ActiveNodes[index];
                if (nodeViewsByIdentifier.TryGetValue(
                    activeNode.NodeIdentifier, out SWStateMachineNodeView nodeView))
                {
                    nodeView.SetRuntimeStatus(true, activeNode.ActiveDuration);
                }
            }
            if (snapshot.TransitionHistory.Count == 0)
                return;
            string latestTransitionIdentifier = snapshot.TransitionHistory[0].TransitionIdentifier;
            foreach (SWStateMachineEdgeView edgeView in edges.OfType<SWStateMachineEdgeView>())
                edgeView.SetRuntimeTriggered(edgeView.Data.Identifier == latestTransitionIdentifier);
        }

        /// <summary>지정한 패널 좌표에 노드 검색 창을 엽니다.</summary>
        public void OpenNodeSearchAtPanelPosition(Vector2 panelPosition)
        {
            if (graphAsset == null)
                return;

            Vector2 graphPosition = contentViewContainer.WorldToLocal(panelPosition);
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(panelPosition);
            OpenNodeSearch(screenPosition, graphPosition);
        }

        /// <summary>화면 가운데에 노드 검색 창을 엽니다.</summary>
        public void OpenNodeSearchAtCenter()
        {
            Vector2 panelPosition = worldBound.center;
            OpenNodeSearchAtPanelPosition(panelPosition);
        }

        /// <summary>화면 좌표와 생성할 그래프 좌표를 분리해 검색 창을 엽니다.</summary>
        private void OpenNodeSearch(Vector2 screenPosition, Vector2 graphPosition)
        {
            searchProvider.Initialize(this, graphAsset, graphPosition);
            SearchWindow.Open(new SearchWindowContext(screenPosition, 360f, 420f), searchProvider);
        }

        /// <summary>노드 연결을 빈 공간에 놓았을 때 표시할 검색 창을 엽니다.</summary>
        private void OpenNodeSearchFromRequest(Vector2 screenPosition)
        {
            Vector2 panelPosition = screenPosition - GUIUtility.GUIToScreenPoint(Vector2.zero);
            Vector2 graphPosition = contentViewContainer.WorldToLocal(panelPosition);
            OpenNodeSearch(screenPosition, graphPosition);
        }

        /// <summary>그래프에 초점을 주고 마지막 포인터 위치를 저장합니다.</summary>
        private void OnGraphMouseDown(MouseDownEvent mouseEvent)
        {
            if (mouseEvent.button == 0)
                Focus();

            Vector2 panelPosition = GetPanelPosition(mouseEvent.localMousePosition);
            lastPointerPanelPosition = panelPosition;

            if (!mouseEvent.actionKey || mouseEvent.button != 0 || mouseEvent.clickCount != 2)
                return;

            VisualElement currentElement = mouseEvent.target as VisualElement;
            while (currentElement != null && currentElement != this)
            {
                if (currentElement is SWStateMachineNodeView nodeView)
                {
                    ClearSelection();
                    AddToSelection(nodeView);
                    SelectReachableStates();
                    mouseEvent.StopPropagation();
                    return;
                }
                currentElement = currentElement.parent;
            }
        }

        /// <summary>그래프에 초점이 있을 때 Space 키로 상태 검색 창을 엽니다.</summary>
        private void OnGraphKeyDown(KeyDownEvent keyEvent)
        {
            if (graphAsset == null)
                return;

            if (keyEvent.actionKey && keyEvent.keyCode == KeyCode.C)
            {
                CopySelection();
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.actionKey && keyEvent.keyCode == KeyCode.V)
            {
                PasteClipboardAtPointer();
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.actionKey && keyEvent.keyCode == KeyCode.D)
            {
                DuplicateSelection();
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.keyCode == KeyCode.A)
            {
                FrameAllNodes();
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.keyCode == KeyCode.O)
            {
                UpdateViewTransform(Vector3.zero, Vector3.one);
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.keyCode == KeyCode.LeftBracket)
            {
                SelectPreviousState();
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.keyCode == KeyCode.RightBracket)
            {
                SelectNextState();
                keyEvent.StopPropagation();
                return;
            }
            if (keyEvent.keyCode != KeyCode.Space)
                return;

            Vector2 panelPosition = lastPointerPanelPosition;
            if (!worldBound.Contains(panelPosition))
                panelPosition = worldBound.center;
            OpenNodeSearchAtPanelPosition(panelPosition);
            keyEvent.StopPropagation();
        }

        /// <summary>선택한 노드와 노드 사이 전이를 내부 클립보드에 복사합니다.</summary>
        public void CopySelection()
        {
            List<SWStateMachineNodeView> selectedNodes = selection
                .OfType<SWStateMachineNodeView>()
                .ToList();
            if (selectedNodes.Count == 0)
                return;

            selectedNodes.Sort((left, right) => string.Compare(
                left.Data.Identifier,
                right.Data.Identifier,
                StringComparison.Ordinal));
            float minimumX = selectedNodes.Min(node => node.Data.Position.x);
            float minimumY = selectedNodes.Min(node => node.Data.Position.y);
            Vector2 origin = new Vector2(minimumX, minimumY);
            Dictionary<string, int> indicesByIdentifier = new(StringComparer.Ordinal);
            SWStateMachineGraphClipboard newClipboard = new()
            {
                Origin = origin,
                GraphType = graphAsset.GraphType,
            };
            for (int index = 0; index < selectedNodes.Count; index++)
            {
                SWStateMachineNodeData node = selectedNodes[index].Data;
                indicesByIdentifier[node.Identifier] = index;
                newClipboard.Nodes.Add(new SWStateMachineNodeClipboardData
                {
                    Kind = node.Kind,
                    DisplayName = node.DisplayName,
                    Description = node.Description,
                    StateTypeName = node.StateTypeName,
                    Layer = node.Layer,
                    PositionOffset = node.Position.position - origin,
                });
            }

            foreach (SWStateMachineTransitionData transition in graphAsset.Transitions)
            {
                if (!indicesByIdentifier.TryGetValue(
                    transition.FromNodeIdentifier, out int fromIndex) ||
                    !indicesByIdentifier.TryGetValue(
                    transition.ToNodeIdentifier, out int toIndex))
                    continue;
                newClipboard.Transitions.Add(new SWStateMachineTransitionClipboardData
                {
                    FromNodeIndex = fromIndex,
                    ToNodeIndex = toIndex,
                    Operation = transition.Operation,
                    UsesCommand = transition.UsesCommand,
                    Command = transition.Command,
                    ConditionTypeName = transition.ConditionTypeName,
                    CanReenter = transition.CanReenter,
                    Priority = transition.Priority,
                    SummaryOffset = transition.SummaryOffset,
                });
            }
            clipboard = newClipboard;
        }

        /// <summary>복사된 그래프 요소를 마지막 포인터 위치에 붙여넣습니다.</summary>
        public void PasteClipboardAtPointer()
        {
            Vector2 panelPosition = lastPointerPanelPosition;
            if (!worldBound.Contains(panelPosition))
                panelPosition = worldBound.center;
            PasteClipboard(contentViewContainer.WorldToLocal(panelPosition));
        }

        /// <summary>선택한 그래프 요소를 기존 위치에서 조금 이동해 복제합니다.</summary>
        public void DuplicateSelection()
        {
            CopySelection();
            if (clipboard != null)
                PasteClipboard(clipboard.Origin + new Vector2(32f, 32f));
        }

        /// <summary>선택한 상태에서 전이를 따라 도달할 수 있는 모든 상태를 선택합니다.</summary>
        public void SelectReachableStates()
        {
            SWStateMachineNodeView rootView = selection
                .OfType<SWStateMachineNodeView>()
                .FirstOrDefault();
            if (rootView == null || graphAsset == null)
                return;

            HashSet<string> reachableIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            CollectReachableStates(rootView.Data.Identifier, reachableIdentifiers);
            ClearSelection();
            foreach (string identifier in reachableIdentifiers)
            {
                if (nodeViewsByIdentifier.TryGetValue(identifier, out SWStateMachineNodeView nodeView))
                    AddToSelection(nodeView);
            }
        }

        /// <summary>지정한 상태에서 나가는 전이를 따라 연결된 상태 식별자를 수집합니다.</summary>
        private void CollectReachableStates(
            string nodeIdentifier,
            HashSet<string> reachableIdentifiers)
        {
            if (!reachableIdentifiers.Add(nodeIdentifier))
                return;

            foreach (SWStateMachineTransitionData transition in graphAsset.Transitions)
            {
                if (transition.FromNodeIdentifier == nodeIdentifier)
                {
                    CollectReachableStates(
                        transition.ToNodeIdentifier,
                        reachableIdentifiers);
                }
            }
        }

        /// <summary>현재 선택 상태로 들어오는 첫 번째 전이의 출발 상태를 선택합니다.</summary>
        private void SelectPreviousState()
        {
            SWStateMachineNodeView selectedView = selection
                .OfType<SWStateMachineNodeView>()
                .FirstOrDefault();
            if (selectedView == null || graphAsset == null)
                return;

            SWStateMachineTransitionData transition = graphAsset.Transitions
                .Where(item => item.ToNodeIdentifier == selectedView.Data.Identifier)
                .OrderByDescending(item => item.Priority)
                .FirstOrDefault();
            SelectAndFrameNode(transition?.FromNodeIdentifier);
        }

        /// <summary>현재 선택 상태에서 나가는 첫 번째 전이의 도착 상태를 선택합니다.</summary>
        private void SelectNextState()
        {
            SWStateMachineNodeView selectedView = selection
                .OfType<SWStateMachineNodeView>()
                .FirstOrDefault();
            if (selectedView == null || graphAsset == null)
                return;

            SWStateMachineTransitionData transition = graphAsset.Transitions
                .Where(item => item.FromNodeIdentifier == selectedView.Data.Identifier)
                .OrderByDescending(item => item.Priority)
                .FirstOrDefault();
            SelectAndFrameNode(transition?.ToNodeIdentifier);
        }

        /// <summary>지정한 식별자의 상태 노드를 선택하고 화면 가운데에 표시합니다.</summary>
        private void SelectAndFrameNode(string nodeIdentifier)
        {
            if (string.IsNullOrWhiteSpace(nodeIdentifier) ||
                !nodeViewsByIdentifier.TryGetValue(
                    nodeIdentifier,
                    out SWStateMachineNodeView nodeView))
                return;

            ClearSelection();
            AddToSelection(nodeView);
            FrameSelection();
        }

        /// <summary>내부 클립보드의 노드와 전이를 지정한 원점에 생성합니다.</summary>
        private void PasteClipboard(Vector2 origin)
        {
            if (graphAsset == null || clipboard == null || clipboard.Nodes.Count == 0 ||
                clipboard.GraphType != graphAsset.GraphType)
                return;
            Undo.RecordObject(graphAsset, "상태 머신 그래프 붙여넣기");
            List<SWStateMachineNodeData> createdNodes = new();
            ClearSelection();
            for (int index = 0; index < clipboard.Nodes.Count; index++)
            {
                SWStateMachineNodeClipboardData source = clipboard.Nodes[index];
                SWStateMachineNodeData node = graphAsset.AddNode(
                    source.Kind,
                    source.DisplayName,
                    source.StateTypeName,
                    new Rect(origin + source.PositionOffset, new Vector2(editorSettings.NodeWidth, 100f)));
                node.Description = source.Description;
                node.Layer = source.Layer;
                node.IsInitialState = false;
                createdNodes.Add(node);
                AddNodeView(node);
                AddToSelection(nodeViewsByIdentifier[node.Identifier]);
            }
            for (int index = 0; index < clipboard.Transitions.Count; index++)
            {
                SWStateMachineTransitionClipboardData source = clipboard.Transitions[index];
                SWStateMachineTransitionData transition = graphAsset.AddTransition(
                    createdNodes[source.FromNodeIndex].Identifier,
                    createdNodes[source.ToNodeIndex].Identifier,
                    source.Operation);
                transition.UsesCommand = source.UsesCommand;
                transition.Command = source.Command;
                transition.ConditionTypeName = source.ConditionTypeName;
                transition.CanReenter = source.CanReenter;
                transition.Priority = source.Priority;
                transition.SummaryOffset = source.SummaryOffset;
                AddEdgeView(transition);
            }
            MarkGraphChanged();
        }
        #endregion // 그래프 연결

        #region 노드 생성
        /// <summary>
        /// 지정한 상태 타입을 사용하는 상태 노드를 생성합니다.
        /// </summary>
        public void CreateStateNode(Type stateType, Vector2 position)
        {
            if (graphAsset == null || stateType == null)
                return;

            RecordGraphUndo("상태 노드 추가");
            SWStateMachineNodeData nodeData = graphAsset.AddNode(
                SWStateMachineNodeKind.State,
                stateType.Name,
                stateType.AssemblyQualifiedName,
                new Rect(position, new Vector2(editorSettings.NodeWidth, 100f)));
            EnsureInitialState(nodeData);
            AddNodeView(nodeData);
            MarkGraphChanged();
        }

        /// <summary>
        /// 모든 상태 전이 또는 이전 상태 복귀 같은 흐름 제어 노드를 생성합니다.
        /// </summary>
        public void CreateSpecialNode(SWStateMachineNodeKind kind, Vector2 position)
        {
            if (graphAsset == null || kind == SWStateMachineNodeKind.State)
                return;

            if (kind == SWStateMachineNodeKind.AnyState &&
                graphAsset.GraphType != SWStateMachineGraphType.Layered)
                return;

            if (kind == SWStateMachineNodeKind.Return &&
                graphAsset.GraphType != SWStateMachineGraphType.Stack)
                return;

            string displayName = kind == SWStateMachineNodeKind.AnyState
                ? "Any State"
                : "Return State";
            RecordGraphUndo("흐름 제어 노드 추가");
            SWStateMachineNodeData nodeData = graphAsset.AddNode(
                kind,
                displayName,
                string.Empty,
                new Rect(position, new Vector2(editorSettings.NodeWidth, 90f)));
            AddNodeView(nodeData);
            MarkGraphChanged();
        }

        /// <summary>상태 노드 보기를 그래프에 추가합니다.</summary>
        private void AddNodeView(SWStateMachineNodeData nodeData)
        {
            SWStateMachineNodeView nodeView = new SWStateMachineNodeView(
                nodeData,
                nodeSelected,
                OnNodePositionChanged);
            nodeViewsByIdentifier[nodeData.Identifier] = nodeView;
            AddElement(nodeView);
            nodeView.SetLayerBadgeVisible(
                graphAsset.GraphType == SWStateMachineGraphType.Layered &&
                nodeData.Kind != SWStateMachineNodeKind.Return);
            SWStateMachineGraphStyles.ApplyNode(nodeView, editorSettings.NodeWidth);
        }

        /// <summary>상태 전이 연결선을 그래프에 추가합니다.</summary>
        private void AddEdgeView(SWStateMachineTransitionData transitionData)
        {
            if (!nodeViewsByIdentifier.TryGetValue(
                transitionData.FromNodeIdentifier,
                out SWStateMachineNodeView fromNode) ||
                !nodeViewsByIdentifier.TryGetValue(
                    transitionData.ToNodeIdentifier,
                    out SWStateMachineNodeView toNode) ||
                fromNode.OutputPort == null ||
                toNode.InputPort == null)
                return;

            SWStateMachineEdgeView edgeView = new SWStateMachineEdgeView(
                transitionData,
                edgeSelected,
                BeginTransitionSummaryMove,
                MarkTransitionSummaryMoved)
            {
                output = fromNode.OutputPort,
                input = toNode.InputPort,
            };
            edgeView.output.Connect(edgeView);
            edgeView.input.Connect(edgeView);
            AddElement(edgeView);
            SWStateMachineGraphStyles.ApplyEdge(edgeView);
            edgeView.SetSummaryVisible(editorSettings.ShowTransitionSummary);
        }

        /// <summary>새 계층의 첫 상태 또는 스택의 첫 상태를 초기 상태로 지정합니다.</summary>
        private void EnsureInitialState(SWStateMachineNodeData nodeData)
        {
            bool hasInitialState = graphAsset.Nodes.Any(node =>
                node.Kind == SWStateMachineNodeKind.State &&
                node.IsInitialState &&
                (graphAsset.GraphType == SWStateMachineGraphType.Stack ||
                    node.Layer == nodeData.Layer));

            if (!hasInitialState)
                graphAsset.SetInitialNode(nodeData.Identifier);
        }
        #endregion // 노드 생성

        #region 연결 규칙
        /// <summary>
        /// 시작 포트와 연결할 수 있는 반대 방향 포트 목록을 반환합니다.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            SWStateMachineNodeView startNode = startPort.node as SWStateMachineNodeView;

            foreach (Port port in ports)
            {
                if (port == startPort || port.direction == startPort.direction)
                    continue;

                SWStateMachineNodeView targetNode = port.node as SWStateMachineNodeView;
                if (startNode == null || targetNode == null)
                    continue;

                SWStateMachineNodeView fromNode = startPort.direction == Direction.Output
                    ? startNode
                    : targetNode;
                SWStateMachineNodeView toNode = startPort.direction == Direction.Output
                    ? targetNode
                    : startNode;

                if (CanConnect(fromNode.Data, toNode.Data))
                    compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        /// <summary>그래프 종류와 노드 종류를 기준으로 연결 가능 여부를 확인합니다.</summary>
        private bool CanConnect(
            SWStateMachineNodeData fromNode,
            SWStateMachineNodeData toNode)
        {
            if (graphAsset == null ||
                fromNode.Kind == SWStateMachineNodeKind.Return ||
                toNode.Kind == SWStateMachineNodeKind.AnyState)
                return false;

            if (graphAsset.GraphType == SWStateMachineGraphType.Layered)
            {
                return toNode.Kind == SWStateMachineNodeKind.State &&
                    fromNode.Layer == toNode.Layer;
            }

            return fromNode.Kind == SWStateMachineNodeKind.State &&
                (toNode.Kind == SWStateMachineNodeKind.State ||
                    toNode.Kind == SWStateMachineNodeKind.Return);
        }
        #endregion // 연결 규칙

        #region 변경 처리
        /// <summary>그래프 요소 생성과 제거 내용을 직렬화 데이터에 반영합니다.</summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (isReloading || graphAsset == null)
                return change;

            bool hasChanged = false;

            if (change.elementsToRemove != null && change.elementsToRemove.Count > 0)
            {
                RecordGraphUndo("그래프 요소 제거");

                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is SWStateMachineNodeView nodeView)
                    {
                        graphAsset.RemoveNode(nodeView.Data.Identifier);
                        nodeViewsByIdentifier.Remove(nodeView.Data.Identifier);
                        hasChanged = true;
                    }
                    else if (element is SWStateMachineEdgeView edgeView && edgeView.Data != null)
                    {
                        graphAsset.RemoveTransition(edgeView.Data.Identifier);
                        hasChanged = true;
                    }
                }
            }

            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
            {
                RecordGraphUndo("상태 전이 연결");

                for (int index = 0; index < change.edgesToCreate.Count; index++)
                {
                    Edge edge = change.edgesToCreate[index];
                    SWStateMachineNodeView fromNode = edge.output?.node as SWStateMachineNodeView;
                    SWStateMachineNodeView toNode = edge.input?.node as SWStateMachineNodeView;
                    if (fromNode == null || toNode == null)
                        continue;

                    SWStateMachineTransitionOperation operation = GetDefaultOperation(toNode.Data);
                    SWStateMachineTransitionData transitionData = graphAsset.AddTransition(
                        fromNode.Data.Identifier,
                        toNode.Data.Identifier,
                        operation);

                    SWStateMachineEdgeView edgeView = new SWStateMachineEdgeView(
                        transitionData,
                        edgeSelected,
                        BeginTransitionSummaryMove,
                        MarkTransitionSummaryMoved)
                    {
                        output = edge.output,
                        input = edge.input,
                    };
                    edge.output.Disconnect(edge);
                    edge.input.Disconnect(edge);
                    change.edgesToCreate[index] = edgeView;
                    SWStateMachineGraphStyles.ApplyEdge(edgeView);
                    edgeView.SetSummaryVisible(editorSettings.ShowTransitionSummary);
                    hasChanged = true;
                }
            }

            if (hasChanged)
                MarkGraphChanged();

            return change;
        }

        /// <summary>그래프 종류와 도착 노드에 맞는 기본 연결 동작을 반환합니다.</summary>
        private SWStateMachineTransitionOperation GetDefaultOperation(SWStateMachineNodeData toNode)
        {
            if (graphAsset.GraphType == SWStateMachineGraphType.Layered)
                return SWStateMachineTransitionOperation.Transition;

            return toNode.Kind == SWStateMachineNodeKind.Return
                ? SWStateMachineTransitionOperation.Pop
                : SWStateMachineTransitionOperation.Push;
        }

        /// <summary>이동한 노드 위치를 그래프 에셋에 저장합니다.</summary>
        private void OnNodePositionChanged(SWStateMachineNodeView nodeView, Rect position)
        {
            if (isReloading || graphAsset == null)
                return;

            if (isApplyingSnap)
            {
                nodeView.Data.Position = position;
                return;
            }

            Undo.RecordObject(graphAsset, "상태 노드 이동");
            if (editorSettings.SnapToGrid)
            {
                float snapSize = editorSettings.GridSnapSize;
                position.position = new Vector2(
                    Mathf.Round(position.x / snapSize) * snapSize,
                    Mathf.Round(position.y / snapSize) * snapSize);
                isApplyingSnap = true;
                nodeView.SetPosition(position);
                isApplyingSnap = false;
            }

            nodeView.Data.Position = position;
            EditorUtility.SetDirty(graphAsset);
            UpdateConnectedTransitionSummaries(nodeView);
        }

        /// <summary>이동한 노드에 연결된 모든 전이 요약 위치를 다시 계산합니다.</summary>
        private static void UpdateConnectedTransitionSummaries(SWStateMachineNodeView nodeView)
        {
            IEnumerable<Edge> inputEdges = nodeView.InputPort?.connections ?? Enumerable.Empty<Edge>();
            IEnumerable<Edge> outputEdges = nodeView.OutputPort?.connections ?? Enumerable.Empty<Edge>();
            foreach (SWStateMachineEdgeView edgeView in inputEdges
                .Concat(outputEdges)
                .OfType<SWStateMachineEdgeView>()
                .Distinct())
            {
                edgeView.UpdateSummaryPosition();
            }
        }

        /// <summary>현재 화면 가운데에 노드를 추가할 기본 위치를 반환합니다.</summary>
        public Vector2 GetDefaultNodePosition()
        {
            return contentViewContainer.WorldToLocal(worldBound.center);
        }

        /// <summary>전이 데이터가 바뀐 연결선의 그래프 표시를 갱신합니다.</summary>
        public void RefreshTransition(SWStateMachineEdgeView edgeView)
        {
            edgeView?.RefreshSummary();
        }

        /// <summary>전이 요약 위치 변경을 실행 취소 기록에 추가합니다.</summary>
        private void BeginTransitionSummaryMove(SWStateMachineEdgeView edgeView)
        {
            if (graphAsset != null)
                Undo.RecordObject(graphAsset, "전이 요약 이동");
        }

        /// <summary>전이 요약 위치 변경을 그래프 에셋에 반영합니다.</summary>
        private void MarkTransitionSummaryMoved(SWStateMachineEdgeView edgeView)
        {
            if (graphAsset != null)
                EditorUtility.SetDirty(graphAsset);
        }

        /// <summary>그래프 빈 공간의 메뉴에 상태 생성 작업을 추가합니다.</summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent populateEvent)
        {
            if (graphAsset == null)
                return;

            Vector2 panelPosition = GetPanelPosition(populateEvent.localMousePosition);
            populateEvent.menu.AppendAction(
                "Create Node...",
                _ => OpenNodeSearchAtPanelPosition(panelPosition));
            populateEvent.menu.AppendAction("Frame All", _ => FrameAllNodes());
            populateEvent.menu.AppendAction("Auto Layout", _ => AutoLayout());

            populateEvent.menu.AppendSeparator();
            populateEvent.menu.AppendAction(
                "Paste",
                _ => PasteClipboardAtPointer(),
                clipboard == null || clipboard.GraphType != graphAsset.GraphType
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal);

            if (selection.Count > 0)
            {
                populateEvent.menu.AppendSeparator();
                populateEvent.menu.AppendAction("Copy", _ => CopySelection());
                populateEvent.menu.AppendAction("Duplicate", _ => DuplicateSelection());
                populateEvent.menu.AppendAction(
                    "Select Reachable States",
                    _ => SelectReachableStates());
                populateEvent.menu.AppendAction(
                    "선택 항목 삭제",
                    _ => DeleteElements(selection.OfType<GraphElement>().ToList()));
            }
        }

        /// <summary>그래프 에셋 변경을 실행 취소 기록에 추가합니다.</summary>
        private void RecordGraphUndo(string operationName)
        {
            Undo.RecordObject(graphAsset, operationName);
        }

        /// <summary>그래프 보기의 로컬 좌표를 편집기 패널 좌표로 변환합니다.</summary>
        private Vector2 GetPanelPosition(Vector2 localPosition)
        {
            return panel == null
                ? localPosition
                : this.ChangeCoordinatesTo(panel.visualTree, localPosition);
        }

        /// <summary>그래프 에셋을 변경 상태로 표시하고 외부에 알립니다.</summary>
        private void MarkGraphChanged()
        {
            EditorUtility.SetDirty(graphAsset);
            graphChanged?.Invoke();
        }

        private sealed class SWStateMachineGraphClipboard
        {
            public Vector2 Origin;
            public SWStateMachineGraphType GraphType;
            public readonly List<SWStateMachineNodeClipboardData> Nodes = new();
            public readonly List<SWStateMachineTransitionClipboardData> Transitions = new();
        }

        private sealed class SWStateMachineNodeClipboardData
        {
            public SWStateMachineNodeKind Kind;
            public string DisplayName;
            public string Description;
            public string StateTypeName;
            public int Layer;
            public Vector2 PositionOffset;
        }

        private sealed class SWStateMachineTransitionClipboardData
        {
            public int FromNodeIndex;
            public int ToNodeIndex;
            public SWStateMachineTransitionOperation Operation;
            public bool UsesCommand;
            public int Command;
            public string ConditionTypeName;
            public bool CanReenter;
            public int Priority;
            public Vector2 SummaryOffset;
        }
        #endregion // 변경 처리
    }
}
