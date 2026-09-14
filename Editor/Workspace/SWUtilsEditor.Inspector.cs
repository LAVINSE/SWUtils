using System;
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
        private void RefreshTabs()
        {
            if (tabsHost == null)
                return;
            tabsHost.Clear();
            if ((!settings.ShowOpenTabs || settings.OpenAssets.Count == 0) && !showingSettings)
            {
                tabsHost.AddToClassList("sw-hidden");
                return;
            }

            tabsHost.RemoveFromClassList("sw-hidden");
            ScrollView scroll = new(ScrollViewMode.Horizontal);
            scroll.AddToClassList("sw-tabs-scroll");
            tabsHost.Add(scroll);
            if (settings.ShowOpenTabs)
                foreach (string identifier in settings.OpenAssets.ToArray())
                {
                    SWEditorAssetEntry entry = catalog.Find(identifier);
                    if (entry == null)
                        continue;
                    VisualElement tab = Element("sw-asset-tab");
                    tab.EnableInClassList("sw-selected", !showingSettings && identifier == settings.ActiveAsset);
                    tab.tooltip = entry.Path;
                    tab.Add(AssetImage(entry));
                    tab.Add(new Label(entry.Asset.name));
                    Button close = new(() => CloseAsset(identifier))
                    {
                        text = "×",
                        tooltip = "Close tab"
                    };
                    close.AddToClassList("sw-tab-close");
                    tab.Add(close);
                    tab.RegisterCallback<ClickEvent>(eventData =>
                    {
                        if (eventData.target is Button || (eventData.target as VisualElement)?.GetFirstAncestorOfType<Button>() != null)
                            return;
                        settings.ActiveAsset = identifier;
                        showingSettings = false;
                        inspectorTab = "Inspector";
                        if (eventData.clickCount == 2)
                        {
                            string categoryIdentifier = catalog.Context(entry).CategoryIdentifier;
                            settings.SelectedCategory = settings.HasCategory(categoryIdentifier) ? categoryIdentifier : SWEditorWorkspaceSettings.AllCategory;
                            settings.SearchText = "";
                            settings.FilteredTypes.Clear();
                            BuildToolbar();
                            RefreshCategories();
                            RefreshBrowser();
                            browserScroll.schedule.Execute(() =>
                            {
                                if (assetElements.TryGetValue(identifier, out VisualElement item))
                                    browserScroll.ScrollTo(item);
                            });
                        }

                        settings.Persist();
                        RefreshTabs();
                        RefreshBrowserSelection();
                        RefreshInspector();
                    });
                    scroll.Add(tab);
                    if (identifier == settings.ActiveAsset && !showingSettings)
                        scroll.schedule.Execute(() => scroll.ScrollTo(tab));
                }

            if (showingSettings)
            {
                VisualElement tab = Element("sw-asset-tab", "sw-selected");
                tab.Add(new Label("⚙  Settings"));
                Button close = new(() =>
                {
                    showingSettings = false;
                    RefreshTabs();
                    RefreshInspector();
                })
                {
                    text = "×"
                };
                close.AddToClassList("sw-tab-close");
                tab.Add(close);
                scroll.Add(tab);
                scroll.schedule.Execute(() => scroll.ScrollTo(tab));
            }
        }

        private void RefreshInspector()
        {
            if (inspectorHost == null)
                return;
            DisposeInspector();
            inspectorHost.Clear();
            inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.AddToClassList("sw-inspector-scroll");
            inspectorHost.Add(inspectorScroll);
            inspectorScroll.contentContainer.AddToClassList("sw-inspector-content");
            string identifier = showingSettings ? "workspace.settings" : "inspector." + (string.IsNullOrEmpty(settings.LockedAsset) ? settings.ActiveAsset : settings.LockedAsset) + "." + settings.SelectedCategory;
            SWEditorViewState state = settings.GetViewState(identifier);
            SWEditorScrollKeeper keeper = new(inspectorScroll, () => state.ScrollPosition, value => state.ScrollPosition = value);
            keeper.Rebuild(BuildInspectorContent);
        }

        private void BuildInspectorContent()
        {
            if (showingSettings)
            {
                BuildSettings(inspectorScroll);
                return;
            }

            SWEditorAssetEntry entry = catalog.Find(string.IsNullOrEmpty(settings.LockedAsset) ? settings.ActiveAsset : settings.LockedAsset);
            if (entry == null)
            {
                BuildOverview(inspectorScroll);
                return;
            }

            SWEditorAssetContext context = catalog.Context(entry);
            VisualElement header = Element("sw-inspector-header", "sw-row");
            inspectorScroll.Add(header);
            TextField title = new()
            {
                value = entry.Asset.name,
                isDelayed = true
            };
            title.AddToClassList("sw-asset-title");
            title.SetEnabled(SWEditorAssetOperations.CanChangeFile(entry));
            title.RegisterValueChangedCallback(eventData =>
            {
                operations.Rename(entry, eventData.newValue);
                title.SetValueWithoutNotify(entry.Asset.name);
            });
            header.Add(title);
            foreach (SWEditorInspectorHeaderExtension extension in SWEditorRegistry.GetInspectorHeaderExtensions(context))
            {
                VisualElement element = SWEditorRegistry.Protect(() => extension.Create(context));
                if (element != null)
                    header.Add(element);
            }

            header.Add(ActionButton("⧉", "Duplicate", () => operations.Duplicate(entry)));
            Button delete = ActionButton("▤", "Delete", () => operations.Delete(entry));
            delete.SetEnabled(SWEditorAssetOperations.CanChangeFile(entry));
            header.Add(delete);
            Button favourite = ActionButton(context.IsFavourite ? "★" : "☆", "Favourites", () => operations.Favourite(entry, !context.IsFavourite));
            favourite.EnableInClassList("sw-selected", context.IsFavourite);
            header.Add(favourite);
            header.Add(ActionButton("⌖", "Ping in Project", () => EditorGUIUtility.PingObject(entry.Asset)));
            foreach (SWEditorAssetValidation validation in SWEditorRegistry.GetValidations(context))
            {
                VisualElement validationRow = Element("sw-section", "sw-validation");
                if (!string.IsNullOrEmpty(validation.Title))
                {
                    Label validationTitle = new(validation.Title);
                    validationTitle.AddToClassList("sw-validation-title");
                    validationRow.Add(validationTitle);
                }
                HelpBox message = new(validation.Message, (HelpBoxMessageType)((int)validation.Severity + 1));
                validationRow.Add(message);
                if (validation.Fix != null)
                    validationRow.Add(new Button(() =>
                    {
                        Undo.RecordObject(entry.Asset, validation.FixLabel ?? "Fix asset");
                        SWEditorRegistry.Protect(() => validation.Fix(context));
                        EditorUtility.SetDirty(entry.Asset);
                        AssetDatabase.SaveAssets();
                        operations.Updated(entry);
                    }) { text = validation.FixLabel ?? "Fix" });
                inspectorScroll.Add(validationRow);
            }

            SWEditorInspectorTab[] extensions = SWEditorRegistry.RegisteredInspectorTabs.Where(tab => SWEditorRegistry.Protect(() => tab.AppliesTo?.Invoke(entry.Asset) ?? true)).ToArray();
            if (extensions.Length > 0)
            {
                VisualElement buttons = Element("sw-row");
                inspectorScroll.Add(buttons);
                Button normal = new(() =>
                {
                    inspectorTab = "Inspector";
                    RefreshInspector();
                })
                {
                    text = "Inspector"
                };
                normal.EnableInClassList("sw-selected", inspectorTab == "Inspector");
                buttons.Add(normal);
                foreach (SWEditorInspectorTab extension in extensions)
                {
                    Button button = new(() =>
                    {
                        inspectorTab = extension.Identifier;
                        RefreshInspector();
                    })
                    {
                        text = extension.Title
                    };
                    button.EnableInClassList("sw-selected", inspectorTab == extension.Identifier);
                    buttons.Add(button);
                }
            }

            SWEditorInspectorTab activeTab = extensions.FirstOrDefault(tab => tab.Identifier == inspectorTab);
            if (activeTab != null)
            {
                try
                {
                    VisualElement content = activeTab.Create(entry.Asset);
                    if (content != null)
                        inspectorScroll.Add(content);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    inspectorScroll.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
                }
            }
            else
                BuildNormalInspector(entry);
        }

        private void BuildNormalInspector(SWEditorAssetEntry entry)
        {
            activeEditor = UnityEditor.Editor.CreateEditor(entry.Asset);
            if (activeEditor == null)
                return;
            VisualElement content;
            try
            {
                content = activeEditor.CreateInspectorGUI();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                content = new HelpBox(exception.Message, HelpBoxMessageType.Error);
            }

            if (content == null)
            {
                UnityEditor.Editor editor = activeEditor;
                content = new IMGUIContainer(() =>
                {
                    if (editor == null || editor.target == null)
                        return;
                    using SWEditorThemeScope theme = new();
                    EditorGUI.BeginChangeCheck();
                    editor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(entry.Asset);
                        operations.Updated(entry);
                    }

                    HandleImmediateReference(entry);
                });
            }
            else
            {
                content.Bind(activeEditor.serializedObject);
                content.TrackSerializedObjectValue(activeEditor.serializedObject, value => operations.Updated(entry));
                content.RegisterCallback<PointerDownEvent>(HandleReference, TrickleDown.TrickleDown);
            }

            SWEditorTheme.ApplyEmbeddedInspector(content);
            inspectorScroll.Add(content);
            SWEditorMetadata[] metadata = SWEditorRegistry.GetMetadata(entry.Asset).ToArray();
            if (metadata.Length > 0)
            {
                VisualElement panel = Element("sw-metadata");
                foreach (SWEditorMetadata item in metadata)
                    panel.Add(new Label(item.Label + ": " + item.Value));
                inspectorScroll.Add(panel);
            }
        }

        private void HandleReference(PointerDownEvent eventData)
        {
            if (eventData.button != 0)
                return;
            VisualElement target = eventData.target as VisualElement;
            for (VisualElement current = target; current != null; current = current.parent)
            {
                if (current.ClassListContains("unity-object-field__selector"))
                    return;
                if (current is not ObjectField field)
                    continue;
                if (field.value is not ScriptableObject asset)
                    return;
                SWEditorAssetEntry reference = catalog.Find(SWEditorAssetCatalog.GetIdentifier(asset));
                if (reference == null)
                    return;
                eventData.StopImmediatePropagation();
                rootVisualElement.schedule.Execute(() => OpenAsset(reference, true));
                return;
            }
        }

        private void HandleImmediateReference(SWEditorAssetEntry inspected)
        {
            // IMGUI 오브젝트 필드의 기본 선택 동작 이후에만 참조를 엽니다.
            if (Event.current.type != EventType.MouseUp || Event.current.button != 0)
                return;
            UnityEngine.Object before = Selection.activeObject;
            rootVisualElement.schedule.Execute(() =>
            {
                if (Selection.activeObject == before || Selection.activeObject == inspected.Asset || Selection.activeObject is not ScriptableObject asset)
                    return;
                SWEditorAssetEntry reference = catalog.Find(SWEditorAssetCatalog.GetIdentifier(asset));
                if (reference != null)
                    OpenAsset(reference, true);
            });
        }

        private void BuildOverview(VisualElement parent)
        {
            var entries = catalog.Query(false);
            var groups = entries.GroupBy(entry => entry.AssetType).OrderByDescending(group => group.Count()).ThenBy(group => group.Key.DisplayName).ToArray();
            Label title = new(GetCategories().FirstOrDefault(item => item.Identifier == settings.SelectedCategory)?.DisplayName ?? "All assets");
            title.AddToClassList("sw-heading");
            parent.Add(title);
            Label subtitle = new("Current category overview");
            subtitle.AddToClassList("sw-muted");
            parent.Add(subtitle);
            VisualElement statistics = Element("sw-statistics");
            parent.Add(statistics);
            AddStatistic(statistics, "Assets", entries.Count.ToString());
            AddStatistic(statistics, "Types", groups.Length.ToString());
            AddStatistic(statistics, "Most common", groups.FirstOrDefault()?.Key.DisplayName ?? "—");
            Label distribution = new("Type distribution");
            distribution.AddToClassList("sw-section-title");
            parent.Add(distribution);
            foreach (var group in groups)
            {
                VisualElement card = Element("sw-distribution");
                VisualElement row = Element("sw-row");
                row.Add(AssetImage(group.First()));
                Label name = new(group.Key.DisplayName);
                name.AddToClassList("sw-grow");
                row.Add(name);
                row.Add(new Label(group.Count().ToString()));
                card.Add(row);
                card.Add(new ProgressBar { lowValue = 0, highValue = Math.Max(1, entries.Count), value = group.Count() });
                parent.Add(card);
            }
        }

        private static void AddStatistic(VisualElement parent, string title, string value)
        {
            VisualElement card = Element("sw-statistic");
            Label label = new(title);
            label.AddToClassList("sw-muted");
            card.Add(label);
            Label amount = new(value);
            amount.AddToClassList("sw-statistic-value");
            card.Add(amount);
            parent.Add(card);
        }
    }
}
