#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SW.Attributes;

namespace SW.SkillTree
{
    public sealed partial class SWSkillTreeView
    {
        #region 편집 미리보기
        private void OnValidate()
        {
            nodeSize = new Vector2(Mathf.Max(1, nodeSize.x), Mathf.Max(1, nodeSize.y));
            QueueEditorPreview();
        }

        private void QueueEditorPreview()
        {
            EditorApplication.delayCall -= RefreshEditorPreview;
            EditorApplication.delayCall += RefreshEditorPreview;
        }

        /// <summary>위치를 다시 생성하지 않고 현재 편집 노드의 숨김 상태만 갱신합니다.</summary>
        public void RefreshEditorPreview()
        {
            if (this == null || Application.isPlaying || EditorUtility.IsPersistent(this) || nodesRoot == null || displayedDefinition == null) return;
            foreach (SWSkillTreeNodeView nodeView in nodesRoot.GetComponentsInChildren<SWSkillTreeNodeView>(true))
                ((RectTransform)nodeView.transform).sizeDelta = nodeSize;
            if (system != null)
            {
                dirty = true;
                RefreshView();
                return;
            }
            if (SWSkillTreeDefinitionValidator.Validate(displayedDefinition).Count != 0) return;
            // 도메인 재로드 후에도 직렬화된 노드 식별자로 미리보기 상태를 복원합니다.
            using SWSkillTreeSystem preview = new(displayedDefinition, new SWSkillTreeWallet());
            foreach (SWSkillTreeNodeView view in nodesRoot.GetComponentsInChildren<SWSkillTreeNodeView>(true))
            {
                if (!preview.TryGetNode(view.NodeIdentifier, out SWSkillTreeNode node)) continue;
                bool visible = showAllNodesInEditor || preview.IsVisible(node.Identifier);
                view.gameObject.SetActive(visible);
                if (!visible) continue;
                if (showAllNodesInEditor || preview.IsRevealed(node.Identifier)) view.Render(node, 0, false, false);
                else view.RenderMasked();
            }
            if (connectionsRoot != null)
                foreach (SWSkillTreeConnectionView connection in connectionsRoot.GetComponentsInChildren<SWSkillTreeConnectionView>(true))
                    connection.gameObject.SetActive(connection.HasVisibleEndpoints);
            SceneView.RepaintAll();
        }
        #endregion // 편집 미리보기

        #region 제작 위치 저장
        /// <summary>View에서 저장한 위치를 제작 편집기에 즉시 알립니다.</summary>
        public static event Action<SWSkillTreeDefinition> LayoutSaved;

        /// <summary>편집 중인 실제 노드 위치를 공용 트리 에셋에 저장합니다. 플레이어 저장 데이터는 변경하지 않습니다.</summary>
        [SWButton("현재 위치 저장")]
        public void SaveLayoutToDefinition()
        {
            if (Application.isPlaying) throw new InvalidOperationException("위치 저장은 편집 모드에서만 사용할 수 있습니다.");
            if (displayedDefinition == null || nodesRoot == null) throw new InvalidOperationException("트리 노드를 먼저 생성하세요.");
            Dictionary<string, Vector2> coordinates = new(StringComparer.Ordinal);
            foreach (SWSkillTreeNodeView node in nodesRoot.GetComponentsInChildren<SWSkillTreeNodeView>(true))
            {
                if (string.IsNullOrEmpty(node.NodeIdentifier)) continue;
                RectTransform rectangle = (RectTransform)node.transform;
                Vector2 point = SWSkillTreeViewGeometry.GetRelativeMatrix(rectangle, nodesRoot).MultiplyPoint3x4(rectangle.rect.center);
                if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y))
                    throw new InvalidOperationException("유효하지 않은 노드 좌표는 저장할 수 없습니다.");
                if (!coordinates.TryAdd(node.NodeIdentifier, new Vector2(point.x, -point.y)))
                    throw new InvalidOperationException("동일한 식별자의 표시 노드가 중복되어 있습니다.");
            }
            if (coordinates.Count == 0) throw new InvalidOperationException("저장할 노드가 없습니다. 노드를 다시 생성하세요.");
            // 모든 원본 노드가 대응되는지 먼저 확인하여 일부 위치만 잘못 저장되는 것을 방지합니다.
            if (coordinates.Count != displayedDefinition.Nodes.Count) throw new InvalidOperationException("노드 구성이 변경되었습니다. 노드를 다시 생성하세요.");
            foreach (SWSkillTreeNode node in displayedDefinition.Nodes)
                if (!coordinates.ContainsKey(node.Identifier)) throw new InvalidOperationException("노드 구성이 변경되었습니다. 노드를 다시 생성하세요.");
            SerializedObject serialized = new(displayedDefinition);
            SerializedProperty nodes = serialized.FindProperty("nodes");
            for (int index = 0; index < nodes.arraySize; index++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(index);
                node.FindPropertyRelative("position").vector2Value = coordinates[node.FindPropertyRelative("identifier").stringValue];
                node.FindPropertyRelative("hasSavedPosition").boolValue = true;
            }
            serialized.ApplyModifiedProperties();
            if (EditorUtility.IsPersistent(displayedDefinition)) AssetDatabase.SaveAssetIfDirty(displayedDefinition);
            LayoutSaved?.Invoke(displayedDefinition);
        }

        /// <summary>트리 에셋의 저장 위치를 현재 표시 노드에 다시 적용합니다.</summary>
        [SWButton("저장 위치 불러오기")]
        public void LoadLayoutFromDefinition()
        {
            if (Application.isPlaying) throw new InvalidOperationException("위치 불러오기는 편집 모드에서만 사용할 수 있습니다.");
            if (displayedDefinition == null || nodesRoot == null) return;
            Dictionary<string, SWSkillTreeNode> definitions = new(StringComparer.Ordinal);
            foreach (SWSkillTreeNode node in displayedDefinition.Nodes) definitions[node.Identifier] = node;
            foreach (SWSkillTreeNodeView view in nodesRoot.GetComponentsInChildren<SWSkillTreeNodeView>(true))
            {
                if (string.IsNullOrEmpty(view.NodeIdentifier) || !definitions.TryGetValue(view.NodeIdentifier, out SWSkillTreeNode node) || !node.HasSavedPosition) continue;
                RectTransform rectangle = (RectTransform)view.transform;
                Undo.RecordObject(rectangle, "스킬트리 저장 위치 불러오기");
                Vector3 center = new(node.Position.x, -node.Position.y, 0);
                Vector3 current = SWSkillTreeViewGeometry.GetRelativeMatrix(rectangle, nodesRoot).MultiplyPoint3x4(rectangle.rect.center);
                rectangle.localPosition += SWSkillTreeViewGeometry.GetRelativeMatrix(rectangle.parent, nodesRoot).inverse.MultiplyVector(center - current);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rectangle);
            }
            foreach (SWSkillTreeConnectionView connection in connectionsRoot.GetComponentsInChildren<SWSkillTreeConnectionView>(true)) connection.RefreshGeometry();
            SceneView.RepaintAll();
        }
        #endregion // 제작 위치 저장
    }
}
#endif
