using System;

namespace SW.StateMachine
{
    /// <summary>
    /// 스택 상태 머신에서 실행되는 상태의 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
    public abstract class SWStackState<TContext>
    {
        #region 프로퍼티
        /// <summary>이 상태를 소유하는 스택 상태 머신입니다.</summary>
        public SWStackStateMachine<TContext> StateMachine { get; private set; }

        /// <summary>상태가 제어할 문맥입니다.</summary>
        public TContext Context { get; private set; }

        /// <summary>상태가 현재 스택에 포함되어 있는지 여부입니다.</summary>
        public bool IsInStack => StateMachine != null && StateMachine.Contains(GetType());

        /// <summary>상태가 스택 최상단의 현재 상태인지 여부입니다.</summary>
        public bool IsCurrent => StateMachine != null && StateMachine.CurrentState == this;
        #endregion // 프로퍼티

        #region 내부 함수
        /// <summary>상태를 스택 상태 머신에 연결하고 초기화합니다.</summary>
        internal void Initialize(SWStackStateMachine<TContext> stateMachine, TContext context)
        {
            if (StateMachine != null)
                throw new InvalidOperationException($"{GetType().Name} 상태는 이미 상태 머신에 등록되어 있습니다.");

            StateMachine = stateMachine;
            Context = context;
            OnInitialize();
        }

        /// <summary>상태 진입 처리를 실행합니다.</summary>
        internal void Enter()
        {
            OnEnter();
        }

        /// <summary>상위 상태에 가려질 때의 처리를 실행합니다.</summary>
        internal void Pause()
        {
            OnPause();
        }

        /// <summary>다시 최상단 상태가 될 때의 처리를 실행합니다.</summary>
        internal void Resume()
        {
            OnResume();
        }

        /// <summary>현재 상태 갱신 처리를 실행합니다.</summary>
        internal void Tick(float deltaTime)
        {
            OnTick(deltaTime);
        }

        /// <summary>상태 종료 처리를 실행합니다.</summary>
        internal void Exit()
        {
            OnExit();
        }

        /// <summary>상태 메시지를 전달합니다.</summary>
        internal bool ReceiveMessage(int message, object data)
        {
            return OnReceiveMessage(message, data);
        }
        #endregion // 내부 함수

        #region 상태 생명주기
        /// <summary>상태가 상태 머신에 처음 등록될 때 호출됩니다.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>상태가 스택에 들어와 현재 상태가 될 때 호출됩니다.</summary>
        protected virtual void OnEnter() { }

        /// <summary>다른 상태가 위에 쌓여 현재 상태가 가려질 때 호출됩니다.</summary>
        protected virtual void OnPause() { }

        /// <summary>위에 있던 상태가 제거되어 다시 현재 상태가 될 때 호출됩니다.</summary>
        protected virtual void OnResume() { }

        /// <summary>상태가 최상단의 현재 상태인 동안 갱신마다 호출됩니다.</summary>
        /// <param name="deltaTime">이전 갱신 이후 흐른 시간입니다.</param>
        protected virtual void OnTick(float deltaTime) { }

        /// <summary>상태가 스택에서 제거될 때 호출됩니다.</summary>
        protected virtual void OnExit() { }

        /// <summary>현재 상태로 전달된 메시지를 처리합니다.</summary>
        /// <param name="message">메시지 번호입니다.</param>
        /// <param name="data">메시지와 함께 전달된 데이터입니다.</param>
        /// <returns>메시지를 처리했으면 true입니다.</returns>
        protected virtual bool OnReceiveMessage(int message, object data)
        {
            return false;
        }
        #endregion // 상태 생명주기
    }
}

