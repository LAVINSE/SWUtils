using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SW.EditorTools.Workspace;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        private void BuildSettings(VisualElement parent)
        {
            VisualElement layout = Section(parent, "Layout");
            PopupField<string> view = new(new List<string> { "Grid", "List" }, settings.UseListView ? 1 : 0);
            view.RegisterValueChangedCallback(eventData =>
            {
                settings.UseListView = eventData.newValue == "List";
                settings.Persist();
                RefreshBrowser();
            });
            SettingRow(layout, "Asset layout", view);
            SliderInt size = new(72, 156)
            {
                value = settings.CellSize,
                showInputField = true
            };
            size.AddToClassList("sw-cell-size");
            size.RegisterValueChangedCallback(eventData =>
            {
                settings.CellSize = Mathf.Clamp(eventData.newValue, 72, 156);
                size.SetValueWithoutNotify(settings.CellSize);
                settings.Persist();
                RefreshBrowser();
            });
            SettingRow(layout, "Cell size", size);
            Toggle showTabs = new()
            {
                value = settings.ShowOpenTabs
            };
            showTabs.RegisterValueChangedCallback(eventData =>
            {
                settings.ShowOpenTabs = eventData.newValue;
                settings.Persist();
                RefreshTabs();
            });
            SettingRow(layout, "Show open tabs", showTabs);
            layout.Add(new Button(() =>
            {
                settings.ResetLayout();
                CreateGUI();
                showingSettings = true;
                RefreshTabs();
                RefreshInspector();
            }) { text = "Reset layout" });
            VisualElement browser = Section(parent, "Asset browser");
            BuildFolderSettings(browser);
            BuildFolderSettings(browser, true);

            VisualElement categories = Section(parent, "Categories");
            VisualElement addRow = Element("sw-row");
            TextField newName = new();
            newName.AddToClassList("sw-grow");
            newName.tooltip = "New category";
            addRow.Add(newName);
            addRow.Add(new Button(() =>
            {
                if (string.IsNullOrWhiteSpace(newName.value))
                    return;
                settings.Categories.Add(new SWEditorCategorySettings { DisplayName = newName.value.Trim() });
                settings.Persist();
                RefreshCategories();
                RefreshInspector();
            }) { text = "Add" });
            categories.Add(addRow);
            foreach (SWEditorCategorySettings category in settings.Categories.ToArray())
            {
                VisualElement row = Element("sw-settings-category-row");
                Label handle = new("↕")
                {
                    tooltip = "Drag to reorder"
                };
                handle.AddToClassList("sw-drag-handle");
                row.Add(handle);
                handle.AddManipulator(new SWEditorAssetPointerManipulator(eventData =>
                {
                }, () =>
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("SWUtilsEditor.Category", category.Identifier);
                    DragAndDrop.StartDrag(category.DisplayName);
                }));
                row.RegisterCallback<DragUpdatedEvent>(eventData =>
                {
                    if (DragAndDrop.GetGenericData("SWUtilsEditor.Category")is not string)
                        return;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    row.AddToClassList("sw-drop-target");
                    eventData.StopPropagation();
                });
                row.RegisterCallback<DragLeaveEvent>(eventData => row.RemoveFromClassList("sw-drop-target"));
                row.RegisterCallback<DragPerformEvent>(eventData =>
                {
                    if (DragAndDrop.GetGenericData("SWUtilsEditor.Category")is not string identifier)
                        return;
                    SWEditorCategorySettings moving = settings.Categories.Find(item => item.Identifier == identifier);
                    if (moving == null || moving == category)
                        return;
                    int targetIndex = settings.Categories.IndexOf(category);
                    settings.Categories.Remove(moving);
                    settings.Categories.Insert(targetIndex, moving);
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.SetGenericData("SWUtilsEditor.Category", null);
                    settings.Persist();
                    RefreshCategories();
                    RefreshInspector();
                    eventData.StopPropagation();
                });
                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(category.IconIdentifier));
                if (icon != null)
                {
                    Image image = new()
                    {
                        image = icon
                    };
                    image.style.width = image.style.height = 22;
                    row.Add(image);
                }

                Label label = new(category.DisplayName);
                label.AddToClassList("sw-grow");
                row.Add(label);
                row.Add(new Button(() => EditCategory(category, row.worldBound)) { text = "Edit" });
                row.Add(new Button(() =>
                {
                    if (!EditorUtility.DisplayDialog("Remove category", $"'{category.DisplayName}' 분류를 제거할까요?\n소속 에셋은 미지정 상태가 되며 All assets에서 계속 볼 수 있습니다.", "Remove", "Cancel"))
                        return;
                    settings.RemoveCategory(category.Identifier);
                    RefreshCategories();
                    RefreshInspector();
                    RefreshBrowser();
                }) { text = "Remove" });
                categories.Add(row);
            }

            VisualElement maintenance = Section(parent, "Maintenance");
            maintenance.Add(new Button(() =>
            {
                DisposeInspector();
                rootVisualElement.Clear();
                BuildTypeSetup();
            }) { text = "Configure asset types" });
            Button reset = new(() =>
            {
                if (!EditorUtility.DisplayDialog("Reset " + DisplayName, "분류, 유형 선택, 즐겨찾기와 저장된 배치를 초기화할까요?\n프로젝트의 실제 에셋은 유지됩니다.", "Reset", "Cancel"))
                    return;
                settings.ResetWorkspace();
                showingSettings = false;
                CreateGUI();
            })
            {
                text = "Reset " + DisplayName
            };
            reset.AddToClassList("sw-danger");
            maintenance.Add(reset);
        }

        private void EditCategory(SWEditorCategorySettings category, Rect anchor)
        {
            ShowPopup(anchor, new Vector2(360, 180), (root, close) =>
            {
                Label heading = new("Edit category");
                heading.AddToClassList("sw-section-title");
                root.Add(heading);
                TextField name = new("Name")
                {
                    value = category.DisplayName
                };
                root.Add(name);
                ObjectField icon = new("Icon")
                {
                    objectType = typeof(Texture2D),
                    allowSceneObjects = false,
                    value = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(category.IconIdentifier))
                };
                root.Add(icon);
                root.Add(new Button(() =>
                {
                    if (string.IsNullOrWhiteSpace(name.value))
                        return;
                    category.DisplayName = name.value.Trim();
                    category.IconIdentifier = icon.value == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(icon.value));
                    settings.Persist();
                    close();
                    RefreshCategories();
                    RefreshInspector();
                }) { text = "Save" });
            });
        }

        private void ShowCreatePicker(Rect anchor)
        {
            if (SWEditorAssetCreation.IsPending)
                return;
            ShowPopup(anchor, new Vector2(420, 520), (root, close) =>
            {
                Label heading = new("Create asset");
                heading.AddToClassList("sw-section-title");
                root.Add(heading);
                SWEditorViewState state = settings.GetViewState("types.create");
                TextField search = new() { value = state.SearchText };
                search.AddToClassList("sw-picker-search");
                search.textEdition.placeholder = "Search categories or asset types...";
                root.Add(search);
                ScrollView list = new(ScrollViewMode.Vertical);
                root.Add(list);
                SWEditorScrollKeeper keeper = new(list, () => state.ScrollPosition, value => state.ScrollPosition = value);
                SWEditorCategory[] categories = GetCategories().Where(category => category.Identifier != SWEditorWorkspaceSettings.AllCategory && category.Identifier != SWEditorWorkspaceSettings.FavouriteCategory).Append(new SWEditorCategory(SWEditorWorkspaceSettings.UncategorizedCategory, "Unassigned")).ToArray();
                void Rebuild(bool resetPosition = false)
                {
                    keeper.Rebuild(Populate, resetPosition);
                }

                void Populate()
                {
                    list.Clear();
                    string[] searchTerms = (search.value ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    bool searching = searchTerms.Length > 0;
                    var sources = catalog.Types.Where(type => type.Settings.Enabled && type.Settings.Classification != SWEditorTypeClassification.Other).GroupBy(type => type.IsSWUtils).OrderByDescending(source => source.Key);
                    foreach (var source in sources)
                    {
                        string sourceIdentifier = source.Key ? "source.swutils" : "source.other";
                        string sourceName = source.Key ? "SWUtils" : "Other assets";
                        bool sourceExpanded = state.IsExpanded(sourceIdentifier, source.Key || source.Any(type => type.Settings.CategoryIdentifier == settings.SelectedCategory));
                        Foldout sourceSection = new()
                        {
                            value = searching || sourceExpanded
                        };
                        sourceSection.AddToClassList("sw-picker-source");
                        sourceSection.RegisterValueChangedCallback(eventData =>
                        {
                            if (eventData.target == sourceSection && !searching)
                            {
                                state.SetExpanded(sourceIdentifier, eventData.newValue);
                                settings.Persist();
                            }
                        });
                        int sourceCount = 0;
                        var groups = source.GroupBy(type => categories.Any(category => category.Identifier == type.Settings.CategoryIdentifier) ? type.Settings.CategoryIdentifier : SWEditorWorkspaceSettings.UncategorizedCategory).OrderByDescending(group => group.Key == settings.SelectedCategory).ThenBy(group => Array.FindIndex(categories, category => category.Identifier == group.Key));
                        foreach (var group in groups)
                        {
                            SWEditorCategory category = categories.First(item => item.Identifier == group.Key);
                            SWEditorAssetType[] matchingTypes = group.Where(type => searchTerms.All(term => (sourceName + " " + category.DisplayName + " " + type.Group + " " + type.Type.FullName + " " + type.DisplayName + " " + type.CreationMenu).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(type =>
                            {
                                int index = settings.RecentTypes.IndexOf(type.Settings.TypeName);
                                return index < 0 ? int.MaxValue : index;
                            }).ThenByDescending(type => type.Policy?.CreationPriority ?? 0).ThenBy(type => type.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
                            if (matchingTypes.Length == 0)
                                continue;
                            string categoryIdentifier = sourceIdentifier + "/" + group.Key;
                            bool expanded = state.IsExpanded(categoryIdentifier, group.Key == settings.SelectedCategory);
                            Foldout section = new()
                            {
                                text = category.DisplayName + "  (" + matchingTypes.Length + ")",
                                value = searching || expanded
                            };
                            section.AddToClassList("sw-picker-group");
                            section.RegisterValueChangedCallback(eventData =>
                            {
                                if (eventData.target == section && !searching)
                                {
                                    state.SetExpanded(categoryIdentifier, eventData.newValue);
                                    settings.Persist();
                                }
                            });
                            foreach (SWEditorAssetType type in matchingTypes)
                                section.Add(CreateTypeButton(type, close));
                            sourceSection.Add(section);
                            sourceCount += matchingTypes.Length;
                        }

                        if (sourceCount == 0)
                            continue;
                        sourceSection.text = sourceName + "  (" + sourceCount + ")";
                        list.Add(sourceSection);
                    }

                    if (list.childCount == 0)
                        list.Add(new Label("No matching types. Enable types in Settings."));
                }

                search.RegisterValueChangedCallback(eventData =>
                {
                    state.SearchText = eventData.newValue;
                    settings.Persist();
                    Rebuild(true);
                });
                Rebuild();
                search.schedule.Execute(search.Focus);
            });
        }

        /// <summary>분류 안에 표시할 유형 이름과 최근 생성 표시를 가진 생성 버튼입니다.</summary>
        private Button CreateTypeButton(SWEditorAssetType type, Action close)
        {
            Button button = new(() =>
            {
                close();
                EditorApplication.delayCall += () => SWEditorAssetCreation.Create(type, OpenCreated);
            });
            button.AddToClassList("sw-picker-option");
            button.tooltip = type.Type.FullName + "\n" + type.CreationMenu;
            Label name = new(type.DisplayName);
            name.AddToClassList("sw-picker-option-name");
            button.Add(name);
            Label count = new(settings.RecentTypes.FirstOrDefault() == type.Settings.TypeName ? "Last created" : type.AssetCountLabel);
            count.AddToClassList("sw-picker-option-count");
            button.Add(count);
            return button;
        }

        private void ShowPopup(Rect anchor, Vector2 size, Action<VisualElement, Action> build)
        {
            SWEditorPopupWindow popup = CreateInstance<SWEditorPopupWindow>();
            popup.Build = build;
            popup.ShowAsDropDown(new Rect(position.x + anchor.x, position.y + anchor.yMax, Math.Min(anchor.width, size.x), 1), size);
        }

        private static VisualElement Section(VisualElement parent, string title)
        {
            VisualElement section = Element("sw-section");
            Label heading = new(title);
            heading.AddToClassList("sw-section-title");
            section.Add(heading);
            parent.Add(section);
            return section;
        }

        private static void SettingRow(VisualElement parent, string name, VisualElement field)
        {
            VisualElement row = Element("sw-setting-row");
            row.Add(new Label(name));
            row.Add(field);
            parent.Add(row);
        }
    }

    /// <summary>작업 공간과 같은 테마를 사용하는 검색 및 편집 팝업입니다.</summary>
    internal sealed class SWEditorPopupWindow : EditorWindow
    {
        internal Action<VisualElement, Action> Build;
        private void OnDisable()
        {
            SWEditorWorkspaceSettings.instance.Persist();
        }

        /// <summary>팝업 내용을 생성합니다.</summary>
        public void CreateGUI()
        {
            SWEditorTheme.Apply(rootVisualElement);
            StyleSheet style = Util.SWEditorUtils.LoadStyleSheetByIdentifier("cb734bb9b20f06f48bb59be35351f534");
            if (style != null)
                rootVisualElement.styleSheets.Add(style);
            rootVisualElement.AddToClassList("sw-picker");
            Build?.Invoke(rootVisualElement, Close);
            rootVisualElement.RegisterCallback<KeyDownEvent>(eventData =>
            {
                if (eventData.keyCode == KeyCode.Escape)
                    Close();
            });
        }
    }
}
