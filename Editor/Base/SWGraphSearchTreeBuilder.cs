using System;
using System.Collections.Generic;

using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SW.EditorTools
{
    /// <summary>슬래시로 구분한 카테고리 경로를 그래프 검색 트리로 구성합니다.</summary>
    internal sealed class SWGraphSearchTreeBuilder
    {
        private readonly CategoryNode rootNode = new CategoryNode(string.Empty);

        /// <summary>검색 항목을 지정한 카테고리 경로에 추가합니다.</summary>
        public void Add(string categoryPath, string displayName, object userData)
        {
            string[] categoryNames = NormalizePath(categoryPath).Split('/');
            CategoryNode currentNode = rootNode;
            for (int index = 0; index < categoryNames.Length; index++)
            {
                string categoryName = categoryNames[index];
                if (!currentNode.Children.TryGetValue(categoryName, out CategoryNode childNode))
                {
                    childNode = new CategoryNode(categoryName);
                    currentNode.Children.Add(categoryName, childNode);
                }

                currentNode = childNode;
            }

            currentNode.Items.Add(new SearchItem(displayName, userData));
        }

        /// <summary>구성한 카테고리와 항목을 검색 트리 항목에 추가합니다.</summary>
        public void AppendTo(List<SearchTreeEntry> entries, int firstLevel)
        {
            AppendChildren(rootNode, entries, firstLevel);
        }

        private static void AppendChildren(
            CategoryNode parentNode,
            List<SearchTreeEntry> entries,
            int level)
        {
            foreach (CategoryNode categoryNode in parentNode.Children.Values)
            {
                entries.Add(new SearchTreeGroupEntry(
                    new GUIContent(categoryNode.Name), level));
                categoryNode.Items.Sort((left, right) => string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison.OrdinalIgnoreCase));
                for (int index = 0; index < categoryNode.Items.Count; index++)
                {
                    SearchItem item = categoryNode.Items[index];
                    entries.Add(new SearchTreeEntry(new GUIContent(item.DisplayName))
                    {
                        level = level + 1,
                        userData = item.UserData,
                    });
                }

                AppendChildren(categoryNode, entries, level + 1);
            }
        }

        private static string NormalizePath(string categoryPath)
        {
            if (string.IsNullOrWhiteSpace(categoryPath))
                return "Uncategorized";

            string[] segments = categoryPath.Split('/');
            List<string> validSegments = new List<string>();
            for (int index = 0; index < segments.Length; index++)
            {
                string segment = segments[index].Trim();
                if (!string.IsNullOrWhiteSpace(segment))
                    validSegments.Add(segment);
            }

            return validSegments.Count == 0
                ? "Uncategorized"
                : string.Join("/", validSegments);
        }

        private sealed class CategoryNode
        {
            public string Name { get; }
            public SortedDictionary<string, CategoryNode> Children { get; } =
                new SortedDictionary<string, CategoryNode>(StringComparer.OrdinalIgnoreCase);
            public List<SearchItem> Items { get; } = new List<SearchItem>();

            public CategoryNode(string name)
            {
                Name = name;
            }
        }

        private sealed class SearchItem
        {
            public string DisplayName { get; }
            public object UserData { get; }

            public SearchItem(string displayName, object userData)
            {
                DisplayName = displayName;
                UserData = userData;
            }
        }
    }
}
