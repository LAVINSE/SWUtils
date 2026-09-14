using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SW.Base;

namespace SW.EditorTools.Workspace
{
    /// <summary>SWUtils 데이터와 설치된 선택적 패키지를 작업 공간에 연결합니다.</summary>
    [InitializeOnLoad]
    internal static class SWEditorIntegration
    {
        private static readonly Dictionary<Type, MemberInfo> spriteMembers = new();
        static SWEditorIntegration()
        {
            RegisterGroup("SW.Quest");
            RegisterGroup("SW.Stat");
            RegisterGroup("SW.SkillTree");
            RegisterGroup("SW.BehaviourTree");
            RegisterGroup("SW.StateMachine");
            RegisterGroup("SW.Base", SWEditorCategoryDefaults.UtilityCategory);
            RegisterGroup("SW.Popup", SWEditorCategoryDefaults.UtilityCategory);
            RegisterGroup("SW.Util", SWEditorCategoryDefaults.UtilityCategory);
            SWEditorRegistry.RegisterTypePolicy(new SWEditorTypePolicy("swutils.samples.types", type => type.Assembly.GetName().Name == "SWUtils.Samples", defaultCategoryIdentifier: "swutils.samples", priority: 20));
            SWEditorRegistry.RegisterSearchProvider(new SWEditorSearchProvider("swutils.identified.search", context => context.Asset is SWIdentifiedObject identified ? new[] { identified.CodeName, identified.DisplayName, identified.Description, identified.ID.ToString() } : null));
            SWEditorRegistry.RegisterMetadataProvider(asset => asset is SWIdentifiedObject identified ? new[] { new SWEditorMetadata("Code name", identified.CodeName), new SWEditorMetadata("Identifier", identified.ID.ToString()) } : null);
        }

        private static void RegisterGroup(string namespacePrefix, string categoryIdentifier = null)
        {
            string identifier = "swutils." + namespacePrefix;
            string category = categoryIdentifier ?? identifier;
            SWEditorRegistry.RegisterTypePolicy(new SWEditorTypePolicy(identifier + ".types", type => type.Namespace != null && (type.Namespace == namespacePrefix || type.Namespace.StartsWith(namespacePrefix + ".", StringComparison.Ordinal)), defaultCategoryIdentifier: category, priority: 10));
        }

        /// <summary>선택적 Game Creator 에셋의 공개 스프라이트를 읽습니다.</summary>
        internal static Sprite FindSprite(ScriptableObject asset)
        {
            Type type = asset.GetType();
            if (!(type.Namespace?.StartsWith("GameCreator.", StringComparison.Ordinal) ?? false))
                return null;
            if (!spriteMembers.TryGetValue(type, out MemberInfo member))
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (property.PropertyType == typeof(Sprite) && property.GetIndexParameters().Length == 0 && property.CanRead)
                    {
                        member = property;
                        break;
                    }

                if (member == null)
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (field.FieldType == typeof(Sprite))
                        {
                            member = field;
                            break;
                        }

                spriteMembers[type] = member;
            }

            return SWEditorRegistry.Protect(() => member is PropertyInfo property ? property.GetValue(asset) as Sprite : (member as FieldInfo)?.GetValue(asset) as Sprite);
        }
    }
}
