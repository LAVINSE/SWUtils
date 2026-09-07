using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SW.Base;

using SW.EditorTools.Util;

using SW.Quest;

namespace SW.EditorTools.Window
{
    public sealed partial class SWQuestSystemWindow
    {
        #region 화면 상태
        private static readonly string[] NavigationNames = { "퀘스트", "업적", "구성 요소", "데이터베이스", "설정" };
        private static readonly string[] ComponentNames = { "작업", "대상", "조건", "보상", "진행 계산", "시작 진행값" };
        private static readonly string[] DatabaseNames = { "퀘스트", "업적" };

        private int componentIndex;
        private int databaseIndex;
        private int settingsKindIndex;
        private GUIStyle assetTitleStyle;
        private GUIStyle assetSubtitleStyle;
        private GUIStyle inspectorTitleStyle;
        private GUIStyle contentPaddingStyle;
        private GUIStyle emptyNoticeStyle;
        private bool stylesUseDarkTheme;
        #endregion // 화면 상태

        #region 화면
        private void OnGUI()
        {
            PrepareStyles();
            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
            DrawNavigation();

            if (navigationIndex == NavigationNames.Length - 1)
            {
                DrawSettings();
                return;
            }

            ManagedAssetKind kind = navigationIndex switch
            {
                0 => ManagedAssetKind.Quest,
                1 => ManagedAssetKind.Achievement,
                2 => (ManagedAssetKind)((int)ManagedAssetKind.Task + componentIndex),
                _ => (ManagedAssetKind)((int)ManagedAssetKind.QuestDatabase + databaseIndex)
            };
            DrawAssetManagement(kind);
        }

        /// <summary>
        /// 상위 분류와 현재 분류에 필요한 하위 탭만 표시합니다.
        /// </summary>
        private void DrawNavigation()
        {
            EditorGUI.BeginChangeCheck();
            navigationIndex = SWEditorUtils.DrawTabBar(navigationIndex, NavigationNames, 30f);
            if (navigationIndex == 2 || navigationIndex == 3)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(12f);
                    if (navigationIndex == 2)
                    {
                        componentIndex = GUILayout.Toolbar(componentIndex, ComponentNames, EditorStyles.miniButton, GUILayout.Height(24f));
                    }
                    else
                    {
                        databaseIndex = GUILayout.Toolbar(databaseIndex, DatabaseNames, EditorStyles.miniButton, GUILayout.Height(24f));
                    }
                    GUILayout.Space(12f);
                }
                GUILayout.Space(10f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
                inspectorScrollPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 목록과 상세 편집 영역을 여백과 단일 구분선으로 나눕니다.
        /// </summary>
        private void DrawAssetManagement(ManagedAssetKind kind)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                float availableListWidth = Mathf.Clamp(listWidth, 260f, Mathf.Max(260f, position.width - 420f));
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(availableListWidth), GUILayout.ExpandHeight(true)))
                {
                    DrawAssetListPanel(kind);
                }

