using UnityEngine;

using SW.Attributes;

using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 에셋으로 구성하는 퀘스트 보상의 기본 클래스입니다.
    /// 프로젝트별 재화, 아이템 또는 경험치 지급 클래스가 이 타입을 상속합니다.
    /// </summary>
    public abstract class SWQuestReward : SWScriptableObject, ISWQuestReward
    {
        #region 필드
        [SWGroup("보상 정보")]
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string description;
        [SerializeField] private int quantity = 1;
        [SerializeField, HideInInspector] private string rewardIdentifier;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public Sprite Icon => icon;
        /// <inheritdoc />
        public string Description => description;
        /// <inheritdoc />
        public int Quantity => quantity;

        /// <summary>지급 기록에 사용하는 고정 식별자입니다. 에셋은 편집기에서 자산 식별자를 저장합니다.</summary>
        public string Identifier => string.IsNullOrEmpty(rewardIdentifier)
            ? GetType().FullName + ":" + name
            : rewardIdentifier;
        #endregion // 프로퍼티

#if UNITY_EDITOR
        /// <summary>복제·이름 변경 뒤에도 각 보상 에셋의 식별자를 유지합니다.</summary>
        protected virtual void OnValidate()
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
                rewardIdentifier = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
        }
#endif

        /// <inheritdoc />
        public abstract void Grant(SWQuestSystem questSystem, SWQuest quest);
    }
}
