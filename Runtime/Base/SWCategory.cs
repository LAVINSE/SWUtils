using UnityEngine;

namespace SW.Base
{
    /// <summary>
    /// 데이터 에셋을 분류하는 카테고리입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWCategory", menuName = "SWBase/Category")]
    public class SWCategory : SWIdentifiedObject
    {
        /// <summary>
        /// 지정한 개체와 현재 카테고리의 동등성을 비교합니다.
        /// </summary>
        /// <param name="other">비교할 개체입니다.</param>
        /// <returns>두 개체가 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override bool Equals(object other)
            => base.Equals(other);

        /// <summary>
        /// 현재 카테고리의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>현재 카테고리의 해시 코드입니다.</returns>
        public override int GetHashCode()
            => base.GetHashCode();

        /// <summary>
        /// 카테고리와 코드명 문자열이 같은지 비교합니다.
        /// </summary>
        /// <param name="lhs">비교할 카테고리입니다.</param>
        /// <param name="rhs">비교할 코드명입니다.</param>
        /// <returns>코드명이 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator ==(SWCategory lhs, string rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }

            return lhs.CodeName == rhs;
        }

        /// <summary>
        /// 카테고리와 코드명 문자열이 다른지 비교합니다.
        /// </summary>
        /// <param name="lhs">비교할 카테고리입니다.</param>
        /// <param name="rhs">비교할 코드명입니다.</param>
        /// <returns>코드명이 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator !=(SWCategory lhs, string rhs)
        {
            return !(lhs == rhs);
        }
    }
}
