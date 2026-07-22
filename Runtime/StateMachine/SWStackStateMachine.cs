using System;
using System.Collections.Generic;

namespace SW.StateMachine
{
    /// <summary>
    /// 스택 상태 머신에서 발생한 변경 종류입니다.
    /// </summary>
    public enum SWStackStateOperation
    {
        /// <summary>새 상태가 최상단에 추가되었습니다.</summary>
        Push,

        /// <summary>최상단 상태가 제거되었습니다.</summary>
        Pop,

        /// <summary>최상단 상태가 다른 상태로 교체되었습니다.</summary>
        Replace,

        /// <summary>스택의 모든 상태가 제거되었습니다.</summary>
        Clear,
    }

    /// <summary>
    /// 이전 상태를 유지하면서 새 상태를 최상단에 쌓을 수 있는 범용 스택 상태 머신입니다.
    /// 최상단 상태만 갱신되며 아래 상태는 제거되지 않고 일시 정지됩니다.
    /// </summary>
    /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
    public sealed class SWStackStateMachine<TContext>
    {
        #region 필드
        private readonly Dictionary<Type, SWStackState<TContext>> registeredStates =
            new Dictionary<Type, SWStackState<TContext>>();
        private readonly List<SWStackState<TContext>> stateStack = new List<SWStackState<TContext>>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>상태들이 제어하는 문맥입니다.</summary>
        public TContext Context { get; }

        /// <summary>상태 머신이 실행 중인지 여부입니다.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>현재 스택에 들어 있는 상태 수입니다.</summary>
        public int Count => stateStack.Count;

        /// <summary>스택 최상단의 현재 상태입니다. 스택이 비어 있으면 null입니다.</summary>
        public SWStackState<TContext> CurrentState =>
            stateStack.Count == 0 ? null : stateStack[stateStack.Count - 1];
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>스택의 현재 상태가 변경되었을 때 호출됩니다.</summary>
        public event Action<
            SWStackStateMachine<TContext>,
            SWStackState<TContext>,
            SWStackState<TContext>,
            SWStackStateOperation> StateChanged;
        #endregion // 이벤트

        #region 생성자
        /// <summary>
        /// 지정한 문맥을 제어하는 스택 상태 머신을 생성합니다.
        /// </summary>
        /// <param name="context">상태들이 제어할 문맥입니다.</param>
        public SWStackStateMachine(TContext context)
        {
            if (ReferenceEquals(context, null))
                throw new ArgumentNullException(nameof(context));

            Context = context;
        }
        #endregion // 생성자

        #region 상태 등록
        /// <summary>
        /// 새 상태를 생성하여 상태 머신에 등록합니다.
        /// </summary>
        /// <typeparam name="TState">등록할 상태 타입입니다.</typeparam>
        /// <returns>생성하여 등록한 상태입니다.</returns>
        public TState AddState<TState>() where TState : SWStackState<TContext>, new()
        {
            TState state = new TState();
            AddStateInternal(state);
            return state;
        }

        /// <summary>
        /// 런타임 타입으로 새 상태를 생성하여 상태 머신에 등록합니다.
        /// </summary>
        /// <param name="stateType">등록할 상태 구현 타입입니다.</param>
        /// <returns>생성하여 등록한 상태입니다.</returns>
        public SWStackState<TContext> AddState(Type stateType)
        {
            if (stateType == null)
                throw new ArgumentNullException(nameof(stateType));

            if (stateType.IsAbstract || !typeof(SWStackState<TContext>).IsAssignableFrom(stateType))
                throw new ArgumentException($"{stateType.FullName} 타입은 사용할 수 있는 스택 상태 타입이 아닙니다.", nameof(stateType));

            SWStackState<TContext> state = Activator.CreateInstance(stateType) as SWStackState<TContext>;
            if (state == null)
                throw new InvalidOperationException($"{stateType.FullName} 상태를 생성할 수 없습니다.");

            AddStateInternal(state);
            return state;
        }

        /// <summary>
        /// 상태 인스턴스를 상태 머신에 등록합니다.
        /// </summary>
        /// <typeparam name="TState">등록할 상태 타입입니다.</typeparam>
        /// <param name="state">등록할 상태입니다.</param>
        /// <returns>등록한 상태입니다.</returns>
        public TState AddState<TState>(TState state) where TState : SWStackState<TContext>
        {
            AddStateInternal(state);
            return state;
        }

