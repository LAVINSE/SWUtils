using System.Collections.Generic;
using SWPooling;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// SWPool의 프리팹별 생성량, 활성 수, 대기 수, 반환 예약을 확인하는 에디터 모니터 창입니다.
    /// </summary>
    public class SWPoolMonitorWindow : EditorWindow
    {
        #region 필드
        private const double REPAINT_INTERVAL = 0.1;

        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private double lastRepaintTime;
        #endregion // 필드

        /// <summary>
        /// Pool Monitor 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/Pool Monitor Window")]
        public static void ShowWindow()
        {
            SWPoolMonitorWindow window = GetWindow<SWPoolMonitorWindow>();
            SWEditorUtils.SetupWindow(window, "SW Pool Monitor", "d_Prefab Icon", 420, 420);
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
            DrawPoolSnapshots();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 상단 검색과 선택 버튼을 그립니다.
        /// </summary>
        private void DrawToolbar()
        {
            SWEditorUtils.DrawHeader("Pool 상태");

            EditorGUILayout.BeginHorizontal();
            searchText = EditorGUILayout.TextField("검색", searchText);

            SWPool pool = FindCurrentPool();
            using (new SWEditorUtils.GUIEnabledScope(pool != null))
            {
                if (GUILayout.Button("풀 선택", GUILayout.Width(70f)))
                {
                    Selection.activeObject = pool;
                    EditorGUIUtility.PingObject(pool);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("플레이 중에 생성된 풀 상태를 확인하는 창입니다.", MessageType.Info);
        }

        /// <summary>
        /// 현재 씬에 존재하는 SWPool을 찾습니다.
        /// </summary>
        /// <returns>찾은 SWPool입니다.</returns>
        private SWPool FindCurrentPool()
        {
            return Object.FindAnyObjectByType<SWPool>();
        }

        /// <summary>
        /// 풀 스냅샷 목록을 그립니다.
        /// </summary>
        private void DrawPoolSnapshots()
        {
            SWPool pool = FindCurrentPool();
            if (pool == null)
            {
                SWEditorUtils.DrawEmptyNotice("현재 씬에 SWPool이 없습니다.", MessageType.Warning);
                return;
            }

            IReadOnlyList<SWPoolSnapshot> snapshots = pool.GetPoolSnapshots();
            if (snapshots.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("아직 생성된 풀이 없습니다.", MessageType.Info);
                return;
            }

            DrawSummary(snapshots);

            int visibleCount = 0;
            for (int index = 0; index < snapshots.Count; index++)
            {
                SWPoolSnapshot snapshot = snapshots[index];
                if (!IsSearchMatched(snapshot)) continue;

                visibleCount++;
                DrawPoolSnapshot(snapshot);
            }

            if (visibleCount == 0)
                SWEditorUtils.DrawEmptyNotice("검색 조건에 맞는 풀이 없습니다.", MessageType.Info);
        }

        /// <summary>
        /// 전체 풀 요약을 그립니다.
        /// </summary>
        /// <param name="snapshots">풀 상태 목록입니다.</param>
        private void DrawSummary(IReadOnlyList<SWPoolSnapshot> snapshots)
        {
            int activeCount = 0;
            int inactiveCount = 0;
            int delayedReleaseCount = 0;

            for (int index = 0; index < snapshots.Count; index++)
            {
                activeCount += snapshots[index].ActiveCount;
                inactiveCount += snapshots[index].InactiveCount;
                delayedReleaseCount += snapshots[index].DelayedReleaseCount;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("풀 개수", snapshots.Count.ToString());
            EditorGUILayout.LabelField("활성 인스턴스", activeCount.ToString());
            EditorGUILayout.LabelField("대기 인스턴스", inactiveCount.ToString());
            EditorGUILayout.LabelField("지연 반환 예약", delayedReleaseCount.ToString());
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 풀 하나의 상태 행을 그립니다.
        /// </summary>
        /// <param name="snapshot">표시할 풀 스냅샷입니다.</param>
        private void DrawPoolSnapshot(SWPoolSnapshot snapshot)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(snapshot.Prefab, typeof(GameObject), false);
            if (GUILayout.Button("Ping", GUILayout.Width(45f)))
                EditorGUIUtility.PingObject(snapshot.Prefab);
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("풀 이름", JoinValues(snapshot.PoolNames));
                EditorGUILayout.TextField("그룹 이름", JoinValues(snapshot.GroupNames));
                EditorGUILayout.IntField("생성 수", snapshot.CreatedCount);
                EditorGUILayout.IntField("활성 수", snapshot.ActiveCount);
                EditorGUILayout.IntField("대기 수", snapshot.InactiveCount);
                EditorGUILayout.IntField("스폰 횟수", snapshot.SpawnCount);
                EditorGUILayout.IntField("반환 횟수", snapshot.ReleaseCount);
                EditorGUILayout.IntField("파괴 수", snapshot.DestroyedCount);
                EditorGUILayout.IntField("지연 반환", snapshot.DelayedReleaseCount);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 검색어와 풀 스냅샷의 일치 여부를 확인합니다.
        /// </summary>
        /// <param name="snapshot">검사할 풀 스냅샷입니다.</param>
        /// <returns>검색어가 비어 있거나 이름과 일치하면 true입니다.</returns>
        private bool IsSearchMatched(SWPoolSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            string keyword = searchText.Trim().ToLowerInvariant();
            string prefabName = snapshot.Prefab != null ? snapshot.Prefab.name.ToLowerInvariant() : string.Empty;

            return prefabName.Contains(keyword)
                || ContainsValue(snapshot.PoolNames, keyword)
                || ContainsValue(snapshot.GroupNames, keyword);
        }

        /// <summary>
        /// 문자열 목록 안에 검색어가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="values">검색할 문자열 목록입니다.</param>
        /// <param name="keyword">소문자로 변환된 검색어입니다.</param>
        /// <returns>포함되어 있으면 true입니다.</returns>
        private bool ContainsValue(IReadOnlyList<string> values, string keyword)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index].ToLowerInvariant().Contains(keyword))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 문자열 목록을 표시용 문자열로 합칩니다.
        /// </summary>
        /// <param name="values">합칠 문자열 목록입니다.</param>
        /// <returns>표시용 문자열입니다.</returns>
        private string JoinValues(IReadOnlyList<string> values)
        {
            return values.Count > 0 ? string.Join(", ", values) : "(없음)";
        }
    }
}
