using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SW.EditorTools.Workspace
{
    /// <summary>프로젝트별 탐색기 설정과 마지막 작업 상태를 보관합니다.</summary>
    [FilePath("ProjectSettings/SWUtilsEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class SWEditorWorkspaceSettings : ScriptableSingleton<SWEditorWorkspaceSettings>
    {
        private string persistedState;
        [SerializeField] private bool categoriesInitialized;
        [SerializeField] private bool searchFoldersInitialized;
        /// <summary>
        /// 설정 화면과 팝업에서 마지막으로 사용한 검색, 펼침 및 스크롤 상태입니다.
        /// </summary>
        public List<SWEditorViewState> ViewStates = new();

        /// <summary>
        /// 기본 분류를 한 번만 설정에 추가하고 이전 자동 분류를 이전합니다.
        /// </summary>
        public void InitializeCategories()
        {
            if (categoriesInitialized)
            {
                return;
            }

            SWEditorCategoryDefaults.Initialize(this);
            categoriesInitialized = true;
            Persist();
        }

        /// <summary>
        /// 화면별로 독립된 검색, 펼침 및 스크롤 상태를 가져옵니다.
        /// </summary>
        public SWEditorViewState GetViewState(string identifier)
        {
            SWEditorViewState state = ViewStates.Find(item => item.Identifier == identifier);
            if (state == null)
            {
                state = new SWEditorViewState { Identifier = identifier };
                ViewStates.Add(state);
            }

            return state;
        }

        /// <summary>
        /// 실제 설정 목록에 저장된 분류인지 확인합니다.
        /// </summary>
        public bool HasCategory(string identifier)
        {
            return Categories.Any(item => item.Identifier == identifier);
        }

        /// <summary>유형 설정을 마쳤는지 나타냅니다.</summary>
        public bool IsConfigured;
        /// <summary>목록 보기를 사용하는지 나타냅니다.</summary>
        public bool UseListView;
        /// <summary>열린 에셋의 탭 표시 여부입니다.</summary>
        public bool ShowOpenTabs = true;
        /// <summary>격자 카드 너비입니다.</summary>
        public int CellSize = 100;
        /// <summary>브라우저 패널 너비입니다.</summary>
        public float BrowserWidth = 352f;
        /// <summary>이름 우선 정렬 여부입니다.</summary>
        public bool SortByName;
        /// <summary>선택한 분류 식별자입니다.</summary>
        public string SelectedCategory = AllCategory;
        /// <summary>검색어입니다.</summary>
        public string SearchText = "";
        /// <summary>활성 에셋 식별자입니다.</summary>
        public string ActiveAsset = "";
        /// <summary>잠근 인스펙터의 에셋 식별자입니다.</summary>
        public string LockedAsset = "";
        /// <summary>브라우저 스크롤 위치입니다.</summary>
        public Vector2 BrowserScroll;
        /// <summary>분류 스크롤 위치입니다.</summary>
        public Vector2 CategoryScroll;
        /// <summary>인스펙터 스크롤 위치입니다.</summary>
        public Vector2 InspectorScroll;
        /// <summary>사용자 분류 목록입니다.</summary>
        public List<SWEditorCategorySettings> Categories = new();
        /// <summary>발견한 유형별 설정입니다.</summary>
        public List<SWEditorTypeSettings> Types = new();
        /// <summary>에셋별 분류 재정의입니다.</summary>
        public List<SWEditorAssetAssignment> Assignments = new();
        /// <summary>즐겨찾기 에셋 식별자입니다.</summary>
        public List<string> Favourites = new();
        /// <summary>열려 있는 에셋 식별자입니다.</summary>
        public List<string> OpenAssets = new();
        /// <summary>최근 생성한 유형 이름입니다.</summary>
        public List<string> RecentTypes = new();
        /// <summary>제외할 Assets 하위 폴더입니다.</summary>
        public List<string> ExcludedFolders = new();
        /// <summary>하위 폴더를 포함해 탐색할 프로젝트 상대 경로입니다. 비어 있으면 검색하지 않습니다.</summary>
        public List<string> SearchFolders = new();
        /// <summary>필터에서 선택한 유형 이름입니다.</summary>
        public List<string> FilteredTypes = new();
        /// <summary>전체 에셋을 표시하는 가상 분류입니다.</summary>
        public const string AllCategory = "builtin.all";
        /// <summary>즐겨찾기를 표시하는 가상 분류입니다.</summary>
        public const string FavouriteCategory = "builtin.favourites";
        /// <summary>분류되지 않은 에셋의 분류입니다.</summary>
        public const string UncategorizedCategory = "builtin.uncategorized";
        /// <summary>보조 에셋의 분류입니다.</summary>
        public const string OtherCategory = "builtin.other";
        /// <summary>현재 설정을 프로젝트에 저장합니다.</summary>
        public void Persist()
        {
            string currentState = JsonUtility.ToJson(this);
            if (currentState == persistedState)
                return;
            Save(true);
            persistedState = currentState;
        }

        /// <summary>유형 기본값보다 에셋별 분류를 우선합니다.</summary>
        public string ResolveCategory(string assetIdentifier, SWEditorTypeSettings typeSettings, string assetPath = null)
        {
            if (HasCategory(SWEditorCategoryDefaults.SamplesCategory) && SWEditorCategoryDefaults.IsSampleAssetPath(assetPath))
            {
                return SWEditorCategoryDefaults.SamplesCategory;
            }

            SWEditorAssetAssignment assignment = Assignments.Find(item => item.AssetIdentifier == assetIdentifier);
            string identifier = assignment?.CategoryIdentifier ?? typeSettings?.CategoryIdentifier;
            return HasCategory(identifier) ? identifier : UncategorizedCategory;
        }

        /// <summary>파일을 이동하지 않고 탐색기 분류만 지정합니다.</summary>
        public void AssignCategory(string assetIdentifier, string categoryIdentifier)
        {
            Assignments.RemoveAll(item => item.AssetIdentifier == assetIdentifier);
            Assignments.Add(new SWEditorAssetAssignment { AssetIdentifier = assetIdentifier, CategoryIdentifier = categoryIdentifier });
        }

        /// <summary>사용자 분류를 제거하고 관련 항목을 미분류로 돌립니다.</summary>
        public void RemoveCategory(string categoryIdentifier)
        {
            Categories.RemoveAll(item => item.Identifier == categoryIdentifier);
            foreach (SWEditorTypeSettings settings in Types.Where(item => item.CategoryIdentifier == categoryIdentifier))
                settings.CategoryIdentifier = UncategorizedCategory;
            foreach (SWEditorAssetAssignment assignment in Assignments.Where(item => item.CategoryIdentifier == categoryIdentifier))
                assignment.CategoryIdentifier = UncategorizedCategory;
            if (SelectedCategory == categoryIdentifier)
                SelectedCategory = AllCategory;
            Persist();
        }

        #region 탐색 범위
        /// <summary>기존 사용자 설정을 보존하며 SWUtils 데이터 폴더를 최초 한 번 기본 탐색 범위로 등록합니다.</summary>
        public void InitializeSearchFolders()
        {
            if (searchFoldersInitialized)
            {
                return;
            }

            SearchFolders ??= new List<string>();
            if (SearchFolders.Count == 0)
            {
                SearchFolders.AddRange(SWEditorSearchFolders.GetDefaults());
            }

            searchFoldersInitialized = true;
            Persist();
        }

        /// <summary>지정한 탐색 폴더에 포함되고 제외 폴더 밖에 있는 경로인지 확인합니다.</summary>
        public bool IsInSearchScope(string assetPath)
        {
            return SearchFolders.Any(folder => SWEditorSearchFolders.Contains(folder, assetPath)) &&
                !IsExcluded(assetPath);
        }

        /// <summary>폴더 경계를 포함해 제외 여부를 검사합니다.</summary>
        public bool IsExcluded(string assetPath)
        {
            return ExcludedFolders.Any(folder => SWEditorSearchFolders.Contains(folder, assetPath));
        }
        #endregion // 탐색 범위

        /// <summary>배치 설정을 기본값으로 되돌립니다.</summary>
        public void ResetLayout()
        {
            UseListView = false;
            ShowOpenTabs = true;
            CellSize = 100;
            BrowserWidth = 352f;
            BrowserScroll = CategoryScroll = InspectorScroll = Vector2.zero;
            foreach (SWEditorViewState state in ViewStates)
            {
                state.ScrollPosition = Vector2.zero;
            }
            Persist();
        }

        /// <summary>실제 에셋을 유지하면서 탐색기 설정을 초기화합니다.</summary>
        public void ResetWorkspace()
        {
            Categories.Clear();
            categoriesInitialized = false;
            ViewStates.Clear();
            Types.Clear();
            Assignments.Clear();
            Favourites.Clear();
            OpenAssets.Clear();
            RecentTypes.Clear();
            ExcludedFolders.Clear();
            SearchFolders.Clear();
            searchFoldersInitialized = false;
            FilteredTypes.Clear();
            ActiveAsset = LockedAsset = SearchText = "";
            SelectedCategory = AllCategory;
            IsConfigured = false;
            SortByName = false;
            InitializeCategories();
            InitializeSearchFolders();
            ResetLayout();
        }
    }

    /// <summary>사용자 분류의 이름과 아이콘을 저장합니다.</summary>
    [Serializable]
    public sealed class SWEditorCategorySettings
    {
        /// <summary>표시 이름 변경과 무관한 식별자입니다.</summary>
        public string Identifier = Guid.NewGuid().ToString("N");
        /// <summary>분류 이름입니다.</summary>
        public string DisplayName = "New category";
        /// <summary>프로젝트 아이콘의 에셋 식별자입니다.</summary>
        public string IconIdentifier = "";
    }

    /// <summary>유형별 탐색 여부와 기본 분류입니다.</summary>
    [Serializable]
    public sealed class SWEditorTypeSettings
    {
        /// <summary>어셈블리를 포함한 유형 이름입니다.</summary>
        public string TypeName;
        /// <summary>유형 표시 여부입니다.</summary>
        public bool Enabled;
        /// <summary>유형의 기본 분류입니다.</summary>
        public string CategoryIdentifier;
        /// <summary>유형의 생성 경로 분류입니다.</summary>
        public SWEditorTypeClassification Classification;
    }

    /// <summary>개별 에셋에 지정한 분류입니다.</summary>
    [Serializable]
    public sealed class SWEditorAssetAssignment
    {
        /// <summary>에셋 식별자입니다.</summary>
        public string AssetIdentifier;
        /// <summary>지정한 분류 식별자입니다.</summary>
        public string CategoryIdentifier;
    }
}
