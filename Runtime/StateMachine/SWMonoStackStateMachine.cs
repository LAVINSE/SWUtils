using System;
using SW.Base;
using UnityEngine;

namespace SW.StateMachine
{
    /// <summary>
    /// 범용 스택 상태 머신을 Unity 컴포넌트 생명주기에서 실행하는 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
    public abstract class SWMonoStackStateMachine<TContext> : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private SWStateMachineUpdateMode updateMode = SWStateMachineUpdateMode.Update;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>실행 중인 범용 스택 상태 머신입니다.</summary>
        public SWStackStateMachine<TContext> StateMachine { get; private set; }

        /// <summary>상태 머신이 초기화되었는지 여부입니다.</summary>
        public bool IsInitialized => StateMachine != null;
        #endregion // 프로퍼티

        #region Unity 생명주기
        /// <summary>일반 프레임 갱신 모드일 때 상태 머신을 갱신합니다.</summary>
        protected virtual void Update()
        {
            if (updateMode == SWStateMachineUpdateMode.Update)
                Tick(Time.deltaTime);
        }

        /// <summary>물리 프레임 갱신 모드일 때 상태 머신을 갱신합니다.</summary>
        protected virtual void FixedUpdate()
        {
            if (updateMode == SWStateMachineUpdateMode.FixedUpdate)
                Tick(Time.fixedDeltaTime);
        }

        /// <summary>컴포넌트가 파괴될 때 실행 중인 상태를 종료합니다.</summary>
        protected virtual void OnDestroy()
        {
            StateMachine?.Stop();
        }
        #endregion // Unity 생명주기

        #region 초기화와 실행
        /// <summary>
        /// 문맥을 사용하여 상태 머신을 구성하고 지정한 초기 상태로 시작합니다.
        /// </summary>
        /// <typeparam name="TInitialState">처음 진입할 상태 타입입니다.</typeparam>
        /// <param name="context">상태들이 제어할 문맥입니다.</param>
        protected void Initialize<TInitialState>(TContext context)
            where TInitialState : SWStackState<TContext>
        {
            if (IsInitialized)
                throw new InvalidOperationException("스택 상태 머신이 이미 초기화되었습니다.");

            StateMachine = new SWStackStateMachine<TContext>(context);
            ConfigureStateMachine(StateMachine);
            StateMachine.Start<TInitialState>();
        }

        /// <summary>
        /// 스택 상태 머신에 사용할 상태를 등록합니다.
        /// </summary>
        /// <param name="stateMachine">구성할 스택 상태 머신입니다.</param>
        protected abstract void ConfigureStateMachine(SWStackStateMachine<TContext> stateMachine);

        /// <summary>
        /// 스택 상태 머신을 한 번 갱신합니다. 수동 갱신 모드에서도 사용할 수 있습니다.
        /// </summary>
        /// <param name="deltaTime">이전 갱신 이후 흐른 시간입니다.</param>
        public void Tick(float deltaTime)
        {
            if (StateMachine != null && StateMachine.IsRunning)
                StateMachine.Tick(deltaTime);
        }
        #endregion // 초기화와 실행
    }
}

