using System.Linq;
using UnityEditor;
using UnityEngine;
using SW.SkillTree;

namespace SW.EditorTools.SkillTree
{
    /// <summary>선행 노드의 식별자 대신 스킬 이름과 요구 레벨을 편집합니다.</summary>
    [CustomPropertyDrawer(typeof(SWSkillTreeRequirement))]
    public sealed class SWSkillTreeRequirementDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SWSkillTreeDefinition tree = property.serializedObject.targetObject as SWSkillTreeDefinition;
            if (tree == null)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("nodeIdentifier"), label);
                EditorGUI.EndProperty();
                return;
            }
            SerializedProperty identifier = property.FindPropertyRelative("nodeIdentifier");
            SerializedProperty level = property.FindPropertyRelative("requiredLevel");
            string[] identifiers = tree.Nodes.Where(node => node != null).Select(node => node.Identifier).ToArray();
            string[] names = tree.Nodes.Where(node => node != null).Select((node, index) => $"{index + 1}. {(node.Skill != null ? node.Skill.DisplayName : "스킬 미연결")}").ToArray();
            int current = System.Array.IndexOf(identifiers, identifier.stringValue);
            Rect field = EditorGUI.PrefixLabel(position, label);
            Rect choice = new(field.x, field.y, Mathf.Max(40, field.width - 58), field.height);
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(choice, current, names);
            if (EditorGUI.EndChangeCheck() && selected >= 0) identifier.stringValue = identifiers[selected];
            Rect number = new(field.xMax - 52, field.y, 52, field.height);
            level.intValue = EditorGUI.IntField(number, level.intValue);
            EditorGUI.EndProperty();
        }
    }
}
