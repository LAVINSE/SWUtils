using System;
using System.Collections.Generic;

namespace SW.StateMachine
{
    /// <summary>
    /// 여러 계층을 독립적으로 실행하는 범용 유한 상태 머신입니다.
    /// 조건 전이, 명령 전이, 모든 상태 전이와 상태 메시지를 지원합니다.
    /// </summary>
    /// <typeparam name="TContext">상태가 제어할 문맥 타입입니다.</typeparam>
    public sealed class SWStateMachine<TContext>
    {
        /// <summary>
        /// 하나의 계층에 등록된 상태와 전이를 관리합니다.
        /// </summary>
        private sealed class LayerData
        {
            #region 프로퍼티
            /// <summary>계층에 등록된 상태입니다.</summary>
            public Dictionary<Type, SWState<TContext>> States { get; } = new Dictionary<Type, SWState<TContext>>();

            /// <summary>계층에 등록된 전이입니다.</summary>
            public List<SWStateTransition<TContext>> Transitions { get; } = new List<SWStateTransition<TContext>>();

            /// <summary>계층이 시작할 때 진입할 상태 타입입니다.</summary>
            public Type InitialStateType { get; set; }

            /// <summary>현재 실행 중인 상태입니다.</summary>
            public SWState<TContext> CurrentState { get; set; }
            #endregion // 프로퍼티
        }

        #region 필드
        private readonly SortedDictionary<int, LayerData> layers = new SortedDictionary<int, LayerData>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>상태들이 제어하는 문맥입니다.</summary>
        public TContext Context { get; }

        /// <summary>상태 머신이 실행 중인지 여부입니다.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>등록된 계층 수입니다.</summary>
        public int LayerCount => layers.Count;
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>계층의 현재 상태가 변경되었을 때 호출됩니다.</summary>
        public event Action<SWStateMachine<TContext>, SWState<TContext>, SWState<TContext>, int> StateChanged;
        #endregion // 이벤트

        #region 생성자
        /// <summary>
        /// 지정한 문맥을 제어하는 상태 머신을 생성합니다.
        /// </summary>
        /// <param name="context">상태들이 제어할 문맥입니다.</param>
        public SWStateMachine(TContext context)
        {
            if (ReferenceEquals(context, null))
                throw new ArgumentNullException(nameof(context));

            Context = context;
        }
        #endregion // 생성자

        #region 상태 등록
        /// <summary>
        /// 새 상태를 생성하여 계층에 등록합니다.
        /// 계층에 처음 등록된 상태는 별도 지정이 없으면 초기 상태가 됩니다.
        /// </summary>
        /// <typeparam name="TState">등록할 상태 타입입니다.</typeparam>
        /// <param name="layer">상태를 등록할 계층 번호입니다.</param>
        /// <returns>생성하여 등록한 상태입니다.</returns>
        public TState AddState<TState>(int layer = 0) where TState : SWState<TContext>, new()
        {
            TState state = new TState();
            AddStateInternal(state, layer);
            return state;
        }

        /// <summary>
        /// 런타임 타입으로 새 상태를 생성하여 계층에 등록합니다.
        /// </summary>
        /// <param name="stateType">등록할 상태 구현 타입입니다.</param>
        /// <param name="layer">상태를 등록할 계층 번호입니다.</param>
        /// <returns>생성하여 등록한 상태입니다.</returns>
        public SWState<TContext> AddState(Type stateType, int layer = 0)
        {
            if (stateType == null)
                throw new ArgumentNullException(nameof(stateType));

            if (stateType.IsAbstract || !typeof(SWState<TContext>).IsAssignableFrom(stateType))
                throw new ArgumentException($"{stateType.FullName} 타입은 사용할 수 있는 상태 타입이 아닙니다.", nameof(stateType));

            SWState<TContext> state = Activator.CreateInstance(stateType) as SWState<TContext>;
            if (state == null)
                throw new InvalidOperationException($"{stateType.FullName} 상태를 생성할 수 없습니다.");

            AddStateInternal(state, layer);
            return state;
        }

