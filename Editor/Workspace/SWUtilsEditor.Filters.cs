using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using SW.EditorTools.Workspace;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        /// <summary>
        /// 유형 필터를 소속과 설정 분류로 묶고 마지막 검색 및 펼침 상태를 복원합니다.
        /// </summary>
        private void ShowFilterPicker(Rect anchor)
        {
            ShowPopup(anchor, new Vector2(420, 560), (root, close) =>
            {
                SWEditorViewState state = settings.GetViewState("types.filter");
                PopupField<string> sort = new("Sort", new List<string> { "Type, then name", "Name A-Z" }, settings.SortByName ? 1 : 0);
                sort.RegisterValueChangedCallback(eventData =>
                {
                    settings.SortByName = eventData.newValue == "Name A-Z";
                    settings.Persist();
                    RefreshBrowser();
                });
                root.Add(sort);
                TextField search = new() { value = state.SearchText };
                search.AddToClassList("sw-picker-search");
                search.textEdition.placeholder = "Search types or categories...";
                root.Add(search);
                Label summary = new();
                summary.AddToClassList("sw-filter-summary");
                root.Add(summary);
                ScrollView list = new(ScrollViewMode.Vertical);
                root.Add(list);
                SWEditorScrollKeeper keeper = new(list, () => state.ScrollPosition, value => state.ScrollPosition = value);

                void RefreshSummary()
                {
                    summary.text = settings.FilteredTypes.Count == 0 ? "All enabled types" : settings.FilteredTypes.Count + " types selected";
                    summary.tooltip = "유형을 선택하면 선택한 유형만 표시합니다. 모두 해제하면 전체 유형을 표시합니다.";
                }

                void Rebuild(bool resetPosition = false)
                {
                    keeper.Rebuild(() =>
                    {
                        list.Clear();
                        bool searching = !string.IsNullOrWhiteSpace(state.SearchText);
                        foreach (IGrouping<bool, SWEditorAssetType> source in catalog.Types.Where(type => type.Settings.Enabled).GroupBy(type => type.IsSWUtils).OrderByDescending(group => group.Key))
                        {
                            string sourceIdentifier = source.Key ? "source.swutils" : "source.other";
                            string sourceName = source.Key ? "SWUtils" : "Other assets";
                            Foldout sourceSection = CreateFilterFoldout(sourceName, sourceIdentifier, "sw-picker-source", true);
                            int sourceCount = 0;
                            foreach (IGrouping<string, SWEditorAssetType> group in source.GroupBy(type => settings.HasCategory(type.Settings.CategoryIdentifier) ? type.Settings.CategoryIdentifier : SWEditorWorkspaceSettings.UncategorizedCategory).OrderBy(group => GetCategoryOrder(group.Key)))
                            {
                                string categoryName = GetCategoryName(group.Key);
                                SWEditorAssetType[] matching = group.Where(type => !searching || (sourceName + " " + categoryName + " " + type.Type.FullName + " " + type.DisplayName).IndexOf(state.SearchText, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                                if (matching.Length == 0)
                                {
                                    continue;
                                }

                                Foldout categorySection = CreateFilterFoldout(categoryName + "  (" + matching.Length + ")", sourceIdentifier + "/" + group.Key, "sw-picker-group", group.Key == settings.SelectedCategory);
                                Toggle selection = new() { text = "Select group" };
                                selection.AddToClassList("sw-filter-group-toggle");
                                UpdateGroupSelection();
                                selection.RegisterValueChangedCallback(eventData =>
                                {
                                    foreach (SWEditorAssetType type in matching)
                                    {
                                        settings.FilteredTypes.Remove(type.Settings.TypeName);
                                        if (eventData.newValue)
                                        {
                                            settings.FilteredTypes.Add(type.Settings.TypeName);
                                        }
                                    }

                                    settings.Persist();
                                    Rebuild();
                                    RefreshBrowser();
                                });
                                categorySection.Add(selection);
                                foreach (SWEditorAssetType type in matching)
                                {
                                    Toggle toggle = new()
                                    {
                                        text = type.DisplayName + "  (" + type.AssetCount + ")",
                                        value = settings.FilteredTypes.Contains(type.Settings.TypeName),
                                        tooltip = type.Type.FullName
                                    };
                                    toggle.AddToClassList("sw-filter-type-toggle");
                                    toggle.RegisterValueChangedCallback(eventData =>
                                    {
                                        settings.FilteredTypes.Remove(type.Settings.TypeName);
                                        if (eventData.newValue)
                                        {
                                            settings.FilteredTypes.Add(type.Settings.TypeName);
                                        }

                                        settings.Persist();
                                        UpdateGroupSelection();
                                        RefreshSummary();
                                        RefreshBrowser();
                                    });
                                    categorySection.Add(toggle);
                                }

                                void UpdateGroupSelection()
                                {
                                    int count = matching.Count(type => settings.FilteredTypes.Contains(type.Settings.TypeName));
                                    selection.SetValueWithoutNotify(count == matching.Length);
                                    selection.showMixedValue = count > 0 && count < matching.Length;
                                }

                                sourceSection.Add(categorySection);
                                sourceCount += matching.Length;
                            }

                            if (sourceCount > 0)
                            {
                                sourceSection.text = sourceName + "  (" + sourceCount + ")";
                                list.Add(sourceSection);
                            }
                        }

                        if (list.childCount == 0)
                        {
                            list.Add(new Label("No matching enabled types."));
                        }

                        RefreshSummary();
                    }, resetPosition);
                }

                Foldout CreateFilterFoldout(string title, string identifier, string className, bool defaultExpanded)
                {
                    bool searching = !string.IsNullOrWhiteSpace(state.SearchText);
                    Foldout foldout = new() { text = title, value = searching || state.IsExpanded(identifier, defaultExpanded) };
                    foldout.AddToClassList(className);
                    foldout.RegisterValueChangedCallback(eventData =>
                    {
                        if (eventData.target == foldout && !searching)
                        {
                            state.SetExpanded(identifier, eventData.newValue);
                            settings.Persist();
                        }
                    });
                    return foldout;
                }

                search.RegisterValueChangedCallback(eventData =>
                {
                    state.SearchText = eventData.newValue;
                    settings.Persist();
                    Rebuild(true);
                });
                VisualElement footer = Element("sw-picker-footer");
                footer.Add(new Button(() =>
                {
                    settings.FilteredTypes.Clear();
                    settings.Persist();
                    Rebuild();
                    RefreshBrowser();
                }) { text = "Clear type filters" });
                footer.Add(new Button(close) { text = "Done" });
                root.Add(footer);
                Rebuild();
            });
        }

        private int GetCategoryOrder(string identifier)
        {
            int index = settings.Categories.FindIndex(category => category.Identifier == identifier);
            return index < 0 ? int.MaxValue : index;
        }
    }
}
