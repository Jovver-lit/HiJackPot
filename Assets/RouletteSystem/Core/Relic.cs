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
        SeizureSeal,
        SkimmingHook,
        ForgedReceipt,
        RefundTicket,
        MagnetStrip
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

        /// <summary>칸에 붙이는 유물이 주는 이득의 단위(칩·피해·판돈). 없으면 칸에 붙지 않는 옛 유물이다.</summary>
        public string GainUnit { get; }

        /// <summary>내 룰렛의 칸에 붙여 그 칸에 착지할 때 발동하는 유물인지(ADR 0017).</summary>
        public bool IsAttachment => GainUnit != null;

        public Relic(RelicId id, string name, string keyword, string description, string icon, string gainUnit = null)
        {
            GainUnit = gainUnit;
            Id = id;
            Name = name;
            Keyword = keyword;
            Description = description;
            Icon = icon;
        }

        public override string ToString() => $"{Keyword} {Name}: {Description}";
    }

    /// <summary>
    /// 유물 데이터. 지금 런에 등장하는 것은 칸에 붙이는 유물 4개다(ADR 0017).
    /// 앞의 12개는 칸에 붙지 않는 옛 유물로, 시험이 끝날 때까지 규칙 코드만 남고 등장하지 않는다(Legacy).
    /// </summary>
    public static class RelicCatalog
    {
        /// <summary>도파민 주사기: 몰수 1번당 CASH OUT 피해 +1.</summary>
        public const int DoubleLedgerPotThreshold = 8;
        public const int DoubleLedgerBonus = 2;
        public const int StickyDividerBonus = 2;
        public const int MarkedCardChips = 3;
        /// <summary>VIP 회원증: 딴 칩 중 칩으로 남는 비율에 더하는 값.</summary>
        public const float VipKeepShareBonus = 0.2f;

        public const int SkimmingHookChips = 1;
        public const int ForgedReceiptDamage = 2;
        public const int MagnetStripPot = 3;

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
            new Relic(RelicId.SkimmingHook, "소매치기 갈고리", "[착지]", $"이 칸에 착지하면 칩 +{SkimmingHookChips}", "갈고", "칩"),
            new Relic(RelicId.ForgedReceipt, "위조 영수증", "[CASH OUT]", $"이 칸에 착지하면 이번 턴 CASH OUT 피해 +{ForgedReceiptDamage}", "영수", "피해"),
            new Relic(RelicId.RefundTicket, "환불 쪽지", "[몰수]", "이 칸에 착지하면 이번 턴 몰수에 걸려도 베팅을 돌려받는다", "환불", "칩"),
            new Relic(RelicId.MagnetStrip, "자석 띠", "[인접]", $"이 칸에 착지했을 때 옆 칸이 같은 종류면 판돈 +{MagnetStripPot}", "자석", "판돈"),
        };

        public static IReadOnlyList<Relic> Relics => All;

        public static Relic Get(RelicId id) => All[(int)id];
    }
}
