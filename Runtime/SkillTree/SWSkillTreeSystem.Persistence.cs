using System;
using System.Collections.Generic;
using System.Linq;

namespace SW.SkillTree
{
    public sealed partial class SWSkillTreeSystem
    {
        /// <summary>마지막 한 레벨을 실제 지불 비용으로 환불합니다. 후속 습득 조건이 깨지면 거절합니다.</summary>
        public bool Refund(string identifier, out string reason)
        {
            reason = string.Empty;
            if (disposed || changing || GetLevel(identifier) == 0) { reason = "No learned level available to refund."; return false; }
            changing = true;
            try
            {
                Dictionary<string, List<SWSkillTreePayment>> candidate = ClonePayments(payments);
                List<SWSkillTreePayment> history = candidate[identifier];
                SWSkillTreePayment last = history[history.Count - 1];
                history.RemoveAt(history.Count - 1);
                if (!ValidateProgress(candidate, out reason)) return false;
                if (!Wallet.TryExchange(last.amounts, true)) { reason = "Refund could not be processed."; return false; }
                payments = candidate;
                NotifyChanged();
                return true;
            }
            catch (Exception exception) { reason = $"Refund failed: {exception.Message}"; return false; }
            finally { changing = false; }
        }

        /// <summary>전체 또는 영구 노드를 제외한 진행을 초기화합니다. 환생에서는 환급을 끌 수 있습니다.</summary>
        public bool Reset(bool retainPermanentNodes, bool refundPayments, out string reason)
        {
            reason = string.Empty;
            if (disposed || changing) { reason = "Skill tree is busy or disposed."; return false; }
            changing = true;
            try
            {
                Dictionary<string, List<SWSkillTreePayment>> candidate = ClonePayments(payments);
                Dictionary<string, double> refunds = new(StringComparer.Ordinal);
                foreach (string identifier in candidate.Keys.ToArray())
                {
                    if (retainPermanentNodes && nodes[identifier].RetainOnReset) continue;
                    if (refundPayments)
                        foreach (SWSkillTreePayment payment in candidate[identifier])
                            foreach (SWSkillTreeAmount amount in payment.amounts)
                            {
                                refunds.TryGetValue(amount.currency, out double previous);
                                double total = previous + amount.value;
                                if (!new SWSkillTreeAmount(amount.currency, total).IsValid || (amount.value > 0 && total <= previous))
                                { reason = "Refund total exceeds numeric precision."; return false; }
                                refunds[amount.currency] = total;
                            }
                    candidate.Remove(identifier);
                }
                if (!ValidateProgress(candidate, out reason)) return false;
                if (refundPayments && !Wallet.TryExchange(ToAmounts(refunds), true)) { reason = "Refund could not be processed."; return false; }
                payments = candidate;
                NotifyChanged();
                return true;
            }
            catch (Exception exception) { reason = $"Reset failed: {exception.Message}"; return false; }
            finally { changing = false; }
        }

        /// <summary>외부에서 수정해도 실행 상태에 영향을 주지 않는 저장 사본을 만듭니다.</summary>
        public SWSkillTreeSaveData CaptureSaveData()
        {
            SWSkillTreeSaveData data = new() { treeIdentifier = Definition.Identifier };
            foreach (KeyValuePair<string, List<SWSkillTreePayment>> entry in ClonePayments(payments))
                if (entry.Value.Count > 0) data.nodes.Add(new SWSkillTreeNodeSaveData { nodeIdentifier = entry.Key, payments = entry.Value });
            return data;
        }

        /// <summary>전체 데이터를 검증한 뒤 진행을 교체합니다. 잔액을 변경하거나 구매 보상을 다시 지급하지 않습니다.</summary>
        public bool Restore(SWSkillTreeSaveData data, out string reason)
        {
            reason = string.Empty;
            if (disposed || changing) { reason = "Skill tree is busy or disposed."; return false; }
            if (data == null || data.version != 1 || data.treeIdentifier != Definition.Identifier || data.nodes == null)
            { reason = "Save version or tree does not match."; return false; }
            changing = true;
            try
            {
                Dictionary<string, List<SWSkillTreePayment>> candidate = new(StringComparer.Ordinal);
                foreach (SWSkillTreeNodeSaveData entry in data.nodes)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.nodeIdentifier) || entry.payments == null
                        || !nodes.ContainsKey(entry.nodeIdentifier) || !candidate.TryAdd(entry.nodeIdentifier, entry.payments))
                    { reason = "Saved nodes are missing or duplicated. Migrate the save data."; return false; }
                    foreach (SWSkillTreePayment payment in entry.payments)
                    {
                        if (payment == null || payment.amounts == null || payment.amounts.Any(amount => !amount.IsValid))
                        { reason = "Saved payment history is invalid."; return false; }
                        HashSet<string> currencies = new(StringComparer.Ordinal);
                        if (payment.amounts.Any(amount => !currencies.Add(amount.currency)))
                        { reason = "Payment history contains duplicate currencies."; return false; }
                    }
                }
                if (!ValidateProgress(candidate, out reason)) return false;
                payments = ClonePayments(candidate);
                NotifyChanged();
                return true;
            }
            catch (Exception exception) { reason = $"Restore failed: {exception.Message}"; return false; }
            finally { changing = false; }
        }

        private bool ValidateProgress(Dictionary<string, List<SWSkillTreePayment>> candidate, out string reason)
        {
            reason = string.Empty;
            int Level(string identifier) => candidate.TryGetValue(identifier, out List<SWSkillTreePayment> history) ? history.Count : 0;
            HashSet<string> groups = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<SWSkillTreePayment>> entry in candidate)
            {
                if (entry.Value.Count == 0) continue;
                SWSkillTreeNode node = nodes[entry.Key];
                if (entry.Value.Count > node.Skill.MaximumLevel) { reason = $"Saved level exceeds maximum: {node.Skill.DisplayName}"; return false; }
                if (!RequirementsSatisfied(node, Level)) { reason = $"A learned node still needs this prerequisite: {node.Skill.DisplayName}"; return false; }
                if (!string.IsNullOrWhiteSpace(node.ExclusiveGroup) && !groups.Add(node.ExclusiveGroup))
                { reason = "Conflicting exclusive branches are present."; return false; }
            }
            return true;
        }

        private static Dictionary<string, List<SWSkillTreePayment>> ClonePayments(Dictionary<string, List<SWSkillTreePayment>> source)
            => source.ToDictionary(entry => entry.Key,
                entry => entry.Value.Select(payment => new SWSkillTreePayment { amounts = new List<SWSkillTreeAmount>(payment.amounts) }).ToList(),
                StringComparer.Ordinal);
    }
}
