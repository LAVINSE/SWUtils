using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SW.EditorTools.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// 플레이 중에 EventSystem 선택 오브젝트, 마우스/터치, UI Raycast 결과,
    /// 입력 상태와 UI Raycast 결과를 실시간으로 보여주는 디버거 창입니다.
    /// </summary>
    public class SWInputDebuggerWindow : EditorWindow
    {
        #region 필드
        private Vector2 scrollPosition;

        // 탭바
        private int selectedTab = 0;
        private static readonly string[] tabNames = { "EventSystem", "Pointer", "Raycast", "Input" };

        // Raycast 결과 버퍼 (매 프레임 재사용)
        private readonly List<RaycastResult> raycastResults = new();
        private PointerEventData cachedPointerData;

        // 마지막 클릭 정보
        private Vector2 lastClickPosition;
        private string lastClickedObjectName = "(없음)";
        private double lastClickTime;

        // 리페인트 주기 제어
        private double lastRepaintTime;
        private const double REPAINT_INTERVAL = 0.05; // 20fps 정도로 갱신
        #endregion // 필드

        /// <summary>
        /// Input Debugger 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/Input/Input Debugger Window")]
        public static void ShowWindow()
        {
            SWInputDebuggerWindow window = GetWindow<SWInputDebuggerWindow>();
            SWEditorUtils.SetupWindow(window, "SW Input Debugger", "d_EventSystem Icon", 320, 400);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;

            if (SWEditorUtils.ShouldRepaint(ref lastRepaintTime, REPAINT_INTERVAL))
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            // SWEditorUtils 탭바 사용
            selectedTab = SWEditorUtils.DrawTabBar(selectedTab, tabNames);

            if (SWEditorUtils.DrawPlayModeOnlyNotice()) { /* 안내만 표시 */ }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawEventSystemTab(); break;
                case 1: DrawPointerTab(); break;
                case 2: DrawRaycastTab(); break;
                case 3: DrawInputTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region EventSystem 탭
        private void DrawEventSystemTab()
        {
            SWEditorUtils.DrawHeader("EventSystem");

            EventSystem es = EventSystem.current;
            if (es == null)
            {
                SWEditorUtils.DrawEmptyNotice("현재 활성 EventSystem이 없습니다.", MessageType.Warning);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("EventSystem", es, typeof(EventSystem), true);
                EditorGUILayout.ObjectField("Selected", es.currentSelectedGameObject, typeof(GameObject), true);

                GameObject firstSelected = es.firstSelectedGameObject;
                EditorGUILayout.ObjectField("First Selected", firstSelected, typeof(GameObject), true);

                EditorGUILayout.Toggle("Send Navigation Events", es.sendNavigationEvents);
                EditorGUILayout.IntField("Pixel Drag Threshold", es.pixelDragThreshold);
            }

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Last Click");
            EditorGUILayout.LabelField("Last Clicked", lastClickedObjectName);
            EditorGUILayout.LabelField("Last Click Pos", lastClickPosition.ToString("F0"));
            double ago = EditorApplication.timeSinceStartup - lastClickTime;
            EditorGUILayout.LabelField("Click Age (s)",
                lastClickTime > 0 ? $"{ago:F2}" : "(없음)");
        }
        #endregion

        #region Pointer 탭
        private void DrawPointerTab()
        {
            SWEditorUtils.DrawHeader("Mouse");

            Vector2 mousePos = Input.mousePosition;
            EditorGUILayout.LabelField("Mouse Position", mousePos.ToString("F0"));
            EditorGUILayout.LabelField("Mouse Normalized",
                new Vector2(mousePos.x / Mathf.Max(1, Screen.width),
                           mousePos.y / Mathf.Max(1, Screen.height)).ToString("F2"));

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Touch");

            int touchCount = Input.touchCount;
            EditorGUILayout.LabelField("Touch Count", touchCount.ToString());
            for (int i = 0; i < touchCount && i < 5; i++)
            {
                Touch t = Input.GetTouch(i);
                EditorGUILayout.LabelField($"  Touch[{i}]", $"{t.phase} @ {t.position:F0} (fid={t.fingerId})");
            }

            // 클릭 감지
            if (Application.isPlaying && Input.GetMouseButtonDown(0))
            {
                lastClickPosition = mousePos;
                lastClickTime = EditorApplication.timeSinceStartup;

                EventSystem es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject != null)
                {
                    lastClickedObjectName = es.currentSelectedGameObject.name;
                }
            }
        }
        #endregion

        #region Raycast 탭
        private void DrawRaycastTab()
        {
            SWEditorUtils.DrawHeader("UI Raycast");

            EventSystem es = EventSystem.current;
            if (es == null)
            {
                SWEditorUtils.DrawEmptyNotice("EventSystem이 없습니다.");
                return;
            }

            if (!Application.isPlaying)
            {
                SWEditorUtils.DrawEmptyNotice("플레이 중에만 Raycast가 수행됩니다.");
                return;
            }

            if (cachedPointerData == null)
            {
                cachedPointerData = new PointerEventData(es);
            }
            cachedPointerData.position = Input.mousePosition;

            raycastResults.Clear();
            es.RaycastAll(cachedPointerData, raycastResults);

            EditorGUILayout.LabelField("Hit Count", raycastResults.Count.ToString());

            if (raycastResults.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("현재 포인터 아래에 닿은 UI가 없습니다.", MessageType.None);
                return;
            }

            for (int i = 0; i < raycastResults.Count && i < 10; i++)
            {
                RaycastResult r = raycastResults[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label($"[{i}]", GUILayout.Width(25));
                GUILayout.Label(r.gameObject != null ? r.gameObject.name : "(null)",
                    GUILayout.ExpandWidth(true));
                GUILayout.Label($"d={r.distance:F1}", GUILayout.Width(55));
                if (SWEditorUtils.SmallButton("Ping", 40f))
                {
                    if (r.gameObject != null)
                    {
                        Selection.activeGameObject = r.gameObject;
                        EditorGUIUtility.PingObject(r.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        #endregion

        #region Input 탭
        private void DrawInputTab()
        {
            SWEditorUtils.DrawHeader("Keyboard / Mouse");

            if (!Application.isPlaying)
            {
                SWEditorUtils.DrawEmptyNotice("플레이 중에만 입력이 표시됩니다.");
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                DrawMouseButton("LMB", 0);
                DrawMouseButton("RMB", 1);
                DrawMouseButton("MMB", 2);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Mouse ScrollDelta", Input.mouseScrollDelta.ToString("F2"));
                EditorGUILayout.LabelField("Any Key", Input.anyKey ? "● 눌림" : "○");

                string modifiers = "";
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) modifiers += "Shift ";
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) modifiers += "Ctrl ";
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) modifiers += "Alt ";
                EditorGUILayout.LabelField("Modifiers", string.IsNullOrEmpty(modifiers) ? "(없음)" : modifiers);
            }
        }

        private void DrawMouseButton(string label, int button)
        {
            bool pressed = Input.GetMouseButton(button);
            GUI.backgroundColor = pressed ? Color.green : Color.white;
            GUILayout.Button(pressed ? $"{label} ●" : $"{label} ○", GUILayout.Height(22));
            GUI.backgroundColor = Color.white;
        }

        #endregion
    }
}
