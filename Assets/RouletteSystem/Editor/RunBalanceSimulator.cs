using System;
using System.Collections.Generic;
using System.Text;
using RouletteLike.Battle;
using UnityEditor;
using UnityEngine;

namespace RouletteLike.Roulette.EditorTools
{
    /// <summary>
    /// 런 전체 헤드리스 밸런스 시뮬레이터(에디터 전용). 코어 규칙만으로 토끼 → 문 선택 → 보스까지 수천 런을 돌려
    /// 탈출률·층별 탈락·전투 길이·HIJACK·하우스 몫·NUDGE·유물별 탈출률을 콘솔에 보고한다.
    /// 플레이어 정책은 단순 휴리스틱(앤티·목표 판돈·NUDGE·HIJACK 선택)이며, 실제 사람의 판단을 대신하지 않는다.
    /// </summary>
    public static class RunBalanceSimulator
    {
        /// <summary>한 정책: 원하는 앤티와 CASH OUT할 판돈.</summary>
        public readonly struct Policy
        {
            public readonly int Ante;
            public readonly int TargetPot;

            public Policy(int ante, int targetPot)
            {
                Ante = ante;
                TargetPot = targetPot;
            }

            public override string ToString() => $"앤티 {(Ante >= 99 ? "최대" : Ante.ToString())} · 판돈 {TargetPot}";
        }

        public static readonly Policy[] DefaultPolicies =
        {
            new Policy(2, 8), new Policy(3, 10), new Policy(3, 14), new Policy(99, 12)
        };

        private const int MaxRoundsPerBattle = 40;

        [MenuItem("Tools/HIJACKPOT/Simulate Runs (Balance Report)")]
        private static void RunFromMenu()
        {
            Debug.Log(Simulate(1000));
        }

        public static string Simulate(int runsPerPolicy, Policy[] policies = null)
        {
            return Simulate(runsPerPolicy, policies, null, true);
        }

        /// <param name="exchange">딴 칩 정산 규칙(null이면 기본값).</param>
        /// <param name="buyChipsWithCash">문 앞에서 현금을 전부 칩으로 바꾸는지(환전 창구를 최대한 쓰는 플레이어).</param>
        public static string Simulate(int runsPerPolicy, Policy[] policies, ChipExchangeRules exchange, bool buyChipsWithCash)
        {
            StringBuilder report = new StringBuilder();
            ChipExchangeRules rules = exchange ?? ChipExchangeRules.Default;
            report.AppendLine($"[정산] 딴 칩 중 칩으로 {rules.KeepShare:P0}, 나머지 현금 · 환전 창구 현금 {rules.CashPerChip} = 칩 1 · 환전 창구 사용 {(buyChipsWithCash ? "함" : "안 함")}");
            foreach (Policy policy in policies ?? DefaultPolicies)
            {
                report.Append(SimulatePolicy(policy, runsPerPolicy, rules, buyChipsWithCash));
            }

            return report.ToString();
        }

        private sealed class DealerStats
        {
            public int Battles, Wins, Hijacks, Rounds, HouseCuts, BigEvaporations, Nudges, LongBattles;
            public long EntryChips;
            public long EntryCash;
        }

