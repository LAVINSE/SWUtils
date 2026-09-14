using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SW.BehaviourTree;

namespace SW.EditorTools.Behaviour
{
    /// <summary>Behaviour Tree Runner와 Blackboard Override를 키 중심으로 편집합니다.</summary>
    [CustomEditor(typeof(SWBehaviourTreeRunner))]
    internal sealed class SWBehaviourTreeRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty treeAssetProperty;
        private SerializedProperty runOnEnableProperty;
        private SerializedProperty overridesProperty;

        private void OnEnable()
        {
            treeAssetProperty = serializedObject.FindProperty("treeAsset");
            runOnEnableProperty = serializedObject.FindProperty("runOnEnable");
            overridesProperty = serializedObject.FindProperty("blackboardOverrides");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(treeAssetProperty, new GUIContent("Tree Asset"));
            EditorGUILayout.PropertyField(runOnEnableProperty, new GUIContent("Run On Enable"));
            EditorGUILayout.Space(7f);
            DrawOverrides(treeAssetProperty.objectReferenceValue as SWBehaviourTreeAsset);
            serializedObject.ApplyModifiedProperties();
            DrawRuntimeControls();
        }

        private void DrawOverrides(SWBehaviourTreeAsset treeAsset)
        {
            EditorGUILayout.LabelField("Blackboard Overrides", EditorStyles.boldLabel);
            if (treeAsset == null)
            {
                EditorGUILayout.HelpBox("Tree Asset을 선택하면 Override Key를 추가할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            for (int index = 0; index < overridesProperty.arraySize; index++)
                DrawOverride(index, treeAsset);

            using (new EditorGUI.DisabledScope(GetSupportedEntries(treeAsset).Count == 0))
            {
                if (GUILayout.Button("Add Override"))
                    ShowAddOverrideMenu(treeAsset);
            }
        }

        private void DrawOverride(int index, SWBehaviourTreeAsset treeAsset)
        {
            SerializedProperty element = overridesProperty.GetArrayElementAtIndex(index);
            SerializedProperty enabledProperty = element.FindPropertyRelative("enabled");
            SerializedProperty keyNameProperty = element.FindPropertyRelative("keyName");
            SerializedProperty valueTypeProperty = element.FindPropertyRelative("valueType");
            List<SWBehaviourBlackboardEntry> entries = GetSupportedEntries(treeAsset);
            string[] names = entries.ConvertAll(entry => entry.Name).ToArray();
            int selectedIndex = Mathf.Max(0, entries.FindIndex(entry =>
                entry.Name == keyNameProperty.stringValue));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            enabledProperty.boolValue = EditorGUILayout.Toggle(enabledProperty.boolValue,
                GUILayout.Width(18f));
            if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("지원하는 Blackboard Key 없음");
            }
            else
            {
                int changedIndex = EditorGUILayout.Popup(selectedIndex, names);
                SWBehaviourBlackboardEntry entry = entries[changedIndex];
                bool keyChanged = keyNameProperty.stringValue != entry.Name;
                keyNameProperty.stringValue = entry.Name;
                valueTypeProperty.enumValueIndex = (int)entry.ValueType;
                if (keyChanged)
                    CopyDefaultValue(element, entry);
            }
            if (GUILayout.Button("−", GUILayout.Width(26f)))
            {
                overridesProperty.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!enabledProperty.boolValue))
            {
                SWBehaviourBlackboardValueType valueType =
                    (SWBehaviourBlackboardValueType)valueTypeProperty.enumValueIndex;
                SerializedProperty valueProperty = valueType == SWBehaviourBlackboardValueType.Custom
                    ? element.FindPropertyRelative("customValue")?.FindPropertyRelative("value")
                    : GetValueProperty(element, valueType);
                if (valueProperty != null)
                    EditorGUILayout.PropertyField(valueProperty, new GUIContent("Value"));
                else if (valueType == SWBehaviourBlackboardValueType.Custom)
                    EditorGUILayout.HelpBox("사용자 Key 값을 다시 선택해 초기화하세요.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        private void ShowAddOverrideMenu(SWBehaviourTreeAsset treeAsset)
        {
            GenericMenu menu = new();
            List<SWBehaviourBlackboardEntry> entries = GetSupportedEntries(treeAsset);
            for (int index = 0; index < entries.Count; index++)
            {
                SWBehaviourBlackboardEntry capturedEntry = entries[index];
                menu.AddItem(new GUIContent(capturedEntry.Name), false, () =>
                {
                    serializedObject.Update();
                    int newIndex = overridesProperty.arraySize;
                    overridesProperty.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty element = overridesProperty.GetArrayElementAtIndex(newIndex);
                    element.FindPropertyRelative("enabled").boolValue = true;
                    element.FindPropertyRelative("keyName").stringValue = capturedEntry.Name;
                    element.FindPropertyRelative("valueType").enumValueIndex =
                        (int)capturedEntry.ValueType;
                    CopyDefaultValue(element, capturedEntry);
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                });
            }
            menu.ShowAsContext();
        }

        private void DrawRuntimeControls()
        {
            if (!Application.isPlaying)
                return;
            SWBehaviourTreeRunner runner = (SWBehaviourTreeRunner)target;
            EditorGUILayout.Space(7f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Tree")) runner.StartTree();
            if (GUILayout.Button("Stop Tree")) runner.StopTree();
            EditorGUILayout.EndHorizontal();
        }

        private static List<SWBehaviourBlackboardEntry> GetSupportedEntries(
            SWBehaviourTreeAsset treeAsset)
        {
            List<SWBehaviourBlackboardEntry> result = new();
            IReadOnlyList<SWBehaviourBlackboardEntry> entries = treeAsset.Blackboard.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null)
                    result.Add(entries[index]);
            }
            return result;
        }

        private static SerializedProperty GetValueProperty(
            SerializedProperty element,
            SWBehaviourBlackboardValueType valueType)
        {
            return element.FindPropertyRelative(valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => "booleanValue",
                SWBehaviourBlackboardValueType.Integer => "integerValue",
                SWBehaviourBlackboardValueType.Float => "floatValue",
                SWBehaviourBlackboardValueType.String => "stringValue",
                SWBehaviourBlackboardValueType.Vector2 => "vector2Value",
                SWBehaviourBlackboardValueType.Vector3 => "vector3Value",
                SWBehaviourBlackboardValueType.Object => "objectValue",
                _ => "stringValue",
            });
        }

        private static void CopyDefaultValue(
            SerializedProperty element,
            SWBehaviourBlackboardEntry entry)
        {
            SerializedProperty valueProperty = GetValueProperty(element, entry.ValueType);
            object value = entry.GetBoxedValue();
            if (entry.ValueType == SWBehaviourBlackboardValueType.Custom)
            {
                SWBehaviourBlackboardEntry copiedEntry =
                    Activator.CreateInstance(entry.GetType()) as SWBehaviourBlackboardEntry;
                if (copiedEntry != null)
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry), copiedEntry);
                element.FindPropertyRelative("customValue").managedReferenceValue = copiedEntry;
                return;
            }
            switch (entry.ValueType)
            {
                case SWBehaviourBlackboardValueType.Boolean:
                    valueProperty.boolValue = value is bool booleanValue && booleanValue;
                    break;
                case SWBehaviourBlackboardValueType.Integer:
                    valueProperty.intValue = value is int integerValue ? integerValue : 0;
                    break;
                case SWBehaviourBlackboardValueType.Float:
                    valueProperty.floatValue = value is float floatValue ? floatValue : 0f;
                    break;
                case SWBehaviourBlackboardValueType.String:
                    valueProperty.stringValue = value as string ?? string.Empty;
                    break;
                case SWBehaviourBlackboardValueType.Vector2:
                    valueProperty.vector2Value = value is Vector2 vector2Value
                        ? vector2Value : Vector2.zero;
                    break;
                case SWBehaviourBlackboardValueType.Vector3:
                    valueProperty.vector3Value = value is Vector3 vector3Value
                        ? vector3Value : Vector3.zero;
                    break;
                case SWBehaviourBlackboardValueType.Object:
                    valueProperty.objectReferenceValue = value as UnityEngine.Object;
                    break;
            }
        }
    }
}
