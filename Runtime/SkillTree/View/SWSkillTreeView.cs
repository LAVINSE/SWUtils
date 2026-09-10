using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SW.Attributes;
using SW.Base;

namespace SW.SkillTree
{
    /// <summary>실제 게임에서 스킬 노드, 연결선과 구매 상세 패널을 표시합니다. 화면은 실행 시스템을 소유하지 않습니다.</summary>
    public sealed partial class SWSkillTreeView : SWMonoBehaviour
    {
        private sealed class Connection
        {
            internal Image image;
            internal SWSkillTreeNode child;
            internal SWSkillTreeRequirement requirement;
        }
        [SerializeField] private SWSkillTreeNodeView nodePrefab;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform connectionsRoot;
        [SerializeField] private RectTransform nodesRoot;
        [SerializeField] private SWSkillTreeDetailsView details;
        [SerializeField] private TextMeshProUGUI heading;
        [SerializeField] private Button fitButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button selectionButton;
        [SWGroup("노드 배치")]
        [SerializeField, InspectorName("노드 크기")] private Vector2 nodeSize = new(180, 92);
        [SerializeField, Tooltip("저장 위치를 우선 사용하고 저장 위치가 없는 노드만 자동 배치합니다.")]
        private bool automaticLayout;
        [SerializeField] private Vector2 nodeSpacing = new(60, 80);
        [SWGroup("화면 탐색")]
        [SerializeField, Tooltip("실행 시 전체 축소 대신 시작 노드에서 탐색을 시작합니다.")]
        private bool focusStartOnBind;
        [SerializeField, Range(0.08f, 2f)] private float initialZoom = 1f;
        [SWGroup("편집 미리보기")]
        [SerializeField, InspectorName("전체 노드 보기"), Tooltip("편집 모드에서 숨김 조건과 관계없이 모든 노드와 연결선을 표시합니다. 실제 게임에는 적용하지 않습니다.")]
        private bool showAllNodesInEditor;
        private readonly Dictionary<string, SWSkillTreeNodeView> views = new(StringComparer.Ordinal);
        private readonly List<Connection> connections = new();
        private SWSkillTreeSystem system;
        private string selectedIdentifier;
        private string feedback;
        private bool dirty;
        private bool fitPending;
        private IReadOnlyDictionary<string, Vector2> positions;
        [SerializeField, HideInInspector] private SWSkillTreeDefinition displayedDefinition;
        private bool ShowAllForEditing => !Application.isPlaying && showAllNodesInEditor;

        /// <summary>현재 연결된 플레이어의 실행 시스템입니다.</summary>
        public SWSkillTreeSystem System => system;
        /// <summary>배치와 생성에 사용하는 노드 크기입니다.</summary>
        public Vector2 NodeSize => nodeSize;
        /// <summary>게임이나 편집기에서 기본 화면 구성을 생성합니다. 기존에 연결한 화면은 유지합니다.</summary>
        public void BuildDefaultLayout()
        {
            if (viewport != null)
            {
                SWSkillTreeViewFactory.RestoreMissingFonts(transform);
                BuildNavigationControls();
                return;
            }
            Image background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.color = SWSkillTreeViewFactory.Background;
            heading = SWSkillTreeViewFactory.CreateText(transform, "Heading", "Skill Tree", 20, SWSkillTreeViewFactory.Text);
            heading.fontStyle = FontStyles.Bold;
            SWSkillTreeViewFactory.PlaceFromTop(heading.rectTransform, 16, 30, 20, 360);
            TextMeshProUGUI help = SWSkillTreeViewFactory.CreateText(transform, "NavigationHelp", "Drag to pan - Scroll to zoom - Select a node to buy", 12, SWSkillTreeViewFactory.MutedText);
            SWSkillTreeViewFactory.PlaceFromTop(help.rectTransform, 48, 22, 20, 20);
            BuildNavigationControls();
            viewport = SWSkillTreeViewFactory.CreateRect("TreeViewport", transform);
            SWSkillTreeViewFactory.Stretch(viewport, 16, 16, 336, 84);
            Image viewportBackground = viewport.gameObject.AddComponent<Image>();
            viewportBackground.color = SWSkillTreeViewFactory.Background;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = SWSkillTreeViewFactory.CreateRect("TreeContent", viewport);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = Vector2.zero;
            connectionsRoot = SWSkillTreeViewFactory.CreateRect("Connections", content);
            nodesRoot = SWSkillTreeViewFactory.CreateRect("Nodes", content);
            connectionsRoot.sizeDelta = nodesRoot.sizeDelta = Vector2.zero;
            viewport.gameObject.AddComponent<SWSkillTreeViewport>().Initialize(content);
            RectTransform detailRectangle = SWSkillTreeViewFactory.CreateRect("Details", transform);
            detailRectangle.anchorMin = new Vector2(1, 0);
            detailRectangle.anchorMax = Vector2.one;
            detailRectangle.offsetMin = new Vector2(-320, 16);
            detailRectangle.offsetMax = new Vector2(-16, -84);
            details = detailRectangle.gameObject.AddComponent<SWSkillTreeDetailsView>();
            details.Build();
            ConnectControls();
        }

