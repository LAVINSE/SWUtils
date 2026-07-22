using System;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Blackboard 값과 고정값을 비교하는 범용 Action 노드입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourComparePropertyNode : SWBehaviourActionNode
    {
        [SerializeField, SWBehaviourBlackboardKeySelector] private string keyName;
        [SerializeField] private SWBehaviourComparison comparison;
        [SerializeField] private SWBehaviourBlackboardValue value = new();

        protected override SWBehaviourStatus OnUpdate(
            SWBehaviourContext context,
            SWBehaviourTreeAsset tree)
        {
            if (!context.Blackboard.TryGetEntry(keyName, out SWBehaviourBlackboardEntry entry))
                return SWBehaviourStatus.Failure;
            object leftValue = entry.GetBoxedValue();
            object rightValue = value.GetBoxedValue();
            return Compare(leftValue, rightValue, comparison)
                ? SWBehaviourStatus.Success
                : SWBehaviourStatus.Failure;
        }

        private static bool Compare(
            object leftValue,
            object rightValue,
            SWBehaviourComparison comparison)
        {
            if (comparison == SWBehaviourComparison.Equal)
                return Equals(leftValue, rightValue);
            if (comparison == SWBehaviourComparison.NotEqual)
                return !Equals(leftValue, rightValue);
            if (leftValue == null || rightValue == null)
                return false;

            int result;
            if (IsNumeric(leftValue) && IsNumeric(rightValue))
                result = Convert.ToDouble(leftValue).CompareTo(Convert.ToDouble(rightValue));
            else if (leftValue.GetType() == rightValue.GetType() && leftValue is IComparable comparable)
                result = comparable.CompareTo(rightValue);
            else
                return false;

            return comparison switch
            {
                SWBehaviourComparison.Greater => result > 0,
                SWBehaviourComparison.GreaterOrEqual => result >= 0,
                SWBehaviourComparison.Less => result < 0,
                SWBehaviourComparison.LessOrEqual => result <= 0,
                _ => false,
            };
        }

        private static bool IsNumeric(object value)
        {
            return Type.GetTypeCode(value.GetType()) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
                TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
                _ => false,
            };
        }
    }
}
