using System;
using System.Collections.Generic;
using System.Linq;

namespace RouletteLike.Battle
{
    /// <summary>한 전투가 지금 무엇을 기다리는지.</summary>
    public enum BattlePhase
    {
        /// <summary>라운드가 끝났거나 시작 전. StartRound를 기다린다.</summary>
        RoundOver,

        /// <summary>현재 차례인 쪽이 베팅을 걸기를 기다린다.</summary>
        AwaitingAnte,

        /// <summary>SPIN을 반복하거나 CASH OUT할 수 있다.</summary>
        Spinning,

        Ended
    }

    public enum BattleOutcome
    {
        None,
        PlayerWinsByBankrupt,
        PlayerWinsByCleanSweep,
        DealerWins
    }

    public sealed class LandingResult
    {
        public Side Side;
        public int Index;
        public SlotKind Kind;
        public IReadOnlyList<int> Group = Array.Empty<int>();
        public int Amount;
        public int PotBefore;
        public int PotAfter;
        public bool EndedTurn;
        public string Formula = "";

        /// <summary>바깥 링이 멈춘 칸(-1 = 바깥 링 없음)과 그 종류.</summary>
        public int OuterIndex = -1;
        public SlotKind? OuterKind;

        /// <summary>안쪽과 바깥 링이 같은 종류로 멈췄다.</summary>
        public bool JackpotLine;

        /// <summary>몰수에 걸렸지만 보호막이 막았다.</summary>
        public bool CutShielded;

        /// <summary>안쪽 또는 바깥 몰수로 판돈이 증발했다.</summary>
        public bool HouseCutHit;

        /// <summary>[라운드 시작]·[CASH OUT] 특수 칸에 착지해 그 효과가 즉시 발동했다.</summary>
        public bool KeywordTriggered;
    }

    public sealed class CashOutResult
    {
        public Side Side;
        public int Pot;
        public int OpponentInsurance;
        public int Damage;
        public bool FullCoverage;

        /// <summary>유물(도파민 주사기·이중 장부)로 더해진 피해.</summary>
        public int RelicBonus;
    }

    public enum HijackError
    {
        None,
        NoChance,
        AlreadyUsedThisRound,
        InvalidIndex,
        SourceSealed,
        HouseCutProtected,
        BattleEnded
    }

    /// <summary>
    /// 판돈 모델의 한 전투 규칙. 화면·연출과 분리되어 있으며 같은 시드면 같은 결과를 낸다.
    /// 한 라운드: 선공 코인플립 → 각자 한 턴(베팅 → SPIN 반복 → CASH OUT 또는 몰수).
    /// </summary>
    public sealed class PotBattle
    {
        private readonly Random _rng;
        private readonly List<string> _log = new List<string>();
        private readonly List<KeyValuePair<int, Slot>> _seizedSlots = new List<KeyValuePair<int, Slot>>();
        private readonly List<string> _roundStartEffects = new List<string>();

        /// <summary>역탈취는 전투당 이 횟수까지만 일어난다(첫 플레이테스트: 보스전 봉인 과다로 중도 포기).</summary>
        public const int MaxCounterHijacksPerBattle = 1;
        private int _turnsThisRound;
        private int _stolenCount;

        public Seat Player { get; }
        public Seat Dealer { get; }
        public DealerProfile Profile { get; }
        public int Round { get; private set; }
        public Side Active { get; private set; }
        public Side FirstThisRound { get; private set; }
        public BattlePhase Phase { get; private set; } = BattlePhase.RoundOver;
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;
        public int HijackChances { get; private set; }

        /// <summary>하우스 룰 진행도(예: 여우의 작은 CASH OUT 횟수). 목표에 닿으면 HIJACK 기회가 되고 0으로 돌아간다.</summary>
        public int HouseRuleProgress { get; private set; }

        /// <summary>하우스 룰 한 번 달성에 필요한 진행도.</summary>
        public int HouseRuleGoal => Profile.HouseRule == HouseRule.SmallCashOuts || Profile.HouseRule == HouseRule.JackpotLines ? 2 : 1;

        private bool _playerMultipliedThisTurn;
        public bool HijackUsedThisRound { get; private set; }

        /// <summary>플레이어가 방금 몰수에 걸려 딜러가 역탈취할 수 있는 상태.</summary>
        public bool CounterHijackPending { get; private set; }

        public IReadOnlyList<string> Log => _log;

        /// <summary>방금 시작한 라운드에 발동한 [라운드 시작] 효과(서비스·선불 등). 화면이 크게 보여 주는 용도.</summary>
        public IReadOnlyList<string> RoundStartEffects => _roundStartEffects;

        /// <summary>이번 전투에 남은 역탈취 횟수.</summary>
        public int CounterHijacksRemaining => Profile.CounterHijacks && !HasRelic(RelicId.SeizureSeal)
            ? MaxCounterHijacksPerBattle - _seizedSlots.Count - _returnedSeizures
            : 0;

        private int _returnedSeizures;
        private Side? _guaranteedInitiative;
        private readonly HashSet<RelicId> _playerRelics = new HashSet<RelicId>();

        /// <summary>NUDGE 기본 횟수(전투당). 유물 「끈 달린 칩」이 +1.</summary>
        public const int BaseNudgesPerBattle = 1;

        /// <summary>플레이어가 가진 유물(런에서 넘어온다).</summary>
        public IReadOnlyCollection<RelicId> PlayerRelics => _playerRelics;

        /// <summary>이번 전투에 남은 NUDGE 횟수. 몰수에 걸렸을 때 룰렛을 옆 칸으로 밀 수 있다.</summary>
        public int NudgesRemaining { get; private set; }

