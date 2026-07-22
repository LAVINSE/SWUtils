using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using SW.BehaviourTree;

namespace SW.EditorTools.Behaviour
{
    /// <summary>NodeProperty를 Blackboard Key 또는 고정값 선택 형태로 표시합니다.</summary>
    [CustomPropertyDrawer(typeof(SWBehaviourNodeProperty<>), true)]
    internal sealed class SWBehaviourNodePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            root.style.marginTop = 2f;
            Label label = new(property.displayName);
            label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            root.Add(label);

            SerializedProperty useBlackboard = property.FindPropertyRelative("useBlackboard");
            SerializedProperty keyName = property.FindPropertyRelative("keyName");
            SerializedProperty fixedValue = property.FindPropertyRelative("value");
            Toggle sourceToggle = new("Use Blackboard") { value = useBlackboard.boolValue };
            root.Add(sourceToggle);

            List<string> choices = GetCompatibleKeyNames(property.serializedObject.targetObject);
            if (!string.IsNullOrWhiteSpace(keyName.stringValue) && !choices.Contains(keyName.stringValue))
                choices.Add(keyName.stringValue);
            int selectedIndex = Math.Max(0, choices.IndexOf(keyName.stringValue));
            DropdownField keyField = new("Key", choices, selectedIndex);
            PropertyField valueField = new(fixedValue, "Value");
            root.Add(keyField);
            root.Add(valueField);

            void RefreshVisibility(bool useKey)
            {
                keyField.style.display = useKey ? DisplayStyle.Flex : DisplayStyle.None;
                valueField.style.display = useKey ? DisplayStyle.None : DisplayStyle.Flex;
            }
            RefreshVisibility(useBlackboard.boolValue);
            sourceToggle.RegisterValueChangedCallback(changeEvent =>
            {
                useBlackboard.boolValue = changeEvent.newValue;
                property.serializedObject.ApplyModifiedProperties();
                RefreshVisibility(changeEvent.newValue);
            });
            keyField.RegisterValueChangedCallback(changeEvent =>
            {
                keyName.stringValue = changeEvent.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });
            return root;
        }

        private List<string> GetCompatibleKeyNames(UnityEngine.Object targetObject)
        {
            List<string> names = new() { string.Empty };
            if (targetObject is not SWBehaviourTreeAsset treeAsset)
                return names;
            Type valueType = fieldInfo?.FieldType.IsGenericType == true
                ? fieldInfo.FieldType.GetGenericArguments()[0]
                : null;
            for (int index = 0; index < treeAsset.Blackboard.Entries.Count; index++)
            {
                SWBehaviourBlackboardEntry entry = treeAsset.Blackboard.Entries[index];
                if (entry != null &&
                    (valueType == null || valueType.IsAssignableFrom(entry.SystemValueType)))
                    names.Add(entry.Name);
            }
            return names;
        }
    }
}
