using System;
using System.Collections.Generic;

namespace SW.StateMachine
{
    /// <summary>상태 전환 콜백에서 요청한 작업을 현재 전환 뒤에 실행합니다.</summary>
    internal sealed class SWStateOperationQueue
    {
        private readonly Queue<Action> pendingOperations = new();
        internal bool IsExecuting { get; private set; }

        /// <summary>요청 순서를 유지하며 실행합니다. 끝없이 전환하는 구성은 오류로 중단합니다.</summary>
        internal void Execute(Action operation)
        {
            pendingOperations.Enqueue(operation);
            if (IsExecuting) return;
            IsExecuting = true;
            try
            {
                int executedCount = 0;
                while (pendingOperations.Count > 0)
                {
                    if (++executedCount > 1024)
                        throw new InvalidOperationException("한 번의 호출에서 상태 전환이 1024회를 넘었습니다. 순환 요청을 확인해주세요.");
                    pendingOperations.Dequeue().Invoke();
                }
            }
            finally
            {
                pendingOperations.Clear();
                IsExecuting = false;
            }
        }

        /// <summary>즉시 실행한 결과를 반환합니다. 전환 중이면 요청 접수 결과를 반환합니다.</summary>
        internal bool Execute(Func<bool> operation)
        {
            bool result = true;
            Execute(() => { result = operation(); });
            return result;
        }
    }
}
