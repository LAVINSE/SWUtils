using System.Collections.Generic;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Behaviour Tree 에셋의 독립 복제본을 게임 오브젝트에서 실행합니다.</summary>
    public sealed class SWBehaviourTreeRunner : SW.Base.SWMonoBehaviour
    {
        [SerializeField] private SWBehaviourTreeAsset treeAsset;
        [SerializeField] private bool runOnEnable = true;
        [SerializeField] private List<SWBehaviourBlackboardOverride> blackboardOverrides = new();
        private SWBehaviourTreeAsset runtimeTree;

        /// <summary>현재 실행 중인 Behaviour Tree 복제본입니다.</summary>
        public SWBehaviourTreeAsset RuntimeTree => runtimeTree;

        /// <summary>런타임 복제본의 원본 Behaviour Tree 에셋입니다.</summary>
        public SWBehaviourTreeAsset TreeAsset => treeAsset;

        /// <summary>실행 중인 Blackboard에서 값을 가져옵니다.</summary>
        public bool TryGetBlackboardValue<T>(string keyName, out T value)
        {
            if (runtimeTree != null)
                return runtimeTree.Blackboard.TryGetValue(keyName, out value);
            value = default;
            return false;
        }

        /// <summary>실행 중인 Blackboard에서 값을 가져오며 없으면 기본값을 반환합니다.</summary>
        public T GetBlackboardValue<T>(string keyName, T defaultValue = default)
        {
            return runtimeTree == null
                ? defaultValue
                : runtimeTree.Blackboard.GetValue(keyName, defaultValue);
        }

        /// <summary>실행 중인 Blackboard 값을 변경합니다.</summary>
        public bool SetBlackboardValue<T>(string keyName, T value)
        {
            return runtimeTree != null && runtimeTree.Blackboard.SetValue(keyName, value);
        }

        /// <summary>반복적인 이름 조회 없이 사용할 Blackboard Key 참조를 반환합니다.</summary>
        public SWBehaviourBlackboardKey<T> FindBlackboardKey<T>(string keyName)
        {
            return runtimeTree?.Blackboard.FindKey<T>(keyName);
        }

        private void OnEnable()
        {
            if (runOnEnable)
                StartTree();
        }

        private void Update()
        {
            runtimeTree?.Tick(Time.deltaTime);
        }

        private void OnDisable() => StopTree();

        /// <summary>에셋에서 독립 실행 트리를 생성합니다.</summary>
        public void StartTree()
        {
            StopTree();
            if (treeAsset != null)
            {
                runtimeTree = treeAsset.CreateRuntimeInstance(gameObject);
                for (int index = 0; index < blackboardOverrides.Count; index++)
                    blackboardOverrides[index]?.Apply(runtimeTree.Blackboard);
            }
        }

        /// <summary>진행 중인 트리를 중단하고 복제본을 제거합니다.</summary>
        public void StopTree()
        {
            if (runtimeTree == null)
                return;
            runtimeTree.Abort();
            Destroy(runtimeTree);
            runtimeTree = null;
        }
    }
}
