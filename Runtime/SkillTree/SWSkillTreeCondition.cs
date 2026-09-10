using System;

namespace SW.SkillTree
{
    /// <summary>직렬화 가능한 프로젝트별 해금 조건입니다. 평가 중 상태를 변경하지 않습니다.</summary>
    [Serializable]
    public abstract class SWSkillTreeCondition
    {
        /// <summary>현재 진행과 프로젝트 문맥으로 조건을 평가하고 실패 이유를 반환합니다.</summary>
        public abstract bool IsSatisfied(SWSkillTreeSystem system, out string reason);
    }
}
