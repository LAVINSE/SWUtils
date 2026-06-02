using System;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// SerializeReference 필드에서 하위 클래스를 선택할 수 있도록 표시하는 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SWSubClassSelectorAttribute : PropertyAttribute
    {
        #region 프로퍼티
        /// <summary>
        /// 선택된 객체의 ToString 반환값을 필드 라벨로 사용할지 여부입니다.
        /// </summary>
        public bool UseToStringAsLabel { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 하위 클래스 선택 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="useToStringAsLabel">선택된 객체의 ToString 반환값을 필드 라벨로 사용할지 여부입니다.</param>
        public SWSubClassSelectorAttribute(bool useToStringAsLabel = false)
        {
            UseToStringAsLabel = useToStringAsLabel;
        }
        #endregion // 함수
    }

    /// <summary>
    /// SWSubClassSelector 메뉴에서 타입이 표시될 경로와 이름을 지정하는 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SWAddTypeMenuAttribute : Attribute
    {
        #region 프로퍼티
        /// <summary>
        /// 타입 선택 메뉴에 표시할 경로입니다.
        /// </summary>
        public string MenuName { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 타입 선택 메뉴 표시 경로를 지정합니다.
        /// </summary>
        /// <param name="menuName">타입 선택 메뉴에 표시할 경로입니다.</param>
        public SWAddTypeMenuAttribute(string menuName)
        {
            MenuName = menuName;
        }
        #endregion // 함수
    }

    /// <summary>
    /// SWSubClassSelector 타입 선택 메뉴에서 타입을 숨기는 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SWHideInTypeMenuAttribute : Attribute
    {
    }
}
