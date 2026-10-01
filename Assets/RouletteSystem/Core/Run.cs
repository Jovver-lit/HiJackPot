using System;
using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>층의 종류. MVP에는 딜러층과 보스층만 있다(상점층은 정식판).</summary>
    public enum FloorKind
    {
        /// <summary>문 앞에서 딜러를 고른다(후보가 하나면 고정).</summary>
        Dealer,
        Boss
    }

    public enum RunOutcome
    {
        InProgress,
        Escaped,
        Bankrupt
    }

    /// <summary>
    /// 한 번의 도전(런). 칩과 플레이어 룰렛이 전투를 넘어 이어진다.
    /// MVP 스테이지: 1층 토끼(고정) → 2~4층 딜러(문 두 개 중 선택) → 5층 보스.
    /// </summary>
    public sealed class Run
    {
        /// <summary>파산 승리 상금 = 그 딜러 시작 칩 × 이 비율(임시값).</summary>
        public const float WinningsRatio = 0.5f;

        private readonly Random _rng;
        private readonly List<Slot> _wheel;
        private readonly List<Slot> _outerRing;
        private readonly IReadOnlyList<Func<DealerProfile>> _dealerPool;
        private readonly Func<DealerProfile> _tutorialDealer;
        private readonly Func<DealerProfile> _boss;
        private readonly List<DealerProfile> _doors = new List<DealerProfile>();
        private readonly List<RelicId?> _doorRelics = new List<RelicId?>();
        private readonly List<RelicId> _relics = new List<RelicId>();
        private readonly Random _relicRng;
        private readonly int _seed;
        private int _enteredDoor = -1;

        public int Chips { get; private set; }
        public IReadOnlyList<Slot> Wheel => _wheel;

        /// <summary>플레이어의 바깥 링. 이전 런에서 보스를 이겨 해금했을 때만 있다.</summary>
        public IReadOnlyList<Slot> OuterRing => _outerRing;

        /// <summary>이번 런에서 보스를 이겨 바깥 링(룰렛 형식)을 새로 해금했다. 저장은 화면 쪽이 맡는다.</summary>
        public bool UnlockedOuterRingThisRun { get; private set; }

        /// <summary>0부터 센다. 0 = 1층.</summary>
        public int FloorIndex { get; private set; }

        public int FloorCount { get; }
        public RunOutcome Outcome { get; private set; } = RunOutcome.InProgress;
        public PotBattle CurrentBattle { get; private set; }

        /// <summary>완전 강탈로 확정 획득한, 아직 룰렛에 넣지 않은 JACKPOT 칸.</summary>
        public Slot PendingJackpot { get; private set; }

        public FloorKind CurrentFloorKind => FloorIndex == FloorCount - 1 ? FloorKind.Boss : FloorKind.Dealer;
        public IReadOnlyList<DealerProfile> Doors => _doors;

        /// <summary>문마다 걸린 유물 상금(같은 순서). 그 딜러를 이기면 받는다. 보스 문에는 없다(null).</summary>
        public IReadOnlyList<RelicId?> DoorRelics => _doorRelics;

        /// <summary>이번 런에 모은 유물.</summary>
        public IReadOnlyList<RelicId> Relics => _relics;

        /// <summary>지금 들어간 문에 걸린 유물 상금(없으면 null). 이기면 CompleteBattle에서 받는다.</summary>
        public RelicId? CurrentPrize => _enteredDoor >= 0 && _enteredDoor < _doorRelics.Count ? _doorRelics[_enteredDoor] : null;

        /// <summary>방금 끝낸 전투에서 받은 유물(없으면 null). 종료 화면 표시용.</summary>
        public RelicId? LastRelicGained { get; private set; }

        public Run(
            int seed,
            int startingChips,
            IEnumerable<Slot> startingWheel,
            Func<DealerProfile> tutorialDealer,
            IReadOnlyList<Func<DealerProfile>> dealerPool,
            Func<DealerProfile> boss,
            int floorCount = 5,
            IEnumerable<Slot> outerRing = null,
            IEnumerable<RelicId> startingRelics = null)
        {
            _seed = seed;
            _rng = new Random(seed);
            // 유물 상금은 별도 RNG로 굴려 문(딜러) 순서가 유물 때문에 바뀌지 않게 한다.
            _relicRng = new Random(seed ^ 0x5EED);
            if (startingRelics != null) _relics.AddRange(startingRelics);
            Chips = startingChips;
            _wheel = new List<Slot>(startingWheel);
            _outerRing = outerRing == null ? new List<Slot>() : new List<Slot>(outerRing);
            _tutorialDealer = tutorialDealer;
            _dealerPool = dealerPool;
            _boss = boss;
            FloorCount = floorCount;
            RollDoors();
        }

        /// <summary>문 하나를 골라 전투를 시작한다.</summary>
        public PotBattle EnterDoor(int doorIndex)
        {
            if (Outcome != RunOutcome.InProgress) throw new InvalidOperationException("런이 끝났습니다.");
            if (CurrentBattle != null) throw new InvalidOperationException("진행 중인 전투가 있습니다.");
            if (PendingJackpot != null) throw new InvalidOperationException("획득한 JACKPOT 칸을 먼저 배치하세요.");
            if (doorIndex < 0 || doorIndex >= _doors.Count) throw new ArgumentOutOfRangeException(nameof(doorIndex));

            _enteredDoor = doorIndex;
            CurrentBattle = new PotBattle(_wheel, Chips, _doors[doorIndex], _seed * 31 + FloorIndex, _outerRing, _relics);
            return CurrentBattle;
        }

        /// <summary>
        /// 끝난 전투의 결과를 런에 반영한다. 칩과 룰렛(HIJACK한 칸)이 그대로 이어지고,
        /// 파산 승리는 상금, 완전 강탈은 JACKPOT 칸 확정 획득이다.
        /// </summary>
        public void CompleteBattle()
        {
            PotBattle battle = CurrentBattle ?? throw new InvalidOperationException("진행 중인 전투가 없습니다.");
            if (battle.Phase != BattlePhase.Ended) throw new InvalidOperationException("전투가 아직 끝나지 않았습니다.");
            CurrentBattle = null;
            LastRelicGained = null;

            if (battle.Outcome == BattleOutcome.DealerWins)
            {
                Chips = 0;
                Outcome = RunOutcome.Bankrupt;
                return;
            }

            Chips = battle.Player.Chips;
            _wheel.Clear();
            _wheel.AddRange(battle.Player.Wheel);

            if (battle.Outcome == BattleOutcome.PlayerWinsByBankrupt)
            {
                Chips += WinningsFor(battle.Profile);
            }

            RelicId? prize = _enteredDoor >= 0 && _enteredDoor < _doorRelics.Count ? _doorRelics[_enteredDoor] : null;
            if (prize.HasValue && !_relics.Contains(prize.Value))
            {
                _relics.Add(prize.Value);
                LastRelicGained = prize;
            }
            else if (battle.Outcome == BattleOutcome.PlayerWinsByCleanSweep)
            {
                PendingJackpot = FindUnclaimedJackpot(battle.Profile);
            }

            bool beatBoss = CurrentFloorKind == FloorKind.Boss;
            FloorIndex++;
            if (FloorIndex >= FloorCount)
            {
                if (beatBoss && _outerRing.Count == 0 && battle.Profile.TableOuterRing.Count > 0)
                {
                    UnlockedOuterRingThisRun = true;
                }

                Outcome = RunOutcome.Escaped;
                return;
            }

            RollDoors();
        }

        /// <summary>파산 승리 상금 = 딜러 시작 칩 × 비율. 유물 「VIP 회원증」이 ×1.5.</summary>
        public int WinningsFor(DealerProfile dealer)
        {
            float ratio = WinningsRatio * (_relics.Contains(RelicId.VipCard) ? RelicCatalog.VipWinningsMultiplier : 1f);
            return (int)Math.Round(dealer.StartingChips * ratio);
        }

        /// <summary>확정 획득한 JACKPOT 칸으로 내 칸 하나를 덮어쓴다(하우스 몫 불가). -1이면 포기한다.</summary>
        public bool PlacePendingJackpot(int playerIndex)
        {
            if (PendingJackpot == null) return false;
            if (playerIndex >= 0)
            {
                if (playerIndex >= _wheel.Count || _wheel[playerIndex].Kind == SlotKind.HouseCut) return false;
                _wheel[playerIndex] = PendingJackpot.AsStolen($"jackpot_{FloorIndex}_{PendingJackpot.Id}");
            }

            PendingJackpot = null;
            return true;
        }

        private Slot FindUnclaimedJackpot(DealerProfile dealer)
        {
            foreach (Slot slot in dealer.Wheel)
            {
                if (!slot.IsJackpot) continue;
                foreach (Slot mine in _wheel)
                {
                    if (mine.IsStolen && mine.IsJackpot && mine.Label == slot.Label) return null;
                }

                return slot;
            }

            return null;
        }

        private void RollDoors()
        {
            _doors.Clear();
            _doorRelics.Clear();
            _enteredDoor = -1;
            RollDealers();

            // 유물은 딜러층 문에만 걸린다. 같은 층의 두 문에는 서로 다른, 아직 없는 유물을 건다.
            List<RelicId> pool = new List<RelicId>();
            foreach (Relic relic in RelicCatalog.Relics)
            {
                if (!_relics.Contains(relic.Id)) pool.Add(relic.Id);
            }

            for (int i = 0; i < _doors.Count; i++)
            {
                if (CurrentFloorKind == FloorKind.Boss || pool.Count == 0)
                {
                    _doorRelics.Add(null);
                    continue;
                }

                int pick = _relicRng.Next(pool.Count);
                _doorRelics.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
        }

        private void RollDealers()
        {
            if (FloorIndex == 0)
            {
                _doors.Add(_tutorialDealer());
                return;
            }

            if (CurrentFloorKind == FloorKind.Boss)
            {
                _doors.Add(_boss());
                return;
            }

            int first = _rng.Next(_dealerPool.Count);
            _doors.Add(_dealerPool[first]());
            if (_dealerPool.Count > 1)
            {
                int second = _rng.Next(_dealerPool.Count - 1);
                if (second >= first) second++;
                _doors.Add(_dealerPool[second]());
            }
        }
    }
}
