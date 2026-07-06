using System.Collections;
using UnityEngine;

namespace SW.Popup
{
    /// <summary>
    /// 팝업 숨김 시 원래 크기에서 살짝 커졌다가 작아지는 기본 숨김 연출입니다.
    /// </summary>
    /// <remarks>
    /// DOTween 없이 코루틴으로 동일한 연출을 재생합니다.
    /// 직렬화 필드 이름이 기존과 동일하므로 이미 만들어둔 에셋 데이터가 그대로 유지됩니다.
    /// </remarks>
    [CreateAssetMenu(menuName = "SWUtils/Popup Hide Effects/Scale", fileName = "SWPopupScaleHideEffect")]
    public class SWPopupScaleHideEffect : SWPopupHideEffect
    {
        #region 필드
        [SerializeField] private float delay;
        [SerializeField] private float scaleUpDuration = 0.1f;
        [SerializeField] private float scaleDownDuration = 0.25f;
        [SerializeField] private float scaleUp = 1f;
        [SerializeField] private float endScale;
        #endregion // 필드

        #region 재생
        /// <inheritdoc/>
        /// <inheritdoc />
        public override SWPopupEffectHandle Play(SWPopupBase popup, Transform target)
        {
            if (target == null) return null;

            target.localScale = Vector3.one;

            return SWPopupEffectHandle.Run(popup, PlayRoutine(target));
        }

        /// <summary>
        /// 스케일 숨김 연출 코루틴 본체입니다.
        /// </summary>
        /// <param name="target">연출 대상 Transform입니다.</param>
        /// <returns>IEnumerator입니다.</returns>
        private IEnumerator PlayRoutine(Transform target)
        {
            if (delay > 0f)
                yield return SWPopupEffectRoutines.WaitRealtime(delay);

            yield return SWPopupEffectRoutines.ScaleTo(
                target, Vector3.one * scaleUp, Mathf.Max(0f, scaleUpDuration));

            yield return SWPopupEffectRoutines.ScaleTo(
                target, Vector3.one * endScale, Mathf.Max(0f, scaleDownDuration));
        }
        #endregion // 재생
    }
}
