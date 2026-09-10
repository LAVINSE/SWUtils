using System;
using UnityEngine;
using SW.Data;
using SW.Util;

namespace SW.SkillTree
{
    /// <summary>SWUtils의 암호화된 설정 저장소에 스킬트리 진행을 저장합니다.</summary>
    public sealed class SWSkillTreePlayerPrefsSaveStore : ISWSkillTreeSaveStore
    {
        /// <inheritdoc />
        public bool Save(string key, SWSkillTreeSaveData data)
        {
            if (string.IsNullOrWhiteSpace(key) || data == null) return false;
            try { SWPlayerPrefs.SetString(key, JsonUtility.ToJson(data)); SWPlayerPrefs.Save(); return true; }
            catch (Exception exception) { SWLog.LogError($"[SWSkillTree] 저장 실패: {exception.Message}"); return false; }
        }
        /// <inheritdoc />
        public bool TryLoad(string key, out SWSkillTreeSaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(key) || !SWPlayerPrefs.HasKey(key)) return false;
            try { data = JsonUtility.FromJson<SWSkillTreeSaveData>(SWPlayerPrefs.GetString(key)); return data != null; }
            catch (Exception exception) { SWLog.LogError($"[SWSkillTree] 불러오기 실패: {exception.Message}"); return false; }
        }
    }
}
