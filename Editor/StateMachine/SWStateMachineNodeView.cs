using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프에서 상태 데이터와 연결 포트를 표시하는 노드입니다.
    /// </summary>
    internal sealed class SWStateMachineNodeView : Node
    {
        #region 상수
        private const string InitialBadgeText = "시작";
        #endregion // 상수

        #region 필드
        private readonly Action<SWStateMachineNodeView> selectionCallback;
        private readonly Action<SWStateMachineNodeView, Rect> positionChanged;
        private readonly Label typeLabel;
        private readonly Label descriptionLabel;
        private readonly Label runtimeBadge;
        private readonly Label layerBadge;
        private readonly Label initialBadge;
        private bool isReady;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>노드가 표시하는 직렬화 데이터입니다.</summary>
        public SWStateMachineNodeData Data { get; }

        /// <summary>노드로 들어오는 연결 포트입니다.</summary>
        public Port InputPort { get; private set; }

        /// <summary>노드에서 나가는 연결 포트입니다.</summary>
        public Port OutputPort { get; private set; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>상태 노드 보기를 생성합니다.</summary>
        public SWStateMachineNodeView(
            SWStateMachineNodeData data,
            Action<SWStateMachineNodeView> selected,
            Action<SWStateMachineNodeView, Rect> positionChanged)
        {
            Data = data;
            selectionCallback = selected;
            this.positionChanged = positionChanged;
            viewDataKey = data.Identifier;
            userData = data;

            AddToClassList("sw-state-node");
            AddToClassList("sw-behaviour-node");
            AddToClassList(GetKindClassName());

            titleContainer.Insert(0, CreateStateMarker());
            VisualElement badgeContainer = new VisualElement();
            badgeContainer.AddToClassList("sw-node-badges");
            layerBadge = CreateBadge("sw-layer-badge");
            initialBadge = CreateBadge("sw-initial-badge");
            badgeContainer.Add(layerBadge);
            badgeContainer.Add(initialBadge);
            titleContainer.Add(badgeContainer);

            CreatePorts();
            typeLabel = new Label();
            typeLabel.AddToClassList("sw-node-type");
            extensionContainer.Add(typeLabel);
            descriptionLabel = new Label();
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            descriptionLabel.style.marginLeft = 8f;
            descriptionLabel.style.marginRight = 8f;
            descriptionLabel.style.marginBottom = 6f;
            extensionContainer.Add(descriptionLabel);
            runtimeBadge = CreateBadge("sw-runtime-badge");
            badgeContainer.Add(runtimeBadge);

            RefreshVisuals();
            base.SetPosition(data.Position);
            RefreshExpandedState();
            RefreshPorts();
            isReady = true;
        }
        #endregion // 생성자

        #region 구성
        /// <summary>상태 종류를 나타내는 색상 표식을 생성합니다.</summary>
        private VisualElement CreateStateMarker()
        {
            VisualElement marker = new VisualElement();
            marker.AddToClassList("sw-state-marker");
            return marker;
        }

        /// <summary>노드 제목 옆에 표시할 작은 정보 표식을 생성합니다.</summary>
        private static Label CreateBadge(string className)
        {
            Label badge = new Label();
            badge.AddToClassList("sw-node-badge");
            badge.AddToClassList(className);
            return badge;
        }

        /// <summary>노드 종류에 맞는 입출력 포트를 생성합니다.</summary>
        private void CreatePorts()
        {
            if (Data.Kind != SWStateMachineNodeKind.AnyState)
            {
                InputPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi,
                    typeof(bool));
                InputPort.portName = "In";
                InputPort.tooltip = Data.Kind == SWStateMachineNodeKind.Return
                    ? "Pop 전이만 연결할 수 있는 입력 포트입니다."
                    : "이 상태로 들어오는 전이";
                InputPort.AddToClassList("sw-state-port");
                inputContainer.Add(InputPort);
            }

            if (Data.Kind != SWStateMachineNodeKind.Return)
            {
                OutputPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Multi,
                    typeof(bool));
                OutputPort.portName = "Out";
                OutputPort.tooltip = "이 상태에서 나가는 전이";
                OutputPort.AddToClassList("sw-state-port");
                outputContainer.Add(OutputPort);
            }
        }

        /// <summary>노드 종류에 대응하는 스타일 클래스 이름을 반환합니다.</summary>
        private string GetKindClassName()
        {
            return Data.Kind switch
            {
                SWStateMachineNodeKind.AnyState => "sw-any-state-node",
                SWStateMachineNodeKind.Return => "sw-return-state-node",
                _ => "sw-regular-state-node",
            };
        }

        /// <summary>노드 종류의 한글 표시 이름을 반환합니다.</summary>
        private string GetKindName()
        {
            return Data.Kind switch
            {
                SWStateMachineNodeKind.AnyState => "Any State",
                SWStateMachineNodeKind.Return => "Return State",
                _ => "State",
            };
        }
        #endregion // 구성

        #region 갱신
        /// <summary>직렬화 데이터에 맞춰 제목과 부가 정보를 갱신합니다.</summary>
        public void RefreshVisuals()
        {
            title = string.IsNullOrWhiteSpace(Data.DisplayName)
                ? GetKindName()
                : Data.DisplayName;

            if (Data.Kind == SWStateMachineNodeKind.State)
            {
                Type stateType = SWStateMachineGraphTypeResolver.Resolve(Data.StateTypeName);
                typeLabel.text = stateType == null ? "상태 타입을 찾을 수 없음" : stateType.Name;
                typeLabel.tooltip = Data.StateTypeName;
            }
            else
            {
                typeLabel.text = Data.Kind == SWStateMachineNodeKind.AnyState
                    ? "Global transition source · Output only"
                    : "Pop target · Input only";
                typeLabel.tooltip = Data.Kind == SWStateMachineNodeKind.AnyState
                    ? "모든 현재 상태에서 전이를 시작하는 출력 전용 노드입니다."
                    : "현재 Stack 상태를 Pop하는 전이의 입력 전용 노드입니다.";
            }

            layerBadge.text = $"Layer {Data.Layer}";
            layerBadge.style.display = Data.Kind == SWStateMachineNodeKind.Return
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            initialBadge.text = InitialBadgeText;
            initialBadge.style.display = Data.IsInitialState
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            descriptionLabel.text = Data.Description;
            descriptionLabel.style.display = string.IsNullOrWhiteSpace(Data.Description)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            runtimeBadge.style.display = DisplayStyle.None;
        }

        /// <summary>Play Mode에서 노드의 활성 상태와 실행 시간을 표시합니다.</summary>
        public void SetRuntimeStatus(bool isActive, float activeDuration)
        {
            runtimeBadge.text = isActive ? $"LIVE {activeDuration:0.0}s" : string.Empty;
            runtimeBadge.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            style.borderTopColor = isActive
                ? new Color(1f, 0.72f, 0.18f)
                : StyleKeyword.Null;
        }

        /// <summary>현재 그래프 종류에 따라 상태 계층 표식의 표시 여부를 변경합니다.</summary>
        public void SetLayerBadgeVisible(bool isVisible)
        {
            layerBadge.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>편집기 설정과 설명 내용에 따라 노드 설명의 표시 여부를 변경합니다.</summary>
        public void SetDescriptionVisible(bool isVisible)
        {
            descriptionLabel.style.display = isVisible &&
                !string.IsNullOrWhiteSpace(Data.Description)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>이전 코드와의 호환을 위해 노드 표시를 갱신합니다.</summary>
        public void RefreshTitle()
        {
            RefreshVisuals();
        }

        /// <summary>노드 위치 변경을 직렬화 데이터에 반영합니다.</summary>
        public override void SetPosition(Rect newPosition)
        {
            base.SetPosition(newPosition);

            if (isReady)
                positionChanged?.Invoke(this, newPosition);
        }

        /// <summary>노드를 선택하면 상세 편집기에 선택 내용을 전달합니다.</summary>
        public override void OnSelected()
        {
            base.OnSelected();
            selectionCallback?.Invoke(this);
        }
        #endregion // 갱신
    }
}
