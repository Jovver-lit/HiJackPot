using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>테이블의 어느 쪽인지.</summary>
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
        private readonly List<Slot> _outerRing;

        public Side Side { get; }
        public int Chips { get; internal set; }
        public int Ante { get; internal set; }
        public int Pot { get; internal set; }
        public int Insurance { get; internal set; }
        public IReadOnlyList<Slot> Wheel => _wheel;

        /// <summary>바깥 링(이중 룰렛의 바깥쪽). 없으면 빈 목록.</summary>
        public IReadOnlyList<Slot> OuterRing => _outerRing;

        public bool HasOuterRing => _outerRing.Count > 0;

        /// <summary>이번 턴에 남은 하우스 몫 무효 횟수(보호막). 앤티를 걸 때 0으로 돌아간다.</summary>
        public int CutShields { get; internal set; }
        public bool IsBankrupt => Chips <= 0 && Pot <= 0;

        public Seat(Side side, int chips, IEnumerable<Slot> wheel, IEnumerable<Slot> outerRing = null)
        {
            Side = side;
            Chips = chips;
            _wheel = new List<Slot>(wheel);
            _outerRing = outerRing == null ? new List<Slot>() : new List<Slot>(outerRing);
        }

        internal void ReplaceSlot(int index, Slot slot)
        {
            _wheel[index] = slot;
        }
    }
}
