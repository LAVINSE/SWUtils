using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace SW.Util
{
    /// <summary>
    /// 기본 Toggle과 ToggleGroup 동작을 유지하며 선택 상태에 맞춰 텍스트 색상을 바꿉니다.
    /// </summary>
    [AddComponentMenu("SWUtils/SW Toggle Extension")]
    public class SWToggleExtension : Toggle
    {
        #region 필드
        [Tooltip("선택 상태에 따라 색상을 바꿀 탭의 TextMeshPro 텍스트입니다.")]
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color unselectedTextColor = Color.black;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 선택 변경을 한 번 구독하여 활성화할 때마다 리스너를 재생성하지 않습니다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(OnValueChanged);
        }

        /// <summary>
        /// 기본 토글을 활성화한 뒤 현재 색상을 반영합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR
            if (!Application.isPlaying && labelText == null)
            {
                return;
            }
#endif

            RefreshTextColor();
        }

        /// <summary>
        /// 파괴 시 선택 변경 구독과 기본 토글의 그룹 상태를 정리합니다.
        /// </summary>
        protected override void OnDestroy()
        {
            onValueChanged.RemoveListener(OnValueChanged);
            base.OnDestroy();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 컴포넌트를 추가하거나 초기화할 때 자식 텍스트를 기본 참조로 연결합니다.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            labelText = GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// 기본 토글의 편집기 갱신 시 연결된 텍스트의 색상도 미리 보여 줍니다.
        /// </summary>
        public override void Rebuild(CanvasUpdate executing)
        {
            base.Rebuild(executing);
            if (executing == CanvasUpdate.Prelayout && labelText != null)
            {
                RefreshTextColor();
            }
        }
#endif
        #endregion // 초기화

        #region 텍스트 색상
        /// <summary>
        /// 현재 isOn 값으로 텍스트 색상을 갱신합니다.
        /// 알림 없이 토글 값을 바꾼 경우 변경된 각 토글에서 호출합니다.
        /// </summary>
        public void RefreshTextColor()
        {
            labelText.color = isOn ? selectedTextColor : unselectedTextColor;
        }

        /// <summary>
        /// 클릭, 확정 입력 또는 ToggleGroup에 의해 선택이 바뀌면 색상을 갱신합니다.
        /// </summary>
        /// <param name="selected">기본 토글이 전달한 선택 상태입니다.</param>
        private void OnValueChanged(bool selected)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && labelText == null)
            {
                return;
            }
#endif

            RefreshTextColor();
        }
        #endregion // 텍스트 색상
    }
}
