using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SW.Util;

namespace SW.EditorTools.Workspace
{
    /// <summary>발견한 생성 가능 유형과 사용자 설정을 연결합니다.</summary>
    public sealed class SWEditorAssetType
    {
        /// <summary>실제 유형입니다.</summary>
        public Type Type { get; internal set; }
        /// <summary>패키지 또는 네임스페이스 그룹입니다.</summary>
        public string Group { get; internal set; }
        /// <summary>유형의 표시 이름입니다.</summary>
        public string DisplayName { get; internal set; }
        /// <summary>기본 Unity 생성 메뉴입니다.</summary>
        public string CreationMenu { get; internal set; }
        /// <summary>기본 생성 파일 이름입니다.</summary>
        public string FileName { get; internal set; }
        /// <summary>사용자 설정입니다.</summary>
        public SWEditorTypeSettings Settings { get; internal set; }
        /// <summary>유형 정책입니다.</summary>
        public SWEditorTypePolicy Policy { get; internal set; }
        /// <summary>현재 발견한 에셋 수입니다.</summary>
        public int AssetCount { get; internal set; }
        /// <summary>해당 유형의 에셋 수를 실제로 조회했는지 나타냅니다.</summary>
        public bool HasAssetCount { get; internal set; }
        /// <summary>조회 전 상태를 0개로 잘못 표시하지 않는 에셋 수 문구입니다.</summary>
        public string AssetCountLabel => HasAssetCount ? AssetCount + " assets" : "Not scanned";
        /// <summary>SWUtils 어셈블리, 네임스페이스 또는 생성 메뉴에 속한 유형인지 확인합니다.</summary>
        public bool IsSWUtils => SWEditorTypeOrigin.IsSWUtils(Type, CreationMenu);
    }

    /// <summary>하위 에셋까지 구분하는 탐색 항목입니다.</summary>
    public sealed class SWEditorAssetEntry
    {
        /// <summary>에셋 식별자입니다.</summary>
        public string Identifier { get; internal set; }
        /// <summary>에셋 객체입니다.</summary>
        public ScriptableObject Asset { get; internal set; }
        /// <summary>에셋 파일 경로입니다.</summary>
        public string Path { get; internal set; }
        /// <summary>발견한 유형 정의입니다.</summary>
        public SWEditorAssetType AssetType { get; internal set; }
    }

    /// <summary>Unity 에셋 검색과 유형 발견을 화면 코드와 분리합니다.</summary>
    public sealed class SWEditorAssetCatalog
    {
        #region 필드
        private readonly SWEditorWorkspaceSettings settings;
        private readonly List<SWEditorAssetType> types = new();
        private readonly List<SWEditorAssetEntry> assets = new();
        private readonly Dictionary<string, SWEditorAssetEntry> assetsByIdentifier = new();
        private SWEditorAssetSearch pendingSearch;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>발견한 구체적인 ScriptableObject 유형입니다.</summary>
        public IReadOnlyList<SWEditorAssetType> Types => types;
        /// <summary>현재 활성화되어 표시 가능한 에셋입니다.</summary>
        public IReadOnlyList<SWEditorAssetEntry> Assets => assets;
        /// <summary>분할 검색이 진행 중인지 나타냅니다.</summary>
        public bool IsRefreshing => pendingSearch != null;
        /// <summary>진행 중인 검색의 로딩 진행률입니다.</summary>
        public float RefreshProgress => pendingSearch?.Progress ?? 0f;
        /// <summary>검색 진행 상태 또는 마지막 실패 안내입니다.</summary>
        public string RefreshStatus { get; private set; } = string.Empty;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>프로젝트 설정을 사용하는 목록을 생성합니다.</summary>
        public SWEditorAssetCatalog(SWEditorWorkspaceSettings settings)
        {
            this.settings = settings;
        }
        #endregion // 초기화