        /// <summary>도파민(유물 「도파민 주사기」 전용): 이번 전투에서 몰수에 걸린 횟수만큼 차오르고 CASH OUT 피해에 더해진다.</summary>
        public int Dopamine { get; private set; }

        public bool HasRelic(RelicId id) => _playerRelics.Contains(id);

        /// <summary>이번 전투에서 하우스 룰을 달성한 횟수. 한 번 이상 달성하고 이기면 문 카드의 유물을 받는다(ADR 0010).</summary>
        public int HouseRulesAchieved { get; private set; }

        /// <param name="playerOuterRing">플레이어 자신의 바깥 링(해금된 경우). 없으면 딜러의 테이블 바깥 링을 빌려 쓴다.</param>
        /// <param name="playerRelics">플레이어 유물. 효과는 이 전투 규칙 곳곳에서 확인한다.</param>
        public PotBattle(IEnumerable<Slot> playerWheel, int playerChips, DealerProfile dealer, int seed, IReadOnlyList<Slot> playerOuterRing = null, IEnumerable<RelicId> playerRelics = null)
        {
            if (playerRelics != null)
            {
                foreach (RelicId relic in playerRelics) _playerRelics.Add(relic);
            }

            NudgesRemaining = BaseNudgesPerBattle + (HasRelic(RelicId.StringChip) ? 1 : 0);
            Profile = dealer;
            IReadOnlyList<Slot> tableRing = dealer.TableOuterRing;
            Player = new Seat(Side.Player, playerChips, playerWheel,
                playerOuterRing != null && playerOuterRing.Count > 0 ? playerOuterRing : tableRing);
            Dealer = new Seat(Side.Dealer, dealer.StartingChips, dealer.Wheel, dealer.DealerOuterRing.Count > 0 ? dealer.DealerOuterRing : tableRing);
            _rng = new Random(seed);
        }

        public Seat SeatOf(Side side) => side == Side.Player ? Player : Dealer;

        /// <summary>딜러가 몰수 없는 카지노 링을 쓰는지(손님 바깥 링과 사양이 다르다).</summary>
        public bool DealerHasCasinoRing => Profile.DealerOuterRing.Count > 0;
        public Seat Opponent(Side side) => side == Side.Player ? Dealer : Player;
        public Seat ActiveSeat => SeatOf(Active);

        /// <summary>
        /// 라운드를 시작한다: [라운드 시작] 칸 발동 → 선공 코인플립. 선공 쪽이 베팅을 걸 차례가 된다.
        /// </summary>
        public Side StartRound()
        {
            RequirePhase(BattlePhase.RoundOver);
            Round++;
            _turnsThisRound = 0;
            _roundStartEffects.Clear();
            HijackUsedThisRound = false;

            ApplyRoundStartSlots(Player);
            ApplyRoundStartSlots(Dealer);
            if (ApplyTableFee())
            {
                return Active;
            }

            if (_guaranteedInitiative.HasValue)
            {
                FirstThisRound = _guaranteedInitiative.Value;
                _guaranteedInitiative = null;
                Active = FirstThisRound;
                Phase = BattlePhase.AwaitingAnte;
                Write($"라운드 {Round}: 선불 착지 효과 → {Name(Active)} 선공 확정");
                _roundStartEffects.Add($"{Name(Active)}의 선불(착지): 선공 확정");
                ApplyInitiativeRelics();
                return Active;
            }

            bool playerPrepaid = HasActiveSlot(Player, SlotKind.Initiative);
            bool dealerPrepaid = HasActiveSlot(Dealer, SlotKind.Initiative);
            if (playerPrepaid != dealerPrepaid)
            {
                // 선불: 동전을 두 번 던져 한 번이라도 이기면 선공(75%).
                Side holder = playerPrepaid ? Side.Player : Side.Dealer;
                bool holderWins = _rng.Next(2) == 0 || _rng.Next(2) == 0;
                FirstThisRound = holderWins ? holder : (holder == Side.Player ? Side.Dealer : Side.Player);
                Active = FirstThisRound;
                Phase = BattlePhase.AwaitingAnte;
                Write($"라운드 {Round}: 코인플립(선불로 두 번) → {Name(Active)} 선공");
                _roundStartEffects.Add($"{Name(holder)}의 선불: 코인플립 두 번 → {(holderWins ? "선공 획득" : "그래도 후공")}");
                ApplyInitiativeRelics();
                return Active;
            }

            FirstThisRound = _rng.Next(2) == 0 ? Side.Player : Side.Dealer;
            Active = FirstThisRound;
            Phase = BattlePhase.AwaitingAnte;
            Write($"라운드 {Round}: 코인플립 → {Name(Active)} 선공");
            ApplyInitiativeRelics();
            return Active;
        }

        /// <summary>테이블 마감: 이 라운드부터 매 라운드 시작에 양쪽이 테이블 사용료 1을 낸다(교착 방지, 한 전투 3~5분 목표).</summary>
        public const int TableFeeStartRound = 10;

        /// <summary>이 라운드부터 사용료가 2로 오른다.</summary>
        public const int TableFeeSurgeRound = 15;

        /// <summary>이번 라운드의 테이블 사용료(0 = 아직 마감 전).</summary>
        public int TableFee => Round >= TableFeeSurgeRound ? 2 : Round >= TableFeeStartRound ? 1 : 0;

        /// <summary>테이블 사용료를 걷는다. 누군가 파산해 전투가 끝나면 true.</summary>
        private bool ApplyTableFee()
        {
            int fee = TableFee;
            if (fee <= 0) return false;
            Player.Chips = Math.Max(0, Player.Chips - fee);
            Dealer.Chips = Math.Max(0, Dealer.Chips - fee);
            Write($"테이블 마감: 사용료 양쪽 칩 −{fee}");
            _roundStartEffects.Add($"테이블 마감: 사용료 양쪽 칩 −{fee}");
            return CheckBankrupt();
        }

