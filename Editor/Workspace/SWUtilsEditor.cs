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
    /// <summary>유형 검색, 분류, 다중 탭과 인스펙터를 통합한 SWUtils 에셋 작업 공간입니다.</summary>
    public sealed partial class SWUtilsEditor : EditorWindow
    {
        /// <summary>
        /// 창과 안내 화면에서 공유하는 에셋 편집기 이름입니다.
        /// </summary>
        public const string DisplayName = "SWUtils Data Editor";

        private SWEditorWorkspaceSettings settings;
        private SWEditorAssetCatalog catalog;
        private SWEditorAssetOperations operations;
        private ScrollView categoryScroll;
        private ScrollView browserScroll;
        private SWEditorScrollKeeper categoryScrollKeeper;
        private SWEditorScrollKeeper browserScrollKeeper;
        private VisualElement browserContent;
        private VisualElement inspectorHost;
        private VisualElement tabsHost;
        private VisualElement toolbar;
        private TwoPaneSplitView splitView;
        private List<SWEditorAssetEntry> visibleAssets = new();
        private UnityEditor.Editor activeEditor;
        private ScrollView inspectorScroll;
        private string rangeAnchor;
        private string inspectorTab = "Inspector";
        [SerializeField] private bool showingSettings;
        [SerializeField] private bool configuringTypes;
        private bool refreshScheduled;
        private double nextRefreshTime;
        private double lastPersistenceTime;
        private Label pendingLabel;
        /// <summary>SWUtils 통합 에셋 작업 공간을 엽니다.</summary>
        [MenuItem("SWTools/" + DisplayName, priority = -100)]
        public static void OpenWindow()
        {
            SWUtilsEditor window = GetWindow<SWUtilsEditor>();
            window.titleContent = new GUIContent(DisplayName);
            window.minSize = new Vector2(860, 480);
            window.Show();
        }

        private void OnEnable()
        {
            settings = SWEditorWorkspaceSettings.instance;
            catalog = new SWEditorAssetCatalog(settings);
            operations = new SWEditorAssetOperations(settings, catalog, RequestRefresh, OpenCreated);
            titleContent = new GUIContent(DisplayName);
            minSize = new Vector2(860, 480);
            EditorApplication.projectChanged += RequestRefresh;
            EditorApplication.update += UpdateWorkspace;
            Undo.undoRedoPerformed += RequestRefresh;
            SWEditorRegistry.Changed += RequestRefresh;
        }

        private void OnDisable()
        {
            catalog?.CancelRefresh();
            EditorApplication.projectChanged -= RequestRefresh;
            EditorApplication.update -= UpdateWorkspace;
            Undo.undoRedoPerformed -= RequestRefresh;
            SWEditorRegistry.Changed -= RequestRefresh;
            settings?.Persist();
            DisposeInspector();
        }

        /// <summary>공통 테마를 사용하는 세 영역의 작업 공간을 생성합니다.</summary>
        public void CreateGUI()
        {
            catalog.CancelRefresh();
            DisposeInspector();
            rootVisualElement.Clear();
            SWEditorTheme.Apply(rootVisualElement);
            StyleSheet style = Util.SWEditorUtils.LoadStyleSheetByIdentifier("cb734bb9b20f06f48bb59be35351f534");
            if (style != null)
                rootVisualElement.styleSheets.Add(style);
            rootVisualElement.AddToClassList("sw-workspace");
            catalog.RefreshTypes();
            if (!settings.IsConfigured || configuringTypes)
            {
                BuildTypeSetup();
                return;
            }

            BuildWorkspace();
            RequestRefresh();
        }

        private void BuildWorkspace()
        {
            VisualElement sidebar = Element("sw-sidebar");
            Label brand = new("SWUtils <color=#80bfff>Data Editor</color>")
            {
                enableRichText = true
            };
            brand.AddToClassList("sw-brand");
            sidebar.Add(brand);
            categoryScroll = new ScrollView(ScrollViewMode.Vertical);
            categoryScroll.AddToClassList("sw-category-scroll");
            categoryScrollKeeper = new SWEditorScrollKeeper(categoryScroll, () => settings.CategoryScroll, value => settings.CategoryScroll = value);
            sidebar.Add(categoryScroll);
            rootVisualElement.Add(sidebar);
            VisualElement workspace = Element("sw-workspace-main");
            rootVisualElement.Add(workspace);
            toolbar = Element("sw-workspace-toolbar", "sw-row");
            workspace.Add(toolbar);
            BuildToolbar();
            BuildSearchStatus(workspace);
            float browserWidth = Mathf.Clamp(settings.BrowserWidth, 220, Mathf.Max(220, position.width - 246 - 301));
            splitView = new TwoPaneSplitView(0, browserWidth, TwoPaneSplitViewOrientation.Horizontal);
            splitView.AddToClassList("sw-workspace-split");
            workspace.Add(splitView);
            VisualElement browserPanel = Element("sw-browser-panel");
            BuildBrowserViewport(browserPanel);
            browserPanel.RegisterCallback<GeometryChangedEvent>(eventData =>
            {
                if (eventData.newRect.width > 0)
                    settings.BrowserWidth = eventData.newRect.width;
            });
            splitView.Add(browserPanel);
            VisualElement inspectorPanel = Element("sw-inspector-panel");
            splitView.Add(inspectorPanel);
            splitView.RegisterCallback<GeometryChangedEvent>(eventData => FitBrowserWidth(eventData.newRect.width));
            tabsHost = Element("sw-tabs-host");
            inspectorPanel.Add(tabsHost);
            inspectorHost = Element("sw-inspector-host");
            inspectorPanel.Add(inspectorHost);
            rootVisualElement.UnregisterCallback<KeyDownEvent>(HandleKeyboard);
            rootVisualElement.RegisterCallback<KeyDownEvent>(HandleKeyboard);
            RefreshViews();
        }

        private void BuildToolbar()
        {
            toolbar.Clear();
            toolbar.Add(ActionButton("+", "Create asset", () => ShowCreatePicker(toolbar.worldBound)));
            TextField search = new()
            {
                value = settings.SearchText
            };
            search.AddToClassList("sw-workspace-search");
            search.textEdition.placeholder = "Search for assets...";
            search.RegisterValueChangedCallback(eventData =>
            {
                settings.SearchText = eventData.newValue;
                RefreshBrowser();
            });
            toolbar.Add(search);
            toolbar.Add(ActionButton("≡", "Sort / filter", () => ShowFilterPicker(toolbar.worldBound)));
            toolbar.Add(ActionButton("↻", "Refresh assets", () =>
            {
                AssetDatabase.Refresh();
                RequestRefresh();
            }));
            pendingLabel = new Label("Creating asset…")
            {
                tooltip = "Unity의 새 에셋 이름 입력을 완료하세요."
            };
            pendingLabel.AddToClassList("sw-muted");
            toolbar.Add(pendingLabel);
            VisualElement spacer = Element("sw-grow");
            toolbar.Add(spacer);
            Button cancel = ActionButton("×", "Cancel pending creation", SWEditorAssetCreation.Cancel);
            cancel.name = "cancel-creation";
            toolbar.Add(cancel);
            Button lockButton = ActionButton(string.IsNullOrEmpty(settings.LockedAsset) ? "◇" : "◆", "Lock inspector", () =>
            {
                settings.LockedAsset = string.IsNullOrEmpty(settings.LockedAsset) ? settings.ActiveAsset : "";
                settings.Persist();
                BuildToolbar();
                RefreshInspector();
            });
            lockButton.EnableInClassList("sw-selected", !string.IsNullOrEmpty(settings.LockedAsset));
            toolbar.Add(lockButton);
            toolbar.Add(ActionButton("⚙", "Settings", () =>
            {
                showingSettings = true;
                RefreshTabs();
                RefreshInspector();
            }));
            UpdatePendingState();
        }

        /// <summary>창을 줄일 때 브라우저가 인스펙터의 최소 너비를 침범하지 않도록 조정합니다.</summary>
        private void FitBrowserWidth(float availableWidth)
        {
            if (availableWidth <= 0 || splitView.fixedPane == null)
                return;
            float maximumWidth = Mathf.Max(220, availableWidth - 301);
            splitView.fixedPane.style.maxWidth = maximumWidth;
            float currentWidth = splitView.fixedPane.resolvedStyle.width;
            if (float.IsNaN(currentWidth) || currentWidth <= maximumWidth)
                return;
            splitView.fixedPaneInitialDimension = maximumWidth;
        }

        private void RequestRefresh()
        {
            refreshScheduled = true;
            nextRefreshTime = EditorApplication.timeSinceStartup + 0.15;
        }

        private void UpdateWorkspace()
        {
            if (settings == null)
                return;
            if (refreshScheduled && EditorApplication.timeSinceStartup >= nextRefreshTime && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                refreshScheduled = false;
                if (configuringTypes)
                {
                    catalog.RefreshTypes();
                    refreshTypeSetup?.Invoke();
                }
                else if (settings.IsConfigured && browserContent != null)
                {
                    catalog.BeginRefresh();
                }
            }

            UpdateAssetSearch();
            UpdatePendingState();
            if (EditorApplication.timeSinceStartup - lastPersistenceTime > 5)
            {
                lastPersistenceTime = EditorApplication.timeSinceStartup;
                settings.Persist();
            }

            if (activeEditor != null && activeEditor.RequiresConstantRepaint())
                Repaint();
        }

        private void UpdatePendingState()
        {
            if (pendingLabel == null)
                return;
            pendingLabel.EnableInClassList("sw-hidden", !SWEditorAssetCreation.IsPending);
            toolbar.Q<Button>("cancel-creation")?.EnableInClassList("sw-hidden", !SWEditorAssetCreation.IsPending);
        }

        private void RefreshViews()
        {
            RefreshCategories();
            RefreshBrowser();
            RefreshTabs();
            RefreshInspector();
        }

        private void OpenCreated(ScriptableObject asset)
        {
            catalog.CancelRefresh();
            if (!catalog.TryRegisterAsset(asset))
            {
                if (asset != null && !settings.IsInSearchScope(AssetDatabase.GetAssetPath(asset)))
                {
                    ShowNotification(new GUIContent("에셋을 생성했습니다. 목록에서 보려면 Settings에 해당 탐색 폴더를 추가하세요."));
                }
                RequestRefresh();
                return;
            }
            SWEditorAssetEntry entry = catalog.Find(SWEditorAssetCatalog.GetIdentifier(asset));
            if (entry == null)
                return;
            OpenAsset(entry, true);
            RefreshCategories();
            RefreshBrowser();
            RequestRefresh();
        }

        private void OpenAsset(SWEditorAssetEntry entry, bool additive = false, bool range = false)
        {
            if (entry == null)
                return;
            if (range && !string.IsNullOrEmpty(rangeAnchor))
            {
                int first = visibleAssets.FindIndex(item => item.Identifier == rangeAnchor);
                int last = visibleAssets.IndexOf(entry);
                if (first >= 0 && last >= 0)
                    foreach (SWEditorAssetEntry item in visibleAssets.Skip(Math.Min(first, last)).Take(Math.Abs(last - first) + 1))
                        if (!settings.OpenAssets.Contains(item.Identifier))
                            settings.OpenAssets.Add(item.Identifier);
            }
            else
            {
                if (!additive)
                    settings.OpenAssets.Clear();
                rangeAnchor = entry.Identifier;
            }

            if (!settings.OpenAssets.Contains(entry.Identifier))
                settings.OpenAssets.Add(entry.Identifier);
            settings.ActiveAsset = entry.Identifier;
            showingSettings = false;
            inspectorTab = "Inspector";
            settings.Persist();
            RefreshTabs();
            RefreshBrowserSelection();
            RefreshInspector();
            SWEditorEvents.RaiseAssetChanged(new SWEditorAssetChange { Kind = SWEditorAssetChangeKind.Opened, Context = catalog.Context(entry) });
        }

        private void CloseAsset(string identifier)
        {
            int index = settings.OpenAssets.IndexOf(identifier);
            settings.OpenAssets.Remove(identifier);
            if (settings.LockedAsset == identifier)
                settings.LockedAsset = "";
            if (settings.ActiveAsset == identifier)
                settings.ActiveAsset = settings.OpenAssets.Count == 0 ? "" : settings.OpenAssets[Mathf.Clamp(index - 1, 0, settings.OpenAssets.Count - 1)];
            settings.Persist();
            BuildToolbar();
            RefreshTabs();
            RefreshBrowserSelection();
            RefreshInspector();
        }

        private void HandleKeyboard(KeyDownEvent eventData)
        {
            if (eventData.target is TextElement || eventData.target is TextField)
                return;
            if (eventData.keyCode == KeyCode.Escape)
            {
                settings.ActiveAsset = "";
                showingSettings = false;
                RefreshInspector();
            }

            if (eventData.keyCode == KeyCode.F5)
            {
                AssetDatabase.Refresh();
                RequestRefresh();
                eventData.StopPropagation();
            }

            if (eventData.actionKey && eventData.keyCode == KeyCode.W)
            {
                CloseAsset(settings.ActiveAsset);
                eventData.StopPropagation();
            }
        }

        private static VisualElement Element(params string[] classes)
        {
            VisualElement element = new();
            foreach (string className in classes)
                element.AddToClassList(className);
            return element;
        }

        private static Button ActionButton(string text, string tooltip, Action clicked)
        {
            Button button = new(clicked)
            {
                text = text,
                tooltip = tooltip
            };
            button.AddToClassList("sw-icon-button");
            if (tooltip == "Ping in Project")
            {
                button.text = "";
                button.Add(new SWEditorLocationIcon());
                return button;
            }

            string iconName = tooltip switch
            {
                "Settings" => "SettingsIcon",
                "Lock inspector" => text == "◆" ? "IN LockButton on" : "IN LockButton",
                "Duplicate" => "TreeEditor.Duplicate",
                "Delete" => "TreeEditor.Trash",
                _ => null
            };
            Texture icon = iconName == null ? null : Util.SWEditorUtils.LoadBuiltinIcon(iconName);
            if (icon != null)
            {
                button.text = "";
                Image image = new()
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore
                };
                image.AddToClassList("sw-toolbar-icon");
                button.Add(image);
            }

            return button;
        }

        private void DisposeInspector()
        {
            if (activeEditor != null)
                DestroyImmediate(activeEditor);
            activeEditor = null;
        }
    }
}
