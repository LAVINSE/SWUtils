using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SW.EditorTools.Util;

using SW.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// 가중치 확률 테이블을 대량 시뮬레이션해서 실제 분포와 획득 통계를 확인하는 에디터 창입니다.
    /// </summary>
    /// <remarks>
    /// 시뮬레이션은 런타임과 동일한 SWRandom.PickIndexWeighted 코드 경로를 사용하므로
    /// 여기서 확인한 분포가 실제 게임의 분포와 일치합니다.
    /// - 분포 검증: N회 뽑기 후 기대 확률 vs 실제 획득률과 오차를 표시합니다.
    /// - 획득 통계: 대상 항목을 처음 얻기까지의 평균/중앙값/상위 90%/최악 시도 횟수를 계산합니다.
    /// - 천장 옵션: N회째 보장(천장)을 켰을 때의 통계 변화를 확인할 수 있습니다.
    /// </remarks>
    public class SWRandomSimulatorWindow : EditorWindow
    {
        #region 데이터
        /// <summary>확률 테이블 항목입니다.</summary>
        [System.Serializable]
        private class TableEntry
        {
            /// <summary>항목 표시 이름입니다.</summary>
            public string name = "항목";
            /// <summary>항목이 선택될 상대 가중치입니다.</summary>
            public float weight = 1f;
        }
        #endregion // 데이터

        #region 필드
        private readonly List<TableEntry> entries = new()
        {
            new TableEntry { name = "일반", weight = 70f },
            new TableEntry { name = "희귀", weight = 25f },
            new TableEntry { name = "전설", weight = 5f },
        };

        private int trialCount = 100000;
        private bool useSeed;
        private int seed = 12345;

        private int targetIndex;
        private bool usePity;
        private int pityCount = 90;
        private int sessionCount = 10000;

        // 분포 결과
        private int[] resultCounts;
        private int resultTrialCount;

        // 획득 통계 결과
        private bool hasFirstHitResult;
        private float firstHitAverage;
        private int firstHitMedian;
        private int firstHitPercentile90;
        private int firstHitWorst;

        private Vector2 scrollPosition;
        #endregion // 필드

        /// <summary>
        /// Random Simulator 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/Simulation/Random Simulator")]
        public static void ShowWindow()
        {
            SWRandomSimulatorWindow window = GetWindow<SWRandomSimulatorWindow>();
            SWEditorUtils.SetupWindow(window, "Random Simulator", "d_Preset.Context", 380, 480);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTableSection();
            EditorGUILayout.Space(10);
            DrawSimulationSection();
            EditorGUILayout.Space(10);
            DrawDistributionResults();
            EditorGUILayout.Space(10);
            DrawFirstHitResults();

            EditorGUILayout.EndScrollView();
        }

        #region 테이블 섹션
        /// <summary>
        /// 확률 테이블 편집 UI를 그립니다.
        /// </summary>
        private void DrawTableSection()
        {
            SWEditorUtils.DrawHeader("확률 테이블");

            float totalWeight = GetTotalWeight();

            for (int index = 0; index < entries.Count; index++)
            {
                TableEntry entry = entries[index];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                entry.name = EditorGUILayout.TextField(entry.name);
                entry.weight = EditorGUILayout.FloatField(entry.weight, GUILayout.Width(70f));

                float probability = totalWeight > 0f && entry.weight > 0f
                    ? entry.weight / totalWeight * 100f
                    : 0f;
                EditorGUILayout.LabelField($"{probability:F3}%", GUILayout.Width(70f));

                if (GUILayout.Button("✕", GUILayout.Width(22f)))
                {
                    entries.RemoveAt(index);
                    ClearResults();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("항목 추가", GUILayout.Height(22f)))
            {
                entries.Add(new TableEntry());
                ClearResults();
            }
            EditorGUILayout.LabelField($"총 가중치: {totalWeight:F2}", GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();

            if (totalWeight <= 0f)
                EditorGUILayout.HelpBox("유효한(0보다 큰) 가중치가 하나 이상 필요합니다.", MessageType.Warning);
        }
        #endregion // 테이블 섹션

        #region 시뮬레이션 섹션
        /// <summary>
        /// 시뮬레이션 설정과 실행 버튼을 그립니다.
        /// </summary>
        private void DrawSimulationSection()
        {
            SWEditorUtils.DrawHeader("시뮬레이션 설정");

            trialCount = Mathf.Clamp(EditorGUILayout.IntField("뽑기 횟수", trialCount), 1, 10000000);

            EditorGUILayout.BeginHorizontal();
            useSeed = EditorGUILayout.ToggleLeft("시드 고정", useSeed, GUILayout.Width(90f));
            using (new SWEditorUtils.GUIEnabledScope(useSeed))
            {
                seed = EditorGUILayout.IntField(seed);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("획득 통계 (대상 항목을 처음 얻기까지)", EditorStyles.miniBoldLabel);

            string[] entryNames = GetEntryNames();
            targetIndex = EditorGUILayout.Popup("대상 항목", Mathf.Clamp(targetIndex, 0, Mathf.Max(0, entryNames.Length - 1)), entryNames);
            sessionCount = Mathf.Clamp(EditorGUILayout.IntField("시뮬레이션 세션 수", sessionCount), 100, 1000000);

            EditorGUILayout.BeginHorizontal();
            usePity = EditorGUILayout.ToggleLeft("천장 사용", usePity, GUILayout.Width(90f));
            using (new SWEditorUtils.GUIEnabledScope(usePity))
            {
                pityCount = Mathf.Max(1, EditorGUILayout.IntField(pityCount));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            using (new SWEditorUtils.GUIEnabledScope(GetTotalWeight() > 0f))
            {
                if (GUILayout.Button("시뮬레이션 실행", GUILayout.Height(30f)))
                {
                    RunSimulation();
                }
            }
        }
        #endregion // 시뮬레이션 섹션

        #region 결과 섹션
        /// <summary>
        /// 분포 결과를 그립니다.
        /// </summary>
        private void DrawDistributionResults()
        {
            if (resultCounts == null) return;

            SWEditorUtils.DrawHeader($"분포 결과 ({resultTrialCount:N0}회)");

            float totalWeight = GetTotalWeight();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("항목", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("기대", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            EditorGUILayout.LabelField("실제", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            EditorGUILayout.LabelField("획득 수", EditorStyles.miniBoldLabel, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();

            for (int index = 0; index < entries.Count && index < resultCounts.Length; index++)
            {
                float expectedRate = totalWeight > 0f && entries[index].weight > 0f
                    ? entries[index].weight / totalWeight * 100f
                    : 0f;
                float actualRate = resultTrialCount > 0
                    ? (float)resultCounts[index] / resultTrialCount * 100f
                    : 0f;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(entries[index].name);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{expectedRate:F3}%", GUILayout.Width(70f));
                EditorGUILayout.LabelField($"{actualRate:F3}%", GUILayout.Width(70f));
                EditorGUILayout.LabelField($"{resultCounts[index]:N0}", GUILayout.Width(80f));
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 획득 통계 결과를 그립니다.
        /// </summary>
        private void DrawFirstHitResults()
        {
            if (!hasFirstHitResult) return;

            string targetName = targetIndex < entries.Count ? entries[targetIndex].name : "?";
            SWEditorUtils.DrawHeader($"'{targetName}' 획득 통계 ({sessionCount:N0} 세션)");

            string pityText = usePity ? $" (천장 {pityCount}회)" : " (천장 없음)";
            EditorGUILayout.HelpBox(
                $"평균 시도: {firstHitAverage:F1}회{pityText}\n" +
                $"중앙값: {firstHitMedian}회 / 상위 90%: {firstHitPercentile90}회 이내\n" +
                $"최악 케이스: {firstHitWorst}회",
                MessageType.Info);
        }
        #endregion // 결과 섹션

        #region 시뮬레이션 로직
        /// <summary>
        /// 분포 시뮬레이션과 획득 통계 시뮬레이션을 실행합니다.
        /// 런타임과 동일한 SWRandom 코드 경로를 사용합니다.
        /// </summary>
        private void RunSimulation()
        {
            if (useSeed)
                SWRandom.SetSeed(seed);

            List<float> weights = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
                weights.Add(entries[index].weight);

            // 1) 분포 시뮬레이션
            resultCounts = new int[entries.Count];
            resultTrialCount = trialCount;

            for (int trial = 0; trial < trialCount; trial++)
            {
                int pickedIndex = SWRandom.PickIndexWeighted(weights);
                if (pickedIndex >= 0)
                    resultCounts[pickedIndex]++;
            }

            // 2) 대상 첫 획득 통계 시뮬레이션
            hasFirstHitResult = false;
            if (targetIndex >= 0 && targetIndex < entries.Count && entries[targetIndex].weight > 0f)
            {
                int[] attempts = new int[sessionCount];
                long totalAttempts = 0;

                for (int session = 0; session < sessionCount; session++)
                {
                    int attemptCount = 0;
                    while (true)
                    {
                        attemptCount++;

                        // 천장 도달 시 보장 획득
                        if (usePity && attemptCount >= pityCount)
                            break;

                        if (SWRandom.PickIndexWeighted(weights) == targetIndex)
                            break;
                    }

                    attempts[session] = attemptCount;
                    totalAttempts += attemptCount;
                }

                System.Array.Sort(attempts);
                firstHitAverage = (float)totalAttempts / sessionCount;
                firstHitMedian = attempts[sessionCount / 2];
                firstHitPercentile90 = attempts[Mathf.Min(sessionCount - 1, Mathf.FloorToInt(sessionCount * 0.9f))];
                firstHitWorst = attempts[sessionCount - 1];
                hasFirstHitResult = true;
            }

            SWLog.Log($"[SWRandomSimulator] 시뮬레이션 완료: 분포 {trialCount:N0}회, 세션 {sessionCount:N0}개");
        }

        /// <summary>
        /// 전체 가중치 합을 반환합니다. 0 이하 가중치는 제외합니다.
        /// </summary>
        private float GetTotalWeight()
        {
            float totalWeight = 0f;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].weight > 0f)
                    totalWeight += entries[index].weight;
            }

            return totalWeight;
        }

        /// <summary>
        /// 팝업용 항목 이름 배열을 반환합니다.
        /// </summary>
        private string[] GetEntryNames()
        {
            string[] names = new string[Mathf.Max(1, entries.Count)];
            for (int index = 0; index < entries.Count; index++)
                names[index] = $"{index}: {entries[index].name}";

            if (entries.Count == 0)
                names[0] = "(항목 없음)";

            return names;
        }

        /// <summary>
        /// 시뮬레이션 결과를 초기화합니다.
        /// </summary>
        private void ClearResults()
        {
            resultCounts = null;
            hasFirstHitResult = false;
        }
        #endregion // 시뮬레이션 로직
    }
}
