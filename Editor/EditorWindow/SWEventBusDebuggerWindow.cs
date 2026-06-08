using System;
using System.Collections.Generic;
using SWUtils;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// SWEventBus에 등록된 이벤트 타입, 리스너 수, 발행 기록을 확인하는 에디터 디버거 창입니다.
    /// </summary>
    public class SWEventBusDebuggerWindow : EditorWindow
    {
        #region 필드
        private const double REPAINT_INTERVAL = 0.1;

        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private double lastRepaintTime;
        #endregion // 필드

        /// <summary>
        /// EventBus Debugger 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/EventBus Debugger Window")]
        public static void ShowWindow()
        {
            SWEventBusDebuggerWindow window = GetWindow<SWEventBusDebuggerWindow>();
            SWEditorUtils.SetupWindow(window, "SW EventBus Debugger", "d_UnityEditor.ConsoleWindow", 360, 420);
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
            if (SWEditorUtils.ShouldRepaint(ref lastRepaintTime, REPAINT_INTERVAL))
                Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawEventSnapshots();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 상단 검색과 기록 정리 버튼을 그립니다.
        /// </summary>
        private void DrawToolbar()
        {
            SWEditorUtils.DrawHeader("EventBus 상태");

            EditorGUILayout.BeginHorizontal();
            searchText = EditorGUILayout.TextField("검색", searchText);

            if (GUILayout.Button("발행 기록 초기화", GUILayout.Width(110f)))
                SWEventBus.ClearPublishRecords();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("리스너 제거 없이 발행 횟수와 마지막 발행 기록만 초기화할 수 있습니다.", MessageType.None);
        }

        /// <summary>
        /// 이벤트 버스 스냅샷 목록을 그립니다.
        /// </summary>
        private void DrawEventSnapshots()
        {
            IReadOnlyList<SWEventBusEventSnapshot> snapshots = SWEventBus.GetEventSnapshots();
            if (snapshots.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("등록되거나 발행된 이벤트가 없습니다.", MessageType.Info);
                return;
            }

            int visibleCount = 0;
            for (int index = 0; index < snapshots.Count; index++)
            {
                SWEventBusEventSnapshot snapshot = snapshots[index];
                if (!IsSearchMatched(snapshot)) continue;

                visibleCount++;
                DrawEventSnapshot(snapshot);
            }

            if (visibleCount == 0)
                SWEditorUtils.DrawEmptyNotice("검색 조건에 맞는 이벤트가 없습니다.", MessageType.Info);
        }

        /// <summary>
        /// 이벤트 하나의 상태 행을 그립니다.
        /// </summary>
        /// <param name="snapshot">표시할 이벤트 스냅샷입니다.</param>
        private void DrawEventSnapshot(SWEventBusEventSnapshot snapshot)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(snapshot.EventType.Name, EditorStyles.boldLabel);
            GUILayout.Label($"리스너 {snapshot.ListenerCount}", GUILayout.Width(70f));
            GUILayout.Label($"발행 {snapshot.PublishCount}", GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("전체 이름", GetFullName(snapshot));
                EditorGUILayout.TextField("마지막 발행", FormatPublishTime(snapshot.LastPublishTime));
                EditorGUILayout.TextField("마지막 데이터", string.IsNullOrEmpty(snapshot.LastPayloadText)
                    ? "(없음)"
                    : snapshot.LastPayloadText);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 검색어와 이벤트 스냅샷의 일치 여부를 확인합니다.
        /// </summary>
        /// <param name="snapshot">검사할 이벤트 스냅샷입니다.</param>
        /// <returns>검색어가 비어 있거나 이벤트 이름과 일치하면 true입니다.</returns>
        private bool IsSearchMatched(SWEventBusEventSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            string keyword = searchText.Trim();
            return snapshot.EventType.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                || GetFullName(snapshot).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 이벤트 타입의 전체 이름을 표시 가능한 문자열로 반환합니다.
        /// </summary>
        /// <param name="snapshot">이름을 가져올 이벤트 스냅샷입니다.</param>
        /// <returns>이벤트 타입 전체 이름입니다.</returns>
        private string GetFullName(SWEventBusEventSnapshot snapshot)
        {
            return string.IsNullOrEmpty(snapshot.EventType.FullName)
                ? snapshot.EventType.Name
                : snapshot.EventType.FullName;
        }

        /// <summary>
        /// 발행 시간을 표시용 문자열로 변환합니다.
        /// </summary>
        /// <param name="publishTime">마지막 발행 시간입니다.</param>
        /// <returns>표시용 시간 문자열입니다.</returns>
        private string FormatPublishTime(DateTime? publishTime)
        {
            return publishTime.HasValue ? publishTime.Value.ToString("HH:mm:ss.fff") : "(없음)";
        }
    }
}
