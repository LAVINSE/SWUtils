using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools
{
    /// <summary>그래프 편집기에서 공유할 도구 모음 요소의 크기와 외형을 생성합니다.</summary>
    internal static class SWGraphEditorVisualUtility
    {
        /// <summary>두 그래프 편집기에서 동일하게 사용할 도구 모음 버튼을 생성합니다.</summary>
        public static ToolbarButton CreateToolbarButton(
            string text,
            Action clicked,
            string tooltip = null)
        {
            ToolbarButton button = new ToolbarButton(clicked) { text = text, tooltip = tooltip };
            button.style.height = 24f;
            button.style.minWidth = 76f;
            button.style.marginLeft = 1f;
            button.style.marginRight = 1f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            return button;
        }

        /// <summary>두 그래프 편집기 제목에 동일한 정렬과 글자 스타일을 적용합니다.</summary>
        public static void ApplyToolbarTitle(Label titleLabel)
        {
            titleLabel.style.flexGrow = 1f;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.paddingLeft = 8f;
            titleLabel.style.fontSize = 13f;
        }
    }
}
