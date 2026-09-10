using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using SW.SkillTree;

namespace SW.EditorTools.SkillTree
{
    /// <summary>스킬트리 노드의 배치와 선행 연결을 편집합니다.</summary>
    internal sealed class SWSkillTreeGraphView : GraphView
    {
        private sealed class SkillNode : Node
        {
            internal SWSkillTreeNode data;
            internal Port input;
            internal Port output;
            internal Action<string> selectionChanged;
            public override void OnSelected() { base.OnSelected(); selectionChanged?.Invoke(data.Identifier); }
        }
        private SWSkillTreeDefinition definition;
        private readonly Action<string> selected;
        private readonly Action changed;
        private bool rebuilding;

        internal SWSkillTreeGraphView(Action<string> selected, Action changed)
        {
            this.selected = selected;
            this.changed = changed;
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.075f, 0.08f, 0.09f);
            SetupZoom(0.2f, 2f);
            GridBackground grid = new();
            grid.StretchToParentSize();
            Insert(0, grid);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = OnGraphChanged;
        }

        /// <summary>에셋에 저장한 노드와 연결선을 표시합니다.</summary>
        internal void Populate(SWSkillTreeDefinition asset)
        {
            rebuilding = true;
            try
            {
                definition = asset;
                DeleteElements(graphElements.ToList());
                if (definition == null) return;
                Dictionary<string, SkillNode> displayed = new(StringComparer.Ordinal);
                foreach (SWSkillTreeNode data in definition.Nodes)
                {
                    if (data == null || string.IsNullOrEmpty(data.Identifier) || displayed.ContainsKey(data.Identifier)) continue;
                    SkillNode node = new() { data = data, selectionChanged = selected, title = data.Skill != null ? data.Skill.DisplayName : "스킬을 연결하세요" };
                    node.style.width = 200;
                    node.style.backgroundColor = new Color(0.157f, 0.165f, 0.18f);
                    node.input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(SWSkillTreeRequirement));
                    node.output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(SWSkillTreeRequirement));
                    node.input.portName = data.RequirementMode == SWSkillTreeRequirementMode.All ? "모두 필요" : "하나 필요";
                    node.output.portName = "후속 해금";
                    node.input.portColor = node.output.portColor = new Color(0.22f, 0.58f, 0.92f);
                    node.inputContainer.Add(node.input);
                    node.outputContainer.Add(node.output);
                    Label summary = new(data.Skill != null ? $"최대 {data.Skill.MaximumLevel}레벨" : "오른쪽 패널에서 스킬 지정");
                    summary.style.marginLeft = 8;
                    summary.style.marginBottom = 8;
                    summary.style.color = new Color(0.68f, 0.71f, 0.75f);
                    node.extensionContainer.Add(summary);
                    if (!string.IsNullOrEmpty(data.ExclusiveGroup)) node.extensionContainer.Add(new Label($"선택 분기: {data.ExclusiveGroup}"));
                    node.RefreshExpandedState();
                    node.RefreshPorts();
                    node.SetPosition(new Rect(data.Position, new Vector2(200, 110)));
                    AddElement(node);
                    displayed.Add(data.Identifier, node);
                }
                foreach (SkillNode node in displayed.Values)
                    foreach (SWSkillTreeRequirement requirement in node.data.Requirements)
                    {
                        if (requirement == null || requirement.NodeIdentifier == null || !displayed.TryGetValue(requirement.NodeIdentifier, out SkillNode parent)) continue;
                        Edge edge = parent.output.ConnectTo(node.input);
                        edge.tooltip = $"{parent.title} {requirement.RequiredLevel}레벨 필요";
                        AddElement(edge);
                    }
            }
            finally { rebuilding = false; }
        }

        /// <inheritdoc />
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            => ports.Where(port => port.direction != startPort.direction && port.node != startPort.node).ToList();

        /// <inheritdoc />
        public override void BuildContextualMenu(ContextualMenuPopulateEvent menuEvent)
        {
            base.BuildContextualMenu(menuEvent);
            if (definition == null) return;
            Vector2 position = contentViewContainer.WorldToLocal(menuEvent.mousePosition);
            menuEvent.menu.AppendAction("스킬 노드 추가", _ => AddNode(position));
        }

        internal void AddNode(Vector2 position)
        {
            if (definition == null) return;
            SerializedObject serialized = new(definition);
            SerializedProperty array = serialized.FindProperty("nodes");
            int index = array.arraySize++;
            SerializedProperty node = array.GetArrayElementAtIndex(index);
            string identifier = Guid.NewGuid().ToString("N");
            node.FindPropertyRelative("identifier").stringValue = identifier;
            node.FindPropertyRelative("skill").objectReferenceValue = null;
            node.FindPropertyRelative("position").vector2Value = position;
            node.FindPropertyRelative("hasSavedPosition").boolValue = true;
            node.FindPropertyRelative("revealPolicy").enumValueIndex = 0;
            node.FindPropertyRelative("concealment").enumValueIndex = 0;
            node.FindPropertyRelative("requirements").ClearArray();
            node.FindPropertyRelative("visibilityConditions").ClearArray();
            node.FindPropertyRelative("purchaseConditions").ClearArray();
            node.FindPropertyRelative("exclusiveGroup").stringValue = string.Empty;
            node.FindPropertyRelative("retainOnReset").boolValue = false;
            node.FindPropertyRelative("requirementMode").enumValueIndex = 0;
            serialized.ApplyModifiedProperties();
            Populate(definition);
            selected(identifier);
            changed();
        }

        private GraphViewChange OnGraphChanged(GraphViewChange change)
        {
            if (rebuilding || definition == null) return change;
            SerializedObject serialized = new(definition);
            SerializedProperty array = serialized.FindProperty("nodes");
            SerializedProperty FindNode(string identifier)
            {
                for (int index = 0; index < array.arraySize; index++)
                    if (array.GetArrayElementAtIndex(index).FindPropertyRelative("identifier").stringValue == identifier) return array.GetArrayElementAtIndex(index);
                return null;
            }
            if (change.movedElements != null)
                foreach (SkillNode node in change.movedElements.OfType<SkillNode>())
                {
                    SerializedProperty position = FindNode(node.data.Identifier)?.FindPropertyRelative("position");
                    if (position != null)
                    {
                        position.vector2Value = node.GetPosition().position;
                        FindNode(node.data.Identifier).FindPropertyRelative("hasSavedPosition").boolValue = true;
                    }
                }
            if (change.elementsToRemove != null)
            {
                HashSet<string> removed = new(change.elementsToRemove.OfType<SkillNode>().Select(node => node.data.Identifier));
                for (int index = array.arraySize - 1; index >= 0; index--)
                {
                    SerializedProperty node = array.GetArrayElementAtIndex(index);
                    if (removed.Contains(node.FindPropertyRelative("identifier").stringValue)) { array.DeleteArrayElementAtIndex(index); continue; }
                    SerializedProperty requirements = node.FindPropertyRelative("requirements");
                    for (int requirementIndex = requirements.arraySize - 1; requirementIndex >= 0; requirementIndex--)
                    {
                        string parent = requirements.GetArrayElementAtIndex(requirementIndex).FindPropertyRelative("nodeIdentifier").stringValue;
                        bool removeEdge = change.elementsToRemove.OfType<Edge>().Any(edge => edge.output?.node is SkillNode parentNode && edge.input?.node is SkillNode childNode
                            && parentNode.data.Identifier == parent && childNode.data.Identifier == node.FindPropertyRelative("identifier").stringValue);
                        if (removed.Contains(parent) || removeEdge) requirements.DeleteArrayElementAtIndex(requirementIndex);
                    }
                }
            }
            if (change.edgesToCreate != null)
                change.edgesToCreate.RemoveAll(edge =>
                {
                    SkillNode parent = edge.output.node as SkillNode;
                    SkillNode child = edge.input.node as SkillNode;
                    if (parent == null || child == null || parent == child || WouldCycle(parent.data.Identifier, child.data.Identifier)) return true;
                    SerializedProperty requirements = FindNode(child.data.Identifier)?.FindPropertyRelative("requirements");
                    if (requirements == null) return true;
                    for (int index = 0; index < requirements.arraySize; index++)
                        if (requirements.GetArrayElementAtIndex(index).FindPropertyRelative("nodeIdentifier").stringValue == parent.data.Identifier) return true;
                    int added = requirements.arraySize++;
                    requirements.GetArrayElementAtIndex(added).FindPropertyRelative("nodeIdentifier").stringValue = parent.data.Identifier;
                    requirements.GetArrayElementAtIndex(added).FindPropertyRelative("requiredLevel").intValue = 1;
                    return false;
                });
            serialized.ApplyModifiedProperties();
            changed();
            return change;
        }

        private bool WouldCycle(string parent, string child)
        {
            HashSet<string> visited = new(StringComparer.Ordinal);
            bool Reaches(string identifier)
            {
                if (identifier == child) return true;
                if (!visited.Add(identifier)) return false;
                SWSkillTreeNode node = definition.Nodes.FirstOrDefault(value => value.Identifier == identifier);
                return node != null && node.Requirements.Any(requirement => requirement != null && Reaches(requirement.NodeIdentifier));
            }
            return Reaches(parent);
        }
    }

}
