using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SW.Base;
using SW.EditorTools.Workspace;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        private readonly Dictionary<string, VisualElement> assetElements = new();
        private IEnumerable<SWEditorCategory> GetCategories()
        {
            yield return new SWEditorCategory(SWEditorWorkspaceSettings.AllCategory, "All assets");
            yield return new SWEditorCategory(SWEditorWorkspaceSettings.FavouriteCategory, "Favourites");
            foreach (SWEditorCategorySettings category in settings.Categories)
                yield return new SWEditorCategory(category.Identifier, category.DisplayName, AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(category.IconIdentifier)));
        }

        private void RefreshCategories()
        {
            if (categoryScroll == null)
                return;
            categoryScrollKeeper.Rebuild(BuildCategories);
        }

        private void BuildCategories()
        {
            categoryScroll.Clear();
            int position = 0;
            foreach (SWEditorCategory category in GetCategories())
            {
                int count = catalog.Assets.Count(entry => catalog.InCategory(entry, category.Identifier));
                if (position++ == 2)
                {
                    Label title = new("Categories");
                    title.AddToClassList("sw-category-title");
                    categoryScroll.Add(title);
                }

                Button button = new(() =>
                {
                    settings.SelectedCategory = category.Identifier;
                    settings.ActiveAsset = "";
                    settings.BrowserScroll = Vector2.zero;
                    showingSettings = false;
                    settings.Persist();
                    RefreshViews();
                });
                button.AddToClassList("sw-category");
                button.EnableInClassList("sw-selected", settings.SelectedCategory == category.Identifier);
                if (category.Icon != null)
                    button.Add(new Image { image = category.Icon, scaleMode = ScaleMode.ScaleToFit });
                else if (category.Identifier == SWEditorWorkspaceSettings.FavouriteCategory)
                {
                    Label icon = new("★");
                    icon.AddToClassList("sw-category-symbol");
                    button.Add(icon);
                }
                else
                {
                    Texture folderIcon = Util.SWEditorUtils.LoadBuiltinIcon("Folder Icon");
                    if (folderIcon != null)
                    {
                        button.Add(new Image { image = folderIcon, scaleMode = ScaleMode.ScaleToFit });
                    }
                }

                Label name = new(category.DisplayName);
                name.AddToClassList("sw-category-name");
                button.Add(name);
                Label total = new(count.ToString());
                total.AddToClassList("sw-category-count");
                button.Add(total);
                if (category.Identifier != SWEditorWorkspaceSettings.AllCategory)
                    RegisterCategoryDrop(button, category.Identifier);
                categoryScroll.Add(button);
            }
        }

        private void RegisterCategoryDrop(VisualElement target, string categoryIdentifier)
        {
            target.RegisterCallback<DragUpdatedEvent>(eventData =>
            {
                if (!DragAndDrop.objectReferences.OfType<ScriptableObject>().Any(asset => catalog.Find(SWEditorAssetCatalog.GetIdentifier(asset)) != null))
                    return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                target.AddToClassList("sw-drop-target");
                eventData.StopPropagation();
            });
            target.RegisterCallback<DragLeaveEvent>(eventData => target.RemoveFromClassList("sw-drop-target"));
            target.RegisterCallback<DragPerformEvent>(eventData =>
            {
                foreach (ScriptableObject asset in DragAndDrop.objectReferences.OfType<ScriptableObject>())
                {
                    SWEditorAssetEntry entry = catalog.Find(SWEditorAssetCatalog.GetIdentifier(asset));
                    if (entry == null)
                        continue;
                    if (categoryIdentifier == SWEditorWorkspaceSettings.FavouriteCategory)
                        operations.Favourite(entry, true);
                    else
                        operations.Categorise(entry, categoryIdentifier);
                }

                DragAndDrop.AcceptDrag();
                target.RemoveFromClassList("sw-drop-target");
                eventData.StopPropagation();
            });
        }

        private void RefreshBrowser()
        {
            if (browserContent == null)
                return;
            browserScrollKeeper.Rebuild(BuildBrowser);
        }

        private void BuildBrowser()
        {
            browserContent.Clear();
            assetElements.Clear();
            visibleAssets = catalog.Query();
            browserContent.EnableInClassList("sw-grid", !settings.UseListView);
            browserContent.EnableInClassList("sw-list", settings.UseListView);
            if (visibleAssets.Count == 0)
            {
                VisualElement empty = Element("sw-empty");
                empty.Add(new Label("No assets found"));
                Label description = new("검색어와 유형 필터를 확인하거나 + 버튼으로 에셋을 만드세요.");
                description.AddToClassList("sw-wrap");
                description.AddToClassList("sw-muted");
                empty.Add(description);
                browserContent.Add(empty);
            }

            foreach (SWEditorAssetEntry entry in visibleAssets)
            {
                SWEditorAssetContext context = catalog.Context(entry);
                VisualElement item = Element(settings.UseListView ? "sw-asset-row" : "sw-asset-card");
                item.name = "asset-" + entry.Identifier;
                item.tooltip = entry.Asset.name + "\n" + entry.AssetType.Type.FullName + "\n" + entry.Path + (context.CategoryIdentifier == SWEditorWorkspaceSettings.UncategorizedCategory ? "\nUncategorized" : "");
                if (!settings.UseListView)
                {
                    item.style.width = Mathf.Clamp(settings.CellSize, 72, 156);
                    item.style.minHeight = Mathf.Max(108, settings.CellSize + 8);
                }

                Image icon = AssetImage(entry);
                icon.AddToClassList("sw-asset-icon");
                item.Add(icon);
                Label name = new(entry.Asset.name);
                name.AddToClassList("sw-asset-name");
                item.Add(name);
                if (settings.UseListView)
                {
                    Label type = new(entry.AssetType.DisplayName);
                    type.AddToClassList("sw-asset-type");
                    item.Add(type);
                }

                SWEditorAssetBadge[] badges = SWEditorRegistry.GetBadges(context).Take(settings.UseListView ? 3 : 2).ToArray();
                if (badges.Length > 0)
                {
                    VisualElement badgeRow = Element("sw-badges", "sw-row");
                    foreach (SWEditorAssetBadge badge in badges)
                    {
                        Label label = new(badge.Text)
                        {
                            tooltip = badge.Tooltip
                        };
                        label.AddToClassList("sw-badge");
                        label.style.color = badge.Color;
                        if (badge.Icon != null)
                            badgeRow.Add(new Image { image = badge.Icon });
                        badgeRow.Add(label);
                    }

                    item.Add(badgeRow);
                }

                item.AddManipulator(new SWEditorAssetPointerManipulator(eventData =>
                {
                    if (eventData.clickCount == 2)
                        EditorGUIUtility.PingObject(entry.Asset);
                    else
                        OpenAsset(entry, eventData.actionKey, eventData.shiftKey);
                }, () => BeginAssetDrag(entry)));
                item.AddManipulator(new ContextualMenuManipulator(eventData => PopulateAssetMenu(eventData.menu, entry)));
                assetElements[entry.Identifier] = item;
                browserContent.Add(item);
            }

            RefreshBrowserSelection();
            SWEditorEvents.RaiseBrowserRefreshed(settings.SelectedCategory, visibleAssets.Count);
        }

        private void RefreshBrowserSelection()
        {
            foreach (KeyValuePair<string, VisualElement> pair in assetElements)
            {
                pair.Value.EnableInClassList("sw-selected", pair.Key == settings.ActiveAsset);
                pair.Value.EnableInClassList("sw-open", settings.OpenAssets.Contains(pair.Key) && pair.Key != settings.ActiveAsset);
            }
        }

        private static Image AssetImage(SWEditorAssetEntry entry)
        {
            Image image = new()
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            Texture2D provided = SWEditorRegistry.GetIcon(entry.AssetType.Type, entry.Asset);
            if (provided != null)
                image.image = provided;
            else if (entry.Asset is SWIdentifiedObject identified && identified.SpriteIcon != null)
                image.sprite = identified.SpriteIcon;
            else
            {
                Sprite sprite = SWEditorIntegration.FindSprite(entry.Asset);
                if (sprite != null)
                    image.sprite = sprite;
                else
                    image.image = AssetPreview.GetMiniThumbnail(entry.Asset);
            }

            return image;
        }

        private void BeginAssetDrag(SWEditorAssetEntry entry)
        {
            IEnumerable<SWEditorAssetEntry> entries = settings.OpenAssets.Contains(entry.Identifier) ? settings.OpenAssets.Select(catalog.Find).Where(item => item != null) : new[]
            {
                entry
            };
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = entries.Select(item => (UnityEngine.Object)item.Asset).ToArray();
            DragAndDrop.StartDrag(entry.Asset.name);
        }

        private void PopulateAssetMenu(DropdownMenu menu, SWEditorAssetEntry entry)
        {
            menu.AppendAction("Open in new tab", action => OpenAsset(entry, true));
            menu.AppendAction("Ping", action => EditorGUIUtility.PingObject(entry.Asset));
            menu.AppendAction(settings.Favourites.Contains(entry.Identifier) ? "Remove from Favourites" : "Add to Favourites", action => operations.Favourite(entry, !settings.Favourites.Contains(entry.Identifier)));
            menu.AppendSeparator();
            menu.AppendAction("Duplicate", action => operations.Duplicate(entry));
            menu.AppendAction("Move / Rename", action => operations.Move(entry), SWEditorAssetOperations.CanChangeFile(entry) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("Delete", action => operations.Delete(entry), SWEditorAssetOperations.CanChangeFile(entry) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            foreach (SWEditorCategory category in GetCategories().Where(item => item.Identifier != SWEditorWorkspaceSettings.AllCategory && item.Identifier != SWEditorWorkspaceSettings.FavouriteCategory))
                menu.AppendAction("Category/" + category.DisplayName, action => operations.Categorise(entry, category.Identifier), catalog.Context(entry).CategoryIdentifier == category.Identifier ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            SWEditorAssetContext context = catalog.Context(entry);
            foreach (SWEditorAssetAction extension in SWEditorRegistry.GetAssetActions(context))
                menu.AppendAction(extension.Title, action =>
                {
                    Undo.RecordObject(entry.Asset, extension.Title);
                    SWEditorRegistry.Protect(() => extension.Execute(context));
                    EditorUtility.SetDirty(entry.Asset);
                    AssetDatabase.SaveAssets();
                    operations.Updated(entry);
                }, SWEditorRegistry.Protect(() => extension.IsEnabled?.Invoke(context) ?? true) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }

    /// <summary>클릭과 드래그를 구분해 다중 선택 상태를 보존합니다.</summary>
    internal sealed class SWEditorAssetPointerManipulator : PointerManipulator
    {
        private readonly Action<PointerUpEvent> clicked;
        private readonly Action drag;
        private Vector2 origin;
        private bool pressed;
        public SWEditorAssetPointerManipulator(Action<PointerUpEvent> clicked, Action drag)
        {
            this.clicked = clicked;
            this.drag = drag;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnDown);
            target.RegisterCallback<PointerMoveEvent>(OnMove);
            target.RegisterCallback<PointerUpEvent>(OnUp);
            target.RegisterCallback<PointerLeaveEvent>(OnLeave);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMove);
            target.UnregisterCallback<PointerUpEvent>(OnUp);
            target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
        }

        private void OnDown(PointerDownEvent eventData)
        {
            if (eventData.button != 0)
                return;
            pressed = true;
            origin = eventData.position;
        }

        private void OnMove(PointerMoveEvent eventData)
        {
            if (!pressed || Vector2.Distance(origin, eventData.position) < 5)
                return;
            pressed = false;
            drag();
            eventData.StopPropagation();
        }

        private void OnUp(PointerUpEvent eventData)
        {
            if (!pressed || eventData.button != 0)
                return;
            pressed = false;
            clicked(eventData);
            eventData.StopPropagation();
        }

        private void OnLeave(PointerLeaveEvent eventData)
        {
            if (eventData.pressedButtons == 0)
                pressed = false;
        }
    }
}
