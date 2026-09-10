using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SW.SkillTree;
using SW.Stat;

namespace SW.Tests.SkillTree
{
    /// <summary>구매의 원자성, 선행 규칙, 저장 복원과 효과 수명 주기를 검증합니다.</summary>
    public sealed class SWSkillTreeSystemTests
    {
        [Serializable]
        private sealed class ConstantCost : SWSkillTreeCost
        {
            internal string currency = "Gold";
            internal double amount = 10;
            public override SWSkillTreeAmount Evaluate(int currentLevel) => new(currency, amount);
        }
        [Serializable]
        private sealed class ToggleCondition : SWSkillTreeCondition
        {
            internal bool satisfied;
            public override bool IsSatisfied(SWSkillTreeSystem system, out string reason)
            { reason = "연구를 먼저 완료하세요."; return satisfied; }
        }
        private readonly List<UnityEngine.Object> created = new();
        private readonly List<IDisposable> disposables = new();
        private SWSkillTreeWallet wallet;

        [SetUp]
        public void SetUp() { wallet = new SWSkillTreeWallet(); wallet.SetBalance("Gold", 1000); }
        [TearDown]
        public void TearDown()
        {
            for (int index = disposables.Count - 1; index >= 0; index--) disposables[index].Dispose();
            for (int index = created.Count - 1; index >= 0; index--) if (created[index] != null) UnityEngine.Object.DestroyImmediate(created[index]);
            disposables.Clear();
            created.Clear();
        }

        [Test]
        public void SharedDefinitionKeepsOwnersIndependent()
        {
            SWSkillTreeNode node = Node();
            SWSkillTreeDefinition definition = Tree(node);
            SWSkillTreeSystem first = System(definition);
            SWSkillTreeSystem second = System(definition);
            Assert.That(first.Purchase(node.Identifier).Success, Is.True);
            Assert.That(first.GetLevel(node.Identifier), Is.EqualTo(1));
            Assert.That(second.GetLevel(node.Identifier), Is.Zero);
            Assert.That(node.Skill.MaximumLevel, Is.EqualTo(10));
        }

        [Test]
        public void MultiCurrencyFailureDoesNotSpendAnyCurrency()
        {
            SWSkillTreeNode node = Node();
            Set(node.Skill, "costs", new SWSkillTreeCost[] { new ConstantCost(), new ConstantCost { currency = "Research", amount = 5 } });
            wallet.SetBalance("Research", 4);
            SWSkillTreeSystem system = System(Tree(node));
            Assert.That(system.Purchase(node.Identifier).Success, Is.False);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(1000));
            Assert.That(wallet.GetBalance("Research"), Is.EqualTo(4));
            Assert.That(system.GetLevel(node.Identifier), Is.Zero);
        }

