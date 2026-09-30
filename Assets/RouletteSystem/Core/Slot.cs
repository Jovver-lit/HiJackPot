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
        Sealed
    }

    /// <summary>
    /// 칸이 언제 발동하는지(키워드). MVP 코어는 [착지]와 [라운드 시작]만 처리한다.
    /// </summary>
    public enum SlotTrigger
    {
        Land,
        RoundStart
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

        /// <summary>[라운드 시작] 칸은 착지해도 발동하지 않고, 연쇄 묶음에도 끼지 않는다.</summary>
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
