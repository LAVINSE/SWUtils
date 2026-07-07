using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SWSubClassSelectorAttribute 사용 예시를 보여주는 샘플 컴포넌트입니다.
/// </summary>
using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Sample
{
/// <summary>
/// SWSubClassSelectorAttribute 사용 예시를 보여주는 샘플 컴포넌트입니다.
/// </summary>
public class SWSubClassSelectorExample : SWMonoBehaviour
{
    #region 필드
    /// <summary>
    /// 추상 클래스를 기준 타입으로 사용하는 단일 SerializeReference 예시입니다.
    /// Inspector에서는 타입 선택 버튼을 누르면 Skill/Heal, Skill/Damage 항목이 검색 가능한 드롭다운으로 표시됩니다.
    /// HiddenSkillAction은 SWHideInTypeMenuAttribute가 붙어 있어 드롭다운에 표시되지 않습니다.
    /// </summary>
    [SerializeReference]
    [SWSubClassSelector]
    [SerializeField] private SkillAction skillAction;

    /// <summary>
    /// 인터페이스를 기준 타입으로 사용하는 단일 SerializeReference 예시입니다.
    /// Inspector에서는 Reward/Gold, Reward/Item 항목이 드롭다운에 표시됩니다.
    /// SWSubClassSelectorAttribute의 true 옵션 때문에 선택 후 필드 라벨은 ToString 반환값으로 표시됩니다.
    /// </summary>
    [SerializeReference]
    [SWSubClassSelector(true)]
    [SerializeField] private IRewardAction rewardAction;

    /// <summary>
    /// 추상 클래스 리스트를 사용하는 SerializeReference 컬렉션 예시입니다.
    /// Inspector에서 리스트 크기를 늘리면 각 Element마다 별도의 타입 선택 드롭다운이 표시됩니다.
    /// </summary>
    [SerializeReference]
    [SWSubClassSelector]
    [SerializeField] private List<SkillAction> skillActions = new List<SkillAction>();
    #endregion // 필드

    #region 함수
    /// <summary>
    /// 선택된 액션들을 실행합니다.
    /// </summary>
    [SWButton("Run Sub Class Selector Example")]
    private void RunExample()
    {
        skillAction?.Execute();
        rewardAction?.Reward();

        foreach (SkillAction action in skillActions)
        {
            action?.Execute();
        }
    }
    #endregion // 함수

    /// <summary>
    /// 스킬 액션의 기준 추상 클래스입니다.
    /// </summary>
    [Serializable]
    public abstract class SkillAction
    {
        #region 필드
        /// <summary>
        /// Inspector에 표시되는 액션 이름입니다.
        /// </summary>
        [SerializeField] private string actionName;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 스킬 액션을 실행합니다.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// 액션 이름을 문자열로 반환합니다.
        /// </summary>
        /// <returns>액션 이름입니다.</returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(actionName) ? GetType().Name : actionName;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 보상 액션의 기준 인터페이스입니다.
    /// </summary>
    public interface IRewardAction
    {
        #region 함수
        /// <summary>
        /// 보상을 지급합니다.
        /// </summary>
        void Reward();
        #endregion // 함수
    }

    /// <summary>
    /// 체력을 회복하는 스킬 액션입니다.
    /// </summary>
    [Serializable]
    [SWAddTypeMenu("Skill/Heal")]
    public class HealSkillAction : SkillAction
    {
        #region 필드
        /// <summary>
        /// 회복할 체력 값입니다.
        /// </summary>
        [SerializeField] private int healAmount = 10;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 체력 회복 액션을 실행합니다.
        /// </summary>
        public override void Execute()
        {
            SWLog.Log($"Heal {healAmount}");
        }
        #endregion // 함수
    }

    /// <summary>
    /// 피해를 주는 스킬 액션입니다.
    /// </summary>
    [Serializable]
    [SWAddTypeMenu("Skill/Damage")]
    public class DamageSkillAction : SkillAction
    {
        #region 필드
        /// <summary>
        /// 피해량입니다.
        /// </summary>
        [SerializeField] private int damageAmount = 5;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 피해 액션을 실행합니다.
        /// </summary>
        public override void Execute()
        {
            SWLog.Log($"Damage {damageAmount}");
        }
        #endregion // 함수
    }

    /// <summary>
    /// 메뉴에서 숨겨지는 스킬 액션입니다.
    /// </summary>
    [Serializable]
    [SWHideInTypeMenu]
    public class HiddenSkillAction : SkillAction
    {
        #region 함수
        /// <summary>
        /// 숨김 액션을 실행합니다.
        /// </summary>
        public override void Execute()
        {
            SWLog.Log("Hidden Skill Action");
        }
        #endregion // 함수
    }

    /// <summary>
    /// 골드 보상을 지급하는 액션입니다.
    /// </summary>
    [Serializable]
    [SWAddTypeMenu("Reward/Gold")]
    public class GoldRewardAction : IRewardAction
    {
        #region 필드
        /// <summary>
        /// 지급할 골드 수량입니다.
        /// </summary>
        [SerializeField] private int goldAmount = 100;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 골드 보상을 지급합니다.
        /// </summary>
        public void Reward()
        {
            SWLog.Log($"Reward Gold {goldAmount}");
        }

        /// <summary>
        /// 보상 이름을 문자열로 반환합니다.
        /// </summary>
        /// <returns>보상 이름입니다.</returns>
        public override string ToString()
        {
            return $"Gold {goldAmount}";
        }
        #endregion // 함수
    }

    /// <summary>
    /// 아이템 보상을 지급하는 액션입니다.
    /// </summary>
    [Serializable]
    [SWAddTypeMenu("Reward/Item")]
    public class ItemRewardAction : IRewardAction
    {
        #region 필드
        /// <summary>
        /// 지급할 아이템 식별자입니다.
        /// </summary>
        [SerializeField] private string itemId = "item_001";
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 아이템 보상을 지급합니다.
        /// </summary>
        public void Reward()
        {
            SWLog.Log($"Reward Item {itemId}");
        }

        /// <summary>
        /// 보상 이름을 문자열로 반환합니다.
        /// </summary>
        /// <returns>보상 이름입니다.</returns>
        public override string ToString()
        {
            return itemId;
        }
        #endregion // 함수
    }
}
}
