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
        /// <summary>딜러층이 하나 오를 때마다 딜러 시작 칩에 더하는 비율(2층 ×1.0, 3층 ×1.3, 4층 ×1.6).</summary>
        public const float DealerChipsPerFloor = 0.3f;

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
        private int _chipsAtEntry;

        /// <summary>지금 전투에 들어갈 때의 칩. 전투가 끝나면 이만큼까지만 가지고 나간다(ADR 0007).</summary>
        public int ChipsAtEntry => _chipsAtEntry;

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

        /// <summary>환전 규칙(딴 칩 중 칩으로 남기는 비율, 현금 → 칩 환전 비율). 시뮬레이션은 다른 값을 넣어 비교한다.</summary>
        public ChipExchangeRules Exchange { get; }

        /// <summary>현금: 전투에서 입장 칩보다 많이 딴 칩 중 칩으로 남기지 않은 몫을 카지노가 환전해 준 것. 문 선택 화면의 환전 창구에서 칩으로 바꿀 수 있다.</summary>
        public int Cash { get; private set; }

        /// <summary>방금 끝낸 전투의 딴 칩 정산 결과. 종료 화면 표시용.</summary>
        public ChipSettlement LastSettlement { get; private set; }

        /// <summary>전투를 끝내고 나갈 때의 정산 미리보기(칩으로 남는 몫, 현금으로 환전되는 몫, 상금).</summary>
        public ChipSettlement PreviewSettlement(PotBattle battle)
        {
            int excess = Math.Max(0, battle.Player.Chips - _chipsAtEntry);
            float keepShare = Math.Min(1f, Exchange.KeepShare + (_relics.Contains(RelicId.VipCard) ? RelicCatalog.VipKeepShareBonus : 0f));
            int keptExcess = (int)Math.Floor(excess * keepShare);
            return new ChipSettlement(
                entryChips: _chipsAtEntry,
                baseChips: Math.Min(battle.Player.Chips, _chipsAtEntry),
                keptExcess: keptExcess,
                cash: excess - keptExcess,
                winnings: battle.Outcome == BattleOutcome.PlayerWinsByBankrupt ? WinningsFor(battle.Profile) : 0);
        }

        /// <summary>환전 창구: 현금으로 칩을 산다(칩 1 = 현금 Exchange.CashPerChip). 살 수 있는 만큼만 사고 산 칩 수를 돌려준다.</summary>
        public int BuyChips(int chips)
        {
            if (Outcome != RunOutcome.InProgress || CurrentBattle != null || chips <= 0) return 0;
            int affordable = Math.Min(chips, Cash / Exchange.CashPerChip);
            if (Exchange.MaxChipsPerVisit > 0) affordable = Math.Min(affordable, Exchange.MaxChipsPerVisit - ChipsBoughtThisVisit);
            if (affordable <= 0) return 0;
            Cash -= affordable * Exchange.CashPerChip;
            Chips += affordable;
            ChipsBoughtThisVisit += affordable;
            return affordable;
        }

        /// <summary>이번 문 선택 화면에서 환전 창구로 산 칩 수(층마다 0으로 돌아간다).</summary>
        public int ChipsBoughtThisVisit { get; private set; }

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
            IEnumerable<RelicId> startingRelics = null,
            ChipExchangeRules exchange = null)
        {
            Exchange = exchange ?? ChipExchangeRules.Default;
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
            _chipsAtEntry = Chips;
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

            // 딴 칩 정산(ADR 0008): 잃은 칩은 그대로 잃는다. 입장 칩보다 많이 딴 칩은 일정 비율만 칩(목숨)으로 남고,
            // 나머지는 카지노가 현금으로 환전해 준다. 제로섬 전투가 목숨의 눈덩이가 되지 않으면서도 딴 칩은 전부 내 것이다.
            LastSettlement = PreviewSettlement(battle);
            Chips = LastSettlement.ChipsAfter;
            Cash += LastSettlement.Cash;
            _wheel.Clear();
            _wheel.AddRange(battle.Player.Wheel);

            if (battle.Outcome == BattleOutcome.PlayerWinsByCleanSweep)
            {
                PendingJackpot = FindUnclaimedJackpot(battle.Profile);
            }

            RelicId? prize = _enteredDoor >= 0 && _enteredDoor < _doorRelics.Count ? _doorRelics[_enteredDoor] : null;
            if (prize.HasValue && !_relics.Contains(prize.Value))
            {
                _relics.Add(prize.Value);
                LastRelicGained = prize;
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

        /// <summary>파산 승리 상금 = 딜러 시작 칩 × 비율(기본 0: 딴 칩 정산이 상금 역할을 한다).</summary>
        public int WinningsFor(DealerProfile dealer)
        {
            return (int)Math.Round(dealer.StartingChips * Exchange.WinningsRatio);
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
            ChipsBoughtThisVisit = 0;
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

        /// <summary>이 층(0부터)부터 딜러 앤티 +1.</summary>
        public const int DealerAnteBonusFromFloor = 3;

        private DealerProfile ScaleForFloor(DealerProfile dealer)
        {
            float scale = 1f + DealerChipsPerFloor * Math.Max(0, FloorIndex - 1);
            int anteBonus = FloorIndex >= DealerAnteBonusFromFloor ? 1 : 0;
            return scale <= 1f && anteBonus == 0 ? dealer : dealer.WithFloorScaling((int)Math.Round(dealer.StartingChips * scale), anteBonus);
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
            _doors.Add(ScaleForFloor(_dealerPool[first]()));
            if (_dealerPool.Count > 1)
            {
                int second = _rng.Next(_dealerPool.Count - 1);
                if (second >= first) second++;
                _doors.Add(ScaleForFloor(_dealerPool[second]()));
            }
        }
    }
}
