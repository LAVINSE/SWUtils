using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SW.Base;
using SW.SkillTree;
using SW.Stat;
using SW.EditorTools.Base;

namespace SW.Tests.SkillTree
{
    /// <summary>기존 식별 에셋의 기본 정의가 파생 설정과 분리된 접힌 그룹으로 표시되는지 검증합니다.</summary>
    public sealed class SWIdentifiedObjectInspectorTests
    {
        [TestCase(typeof(SWIdentifiedObject))]
        [TestCase(typeof(SWStat))]
        [TestCase(typeof(SWSkillDefinition))]
        public void BaseDefinitionIsCollapsedAndContainsOnlyBaseFields(Type assetType)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);
            Editor editor = Editor.CreateEditor(asset);
            try
            {
                VisualElement root = editor.CreateInspectorGUI();
                Foldout definition = root.Query<Foldout>().ToList().Single(foldout => foldout.text == "기본 정의");
                Assert.That(definition.value, Is.False);
                SWMonoBehaviourEditor inspector = (SWMonoBehaviourEditor)editor;
                CollectionAssert.AreEquivalent(new[] { "categories", "id", "codeName", "displayName", "description", "spriteIcon" },
                    inspector.GroupDataDict["기본 정의"].PropertiesList.Select(property => property.name).ToArray());
                Assert.That(editor.GetType().GetMethod("OnInspectorGUI").DeclaringType, Is.EqualTo(typeof(SWScriptableObjectEditor)));
                if (assetType == typeof(SWStat))
                    Assert.That(inspector.PropertiesList.Any(property => property.name == "defaultValue"), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(editor); UnityEngine.Object.DestroyImmediate(asset); }
        }
    }
}
