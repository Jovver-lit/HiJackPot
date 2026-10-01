using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>
    /// 딜러 특수 룰(실험, 2026-10-01): 딜러마다 하나씩 가진 고유 규칙. 그 딜러의 좌석에만 적용되고,
    /// 조건을 채워 이기면 플레이어가 빼앗아 자기 좌석에 적용할 수 있다. 형식 두 가지를 비교한다.
    /// - 보너스형(Bonus): 특정 칸에 착지하면 덤이 붙는다. 읽기 쉽고 손해가 없다.
    /// - 치환형(Substitution): 특정 칸의 효과를 다른 효과로 바꾼다. 손해와 이득이 함께 있어 운영 방식이 달라진다.
    /// </summary>
    public enum DealerTrick
    {
        None,

        /// <summary>여우·보너스 「보험 사기」: 보험 칸에 착지하면 보험만큼 칩도 받는다.</summary>
        FoxInsuranceRebate,

        /// <summary>여우·치환 「잽」: 레이즈 칸이 판돈을 올리는 대신 그 값만큼 상대 칩을 즉시 깎는다(보험 무시).</summary>
        FoxJab,

        /// <summary>고양이·보너스 「캣닢」: 배율 칸에 착지하면 곱하기 전에 판돈 +2.</summary>
        CatCatnip,

        /// <summary>고양이·치환 「올인」: 보험을 쌓지 못하는 대신 배율 칸이 ×(값+1).</summary>
        CatAllIn,

        /// <summary>까마귀·보너스 「반짝이 수집」: 배당 칸에 착지하면 그만큼 상대 칩을 훔친다.</summary>
        CrowShiny,

        /// <summary>까마귀·치환 「선불 정산」: 선공인 라운드의 CASH OUT 피해 +3, 후공이면 −2.</summary>
        CrowPrepay
    }

    /// <summary>특수 룰 형식(실험 비교용).</summary>
    public enum TrickFormat
    {
        None,
        Bonus,
        Substitution
    }

    public static class DealerTricks
    {
        public const int CatnipBonus = 2;
        public const int PrepayBonus = 3;
        public const int PrepayPenalty = 2;

        /// <summary>딜러 이름과 형식으로 특수 룰을 고른다. 보스·토끼는 없다.</summary>
        public static DealerTrick For(string dealerName, TrickFormat format)
        {
            if (format == TrickFormat.None) return DealerTrick.None;
            bool bonus = format == TrickFormat.Bonus;
            if (dealerName.Contains("여우")) return bonus ? DealerTrick.FoxInsuranceRebate : DealerTrick.FoxJab;
            if (dealerName.Contains("고양이")) return bonus ? DealerTrick.CatCatnip : DealerTrick.CatAllIn;
            if (dealerName.Contains("까마귀")) return bonus ? DealerTrick.CrowShiny : DealerTrick.CrowPrepay;
            return DealerTrick.None;
        }

        public static string Describe(DealerTrick trick)
        {
            switch (trick)
            {
                case DealerTrick.FoxInsuranceRebate: return "「보험 사기」 보험 칸에 착지하면 보험만큼 칩도 받는다";
                case DealerTrick.FoxJab: return "「잽」 레이즈 칸이 판돈 대신 그 값만큼 상대 칩을 즉시 깎는다";
                case DealerTrick.CatCatnip: return $"「캣닢」 배율 칸 착지 시 곱하기 전 판돈 +{CatnipBonus}";
                case DealerTrick.CatAllIn: return "「올인」 보험을 못 쌓는 대신 배율 칸 ×(값+1)";
                case DealerTrick.CrowShiny: return "「반짝이 수집」 배당 칸 착지 시 그만큼 상대 칩을 훔친다";
                case DealerTrick.CrowPrepay: return $"「선불 정산」 선공 라운드 CASH OUT 피해 +{PrepayBonus}, 후공이면 −{PrepayPenalty}";
                default: return "";
            }
        }
    }
}
