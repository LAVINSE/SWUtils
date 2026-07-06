using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SW.Editor.Util;

using SW.Util;

namespace SW.Editor.Window
{
    /// <summary>
    /// SWEventBus의 이벤트 타입별 리스너 수와 발행 기록을 확인하는 에디터 디버깅 창입니다.
    /// </summary>
    /// <remarks>
    /// 이벤트 검색과 정렬을 지원하며 마지막 발행 데이터는 펼침 영역에 표시합니다.
    /// </remarks>
    public class SWEventBusDebuggerWindow : EditorWindow
    {
        #region 필드
        private const double REPAINT_INTERVAL = 0.1;
        private const double SNAPSHOT_INTERVAL = 0.25;
        private const string SearchSessionKey = "SWTools.EventBusDebugger.Search";
        private const string SortSessionKey = "SWTools.EventBusDebugger.Sort";
        private const string ExpandedSessionKey = "SWTools.EventBusDebugger.Expanded";

        private static readonly string[] SortNames = { "이름순", "발행 많은순", "최근 발행순" };

        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private int sortMode;
        private double lastRepaintTime;
        private double lastSnapshotTime;

        /// <summary>캐시된 이벤트 스냅샷입니다. SNAPSHOT_INTERVAL마다 갱신됩니다.</summary>
        private List<SWEventBusEventSnapshot> cachedSnapshots = new();
        /// <summary>펼쳐진 행의 이벤트 타입 이름 집합입니다.</summary>
        private readonly HashSet<string> expandedTypeNames = new();

        private GUIStyle rightAlignedLabelStyle;
        private GUIStyle boldMiniLabelStyle;
        private GUIStyle payloadStyle;
        #endregion // 필드

        /// <summary>
        /// EventBus Debugger 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/EventBus Debugger Window")]
        public static void ShowWindow()
        {
            SWEventBusDebuggerWindow window = GetWindow<SWEventBusDebuggerWindow>();
            SWEditorUtils.SetupWindow(window, "SW EventBus", "d_UnityEditor.ConsoleWindow", 360, 420);
            window.Show();
        }

        private void OnEnable()
        {
            searchText = SessionState.GetString(SearchSessionKey, string.Empty);
            sortMode = SessionState.GetInt(SortSessionKey, 0);
            LoadExpandedState();
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
            EnsureStyles();
            RefreshSnapshotsIfNeeded();

            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawEventSnapshots();
            EditorGUILayout.EndScrollView();
        }

        #region 갱신
        /// <summary>
        /// 이벤트 스냅샷을 갱신 주기에 맞춰 갱신하고 정렬합니다.
        /// </summary>
        private void RefreshSnapshotsIfNeeded()
        {
            double now = EditorApplication.timeSinceStartup;
            if (cachedSnapshots.Count > 0 && now - lastSnapshotTime < SNAPSHOT_INTERVAL)
                return;

            lastSnapshotTime = now;

            cachedSnapshots.Clear();
            cachedSnapshots.AddRange(SWEventBus.GetEventSnapshots());
            SortSnapshots();
        }

        /// <summary>
        /// 현재 정렬 모드에 맞춰 스냅샷을 정렬합니다.
        /// </summary>
        private void SortSnapshots()
        {
            switch (sortMode)
            {
                case 1: // 발행 많은순
                    cachedSnapshots.Sort((left, right) => right.PublishCount.CompareTo(left.PublishCount));
                    break;
                case 2: // 최근 발행순
                    cachedSnapshots.Sort((left, right) =>
                    {
                        DateTime leftTime = left.LastPublishTime ?? DateTime.MinValue;
                        DateTime rightTime = right.LastPublishTime ?? DateTime.MinValue;
                        return rightTime.CompareTo(leftTime);
                    });
                    break;
                default: // 이름순
                    cachedSnapshots.Sort((left, right) => string.Compare(
                        left.EventType.Name, right.EventType.Name, StringComparison.Ordinal));
                    break;
            }
        }

