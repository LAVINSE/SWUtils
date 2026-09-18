using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SW.EditorTools.Workspace;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        #region 목록 상태
        private ListView browserListView;
        private VisualElement browserEmptyState;
        private readonly List<int> browserRowStarts = new();
        private int browserColumnCount = 1;
        private bool browserResizeScheduled;
        #endregion // 목록 상태

        #region 목록 초기화
        /// <summary>격자와 목록에서 보이는 행만 생성하는 스크롤 영역을 준비합니다.</summary>
        private void BuildBrowserViewport(VisualElement parent)
        {
            assetElements.Clear();
            browserResizeScheduled = false;
            browserContent = Element("sw-browser-viewport");
            parent.Add(browserContent);
            browserListView = new ListView
            {
                itemsSource = browserRowStarts,
                selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = CreateBrowserRow,
                bindItem = BindBrowserRow,
                unbindItem = UnbindBrowserRow,
                destroyItem = ReleaseBrowserRow
            };
            browserListView.AddToClassList("sw-browser-scroll");
            browserContent.Add(browserListView);
            browserScroll = browserListView.Q<ScrollView>();
            browserScrollKeeper = new SWEditorScrollKeeper(browserScroll,
                () => settings.BrowserScroll, value => settings.BrowserScroll = value);
            browserScroll.contentViewport.RegisterCallback<GeometryChangedEvent>(HandleBrowserResize);

            browserEmptyState = Element("sw-empty");
            browserEmptyState.Add(new Label("No assets found"));
            Label description = new("Settings의 탐색 폴더와 유형 필터를 확인하거나 + 버튼으로 에셋을 만드세요.");
            description.AddToClassList("sw-wrap");
            description.AddToClassList("sw-muted");
            browserEmptyState.Add(description);
            browserContent.Add(browserEmptyState);
        }
        #endregion // 목록 초기화

        #region 행 재사용
        /// <summary>재사용할 빈 행을 생성합니다. 에셋과 이미지 로딩은 바인딩 시 수행합니다.</summary>
        private VisualElement CreateBrowserRow()
        {
            return Element("sw-browser-virtual-row");
        }

        /// <summary>화면에 들어온 행에 필요한 항목만 연결합니다.</summary>
        private void BindBrowserRow(VisualElement row, int rowIndex)
        {
            ReleaseBrowserRow(row);
            if (rowIndex < 0 || rowIndex >= browserRowStarts.Count)
            {
                return;
            }

            row.EnableInClassList("sw-grid", !settings.UseListView);
            row.EnableInClassList("sw-list", settings.UseListView);
            int first = browserRowStarts[rowIndex];
            int last = Mathf.Min(visibleAssets.Count, first + browserColumnCount);
            for (int index = first; index < last; index++)
            {
                row.Add(CreateBrowserItem(visibleAssets[index]));
            }
        }

        /// <summary>화면에서 벗어난 행의 이미지와 이벤트를 가진 항목 참조를 해제합니다.</summary>
        private void UnbindBrowserRow(VisualElement row, int rowIndex)
        {
            ReleaseBrowserRow(row);
        }

        /// <summary>이 행에 속한 항목만 선택 상태 목록에서 제거하고 행을 비웁니다.</summary>
        private void ReleaseBrowserRow(VisualElement row)
        {
            foreach (VisualElement item in row.Children())
            {
                if (item.userData is string identifier &&
                    assetElements.TryGetValue(identifier, out VisualElement current) &&
                    ReferenceEquals(current, item))
                {
                    assetElements.Remove(identifier);
                }
            }

            row.Clear();
        }
        #endregion // 행 재사용

        #region 크기 변경
        /// <summary>아직 화면에 생성되지 않은 에셋도 행 위치를 기준으로 찾아 이동합니다. 목록에 없으면 유지합니다.</summary>
        private void ScrollToBrowserAsset(string identifier)
        {
            browserListView.schedule.Execute(() =>
            {
                int index = visibleAssets.FindIndex(entry => entry.Identifier == identifier);
                if (index >= 0 && browserListView.panel != null)
                {
                    browserListView.ScrollToItem(index / browserColumnCount);
                }
            }).StartingIn(1);
        }

        /// <summary>현재 영역에 들어가는 열 수를 계산합니다. 크기 미확정 시 한 열을 사용합니다.</summary>
        private int GetBrowserColumnCount()
        {
            float width = browserScroll.contentViewport.resolvedStyle.width;
            if (settings.UseListView || float.IsNaN(width) || width <= 0f)
            {
                return 1;
            }

            float itemWidth = Mathf.Clamp(settings.CellSize, 72, 156) + 6f;
            return Mathf.Max(1, Mathf.FloorToInt((width - 12f) / itemWidth));
        }

        /// <summary>열 수가 달라질 때만 다음 화면 갱신에 행 구성을 다시 계산합니다.</summary>
        private void HandleBrowserResize(GeometryChangedEvent eventData)
        {
            if (browserResizeScheduled || browserColumnCount == GetBrowserColumnCount())
            {
                return;
            }

            browserResizeScheduled = true;
            browserListView.schedule.Execute(() =>
            {
                browserResizeScheduled = false;
                if (browserListView.panel != null)
                {
                    RefreshBrowser();
                }
            });
        }
        #endregion // 크기 변경
    }
}
