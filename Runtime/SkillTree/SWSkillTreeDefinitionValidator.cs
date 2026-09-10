using System;
using System.Collections.Generic;

namespace SW.SkillTree
{
    /// <summary>실행 전에 스킬트리의 식별자, 선행 연결과 기본 성장 설정을 검사합니다.</summary>
    public static class SWSkillTreeDefinitionValidator
    {
        /// <summary>잘못된 설정의 설명을 반환합니다. 빈 목록이면 실행할 수 있습니다.</summary>
        public static List<string> Validate(SWSkillTreeDefinition definition)
        {
            List<string> errors = new();
            if (definition == null) { errors.Add("스킬트리 정의가 없습니다."); return errors; }
            if (string.IsNullOrWhiteSpace(definition.Identifier)) errors.Add("트리 식별자가 없습니다.");
            Dictionary<string, SWSkillTreeNode> nodes = new(StringComparer.Ordinal);
            foreach (SWSkillTreeNode node in definition.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Identifier)) { errors.Add("노드 또는 식별자가 없습니다."); continue; }
                if (!nodes.TryAdd(node.Identifier, node)) errors.Add($"중복 노드 식별자: {node.Identifier}");
                if (!Enum.IsDefined(typeof(SWSkillTreeRequirementMode), node.RequirementMode)) errors.Add($"선행 조건 방식이 잘못되었습니다: {node.Identifier}");
                if (!Enum.IsDefined(typeof(SWSkillTreeRevealPolicy), node.RevealPolicy)) errors.Add($"정보 공개 조건이 잘못되었습니다: {node.Identifier}");
                if (!Enum.IsDefined(typeof(SWSkillTreeConcealment), node.Concealment)) errors.Add($"공개 전 표시 방식이 잘못되었습니다: {node.Identifier}");
                if (float.IsNaN(node.Position.x) || float.IsNaN(node.Position.y) || float.IsInfinity(node.Position.x) || float.IsInfinity(node.Position.y))
                    errors.Add($"노드 배치 좌표가 유효하지 않습니다: {node.Identifier}");
                if (node.Skill == null) { errors.Add($"스킬을 연결하세요: {node.Identifier}"); continue; }
                if (node.Skill.MaximumLevel < 1) errors.Add($"최대 레벨은 1 이상이어야 합니다: {node.Skill.name}");
                foreach (SWSkillTreeCost cost in node.Skill.Costs)
                {
                    try
                    {
                        if (cost == null || !cost.Evaluate(0).IsValid || !cost.Evaluate(node.Skill.MaximumLevel - 1).IsValid)
                            errors.Add($"비용 설정 또는 최대 레벨 비용을 확인하세요: {node.Skill.name}");
                    }
                    catch (Exception exception) { errors.Add($"비용 계산 실패: {node.Skill.name} / {exception.Message}"); }
                }
                foreach (SWSkillTreeEffect effect in node.Skill.Effects)
                    if (effect == null) errors.Add($"효과 참조가 없습니다: {node.Skill.name}");
                foreach (SWSkillTreeCondition condition in node.VisibilityConditions)
                    if (condition == null) errors.Add($"표시 조건이 없습니다: {node.Skill.name}");
                foreach (SWSkillTreeCondition condition in node.PurchaseConditions)
                    if (condition == null) errors.Add($"구매 조건이 없습니다: {node.Skill.name}");
            }
            foreach (SWSkillTreeNode node in nodes.Values)
            {
                HashSet<string> seen = new(StringComparer.Ordinal);
                foreach (SWSkillTreeRequirement requirement in node.Requirements)
                {
                    if (requirement == null || string.IsNullOrWhiteSpace(requirement.NodeIdentifier)
                        || !nodes.TryGetValue(requirement.NodeIdentifier, out SWSkillTreeNode parent))
                    { errors.Add($"존재하지 않는 선행 연결: {node.Identifier}"); continue; }
                    if (!seen.Add(requirement.NodeIdentifier)) errors.Add($"중복 선행 연결: {node.Identifier}");
                    if (requirement.RequiredLevel < 1 || parent.Skill == null || requirement.RequiredLevel > parent.Skill.MaximumLevel)
                        errors.Add($"선행 요구 레벨을 확인하세요: {node.Identifier}");
                    if (node.RequirementMode == SWSkillTreeRequirementMode.All && !string.IsNullOrWhiteSpace(node.ExclusiveGroup)
                        && node.ExclusiveGroup == parent.ExclusiveGroup)
                        errors.Add($"동시에 습득할 수 없는 선행 노드입니다: {node.Identifier}");
                }
            }
            Dictionary<string, int> visits = new(StringComparer.Ordinal);
            foreach (string identifier in nodes.Keys)
                if (HasCycle(identifier, nodes, visits)) { errors.Add("선행 연결에 순환이 있습니다."); break; }
            return errors;
        }

        private static bool HasCycle(string identifier, Dictionary<string, SWSkillTreeNode> nodes, Dictionary<string, int> visits)
        {
            if (visits.TryGetValue(identifier, out int state)) return state == 1;
            visits[identifier] = 1;
            foreach (SWSkillTreeRequirement requirement in nodes[identifier].Requirements)
                if (requirement != null && requirement.NodeIdentifier != null && nodes.ContainsKey(requirement.NodeIdentifier)
                    && HasCycle(requirement.NodeIdentifier, nodes, visits)) return true;
            visits[identifier] = 2;
            return false;
        }
    }
}
