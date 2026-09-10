using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SW.SkillTree
{
    /// <summary>선행 관계의 깊이별로 노드를 겹치지 않게 배치합니다. 정의에 저장한 좌표는 변경하지 않습니다.</summary>
    public static class SWSkillTreeAutoLayout
    {
        #region 배치
        /// <summary>저장 좌표를 우선 사용하고 좌표가 없는 노드만 빈 공간에 자동 배치합니다.</summary>
        public static IReadOnlyDictionary<string, Vector2> Resolve(SWSkillTreeDefinition definition, Vector2 nodeSize, Vector2 spacing)
        {
            Dictionary<string, Vector2> result = definition.Nodes.Where(node => node.HasSavedPosition)
                .ToDictionary(node => node.Identifier, node => new Vector2(node.Position.x, -node.Position.y));
            if (result.Count == definition.Nodes.Count) return result;
            IReadOnlyDictionary<string, Vector2> calculated = Calculate(definition, nodeSize, spacing);
            foreach (SWSkillTreeNode node in definition.Nodes.Where(node => !node.HasSavedPosition).OrderBy(node => node.Identifier, StringComparer.Ordinal))
            {
                Vector2 position = calculated[node.Identifier];
                while (result.Values.Any(other => Mathf.Abs(other.x - position.x) < Mathf.Max(1, nodeSize.x) + Mathf.Max(0, spacing.x)
                    && Mathf.Abs(other.y - position.y) < Mathf.Max(1, nodeSize.y) + Mathf.Max(0, spacing.y)))
                    position.x += Mathf.Max(1, nodeSize.x) + Mathf.Max(0, spacing.x);
                result.Add(node.Identifier, position);
            }
            return result;
        }

        /// <summary>선행 노드는 위에, 후속 노드는 아래에 배치한 화면 좌표를 반환합니다.</summary>
        /// <param name="definition">배치할 트리 정의입니다.</param>
        /// <param name="nodeSize">노드 사각형의 크기입니다.</param>
        /// <param name="spacing">노드 사이의 가로와 세로 여백입니다.</param>
        /// <returns>노드 식별자별 화면 좌표입니다. 위쪽이 양수입니다.</returns>
        public static IReadOnlyDictionary<string, Vector2> Calculate(
            SWSkillTreeDefinition definition, Vector2 nodeSize, Vector2 spacing)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            Dictionary<string, SWSkillTreeNode> nodes = definition.Nodes.ToDictionary(node => node.Identifier, StringComparer.Ordinal);
            Dictionary<string, int> depths = new(StringComparer.Ordinal);
            HashSet<string> visiting = new(StringComparer.Ordinal);

            int GetDepth(string identifier)
            {
                if (depths.TryGetValue(identifier, out int depth)) return depth;
                if (!nodes.TryGetValue(identifier, out SWSkillTreeNode node))
                    throw new ArgumentException("선행 노드가 존재하지 않습니다.", nameof(definition));
                if (!visiting.Add(identifier))
                    throw new ArgumentException("순환 연결은 자동 배치할 수 없습니다.", nameof(definition));
                depth = node.Requirements.Count == 0 ? 0 : node.Requirements.Max(requirement => GetDepth(requirement.NodeIdentifier)) + 1;
                visiting.Remove(identifier);
                depths.Add(identifier, depth);
                return depth;
            }

            foreach (SWSkillTreeNode node in definition.Nodes) GetDepth(node.Identifier);
            Vector2 step = new(Mathf.Max(1f, nodeSize.x) + Mathf.Max(0f, spacing.x),
                Mathf.Max(1f, nodeSize.y) + Mathf.Max(0f, spacing.y));
            Dictionary<string, Vector2> positions = new(StringComparer.Ordinal);
            foreach (IGrouping<int, SWSkillTreeNode> layer in definition.Nodes.GroupBy(node => depths[node.Identifier]).OrderBy(group => group.Key))
            {
                // 같은 깊이에서는 부모들의 중심과 식별자로 정렬하여 입력 목록 순서에 영향을 받지 않습니다.
                SWSkillTreeNode[] ordered = layer.OrderBy(node => node.Requirements.Count == 0 ? 0f
                    : node.Requirements.Average(requirement => positions[requirement.NodeIdentifier].x))
                    .ThenBy(node => node.Identifier, StringComparer.Ordinal).ToArray();
                for (int index = 0; index < ordered.Length; index++)
                    positions.Add(ordered[index].Identifier, new Vector2((index - (ordered.Length - 1) * 0.5f) * step.x, -layer.Key * step.y));
            }
            return positions;
        }
        #endregion // 배치
    }
}