        /// <summary>[라운드 시작] 유물: 「뒷면만 나오는 동전」은 후공이 된 라운드에 칩 +1.</summary>
        private void ApplyInitiativeRelics()
        {
            if (FirstThisRound == Side.Dealer && HasRelic(RelicId.LuckyCoin))
            {
                Player.Chips += 1;
                Write("유물 「뒷면만 나오는 동전」: 후공 → 칩 +1");
                _roundStartEffects.Add("뒷면만 나오는 동전: 후공 → 칩 +1");
            }
        }

        /// <summary>
        /// NUDGE: 몰수에 걸린 플레이어 룰렛을 옆 칸으로 민다. 횟수를 하나 쓰며, 어느 칸으로 밀지는 화면이 정해 Land에 넘긴다.
        /// </summary>
        public bool UseNudge()
        {
            if (Active != Side.Player || Phase != BattlePhase.Spinning || NudgesRemaining <= 0)
            {
                return false;
            }

            NudgesRemaining--;
            Write($"NUDGE: 룰렛을 옆 칸으로 밀었다 (남은 {NudgesRemaining})");
            return true;
        }

        /// <summary>베팅 가능 범위의 최대치. 테이블 한도와 남은 칩 중 작은 쪽.</summary>
        public int MaxAnte(Side side)
        {
            int limit = Profile.TableLimit + (side == Side.Player && HasRelic(RelicId.HighRollerBadge) ? 1 : 0);
            return Math.Max(0, Math.Min(limit, SeatOf(side).Chips));
        }

        /// <summary>
        /// 현재 차례인 쪽이 베팅을 건다. 1 ~ MaxAnte로 맞춰지며, 이전 턴의 보험은 여기서 사라진다.
        /// </summary>
        public int PlaceAnte(int amount)
        {
            RequirePhase(BattlePhase.AwaitingAnte);
            Seat seat = ActiveSeat;
            int ante = Math.Max(1, Math.Min(amount, MaxAnte(Active)));
            seat.Insurance = Active == Side.Dealer ? Profile.BaseInsurance : HasRelic(RelicId.InsurancePolicy) ? 1 : 0;
            seat.CutShields = 0;
            seat.IgnoresInsuranceThisTurn = false;
            if (Active == Side.Player) _playerMultipliedThisTurn = false;
            seat.Chips -= ante;
            seat.Ante = ante;
            seat.Pot = ante;
            SpinsThisTurn = 0;
            Phase = BattlePhase.Spinning;
            Write($"{Name(Active)} 베팅 {ante}");
            return ante;
        }

