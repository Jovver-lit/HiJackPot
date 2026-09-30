using System.Collections.Generic;

namespace RouletteLike.Battle
{
    public enum Side
    {
        Player,
        Dealer
    }

    /// <summary>
    /// 테이블의 한 자리(플레이어 또는 딜러)의 칩·판돈·보험·룰렛.
    /// </summary>
    public sealed class Seat
    {
        private readonly List<Slot> _wheel;

        public Side Side { get; }
        public int Chips { get; internal set; }
        public int Ante { get; internal set; }
        public int Pot { get; internal set; }
        public int Insurance { get; internal set; }
        public IReadOnlyList<Slot> Wheel => _wheel;
        public bool IsBankrupt => Chips <= 0 && Pot <= 0;

        public Seat(Side side, int chips, IEnumerable<Slot> wheel)
        {
            Side = side;
            Chips = chips;
            _wheel = new List<Slot>(wheel);
        }

        internal void ReplaceSlot(int index, Slot slot)
        {
            _wheel[index] = slot;
        }
    }
}