        private static string SimulatePolicy(Policy policy, int runs, ChipExchangeRules rules, bool buyChipsWithCash)
        {
            int escaped = 0, stalls = 0;
            int[] diedAt = new int[11];
            long shopCash = 0, shopVisits = 0;
            int[] roundBuckets = new int[6]; // 0-2, 3-5, 6-8, 9-11, 12-14, 15+
            Dictionary<string, DealerStats> dealers = new Dictionary<string, DealerStats>();
            Dictionary<RelicId, int[]> relicEscapes = new Dictionary<RelicId, int[]>();

            for (int r = 0; r < runs; r++)
            {
                Run run = new Run(1000 + r, BattlePresets.PlayerStartingChips, BattlePresets.CreateStarterWheel(),
                    BattlePresets.CreateRabbitDealer, BattlePresets.CreateDealerPool(), BattlePresets.CreateStageBoss,
                    exchange: rules);
                System.Random doorRng = new System.Random(r * 7 + 3);
                bool abandoned = false;
                while (run.Outcome == RunOutcome.InProgress)
                {
                    if (run.PendingJackpot != null) run.PlacePendingJackpot(WeakestSlot(run.Wheel));
                    if (run.CurrentFloorKind == FloorKind.Shop)
                    {
                        shopVisits++;
                        shopCash += run.Cash;
                        ShopPolicy(run, buyChipsWithCash);
                        run.LeaveShop();
                        continue;
                    }

                    if (buyChipsWithCash) run.BuyChips(int.MaxValue);
                    int entryChips = run.Chips;
                    PotBattle battle = run.EnterDoor(doorRng.Next(run.Doors.Count));
                    if (!dealers.TryGetValue(battle.Profile.Name, out DealerStats stats))
                    {
                        stats = new DealerStats();
                        dealers[battle.Profile.Name] = stats;
                    }

                    stats.Battles++;
                    stats.EntryChips += entryChips;
                    stats.EntryCash += run.Cash;
                    PlayBattle(battle, policy, stats);
                    if (battle.Phase != BattlePhase.Ended)
                    {
                        stalls++;
                        abandoned = true;
                        break;
                    }

                    stats.Rounds += battle.Round;
                    if (battle.Round >= 15) stats.LongBattles++;
                    roundBuckets[Math.Min(5, battle.Round / 3)]++;
                    if (battle.Outcome != BattleOutcome.DealerWins) stats.Wins++;
                    int floor = run.FloorIndex;
                    run.CompleteBattle();
                    if (run.Outcome == RunOutcome.Bankrupt) diedAt[floor]++;
                }

                if (abandoned) continue;
                bool escapedRun = run.Outcome == RunOutcome.Escaped;
                if (escapedRun) escaped++;
                foreach (RelicId relic in run.Relics)
                {
                    if (!relicEscapes.TryGetValue(relic, out int[] counts)) relicEscapes[relic] = counts = new int[2];
                    counts[0]++;
                    if (escapedRun) counts[1]++;
                }
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine($"== {policy}: 탈출 {100.0 * escaped / runs:0.0}% · 층별 탈락(1~10층) {string.Join(",", Array.ConvertAll(diedAt, x => x.ToString()), 0, 10)} · 상점 도착 {100.0 * shopVisits / runs:0}%(현금 {(shopVisits > 0 ? shopCash / (double)shopVisits : 0):0}) · 교착({MaxRoundsPerBattle}라운드+) {stalls}");
            text.AppendLine($"   전투 라운드 분포 0-2/3-5/6-8/9-11/12-14/15+: {string.Join(" / ", roundBuckets)}");
            foreach (KeyValuePair<string, DealerStats> pair in dealers)
            {
                DealerStats s = pair.Value;
                double n = Math.Max(1, s.Battles);
                text.AppendLine($"   {pair.Key}: 승 {100.0 * s.Wins / n:0}% · 입장 칩 {s.EntryChips / n:0.0} · 현금 {s.EntryCash / n:0.0} · 라운드 {s.Rounds / n:0.0} (15+ {s.LongBattles}) · HIJACK {s.Hijacks / n:0.00} · 하우스 몫 {s.HouseCuts / n:0.0} · 판돈6+ 증발 {s.BigEvaporations / n:0.00} · NUDGE {s.Nudges / n:0.00} (n={s.Battles})");
            }

            List<string> relicLines = new List<string>();
            foreach (KeyValuePair<RelicId, int[]> pair in relicEscapes)
            {
                relicLines.Add($"{RelicCatalog.Get(pair.Key).Name} {100.0 * pair.Value[1] / pair.Value[0]:0}%");
            }

            text.AppendLine($"   유물 보유 시 탈출률: {string.Join(", ", relicLines)}");
            return text.ToString();
        }

        private static void PlayBattle(PotBattle battle, Policy policy, DealerStats stats)
        {
            while (battle.Phase != BattlePhase.Ended && battle.Round <= MaxRoundsPerBattle)
            {
                if (battle.HijackChances > 0 && !battle.HijackUsedThisRound)
                {
                    int source = BestDealerSlot(battle.Dealer.Wheel);
                    int destination = WeakestSlot(battle.Player.Wheel);
                    if (source >= 0 && destination >= 0 && battle.Hijack(source, destination) == HijackError.None) stats.Hijacks++;
                    if (battle.Phase == BattlePhase.Ended) return;
                }

                if (battle.Phase == BattlePhase.RoundOver)
                {
                    battle.StartRound();
                    continue;
                }

                if (battle.Active == Side.Dealer)
                {
                    PlayDealerTurn(battle);
                    continue;
                }

                PlayPlayerTurn(battle, policy, stats);
            }
        }

        private static void PlayDealerTurn(PotBattle battle)
        {
            battle.PlaceAnte(battle.Profile.DealerAnte);
            for (int spins = 0; battle.Phase == BattlePhase.Spinning && battle.Active == Side.Dealer; spins++)
            {
                if (battle.DealerWantsToCashOut() || spins >= 6)
                {
                    battle.CashOut();
                    return;
                }

                battle.Land(battle.RollLandingIndex(Side.Dealer), battle.RollOuterIndex(Side.Dealer));
            }
        }

        private static void PlayPlayerTurn(PotBattle battle, Policy policy, DealerStats stats)
        {
            battle.PlaceAnte(Math.Min(policy.Ante, battle.MaxAnte(Side.Player)));
            for (int spins = 0; battle.Phase == BattlePhase.Spinning && battle.Active == Side.Player; spins++)
            {
                if (battle.PreviewCashOutDamage(Side.Player) >= battle.Dealer.Chips || battle.Player.Pot >= policy.TargetPot || spins >= 8)
                {
                    battle.CashOut();
                    return;
                }

                int index = battle.RollLandingIndex(Side.Player);
                IReadOnlyList<Slot> wheel = battle.Player.Wheel;
                if (wheel[index].Kind == SlotKind.HouseCut && battle.Player.CutShields == 0 && battle.NudgesRemaining > 0 && battle.Player.Pot >= 4)
                {
                    int pick = -1;
                    foreach (int neighbor in new[] { (index - 1 + wheel.Count) % wheel.Count, (index + 1) % wheel.Count })
                    {
                        if (wheel[neighbor].Kind == SlotKind.HouseCut) continue;
                        if (pick < 0 || PotBattle.IsGoodSlot(wheel[neighbor])) pick = neighbor;
                    }

                    if (pick >= 0 && battle.UseNudge())
                    {
                        index = pick;
                        stats.Nudges++;
                    }
                }

                LandingResult landing = battle.Land(index, battle.RollOuterIndex(Side.Player));
                if (landing.HouseCutHit)
                {
                    stats.HouseCuts++;
                    if (landing.Amount >= 6) stats.BigEvaporations++;
                }

                if (battle.CounterHijackPending) battle.ResolveCounterHijack();
            }
        }

        /// <summary>상점 휴리스틱: 칩을 살 수 있는 만큼 사고, 남은 현금으로 레이즈·배율 칸을 강화한다(슬롯머신은 하지 않음).</summary>
        private static void ShopPolicy(Run run, bool buyChips)
        {
            if (buyChips) run.BuyChips(int.MaxValue);
            for (int guard = 0; guard < 20; guard++)
            {
                int best = -1;
                for (int i = 0; i < run.Wheel.Count; i++)
                {
                    Slot slot = run.Wheel[i];
                    if (slot.Kind != SlotKind.Raise && slot.Kind != SlotKind.Multiplier) continue;
                    int price = run.UpgradePrice(i);
                    if (price < 0 || price > run.Cash) continue;
                    if (best < 0 || slot.Value < run.Wheel[best].Value) best = i;
                }

                if (best < 0 || run.UpgradeSlot(best) != ShopError.None) break;
            }
        }

        /// <summary>빼앗을 칸: JACKPOT 우선, 없으면 값이 큰 칸(봉인·하우스 몫 제외).</summary>
        private static int BestDealerSlot(IReadOnlyList<Slot> wheel)
        {
            int best = -1;
            for (int i = 0; i < wheel.Count; i++)
            {
                Slot slot = wheel[i];
                if (slot.Kind == SlotKind.Sealed || slot.Kind == SlotKind.HouseCut) continue;
                if (best < 0 || slot.IsJackpot && !wheel[best].IsJackpot || slot.IsJackpot == wheel[best].IsJackpot && slot.Value > wheel[best].Value) best = i;
            }

            return best;
        }

        /// <summary>덮어쓸 내 칸: 봉인 칸 우선, 그다음 빼앗아 오지 않은 값이 작은 칸(하우스 몫 제외).</summary>
        private static int WeakestSlot(IReadOnlyList<Slot> wheel)
        {
            int weakest = -1;
            for (int i = 0; i < wheel.Count; i++)
            {
                Slot slot = wheel[i];
                if (slot.Kind == SlotKind.HouseCut) continue;
                if (weakest < 0) { weakest = i; continue; }
                Slot current = wheel[weakest];
                bool sealedSlot = slot.Kind == SlotKind.Sealed, sealedCurrent = current.Kind == SlotKind.Sealed;
                if (sealedSlot != sealedCurrent) { if (sealedSlot) weakest = i; continue; }
                if (slot.IsStolen != current.IsStolen) { if (!slot.IsStolen) weakest = i; continue; }
                if (slot.Value < current.Value) weakest = i;
            }

            return weakest;
        }
    }
}