        /// <summary>
        /// 현재 차례인 쪽의 룰렛이 index 칸에 착지했다. 칸은 혼자 발동한다(테이블 규칙 「연쇄」가 있을 때만 이어진 같은 종류 칸이 한 묶음으로).
        /// 바깥 링이 있으면 outerIndex 칸도 함께 멈춘다: 보호막은 착지보다 먼저, 레이즈·보험은 착지 뒤에 더하고,
        /// 배율 링은 안쪽 효과를 한 번 더 발동한다. 안쪽과 바깥이 같은 종류면 잭팟 라인으로 안쪽 효과가 또 한 번 발동한다.
        /// 바깥 몰수는 안쪽 효과가 끝난 뒤 판돈을 증발시킨다(보호막이 있으면 막는다).
        /// </summary>
        public LandingResult Land(int index, int outerIndex = -1)
        {
            RequirePhase(BattlePhase.Spinning);
            SpinsThisTurn++;
            Seat seat = ActiveSeat;
            if (index < 0 || index >= seat.Wheel.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Slot outer = seat.HasOuterRing && outerIndex >= 0 && outerIndex < seat.OuterRing.Count
                ? seat.OuterRing[outerIndex]
                : null;
            Slot slot = seat.Wheel[index];
            LandingResult result = new LandingResult
            {
                Side = Active,
                Index = index,
                OuterIndex = outer == null ? -1 : outerIndex,
                Kind = slot.Kind,
                OuterKind = outer?.Kind,
                PotBefore = seat.Pot
            };

            List<string> parts = new List<string>();
            if (outer != null && outer.Kind == SlotKind.CutShield)
            {
                seat.CutShields++;
                parts.Add("바깥 보호막");
            }

            if (!slot.FiresOnLand)
            {
                result.Group = new[] { index };
                parts.Add(slot.Kind == SlotKind.Sealed ? "봉인 칸 · 효과 없음" : ApplyKeywordLanding(seat, slot, result));
                ApplyOuterBonus(seat, outer, parts);
                return FinishLanding(result, seat, parts);
            }

            List<int> group = LandingGroup(Active, index);
            result.Group = group;

            if (Active == Side.Player && ComebackBoost > 0f && IsGoodSlot(slot))
            {
                _comebackSuspendedThroughRound = Round + 1;
                Write("(역전 보정 해제: 좋은 칸이 걸렸다)");
            }

            if (slot.Kind == SlotKind.HouseCut)
            {
                if (seat.CutShields > 0)
                {
                    seat.CutShields--;
                    result.CutShielded = true;
                    parts.Add("몰수 → 보호막이 막았다 (판돈 유지)");
                    ApplyOuterBonus(seat, outer, parts);
                    return FinishLanding(result, seat, parts);
                }

                ApplyHouseCut(seat, result, parts, "몰수");
                return FinishLanding(result, seat, parts);
            }

            if (Active == Side.Player && slot.Kind == SlotKind.Multiplier && HasRelic(RelicId.FoldedCorner))
            {
                seat.Pot += 1;
                parts.Add($"모서리 접힌 카드: 곱하기 전에 판돈 +1 = {seat.Pot}");
            }

            parts.Add(ApplyInnerEffect(seat, slot, group, result));
            if (Active == Side.Player && HasSameKindNeighbor(seat.Wheel, index) && HasRelic(RelicId.StickyDivider))
            {
                seat.Pot += RelicCatalog.StickyDividerBonus;
                parts.Add($"끈적한 칸막이: 옆에 같은 칸 → 판돈 +{RelicCatalog.StickyDividerBonus} = {seat.Pot}");
            }

            if (outer != null && outer.Kind == SlotKind.Multiplier)
            {
                parts.Add("배율 링: " + ApplyInnerEffect(seat, slot, group, result));
            }

            if (outer != null && outer.Kind == slot.Kind)
            {
                result.JackpotLine = true;
                parts.Add("잭팟 라인! " + ApplyInnerEffect(seat, slot, group, result));
                if (Active == Side.Player && Profile.HouseRule == HouseRule.JackpotLines)
                {
                    AdvanceHouseRule("잭팟 라인");
                }
            }

            ApplyOuterBonus(seat, outer, parts);
            if (outer != null && outer.Kind == SlotKind.HouseCut)
            {
                if (seat.CutShields > 0)
                {
                    seat.CutShields--;
                    result.CutShielded = true;
                    parts.Add("바깥 몰수 → 보호막이 막았다");
                }
                else
                {
                    ApplyHouseCut(seat, result, parts, "바깥 몰수");
                }
            }

            return FinishLanding(result, seat, parts);
        }

        private void ApplyHouseCut(Seat seat, LandingResult result, List<string> parts, string label)
        {
            result.Amount = seat.Pot;
            result.HouseCutHit = true;
            parts.Add($"{label} → 판돈 {seat.Pot} 증발");
            if (Active == Side.Player)
            {
                if (HasRelic(RelicId.DopamineShot))
                {
                    Dopamine++;
                    parts.Add($"도파민 +1 = {Dopamine}");
                }

                if (HasRelic(RelicId.Consolation) && seat.Ante > 0)
                {
                    seat.Chips += seat.Ante;
                    parts.Add($"위로금 봉투: 베팅 {seat.Ante} 반환");
                }
            }

            seat.Pot = 0;
            seat.Ante = 0;
            result.EndedTurn = true;
            if (Active == Side.Player && CounterHijacksRemaining > 0)
            {
                CounterHijackPending = true;
            }
        }

        /// <summary>
        /// [라운드 시작]·[CASH OUT] 특수 칸에 착지하면 그 효과가 즉시 한 번 더 발동한다(첫 플레이테스트: 착지해도 아무 일 없어 죽은 칸처럼 보였다).
        /// 서비스류: 칩 즉시 지급 / 선불: 다음 라운드 선공 확정 / 허풍: 이번 턴 CASH OUT에서 상대 보험 완전 무시.
        /// </summary>
        private string ApplyKeywordLanding(Seat seat, Slot slot, LandingResult result)
        {
            result.KeywordTriggered = true;
            switch (slot.Kind)
            {
                case SlotKind.Dividend:
                    seat.Chips += slot.Value;
                    result.Amount = slot.Value;
                    return $"{slot.Label} 착지 → 즉시 칩 +{slot.Value}";
                case SlotKind.Initiative:
                    _guaranteedInitiative = Active;
                    return $"{slot.Label} 착지 → 다음 라운드 선공 확정";
                case SlotKind.MinimumPayout:
                    seat.IgnoresInsuranceThisTurn = true;
                    return $"{slot.Label} 착지 → 이번 턴 CASH OUT은 상대 보험 무시";
                default:
                    result.KeywordTriggered = false;
                    return $"{slot.Label} · 착지 효과 없음";
            }
        }

        /// <summary>안쪽 칸 묶음의 효과를 한 번 적용하고 계산식을 돌려준다(배율 링·잭팟 라인이면 여러 번 불린다).</summary>
        private string ApplyInnerEffect(Seat seat, Slot slot, List<int> group, LandingResult result)
        {
            int before = seat.Pot;
            switch (slot.Kind)
            {
                case SlotKind.Raise:
                {
                    int sum = SumValues(seat.Wheel, group);
                    seat.Pot += sum;
                    result.Amount += sum;
                    return $"레이즈 {JoinValues(seat.Wheel, group, " + ")} → 판돈 {before} + {sum} = {seat.Pot}";
                }
                case SlotKind.Multiplier:
                {
                    int product = 1;
                    foreach (int i in group) product *= Math.Max(1, seat.Wheel[i].Value);
                    seat.Pot *= product;
                    result.Amount = Math.Max(result.Amount, 1) * product;
                    if (Active == Side.Player && product > 1) _playerMultipliedThisTurn = true;
                    return $"배율 {JoinValues(seat.Wheel, group, " × ")} → 판돈 {before} × {product} = {seat.Pot}";
                }
                case SlotKind.Insurance:
                {
                    int sum = SumValues(seat.Wheel, group);
                    seat.Insurance += sum;
                    result.Amount += sum;
                    return $"보험 {JoinValues(seat.Wheel, group, " + ")} → 보험 {seat.Insurance}";
                }
                case SlotKind.Dividend:
                {
                    int sum = SumValues(seat.Wheel, group);
                    seat.Chips += sum;
                    result.Amount += sum;
                    return $"배당 {JoinValues(seat.Wheel, group, " + ")} → 칩 +{sum}";
                }
                case SlotKind.CutShield:
                {
                    seat.CutShields += group.Count;
                    result.Amount += group.Count;
                    return $"{slot.Label} → 이번 턴 몰수 무효 {seat.CutShields}회";
                }
                default:
                    return $"{slot.Label} · 효과 없음";
            }
        }

        /// <summary>바깥 링의 레이즈·보험 칸은 안쪽 착지 뒤에 더해진다.</summary>
        private static void ApplyOuterBonus(Seat seat, Slot outer, List<string> parts)
        {
            if (outer == null) return;
            if (outer.Kind == SlotKind.Raise)
            {
                seat.Pot += outer.Value;
                parts.Add($"바깥 레이즈 +{outer.Value} → 판돈 {seat.Pot}");
            }
            else if (outer.Kind == SlotKind.Insurance)
            {
                seat.Insurance += outer.Value;
                parts.Add($"바깥 보험 +{outer.Value} → 보험 {seat.Insurance}");
            }
        }

        private LandingResult FinishLanding(LandingResult result, Seat seat, List<string> parts)
        {
            result.PotAfter = seat.Pot;
            result.Formula = string.Join(" · ", parts);
            Write($"{Name(Active)} 착지: {result.Formula}");
            if (result.EndedTurn)
            {
                EndTurn();
            }

            return result;
        }

        /// <summary>바깥 링 착지 칸을 고른다(균등). 바깥 링이 없으면 -1.</summary>
        public int RollOuterIndex(Side side)
        {
            Seat seat = SeatOf(side);
            return seat.HasOuterRing ? _rng.Next(seat.OuterRing.Count) : -1;
        }

        /// <summary>CASH OUT 했을 때 상대가 받을 피해 미리보기(판돈 − 상대 보험, 허풍 최소 피해, 유물 보너스).</summary>
        public int PreviewCashOutDamage(Side side)
        {
            return PreviewCashOutDamage(side, out _);
        }

        /// <param name="relicBonus">그중 유물(도파민 주사기·이중 장부)이 더한 몫(상대 칩 상한 적용 뒤).</param>
        public int PreviewCashOutDamage(Side side, out int relicBonus)
        {
            Seat seat = SeatOf(side);
            Seat opponent = Opponent(side);
            int insurance = seat.IgnoresInsuranceThisTurn ? 0 : opponent.Insurance;
            int damage = Math.Max(0, seat.Pot - insurance);
            if (seat.Pot > 0)
            {
                foreach (Slot slot in seat.Wheel)
                {
                    if (slot.Kind == SlotKind.MinimumPayout && slot.Trigger == SlotTrigger.CashOut)
                    {
                        damage = Math.Max(damage, slot.Value);
                    }
                }
            }

            int bonus = 0;
            if (side == Side.Player && seat.Pot > 0)
            {
                if (HasRelic(RelicId.DopamineShot)) bonus += Dopamine;
                if (HasRelic(RelicId.DoubleLedger) && seat.Pot >= RelicCatalog.DoubleLedgerPotThreshold) bonus += RelicCatalog.DoubleLedgerBonus;
            }

            int total = Math.Min(opponent.Chips, damage + bonus);
            relicBonus = Math.Max(0, total - Math.Min(opponent.Chips, damage));
            return total;
        }

        /// <summary>
        /// 판돈을 터뜨린다. 상대 칩이 (판돈 − 상대 보험)만큼 줄고, 그만큼과 베팅이 내 칩으로 돌아온다(제로섬).
        /// 딜러의 CASH OUT이 보험에 전부 막히면 토끼의 하우스 룰(전액 보장)이 달성된다.
        /// </summary>
        public CashOutResult CashOut()
        {
            RequirePhase(BattlePhase.Spinning);
            Seat seat = ActiveSeat;
            Seat opponent = Opponent(Active);
            int damage = PreviewCashOutDamage(Active, out int relicBonus);

            CashOutResult result = new CashOutResult
            {
                Side = Active,
                Pot = seat.Pot,
                OpponentInsurance = opponent.Insurance,
                Damage = damage
            };

            opponent.Chips -= damage;
            seat.Chips += seat.Ante + damage;
            result.RelicBonus = relicBonus;
            Write($"{Name(Active)} CASH OUT: 판돈 {seat.Pot} − 보험 {opponent.Insurance} = 피해 {damage}{(result.RelicBonus > 0 ? $" (유물 +{result.RelicBonus})" : "")}");

            if (Active == Side.Dealer
                && Profile.HouseRule == HouseRule.FullCoverage
                && seat.Pot > 0
                && damage == 0)
            {
                result.FullCoverage = true;
                AdvanceHouseRule("전액 보장");
            }

            if (Active == Side.Player)
            {
                switch (Profile.HouseRule)
                {
                    case HouseRule.SmallCashOuts when seat.Pot <= 4:
                        AdvanceHouseRule($"판돈 {seat.Pot}로 작게 CASH OUT");
                        break;
                    case HouseRule.MultipliedCashOut when _playerMultipliedThisTurn:
                        AdvanceHouseRule("배율로 불린 판돈 CASH OUT");
                        break;
                    case HouseRule.FirstStrike when FirstThisRound == Side.Player && damage >= 5:
                        AdvanceHouseRule($"선공 CASH OUT 피해 {damage}");
                        break;
                }
            }

            seat.Pot = 0;
            seat.Ante = 0;
            EndTurn();
            return result;
        }

        /// <summary>딜러의 성향: 판돈이 기준 이상이면 CASH OUT. 튜토리얼 대본 라운드에는 베팅만으로 곧장 CASH OUT.</summary>
        public bool DealerWantsToCashOut()
        {
            return IsScriptedInstantCashOutRound || Dealer.Pot >= Profile.CashOutAt;
        }

        public bool IsScriptedInstantCashOutRound => Profile.ScriptedInstantCashOutRounds.Contains(Round);

        // ───────────── 역전 보정 ─────────────
        // 플레이어가 칩에서 크게 밀리면, SPIN 강도로 정한 착지 구역 안에서 좋은 칸(레이즈·배율·배당)이
        // 조금 더 잘 걸리게 한다. 몰수의 무게는 그대로 둬서 판돈이 털리는 상실감은 유지한다.
        // 보정 중에 좋은 칸이 한 번 걸리면 곧바로 꺼지고, 다음 라운드가 끝날 때까지 다시 켜지지 않는다.

        /// <summary>칩 차이가 이 값 이상일 때부터 보정이 켜진다.</summary>
        public const int ComebackDeficitThreshold = 5;

        /// <summary>좋은 칸 무게에 더해지는 최대치(+60%).</summary>
        public const float ComebackMaxBoost = 0.6f;

        private const float ComebackBoostPerChip = 0.06f;
        private int _comebackSuspendedThroughRound;

        /// <summary>현재 플레이어에게 적용되는 좋은 칸 무게 보너스(0 = 보정 없음).</summary>
        public float ComebackBoost
        {
            get
            {
                if (Round <= _comebackSuspendedThroughRound) return 0f;
                int deficit = Dealer.Chips - (Player.Chips + Player.Pot);
                if (deficit < ComebackDeficitThreshold) return 0f;
                return Math.Min(ComebackMaxBoost, (deficit - ComebackDeficitThreshold + 1) * ComebackBoostPerChip);
            }
        }

        /// <summary>
        /// 착지 칸을 고를 때의 상대 무게(헤드리스 추첨용). 몰수는 이번 턴 SPIN 횟수만큼 무거워지고(HouseCutWeight),
        /// 나머지 칸은 역전 보정 무게(ComebackWeight)를 쓴다.
        /// </summary>
        public float LandingWeight(Side side, Slot slot)
        {
            return slot.Kind == SlotKind.HouseCut ? HouseCutWeight(side) : ComebackWeight(side, slot);
        }

        /// <summary>역전 보정만 반영한 무게. 연출 쪽 회전은 몰수 폭을 칸 크기로 그리고, 이 값만 착지 구역 안에서 반영한다.</summary>
        public float ComebackWeight(Side side, Slot slot)
        {
            if (side != Side.Player) return 1f;
            float boost = ComebackBoost;
            if (boost <= 0f) return 1f;
            if (IsGoodSlot(slot)) return 1f + boost;
            if (slot.Kind == SlotKind.HouseCut) return 1f;
            return Math.Max(0.4f, 1f - boost * 0.5f);
        }

        // ───────────── 몰수 상승 (ADR 0012) ─────────────
        // 한 턴 안에서 N번째 SPIN의 (안쪽) 몰수 확률은 N × 5%, 최대 35%. 턴이 끝나면(CASH OUT·증발) 처음으로.
        // 지금 판돈과 몇 번째 SPIN인지가 "한 번 더?"의 판단 재료가 된다. 딜러도 같은 규칙. 바깥 링 몰수는 그대로.

        /// <summary>SPIN 한 번마다 오르는 몰수 확률.</summary>
        public const float HouseCutChancePerSpin = 0.05f;

        /// <summary>몰수 확률의 상한.</summary>
        public const float HouseCutChanceMax = 0.35f;

        /// <summary>이번 턴에 이미 돈 SPIN 횟수(베팅을 걸면 0).</summary>
        public int SpinsThisTurn { get; private set; }

        /// <summary>그쪽의 다음 SPIN이 안쪽 몰수에 걸릴 확률(0~1). 몰수가 여러 칸이면 그만큼 곱한다.</summary>
        public float HouseCutChance(Side side)
        {
            IReadOnlyList<Slot> wheel = SeatOf(side).Wheel;
            int cuts = CountHouseCuts(wheel);
            if (cuts == 0 || cuts == wheel.Count) return cuts == 0 ? 0f : 1f;
            int nextSpin = side == Active && Phase == BattlePhase.Spinning ? SpinsThisTurn + 1 : 1;
            float perCut = Math.Min(HouseCutChanceMax, HouseCutChancePerSpin * nextSpin);
            return Math.Min(0.9f, perCut * cuts);
        }

        /// <summary>몰수 한 칸의 무게(다른 칸 = 1). 이 무게로 그린 칸 폭의 비율이 곧 HouseCutChance다.</summary>
        public float HouseCutWeight(Side side)
        {
            IReadOnlyList<Slot> wheel = SeatOf(side).Wheel;
            int cuts = CountHouseCuts(wheel);
            int others = wheel.Count - cuts;
            if (cuts == 0 || others == 0) return 1f;
            float chance = HouseCutChance(side);
            return chance / (1f - chance) * others / cuts;
        }

        private static int CountHouseCuts(IReadOnlyList<Slot> wheel)
        {
            int cuts = 0;
            foreach (Slot slot in wheel)
            {
                if (slot.Kind == SlotKind.HouseCut) cuts++;
            }

            return cuts;
        }

        public static bool IsGoodSlot(Slot slot)
        {
            return slot.FiresOnLand
                   && (slot.Kind == SlotKind.Raise || slot.Kind == SlotKind.Multiplier || slot.Kind == SlotKind.Dividend);
        }

        public HijackError CanHijack(int dealerIndex, int playerIndex)
        {
            if (Phase == BattlePhase.Ended) return HijackError.BattleEnded;
            if (HijackUsedThisRound) return HijackError.AlreadyUsedThisRound;
            if (HijackChances <= 0) return HijackError.NoChance;
            if (dealerIndex < 0 || dealerIndex >= Dealer.Wheel.Count
                || playerIndex < 0 || playerIndex >= Player.Wheel.Count)
            {
                return HijackError.InvalidIndex;
            }

            if (Dealer.Wheel[dealerIndex].Kind == SlotKind.Sealed) return HijackError.SourceSealed;
            if (Dealer.Wheel[dealerIndex].Kind == SlotKind.HouseCut) return HijackError.HouseCutProtected;
            if (Player.Wheel[playerIndex].Kind == SlotKind.HouseCut) return HijackError.HouseCutProtected;
            return HijackError.None;
        }

        /// <summary>
        /// 딜러 칸 하나로 내 칸 하나를 영구히 덮어쓴다. 딜러 칸은 봉인되고, 몰수를 뺀 모든 칸이 봉인되면 완전 강탈로 승리한다.
        /// 몰수는 양쪽 모두 빼앗을 수도, 덮어쓸 수도 없다.
        /// </summary>
        public HijackError Hijack(int dealerIndex, int playerIndex)
        {
            HijackError error = CanHijack(dealerIndex, playerIndex);
            if (error != HijackError.None)
            {
                return error;
            }

            Slot stolen = Dealer.Wheel[dealerIndex];
            Slot replaced = Player.Wheel[playerIndex];
            _stolenCount++;
            Player.ReplaceSlot(playerIndex, stolen.AsStolen($"hijacked_{_stolenCount}_{stolen.Id}"));
            Dealer.ReplaceSlot(dealerIndex, Slot.Sealed($"sealed_{_stolenCount}_{stolen.Id}"));
            HijackChances--;
            HijackUsedThisRound = true;
            Write($"HIJACK: {Profile.Name}의 {stolen.Label}{(stolen.IsJackpot ? "(JACKPOT)" : "")} → 내 {playerIndex + 1}번 칸({replaced.Label})");
            if (HasRelic(RelicId.MarkedCard))
            {
                Player.Chips += RelicCatalog.MarkedCardChips;
                Write($"유물 「표시된 카드」: HIJACK → 칩 +{RelicCatalog.MarkedCardChips}");
            }

            bool allSealed = true;
            foreach (Slot slot in Dealer.Wheel)
            {
                if (slot.Kind != SlotKind.Sealed && slot.Kind != SlotKind.HouseCut)
                {
                    allSealed = false;
                    break;
                }
            }

            if (allSealed)
            {
                Finish(BattleOutcome.PlayerWinsByCleanSweep);
            }

            return HijackError.None;
        }

        /// <summary>
        /// 역탈취: 플레이어가 몰수에 걸린 직후 딜러가 플레이어 칸 하나를 압수한다.
        /// 플레이어가 빼앗아 온 칸을 먼저 되찾고, 없으면 값이 가장 큰 칸을 가져간다. 몰수는 대상이 아니다.
        /// 압수된 칸은 이 전투 동안 봉인되며, 플레이어가 이기면 원래대로 돌아온다.
        /// </summary>
        /// <returns>압수한 칸의 인덱스. 대상이 없으면 -1.</returns>
        public int ResolveCounterHijack()
        {
            if (!CounterHijackPending)
            {
                return -1;
            }

            CounterHijackPending = false;
            int target = -1;
            for (int i = 0; i < Player.Wheel.Count; i++)
            {
                Slot candidate = Player.Wheel[i];
                if (candidate.Kind == SlotKind.HouseCut || candidate.Kind == SlotKind.Sealed) continue;
                if (target < 0) { target = i; continue; }

                Slot best = Player.Wheel[target];
                bool better = candidate.IsStolen != best.IsStolen
                    ? candidate.IsStolen
                    : candidate.Value > best.Value;
                if (better) target = i;
            }

            if (target < 0)
            {
                return -1;
            }

            Slot seized = Player.Wheel[target];
            _seizedSlots.Add(new KeyValuePair<int, Slot>(target, seized));
            Player.ReplaceSlot(target, Slot.Sealed($"seized_{_seizedSlots.Count}_{seized.Id}"));
            Write($"역탈취: {Profile.Name}이(가) 내 {target + 1}번 칸({seized.Label})을 압수");
            return target;
        }

        /// <summary>시뮬레이션·딜러 헤드리스 진행용. 연출 쪽은 자기 회전 RNG로 착지 칸을 정한다.</summary>
        public int RollLandingIndex(Side side)
        {
            IReadOnlyList<Slot> wheel = SeatOf(side).Wheel;
            float total = 0f;
            foreach (Slot slot in wheel) total += LandingWeight(side, slot);

            double roll = _rng.NextDouble() * total;
            for (int i = 0; i < wheel.Count; i++)
            {
                roll -= LandingWeight(side, wheel[i]);
                if (roll < 0) return i;
            }

            return wheel.Count - 1;
        }

        /// <summary>
        /// 한 칸에서 시작해 양옆으로 이어진, [착지]로 발동하는 같은 종류 칸의 인덱스(시계 방향 순서).
        /// </summary>
        /// <summary>테이블 규칙 「연쇄」가 켜져 있는지(ADR 0013: 기본은 꺼짐).</summary>
        public bool ChainsEnabled => Profile.TableChains;

        /// <summary>이 칸에 착지하면 함께 발동하는 칸들. 기본은 그 칸 하나, 「연쇄」 테이블이면 이어진 같은 종류 칸 묶음.</summary>
        public List<int> LandingGroup(Side side, int index)
        {
            return ChainsEnabled ? FindChainGroup(SeatOf(side).Wheel, index) : new List<int> { index };
        }

        /// <summary>바로 옆(양옆) 칸 중 같은 종류의 [착지] 칸이 있는지. 유물 「끈적한 칸막이」가 쓴다.</summary>
        public static bool HasSameKindNeighbor(IReadOnlyList<Slot> wheel, int index)
        {
            Slot origin = wheel[index];
            if (!origin.FiresOnLand || origin.Kind == SlotKind.HouseCut || wheel.Count < 2) return false;
            Slot left = wheel[(index - 1 + wheel.Count) % wheel.Count];
            Slot right = wheel[(index + 1) % wheel.Count];
            return SameChain(origin, left) || SameChain(origin, right);
        }

        /// <summary>테이블 규칙 「연쇄」용: 한 칸에서 양옆으로 이어진 같은 종류 [착지] 칸 묶음.</summary>
        public static List<int> FindChainGroup(IReadOnlyList<Slot> wheel, int index)
        {
            int count = wheel.Count;
            Slot origin = wheel[index];
            List<int> group = new List<int> { index };
            if (!origin.FiresOnLand || origin.Kind == SlotKind.HouseCut)
            {
                return group;
            }

            int start = index;
            for (int step = 1; step < count; step++)
            {
                int previous = (index - step + count) % count;
                if (!SameChain(origin, wheel[previous])) break;
                start = previous;
            }

            group.Clear();
            for (int step = 0; step < count; step++)
            {
                int cursor = (start + step) % count;
                if (step > 0 && !SameChain(origin, wheel[cursor])) break;
                group.Add(cursor);
            }

            return group;
        }

        private static bool SameChain(Slot origin, Slot other)
        {
            return other.FiresOnLand && other.Kind == origin.Kind;
        }

        private void EndTurn()
        {
            Seat seat = ActiveSeat;
            seat.Pot = 0;
            seat.Ante = 0;
            SpinsThisTurn = 0;
            _turnsThisRound++;

            if (CheckBankrupt())
            {
                return;
            }

            if (_turnsThisRound >= 2)
            {
                Phase = BattlePhase.RoundOver;
                return;
            }

            Active = Active == Side.Player ? Side.Dealer : Side.Player;
            Phase = BattlePhase.AwaitingAnte;
        }

        private bool CheckBankrupt()
        {
            if (Dealer.Chips <= 0)
            {
                Finish(BattleOutcome.PlayerWinsByBankrupt);
                return true;
            }

            if (Player.Chips <= 0)
            {
                Finish(BattleOutcome.DealerWins);
                return true;
            }

            return false;
        }

        private void Finish(BattleOutcome outcome)
        {
            Outcome = outcome;
            Phase = BattlePhase.Ended;
            CounterHijackPending = false;
            if (outcome != BattleOutcome.DealerWins)
            {
                for (int i = _seizedSlots.Count - 1; i >= 0; i--)
                {
                    Player.ReplaceSlot(_seizedSlots[i].Key, _seizedSlots[i].Value);
                    Write($"압수된 {_seizedSlots[i].Value.Label} 반환");
                }

                _returnedSeizures += _seizedSlots.Count;
                _seizedSlots.Clear();
            }

            Write(outcome switch
            {
                BattleOutcome.PlayerWinsByBankrupt => $"{Profile.Name} 파산 · 승리",
                BattleOutcome.PlayerWinsByCleanSweep => "완전 강탈 · 승리",
                _ => "파산 · 계약 되감기"
            });
        }

        private void AdvanceHouseRule(string reason)
        {
            HouseRuleProgress++;
            if (HouseRuleProgress < HouseRuleGoal)
            {
                Write($"하우스 룰 진행: {reason} ({HouseRuleProgress}/{HouseRuleGoal})");
                return;
            }

            HouseRuleProgress = 0;
            HijackChances++;
            HouseRulesAchieved++;
            Write($"하우스 룰 달성: {reason} → HIJACK 기회 +1");
        }

        private static bool HasActiveSlot(Seat seat, SlotKind kind)
        {
            foreach (Slot slot in seat.Wheel)
            {
                if (slot.Kind == kind) return true;
            }

            return false;
        }

        private void ApplyRoundStartSlots(Seat seat)
        {
            foreach (Slot slot in seat.Wheel)
            {
                if (slot.Trigger == SlotTrigger.RoundStart && slot.Kind == SlotKind.Dividend)
                {
                    seat.Chips += slot.Value;
                    Write($"{Name(seat.Side)} [라운드 시작] {slot.Label}: 칩 +{slot.Value}");
                    _roundStartEffects.Add($"{Name(seat.Side)}의 {slot.Label}: 칩 +{slot.Value}");
                }
            }
        }

        private void RequirePhase(BattlePhase expected)
        {
            if (Phase != expected)
            {
                throw new InvalidOperationException($"현재 단계는 {Phase}이며 {expected}에서만 가능합니다.");
            }
        }

        private string Name(Side side) => side == Side.Player ? "플레이어" : Profile.Name;

        private void Write(string line) => _log.Add(line);

        private static int SumValues(IReadOnlyList<Slot> wheel, List<int> group)
        {
            int sum = 0;
            foreach (int i in group) sum += wheel[i].Value;
            return sum;
        }

        private static string JoinValues(IReadOnlyList<Slot> wheel, List<int> group, string separator)
        {
            string[] parts = new string[group.Count];
            for (int i = 0; i < group.Count; i++) parts[i] = wheel[group[i]].Value.ToString();
            return string.Join(separator, parts);
        }
    }
}
