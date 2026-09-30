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
    }
}
