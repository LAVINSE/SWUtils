using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools
{
    /// <summary>글꼴에 의존하지 않고 위치 찾기 기호를 중앙에 그립니다.</summary>
    internal sealed class SWEditorLocationIcon : VisualElement
    {
        /// <summary>버튼 입력을 가로채지 않는 위치 아이콘을 생성합니다.</summary>
        internal SWEditorLocationIcon()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("sw-toolbar-icon");
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            Vector2 center = contentRect.center;
            float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.3f;
            Painter2D painter = context.painter2D;
            painter.strokeColor = SWEditorTheme.Text;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            painter.Stroke();
            painter.BeginPath();
            foreach (Vector2 direction in new[]
            {
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            }

            )
            {
                painter.MoveTo(center + direction * radius * 0.55f);
                painter.LineTo(center + direction * radius * 1.5f);
            }

            painter.Stroke();
        }
    }
}
