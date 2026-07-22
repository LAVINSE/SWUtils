using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

using SW.StateMachine;

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 빈 그래프에서 상태 타입과 흐름 제어 노드를 이름으로 검색해 생성하는 제공자입니다.
    /// </summary>
    internal sealed class SWStateMachineSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        #region 필드
        private SWStateMachineGraphView graphView;
        private SWStateMachineGraphAsset graphAsset;
        private Vector2 graphPosition;
        #endregion // 필드

        #region 초기화
        /// <summary>검색 결과가 노드를 생성할 그래프를 설정합니다.</summary>
        public void Initialize(
            SWStateMachineGraphView targetGraphView,
            SWStateMachineGraphAsset targetGraphAsset,
            Vector2 targetGraphPosition)
        {
            graphView = targetGraphView;
            graphAsset = targetGraphAsset;
            graphPosition = targetGraphPosition;
        }
        #endregion // 초기화

        #region 검색
        /// <summary>현재 그래프에서 생성 가능한 노드 검색 트리를 만듭니다.</summary>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            if (graphAsset == null)
                return entries;

            entries.Add(new SearchTreeGroupEntry(new GUIContent("Flow Control"), 1));
            if (graphAsset.GraphType == SWStateMachineGraphType.Layered)
            {
                entries.Add(new SearchTreeEntry(new GUIContent("Any State"))
                {
                    level = 2,
                    userData = SWStateMachineNodeKind.AnyState,
                });
            }
            else
            {
                entries.Add(new SearchTreeEntry(new GUIContent("Return State"))
                {
                    level = 2,
                    userData = SWStateMachineNodeKind.Return,
                });
            }

            SWGraphSearchTreeBuilder treeBuilder = new SWGraphSearchTreeBuilder();
            IReadOnlyList<Type> stateTypes = SWStateMachineTypeUtility.GetStateTypes(graphAsset.GraphType);
            foreach (Type stateType in stateTypes)
            {
                treeBuilder.Add(
                    SWStateMachineTypeUtility.GetCategoryPath(stateType, "States"),
                    SWStateMachineTypeUtility.GetDisplayName(stateType),
                    stateType);
            }
            treeBuilder.AppendTo(entries, 1);

            return entries;
        }

        /// <summary>선택한 검색 결과에 해당하는 노드를 마우스 위치에 생성합니다.</summary>
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (graphView == null || graphAsset == null)
                return false;

            if (searchTreeEntry.userData is Type stateType)
            {
                graphView.CreateStateNode(stateType, graphPosition);
                return true;
            }

            if (searchTreeEntry.userData is SWStateMachineNodeKind nodeKind)
            {
                graphView.CreateSpecialNode(nodeKind, graphPosition);
                return true;
            }

            return false;
        }
        #endregion // 검색
    }
}