        /// <summary>플레이어의 진행과 화면을 연결합니다.</summary>
        public void Bind(SWSkillTreeSystem value)
        {
            if (system != null) system.Changed -= MarkDirty;
            system = value;
            if (value != null) displayedDefinition = value.Definition;
            BuildDefaultLayout();
            ConnectControls();
            if (system != null && isActiveAndEnabled) system.Changed += MarkDirty;
            Rebuild();
        }

        private void ConnectControls()
        {
            fitButton.onClick.RemoveListener(FitAll);
            fitButton.onClick.AddListener(FitAll);
            startButton.onClick.RemoveListener(FocusStart);
            startButton.onClick.AddListener(FocusStart);
            selectionButton.onClick.RemoveListener(FocusSelection);
            selectionButton.onClick.AddListener(FocusSelection);
            details.BindActions(PurchaseSelected, RefundSelected);
        }

        private void BuildNavigationControls()
        {
            if (fitButton == null) fitButton = SWSkillTreeViewFactory.CreateButton(transform, "Fit", "Fit All");
            if (selectionButton == null) selectionButton = SWSkillTreeViewFactory.CreateButton(transform, "FocusSelection", "Selected");
            if (startButton == null) startButton = SWSkillTreeViewFactory.CreateButton(transform, "FocusStart", "Start");
            Button[] buttons = { fitButton, selectionButton, startButton };
            for (int index = 0; index < buttons.Length; index++)
            {
                RectTransform rectangle = (RectTransform)buttons[index].transform;
                rectangle.anchorMin = rectangle.anchorMax = rectangle.pivot = Vector2.one;
                rectangle.anchoredPosition = new Vector2(-20 - index * 108, -20);
                rectangle.sizeDelta = new Vector2(100, 32);
            }
        }

        /// <summary>첫 공개 노드를 읽기 좋은 배율로 화면 가운데에 배치합니다.</summary>
        public void FocusStart()
        {
            SWSkillTreeDefinition definition = system?.Definition ?? displayedDefinition;
            if (definition == null) return;
            string identifier = definition.Nodes.FirstOrDefault(node => system == null || system.IsRevealed(node.Identifier))?.Identifier;
            FocusNode(identifier);
        }

        /// <summary>현재 선택한 노드로 화면을 이동합니다.</summary>
        public void FocusSelection() => FocusNode(selectedIdentifier);

        private void FocusNode(string identifier)
        {
            fitPending = false;
            if (content == null || nodesRoot == null || string.IsNullOrEmpty(identifier)) return;
            foreach (SWSkillTreeNodeView node in nodesRoot.GetComponentsInChildren<SWSkillTreeNodeView>(true))
            {
                if (node.NodeIdentifier != identifier || !node.gameObject.activeSelf) continue;
                RectTransform rectangle = (RectTransform)node.transform;
                Vector2 center = SWSkillTreeViewGeometry.GetRelativeMatrix(rectangle, content).MultiplyPoint3x4(rectangle.rect.center);
                float scale = Mathf.Clamp(initialZoom, SWSkillTreeViewport.MinimumZoom, SWSkillTreeViewport.MaximumZoom);
                content.localScale = Vector3.one * scale;
                content.anchoredPosition = -center * scale;
                fitPending = false;
                return;
            }
        }

