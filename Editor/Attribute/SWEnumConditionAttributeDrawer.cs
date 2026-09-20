using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.Attributes;

using SW.Util;

namespace SW.EditorTools.Attributes
{
    /// <summary>
    /// Inspector에서 렌더링하는 커스텀 PropertyDrawer입니다.
    /// 조건 열거형 값에 따라 필드를 표시하거나 숨기고, 비활성화 상태를 제어합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWEnumConditionAttribute))]
    public class SWEnumConditionAttributeDrawer : PropertyDrawer
    {
        #region 필드
        /// <summary>
        /// 대상 필드 경로와 조건 필드 이름을 묶어 조건 열거형의 경로를 저장합니다.
        /// </summary>
        private static Dictionary<string, string> cachedPaths = new();
        #endregion // 필드


        [InitializeOnLoadMethod]
        private static void ClearCacheOnReload()
        {
            cachedPaths.Clear();
        }

        #region 조건부 필드 표시
        /// <summary>
        /// 열거형 조건을 추적하는 필드를 생성합니다.
        /// 조건 필드가 없으면 기존 검증 규칙에 따라 원인을 기록합니다.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SWEnumConditionAttribute conditionAttribute = (SWEnumConditionAttribute)attribute;
            PropertyField field = new PropertyField(property);
            RefreshCondition();
            int separatorIndex = property.propertyPath.LastIndexOf('.');
            string conditionPath = property.propertyPath.Substring(0, separatorIndex + 1)
                + conditionAttribute.ConditionEnum;
            SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);
            if (conditionProperty != null)
            {
                field.TrackPropertyValue(conditionProperty, changedProperty => RefreshCondition());
            }
            return field;

            /// <summary>
            /// 현재 열거형 조건에 맞춰 필드의 표시와 활성 상태를 갱신합니다.
            /// </summary>
            void RefreshCondition()
            {
                bool enabled = GetConditionAttributeResult(conditionAttribute, property);
                field.style.display = !conditionAttribute.Hidden || enabled
                    ? DisplayStyle.Flex : DisplayStyle.None;
                field.SetEnabled(enabled);
            }
        }
        #endregion // 조건부 필드 표시

        /// <summary>
        /// 조건 열거형 값에 따라 필드를 표시하거나 비활성화 상태로 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SWEnumConditionAttribute enumConditionAttribute = (SWEnumConditionAttribute)attribute;
            bool enabled = GetConditionAttributeResult(enumConditionAttribute, property);
            bool previouslyEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (!enumConditionAttribute.Hidden || enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            GUI.enabled = previouslyEnabled;
        }

        /// <summary>
        /// 조건 열거형 값을 확인하여 필드 표시 여부를 결정합니다.
        /// </summary>
        /// <param name="enumConditionAttribute">검사할 어트리뷰트</param>
        /// <param name="property">대상 프로퍼티</param>
        /// <returns>필드를 표시하면 true, 아니면 false</returns>
        private bool GetConditionAttributeResult(SWEnumConditionAttribute enumConditionAttribute, SerializedProperty property)
        {
            bool enabled = true;

            SerializedProperty enumProp;
            string enumPropPath = string.Empty;
            string propertyPath = property.propertyPath;

            string cacheKey = propertyPath + "\n" + enumConditionAttribute.ConditionEnum;
            if (!cachedPaths.TryGetValue(cacheKey, out enumPropPath))
            {
                int separatorIndex = propertyPath.LastIndexOf('.');
                enumPropPath = propertyPath.Substring(0, separatorIndex + 1) + enumConditionAttribute.ConditionEnum;
                cachedPaths.Add(cacheKey, enumPropPath);
            }

            enumProp = property.serializedObject.FindProperty(enumPropPath);

            if (enumProp != null)
            {
                int currentEnum = enumProp.enumValueIndex;
                enabled = enumConditionAttribute.ContainsBitFlag(currentEnum);
            }
            else
            {
                SWLog.LogError($"[SWEnumCondition] 조건 enum을 찾을 수 없습니다: '{enumConditionAttribute.ConditionEnum}'");
            }

            return enabled;
        }

        /// <summary>
        /// 프로퍼티의 높이를 반환합니다. 숨김 상태면 0을 반환하여 공간을 차지하지 않습니다.
        /// </summary>
        /// <param name="property">대상 프로퍼티</param>
        /// <param name="label">표시할 라벨</param>
        /// <returns>프로퍼티 높이 (픽셀)</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SWEnumConditionAttribute enumConditionAttribute = (SWEnumConditionAttribute)attribute;
            bool enabled = GetConditionAttributeResult(enumConditionAttribute, property);

            if (!enumConditionAttribute.Hidden || enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }
    }
}
