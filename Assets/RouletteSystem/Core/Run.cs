using System;
using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>층의 종류.</summary>
    public enum FloorKind
    {
        /// <summary>문 앞에서 딜러를 고른다(후보가 하나면 고정).</summary>
        Dealer,
        Boss,

        /// <summary>전투 없이 현금으로 룰렛을 다듬고 칩을 사고 슬롯머신을 하는 층(ADR 0009).</summary>
        Shop
    }

    public enum RunOutcome
    {
        InProgress,
        Escaped,
        Bankrupt
    }

    /// <summary>
    /// 한 번의 도전(런). 칩·현금·룰렛·유물이 전투를 넘어 이어진다.
    /// 기본 구성(ADR 0009): 1층 토끼 → 2~4층 딜러(문 선택) → 5층 보스 → 6층 상점 → 7~9층 딜러(2회차, 더 강함) → 10층 강화 보스.
    /// 상점 기능은 RunShop.cs(같은 클래스의 나머지 절반).
    /// </summary>
    public sealed partial class Run
    {
        /// <summary>기본 런의 층 구성.</summary>
        public static readonly IReadOnlyList<FloorKind> StandardLayout = new[]
        {
            FloorKind.Dealer, FloorKind.Dealer, FloorKind.Dealer, FloorKind.Dealer, FloorKind.Boss,
            FloorKind.Shop,
            FloorKind.Dealer, FloorKind.Dealer, FloorKind.Dealer, FloorKind.Boss
        };

        private readonly List<FloorKind> _layout = new List<FloorKind>();
        private readonly List<Slot> _startingWheel;

        /// <summary>층 구성(0부터). 0층은 언제나 튜토리얼 딜러.</summary>
        public IReadOnlyList<FloorKind> Layout => _layout;

        /// <summary>지금 층이 몇 회차(스테이지)인지. 보스를 하나 넘을 때마다 +1(0부터).</summary>
        public int Stage
        {
            get
            {
                int bosses = 0;
                for (int i = 0; i < FloorIndex && i < _layout.Count; i++)
                {
                    if (_layout[i] == FloorKind.Boss) bosses++;
                }

                return bosses;
            }
        }
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

        public FloorKind CurrentFloorKind => FloorIndex < _layout.Count ? _layout[FloorIndex] : FloorKind.Boss;

        /// <summary>다음 층의 종류(마지막 층이면 null). 종료 화면의 "다음으로" 문구용.</summary>
        public FloorKind? NextFloorKind => FloorIndex + 1 < _layout.Count ? _layout[FloorIndex + 1] : (FloorKind?)null;
        public IReadOnlyList<DealerProfile> Doors => _doors;

        /// <summary>문마다 걸린 유물(같은 순서). 그 딜러의 하우스 룰을 달성하고 이기면 받는다. 보스 문에는 없다(null).</summary>
        public IReadOnlyList<RelicId?> DoorRelics => _doorRelics;

        /// <summary>이번 런에 모은 유물.</summary>
        public IReadOnlyList<RelicId> Relics => _relics;

        /// <summary>문 카드 유물을 받으려면 그 전투에서 하우스 룰을 이만큼 달성하고 이겨야 한다(도전 유물, ADR 0010).</summary>
        public const int RelicChallengeHouseRules = 1;

        /// <summary>이 전투에서 유물 도전 조건(하우스 룰 달성)을 채웠는지. 이기기도 해야 받는다.</summary>
        public static bool RelicChallengeMet(PotBattle battle) => battle.HouseRulesAchieved >= RelicChallengeHouseRules;

        /// <summary>방금 끝낸 전투의 문에 유물이 걸려 있었지만 도전 조건을 못 채워 받지 못했다.</summary>
        public RelicId? LastRelicMissed { get; private set; }

        /// <summary>지금 들어간 문에 걸린 유물(없으면 null). 하우스 룰을 달성하고 이기면 CompleteBattle에서 받는다.</summary>
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

        /// <summary>방금 끝낸 전투(보스)에서 룰렛 형식(바깥 링)을 이 런 안에서 얻었다.</summary>
        public bool GainedWheelFormThisBattle { get; private set; }

        /// <summary>방금 끝낸 전투에서 받은 유물(없으면 null). 종료 화면 표시용.</summary>
        public RelicId? LastRelicGained { get; private set; }

        public Run(
            int seed,
            int startingChips,
            IEnumerable<Slot> startingWheel,
            Func<DealerProfile> tutorialDealer,
            IReadOnlyList<Func<DealerProfile>> dealerPool,
            Func<DealerProfile> boss,
            int floorCount = 0,
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
            _startingWheel = new List<Slot>(_wheel);
            _shopRng = new Random(seed ^ 0x5409);
            _outerRing = outerRing == null ? new List<Slot>() : new List<Slot>(outerRing);
            _tutorialDealer = tutorialDealer;
            _dealerPool = dealerPool;
            _boss = boss;
            // floorCount를 주면 예전 구성(딜러 … 마지막 보스, 상점 없음)을 쓴다. 테스트·시뮬레이션용.
            if (floorCount > 0)
            {
                for (int i = 0; i < floorCount; i++) _layout.Add(i == floorCount - 1 ? FloorKind.Boss : FloorKind.Dealer);
            }
            else
            {
                _layout.AddRange(StandardLayout);
            }

            FloorCount = _layout.Count;
            RollDoors();
        }

        /// <summary>문 하나를 골라 전투를 시작한다.</summary>
        public PotBattle EnterDoor(int doorIndex)
        {
            if (Outcome != RunOutcome.InProgress) throw new InvalidOperationException("런이 끝났습니다.");
            if (CurrentFloorKind == FloorKind.Shop) throw new InvalidOperationException("상점층에는 문이 없습니다. LeaveShop으로 다음 층에 갑니다.");
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
            LastRelicMissed = null;

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
            if (prize.HasValue && !RelicChallengeMet(battle))
            {
                LastRelicMissed = prize;
                prize = null;
            }

            if (prize.HasValue && !_relics.Contains(prize.Value))
            {
                _relics.Add(prize.Value);
                LastRelicGained = prize;
            }

            bool beatBoss = CurrentFloorKind == FloorKind.Boss;
            if (beatBoss && _outerRing.Count == 0 && battle.Profile.TableOuterRing.Count > 0)
            {
                // 보스를 이기면 그 보스의 룰렛 형식(바깥 링)을 이 런 안에서 바로 얻는다. 다음 런 해금도 함께(화면이 저장).
                _outerRing.AddRange(battle.Profile.TableOuterRing);
                UnlockedOuterRingThisRun = true;
                GainedWheelFormThisBattle = true;
            }
            else
            {
                GainedWheelFormThisBattle = false;
            }

            FloorIndex++;
            if (FloorIndex >= FloorCount)
            {
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

        /// <summary>확정 획득한 JACKPOT 칸으로 내 칸 하나를 덮어쓴다(몰수 불가). -1이면 포기한다.</summary>
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
            if (CurrentFloorKind == FloorKind.Shop)
            {
                StockShop();
                return;
            }

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

        /// <summary>회차(스테이지) 안에서 3번째 딜러층부터 딜러 베팅 +1.</summary>
        public const int DealerAnteBonusFromPosition = 3;

        /// <summary>
        /// 2회차 딜러: 강해지는 몫은 주로 카지노 링(몰수 없는 바깥 링)이 맡는다. 링만으로 7층 탈락이 4배가 되어
        /// (시뮬레이션 2026-10-02) 기존 가산(칩 ×1.2·베팅 +2·성향 +4)을 칩 ×1.0·베팅 +1·성향 +0으로 줄였다.
        /// </summary>
        public const float SecondStageChipBase = 1.0f;
        public const int SecondStageAnteBonus = 1;
        public const int SecondStageCashOutBonus = 0;

        /// <summary>2회차 보스: 칩 배율·베팅·보험·성향 가산과 이름(시뮬레이션으로 정한 임시값).</summary>
        public const float SecondStageBossChipScale = 1.6f;
        public const int SecondStageBossAnteBonus = 3;
        public const int SecondStageBossInsuranceBonus = 2;
        public const int SecondStageBossCashOutBonus = 6;
        public const string SecondStageBossSuffix = " · 야간 근무";

        /// <summary>2회차 딜러가 카지노 링(몰수 없는 바깥 링)을 쓰는지. 시뮬레이션 비교용으로 끌 수 있다.</summary>
        public static bool SecondStageDealersUseCasinoRing = true;

        /// <summary>회차 안에서 이 층이 몇 번째 딜러층인지(1부터). 1층 토끼는 0.</summary>
        private int DealerPositionInStage()
        {
            int position = 0;
            for (int i = FloorIndex; i >= 1 && _layout[i] == FloorKind.Dealer; i--) position++;
            return position;
        }

        private DealerProfile ScaleForFloor(DealerProfile dealer)
        {
            int position = DealerPositionInStage();
            float baseScale = Stage == 0 ? 1f : SecondStageChipBase;
            float scale = baseScale + DealerChipsPerFloor * Math.Max(0, position - 1);
            int anteBonus = (position >= DealerAnteBonusFromPosition ? 1 : 0) + (Stage > 0 ? SecondStageAnteBonus : 0);
            int cashOutBonus = Stage > 0 ? SecondStageCashOutBonus : 0;
            DealerProfile scaled = Math.Abs(scale - 1f) < 0.001f && anteBonus == 0 && cashOutBonus == 0
                ? dealer
                : dealer.WithStakes(dealer.Name, (int)Math.Round(dealer.StartingChips * scale), anteBonus, 0, cashOutBonus);
            // 2회차 딜러는 몰수 없는 카지노 링을 쓴다(플레이어가 5층 보스에게서 바깥 링을 얻은 뒤의 맞대응).
            return Stage > 0 && SecondStageDealersUseCasinoRing ? scaled.WithDealerOuterRing(BattlePresets.CreateCasinoRing()) : scaled;
        }

        private DealerProfile ScaleBoss(DealerProfile boss)
        {
            if (Stage == 0) return boss;
            return boss.WithStakes(boss.Name + SecondStageBossSuffix, (int)Math.Round(boss.StartingChips * SecondStageBossChipScale),
                SecondStageBossAnteBonus, SecondStageBossInsuranceBonus, SecondStageBossCashOutBonus);
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
                _doors.Add(ScaleBoss(_boss()));
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
