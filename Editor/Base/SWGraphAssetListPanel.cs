using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools
{
    /// <summary>그래프 에셋의 검색, 선택, 생성, 이름 변경, 삭제를 제공하는 공통 목록 패널입니다.</summary>
    internal sealed class SWGraphAssetListPanel : VisualElement
    {
        private readonly Type assetType;
        private readonly Action createAssetRequested;
        private readonly Action<UnityEngine.Object> selectedAssetChanged;
        private readonly List<UnityEngine.Object> allAssets = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> visibleAssets = new List<UnityEngine.Object>();
        private readonly ToolbarSearchField searchField;
        private readonly ListView assetListView;
        private readonly TextField assetNameField;
        private readonly Button pingButton;
        private readonly Button deleteButton;
        private UnityEngine.Object selectedAsset;
        private bool isSynchronizingSelection;

        /// <summary>지정한 형식의 그래프 에셋 목록을 생성합니다.</summary>
        public SWGraphAssetListPanel(
            string title,
            string createButtonText,
            Type assetType,
            Action createAssetRequested,
            Action<UnityEngine.Object> selectedAssetChanged)
        {
            this.assetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
            this.createAssetRequested = createAssetRequested
                ?? throw new ArgumentNullException(nameof(createAssetRequested));
            this.selectedAssetChanged = selectedAssetChanged
                ?? throw new ArgumentNullException(nameof(selectedAssetChanged));

            style.flexGrow = 1f;
            style.minWidth = 210f;
            style.backgroundColor = new Color(0.095f, 0.10f, 0.11f);
            style.borderRightWidth = 1f;
            style.borderRightColor = new Color(0.24f, 0.26f, 0.28f);

            Label titleLabel = new Label(title);
            titleLabel.style.height = 30f;
            titleLabel.style.paddingLeft = 10f;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(titleLabel);

            Button createButton = new Button(createAssetRequested) { text = createButtonText };
            createButton.style.height = 25f;
            createButton.style.marginLeft = 6f;
            createButton.style.marginRight = 6f;
            Add(createButton);

            VisualElement commandRow = new VisualElement();
            commandRow.style.flexDirection = FlexDirection.Row;
            commandRow.style.marginLeft = 6f;
            commandRow.style.marginRight = 6f;
            deleteButton = new Button(() => DeleteAsset(selectedAsset)) { text = "선택 삭제" };
            deleteButton.style.flexGrow = 1f;
            commandRow.Add(deleteButton);
            Button refreshButton = new Button(Refresh) { text = "새로고침" };
            refreshButton.style.flexGrow = 1f;
            commandRow.Add(refreshButton);
            Add(commandRow);

            searchField = new ToolbarSearchField();
            searchField.tooltip = "그래프 이름 또는 경로 검색";
            searchField.style.marginLeft = 6f;
            searchField.style.marginRight = 6f;
            searchField.style.marginTop = 5f;
            searchField.style.marginBottom = 5f;
            searchField.RegisterValueChangedCallback(_ => ApplySearchFilter());
            Add(searchField);

            assetListView = new ListView
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = 40f,
                makeItem = CreateAssetRow,
                bindItem = BindAssetRow,
            };
            assetListView.style.flexGrow = 1f;
            assetListView.selectionChanged += OnSelectionChanged;
            Add(assetListView);

            VisualElement selectedAssetPanel = new VisualElement();
            selectedAssetPanel.style.paddingLeft = 6f;
            selectedAssetPanel.style.paddingRight = 6f;
            selectedAssetPanel.style.paddingTop = 5f;
            selectedAssetPanel.style.paddingBottom = 6f;
            selectedAssetPanel.style.borderTopWidth = 1f;
            selectedAssetPanel.style.borderTopColor = new Color(0.24f, 0.26f, 0.28f);
            assetNameField = new TextField("Asset Name") { isDelayed = true };
            assetNameField.RegisterValueChangedCallback(changeEvent =>
                RenameSelectedAsset(changeEvent.newValue));
            selectedAssetPanel.Add(assetNameField);
            pingButton = new Button(PingSelectedAsset) { text = "Ping" };
            selectedAssetPanel.Add(pingButton);
            Add(selectedAssetPanel);

            SetSelectedAssetControlsEnabled(false);
            RegisterCallback<AttachToPanelEvent>(_ => EditorApplication.projectChanged += Refresh);
            RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.projectChanged -= Refresh);
            Refresh();
        }

        /// <summary>프로젝트에서 그래프 에셋을 다시 검색하고 현재 선택을 유지합니다.</summary>
        public void Refresh()
        {
            UnityEngine.Object assetToRestore = selectedAsset;
            allAssets.Clear();
            string[] assetIdentifiers = AssetDatabase.FindAssets($"t:{assetType.Name}");
            Array.Sort(assetIdentifiers, (left, right) => string.Compare(
                AssetDatabase.GUIDToAssetPath(left),
                AssetDatabase.GUIDToAssetPath(right),
                StringComparison.OrdinalIgnoreCase));
            for (int index = 0; index < assetIdentifiers.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetIdentifiers[index]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
                if (asset != null && asset.GetType() == assetType)
                    allAssets.Add(asset);
            }
            ApplySearchFilter();
            SelectAsset(assetToRestore);
        }

        /// <summary>외부에서 열린 그래프 에셋을 목록 선택과 동기화합니다.</summary>
        public void SelectAsset(UnityEngine.Object asset)
        {
            selectedAsset = asset;
            int selectedIndex = asset == null ? -1 : visibleAssets.IndexOf(asset);
            isSynchronizingSelection = true;
            if (selectedIndex >= 0)
                assetListView.SetSelectionWithoutNotify(new[] { selectedIndex });
            else
                assetListView.ClearSelection();
            isSynchronizingSelection = false;
            UpdateSelectedAssetControls();
        }

        /// <summary>목록 패널을 왼쪽으로 접거나 다시 펼치는 그래프 오버레이 버튼을 생성합니다.</summary>
        public Button CreateCollapseButton(TwoPaneSplitView splitView)
        {
            if (splitView == null)
                throw new ArgumentNullException(nameof(splitView));

            bool isExpanded = true;
            Button collapseButton = null;
            collapseButton = new Button(() =>
            {
                if (isExpanded)
                    splitView.CollapseChild(0);
                else
                    splitView.UnCollapse();
                isExpanded = !isExpanded;
                collapseButton.text = isExpanded ? "◀" : "▶";
                collapseButton.tooltip = isExpanded
                    ? "Graph List를 왼쪽으로 접습니다."
                    : "Graph List를 펼칩니다.";
            })
            {
                text = "◀",
                tooltip = "Graph List를 왼쪽으로 접습니다.",
            };
            collapseButton.style.position = Position.Absolute;
            collapseButton.style.left = 5f;
            collapseButton.style.top = 5f;
            collapseButton.style.width = 25f;
            collapseButton.style.height = 24f;
            collapseButton.style.paddingLeft = 0f;
            collapseButton.style.paddingRight = 0f;
            collapseButton.style.backgroundColor = new Color(0.12f, 0.13f, 0.14f, 0.96f);
            return collapseButton;
        }

        /// <summary>가상화 목록에서 재사용할 그래프 에셋 행을 생성합니다.</summary>
        private VisualElement CreateAssetRow()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6f;
            row.style.paddingRight = 3f;
            Image icon = new Image { name = "asset-icon", scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 20f;
            icon.style.height = 20f;
            icon.style.marginRight = 6f;
            row.Add(icon);
            VisualElement textGroup = new VisualElement();
            textGroup.style.flexGrow = 1f;
            textGroup.style.overflow = Overflow.Hidden;
            Label nameLabel = new Label { name = "asset-name" };
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label pathLabel = new Label { name = "asset-path" };
            pathLabel.style.fontSize = 9f;
            pathLabel.style.color = new Color(0.55f, 0.58f, 0.61f);
            textGroup.Add(nameLabel);
            textGroup.Add(pathLabel);
            row.Add(textGroup);
            Button rowDeleteButton = new Button(() =>
                DeleteAsset(row.userData as UnityEngine.Object)) { text = "×" };
            rowDeleteButton.style.width = 22f;
            rowDeleteButton.style.height = 20f;
            row.Add(rowDeleteButton);
            return row;
        }

        /// <summary>지정한 목록 위치의 그래프 에셋 정보를 행에 연결합니다.</summary>
        private void BindAssetRow(VisualElement row, int index)
        {
            if (index < 0 || index >= visibleAssets.Count)
                return;
            UnityEngine.Object asset = visibleAssets[index];
            string path = AssetDatabase.GetAssetPath(asset);
            row.userData = asset;
            row.Q<Image>("asset-icon").image = AssetPreview.GetMiniThumbnail(asset);
            row.Q<Label>("asset-name").text = asset.name;
            row.Q<Label>("asset-path").text = Path.GetDirectoryName(path)?.Replace('\\', '/');
            row.tooltip = path;
        }

        /// <summary>현재 검색어에 맞는 그래프 에셋만 목록에 표시합니다.</summary>
        private void ApplySearchFilter()
        {
            string searchText = searchField?.value?.Trim() ?? string.Empty;
            visibleAssets.Clear();
            for (int index = 0; index < allAssets.Count; index++)
            {
                UnityEngine.Object asset = allAssets[index];
                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrWhiteSpace(searchText)
                    || asset.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    visibleAssets.Add(asset);
            }
            assetListView.itemsSource = visibleAssets;
            assetListView.Rebuild();
            SelectAsset(selectedAsset);
        }

        /// <summary>목록 선택을 그래프 편집기와 선택 항목 편집 영역에 전달합니다.</summary>
        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (isSynchronizingSelection)
                return;
            UnityEngine.Object newSelection = null;
            foreach (object selectedItem in selectedItems)
            {
                newSelection = selectedItem as UnityEngine.Object;
                break;
            }
            selectedAsset = newSelection;
            UpdateSelectedAssetControls();
            selectedAssetChanged(selectedAsset);
        }

        /// <summary>선택한 그래프 에셋의 파일 이름을 변경합니다.</summary>
        private void RenameSelectedAsset(string requestedName)
        {
            if (selectedAsset == null)
                return;
            string newName = requestedName?.Trim();
            if (string.IsNullOrWhiteSpace(newName) || newName == selectedAsset.name)
            {
                assetNameField.SetValueWithoutNotify(selectedAsset.name);
                return;
            }
            string errorMessage = AssetDatabase.RenameAsset(
                AssetDatabase.GetAssetPath(selectedAsset), newName);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                EditorUtility.DisplayDialog("이름 변경 실패", errorMessage, "확인");
                assetNameField.SetValueWithoutNotify(selectedAsset.name);
                return;
            }
            AssetDatabase.SaveAssets();
            selectedAssetChanged(selectedAsset);
            Refresh();
        }

        /// <summary>확인 창을 표시한 뒤 지정한 그래프 에셋을 삭제합니다.</summary>
        private void DeleteAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return;
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!EditorUtility.DisplayDialog(
                    "그래프 에셋 삭제",
                    $"'{asset.name}' 에셋을 삭제할까요?\n{assetPath}\n\n이 작업은 되돌릴 수 없습니다.",
                    "삭제", "취소"))
                return;
            if (asset == selectedAsset)
            {
                selectedAsset = null;
                selectedAssetChanged(null);
            }
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        /// <summary>Project 창에서 선택한 그래프 에셋의 위치를 표시합니다.</summary>
        private void PingSelectedAsset()
        {
            if (selectedAsset == null)
                return;
            Selection.activeObject = selectedAsset;
            EditorGUIUtility.PingObject(selectedAsset);
        }

        /// <summary>선택한 그래프 에셋의 이름과 조작 가능 상태를 갱신합니다.</summary>
        private void UpdateSelectedAssetControls()
        {
            SetSelectedAssetControlsEnabled(selectedAsset != null);
            assetNameField.SetValueWithoutNotify(selectedAsset == null
                ? string.Empty : selectedAsset.name);
        }

        /// <summary>선택 항목 전용 조작 요소의 활성 상태를 변경합니다.</summary>
        private void SetSelectedAssetControlsEnabled(bool isEnabled)
        {
            assetNameField.SetEnabled(isEnabled);
            pingButton.SetEnabled(isEnabled);
            deleteButton.SetEnabled(isEnabled);
        }
    }
}
