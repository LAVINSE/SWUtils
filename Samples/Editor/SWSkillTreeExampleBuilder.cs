using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SW.SkillTree;
using SW.Stat;

namespace SW.Samples.EditorTools
{
    /// <summary>실행 가능한 스킬트리 예제 에셋과 화면 프리팹을 생성합니다.</summary>
    public static class SWSkillTreeExampleBuilder
    {
        /// <summary>사용자 프로젝트에 수정 가능한 예제를 새 폴더로 생성합니다.</summary>
        public static void CreateExample()
        {
            string directory = AssetDatabase.GenerateUniqueAssetPath("Assets/SWUtilsSkillTreeExample");
            GameObject prefab = CreateAssets(directory);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        /// <summary>지정 폴더에 기본 화면과 채굴 성장 예제를 생성합니다. 기존 폴더를 덮어쓰지 않습니다.</summary>
        public static GameObject CreateAssets(string directory)
        {
            if (AssetDatabase.IsValidFolder(directory)) throw new InvalidOperationException("새 예제 폴더를 지정하세요.");
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
            SWStat mining = SaveAsset<SWStat>(directory + "/MiningPower.asset");
            Edit(mining, serialized =>
            {
                serialized.FindProperty("displayName").stringValue = "Mining Power";
                serialized.FindProperty("maxValue").floatValue = 1000000;
                serialized.FindProperty("defaultValue").floatValue = 1;
            });
            SWSkillTreeStatEffect miningEffect = SaveAsset<SWSkillTreeStatEffect>(directory + "/MiningEffect.asset");
            Edit(miningEffect, serialized => { serialized.FindProperty("stat").objectReferenceValue = mining; serialized.FindProperty("amountPerLevel").floatValue = 1; });
            SWSkillTreeFeatureEffect automatic = SaveAsset<SWSkillTreeFeatureEffect>(directory + "/AutomaticMiningEffect.asset");
            string[] names = { "Start Mining", "Mining Power", "Unlock Research", "Auto Mining", "Precision Mining", "Bulk Mining", "Mining Speed", "Permanent Knowledge", "Deep Exploration" };
            Vector2[] positions = { new(0, 0), new(-240, 170), new(240, 170), new(-240, 340), new(0, 510), new(-480, 510), new(-240, 680), new(480, 0), new(240, 510) };
            int[] maximumLevels = { 1, 25, 1, 1, 10, 10, 50, 5, 20 };
            SWSkillDefinition[] skills = new SWSkillDefinition[names.Length];
            for (int index = 0; index < names.Length; index++)
            {
                int current = index;
                skills[index] = SaveAsset<SWSkillDefinition>($"{directory}/Skill{index + 1}.asset");
                Edit(skills[index], serialized =>
                {
                    serialized.FindProperty("displayName").stringValue = names[current];
                    serialized.FindProperty("description").stringValue = current == 4 || current == 5 ? "Choose one mining method. Refund the branch to switch methods."
                        : current == 7 ? "An independent upgrade retained when progress is reset." : "Purchase levels to improve mining. Edit costs and effects in the skill tree editor.";
                    serialized.FindProperty("maximumLevel").intValue = maximumLevels[current];
                    SerializedProperty costs = serialized.FindProperty("costs");
                    costs.arraySize = current == 8 ? 2 : 1;
                    for (int costIndex = 0; costIndex < costs.arraySize; costIndex++)
                    {
                        costs.GetArrayElementAtIndex(costIndex).managedReferenceValue = new SWSkillTreeFormulaCost();
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    serialized.Update();
                    costs = serialized.FindProperty("costs");
                    costs.GetArrayElementAtIndex(0).FindPropertyRelative("initialCost").doubleValue = current == 0 ? 10 : 25 * (current + 1);
                    costs.GetArrayElementAtIndex(0).FindPropertyRelative("growthValue").doubleValue = 1.2;
                    if (costs.arraySize > 1)
                    {
                        costs.GetArrayElementAtIndex(1).FindPropertyRelative("currency").stringValue = "Research";
                        costs.GetArrayElementAtIndex(1).FindPropertyRelative("initialCost").doubleValue = 1;
                        costs.GetArrayElementAtIndex(1).FindPropertyRelative("growth").enumValueIndex = 0;
                    }
                    SerializedProperty effects = serialized.FindProperty("effects");
                    effects.arraySize = 1;
                    effects.GetArrayElementAtIndex(0).objectReferenceValue = current == 3 || current == 6 ? automatic : miningEffect;
                });
            }
            SWSkillTreeDefinition tree = SaveAsset<SWSkillTreeDefinition>(directory + "/MiningSkillTree.asset");
            Edit(tree, serialized =>
            {
                SerializedProperty nodes = serialized.FindProperty("nodes");
                nodes.arraySize = names.Length;
                for (int index = 0; index < names.Length; index++)
                {
                    SerializedProperty node = nodes.GetArrayElementAtIndex(index);
                    node.FindPropertyRelative("identifier").stringValue = "mining-node-" + (index + 1);
                    node.FindPropertyRelative("skill").objectReferenceValue = skills[index];
                    node.FindPropertyRelative("position").vector2Value = positions[index];
                    node.FindPropertyRelative("requirements").ClearArray();
                    node.FindPropertyRelative("visibilityConditions").ClearArray();
                    node.FindPropertyRelative("purchaseConditions").ClearArray();
                    node.FindPropertyRelative("exclusiveGroup").stringValue = index == 4 || index == 5 ? "MiningMethod" : string.Empty;
                    node.FindPropertyRelative("retainOnReset").boolValue = index == 7;
                }
                AddRequirement(nodes, 1, 0, 1);
                AddRequirement(nodes, 2, 0, 1);
                AddRequirement(nodes, 3, 1, 3);
                AddRequirement(nodes, 4, 3, 1);
                AddRequirement(nodes, 5, 3, 1);
                AddRequirement(nodes, 6, 4, 1);
                AddRequirement(nodes, 6, 5, 1);
                nodes.GetArrayElementAtIndex(6).FindPropertyRelative("requirementMode").enumValueIndex = 1;
                AddRequirement(nodes, 8, 2, 1);
                AddRequirement(nodes, 8, 4, 1);
            });
            return CreatePrefab(directory, tree, mining);
        }

        /// <summary>패키지에 포함된 채굴 트리를 사용하여 완성된 예제 화면을 저장합니다. Unity 명령줄에서도 호출할 수 있습니다.</summary>
        public static void RebuildBundledExample()
        {
            string[] builders = AssetDatabase.FindAssets("SWSkillTreeExampleBuilder t:MonoScript");
            if (builders.Length != 1) throw new InvalidOperationException("예제 생성기 스크립트를 하나만 포함하세요.");
            string samples = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(builders[0])))?.Replace('\\', '/');
            string data = samples + "/Data/SkillTree";
            SWSkillTreeDefinition tree = AssetDatabase.LoadAssetAtPath<SWSkillTreeDefinition>(data + "/MiningSkillTree.asset");
            SWStat mining = AssetDatabase.LoadAssetAtPath<SWStat>(data + "/MiningPower.asset");
            if (tree == null || mining == null) throw new InvalidOperationException("기본 채굴 트리 에셋을 찾을 수 없습니다.");
            if (TMP_Settings.defaultFontAsset == null)
                throw new InvalidOperationException("프로젝트의 TextMeshPro 기본 글꼴을 먼저 설정하세요. SWUtils에는 글꼴 데이터를 포함하지 않습니다.");
            GameObject prefab = CreatePrefab(samples + "/Prefab", tree, mining);
            ValidatePrefab(prefab, tree);
            Debug.Log($"[SWSkillTreeExampleBuilder] 완성: {AssetDatabase.GetAssetPath(prefab)} / {tree.Nodes.Count}개 노드");
        }

