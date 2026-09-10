using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools
{
    /// <summary>그래프 편집기에서 공유할 도구 모음 요소의 크기와 외형을 생성합니다.</summary>
    internal static class SWGraphEditorVisualUtility
    {
        /// <summary>SW 그래프 편집기의 도구 모음에 공통 간격, 배경과 구분선을 적용합니다.</summary>
        public static void ApplyToolbar(VisualElement toolbar)
        {
            toolbar.style.height = 34f;
            toolbar.style.flexShrink = 0f;
            toolbar.style.paddingLeft = 8f;
            toolbar.style.paddingRight = 8f;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.backgroundColor = new Color(0.149f, 0.157f, 0.173f);
            toolbar.style.borderBottomWidth = 1f;
            toolbar.style.borderBottomColor = new Color(0.282f, 0.298f, 0.322f);
        }

        /// <summary>그래프 편집기에서 동일하게 사용할 도구 모음 버튼을 생성합니다.</summary>
        public static ToolbarButton CreateToolbarButton(
            string text,
            Action clicked,
            string tooltip = null)
        {
            ToolbarButton button = new ToolbarButton(clicked) { text = text, tooltip = tooltip };
            button.style.height = 24f;
            button.style.flexShrink = 0f;
            button.style.minWidth = 76f;
            button.style.marginLeft = 1f;
            button.style.marginRight = 1f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            return button;
        }

        /// <summary>그래프 편집기 제목에 동일한 정렬과 글자 스타일을 적용합니다.</summary>
        public static void ApplyToolbarTitle(Label titleLabel)
        {
            titleLabel.style.flexGrow = 1f;
            titleLabel.style.minWidth = 0f;
            titleLabel.style.overflow = Overflow.Hidden;
            titleLabel.style.textOverflow = TextOverflow.Ellipsis;
            titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.paddingLeft = 8f;
            titleLabel.style.fontSize = 13f;
        }
    }
}
