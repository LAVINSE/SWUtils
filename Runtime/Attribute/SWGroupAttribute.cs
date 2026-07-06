using System;
using UnityEngine;

namespace SW.Attribute
{
    /// <summary>
    /// 인스펙터 필드를 접을 수 있는 그룹으로 묶는 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class SWGroupAttribute : PropertyAttribute
    {
        #region 필드
        private static readonly Color groupColor = new Color32(222, 184, 135, 255);
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 인스펙터에 표시할 그룹 이름입니다.
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// 다음 그룹 어트리뷰트 전까지의 필드를 현재 그룹에 포함할지 여부입니다.
        /// </summary>
        public bool GroupAllFieldsUntilNextGroupAttribute { get; private set; }

        /// <summary>
        /// 그룹의 표시 색상입니다.
        /// </summary>
        public Color GroupColor { get; private set; }

        /// <summary>
        /// 그룹을 기본적으로 접힌 상태로 표시할지 여부입니다.
        /// </summary>
        public bool ClosedByDefault { get; private set; }
        #endregion // 프로퍼티


        /// <summary>
        /// 기본 그룹 색상으로 그룹 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="groupName">인스펙터에 표시할 그룹 이름입니다.</param>
        /// <param name="groupAllFieldsUntilNextGroupAttribute">다음 그룹 어트리뷰트 전까지 필드를 모두 포함할지 여부입니다.</param>
        /// <param name="closedByDefault">그룹을 기본적으로 접힌 상태로 시작할지 여부입니다.</param>
        public SWGroupAttribute(string groupName, bool groupAllFieldsUntilNextGroupAttribute = true, bool closedByDefault = false)
        {
            this.GroupName = groupName;
            this.GroupAllFieldsUntilNextGroupAttribute = groupAllFieldsUntilNextGroupAttribute;
            this.GroupColor = groupColor;
            this.ClosedByDefault = closedByDefault;
        }

        /// <summary>
        /// 그룹 색상을 직접 지정해 그룹 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="groupName">인스펙터에 표시할 그룹 이름입니다.</param>
        /// <param name="color">그룹에 적용할 색상입니다.</param>
        /// <param name="groupAllFieldsUntilNextGroupAttribute">다음 그룹 어트리뷰트 전까지 필드를 모두 포함할지 여부입니다.</param>
        /// <param name="closedByDefault">그룹을 기본적으로 접힌 상태로 시작할지 여부입니다.</param>
        public SWGroupAttribute(string groupName, Color color, bool groupAllFieldsUntilNextGroupAttribute = true,  bool closedByDefault = false)
        {
            this.GroupName = groupName;
            this.GroupAllFieldsUntilNextGroupAttribute = groupAllFieldsUntilNextGroupAttribute;
            this.GroupColor = color;
            this.ClosedByDefault = closedByDefault;
        }
    }
}
