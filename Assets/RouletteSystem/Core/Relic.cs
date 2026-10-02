using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>유물 종류. 효과는 <see cref="PotBattle"/>·<see cref="Run"/>이 이 값을 확인해 적용한다.</summary>
    public enum RelicId
    {
        StringChip,
        HighRollerBadge,
        InsurancePolicy,
        StickyDivider,
        DopamineShot,
        Consolation,
        MarkedCard,
        DoubleLedger,
        LuckyCoin,
        FoldedCorner,
        VipCard,
        SeizureSeal
    }

    /// <summary>
    /// 유물: 이전 탈출자들이 남긴 부정행위 도구. 칸이 아니라 규칙 자체를 비튼다.
    /// 이름·키워드·설명(표시용 데이터)만 가지며, 규칙 계산은 전투·런 코어가 맡는다.
    /// </summary>
    public sealed class Relic
    {
        public RelicId Id { get; }
        public string Name { get; }

        /// <summary>발동 시점 키워드([착지]·[인접]·[CASH OUT]·[몰수]·[라운드 시작]·[HIJACK]) 또는 [개입]·[테이블]·[상금].</summary>
        public string Keyword { get; }
        public string Description { get; }

        /// <summary>아이콘 글자(그레이박스, 1~3자). 아트가 나오면 아이콘 이미지로 바뀐다.</summary>
        public string Icon { get; }

        public Relic(RelicId id, string name, string keyword, string description, string icon)
        {
            Id = id;
            Name = name;
            Keyword = keyword;
            Description = description;
            Icon = icon;
        }

        public override string ToString() => $"{Keyword} {Name}: {Description}";
    }

    /// <summary>MVP 유물 12개(데이터). 문 카드 상금으로 등장한다(ADR 0006).</summary>
    public static class RelicCatalog
    {
        /// <summary>도파민 주사기: 몰수 1번당 CASH OUT 피해 +1.</summary>
        public const int DoubleLedgerPotThreshold = 8;
        public const int DoubleLedgerBonus = 2;
        public const int StickyDividerBonus = 2;
        public const int MarkedCardChips = 3;
        /// <summary>VIP 회원증: 딴 칩 중 칩으로 남는 비율에 더하는 값.</summary>
        public const float VipKeepShareBonus = 0.2f;

        private static readonly Relic[] All =
        {
            new Relic(RelicId.StringChip, "끈 달린 칩", "[개입]", "NUDGE 전투당 +1회", "끈"),
            new Relic(RelicId.HighRollerBadge, "하이 롤러 배지", "[테이블]", "내 테이블 한도 +1", "한도"),
            new Relic(RelicId.InsurancePolicy, "보험 약관 사본", "[CASH OUT]", "내 턴을 보험 1로 시작한다", "약관"),
            new Relic(RelicId.StickyDivider, "끈적한 칸막이", "[인접]", $"착지한 칸 바로 옆에 같은 종류 칸이 있으면 판돈 +{StickyDividerBonus}", "칸막"),
            new Relic(RelicId.DopamineShot, "도파민 주사기", "[몰수]", "몰수에 걸릴 때마다 도파민 +1. 내 CASH OUT 피해 + 도파민(이번 전투)", "도파"),
            new Relic(RelicId.Consolation, "위로금 봉투", "[몰수]", "몰수에 걸리면 베팅만은 돌려받는다", "위로"),
            new Relic(RelicId.MarkedCard, "표시된 카드", "[HIJACK]", $"HIJACK할 때마다 칩 +{MarkedCardChips}", "카드"),
            new Relic(RelicId.DoubleLedger, "이중 장부", "[CASH OUT]", $"판돈 {DoubleLedgerPotThreshold} 이상으로 CASH OUT하면 피해 +{DoubleLedgerBonus}", "장부"),
            new Relic(RelicId.LuckyCoin, "뒷면만 나오는 동전", "[라운드 시작]", "코인플립에서 후공이 되면 칩 +1", "동전"),
            new Relic(RelicId.FoldedCorner, "모서리 접힌 카드", "[착지]", "배율 칸에 착지하면 곱하기 전에 판돈 +1", "모서"),
            new Relic(RelicId.VipCard, "VIP 회원증", "[정산]", "딴 칩 중 칩으로 남는 몫 30% → 50% (나머지는 현금)", "VIP"),
            new Relic(RelicId.SeizureSeal, "회수 방지 봉인", "[HIJACK]", "역탈취를 당하지 않는다", "봉인"),
        };

        public static IReadOnlyList<Relic> Relics => All;

        public static Relic Get(RelicId id) => All[(int)id];
    }
}
