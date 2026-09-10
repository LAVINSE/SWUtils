using UnityEngine;
using UnityEngine.EventSystems;

namespace SW.SkillTree
{
    /// <summary>스킬트리 작업 영역의 이동과 포인터 중심 확대를 처리합니다.</summary>
    public sealed class SWSkillTreeViewport : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        /// <summary>넓은 트리 전체를 확인할 수 있는 최소 확대 배율입니다.</summary>
        public const float MinimumZoom = 0.08f;
        /// <summary>노드 상세 확인을 위한 최대 확대 배율입니다.</summary>
        public const float MaximumZoom = 2f;
        [SerializeField] private RectTransform content;
        private Vector2 lastPointer;
        /// <summary>이동할 노드와 연결선의 공통 부모를 지정합니다.</summary>
        public void Initialize(RectTransform target) => content = target;
        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
            => RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out lastPointer);
        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            if (content == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 current);
            content.anchoredPosition += current - lastPointer;
            lastPointer = current;
        }
        /// <inheritdoc />
        public void OnScroll(PointerEventData eventData)
        {
            if (content == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.enterEventCamera, out Vector2 pointer);
            float previous = Mathf.Max(MinimumZoom, content.localScale.x);
            float scale = Mathf.Clamp(previous * Mathf.Pow(1.15f, eventData.scrollDelta.y), MinimumZoom, MaximumZoom);
            content.anchoredPosition = pointer - (pointer - content.anchoredPosition) * (scale / previous);
            content.localScale = Vector3.one * scale;
        }
    }
}
