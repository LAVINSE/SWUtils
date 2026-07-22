namespace SW.BehaviourTree
{
    /// <summary>반복적인 이름 조회 없이 Blackboard 값을 읽고 쓰는 타입 안전 참조입니다.</summary>
    public sealed class SWBehaviourBlackboardKey<T>
    {
        private readonly SWBehaviourBlackboardEntry entry;

        internal SWBehaviourBlackboardKey(string name, SWBehaviourBlackboardEntry entry)
        {
            Name = name;
            this.entry = entry;
        }

        /// <summary>Blackboard에 등록된 Key 이름입니다.</summary>
        public string Name { get; }

        /// <summary>현재 값을 가져오거나 변경합니다.</summary>
        public T Value
        {
            get => entry.TryGetValue(out T value) ? value : default;
            set => entry.TrySetValue(value);
        }

        /// <summary>현재 값을 안전하게 반환합니다.</summary>
        public bool TryGetValue(out T value) => entry.TryGetValue(out value);

        /// <summary>호환되는 값이면 현재 값을 변경합니다.</summary>
        public bool TrySetValue(T value) => entry.TrySetValue(value);
    }
}