        /// <summary>상태 인스턴스를 실제로 등록합니다.</summary>
        private void AddStateInternal(SWStackState<TContext> state)
        {
            EnsureNotRunning();

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Type stateType = state.GetType();
            if (registeredStates.ContainsKey(stateType))
                throw new InvalidOperationException($"{stateType.Name} 상태가 이미 등록되어 있습니다.");

            state.Initialize(this, Context);
            registeredStates.Add(stateType, state);
        }
        #endregion // 상태 등록

        #region 실행
        /// <summary>
        /// 지정한 상태를 초기 상태로 사용하여 상태 머신을 시작합니다.
        /// </summary>
        /// <typeparam name="TInitialState">처음 진입할 상태 타입입니다.</typeparam>
        public void Start<TInitialState>() where TInitialState : SWStackState<TContext>
        {
            Start(typeof(TInitialState));
        }

        /// <summary>
        /// 런타임 타입으로 지정한 상태를 초기 상태로 사용하여 상태 머신을 시작합니다.
        /// </summary>
        /// <param name="initialStateType">처음 진입할 상태 타입입니다.</param>
        public void Start(Type initialStateType)
        {
            if (IsRunning)
                throw new InvalidOperationException("스택 상태 머신이 이미 실행 중입니다.");

            EnsureStateExists(initialStateType);
            IsRunning = true;
            PushInternal(initialStateType, SWStackStateOperation.Push);
        }

        /// <summary>
        /// 최상단의 현재 상태를 한 번 갱신합니다.
        /// </summary>
        /// <param name="deltaTime">이전 갱신 이후 흐른 시간입니다.</param>
        public void Tick(float deltaTime)
        {
            EnsureRunning();
            CurrentState?.Tick(deltaTime);
        }

        /// <summary>
        /// 스택에 남아 있는 상태를 모두 종료하고 상태 머신을 정지합니다.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            ClearInternal(false);
            IsRunning = false;
        }
        #endregion // 실행

        #region 스택 제어
        /// <summary>
        /// 현재 상태를 일시 정지하고 지정한 상태를 최상단에 추가합니다.
        /// </summary>
        /// <typeparam name="TState">추가할 상태 타입입니다.</typeparam>
        public void Push<TState>() where TState : SWStackState<TContext>
        {
            Push(typeof(TState));
        }

        /// <summary>
        /// 런타임 타입으로 지정한 상태를 최상단에 추가합니다.
        /// </summary>
        /// <param name="stateType">추가할 상태 타입입니다.</param>
        public void Push(Type stateType)
        {
            EnsureRunning();
            EnsureStateExists(stateType);
            PushInternal(stateType, SWStackStateOperation.Push);
        }

        /// <summary>
        /// 최상단 상태를 제거하고 아래 상태를 다시 활성화합니다.
        /// </summary>
        /// <returns>제거한 상태가 있으면 true입니다.</returns>
        public bool Pop()
        {
            EnsureRunning();

            if (stateStack.Count == 0)
                return false;

            SWStackState<TContext> previousState = CurrentState;
            previousState.Exit();
            stateStack.RemoveAt(stateStack.Count - 1);

            SWStackState<TContext> currentState = CurrentState;
            currentState?.Resume();
            StateChanged?.Invoke(this, currentState, previousState, SWStackStateOperation.Pop);
            return true;
        }

        /// <summary>
        /// 최상단 상태를 종료하고 지정한 상태로 교체합니다.
        /// 아래에 있는 상태는 중간에 다시 활성화되지 않습니다.
        /// </summary>
        /// <typeparam name="TState">교체할 상태 타입입니다.</typeparam>
        public void Replace<TState>() where TState : SWStackState<TContext>
        {
            Replace(typeof(TState));
        }

        /// <summary>
        /// 최상단 상태를 런타임 타입으로 지정한 상태로 교체합니다.
        /// </summary>
        /// <param name="stateType">교체할 상태 타입입니다.</param>
        public void Replace(Type stateType)
        {
            EnsureRunning();
            EnsureStateExists(stateType);

            SWStackState<TContext> previousState = CurrentState;
            SWStackState<TContext> newState = registeredStates[stateType];

            if (stateStack.Contains(newState) && previousState != newState)
                throw new InvalidOperationException($"{stateType.Name} 상태가 이미 스택에 들어 있습니다.");

            if (previousState != null)
            {
                previousState.Exit();
                stateStack.RemoveAt(stateStack.Count - 1);
            }

            stateStack.Add(newState);
            newState.Enter();
            StateChanged?.Invoke(this, newState, previousState, SWStackStateOperation.Replace);
        }

