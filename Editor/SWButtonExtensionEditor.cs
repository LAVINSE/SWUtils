using UnityEditor;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

using SW.Util;

namespace SW.EditorTools.Util
{
    /// <summary>
    /// 기본 Button 편집과 Iris Line 테마의 확장 설정을 제공하는 인스펙터입니다.
    /// </summary>
    [CustomEditor(typeof(SWButtonExtension), true)]
    [CanEditMultipleObjects]
    public class SWButtonExtensionEditor : ButtonEditor
    {
        #region 데이터
        /// <summary>
        /// 인스펙터를 열 때 한 번 조회한 섹션의 제목, 설명과 직렬화 속성입니다.
        /// </summary>
        private sealed class Section
        {
            public readonly string title;
            public readonly string description;
            public readonly SerializedProperty[] properties;

            /// <summary>
            /// 섹션에 필요한 속성을 캐시하여 다시 그릴 때 조회하지 않습니다.
            /// </summary>
            public Section(
                SerializedObject serializedObject, string title, string description, params string[] propertyNames)
            {
                this.title = title;
                this.description = description;
                properties = new SerializedProperty[propertyNames.Length];
                for (int index = 0; index < propertyNames.Length; index++)
                {
                    properties[index] = serializedObject.FindProperty(propertyNames[index]);
                }
            }
        }
        #endregion // 데이터

        #region 필드
        private Section[] sections;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 기본 Button 속성과 확장 설정을 인스펙터 생명주기당 한 번 준비합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            sections = new[]
            {
                new Section(serializedObject, "연타 방지", "클릭 후 지정한 시간 동안 추가 입력을 막습니다.",
                    "useCooldown", "cooldownSeconds", "disableButtonDuringCooldown"),
                new Section(serializedObject, "롱프레스", "길게 누르면 한 번 실행합니다. 홀드 반복과 함께 켜면 반복이 우선됩니다.",
                    "useLongPress", "longPressSeconds", "suppressClickAfterLongPress", "onLongPress"),
                new Section(serializedObject, "홀드 반복", "누르는 동안 반복 실행하며 최소 간격까지 점차 빨라집니다.",
                    "useRepeat", "repeatStartDelay", "repeatInterval", "repeatMinInterval", "repeatAccelerateDuration", "onRepeat"),
                new Section(serializedObject, "사운드", "클릭 효과음을 SWAudioManager로 재생합니다.",
                    "useClickSfx", "clickSfxKey"),
                new Section(serializedObject, "시간", "일시정지 중에도 동작해야 하는 UI는 배율 무시를 켜세요.",
                    "useUnscaledTime"),
            };
        }
        #endregion // 초기화

        #region 인스펙터
        /// <summary>
        /// UI Toolkit 설정 패널을 한 번 만들고 속성 바인딩으로 변경을 반영합니다.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = SWSelectableExtensionEditorUI.CreateRoot("기본 버튼", DrawBaseInspector);
            foreach (Section section in sections)
            {
                VisualElement panel = SWSelectableExtensionEditorUI.CreateSection(section.title, section.description);
                foreach (SerializedProperty property in section.properties)
                {
                    panel.Add(new PropertyField(property));
                }
                root.Add(panel);
            }

            return root;
        }

        /// <summary>
        /// Unity의 기본 클릭 이벤트와 전환 설정을 유지합니다.
        /// </summary>
        private void DrawBaseInspector()
        {
            base.OnInspectorGUI();
        }

        /// <summary>
        /// IMGUI 기반 작업 공간에서는 캐시한 속성으로 확장 설정을 표시합니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            foreach (Section section in sections)
            {
                EditorGUILayout.Space(8f);
                SWEditorUtils.DrawHeader(section.title, SWEditorTheme.Accent);
                foreach (SerializedProperty property in section.properties)
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
        #endregion // 인스펙터
    }
}