        #region 유형 검색
        /// <summary>에셋 파일을 불러오지 않고 유형과 사용자 선택만 갱신합니다.</summary>
        public void RefreshTypes()
        {
            settings.InitializeCategories();
            settings.InitializeSearchFolders();
            if (settings.SelectedCategory != SWEditorWorkspaceSettings.AllCategory &&
                settings.SelectedCategory != SWEditorWorkspaceSettings.FavouriteCategory &&
                !settings.HasCategory(settings.SelectedCategory))
            {
                settings.SelectedCategory = SWEditorWorkspaceSettings.AllCategory;
            }
            types.Clear();
            Dictionary<string, SWEditorTypeSettings> savedTypes = settings.Types.GroupBy(item => item.TypeName).ToDictionary(group => group.Key, group => group.First());
            Dictionary<Assembly, string> groups = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.IsAbstract || type.IsGenericType || type.ContainsGenericParameters)
                {
                    continue;
                }
                if (!type.IsPublic && !type.IsNestedPublic)
                {
                    continue;
                }
                if (typeof(EditorWindow).IsAssignableFrom(type) || typeof(UnityEditor.Editor).IsAssignableFrom(type))
                {
                    continue;
                }
                CreateAssetMenuAttribute menu = type.GetCustomAttribute<CreateAssetMenuAttribute>();
                SWEditorTypePolicy policy = SWEditorRegistry.GetTypePolicy(type);
                SWEditorTypeClassification classification = policy?.Classification ?? (LooksLikeSupportingAsset(type) ? SWEditorTypeClassification.Other : menu != null ? SWEditorTypeClassification.CreateAssetMenu : SWEditorRegistry.GetCreationWorkflow(type) != null || SWEditorRegistry.GetCreator(type) != null ? SWEditorTypeClassification.Provider : SWEditorTypeClassification.Other);
                if (!savedTypes.TryGetValue(type.AssemblyQualifiedName, out SWEditorTypeSettings choice))
                {
                    choice = new SWEditorTypeSettings
                    {
                        TypeName = type.AssemblyQualifiedName,
                        Enabled = policy?.Enabled ?? classification != SWEditorTypeClassification.Other,
                        CategoryIdentifier = settings.HasCategory(policy?.DefaultCategoryIdentifier) ? policy.DefaultCategoryIdentifier : settings.HasCategory(SWEditorWorkspaceSettings.OtherCategory) ? SWEditorWorkspaceSettings.OtherCategory : SWEditorWorkspaceSettings.UncategorizedCategory
                    };
                    settings.Types.Add(choice);
                }

                choice.Classification = classification;
                if (!groups.TryGetValue(type.Assembly, out string group))
                {
                    group = SWEditorRegistry.Protect(() => UnityEditor.PackageManager.PackageInfo.FindForAssembly(type.Assembly)?.displayName);
                    groups[type.Assembly] = group;
                }

                group ??= type.Namespace?.Split('.')[0] ?? "Project";
                SWEditorAssetType item = new()
                {
                    Type = type,
                    Group = group,
                    DisplayName = ObjectNames.NicifyVariableName(type.Name),
                    CreationMenu = menu == null ? null : "Assets/Create/" + (string.IsNullOrWhiteSpace(menu.menuName) ? ObjectNames.NicifyVariableName(type.Name) : menu.menuName),
                    FileName = string.IsNullOrWhiteSpace(menu?.fileName) ? "New " + ObjectNames.NicifyVariableName(type.Name) : menu.fileName,
                    Settings = choice,
                    Policy = policy
                };
                types.Add(item);
            }

