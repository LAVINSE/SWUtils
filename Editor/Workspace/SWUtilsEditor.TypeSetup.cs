using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using SW.EditorTools.Workspace;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        private Action refreshTypeSetup;

        /// <summary>
        /// 저장한 검색과 펼침 상태를 복원하여 유형 설정을 표시합니다.
        /// </summary>
        private void BuildTypeSetup()
        {
            configuringTypes = true;
            browserContent = null;
            inspectorHost = null;
            tabsHost = null;
            pendingLabel = null;
            rootVisualElement.RemoveFromClassList("sw-workspace");
            SWEditorViewState state = settings.GetViewState("types.setup");
            VisualElement setup = Element("sw-setup");
            rootVisualElement.Add(setup);
            Label heading = new("Configure asset types");
            heading.AddToClassList("sw-heading");
            setup.Add(heading);
            Label description = new("관리할 유형과 기본 분류를 선택하세요. 분류 추가·삭제는 Settings에서 합니다. 변경 사항은 자동 저장됩니다.");
            description.AddToClassList("sw-muted");
            description.AddToClassList("sw-wrap");
            setup.Add(description);
            VisualElement bar = Element("sw-setup-toolbar");
            TextField search = new() { value = state.SearchText };
            search.AddToClassList("sw-setup-search");
            search.textEdition.placeholder = "Search asset types...";
            bar.Add(search);
            ScrollView groups = new(ScrollViewMode.Vertical);
            groups.AddToClassList("sw-grow");
            SWEditorScrollKeeper keeper = new(groups, () => state.ScrollPosition, value => state.ScrollPosition = value);
            bar.Add(new Button(() =>
            {
                catalog.Refresh();
                RenderGroups();
            }) { text = "Rescan types" });
            setup.Add(bar);
            setup.Add(groups);
            search.RegisterValueChangedCallback(eventData =>
            {
                state.SearchText = eventData.newValue;
                settings.Persist();
                RenderGroups(true);
            });

            void RenderGroups(bool resetPosition = false)
            {
                keeper.Rebuild(() =>
                {
                    groups.Clear();
                    bool searching = !string.IsNullOrWhiteSpace(state.SearchText);
                    SWEditorAssetType[] matching = catalog.Types.Where(type => type.Policy?.ShowInOnboarding != false &&
                        (!searching || (type.Type.FullName + " " + type.Group + " " + GetCategoryName(type.Settings.CategoryIdentifier)).IndexOf(state.SearchText, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                    foreach (IGrouping<string, SWEditorAssetType> group in matching.GroupBy(type => type.Group))
                    {
                        Foldout section = new()
                        {
                            text = group.Key + "  (" + group.Count() + ")",
                            value = searching || state.IsExpanded(group.Key, group.Any(type => type.AssetCount > 0))
                        };
                        section.AddToClassList("sw-type-group");
                        section.RegisterValueChangedCallback(eventData =>
                        {
                            if (eventData.target == section && !searching)
                            {
                                state.SetExpanded(group.Key, eventData.newValue);
                                settings.Persist();
                            }
                        });
                        VisualElement header = Element("sw-type-row");
                        Toggle enabled = new()
                        {
                            text = "Select group",
                            value = group.All(type => type.Settings.Enabled),
                            showMixedValue = group.Any(type => type.Settings.Enabled) && !group.All(type => type.Settings.Enabled)
                        };
                        enabled.RegisterValueChangedCallback(eventData =>
                        {
                            foreach (SWEditorAssetType type in group)
                            {
                                type.Settings.Enabled = eventData.newValue;
                            }

                            settings.Persist();
                            RenderGroups();
                        });
                        header.Add(enabled);
                        string selectedCategory = group.Select(type => type.Settings.CategoryIdentifier).Distinct().Count() == 1 ? group.First().Settings.CategoryIdentifier : null;
                        header.Add(CategoryPicker(selectedCategory, identifier =>
                        {
                            foreach (SWEditorAssetType type in group)
                            {
                                type.Settings.CategoryIdentifier = identifier;
                            }

                            settings.Persist();
                            RenderGroups();
                        }, "Mixed / unassigned"));
                        section.Add(header);
                        foreach (SWEditorAssetType type in group)
                        {
                            VisualElement row = Element("sw-type-row");
                            Toggle choice = new()
                            {
                                text = type.DisplayName,
                                value = type.Settings.Enabled,
                                tooltip = type.Type.FullName
                            };
                            choice.RegisterValueChangedCallback(eventData =>
                            {
                                type.Settings.Enabled = eventData.newValue;
                                enabled.SetValueWithoutNotify(group.All(item => item.Settings.Enabled));
                                enabled.showMixedValue = group.Any(item => item.Settings.Enabled) && !group.All(item => item.Settings.Enabled);
                                settings.Persist();
                            });
                            row.Add(choice);
                            Label count = new(type.AssetCount + " assets");
                            count.AddToClassList("sw-type-count");
                            row.Add(count);
                            row.Add(CategoryPicker(type.Settings.CategoryIdentifier, identifier =>
                            {
                                type.Settings.CategoryIdentifier = identifier;
                                settings.Persist();
                            }));
                            section.Add(row);
                        }

                        groups.Add(section);
                    }

                    if (matching.Length == 0)
                    {
                        groups.Add(new Label("No matching asset types."));
                    }
                }, resetPosition);
            }

            refreshTypeSetup = () => RenderGroups();
            RenderGroups();
            VisualElement footer = Element("sw-setup-footer");
            footer.Add(new Button(() => FinishTypeSetup(true)) { text = "Back to settings" });
            Button done = new(() => FinishTypeSetup(false))
            {
                text = settings.IsConfigured ? "Done" : "Start browsing"
            };
            done.AddToClassList("sw-primary");
            done.AddToClassList("sw-action");
            footer.Add(done);
            setup.Add(footer);
        }

        private void FinishTypeSetup(bool returnToSettings)
        {
            settings.IsConfigured = true;
            settings.Persist();
            configuringTypes = false;
            refreshTypeSetup = null;
            showingSettings = returnToSettings;
            CreateGUI();
        }

        /// <summary>
        /// 설정 목록에 있는 분류만 선택지로 제공하며 미지정 값은 안내 문구로 표시합니다.
        /// </summary>
        private PopupField<string> CategoryPicker(string selected, Action<string> changed, string emptyLabel = "Unassigned")
        {
            List<string> identifiers = settings.Categories.Select(item => item.Identifier).ToList();
            PopupField<string> field = new()
            {
                choices = identifiers,
                formatSelectedValueCallback = identifier => settings.HasCategory(identifier) ? GetCategoryName(identifier) : emptyLabel,
                formatListItemCallback = GetCategoryName
            };
            field.SetValueWithoutNotify(settings.HasCategory(selected) ? selected : null);
            field.SetEnabled(identifiers.Count > 0);
            field.tooltip = identifiers.Count == 0 ? "Settings에서 카테고리를 먼저 추가하세요." : "Settings에 등록된 카테고리";
            field.RegisterValueChangedCallback(eventData => changed(eventData.newValue));
            return field;
        }

        private string GetCategoryName(string identifier)
        {
            return settings.Categories.Find(item => item.Identifier == identifier)?.DisplayName ?? "Unassigned";
        }
    }
}
