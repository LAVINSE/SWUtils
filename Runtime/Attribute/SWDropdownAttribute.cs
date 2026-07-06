using System;
using UnityEngine;

namespace SW.Attribute
{
    /// <summary>
    /// Inspector에서 드롭다운 메뉴로 값을 선택할 수 있게 해주는 커스텀 속성입니다.
    /// 문자열, 정수, 실수 타입의 필드에 사용할 수 있습니다.
    /// </summary>
    /// <example>
    /// <code>[SWDropdown("옵션1", "옵션2", "옵션3")]</code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class SWDropdownAttribute : PropertyAttribute
    {
        #region 필드
        /// <summary>
        /// 드롭다운에 표시할 값입니다.
        /// </summary>
        public readonly object[] DropdownValues;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 드롭다운에 표시할 값 목록을 지정해 드롭다운 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="dropdownValues">드롭다운 항목으로 사용할 값 목록입니다.</param>
        public SWDropdownAttribute(params object[] dropdownValues)
        {
            DropdownValues = dropdownValues;
        }
    }
}
