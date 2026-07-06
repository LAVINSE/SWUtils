using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace SW.Editor.Base
{
    /// <summary>
    /// Unity 개체의 직렬화 대상 필드 정보를 수집하고 캐시합니다.
    /// </summary>
    /// <remarks>
    /// 상속 계층의 필드를 선언 순서대로 수집하고 타입별로 결과를 재사용합니다.
    /// </remarks>
    public static class SWMonoBehaviourFieldInfo
    {
        #region 필드
        /// <summary>
        /// Key - 대상 타입
        /// Value - 해당 타입의 모든 필드 정보 리스트 (부모 타입 필드가 앞에 오도록 정렬)
        /// </summary>
        private static readonly Dictionary<Type, List<FieldInfo>> fieldInfoDictionary = new();
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 어셈블리를 다시 불러올 때 캐시를 초기화합니다.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnReload()
        {
            fieldInfoDictionary.Clear();
        }
        #endregion // 초기화

        /// <summary>
        /// 오브젝트의 모든 필드 정보를 가져옵니다.
        /// </summary>
        /// <param name="target">필드를 수집할 대상 오브젝트입니다.</param>
        /// <param name="fieldInfoList">수집된 필드 정보 리스트입니다.</param>
        /// <returns>필드의 총 개수</returns>
        public static int GetFieldInfo(Object target, out List<FieldInfo> fieldInfoList)
        {
            Type targetType = target.GetType();

            if (!fieldInfoDictionary.TryGetValue(targetType, out fieldInfoList))
            {
                // 모든 필드 정보 수집
                // 상속 계층 순서대로 수집
                // 부모일수록 앞으로 정렬
                fieldInfoList = new List<FieldInfo>();
                Stack<Type> typeStack = new();
                Type currentType = targetType;

                Type unityBaseType = target is ScriptableObject
                    ? typeof(ScriptableObject)
                    : typeof(MonoBehaviour);

                while (currentType != null && currentType != unityBaseType)
                {
                    typeStack.Push(currentType);
                    currentType = currentType.BaseType;
                }

                while (typeStack.Count > 0)
                {
                    Type type = typeStack.Pop();
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly);

                    fieldInfoList.AddRange(fields);
                }

                fieldInfoDictionary[targetType] = fieldInfoList;
            }

            return fieldInfoList.Count;
        }
    }
}