        /// <summary>
        /// 스타일 객체를 준비합니다.
        /// </summary>
        private void EnsureStyles()
        {
            rightAlignedLabelStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            boldMiniLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            payloadStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        }
        #endregion // 갱신

        #region 툴바
        /// <summary>
        /// 상단 검색, 정렬, 기록 초기화 버튼을 그립니다.
        /// </summary>
        private void DrawToolbar()
        {
            SWEditorUtils.DrawHeader("EventBus 상태");

            EditorGUILayout.BeginHorizontal();

            string newSearchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                SessionState.SetString(SearchSessionKey, searchText);
            }

            int newSortMode = EditorGUILayout.Popup(sortMode, SortNames, EditorStyles.toolbarPopup, GUILayout.Width(96f));
            if (newSortMode != sortMode)
            {
                sortMode = newSortMode;
                SessionState.SetInt(SortSessionKey, sortMode);
                SortSnapshots();
            }

            if (GUILayout.Button("기록 초기화", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                SWEventBus.ClearPublishRecords();
                lastSnapshotTime = 0d;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool logEnabled = EditorGUILayout.ToggleLeft("이벤트 버스 로그 출력", SWEventBus.IsLogOutputEnabled);
            if (logEnabled != SWEventBus.IsLogOutputEnabled)
                SWEventBus.IsLogOutputEnabled = logEnabled;
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("플레이 중에 구독/발행된 이벤트 상태를 확인하는 창입니다.", MessageType.Info);
        }
        #endregion // 툴바

        #region 목록
        /// <summary>
        /// 이벤트 스냅샷 목록을 그립니다.
        /// </summary>
        private void DrawEventSnapshots()
        {
            if (cachedSnapshots.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("구독되거나 발행된 이벤트가 없습니다.", MessageType.Info);
                return;
            }

            DrawSummary();
            DrawColumnHeader();

            int visibleCount = 0;
            for (int index = 0; index < cachedSnapshots.Count; index++)
            {
                SWEventBusEventSnapshot snapshot = cachedSnapshots[index];
                if (!IsSearchMatched(snapshot)) continue;

                visibleCount++;
                DrawEventRow(snapshot);
            }

            if (visibleCount == 0)
                SWEditorUtils.DrawEmptyNotice("검색 조건에 맞는 이벤트가 없습니다.", MessageType.Info);
        }

        /// <summary>
        /// 전체 이벤트 요약을 한 줄로 그립니다.
        /// </summary>
        private void DrawSummary()
        {
            int listenerCount = 0;
            int publishCount = 0;

            for (int index = 0; index < cachedSnapshots.Count; index++)
            {
                listenerCount += cachedSnapshots[index].ListenerCount;
                publishCount += cachedSnapshots[index].PublishCount;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"이벤트 {cachedSnapshots.Count}", EditorStyles.boldLabel, GUILayout.Width(80f));
            GUILayout.Label($"리스너 {listenerCount}", GUILayout.Width(80f));
            GUILayout.Label($"누적 발행 {publishCount}");
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 컬럼 제목 행을 그립니다.
        /// </summary>
        private void DrawColumnHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(16f);
            GUILayout.Label("이벤트", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("리스너", boldMiniLabelStyle, GUILayout.Width(48f));
            GUILayout.Label("발행", boldMiniLabelStyle, GUILayout.Width(44f));
            GUILayout.Label("마지막 발행", boldMiniLabelStyle, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 이벤트 하나를 한 줄 요약 + 펼침 상세로 그립니다.
        /// </summary>
        /// <param name="snapshot">이벤트 상태입니다.</param>
        private void DrawEventRow(SWEventBusEventSnapshot snapshot)
        {
            string typeName = snapshot.EventType.Name;
            bool expanded = expandedTypeNames.Contains(typeName);
            bool hasListener = snapshot.ListenerCount > 0;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            bool newExpanded = GUILayout.Toggle(expanded, GUIContent.none, EditorStyles.foldout, GUILayout.Width(14f));
            if (newExpanded != expanded)
            {
                if (newExpanded) expandedTypeNames.Add(typeName);
                else expandedTypeNames.Remove(typeName);
                SaveExpandedState();
            }

            using (new SWEditorUtils.GUIColorScope(hasListener ? new Color(0.65f, 1f, 0.65f) : GUI.color))
            {
                GUILayout.Label(typeName, EditorStyles.label);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(snapshot.ListenerCount.ToString(), rightAlignedLabelStyle, GUILayout.Width(48f));
            GUILayout.Label(snapshot.PublishCount.ToString(), rightAlignedLabelStyle, GUILayout.Width(44f));
            GUILayout.Label(GetLastPublishText(snapshot.LastPublishTime), rightAlignedLabelStyle, GUILayout.Width(80f));

            EditorGUILayout.EndHorizontal();

            if (newExpanded)
                DrawEventDetails(snapshot);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 펼쳐진 이벤트의 상세 정보를 그립니다.
        /// </summary>
        /// <param name="snapshot">이벤트 상태입니다.</param>
        private void DrawEventDetails(SWEventBusEventSnapshot snapshot)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("전체 이름", snapshot.EventType.FullName);

            if (snapshot.LastPublishTime.HasValue)
                EditorGUILayout.LabelField("마지막 발행 시각", snapshot.LastPublishTime.Value.ToString("HH:mm:ss"));

            EditorGUILayout.LabelField("마지막 페이로드");
            string payloadText = string.IsNullOrEmpty(snapshot.LastPayloadText) ? "(없음)" : snapshot.LastPayloadText;
            EditorGUILayout.SelectableLabel(payloadText, payloadStyle,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2f));

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 마지막 발행 시간을 "n초 전" 형식으로 반환합니다.
        /// </summary>
        /// <param name="lastPublishTime">마지막 발행 시각입니다.</param>
        /// <returns>표시용 문자열입니다.</returns>
        private static string GetLastPublishText(DateTime? lastPublishTime)
        {
            if (!lastPublishTime.HasValue) return "-";

            TimeSpan elapsed = DateTime.Now - lastPublishTime.Value;
            if (elapsed.TotalSeconds < 1d) return "방금";
            if (elapsed.TotalSeconds < 60d) return $"{(int)elapsed.TotalSeconds}초 전";
            if (elapsed.TotalMinutes < 60d) return $"{(int)elapsed.TotalMinutes}분 전";
            return $"{(int)elapsed.TotalHours}시간 전";
        }

        /// <summary>
        /// 검색어와 이벤트가 일치하는지 확인합니다.
        /// </summary>
        /// <param name="snapshot">이벤트 상태입니다.</param>
        /// <returns>일치하면 true입니다.</returns>
        private bool IsSearchMatched(SWEventBusEventSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;

            return SWEditorUtils.MatchesFilter(snapshot.EventType.Name, searchText)
                || SWEditorUtils.MatchesFilter(snapshot.EventType.FullName, searchText);
        }
        #endregion // 목록

        #region 상태 유지
        /// <summary>
        /// 펼침 상태를 SessionState에 저장합니다.
        /// </summary>
        private void SaveExpandedState()
        {
            SessionState.SetString(ExpandedSessionKey, string.Join(",", expandedTypeNames));
        }

        /// <summary>
        /// SessionState에서 펼침 상태를 복원합니다.
        /// </summary>
        private void LoadExpandedState()
        {
            expandedTypeNames.Clear();

            string saved = SessionState.GetString(ExpandedSessionKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return;

            string[] tokens = saved.Split(',');
            for (int index = 0; index < tokens.Length; index++)
            {
                if (!string.IsNullOrEmpty(tokens[index]))
                    expandedTypeNames.Add(tokens[index]);
            }
        }
        #endregion // 상태 유지
    }
}
