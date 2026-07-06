using System.Collections.Generic;

namespace SW.Util
{
    /// <summary>
    /// 모든 항목을 한 번씩 소진할 때까지 중복 없이 뽑는 셔플백입니다.
    /// </summary>
    /// <remarks>
    /// 순수 랜덤은 같은 결과가 연속으로 나올 수 있지만, 셔플백은 가방이 빌 때까지
    /// 모든 항목이 정확히 한 번씩 등장하는 것을 보장합니다. (BGM 랜덤 재생, 아이템 분배, 테트리스식 조각 뽑기 등)
    /// 가방이 비면 자동으로 다시 채워 섞습니다.
    /// </remarks>
    /// <typeparam name="T">뽑을 항목 타입입니다.</typeparam>
    public class SWShuffleBag<T>
    {
        #region 필드
        private readonly List<T> sourceItems = new();
        private readonly List<T> currentBag = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>가방에 등록된 전체 항목 수입니다.</summary>
        public int Count => sourceItems.Count;

        /// <summary>이번 사이클에서 아직 뽑히지 않은 항목 수입니다.</summary>
        public int Remaining => currentBag.Count;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 빈 셔플백을 생성합니다.
        /// </summary>
        public SWShuffleBag()
        {
        }

        /// <summary>
        /// 항목 목록으로 셔플백을 생성합니다.
        /// </summary>
        /// <param name="items">등록할 항목들입니다.</param>
        public SWShuffleBag(IEnumerable<T> items)
        {
            if (items == null) return;

            sourceItems.AddRange(items);
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 항목을 가방에 추가합니다. 다음 리필부터 반영됩니다.
        /// </summary>
        /// <param name="item">추가할 항목입니다.</param>
        /// <param name="count">추가할 개수입니다. 여러 개 넣으면 그만큼 등장 빈도가 높아집니다.</param>
        public void Add(T item, int count = 1)
        {
            for (int index = 0; index < count; index++)
                sourceItems.Add(item);
        }

        /// <summary>
        /// 가방에서 항목 하나를 뽑습니다. 가방이 비어 있으면 자동으로 리필 후 섞습니다.
        /// </summary>
        /// <returns>뽑힌 항목. 등록된 항목이 없으면 default입니다.</returns>
        public T Next()
        {
            if (sourceItems.Count == 0)
            {
                SWLog.LogWarning("[SWShuffleBag] Next 실패: 등록된 항목이 없습니다.");
                return default;
            }

            if (currentBag.Count == 0)
                Refill();

            int lastIndex = currentBag.Count - 1;
            T item = currentBag[lastIndex];
            currentBag.RemoveAt(lastIndex);
            return item;
        }

        /// <summary>
        /// 현재 사이클을 버리고 가방을 다시 채워 섞습니다.
        /// </summary>
        public void Reset()
        {
            Refill();
        }

        /// <summary>
        /// 등록된 항목과 현재 사이클을 모두 비웁니다.
        /// </summary>
        public void Clear()
        {
            sourceItems.Clear();
            currentBag.Clear();
        }

        /// <summary>
        /// 원본 항목으로 가방을 채우고 섞습니다.
        /// </summary>
        private void Refill()
        {
            currentBag.Clear();
            currentBag.AddRange(sourceItems);
            SWRandom.Shuffle(currentBag);
        }
        #endregion // 함수
    }
}