        /// <summary>
        /// 상태 인스턴스를 계층에 등록합니다.
        /// 계층에 처음 등록된 상태는 별도 지정이 없으면 초기 상태가 됩니다.
        /// </summary>
        /// <typeparam name="TState">등록할 상태 타입입니다.</typeparam>
        /// <param name="state">등록할 상태입니다.</param>
        /// <param name="layer">상태를 등록할 계층 번호입니다.</param>
        /// <returns>등록한 상태입니다.</returns>
        public TState AddState<TState>(TState state, int layer = 0) where TState : SWState<TContext>
        {
            AddStateInternal(state, layer);
            return state;
        }

        /// <summary>상태 인스턴스를 실제로 계층에 등록합니다.</summary>
        private void AddStateInternal(SWState<TContext> state, int layer)
        {
            EnsureNotRunning();

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            LayerData layerData = GetOrCreateLayer(layer);
            Type stateType = state.GetType();

            if (layerData.States.ContainsKey(stateType))
                throw new InvalidOperationException($"{layer}번 계층에 {stateType.Name} 상태가 이미 등록되어 있습니다.");

            state.Initialize(this, Context, layer);
            layerData.States.Add(stateType, state);

            if (layerData.InitialStateType == null)
                layerData.InitialStateType = stateType;

        }

        /// <summary>
        /// 계층이 시작할 때 진입할 초기 상태를 지정합니다.
        /// </summary>
        /// <typeparam name="TState">초기 상태 타입입니다.</typeparam>
        /// <param name="layer">초기 상태를 지정할 계층 번호입니다.</param>
        public void SetInitialState<TState>(int layer = 0) where TState : SWState<TContext>
        {
            SetInitialState(typeof(TState), layer);
        }

        /// <summary>
        /// 런타임 타입으로 계층의 초기 상태를 지정합니다.
        /// </summary>
        /// <param name="stateType">초기 상태로 사용할 타입입니다.</param>
        /// <param name="layer">초기 상태를 지정할 계층 번호입니다.</param>
        public void SetInitialState(Type stateType, int layer = 0)
        {
            EnsureNotRunning();
            LayerData layerData = GetLayer(layer);

            if (!layerData.States.ContainsKey(stateType))
                throw new KeyNotFoundException($"{layer}번 계층에 {stateType?.Name} 상태가 등록되어 있지 않습니다.");

            layerData.InitialStateType = stateType;
        }
        #endregion // 상태 등록

        #region 일반 전이 등록
        /// <summary>
        /// 조건이 만족되면 자동으로 실행되는 상태 전이를 등록합니다.
        /// </summary>
        public void AddTransition<TFromState, TToState>(
            Func<SWState<TContext>, bool> condition,
            int layer = 0,
            bool canReenter = false)
            where TFromState : SWState<TContext>
            where TToState : SWState<TContext>
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            AddTransitionInternal<TFromState, TToState>(null, condition, layer, canReenter);
        }

        /// <summary>
        /// 명령을 받으면 실행되는 상태 전이를 등록합니다.
        /// </summary>
        public void AddTransition<TFromState, TToState>(
            int command,
            Func<SWState<TContext>, bool> condition = null,
            int layer = 0,
            bool canReenter = false)
            where TFromState : SWState<TContext>
            where TToState : SWState<TContext>
        {
            AddTransitionInternal<TFromState, TToState>(command, condition, layer, canReenter);
        }

