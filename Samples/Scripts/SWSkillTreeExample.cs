using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SW.Attributes;
using SW.Base;
using SW.Data;
using SW.SkillTree;
using SW.Stat;
using SW.Util;

/// <summary>실제 게임 화면에서 재화 지급, 스킬 구매, 저장 복원과 환생을 확인하는 예제입니다.</summary>
public sealed class SWSkillTreeExample : SWMonoBehaviour
{
    [Serializable]
    private sealed class SaveData
    {
        public double gold;
        public double research;
        public SWSkillTreeSaveData tree;
    }
    [SerializeField] private SWSkillTreeDefinition definition;
    [SerializeField] private SWSkillTreeView view;
    [SerializeField] private SWStats stats;
    [SerializeField] private TextMeshProUGUI balanceLabel;
    [SerializeField] private TextMeshProUGUI feedbackLabel;
    [SerializeField] private Button addCurrencyButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private string saveKey = "SWUtils.SkillTree.Example";
    private SWSkillTreeWallet wallet;
    private SWSkillTreeEffectBinding effects;
    private SWSkillTreeGameContext context;
    private SWSkillTreeSystem previewSystem;

    /// <summary>예제에서 실행 중인 스킬트리입니다.</summary>
    public SWSkillTreeSystem System { get; private set; }

    private void Start() => Initialize();
    /// <summary>예제 실행을 초기화합니다. 여러 번 호출해도 한 번만 생성합니다.</summary>
    public void Initialize()
    {
        if (System != null) return;
        previewSystem?.Dispose();
        previewSystem = null;
        if (definition == null || view == null) { SWLog.LogError("[SWSkillTreeExample] 트리와 화면을 연결하세요."); return; }
        EnsureEventSystem();
        if (stats != null && !stats.IsSetup) stats.Setup();
        wallet = new SWSkillTreeWallet();
        wallet.SetBalance("Gold", 250);
        wallet.SetBalance("Research", 5);
        context = new SWSkillTreeGameContext(stats);
        System = new SWSkillTreeSystem(definition, wallet, context);
        effects = new SWSkillTreeEffectBinding(System);
        view.Bind(System);
        if (balanceLabel == null) BuildControls(transform);
        SWSkillTreeViewFactory.RestoreMissingFonts(transform);
        System.Changed += UpdateBalance;
        addCurrencyButton.onClick.AddListener(AddCurrency);
        saveButton.onClick.AddListener(Save);
        loadButton.onClick.AddListener(Load);
        resetButton.onClick.AddListener(ResetProgress);
        UpdateBalance();
    }

    /// <summary>연결한 트리 정의로 미리보기 노드와 연결선을 생성하고 선행 관계에 따라 자동 배치합니다.</summary>
    [SWButton("노드 생성 및 자동 배치")]
    public void GenerateAndArrangeNodes()
    {
        if (definition == null || view == null)
        {
            SWLog.LogError("[SWSkillTreeExample] 트리 정의와 화면을 먼저 연결하세요.");
            return;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.EditorUtility.IsPersistent(this))
        {
            SWLog.LogWarning("프리팹 편집 모드에서 열거나 씬에 배치한 뒤 노드를 생성하세요.");
            return;
        }
#endif
        if (Application.isPlaying)
        {
            Initialize();
            view.ArrangeAutomatically();
            return;
        }
        // 미리보기는 실제 재화, 능력치와 저장 데이터에 영향을 주지 않습니다.
        SWSkillTreeWallet previewWallet = new();
        previewWallet.SetBalance("Gold", 250);
        previewWallet.SetBalance("Research", 5);
        SWSkillTreeSystem nextPreview = new(definition, previewWallet);
        previewSystem?.Dispose();
        previewSystem = nextPreview;
        view.Bind(previewSystem);
        view.ArrangeAutomatically();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(view);
        if (gameObject.scene.IsValid() && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene))
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

#if UNITY_EDITOR
    /// <summary>수정한 화면 노드의 좌표를 트리 에셋과 Skill Tree Editor에 반영합니다.</summary>
    [SWButton("현재 위치 저장")]
    public void SaveNodePositions() => view?.SaveLayoutToDefinition();

    /// <summary>트리 에셋에 저장한 좌표로 화면 위치를 복원합니다.</summary>
    [SWButton("저장 위치 불러오기")]
    public void LoadNodePositions() => view?.LoadLayoutFromDefinition();
#endif

