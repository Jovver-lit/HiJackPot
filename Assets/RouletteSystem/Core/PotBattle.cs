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

        /// <summary>현재 차례인 쪽이 앤티를 걸기를 기다린다.</summary>
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

        /// <summary>하우스 몫에 걸렸지만 보호막이 막았다.</summary>
        public bool CutShielded;
    }

    public sealed class CashOutResult
    {
        public Side Side;
        public int Pot;
        public int OpponentInsurance;
        public int Damage;
        public bool FullCoverage;
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
    /// 한 라운드: 선공 코인플립 → 각자 한 턴(앤티 → SPIN 반복 → CASH OUT 또는 하우스 몫).
    /// </summary>
    public sealed class PotBattle
    {
        private readonly Random _rng;
        private readonly List<string> _log = new List<string>();
        private readonly List<KeyValuePair<int, Slot>> _seizedSlots = new List<KeyValuePair<int, Slot>>();
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

        /// <summary>플레이어가 방금 하우스 몫에 걸려 딜러가 역탈취할 수 있는 상태.</summary>
        public bool CounterHijackPending { get; private set; }

        public IReadOnlyList<string> Log => _log;

        /// <param name="playerOuterRing">플레이어 자신의 바깥 링(해금된 경우). 없으면 딜러의 테이블 바깥 링을 빌려 쓴다.</param>
        public PotBattle(IEnumerable<Slot> playerWheel, int playerChips, DealerProfile dealer, int seed, IReadOnlyList<Slot> playerOuterRing = null)
        {
            Profile = dealer;
            IReadOnlyList<Slot> tableRing = dealer.TableOuterRing;
            Player = new Seat(Side.Player, playerChips, playerWheel,
                playerOuterRing != null && playerOuterRing.Count > 0 ? playerOuterRing : tableRing);
            Dealer = new Seat(Side.Dealer, dealer.StartingChips, dealer.Wheel, tableRing);
            _rng = new Random(seed);
        }

        public Seat SeatOf(Side side) => side == Side.Player ? Player : Dealer;
        public Seat Opponent(Side side) => side == Side.Player ? Dealer : Player;
        public Seat ActiveSeat => SeatOf(Active);

        /// <summary>
        /// 라운드를 시작한다: [라운드 시작] 칸 발동 → 선공 코인플립. 선공 쪽이 앤티를 걸 차례가 된다.
        /// </summary>
        public Side StartRound()
        {
            RequirePhase(BattlePhase.RoundOver);
            Round++;
            _turnsThisRound = 0;
            HijackUsedThisRound = false;

            ApplyRoundStartSlots(Player);
            ApplyRoundStartSlots(Dealer);

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
                return Active;
            }

            FirstThisRound = _rng.Next(2) == 0 ? Side.Player : Side.Dealer;
            Active = FirstThisRound;
            Phase = BattlePhase.AwaitingAnte;
            Write($"라운드 {Round}: 코인플립 → {Name(Active)} 선공");
            return Active;
        }

        /// <summary>앤티 가능 범위의 최대치. 테이블 한도와 남은 칩 중 작은 쪽.</summary>
        public int MaxAnte(Side side)
        {
            return Math.Max(0, Math.Min(Profile.TableLimit, SeatOf(side).Chips));
        }

        /// <summary>
        /// 현재 차례인 쪽이 앤티를 건다. 1 ~ MaxAnte로 맞춰지며, 이전 턴의 보험은 여기서 사라진다.
        /// </summary>
        public int PlaceAnte(int amount)
        {
            RequirePhase(BattlePhase.AwaitingAnte);
            Seat seat = ActiveSeat;
            int ante = Math.Max(1, Math.Min(amount, MaxAnte(Active)));
            seat.Insurance = Active == Side.Dealer ? Profile.BaseInsurance : 0;
            seat.CutShields = 0;
            if (Active == Side.Player) _playerMultipliedThisTurn = false;
            seat.Chips -= ante;
            seat.Ante = ante;
            seat.Pot = ante;
            Phase = BattlePhase.Spinning;
            Write($"{Name(Active)} 앤티 {ante}");
            return ante;
        }

        /// <summary>
        /// 현재 차례인 쪽의 룰렛이 index 칸에 착지했다. 이어진 같은 종류 칸이 한 묶음(연쇄)으로 발동한다.
        /// 바깥 링이 있으면 outerIndex 칸도 함께 멈춘다: 보호막은 착지보다 먼저, 레이즈·보험은 착지 뒤에 더하고,
        /// 배율 링은 안쪽 효과를 한 번 더 발동한다. 안쪽과 바깥이 같은 종류면 잭팟 라인으로 안쪽 효과가 또 한 번 발동한다.
        /// </summary>
        public LandingResult Land(int index, int outerIndex = -1)
        {
            RequirePhase(BattlePhase.Spinning);
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
                parts.Add(slot.Kind == SlotKind.Sealed
                    ? "봉인 칸 · 효과 없음"
                    : $"{slot.Label} · [{(slot.Trigger == SlotTrigger.CashOut ? "CASH OUT" : "라운드 시작")}] 칸이라 착지 효과 없음");
                ApplyOuterBonus(seat, outer, parts);
                return FinishLanding(result, seat, parts);
            }

            List<int> group = FindChainGroup(seat.Wheel, index);
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
                    parts.Add("하우스 몫 → 보호막이 막았다 (판돈 유지)");
                    ApplyOuterBonus(seat, outer, parts);
                    return FinishLanding(result, seat, parts);
                }

                result.Amount = seat.Pot;
                parts.Add($"하우스 몫 → 판돈 {seat.Pot} 증발");
                seat.Pot = 0;
                seat.Ante = 0;
                result.EndedTurn = true;
                if (Active == Side.Player && Profile.CounterHijacks)
                {
                    CounterHijackPending = true;
                }

                return FinishLanding(result, seat, parts);
            }

            parts.Add(ApplyInnerEffect(seat, slot, group, result));
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
            return FinishLanding(result, seat, parts);
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
                    return $"{slot.Label} → 이번 턴 하우스 몫 무효 {seat.CutShields}회";
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

        /// <summary>CASH OUT 했을 때 상대가 받을 피해 미리보기(판돈 − 상대 보험).</summary>
        public int PreviewCashOutDamage(Side side)
        {
            Seat seat = SeatOf(side);
            Seat opponent = Opponent(side);
            int damage = Math.Max(0, seat.Pot - opponent.Insurance);
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

            return Math.Min(opponent.Chips, damage);
        }

        /// <summary>
        /// 판돈을 터뜨린다. 상대 칩이 (판돈 − 상대 보험)만큼 줄고, 그만큼과 앤티가 내 칩으로 돌아온다(제로섬).
        /// 딜러의 CASH OUT이 보험에 전부 막히면 토끼의 하우스 룰(전액 보장)이 달성된다.
        /// </summary>
        public CashOutResult CashOut()
        {
            RequirePhase(BattlePhase.Spinning);
            Seat seat = ActiveSeat;
            Seat opponent = Opponent(Active);
            int damage = PreviewCashOutDamage(Active);

            CashOutResult result = new CashOutResult
            {
                Side = Active,
                Pot = seat.Pot,
                OpponentInsurance = opponent.Insurance,
                Damage = damage
            };

            opponent.Chips -= damage;
            seat.Chips += seat.Ante + damage;
            Write($"{Name(Active)} CASH OUT: 판돈 {seat.Pot} − 보험 {opponent.Insurance} = 피해 {damage}");

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

        /// <summary>딜러의 성향: 판돈이 기준 이상이면 CASH OUT. 튜토리얼 대본 라운드에는 앤티만으로 곧장 CASH OUT.</summary>
        public bool DealerWantsToCashOut()
        {
            return IsScriptedInstantCashOutRound || Dealer.Pot >= Profile.CashOutAt;
        }

        public bool IsScriptedInstantCashOutRound => Profile.ScriptedInstantCashOutRounds.Contains(Round);

        // ───────────── 역전 보정 ─────────────
        // 플레이어가 칩에서 크게 밀리면, SPIN 강도로 정한 착지 구역 안에서 좋은 칸(레이즈·배율·배당)이
        // 조금 더 잘 걸리게 한다. 하우스 몫의 무게는 그대로 둬서 판돈이 털리는 상실감은 유지한다.
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

        /// <summary>착지 칸을 고를 때의 상대 무게. 1이 기본이며, 연출 쪽 회전도 이 값을 쓴다.</summary>
        public float LandingWeight(Side side, Slot slot)
        {
            if (side != Side.Player) return 1f;
            float boost = ComebackBoost;
            if (boost <= 0f) return 1f;
            if (IsGoodSlot(slot)) return 1f + boost;
            if (slot.Kind == SlotKind.HouseCut) return 1f;
            return Math.Max(0.4f, 1f - boost * 0.5f);
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
        /// 딜러 칸 하나로 내 칸 하나를 영구히 덮어쓴다. 딜러 칸은 봉인되고, 하우스 몫을 뺀 모든 칸이 봉인되면 완전 강탈로 승리한다.
        /// 하우스 몫은 양쪽 모두 빼앗을 수도, 덮어쓸 수도 없다.
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
        /// 역탈취: 플레이어가 하우스 몫에 걸린 직후 딜러가 플레이어 칸 하나를 압수한다.
        /// 플레이어가 빼앗아 온 칸을 먼저 되찾고, 없으면 값이 가장 큰 칸을 가져간다. 하우스 몫은 대상이 아니다.
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