        /// <summary>
        /// 열거형 명령을 받으면 실행되는 상태 전이를 등록합니다.
        /// </summary>
        public void AddTransition<TFromState, TToState>(
            Enum command,
            Func<SWState<TContext>, bool> condition = null,
            int layer = 0,
            bool canReenter = false)
            where TFromState : SWState<TContext>
            where TToState : SWState<TContext>
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            AddTransitionInternal<TFromState, TToState>(Convert.ToInt32(command), condition, layer, canReenter);
        }

        /// <summary>일반 상태 전이를 실제로 등록합니다.</summary>
        private void AddTransitionInternal<TFromState, TToState>(
            int? command,
            Func<SWState<TContext>, bool> condition,
            int layer,
            bool canReenter)
            where TFromState : SWState<TContext>
            where TToState : SWState<TContext>
        {
            EnsureNotRunning();
            LayerData layerData = GetLayer(layer);
            EnsureStateExists<TFromState>(layerData, layer);
            EnsureStateExists<TToState>(layerData, layer);
            layerData.Transitions.Add(new SWStateTransition<TContext>(
                typeof(TFromState), typeof(TToState), command, condition, canReenter));
        }

        /// <summary>그래프 데이터에서 런타임 타입 기반 일반 상태 전이를 등록합니다.</summary>
        internal void AddGraphTransition(
            Type fromStateType,
            Type toStateType,
            int? command,
            Func<SWState<TContext>, bool> condition,
            int layer,
            bool canReenter)
        {
            EnsureNotRunning();
            LayerData layerData = GetLayer(layer);

            if (!layerData.States.ContainsKey(fromStateType) || !layerData.States.ContainsKey(toStateType))
                throw new KeyNotFoundException("그래프 전이에 사용된 상태가 지정한 계층에 등록되어 있지 않습니다.");

            layerData.Transitions.Add(new SWStateTransition<TContext>(
                fromStateType, toStateType, command, condition, canReenter));
        }
        #endregion // 일반 전이 등록

        #region 모든 상태 전이 등록
        /// <summary>
        /// 현재 상태와 관계없이 조건이 만족되면 자동으로 실행되는 전이를 등록합니다.
        /// </summary>
        public void AddAnyTransition<TToState>(
            Func<SWState<TContext>, bool> condition,
            int layer = 0,
            bool canReenter = false)
            where TToState : SWState<TContext>
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            AddAnyTransitionInternal<TToState>(null, condition, layer, canReenter);
        }

        /// <summary>
        /// 현재 상태와 관계없이 명령을 받으면 실행되는 전이를 등록합니다.
        /// </summary>
        public void AddAnyTransition<TToState>(
            int command,
            Func<SWState<TContext>, bool> condition = null,
            int layer = 0,
            bool canReenter = false)
            where TToState : SWState<TContext>
        {
            AddAnyTransitionInternal<TToState>(command, condition, layer, canReenter);
        }

        /// <summary>
        /// 현재 상태와 관계없이 열거형 명령을 받으면 실행되는 전이를 등록합니다.
        /// </summary>
        public void AddAnyTransition<TToState>(
            Enum command,
            Func<SWState<TContext>, bool> condition = null,
            int layer = 0,
            bool canReenter = false)
            where TToState : SWState<TContext>
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            AddAnyTransitionInternal<TToState>(Convert.ToInt32(command), condition, layer, canReenter);
        }

        /// <summary>모든 상태 전이를 실제로 등록합니다.</summary>
        private void AddAnyTransitionInternal<TToState>(
            int? command,
            Func<SWState<TContext>, bool> condition,
            int layer,
            bool canReenter)
            where TToState : SWState<TContext>
        {
            EnsureNotRunning();
            LayerData layerData = GetLayer(layer);
            EnsureStateExists<TToState>(layerData, layer);
            layerData.Transitions.Add(new SWStateTransition<TContext>(
                null, typeof(TToState), command, condition, canReenter));
        }

        /// <summary>그래프 데이터에서 런타임 타입 기반 모든 상태 전이를 등록합니다.</summary>
        internal void AddGraphAnyTransition(
            Type toStateType,
            int? command,
            Func<SWState<TContext>, bool> condition,
            int layer,
            bool canReenter)
        {
            EnsureNotRunning();
            LayerData layerData = GetLayer(layer);

            if (!layerData.States.ContainsKey(toStateType))
                throw new KeyNotFoundException("그래프의 모든 상태 전이 도착점이 지정한 계층에 등록되어 있지 않습니다.");

            layerData.Transitions.Add(new SWStateTransition<TContext>(
                null, toStateType, command, condition, canReenter));
        }
        #endregion // 모든 상태 전이 등록

        #region 실행
        /// <summary>
        /// 모든 계층을 초기 상태로 진입시키고 상태 머신을 시작합니다.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("상태 머신이 이미 실행 중입니다.");

            if (layers.Count == 0)
                throw new InvalidOperationException("등록된 상태 계층이 없습니다.");

            IsRunning = true;

            foreach (KeyValuePair<int, LayerData> pair in layers)
            {
                ChangeState(pair.Value, pair.Value.InitialStateType, pair.Key);
            }
        }

        /// <summary>
        /// 모든 계층의 자동 전이를 확인하고 현재 상태를 갱신합니다.
        /// 계층 번호가 낮은 순서대로 실행됩니다.
        /// </summary>
        /// <param name="deltaTime">이전 갱신 이후 흐른 시간입니다.</param>
        public void Tick(float deltaTime)
        {
            EnsureRunning();

            foreach (KeyValuePair<int, LayerData> pair in layers)
            {
                LayerData layerData = pair.Value;
                if (!TryAutomaticTransition(layerData, pair.Key))
                    layerData.CurrentState.Tick(deltaTime);
            }
        }

        /// <summary>
        /// 모든 계층의 현재 상태를 종료하고 상태 머신을 정지합니다.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            foreach (LayerData layerData in layers.Values)
            {
                layerData.CurrentState?.Exit();
                layerData.CurrentState = null;
            }

            IsRunning = false;
        }

        /// <summary>자동 전이를 등록 순서대로 확인하여 첫 번째로 만족한 전이를 실행합니다.</summary>
        private bool TryAutomaticTransition(LayerData layerData, int layer)
        {
            SWStateTransition<TContext> transition = FindTransition(layerData, null, true);
            if (transition == null)
                return false;

            ChangeState(layerData, transition.ToStateType, layer);
            return true;
        }

        /// <summary>지정한 상태로 전환합니다.</summary>
        private void ChangeState(LayerData layerData, Type stateType, int layer)
        {
            SWState<TContext> previousState = layerData.CurrentState;
            SWState<TContext> newState = layerData.States[stateType];

            previousState?.Exit();
            layerData.CurrentState = newState;
            newState.Enter();
            StateChanged?.Invoke(this, newState, previousState, layer);
        }
        #endregion // 실행

        #region 명령과 메시지
        /// <summary>
        /// 특정 계층에 명령을 전달하여 상태 전이를 시도합니다.
        /// </summary>
        public bool ExecuteCommand(int command, int layer)
        {
            EnsureRunning();
            LayerData layerData = GetLayer(layer);
            SWStateTransition<TContext> transition = FindTransition(layerData, command, false);

            if (transition == null)
                return false;

            ChangeState(layerData, transition.ToStateType, layer);
            return true;
        }

        /// <summary>
        /// 특정 계층에 열거형 명령을 전달하여 상태 전이를 시도합니다.
        /// </summary>
        public bool ExecuteCommand(Enum command, int layer)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return ExecuteCommand(Convert.ToInt32(command), layer);
        }

        /// <summary>
        /// 모든 계층에 명령을 전달하여 상태 전이를 시도합니다.
        /// </summary>
        public bool ExecuteCommand(int command)
        {
            EnsureRunning();
            bool hasTransitioned = false;

            foreach (int layer in layers.Keys)
            {
                if (ExecuteCommand(command, layer))
                    hasTransitioned = true;
            }

            return hasTransitioned;
        }

        /// <summary>
        /// 모든 계층에 열거형 명령을 전달하여 상태 전이를 시도합니다.
        /// </summary>
        public bool ExecuteCommand(Enum command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return ExecuteCommand(Convert.ToInt32(command));
        }

        /// <summary>
        /// 특정 계층의 현재 상태에 메시지를 전달합니다.
        /// </summary>
        public bool SendMessage(int message, int layer, object data = null)
        {
            EnsureRunning();
            return GetLayer(layer).CurrentState.ReceiveMessage(message, data);
        }

        /// <summary>
        /// 특정 계층의 현재 상태에 열거형 메시지를 전달합니다.
        /// </summary>
        public bool SendMessage(Enum message, int layer, object data = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return SendMessage(Convert.ToInt32(message), layer, data);
        }

        /// <summary>
        /// 모든 계층의 현재 상태에 메시지를 전달합니다.
        /// </summary>
        public bool SendMessage(int message, object data = null)
        {
            EnsureRunning();
            bool hasHandled = false;

            foreach (int layer in layers.Keys)
            {
                if (SendMessage(message, layer, data))
                    hasHandled = true;
            }

            return hasHandled;
        }

        /// <summary>
        /// 모든 계층의 현재 상태에 열거형 메시지를 전달합니다.
        /// </summary>
        public bool SendMessage(Enum message, object data = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return SendMessage(Convert.ToInt32(message), data);
        }

        /// <summary>등록 순서대로 실행 가능한 전이를 찾습니다. 모든 상태 전이를 먼저 확인합니다.</summary>
        private SWStateTransition<TContext> FindTransition(LayerData layerData, int? command, bool isAutomatic)
        {
            SWState<TContext> currentState = layerData.CurrentState;

            for (int pass = 0; pass < 2; pass++)
            {
                bool findAnyTransition = pass == 0;

                foreach (SWStateTransition<TContext> transition in layerData.Transitions)
                {
                    bool isAnyTransition = transition.FromStateType == null;
                    if (isAnyTransition != findAnyTransition)
                        continue;

                    if (!isAnyTransition && transition.FromStateType != currentState.GetType())
                        continue;

                    if (isAutomatic != !transition.Command.HasValue)
                        continue;

                    if (command.HasValue && transition.Command != command)
                        continue;

                    if (!transition.CanReenter && transition.ToStateType == currentState.GetType())
                        continue;

                    if (transition.IsConditionMet(currentState))
                        return transition;
                }
            }

            return null;
        }
        #endregion // 명령과 메시지

        #region 조회
        /// <summary>
        /// 특정 계층이 지정한 상태인지 확인합니다.
        /// </summary>
        public bool IsInState<TState>(int layer = 0) where TState : SWState<TContext>
        {
            EnsureRunning();
            return GetLayer(layer).CurrentState.GetType() == typeof(TState);
        }

        /// <summary>
        /// 어느 계층에서든 지정한 상태가 실행 중인지 확인합니다.
        /// </summary>
        public bool IsInStateOnAnyLayer<TState>() where TState : SWState<TContext>
        {
            EnsureRunning();

            foreach (LayerData layerData in layers.Values)
            {
                if (layerData.CurrentState.GetType() == typeof(TState))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 특정 계층의 현재 상태를 반환합니다.
        /// </summary>
        public SWState<TContext> GetCurrentState(int layer = 0)
        {
            EnsureRunning();
            return GetLayer(layer).CurrentState;
        }

        /// <summary>
        /// 특정 계층의 현재 상태 타입을 반환합니다.
        /// </summary>
        public Type GetCurrentStateType(int layer = 0)
        {
            return GetCurrentState(layer).GetType();
        }
        #endregion // 조회

        #region 검증
        /// <summary>계층을 가져오거나 새로 생성합니다.</summary>
        private LayerData GetOrCreateLayer(int layer)
        {
            if (!layers.TryGetValue(layer, out LayerData layerData))
            {
                layerData = new LayerData();
                layers.Add(layer, layerData);
            }

            return layerData;
        }

        /// <summary>등록된 계층을 반환합니다.</summary>
        private LayerData GetLayer(int layer)
        {
            if (!layers.TryGetValue(layer, out LayerData layerData))
                throw new KeyNotFoundException($"{layer}번 계층이 등록되어 있지 않습니다.");

            return layerData;
        }

        /// <summary>상태가 지정한 계층에 등록되어 있는지 확인합니다.</summary>
        private static void EnsureStateExists<TState>(LayerData layerData, int layer) where TState : SWState<TContext>
        {
            if (!layerData.States.ContainsKey(typeof(TState)))
                throw new KeyNotFoundException($"{layer}번 계층에 {typeof(TState).Name} 상태가 등록되어 있지 않습니다.");
        }

        /// <summary>상태 머신이 실행 중이지 않은지 확인합니다.</summary>
        private void EnsureNotRunning()
        {
            if (IsRunning)
                throw new InvalidOperationException("실행 중인 상태 머신의 구성을 변경할 수 없습니다.");
        }

        /// <summary>상태 머신이 실행 중인지 확인합니다.</summary>
        private void EnsureRunning()
        {
            if (!IsRunning)
                throw new InvalidOperationException("상태 머신이 실행 중이 아닙니다.");
        }
        #endregion // 검증
    }
}