    /// <summary>예제 조작 막대를 생성합니다. 편집기에서 생성한 결과는 프리팹으로 저장할 수 있습니다.</summary>
    public void BuildControls(Transform parent)
    {
        RectTransform bar = SWSkillTreeViewFactory.CreateRect("ExampleControls", parent);
        SWSkillTreeViewFactory.PlaceFromTop(bar, 0, 60, 16, 16);
        balanceLabel = SWSkillTreeViewFactory.CreateText(bar, "Balance", "Gold 250 · Research 5", 15, SWSkillTreeViewFactory.Text);
        SWSkillTreeViewFactory.Stretch(balanceLabel.rectTransform, 4, 4, 620, 4);
        balanceLabel.alignment = TextAlignmentOptions.MidlineLeft;
        addCurrencyButton = CreateControl(bar, "AddCurrency", "Add Currency", 0);
        saveButton = CreateControl(bar, "Save", "Save", 1);
        loadButton = CreateControl(bar, "Load", "Load", 2);
        resetButton = CreateControl(bar, "Reset", "Reset", 3);
        feedbackLabel = SWSkillTreeViewFactory.CreateText(parent, "ExampleFeedback", "Add currency to try purchases, save, load and reset.", 12, SWSkillTreeViewFactory.MutedText);
        RectTransform feedback = feedbackLabel.rectTransform;
        feedback.anchorMin = Vector2.zero;
        feedback.anchorMax = new Vector2(1, 0);
        feedback.pivot = Vector2.zero;
        feedback.offsetMin = new Vector2(20, 4);
        feedback.offsetMax = new Vector2(-20, 26);
    }
    private static Button CreateControl(Transform parent, string name, string label, int index)
    {
        Button button = SWSkillTreeViewFactory.CreateButton(parent, name, label);
        RectTransform rectangle = (RectTransform)button.transform;
        rectangle.anchorMin = rectangle.anchorMax = new Vector2(1, 0.5f);
        rectangle.pivot = new Vector2(1, 0.5f);
        rectangle.anchoredPosition = new Vector2(-(3 - index) * 112, 0);
        rectangle.sizeDelta = new Vector2(104, 32);
        return button;
    }
    /// <summary>기본 재화와 연구 재화를 지급합니다.</summary>
    public void AddCurrency()
    {
        if (wallet == null) return;
        wallet.TryExchange(new[] { new SWSkillTreeAmount("Gold", 1000), new SWSkillTreeAmount("Research", 5) }, true);
    }
    /// <summary>진행과 지갑을 하나의 저장 값으로 함께 보관합니다.</summary>
    public void Save()
    {
        if (System == null) return;
        try
        {
            SaveData data = new() { gold = wallet.GetBalance("Gold"), research = wallet.GetBalance("Research"), tree = System.CaptureSaveData() };
            SWPlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            SWPlayerPrefs.Save();
            feedbackLabel.text = "Progress and currency saved.";
        }
        catch (Exception exception) { feedbackLabel.text = $"Save failed: {exception.Message}"; }
    }
    /// <summary>저장 데이터를 사전 검증한 뒤 지갑과 진행을 함께 복원합니다.</summary>
    public void Load()
    {
        if (System == null || !SWPlayerPrefs.HasKey(saveKey)) { feedbackLabel.text = "No saved example."; return; }
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(SWPlayerPrefs.GetString(saveKey));
            if (data == null || !new SWSkillTreeAmount("Gold", data.gold).IsValid || !new SWSkillTreeAmount("Research", data.research).IsValid)
                throw new InvalidOperationException("Saved currency is invalid.");
            using SWSkillTreeSystem validation = new(definition, new SWSkillTreeWallet());
            if (!validation.Restore(data.tree, out string reason)) { feedbackLabel.text = reason; return; }
            if (!System.Restore(data.tree, out reason)) { feedbackLabel.text = reason; return; }
            wallet.SetBalance("Gold", data.gold);
            wallet.SetBalance("Research", data.research);
            feedbackLabel.text = "Progress and currency restored.";
        }
        catch (Exception exception) { feedbackLabel.text = $"Load failed: {exception.Message}"; }
    }
    /// <summary>영구 노드를 유지하고 나머지 진행을 환급 없이 초기화합니다.</summary>
    public void ResetProgress()
    {
        if (System == null) return;
        feedbackLabel.text = System.Reset(true, false, out string reason) ? "Progress reset. Permanent nodes retained." : reason;
    }
    private void UpdateBalance()
    {
        balanceLabel.text = $"{new SWSkillTreeAmount("Gold", wallet.GetBalance("Gold"))} · {new SWSkillTreeAmount("Research", wallet.GetBalance("Research"))}";
    }
    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject events = new("SWSkillTreeEventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModule != null) events.AddComponent(inputModule);
#else
        events.AddComponent<StandaloneInputModule>();
#endif
    }
    private void OnDestroy()
    {
        if (System != null) System.Changed -= UpdateBalance;
        if (System != null && view != null) view.Bind(null);
        effects?.Dispose();
        System?.Dispose();
        previewSystem?.Dispose();
    }
}
