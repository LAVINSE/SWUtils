using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

using SW.SkillTree;

namespace SW.Tests.SkillTree
{
    /// <summary>실제 화면 좌표의 연결선 갱신과 제작 좌표 저장을 검사합니다.</summary>
    public sealed class SWSkillTreeLayoutTests
    {
        /// <summary>드래그 이동과 포인터 중심 확대가 동일한 노드 좌표를 유지하고 확대 제한을 지킵니다.</summary>
        [Test]
        public void ViewportPansAndZoomsAroundPointer()
        {
            GameObject root = new("ViewportTest", typeof(RectTransform));
            GameObject events = new("Events", typeof(EventSystem));
            try
            {
                RectTransform content = SWSkillTreeViewFactory.CreateRect("Content", root.transform);
                SWSkillTreeViewport viewport = root.AddComponent<SWSkillTreeViewport>();
                viewport.Initialize(content);
                PointerEventData pointer = new(events.GetComponent<EventSystem>()) { position = new Vector2(100, 80) };
                viewport.OnBeginDrag(pointer);
                pointer.position += new Vector2(50, -20);
                viewport.OnDrag(pointer);
                Assert.That(content.anchoredPosition, Is.EqualTo(new Vector2(50, -20)));
                Vector2 before = (pointer.position - content.anchoredPosition) / content.localScale.x;
                pointer.scrollDelta = Vector2.up;
                viewport.OnScroll(pointer);
                Vector2 after = (pointer.position - content.anchoredPosition) / content.localScale.x;
                Assert.That(Vector2.Distance(before, after), Is.LessThan(0.001f));
                pointer.scrollDelta = Vector2.up * 100;
                viewport.OnScroll(pointer);
                Assert.That(content.localScale.x, Is.EqualTo(SWSkillTreeViewport.MaximumZoom));
                pointer.scrollDelta = Vector2.down * 100;
                viewport.OnScroll(pointer);
                Assert.That(content.localScale.x, Is.EqualTo(SWSkillTreeViewport.MinimumZoom));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(events); }
        }

        /// <summary>저장 좌표를 변경하지 않고 시작 노드를 지정한 배율로 중앙에 배치합니다.</summary>
        [Test]
        public void FocusStartPreservesNodeCoordinates()
        {
            GameObject root = new("FocusTest", typeof(RectTransform));
            SWSkillTreeDefinition definition = ScriptableObject.CreateInstance<SWSkillTreeDefinition>();
            try
            {
                SWSkillTreeNode node = new(null, new Vector2(300, 600));
                SWSkillTreeSystemTests.Set(definition, "nodes", new List<SWSkillTreeNode> { node });
                SWSkillTreeView view = root.AddComponent<SWSkillTreeView>();
                RectTransform content = SWSkillTreeViewFactory.CreateRect("Content", root.transform);
                RectTransform nodes = SWSkillTreeViewFactory.CreateRect("Nodes", content);
                RectTransform rectangle = SWSkillTreeViewFactory.CreateRect("Node", nodes);
                rectangle.anchoredPosition = new Vector2(300, -600);
                rectangle.gameObject.AddComponent<SWSkillTreeNodeView>().BindIdentifier(node.Identifier);
                SWSkillTreeSystemTests.Set(view, "content", content);
                SWSkillTreeSystemTests.Set(view, "nodesRoot", nodes);
                SWSkillTreeSystemTests.Set(view, "displayedDefinition", definition);
                SWSkillTreeSystemTests.Set(view, "initialZoom", 0.75f);
                view.FocusStart();
                Assert.That(content.localScale.x, Is.EqualTo(0.75f));
                Assert.That(content.anchoredPosition, Is.EqualTo(new Vector2(-225, 450)));
                Assert.That(rectangle.anchoredPosition, Is.EqualTo(new Vector2(300, -600)));
                Assert.That(node.Position, Is.EqualTo(new Vector2(300, 600)));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(definition); }
        }

