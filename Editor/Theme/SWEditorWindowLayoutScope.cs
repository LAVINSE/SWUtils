using System;
using UnityEditor;
using UnityEngine;

namespace SW.EditorTools
{
    /// <summary>기존 도구에 왼쪽 탐색과 상단 도구 모음, 독립된 작업 영역을 제공합니다.</summary>
    public sealed class SWEditorWindowLayoutScope : IDisposable
    {
        #region 필드
        private static SWEditorWindowLayoutScope current;
        private static GUIStyle navigationStyle;
        private static GUIStyle brandStyle;
        private static GUIStyle headingStyle;
        private readonly SWEditorWindowLayoutScope previous;
        private readonly string[] navigation;
        private readonly float sidebarWidth;
        private readonly float toolbarHeight = 55f;
        private readonly int originalSelectedIndex;
        private int selectedIndex;
        private bool disposed;
        #endregion // 필드

        #region 초기화와 정리
        /// <summary>현재 편집기 이름과 탐색 항목으로 공통 작업 영역을 시작합니다.</summary>
        public SWEditorWindowLayoutScope(EditorWindow window, string[] navigation = null, int selectedIndex = 0,
            float minimumContentWidth = 0f)
        {
            Util.SWEditorUtils.RestoreWindowTitle(window);
            previous = current;
            current = this;
            this.navigation = navigation;
            this.selectedIndex = selectedIndex;
            originalSelectedIndex = selectedIndex;

            float candidateSidebarWidth = window.position.width >= 1000f ? 246f : 200f;
            float requiredContentWidth = minimumContentWidth > 0f ? minimumContentWidth : Mathf.Max(360f, window.minSize.x - 20f);
            bool hasSidebar = window.position.width >= 620f && navigation != null && navigation.Length > 1
                && window.position.width - candidateSidebarWidth - 20f >= requiredContentWidth;
            sidebarWidth = hasSidebar ? candidateSidebarWidth : 0f;
            EnsureStyles();

            float width = window.position.width;
            float height = window.position.height;
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(0f, 0f, sidebarWidth, height), SWEditorTheme.Sidebar);
                EditorGUI.DrawRect(new Rect(sidebarWidth, 0f, width - sidebarWidth, toolbarHeight), SWEditorTheme.Toolbar);
                EditorGUI.DrawRect(new Rect(sidebarWidth, toolbarHeight - 1f, width - sidebarWidth, 1f), SWEditorTheme.Border);
            }

            if (hasSidebar)
            {
                DrawSidebar(window, height);
            }

            DrawToolbar(window, width);
            GUILayout.BeginArea(new Rect(
                sidebarWidth + 10f,
                toolbarHeight + 8f,
                GetContentWidth(width),
                GetContentHeight(height)));
        }

        /// <summary>레이아웃 종료 중 오류가 발생해도 바깥쪽 작업 영역 상태를 복원합니다.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            try
            {
                GUILayout.EndArea();
            }
            finally
            {
                current = previous;
            }
        }
        #endregion // 초기화와 정리

        #region 탐색과 작업 영역
        /// <summary>상위 탐색으로 옮긴 탭 선택을 전달합니다. 해당 탐색이 아니면 false를 반환합니다.</summary>
        public static bool TryGetNavigation(string[] names, out int selectedIndex)
        {
            selectedIndex = 0;
            if (current == null || current.sidebarWidth == 0f || !ReferenceEquals(current.navigation, names))
            {
                return false;
            }

            selectedIndex = current.selectedIndex;
            if (selectedIndex != current.originalSelectedIndex)
            {
                GUI.changed = true;
            }
            return true;
        }

        /// <summary>작업 영역 안에서 사용하는 너비를 계산합니다.</summary>
        public static float GetContentWidth(float windowWidth)
        {
            return current == null ? windowWidth : Mathf.Max(1f, windowWidth - current.sidebarWidth - 20f);
        }

        /// <summary>작업 영역 안에서 사용하는 높이를 계산합니다.</summary>
        public static float GetContentHeight(float windowHeight)
        {
            return current == null ? windowHeight : Mathf.Max(1f, windowHeight - current.toolbarHeight - 16f);
        }
        #endregion // 탐색과 작업 영역

        #region 화면
        /// <summary>새로 선택한 항목만 반영하여 기존 선택 항목이 클릭 결과를 덮어쓰지 않게 합니다.</summary>
        private void DrawSidebar(EditorWindow window, float height)
        {
            GUI.Label(new Rect(10f, 0f, sidebarWidth - 20f, toolbarHeight), window.titleContent.text, brandStyle);
            float rowPosition = 60f;
            float rowHeight = Mathf.Min(55f, Mathf.Max(29f, (height - 80f) / navigation.Length - 5f));
            for (int index = 0; index < navigation.Length; index++)
            {
                bool wasSelected = originalSelectedIndex == index;
                Rect rectangle = new(10f, rowPosition, sidebarWidth - 20f, rowHeight);
                bool isSelected = GUI.Toggle(rectangle, wasSelected, navigation[index], navigationStyle);
                if (isSelected && !wasSelected)
                {
                    selectedIndex = index;
                    window.Repaint();
                }

                if (wasSelected && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(new Rect(rectangle.x, rectangle.y, 3f, rectangle.height), SWEditorTheme.Accent);
                }
                rowPosition += rowHeight + 5f;
            }
        }

        /// <summary>현재 화면 제목과 데이터 편집기 열기 버튼을 한 번만 표시합니다.</summary>
        private void DrawToolbar(EditorWindow window, float width)
        {
            string title = sidebarWidth > 0f
                ? navigation[Mathf.Clamp(selectedIndex, 0, navigation.Length - 1)]
                : window.titleContent.text;
            const float editorButtonWidth = 142f;
            Rect titleRectangle = new(sidebarWidth + 15f, 0f, Mathf.Max(1f, width - sidebarWidth - editorButtonWidth - 42f), toolbarHeight);
            GUI.Label(titleRectangle, new GUIContent(title, title), headingStyle);
            if (GUI.Button(new Rect(width - editorButtonWidth - 12f, 11f, editorButtonWidth, 33f), "데이터 편집기 열기"))
            {
                Window.SWUtilsEditor.OpenWindow();
            }
        }

        /// <summary>공통 테마가 적용된 스타일을 준비합니다.</summary>
        private static void EnsureStyles()
        {
            if (navigationStyle != null)
            {
                return;
            }

            navigationStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                fontSize = SWEditorTheme.BodyFontSize,
                padding = new RectOffset(16, 10, 4, 4)
            };
            brandStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            headingStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
        }
        #endregion // 화면
    }
}
