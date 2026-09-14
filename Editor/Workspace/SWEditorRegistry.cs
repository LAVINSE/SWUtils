using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools.Workspace
{
    /// <summary>탐색기 확장을 등록하고 외부 콜백의 오류를 격리합니다.</summary>
    public static class SWEditorRegistry
    {
        private static readonly Dictionary<string, SWEditorCategory> categories = new();
        private static readonly Dictionary<string, SWEditorTypePolicy> policies = new();
        private static readonly Dictionary<string, SWEditorCreationWorkflow> workflows = new();
        private static readonly Dictionary<string, SWEditorCreateAction> createActions = new();
        private static readonly Dictionary<string, SWEditorAssetAction> actions = new();
        private static readonly Dictionary<string, SWEditorSearchProvider> searches = new();
        private static readonly Dictionary<string, SWEditorAssetBadgeProvider> badges = new();
        private static readonly Dictionary<string, SWEditorAssetValidator> validators = new();
        private static readonly Dictionary<string, SWEditorInspectorTab> tabs = new();
        private static readonly Dictionary<string, SWEditorInspectorHeaderExtension> headers = new();
        private static readonly Dictionary<Type, Action<ScriptableObject>> creators = new();
        private static readonly Dictionary<Type, Func<ScriptableObject, Texture2D>> icons = new();
        private static readonly List<Func<ScriptableObject, IEnumerable<SWEditorMetadata>>> metadata = new();
        /// <summary>등록 변경 알림입니다.</summary>
        public static event Action Changed;
        /// <summary>외부 등록 분류의 읽기 전용 스냅샷입니다. 화면에는 프로젝트 설정에 저장된 분류만 표시합니다.</summary>
        public static IReadOnlyList<SWEditorCategory> RegisteredCategories => Array.AsReadOnly(categories.Values.OrderBy(item => item.Order).ToArray());
        /// <summary>등록된 유형 정책입니다.</summary>
        public static IReadOnlyList<SWEditorTypePolicy> RegisteredTypePolicies => Array.AsReadOnly(policies.Values.ToArray());
        /// <summary>등록된 생성 흐름입니다.</summary>
        public static IReadOnlyList<SWEditorCreationWorkflow> RegisteredCreationWorkflows => Array.AsReadOnly(workflows.Values.ToArray());
        /// <summary>등록된 생성 초기화입니다.</summary>
        public static IReadOnlyList<SWEditorCreateAction> RegisteredCreateActions => Array.AsReadOnly(createActions.Values.ToArray());
        /// <summary>등록된 에셋 작업입니다.</summary>
        public static IReadOnlyList<SWEditorAssetAction> RegisteredAssetActions => Array.AsReadOnly(actions.Values.ToArray());
        /// <summary>등록된 검색 공급자입니다.</summary>
        public static IReadOnlyList<SWEditorSearchProvider> RegisteredSearchProviders => Array.AsReadOnly(searches.Values.ToArray());
        /// <summary>등록된 배지 공급자입니다.</summary>
        public static IReadOnlyList<SWEditorAssetBadgeProvider> RegisteredAssetBadgeProviders => Array.AsReadOnly(badges.Values.ToArray());
        /// <summary>등록된 검증 공급자입니다.</summary>
        public static IReadOnlyList<SWEditorAssetValidator> RegisteredAssetValidators => Array.AsReadOnly(validators.Values.ToArray());
        /// <summary>등록된 인스펙터 탭입니다.</summary>
        public static IReadOnlyList<SWEditorInspectorTab> RegisteredInspectorTabs => Array.AsReadOnly(tabs.Values.ToArray());
        /// <summary>등록된 머리글 확장입니다.</summary>
        public static IReadOnlyList<SWEditorInspectorHeaderExtension> RegisteredInspectorHeaderExtensions => Array.AsReadOnly(headers.Values.ToArray());

        /// <summary>동일한 식별자의 분류는 교체합니다.</summary>
        public static void RegisterCategory(string identifier, string displayName, int order = 0)
        {
            RegisterCategory(identifier, displayName, null, order);
        }

        /// <summary>외부 분류 정의를 보관합니다. 삭제한 설정 분류를 다시 생성하지 않습니다.</summary>
        public static void RegisterCategory(string identifier, string displayName, Texture2D icon, int order = 0)
        {
            Register(categories, identifier, new SWEditorCategory(identifier, displayName, icon, order));
        }

        /// <summary>유형 기본 정책을 등록합니다.</summary>
        public static void RegisterTypePolicy(SWEditorTypePolicy policy)
        {
            Register(policies, policy.Identifier, policy);
        }

        /// <summary>생성 흐름을 등록합니다.</summary>
        public static void RegisterCreationWorkflow(SWEditorCreationWorkflow workflow)
        {
            Register(workflows, workflow.Identifier, workflow);
        }

        /// <summary>직접 생성 초기화를 등록합니다.</summary>
        public static void RegisterCreateAction(Type assetType, string identifier, string displayName, Action<ScriptableObject, string> configure = null)
        {
            Register(createActions, identifier, new SWEditorCreateAction(assetType, identifier, displayName, configure));
        }

        /// <summary>문맥 메뉴 작업을 등록합니다.</summary>
        public static void RegisterAssetAction(SWEditorAssetAction action)
        {
            Register(actions, action.Identifier, action);
        }

        /// <summary>추가 검색어를 등록합니다.</summary>
        public static void RegisterSearchProvider(SWEditorSearchProvider provider)
        {
            Register(searches, provider.Identifier, provider);
        }

        /// <summary>상태 배지를 등록합니다.</summary>
        public static void RegisterAssetBadgeProvider(SWEditorAssetBadgeProvider provider)
        {
            Register(badges, provider.Identifier, provider);
        }

        /// <summary>검증 공급자를 등록합니다.</summary>
        public static void RegisterAssetValidator(SWEditorAssetValidator validator)
        {
            Register(validators, validator.Identifier, validator);
        }

        /// <summary>인스펙터 탭을 등록합니다.</summary>
        public static void RegisterInspectorTab(string identifier, string title, Func<ScriptableObject, bool> appliesTo, Func<ScriptableObject, VisualElement> create)
        {
            Register(tabs, identifier, new SWEditorInspectorTab(identifier, title, appliesTo, create));
        }

        /// <summary>인스펙터 머리글 확장을 등록합니다.</summary>
        public static void RegisterInspectorHeaderExtension(SWEditorInspectorHeaderExtension extension)
        {
            Register(headers, extension.Identifier, extension);
        }

        /// <summary>부가 정보 공급자를 추가합니다.</summary>
        public static void RegisterMetadataProvider(Func<ScriptableObject, IEnumerable<SWEditorMetadata>> provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            metadata.Add(provider);
            NotifyChanged();
        }

        /// <summary>기본 생성 초기화를 등록합니다.</summary>
        public static void RegisterCreator(Type assetType, Action<ScriptableObject> configure)
        {
            creators[assetType] = configure;
            NotifyChanged();
        }

        /// <summary>유형과 파생 유형의 아이콘 공급자를 등록합니다.</summary>
        public static void RegisterIconProvider(Type assetType, Func<ScriptableObject, Texture2D> provider)
        {
            icons[assetType] = provider;
            NotifyChanged();
        }

        /// <summary>식별자로 등록한 확장을 해제합니다.</summary>
        public static void Unregister(string identifier)
        {
            categories.Remove(identifier);
            policies.Remove(identifier);
            workflows.Remove(identifier);
            createActions.Remove(identifier);
            actions.Remove(identifier);
            searches.Remove(identifier);
            badges.Remove(identifier);
            validators.Remove(identifier);
            tabs.Remove(identifier);
            headers.Remove(identifier);
            NotifyChanged();
        }

        /// <summary>가장 가까운 기본 유형의 초기화를 찾습니다.</summary>
        public static Action<ScriptableObject> GetCreator(Type assetType)
        {
            return FindNearest(creators, assetType);
        }

        /// <summary>가장 가까운 기본 유형의 아이콘을 읽습니다.</summary>
        public static Texture2D GetIcon(Type assetType, ScriptableObject asset = null)
        {
            return Protect(() => FindNearest(icons, assetType)?.Invoke(asset));
        }

        /// <summary>우선순위와 식별자 순서로 유형 정책을 결정합니다.</summary>
        public static SWEditorTypePolicy GetTypePolicy(Type assetType)
        {
            return policies.Values.Where(item => Protect(() => item.AppliesTo?.Invoke(assetType) ?? false)).OrderByDescending(item => item.Priority).ThenBy(item => item.Identifier, StringComparer.Ordinal).FirstOrDefault();
        }

        /// <summary>가장 가까운 기본 유형과 우선순위로 생성 흐름을 결정합니다.</summary>
        public static SWEditorCreationWorkflow GetCreationWorkflow(Type assetType)
        {
            return workflows.Values.Where(item => item.AssetType != null && item.AssetType.IsAssignableFrom(assetType)).OrderBy(item => Distance(assetType, item.AssetType)).ThenByDescending(item => item.Priority).ThenBy(item => item.Identifier, StringComparer.Ordinal).FirstOrDefault();
        }

        /// <summary>에셋에 적용할 작업을 정렬해 반환합니다.</summary>
        public static IEnumerable<SWEditorAssetAction> GetAssetActions(SWEditorAssetContext context)
        {
            return actions.Values.Where(item => Protect(() => item.AppliesTo?.Invoke(context) ?? true)).OrderByDescending(item => item.Priority).ToArray();
        }

        /// <summary>에셋에 적용할 머리글 확장입니다.</summary>
        public static IEnumerable<SWEditorInspectorHeaderExtension> GetInspectorHeaderExtensions(SWEditorAssetContext context)
        {
            return headers.Values.Where(item => Protect(() => item.AppliesTo?.Invoke(context) ?? true)).OrderByDescending(item => item.Priority).ToArray();
        }

        /// <summary>검색 공급자의 지연 열거 오류도 격리합니다.</summary>
        public static IEnumerable<string> GetSearchTerms(SWEditorAssetContext context)
        {
            return searches.Values.ToArray().SelectMany(item => Materialize(() => item.Provide(context)));
        }

        /// <summary>상태 배지를 안전하게 읽습니다.</summary>
        public static IEnumerable<SWEditorAssetBadge> GetBadges(SWEditorAssetContext context)
        {
            return badges.Values.ToArray().SelectMany(item => Materialize(() => item.Provide(context)));
        }

        /// <summary>검증 결과를 안전하게 읽습니다.</summary>
        public static IEnumerable<SWEditorAssetValidation> GetValidations(SWEditorAssetContext context)
        {
            return validators.Values.ToArray().SelectMany(item => Materialize(() => item.Validate(context)));
        }

        /// <summary>부가 정보를 안전하게 읽습니다.</summary>
        public static IEnumerable<SWEditorMetadata> GetMetadata(ScriptableObject asset)
        {
            return metadata.ToArray().SelectMany(provider => Materialize(() => provider(asset)));
        }

        /// <summary>외부 콜백 실패를 콘솔에 기록하고 대체값을 반환합니다.</summary>
        internal static T Protect<T>(Func<T> callback, T fallback = default)
        {
            try
            {
                return callback();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return fallback;
            }
        }

        /// <summary>외부 작업의 오류가 다음 작업을 중단하지 않도록 합니다.</summary>
        internal static void Protect(Action callback)
        {
            Protect(() =>
            {
                callback?.Invoke();
                return true;
            });
        }

        private static IEnumerable<T> Materialize<T>(Func<IEnumerable<T>> callback)
        {
            return Protect(() => callback()?.Where(item => item != null).ToArray(), Array.Empty<T>()) ?? Array.Empty<T>();
        }

        private static T FindNearest<T>(Dictionary<Type, T> values, Type assetType)
        {
            for (Type current = assetType; current != null; current = current.BaseType)
                if (values.TryGetValue(current, out T value))
                    return value;
            return default;
        }

        private static int Distance(Type assetType, Type baseType)
        {
            int distance = 0;
            for (Type current = assetType; current != null && current != baseType; current = current.BaseType)
                distance++;
            return distance;
        }

        private static void Register<T>(Dictionary<string, T> collection, string identifier, T value)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("확장 식별자는 비어 있을 수 없습니다.", nameof(identifier));
            collection[identifier] = value;
            NotifyChanged();
        }

        private static void NotifyChanged()
        {
            if (Changed != null)
                foreach (Action callback in Changed.GetInvocationList())
                    Protect(callback);
        }
    }

    /// <summary>탐색기 작업 완료 이벤트를 안전하게 전달합니다.</summary>
    public static class SWEditorEvents
    {
        /// <summary>에셋 작업 완료 알림입니다.</summary>
        public static event Action<SWEditorAssetChange> AssetChanged;
        /// <summary>목록 갱신 완료 알림입니다.</summary>
        public static event Action<SWEditorBrowserRefresh> BrowserRefreshed;
        internal static void RaiseAssetChanged(SWEditorAssetChange change)
        {
            if (AssetChanged != null)
                foreach (Action<SWEditorAssetChange> callback in AssetChanged.GetInvocationList())
                    SWEditorRegistry.Protect(() => callback(change));
        }

        internal static void RaiseBrowserRefreshed(string categoryIdentifier, int visibleAssetCount)
        {
            if (BrowserRefreshed == null)
                return;
            SWEditorBrowserRefresh refresh = new()
            {
                CategoryIdentifier = categoryIdentifier,
                VisibleAssetCount = visibleAssetCount
            };
            foreach (Action<SWEditorBrowserRefresh> callback in BrowserRefreshed.GetInvocationList())
                SWEditorRegistry.Protect(() => callback(refresh));
        }
    }
}