        [Test]
        public void DuplicateCurrencyCostsAreAggregated()
        {
            SWSkillTreeNode node = Node();
            Set(node.Skill, "costs", new SWSkillTreeCost[] { new ConstantCost(), new ConstantCost { amount = 20 } });
            SWSkillTreeSystem system = System(Tree(node));
            Assert.That(system.PreviewPurchase(node.Identifier).Costs.Single().value, Is.EqualTo(30));
            Assert.That(system.Purchase(node.Identifier).Success, Is.True);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(970));
        }

        [Test]
        public void FailedExactBatchLeavesBalanceAndLevelUnchanged()
        {
            SWSkillTreeNode node = Node();
            wallet.SetBalance("Gold", 95);
            SWSkillTreeSystem system = System(Tree(node));
            Assert.That(system.Purchase(node.Identifier, 10).Success, Is.False);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(95));
            Assert.That(system.GetLevel(node.Identifier), Is.Zero);
            Assert.That(system.Purchase(node.Identifier, maximum: true).Levels, Is.EqualTo(9));
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(5));
        }

        [Test]
        public void MaximumFreePurchaseStopsAtDocumentedBatchLimit()
        {
            SWSkillTreeNode node = Node(maximum: 20000);
            Set(node.Skill, "costs", Array.Empty<SWSkillTreeCost>());
            SWSkillTreeSystem system = System(Tree(node));
            SWSkillTreePurchase result = system.Purchase(node.Identifier, maximum: true);
            Assert.That(result.Levels, Is.EqualTo(SWSkillTreeSystem.MaximumBatchLevels));
            Assert.That(result.Reason, Is.Not.Empty);
        }

        [Test]
        public void PurchaseRevalidatesWalletInsteadOfTrustingOldPreview()
        {
            SWSkillTreeNode node = Node();
            SWSkillTreeSystem system = System(Tree(node));
            Assert.That(system.PreviewPurchase(node.Identifier).Success, Is.True);
            wallet.SetBalance("Gold", 0);
            Assert.That(system.Purchase(node.Identifier).Success, Is.False);
            Assert.That(system.GetLevel(node.Identifier), Is.Zero);
        }

        [Test]
        public void WalletCallbackCannotReenterPurchase()
        {
            SWSkillTreeNode node = Node();
            SWSkillTreeSystem system = System(Tree(node));
            SWSkillTreePurchase nested = null;
            wallet.Changed += () => nested = system.Purchase(node.Identifier);
            Assert.That(system.Purchase(node.Identifier).Success, Is.True);
            Assert.That(nested.Success, Is.False);
            Assert.That(system.GetLevel(node.Identifier), Is.EqualTo(1));
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(990));
        }

        [Test]
        public void AllRequirementsAndRequiredLevelsAreEnforced()
        {
            SWSkillTreeNode first = Node(), second = Node(), child = Node();
            Require(child, first, 2);
            Require(child, second, 1);
            SWSkillTreeSystem system = System(Tree(first, second, child));
            system.Purchase(first.Identifier, 2);
            Assert.That(system.Purchase(child.Identifier).Success, Is.False);
            system.Purchase(second.Identifier);
            Assert.That(system.Purchase(child.Identifier).Success, Is.True);
            Assert.That(system.Refund(first.Identifier, out _), Is.False);
            Assert.That(system.GetLevel(first.Identifier), Is.EqualTo(2));
        }

        [Test]
        public void AnyRequirementAllowsAlternateParentDuringRefund()
        {
            SWSkillTreeNode first = Node(), second = Node(), child = Node();
            Require(child, first); Require(child, second);
            Set(child, "requirementMode", SWSkillTreeRequirementMode.Any);
            SWSkillTreeSystem system = System(Tree(first, second, child));
            system.Purchase(first.Identifier);
            system.Purchase(second.Identifier);
            system.Purchase(child.Identifier);
            Assert.That(system.Refund(first.Identifier, out _), Is.True);
            Assert.That(system.Refund(second.Identifier, out _), Is.False);
        }

        [Test]
        public void ExclusiveBranchCanBeChangedAfterRefund()
        {
            SWSkillTreeNode first = Node(), second = Node();
            Set(first, "exclusiveGroup", "Choice"); Set(second, "exclusiveGroup", "Choice");
            SWSkillTreeSystem system = System(Tree(first, second));
            system.Purchase(first.Identifier);
            Assert.That(system.Purchase(second.Identifier).Success, Is.False);
            Assert.That(system.Refund(first.Identifier, out _), Is.True);
            Assert.That(system.Purchase(second.Identifier).Success, Is.True);
        }

        [Test]
        public void VisibilityAndPurchaseConditionsAreIndependent()
        {
            SWSkillTreeNode node = Node();
            ToggleCondition visibility = new(), purchase = new();
            Set(node, "visibilityConditions", new SWSkillTreeCondition[] { visibility });
            Set(node, "purchaseConditions", new SWSkillTreeCondition[] { purchase });
            SWSkillTreeSystem system = System(Tree(node));
            Assert.That(system.IsVisible(node.Identifier), Is.False);
            visibility.satisfied = true;
            Assert.That(system.IsVisible(node.Identifier), Is.True);
            Assert.That(system.Purchase(node.Identifier).Reason, Is.EqualTo("연구를 먼저 완료하세요."));
            purchase.satisfied = true;
            Assert.That(system.Purchase(node.Identifier).Success, Is.True);
            visibility.satisfied = false;
            Assert.That(system.IsVisible(node.Identifier), Is.True);
        }

        [Test]
        public void RestoreUsesIdentifiersAndDoesNotGrantPurchaseAgain()
        {
            SWSkillTreeNode first = Node(), second = Node();
            SWSkillTreeDefinition definition = Tree(first, second);
            SWSkillTreeSystem system = System(definition);
            system.Purchase(first.Identifier, 3);
            SWSkillTreeSaveData save = JsonUtility.FromJson<SWSkillTreeSaveData>(JsonUtility.ToJson(system.CaptureSaveData()));
            Set(definition, "nodes", new List<SWSkillTreeNode> { second, first });
            SWSkillTreeSystem restored = System(definition);
            int purchases = 0;
            restored.Purchased += (_, _) => purchases++;
            Assert.That(restored.Restore(save, out _), Is.True);
            Assert.That(restored.GetLevel(first.Identifier), Is.EqualTo(3));
            Assert.That(purchases, Is.Zero);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(970));
            save.nodes[0].payments.Clear();
            Assert.That(restored.GetLevel(first.Identifier), Is.EqualTo(3));
        }

        [Test]
        public void CorruptRestorePreservesExistingProgress()
        {
            SWSkillTreeNode node = Node();
            SWSkillTreeSystem system = System(Tree(node));
            system.Purchase(node.Identifier);
            SWSkillTreeSaveData save = system.CaptureSaveData();
            save.nodes[0].payments[0].amounts[0] = new SWSkillTreeAmount("Gold", double.NaN);
            Assert.That(system.Restore(save, out _), Is.False);
            Assert.That(system.GetLevel(node.Identifier), Is.EqualTo(1));
            save = system.CaptureSaveData();
            save.version = 2;
            Assert.That(system.Restore(save, out _), Is.False);
        }

        [Test]
        public void RefundUsesOriginalPaymentAfterBalanceChange()
        {
            SWSkillTreeNode node = Node();
            SWSkillTreeSystem system = System(Tree(node));
            system.Purchase(node.Identifier);
            ConstantCost cost = (ConstantCost)node.Skill.Costs[0];
            cost.amount = 100;
            Assert.That(system.Refund(node.Identifier, out _), Is.True);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(1000));
        }

        [Test]
        public void ResetRetainsPermanentNodesWithoutRefund()
        {
            SWSkillTreeNode permanent = Node(), temporary = Node();
            Set(permanent, "retainOnReset", true);
            SWSkillTreeSystem system = System(Tree(permanent, temporary));
            system.Purchase(permanent.Identifier);
            system.Purchase(temporary.Identifier);
            Assert.That(system.Reset(true, false, out _), Is.True);
            Assert.That(system.GetLevel(permanent.Identifier), Is.EqualTo(1));
            Assert.That(system.GetLevel(temporary.Identifier), Is.Zero);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(980));
        }

        [Test]
        public void ResetRejectsBreakingPermanentPrerequisite()
        {
            SWSkillTreeNode parent = Node(), permanent = Node();
            Require(permanent, parent); Set(permanent, "retainOnReset", true);
            SWSkillTreeSystem system = System(Tree(parent, permanent));
            system.Purchase(parent.Identifier); system.Purchase(permanent.Identifier);
            Assert.That(system.Reset(true, true, out _), Is.False);
            Assert.That(wallet.GetBalance("Gold"), Is.EqualTo(980));
            Assert.That(system.GetLevel(parent.Identifier), Is.EqualTo(1));
        }

        [Test]
        public void CyclesAndDuplicateIdentifiersAreRejected()
        {
            SWSkillTreeNode first = Node(), second = Node();
            Require(first, second); Require(second, first);
            Assert.That(SWSkillTreeDefinitionValidator.Validate(Tree(first, second)).Any(error => error.Contains("순환")), Is.True);
            Set(second, "identifier", first.Identifier);
            Assert.That(SWSkillTreeDefinitionValidator.Validate(Tree(first, second)).Any(error => error.Contains("중복")), Is.True);
        }

        [Test]
        public void NonfiniteCostsAndPrecisionLossCannotProduceFreePurchases()
        {
            SWSkillTreeNode node = Node();
            wallet.SetBalance("Gold", 1e100);
            SWSkillTreeSystem system = System(Tree(node));
            Assert.That(system.Purchase(node.Identifier).Success, Is.False);
            Assert.That(system.GetLevel(node.Identifier), Is.Zero);
            ((ConstantCost)node.Skill.Costs[0]).amount = double.PositiveInfinity;
            Assert.That(system.Purchase(node.Identifier).Success, Is.False);
        }

        [Test]
        public void RepeatedRestoreDoesNotDuplicateStatEffectsAndDisposeRemovesOwnBonus()
        {
            SWStat stat = Asset<SWStat>();
            Set(stat, "maxValue", 1000f);
            GameObject owner = new("TestStats"); owner.SetActive(false); created.Add(owner);
            SWStats stats = owner.AddComponent<SWStats>();
            Set(stats, "setupOnAwake", false);
            Set(stats, "statOverrides", new[] { new SWStatOverride(stat) });
            owner.SetActive(true);
            stats.Setup();
            object unrelatedSource = new();
            stats.SetBonusValue(stat, unrelatedSource, 7);
            SWSkillTreeStatEffect effect = Asset<SWSkillTreeStatEffect>();
            Set(effect, "stat", stat); Set(effect, "amountPerLevel", 2f);
            SWSkillTreeNode node = Node();
            Set(node.Skill, "effects", new SWSkillTreeEffect[] { effect });
            SWSkillTreeSystem system = System(Tree(node), new SWSkillTreeGameContext(stats));
            SWSkillTreeEffectBinding binding = new(system); disposables.Add(binding);
            system.Purchase(node.Identifier, 3);
            SWSkillTreeSaveData save = system.CaptureSaveData();
            Assert.That(stats.GetBonusValue(stat), Is.EqualTo(13));
            system.Restore(save, out _); system.Restore(save, out _);
            Assert.That(stats.GetBonusValue(stat), Is.EqualTo(13));
            binding.Dispose();
            Assert.That(stats.GetBonusValue(stat), Is.EqualTo(7));
        }

        [TestCase(SWSkillTreeCostGrowth.Fixed, 10)]
        [TestCase(SWSkillTreeCostGrowth.Linear, 16)]
        [TestCase(SWSkillTreeCostGrowth.Exponential, 80)]
        public void BuiltInCostCurvesUseCurrentLevel(SWSkillTreeCostGrowth growth, double expected)
        {
            SWSkillTreeFormulaCost cost = new();
            Set(cost, "growth", growth); Set(cost, "initialCost", 10d); Set(cost, "growthValue", 2d);
            Assert.That(cost.Evaluate(3).value, Is.EqualTo(expected));
        }

        /// <summary>자동 배치가 선행 순서와 간격을 보장하고 정의 좌표를 보존하는지 검사합니다.</summary>
        [Test]
        public void AutomaticLayoutPreservesDefinitionAndSeparatesBranches()
        {
            SWSkillTreeNode root = Node();
            SWSkillTreeNode first = Node();
            SWSkillTreeNode second = Node();
            SWSkillTreeNode merged = Node();
            Require(first, root); Require(second, root);
            Require(merged, first); Require(merged, second);
            SWSkillTreeDefinition tree = Tree(merged, second, root, first);
            Vector2 nodeSize = new(180, 92);
            Vector2 spacing = new(60, 80);
            IReadOnlyDictionary<string, Vector2> layout = SWSkillTreeAutoLayout.Calculate(tree, nodeSize, spacing);
            Assert.That(layout[first.Identifier].y, Is.LessThan(layout[root.Identifier].y));
            Assert.That(layout[merged.Identifier].y, Is.LessThan(layout[first.Identifier].y));
            Assert.That(Math.Abs(layout[first.Identifier].x - layout[second.Identifier].x), Is.GreaterThanOrEqualTo(nodeSize.x + spacing.x));
            Assert.That(tree.Nodes.All(node => node.Position == Vector2.zero), Is.True);
            Set(tree, "nodes", tree.Nodes.Reverse().ToList());
            IReadOnlyDictionary<string, Vector2> reordered = SWSkillTreeAutoLayout.Calculate(tree, nodeSize, spacing);
            foreach (SWSkillTreeNode node in tree.Nodes)
                Assert.That(reordered[node.Identifier], Is.EqualTo(layout[node.Identifier]));
        }

        /// <summary>순환된 정의가 무한 재귀 없이 거절되는지 검사합니다.</summary>
        [Test]
        public void AutomaticLayoutRejectsCycles()
        {
            SWSkillTreeNode first = Node();
            SWSkillTreeNode second = Node();
            Require(first, second); Require(second, first);
            Assert.Throws<ArgumentException>(() => SWSkillTreeAutoLayout.Calculate(Tree(first, second), new Vector2(180, 92), Vector2.zero));
        }

        /// <summary>저장된 원점도 자동 배치보다 우선하며 신규 노드만 빈 공간에 배치되는지 확인합니다.</summary>
        [Test]
        public void SavedPositionsTakePriorityOverAutomaticLayout()
        {
            SWSkillTreeNode saved = Node();
            SWSkillTreeNode added = Node();
            Set(added, "hasSavedPosition", false);
            SWSkillTreeDefinition tree = Tree(saved, added);
            var positions = SWSkillTreeAutoLayout.Resolve(tree, new Vector2(180, 92), new Vector2(60, 80));
            Assert.That(positions[saved.Identifier], Is.EqualTo(Vector2.zero));
            Assert.That(Math.Abs(positions[added.Identifier].x), Is.GreaterThanOrEqualTo(240));
            Set(saved, "position", new Vector2(350, 120));
            positions = SWSkillTreeAutoLayout.Resolve(tree, new Vector2(180, 92), new Vector2(60, 80));
            Assert.That(positions[saved.Identifier], Is.EqualTo(new Vector2(350, -120)));
        }

        /// <summary>선행 노드 습득 공개와 요구 레벨 공개를 구분하고 가려진 노드의 구매를 차단합니다.</summary>
        [Test]
        public void RevealPoliciesSeparateVisibilityFromPurchaseRequirements()
        {
            SWSkillTreeNode parent = Node();
            SWSkillTreeNode child = Node();
            Require(child, parent, 3);
            Set(child, "revealPolicy", SWSkillTreeRevealPolicy.PrerequisiteLearned);
            SWSkillTreeSystem system = System(Tree(parent, child));
            Assert.That(system.IsVisible(child.Identifier), Is.False);
            Set(child, "concealment", SWSkillTreeConcealment.Masked);
            Assert.That(system.IsVisible(child.Identifier), Is.True);
            Assert.That(system.IsRevealed(child.Identifier), Is.False);
            Assert.That(system.Purchase(child.Identifier).Success, Is.False);
            Assert.That(system.Purchase(parent.Identifier).Success, Is.True);
            Assert.That(system.IsRevealed(child.Identifier), Is.True);
            Assert.That(system.Purchase(child.Identifier).Success, Is.False);
            Set(child, "revealPolicy", SWSkillTreeRevealPolicy.PrerequisiteLevelReached);
            Assert.That(system.IsRevealed(child.Identifier), Is.False);
            Assert.That(system.Purchase(parent.Identifier, 2).Success, Is.True);
            Assert.That(system.IsRevealed(child.Identifier), Is.True);
            Assert.That(system.Purchase(child.Identifier).Success, Is.True);
        }

        /// <summary>모든 선행 조건과 하나 이상 조건을 정보 공개에도 동일하게 적용합니다.</summary>
        [Test]
        public void RevealPolicyRespectsAllAndAnyPrerequisites()
        {
            SWSkillTreeNode first = Node();
            SWSkillTreeNode second = Node();
            SWSkillTreeNode child = Node();
            Require(child, first); Require(child, second);
            Set(child, "revealPolicy", SWSkillTreeRevealPolicy.PrerequisiteLearned);
            SWSkillTreeSystem system = System(Tree(first, second, child));
            Assert.That(system.Purchase(first.Identifier).Success, Is.True);
            Assert.That(system.IsRevealed(child.Identifier), Is.False);
            Set(child, "requirementMode", SWSkillTreeRequirementMode.Any);
            Assert.That(system.IsRevealed(child.Identifier), Is.True);
            Set(child, "requirementMode", SWSkillTreeRequirementMode.All);
            Assert.That(system.Purchase(second.Identifier).Success, Is.True);
            Assert.That(system.IsRevealed(child.Identifier), Is.True);
        }

        private SWSkillTreeNode Node(int maximum = 10)
        {
            SWSkillDefinition skill = Asset<SWSkillDefinition>();
            Set(skill, "maximumLevel", maximum);
            Set(skill, "costs", new SWSkillTreeCost[] { new ConstantCost() });
            return new SWSkillTreeNode(skill, Vector2.zero);
        }
        private T Asset<T>() where T : ScriptableObject
        { T asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset; }
        private SWSkillTreeDefinition Tree(params SWSkillTreeNode[] nodes)
        { SWSkillTreeDefinition tree = Asset<SWSkillTreeDefinition>(); Set(tree, "nodes", nodes.ToList()); return tree; }
        private SWSkillTreeSystem System(SWSkillTreeDefinition definition, object context = null)
        { SWSkillTreeSystem system = new(definition, wallet, context); disposables.Add(system); return system; }
        private static void Require(SWSkillTreeNode child, SWSkillTreeNode parent, int level = 1)
            => ((List<SWSkillTreeRequirement>)child.Requirements).Add(new SWSkillTreeRequirement(parent.Identifier, level));
        internal static void Set(object target, string name, object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
            throw new MissingFieldException(name);
        }
    }
}
