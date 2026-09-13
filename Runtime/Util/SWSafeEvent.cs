using System;

namespace SW.Util
{
    /// <summary>외부 구독자 하나의 예외가 나머지 알림과 내부 정리를 중단하지 않게 합니다.</summary>
    internal static class SWSafeEvent
    {
        /// <summary>호출 시점의 구독 목록을 사용하여 각 처리자를 독립적으로 호출합니다.</summary>
        internal static void Invoke<THandler>(THandler handlers, Action<THandler> invoke) where THandler : Delegate
        {
            if (handlers == null) return;
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try { invoke((THandler)handler); }
                catch (Exception exception) { SWLog.LogError($"[SWUtils] 이벤트 처리 실패: {exception}"); }
            }
        }
    }
}
