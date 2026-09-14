using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools.Workspace
{
    /// <summary>유형을 생성 경로에 따라 분류합니다.</summary>
    public enum SWEditorTypeClassification
    {
        CreateAssetMenu,
        Provider,
        Other,
        Unknown
    }

    /// <summary>에셋 검사 결과의 심각도입니다.</summary>
    public enum SWEditorValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>탐색기에서 수행한 에셋 작업입니다.</summary>
    public enum SWEditorAssetChangeKind
    {
        Created,
        Duplicated,
        Deleted,
        Moved,
        Updated,
        Opened,
        Favourited,
        Categorised
    }

    /// <summary>확장 기능에 전달하는 현재 에셋 상태입니다.</summary>
    public sealed class SWEditorAssetContext
    {
        /// <summary>대상 에셋입니다.</summary>
        public ScriptableObject Asset { get; internal set; }
        /// <summary>에셋의 실제 유형입니다.</summary>
        public Type AssetType { get; internal set; }
        /// <summary>프로젝트 상대 경로입니다.</summary>
        public string AssetPath { get; internal set; }
        /// <summary>파일과 하위 에셋을 구분하는 식별자입니다.</summary>
        public string AssetIdentifier { get; internal set; }
        /// <summary>현재 분류입니다.</summary>
        public string CategoryIdentifier { get; internal set; }
        /// <summary>즐겨찾기 여부입니다.</summary>
        public bool IsFavourite { get; internal set; }
        /// <summary>열린 탭에 포함되는지 나타냅니다.</summary>
        public bool IsOpen { get; internal set; }
        /// <summary>현재 활성 에셋인지 나타냅니다.</summary>
        public bool IsActive { get; internal set; }
    }

    /// <summary>코드에서 제공하는 읽기 전용 분류입니다.</summary>
    public sealed class SWEditorCategory
    {
        /// <summary>분류 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>표시 이름입니다.</summary>
        public string DisplayName { get; }
        /// <summary>분류 아이콘입니다.</summary>
        public Texture2D Icon { get; }
        /// <summary>표시 순서입니다.</summary>
        public int Order { get; }

        /// <summary>분류 정의를 생성합니다.</summary>
        public SWEditorCategory(string identifier, string displayName, Texture2D icon = null, int order = 0)
        {
            Identifier = identifier;
            DisplayName = displayName;
            Icon = icon;
            Order = order;
        }
    }

    /// <summary>새로 발견한 유형에 적용할 기본 정책입니다.</summary>
    public sealed class SWEditorTypePolicy
    {
        /// <summary>정책 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>정책 적용 조건입니다.</summary>
        public Func<Type, bool> AppliesTo { get; }
        /// <summary>유형 분류 재정의입니다.</summary>
        public SWEditorTypeClassification? Classification { get; }
        /// <summary>기본 분류입니다.</summary>
        public string DefaultCategoryIdentifier { get; }
        /// <summary>기본 표시 여부입니다.</summary>
        public bool? Enabled { get; }
        /// <summary>초기 유형 선택 화면에 표시할지 결정합니다.</summary>
        public bool ShowInOnboarding { get; }
        /// <summary>생성 메뉴 우선순위입니다.</summary>
        public int CreationPriority { get; }
        /// <summary>동시에 일치하는 정책의 우선순위입니다.</summary>
        public int Priority { get; }

        /// <summary>유형 정책을 생성합니다.</summary>
        public SWEditorTypePolicy(string identifier, Func<Type, bool> appliesTo, SWEditorTypeClassification? classification = null, string defaultCategoryIdentifier = null, bool? enabled = null, bool showInOnboarding = true, int creationPriority = 0, int priority = 0)
        {
            Identifier = identifier;
            AppliesTo = appliesTo;
            Classification = classification;
            DefaultCategoryIdentifier = defaultCategoryIdentifier;
            Enabled = enabled;
            ShowInOnboarding = showInOnboarding;
            CreationPriority = creationPriority;
            Priority = priority;
        }
    }

    /// <summary>에셋 생성 과정을 확장합니다.</summary>
    public sealed class SWEditorCreationWorkflow
    {
        /// <summary>생성 흐름 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>대상 기본 유형입니다.</summary>
        public Type AssetType { get; }
        /// <summary>에셋 생성 함수입니다. 빈 결과는 기본 경로를 사용합니다.</summary>
        public Func<string, ScriptableObject> Create { get; }
        /// <summary>모든 생성 경로에서 실행하는 완료 처리입니다.</summary>
        public Action<ScriptableObject, string> OnCreated { get; }
        /// <summary>기본 파일 이름을 결정합니다.</summary>
        public Func<Type, string> DefaultFileName { get; }
        /// <summary>같은 기본 유형의 흐름 우선순위입니다.</summary>
        public int Priority { get; }

        /// <summary>생성 흐름을 정의합니다.</summary>
        public SWEditorCreationWorkflow(string identifier, Type assetType, Func<string, ScriptableObject> create = null, Action<ScriptableObject, string> onCreated = null, Func<Type, string> defaultFileName = null, int priority = 0)
        {
            Identifier = identifier;
            AssetType = assetType;
            Create = create;
            OnCreated = onCreated;
            DefaultFileName = defaultFileName;
            Priority = priority;
        }
    }

    /// <summary>직접 생성 경로에서 실행하는 이름 있는 초기화입니다.</summary>
    public sealed class SWEditorCreateAction
    {
        /// <summary>초기화 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>표시 이름입니다.</summary>
        public string DisplayName { get; }
        /// <summary>정확하게 일치해야 하는 대상 유형입니다.</summary>
        public Type AssetType { get; }
        /// <summary>직접 생성 직후 실행할 처리입니다.</summary>
        public Action<ScriptableObject, string> Configure { get; }

        /// <summary>생성 초기화를 정의합니다.</summary>
        public SWEditorCreateAction(Type assetType, string identifier, string displayName, Action<ScriptableObject, string> configure = null)
        {
            AssetType = assetType;
            Identifier = identifier;
            DisplayName = displayName;
            Configure = configure;
        }
    }

    /// <summary>에셋 문맥 메뉴 작업입니다.</summary>
    public sealed class SWEditorAssetAction
    {
        /// <summary>작업 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>메뉴 제목입니다.</summary>
        public string Title { get; }
        /// <summary>작업 본문입니다.</summary>
        public Action<SWEditorAssetContext> Execute { get; }
        /// <summary>표시 조건입니다.</summary>
        public Func<SWEditorAssetContext, bool> AppliesTo { get; }
        /// <summary>실행 가능 조건입니다.</summary>
        public Func<SWEditorAssetContext, bool> IsEnabled { get; }
        /// <summary>확장 화면에서 사용할 아이콘입니다.</summary>
        public Texture2D Icon { get; }
        /// <summary>메뉴 표시 우선순위입니다.</summary>
        public int Priority { get; }

        /// <summary>문맥 메뉴 작업을 정의합니다.</summary>
        public SWEditorAssetAction(string identifier, string title, Action<SWEditorAssetContext> execute, Func<SWEditorAssetContext, bool> appliesTo = null, Func<SWEditorAssetContext, bool> isEnabled = null, Texture2D icon = null, int priority = 0)
        {
            Identifier = identifier;
            Title = title;
            Execute = execute;
            AppliesTo = appliesTo;
            IsEnabled = isEnabled;
            Icon = icon;
            Priority = priority;
        }
    }

    /// <summary>목록과 카드의 상태 표시입니다.</summary>
    public sealed class SWEditorAssetBadge
    {
        /// <summary>배지 문자열입니다.</summary>
        public string Text { get; }
        /// <summary>배지 아이콘입니다.</summary>
        public Texture2D Icon { get; }
        /// <summary>배지 글자 색상입니다.</summary>
        public Color Color { get; }
        /// <summary>자세한 설명입니다.</summary>
        public string Tooltip { get; }

        /// <summary>상태 배지를 생성합니다.</summary>
        public SWEditorAssetBadge(string text = null, Texture2D icon = null, Color? color = null, string tooltip = null)
        {
            Text = text;
            Icon = icon;
            Color = color ?? UnityEngine.Color.white;
            Tooltip = tooltip;
        }
    }

    /// <summary>인스펙터 검사 결과와 수정 작업입니다.</summary>
    public sealed class SWEditorAssetValidation
    {
        /// <summary>검사 심각도입니다.</summary>
        public SWEditorValidationSeverity Severity { get; }
        /// <summary>검사 설명입니다.</summary>
        public string Message { get; }
        /// <summary>검사 제목입니다.</summary>
        public string Title { get; }
        /// <summary>수정 버튼 이름입니다.</summary>
        public string FixLabel { get; }
        /// <summary>선택적으로 실행할 수정입니다.</summary>
        public Action<SWEditorAssetContext> Fix { get; }

        /// <summary>검사 결과를 생성합니다.</summary>
        public SWEditorAssetValidation(SWEditorValidationSeverity severity, string message, string title = null, string fixLabel = null, Action<SWEditorAssetContext> fix = null)
        {
            Severity = severity;
            Message = message;
            Title = title;
            FixLabel = fixLabel;
            Fix = fix;
        }
    }

    /// <summary>추가 검색어 공급자입니다.</summary>
    public sealed class SWEditorSearchProvider
    {
        /// <summary>공급자 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>검색어를 읽는 함수입니다.</summary>
        public Func<SWEditorAssetContext, IEnumerable<string>> Provide { get; }

        /// <summary>검색어 공급자를 정의합니다.</summary>
        public SWEditorSearchProvider(string identifier, Func<SWEditorAssetContext, IEnumerable<string>> provide)
        {
            Identifier = identifier;
            Provide = provide;
        }
    }

    /// <summary>에셋 배지 공급자입니다.</summary>
    public sealed class SWEditorAssetBadgeProvider
    {
        /// <summary>공급자 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>배지를 읽는 함수입니다.</summary>
        public Func<SWEditorAssetContext, IEnumerable<SWEditorAssetBadge>> Provide { get; }

        /// <summary>배지 공급자를 정의합니다.</summary>
        public SWEditorAssetBadgeProvider(string identifier, Func<SWEditorAssetContext, IEnumerable<SWEditorAssetBadge>> provide)
        {
            Identifier = identifier;
            Provide = provide;
        }
    }

    /// <summary>에셋 검증 공급자입니다.</summary>
    public sealed class SWEditorAssetValidator
    {
        /// <summary>검증 공급자 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>검사 결과를 읽는 함수입니다.</summary>
        public Func<SWEditorAssetContext, IEnumerable<SWEditorAssetValidation>> Validate { get; }

        /// <summary>검증 공급자를 정의합니다.</summary>
        public SWEditorAssetValidator(string identifier, Func<SWEditorAssetContext, IEnumerable<SWEditorAssetValidation>> validate)
        {
            Identifier = identifier;
            Validate = validate;
        }
    }

    /// <summary>사용자 지정 인스펙터 탭입니다.</summary>
    public sealed class SWEditorInspectorTab
    {
        /// <summary>탭 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>탭 이름입니다.</summary>
        public string Title { get; }
        /// <summary>적용 조건입니다.</summary>
        public Func<ScriptableObject, bool> AppliesTo { get; }
        /// <summary>탭 내용을 생성합니다.</summary>
        public Func<ScriptableObject, VisualElement> Create { get; }

        /// <summary>확장 탭을 정의합니다.</summary>
        public SWEditorInspectorTab(string identifier, string title, Func<ScriptableObject, bool> appliesTo, Func<ScriptableObject, VisualElement> create)
        {
            Identifier = identifier;
            Title = title;
            AppliesTo = appliesTo;
            Create = create;
        }
    }

    /// <summary>인스펙터 머리글에 추가할 요소입니다.</summary>
    public sealed class SWEditorInspectorHeaderExtension
    {
        /// <summary>확장 식별자입니다.</summary>
        public string Identifier { get; }
        /// <summary>요소 생성 함수입니다.</summary>
        public Func<SWEditorAssetContext, VisualElement> Create { get; }
        /// <summary>표시 조건입니다.</summary>
        public Func<SWEditorAssetContext, bool> AppliesTo { get; }
        /// <summary>표시 우선순위입니다.</summary>
        public int Priority { get; }

        /// <summary>머리글 확장을 정의합니다.</summary>
        public SWEditorInspectorHeaderExtension(string identifier, Func<SWEditorAssetContext, VisualElement> create, Func<SWEditorAssetContext, bool> appliesTo = null, int priority = 0)
        {
            Identifier = identifier;
            Create = create;
            AppliesTo = appliesTo;
            Priority = priority;
        }
    }

    /// <summary>인스펙터 하단에 표시할 부가 정보입니다.</summary>
    public sealed class SWEditorMetadata
    {
        /// <summary>정보 이름입니다.</summary>
        public string Label { get; }
        /// <summary>정보 내용입니다.</summary>
        public string Value { get; }

        /// <summary>부가 정보를 생성합니다.</summary>
        public SWEditorMetadata(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }

    /// <summary>에셋 작업 완료 알림입니다.</summary>
    public sealed class SWEditorAssetChange
    {
        /// <summary>작업 종류입니다.</summary>
        public SWEditorAssetChangeKind Kind { get; internal set; }
        /// <summary>작업 후 상태이며 삭제일 때는 삭제 전 상태입니다.</summary>
        public SWEditorAssetContext Context { get; internal set; }
        /// <summary>이전 에셋 경로입니다.</summary>
        public string PreviousAssetPath { get; internal set; }
        /// <summary>이전 분류입니다.</summary>
        public string PreviousCategoryIdentifier { get; internal set; }
    }

    /// <summary>탐색 목록 갱신 알림입니다.</summary>
    public sealed class SWEditorBrowserRefresh
    {
        /// <summary>현재 선택한 분류입니다.</summary>
        public string CategoryIdentifier { get; internal set; }
        /// <summary>검색과 필터 적용 후 에셋 수입니다.</summary>
        public int VisibleAssetCount { get; internal set; }
    }
}
