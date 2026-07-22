using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using SW.BehaviourTree;

namespace SW.EditorTools.Behaviour
{
    /// <summary>Behaviour Tree 노드 생성, 배치, 연결과 삭제를 처리하는 그래프입니다.</summary>
    internal sealed class SWBehaviourGraphView : GraphView
    {
        private readonly Dictionary<string, SWBehaviourNodeView> nodeViews = new();
        private readonly Action<SWBehaviourNodeView> nodeSelected;
        private readonly Action graphChanged;
        private readonly SWBehaviourSearchProvider searchProvider;
        private SWBehaviourTreeAsset treeAsset;
        private bool isReloading;
        private Vector2 lastPointerPanelPosition;
        private static SWBehaviourGraphClipboard clipboard;

        public SWBehaviourGraphView(
            Action<SWBehaviourNodeView> nodeSelected,
            Action graphChanged)
        {
            this.nodeSelected = nodeSelected;
            this.graphChanged = graphChanged;
            style.flexGrow = 1f;

            GridBackground grid = new();
            grid.StretchToParentSize();
            Insert(0, grid);
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            searchProvider = ScriptableObject.CreateInstance<SWBehaviourSearchProvider>();
            nodeCreationRequest = request => OpenSearch(request.screenMousePosition);
            graphViewChanged = OnGraphViewChanged;
            viewTransformChanged = _ => SaveViewTransform();
            RegisterCallback<MouseMoveEvent>(mouseEvent =>
                lastPointerPanelPosition = this.ChangeCoordinatesTo(
                    panel.visualTree, mouseEvent.localMousePosition));
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            focusable = true;
        }

        public void SetTree(SWBehaviourTreeAsset asset)
        {
            treeAsset = asset;
            Reload();
            schedule.Execute(RestoreViewTransform);
        }

        public void Reload()
        {
            isReloading = true;
            DeleteElements(graphElements.ToList());
            nodeViews.Clear();
            if (treeAsset != null)
            {
                for (int index = 0; index < treeAsset.Nodes.Count; index++)
                    AddNodeView(treeAsset.Nodes[index]);

                for (int parentIndex = 0; parentIndex < treeAsset.Nodes.Count; parentIndex++)
                {
                    SWBehaviourNode parent = treeAsset.Nodes[parentIndex];
                    for (int childIndex = 0; childIndex < parent.ChildIdentifiers.Count; childIndex++)
                        AddEdge(parent.Identifier, parent.ChildIdentifiers[childIndex]);
                }
            }
            isReloading = false;
        }

        public void CreateNode(Type nodeType, Vector2 position)
        {
            if (treeAsset == null)
                return;
            Undo.RecordObject(treeAsset, "Behaviour 노드 추가");
            SWBehaviourNode node = treeAsset.AddNode(nodeType, position);
            if (node == null)
                return;
            AddNodeView(node);
            MarkChanged();
        }

        public void OpenSearchAtCenter()
        {
            Vector2 panelPosition = worldBound.center;
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(panelPosition);
            OpenSearch(screenPosition);
        }

        public void FrameAllNodes() => FrameAll();

        /// <summary>Root를 기준으로 자식 노드를 위에서 아래, 왼쪽에서 오른쪽으로 자동 배치합니다.</summary>
        public void AutoLayout()
        {
            if (treeAsset == null ||
                !treeAsset.TryGetNode(treeAsset.RootNodeIdentifier, out SWBehaviourNode root))
                return;
            Undo.RecordObject(treeAsset, "Behaviour Tree 자동 배치");
            Dictionary<string, float> subtreeWidths = new(StringComparer.Ordinal);
            CalculateSubtreeWidth(root, subtreeWidths, new HashSet<string>());
            Vector2 rootPosition = root.Position.position;
            isReloading = true;
            LayoutSubtree(root, rootPosition.x, rootPosition.y, subtreeWidths, new HashSet<string>());
            isReloading = false;
            treeAsset.SortChildrenByPosition();
            MarkChanged();
            FrameAll();
        }

