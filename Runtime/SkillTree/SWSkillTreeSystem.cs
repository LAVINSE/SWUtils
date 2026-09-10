using System;
using System.Collections.Generic;
using System.Linq;
using SW.Util;

namespace SW.SkillTree
{
    /// <summary>한 소유자의 스킬트리 구매와 진행을 처리합니다. Unity 생명 주기나 전역 인스턴스에 의존하지 않습니다.</summary>
    public sealed partial class SWSkillTreeSystem : IDisposable
    {
        /// <summary>한 요청이 처리할 수 있는 최대 레벨 수입니다. 최대 구매도 이 한도까지 수행합니다.</summary>
        public const int MaximumBatchLevels = 10000;
        private readonly Dictionary<string, SWSkillTreeNode> nodes;
        private Dictionary<string, List<SWSkillTreePayment>> payments = new(StringComparer.Ordinal);
        private bool changing;
        private bool disposed;

        /// <summary>현재 사용 중인 정의입니다. 실행 중에는 정의를 변경하지 않습니다.</summary>
        public SWSkillTreeDefinition Definition { get; }
        /// <summary>프로젝트 재화 연결입니다.</summary>
        public ISWSkillTreeWallet Wallet { get; }
        /// <summary>사용자 정의 조건과 효과에 전달하는 프로젝트 문맥입니다.</summary>
        public object Context { get; }
        /// <summary>진행이나 외부 재화가 변경된 후 발생합니다. 복원과 초기화도 포함합니다.</summary>
        public event Action Changed;
        /// <summary>실제 구매 성공 후에만 발생합니다. 저장 복원에서는 발생하지 않습니다.</summary>
        public event Action<SWSkillTreeNode, int> Purchased;

