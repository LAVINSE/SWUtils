namespace SW.BehaviourTree
{
    /// <summary>Behaviour 노드의 현재 실행 결과입니다.</summary>
    public enum SWBehaviourStatus
    {
        Inactive,
        Running,
        Success,
        Failure,
        Aborted,
    }
}
