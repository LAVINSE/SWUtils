using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 떠 있는 그래프 패널의 모서리를 끌어 너비와 높이를 조절합니다.
    /// </summary>
    internal sealed class SWPanelResizeManipulator : PointerManipulator
    {
        #region 필드
        private readonly VisualElement panelElement;
        private readonly bool resizeFromLeft;
        private readonly Action<Vector2> resizeCompleted;
        private Vector2 pointerStart;
        private Vector2 sizeStart;
        private int activePointerIdentifier = -1;
        #endregion // 필드

        #region 생성자
        /// <summary>패널 크기 조절 조작기를 생성합니다.</summary>
        public SWPanelResizeManipulator(
            VisualElement panelElement,
            bool resizeFromLeft,
            Action<Vector2> resizeCompleted)
        {
            this.panelElement = panelElement;
            this.resizeFromLeft = resizeFromLeft;
            this.resizeCompleted = resizeCompleted;
        }
        #endregion // 생성자

        #region 등록
        /// <summary>포인터 이벤트를 크기 조절 손잡이에 등록합니다.</summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        /// <summary>포인터 이벤트 등록을 해제합니다.</summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }
        #endregion // 등록

        #region 포인터 처리
        /// <summary>크기 조절 시작 위치와 패널 크기를 저장합니다.</summary>
        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || activePointerIdentifier >= 0)
                return;

            activePointerIdentifier = pointerEvent.pointerId;
            pointerStart = pointerEvent.position;
            sizeStart = new Vector2(
                panelElement.resolvedStyle.width,
                panelElement.resolvedStyle.height);
            target.CapturePointer(activePointerIdentifier);
            pointerEvent.StopPropagation();
        }

        /// <summary>포인터 이동량에 맞춰 패널 크기를 실시간으로 변경합니다.</summary>
        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (pointerEvent.pointerId != activePointerIdentifier ||
                !target.HasPointerCapture(activePointerIdentifier))
                return;

            Vector2 delta = (Vector2)pointerEvent.position - pointerStart;
            float widthDelta = resizeFromLeft ? -delta.x : delta.x;
            panelElement.style.width = Mathf.Clamp(sizeStart.x + widthDelta, 240f, 560f);
            panelElement.style.height = Mathf.Clamp(sizeStart.y + delta.y, 220f, 800f);
            pointerEvent.StopPropagation();
        }

        /// <summary>조절한 패널 크기를 사용자 설정에 저장하도록 알립니다.</summary>
        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != activePointerIdentifier)
                return;

            CompleteResize();
            pointerEvent.StopPropagation();
        }

        /// <summary>포인터 캡처가 외부에서 해제되어도 조절 결과를 보존합니다.</summary>
        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (activePointerIdentifier >= 0)
                CompleteResize();
        }

        /// <summary>포인터 캡처를 해제하고 최종 크기를 전달합니다.</summary>
        private void CompleteResize()
        {
            int pointerIdentifier = activePointerIdentifier;
            activePointerIdentifier = -1;
            if (pointerIdentifier >= 0 && target.HasPointerCapture(pointerIdentifier))
                target.ReleasePointer(pointerIdentifier);

            resizeCompleted?.Invoke(new Vector2(
                panelElement.resolvedStyle.width,
                panelElement.resolvedStyle.height));
        }
        #endregion // 포인터 처리
    }
}
