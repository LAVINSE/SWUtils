using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using SW.Base;
using SW.Stat;
using SW.EditorTools.Workspace;
using SW.EditorTools.Window;

namespace SW.Tests.EditorWorkspace
{
    /// <summary>탐색기의 분류, 검색, 확장과 파일 식별 안정성을 검증합니다.</summary>
    public sealed class SWEditorWorkspaceTests
    {
        private SWEditorWorkspaceSettings settings;
        private string originalSettings;
        private string folder;
        private readonly List<string> registrations = new();
        /// <summary>기존 프로젝트 설정을 보관하고 독립된 검증 폴더를 준비합니다.</summary>
        [SetUp]
        public void SetUp()
        {
            settings = SWEditorWorkspaceSettings.instance;
            originalSettings = JsonUtility.ToJson(settings);
            folder = "Assets/__SWUtilsEditorVerification_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
        }

        /// <summary>검증에서 만든 에셋과 등록을 제거하고 사용자 설정을 복원합니다.</summary>
        [TearDown]
        public void TearDown()
        {
            foreach (string identifier in registrations)
                SWEditorRegistry.Unregister(identifier);
            registrations.Clear();
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
            JsonUtility.FromJsonOverwrite(originalSettings, settings);
            settings.Persist();
        }

        /// <summary>개별 분류가 유형 기본값보다 우선하며 분류 제거 후 미분류로 복귀합니다.</summary>
        [Test]
        public void AssetAssignmentOverridesTypeAndRemovedCategoryReturnsToUncategorized()
        {
            SWEditorCategorySettings category = new()
            {
                DisplayName = "Verification"
            };
            settings.Categories.Add(category);
            SWEditorTypeSettings type = new()
            {
                TypeName = "verification.type",
                CategoryIdentifier = category.Identifier
            };
            settings.Types.Add(type);
            settings.AssignCategory("verification.asset", category.Identifier);
            Assert.That(settings.ResolveCategory("verification.asset", new SWEditorTypeSettings { CategoryIdentifier = "different" }), Is.EqualTo(category.Identifier));
            settings.RemoveCategory(category.Identifier);
            Assert.That(type.CategoryIdentifier, Is.EqualTo(SWEditorWorkspaceSettings.UncategorizedCategory));
            Assert.That(settings.ResolveCategory("verification.asset", type), Is.EqualTo(SWEditorWorkspaceSettings.UncategorizedCategory));
        }

        /// <summary>비슷한 접두사를 가진 인접 폴더는 제외하지 않습니다.</summary>
        [Test]
        public void FolderExclusionRespectsDirectoryBoundaries()
        {
            settings.ExcludedFolders.Clear();
            settings.ExcludedFolders.Add("Assets/Generated");
            Assert.That(settings.IsExcluded("Assets/Generated/Nested/Entry.asset"), Is.True);
            Assert.That(settings.IsExcluded("Assets/GeneratedCopy/Entry.asset"), Is.False);
            Assert.That(settings.IsExcluded("Assets/generated/Entry.asset"), Is.True);
        }

        /// <summary>네임스페이스 없는 SWUtils 샘플도 외부 에셋으로 분류되지 않습니다.</summary>
        [Test]
        public void SampleAssemblyIdentifiesSWUtilsWithoutNamespace()
        {
            Type sampleType = TypeCache.GetTypesDerivedFrom<ScriptableObject>().FirstOrDefault(type => type.Name == "SWQuestScoreRewardExample");
            if (sampleType == null)
                Assert.Ignore("SWUtils 샘플이 설치된 프로젝트에서 검사합니다.");
            Assert.That(sampleType.Namespace, Is.Null.Or.Empty);
            Assert.That(SWEditorTypeOrigin.IsSWUtils(sampleType, null), Is.True);
        }

        /// <summary>생성 메뉴의 SWUtils 분류를 지원하되 비슷한 이름의 다른 분류는 포함하지 않습니다.</summary>
        [Test]
        public void CreationMenuOriginRespectsCategoryBoundary()
        {
            Assert.That(SWEditorTypeOrigin.IsSWUtils(typeof(ScriptableObject), "Assets/Create/SWUtils/Samples/Reward"), Is.True);
            Assert.That(SWEditorTypeOrigin.IsSWUtils(typeof(ScriptableObject), "Assets/Create/SWUtilsCopy/Reward"), Is.False);
            Assert.That(SWEditorTypeOrigin.IsSWUtils(typeof(ScriptableObject), "Assets/Create/Other/Reward"), Is.False);
        }

