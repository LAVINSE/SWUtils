using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프의 전이 데이터와 요약 정보를 표시하는 연결선입니다.
    /// </summary>
    internal sealed class SWStateMachineEdgeView : Edge
    {
        #region 필드
        private readonly Action<SWStateMachineEdgeView> selectionCallback;
        private readonly Action<SWStateMachineEdgeView> summaryMoveStarted;
        private readonly Action<SWStateMachineEdgeView> summaryMoved;
        private readonly Label summaryLabel;
        private Vector2 summaryPointerStart;
        private Vector2 summaryOffsetStart;
        private int summaryPointerIdentifier = -1;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>연결선이 표시하는 직렬화 전이 데이터입니다.</summary>
        public SWStateMachineTransitionData Data { get; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>상태 전이 연결선을 생성합니다.</summary>
        public SWStateMachineEdgeView(
            SWStateMachineTransitionData data,
            Action<SWStateMachineEdgeView> selected,
            Action<SWStateMachineEdgeView> summaryMoveStarted,
            Action<SWStateMachineEdgeView> summaryMoved)
        {
            Data = data;
            selectionCallback = selected;
            this.summaryMoveStarted = summaryMoveStarted;
            this.summaryMoved = summaryMoved;
            userData = data;
            viewDataKey = data?.Identifier;
            AddToClassList("sw-state-transition");

            summaryLabel = new Label();
            summaryLabel.AddToClassList("sw-transition-label");
            summaryLabel.pickingMode = PickingMode.Position;
            summaryLabel.tooltip = "Alt 키를 누른 채 끌어서 위치 이동";
            summaryLabel.RegisterCallback<PointerDownEvent>(OnSummaryPointerDown);
            summaryLabel.RegisterCallback<PointerMoveEvent>(OnSummaryPointerMove);
            summaryLabel.RegisterCallback<PointerUpEvent>(OnSummaryPointerUp);
            summaryLabel.RegisterCallback<PointerCaptureOutEvent>(OnSummaryPointerCaptureOut);
            Add(summaryLabel);
            RegisterCallback<GeometryChangedEvent>(_ => UpdateLabelPosition());
            edgeControl.RegisterCallback<GeometryChangedEvent>(_ => UpdateLabelPosition());
            RefreshSummary();
        }
        #endregion // 생성자

        #region 갱신
        /// <summary>전이 동작, 명령과 우선순위를 연결선 위에 요약해 표시합니다.</summary>
        public void RefreshSummary()
        {
            if (Data == null)
            {
                summaryLabel.text = "전이";
                return;
            }

            string operationName = Data.Operation switch
            {
                SWStateMachineTransitionOperation.Push => "Push",
                SWStateMachineTransitionOperation.Replace => "Replace",
                SWStateMachineTransitionOperation.Pop => "Pop",
                _ => "Transition",
            };
            string commandText = Data.UsesCommand ? $" · 명령 {Data.Command}" : string.Empty;
            summaryLabel.text = $"{operationName}{commandText} · 우선순위 {Data.Priority}";
            summaryLabel.tooltip = string.IsNullOrWhiteSpace(Data.ConditionTypeName)
                ? "조건 없음"
                : Data.ConditionTypeName;
            schedule.Execute(UpdateLabelPosition);
        }

        /// <summary>전이 요약 표식의 표시 여부를 변경합니다.</summary>
        public void SetSummaryVisible(bool isVisible)
        {
            summaryLabel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>현재 연결 포트 위치를 기준으로 전이 요약 표식을 즉시 다시 배치합니다.</summary>
        public void UpdateSummaryPosition()
        {
            UpdateLabelPosition();
            schedule.Execute(UpdateLabelPosition);
        }

        /// <summary>Play Mode에서 최근 실행된 전이인지 강조합니다.</summary>
        public void SetRuntimeTriggered(bool isTriggered)
        {
            summaryLabel.style.borderTopColor = isTriggered
                ? new Color(1f, 0.72f, 0.18f)
                : StyleKeyword.Null;
            summaryLabel.style.borderBottomColor = isTriggered
                ? new Color(1f, 0.72f, 0.18f)
                : StyleKeyword.Null;
            summaryLabel.style.borderLeftColor = isTriggered
                ? new Color(1f, 0.72f, 0.18f)
                : StyleKeyword.Null;
            summaryLabel.style.borderRightColor = isTriggered
                ? new Color(1f, 0.72f, 0.18f)
                : StyleKeyword.Null;
        }

        /// <summary>연결선의 가운데에 전이 요약 표식을 배치합니다.</summary>
        private void UpdateLabelPosition()
        {
            if (output == null || input == null || panel == null)
                return;

            Matrix4x4 inverseWorldTransform = worldTransform.inverse;
            Vector2 outputCenter = inverseWorldTransform.MultiplyPoint3x4(output.worldBound.center);
            Vector2 inputCenter = inverseWorldTransform.MultiplyPoint3x4(input.worldBound.center);
            Vector2 center = (outputCenter + inputCenter) * 0.5f + Data.SummaryOffset;
            summaryLabel.style.left = center.x - (summaryLabel.resolvedStyle.width * 0.5f);
            summaryLabel.style.top = center.y - 11f;
        }

        /// <summary>Alt 키가 눌린 경우 전이 요약 위치 이동을 시작합니다.</summary>
        private void OnSummaryPointerDown(PointerDownEvent pointerEvent)
        {
            if (Data == null || !pointerEvent.altKey || pointerEvent.button != 0 ||
                summaryPointerIdentifier >= 0)
                return;

            summaryPointerIdentifier = pointerEvent.pointerId;
            summaryPointerStart = pointerEvent.position;
            summaryOffsetStart = Data.SummaryOffset;
            summaryMoveStarted?.Invoke(this);
            summaryLabel.CapturePointer(summaryPointerIdentifier);
            pointerEvent.StopPropagation();
        }

        /// <summary>포인터 이동량을 그래프 좌표로 변환하여 전이 요약 오프셋을 변경합니다.</summary>
        private void OnSummaryPointerMove(PointerMoveEvent pointerEvent)
        {
            if (pointerEvent.pointerId != summaryPointerIdentifier ||
                !summaryLabel.HasPointerCapture(summaryPointerIdentifier))
                return;

            Vector2 pointerDelta = (Vector2)pointerEvent.position - summaryPointerStart;
            Vector2 localDelta = worldTransform.inverse.MultiplyVector(pointerDelta);
            Data.SummaryOffset = summaryOffsetStart + localDelta;
            UpdateLabelPosition();
            pointerEvent.StopPropagation();
        }

        /// <summary>전이 요약 위치 이동을 완료하고 변경 사실을 알립니다.</summary>
        private void OnSummaryPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != summaryPointerIdentifier)
                return;

            CompleteSummaryMove();
            pointerEvent.StopPropagation();
        }

        /// <summary>포인터 캡처가 해제된 경우 진행 중인 이동을 마칩니다.</summary>
        private void OnSummaryPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (summaryPointerIdentifier >= 0)
                CompleteSummaryMove();
        }

        /// <summary>포인터 캡처를 해제하고 전이 요약 변경을 저장하도록 요청합니다.</summary>
        private void CompleteSummaryMove()
        {
            int pointerIdentifier = summaryPointerIdentifier;
            summaryPointerIdentifier = -1;
            if (pointerIdentifier >= 0 && summaryLabel.HasPointerCapture(pointerIdentifier))
                summaryLabel.ReleasePointer(pointerIdentifier);

            summaryMoved?.Invoke(this);
        }
        #endregion // 갱신

        #region 선택
        /// <summary>연결선을 선택하면 상세 편집기에 선택 내용을 전달합니다.</summary>
        public override void OnSelected()
        {
            base.OnSelected();
            selectionCallback?.Invoke(this);
        }
        #endregion // 선택
    }
}