        /// <summary>노드 이동과 크기 변경 시 테두리 사이의 길이와 각도를 갱신합니다.</summary>
        [Test]
        public void ConnectionTracksMovedAndResizedRectangles()
        {
            GameObject root = new("LayoutTest", typeof(RectTransform));
            try
            {
                RectTransform source = SWSkillTreeViewFactory.CreateRect("Source", root.transform);
                RectTransform destination = SWSkillTreeViewFactory.CreateRect("Destination", root.transform);
                source.sizeDelta = destination.sizeDelta = new Vector2(100, 60);
                source.localPosition = Vector3.zero;
                destination.localPosition = new Vector3(300, 0, 0);
                RectTransform line = SWSkillTreeViewFactory.CreateRect("Line", root.transform);
                SWSkillTreeConnectionView connection = line.gameObject.AddComponent<SWSkillTreeConnectionView>();
                connection.Initialize(source, destination);
                Assert.That(line.sizeDelta.x, Is.EqualTo(200).Within(0.01));
                Assert.That(line.localPosition.x, Is.EqualTo(150).Within(0.01));
                destination.localPosition = new Vector3(0, 300, 0);
                destination.sizeDelta = new Vector2(100, 100);
                connection.RefreshGeometry();
                Assert.That(line.sizeDelta.x, Is.EqualTo(220).Within(0.01));
                Assert.That(line.localPosition.y, Is.EqualTo(140).Within(0.01));
                Assert.That(line.localEulerAngles.z, Is.EqualTo(90).Within(0.01));
                root.transform.localScale = new Vector3(2, 3, 1);
                connection.RefreshGeometry();
                Assert.That(line.sizeDelta.x, Is.EqualTo(220).Within(0.01));
                destination.localPosition = Vector3.zero;
                connection.RefreshGeometry();
                Assert.That(line.sizeDelta.x, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>확대와 이동에 영향받지 않는 좌표를 에셋에 저장하고 원점 좌표도 복원합니다.</summary>
        [Test]
        public void ViewSavesAndReloadsSharedEditorCoordinates()
        {
            GameObject root = new("SaveLayoutTest", typeof(RectTransform));
            SWSkillTreeDefinition definition = ScriptableObject.CreateInstance<SWSkillTreeDefinition>();
            try
            {
                SWSkillTreeNode node = new(null, Vector2.zero);
                SWSkillTreeSystemTests.Set(definition, "nodes", new List<SWSkillTreeNode> { node });
                SWSkillTreeView view = root.AddComponent<SWSkillTreeView>();
                RectTransform nodesRoot = SWSkillTreeViewFactory.CreateRect("Nodes", root.transform);
                RectTransform connectionsRoot = SWSkillTreeViewFactory.CreateRect("Connections", root.transform);
                SWSkillTreeSystemTests.Set(view, "nodesRoot", nodesRoot);
                SWSkillTreeSystemTests.Set(view, "connectionsRoot", connectionsRoot);
                SWSkillTreeSystemTests.Set(view, "displayedDefinition", definition);
                RectTransform rectangle = SWSkillTreeViewFactory.CreateRect("Node", nodesRoot);
                SWSkillTreeNodeView nodeView = rectangle.gameObject.AddComponent<SWSkillTreeNodeView>();
                nodeView.BindIdentifier(node.Identifier);
                root.transform.localScale = Vector3.one * 2;
                root.transform.localPosition = new Vector3(800, 200, 0);
                rectangle.anchoredPosition = new Vector2(120, -240);
                view.SaveLayoutToDefinition();
                Assert.That(node.Position, Is.EqualTo(new Vector2(120, 240)));
                Assert.That(node.HasSavedPosition, Is.True);
                rectangle.anchoredPosition = Vector2.zero;
                view.LoadLayoutFromDefinition();
                Assert.That(rectangle.anchoredPosition, Is.EqualTo(new Vector2(120, -240)));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(definition); }
        }
    }
}
