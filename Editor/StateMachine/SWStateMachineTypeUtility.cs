using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 그래프에서 선택할 수 있는 상태 구현 타입을 검색합니다.
    /// </summary>
    internal static class SWStateMachineTypeUtility
    {
        #region 함수
        /// <summary>
        /// 그래프 종류에 맞는 구체 상태 타입을 반환합니다.
        /// </summary>
        /// <param name="graphType">검색할 상태 머신 그래프 종류입니다.</param>
        /// <returns>이름순으로 정렬된 상태 구현 타입 목록입니다.</returns>
        public static IReadOnlyList<Type> GetStateTypes(SWStateMachineGraphType graphType)
        {
            Type genericBaseType = graphType == SWStateMachineGraphType.Layered
                ? typeof(SWState<>)
                : typeof(SWStackState<>);
            List<Type> stateTypes = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type == null || type.IsAbstract || type.IsGenericTypeDefinition)
                        continue;

                    if (InheritsOpenGenericType(type, genericBaseType))
                        stateTypes.Add(type);
                }
            }

            stateTypes = stateTypes
                .GroupBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            stateTypes.Sort((left, right) =>
                string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
            return stateTypes;
        }

        /// <summary>
        /// 그래프 연결에서 선택할 수 있는 구체 조건 타입을 반환합니다.
        /// </summary>
        /// <returns>이름순으로 정렬된 조건 구현 타입 목록입니다.</returns>
        public static IReadOnlyList<Type> GetConditionTypes()
        {
            List<Type> conditionTypes = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type == null || type.IsAbstract || type.IsGenericTypeDefinition)
                        continue;

                    if (InheritsOpenGenericType(type, typeof(SWStateMachineGraphCondition<>)))
                        conditionTypes.Add(type);
                }
            }

            conditionTypes = conditionTypes
                .GroupBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            conditionTypes.Sort((left, right) =>
                string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
            return conditionTypes;
        }

        /// <summary>중첩 타입과 선언 타입을 구분할 수 있는 검색 표시 이름을 반환합니다.</summary>
        public static string GetDisplayName(Type type)
        {
            if (type == null)
                return "Missing Type";

            string ownerName = type.DeclaringType == null
                ? type.Namespace
                : type.DeclaringType.Name;
            return string.IsNullOrWhiteSpace(ownerName)
                ? type.Name
                : $"{type.Name}  —  {ownerName}";
        }

        /// <summary>타입에 지정된 그래프 카테고리 경로 또는 기본 경로를 반환합니다.</summary>
        public static string GetCategoryPath(Type type, string defaultCategory)
        {
            SWStateMachineNodeCategoryAttribute categoryAttribute =
                type?.GetCustomAttribute<SWStateMachineNodeCategoryAttribute>();
            return string.IsNullOrWhiteSpace(categoryAttribute?.CategoryPath)
                ? defaultCategory
                : categoryAttribute.CategoryPath;
        }

        /// <summary>
        /// 조립체에서 정상적으로 불러올 수 있는 타입만 반환합니다.
        /// </summary>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types;
            }
        }

        /// <summary>
        /// 타입이 지정한 열린 제네릭 타입을 상속하는지 확인합니다.
        /// </summary>
        private static bool InheritsOpenGenericType(Type type, Type genericBaseType)
        {
            Type currentType = type;

            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType &&
                    currentType.GetGenericTypeDefinition() == genericBaseType)
                    return true;

                currentType = currentType.BaseType;
            }

            return false;
        }
        #endregion // 함수
    }
}