        /// <summary>정의를 검증하고 독립적인 진행 상태를 생성합니다.</summary>
        public SWSkillTreeSystem(SWSkillTreeDefinition definition, ISWSkillTreeWallet wallet, object context = null)
        {
            List<string> errors = SWSkillTreeDefinitionValidator.Validate(definition);
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors), nameof(definition));
            Definition = definition;
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Context = context;
            nodes = definition.Nodes.ToDictionary(node => node.Identifier, StringComparer.Ordinal);
            Wallet.Changed += OnWalletChanged;
        }

        /// <summary>현재 노드 레벨을 반환합니다.</summary>
        public int GetLevel(string identifier) => identifier != null && payments.TryGetValue(identifier, out List<SWSkillTreePayment> history) ? history.Count : 0;
        /// <summary>식별자로 노드를 찾습니다.</summary>
        public bool TryGetNode(string identifier, out SWSkillTreeNode node)
        {
            node = null;
            return identifier != null && nodes.TryGetValue(identifier, out node);
        }
        /// <summary>표시 조건과 습득 상태로 노드 표시 여부를 반환합니다.</summary>
        public bool IsVisible(string identifier)
            => TryGetNode(identifier, out SWSkillTreeNode node) &&
                (IsRevealed(identifier) || node.Concealment == SWSkillTreeConcealment.Masked);

        /// <summary>노드 이름, 효과와 구매 정보를 공개할 수 있는지 반환합니다. 습득한 노드는 항상 공개합니다.</summary>
        public bool IsRevealed(string identifier)
        {
            if (!TryGetNode(identifier, out SWSkillTreeNode node)) return false;
            if (GetLevel(identifier) > 0) return true;
            if (!CheckConditions(node.VisibilityConditions, out _)) return false;
            if (node.RevealPolicy == SWSkillTreeRevealPolicy.Always || node.Requirements.Count == 0) return true;
            bool Satisfied(SWSkillTreeRequirement requirement) => GetLevel(requirement.NodeIdentifier) >=
                (node.RevealPolicy == SWSkillTreeRevealPolicy.PrerequisiteLearned ? 1 : requirement.RequiredLevel);
            return node.RequirementMode == SWSkillTreeRequirementMode.All
                ? node.Requirements.All(Satisfied) : node.Requirements.Any(Satisfied);
        }
        /// <summary>외부 조건 값이 바뀌었을 때 화면과 효과 연결에 재평가를 알립니다.</summary>
        public void Refresh() { if (!disposed && !changing) NotifyChanged(); }

        /// <summary>지정 레벨 수 또는 한도 내 최대 구매의 비용과 실패 사유를 조회합니다.</summary>
        public SWSkillTreePurchase PreviewPurchase(string identifier, int levels = 1, bool maximum = false)
        {
            if (disposed || changing) return Failure("Skill tree is unavailable or busy.");
            // 사용자 조건이나 비용 계산기에서도 구매를 재진입하지 못하게 합니다.
            changing = true;
            try { return BuildPurchase(identifier, levels, maximum, out _); }
            catch (Exception exception) { return Failure($"Purchase calculation failed: {exception.Message}"); }
            finally { changing = false; }
        }

        /// <summary>최신 조건과 잔액으로 견적을 다시 계산하여 전체 비용을 한 번에 차감합니다.</summary>
        public SWSkillTreePurchase Purchase(string identifier, int levels = 1, bool maximum = false)
        {
            if (disposed || changing) return Failure("Skill tree is unavailable or busy.");
            changing = true;
            try
            {
                SWSkillTreePurchase result = BuildPurchase(identifier, levels, maximum, out List<SWSkillTreePayment> additions);
                if (!result.Success) return result;
                if (!Wallet.TryExchange(result.Costs, false)) return Failure("Currency changed or the transaction could not be processed.");
                if (!payments.TryGetValue(identifier, out List<SWSkillTreePayment> history))
                { history = new(); payments.Add(identifier, history); }
                history.AddRange(additions);
                NotifyChanged();
                if (Purchased != null)
                    foreach (Action<SWSkillTreeNode, int> listener in Purchased.GetInvocationList())
                        InvokeSafely(() => listener(nodes[identifier], result.Levels));
                return result;
            }
            catch (Exception exception) { return Failure($"Purchase failed: {exception.Message}"); }
            finally { changing = false; }
        }

        private SWSkillTreePurchase BuildPurchase(string identifier, int levels, bool maximum, out List<SWSkillTreePayment> additions)
        {
            additions = new();
            if (!TryGetNode(identifier, out SWSkillTreeNode node)) return Failure("Node not found.");
            int currentLevel = GetLevel(identifier);
            if (currentLevel >= node.Skill.MaximumLevel) return Failure("Maximum level reached.");
            if (!IsRevealed(identifier)) return Failure("This skill is not revealed yet.");
            if (!RequirementsSatisfied(node, GetLevel)) return Failure("Prerequisite levels are required.");
            if (!string.IsNullOrWhiteSpace(node.ExclusiveGroup) && nodes.Values.Any(other => other != node
                && other.ExclusiveGroup == node.ExclusiveGroup && GetLevel(other.Identifier) > 0)) return Failure("An exclusive branch is already learned.");
            if (!CheckConditions(node.PurchaseConditions, out string reason)) return Failure(reason);
            if (levels < 1 || (!maximum && levels > MaximumBatchLevels)) return Failure($"Purchase between 1 and {MaximumBatchLevels} levels.");
            int count = maximum ? Math.Min(MaximumBatchLevels, node.Skill.MaximumLevel - currentLevel) : levels;
            if (count > node.Skill.MaximumLevel - currentLevel) return Failure("Requested levels exceed the maximum.");
            Dictionary<string, double> totals = new(StringComparer.Ordinal);
            for (int offset = 0; offset < count; offset++)
            {
                Dictionary<string, double> single = new(StringComparer.Ordinal);
                foreach (SWSkillTreeCost cost in node.Skill.Costs)
                {
                    SWSkillTreeAmount amount = cost.Evaluate(currentLevel + offset);
                    if (!amount.IsValid) return Failure("Cost is outside the valid numeric range.");
                    single.TryGetValue(amount.currency, out double previous);
                    if (!new SWSkillTreeAmount(amount.currency, previous + amount.value).IsValid
                        || (amount.value > 0 && previous + amount.value <= previous))
                        return Failure("Level cost exceeds numeric precision.");
                    single[amount.currency] = previous + amount.value;
                }
                bool affordable = true;
                foreach (KeyValuePair<string, double> entry in single)
                {
                    totals.TryGetValue(entry.Key, out double previous);
                    double total = previous + entry.Value;
                    if (!new SWSkillTreeAmount(entry.Key, total).IsValid || (entry.Value > 0 && total <= previous))
                        return Failure("Total cost exceeds numeric precision.");
                    double balance = Wallet.GetBalance(entry.Key);
                    if (!new SWSkillTreeAmount(entry.Key, balance).IsValid || !SWSkillTreeAmount.CanSubtract(balance, total)) affordable = false;
                }
                if (!affordable)
                {
                    if (!maximum || additions.Count == 0) return Failure("Insufficient currency or numeric precision.", ToAmounts(single));
                    break;
                }
                foreach (KeyValuePair<string, double> entry in single)
                { totals.TryGetValue(entry.Key, out double previous); totals[entry.Key] = previous + entry.Value; }
                additions.Add(new SWSkillTreePayment { amounts = ToAmounts(single).ToList() });
            }
            return new SWSkillTreePurchase(true, maximum && additions.Count == MaximumBatchLevels
                ? $"Purchase limited to {MaximumBatchLevels} levels per request." : string.Empty, additions.Count, ToAmounts(totals));
        }

        private bool CheckConditions(IReadOnlyList<SWSkillTreeCondition> conditions, out string reason)
        {
            reason = string.Empty;
            foreach (SWSkillTreeCondition condition in conditions)
            {
                try
                {
                    if (condition != null && condition.IsSatisfied(this, out reason)) continue;
                    if (string.IsNullOrWhiteSpace(reason)) reason = "Additional requirements must be met.";
                    return false;
                }
                catch (Exception exception) { reason = $"Condition evaluation failed: {exception.Message}"; return false; }
            }
            return true;
        }

        private static bool RequirementsSatisfied(SWSkillTreeNode node, Func<string, int> level)
        {
            if (node.Requirements.Count == 0) return true;
            return node.RequirementMode == SWSkillTreeRequirementMode.All
                ? node.Requirements.All(requirement => level(requirement.NodeIdentifier) >= requirement.RequiredLevel)
                : node.Requirements.Any(requirement => level(requirement.NodeIdentifier) >= requirement.RequiredLevel);
        }
        private static SWSkillTreeAmount[] ToAmounts(Dictionary<string, double> values)
            => values.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => new SWSkillTreeAmount(entry.Key, entry.Value)).ToArray();
        private static SWSkillTreePurchase Failure(string reason, SWSkillTreeAmount[] costs = null) => new(false, reason, costs: costs);
        private void OnWalletChanged() { if (!changing && !disposed) NotifyChanged(); }
        private void NotifyChanged()
        {
            if (Changed != null) foreach (Action listener in Changed.GetInvocationList()) InvokeSafely(listener);
        }
        private static void InvokeSafely(Action action)
        {
            try { action(); }
            catch (Exception exception) { SWLog.LogError($"[SWSkillTreeSystem] 변경 알림 실패: {exception}"); }
        }
        /// <summary>외부 지갑의 이벤트 구독을 해제합니다.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Wallet.Changed -= OnWalletChanged;
            Changed = null;
            Purchased = null;
        }
    }
}