        /// <summary>같은 파일의 하위 에셋을 구분하고 이름 변경 후에도 식별자를 유지합니다.</summary>
        [Test]
        public void SubAssetsHaveDifferentStableIdentifiersAfterRename()
        {
            SWIdentifiedObject main = ScriptableObject.CreateInstance<SWIdentifiedObject>();
            SWIdentifiedObject child = ScriptableObject.CreateInstance<SWIdentifiedObject>();
            AssetDatabase.CreateAsset(main, folder + "/Main.asset");
            AssetDatabase.AddObjectToAsset(child, main);
            AssetDatabase.SaveAssets();
            string mainIdentifier = SWEditorAssetCatalog.GetIdentifier(main), childIdentifier = SWEditorAssetCatalog.GetIdentifier(child);
            Assert.That(mainIdentifier, Is.Not.Empty);
            Assert.That(childIdentifier, Is.Not.EqualTo(mainIdentifier));
            Assert.That(AssetDatabase.RenameAsset(folder + "/Main.asset", "Renamed"), Is.Empty);
            Assert.That(SWEditorAssetCatalog.GetIdentifier(main), Is.EqualTo(mainIdentifier));
            Assert.That(SWEditorAssetCatalog.GetIdentifier(child), Is.EqualTo(childIdentifier));
        }

        /// <summary>새로운 정책은 이미 저장한 사용자의 유형 선택을 덮어쓰지 않습니다.</summary>
        [Test]
        public void TypeRescanPreservesExistingUserChoice()
        {
            settings.IsConfigured = true;
            SWEditorAssetCatalog catalog = new(settings);
            catalog.Refresh();
            SWEditorTypeSettings choice = settings.Types.Single(item => item.TypeName == typeof(SWStat).AssemblyQualifiedName);
            choice.Enabled = false;
            choice.CategoryIdentifier = SWEditorWorkspaceSettings.UncategorizedCategory;
            string identifier = RegisterIdentifier();
            SWEditorRegistry.RegisterTypePolicy(new SWEditorTypePolicy(identifier, type => type == typeof(SWStat), enabled: true, defaultCategoryIdentifier: "swutils.SW.Stat", priority: 1000));
            catalog.Refresh();
            Assert.That(choice.Enabled, Is.False);
            Assert.That(choice.CategoryIdentifier, Is.EqualTo(SWEditorWorkspaceSettings.UncategorizedCategory));
        }

        /// <summary>동일한 확장 식별자는 중복 추가되지 않고 교체됩니다.</summary>
        [Test]
        public void RepeatedRegistrationReplacesCategory()
        {
            string identifier = RegisterIdentifier();
            SWEditorRegistry.RegisterCategory(identifier, "First");
            SWEditorRegistry.RegisterCategory(identifier, "Second");
            Assert.That(SWEditorRegistry.RegisteredCategories.Count(item => item.Identifier == identifier), Is.EqualTo(1));
            Assert.That(SWEditorRegistry.RegisteredCategories.Single(item => item.Identifier == identifier).DisplayName, Is.EqualTo("Second"));
        }

        /// <summary>높은 우선순위보다 가까운 기본 유형의 생성 흐름을 우선합니다.</summary>
        [Test]
        public void CreationWorkflowUsesNearestBaseBeforePriority()
        {
            string general = RegisterIdentifier(), specific = RegisterIdentifier();
            SWEditorRegistry.RegisterCreationWorkflow(new SWEditorCreationWorkflow(general, typeof(ScriptableObject), priority: 999));
            SWEditorRegistry.RegisterCreationWorkflow(new SWEditorCreationWorkflow(specific, typeof(SWIdentifiedObject), priority: 1));
            Assert.That(SWEditorRegistry.GetCreationWorkflow(typeof(SWStat)).Identifier, Is.EqualTo(specific));
        }

        /// <summary>지연 열거 도중 발생한 검색 공급자 예외도 다른 공급자와 격리합니다.</summary>
        [Test]
        public void ThrowingSearchProviderDoesNotPreventOtherProviders()
        {
            string broken = RegisterIdentifier(), working = RegisterIdentifier();
            SWEditorRegistry.RegisterSearchProvider(new SWEditorSearchProvider(broken, context => BrokenSearch()));
            SWEditorRegistry.RegisterSearchProvider(new SWEditorSearchProvider(working, context => new[] { "working-result" }));
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: verification-provider-error");
            Assert.That(SWEditorRegistry.GetSearchTerms(new SWEditorAssetContext()), Does.Contain("working-result"));
        }

