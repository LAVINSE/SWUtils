namespace SW.Pooling
{
    /// <summary>
    /// 풀 그룹에서 프리팹을 선택하는 방식입니다.
    /// </summary>
    public enum SWPoolGroupSelectionMode
    {
        /// <summary>그룹에 등록된 프리팹 중 하나를 무작위로 선택합니다.</summary>
        Random,
        /// <summary>그룹에 등록된 프리팹을 순서대로 선택합니다.</summary>
        Sequence
    }
}
