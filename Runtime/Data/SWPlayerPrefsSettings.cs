using UnityEngine;

namespace SW.Data
{
    /// <summary>
    /// SWPlayerPrefs 암호화 설정을 보관하는 ScriptableObject입니다.
    /// 프로젝트의 Resources 폴더에 같은 이름으로 생성하면 런타임에서 자동으로 사용합니다.
    /// </summary>
    public class SWPlayerPrefsSettings : ScriptableObject
    {
        /// <summary>Resources 폴더에서 찾는 설정 에셋 이름입니다.</summary>
        public const string ResourceAssetName = "SWPlayerPrefsSettings";

        /// <summary>기본 salt 값입니다. 프로젝트별 설정 에셋이 없을 때 사용합니다.</summary>
        public const string DefaultSalt = "SwUtilsPrefs_2026_SaltKey_ChangeMe";

        /// <summary>기본 IV salt 값입니다. 프로젝트별 설정 에셋이 없을 때 사용합니다.</summary>
        public const string DefaultIVSalt = "SwUtilsIVSalt";

        [SerializeField] private string salt = DefaultSalt;
        [SerializeField] private string ivSalt = DefaultIVSalt;

        /// <summary>암호화 키와 저장 키 해시에 사용할 salt 값입니다.</summary>
        public string Salt => string.IsNullOrWhiteSpace(salt) ? DefaultSalt : salt;

        /// <summary>암호화 IV 생성에 사용할 salt 값입니다.</summary>
        public string IVSalt => string.IsNullOrWhiteSpace(ivSalt) ? DefaultIVSalt : ivSalt;

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 창에서 salt 값을 변경합니다.
        /// </summary>
        /// <param name="newSalt">새 salt 값입니다.</param>
        /// <param name="newIVSalt">새 IV salt 값입니다.</param>
        public void SetValues(string newSalt, string newIVSalt)
        {
            salt = string.IsNullOrWhiteSpace(newSalt) ? DefaultSalt : newSalt.Trim();
            ivSalt = string.IsNullOrWhiteSpace(newIVSalt) ? DefaultIVSalt : newIVSalt.Trim();
        }
#endif
    }
}
