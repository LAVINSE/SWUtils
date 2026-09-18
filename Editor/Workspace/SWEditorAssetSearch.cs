using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SW.Util;

namespace SW.EditorTools.Workspace
{
    /// <summary>
    /// 선택한 유형의 에셋만 검색하고 편집기 갱신 사이에 로딩을 나눠 처리합니다.
    /// 취소하거나 실패한 검색 결과는 기존 목록에 적용하지 않습니다.
    /// </summary>
    internal sealed class SWEditorAssetSearch : IDisposable
    {
        #region 필드
        private readonly Dictionary<Type, SWEditorAssetType> enabledTypes;
        private readonly string[] searchFolders;
        private readonly string[] excludedFolders;
        private readonly List<SWEditorAssetEntry> assets = new();
        private readonly Dictionary<string, SWEditorAssetEntry> assetsByIdentifier = new();
        private readonly Dictionary<Type, int> counts = new();
        private IEnumerator<bool> steps;
        private int processedFileCount;
        private int totalFileCount;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>완료 후 적용할 에셋 목록입니다.</summary>
        public IReadOnlyList<SWEditorAssetEntry> Assets => assets;
        /// <summary>완료 후 적용할 유형별 에셋 수입니다.</summary>
        public IReadOnlyDictionary<Type, int> Counts => counts;
        /// <summary>검색 결과 파일의 로딩 진행률입니다. 검색 전에는 0입니다.</summary>
        public float Progress => totalFileCount == 0 ? 0f : (float)processedFileCount / totalFileCount;
        /// <summary>현재 진행 상태를 설명하는 문구입니다.</summary>
        public string Status => totalFileCount == 0
            ? "선택한 유형의 에셋을 검색하고 있습니다."
            : $"에셋 불러오는 중: {processedFileCount:N0} / {totalFileCount:N0}";
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>검증된 검색 조건을 보관하며 실제 검색은 다음 처리 시점까지 미룹니다.</summary>
        private SWEditorAssetSearch(
            IReadOnlyList<SWEditorAssetType> types,
            IReadOnlyList<string> searchFolders,
            IReadOnlyList<string> excludedFolders)
        {
            enabledTypes = types.Where(type => type.Settings.Enabled).ToDictionary(type => type.Type);
            this.searchFolders = SWEditorSearchFolders.GetValidFolders(searchFolders);
            this.excludedFolders = excludedFolders.Where(folder => !string.IsNullOrWhiteSpace(folder))
                .Select(folder => folder.Replace('\\', '/').TrimEnd('/')).ToArray();
            foreach (Type type in enabledTypes.Keys)
            {
                counts[type] = 0;
            }

            steps = Search().GetEnumerator();
        }

        /// <summary>검색 조건을 검증합니다. 필수 목록이 없으면 경고를 기록하고 null을 반환합니다.</summary>
        public static SWEditorAssetSearch Create(
            IReadOnlyList<SWEditorAssetType> types,
            IReadOnlyList<string> searchFolders,
            IReadOnlyList<string> excludedFolders)
        {
            if (types == null || searchFolders == null || excludedFolders == null)
            {
                SWLog.LogWarning("[SWEditorAssetSearch] 검색 준비 실패: 유형 또는 폴더 목록이 없습니다.");
                return null;
            }

            return new SWEditorAssetSearch(types, searchFolders, excludedFolders);
        }
        #endregion // 초기화

        #region 검색
        /// <summary>
        /// 지정한 시간 안에서 로딩을 진행하고 완료 시 true를 반환합니다.
        /// Unity의 단일 검색 또는 단일 파일 로딩 호출 중에는 작업을 중단할 수 없습니다.
        /// </summary>
        public bool Advance(double milliseconds)
        {
            if (steps == null)
            {
                return true;
            }

            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                if (!steps.MoveNext())
                {
                    Dispose();
                    return true;
                }
            }
            while (timer.Elapsed.TotalMilliseconds < milliseconds);

            return false;
        }

        /// <summary>유형 필터를 먼저 적용하며 비활성 유형만 있는 파일은 불러오지 않습니다.</summary>
        private IEnumerable<bool> Search()
        {
            if (enabledTypes.Count == 0 || searchFolders.Length == 0)
            {
                yield break;
            }

            string filter = string.Join(" ", enabledTypes.Keys.Select(type => "t:" + type.Name).Distinct());
            string[] identifiers = AssetDatabase.FindAssets(filter, searchFolders);
            totalFileCount = identifiers.Length;
            yield return true;

            foreach (string identifier in identifiers)
            {
                string path = AssetDatabase.GUIDToAssetPath(identifier);
                if (searchFolders.Any(folder => SWEditorSearchFolders.Contains(folder, path)) && !IsExcluded(path))
                {
                    foreach (UnityEngine.Object candidate in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (candidate is ScriptableObject asset &&
                            enabledTypes.TryGetValue(asset.GetType(), out SWEditorAssetType type))
                        {
                            string assetIdentifier = SWEditorAssetCatalog.GetIdentifier(asset);
                            if (!string.IsNullOrEmpty(assetIdentifier) && !assetsByIdentifier.ContainsKey(assetIdentifier))
                            {
                                SWEditorAssetEntry entry = new()
                                {
                                    Identifier = assetIdentifier,
                                    Asset = asset,
                                    Path = path,
                                    AssetType = type
                                };
                                assets.Add(entry);
                                assetsByIdentifier.Add(assetIdentifier, entry);
                                counts[type.Type]++;
                            }
                        }

                        yield return true;
                    }
                }

                processedFileCount++;
                yield return true;
            }
        }

        /// <summary>제외 폴더 자체와 하위 파일을 검사하며 비슷한 접두사의 다른 폴더는 허용합니다.</summary>
        private bool IsExcluded(string path)
        {
            return excludedFolders.Any(folder => SWEditorSearchFolders.Contains(folder, path));
        }
        #endregion // 검색

        #region 정리
        /// <summary>진행 중인 검색을 중단합니다. 로드한 프로젝트 에셋 자체는 파괴하지 않습니다.</summary>
        public void Dispose()
        {
            steps?.Dispose();
            steps = null;
        }
        #endregion // 정리
    }
}
