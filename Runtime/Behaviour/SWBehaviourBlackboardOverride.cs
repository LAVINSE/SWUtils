using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Runner별로 Behaviour Tree Blackboard 기본값을 덮어씁니다.</summary>
    [Serializable]
    public sealed class SWBehaviourBlackboardOverride
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string keyName;
        [SerializeField] private SWBehaviourBlackboardValueType valueType;
        [SerializeField] private bool booleanValue;
        [SerializeField] private int integerValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private Vector2 vector2Value;
        [SerializeField] private Vector3 vector3Value;
        [SerializeField] private UnityEngine.Object objectValue;
        [SerializeReference] private SWBehaviourBlackboardEntry customValue;

        public bool Enabled => enabled;
        public string KeyName => keyName;

        /// <summary>Override에 저장된 값을 런타임 Blackboard에 적용합니다.</summary>
        public bool Apply(SWBehaviourBlackboard blackboard)
        {
            if (!enabled || blackboard == null)
                return false;
            return valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => blackboard.SetValue(keyName, booleanValue),
                SWBehaviourBlackboardValueType.Integer => blackboard.SetValue(keyName, integerValue),
                SWBehaviourBlackboardValueType.Float => blackboard.SetValue(keyName, floatValue),
                SWBehaviourBlackboardValueType.String => blackboard.SetValue(keyName, stringValue),
                SWBehaviourBlackboardValueType.Vector2 => blackboard.SetValue(keyName, vector2Value),
                SWBehaviourBlackboardValueType.Vector3 => blackboard.SetValue(keyName, vector3Value),
                SWBehaviourBlackboardValueType.Object => blackboard.SetValue(keyName, objectValue),
                SWBehaviourBlackboardValueType.Custom =>
                    blackboard.TryGetEntry(keyName, out SWBehaviourBlackboardEntry targetEntry) &&
                    customValue != null &&
                    targetEntry.TrySetBoxedValue(customValue.GetBoxedValue()),
                _ => false,
            };
        }
    }
}