        /// <summary>모든 노드의 런타임 실행 결과 표시를 갱신합니다.</summary>
        public void RefreshNodeStatuses(SWBehaviourTreeAsset runtimeTree = null)
        {
            foreach (SWBehaviourNodeView nodeView in nodeViews.Values)
            {
                SWBehaviourStatus status = nodeView.Data.Status;
                if (runtimeTree != null &&
                    runtimeTree.TryGetNode(nodeView.Data.Identifier, out SWBehaviourNode runtimeNode))
                {
                    status = runtimeNode.Status;
                }
                nodeView.RefreshStatus(status);
            }

            foreach (Edge edge in edges)
            {
                if (edge.output?.node is not SWBehaviourNodeView parentView ||
                    edge.input?.node is not SWBehaviourNodeView childView)
                    continue;
                SWBehaviourStatus parentStatus = GetDisplayedStatus(parentView, runtimeTree);
                SWBehaviourStatus childStatus = GetDisplayedStatus(childView, runtimeTree);
                bool isActivePath = runtimeTree != null &&
                    parentStatus != SWBehaviourStatus.Inactive &&
                    childStatus != SWBehaviourStatus.Inactive;
                Color pathColor = childStatus switch
                {
                    SWBehaviourStatus.Running => new Color(1f, 0.72f, 0.18f),
                    SWBehaviourStatus.Success => new Color(0.25f, 0.85f, 0.42f),
                    SWBehaviourStatus.Failure => new Color(0.95f, 0.30f, 0.30f),
                    SWBehaviourStatus.Aborted => new Color(0.57f, 0.59f, 0.62f),
                    _ => new Color(0.42f, 0.45f, 0.49f),
                };
                edge.edgeControl.inputColor = isActivePath ? pathColor :
                    new Color(0.42f, 0.45f, 0.49f);
                edge.edgeControl.outputColor = edge.edgeControl.inputColor;
                edge.style.opacity = isActivePath ? 1f : 0.65f;
                edge.edgeControl.MarkDirtyRepaint();
            }
        }

        private static SWBehaviourStatus GetDisplayedStatus(
            SWBehaviourNodeView nodeView,
            SWBehaviourTreeAsset runtimeTree)
        {
            return runtimeTree != null &&
                runtimeTree.TryGetNode(nodeView.Data.Identifier, out SWBehaviourNode runtimeNode)
                ? runtimeNode.Status
                : nodeView.Data.Status;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> result = new();
            foreach (Port port in ports)
            {
                if (port == startPort || port.direction == startPort.direction || port.node == startPort.node)
                    continue;
                Port output = startPort.direction == Direction.Output ? startPort : port;
                Port input = startPort.direction == Direction.Input ? startPort : port;
                SWBehaviourNodeView parent = output.node as SWBehaviourNodeView;
                SWBehaviourNodeView child = input.node as SWBehaviourNodeView;
                if (parent != null && child != null)
                    result.Add(port);
            }
            return result;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent populateEvent)
        {
            if (treeAsset == null)
                return;
            Vector2 panelPosition = this.ChangeCoordinatesTo(panel.visualTree,
                populateEvent.localMousePosition);
            Vector2 graphPosition = contentViewContainer.WorldToLocal(panelPosition);
            populateEvent.menu.AppendAction("Create Node...", _ =>
            {
                Vector2 screenPosition = GUIUtility.GUIToScreenPoint(panelPosition);
                searchProvider.Initialize(this, graphPosition);
                SearchWindow.Open(new SearchWindowContext(screenPosition, 360f, 420f), searchProvider);
            });
            populateEvent.menu.AppendAction("Frame All", _ => FrameAll());
            populateEvent.menu.AppendAction("Auto Layout", _ => AutoLayout());
            populateEvent.menu.AppendSeparator();
            populateEvent.menu.AppendAction(
                "Paste",
                _ => PasteAt(graphPosition),
                clipboard == null
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal);
            if (selection.Count > 0)
            {
                populateEvent.menu.AppendAction("Copy", _ => CopySelection());
                populateEvent.menu.AppendAction("Duplicate", _ => DuplicateSelection());
                populateEvent.menu.AppendAction("Select SubTree", _ => SelectSubTree());
                populateEvent.menu.AppendAction("Delete Selection", _ =>
                    DeleteElements(selection.OfType<GraphElement>().ToList()));
            }
        }

