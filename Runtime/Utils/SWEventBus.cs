using System;
using System.Collections.Generic;

namespace SWUtils
{
    /// <summary>
    /// 시스템 간 의존성을 줄이기 위한 타입 기반 전역 이벤트 버스.
    /// </summary>
    public static class SWEventBus
    {
        #region 필드
        private static readonly Dictionary<Type, Delegate> eventTable = new();
        private static readonly Dictionary<Type, PublishRecord> publishRecordTable = new();
        #endregion // 필드

        #region 데이터
        /// <summary>
        /// 이벤트 타입별 마지막 발행 상태를 저장하는 내부 기록입니다.
        /// </summary>
        private sealed class PublishRecord
        {
            /// <summary>이벤트가 발행된 총 횟수입니다.</summary>
            public int publishCount;
            /// <summary>마지막 발행 시간입니다.</summary>
            public DateTime lastPublishTime;
            /// <summary>마지막 발행 데이터 요약입니다.</summary>
            public string lastPayloadText;
        }
        #endregion // 데이터

        #region 함수
        /// <summary>
        /// 지정한 이벤트 타입에 리스너를 등록한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <param name="listener">등록할 리스너.</param>
        public static void Subscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                SWUtilsLog.LogWarning($"[SWEventBus] Subscribe failed. Listener is null. Event: {typeof(TEvent).Name}");
                return;
            }

            Type eventType = typeof(TEvent);
            if (eventTable.TryGetValue(eventType, out Delegate existing))
                eventTable[eventType] = Delegate.Combine(existing, listener);
            else
                eventTable[eventType] = listener;

            SWUtilsLog.Log($"[SWEventBus] Subscribe: {eventType.Name}");
        }

        /// <summary>
        /// 지정한 이벤트 타입에서 리스너를 제거한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <param name="listener">제거할 리스너.</param>
        public static void Unsubscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                SWUtilsLog.LogWarning($"[SWEventBus] Unsubscribe failed. Listener is null. Event: {typeof(TEvent).Name}");
                return;
            }

            Type eventType = typeof(TEvent);
            if (!eventTable.TryGetValue(eventType, out Delegate existing))
                return;

            Delegate updated = Delegate.Remove(existing, listener);
            if (updated == null)
                eventTable.Remove(eventType);
            else
                eventTable[eventType] = updated;

            SWUtilsLog.Log($"[SWEventBus] Unsubscribe: {eventType.Name}");
        }

        /// <summary>
        /// 지정한 이벤트 데이터를 등록된 모든 리스너에게 전달한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <param name="eventData">전달할 이벤트 데이터.</param>
        public static void Publish<TEvent>(TEvent eventData)
        {
            Type eventType = typeof(TEvent);
            if (!eventTable.TryGetValue(eventType, out Delegate existing))
            {
                RecordPublish(eventType, eventData);
                SWUtilsLog.Log($"[SWEventBus] Publish skipped. No listeners: {eventType.Name}");
                return;
            }

            Action<TEvent> callback = existing as Action<TEvent>;
            if (callback == null)
            {
                SWUtilsLog.LogError($"[SWEventBus] Publish failed. Invalid delegate type: {eventType.Name}");
                return;
            }

            foreach (Delegate listenerDelegate in callback.GetInvocationList())
            {
                try
                {
                    Action<TEvent> listener = (Action<TEvent>)listenerDelegate;
                    listener.Invoke(eventData);
                }
                catch (Exception exception)
                {
                    SWUtilsLog.LogError($"[SWEventBus] Listener exception. Event: {eventType.Name}, Error: {exception.Message}");
                }
            }

            RecordPublish(eventType, eventData);
            SWUtilsLog.Log($"[SWEventBus] Publish: {eventType.Name}");
        }

        /// <summary>
        /// 지정한 이벤트 타입에 등록된 모든 리스너를 제거한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        public static void Clear<TEvent>()
        {
            eventTable.Remove(typeof(TEvent));
            SWUtilsLog.Log($"[SWEventBus] Clear: {typeof(TEvent).Name}");
        }

        /// <summary>
        /// 모든 이벤트 리스너를 제거한다.
        /// </summary>
        public static void ClearAll()
        {
            eventTable.Clear();
            publishRecordTable.Clear();
            SWUtilsLog.Log("[SWEventBus] Clear all.");
        }

        /// <summary>
        /// 지정한 이벤트 타입에 리스너가 하나 이상 등록되어 있는지 확인한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <returns>리스너가 있으면 true.</returns>
        public static bool HasListener<TEvent>()
        {
            return eventTable.ContainsKey(typeof(TEvent));
        }

        /// <summary>
        /// 현재 이벤트 버스 상태를 디버깅 창에서 표시할 수 있는 스냅샷으로 반환합니다.
        /// </summary>
        /// <returns>이벤트 타입별 현재 리스너와 발행 기록 목록입니다.</returns>
        public static IReadOnlyList<SWEventBusEventSnapshot> GetEventSnapshots()
        {
            List<SWEventBusEventSnapshot> snapshots = new();
            HashSet<Type> eventTypes = new();

            foreach (Type eventType in eventTable.Keys)
                eventTypes.Add(eventType);

            foreach (Type eventType in publishRecordTable.Keys)
                eventTypes.Add(eventType);

            foreach (Type eventType in eventTypes)
            {
                int listenerCount = 0;
                if (eventTable.TryGetValue(eventType, out Delegate existing) && existing != null)
                    listenerCount = existing.GetInvocationList().Length;

                int publishCount = 0;
                DateTime? lastPublishTime = null;
                string lastPayloadText = string.Empty;

                if (publishRecordTable.TryGetValue(eventType, out PublishRecord record))
                {
                    publishCount = record.publishCount;
                    lastPublishTime = record.lastPublishTime;
                    lastPayloadText = record.lastPayloadText;
                }

                snapshots.Add(new SWEventBusEventSnapshot(eventType, listenerCount, publishCount,
                    lastPublishTime, lastPayloadText));
            }

            snapshots.Sort((left, right) => string.Compare(left.EventType.Name, right.EventType.Name,
                StringComparison.Ordinal));
            return snapshots;
        }

        /// <summary>
        /// 이벤트 발행 기록만 제거합니다. 현재 등록된 리스너는 유지합니다.
        /// </summary>
        public static void ClearPublishRecords()
        {
            publishRecordTable.Clear();
            SWUtilsLog.Log("[SWEventBus] Clear publish records.");
        }

        /// <summary>
        /// 이벤트 발행 횟수와 마지막 발행 데이터를 기록합니다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입입니다.</typeparam>
        /// <param name="eventType">이벤트 데이터 타입입니다.</param>
        /// <param name="eventData">발행된 이벤트 데이터입니다.</param>
        private static void RecordPublish<TEvent>(Type eventType, TEvent eventData)
        {
            if (!publishRecordTable.TryGetValue(eventType, out PublishRecord record))
            {
                record = new PublishRecord();
                publishRecordTable[eventType] = record;
            }

            record.publishCount++;
            record.lastPublishTime = DateTime.Now;
            record.lastPayloadText = eventData != null ? eventData.ToString() : "(null)";
        }
        #endregion // 함수
    }
}