        /// <summary>자동 배치 설정 또는 정의에 저장한 좌표로 노드와 연결선을 다시 생성합니다.</summary>
        public void Rebuild()
        {
            ClearGeneratedChildren(nodesRoot);
            ClearGeneratedChildren(connectionsRoot);
            views.Clear();
            connections.Clear();
            if (system == null) { dirty = true; return; }
            positions = automaticLayout ? SWSkillTreeAutoLayout.Resolve(system.Definition, nodeSize, nodeSpacing)
                : system.Definition.Nodes.ToDictionary(node => node.Identifier, node => new Vector2(node.Position.x, -node.Position.y));
            heading.text = system.Definition.name;
            foreach (SWSkillTreeNode node in system.Definition.Nodes)
            {
                SWSkillTreeNodeView view = nodePrefab != null ? Instantiate(nodePrefab, nodesRoot)
                    : SWSkillTreeViewFactory.CreateRect(node.Skill.DisplayName, nodesRoot).gameObject.AddComponent<SWSkillTreeNodeView>();
                view.gameObject.SetActive(true);
                view.Build();
                RectTransform rectangle = (RectTransform)view.transform;
                rectangle.anchoredPosition = positions[node.Identifier];
                rectangle.sizeDelta = nodeSize;
                view.BindIdentifier(node.Identifier);
                view.BindSelection(() => SelectNode(node.Identifier));
                views.Add(node.Identifier, view);
            }
            foreach (SWSkillTreeNode node in system.Definition.Nodes)
            {
                foreach (SWSkillTreeRequirement requirement in node.Requirements)
                {
                    system.TryGetNode(requirement.NodeIdentifier, out SWSkillTreeNode parent);
                    Image line = SWSkillTreeViewFactory.CreateRect("Connection", connectionsRoot).gameObject.AddComponent<Image>();
                    line.raycastTarget = false;
                    line.gameObject.AddComponent<SWSkillTreeConnectionView>().Initialize(
                        (RectTransform)views[parent.Identifier].transform, (RectTransform)views[node.Identifier].transform);
                    connections.Add(new Connection { image = line, child = node, requirement = requirement });
                }
            }
            selectedIdentifier = system.Definition.Nodes.FirstOrDefault(node => system.IsRevealed(node.Identifier))?.Identifier;
            feedback = string.Empty;
            fitPending = dirty = true;
        }

        /// <summary>자동 배치를 활성화하고 현재 노드와 연결선을 다시 생성합니다.</summary>
        public void ArrangeAutomatically()
        {
            automaticLayout = true;
            Rebuild();
            Canvas.ForceUpdateCanvases();
            RefreshView();
        }

