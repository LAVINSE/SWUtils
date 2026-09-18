using UnityEditor;
using UnityEngine.UIElements;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        #region 검색 상태
        private VisualElement searchStatusBar;
        private Label searchStatusLabel;
        private Button searchStatusButton;
        private bool searchWasRunning;
        private string searchNotice = string.Empty;
        #endregion // 검색 상태

        #region 검색 화면
        /// <summary>에셋 검색 중에도 사용할 수 있는 상태 안내와 취소 버튼을 구성합니다.</summary>
        private void BuildSearchStatus(VisualElement parent)
        {
            searchNotice = string.Empty;
            searchWasRunning = false;
            searchStatusBar = Element("sw-workspace-toolbar", "sw-row");
            searchStatusLabel = new Label();
            searchStatusLabel.AddToClassList("sw-grow");
            searchStatusLabel.AddToClassList("sw-muted");
            searchStatusBar.Add(searchStatusLabel);
            searchStatusButton = new Button(HandleSearchAction) { text = "취소" };
            searchStatusBar.Add(searchStatusButton);
            searchStatusBar.AddToClassList("sw-hidden");
            parent.Add(searchStatusBar);
        }

        /// <summary>진행 중인 검색을 취소하거나 중단한 검색을 다시 요청합니다.</summary>
        private void HandleSearchAction()
        {
            if (catalog.IsRefreshing)
            {
                catalog.CancelRefresh();
                refreshScheduled = false;
                searchWasRunning = false;
                searchNotice = "검색을 취소했습니다. 이전 목록을 유지합니다.";
                UpdateSearchStatus();
                return;
            }

            searchNotice = string.Empty;
            RequestRefresh();
        }

        /// <summary>한 갱신에서 제한된 양의 에셋을 읽고 완료된 결과만 화면에 반영합니다.</summary>
        private void UpdateAssetSearch()
        {
            if (configuringTypes || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (catalog.IsRefreshing)
            {
                searchNotice = string.Empty;
                // 검색 시작 상태를 먼저 그린 뒤 다음 갱신부터 에셋을 조회합니다.
                if (searchWasRunning && catalog.AdvanceRefresh())
                {
                    searchWasRunning = false;
                    RefreshCategories();
                    RefreshBrowser();
                    RefreshTabs();
                    // 검색 완료가 설정에서 작성 중인 폴더 경로를 지우지 않도록 합니다.
                    if (!showingSettings)
                    {
                        RefreshInspector();
                    }
                }
                else
                {
                    searchWasRunning = catalog.IsRefreshing;
                    searchNotice = catalog.RefreshStatus;
                }
            }

            UpdateSearchStatus();
        }

        /// <summary>진행률 또는 취소·실패 안내를 갱신하며 완료 후 안내 영역을 숨깁니다.</summary>
        private void UpdateSearchStatus()
        {
            if (searchStatusBar == null)
            {
                return;
            }

            bool running = catalog.IsRefreshing;
            searchStatusBar.EnableInClassList("sw-hidden", !running && string.IsNullOrEmpty(searchNotice));
            searchStatusLabel.text = running ? catalog.RefreshStatus : searchNotice;
            searchStatusButton.text = running ? "취소" : "다시 검색";
        }
        #endregion // 검색 화면
    }
}
