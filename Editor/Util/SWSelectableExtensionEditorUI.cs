using System;
using UnityEngine.UIElements;

namespace SW.EditorTools.Util
{
    /// <summary>
    /// 버튼과 토글 확장 인스펙터에 공통 Iris Line 테마와 설정 패널을 구성합니다.
    /// </summary>
    internal static class SWSelectableExtensionEditorUI
    {
        #region 인스펙터 구성
        /// <summary>
        /// 테마를 적용한 루트와 기본 Unity 인스펙터 영역을 한 번 생성합니다.
        /// </summary>
        internal static VisualElement CreateRoot(string title, Action drawBaseInspector)
        {
            VisualElement root = new();
            root.AddToClassList("sw-inspector-root");
            root.AddToClassList("sw-selectable-inspector");
            SWEditorTheme.ApplyInspector(root);

            StyleSheet inspectorStyle = SWEditorUtils.LoadStyleSheetByIdentifier("f69b197a66348b44795b34f16b1c31a3");
            if (inspectorStyle != null)
            {
                root.styleSheets.Add(inspectorStyle);
            }

            VisualElement section = CreateSection(title);
            section.Add(new IMGUIContainer(drawBaseInspector));
            root.Add(section);
            return root;
        }

        /// <summary>
        /// 공통 테마의 패널, 제목과 선택적인 설명을 생성합니다.
        /// </summary>
        internal static VisualElement CreateSection(string title, string description = null)
        {
            VisualElement section = new();
            section.AddToClassList("sw-section");

            Label heading = new(title);
            heading.AddToClassList("sw-section-title");
            heading.AddToClassList("sw-wrap");
            section.Add(heading);

            if (!string.IsNullOrEmpty(description))
            {
                Label hint = new(description);
                hint.AddToClassList("sw-secondary-text");
                hint.AddToClassList("sw-wrap");
                section.Add(hint);
            }

            return section;
        }
        #endregion // 인스펙터 구성
    }
}