        /// <summary>선택 노드와 내부 연결을 복사합니다.</summary>
        public void CopySelection()
        {
            List<SWBehaviourNodeView> selectedNodes = selection
                .OfType<SWBehaviourNodeView>()
                .ToList();
            if (selectedNodes.Count == 0)
                return;
            float minimumX = selectedNodes.Min(view => view.Data.Position.x);
            float minimumY = selectedNodes.Min(view => view.Data.Position.y);
            Vector2 origin = new(minimumX, minimumY);
            Dictionary<string, int> indices = new(StringComparer.Ordinal);
            SWBehaviourGraphClipboard copied = new() { Origin = origin };
            for (int index = 0; index < selectedNodes.Count; index++)
            {
                SWBehaviourNode node = selectedNodes[index].Data;
                indices[node.Identifier] = index;
                copied.Nodes.Add(new SWBehaviourNodeClipboardData
                {
                    TypeName = node.GetType().AssemblyQualifiedName,
                    SerializedNode = JsonUtility.ToJson(node),
                    PositionOffset = node.Position.position - origin,
                });
            }
            for (int parentIndex = 0; parentIndex < selectedNodes.Count; parentIndex++)
            {
                SWBehaviourNode parent = selectedNodes[parentIndex].Data;
                for (int childIndex = 0; childIndex < parent.ChildIdentifiers.Count; childIndex++)
                {
                    if (indices.TryGetValue(parent.ChildIdentifiers[childIndex], out int copiedChildIndex))
                        copied.Connections.Add(new Vector2Int(parentIndex, copiedChildIndex));
                }
            }
            clipboard = copied;
        }

        /// <summary>복사된 노드를 마지막 포인터 위치에 붙여넣습니다.</summary>
        public void PasteAtPointer()
        {
            Vector2 panelPosition = lastPointerPanelPosition;
            if (!worldBound.Contains(panelPosition)) panelPosition = worldBound.center;
            PasteAt(contentViewContainer.WorldToLocal(panelPosition));
        }

        /// <summary>선택한 노드와 내부 연결을 조금 이동하여 복제합니다.</summary>
        public void DuplicateSelection()
        {
            CopySelection();
            if (clipboard != null)
                PasteAt(clipboard.Origin + new Vector2(32f, 32f));
        }

        /// <summary>선택한 부모 아래의 모든 하위 노드를 선택합니다.</summary>
        public void SelectSubTree()
        {
            SWBehaviourNodeView rootView = selection.OfType<SWBehaviourNodeView>().FirstOrDefault();
            if (rootView == null)
                return;
            HashSet<string> identifiers = new(StringComparer.Ordinal);
            CollectSubTree(rootView.Data, identifiers);
            ClearSelection();
            foreach (string identifier in identifiers)
            {
                if (nodeViews.TryGetValue(identifier, out SWBehaviourNodeView view))
                    AddToSelection(view);
            }
        }

        private void OpenSearch(Vector2 screenPosition)
        {
            if (treeAsset == null)
                return;
            Vector2 panelPosition = screenPosition - GUIUtility.GUIToScreenPoint(Vector2.zero);
            Vector2 graphPosition = contentViewContainer.WorldToLocal(panelPosition);
            searchProvider.Initialize(this, graphPosition);
            SearchWindow.Open(new SearchWindowContext(screenPosition, 360f, 420f), searchProvider);
        }

        private void AddNodeView(SWBehaviourNode node)
        {
            SWBehaviourNodeView view = new(node, nodeSelected, OnNodePositionChanged);
            view.SetRoot(treeAsset != null && treeAsset.RootNodeIdentifier == node.Identifier);
            nodeViews[node.Identifier] = view;
            AddElement(view);
        }

        /// <summary>에셋별 그래프 이동 위치와 확대 비율을 EditorPrefs에 저장합니다.</summary>
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

        /// <summary>마지막으로 저장한 에셋별 그래프 이동 위치와 확대 비율을 복원합니다.</summary>
        private void RestoreViewTransform()
        {
            string key = GetViewTransformKey();
            if (string.IsNullOrEmpty(key) || !EditorPrefs.HasKey(key + ".Scale"))
                return;
            Vector3 position = new(
                EditorPrefs.GetFloat(key + ".PositionX"),
                EditorPrefs.GetFloat(key + ".PositionY"),
                0f);
            float scaleValue = EditorPrefs.GetFloat(key + ".Scale", 1f);
            UpdateViewTransform(position, Vector3.one * scaleValue);
        }

        private string GetViewTransformKey()
        {
            if (treeAsset == null)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(treeAsset);
            string identifier = AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrWhiteSpace(identifier)
                ? string.Empty
                : "SWUtils.BehaviourTree.View." + identifier;
        }

        private void AddEdge(string parentIdentifier, string childIdentifier)
        {
            if (!nodeViews.TryGetValue(parentIdentifier, out SWBehaviourNodeView parent) ||
                !nodeViews.TryGetValue(childIdentifier, out SWBehaviourNodeView child) ||
                parent.OutputPort == null)
                return;
            Edge edge = parent.OutputPort.ConnectTo(child.InputPort);
            AddElement(edge);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (isReloading || treeAsset == null)
                return change;

            bool changed = false;
            if (change.elementsToRemove != null && change.elementsToRemove.Count > 0)
            {
                Undo.RecordObject(treeAsset, "Behaviour 그래프 요소 삭제");
                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is SWBehaviourNodeView nodeView)
                    {
                        treeAsset.RemoveNode(nodeView.Data.Identifier);
                        nodeViews.Remove(nodeView.Data.Identifier);
                        changed = true;
                    }
                    else if (element is Edge edge &&
                        edge.output?.node is SWBehaviourNodeView parent &&
                        edge.input?.node is SWBehaviourNodeView child)
                    {
                        treeAsset.Disconnect(parent.Data.Identifier, child.Data.Identifier);
                        changed = true;
                    }
                }
            }

