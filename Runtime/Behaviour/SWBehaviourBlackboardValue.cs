using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>범용 Property 노드가 직렬화하는 고정 Blackboard 값입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourBlackboardValue
    {
        [SerializeField] private SWBehaviourBlackboardValueType valueType;
        [SerializeField] private bool booleanValue;
        [SerializeField] private int integerValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private Vector2 vector2Value;
        [SerializeField] private Vector3 vector3Value;
        [SerializeField] private UnityEngine.Object objectValue;
        [SerializeReference] private SWBehaviourBlackboardEntry customValue;

        /// <summary>저장된 값 종류입니다.</summary>
        public SWBehaviourBlackboardValueType ValueType => valueType;

        /// <summary>현재 값을 박싱하여 반환합니다.</summary>
        public object GetBoxedValue()
        {
            return valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => booleanValue,
                SWBehaviourBlackboardValueType.Integer => integerValue,
                SWBehaviourBlackboardValueType.Float => floatValue,
                SWBehaviourBlackboardValueType.String => stringValue,
                SWBehaviourBlackboardValueType.Vector2 => vector2Value,
                SWBehaviourBlackboardValueType.Vector3 => vector3Value,
                SWBehaviourBlackboardValueType.Object => objectValue,
                SWBehaviourBlackboardValueType.Custom => customValue?.GetBoxedValue(),
                _ => null,
            };
        }
    }

    /// <summary>범용 Compare Property 노드의 비교 연산입니다.</summary>
    public enum SWBehaviourComparison
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }
}
