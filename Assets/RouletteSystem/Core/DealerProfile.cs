using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>
    /// 딜러마다 다른, HIJACK 기회를 주는 조건(하우스 룰).
    /// </summary>
    public enum HouseRule
    {
        /// <summary>토끼 딜러: 딜러의 CASH OUT을 보험으로 전액 보장하면 HIJACK 기회.</summary>
        FullCoverage
    }

    /// <summary>
    /// 딜러 한 명의 공개 정보와 시작 룰렛. 전투 중 바뀌는 값은 Seat가 가진다.
    /// </summary>
    public sealed class DealerProfile
    {
        public string Name { get; }
        public int StartingChips { get; }

        /// <summary>앤티의 최대치. 플레이어와 딜러 모두에게 적용된다.</summary>
        public int TableLimit { get; }

        public int DealerAnte { get; }

        /// <summary>성향: 판돈이 이 값 이상이면 SPIN을 멈추고 CASH OUT한다.</summary>
        public int CashOutAt { get; }

        public HouseRule HouseRule { get; }

        /// <summary>다음 행동을 미리 보여주는지. 튜토리얼 딜러만 true.</summary>
        public bool Telegraphs { get; }

        /// <summary>플레이어가 하우스 몫에 걸렸을 때 칸을 빼앗는지(역탈취).</summary>
        public bool CounterHijacks { get; }

        public IReadOnlyList<Slot> Wheel { get; }

        /// <summary>
        /// 튜토리얼 대본: 이 라운드에는 딜러가 SPIN 없이 앤티만으로 곧장 CASH OUT한다(예고와 함께).
        /// 토끼 R3에서 전액 보장 → HIJACK을 반드시 한 번 경험시키기 위한 장치.
        /// </summary>
        public IReadOnlyCollection<int> ScriptedInstantCashOutRounds { get; }

        public DealerProfile(
            string name,
            int startingChips,
            int tableLimit,
            int dealerAnte,
            int cashOutAt,
            HouseRule houseRule,
            bool telegraphs,
            bool counterHijacks,
            IReadOnlyList<Slot> wheel,
            IReadOnlyCollection<int> scriptedInstantCashOutRounds = null)
        {
            Name = name;
            StartingChips = startingChips;
            TableLimit = tableLimit;
            DealerAnte = dealerAnte;
            CashOutAt = cashOutAt;
            HouseRule = houseRule;
            Telegraphs = telegraphs;
            CounterHijacks = counterHijacks;
            Wheel = wheel;
            ScriptedInstantCashOutRounds = scriptedInstantCashOutRounds ?? System.Array.Empty<int>();
        }
    }
}
