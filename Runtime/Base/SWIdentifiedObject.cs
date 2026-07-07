using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

namespace SW.Base
{
    /// <summary>
    /// ID, 코드명, 표시명, 카테고리를 가진 데이터 에셋.
    /// </summary>
    /// <remarks>
    /// 스탯, 스킬, 아이템처럼 "정의에셋 + 런타임 복제본" 구조로 쓰이는 데이터 공통 베이스
    /// </remarks>
    [CreateAssetMenu(fileName = "SWIdentifiedObject", menuName = "SWBase/Identified Object")]
    public class SWIdentifiedObject : SWScriptableObject, ICloneable
    {
        #region 필드
        [SWGroup("데이터 정의")]
        [SerializeField] private SWCategory[] categories;
        [SerializeField] private int id;
        [SerializeField] private string codeName;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>고유 ID</summary>
        public int ID => id;
        /// <summary>코드 이름</summary>
        public string CodeName => codeName;
        /// <summary>표시용 이름, 비어 있으면 에셋 이름</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>설명</summary>
        public virtual string Description => description;

        /// <summary>카테고리 목록</summary>
        public IReadOnlyList<SWCategory> Categories => categories ?? Array.Empty<SWCategory>();
        #endregion // 프로퍼티

        #region 복사
        /// <summary>
        /// 런타임 복제본을 생성합니다.
        /// </summary>
        /// <returns>복제된 객체</returns>
        public virtual object Clone()
            => Instantiate(this);
        #endregion // 복사

        /// <summary>
        /// 지정한 카테고리에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="category">확인할 카테고리</param>
        /// <returns>카테고리에 포함되어있으면 true</returns>
        public bool HasCategory(SWCategory category)
        {
            if (category == null || categories == null)
            {
                return false;
            }

            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] != null && categories[i].ID == category.ID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 카테고리에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="category">확인할 카테고리</param>
        /// <returns>카테고리에 포함되어있으면 true</returns>
        public bool HasCategory(string category)
        {
            if (categories == null)
            {
                return false;
            }

            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] != null && categories[i] == category)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
