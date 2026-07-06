using UnityEngine;

namespace SW.Popup
{
    /// <summary>
    /// 팝업이 숨겨질 때 재생되는 연출의 ScriptableObject 기반 클래스입니다.
    /// </summary>
    /// <remarks>
    /// DOTween 의존성이 제거되었습니다. 연출은 코루틴 기반 <see cref="SWPopupEffectHandle"/>로 재생됩니다.
    /// </remarks>
    public abstract class SWPopupHideEffect : ScriptableObject
    {
        /// <summary>
        /// 팝업 숨김 연출을 단일 대상에 재생합니다.
        /// </summary>
        /// <param name="popup">숨겨지는 팝업입니다.</param>
        /// <param name="target">연출을 적용할 Transform입니다.</param>
        /// <returns>재생된 연출 핸들. 연출이 없으면 null일 수 있습니다.</returns>
        public abstract SWPopupEffectHandle Play(SWPopupBase popup, Transform target);
    }
}
