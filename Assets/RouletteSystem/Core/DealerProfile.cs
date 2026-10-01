using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>
    /// 딜러마다 다른, HIJACK 기회를 주는 조건(하우스 룰).
    /// </summary>
    public enum HouseRule
    {
        /// <summary>토끼 딜러: 딜러의 CASH OUT을 보험으로 전액 보장하면 HIJACK 기회.</summary>
        FullCoverage,

        /// <summary>여우 딜러: 판돈 4 이하로 CASH OUT을 두 번 하면 HIJACK 기회(보험 3 앞에서 작게 가야 한다).</summary>
        SmallCashOuts,

        /// <summary>고양이 딜러: 그 턴에 배율 칸으로 판돈을 불린 뒤 CASH OUT하면 HIJACK 기회.</summary>
        MultipliedCashOut,

        /// <summary>까마귀 딜러: 내가 선공인 라운드에 CASH OUT 피해 5 이상이면 HIJACK 기회.</summary>
        FirstStrike,

        /// <summary>보스 매니저: 안쪽과 바깥 링이 같은 종류로 멈추는 잭팟 라인을 2번 만들면 HIJACK 기회.</summary>
        JackpotLines
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

        /// <summary>딜러가 앤티를 걸 때마다 보험이 이 값으로 시작한다(여우 3, 토끼 0).</summary>
        public int BaseInsurance { get; }

        /// <summary>
        /// 테이블 규칙: 이 딜러와 싸우는 동안 양쪽 룰렛에 붙는 바깥 링(보스 매니저). 비어 있으면 없음.
        /// 플레이어가 이미 자기 바깥 링을 가졌다면 그것을 쓴다.
        /// </summary>
        public IReadOnlyList<Slot> TableOuterRing { get; }

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
            IReadOnlyCollection<int> scriptedInstantCashOutRounds = null,
            int baseInsurance = 0,
            IReadOnlyList<Slot> tableOuterRing = null)
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
            BaseInsurance = baseInsurance;
            TableOuterRing = tableOuterRing ?? System.Array.Empty<Slot>();
        }

        /// <summary>시작 칩·앤티를 바꾼 사본(층이 오를수록 딜러가 단단하고 아파진다).</summary>
        public DealerProfile WithFloorScaling(int startingChips, int anteBonus)
        {
            return WithStakes(Name, startingChips, anteBonus, 0, 0);
        }

        /// <summary>이름·시작 칩·앤티·기본 보험·성향(CASH OUT 판돈)을 바꾼 사본. 2회차 보스 등.</summary>
        public DealerProfile WithStakes(string name, int startingChips, int anteBonus, int insuranceBonus, int cashOutAtBonus)
        {
            DealerProfile copy = new DealerProfile(name, startingChips, TableLimit, DealerAnte + anteBonus, CashOutAt + cashOutAtBonus, HouseRule, Telegraphs, CounterHijacks,
                Wheel, new List<int>(ScriptedInstantCashOutRounds), BaseInsurance + insuranceBonus, TableOuterRing);
            copy.Trick = Trick;
            return copy;
        }

        /// <summary>딜러 특수 룰(실험). 기본 None.</summary>
        public DealerTrick Trick { get; private set; }

        /// <summary>특수 룰을 붙인 사본.</summary>
        public DealerProfile WithTrick(DealerTrick trick)
        {
            DealerProfile copy = WithStakes(Name, StartingChips, 0, 0, 0);
            copy.Trick = trick;
            return copy;
        }

        /// <summary>테이블 규칙으로 양쪽에 바깥 링을 붙인 사본(2회차 딜러 실험).</summary>
        public DealerProfile WithTableRing(IReadOnlyList<Slot> ring)
        {
            DealerProfile copy = new DealerProfile(Name, StartingChips, TableLimit, DealerAnte, CashOutAt, HouseRule, Telegraphs, CounterHijacks,
                Wheel, new List<int>(ScriptedInstantCashOutRounds), BaseInsurance, ring);
            copy.Trick = Trick;
            return copy;
        }
    }
}
