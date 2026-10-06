using System;

namespace RouletteLike.Battle
{
    /// <summary>
    /// 딴 칩 정산 규칙(ADR 0008). 전투에서 입장 칩보다 많이 딴 칩 중 KeepShare만 칩(목숨)으로 남고 나머지는 현금으로 환전된다.
    /// 현금은 환전 창구에서 CashPerChip당 칩 1개로 되살 수 있다. 값은 시뮬레이션(`RunBalanceSimulator`)으로 정한 임시값.
    /// </summary>
    public sealed class ChipExchangeRules
    {
        /// <summary>기본값: 딴 칩의 30%는 칩, 70%는 현금. 상금 없음(딴 칩 30%가 상금 역할). 환전 창구 현금 3 = 칩 1, 한 층에 최대 5칩.</summary>
        public static readonly ChipExchangeRules Default = new ChipExchangeRules(0.3f, 3, 0f, 5);

        /// <summary>파산 승리 상금 = 딜러 시작 칩 × 이 비율. 기본 0(딴 칩 정산이 대신한다). 시뮬레이션 비교용으로 남긴다.</summary>
        public float WinningsRatio { get; }

        /// <summary>환전 창구에서 한 번(문 선택 화면 한 번)에 살 수 있는 칩 상한. 0이면 무제한.</summary>
        public int MaxChipsPerVisit { get; }

        /// <summary>입장 칩을 넘게 딴 칩 중 칩으로 남기는 비율(0~1, 내림). 유물 「VIP 회원증」이 +20%p.</summary>
        public float KeepShare { get; }

        /// <summary>환전 창구에서 칩 1개를 사는 데 드는 현금.</summary>
        public int CashPerChip { get; }

        public ChipExchangeRules(float keepShare, int cashPerChip, float winningsRatio = 0.3f, int maxChipsPerVisit = 0)
        {
            KeepShare = Math.Max(0f, Math.Min(1f, keepShare));
            CashPerChip = Math.Max(1, cashPerChip);
            WinningsRatio = Math.Max(0f, winningsRatio);
            MaxChipsPerVisit = Math.Max(0, maxChipsPerVisit);
        }
    }

    /// <summary>한 전투가 끝났을 때의 칩 정산 결과(화면 표시와 테스트용).</summary>
    public readonly struct ChipSettlement
    {
        public readonly int EntryChips;
        /// <summary>입장 칩까지의 몫(잃었다면 남은 칩).</summary>
        public readonly int BaseChips;
        /// <summary>입장 칩을 넘게 딴 칩 중 칩으로 남는 몫.</summary>
        public readonly int KeptExcess;
        /// <summary>입장 칩을 넘게 딴 칩 중 현금으로 환전된 몫.</summary>
        public readonly int Cash;
        /// <summary>파산 승리 상금(칩).</summary>
        public readonly int Winnings;

        public ChipSettlement(int entryChips, int baseChips, int keptExcess, int cash, int winnings)
        {
            EntryChips = entryChips;
            BaseChips = baseChips;
            KeptExcess = keptExcess;
            Cash = cash;
            Winnings = winnings;
        }

        public int ChipsAfter => BaseChips + KeptExcess + Winnings;
    }
}
