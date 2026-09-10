using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SW.Base;

namespace SW.SkillTree
{
    /// <summary>선택한 스킬의 효과, 조건, 비용과 구매 동작을 표시하는 교체 가능한 상세 패널입니다.</summary>
    public class SWSkillTreeDetailsView : SWMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private TextMeshProUGUI effects;
        [SerializeField] private TextMeshProUGUI requirements;
        [SerializeField] private TextMeshProUGUI costs;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Button purchaseOne;
        [SerializeField] private Button purchaseTen;
        [SerializeField] private Button purchaseMaximum;
        [SerializeField] private Button refund;
        private Action<int, bool> purchaseRequested;
        private Action refundRequested;

        /// <summary>상세 내용을 스크롤할 수 있는 기본 패널을 생성합니다.</summary>
        public virtual void Build()
        {
            if (title != null) return;
            Image background = gameObject.AddComponent<Image>();
            background.color = SWSkillTreeViewFactory.Panel;
            RectTransform viewport = SWSkillTreeViewFactory.CreateRect("Viewport", transform);
            SWSkillTreeViewFactory.Stretch(viewport, 16, 16, 16, 16);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = SWSkillTreeViewFactory.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            title = AddText(content, "Title", "Select a skill", 21);
            title.fontStyle = FontStyles.Bold;
            description = AddText(content, "Description", "Select a node to view effects and requirements.", 14);
            effects = AddText(content, "Effects", "", 14);
            requirements = AddText(content, "Requirements", "", 13);
            costs = AddText(content, "Costs", "", 14);
            message = AddText(content, "Message", "", 13);
            message.color = SWSkillTreeViewFactory.MutedText;
            purchaseOne = AddButton(content, "PurchaseOne", "Buy 1 level");
            purchaseTen = AddButton(content, "PurchaseTen", "Buy 10 levels");
            purchaseMaximum = AddButton(content, "PurchaseMaximum", "Buy Max");
            refund = AddButton(content, "Refund", "Refund 1 level");
        }

        /// <summary>화면 동작을 연결합니다. 재연결할 때 기존 내부 구독을 제거합니다.</summary>
        public void BindActions(Action<int, bool> purchase, Action refundAction)
        {
            purchaseRequested = purchase;
            refundRequested = refundAction;
            purchaseOne.onClick.RemoveListener(PurchaseOne);
            purchaseTen.onClick.RemoveListener(PurchaseTen);
            purchaseMaximum.onClick.RemoveListener(PurchaseMaximum);
            refund.onClick.RemoveListener(Refund);
            purchaseOne.onClick.AddListener(PurchaseOne);
            purchaseTen.onClick.AddListener(PurchaseTen);
            purchaseMaximum.onClick.AddListener(PurchaseMaximum);
            refund.onClick.AddListener(Refund);
        }

        /// <summary>선택한 노드의 현재 효과와 다음 효과, 잔액과 구매 가능 여부를 표시합니다.</summary>
        public virtual void Render(SWSkillTreeSystem system, SWSkillTreeNode node, string feedback)
        {
            bool valid = system != null && node != null;
            purchaseOne.interactable = purchaseTen.interactable = purchaseMaximum.interactable = refund.interactable = false;
            if (!valid)
            {
                title.text = "Select a skill";
                description.text = "Select a node to view effects and requirements.";
                effects.text = requirements.text = costs.text = message.text = string.Empty;
                return;
            }
            int level = system.GetLevel(node.Identifier);
            title.text = $"{node.Skill.DisplayName}\n{level} / {node.Skill.MaximumLevel}";
            description.text = node.Skill.Description;
            effects.text = "Current effects\n" + Describe(node, level);
            if (level < node.Skill.MaximumLevel) effects.text += "\n\nNext level effects\n" + Describe(node, level + 1);
            requirements.text = node.Requirements.Count == 0 ? "No prerequisites" :
                (node.RequirementMode == SWSkillTreeRequirementMode.All ? "All prerequisites required\n" : "Any prerequisite required\n")
                + string.Join("\n", node.Requirements.Select(requirement => system.TryGetNode(requirement.NodeIdentifier, out SWSkillTreeNode parent)
                    ? $"{parent.Skill.DisplayName} {system.GetLevel(parent.Identifier)} / {requirement.RequiredLevel}" : "Invalid connection"));
            SWSkillTreePurchase one = system.PreviewPurchase(node.Identifier);
            SWSkillTreePurchase ten = system.PreviewPurchase(node.Identifier, 10);
            SWSkillTreePurchase maximum = system.PreviewPurchase(node.Identifier, maximum: true);
            // 잠긴 노드도 다음 비용을 볼 수 있도록 계산만 별도로 수행합니다.
            try
            {
                costs.text = level >= node.Skill.MaximumLevel ? "All levels learned." : "Next level cost\n" +
                    (node.Skill.Costs.Count == 0 ? "Free" : string.Join("\n", node.Skill.Costs.Select(cost => cost.Evaluate(level))
                        .GroupBy(amount => amount.currency).Select(group =>
                        $"{new SWSkillTreeAmount(group.Key, group.Sum(amount => amount.value))}\nBalance {new SWSkillTreeAmount(group.Key, system.Wallet.GetBalance(group.Key))}")));
            }
            catch (Exception exception) { costs.text = $"Cost calculation failed: {exception.Message}"; }
            message.text = !string.IsNullOrEmpty(feedback) ? feedback : one.Reason;
            purchaseOne.interactable = one.Success;
            purchaseTen.interactable = ten.Success;
            purchaseMaximum.interactable = maximum.Success;
            purchaseMaximum.GetComponentInChildren<TextMeshProUGUI>().text = maximum.Success ? $"Buy Max ({maximum.Levels})" : "Buy Max";
            refund.interactable = level > 0;
        }
        private static string Describe(SWSkillTreeNode node, int level)
        {
            try { return node.Skill.Effects.Count == 0 ? "No persistent effects" : string.Join("\n", node.Skill.Effects.Select(effect => effect.Describe(level))); }
            catch (Exception exception) { return $"Effect description failed: {exception.Message}"; }
        }
        private static TextMeshProUGUI AddText(Transform parent, string name, string value, int size)
        {
            TextMeshProUGUI result = SWSkillTreeViewFactory.CreateText(parent, name, value, size, SWSkillTreeViewFactory.Text);
            result.gameObject.AddComponent<LayoutElement>().minHeight = size + 4;
            return result;
        }
        private static Button AddButton(Transform parent, string name, string label)
        {
            Button button = SWSkillTreeViewFactory.CreateButton(parent, name, label);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            return button;
        }
        private void PurchaseOne() => purchaseRequested?.Invoke(1, false);
        private void PurchaseTen() => purchaseRequested?.Invoke(10, false);
        private void PurchaseMaximum() => purchaseRequested?.Invoke(1, true);
        private void Refund() => refundRequested?.Invoke();
    }
}