        /// <summary>
        /// 스택에 있는 모든 상태를 최상단부터 종료하고 제거합니다.
        /// 상태 머신은 실행 상태를 유지하므로 이후 새 상태를 추가할 수 있습니다.
        /// </summary>
        public void Clear()
        {
            EnsureRunning();
            ClearInternal(true);
        }

        /// <summary>지정한 상태를 스택에 추가합니다.</summary>
        private void PushInternal(Type stateType, SWStackStateOperation operation)
        {
            SWStackState<TContext> newState = registeredStates[stateType];
            if (stateStack.Contains(newState))
                throw new InvalidOperationException($"{stateType.Name} 상태가 이미 스택에 들어 있습니다.");

            SWStackState<TContext> previousState = CurrentState;
            previousState?.Pause();
            stateStack.Add(newState);
            newState.Enter();
            StateChanged?.Invoke(this, newState, previousState, operation);
        }

        /// <summary>스택을 실제로 비웁니다.</summary>
        private void ClearInternal(bool notifyStateChanged)
        {
            SWStackState<TContext> previousState = CurrentState;

            for (int index = stateStack.Count - 1; index >= 0; index--)
            {
                stateStack[index].Exit();
            }

            stateStack.Clear();

            if (notifyStateChanged && previousState != null)
                StateChanged?.Invoke(this, null, previousState, SWStackStateOperation.Clear);
        }
        #endregion // 스택 제어

        #region 메시지와 조회
        /// <summary>
        /// 최상단의 현재 상태에 메시지를 전달합니다.
        /// </summary>
        public bool SendMessage(int message, object data = null)
        {
            EnsureRunning();
            return CurrentState != null && CurrentState.ReceiveMessage(message, data);
        }

        /// <summary>
        /// 최상단의 현재 상태에 열거형 메시지를 전달합니다.
        /// </summary>
        public bool SendMessage(Enum message, object data = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return SendMessage(Convert.ToInt32(message), data);
        }

        /// <summary>
        /// 지정한 타입의 상태가 등록되어 있는지 확인합니다.
        /// </summary>
        public bool IsRegistered<TState>() where TState : SWStackState<TContext>
        {
            return registeredStates.ContainsKey(typeof(TState));
        }

        /// <summary>
        /// 지정한 타입의 상태가 현재 스택에 들어 있는지 확인합니다.
        /// </summary>
        public bool Contains<TState>() where TState : SWStackState<TContext>
        {
            return Contains(typeof(TState));
        }

        /// <summary>
        /// 지정한 타입의 상태가 현재 스택에 들어 있는지 확인합니다.
        /// </summary>
        internal bool Contains(Type stateType)
        {
            return registeredStates.TryGetValue(stateType, out SWStackState<TContext> state) &&
                stateStack.Contains(state);
        }

        /// <summary>
        /// 최상단의 현재 상태가 지정한 타입인지 확인합니다.
        /// </summary>
        public bool IsCurrentState<TState>() where TState : SWStackState<TContext>
        {
            return CurrentState != null && CurrentState.GetType() == typeof(TState);
        }

        /// <summary>
        /// 등록된 상태를 반환합니다.
        /// </summary>
        public TState GetState<TState>() where TState : SWStackState<TContext>
        {
            EnsureStateExists<TState>();
            return (TState)registeredStates[typeof(TState)];
        }
        #endregion // 메시지와 조회

        #region 검증
        /// <summary>지정한 상태가 등록되어 있는지 확인합니다.</summary>
        private void EnsureStateExists<TState>() where TState : SWStackState<TContext>
        {
            EnsureStateExists(typeof(TState));
        }

        /// <summary>런타임 타입으로 지정한 상태가 등록되어 있는지 확인합니다.</summary>
        private void EnsureStateExists(Type stateType)
        {
            if (stateType == null || !registeredStates.ContainsKey(stateType))
                throw new KeyNotFoundException($"{stateType?.Name} 상태가 등록되어 있지 않습니다.");
        }

        /// <summary>상태 머신이 실행 중이지 않은지 확인합니다.</summary>
        private void EnsureNotRunning()
        {
            if (IsRunning)
                throw new InvalidOperationException("실행 중인 스택 상태 머신의 구성을 변경할 수 없습니다.");
        }

        /// <summary>상태 머신이 실행 중인지 확인합니다.</summary>
        private void EnsureRunning()
        {
            if (!IsRunning)
                throw new InvalidOperationException("스택 상태 머신이 실행 중이 아닙니다.");
        }
        #endregion // 검증
    }
}
