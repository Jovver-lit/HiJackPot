using System;
using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>슬롯머신 릴 기호. 표시 문자는 <see cref="SlotMachine.Glyph"/>.</summary>
    public enum ReelSymbol
    {
        Raise,
        Insurance,
        Dividend,
        Multiplier,
        Seven,
        Jackpot,
        HouseCut
    }

    /// <summary>한 번 당긴 결과: 세 릴, 건 현금, 돌려받는 현금(건 돈 포함, 0이면 잃음), 결과 이름.</summary>
    public sealed class SlotMachineResult
    {
        public ReelSymbol[] Reels;
        public int Bet;
        public int Payout;
        public string Outcome = "";

        public int Net => Payout - Bet;
    }

    /// <summary>
    /// 상점 미니게임 슬롯머신(ADR 0009). 현금을 걸고 세 릴을 돌려 배당을 받는다. 기대 환수율 약 93%로 카지노가 조금 유리하다
    /// — 현금을 불릴 수도 있지만, 오래 당기면 줄어든다. 릴 하나라도 「몰수」(몰수)이면 건 돈을 잃는다.
    /// 규칙과 확률만 담고 화면(릴 애니메이션)은 모른다. 같은 시드면 같은 결과.
    /// </summary>
    public static class SlotMachine
    {
        /// <summary>한 번에 걸 수 있는 현금.</summary>
        public static readonly IReadOnlyList<int> BetSizes = new[] { 1, 5, 10 };

        /// <summary>릴 한 칸의 기호 무게(합 20).</summary>
        private static readonly int[] Weights = { 4, 4, 4, 3, 2, 1, 2 };

        public const int PairPayout = 2;
        public const int BasicTriplePayout = 7;
        public const int MultiplierTriplePayout = 12;
        public const int SevenTriplePayout = 25;
        public const int JackpotTriplePayout = 80;
        public const int JackpotPairPayout = 5;

        public static string Glyph(ReelSymbol symbol)
        {
            switch (symbol)
            {
                case ReelSymbol.Raise: return "▲";
                case ReelSymbol.Insurance: return "◆";
                case ReelSymbol.Dividend: return "●";
                case ReelSymbol.Multiplier: return "×";
                case ReelSymbol.Seven: return "7";
                case ReelSymbol.Jackpot: return "JP";
                default: return "몰수";
            }
        }

        /// <summary>배당표(화면 표시용).</summary>
        public static string PayTable =>
            $"JP JP JP ×{JackpotTriplePayout}   7 7 7 ×{SevenTriplePayout}   × × × ×{MultiplierTriplePayout}\n" +
            $"▲▲▲ ◆◆◆ ●●● ×{BasicTriplePayout}   JP 두 개 ×{JackpotPairPayout}   같은 기호 두 개 ×{PairPayout}\n" +
            "「몰수」가 하나라도 나오면 건 돈을 잃음";

        public static ReelSymbol RollSymbol(Random rng)
        {
            int total = 0;
            foreach (int weight in Weights) total += weight;
            int roll = rng.Next(total);
            for (int i = 0; i < Weights.Length; i++)
            {
                roll -= Weights[i];
                if (roll < 0) return (ReelSymbol)i;
            }

            return ReelSymbol.HouseCut;
        }

        public static SlotMachineResult Spin(int bet, Random rng)
        {
            ReelSymbol[] reels = { RollSymbol(rng), RollSymbol(rng), RollSymbol(rng) };
            int multiplier = PayoutMultiplier(reels, out string outcome);
            return new SlotMachineResult { Reels = reels, Bet = bet, Payout = bet * multiplier, Outcome = outcome };
        }

        /// <summary>세 릴의 배당 배율(돌려받는 현금 = 건 돈 × 배율). 0이면 잃는다.</summary>
        public static int PayoutMultiplier(IReadOnlyList<ReelSymbol> reels, out string outcome)
        {
            ReelSymbol a = reels[0], b = reels[1], c = reels[2];
            if (a == ReelSymbol.HouseCut || b == ReelSymbol.HouseCut || c == ReelSymbol.HouseCut)
            {
                outcome = "몰수! 건 돈을 잃었다";
                return 0;
            }

            if (a == b && b == c)
            {
                switch (a)
                {
                    case ReelSymbol.Jackpot: outcome = "JACKPOT!!!"; return JackpotTriplePayout;
                    case ReelSymbol.Seven: outcome = "트리플 세븐!"; return SevenTriplePayout;
                    case ReelSymbol.Multiplier: outcome = "배율 세 개!"; return MultiplierTriplePayout;
                    default: outcome = "같은 기호 세 개!"; return BasicTriplePayout;
                }
            }

            int jackpots = (a == ReelSymbol.Jackpot ? 1 : 0) + (b == ReelSymbol.Jackpot ? 1 : 0) + (c == ReelSymbol.Jackpot ? 1 : 0);
            if (jackpots == 2)
            {
                outcome = "JP 두 개!";
                return JackpotPairPayout;
            }

            if (a == b || b == c || a == c)
            {
                outcome = "같은 기호 두 개";
                return PairPayout;
            }

            outcome = "꽝";
            return 0;
        }

        /// <summary>기대 환수율(배당 기대값 ÷ 건 돈). 테스트·문서용.</summary>
        public static double ExpectedReturn()
        {
            int total = 0;
            foreach (int weight in Weights) total += weight;
            double expected = 0;
            ReelSymbol[] reels = new ReelSymbol[3];
            for (int i = 0; i < Weights.Length; i++)
            for (int j = 0; j < Weights.Length; j++)
            for (int k = 0; k < Weights.Length; k++)
            {
                reels[0] = (ReelSymbol)i;
                reels[1] = (ReelSymbol)j;
                reels[2] = (ReelSymbol)k;
                double p = (double)Weights[i] * Weights[j] * Weights[k] / ((double)total * total * total);
                expected += p * PayoutMultiplier(reels, out _);
            }

            return expected;
        }
    }
}