            types.Sort((left, right) => string.Compare(left.Group + left.DisplayName, right.Group + right.DisplayName, StringComparison.OrdinalIgnoreCase));
            HashSet<string> enabledTypes = new(types.Where(type => type.Settings.Enabled).Select(type => type.Settings.TypeName));
            settings.FilteredTypes.RemoveAll(typeName => !enabledTypes.Contains(typeName));
            settings.Persist();
        }
        #endregion // 유형 검색

        #region 에셋 검색
        /// <summary>선택된 유형을 동기 검색합니다. 편집기 창에서는 BeginRefresh와 AdvanceRefresh를 사용합니다.</summary>
        public void Refresh()
        {
            BeginRefresh();
            while (IsRefreshing)
            {
                AdvanceRefresh();
            }
        }

        /// <summary>기존 결과를 유지한 채 선택한 유형의 분할 검색을 준비합니다.</summary>
        public void BeginRefresh()
        {
            CancelRefresh();
            RefreshTypes();
            pendingSearch = SWEditorAssetSearch.Create(types, settings.SearchFolders, settings.ExcludedFolders);
            RefreshStatus = pendingSearch?.Status ?? "검색을 준비하지 못했습니다.";
        }

        /// <summary>한 번에 약 5밀리초씩 로딩합니다. 성공적으로 완료된 경우에만 true를 반환합니다.</summary>
        public bool AdvanceRefresh()
        {
            if (pendingSearch == null)
            {
                return false;
            }

            try
            {
                if (!pendingSearch.Advance(5d))
                {
                    RefreshStatus = pendingSearch.Status;
                    return false;
                }
            }
            catch (Exception exception)
            {
                CancelRefresh();
                RefreshStatus = "에셋 검색에 실패했습니다. 이전 목록을 유지합니다.";
                SWLog.LogError($"[SWEditorAssetCatalog] 검색 실패: {exception}");
                return false;
            }

            assets.Clear();
            assetsByIdentifier.Clear();
            foreach (SWEditorAssetEntry entry in pendingSearch.Assets)
            {
                assets.Add(entry);
                assetsByIdentifier.Add(entry.Identifier, entry);
            }

            foreach (SWEditorAssetType type in types)
            {
                type.HasAssetCount = pendingSearch.Counts.TryGetValue(type.Type, out int count);
                type.AssetCount = count;
            }

            pendingSearch.Dispose();
            pendingSearch = null;
            RefreshStatus = string.Empty;

            settings.OpenAssets.RemoveAll(identifier => !assetsByIdentifier.ContainsKey(identifier));
            if (!assetsByIdentifier.ContainsKey(settings.ActiveAsset ?? ""))
            {
                settings.ActiveAsset = settings.OpenAssets.LastOrDefault() ?? "";
            }
            if (!assetsByIdentifier.ContainsKey(settings.LockedAsset ?? ""))
            {
                settings.LockedAsset = "";
            }
            settings.Persist();
            return true;
        }

        /// <summary>진행 중인 검색만 취소하며 기존 목록과 열린 에셋 상태를 보존합니다.</summary>
        public void CancelRefresh()
        {
            pendingSearch?.Dispose();
            pendingSearch = null;
            RefreshStatus = string.Empty;
        }

        /// <summary>새로 만든 에셋 하나를 등록합니다. 저장되지 않았거나 비활성 유형 또는 탐색 범위 밖이면 false를 반환합니다.</summary>
        public bool TryRegisterAsset(ScriptableObject asset)
        {
            if (asset == null)
            {
                return false;
            }

            SWEditorAssetType type = types.Find(item => item.Type == asset.GetType() && item.Settings.Enabled);
            string identifier = GetIdentifier(asset);
            string path = AssetDatabase.GetAssetPath(asset);
            if (type == null || string.IsNullOrEmpty(identifier) || !settings.IsInSearchScope(path))
            {
                return false;
            }

            if (!assetsByIdentifier.ContainsKey(identifier))
            {
                SWEditorAssetEntry entry = new()
                {
                    Identifier = identifier,
                    Asset = asset,
                    Path = path,
                    AssetType = type
                };
                assets.Add(entry);
                assetsByIdentifier.Add(identifier, entry);
                type.AssetCount++;
            }

            return true;
        }
        #endregion // 에셋 검색

        #region 목록 조회
        /// <summary>식별자로 활성 에셋을 찾습니다.</summary>
        public SWEditorAssetEntry Find(string identifier)
        {
            return identifier != null && assetsByIdentifier.TryGetValue(identifier, out SWEditorAssetEntry asset) ? asset : null;
        }

        /// <summary>현재 설정을 반영한 확장 문맥을 생성합니다.</summary>
        public SWEditorAssetContext Context(SWEditorAssetEntry entry)
        {
            return new()
            {
                Asset = entry.Asset,
                AssetType = entry.AssetType.Type,
                AssetIdentifier = entry.Identifier,
                AssetPath = entry.Path,
                CategoryIdentifier = settings.ResolveCategory(entry.Identifier, entry.AssetType.Settings, entry.Path),
                IsFavourite = settings.Favourites.Contains(entry.Identifier),
                IsOpen = settings.OpenAssets.Contains(entry.Identifier),
                IsActive = settings.ActiveAsset == entry.Identifier
            };
        }

        /// <summary>분류와 검색, 여러 유형 필터를 결합합니다.</summary>
        public List<SWEditorAssetEntry> Query(bool includeSearch = true)
        {
            IEnumerable<SWEditorAssetEntry> result = assets.Where(entry => InCategory(entry, settings.SelectedCategory));
            if (includeSearch)
            {
                if (settings.FilteredTypes.Count > 0)
                    result = result.Where(entry => settings.FilteredTypes.Contains(entry.AssetType.Settings.TypeName));
                if (!string.IsNullOrWhiteSpace(settings.SearchText))
                    result = result.Where(entry => Matches(entry, settings.SearchText.Trim()));
            }

            result = settings.SortByName ? result.OrderBy(entry => entry.Asset.name, StringComparer.OrdinalIgnoreCase) : result.OrderBy(entry => entry.AssetType.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Asset.name, StringComparer.OrdinalIgnoreCase);
            return result.ToList();
        }

        /// <summary>지정한 가상 분류 또는 실제 분류에 속하는지 검사합니다.</summary>
        public bool InCategory(SWEditorAssetEntry entry, string categoryIdentifier)
        {
            return categoryIdentifier == SWEditorWorkspaceSettings.AllCategory || (categoryIdentifier == SWEditorWorkspaceSettings.FavouriteCategory ? settings.Favourites.Contains(entry.Identifier) : settings.ResolveCategory(entry.Identifier, entry.AssetType.Settings, entry.Path) == categoryIdentifier);
        }

        /// <summary>유형 이름과 공급자 검색어까지 검색합니다.</summary>
        public bool Matches(SWEditorAssetEntry entry, string searchText)
        {
            string builtIn = entry.Asset.name + " " + entry.AssetType.Type.FullName + " " + entry.AssetType.DisplayName;
            return builtIn.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 || SWEditorRegistry.GetSearchTerms(Context(entry)).Any(term => term != null && term.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>파일 이동에도 유지되는 식별자를 읽습니다.</summary>
        public static string GetIdentifier(UnityEngine.Object asset)
        {
            return asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long localIdentifier) ? assetGuid + ":" + localIdentifier : "";
        }

        /// <summary>이름 접미사를 기준으로 기본 탐색에서 제외할 보조 데이터 유형인지 확인합니다.</summary>
        private static bool LooksLikeSupportingAsset(Type type)
        {
            string[] suffixes =
            {
                "Settings",
                "Library",
                "Catalog",
                "Catalogue",
                "Config",
                "Configuration",
                "Signal"
            };
            return suffixes.Any(suffix => type.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }
        #endregion // 목록 조회
    }
}
