using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SW.SkillTree
{
    /// <summary>기본 화면과 예제에서 사용하는 단순한 사각형 기반 화면 요소를 생성합니다.</summary>
    public static class SWSkillTreeViewFactory
    {
        /// <summary>전체 배경색입니다.</summary>
        public static readonly Color Background = new(0.075f, 0.08f, 0.09f);
        /// <summary>노드와 상세 패널 배경색입니다.</summary>
        public static readonly Color Panel = new(0.157f, 0.165f, 0.18f);
        /// <summary>구매 가능한 노드의 강조색입니다.</summary>
        public static readonly Color Accent = new(0.22f, 0.58f, 0.92f);
        /// <summary>미해금 연결선과 테두리 색상입니다.</summary>
        public static readonly Color Border = new(0.282f, 0.298f, 0.322f);
        /// <summary>습득 상태 색상입니다.</summary>
        public static readonly Color Learned = new(0.25f, 0.67f, 0.48f);
        /// <summary>본문 글자색입니다.</summary>
        public static readonly Color Text = new(0.92f, 0.93f, 0.95f);
        /// <summary>보조 글자색입니다.</summary>
        public static readonly Color MutedText = new(0.68f, 0.71f, 0.75f);

        /// <summary>프리팹에서 비어 있는 글꼴 참조를 프로젝트의 TextMeshPro 기본 글꼴로 연결합니다.</summary>
        public static void RestoreMissingFonts(Transform root)
        {
            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
        }
        /// <summary>부모 아래에 빈 사각형 요소를 생성합니다.</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            RectTransform result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            return result;
        }
        /// <summary>지정 여백으로 부모 전체에 맞춥니다.</summary>
        public static void Stretch(RectTransform rectangle, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rectangle.anchorMin = Vector2.zero;
            rectangle.anchorMax = Vector2.one;
            rectangle.offsetMin = new Vector2(left, bottom);
            rectangle.offsetMax = new Vector2(-right, -top);
        }
        /// <summary>상단 기준으로 고정 높이의 텍스트를 생성합니다.</summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string value, int size, Color color)
        {
            TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }
        /// <summary>텍스트와 배경을 가진 기본 버튼을 생성합니다.</summary>
        public static Button CreateButton(Transform parent, string name, string label)
        {
            RectTransform rectangle = CreateRect(name, parent);
            Image background = rectangle.gameObject.AddComponent<Image>();
            background.color = SWSkillTreeViewFactory.Panel;
            Button button = rectangle.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            TextMeshProUGUI text = CreateText(rectangle, "Label", label, 14, SWSkillTreeViewFactory.Text);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform, 4, 3, 4, 3);
            return button;
        }
        /// <summary>요소를 위쪽 기준으로 지정한 위치와 높이에 배치합니다.</summary>
        public static void PlaceFromTop(RectTransform rectangle, float top, float height, float left = 16, float right = 16)
        {
            rectangle.anchorMin = new Vector2(0, 1);
            rectangle.anchorMax = Vector2.one;
            rectangle.pivot = new Vector2(0.5f, 1);
            rectangle.offsetMin = new Vector2(left, -top - height);
            rectangle.offsetMax = new Vector2(-right, -top);
        }
    }
}
