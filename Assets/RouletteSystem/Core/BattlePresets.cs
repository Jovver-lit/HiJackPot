using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>
    /// 첫 스테이지 1층(토끼 딜러 튜토리얼)의 시작 데이터. 수치는 전부 임시값이다.
    /// </summary>
    public static class BattlePresets
    {
        public const int PlayerStartingChips = 20;

        /// <summary>
        /// 시작 8칸: 레이즈 3 / 배율 1 / 보험 2 / 배당 1 / 하우스 몫 1.
        /// 레이즈 두 칸을 붙여 첫 연쇄를 바로 보여주고, 배율은 그 뒤에 둔다.
        /// </summary>
        public static List<Slot> CreateStarterWheel()
        {
            return new List<Slot>
            {
                new Slot("p_raise_2_a", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("p_raise_2_b", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("p_mult_2", SlotKind.Multiplier, 2, "배율 ×2"),
                new Slot("p_insurance_3", SlotKind.Insurance, 3, "보험 +3"),
                new Slot("p_raise_3", SlotKind.Raise, 3, "레이즈 +3"),
                new Slot("p_house_cut", SlotKind.HouseCut, 0, "하우스 몫"),
                new Slot("p_dividend_2", SlotKind.Dividend, 2, "배당 +2"),
                new Slot("p_insurance_2", SlotKind.Insurance, 2, "보험 +2")
            };
        }

        /// <summary>
        /// 토끼 딜러: 칩 15, 테이블 한도 3, 앤티 1, 판돈 5 이상이면 CASH OUT(소심한 튜토리얼 딜러), 하우스 룰은 전액 보장.
        /// JACKPOT 칸은 「서비스」([라운드 시작] 칩 +1).
        /// 보험 칸이 없다(보험 0 고정). 원안의 "토끼 방어력 0"처럼 작은 판돈 갉아먹기가 통하게 한다.
        /// R3는 대본: 예고 후 앤티만으로 곧장 CASH OUT → 보험이 1 이상이면 전액 보장.
        /// </summary>
        public static DealerProfile CreateRabbitDealer()
        {
            List<Slot> wheel = new List<Slot>
            {
                new Slot("r_raise_2_a", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("r_raise_2_b", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("r_mult_2", SlotKind.Multiplier, 2, "배율 ×2"),
                new Slot("r_raise_1", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("r_raise_3", SlotKind.Raise, 3, "레이즈 +3"),
                new Slot("r_service", SlotKind.Dividend, 1, "서비스", SlotTrigger.RoundStart, isJackpot: true)
            };

            return new DealerProfile(
                name: "토끼 딜러",
                startingChips: 15,
                tableLimit: 3,
                dealerAnte: 1,
                cashOutAt: 5,
                houseRule: HouseRule.FullCoverage,
                telegraphs: true,
                counterHijacks: false,
                wheel: wheel,
                scriptedInstantCashOutRounds: new[] { 3 });
        }

        /// <summary>
        /// 바깥 링 4칸: 레이즈 +3 / 배율 ×2(안쪽 효과 한 번 더) / 보험 +2 / 바깥 하우스 몫(판돈 증발).
        /// 바깥 링도 보상과 위험을 함께 가진다(ADR 0005). 보호막은 보스 JACKPOT 「VIP 보호막」에만 있다.
        /// 보스 매니저의 테이블 규칙이자, 보스를 이긴 뒤 다음 런부터 플레이어가 갖는 바깥 링이다.
        /// </summary>
        public static List<Slot> CreateOuterRing()
        {
            return new List<Slot>
            {
                new Slot("o_raise_3", SlotKind.Raise, 3, "레이즈 +3"),
                new Slot("o_mult_2", SlotKind.Multiplier, 2, "배율 ×2"),
                new Slot("o_insurance_2", SlotKind.Insurance, 2, "보험 +2"),
                new Slot("o_house_cut", SlotKind.HouseCut, 0, "하우스 몫")
            };
        }

        /// <summary>
        /// 스테이지 1 보스 「매니저」. 테이블 규칙으로 양쪽 룰렛에 바깥 링이 붙는다.
        /// 하우스 룰 "잭팟 라인 2번"(안쪽과 바깥이 같은 종류로 멈추기). JACKPOT 「VIP 보호막」: [착지] 이번 턴 하우스 몫 1회 무효.
        /// 수치는 임시값이다.
        /// </summary>
        public static DealerProfile CreateStageBoss()
        {
            List<Slot> wheel = new List<Slot>
            {
                new Slot("m_raise_2_a", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("m_raise_2_b", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("m_mult_2", SlotKind.Multiplier, 2, "배율 ×2"),
                new Slot("m_raise_3", SlotKind.Raise, 3, "레이즈 +3"),
                new Slot("m_insurance_2", SlotKind.Insurance, 2, "보험 +2"),
                new Slot("m_vip_shield", SlotKind.CutShield, 1, "VIP 보호막", isJackpot: true),
                new Slot("m_raise_1", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("m_dividend_2", SlotKind.Dividend, 2, "배당 +2"),
                new Slot("m_raise_2_c", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("m_house_cut", SlotKind.HouseCut, 0, "하우스 몫")
            };

            return new DealerProfile(
                name: "매니저", startingChips: 48, tableLimit: 5, dealerAnte: 2, cashOutAt: 10,
                houseRule: HouseRule.JackpotLines, telegraphs: false, counterHijacks: true,
                wheel: wheel, baseInsurance: 2, tableOuterRing: CreateOuterRing());
        }

        /// <summary>
        /// 2~4층 문 뒤에 설 수 있는 일반 딜러들. 일반 딜러는 룰렛에 하우스 몫이 1칸 있다(ADR 0004).
        /// 튜토리얼 토끼만 하우스 몫이 없다.
        /// </summary>
        public static IReadOnlyList<System.Func<DealerProfile>> CreateDealerPool()
        {
            return new System.Func<DealerProfile>[] { CreateFoxDealer, CreateCatDealer, CreateCrowDealer };
        }

        /// <summary>
        /// 여우 딜러: 칩 14, 판돈 5 이상이면 CASH OUT. 보험 3 고정이라 작은 판돈이 안 통한다 → 이기려면 크게 키워야 한다.
        /// 하우스 룰은 반대로 "판돈 4 이하로 CASH OUT 2번" → 털려면 작게 가야 한다.
        /// JACKPOT 「허풍」: [CASH OUT] 상대 보험과 관계없이 피해 최소 3.
        /// </summary>
        public static DealerProfile CreateFoxDealer()
        {
            List<Slot> wheel = new List<Slot>
            {
                new Slot("f_raise_2_a", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("f_raise_2_b", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("f_mult_2", SlotKind.Multiplier, 2, "배율 ×2"),
                new Slot("f_raise_3", SlotKind.Raise, 3, "레이즈 +3"),
                new Slot("f_raise_1", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("f_bluff", SlotKind.MinimumPayout, 3, "허풍", SlotTrigger.CashOut, isJackpot: true),
                new Slot("f_raise_2_c", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("f_insurance_2", SlotKind.Insurance, 2, "보험 +2"),
                new Slot("f_house_cut", SlotKind.HouseCut, 0, "하우스 몫")
            };

            return new DealerProfile(
                name: "여우 딜러", startingChips: 14, tableLimit: 4, dealerAnte: 1, cashOutAt: 5,
                houseRule: HouseRule.SmallCashOuts, telegraphs: false, counterHijacks: true,
                wheel: wheel, baseInsurance: 3);
        }

        /// <summary>
        /// 고양이 딜러: 느긋해서 판돈 12까지 키운다 → 한 방이 크다.
        /// 하우스 룰 "배율 칸으로 판돈을 불린 뒤 CASH OUT" → 연쇄 빌드를 시험한다.
        /// JACKPOT 「더블 다운」: 배율 ×3.
        /// </summary>
        public static DealerProfile CreateCatDealer()
        {
            List<Slot> wheel = new List<Slot>
            {
                new Slot("c_raise_2_a", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("c_raise_2_b", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("c_double_down", SlotKind.Multiplier, 3, "더블 다운 ×3", isJackpot: true),
                new Slot("c_raise_3", SlotKind.Raise, 3, "레이즈 +3"),
                new Slot("c_insurance_3", SlotKind.Insurance, 3, "보험 +3"),
                new Slot("c_raise_1", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("c_house_cut", SlotKind.HouseCut, 0, "하우스 몫")
            };

            return new DealerProfile(
                name: "고양이 딜러", startingChips: 18, tableLimit: 4, dealerAnte: 1, cashOutAt: 12,
                houseRule: HouseRule.MultipliedCashOut, telegraphs: false, counterHijacks: true,
                wheel: wheel);
        }

        /// <summary>
        /// 까마귀 딜러: 수금원. 칸이 12개로 잘게 나뉘어 한 번에 작게 자주 뜯는다.
        /// 하우스 룰 "내가 선공인 라운드에 CASH OUT 피해 5 이상" → 코인플립 운을 살린다.
        /// JACKPOT 「선불」: [라운드 시작] 선공 코인플립 두 번(선공 75%). 까마귀도 가끔은 후공이라 하우스 룰이 열린다.
        /// </summary>
        public static DealerProfile CreateCrowDealer()
        {
            List<Slot> wheel = new List<Slot>
            {
                new Slot("k_raise_1_a", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("k_raise_1_b", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("k_insurance_1_a", SlotKind.Insurance, 1, "보험 +1"),
                new Slot("k_raise_2_a", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("k_mult_2", SlotKind.Multiplier, 2, "배율 ×2"),
                new Slot("k_raise_1_c", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("k_prepaid", SlotKind.Initiative, 0, "선불", SlotTrigger.RoundStart, isJackpot: true),
                new Slot("k_raise_2_b", SlotKind.Raise, 2, "레이즈 +2"),
                new Slot("k_raise_1_d", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("k_insurance_1_b", SlotKind.Insurance, 1, "보험 +1"),
                new Slot("k_raise_1_e", SlotKind.Raise, 1, "레이즈 +1"),
                new Slot("k_dividend_1", SlotKind.Dividend, 1, "배당 +1"),
                new Slot("k_house_cut", SlotKind.HouseCut, 0, "하우스 몫")
            };

            return new DealerProfile(
                name: "까마귀 딜러", startingChips: 22, tableLimit: 3, dealerAnte: 1, cashOutAt: 7,
                houseRule: HouseRule.FirstStrike, telegraphs: false, counterHijacks: true,
                wheel: wheel);
        }
    }
}
