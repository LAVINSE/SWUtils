using System;

namespace SW.Util
{
    /// <summary>
    /// SWEventBus에 등록된 이벤트 타입의 현재 상태를 표시하기 위한 읽기 전용 스냅샷입니다.
    /// </summary>
    public sealed class SWEventBusEventSnapshot
    {
        /// <summary>
        /// 이벤트 타입, 리스너 수, 발행 기록을 지정해 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="eventType">이벤트 데이터 타입입니다.</param>
        /// <param name="listenerCount">현재 등록된 리스너 수입니다.</param>
        /// <param name="publishCount">이벤트가 발행된 총 횟수입니다.</param>
        /// <param name="lastPublishTime">마지막 발행 시간입니다.</param>
        /// <param name="lastPayloadText">마지막 발행 데이터 요약입니다.</param>
        public SWEventBusEventSnapshot(Type eventType, int listenerCount, int publishCount,
            DateTime? lastPublishTime, string lastPayloadText)
        {
            EventType = eventType;
            ListenerCount = listenerCount;
            PublishCount = publishCount;
            LastPublishTime = lastPublishTime;
            LastPayloadText = lastPayloadText;
        }

        /// <summary>이벤트 데이터 타입입니다.</summary>
        public Type EventType { get; }

        /// <summary>현재 등록된 리스너 수입니다.</summary>
        public int ListenerCount { get; }

        /// <summary>이벤트가 발행된 총 횟수입니다.</summary>
        public int PublishCount { get; }

        /// <summary>마지막 발행 시간입니다.</summary>
        public DateTime? LastPublishTime { get; }

        /// <summary>마지막 발행 데이터 요약입니다.</summary>
        public string LastPayloadText { get; }
    }
}
