using System;
using System.Collections.Generic;
using SW.Util;

namespace SW.SkillTree
{
    /// <summary>진행 상태를 프로젝트 효과에 동기화합니다. 출처별 교체로 복원 시 중복 적용을 방지합니다.</summary>
    public sealed class SWSkillTreeEffectBinding : IDisposable
    {
        private sealed class Entry
        {
            internal string identifier;
            internal SWSkillTreeEffect effect;
            internal object source = new();
            internal int appliedLevel = -1;
        }
        private readonly SWSkillTreeSystem system;
        private readonly List<Entry> entries = new();
        private bool disposed;
        /// <summary>실패한 사용자 효과를 알립니다. 진행은 유지하며 다음 동기화에서 다시 시도합니다.</summary>
        public event Action<Exception> Failed;

        /// <summary>각 노드 효과에 독립적인 출처를 할당하고 현재 진행을 적용합니다.</summary>
        public SWSkillTreeEffectBinding(SWSkillTreeSystem system)
        {
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            foreach (SWSkillTreeNode node in system.Definition.Nodes)
                foreach (SWSkillTreeEffect effect in node.Skill.Effects)
                    entries.Add(new Entry { identifier = node.Identifier, effect = effect });
            system.Changed += Synchronize;
            Synchronize();
        }

        /// <summary>변경된 레벨의 효과를 적용하며 이전에 실패한 효과도 다시 시도합니다.</summary>
        public void Synchronize()
        {
            if (disposed) return;
            foreach (Entry entry in entries)
            {
                int level = system.GetLevel(entry.identifier);
                if (entry.appliedLevel == level) continue;
                try { entry.effect.Apply(system.Context, entry.source, level); entry.appliedLevel = level; }
                catch (Exception exception)
                {
                    SWLog.LogError($"[SWSkillTreeEffectBinding] 효과 적용 실패: {exception}");
                    if (Failed != null)
                        foreach (Action<Exception> listener in Failed.GetInvocationList())
                            try { listener(exception); } catch (Exception notification) { SWLog.LogError(notification.Message); }
                }
            }
        }

        /// <summary>이 연결이 만든 효과만 제거하고 구독을 해제합니다. 시스템보다 먼저 호출합니다.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            system.Changed -= Synchronize;
            foreach (Entry entry in entries)
                try { entry.effect.Apply(system.Context, entry.source, 0); }
                catch (Exception exception) { SWLog.LogError($"[SWSkillTreeEffectBinding] 효과 해제 실패: {exception}"); }
        }
    }
}
