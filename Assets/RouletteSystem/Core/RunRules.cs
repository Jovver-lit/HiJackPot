namespace RouletteLike.Battle
{
    /// <summary>유물을 어디서 얻는가(실험 비교용).</summary>
    public enum RelicSource
    {
        /// <summary>딜러층 문마다 유물이 걸려 이기면 받는다(ADR 0006, 현재 기본).</summary>
        DoorEveryBattle,

        /// <summary>상점층에서만 산다.</summary>
        ShopOnly,

        /// <summary>문마다 유물이 걸리지만, 그 딜러의 도전(하우스 룰 StealHouseRules번 또는 완전 강탈)을 깨고 이겨야 받는다. 상점에서도 산다.</summary>
        DoorChallenge
    }

    /// <summary>
    /// 런 구성 실험 옵션(2026-10-01 딜러 특수 룰 검증). 유물 획득처, 2회차 딜러 바깥 링, 딜러 특수 룰 형식과 적용 회차,
    /// 특수 룰을 빼앗는 조건. 기본값은 지금의 게임과 같다. 시뮬레이터가 여러 조합을 비교하는 데 쓴다.
    /// </summary>
    public sealed class RunRules
    {
        public static readonly RunRules Default = new RunRules();

        public RelicSource Relics { get; set; } = RelicSource.DoorEveryBattle;

        /// <summary>2회차 딜러 테이블에 바깥 링(양쪽 이중 룰렛)을 붙인다.</summary>
        public bool SecondStageOuterRing { get; set; }

        /// <summary>딜러 특수 룰 형식. None이면 특수 룰이 없다.</summary>
        public TrickFormat Tricks { get; set; } = TrickFormat.None;

        /// <summary>특수 룰이 붙기 시작하는 회차(0 = 1회차부터, 1 = 2회차부터).</summary>
        public int TricksFromStage { get; set; } = 1;

        /// <summary>특수 룰을 빼앗을 수 있는지. 조건: 완전 강탈하거나, 한 전투에서 하우스 룰을 이만큼 달성하고 이기기.</summary>
        public bool CanStealTricks { get; set; }

        public int StealHouseRules { get; set; } = 2;
    }
}
