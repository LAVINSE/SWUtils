using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using SW.SkillTree;
using SW.Stat;

namespace SW.Samples.EditorTools
{
    /// <summary>기존 예제의 식별자와 배치를 유지하면서 넓은 채굴 성장 경로를 추가합니다.</summary>
    public static class SWSkillTreeLargeExampleBuilder
    {
        #region 예제 확장
        /// <summary>기본 9개 노드에 6개 성장 경로의 72개 노드를 추가합니다. 이미 있는 확장 노드는 유지합니다.</summary>
        public static void ExpandBundledExample()
        {
            string[] builders = AssetDatabase.FindAssets("SWSkillTreeExampleBuilder t:MonoScript");
            string script = Array.Find(builders, identifier => AssetDatabase.GUIDToAssetPath(identifier).EndsWith("/SWSkillTreeExampleBuilder.cs", StringComparison.Ordinal));
            if (script == null) throw new InvalidOperationException("예제 생성기 스크립트를 찾을 수 없습니다.");
            string samples = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(script)))?.Replace('\\', '/');
            string directory = samples + "/Data/SkillTree";
            SWSkillTreeDefinition tree = AssetDatabase.LoadAssetAtPath<SWSkillTreeDefinition>(directory + "/MiningSkillTree.asset");
            SWStat mining = AssetDatabase.LoadAssetAtPath<SWStat>(directory + "/MiningPower.asset");
            if (tree == null || mining == null) throw new InvalidOperationException("기본 채굴 트리와 능력치가 필요합니다.");
            string[] branches = { "Excavation", "Engineering", "Surveying", "Logistics", "Geology", "Automation" };
            string[] parents = { "mining-node-2", "mining-node-3", "mining-node-4", "mining-node-7", "mining-node-8", "mining-node-9" };
            string[] tiers = { "I", "II", "III", "IV" };
            HashSet<string> existing = new(StringComparer.Ordinal);
            foreach (SWSkillTreeNode node in tree.Nodes) existing.Add(node.Identifier);
            foreach (string parent in parents)
                if (!existing.Contains(parent)) throw new InvalidOperationException("확장의 시작 노드가 없습니다: " + parent);
            for (int branch = 0; branch < branches.Length; branch++)
            {
                float angle = (-150 + branch * 60) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 perpendicular = new(-direction.y, direction.x);
                SWSkillTreeStatEffect effect = LoadOrCreate<SWSkillTreeStatEffect>($"{directory}/ExpansionEffect{branch + 1}.asset");
                Edit(effect, serialized =>
                {
                    serialized.FindProperty("stat").objectReferenceValue = mining;
                    serialized.FindProperty("amountPerLevel").floatValue = branch + 1;
                });
                for (int tier = 0; tier < tiers.Length; tier++)
                {
                    string prefix = $"mining-expansion-{branch + 1}-{tier + 1}";
                    string previous = $"mining-expansion-{branch + 1}-{tier}";
                    Vector2 center = direction * (1000 + tier * 640);
                    for (int choice = 0; choice < 3; choice++)
                    {
                        string identifier = prefix + "-" + choice;
                        if (existing.Contains(identifier)) continue;
                        string label = choice == 0 ? "Core" : choice == 1 ? "Efficiency" : "Breakthrough";
                        SWSkillDefinition skill = LoadOrCreate<SWSkillDefinition>($"{directory}/ExpansionSkill{branch + 1}_{tier + 1}_{choice}.asset");
                        ConfigureSkill(skill, $"{branches[branch]} {label} {tiers[tier]}", effect, branch, tier, choice);
                        Vector2 position = choice == 0 ? center : center + direction * 280 + perpendicular * (choice == 1 ? -240 : 240);
                        Edit(tree, serialized =>
                        {
                            SerializedProperty nodes = serialized.FindProperty("nodes");
                            SerializedProperty node = nodes.GetArrayElementAtIndex(nodes.arraySize++);
                            node.FindPropertyRelative("identifier").stringValue = identifier;
                            node.FindPropertyRelative("skill").objectReferenceValue = skill;
                            node.FindPropertyRelative("position").vector2Value = position;
                            node.FindPropertyRelative("hasSavedPosition").boolValue = true;
                            node.FindPropertyRelative("revealPolicy").enumValueIndex = (int)SWSkillTreeRevealPolicy.PrerequisiteLearned;
                            node.FindPropertyRelative("concealment").enumValueIndex = (int)SWSkillTreeConcealment.Hidden;
                            node.FindPropertyRelative("retainOnReset").boolValue = false;
                            node.FindPropertyRelative("exclusiveGroup").stringValue = choice == 0 ? string.Empty : prefix + "-choice";
                            node.FindPropertyRelative("visibilityConditions").ClearArray();
                            node.FindPropertyRelative("purchaseConditions").ClearArray();
                            SerializedProperty requirements = node.FindPropertyRelative("requirements");
                            requirements.ClearArray();
                            if (choice == 0 && tier > 0)
                            {
                                AddRequirement(requirements, previous + "-1", 1);
                                AddRequirement(requirements, previous + "-2", 1);
                                node.FindPropertyRelative("requirementMode").enumValueIndex = (int)SWSkillTreeRequirementMode.Any;
                            }
                            else
                            {
                                AddRequirement(requirements, choice == 0 ? parents[branch] : prefix + "-0", choice == 0 ? 1 : 3);
                                node.FindPropertyRelative("requirementMode").enumValueIndex = (int)SWSkillTreeRequirementMode.All;
                            }
                        });
                        existing.Add(identifier);
                    }
                }
            }
            List<string> errors = SWSkillTreeDefinitionValidator.Validate(tree);
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            AssetDatabase.SaveAssets();
            SWSkillTreeExampleBuilder.RebuildBundledExample();
        }
        #endregion // 예제 확장

        #region 에셋 구성
        private static void ConfigureSkill(SWSkillDefinition skill, string name, SWSkillTreeStatEffect effect, int branch, int tier, int choice)
        {
            Edit(skill, serialized =>
            {
                serialized.FindProperty("displayName").stringValue = name;
                serialized.FindProperty("description").stringValue = choice == 0
                    ? "Upgrade this core to level 3 to unlock a specialization. Complete either specialization to reveal the next tier."
                    : "Choose Efficiency for many smaller upgrades or Breakthrough for fewer upgrades. Refund this path before switching specializations.";
                serialized.FindProperty("maximumLevel").intValue = choice == 0 ? 10 : choice == 1 ? 25 : 5;
                SerializedProperty costs = serialized.FindProperty("costs");
                costs.arraySize = tier == 0 ? 1 : 2;
                for (int index = 0; index < costs.arraySize; index++) costs.GetArrayElementAtIndex(index).managedReferenceValue = new SWSkillTreeFormulaCost();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                costs = serialized.FindProperty("costs");
                costs.GetArrayElementAtIndex(0).FindPropertyRelative("initialCost").doubleValue = (100 + branch * 25) * Math.Pow(2.5, tier) * (choice + 1);
                costs.GetArrayElementAtIndex(0).FindPropertyRelative("growthValue").doubleValue = choice == 1 ? 1.12 : 1.2;
                if (costs.arraySize > 1)
                {
                    costs.GetArrayElementAtIndex(1).FindPropertyRelative("currency").stringValue = "Research";
                    costs.GetArrayElementAtIndex(1).FindPropertyRelative("initialCost").doubleValue = tier;
                    costs.GetArrayElementAtIndex(1).FindPropertyRelative("growth").enumValueIndex = 0;
                }
                SerializedProperty effects = serialized.FindProperty("effects");
                effects.arraySize = 1;
                effects.GetArrayElementAtIndex(0).objectReferenceValue = effect;
            });
        }

        private static void AddRequirement(SerializedProperty requirements, string identifier, int level)
        {
            SerializedProperty requirement = requirements.GetArrayElementAtIndex(requirements.arraySize++);
            requirement.FindPropertyRelative("nodeIdentifier").stringValue = identifier;
            requirement.FindPropertyRelative("requiredLevel").intValue = level;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void Edit(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            SerializedObject serialized = new(target);
            edit(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion // 에셋 구성
    }
}