        private static GameObject CreatePrefab(string directory, SWSkillTreeDefinition tree, SWStat mining)
        {
            GameObject root = new("SWSkillTreeExample", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1280, 800);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 0.5f;
            SWStats stats = root.AddComponent<SWStats>();
            Edit(stats, serialized =>
            {
                SerializedProperty overrides = serialized.FindProperty("statOverrides");
                overrides.arraySize = 1;
                overrides.GetArrayElementAtIndex(0).FindPropertyRelative("stat").objectReferenceValue = mining;
            });
            SWSkillTreeExample example = root.AddComponent<SWSkillTreeExample>();
            RectTransform viewRectangle = SWSkillTreeViewFactory.CreateRect("SkillTreeView", root.transform);
            SWSkillTreeViewFactory.Stretch(viewRectangle, 0, 30, 0, 60);
            SWSkillTreeView view = viewRectangle.gameObject.AddComponent<SWSkillTreeView>();
            Edit(view, serialized =>
            {
                serialized.FindProperty("focusStartOnBind").boolValue = true;
                serialized.FindProperty("initialZoom").floatValue = 1f;
                serialized.FindProperty("showAllNodesInEditor").boolValue = true;
            });
            view.BuildDefaultLayout();
            example.BuildControls(root.transform);
            Edit(example, serialized =>
            {
                serialized.FindProperty("definition").objectReferenceValue = tree;
                serialized.FindProperty("view").objectReferenceValue = view;
                serialized.FindProperty("stats").objectReferenceValue = stats;
            });
            // 실행 화면과 별도로 외형을 수정할 수 있는 기본 노드 프리팹도 제공합니다.
            RectTransform nodeRectangle = SWSkillTreeViewFactory.CreateRect("SWSkillTreeNode", null);
            nodeRectangle.sizeDelta = view.NodeSize;
            SWSkillTreeNodeView nodeView = nodeRectangle.gameObject.AddComponent<SWSkillTreeNodeView>();
            nodeView.Build();
            GameObject nodePrefab = PrefabUtility.SaveAsPrefabAsset(nodeRectangle.gameObject, directory + "/SWSkillTreeNode.prefab");
            UnityEngine.Object.DestroyImmediate(nodeRectangle.gameObject);
            Edit(view, serialized => serialized.FindProperty("nodePrefab").objectReferenceValue = nodePrefab.GetComponent<SWSkillTreeNodeView>());
            SWSkillTreeWallet previewWallet = new();
            previewWallet.SetBalance("Gold", 250);
            previewWallet.SetBalance("Research", 5);
            using SWSkillTreeSystem preview = new(tree, previewWallet);
            view.Bind(preview);
            view.ArrangeAutomatically();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, directory + "/SWSkillTreeExample.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static void ValidatePrefab(GameObject prefab, SWSkillTreeDefinition tree)
        {
            if (prefab == null || prefab.GetComponentsInChildren<SWSkillTreeNodeView>(true).Length != tree.Nodes.Count)
                throw new InvalidOperationException("예제 노드가 모두 저장되지 않았습니다.");
            if (prefab.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length != 0)
                throw new InvalidOperationException("예제에 이전 텍스트 컴포넌트가 남아 있습니다.");
            if (prefab.GetComponentsInChildren<TextMeshProUGUI>(true).Length == 0)
                throw new InvalidOperationException("TextMeshPro 화면이 생성되지 않았습니다.");
            foreach (TextMeshProUGUI text in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.font == null) throw new InvalidOperationException($"글꼴이 연결되지 않았습니다: {text.name}");
            UnityEngine.SceneManagement.Scene previewScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
                SWSkillTreeWallet wallet = new();
                wallet.SetBalance("Gold", 250);
                wallet.SetBalance("Research", 5);
                using SWSkillTreeSystem system = new(tree, wallet);
                SWSkillTreeView view = instance.GetComponentInChildren<SWSkillTreeView>();
                view.Bind(system);
                view.RefreshView();
                view.Bind(system);
                view.RefreshView();
                if (instance.GetComponentsInChildren<SWSkillTreeNodeView>(true).Length != tree.Nodes.Count)
                    throw new InvalidOperationException("재연결 시 미리보기 노드가 중복되었습니다.");
                SWSkillTreePurchase purchase = system.Purchase(tree.Nodes[0].Identifier);
                if (!purchase.Success) throw new InvalidOperationException(purchase.Reason);
                view.RefreshView();
                SWSkillTreeExample example = instance.GetComponent<SWSkillTreeExample>();
                example.GenerateAndArrangeNodes();
                example.GenerateAndArrangeNodes();
                if (instance.GetComponentsInChildren<SWSkillTreeNodeView>(true).Length != tree.Nodes.Count)
                    throw new InvalidOperationException("자동 생성 버튼을 다시 실행하면 노드가 중복됩니다.");
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }
        private static T SaveAsset<T>(string path) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        private static void Edit(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            SerializedObject serialized = new(target);
            edit(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void AddRequirement(SerializedProperty nodes, int child, int parent, int level)
        {
            SerializedProperty requirements = nodes.GetArrayElementAtIndex(child).FindPropertyRelative("requirements");
            int index = requirements.arraySize++;
            SerializedProperty requirement = requirements.GetArrayElementAtIndex(index);
            requirement.FindPropertyRelative("nodeIdentifier").stringValue = "mining-node-" + (parent + 1);
            requirement.FindPropertyRelative("requiredLevel").intValue = level;
        }
    }
}
