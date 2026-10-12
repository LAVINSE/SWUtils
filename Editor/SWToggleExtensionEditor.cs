using UnityEditor;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.Util;

namespace SW.EditorTools.Util
{
    /// <summary>
    /// 기본 Toggle 인스펙터 아래에 선택 상태별 텍스트 색상 설정을 표시합니다.
    /// </summary>
    [CustomEditor(typeof(SWToggleExtension), true)]
    [CanEditMultipleObjects]
    public class SWToggleExtensionEditor : ToggleEditor
    {
        #region 필드
        private SerializedProperty useTextColor;
        private SerializedProperty labelText;
        private SerializedProperty selectedTextColor;
        private SerializedProperty unselectedTextColor;
        private readonly GUIContent useTextColorLabel = new("텍스트 색상 변경 사용");
        private readonly GUIContent textLabel = new("텍스트");
        private readonly GUIContent selectedLabel = new("선택 색상");
        private readonly GUIContent unselectedLabel = new("비선택 색상");
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 기본 토글과 추가 색상 설정의 직렬화 속성을 준비합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            useTextColor = serializedObject.FindProperty("useTextColor");
            labelText = serializedObject.FindProperty("labelText");
            selectedTextColor = serializedObject.FindProperty("selectedTextColor");
            unselectedTextColor = serializedObject.FindProperty("unselectedTextColor");
        }
        #endregion // 초기화

        #region 인스펙터
        /// <summary>
        /// 기본 Toggle과 같은 테마의 텍스트 색상 패널을 구성합니다.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = SWSelectableExtensionEditorUI.CreateRoot("기본 토글", DrawBaseInspector);
            VisualElement section = SWSelectableExtensionEditorUI.CreateSection(
                "선택 상태별 텍스트 색상", "Is On 상태에 따라 탭 텍스트의 색상이 바뀝니다.");
            section.Add(new PropertyField(useTextColor, useTextColorLabel.text));
            section.Add(new PropertyField(labelText, textLabel.text));
            section.Add(new PropertyField(selectedTextColor, selectedLabel.text));
            section.Add(new PropertyField(unselectedTextColor, unselectedLabel.text));

            HelpBox warning = new("색상을 변경할 TextMeshPro 텍스트를 연결하세요.", HelpBoxMessageType.Warning);
            section.Add(warning);
            RefreshReferenceWarning(warning);
            warning.TrackPropertyValue(labelText, property => RefreshReferenceWarning(warning));
            warning.TrackPropertyValue(useTextColor, property => RefreshReferenceWarning(warning));
            root.Add(section);
            return root;
        }

        /// <summary>
        /// Unity의 기본 그룹, 체크 그래픽과 선택 이벤트 설정을 표시합니다.
        /// </summary>
        private void DrawBaseInspector()
        {
            base.OnInspectorGUI();
        }

        /// <summary>
        /// 사용 여부나 참조가 바뀔 때만 누락 안내의 표시 여부를 갱신합니다.
        /// </summary>
        private void RefreshReferenceWarning(HelpBox warning)
        {
            warning.EnableInClassList(
                "sw-hidden", !useTextColor.boolValue || useTextColor.hasMultipleDifferentValues
                    || labelText.hasMultipleDifferentValues || labelText.objectReferenceValue != null);
        }

        /// <summary>
        /// 기본 설정과 텍스트 색상을 편집하고 필수 텍스트 참조 누락을 안내합니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(10f);
            SWEditorUtils.DrawHeader("선택 상태별 텍스트 색상");
            EditorGUILayout.PropertyField(useTextColor, useTextColorLabel);
            EditorGUILayout.PropertyField(labelText, textLabel);
            EditorGUILayout.PropertyField(selectedTextColor, selectedLabel);
            EditorGUILayout.PropertyField(unselectedTextColor, unselectedLabel);

            if (useTextColor.boolValue && !useTextColor.hasMultipleDifferentValues
                && !labelText.hasMultipleDifferentValues && labelText.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("색상을 변경할 TextMeshPro 텍스트를 연결하세요.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion // 인스펙터
    }
}
