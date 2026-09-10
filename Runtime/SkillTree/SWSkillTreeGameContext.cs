using System;
using System.Collections.Generic;
using System.Linq;
using SW.Stat;

namespace SW.SkillTree
{
    /// <summary>기본 능력치와 기능 해금을 연결하는 선택적 문맥입니다. 다른 프로젝트는 자체 문맥과 효과를 사용할 수 있습니다.</summary>
    public sealed class SWSkillTreeGameContext
    {
        private readonly Dictionary<string, Dictionary<object, int>> features = new(StringComparer.Ordinal);
        /// <summary>스킬 효과를 적용할 런타임 능력치 모음입니다.</summary>
        public SWStats Stats { get; }
        /// <summary>능력치가 없는 프로젝트에서는 빈 값으로 생성할 수 있습니다.</summary>
        public SWSkillTreeGameContext(SWStats stats = null) => Stats = stats;
        /// <summary>해금된 기능의 출처별 레벨 합을 반환합니다.</summary>
        public long GetFeatureLevel(string feature)
            => feature != null && features.TryGetValue(feature, out Dictionary<object, int> sources) ? sources.Values.Sum(level => (long)level) : 0;
        /// <summary>특정 출처가 제공하는 기능 레벨을 교체합니다.</summary>
        public void SetFeatureLevel(string feature, object source, int level)
        {
            if (string.IsNullOrWhiteSpace(feature) || source == null || level < 0) throw new ArgumentException("기능 이름, 출처와 레벨을 확인하세요.");
            if (!features.TryGetValue(feature, out Dictionary<object, int> sources))
            { if (level == 0) return; sources = new(); features.Add(feature, sources); }
            if (level == 0) sources.Remove(source); else sources[source] = level;
        }
    }
}
