using System;

using SW.Attributes;
using SW.Base;
using SW.BehaviourTree;
using SW.StateMachine;
using SW.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace SW.Example
{
    /// <summary>State Machine과 Behaviour Tree의 그래프 에셋 사용법을 한곳에서 보여주는 예제입니다.</summary>
    [RequireComponent(typeof(SWBehaviourTreeRunner))]
    public sealed class SWGraphAssetsExample : SWMonoBehaviour
    {
        #region 필드
        [Header("Graph Assets")]
        [FormerlySerializedAs("stateMachineGraphAsset")]
        [SerializeField] private SWStateMachineGraphAsset layeredStateMachineGraphAsset;
        [SerializeField] private SWStateMachineGraphAsset stackStateMachineGraphAsset;
        [SerializeField] private SWBehaviourTreeRunner behaviourTreeRunner;

        [Header("Example Conditions")]
        [SerializeField] private bool isMoving;
        [SerializeField] private bool isPaused;

        private SWStateMachine<SWGraphAssetsExample> layeredStateMachine;
        private SWStackStateMachineGraphController<SWGraphAssetsExample> stackStateMachineController;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이동 상태로 전환할 조건이 활성화되어 있는지 반환합니다.</summary>
        public bool IsMoving => isMoving;

        /// <summary>일시정지 상태로 전환할 조건이 활성화되어 있는지 반환합니다.</summary>
        public bool IsPaused => isPaused;
        #endregion // 프로퍼티

        #region Unity 생명주기
        /// <summary>지정된 그래프 에셋으로 상태 머신을 만들고 Behaviour Tree Runner를 확인합니다.</summary>
        private void Awake()
        {
            behaviourTreeRunner ??= GetComponent<SWBehaviourTreeRunner>();

            if (layeredStateMachineGraphAsset != null)
            {
                layeredStateMachine = SWStateMachineGraphFactory.CreateLayered(
                    layeredStateMachineGraphAsset,
                    this);
            }

            if (stackStateMachineGraphAsset != null)
            {
                stackStateMachineController = SWStateMachineGraphFactory.CreateStack(
                    stackStateMachineGraphAsset,
                    this);
            }

            if (layeredStateMachineGraphAsset == null && stackStateMachineGraphAsset == null)
            {
                SWLog.LogWarning("[그래프 예제] State Machine Graph가 지정되지 않았습니다.");
            }

            if (behaviourTreeRunner == null || behaviourTreeRunner.TreeAsset == null)
            {
                SWLog.LogWarning(
                    "[그래프 예제] Behaviour Tree Runner에 Example Behaviour Tree를 지정하세요.");
            }
        }

        /// <summary>생성된 상태 머신을 매 프레임 갱신합니다.</summary>
        private void Update()
        {
            layeredStateMachine?.Tick(Time.deltaTime);
            stackStateMachineController?.Tick(Time.deltaTime);
        }

        /// <summary>컴포넌트가 제거될 때 실행 중인 상태 머신을 종료합니다.</summary>
        private void OnDestroy()
        {
            layeredStateMachine?.Stop();
            stackStateMachineController?.Stop();
        }
        #endregion // Unity 생명주기

        #region 예제 제어
        /// <summary>Idle과 Moving 상태 사이의 전이 조건을 반전합니다.</summary>
        [SWButton("이동 상태 전환")]
        private void ToggleMoving()
        {
            isMoving = !isMoving;
        }

        /// <summary>Gameplay와 Pause 상태 사이의 전이 조건을 반전합니다.</summary>
        [SWButton("일시정지 상태 전환")]
        private void TogglePaused()
        {
            isPaused = !isPaused;
        }
        #endregion // 예제 제어

        
    }
}
