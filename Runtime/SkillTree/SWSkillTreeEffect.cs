using SW.Base;

namespace SW.SkillTree
{
    /// <summary>레벨의 절댓값으로 다시 적용 가능한 지속 효과입니다. 일회성 보상은 구매 이벤트에서 처리합니다.</summary>
    public abstract class SWSkillTreeEffect : SWScriptableObject
    {
        /// <summary>같은 출처의 효과를 교체합니다. 레벨이 0이면 해당 출처의 효과만 제거해야 합니다.</summary>
        public abstract void Apply(object context, object source, int level);
        /// <summary>현재 또는 다음 레벨의 효과를 화면에 표시합니다.</summary>
        public abstract string Describe(int level);
    }
}
