using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>유물 12개의 규칙 효과, NUDGE 횟수, 문 카드 유물 상금(런) EditMode 테스트.</summary>
    public sealed class RelicTests
    {
        // 시작 룰렛: 0 레이즈+2, 1 레이즈+2, 2 배율×2, 3 보험+3, 4 레이즈+3, 5 하우스 몫, 6 배당+2, 7 보험+2
        private const int RaisePairA = 0, Multiplier = 2, Raise3 = 4, HouseCut = 5;

        private static PotBattle FoxBattle(params RelicId[] relics)
        {
            return new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateFoxDealer(), 4, null, relics);
        }

        private static void AdvanceToPlayer(PotBattle battle)
        {
            for (int guard = 0; guard < 10; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Active == Side.Player && battle.Phase == BattlePhase.AwaitingAnte) return;
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.Fail("플레이어 차례에 도달하지 못했습니다.");
        }

        [Test]
        public void Catalog_HasTwelveRelics_CoveringAllSixKeywords()
        {
            Assert.AreEqual(12, RelicCatalog.Relics.Count);
            HashSet<string> keywords = new HashSet<string>();
            foreach (Relic relic in RelicCatalog.Relics) keywords.Add(relic.Keyword);
            foreach (string keyword in new[] { "[착지]", "[인접]", "[CASH OUT]", "[하우스 몫]", "[라운드 시작]", "[HIJACK]" })
            {
                Assert.IsTrue(keywords.Contains(keyword), keyword);
            }
        }

        [Test]
        public void Nudge_OnePerBattle_StringChipAddsOne()
        {
            Assert.AreEqual(1, FoxBattle().NudgesRemaining);
            Assert.AreEqual(2, FoxBattle(RelicId.StringChip).NudgesRemaining);
        }

        [Test]
        public void Nudge_OnlyDuringPlayerSpin_AndSpendsACharge()
        {
            PotBattle battle = FoxBattle();
            AdvanceToPlayer(battle);
            Assert.IsFalse(battle.UseNudge()); // 앤티 전
            battle.PlaceAnte(1);

            Assert.IsTrue(battle.UseNudge());
            Assert.AreEqual(0, battle.NudgesRemaining);
            Assert.IsFalse(battle.UseNudge());
        }

        [Test]
        public void HighRollerBadge_RaisesPlayerTableLimit()
        {
            Assert.AreEqual(4, FoxBattle().MaxAnte(Side.Player));
            Assert.AreEqual(5, FoxBattle(RelicId.HighRollerBadge).MaxAnte(Side.Player));
        }

        [Test]
        public void InsurancePolicy_StartsTurnWithInsuranceOne()
        {
            PotBattle battle = FoxBattle(RelicId.InsurancePolicy);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(1);
            Assert.AreEqual(1, battle.Player.Insurance);
        }

        [Test]
        public void StickyDivider_AddsTwoOnChainLanding()
        {
            PotBattle battle = FoxBattle(RelicId.StickyDivider);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(1);
            battle.Land(RaisePairA); // 레이즈 2 + 2 연쇄
            Assert.AreEqual(1 + 4 + RelicCatalog.StickyDividerBonus, battle.Player.Pot);

            battle.Land(Raise3); // 혼자인 칸은 보너스 없음
            Assert.AreEqual(1 + 4 + 2 + 3, battle.Player.Pot);
        }

        [Test]
        public void FoldedCorner_AddsOneBeforeMultiplying()
        {
            PotBattle battle = FoxBattle(RelicId.FoldedCorner);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(2);
            battle.Land(Multiplier);
            Assert.AreEqual((2 + 1) * 2, battle.Player.Pot);
        }

        [Test]
        public void DopamineShot_HouseCutsAddCashOutDamage()
        {
            PotBattle battle = FoxBattle(RelicId.DopamineShot);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(1);
            battle.Land(HouseCut);
            Assert.AreEqual(1, battle.Dopamine);

            AdvanceToPlayer(battle);
            battle.PlaceAnte(4);
            battle.Land(Raise3); // 판돈 7
            int plain = 7 - battle.Dealer.Insurance;
            Assert.AreEqual(plain + 1, battle.PreviewCashOutDamage(Side.Player, out int bonus));
            Assert.AreEqual(1, bonus);
        }

        [Test]
        public void Consolation_ReturnsAnteOnHouseCut()
        {
            PotBattle battle = FoxBattle(RelicId.Consolation);
            AdvanceToPlayer(battle);
            int before = battle.Player.Chips;
            battle.PlaceAnte(3);
            battle.Land(HouseCut);
            Assert.AreEqual(before, battle.Player.Chips);
        }

        [Test]
        public void DoubleLedger_BigPotCashOutDealsTwoMore()
        {
            PotBattle battle = FoxBattle(RelicId.DoubleLedger);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(4);
            battle.Land(Raise3); // 7: 문턱 아래
            int insurance = battle.Dealer.Insurance;
            Assert.AreEqual(7 - insurance, battle.PreviewCashOutDamage(Side.Player));
            battle.Land(Multiplier); // 14
            CashOutResult result = battle.CashOut();
            Assert.AreEqual(14 - insurance + RelicCatalog.DoubleLedgerBonus, result.Damage);
            Assert.AreEqual(RelicCatalog.DoubleLedgerBonus, result.RelicBonus);
        }

        [Test]
        public void MarkedCard_HijackGivesChips()
        {
            PotBattle battle = FoxBattle(RelicId.MarkedCard);
            AdvanceToPlayer(battle);
            // 여우 하우스 룰: 판돈 4 이하로 CASH OUT 2번
            for (int i = 0; i < 2; i++)
            {
                AdvanceToPlayer(battle);
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.AreEqual(1, battle.HijackChances);
            int before = battle.Player.Chips;
            Assert.AreEqual(HijackError.None, battle.Hijack(0, RaisePairA));
            Assert.AreEqual(before + RelicCatalog.MarkedCardChips, battle.Player.Chips);
        }

        [Test]
        public void SeizureSeal_PreventsCounterHijack()
        {
            PotBattle battle = FoxBattle(RelicId.SeizureSeal);
            Assert.AreEqual(0, battle.CounterHijacksRemaining);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(1);
            battle.Land(HouseCut);
            Assert.IsFalse(battle.CounterHijackPending);
        }

        [Test]
        public void LuckyCoin_PaysOneWhenGoingSecond()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateFoxDealer(), seed, null, new[] { RelicId.LuckyCoin });
                battle.StartRound();
                Assert.AreEqual(battle.FirstThisRound == Side.Dealer ? 21 : 20, battle.Player.Chips, $"seed {seed}");
            }
        }

        [Test]
        public void Run_DealerDoorsCarryDistinctRelics_BossDoorNone()
        {
            Run run = new Run(9, 20, BattlePresets.CreateStarterWheel(), BattlePresets.CreateRabbitDealer,
                BattlePresets.CreateDealerPool(), BattlePresets.CreateStageBoss);
            Assert.AreEqual(1, run.DoorRelics.Count);
            Assert.IsTrue(run.DoorRelics[0].HasValue);

            Run boss = new Run(9, 20, BattlePresets.CreateStarterWheel(), BattlePresets.CreateStageBoss,
                BattlePresets.CreateDealerPool(), BattlePresets.CreateStageBoss, floorCount: 1);
            Assert.IsFalse(boss.DoorRelics[0].HasValue);
        }

        [Test]
        public void Run_WinningGrantsDoorRelic_AndVipCardBoostsWinnings()
        {
            Func<DealerProfile> weak = () =>
            {
                DealerProfile fox = BattlePresets.CreateFoxDealer();
                return new DealerProfile(fox.Name, 2, fox.TableLimit, 1, 99, fox.HouseRule, false, false, fox.Wheel);
            };
            Run run = new Run(1, 20, BattlePresets.CreateStarterWheel(), weak, new List<Func<DealerProfile>> { weak }, weak,
                floorCount: 3, startingRelics: new[] { RelicId.VipCard });
            RelicId? prize = run.DoorRelics[0];
            Assert.IsTrue(prize.HasValue);
            Assert.AreNotEqual(RelicId.VipCard, prize.Value);

            PotBattle battle = run.EnterDoor(0);
            for (int guard = 0; guard < 100 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                battle.PlaceAnte(battle.Active == Side.Player ? 3 : 1);
                if (battle.Active == Side.Player) battle.Land(0);
                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            Assert.AreEqual(BattleOutcome.PlayerWinsByBankrupt, battle.Outcome);
            int kept = Math.Min(battle.Player.Chips, 20);
            run.CompleteBattle();
            Assert.AreEqual(prize, run.LastRelicGained);
            Assert.Contains(prize.Value, new List<RelicId>(run.Relics));
            Assert.AreEqual(kept + (int)Math.Round(2 * Run.WinningsRatio * RelicCatalog.VipWinningsMultiplier), run.Chips);
        }
    }
}
