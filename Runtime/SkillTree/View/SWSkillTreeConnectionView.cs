using UnityEngine;

using SW.Base;

namespace SW.SkillTree
{
    /// <summary>실제 노드 사각형의 테두리에 연결선을 맞춥니다. 편집 모드의 이동과 크기 변경도 반영합니다.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SWSkillTreeConnectionView : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private RectTransform source;
        [SerializeField] private RectTransform destination;
        private Matrix4x4 previousSource;
        private Matrix4x4 previousDestination;
        private Rect previousSourceRect;
        private Rect previousDestinationRect;
        private bool initialized;
        #endregion // 필드

        #region 연결
        /// <summary>연결한 양쪽 노드가 현재 화면에 표시되어 있는지 반환합니다.</summary>
        public bool HasVisibleEndpoints => source != null && destination != null && source.gameObject.activeSelf && destination.gameObject.activeSelf;

        /// <summary>연결할 두 노드를 지정하고 즉시 선의 배치를 갱신합니다.</summary>
        public void Initialize(RectTransform sourceNode, RectTransform destinationNode)
        {
            source = sourceNode;
            destination = destinationNode;
            initialized = false;
            RefreshGeometry();
        }

        private void LateUpdate() => RefreshGeometry();

        /// <summary>노드 또는 부모 좌표가 바뀐 경우에만 연결선의 중심, 길이와 각도를 다시 계산합니다.</summary>
        public void RefreshGeometry()
        {
            if (source == null || destination == null || transform.parent == null) return;
            Transform parent = transform.parent;
            Matrix4x4 sourceMatrix = SWSkillTreeViewGeometry.GetRelativeMatrix(source, parent);
            Matrix4x4 destinationMatrix = SWSkillTreeViewGeometry.GetRelativeMatrix(destination, parent);
            if (initialized && previousSource == sourceMatrix && previousDestination == destinationMatrix
                && previousSourceRect == source.rect && previousDestinationRect == destination.rect) return;
            previousSource = sourceMatrix;
            previousDestination = destinationMatrix;
            previousSourceRect = source.rect;
            previousDestinationRect = destination.rect;
            initialized = true;

            Vector3 sourceCenter = sourceMatrix.MultiplyPoint3x4(source.rect.center);
            Vector3 destinationCenter = destinationMatrix.MultiplyPoint3x4(destination.rect.center);
            Vector2 start = sourceMatrix.MultiplyPoint3x4(GetBoundary(source.rect, sourceMatrix.inverse.MultiplyPoint3x4(destinationCenter)));
            Vector2 end = destinationMatrix.MultiplyPoint3x4(GetBoundary(destination.rect, destinationMatrix.inverse.MultiplyPoint3x4(sourceCenter)));
            Vector2 direction = destinationCenter - sourceCenter;
            Vector2 difference = end - start;
            RectTransform line = (RectTransform)transform;
            line.pivot = new Vector2(0.5f, 0.5f);
            line.localPosition = (start + end) * 0.5f;
            line.sizeDelta = new Vector2(Vector2.Dot(difference, direction) > 0 ? difference.magnitude : 0, 2);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg);
        }

        private static Vector3 GetBoundary(Rect rectangle, Vector2 target)
        {
            Vector2 center = rectangle.center;
            Vector2 difference = target - center;
            float horizontal = Mathf.Abs(difference.x) > 0.0001f ? rectangle.width * 0.5f / Mathf.Abs(difference.x) : float.PositiveInfinity;
            float vertical = Mathf.Abs(difference.y) > 0.0001f ? rectangle.height * 0.5f / Mathf.Abs(difference.y) : float.PositiveInfinity;
            float factor = Mathf.Min(horizontal, vertical);
            return center + difference * (float.IsInfinity(factor) ? 0 : factor);
        }
        #endregion // 연결
    }
}
