using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.EditorTools.Util;
using SW.SkillTree;

#if UNITY_6000_4_OR_NEWER
using SWObjectIdentifier = UnityEngine.EntityId;
#else
using SWObjectIdentifier = System.Int32;
#endif

namespace SW.EditorTools.SkillTree
{
    /// <summary>SW 스타일의 목록, 그래프와 상세 패널로 스킬트리를 제작하는 창입니다.</summary>
    public sealed class SWSkillTreeEditorWindow : EditorWindow
    {
        [SerializeField] private SWSkillTreeDefinition definition;
        [SerializeField] private string selectedIdentifier;
        private SWSkillTreeGraphView graph;
        private SWGraphAssetListPanel assetList;
        private Label status;
        private Label titleLabel;
        private Editor skillEditor;
        private readonly List<string> errors = new();
        private Vector2 inspectorScroll;

        /// <summary>스킬트리 제작 창을 엽니다.</summary>
        [MenuItem("SWTools/Utils/Data/Skill Tree Editor")]
        public static void OpenWindow() => Open(null);
        /// <summary>지정한 트리를 제작 창에서 엽니다.</summary>
        public static void Open(SWSkillTreeDefinition asset)
        {
            SWSkillTreeEditorWindow window = GetWindow<SWSkillTreeEditorWindow>();
            SWEditorUtils.SetupWindow(window, "SW Skill Tree", "d_ScriptableObject Icon", 960, 540);
            if (asset != null) window.SelectAsset(asset);
            window.Show();
        }
        /// <summary>트리 에셋을 두 번 클릭하면 Unity 버전에 맞는 식별자로 제작 창을 엽니다.</summary>
        [OnOpenAsset]
        private static bool OnOpenAsset(SWObjectIdentifier objectIdentifier, int line)
        {
            if (SWEditorObjectUtility.FindObject(objectIdentifier) is not SWSkillTreeDefinition tree) return false;
            Open(tree);
            return true;
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndo;
            SWSkillTreeView.LayoutSaved += OnLayoutSaved;
        }
        private void OnLayoutSaved(SWSkillTreeDefinition changed)
        {
            if (changed == definition) { graph?.Populate(definition); Repaint(); }
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            SWSkillTreeView.LayoutSaved -= OnLayoutSaved;
            if (skillEditor != null) DestroyImmediate(skillEditor);
        }
        /// <summary>목록, 도구 모음, 그래프와 인스펙터를 구성합니다.</summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = new Color(0.075f, 0.08f, 0.09f);
            Toolbar toolbar = new();
            SWGraphEditorVisualUtility.ApplyToolbar(toolbar);
            titleLabel = new Label("SW Skill Tree");
            SWGraphEditorVisualUtility.ApplyToolbarTitle(titleLabel);
            toolbar.Add(titleLabel);
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton("노드 추가", () => graph.AddNode(new Vector2(100, 100)), "빈 노드를 추가하고 상세 패널에서 스킬을 연결합니다."));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton("전체 보기", () => graph.FrameAll(), "모든 노드가 보이도록 화면을 맞춥니다."));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton("검증", Validate, "식별자, 순환 연결, 요구 레벨과 비용을 검사합니다."));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton("저장", () => { AssetDatabase.SaveAssets(); Validate(); }, "변경된 에셋을 저장합니다."));
            rootVisualElement.Add(toolbar);
            TwoPaneSplitView outer = new(0, 230, TwoPaneSplitViewOrientation.Horizontal);
            outer.style.flexGrow = 1;
            assetList = new SWGraphAssetListPanel("스킬트리", "새 트리 만들기", typeof(SWSkillTreeDefinition), CreateTree, value => SelectAsset(value as SWSkillTreeDefinition));
            outer.Add(assetList);
            TwoPaneSplitView inner = new(1, 320, TwoPaneSplitViewOrientation.Horizontal);
            graph = new SWSkillTreeGraphView(identifier => { selectedIdentifier = identifier; Repaint(); }, Validate);
            inner.Add(graph);
            IMGUIContainer inspector = new(DrawInspector);
            inspector.style.minWidth = 280;
            inspector.style.backgroundColor = new Color(0.149f, 0.157f, 0.173f);
            inner.Add(inspector);
            outer.Add(inner);
            rootVisualElement.Add(outer);
            status = new Label();
            status.style.height = 26;
            status.style.paddingLeft = 10;
            status.style.unityTextAlign = TextAnchor.MiddleLeft;
            rootVisualElement.Add(status);
            SelectAsset(definition);
        }

        private void SelectAsset(SWSkillTreeDefinition asset)
        {
            definition = asset;
            selectedIdentifier = null;
            if (graph == null) return;
            graph.Populate(definition);
            assetList.SelectAsset(definition);
            titleLabel.text = definition != null ? definition.name : "SW Skill Tree";
            titleLabel.tooltip = definition != null ? AssetDatabase.GetAssetPath(definition) : "새 트리를 만들거나 목록에서 선택하세요.";
            Validate();
            graph.schedule.Execute(() => graph.FrameAll());
        }
        private void OnUndo() { graph?.Populate(definition); Validate(); Repaint(); }
        private void Validate()
        {
            errors.Clear();
            if (definition != null) errors.AddRange(SWSkillTreeDefinitionValidator.Validate(definition));
            if (status != null) status.text = definition == null ? "새 트리를 만들거나 왼쪽 목록에서 선택하세요."
                : errors.Count == 0 ? $"{definition.Nodes.Count}개 노드 · 정의 검증 통과 · 출력에서 입력으로 드래그하여 연결"
                : $"수정할 설정 {errors.Count}개 · 오른쪽 패널에서 내용을 확인하세요.";
            Repaint();
        }

        private void DrawInspector()
        {
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            try
            {
                EditorGUILayout.Space(10);
                if (definition == null) { EditorGUILayout.HelpBox("트리를 선택하면 노드와 선행 조건을 편집할 수 있습니다.", MessageType.None); return; }
                foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Warning);
                SerializedObject serialized = new(definition);
                SerializedProperty array = serialized.FindProperty("nodes");
                SerializedProperty selected = null;
                for (int index = 0; index < array.arraySize; index++)
                    if (array.GetArrayElementAtIndex(index).FindPropertyRelative("identifier").stringValue == selectedIdentifier) selected = array.GetArrayElementAtIndex(index);
                if (selected == null)
                {
                    EditorGUILayout.LabelField("노드를 선택하세요", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("빈 곳의 오른쪽 클릭으로 노드를 추가합니다.\n출력 포트에서 다른 노드 입력 포트로 연결하세요.\n선택한 노드나 연결선은 Delete 키로 삭제합니다.", MessageType.None);
                    return;
                }
                SWEditorUtils.DrawHeader("노드 설정");
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(selected.FindPropertyRelative("identifier"), new GUIContent("저장 식별자"));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("skill"), new GUIContent("스킬 정의"));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("position"), new GUIContent("배치 위치"));
                if (EditorGUI.EndChangeCheck()) selected.FindPropertyRelative("hasSavedPosition").boolValue = true;
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("hasSavedPosition"), new GUIContent("저장 위치 사용", "자동 배치에서도 이 좌표를 우선 사용합니다. 해제하면 자동 배치로 계산합니다."));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("requirementMode"), new GUIContent("선행 조건 방식"));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("requirements"), new GUIContent("선행 노드"), true);
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("exclusiveGroup"), new GUIContent("상호 배타 그룹"));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("retainOnReset"), new GUIContent("진행 초기화 시 유지", "유지 설정을 사용하는 진행 초기화에서 이 노드의 습득 레벨을 보존합니다."));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("revealPolicy"), new GUIContent("정보 공개 조건"));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("concealment"), new GUIContent("공개 전 표시"));
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("visibilityConditions"), new GUIContent("표시 조건"), true);
                EditorGUILayout.PropertyField(selected.FindPropertyRelative("purchaseConditions"), new GUIContent("구매 조건"), true);
                bool modified = EditorGUI.EndChangeCheck();
                SWSkillDefinition skill = selected.FindPropertyRelative("skill").objectReferenceValue as SWSkillDefinition;
                if (modified) { serialized.ApplyModifiedProperties(); graph.Populate(definition); Validate(); }
                if (skill == null)
                {
                    if (GUILayout.Button("새 스킬 에셋 만들어 연결")) CreateSkill(selectedIdentifier);
                    return;
                }
                EditorGUILayout.Space(14);
                SWEditorUtils.DrawHeader("공유 스킬 정의");
                EditorGUILayout.LabelField("이 스킬을 사용하는 다른 노드에도 적용됩니다.", EditorStyles.wordWrappedMiniLabel);
                Editor.CreateCachedEditor(skill, null, ref skillEditor);
                EditorGUI.BeginChangeCheck();
                skillEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck()) { graph.Populate(definition); Validate(); }
            }
            finally { EditorGUILayout.EndScrollView(); }
        }

        private void CreateTree()
        {
            string path = EditorUtility.SaveFilePanelInProject("스킬트리 만들기", "SWSkillTree", "asset", "트리를 저장할 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path)) return;
            SWSkillTreeDefinition tree = CreateInstance<SWSkillTreeDefinition>();
            AssetDatabase.CreateAsset(tree, path);
            Undo.RegisterCreatedObjectUndo(tree, "스킬트리 생성");
            AssetDatabase.SaveAssets();
            assetList.Refresh();
            SelectAsset(tree);
        }
        private void CreateSkill(string identifier)
        {
            string path = EditorUtility.SaveFilePanelInProject("스킬 만들기", "SWSkill", "asset", "스킬을 저장할 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path)) return;
            SWSkillDefinition skill = CreateInstance<SWSkillDefinition>();
            AssetDatabase.CreateAsset(skill, path);
            Undo.RegisterCreatedObjectUndo(skill, "스킬 생성");
            SerializedObject serialized = new(definition);
            SerializedProperty array = serialized.FindProperty("nodes");
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty node = array.GetArrayElementAtIndex(index);
                if (node.FindPropertyRelative("identifier").stringValue == identifier) node.FindPropertyRelative("skill").objectReferenceValue = skill;
            }
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            graph.Populate(definition);
            Validate();
        }
    }
}