        /// <summary>식별자로 노드를 선택해 상세 패널에 표시합니다.</summary>
        public void SelectNode(string identifier)
        {
            if (system == null || !system.IsRevealed(identifier)) return;
            selectedIdentifier = identifier;
            feedback = string.Empty;
            dirty = true;
        }
        /// <summary>선택한 노드를 구매하고 처리 결과를 상세 패널에 표시합니다.</summary>
        public void PurchaseSelected(int levels, bool maximum)
        {
            if (system == null) return;
            SWSkillTreePurchase result = system.Purchase(selectedIdentifier, levels, maximum);
            feedback = result.Success ? $"Purchased {result.Levels} levels. {result.Reason}" : result.Reason;
            dirty = true;
        }
        /// <summary>선택한 노드의 마지막 레벨을 환불합니다.</summary>
        public void RefundSelected()
        {
            if (system == null) return;
            feedback = system.Refund(selectedIdentifier, out string reason) ? "Refunded the last level." : reason;
            dirty = true;
        }
        /// <summary>공개된 노드를 작업 영역에 맞추고 가운데에 배치합니다.</summary>
        public void FitAll()
        {
            if (nodesRoot == null || content == null || viewport == null) return;
            if (viewport.rect.width <= 0 || viewport.rect.height <= 0) return;
            Vector2 minimum = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new(float.NegativeInfinity, float.NegativeInfinity);
            Vector3[] corners = new Vector3[4];
            foreach (SWSkillTreeNodeView node in nodesRoot.GetComponentsInChildren<SWSkillTreeNodeView>(true))
            {
                if (!node.gameObject.activeSelf) continue;
                ((RectTransform)node.transform).GetLocalCorners(corners);
                Matrix4x4 matrix = SWSkillTreeViewGeometry.GetRelativeMatrix(node.transform, content);
                foreach (Vector3 corner in corners)
                {
                    Vector2 point = matrix.MultiplyPoint3x4(corner);
                    minimum = Vector2.Min(minimum, point);
                    maximum = Vector2.Max(maximum, point);
                }
            }
            if (float.IsInfinity(minimum.x)) { fitPending = false; return; }
            Vector2 size = maximum - minimum + Vector2.one * 60;
            float scale = Mathf.Clamp(Mathf.Min(viewport.rect.width / size.x, viewport.rect.height / size.y), SWSkillTreeViewport.MinimumZoom, 1f);
            content.localScale = Vector3.one * scale;
            content.anchoredPosition = -(minimum + maximum) * 0.5f * scale;
            fitPending = false;
        }
        private void LateUpdate() => RefreshView();

        /// <summary>보류 중인 표시 변경을 즉시 적용합니다. 편집기의 예제 미리보기 생성에도 사용합니다.</summary>
        public void RefreshView()
        {
            if (!dirty || details == null)
            {
                if (fitPending) ApplyInitialFocus();
                return;
            }
            dirty = false;
            SWSkillTreeNode selected = null;
            if (system != null)
            {
                foreach (SWSkillTreeNode node in system.Definition.Nodes)
                {
                    bool visible = ShowAllForEditing || system.IsVisible(node.Identifier);
                    views[node.Identifier].gameObject.SetActive(visible);
                    if (!visible) continue;
                    if (ShowAllForEditing || system.IsRevealed(node.Identifier)) views[node.Identifier].Render(node, system.GetLevel(node.Identifier),
                        system.PreviewPurchase(node.Identifier).Success, node.Identifier == selectedIdentifier);
                    else views[node.Identifier].RenderMasked();
                }
                foreach (Connection connection in connections)
                {
                    connection.image.gameObject.SetActive(ShowAllForEditing || (system.IsVisible(connection.child.Identifier) && system.IsVisible(connection.requirement.NodeIdentifier)));
                    connection.image.color = system.GetLevel(connection.requirement.NodeIdentifier) >= connection.requirement.RequiredLevel ? SWSkillTreeViewFactory.Learned : SWSkillTreeViewFactory.Border;
                }
                if (system.IsRevealed(selectedIdentifier)) system.TryGetNode(selectedIdentifier, out selected);
            }
            details.Render(system, selected, feedback);
            if (fitPending) ApplyInitialFocus();
        }
        private void ApplyInitialFocus()
        {
            if (focusStartOnBind) FocusStart();
            else FitAll();
        }
        private void MarkDirty() => dirty = true;
        private void OnRectTransformDimensionsChange() { if (system != null && !focusStartOnBind) fitPending = true; }
        private void OnEnable()
        {
            if (system != null) { system.Changed -= MarkDirty; system.Changed += MarkDirty; }
            dirty = true;
#if UNITY_EDITOR
            QueueEditorPreview();
#endif
        }
        private void OnDisable()
        {
            if (system != null) system.Changed -= MarkDirty;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RefreshEditorPreview;
#endif
        }
        private void OnDestroy()
        {
            if (system != null) system.Changed -= MarkDirty;
        }
        private static void RemoveObject(UnityEngine.Object value)
        {
            if (value is GameObject gameObject) gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private static void ClearGeneratedChildren(Transform parent)
        {
            if (parent == null) return;
            for (int index = parent.childCount - 1; index >= 0; index--) RemoveObject(parent.GetChild(index).gameObject);
        }
    }
}
