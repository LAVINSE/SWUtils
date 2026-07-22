using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// Shader Graph 편집기 계열의 색상, 간격과 공통 크기를 상태 머신 그래프에 적용합니다.
    /// </summary>
    internal static class SWStateMachineGraphStyles
    {
        #region 색상
        private static readonly Color WindowBackground = new Color(0.075f, 0.08f, 0.09f);
        private static readonly Color PanelBackground = new Color(0.149f, 0.157f, 0.173f);
        private static readonly Color CardBackground = new Color(0.157f, 0.165f, 0.18f);
        private static readonly Color BorderColor = new Color(0.282f, 0.298f, 0.322f);
        private static readonly Color MutedTextColor = new Color(0.62f, 0.65f, 0.68f);
        private static readonly Color PrimaryColor = new Color(0.22f, 0.58f, 0.92f);
        private static readonly Color StateColor = new Color(0.25f, 0.67f, 0.48f);
        private static readonly Color AnyStateColor = new Color(0.68f, 0.45f, 0.88f);
        private static readonly Color ReturnColor = new Color(0.29f, 0.68f, 0.82f);
        #endregion // 색상

        #region 창
        /// <summary>편집기 창의 전체 배경과 기본 글자색을 적용합니다.</summary>
        public static void ApplyWindow(VisualElement root)
        {
            root.style.backgroundColor = WindowBackground;
            root.style.color = new StyleColor(Color.white);
        }

        /// <summary>상단 도구 모음의 크기와 구분선을 적용합니다.</summary>
        public static void ApplyToolbar(VisualElement toolbar)
        {
            toolbar.style.height = 34f;
            toolbar.style.flexShrink = 0f;
            toolbar.style.paddingLeft = 8f;
            toolbar.style.paddingRight = 8f;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.backgroundColor = PanelBackground;
            toolbar.style.borderBottomWidth = 1f;
            toolbar.style.borderBottomColor = BorderColor;
        }

        /// <summary>도구 모음의 주요 작업 버튼을 강조합니다.</summary>
        public static void ApplyPrimaryButton(Button button)
        {
            button.style.height = 24f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.backgroundColor = PrimaryColor;
            button.style.color = Color.white;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetRoundedCorners(button, 4f);
        }

        /// <summary>그래프 인스펙터 탭의 선택 상태를 일관된 크기와 색상으로 표시합니다.</summary>
        public static void ApplyTabButton(Button button, bool isSelected)
        {
            button.style.height = 26f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.backgroundColor = isSelected
                ? new Color(0.22f, 0.25f, 0.29f)
                : new Color(0.12f, 0.125f, 0.135f);
            button.style.borderBottomWidth = isSelected ? 2f : 1f;
            button.style.borderBottomColor = isSelected ? PrimaryColor : BorderColor;
        }

        /// <summary>Shader Graph처럼 그래프 위에 떠 있는 패널 스타일을 적용합니다.</summary>
        public static void ApplyFloatingPanel(VisualElement panel)
        {
            panel.style.position = Position.Absolute;
            panel.style.backgroundColor = new Color(0.114f, 0.122f, 0.133f, 0.98f);
            panel.style.paddingLeft = 10f;
            panel.style.paddingRight = 10f;
            panel.style.paddingTop = 10f;
            panel.style.paddingBottom = 10f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopColor = BorderColor;
            panel.style.borderBottomColor = BorderColor;
            panel.style.borderLeftColor = BorderColor;
            panel.style.borderRightColor = BorderColor;
            SetRoundedCorners(panel, 5f);
        }

        /// <summary>패널의 큰 제목을 그래프 인스펙터 형식으로 표시합니다.</summary>
        public static Label CreatePanelTitle(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 15f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 8f;
            return label;
        }

        /// <summary>설정 항목을 묶는 카드 컨테이너를 생성합니다.</summary>
        public static VisualElement CreateCard(string title)
        {
            VisualElement card = new VisualElement();
            card.style.backgroundColor = CardBackground;
            card.style.marginTop = 5f;
            card.style.marginBottom = 5f;
            card.style.paddingLeft = 10f;
            card.style.paddingRight = 10f;
            card.style.paddingTop = 9f;
            card.style.paddingBottom = 9f;
            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderTopColor = BorderColor;
            card.style.borderBottomColor = BorderColor;
            card.style.borderLeftColor = BorderColor;
            card.style.borderRightColor = BorderColor;
            SetRoundedCorners(card, 5f);

            if (!string.IsNullOrWhiteSpace(title))
            {
                Label titleLabel = new Label(title);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.marginBottom = 6f;
                card.Add(titleLabel);
            }

            return card;
        }

        /// <summary>설명과 비활성 정보에 사용할 보조 글자색을 적용합니다.</summary>
        public static void ApplyMutedText(Label label)
        {
            label.style.color = MutedTextColor;
            label.style.whiteSpace = WhiteSpace.Normal;
        }
        #endregion // 창

        #region 그래프
        /// <summary>그래프 작업 영역에 어두운 배경과 확대 범위를 적용합니다.</summary>
        public static void ApplyGraphView(GraphView graphView)
        {
            graphView.style.backgroundColor = WindowBackground;
            graphView.SetupZoom(0.2f, 2f);
        }

        /// <summary>상태 노드를 종류별 카드 형태로 꾸밉니다.</summary>
        public static void ApplyNode(SWStateMachineNodeView node, float nodeWidth)
        {
            Color accentColor = node.Data.Kind switch
            {
                SW.StateMachine.SWStateMachineNodeKind.AnyState => AnyStateColor,
                SW.StateMachine.SWStateMachineNodeKind.Return => ReturnColor,
                _ => StateColor,
            };

            node.style.width = nodeWidth;
            node.style.minWidth = nodeWidth;
            node.style.maxWidth = nodeWidth;
            node.style.backgroundColor = CardBackground;
            node.style.borderTopWidth = 3f;
            node.style.borderBottomWidth = 1f;
            node.style.borderLeftWidth = 1f;
            node.style.borderRightWidth = 1f;
            node.style.borderTopColor = accentColor;
            node.style.borderBottomColor = BorderColor;
            node.style.borderLeftColor = BorderColor;
            node.style.borderRightColor = BorderColor;
            SetRoundedCorners(node, 5f);

            VisualElement titleContainer = node.Q<VisualElement>("title");
            if (titleContainer != null)
            {
                titleContainer.style.minHeight = 34f;
                titleContainer.style.height = 34f;
                titleContainer.style.backgroundColor = new Color(
                    accentColor.r * 0.32f,
                    accentColor.g * 0.32f,
                    accentColor.b * 0.32f,
                    1f);
                titleContainer.style.borderBottomWidth = 1f;
                titleContainer.style.borderBottomColor = accentColor;
            }

            VisualElement marker = node.Q<VisualElement>(className: "sw-state-marker");
            if (marker != null)
            {
                marker.style.width = 5f;
                marker.style.height = 18f;
                marker.style.marginRight = 6f;
                marker.style.backgroundColor = accentColor;
                SetRoundedCorners(marker, 2f);
            }

            Label typeLabel = node.Q<Label>(className: "sw-node-type");
            if (typeLabel != null)
            {
                typeLabel.style.color = MutedTextColor;
                typeLabel.style.marginLeft = 8f;
                typeLabel.style.marginRight = 8f;
                typeLabel.style.marginTop = 6f;
                typeLabel.style.marginBottom = 7f;
                typeLabel.style.height = 20f;
            }

            VisualElement badgeContainer = node.Q<VisualElement>(className: "sw-node-badges");
            if (badgeContainer != null)
            {
                badgeContainer.style.flexDirection = FlexDirection.Row;
                badgeContainer.style.marginLeft = 5f;
            }

            foreach (Label badge in node.Query<Label>(className: "sw-node-badge").ToList())
            {
                badge.style.fontSize = 9f;
                badge.style.marginLeft = 3f;
                badge.style.paddingLeft = 5f;
                badge.style.paddingRight = 5f;
                badge.style.paddingTop = 2f;
                badge.style.paddingBottom = 2f;
                badge.style.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 0.8f);
                SetRoundedCorners(badge, 6f);
            }
        }

        /// <summary>전이 연결선의 정보 표식을 카드 형태로 꾸밉니다.</summary>
        public static void ApplyEdge(SWStateMachineEdgeView edge)
        {
            Label label = edge.Q<Label>(className: "sw-transition-label");
            if (label == null)
                return;

            label.style.position = Position.Absolute;
            label.style.fontSize = 9f;
            label.style.paddingLeft = 6f;
            label.style.paddingRight = 6f;
            label.style.paddingTop = 3f;
            label.style.paddingBottom = 3f;
            label.style.backgroundColor = new Color(0.11f, 0.12f, 0.13f, 0.95f);
            label.style.borderTopWidth = 1f;
            label.style.borderBottomWidth = 1f;
            label.style.borderLeftWidth = 1f;
            label.style.borderRightWidth = 1f;
            label.style.borderTopColor = BorderColor;
            label.style.borderBottomColor = BorderColor;
            label.style.borderLeftColor = BorderColor;
            label.style.borderRightColor = BorderColor;
            SetRoundedCorners(label, 7f);
        }
        #endregion // 그래프

        #region 공통
        /// <summary>시각 요소의 네 모서리 둥글기를 동일하게 설정합니다.</summary>
        private static void SetRoundedCorners(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
        #endregion // 공통
    }
}