        /// <summary>여러 유형 필터와 즐겨찾기, 확장 검색어를 결합합니다.</summary>
        [Test]
        public void BrowserCombinesCategoryTypeFilterAndProviderSearch()
        {
            SWStat first = ScriptableObject.CreateInstance<SWStat>();
            AssetDatabase.CreateAsset(first, folder + "/First.asset");
            SWIdentifiedObject second = ScriptableObject.CreateInstance<SWIdentifiedObject>();
            AssetDatabase.CreateAsset(second, folder + "/Second.asset");
            settings.IsConfigured = true;
            SWEditorAssetCatalog catalog = new(settings);
            catalog.Refresh();
            foreach (SWEditorTypeSettings choice in settings.Types.Where(item => item.TypeName == typeof(SWStat).AssemblyQualifiedName || item.TypeName == typeof(SWIdentifiedObject).AssemblyQualifiedName))
                choice.Enabled = true;
            catalog.Refresh();
            settings.Favourites.Clear();
            settings.Favourites.Add(SWEditorAssetCatalog.GetIdentifier(first));
            settings.Favourites.Add(SWEditorAssetCatalog.GetIdentifier(second));
            settings.SelectedCategory = SWEditorWorkspaceSettings.FavouriteCategory;
            settings.FilteredTypes.Clear();
            settings.FilteredTypes.Add(typeof(SWStat).AssemblyQualifiedName);
            settings.FilteredTypes.Add(typeof(SWIdentifiedObject).AssemblyQualifiedName);
            string provider = RegisterIdentifier();
            SWEditorRegistry.RegisterSearchProvider(new SWEditorSearchProvider(provider, context => context.Asset == first ? new[] { "verification-search-term" } : null));
            settings.SearchText = "verification-search-term";
            Assert.That(catalog.Query().Select(item => item.Asset).ToArray(), Is.EqualTo(new ScriptableObject[] { first }));
            Assert.That(catalog.Query(false).Count, Is.EqualTo(2));
        }

        /// <summary>복제와 이름 변경이 분류를 보존하고 완료 알림을 발생시킵니다.</summary>
        [Test]
        public void DuplicateAndRenamePreserveCategoryAndEmitEvents()
        {
            SWStat original = ScriptableObject.CreateInstance<SWStat>();
            AssetDatabase.CreateAsset(original, folder + "/Original.asset");
            settings.IsConfigured = true;
            SWEditorAssetCatalog catalog = new(settings);
            catalog.Refresh();
            settings.Types.Single(item => item.TypeName == typeof(SWStat).AssemblyQualifiedName).Enabled = true;
            catalog.Refresh();
            SWEditorAssetEntry entry = catalog.Find(SWEditorAssetCatalog.GetIdentifier(original));
            settings.AssignCategory(entry.Identifier, SWEditorWorkspaceSettings.OtherCategory);
            ScriptableObject opened = null;
            List<SWEditorAssetChangeKind> changes = new();
            void OnChanged(SWEditorAssetChange change) => changes.Add(change.Kind);
            SWEditorEvents.AssetChanged += OnChanged;
            try
            {
                SWEditorAssetOperations operations = new(settings, catalog, () =>
                {
                }, asset => opened = asset);
                operations.Duplicate(entry);
                Assert.That(opened, Is.Not.Null);
                Assert.That(opened, Is.Not.SameAs(original));
                SWEditorAssetEntry copy = catalog.Find(SWEditorAssetCatalog.GetIdentifier(opened));
                Assert.That(catalog.Context(copy).CategoryIdentifier, Is.EqualTo(SWEditorWorkspaceSettings.OtherCategory));
                operations.Rename(copy, "Renamed Copy");
                Assert.That(AssetDatabase.GetAssetPath(opened), Is.EqualTo(folder + "/Renamed Copy.asset"));
                CollectionAssert.AreEqual(new[] { SWEditorAssetChangeKind.Duplicated, SWEditorAssetChangeKind.Moved }, changes);
            }
            finally
            {
                SWEditorEvents.AssetChanged -= OnChanged;
            }
        }

