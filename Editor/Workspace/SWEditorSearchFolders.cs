using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace SW.EditorTools.Workspace
{
    /// <summary>탐색 폴더의 기본값과 경계를 검증하며 프로젝트 전체 검색으로 범위를 확대하지 않습니다.</summary>
    internal static class SWEditorSearchFolders
    {
        #region 기본값
        /// <summary>SWUtils의 ScriptableObject 예제와 하위 폴더를 직접 식별합니다. 찾지 못하면 빈 목록을 반환합니다.</summary>
        public static string[] GetDefaults()
        {
            string folder = AssetDatabase.GUIDToAssetPath("ce2025e71e8eeeb44b882f88cf9e37d7");
            return AssetDatabase.IsValidFolder(folder) ? new[] { folder } : Array.Empty<string>();
        }
        #endregion // 기본값

        #region 경로 검사
        /// <summary>프로젝트 상대 경로를 정규화합니다. 외부 경로 또는 상위 경로 이동이 있으면 빈 문자열을 반환합니다.</summary>
        public static string Normalize(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return string.Empty;
            }

            string normalized = folder.Trim().Replace('\\', '/').TrimEnd('/');
            string[] parts = normalized.Split('/');
            bool projectFolder = parts[0] == "Assets" || (parts[0] == "Packages" && parts.Length > 1);
            if (!projectFolder || parts.Any(part => string.IsNullOrEmpty(part) || part == "." || part == ".."))
            {
                return string.Empty;
            }

            return normalized;
        }

        /// <summary>폴더 경계를 포함해 하위 경로인지 검사합니다. 빈 폴더는 어떤 경로도 포함하지 않습니다.</summary>
        public static bool Contains(string folder, string path)
        {
            folder = Normalize(folder);
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(path))
            {
                return false;
            }

            path = path.Replace('\\', '/');
            return string.Equals(path, folder, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>존재하는 폴더만 반환하고 중복 및 상위 폴더에 포함된 경로를 제거합니다. 빈 결과는 검색 중단을 의미합니다.</summary>
        public static string[] GetValidFolders(IEnumerable<string> folders)
        {
            string[] normalized = (folders ?? Array.Empty<string>()).Select(Normalize)
                .Where(folder => !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return normalized.Where(folder => !normalized.Any(parent =>
                !string.Equals(parent, folder, StringComparison.OrdinalIgnoreCase) && Contains(parent, folder))).ToArray();
        }
        #endregion // 경로 검사
    }
}
