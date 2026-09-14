using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

using SW.Attributes;

using SW.Util;

namespace SW.EditorTools.Util
{
    /// <summary>
    /// SWButtonExtension의 커스텀 인스펙터입니다.
    /// </summary>
    /// <remarks>
    /// Button을 상속한 클래스에는 Unity의 ButtonEditor가 적용되어
    /// 파생 클래스에서 추가한 직렬화 필드가 인스펙터에 표시되지 않습니다.
    /// 이 에디터는 기본 Button 인스펙터(Interactable, Transition, Navigation, OnClick)를
    /// 먼저 그린 뒤, 확장 필드를 SWEditorUtils.DrawHeader 기반의 섹션으로 그립니다.
    /// SWCondition 어트리뷰트는 PropertyDrawer로 동작하므로 조건부 표시가 그대로 유지됩니다.
    /// </remarks>
    [CustomEditor(typeof(SWButtonExtension), true)]
    [CanEditMultipleObjects]
    public class SWButtonExtensionEditor : ButtonEditor
    {
        #region 데이터
        /// <summary>인스펙터 섹션 정의입니다.</summary>
        private readonly struct Section
        {
            /// <summary>섹션 제목입니다.</summary>
            public readonly string title;
            /// <summary>섹션에 포함될 직렬화 필드 이름 목록입니다.</summary>
            public readonly string[] propertyNames;

            /// <summary>
            /// 제목과 직렬화 필드 이름으로 인스펙터 섹션을 생성합니다.
            /// </summary>
            /// <param name="title">섹션 제목입니다.</param>
            /// <param name="propertyNames">섹션에 포함할 직렬화 필드 이름입니다.</param>
            public Section(string title, params string[] propertyNames)
            {
                this.title = title;
                this.propertyNames = propertyNames;
            }
        }
        #endregion // 데이터

        #region 필드
        /// <summary>인스펙터에 그릴 섹션 목록입니다. 선언 순서대로 표시됩니다.</summary>
        private static readonly Section[] Sections =
        {
            new("연타 방지", "useCooldown", "cooldownSeconds", "disableButtonDuringCooldown"),
            new("롱프레스", "useLongPress", "longPressSeconds", "suppressClickAfterLongPress", "onLongPress"),
            new("홀드 반복", "useRepeat", "repeatStartDelay", "repeatInterval", "repeatMinInterval", "repeatAccelerateDuration", "onRepeat"),
            new("사운드", "useClickSfx", "clickSfxKey"),
            new("설정", "useUnscaledTime"),
        };
        #endregion // 필드

        /// <summary>
        /// 기본 Button 인스펙터를 그린 뒤 확장 필드를 섹션별로 그립니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10f);

            serializedObject.Update();

            for (int sectionIndex = 0; sectionIndex < Sections.Length; sectionIndex++)
            {
                Section section = Sections[sectionIndex];

                SWEditorUtils.DrawHeader(section.title);

                for (int propertyIndex = 0; propertyIndex < section.propertyNames.Length; propertyIndex++)
                {
                    SerializedProperty property = serializedObject.FindProperty(section.propertyNames[propertyIndex]);
                    if (property != null)
                        EditorGUILayout.PropertyField(property, true);
                }

                EditorGUILayout.Space(6f);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
