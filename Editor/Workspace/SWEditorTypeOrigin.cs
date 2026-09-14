using System;

namespace SW.EditorTools.Workspace
{
    /// <summary>네임스페이스가 없는 예제까지 고려하여 유형의 SWUtils 소속을 판별합니다.</summary>
    public static class SWEditorTypeOrigin
    {
        /// <summary>어셈블리, 네임스페이스와 생성 메뉴의 경계를 확인합니다.</summary>
        public static bool IsSWUtils(Type type, string creationMenu)
        {
            if (type == null)
                return false;
            string assemblyName = type.Assembly.GetName().Name;
            return HasPrefix(assemblyName, "SWUtils", '.') || HasPrefix(type.Namespace, "SW", '.') || HasPrefix(creationMenu, "Assets/Create/SWUtils", '/') || HasPrefix(creationMenu, "SWUtils", '/');
        }

        private static bool HasPrefix(string value, string prefix, char separator)
        {
            return value == prefix || (value?.StartsWith(prefix + separator, StringComparison.Ordinal) ?? false);
        }
    }
}
