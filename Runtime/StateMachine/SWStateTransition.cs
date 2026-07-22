using System;

namespace SW.StateMachine
{
    /// <summary>
    /// 상태 사이의 전이 조건과 대상을 보관합니다.
    /// </summary>
    /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
    internal sealed class SWStateTransition<TContext>
    {
        #region 프로퍼티
        /// <summary>출발 상태 타입입니다. 모든 상태 전이라면 null입니다.</summary>
        public Type FromStateType { get; }

        /// <summary>도착 상태 타입입니다.</summary>
        public Type ToStateType { get; }

        /// <summary>전이를 실행하는 명령 번호입니다. 자동 전이라면 값이 없습니다.</summary>
        public int? Command { get; }

        /// <summary>현재 상태와 같은 상태로 다시 진입할 수 있는지 여부입니다.</summary>
        public bool CanReenter { get; }
        #endregion // 프로퍼티

        #region 필드
        private readonly Func<SWState<TContext>, bool> condition;
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 상태 전이를 생성합니다.
        /// </summary>
        public SWStateTransition(
            Type fromStateType,
            Type toStateType,
            int? command,
            Func<SWState<TContext>, bool> condition,
            bool canReenter)
        {
            FromStateType = fromStateType;
            ToStateType = toStateType;
            Command = command;
            this.condition = condition;
            CanReenter = canReenter;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 현재 상태에서 전이 조건을 만족하는지 확인합니다.
        /// </summary>
        public bool IsConditionMet(SWState<TContext> currentState)
        {
            return condition == null || condition(currentState);
        }
        #endregion // 함수
    }
}
