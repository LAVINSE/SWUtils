using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>고정값 또는 Blackboard Key 중 하나에서 노드 값을 읽고 씁니다.</summary>
    [Serializable]
    public sealed class SWBehaviourNodeProperty<T>
    {
        [SerializeField] private bool useBlackboard;
        [SerializeField] private string keyName;
        [SerializeField] private T value;

        public bool UseBlackboard { get => useBlackboard; set => useBlackboard = value; }
        public string KeyName { get => keyName; set => keyName = value; }
        public T FixedValue { get => value; set => this.value = value; }

        /// <summary>현재 설정에 따라 Blackboard 값 또는 고정값을 반환합니다.</summary>
        public T GetValue(SWBehaviourContext context)
        {
            return useBlackboard && context?.Blackboard != null
                ? context.Blackboard.GetValue(keyName, value)
                : value;
        }

        /// <summary>Blackboard 참조 상태면 Key에, 아니면 고정값에 기록합니다.</summary>
        public bool SetValue(SWBehaviourContext context, T changedValue)
        {
            if (!useBlackboard)
            {
                value = changedValue;
                return true;
            }
            return context?.Blackboard != null &&
                context.Blackboard.SetValue(keyName, changedValue);
        }
    }
}
