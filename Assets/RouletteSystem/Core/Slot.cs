namespace RouletteLike.Battle
{
    /// <summary>
    /// 칸이 착지했을 때 무엇을 바꾸는지. 용어는 CONTEXT.md의 기본 칸을 따른다.
    /// </summary>
    public enum SlotKind
    {
        /// <summary>판돈 +N</summary>
        Raise,

        /// <summary>판돈 ×N</summary>
        Multiplier,

        /// <summary>이번 라운드 보험 +N</summary>
        Insurance,

        /// <summary>판돈을 거치지 않고 칩 +N</summary>
        Dividend,

        /// <summary>판돈이 전부 증발한다. HIJACK으로 덮어쓸 수 없다.</summary>
        HouseCut,

        /// <summary>HIJACK당해 비어 버린 딜러 칸. 걸려도 아무 일도 없다.</summary>
        Sealed,

        /// <summary>[CASH OUT] 상대 보험과 관계없이 피해가 최소 N이 된다(여우 JACKPOT 「허풍」).</summary>
        MinimumPayout,

        /// <summary>
        /// 보호막: 이번 턴에 몰수 1회를 무효로 만든다. 바깥 링에 있으면 그 SPIN의 착지보다 먼저 발동한다.
        /// </summary>
        CutShield,

        /// <summary>[라운드 시작] 선공 코인플립을 두 번 던져 한 번이라도 이기면 선공(75%, 까마귀 JACKPOT 「선불」). 양쪽 다 있으면 보통 코인플립.</summary>
        Initiative
    }

    /// <summary>
    /// 칸이 언제 발동하는지(키워드). MVP 코어는 [착지]·[라운드 시작]·[CASH OUT]을 처리한다.
    /// </summary>
    public enum SlotTrigger
    {
        Land,
        RoundStart,
        CashOut
    }

    /// <summary>
    /// 룰렛의 한 칸. 전투 중 값이 바뀌지 않는 불변 데이터이며, 바뀔 때는 새 칸으로 교체한다.
    /// </summary>
    public sealed class Slot
    {
        public string Id { get; }
        public SlotKind Kind { get; }
        public int Value { get; }
        public SlotTrigger Trigger { get; }
        public string Label { get; }
        public bool IsJackpot { get; }
        public bool IsStolen { get; }

        public Slot(
            string id,
            SlotKind kind,
            int value,
            string label,
            SlotTrigger trigger = SlotTrigger.Land,
            bool isJackpot = false,
            bool isStolen = false)
        {
            Id = id;
            Kind = kind;
            Value = value;
            Label = label;
            Trigger = trigger;
            IsJackpot = isJackpot;
            IsStolen = isStolen;
        }

        /// <summary>[라운드 시작] 칸은 착지해도 발동하지 않고, (테이블 규칙 「연쇄」에서) 묶음에도 끼지 않는다.</summary>
        public bool FiresOnLand => Trigger == SlotTrigger.Land && Kind != SlotKind.Sealed;

        public Slot AsStolen(string newId)
        {
            return new Slot(newId, Kind, Value, Label, Trigger, IsJackpot, true);
        }

        public static Slot Sealed(string id)
        {
            return new Slot(id, SlotKind.Sealed, 0, "봉인");
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