                Rect separator = GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(separator, SWEditorUtils.HeaderLineColor);

                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(360f), GUILayout.ExpandHeight(true)))
                {
                    DrawInspectorPanel(kind);
                }
            }
        }

        /// <summary>
        /// 생성, 검색과 정렬 도구를 목록 위에 간결하게 배치합니다.
        /// </summary>
        private void DrawAssetListPanel(ManagedAssetKind kind)
        {
            using (new EditorGUILayout.VerticalScope(contentPaddingStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{GetKindLabel(kind)}  {assetsByKind[kind].Count}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(creationTypesByKind[kind].Length == 0))
                    {
                        if (GUILayout.Button("+ 새로 만들기", EditorStyles.miniButton, GUILayout.Width(98f), GUILayout.Height(24f)))
                        {
                            ShowCreationMenu(kind);
                        }
                    }
                }

                GUILayout.Space(10f);
                searchTextsByKind[kind] = EditorGUILayout.TextField(
                    new GUIContent(string.Empty, "코드명, 표시명, 에셋 이름 또는 타입으로 검색"),
                    searchTextsByKind[kind], EditorStyles.toolbarSearchField);

                GUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    sortMode = (AssetSortMode)EditorGUILayout.Popup((int)sortMode, SortModeNames, GUILayout.Width(112f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("새로 고침", EditorStyles.miniButton, GUILayout.Width(72f)))
                    {
                        RefreshAssets(kind);
                    }
                }
            }

            listScrollPositionsByKind[kind] = EditorGUILayout.BeginScrollView(
                listScrollPositionsByKind[kind], false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            DrawAssetRows(kind);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 생성 가능한 파생 타입을 메뉴로 표시하고 선택한 타입을 생성합니다.
        /// </summary>
        private void ShowCreationMenu(ManagedAssetKind kind)
        {
            Type[] creationTypes = creationTypesByKind[kind];
            if (creationTypes.Length == 1)
            {
                creationTypeIndexesByKind[kind] = 0;
                CreateAsset(kind);
                searchTextsByKind[kind] = string.Empty;
                GUIUtility.ExitGUI();
                return;
            }

            GenericMenu menu = new();
            for (int index = 0; index < creationTypes.Length; index++)
            {
                int creationTypeIndex = index;
                Type creationType = creationTypes[index];
                menu.AddItem(new GUIContent(creationType.FullName ?? creationType.Name), false, () =>
                {
                    creationTypeIndexesByKind[kind] = creationTypeIndex;
                    CreateAsset(kind);
                    searchTextsByKind[kind] = string.Empty;
                    Repaint();
                });
            }
            menu.ShowAsContext();
        }

        /// <summary>
        /// 검색과 정렬을 반영해 아이콘과 두 줄 이름을 가진 넓은 선택 행을 그립니다.
        /// </summary>
        private void DrawAssetRows(ManagedAssetKind kind)
        {
            List<ScriptableObject> visibleAssets = new(assetsByKind[kind]);
            visibleAssets.RemoveAll(asset => asset == null || !MatchesSearch(asset, searchTextsByKind[kind]));
            visibleAssets.Sort(CompareAssets);

            for (int index = 0; index < visibleAssets.Count; index++)
            {
                ScriptableObject asset = visibleAssets[index];
                Rect row = GUILayoutUtility.GetRect(0f, 56f, GUILayout.ExpandWidth(true));
                bool selected = selectedAssetsByKind[kind] == asset;
                if (Event.current.type == EventType.Repaint)
                {
                    if (selected)
                    {
                        EditorGUI.DrawRect(row, EditorGUIUtility.isProSkin
                            ? new Color(0.24f, 0.34f, 0.44f) : new Color(0.70f, 0.81f, 0.91f));
                    }
                    else if (row.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.12f));
                    }
                }

                GUI.DrawTexture(new Rect(row.x + 12f, row.y + 12f, 32f, 32f), GetAssetIcon(asset), ScaleMode.ScaleToFit);
                Rect title = new(row.x + 54f, row.y + 9f, Mathf.Max(0f, row.width - 66f), 20f);
                Rect subtitle = new(title.x, row.y + 30f, title.width, 18f);
                GUI.Label(title, new GUIContent(GetAssetLabel(asset), AssetDatabase.GetAssetPath(asset)), assetTitleStyle);
                GUI.Label(subtitle, new GUIContent(GetAssetSubtitle(asset), asset.GetType().FullName), assetSubtitleStyle);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                    && row.Contains(Event.current.mousePosition))
                {
                    GUI.FocusControl(null);
                    SelectAsset(kind, asset);
                    Event.current.Use();
                    Repaint();
                }
            }

            if (visibleAssets.Count == 0)
            {
                GUILayout.Space(32f);
                GUILayout.Label(string.IsNullOrWhiteSpace(searchTextsByKind[kind])
                    ? $"아직 {GetKindLabel(kind)} 에셋이 없습니다.\n새로 만들기로 추가하세요."
                    : "검색 결과가 없습니다.\n다른 이름으로 검색하세요.", emptyNoticeStyle);
            }
        }

        /// <summary>
        /// 에셋 제목과 관리 도구 아래에 기존 상세 인스펙터를 표시합니다.
        /// </summary>
        private void DrawInspectorPanel(ManagedAssetKind kind)
        {
            ScriptableObject selectedAsset = selectedAssetsByKind[kind];
            if (selectedAsset == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("편집할 에셋을 선택하세요", inspectorTitleStyle);
                GUILayout.Space(8f);
                GUILayout.Label("왼쪽 목록에서 선택한 에셋의 세부 설정이 여기에 표시됩니다.", emptyNoticeStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(GetKindLabel(kind), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("위치 표시", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    SWEditorUtils.PingAndSelect(selectedAsset);
                }
                if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                {
                    DuplicateAsset(kind, selectedAsset);
                    searchTextsByKind[kind] = string.Empty;
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("삭제", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                {
                    DeleteAsset(kind, selectedAsset);
                    GUIUtility.ExitGUI();
                }
            }

            inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);
            using (new EditorGUILayout.VerticalScope(contentPaddingStyle))
            {
                GUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(GetAssetIcon(selectedAsset), GUILayout.Width(40f), GUILayout.Height(40f));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(GetAssetLabel(selectedAsset), inspectorTitleStyle);
                        GUILayout.Label(selectedAsset.GetType().Name, assetSubtitleStyle);
                    }
                }

                GUILayout.Space(14f);
                DrawRenameControls(kind, selectedAsset);
                DrawDatabaseControls(selectedAsset);
                SWEditorUtils.DrawSeparatorWithSpace(10f, 12f);

                Editor.CreateCachedEditor(selectedAsset, null, ref cachedEditor);
                if (cachedEditor != null)
                {
                    cachedEditor.OnInspectorGUI();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 선택 에셋의 파일 이름을 변경합니다.
        /// </summary>
        private void DrawRenameControls(ManagedAssetKind kind, ScriptableObject selectedAsset)
        {
            string requestedName = EditorGUILayout.DelayedTextField("에셋 이름", selectedAsset.name);
            if (string.Equals(requestedName, selectedAsset.name, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(requestedName))
            {
                return;
            }

            string errorMessage = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(selectedAsset), requestedName.Trim());
            if (string.IsNullOrEmpty(errorMessage))
            {
                RefreshAssets(kind, selectedAsset);
            }
            else
            {
                ShowNotification(new GUIContent(errorMessage));
            }
        }

        /// <summary>
        /// 데이터베이스 에셋에만 동기화와 검증 도구를 표시합니다.
        /// </summary>
        private void DrawDatabaseControls(ScriptableObject selectedAsset)
        {
            if (selectedAsset is not SWQuestDatabase && selectedAsset is not SWAchievementDatabase)
            {
                return;
            }

            GUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("프로젝트 정의 동기화", GUILayout.Height(24f)))
                {
                    SynchronizeDatabase(selectedAsset);
                }
                if (GUILayout.Button("구성 검증", GUILayout.Height(24f)))
                {
                    ValidateDatabase(selectedAsset);
                }
            }
        }

        /// <summary>
        /// 공통 설정과 선택한 분류의 생성 규칙을 간결한 양식으로 표시합니다.
        /// </summary>
        private void DrawSettings()
        {
            settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);
            using (new EditorGUILayout.VerticalScope(contentPaddingStyle))
            {
                SWEditorUtils.DrawHeader("목록과 저장");
                listWidth = EditorGUILayout.Slider("목록 너비", listWidth, 260f, 520f);
                saveAssetsAutomatically = EditorGUILayout.Toggle("변경 후 자동 저장", saveAssetsAutomatically);

                GUILayout.Space(24f);
                SWEditorUtils.DrawHeader("에셋 생성 규칙");
                settingsKindIndex = EditorGUILayout.Popup("분류", settingsKindIndex, AssetKindNames);
                GUILayout.Space(8f);
                createPaths[settingsKindIndex] = EditorGUILayout.TextField("생성 경로", createPaths[settingsKindIndex]);
                namePrefixes[settingsKindIndex] = EditorGUILayout.TextField("이름 접두사", namePrefixes[settingsKindIndex]);
                GUILayout.Space(6f);
                EditorGUILayout.LabelField("분류를 선택해 각 에셋의 저장 폴더와 이름 접두사를 설정하세요.", EditorStyles.wordWrappedMiniLabel);

                GUILayout.Space(24f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("설정 저장", GUILayout.Width(120f), GUILayout.Height(28f)))
                    {
                        SaveSettings();
                        ShowNotification(new GUIContent("퀘스트 편집기 설정을 저장했습니다."));
                    }
                    if (GUILayout.Button("기본값 복원", GUILayout.Width(120f), GUILayout.Height(28f)))
                    {
                        ResetSettings();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion // 화면

        #region 화면 스타일
        /// <summary>
        /// 밝은 테마와 어두운 테마에 맞춰 텍스처 생성 없이 스타일을 준비합니다.
        /// </summary>
        private void PrepareStyles()
        {
            if (assetTitleStyle != null && stylesUseDarkTheme == EditorGUIUtility.isProSkin)
            {
                return;
            }

            stylesUseDarkTheme = EditorGUIUtility.isProSkin;
            wantsMouseMove = true;
            assetTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                clipping = TextClipping.Clip,
                padding = new RectOffset(),
                margin = new RectOffset()
            };
            assetSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                clipping = TextClipping.Clip,
                padding = new RectOffset(),
                margin = new RectOffset()
            };
            assetSubtitleStyle.normal.textColor = stylesUseDarkTheme
                ? new Color(0.73f, 0.73f, 0.73f) : new Color(0.30f, 0.30f, 0.30f);
            inspectorTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                wordWrap = true,
                padding = new RectOffset(0, 0, 2, 2)
            };
            contentPaddingStyle = new GUIStyle { padding = new RectOffset(12, 12, 10, 12) };
            emptyNoticeStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true,
                fontSize = 12,
                padding = new RectOffset(16, 16, 0, 0)
            };
        }

        /// <summary>
        /// 코드명과 중복되지 않는 표시명 또는 타입을 보조 정보로 반환합니다.
        /// </summary>
        private static string GetAssetSubtitle(ScriptableObject asset)
        {
            string displayName = asset is SWIdentifiedObject identifiedAsset ? identifiedAsset.DisplayName : asset.name;
            return string.Equals(displayName, GetAssetLabel(asset), StringComparison.Ordinal)
                ? asset.GetType().Name : displayName;
        }

        /// <summary>
        /// 사용자 지정 아이콘이 있으면 우선 표시하고 에셋 아이콘으로 대체합니다.
        /// </summary>
        private static Texture GetAssetIcon(ScriptableObject asset)
        {
            if (asset is SWIdentifiedObject identifiedAsset && identifiedAsset.SpriteIcon != null)
            {
                Texture preview = AssetPreview.GetAssetPreview(identifiedAsset.SpriteIcon);
                if (preview != null)
                {
                    return preview;
                }
                return AssetPreview.GetMiniThumbnail(identifiedAsset.SpriteIcon);
            }
            return AssetPreview.GetMiniThumbnail(asset);
        }
        #endregion // 화면 스타일
    }
}
