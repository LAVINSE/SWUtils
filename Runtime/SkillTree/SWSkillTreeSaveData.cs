using System;
using System.Collections.Generic;

namespace SW.SkillTree
{
    /// <summary>노드 하나의 레벨별 실제 지불 이력입니다.</summary>
    [Serializable]
    public sealed class SWSkillTreeNodeSaveData
    {
        /// <summary>노드의 영구 식별자입니다.</summary>
        public string nodeIdentifier;
        /// <summary>구매 순서대로 기록한 레벨별 비용입니다. 개수가 현재 레벨입니다.</summary>
        public List<SWSkillTreePayment> payments = new();
    }

    /// <summary>한 레벨을 구매할 때 실제로 지불한 재화 목록입니다.</summary>
    [Serializable]
    public sealed class SWSkillTreePayment
    {
        /// <summary>환불 시 사용하는 실제 지불 비용입니다.</summary>
        public List<SWSkillTreeAmount> amounts = new();
    }

    /// <summary>플레이어별 스킬트리 진행 데이터입니다. 재화 저장은 프로젝트가 같은 저장 단위로 묶어야 합니다.</summary>
    [Serializable]
    public sealed class SWSkillTreeSaveData
    {
        /// <summary>현재 저장 형식 버전입니다.</summary>
        public int version = 1;
        /// <summary>저장한 트리의 영구 식별자입니다.</summary>
        public string treeIdentifier;
        /// <summary>습득한 노드와 실제 지불 이력입니다.</summary>
        public List<SWSkillTreeNodeSaveData> nodes = new();
    }
}
