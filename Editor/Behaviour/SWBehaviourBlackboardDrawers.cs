using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using SW.BehaviourTree;

namespace SW.EditorTools.Behaviour
{
    /// <summary>Blackboard Key 문자열을 현재 Tree의 Key 선택 목록으로 표시합니다.</summary>
    [CustomPropertyDrawer(typeof(SWBehaviourBlackboardKeySelectorAttribute))]
    internal sealed class SWBehaviourBlackboardKeySelectorDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            List<string> choices = new() { string.Empty };
            if (property.serializedObject.targetObject is SWBehaviourTreeAsset treeAsset)
            {
                for (int index = 0; index < treeAsset.Blackboard.Entries.Count; index++)
                {
                    SWBehaviourBlackboardEntry entry = treeAsset.Blackboard.Entries[index];
                    if (entry != null)
                        choices.Add(entry.Name);
                }
            }
            if (!string.IsNullOrWhiteSpace(property.stringValue) &&
                !choices.Contains(property.stringValue))
                choices.Add(property.stringValue);
            DropdownField field = new(property.displayName, choices,
                Math.Max(0, choices.IndexOf(property.stringValue)));
            field.RegisterValueChangedCallback(changeEvent =>
            {
                property.stringValue = changeEvent.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }
    }

    /// <summary>범용 Blackboard 고정값에서 선택한 타입에 해당하는 값만 표시합니다.</summary>
    [CustomPropertyDrawer(typeof(SWBehaviourBlackboardValue))]
    internal sealed class SWBehaviourBlackboardValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            Label title = new(property.displayName);
            title.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            root.Add(title);
            SerializedProperty valueType = property.FindPropertyRelative("valueType");
            EnumField typeField = new("Type",
                (SWBehaviourBlackboardValueType)valueType.enumValueIndex);
            root.Add(typeField);
            VisualElement valueContainer = new();
            root.Add(valueContainer);

            void RebuildValue()
            {
                valueContainer.Clear();
                SWBehaviourBlackboardValueType selectedType =
                    (SWBehaviourBlackboardValueType)valueType.enumValueIndex;
                if (selectedType == SWBehaviourBlackboardValueType.Custom)
                    AddCustomValue(property, valueContainer);
                else
                    valueContainer.Add(new PropertyField(property.FindPropertyRelative(
                        GetValuePropertyName(selectedType)), "Value"));
            }

            typeField.RegisterValueChangedCallback(changeEvent =>
            {
                valueType.enumValueIndex = (int)(SWBehaviourBlackboardValueType)changeEvent.newValue;
                property.serializedObject.ApplyModifiedProperties();
                RebuildValue();
            });
            RebuildValue();
            return root;
        }

        private static void AddCustomValue(
            SerializedProperty property,
            VisualElement container)
        {
            SerializedProperty customValue = property.FindPropertyRelative("customValue");
            List<Type> types = GetCustomEntryTypes();
            List<string> names = new() { "None" };
            int selectedIndex = 0;
            Type currentType = customValue.managedReferenceValue?.GetType();
            for (int index = 0; index < types.Count; index++)
            {
                names.Add(ObjectNames.NicifyVariableName(types[index].Name));
                if (types[index] == currentType)
                    selectedIndex = index + 1;
            }
            DropdownField typeSelector = new("Custom Type", names, selectedIndex);
            container.Add(typeSelector);
            if (customValue.managedReferenceValue != null)
            {
                SerializedProperty customProperty = customValue.FindPropertyRelative("value");
                if (customProperty != null)
                    container.Add(new PropertyField(customProperty, "Value"));
            }
            typeSelector.RegisterValueChangedCallback(changeEvent =>
            {
                int changedIndex = names.IndexOf(changeEvent.newValue) - 1;
                customValue.managedReferenceValue = changedIndex < 0
                    ? null
                    : Activator.CreateInstance(types[changedIndex]);
                property.serializedObject.ApplyModifiedProperties();
                container.schedule.Execute(() =>
                {
                    container.Clear();
                    AddCustomValue(property, container);
                });
            });
        }

        private static List<Type> GetCustomEntryTypes()
        {
            List<Type> result = new();
            TypeCache.TypeCollection types =
                TypeCache.GetTypesDerivedFrom<SWBehaviourBlackboardEntry>();
            foreach (Type type in types)
            {
                if (!type.IsAbstract && !type.IsGenericType &&
                    type != typeof(SWBehaviourBlackboardEntry) &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                    result.Add(type);
            }
            result.Sort((left, right) =>
                string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
            return result;
        }

        private static string GetValuePropertyName(SWBehaviourBlackboardValueType valueType)
        {
            return valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => "booleanValue",
                SWBehaviourBlackboardValueType.Integer => "integerValue",
                SWBehaviourBlackboardValueType.Float => "floatValue",
                SWBehaviourBlackboardValueType.String => "stringValue",
                SWBehaviourBlackboardValueType.Vector2 => "vector2Value",
                SWBehaviourBlackboardValueType.Vector3 => "vector3Value",
                SWBehaviourBlackboardValueType.Object => "objectValue",
                _ => "stringValue",
            };
        }
    }
}
