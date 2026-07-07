using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using SW.Attributes;

using SW.Util;

namespace SW.EditorTools.Attributes
{
    /// <summary>
    /// Boolean 필드 값에 따라 프로퍼티를 표시하거나 숨기고 비활성화하는 서랍입니다.
    /// </summary>
    /// <remarks>
    /// 중첩 개체와 배열의 직렬화 경로를 지원하며 계산된 조건 경로를 캐시합니다.
    /// </remarks>
    [CustomPropertyDrawer(typeof(SWConditionAttribute))]
    public class SWConditionAttributeDrawer : PropertyDrawer
    {
        #region 필드
        /// <summary>
        /// "propertyPath|조건필드명"을 키로, 조건 경로를 값으로 저장하는 캐시입니다.
        /// 매 프레임 문자열 연산을 피하기 위해 사용합니다.
        /// </summary>
        private static readonly Dictionary<string, string> cachedPaths = new();
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 어셈블리 리로드 시 경로 캐시를 비웁니다.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnReload()
        {
            cachedPaths.Clear();
        }
        #endregion // 초기화

        /// <summary>
        /// Inspector에서 프로퍼티를 그리는 메서드입니다.
        /// 조건에 따라 프로퍼티를 활성화/비활성화하거나 완전히 숨깁니다.
        /// </summary>
        /// <param name="position">프로퍼티가 그려질 사각형 영역</param>
        /// <param name="property">그려질 대상 SerializedProperty</param>
        /// <param name="label">프로퍼티 라벨 (필드 이름)</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SWConditionAttribute conditionAttribute = (SWConditionAttribute)attribute;

            bool enabled = GetConditionAttributeResult(conditionAttribute, property);
            bool previouslyEnabled = GUI.enabled;
            bool shouldDisplay = ShouldDisplay(conditionAttribute, enabled);
            if (shouldDisplay)
            {
                GUI.enabled = enabled;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = previouslyEnabled;
            }
        }

        /// <summary>
        /// 프로퍼티 높이를 반환합니다. Hidden 조건으로 숨겨진 필드는 높이를 차지하지 않습니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        /// <returns>Inspector 표시 높이입니다.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SWConditionAttribute conditionAttribute = (SWConditionAttribute)attribute;

            bool enabled = GetConditionAttributeResult(conditionAttribute, property);
            if (!ShouldDisplay(conditionAttribute, enabled))
                return -EditorGUIUtility.standardVerticalSpacing;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// 조건 boolean 필드의 값을 읽어와 최종 조건 결과를 반환합니다.
        /// Negative 옵션이 설정된 경우 결과를 반전시킵니다.
        /// </summary>
        /// <param name="conditionAttribute">조건 어트리뷰트 정보</param>
        /// <param name="property">현재 프로퍼티</param>
        /// <returns>조건 충족 여부 (true: 활성화, false: 비활성화)</returns>
        private bool GetConditionAttributeResult(SWConditionAttribute conditionAttribute, SerializedProperty property)
        {
            bool enabled = true;

            string conditionPath = GetConditionPath(property, conditionAttribute.ConditionBoolean);
            SerializedProperty propertyValue = property.serializedObject.FindProperty(conditionPath);

            // 중첩 경로에서 찾지 못하면 루트 레벨 필드로 한 번 더 시도합니다.
            propertyValue ??= property.serializedObject.FindProperty(conditionAttribute.ConditionBoolean);

            if (propertyValue != null)
            {
                enabled = propertyValue.boolValue;
            }
            else
            {
                SWLog.LogError("지정한 Boolean 필드명을 찾을 수 없습니다 - " + conditionAttribute.ConditionBoolean);
            }

            // Negative 옵션이 설정된 경우 결과를 반전
            // 예: 조건이 true일 때 숨기고 싶은 경우 사용
            if (conditionAttribute.Negative)
            {
                enabled = !enabled;
            }

            return enabled;
        }

        /// <summary>
        /// 같은 계층에 있는 조건 필드의 경로를 계산합니다.
        /// 경로의 마지막 세그먼트만 조건 필드명으로 교체하며, 결과를 캐시합니다.
        /// </summary>
        /// <param name="property">현재 프로퍼티</param>
        /// <param name="conditionFieldName">조건 boolean 필드명</param>
        /// <returns>조건 필드의 SerializedProperty 경로</returns>
        private static string GetConditionPath(SerializedProperty property, string conditionFieldName)
        {
            string propertyPath = property.propertyPath;
            string cacheKey = string.Concat(propertyPath, "|", conditionFieldName);

            if (cachedPaths.TryGetValue(cacheKey, out string conditionPath))
                return conditionPath;

            int lastDotIndex = propertyPath.LastIndexOf('.');
            conditionPath = lastDotIndex < 0
                ? conditionFieldName
                : string.Concat(propertyPath.Substring(0, lastDotIndex + 1), conditionFieldName);

            cachedPaths[cacheKey] = conditionPath;
            return conditionPath;
        }

        /// <summary>
        /// Hidden 옵션과 조건 결과를 종합하여 프로퍼티 표시 여부를 결정합니다.
        /// </summary>
        /// <param name="conditionAttribute">조건 어트리뷰트 정보</param>
        /// <param name="enabled">조건 충족 여부</param>
        /// <returns>프로퍼티를 그려야 하면 true</returns>
        private static bool ShouldDisplay(SWConditionAttribute conditionAttribute, bool enabled)
        {
            return !conditionAttribute.Hidden || enabled;
        }
    }
}
