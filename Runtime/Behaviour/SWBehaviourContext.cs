using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Behaviour Tree 실행 중 모든 노드에 전달되는 공통 문맥입니다.</summary>
    public sealed class SWBehaviourContext
    {
        /// <summary>Behaviour Tree를 실행하는 게임 오브젝트입니다.</summary>
        public GameObject Owner { get; internal set; }

        /// <summary>노드 사이에서 데이터를 공유하는 Blackboard입니다.</summary>
        public SWBehaviourBlackboard Blackboard { get; internal set; }

        /// <summary>현재 실행 프레임의 시간 간격입니다.</summary>
        public float DeltaTime { get; internal set; }

        /// <summary>Owner에서 지정한 컴포넌트를 찾습니다.</summary>
        public T GetComponent<T>() where T : Component => Owner == null ? null : Owner.GetComponent<T>();
    }
}
