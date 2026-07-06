using System.Collections.Generic;
using UnityEngine;

namespace SW.Util
{
    /// <summary>
    /// 시드 고정, 확률 판정, 가중치 선택, 셔플을 제공하는 랜덤 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// UnityEngine.Random과 달리 System.Random 기반이라 시드를 고정하면
    /// 엔진 내부 사용에 영향받지 않고 동일한 결과 순서를 재현할 수 있습니다. (리플레이, 테스트, 데일리 시드 등)
    /// Domain Reload 비활성화 환경에서도 플레이 진입 시 상태가 초기화됩니다.
    /// </remarks>
    public static class SWRandom
    {
        #region 필드
        private static System.Random random = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>0 이상 1 미만의 랜덤 float입니다.</summary>
        public static float Value => (float)random.NextDouble();

        /// <summary>50% 확률의 랜덤 bool입니다.</summary>
        public static bool Bool => random.NextDouble() < 0.5d;

        /// <summary>50% 확률로 1 또는 -1을 반환합니다.</summary>
        public static int Sign => random.NextDouble() < 0.5d ? 1 : -1;

        /// <summary>단위 원 내부의 랜덤 지점입니다.</summary>
        public static Vector2 InsideUnitCircle
        {
            get
            {
                float angle = Value * Mathf.PI * 2f;
                float radius = Mathf.Sqrt(Value);
                return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
        }

        /// <summary>단위 원 둘레의 랜덤 지점입니다.</summary>
        public static Vector2 OnUnitCircle
        {
            get
            {
                float angle = Value * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 플레이 진입 시 정적 상태를 초기화합니다. Domain Reload가 꺼져 있어도 항상 호출됩니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            random = new System.Random();
        }

        /// <summary>
        /// 시드를 고정합니다. 이후 모든 호출이 동일한 순서로 재현됩니다.
        /// </summary>
        /// <param name="seed">고정할 시드 값입니다.</param>
        public static void SetSeed(int seed)
        {
            random = new System.Random(seed);
        }
        #endregion // 초기화

        #region 범위
        /// <summary>
        /// 최소값 이상 최대값 미만의 랜덤 int를 반환합니다.
        /// </summary>
        /// <param name="minInclusive">최소값(포함)입니다.</param>
        /// <param name="maxExclusive">최대값(제외)입니다.</param>
        /// <returns>랜덤 int입니다.</returns>
        public static int Range(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }

        /// <summary>
        /// 최소값 이상 최대값 이하의 랜덤 float를 반환합니다.
        /// </summary>
        /// <param name="minInclusive">최소값(포함)입니다.</param>
        /// <param name="maxInclusive">최대값(포함)입니다.</param>
        /// <returns>랜덤 float입니다.</returns>
        public static float Range(float minInclusive, float maxInclusive)
        {
            return minInclusive + (maxInclusive - minInclusive) * Value;
        }
        #endregion // 범위

        #region 확률
        /// <summary>
        /// 지정한 확률로 true를 반환합니다.
        /// </summary>
        /// <param name="probability">0~1 사이의 성공 확률입니다. (0.25 = 25%)</param>
        /// <returns>판정 결과입니다.</returns>
        public static bool Chance(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return random.NextDouble() < probability;
        }
        #endregion // 확률

        #region 선택
        /// <summary>
        /// 목록에서 임의의 요소 하나를 반환합니다.
        /// </summary>
        /// <typeparam name="T">요소 타입입니다.</typeparam>
        /// <param name="list">선택 대상 목록입니다.</param>
        /// <returns>선택된 요소. 목록이 비어 있으면 default입니다.</returns>
        public static T Pick<T>(IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                SWLog.LogWarning("[SWRandom] Pick 실패: 목록이 비어 있습니다.");
                return default;
            }

            return list[random.Next(0, list.Count)];
        }

        /// <summary>
        /// 가중치 목록에서 선택된 인덱스를 반환합니다. 가중치가 클수록 선택 확률이 높습니다.
        /// </summary>
        /// <param name="weights">각 항목의 가중치입니다. 0 이하 가중치는 선택되지 않습니다.</param>
        /// <returns>선택된 인덱스. 유효한 가중치가 없으면 -1입니다.</returns>
        public static int PickIndexWeighted(IReadOnlyList<float> weights)
        {
            if (weights == null || weights.Count == 0) return -1;

            float totalWeight = 0f;
            for (int index = 0; index < weights.Count; index++)
            {
                if (weights[index] > 0f)
                    totalWeight += weights[index];
            }

            if (totalWeight <= 0f) return -1;

            float pickValue = Value * totalWeight;
            float accumulated = 0f;

            for (int index = 0; index < weights.Count; index++)
            {
                if (weights[index] <= 0f) continue;

                accumulated += weights[index];
                if (pickValue < accumulated)
                    return index;
            }

            // 부동소수점 오차 대비: 마지막 유효 인덱스 반환
            for (int index = weights.Count - 1; index >= 0; index--)
            {
                if (weights[index] > 0f)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// 가중치를 적용해 목록에서 요소 하나를 선택합니다.
        /// </summary>
        /// <typeparam name="T">요소 타입입니다.</typeparam>
        /// <param name="list">선택 대상 목록입니다.</param>
        /// <param name="weights">각 요소의 가중치입니다. 목록과 개수가 같아야 합니다.</param>
        /// <returns>선택된 요소. 실패 시 default입니다.</returns>
        public static T PickWeighted<T>(IReadOnlyList<T> list, IReadOnlyList<float> weights)
        {
            if (list == null || weights == null || list.Count != weights.Count)
            {
                SWLog.LogWarning("[SWRandom] PickWeighted 실패: 목록과 가중치 개수가 일치하지 않습니다.");
                return default;
            }

            int pickedIndex = PickIndexWeighted(weights);
            return pickedIndex >= 0 ? list[pickedIndex] : default;
        }
        #endregion // 선택

        #region 셔플
        /// <summary>
        /// 목록을 제자리에서 무작위로 섞습니다. (Fisher-Yates)
        /// </summary>
        /// <typeparam name="T">요소 타입입니다.</typeparam>
        /// <param name="list">섞을 목록입니다.</param>
        public static void Shuffle<T>(IList<T> list)
        {
            if (list == null) return;

            for (int index = list.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(0, index + 1);
                (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
            }
        }
        #endregion // 셔플
    }
}
