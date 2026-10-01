using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>딜러 특수 룰(실험) EditMode 테스트: 보너스형·치환형 효과가 그 좌석에만 적용되는지, 빼앗은 룰이 플레이어에 적용되는지.</summary>
    public sealed class DealerTrickTests
    {
        // 시작 룰렛: 0 레이즈+2, 1 레이즈+2, 2 배율×2, 3 보험+3, 4 레이즈+3, 5 하우스 몫, 6 배당+2, 7 보험+2
        private static PotBattle PlayerWith(DealerTrick trick)
        {
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateCatDealer(), 4, null, null, new[] { trick });
            for (int guard = 0; guard < 10; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Active == Side.Player && battle.Phase == BattlePhase.AwaitingAnte) break;
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            battle.PlaceAnte(2);
            return battle;
        }

        [Test]
        public void DealerTricks_MapByNameAndFormat()
        {
            Assert.AreEqual(DealerTrick.FoxInsuranceRebate, DealerTricks.For("여우 딜러", TrickFormat.Bonus));
            Assert.AreEqual(DealerTrick.FoxJab, DealerTricks.For("여우 딜러", TrickFormat.Substitution));
            Assert.AreEqual(DealerTrick.None, DealerTricks.For("매니저", TrickFormat.Bonus));
            Assert.AreEqual(DealerTrick.None, DealerTricks.For("여우 딜러", TrickFormat.None));
        }

        [Test]
        public void InsuranceRebate_GivesChips()
        {
            PotBattle battle = PlayerWith(DealerTrick.FoxInsuranceRebate);
            int chips = battle.Player.Chips;
            battle.Land(3);
            Assert.AreEqual(chips + 3, battle.Player.Chips);
            Assert.AreEqual(3, battle.Player.Insurance);
        }

        [Test]
        public void Jab_HitsChipsInsteadOfPot()
        {
            PotBattle battle = PlayerWith(DealerTrick.FoxJab);
            int dealer = battle.Dealer.Chips;
            battle.Land(4);
            Assert.AreEqual(2, battle.Player.Pot);
            Assert.AreEqual(dealer - 3, battle.Dealer.Chips);
        }

        [Test]
        public void Catnip_And_AllIn_ChangeMultiplier()
        {
            PotBattle catnip = PlayerWith(DealerTrick.CatCatnip);
            catnip.Land(2);
            Assert.AreEqual((2 + DealerTricks.CatnipBonus) * 2, catnip.Player.Pot);

            PotBattle allIn = PlayerWith(DealerTrick.CatAllIn);
            allIn.Land(2);
            Assert.AreEqual(2 * 3, allIn.Player.Pot);
            allIn.Land(3);
            Assert.AreEqual(0, allIn.Player.Insurance);
        }

        [Test]
        public void Shiny_StealsOnDividend()
        {
            PotBattle battle = PlayerWith(DealerTrick.CrowShiny);
            int me = battle.Player.Chips, dealer = battle.Dealer.Chips;
            battle.Land(6);
            Assert.AreEqual(me + 4, battle.Player.Chips);
            Assert.AreEqual(dealer - 2, battle.Dealer.Chips);
        }

        [Test]
        public void DealerTrick_AppliesOnlyToDealerSeat()
        {
            DealerProfile fox = BattlePresets.CreateFoxDealer().WithTrick(DealerTrick.FoxJab);
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, fox, 4);
            Assert.IsTrue(battle.HasTrick(Side.Dealer, DealerTrick.FoxJab));
            Assert.IsFalse(battle.HasTrick(Side.Player, DealerTrick.FoxJab));
        }

        [Test]
        public void Run_StealsTrick_WhenChallengeMet()
        {
            RunRules rules = new RunRules { Tricks = TrickFormat.Bonus, TricksFromStage = 0, CanStealTricks = true, StealHouseRules = 1 };
            System.Func<DealerProfile> fox = () => BattlePresets.CreateFoxDealer().WithFloorScaling(30, 0);
            System.Func<DealerProfile> tutorial = () => BattlePresets.CreateFoxDealer().WithFloorScaling(1, 0);
            Run run = new Run(2, 40, BattlePresets.CreateStarterWheel(), tutorial, new List<System.Func<DealerProfile>> { fox }, fox, floorCount: 3, rules: rules);
            Assert.AreEqual(DealerTrick.None, run.Doors[0].Trick, "1층 튜토리얼 딜러에는 특수 룰이 없다");
            PotBattle first = run.EnterDoor(0);
            for (int guard = 0; guard < 50 && first.Phase != BattlePhase.Ended; guard++)
            {
                if (first.Phase == BattlePhase.RoundOver) first.StartRound();
                first.PlaceAnte(first.Active == Side.Player ? 4 : 1);
                if (first.Active == Side.Player) first.Land(0);
                if (first.Phase == BattlePhase.Spinning) first.CashOut();
            }

            run.CompleteBattle();
            Assert.AreEqual(DealerTrick.FoxInsuranceRebate, run.Doors[0].Trick);
            PotBattle battle = run.EnterDoor(0);
            // 여우 하우스 룰: 판돈 4 이하로 CASH OUT 2번 → 한 번 달성 뒤 이긴다.
            for (int guard = 0; guard < 300 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Active == Side.Player)
                {
                    battle.PlaceAnte(battle.HouseRulesAchieved > 0 ? 4 : 1);
                    if (battle.HouseRulesAchieved > 0) { battle.Land(0); battle.Land(2); }
                }
                else battle.PlaceAnte(1);

                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            Assume.That(battle.Outcome, Is.Not.EqualTo(BattleOutcome.DealerWins));
            Assert.GreaterOrEqual(battle.HouseRulesAchieved, 1, $"outcome {battle.Outcome} round {battle.Round}");
            run.CompleteBattle();
            Assert.AreEqual(DealerTrick.FoxInsuranceRebate, run.LastTrickStolen);
            CollectionAssert.Contains(new List<DealerTrick>(run.Tricks), DealerTrick.FoxInsuranceRebate);
        }
    }
}