        /// <summary>인스펙터 잠금 중 탭을 열어도 편집 대상은 바뀌지 않습니다.</summary>
        [Test]
        public void LockedInspectorKeepsTargetWhenAnotherTabOpens()
        {
            SWStat first = ScriptableObject.CreateInstance<SWStat>();
            AssetDatabase.CreateAsset(first, folder + "/First.asset");
            SWStat second = ScriptableObject.CreateInstance<SWStat>();
            AssetDatabase.CreateAsset(second, folder + "/Second.asset");
            settings.IsConfigured = true;
            settings.OpenAssets.Clear();
            settings.LockedAsset = "";
            settings.ActiveAsset = "";
            SWUtilsEditor window = ScriptableObject.CreateInstance<SWUtilsEditor>();
            try
            {
                settings.Types.Single(item => item.TypeName == typeof(SWStat).AssemblyQualifiedName).Enabled = true;
                window.CreateGUI();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                SWEditorAssetCatalog catalog = (SWEditorAssetCatalog)typeof(SWUtilsEditor).GetField("catalog", flags).GetValue(window);
                MethodInfo open = typeof(SWUtilsEditor).GetMethod("OpenAsset", flags);
                open.Invoke(window, new object[] { catalog.Find(SWEditorAssetCatalog.GetIdentifier(first)), false, false });
                settings.LockedAsset = SWEditorAssetCatalog.GetIdentifier(first);
                open.Invoke(window, new object[] { catalog.Find(SWEditorAssetCatalog.GetIdentifier(second)), true, false });
                Editor editor = (Editor)typeof(SWUtilsEditor).GetField("activeEditor", flags).GetValue(window);
                Assert.That(editor.target, Is.SameAs(first));
                Assert.That(settings.OpenAssets.Count, Is.EqualTo(2));
                Assert.That(settings.ActiveAsset, Is.EqualTo(SWEditorAssetCatalog.GetIdentifier(second)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private string RegisterIdentifier()
        {
            string identifier = "verification." + Guid.NewGuid().ToString("N");
            registrations.Add(identifier);
            return identifier;
        }

        /// <summary>
        /// 기본 분류는 설정에 한 번만 추가되며 삭제 이후 재검색해도 복구되지 않습니다.
        /// </summary>
        [Test]
        public void DefaultCategoriesRemainDeletedAfterRescan()
        {
            settings.ResetWorkspace();
            Assert.That(settings.Categories.Select(item => item.DisplayName), Is.EqualTo(new[]
            {
                "SWUtility", "SWSamples", "SWSkillTree", "SWStat",
                "SWBehaviour Tree", "SWStateMachine", "SWQuest", "Other"
            }));
            settings.RemoveCategory("swutils.SW.Stat");
            SWEditorAssetCatalog catalog = new(settings);
            catalog.Refresh();
            catalog.Refresh();
            Assert.That(settings.Categories.Count, Is.EqualTo(7));
            Assert.That(settings.HasCategory("swutils.SW.Stat"), Is.False);
            Assert.That(settings.Categories.Any(item => item.Identifier.StartsWith("discovered.")), Is.False);
        }

        /// <summary>
        /// 코드에만 등록된 분류는 선택창이나 왼쪽 목록에 나타나지 않습니다.
        /// </summary>
        [Test]
        public void CategoryPickerAndSidebarOnlyUseSavedCategories()
        {
            settings.InitializeCategories();
            string identifier = RegisterIdentifier();
            SWEditorRegistry.RegisterCategory(identifier, "Code only category");
            SWUtilsEditor window = ScriptableObject.CreateInstance<SWUtilsEditor>();
            try
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var categories = (IEnumerable<SWEditorCategory>)typeof(SWUtilsEditor).GetMethod("GetCategories", flags).Invoke(window, null);
                Assert.That(categories.Any(category => category.Identifier == identifier), Is.False);
                var picker = (UnityEngine.UIElements.PopupField<string>)typeof(SWUtilsEditor).GetMethod("CategoryPicker", flags).Invoke(window, new object[]
                {
                    identifier, new Action<string>(value =>
                    {
                    }), "Unassigned"
                });
                Assert.That(picker.choices, Is.EqualTo(settings.Categories.Select(item => item.Identifier)));
                Assert.That(picker.value, Is.Null);
                settings.Categories.Clear();
                picker = (UnityEngine.UIElements.PopupField<string>)typeof(SWUtilsEditor).GetMethod("CategoryPicker", flags).Invoke(window, new object[]
                {
                    identifier, new Action<string>(value =>
                    {
                    }), "Unassigned"
                });
                Assert.That(picker.choices, Is.Empty);
                Assert.That(picker.enabledSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        /// <summary>
        /// 샘플 폴더는 유형과 수동 분류보다 우선하며 비슷한 이름의 폴더는 포함하지 않습니다.
        /// </summary>
        [Test]
        public void SampleFolderOverridesTypeAndAssignmentWithoutMatchingAdjacentFolder()
        {
            settings.InitializeCategories();
            SWEditorTypeSettings type = new() { CategoryIdentifier = "swutils.SW.Stat" };
            settings.AssignCategory("verification.sample", SWEditorWorkspaceSettings.OtherCategory);
            string path = "Assets/SWUtils/Samples/Data/SkillTree/MiningPower.asset";
            Assert.That(settings.ResolveCategory("verification.sample", type, path), Is.EqualTo(SWEditorCategoryDefaults.SamplesCategory));
            Assert.That(settings.ResolveCategory("verification.sample", type, "Assets/SWUtils/SamplesCopy/MiningPower.asset"), Is.EqualTo(SWEditorWorkspaceSettings.OtherCategory));
            Assert.That(SWEditorCategoryDefaults.IsSampleAssetPath("Assets/Other/Samples/Data.asset"), Is.False);
            Assert.That(SWEditorCategoryDefaults.IsSampleAssetPath("Packages/com.swtools.swutils/Samples/Data/MiningPower.asset"), Is.True);
            Assert.That(SWEditorCategoryDefaults.IsSampleAssetPath("Packages/com.swtools.swutils-copy/Samples/Data/MiningPower.asset"), Is.False);
            Assert.That(SWEditorCategoryDefaults.IsSampleAssetPath("Assets/Samples/SWUtils/1.4.0/Data/MiningPower.asset"), Is.True);
            Assert.That(SWEditorCategoryDefaults.IsSampleAssetPath("Assets/Samples/SWUtilsCopy/1.4.0/Data/MiningPower.asset"), Is.False);
            settings.RemoveCategory(SWEditorCategoryDefaults.SamplesCategory);
            Assert.That(settings.ResolveCategory("verification.sample", type, path), Is.Not.EqualTo(SWEditorCategoryDefaults.SamplesCategory));
        }

        /// <summary>
        /// 유형 설정을 다시 생성해도 사용자가 접은 그룹과 검색 상태를 복원합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator TypeSetupRestoresCollapsedGroupsAndReturnsToSettings()
        {
            settings.InitializeCategories();
            settings.IsConfigured = true;
            SWEditorViewState state = settings.GetViewState("types.setup");
            state.SearchText = "";
            state.SetExpanded("SW", false);
            SWUtilsEditor window = ScriptableObject.CreateInstance<SWUtilsEditor>();
            try
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(SWUtilsEditor).GetField("configuringTypes", flags).SetValue(window, true);
                window.CreateGUI();
                window.ShowUtility();
                window.position = new Rect(120, 120, 900, 640);
                yield return null;
                yield return null;
                var group = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Foldout>(window.rootVisualElement, className: "sw-type-group").ToList().First(item => item.text.StartsWith("SW  ("));
                Assert.That(group.value, Is.False);
                group.value = true;
                yield return null;
                var scroll = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.ScrollView>(window.rootVisualElement).ToList().Single();
                scroll.scrollOffset = new Vector2(0, 300);
                yield return null;
                float savedPosition = scroll.scrollOffset.y;
                Assert.That(savedPosition, Is.GreaterThan(100));
                ((Action)typeof(SWUtilsEditor).GetField("refreshTypeSetup", flags).GetValue(window))();
                yield return null;
                yield return null;
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(savedPosition).Within(1));
                window.CreateGUI();
                yield return null;
                yield return null;
                group = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Foldout>(window.rootVisualElement, className: "sw-type-group").ToList().First(item => item.text.StartsWith("SW  ("));
                Assert.That(group.value, Is.True);
                scroll = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.ScrollView>(window.rootVisualElement).ToList().Single();
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(savedPosition).Within(1));
                typeof(SWUtilsEditor).GetMethod("FinishTypeSetup", flags).Invoke(window, new object[] { true });
                Assert.That((bool)typeof(SWUtilsEditor).GetField("configuringTypes", flags).GetValue(window), Is.False);
                Assert.That((bool)typeof(SWUtilsEditor).GetField("showingSettings", flags).GetValue(window), Is.True);
                Assert.That(UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(window.rootVisualElement).ToList().Any(item => item.text == "Categories"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static IEnumerable<string> BrokenSearch()
        {
            yield return "partial";
            throw new InvalidOperationException("verification-provider-error");
        }
    }
}
