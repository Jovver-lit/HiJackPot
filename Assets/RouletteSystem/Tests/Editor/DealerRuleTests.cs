using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>딜러별 하우스 룰·고유 칸(여우·고양이·까마귀)의 EditMode 테스트.</summary>
    public sealed class DealerRuleTests
    {
        private static PotBattle Battle(DealerProfile dealer, int seed = 9)
        {
            return new PotBattle(BattlePresets.CreateStarterWheel(), 20, dealer, seed);
        }

        /// <summary>원하는 쪽 차례가 올 때까지 상대 턴을 베팅 1 → CASH OUT으로 넘긴다.</summary>
        private static void AdvanceTo(PotBattle battle, Side side)
        {
            for (int guard = 0; guard < 10; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Active == side && battle.Phase == BattlePhase.AwaitingAnte) return;
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.Fail("차례에 도달하지 못했습니다.");
        }

        [Test]
        public void Fox_HasBaseInsuranceThree_EveryTurn()
        {
            PotBattle battle = Battle(BattlePresets.CreateFoxDealer());
            AdvanceTo(battle, Side.Dealer);

            battle.PlaceAnte(1);

            Assert.AreEqual(3, battle.Dealer.Insurance);
        }

        [Test]
        public void Fox_TwoSmallCashOuts_GrantHijack()
        {
            PotBattle battle = Battle(BattlePresets.CreateFoxDealer());
            for (int i = 0; i < 2; i++)
            {
                AdvanceTo(battle, Side.Player);
                battle.PlaceAnte(2);
                battle.CashOut(); // 판돈 2 ≤ 4
            }

            Assert.AreEqual(1, battle.HijackChances);
            Assert.AreEqual(0, battle.HouseRuleProgress);
        }

        [Test]
        public void Fox_StolenBluff_IgnoresInsuranceForMinimumDamage()
        {
            PotBattle battle = Battle(BattlePresets.CreateFoxDealer());
            for (int i = 0; i < 2; i++)
            {
                AdvanceTo(battle, Side.Player);
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.AreEqual(HijackError.None, battle.Hijack(5, 7)); // 허풍 → 내 8번 칸
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(1);

            Assert.GreaterOrEqual(battle.PreviewCashOutDamage(Side.Player), 3);
        }

        [Test]
        public void DealerHouseCut_CannotBeHijacked()
        {
            PotBattle battle = Battle(BattlePresets.CreateFoxDealer());
            for (int i = 0; i < 2; i++)
            {
                AdvanceTo(battle, Side.Player);
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            int houseCut = battle.Dealer.Wheel.Count - 1;
            Assert.AreEqual(SlotKind.HouseCut, battle.Dealer.Wheel[houseCut].Kind);
            Assert.AreEqual(HijackError.HouseCutProtected, battle.CanHijack(houseCut, 0));
        }

        [Test]
        public void CleanSweep_IgnoresDealerHouseCut()
        {
            DealerProfile tiny = new DealerProfile(
                "시험 딜러", 30, 3, 1, 99, HouseRule.SmallCashOuts, false, false,
                new System.Collections.Generic.List<Slot>
                {
                    new Slot("x", SlotKind.Raise, 1, "레이즈 +1"),
                    new Slot("x_cut", SlotKind.HouseCut, 0, "몰수")
                });
            PotBattle battle = Battle(tiny);
            for (int i = 0; i < 2; i++)
            {
                AdvanceTo(battle, Side.Player);
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            battle.Hijack(0, 0);

            Assert.AreEqual(BattleOutcome.PlayerWinsByCleanSweep, battle.Outcome);
        }

        [Test]
        public void LandingOnStolenService_PaysChipsImmediately()
        {
            System.Collections.Generic.List<Slot> wheel = BattlePresets.CreateStarterWheel();
            wheel[6] = BattlePresets.CreateRabbitDealer().Wheel[5].AsStolen("svc");
            PotBattle battle = new PotBattle(wheel, 20, BattlePresets.CreateFoxDealer(), 9);
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(1);
            int chips = battle.Player.Chips;

            LandingResult result = battle.Land(6);

            Assert.IsTrue(result.KeywordTriggered);
            Assert.AreEqual(chips + 1, battle.Player.Chips);
        }

        [Test]
        public void LandingOnStolenPrepaid_GuaranteesNextRoundInitiative()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                System.Collections.Generic.List<Slot> wheel = BattlePresets.CreateStarterWheel();
                wheel[6] = BattlePresets.CreateCrowDealer().Wheel[6].AsStolen("pre");
                PotBattle battle = new PotBattle(wheel, 20, BattlePresets.CreateFoxDealer(), seed);
                AdvanceTo(battle, Side.Player);
                battle.PlaceAnte(1);
                battle.Land(6);
                battle.CashOut();
                while (battle.Phase != BattlePhase.RoundOver)
                {
                    battle.PlaceAnte(1);
                    battle.CashOut();
                }

                Assert.AreEqual(Side.Player, battle.StartRound(), $"seed {seed}");
            }
        }

        [Test]
        public void LandingOnStolenBluff_IgnoresInsuranceThisTurn()
        {
            System.Collections.Generic.List<Slot> wheel = BattlePresets.CreateStarterWheel();
            wheel[6] = BattlePresets.CreateFoxDealer().Wheel[5].AsStolen("bluff");
            PotBattle battle = new PotBattle(wheel, 20, BattlePresets.CreateFoxDealer(), 9);
            AdvanceTo(battle, Side.Dealer);
            battle.PlaceAnte(1); // 여우 보험 3
            battle.CashOut();
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(3);
            battle.Land(0);
            battle.Land(1); // 판돈 3 + 2 + 2 = 7
            battle.Land(6); // 허풍 착지

            Assert.AreEqual(System.Math.Min(battle.Dealer.Chips, 7), battle.PreviewCashOutDamage(Side.Player));
        }

        [Test]
        public void Cat_CashOutAfterMultiplier_GrantsHijack()
        {
            PotBattle battle = Battle(BattlePresets.CreateCatDealer());
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(2);
            battle.Land(2); // 배율 ×2

            battle.CashOut();

            Assert.AreEqual(1, battle.HijackChances);
        }

        [Test]
        public void Cat_CashOutWithoutMultiplier_DoesNotGrantHijack()
        {
            PotBattle battle = Battle(BattlePresets.CreateCatDealer());
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(2);
            battle.Land(0);

            battle.CashOut();

            Assert.AreEqual(0, battle.HijackChances);
        }

        [Test]
        public void Crow_PrepaidWinsInitiativeAboutThreeQuartersOfRounds()
        {
            int dealerFirst = 0;
            const int Rounds = 400;
            for (int seed = 0; seed < Rounds; seed++)
            {
                PotBattle battle = Battle(BattlePresets.CreateCrowDealer(), seed);
                if (battle.StartRound() == Side.Dealer) dealerFirst++;
            }

            Assert.That(dealerFirst, Is.InRange(260, 340));
        }

        [Test]
        public void Crow_FirstStrikeBigCashOut_GrantsHijack()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                PotBattle battle = Battle(BattlePresets.CreateCrowDealer(), seed);
                if (battle.StartRound() != Side.Player) continue;

                battle.PlaceAnte(3);
                battle.Land(0); // 레이즈 2+2 → 판돈 7, 까마귀 보험 0
                battle.CashOut();

                Assert.AreEqual(1, battle.HijackChances);
                return;
            }

            Assert.Fail("플레이어 선공 시드를 찾지 못했습니다.");
        }
    }
}
