using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

using SW.EditorTools.Util;

using SW.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// 선택한 에셋을 참조하는 다른 에셋/씬/프리팹을 역방향으로 찾아주는 윈도우입니다.
    /// </summary>
    /// <remarks>
    /// 정방향 의존성, 역방향 참조, 미사용 에셋 후보와 직렬화 필드의 상세 참조 위치를 검색합니다.
    /// </remarks>
    public class SWReferenceFinderWindow : EditorWindow
    {
        #region 데이터
        /// <summary>검색 모드입니다.</summary>
        private enum SearchMode
        {
            /// <summary>대상을 참조하는 에셋을 찾습니다.</summary>
            ReverseReference = 0,
            /// <summary>대상이 참조하는 에셋을 찾습니다.</summary>
            Dependency = 1,
            /// <summary>아무도 참조하지 않는 에셋을 찾습니다.</summary>
            Unused = 2,
        }
        #endregion // 데이터

        #region 필드
        private const string INCLUDE_PACKAGES_KEY = "SWTools.ReferenceFinder.IncludePackages";
        private const string SEARCH_EXTENSIONS_KEY = "SWTools.ReferenceFinder.SearchExtensions";
        private const string SEARCH_MODE_KEY = "SWTools.ReferenceFinder.SearchMode";

        private static readonly string[] ModeTabNames = { "역참조", "의존성", "미사용" };

        private static readonly string[] defaultSearchExtensions =
        {
            ".unity", ".prefab", ".asset", ".mat", ".controller",
            ".overrideController", ".playable", ".spriteatlas", ".anim",
            ".mask", ".preset", ".shadergraph", ".shadervariants", ".guiskin",
            ".physicMaterial", ".physicsMaterial2D", ".fontsettings", ".mixer",
        };

        /// <summary>미사용 검색에서 제외할 폴더 경로 조각입니다. 코드로 로드될 가능성이 높은 위치입니다.</summary>
        private static readonly string[] unusedExcludedFolders =
        {
            "/Editor/", "/Resources/", "/StreamingAssets/", "/Plugins/", "/Gizmos/",
        };

        /// <summary>미사용 검색에서 제외할 확장자입니다.</summary>
        private static readonly HashSet<string> unusedExcludedExtensions = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".asmdef", ".asmref", ".rsp", ".dll", ".shader", ".cginc", ".hlsl", ".uss", ".uxml",
        };

        private Object targetAsset;
        private bool includePackages = false;
        private string searchExtensionsRaw;
        private SearchMode searchMode = SearchMode.ReverseReference;
        private bool recursiveDependencies = false;

        // 역참조 인덱스 캐시
        private Dictionary<string, List<string>> reverseIndex;
        private double lastIndexBuildTime;
        private int indexedAssetCount;

        // 현재 검색 결과
        private List<string> currentResults = new();
        private Vector2 resultsScroll;

        // 진행 상황
        private bool isBuilding;

        // 일괄 선택용
        private readonly HashSet<string> selectedResults = new();

        // 필터
        private string resultFilter = "";

        // 결과 타입별 아이콘 캐시
        private readonly Dictionary<string, GUIContent> guiContentCache = new();

        // 내부 참조 위치 결과
        private string detailSourcePath;
        private readonly List<string> detailResults = new();
        private Vector2 detailScroll;
        #endregion // 필드

        /// <summary>
        /// Reference Finder 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/Reference Finder %#r")]
        public static void ShowWindow()
        {
            SWReferenceFinderWindow window = GetWindow<SWReferenceFinderWindow>();
            SWEditorUtils.SetupWindow(window, "Reference Finder", "d_Search Icon", 380, 400);
            window.Show();
        }

        [MenuItem("Assets/SWTools/Find References In Project", false, 25)]
        private static void FindReferencesForSelected()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            SWReferenceFinderWindow window = GetWindow<SWReferenceFinderWindow>();
            SWEditorUtils.SetupWindow(window, "Reference Finder", "d_Search Icon", 380, 400);
            window.targetAsset = selected;
            window.searchMode = SearchMode.ReverseReference;
            window.Show();
            window.FindReferences();
        }

        [MenuItem("Assets/SWTools/Find References In Project", true)]
        private static bool FindReferencesForSelectedValidate()
        {
            return Selection.activeObject != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        private void OnEnable()
        {
            includePackages = SWEditorUtils.LoadPref(INCLUDE_PACKAGES_KEY, false);
            searchExtensionsRaw = SWEditorUtils.LoadPref(SEARCH_EXTENSIONS_KEY, string.Join(",", defaultSearchExtensions));
            searchMode = (SearchMode)SWEditorUtils.LoadPref(SEARCH_MODE_KEY, 0);
        }

        private void OnDisable()
        {
            SWEditorUtils.SavePref(INCLUDE_PACKAGES_KEY, includePackages);
            SWEditorUtils.SavePref(SEARCH_EXTENSIONS_KEY, searchExtensionsRaw);
            SWEditorUtils.SavePref(SEARCH_MODE_KEY, (int)searchMode);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            SearchMode previousMode = searchMode;
            searchMode = (SearchMode)SWEditorUtils.DrawTabBar((int)searchMode, ModeTabNames);
            if (previousMode != searchMode)
            {
                currentResults.Clear();
                selectedResults.Clear();
                ClearDetailResults();
            }

            EditorGUILayout.Space(5);

            if (searchMode != SearchMode.Unused)
                DrawTargetSection();
            else
                DrawUnusedSection();

            EditorGUILayout.Space(10);
            DrawIndexSection();

            EditorGUILayout.Space(10);
            DrawResultsSection();

            if (detailResults.Count > 0 || !string.IsNullOrEmpty(detailSourcePath))
            {
                EditorGUILayout.Space(10);
                DrawDetailSection();
            }
        }

        #region Target 섹션
        private void DrawTargetSection()
        {
            SWEditorUtils.DrawHeader("Target Asset");

            EditorGUI.BeginChangeCheck();
            targetAsset = EditorGUILayout.ObjectField("찾을 에셋", targetAsset, typeof(Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                currentResults.Clear();
                selectedResults.Clear();
                ClearDetailResults();
            }

            if (searchMode == SearchMode.Dependency)
                recursiveDependencies = EditorGUILayout.ToggleLeft("재귀 검색 (간접 의존성 포함)", recursiveDependencies);

            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(targetAsset != null))
            {
                string searchButtonLabel = searchMode == SearchMode.Dependency ? "의존성 찾기" : "참조 찾기";
                if (GUILayout.Button(searchButtonLabel, GUILayout.Height(28)))
                {
                    RunSearch();
                }
            }

            if (GUILayout.Button("선택한 에셋으로", GUILayout.Width(120), GUILayout.Height(28)))
            {
                if (Selection.activeObject != null)
                {
                    targetAsset = Selection.activeObject;
                    RunSearch();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (targetAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(targetAsset);
                EditorGUILayout.LabelField("Path", path, EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 미사용 모드의 검색 섹션을 그립니다.
        /// </summary>
        private void DrawUnusedSection()
        {
            SWEditorUtils.DrawHeader("Unused Assets");

            EditorGUILayout.HelpBox(
                "역참조 인덱스에서 아무도 참조하지 않는 Assets/ 하위 에셋 후보를 찾습니다.\n" +
                "주의: Resources.Load, Addressables, 코드에 의한 로드는 감지하지 못합니다.\n" +
                "Editor/Resources/StreamingAssets/Plugins 폴더와 스크립트/셰이더, 빌드 세팅에 포함된 씬은 제외됩니다.",
                MessageType.Warning);

            if (GUILayout.Button("미사용 에셋 찾기", GUILayout.Height(28)))
            {
                FindUnusedAssets();
            }
        }

        /// <summary>
        /// 현재 모드에 맞는 검색을 실행합니다.
        /// </summary>
        private void RunSearch()
        {
            ClearDetailResults();

            if (searchMode == SearchMode.Dependency)
                FindDependencies();
            else
                FindReferences();
        }
        #endregion // Target 섹션

        #region Index 섹션
        private void DrawIndexSection()
        {
            // 의존성 모드는 인덱스가 필요 없습니다.
            if (searchMode == SearchMode.Dependency) return;

            SWEditorUtils.DrawHeader("Index");

            EditorGUILayout.BeginHorizontal();
            includePackages = EditorGUILayout.ToggleLeft("Packages 포함", includePackages, GUILayout.Width(120));
            if (GUILayout.Button("인덱스 재구축", GUILayout.Height(20)))
            {
                BuildReverseIndex();
            }
            if (GUILayout.Button("캐시 지우기", GUILayout.Height(20)))
            {
                reverseIndex = null;
                indexedAssetCount = 0;
                currentResults.Clear();
                selectedResults.Clear();
                ClearDetailResults();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("검색 대상 확장자 (쉼표 구분)", EditorStyles.miniLabel);
            searchExtensionsRaw = EditorGUILayout.TextField(searchExtensionsRaw);

            if (GUILayout.Button("확장자 기본값으로", GUILayout.Height(18)))
            {
                searchExtensionsRaw = string.Join(",", defaultSearchExtensions);
            }

            if (reverseIndex != null)
            {
                EditorGUILayout.HelpBox(
                    $"인덱스: {indexedAssetCount}개 파일 스캔됨, {reverseIndex.Count}개 고유 GUID 참조됨\n" +
                    $"최근 빌드 시간: {SWEditorUtils.FormatDuration(lastIndexBuildTime)}",
                    MessageType.Info);
            }
            else
            {
                SWEditorUtils.DrawEmptyNotice("아직 인덱스가 없습니다. \"참조 찾기\" 또는 \"인덱스 재구축\"을 누르세요.", MessageType.None);
            }
        }
        #endregion // Index 섹션

        #region 결과 섹션
        private void DrawResultsSection()
        {
            SWEditorUtils.DrawHeader($"Results ({currentResults.Count})");

            if (currentResults.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("검색 결과가 없습니다.", MessageType.None);
                return;
            }

            // 결과 내 필터
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("필터", GUILayout.Width(30));
            resultFilter = EditorGUILayout.TextField(resultFilter);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                resultFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 일괄 액션 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모두 선택", GUILayout.Height(20)))
            {
                foreach (string p in GetFilteredResults()) selectedResults.Add(p);
            }
            if (GUILayout.Button("선택 해제", GUILayout.Height(20)))
            {
                selectedResults.Clear();
            }
            using (new SWEditorUtils.GUIEnabledScope(selectedResults.Count > 0))
            {
                if (GUILayout.Button($"Project에서 선택 ({selectedResults.Count})", GUILayout.Height(20)))
                {
                    SelectInProject(selectedResults);
                }
            }
            if (GUILayout.Button("경로 복사", GUILayout.Width(80), GUILayout.Height(20)))
            {
                CopyResultPathsToClipboard();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            resultsScroll = EditorGUILayout.BeginScrollView(resultsScroll);

            foreach (string path in GetFilteredResults())
            {
                DrawResultRow(path);
            }

            EditorGUILayout.EndScrollView();
        }

        private IEnumerable<string> GetFilteredResults()
        {
            if (string.IsNullOrEmpty(resultFilter)) return currentResults;
            return currentResults.Where(p => SWEditorUtils.MatchesFilter(p, resultFilter));
        }

        private void DrawResultRow(string path)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            bool wasSelected = selectedResults.Contains(path);
            bool nowSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(16));
            if (nowSelected != wasSelected)
            {
                if (nowSelected) selectedResults.Add(path);
                else selectedResults.Remove(path);
            }

            if (!guiContentCache.TryGetValue(path, out GUIContent content))
            {
                Texture icon = AssetDatabase.GetCachedIcon(path);
                content = new GUIContent(Path.GetFileName(path), icon, path);
                guiContentCache[path] = content;
            }

            GUILayout.Label(content, GUILayout.ExpandWidth(true), GUILayout.Height(18));

            // 역참조 모드에서만: 이 에셋 내부의 어느 위치가 대상을 참조하는지 찾기
            if (searchMode == SearchMode.ReverseReference && targetAsset != null)
            {
                if (SWEditorUtils.SmallButton("내부", 40f))
                {
                    FindInsideReferences(path);
                }
            }

            if (GUILayout.Button("◎", GUILayout.Width(22), GUILayout.Height(18)))
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }

            if (SWEditorUtils.SmallButton("Open", 45f))
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null) AssetDatabase.OpenAsset(obj);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SelectInProject(IEnumerable<string> paths)
        {
            List<Object> objs = new();
            foreach (string p in paths)
            {
                Object o = AssetDatabase.LoadMainAssetAtPath(p);
                if (o != null) objs.Add(o);
            }
            Selection.objects = objs.ToArray();
        }

        /// <summary>
        /// 현재 필터를 통과한 결과 경로를 클립보드에 복사합니다.
        /// </summary>
        private void CopyResultPathsToClipboard()
        {
            StringBuilder builder = new();
            foreach (string path in GetFilteredResults())
                builder.AppendLine(path);

            GUIUtility.systemCopyBuffer = builder.ToString();
            ShowNotification(new GUIContent("경로 복사 완료"));
        }
        #endregion // 결과 섹션

        #region 내부 참조 위치
        /// <summary>
        /// 결과 에셋 내부에서 대상을 참조하는 GameObject/컴포넌트/필드 위치를 찾습니다.
        /// </summary>
        /// <param name="sourcePath">검사할 에셋 경로입니다.</param>
        private void FindInsideReferences(string sourcePath)
        {
            detailResults.Clear();
            detailSourcePath = sourcePath;

            string targetPath = AssetDatabase.GetAssetPath(targetAsset);
            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            try
            {
                if (extension == ".prefab")
                {
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(sourcePath);
                    try
                    {
                        ScanGameObjectTree(prefabRoot.transform, targetPath);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
                else if (extension == ".unity")
                {
                    Scene scene = SceneManager.GetSceneByPath(sourcePath);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        detailResults.Add("(씬이 열려 있지 않습니다. 씬을 연 뒤 다시 시도하세요.)");
                        return;
                    }

                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    for (int index = 0; index < rootObjects.Length; index++)
                        ScanGameObjectTree(rootObjects[index].transform, targetPath);
                }
                else
                {
                    // ScriptableObject, Material 등 일반 에셋
                    Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
                    for (int index = 0; index < subAssets.Length; index++)
                    {
                        if (subAssets[index] == null) continue;
                        ScanObjectProperties(subAssets[index], targetPath, subAssets[index].name);
                    }
                }

                if (detailResults.Count == 0)
                    detailResults.Add("(직렬화 필드에서 직접 참조를 찾지 못했습니다.)");
            }
            catch (System.Exception exception)
            {
                detailResults.Add($"(검사 실패: {exception.Message})");
            }
        }

        /// <summary>
        /// GameObject 트리를 순회하며 컴포넌트의 직렬화 필드를 검사합니다.
        /// </summary>
        private void ScanGameObjectTree(Transform root, string targetPath)
        {
            Component[] components = root.GetComponents<Component>();
            string hierarchyPath = GetHierarchyPath(root);

            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] == null) continue;
                ScanObjectProperties(components[index], targetPath,
                    $"{hierarchyPath} > {components[index].GetType().Name}");
            }

            for (int index = 0; index < root.childCount; index++)
                ScanGameObjectTree(root.GetChild(index), targetPath);
        }

        /// <summary>
        /// 오브젝트의 직렬화 필드를 순회하며 대상 에셋 참조를 찾습니다. 서브에셋(스프라이트 등) 참조도 감지합니다.
        /// </summary>
        private void ScanObjectProperties(Object scanTarget, string targetPath, string locationLabel)
        {
            SerializedObject serializedObject = new(scanTarget);
            SerializedProperty property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue == null) continue;

                string referencedPath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                if (referencedPath != targetPath) continue;

                detailResults.Add($"{locationLabel} . {property.propertyPath}");
            }
        }

        /// <summary>
        /// Transform의 계층 경로 문자열을 반환합니다.
        /// </summary>
        private static string GetHierarchyPath(Transform transform)
        {
            StringBuilder builder = new(transform.name);
            Transform parent = transform.parent;

            while (parent != null)
            {
                builder.Insert(0, '/').Insert(0, parent.name);
                parent = parent.parent;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 내부 참조 위치 결과 섹션을 그립니다.
        /// </summary>
        private void DrawDetailSection()
        {
            SWEditorUtils.DrawHeader($"내부 참조 위치 ({detailResults.Count})");
            EditorGUILayout.LabelField(detailSourcePath, EditorStyles.miniLabel);

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll, GUILayout.MaxHeight(140f));
            for (int index = 0; index < detailResults.Count; index++)
                EditorGUILayout.LabelField(detailResults[index], EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("결과 닫기", GUILayout.Height(20)))
                ClearDetailResults();
        }

        /// <summary>
        /// 내부 참조 위치 결과를 초기화합니다.
        /// </summary>
        private void ClearDetailResults()
        {
            detailResults.Clear();
            detailSourcePath = null;
        }
        #endregion // 내부 참조 위치

        #region 인덱스 빌드 / 검색 로직
        private void FindReferences()
        {
            if (targetAsset == null) return;

            if (reverseIndex == null)
            {
                BuildReverseIndex();
            }

            if (reverseIndex == null) return;

            string targetPath = AssetDatabase.GetAssetPath(targetAsset);
            string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);

            currentResults.Clear();
            selectedResults.Clear();
            guiContentCache.Clear();

            if (reverseIndex.TryGetValue(targetGuid, out List<string> refs))
            {
                currentResults.AddRange(refs.Where(p => p != targetPath).Distinct().OrderBy(p => p));
            }

            SWLog.Log($"[SWReferenceFinder] '{targetPath}'를 참조하는 에셋 {currentResults.Count}개를 찾았습니다.");
        }

        /// <summary>
        /// 대상 에셋이 참조하는 에셋(정방향 의존성)을 찾습니다.
        /// </summary>
        private void FindDependencies()
        {
            if (targetAsset == null) return;

            string targetPath = AssetDatabase.GetAssetPath(targetAsset);

            currentResults.Clear();
            selectedResults.Clear();
            guiContentCache.Clear();

            string[] dependencies = AssetDatabase.GetDependencies(targetPath, recursiveDependencies);
            currentResults.AddRange(dependencies.Where(p => p != targetPath).Distinct().OrderBy(p => p));

            SWLog.Log($"[SWReferenceFinder] '{targetPath}'가 참조하는 에셋 {currentResults.Count}개를 찾았습니다. (재귀: {recursiveDependencies})");
        }

        /// <summary>
        /// 역참조 인덱스를 재활용해 아무도 참조하지 않는 에셋 후보를 찾습니다.
        /// </summary>
        private void FindUnusedAssets()
        {
            if (reverseIndex == null)
            {
                BuildReverseIndex();
            }

            if (reverseIndex == null) return;

            currentResults.Clear();
            selectedResults.Clear();
            guiContentCache.Clear();

            // 빌드 세팅에 포함된 씬은 사용 중으로 간주합니다.
            HashSet<string> buildScenePaths = new();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled)
                    buildScenePaths.Add(buildScene.path);
            }

            string[] allGuids = AssetDatabase.FindAssets("", new[] { "Assets" });

            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (buildScenePaths.Contains(path)) continue;

                string extension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(extension)) continue;
                if (unusedExcludedExtensions.Contains(extension)) continue;

                bool isExcludedFolder = false;
                for (int index = 0; index < unusedExcludedFolders.Length; index++)
                {
                    if (path.Contains(unusedExcludedFolders[index]))
                    {
                        isExcludedFolder = true;
                        break;
                    }
                }
                if (isExcludedFolder) continue;

                // 아무도 참조하지 않으면 미사용 후보입니다.
                if (!reverseIndex.ContainsKey(guid))
                    currentResults.Add(path);
            }

            currentResults.Sort(System.StringComparer.Ordinal);
            SWLog.Log($"[SWReferenceFinder] 미사용 에셋 후보 {currentResults.Count}개를 찾았습니다.");
        }

        private void BuildReverseIndex()
        {
            if (isBuilding) return;
            isBuilding = true;

            double startTime = EditorApplication.timeSinceStartup;

            try
            {
                HashSet<string> extSet = ParseExtensions(searchExtensionsRaw);

                string[] allGuids = AssetDatabase.FindAssets("");
                List<string> pathsToScan = new(allGuids.Length);

                foreach (string guid in allGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!path.StartsWith("Assets/") && !(includePackages && path.StartsWith("Packages/"))) continue;

                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (!extSet.Contains(ext)) continue;

                    pathsToScan.Add(path);
                }

                reverseIndex = new Dictionary<string, List<string>>(pathsToScan.Count * 2);
                indexedAssetCount = pathsToScan.Count;

                for (int i = 0; i < pathsToScan.Count; i++)
                {
                    string path = pathsToScan[i];

                    if (i % 50 == 0)
                    {
                        float progress = (float)i / pathsToScan.Count;
                        if (EditorUtility.DisplayCancelableProgressBar(
                            "Reference Finder",
                            $"의존성 스캔 중... ({i}/{pathsToScan.Count})\n{path}",
                            progress))
                        {
                            reverseIndex = null;
                            indexedAssetCount = 0;
                            return;
                        }
                    }

                    string[] deps = AssetDatabase.GetDependencies(path, false);
                    foreach (string dep in deps)
                    {
                        if (dep == path) continue;
                        string depGuid = AssetDatabase.AssetPathToGUID(dep);
                        if (string.IsNullOrEmpty(depGuid)) continue;

                        if (!reverseIndex.TryGetValue(depGuid, out List<string> list))
                        {
                            list = new List<string>();
                            reverseIndex[depGuid] = list;
                        }
                        list.Add(path);
                    }
                }

                lastIndexBuildTime = EditorApplication.timeSinceStartup - startTime;
                SWLog.Log($"[SWReferenceFinder] 인덱스 빌드 완료: {indexedAssetCount}개 파일, {reverseIndex.Count}개 고유 GUID, {SWEditorUtils.FormatDuration(lastIndexBuildTime)}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isBuilding = false;
            }
        }

        private HashSet<string> ParseExtensions(string raw)
        {
            HashSet<string> set = new(System.StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(raw)) return set;

            string[] tokens = raw.Split(new[] { ',', ';', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in tokens)
            {
                string token = t.Trim();
                if (string.IsNullOrEmpty(token)) continue;
                if (!token.StartsWith(".")) token = "." + token;
                set.Add(token.ToLowerInvariant());
            }
            return set;
        }
        #endregion // 인덱스 빌드 / 검색 로직
    }
}