            if (change.edgesToCreate != null)
            {
                List<Edge> acceptedEdges = new();
                Undo.RecordObject(treeAsset, "Behaviour 노드 연결");
                foreach (Edge edge in change.edgesToCreate)
                {
                    if (edge.output?.node is SWBehaviourNodeView parent &&
                        edge.input?.node is SWBehaviourNodeView child &&
                        treeAsset.Connect(parent.Data.Identifier, child.Data.Identifier))
                    {
                        acceptedEdges.Add(edge);
                        changed = true;
                    }
                }
                change.edgesToCreate = acceptedEdges;
            }

            if (changed)
                MarkChanged();
            return change;
        }

        private void OnNodePositionChanged(SWBehaviourNodeView view, Rect position)
        {
            if (isReloading || treeAsset == null)
                return;
            Undo.RecordObject(treeAsset, "Behaviour 노드 이동");
            view.Data.Position = position;
            treeAsset.SortChildrenByPosition();
            EditorUtility.SetDirty(treeAsset);
        }

        private void MarkChanged()
        {
            EditorUtility.SetDirty(treeAsset);
            graphChanged?.Invoke();
        }

        private void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (treeAsset == null)
                return;
            if (keyEvent.actionKey && keyEvent.keyCode == KeyCode.C) CopySelection();
            else if (keyEvent.actionKey && keyEvent.keyCode == KeyCode.V) PasteAtPointer();
            else if (keyEvent.actionKey && keyEvent.keyCode == KeyCode.D) DuplicateSelection();
            else if (keyEvent.keyCode == KeyCode.A) FrameAll();
            else if (keyEvent.keyCode == KeyCode.O) UpdateViewTransform(Vector3.zero, Vector3.one);
            else if (keyEvent.keyCode == KeyCode.LeftBracket) SelectParent();
            else if (keyEvent.keyCode == KeyCode.RightBracket) SelectFirstChild();
            else if (keyEvent.keyCode == KeyCode.Space) OpenSearchAtPointer();
            else return;
            keyEvent.StopPropagation();
        }

        private void OpenSearchAtPointer()
        {
            Vector2 panelPosition = lastPointerPanelPosition;
            if (!worldBound.Contains(panelPosition))
                panelPosition = worldBound.center;
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(panelPosition);
            OpenSearch(screenPosition);
        }

        private void OnMouseDown(MouseDownEvent mouseEvent)
        {
            if (!mouseEvent.actionKey || mouseEvent.button != 0 || mouseEvent.clickCount != 2)
                return;
            VisualElement element = mouseEvent.target as VisualElement;
            while (element != null && element != this)
            {
                if (element is SWBehaviourNodeView view)
                {
                    ClearSelection();
                    AddToSelection(view);
                    SelectSubTree();
                    mouseEvent.StopPropagation();
                    return;
                }
                element = element.parent;
            }
        }

        private void PasteAt(Vector2 origin)
        {
            if (clipboard == null || treeAsset == null)
                return;
            Undo.RecordObject(treeAsset, "Behaviour 노드 붙여넣기");
            List<SWBehaviourNode> createdNodes = new();
            ClearSelection();
            for (int index = 0; index < clipboard.Nodes.Count; index++)
            {
                SWBehaviourNodeClipboardData source = clipboard.Nodes[index];
                Type nodeType = Type.GetType(source.TypeName);
                SWBehaviourNode node = treeAsset.AddNodeCopy(
                    nodeType,
                    source.SerializedNode,
                    origin + source.PositionOffset);
                createdNodes.Add(node);
                if (node == null)
                    continue;
                AddNodeView(node);
                AddToSelection(nodeViews[node.Identifier]);
            }
            for (int index = 0; index < clipboard.Connections.Count; index++)
            {
                Vector2Int connection = clipboard.Connections[index];
                if (connection.x >= createdNodes.Count || connection.y >= createdNodes.Count ||
                    createdNodes[connection.x] == null || createdNodes[connection.y] == null)
                    continue;
                if (treeAsset.Connect(
                    createdNodes[connection.x].Identifier,
                    createdNodes[connection.y].Identifier))
                {
                    AddEdge(
                        createdNodes[connection.x].Identifier,
                        createdNodes[connection.y].Identifier);
                }
            }
            MarkChanged();
        }

        private void CollectSubTree(SWBehaviourNode node, HashSet<string> identifiers)
        {
            if (node == null || !identifiers.Add(node.Identifier))
                return;
            for (int index = 0; index < node.ChildIdentifiers.Count; index++)
            {
                if (treeAsset.TryGetNode(node.ChildIdentifiers[index], out SWBehaviourNode child))
                    CollectSubTree(child, identifiers);
            }
        }

        private void SelectParent()
        {
            SWBehaviourNodeView selectedView = selection.OfType<SWBehaviourNodeView>().FirstOrDefault();
            if (selectedView == null) return;
            foreach (SWBehaviourNode node in treeAsset.Nodes)
            {
                if (node.ChildIdentifiers.Contains(selectedView.Data.Identifier))
                {
                    ClearSelection();
                    AddToSelection(nodeViews[node.Identifier]);
                    FrameSelection();
                    return;
                }
            }
        }

        private void SelectFirstChild()
        {
            SWBehaviourNodeView selectedView = selection.OfType<SWBehaviourNodeView>().FirstOrDefault();
            if (selectedView == null || selectedView.Data.ChildIdentifiers.Count == 0) return;
            string identifier = selectedView.Data.ChildIdentifiers[0];
            if (!nodeViews.TryGetValue(identifier, out SWBehaviourNodeView childView)) return;
            ClearSelection();
            AddToSelection(childView);
            FrameSelection();
        }

        private float CalculateSubtreeWidth(
            SWBehaviourNode node,
            Dictionary<string, float> widths,
            HashSet<string> visited)
        {
            float nodeWidth = SWBehaviourTreeEditorSettings.instance.NodeWidth;
            float horizontalSpacing = SWBehaviourTreeEditorSettings.instance.HorizontalSpacing;
            if (!visited.Add(node.Identifier)) return nodeWidth;
            float width = 0f;
            for (int index = 0; index < node.ChildIdentifiers.Count; index++)
            {
                if (treeAsset.TryGetNode(node.ChildIdentifiers[index], out SWBehaviourNode child))
                    width += CalculateSubtreeWidth(child, widths, visited) +
                        horizontalSpacing;
            }
            width = Mathf.Max(nodeWidth, width - (width > 0f ? horizontalSpacing : 0f));
            widths[node.Identifier] = width;
            return width;
        }

        private void LayoutSubtree(
            SWBehaviourNode node,
            float centerX,
            float y,
            Dictionary<string, float> widths,
            HashSet<string> visited)
        {
            if (!visited.Add(node.Identifier)) return;
            float nodeWidth = SWBehaviourTreeEditorSettings.instance.NodeWidth;
            float horizontalSpacing = SWBehaviourTreeEditorSettings.instance.HorizontalSpacing;
            Rect position = node.Position;
            position.position = new Vector2(centerX - position.width * 0.5f, y);
            node.Position = position;
            if (nodeViews.TryGetValue(node.Identifier, out SWBehaviourNodeView view))
                view.SetPosition(position);
            float childrenWidth = 0f;
            for (int index = 0; index < node.ChildIdentifiers.Count; index++)
                childrenWidth += widths.GetValueOrDefault(node.ChildIdentifiers[index], nodeWidth) +
                    horizontalSpacing;
            childrenWidth -= node.ChildIdentifiers.Count > 0 ? horizontalSpacing : 0f;
            float childX = centerX - childrenWidth * 0.5f;
            for (int index = 0; index < node.ChildIdentifiers.Count; index++)
            {
                string identifier = node.ChildIdentifiers[index];
                if (!treeAsset.TryGetNode(identifier, out SWBehaviourNode child)) continue;
                float width = widths.GetValueOrDefault(identifier, nodeWidth);
                LayoutSubtree(child, childX + width * 0.5f,
                    y + SWBehaviourTreeEditorSettings.instance.VerticalSpacing,
                    widths, visited);
                childX += width + horizontalSpacing;
            }
        }

        private sealed class SWBehaviourGraphClipboard
        {
            public Vector2 Origin;
            public readonly List<SWBehaviourNodeClipboardData> Nodes = new();
            public readonly List<Vector2Int> Connections = new();
        }

        private sealed class SWBehaviourNodeClipboardData
        {
            public string TypeName;
            public string SerializedNode;
            public Vector2 PositionOffset;
        }
    }
}
