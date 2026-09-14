using System;
using System.Linq;

namespace SW.EditorTools.Workspace
{
    /// <summary>
    /// 최초 분류와 이전 버전의 분류 이전 규칙을 정의합니다.
    /// 화면은 이 정의 대신 프로젝트 설정에 저장된 목록만 사용합니다.
    /// </summary>
    public static class SWEditorCategoryDefaults
    {
        /// <summary>공용 기능의 기본 분류 식별자입니다.</summary>
        public const string UtilityCategory = "swutils.utility";
        /// <summary>샘플 폴더의 기본 분류 식별자입니다.</summary>
        public const string SamplesCategory = "swutils.samples";

        /// <summary>
        /// 사용자 지정 분류를 보존하며 기본 분류를 설정에 한 번 추가합니다.
        /// </summary>
        public static void Initialize(SWEditorWorkspaceSettings settings)
        {
            SWEditorCategorySettings[] defaults =
            {
                Create(UtilityCategory, "SWUtility"),
                Create(SamplesCategory, "SWSamples"),
                Create("swutils.SW.SkillTree", "SWSkillTree"),
                Create("swutils.SW.Stat", "SWStat"),
                Create("swutils.SW.BehaviourTree", "SWBehaviour Tree"),
                Create("swutils.SW.StateMachine", "SWStateMachine"),
                Create("swutils.SW.Quest", "SWQuest"),
                Create(SWEditorWorkspaceSettings.OtherCategory, "Other")
            };
            string[] discovered = settings.Categories.Where(item => item.Identifier.StartsWith("discovered.", StringComparison.Ordinal)).Select(item => item.Identifier).ToArray();
            settings.Categories.RemoveAll(item => discovered.Contains(item.Identifier));
            foreach (SWEditorCategorySettings category in defaults.Reverse())
            {
                if (!settings.HasCategory(category.Identifier))
                {
                    settings.Categories.Insert(0, category);
                }
            }

            foreach (SWEditorTypeSettings type in settings.Types)
            {
                if (discovered.Contains(type.CategoryIdentifier))
                {
                    Type actualType = Type.GetType(type.TypeName);
                    string preferred = actualType == null ? null : SWEditorRegistry.GetTypePolicy(actualType)?.DefaultCategoryIdentifier;
                    type.CategoryIdentifier = settings.HasCategory(preferred) ? preferred : SWEditorWorkspaceSettings.OtherCategory;
                }
                else if (IsLegacyUtility(type.CategoryIdentifier))
                {
                    type.CategoryIdentifier = UtilityCategory;
                }
            }

            foreach (SWEditorAssetAssignment assignment in settings.Assignments)
            {
                if (IsLegacyUtility(assignment.CategoryIdentifier))
                {
                    assignment.CategoryIdentifier = UtilityCategory;
                }
                else if (discovered.Contains(assignment.CategoryIdentifier))
                {
                    assignment.CategoryIdentifier = SWEditorWorkspaceSettings.OtherCategory;
                }
            }

            if (IsLegacyUtility(settings.SelectedCategory))
            {
                settings.SelectedCategory = UtilityCategory;
            }
            else if (discovered.Contains(settings.SelectedCategory))
            {
                settings.SelectedCategory = SWEditorWorkspaceSettings.AllCategory;
            }
        }

        /// <summary>
        /// 프로젝트, 패키지 및 가져온 샘플 폴더의 경계를 검사합니다.
        /// </summary>
        public static bool IsSampleAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/');
            string[] segments = normalized.Split('/');
            for (int index = 0; index < segments.Length - 1; index++)
            {
                if (string.Equals(segments[index], "SWSample", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(segments[index], "SWSamples", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if ((segments[index].Equals("SWUtils", StringComparison.OrdinalIgnoreCase) ||
                    segments[index].Equals("com.swtools.swutils", StringComparison.OrdinalIgnoreCase)) &&
                    index + 1 < segments.Length - 1 &&
                    (segments[index + 1].Equals("Samples", StringComparison.OrdinalIgnoreCase) || segments[index + 1].Equals("Samples~", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (segments[index].Equals("Samples", StringComparison.OrdinalIgnoreCase) &&
                    index + 1 < segments.Length - 1 &&
                    segments[index + 1].Equals("SWUtils", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static SWEditorCategorySettings Create(string identifier, string displayName)
        {
            return new SWEditorCategorySettings { Identifier = identifier, DisplayName = displayName };
        }

        private static bool IsLegacyUtility(string identifier)
        {
            return identifier == "swutils.SW.Base" || identifier == "swutils.SW.Popup" || identifier == "swutils.SW.Util";
        }
    }
}
