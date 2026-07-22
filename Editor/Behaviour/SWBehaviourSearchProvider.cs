using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using SW.BehaviourTree;

namespace SW.EditorTools.Behaviour
{
    /// <summary>프로젝트의 Behaviour 노드 타입을 분류하고 검색 창에 표시합니다.</summary>
    internal sealed class SWBehaviourSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private SWBehaviourGraphView graphView;
        private Vector2 graphPosition;

        public void Initialize(SWBehaviourGraphView graphView, Vector2 graphPosition)
        {
            this.graphView = graphView;
            this.graphPosition = graphPosition;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> entries = new()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Behaviour Node"), 0),
            };
            SWGraphSearchTreeBuilder treeBuilder = new();
            AddCategory(treeBuilder, "Actions", typeof(SWBehaviourActionNode));
            AddCategory(treeBuilder, "Composites", typeof(SWBehaviourCompositeNode));
            AddCategory(treeBuilder, "Decorators", typeof(SWBehaviourDecoratorNode));
            treeBuilder.AppendTo(entries, 1);
            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is not Type nodeType)
                return false;
            graphView.CreateNode(nodeType, graphPosition);
            return true;
        }

        private static void AddCategory(
            SWGraphSearchTreeBuilder treeBuilder,
            string defaultCategory,
            Type baseType)
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(baseType);
            List<Type> sortedTypes = new();
            foreach (Type type in types)
            {
                if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                    sortedTypes.Add(type);
            }
            sortedTypes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
            for (int index = 0; index < sortedTypes.Count; index++)
            {
                Type type = sortedTypes[index];
                SWBehaviourNodeCategoryAttribute categoryAttribute =
                    type.GetCustomAttribute<SWBehaviourNodeCategoryAttribute>();
                string categoryPath = string.IsNullOrWhiteSpace(categoryAttribute?.CategoryPath)
                    ? defaultCategory
                    : categoryAttribute.CategoryPath;
                treeBuilder.Add(
                    categoryPath,
                    ObjectNames.NicifyVariableName(type.Name),
                    type);
            }
        }
    }
}
