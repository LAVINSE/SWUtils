using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SW.Base;

namespace SW.SkillTree
{
    /// <summary>프리팹이나 상속으로 교체할 수 있는 스킬 노드 표시입니다.</summary>
    public class SWSkillTreeNodeView : SWMonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image border;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI level;
        [SerializeField] private TextMeshProUGUI state;
        private Action selected;
        [SerializeField, HideInInspector] private string nodeIdentifier;

        /// <summary>재생성 후에도 위치 저장의 대응 관계에 사용하는 노드 식별자입니다.</summary>
        public string NodeIdentifier => nodeIdentifier;

        /// <summary>표시 노드에 원본 정의의 식별자를 연결합니다.</summary>
        public void BindIdentifier(string identifier) => nodeIdentifier = identifier;

        /// <summary>미공개 노드의 이름과 효과를 노출하지 않는 잠금 표시를 그립니다.</summary>
        public virtual void RenderMasked()
        {
            title.text = "???";
            level.text = string.Empty;
            state.text = "Undiscovered";
            icon.sprite = null;
            icon.enabled = false;
            border.color = SWSkillTreeViewFactory.Border;
            button.interactable = false;
        }

        /// <summary>기본 노드 구성을 생성하거나 연결된 사용자 프리팹을 그대로 사용합니다.</summary>
        public virtual void Build()
        {
            if (button != null) { SWSkillTreeViewFactory.RestoreMissingFonts(transform); return; }
            border = gameObject.AddComponent<Image>();
            border.color = SWSkillTreeViewFactory.Border;
            RectTransform surface = SWSkillTreeViewFactory.CreateRect("Surface", transform);
            SWSkillTreeViewFactory.Stretch(surface, 2, 2, 2, 2);
            Image background = surface.gameObject.AddComponent<Image>();
            background.color = SWSkillTreeViewFactory.Panel;
            button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            icon = SWSkillTreeViewFactory.CreateRect("Icon", surface).gameObject.AddComponent<Image>();
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, 1);
            icon.rectTransform.pivot = new Vector2(0, 1);
            icon.rectTransform.anchoredPosition = new Vector2(10, -10);
            icon.rectTransform.sizeDelta = new Vector2(30, 30);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            title = SWSkillTreeViewFactory.CreateText(surface, "Title", "", 14, SWSkillTreeViewFactory.Text);
            title.fontStyle = FontStyles.Bold;
            SWSkillTreeViewFactory.PlaceFromTop(title.rectTransform, 9, 34, 48, 8);
            level = SWSkillTreeViewFactory.CreateText(surface, "Level", "", 13, SWSkillTreeViewFactory.Text);
            SWSkillTreeViewFactory.PlaceFromTop(level.rectTransform, 45, 20, 10, 8);
            state = SWSkillTreeViewFactory.CreateText(surface, "State", "", 11, SWSkillTreeViewFactory.MutedText);
            SWSkillTreeViewFactory.PlaceFromTop(state.rectTransform, 65, 17, 10, 8);
        }
        /// <summary>이 노드의 선택 동작을 연결합니다.</summary>
        public void BindSelection(Action action)
        {
            button.onClick.RemoveListener(Select);
            selected = action;
            button.onClick.AddListener(Select);
        }
        /// <summary>현재 상태를 표시합니다. 구매 불가능한 노드도 선택하여 이유를 볼 수 있습니다.</summary>
        public virtual void Render(SWSkillTreeNode node, int currentLevel, bool canPurchase, bool isSelected)
        {
            button.interactable = true;
            title.text = node.Skill.DisplayName;
            icon.sprite = node.Skill.Icon;
            icon.enabled = node.Skill.Icon != null;
            Vector2 titleMinimum = title.rectTransform.offsetMin;
            titleMinimum.x = icon.enabled ? 48 : 10;
            title.rectTransform.offsetMin = titleMinimum;
            level.text = $"{currentLevel} / {node.Skill.MaximumLevel}";
            state.text = currentLevel == node.Skill.MaximumLevel ? "Max level" : canPurchase ? "Available" : currentLevel > 0 ? "Learned - Unavailable" : "Locked";
            border.color = isSelected ? SWSkillTreeViewFactory.Text : canPurchase ? SWSkillTreeViewFactory.Accent : currentLevel > 0 ? SWSkillTreeViewFactory.Learned : SWSkillTreeViewFactory.Border;
        }
        private void Select() => selected?.Invoke();
    }
}
