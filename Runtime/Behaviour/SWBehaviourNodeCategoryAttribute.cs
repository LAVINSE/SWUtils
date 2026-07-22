using System;

namespace SW.BehaviourTree
{
    /// <summary>Behaviour Tree 노드가 생성 검색 창에 표시될 사용자 정의 카테고리를 지정합니다.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SWBehaviourNodeCategoryAttribute : Attribute
    {
        /// <summary>슬래시로 계층을 구분한 카테고리 경로입니다.</summary>
        public string CategoryPath { get; }

        /// <summary>Behaviour Tree 노드의 카테고리 경로를 지정합니다.</summary>
        /// <param name="categoryPath">예: <c>Combat/Movement</c> 형식의 경로입니다.</param>
        public SWBehaviourNodeCategoryAttribute(string categoryPath)
        {
            CategoryPath = categoryPath;
        }
    }
}
