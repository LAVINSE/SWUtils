using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using SW.Base;

using SW.EditorTools.Util;

using SW.Stat;

using SW.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// SWCategory, SWStat 데이터 에셋을 한 화면에서 생성/삭제/편집하는 에디터 윈도우입니다.
    /// </summary>
    /// <remarks>
    /// 타입별 에셋 목록과 선택한 에셋의 인스펙터를 함께 표시하며,
    /// 생성 경로, 이름 접두사, 자동 식별자와 정렬 방식을 설정할 수 있습니다.
    /// </remarks>
    public class SWStatSystemWindow : EditorWindow
    {
        #region 상수
        private const string PrefPrefix = "SWTools.StatEditor.";

        private static readonly Type[] ManagedTypes = { typeof(SWCategory), typeof(SWStat) };
        private static readonly string[] DefaultPaths = { "Assets/SWData/Category", "Assets/SWData/Stat" };
        private static readonly string[] DefaultPrefixes = { "CATEGORY_", "STAT_" };

        private static readonly string[] SortModeNames = { "코드명순", "표시명순", "ID순" };
        private static readonly string[] LabelModeNames = { "코드명", "표시명", "에셋 이름" };
        private const float DefaultListRowHeight = 24f;
        private const float ListRowPadding = 2f;
        private const float DefaultListIconSize = 20f;
        private const float DefaultDeleteButtonWidth = 22f;
        private const float DefaultDeleteButtonHeight = 18f;
        private const int DefaultListLabelFontSize = 12;
        private const float ListRowRightSafePadding = 18f;
        #endregion // 상수

        #region 필드
        private int toolbarIndex;
        private string[] toolbarNames;

        private readonly Dictionary<Type, List<SWIdentifiedObject>> assetsByType = new();
        private readonly Dictionary<Type, Vector2> scrollPositionsByType = new();
        private readonly Dictionary<Type, SWIdentifiedObject> selectedObjectsByType = new();
        private readonly Dictionary<Type, string> searchTextsByType = new();

        private Vector2 drawingEditorScrollPosition;
        private Vector2 settingsScrollPosition;
        private Editor cachedEditor;

        private Texture2D selectedBoxTexture;
        private GUIStyle selectedBoxStyle;

        // 설정 값 (EditorPrefs에 저장)
        private string[] createPaths;
        private string[] namePrefixes;
        private bool useAutoId = true;
        private bool autoSaveAssets = true;
        private float listWidth = 300f;
        private float listRowHeight = DefaultListRowHeight;
        private float listIconSize = DefaultListIconSize;
        private float deleteButtonWidth = DefaultDeleteButtonWidth;
        private float deleteButtonHeight = DefaultDeleteButtonHeight;
        private int listLabelFontSize = DefaultListLabelFontSize;
        private int sortMode;
        private int labelMode;
        #endregion // 필드

        /// <summary>
        /// Stat System 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/Data/Stat System Editor")]
        public static void ShowWindow()
        {
            SWStatSystemWindow window = GetWindow<SWStatSystemWindow>();
            SWEditorUtils.SetupWindow(window, "SW Stat System", "d_ScriptableObject Icon", 720, 480);
            window.Show();
        }

        #region 초기화
        private void OnEnable()
        {
            SetupStyle();
            LoadSettings();

            toolbarNames = new string[ManagedTypes.Length + 1];
            for (int index = 0; index < ManagedTypes.Length; index++)
            {
                Type type = ManagedTypes[index];
                toolbarNames[index] = type.Name;

                scrollPositionsByType.TryAdd(type, Vector2.zero);
                selectedObjectsByType.TryAdd(type, null);
                searchTextsByType.TryAdd(type, string.Empty);

                RefreshAssets(type);
            }

            toolbarNames[ManagedTypes.Length] = "설정";
        }

        private void OnDisable()
        {
            SaveSettings();
            DestroyImmediate(cachedEditor);
            DestroyImmediate(selectedBoxTexture);
        }

        /// <summary>
        /// 선택 행 강조용 스타일을 준비합니다.
        /// </summary>
        private void SetupStyle()
        {
            selectedBoxTexture = new Texture2D(1, 1);
            selectedBoxTexture.SetPixel(0, 0, new Color(0.31f, 0.40f, 0.50f));
            selectedBoxTexture.Apply();
            // Play 상태에 종속되어 파괴되지 않도록 DontSave 설정
            selectedBoxTexture.hideFlags = HideFlags.DontSave;

            selectedBoxStyle = new GUIStyle();
            selectedBoxStyle.normal.background = selectedBoxTexture;
        }
        #endregion // 초기화

        #region 설정 저장/불러오기
        /// <summary>
        /// EditorPrefs에서 설정을 불러옵니다.
        /// </summary>
        private void LoadSettings()
        {
            createPaths = new string[ManagedTypes.Length];
            namePrefixes = new string[ManagedTypes.Length];

            for (int index = 0; index < ManagedTypes.Length; index++)
            {
                string typeName = ManagedTypes[index].Name;
                createPaths[index] = SWEditorUtils.LoadPref($"{PrefPrefix}Path.{typeName}", DefaultPaths[index]);
                namePrefixes[index] = SWEditorUtils.LoadPref($"{PrefPrefix}Prefix.{typeName}", DefaultPrefixes[index]);
            }

            useAutoId = SWEditorUtils.LoadPref($"{PrefPrefix}UseAutoId", true);
            autoSaveAssets = SWEditorUtils.LoadPref($"{PrefPrefix}AutoSave", true);
            listWidth = SWEditorUtils.LoadPref($"{PrefPrefix}ListWidth", 300f);
            listRowHeight = SWEditorUtils.LoadPref($"{PrefPrefix}ListRowHeight", DefaultListRowHeight);
            listIconSize = SWEditorUtils.LoadPref($"{PrefPrefix}ListIconSize", DefaultListIconSize);
            deleteButtonWidth = SWEditorUtils.LoadPref($"{PrefPrefix}DeleteButtonWidth", DefaultDeleteButtonWidth);
            deleteButtonHeight = SWEditorUtils.LoadPref($"{PrefPrefix}DeleteButtonHeight", DefaultDeleteButtonHeight);
            listLabelFontSize = SWEditorUtils.LoadPref($"{PrefPrefix}ListLabelFontSize", DefaultListLabelFontSize);
            sortMode = SWEditorUtils.LoadPref($"{PrefPrefix}SortMode", 0);
            labelMode = SWEditorUtils.LoadPref($"{PrefPrefix}LabelMode", 0);
        }

        /// <summary>
        /// EditorPrefs에 설정을 저장합니다.
        /// </summary>
        private void SaveSettings()
        {
            for (int index = 0; index < ManagedTypes.Length; index++)
            {
                string typeName = ManagedTypes[index].Name;
                SWEditorUtils.SavePref($"{PrefPrefix}Path.{typeName}", createPaths[index]);
                SWEditorUtils.SavePref($"{PrefPrefix}Prefix.{typeName}", namePrefixes[index]);
            }

            SWEditorUtils.SavePref($"{PrefPrefix}UseAutoId", useAutoId);
            SWEditorUtils.SavePref($"{PrefPrefix}AutoSave", autoSaveAssets);
            SWEditorUtils.SavePref($"{PrefPrefix}ListWidth", listWidth);
            SWEditorUtils.SavePref($"{PrefPrefix}ListRowHeight", listRowHeight);
            SWEditorUtils.SavePref($"{PrefPrefix}ListIconSize", listIconSize);
            SWEditorUtils.SavePref($"{PrefPrefix}DeleteButtonWidth", deleteButtonWidth);
            SWEditorUtils.SavePref($"{PrefPrefix}DeleteButtonHeight", deleteButtonHeight);
            SWEditorUtils.SavePref($"{PrefPrefix}ListLabelFontSize", listLabelFontSize);
            SWEditorUtils.SavePref($"{PrefPrefix}SortMode", sortMode);
            SWEditorUtils.SavePref($"{PrefPrefix}LabelMode", labelMode);
        }

        /// <summary>
        /// 설정을 기본값으로 되돌립니다.
        /// </summary>
        private void ResetSettings()
        {
            for (int index = 0; index < ManagedTypes.Length; index++)
            {
                createPaths[index] = DefaultPaths[index];
                namePrefixes[index] = DefaultPrefixes[index];
            }

            useAutoId = true;
            autoSaveAssets = true;
            listWidth = 300f;
            listRowHeight = DefaultListRowHeight;
            listIconSize = DefaultListIconSize;
            deleteButtonWidth = DefaultDeleteButtonWidth;
            deleteButtonHeight = DefaultDeleteButtonHeight;
            listLabelFontSize = DefaultListLabelFontSize;
            sortMode = 0;
            labelMode = 0;
            SaveSettings();
        }
        #endregion // 설정 저장/불러오기

        #region GUI
        private void OnGUI()
        {
            toolbarIndex = SWEditorUtils.DrawTabBar(toolbarIndex, toolbarNames);

            if (toolbarIndex >= ManagedTypes.Length)
            {
                DrawSettingsTab();
                return;
            }

            DrawDataTab(ManagedTypes[toolbarIndex], toolbarIndex);
        }

        /// <summary>
        /// 타입별 데이터 탭(목록 + 인스펙터)을 그립니다.
        /// </summary>
        /// <param name="dataType">표시할 데이터 타입입니다.</param>
        /// <param name="typeIndex">ManagedTypes에서의 인덱스입니다.</param>
        private void DrawDataTab(Type dataType, int typeIndex)
        {
            List<SWIdentifiedObject> assets = assetsByType[dataType];
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(32, 32 + assets.Count));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(listWidth));
                {
                    DrawListToolButtons(dataType, typeIndex);

                    EditorGUILayout.Space(4f);

                    // 검색
                    searchTextsByType[dataType] = GUILayout.TextField(searchTextsByType[dataType], EditorStyles.toolbarSearchField);

                    scrollPositionsByType[dataType] = EditorGUILayout.BeginScrollView(
                        scrollPositionsByType[dataType], false, true,
                        GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                    {
                        DrawAssetList(dataType, assets);
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();

                SWIdentifiedObject selected = selectedObjectsByType[dataType];
                if (selected != null)
                {
                    drawingEditorScrollPosition = EditorGUILayout.BeginScrollView(drawingEditorScrollPosition);
                    {
                        EditorGUILayout.Space(2f);
                        DrawSelectedObjectHeader(dataType, selected);
                        Editor.CreateCachedEditor(selected, null, ref cachedEditor);
                        cachedEditor.OnInspectorGUI();
                    }
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.BeginVertical();
                    SWEditorUtils.DrawEmptyNotice("왼쪽 목록에서 에셋을 선택하거나 새로 만들어주세요.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 목록 상단의 생성/삭제/새로고침 버튼을 그립니다.
        /// </summary>
        private void DrawListToolButtons(Type dataType, int typeIndex)
        {
            using (new SWEditorUtils.GUIBgColorScope(new Color(0.6f, 1f, 0.6f)))
            {
                if (GUILayout.Button($"New {dataType.Name}", GUILayout.Height(24f)))
                    CreateNewAsset(dataType, typeIndex);
            }

            EditorGUILayout.BeginHorizontal();

            SWIdentifiedObject selected = selectedObjectsByType[dataType];
            using (new SWEditorUtils.GUIEnabledScope(selected != null))
            using (new SWEditorUtils.GUIBgColorScope(new Color(1f, 0.6f, 0.6f)))
            {
                if (GUILayout.Button("선택 삭제", GUILayout.Height(20f)))
                    DeleteAsset(dataType, selected);
            }

            if (GUILayout.Button("새로고침", GUILayout.Height(20f)))
                RefreshAssets(dataType);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("정렬", GUILayout.Width(30f));
            DrawSortShortcutButton("코드명", 0);
            DrawSortShortcutButton("표시명", 1);
            DrawSortShortcutButton("ID", 2);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 선택한 에셋의 이름 변경과 위치 확인 도구를 그립니다.
        /// </summary>
        private void DrawSelectedObjectHeader(Type dataType, SWIdentifiedObject selectedObject)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            string changedName = EditorGUILayout.DelayedTextField("에셋 이름", selectedObject.name);
            if (EditorGUI.EndChangeCheck())
            {
                RenameAsset(dataType, selectedObject, changedName);
            }

            if (GUILayout.Button("Ping", GUILayout.Width(45f)))
                SWEditorUtils.PingAndSelect(selectedObject);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 목록 정렬 기준을 바로 바꾸는 단축 버튼을 그립니다.
        /// </summary>
        private void DrawSortShortcutButton(string label, int targetSortMode)
        {
            bool isSelected = sortMode == targetSortMode;
            using (new SWEditorUtils.GUIBgColorScope(isSelected ? new Color(0.55f, 0.75f, 1f) : Color.white))
            {
                if (GUILayout.Button(label, EditorStyles.toolbarButton))
                    SetSortMode(targetSortMode);
            }
        }

        /// <summary>
        /// 에셋 목록을 그립니다.
        /// </summary>
        private void DrawAssetList(Type dataType, List<SWIdentifiedObject> assets)
        {
            if (assets.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice($"{dataType.Name} 에셋이 없습니다.", MessageType.None);
                return;
            }

            string searchText = searchTextsByType[dataType];
            bool anyDeleted = false;
            float drawRowHeight = GetListDrawRowHeight();

            for (int index = 0; index < assets.Count; index++)
            {
                SWIdentifiedObject data = assets[index];
                if (data == null) { anyDeleted = true; continue; }

                string label = GetListLabel(data);
                if (!string.IsNullOrEmpty(searchText)
                    && !SWEditorUtils.MatchesFilter(label, searchText)
                    && !SWEditorUtils.MatchesFilter(data.CodeName, searchText))
                {
                    continue;
                }

                Rect rowRectangle = GUILayoutUtility.GetRect(0f, drawRowHeight, GUILayout.ExpandWidth(true));
                bool isSelected = selectedObjectsByType[dataType] == data;
                string idText = data.ID != 0 ? $"[{data.ID}] " : string.Empty;
                bool isDeleteClicked = DrawListRow(
                    rowRectangle,
                    $"{idText}{label}",
                    isSelected,
                    iconRectangle => SWEditorUtils.DrawIdentifiedObjectIcon(iconRectangle, data),
                    out Rect deleteButtonRectangle);
                if (isDeleteClicked)
                {
                    DeleteAsset(dataType, data);
                    anyDeleted = true;
                }

                if (anyDeleted) break;

                // 행 클릭으로 선택
                if (Event.current.type == EventType.MouseDown
                    && rowRectangle.Contains(Event.current.mousePosition)
                    && !deleteButtonRectangle.Contains(Event.current.mousePosition))
                {
                    selectedObjectsByType[dataType] = data;
                    drawingEditorScrollPosition = Vector2.zero;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// 설정 탭을 그립니다.
        /// </summary>
        private void DrawSettingsTab()
        {
            settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);

            SWEditorUtils.DrawHeader("에셋 생성 설정");

            for (int index = 0; index < ManagedTypes.Length; index++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ManagedTypes[index].Name, EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                createPaths[index] = EditorGUILayout.TextField("생성 경로", createPaths[index]);
                if (GUILayout.Button("선택", GUILayout.Width(44f)))
                {
                    string pickedPath = PickProjectFolder(createPaths[index]);
                    if (!string.IsNullOrEmpty(pickedPath))
                        createPaths[index] = pickedPath;
                }
                EditorGUILayout.EndHorizontal();

                if (!IsValidProjectPath(createPaths[index]))
                    EditorGUILayout.HelpBox("경로는 Assets/ 로 시작해야 합니다.", MessageType.Warning);

                namePrefixes[index] = EditorGUILayout.TextField("파일 이름 접두사", namePrefixes[index]);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6f);
            SWEditorUtils.DrawHeader("동작 설정");

            useAutoId = EditorGUILayout.ToggleLeft("새 에셋에 자동 ID 부여 (현재 최대 ID + 1)", useAutoId);
            autoSaveAssets = EditorGUILayout.ToggleLeft("생성/삭제 시 즉시 저장 (SaveAssets)", autoSaveAssets);

            EditorGUILayout.Space(6f);
            SWEditorUtils.DrawHeader("표시 설정");

            listWidth = EditorGUILayout.Slider("목록 넓이", listWidth, 200f, 450f);
            listRowHeight = EditorGUILayout.Slider("목록 행 높이", listRowHeight, 20f, 48f);
            listIconSize = EditorGUILayout.Slider("목록 아이콘 크기", listIconSize, 16f, 40f);
            deleteButtonWidth = EditorGUILayout.Slider("삭제 버튼 넓이", deleteButtonWidth, 20f, 44f);
            deleteButtonHeight = EditorGUILayout.Slider("삭제 버튼 높이", deleteButtonHeight, 16f, 40f);
            listLabelFontSize = EditorGUILayout.IntSlider("목록 글자 크기", listLabelFontSize, 10, 18);
            labelMode = EditorGUILayout.Popup("목록 표시 이름", labelMode, LabelModeNames);

            DrawListDisplayPreview();

            int newSortMode = EditorGUILayout.Popup("정렬 기준", sortMode, SortModeNames);
            if (newSortMode != sortMode)
            {
                SetSortMode(newSortMode);
            }

            EditorGUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("설정 저장", GUILayout.Height(24f)))
            {
                SaveSettings();
                ShowNotification(new GUIContent("설정 저장 완료"));
            }

            if (GUILayout.Button("기본값 복원", GUILayout.Height(24f)))
            {
                if (EditorUtility.DisplayDialog("설정 초기화", "모든 설정을 기본값으로 되돌릴까요?", "복원", "취소"))
                {
                    ResetSettings();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 표시 설정 값이 적용된 목록 행을 미리보기로 그립니다.
        /// </summary>
        private void DrawListDisplayPreview()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

            Rect previewAreaRectangle = EditorGUILayout.GetControlRect(
                false,
                GetListDrawRowHeight() + 8f,
                GUILayout.MaxWidth(listWidth));
            GUI.Box(previewAreaRectangle, GUIContent.none, EditorStyles.helpBox);

            Rect rowRectangle = new(
                previewAreaRectangle.x + 4f,
                previewAreaRectangle.y + 4f,
                previewAreaRectangle.width - 8f,
                GetListDrawRowHeight());
            Texture previewIcon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image;
            DrawListRow(
                rowRectangle,
                "[1] CATEGORY_preview",
                true,
                iconRectangle =>
                {
                    if (previewIcon != null)
                        GUI.DrawTexture(iconRectangle, previewIcon, ScaleMode.ScaleToFit);
                },
                out _);
        }

        /// <summary>
        /// 현재 표시 설정에서 실제로 필요한 목록 행 높이를 반환합니다.
        /// </summary>
        private float GetListDrawRowHeight()
        {
            return Mathf.Max(listRowHeight, listIconSize + ListRowPadding * 2f, deleteButtonHeight + ListRowPadding * 2f);
        }

        /// <summary>
        /// 목록 행 하나를 현재 표시 설정으로 그립니다.
        /// </summary>
        private bool DrawListRow(Rect rowRectangle, string label, bool isSelected, Action<Rect> drawIcon, out Rect deleteButtonRectangle)
        {
            if (isSelected)
                GUI.Box(rowRectangle, GUIContent.none, selectedBoxStyle);

            Rect iconRectangle = new(
                rowRectangle.x + ListRowPadding,
                rowRectangle.y + (rowRectangle.height - listIconSize) * 0.5f,
                listIconSize,
                listIconSize);
            drawIcon?.Invoke(iconRectangle);

            deleteButtonRectangle = new(
                rowRectangle.xMax - ListRowRightSafePadding - deleteButtonWidth - ListRowPadding,
                rowRectangle.y + (rowRectangle.height - deleteButtonHeight) * 0.5f,
                deleteButtonWidth,
                deleteButtonHeight);

            Rect labelRectangle = new(
                iconRectangle.xMax + 4f,
                rowRectangle.y + ListRowPadding,
                Mathf.Max(1f, deleteButtonRectangle.x - iconRectangle.xMax - 8f),
                rowRectangle.height - ListRowPadding * 2f);
            GUIStyle listLabelStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = listLabelFontSize
            };
            EditorGUI.LabelField(labelRectangle, label, listLabelStyle);

            using (new SWEditorUtils.GUIBgColorScope(new Color(1f, 0.6f, 0.6f)))
            {
                GUIStyle deleteButtonStyle = new(GUI.skin.button)
                {
                    fontSize = listLabelFontSize
                };
                return GUI.Button(deleteButtonRectangle, "x", deleteButtonStyle);
            }
        }
        #endregion // GUI

        #region 에셋 관리
        /// <summary>
        /// 프로젝트에서 타입의 모든 에셋을 다시 수집하고 정렬합니다.
        /// </summary>
        private void RefreshAssets(Type dataType)
        {
            if (!assetsByType.TryGetValue(dataType, out List<SWIdentifiedObject> assets))
            {
                assets = new List<SWIdentifiedObject>();
                assetsByType[dataType] = assets;
            }

            assets.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{dataType.Name}");
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                var asset = AssetDatabase.LoadAssetAtPath(path, dataType) as SWIdentifiedObject;
                // 파생 타입(예: SWCategory 검색에 걸린 SWStat 방지)을 정확히 필터링
                if (asset != null && asset.GetType() == dataType)
                    assets.Add(asset);
            }

            SortAssets(assets);
        }

        /// <summary>
        /// 설정된 정렬 기준으로 목록을 정렬합니다.
        /// </summary>
        private void SortAssets(List<SWIdentifiedObject> assets)
        {
            switch (sortMode)
            {
                case 1: // 표시명순
                    assets.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
                    break;
                case 2: // ID순
                    assets.Sort((left, right) => left.ID.CompareTo(right.ID));
                    break;
                default: // 코드명순
                    assets.Sort((left, right) => string.Compare(left.CodeName, right.CodeName, StringComparison.Ordinal));
                    break;
            }
        }

        /// <summary>
        /// 정렬 기준을 변경하고 모든 관리 목록을 다시 정렬합니다.
        /// </summary>
        private void SetSortMode(int newSortMode)
        {
            if (sortMode == newSortMode)
            {
                return;
            }

            sortMode = newSortMode;
            for (int index = 0; index < ManagedTypes.Length; index++)
                SortAssets(assetsByType[ManagedTypes[index]]);

            SaveSettings();
        }

        /// <summary>
        /// 설정된 경로에 새 에셋을 생성합니다. 임시 코드명은 GUID, ID는 옵션에 따라 자동 부여됩니다.
        /// </summary>
        private void CreateNewAsset(Type dataType, int typeIndex)
        {
            string createPath = createPaths[typeIndex];
            if (!IsValidProjectPath(createPath))
            {
                EditorUtility.DisplayDialog("생성 실패", $"생성 경로가 올바르지 않습니다:\n{createPath}\n\n설정 탭에서 Assets/ 로 시작하는 경로를 지정해주세요.", "확인");
                return;
            }

            EnsureFolderExists(createPath);

            var guid = Guid.NewGuid();
            var newData = CreateInstance(dataType) as SWIdentifiedObject;

            // SerializedObject로 private 필드(codeName, id)를 설정
            SerializedObject serializedData = new(newData);
            serializedData.FindProperty("codeName").stringValue = guid.ToString();
            if (useAutoId)
                serializedData.FindProperty("id").intValue = GetNextId(dataType);
            serializedData.ApplyModifiedPropertiesWithoutUndo();

            string prefix = namePrefixes[typeIndex] ?? string.Empty;
            string assetPath = $"{createPath.TrimEnd('/')}/{prefix}{guid}.asset";
            AssetDatabase.CreateAsset(newData, assetPath);

            if (autoSaveAssets)
                AssetDatabase.SaveAssets();

            RefreshAssets(dataType);
            selectedObjectsByType[dataType] = newData;
            drawingEditorScrollPosition = Vector2.zero;

            SWLog.Log($"[SWStatSystemWindow] 생성 완료: {assetPath}");
        }

        /// <summary>
        /// 현재 최대 ID + 1을 반환합니다.
        /// </summary>
        private int GetNextId(Type dataType)
        {
            int maxId = 0;
            List<SWIdentifiedObject> assets = assetsByType[dataType];

            for (int index = 0; index < assets.Count; index++)
            {
                if (assets[index] != null && assets[index].ID > maxId)
                    maxId = assets[index].ID;
            }

            return maxId + 1;
        }

        /// <summary>
        /// 확인 다이얼로그 후 에셋을 삭제합니다.
        /// </summary>
        private void DeleteAsset(Type dataType, SWIdentifiedObject data)
        {
            if (data == null) return;

            string assetPath = AssetDatabase.GetAssetPath(data);
            if (!EditorUtility.DisplayDialog("에셋 삭제",
                $"'{data.CodeName}' 을(를) 삭제할까요?\n{assetPath}\n\n이 작업은 되돌릴 수 없습니다.", "삭제", "취소"))
            {
                return;
            }

            if (selectedObjectsByType[dataType] == data)
                selectedObjectsByType[dataType] = null;

            AssetDatabase.DeleteAsset(assetPath);

            if (autoSaveAssets)
                AssetDatabase.SaveAssets();

            RefreshAssets(dataType);
            SWLog.Log($"[SWStatSystemWindow] 삭제 완료: {assetPath}");
        }

        /// <summary>
        /// 선택한 에셋의 객체 이름과 파일 이름을 함께 변경합니다.
        /// </summary>
        private void RenameAsset(Type dataType, SWIdentifiedObject data, string newName)
        {
            if (data == null)
            {
                return;
            }

            newName = string.IsNullOrWhiteSpace(newName) ? data.name : newName.Trim();
            if (newName == data.name)
            {
                return;
            }

            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.Contains("/") || newName.Contains("\\"))
            {
                EditorUtility.DisplayDialog("이름 변경 실패", "파일 이름으로 사용할 수 없는 문자가 포함되어 있습니다.", "확인");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(data);
            Undo.RecordObject(data, "Rename Identified Object");

            string error = AssetDatabase.RenameAsset(assetPath, newName);
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("이름 변경 실패", error, "확인");
                return;
            }

            data.name = newName;
            EditorUtility.SetDirty(data);

            if (autoSaveAssets)
                AssetDatabase.SaveAssets();

            RefreshAssets(dataType);
            selectedObjectsByType[dataType] = data;
            SWLog.Log($"[SWStatSystemWindow] 이름 변경 완료: {assetPath} -> {newName}");
        }

        /// <summary>
        /// 목록에 표시할 이름을 설정에 따라 반환합니다.
        /// </summary>
        private string GetListLabel(SWIdentifiedObject data)
        {
            return labelMode switch
            {
                1 => data.DisplayName,
                2 => data.name,
                _ => string.IsNullOrEmpty(data.CodeName) ? data.name : data.CodeName,
            };
        }
        #endregion // 에셋 관리

        #region 경로 유틸
        /// <summary>
        /// 경로가 프로젝트 내부(Assets/) 경로인지 확인합니다.
        /// </summary>
        private static bool IsValidProjectPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && (path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal));
        }

        /// <summary>
        /// 폴더 선택 창을 열고 프로젝트 상대 경로로 변환해 반환합니다.
        /// </summary>
        private static string PickProjectFolder(string currentPath)
        {
            string startFolder = IsValidProjectPath(currentPath) && AssetDatabase.IsValidFolder(currentPath)
                ? currentPath
                : "Assets";

            string absolutePath = EditorUtility.OpenFolderPanel("에셋 생성 폴더 선택", startFolder, string.Empty);
            if (string.IsNullOrEmpty(absolutePath)) return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            absolutePath = absolutePath.Replace('\\', '/');

            if (string.IsNullOrEmpty(projectRoot) || !absolutePath.StartsWith(projectRoot, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("경로 오류", "프로젝트 내부(Assets 하위) 폴더만 선택할 수 있습니다.", "확인");
                return null;
            }

            return absolutePath.Substring(projectRoot.Length + 1);
        }

        /// <summary>
        /// 폴더가 없으면 상위부터 차례대로 생성합니다.
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0]; // "Assets"

            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = $"{currentPath}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[index]);

                currentPath = nextPath;
            }
        }
        #endregion // 경로 유틸
    }
}
