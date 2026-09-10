namespace SW.SkillTree
{
    /// <summary>프로젝트 저장 방식과 스킬트리 진행을 연결합니다.</summary>
    public interface ISWSkillTreeSaveStore
    {
        /// <summary>지정 키에 진행 데이터를 저장합니다.</summary>
        bool Save(string key, SWSkillTreeSaveData data);
        /// <summary>지정 키에서 진행 데이터를 읽습니다.</summary>
        bool TryLoad(string key, out SWSkillTreeSaveData data);
    }
}
