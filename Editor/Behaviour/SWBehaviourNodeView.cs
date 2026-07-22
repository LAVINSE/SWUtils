using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using SW.BehaviourTree;

namespace SW.EditorTools.Behaviour
{
    /// <summary>Behaviour 노드와 입출력 포트, 설명과 실행 결과를 표시합니다.</summary>
    internal sealed class SWBehaviourNodeView : Node
    {
        private readonly Action<SWBehaviourNodeView> selectionCallback;
        private readonly Action<SWBehaviourNodeView, Rect> positionChanged;
        private readonly Label descriptionLabel;
        private readonly Label statusLabel;
        private readonly Label rootLabel;
        private bool isReady;

        public SWBehaviourNode Data { get; }
        public Port InputPort { get; }
        public Port OutputPort { get; }

        public SWBehaviourNodeView(
            SWBehaviourNode data,
            Action<SWBehaviourNodeView> selected,
            Action<SWBehaviourNodeView, Rect> positionChanged)
        {
            Data = data;
            AddToClassList("sw-behaviour-node");
            AddToClassList(data is SWBehaviourCompositeNode
                ? "sw-behaviour-node--composite"
                : data is SWBehaviourDecoratorNode
                    ? "sw-behaviour-node--decorator"
                    : "sw-behaviour-node--action");
            selectionCallback = selected;
            this.positionChanged = positionChanged;
            viewDataKey = data.Identifier;
            title = GetTitle(data);

            InputPort = InstantiatePort(
                Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            if (data.MaximumChildCount != 0)
            {
                OutputPort = InstantiatePort(
                    Orientation.Vertical, Direction.Output,
                    data.MaximumChildCount == 1 ? Port.Capacity.Single : Port.Capacity.Multi,
                    typeof(bool));
                OutputPort.portName = "Out";
                outputContainer.Add(OutputPort);
            }

            descriptionLabel = new Label(data.Description);
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            descriptionLabel.style.marginLeft = 8f;
            descriptionLabel.style.marginRight = 8f;
            descriptionLabel.style.marginBottom = 6f;
            extensionContainer.Add(descriptionLabel);

            statusLabel = new Label();
            statusLabel.style.position = Position.Absolute;
            statusLabel.style.right = 6f;
            statusLabel.style.top = 6f;
            statusLabel.style.fontSize = 10f;
            titleContainer.Add(statusLabel);

            rootLabel = new Label("ROOT");
            rootLabel.style.position = Position.Absolute;
            rootLabel.style.left = 7f;
            rootLabel.style.top = 6f;
            rootLabel.style.fontSize = 9f;
            rootLabel.style.color = new Color(0.35f, 0.75f, 1f);
            rootLabel.style.display = DisplayStyle.None;
            titleContainer.Add(rootLabel);

            SWBehaviourTreeEditorSettings settings = SWBehaviourTreeEditorSettings.instance;
            style.width = settings.NodeWidth;
            descriptionLabel.style.display = settings.ShowDescriptions
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            style.borderTopWidth = 3f;
            style.borderTopColor = GetCategoryColor(data);
            base.SetPosition(data.Position);
            RefreshExpandedState();
            RefreshPorts();
            RefreshVisuals();
            isReady = true;
        }

        public void RefreshVisuals()
        {
            title = GetTitle(Data);
            descriptionLabel.text = Data.Description;
            statusLabel.text = Data.Status == SWBehaviourStatus.Inactive
                ? string.Empty
                : Data.Status.ToString();
            statusLabel.style.color = Data.Status switch
            {
                SWBehaviourStatus.Running => new Color(1f, 0.72f, 0.18f),
                SWBehaviourStatus.Success => new Color(0.25f, 0.85f, 0.42f),
                SWBehaviourStatus.Failure => new Color(0.95f, 0.30f, 0.30f),
                SWBehaviourStatus.Aborted => new Color(0.65f, 0.67f, 0.70f),
                _ => Color.gray,
            };
        }

        public override void SetPosition(Rect newPosition)
        {
            base.SetPosition(newPosition);
            if (isReady)
                positionChanged?.Invoke(this, newPosition);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            selectionCallback?.Invoke(this);
        }

        /// <summary>지정한 런타임 실행 결과를 노드에 표시합니다.</summary>
        public void RefreshStatus(SWBehaviourStatus runtimeStatus)
        {
            RemoveFromClassList("sw-behaviour-node--running");
            RemoveFromClassList("sw-behaviour-node--success");
            RemoveFromClassList("sw-behaviour-node--failure");
            RemoveFromClassList("sw-behaviour-node--aborted");
            string statusClass = runtimeStatus switch
            {
                SWBehaviourStatus.Running => "sw-behaviour-node--running",
                SWBehaviourStatus.Success => "sw-behaviour-node--success",
                SWBehaviourStatus.Failure => "sw-behaviour-node--failure",
                SWBehaviourStatus.Aborted => "sw-behaviour-node--aborted",
                _ => string.Empty,
            };
            if (!string.IsNullOrEmpty(statusClass))
                AddToClassList(statusClass);
            statusLabel.text = runtimeStatus == SWBehaviourStatus.Inactive
                ? string.Empty
                : runtimeStatus.ToString();
            statusLabel.style.color = runtimeStatus switch
            {
                SWBehaviourStatus.Running => new Color(1f, 0.72f, 0.18f),
                SWBehaviourStatus.Success => new Color(0.25f, 0.85f, 0.42f),
                SWBehaviourStatus.Failure => new Color(0.95f, 0.30f, 0.30f),
                SWBehaviourStatus.Aborted => new Color(0.65f, 0.67f, 0.70f),
                _ => Color.gray,
            };
        }

        /// <summary>노드가 Behaviour Tree의 Root인지 표시합니다.</summary>
        public void SetRoot(bool isRoot)
        {
            rootLabel.style.display = isRoot ? DisplayStyle.Flex : DisplayStyle.None;
            InputPort.SetEnabled(!isRoot);
            InputPort.tooltip = isRoot
                ? "Root Node에는 입력 연결을 만들 수 없습니다."
                : string.Empty;
        }

        private static string GetTitle(SWBehaviourNode node)
        {
            return string.IsNullOrWhiteSpace(node.DisplayName)
                ? node.GetType().Name
                : node.DisplayName;
        }

        private static Color GetCategoryColor(SWBehaviourNode node)
        {
            if (node is SWBehaviourCompositeNode) return new Color(0.25f, 0.55f, 0.95f);
            if (node is SWBehaviourDecoratorNode) return new Color(0.68f, 0.38f, 0.90f);
            return new Color(0.25f, 0.75f, 0.48f);
        }
    }
}
