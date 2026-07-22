using System;
using System.Reflection;

namespace SW.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프에 직렬화된 타입 이름을 현재 실행 환경의 타입으로 해석합니다.
    /// </summary>
    public static class SWStateMachineGraphTypeResolver
    {
        /// <summary>
        /// 조립체 한정 이름을 먼저 사용하고, 조립체가 이동한 경우 타입 전체 이름으로 다시 검색합니다.
        /// </summary>
        /// <param name="serializedTypeName">그래프에 저장된 타입 이름입니다.</param>
        /// <returns>찾은 타입이며, 찾지 못하면 <see langword="null"/>입니다.</returns>
        public static Type Resolve(string serializedTypeName)
        {
            if (string.IsNullOrWhiteSpace(serializedTypeName))
                return null;

            Type resolvedType = Type.GetType(serializedTypeName, false);
            if (resolvedType != null)
                return resolvedType;

            string fullTypeName = GetFullTypeName(serializedTypeName);
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < loadedAssemblies.Length; index++)
            {
                resolvedType = loadedAssemblies[index].GetType(fullTypeName, false);
                if (resolvedType != null)
                    return resolvedType;
            }

            return null;
        }

        /// <summary>조립체 한정 이름에서 타입 전체 이름만 분리합니다.</summary>
        private static string GetFullTypeName(string serializedTypeName)
        {
            int separatorIndex = serializedTypeName.IndexOf(',');
            return separatorIndex < 0
                ? serializedTypeName.Trim()
                : serializedTypeName.Substring(0, separatorIndex).Trim();
        }
    }
}
