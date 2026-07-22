using System;

namespace SW.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프 연결에서 사용할 코드 기반 조건의 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="TContext">조건을 평가할 문맥 타입입니다.</typeparam>
    public abstract class SWStateMachineGraphCondition<TContext>
    {
        #region 함수
        /// <summary>
        /// 현재 문맥이 상태 전이 조건을 만족하는지 평가합니다.
        /// </summary>
        /// <param name="context">상태 머신이 제어하는 현재 문맥입니다.</param>
        /// <returns>상태를 전이할 수 있으면 true입니다.</returns>
        public abstract bool Evaluate(TContext context);
        #endregion // 함수
    }
}

