using System;
using UnityEditor;
using UnityEngine;

namespace SW.EditorTools
{
    /// <summary>기존 도구에 왼쪽 탐색과 상단 도구 모음, 독립된 작업 영역을 제공합니다.</summary>
    public sealed class SWEditorWindowLayoutScope : IDisposable
    {
        private static SWEditorWindowLayoutScope current;
        private static GUIStyle navigationStyle;
        private static GUIStyle brandStyle;
        private static GUIStyle headingStyle;
        private readonly SWEditorWindowLayoutScope previous;
        private readonly string[] navigation;
        private readonly float sidebarWidth;
        private readonly float toolbarHeight;
        private readonly EditorWindow window;
        private int selectedIndex;
        private bool disposed;
        /// <summary>기존 도구의 기능을 유지하면서 공통 작업 영역을 시작합니다.</summary>
        public SWEditorWindowLayoutScope(EditorWindow window, string[] navigation = null, int selectedIndex = 0)
        {
            previous = current;
            current = this;
            this.window = window;
            this.navigation = navigation;
            this.selectedIndex = selectedIndex;
            bool compact = window.position.width < 620;
            sidebarWidth = compact ? 0 : window.position.width >= 1000 ? 246 : 200;
            toolbarHeight = 55;
            EnsureStyles();
            float width = window.position.width, height = window.position.height;
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(0, 0, sidebarWidth, height), SWEditorTheme.Sidebar);
                EditorGUI.DrawRect(new Rect(sidebarWidth, 0, width - sidebarWidth, toolbarHeight), SWEditorTheme.Toolbar);
                EditorGUI.DrawRect(new Rect(sidebarWidth, toolbarHeight - 1, width - sidebarWidth, 1), SWEditorTheme.Border);
            }

            if (!compact)
            {
                GUI.Label(new Rect(10, 0, sidebarWidth - 20, 55), Window.SWUtilsEditor.DisplayName, brandStyle);
                float rowPosition = 60;
                if (navigation == null || navigation.Length == 0)
                    GUI.Toggle(new Rect(10, rowPosition, sidebarWidth - 20, 55), true, window.titleContent.text, navigationStyle);
                else
                    for (int index = 0; index < navigation.Length; index++)
                    {
                        float rowHeight = Mathf.Min(55, Mathf.Max(29, (height - 150) / navigation.Length));
                        if (GUI.Toggle(new Rect(10, rowPosition, sidebarWidth - 20, rowHeight), selectedIndex == index, navigation[index], navigationStyle))
                            this.selectedIndex = index;
                        rowPosition += rowHeight + 5;
                    }

                if (GUI.Button(new Rect(10, height - 45, sidebarWidth - 20, 33), "에셋 작업 공간 열기"))
                    Window.SWUtilsEditor.OpenWindow();
            }

            string title = navigation != null && navigation.Length > 0 ? navigation[Mathf.Clamp(this.selectedIndex, 0, navigation.Length - 1)] : window.titleContent.text;
            float editorButtonWidth = 170;
            GUI.Label(new Rect(sidebarWidth + 15, 0, Mathf.Max(80, width - sidebarWidth - editorButtonWidth - 40), toolbarHeight), title, headingStyle);
            if (GUI.Button(new Rect(width - editorButtonWidth - 12, 11, editorButtonWidth, 33), Window.SWUtilsEditor.DisplayName))
                Window.SWUtilsEditor.OpenWindow();
            GUILayout.BeginArea(new Rect(sidebarWidth + 10, toolbarHeight + 8, Mathf.Max(1, width - sidebarWidth - 20), Mathf.Max(1, height - toolbarHeight - 16)));
        }

        /// <summary>바깥쪽 작업 영역과 전역 탐색 상태를 복원합니다.</summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            GUILayout.EndArea();
            current = previous;
        }

        /// <summary>상위 탐색으로 옮긴 탭 선택을 기존 도구에 전달합니다.</summary>
        public static bool TryGetNavigation(string[] names, out int selectedIndex)
        {
            selectedIndex = 0;
            if (current == null || current.sidebarWidth == 0 || !ReferenceEquals(current.navigation, names))
                return false;
            selectedIndex = current.selectedIndex;
            return true;
        }

        /// <summary>작업 영역 안에서 사용하는 너비를 계산합니다.</summary>
        public static float GetContentWidth(float windowWidth)
        {
            return current == null ? windowWidth : Mathf.Max(1, windowWidth - current.sidebarWidth - 20);
        }

        /// <summary>작업 영역 안에서 사용하는 높이를 계산합니다.</summary>
        public static float GetContentHeight(float windowHeight)
        {
            return current == null ? windowHeight : Mathf.Max(1, windowHeight - current.toolbarHeight - 16);
        }

        private static void EnsureStyles()
        {
            if (navigationStyle != null)
                return;
            navigationStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 13,
                padding = new RectOffset(10, 10, 4, 4)
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
                alignment = TextAnchor.MiddleLeft
            };
        }
    }
}
