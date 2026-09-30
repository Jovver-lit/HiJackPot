using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>한 전투 규칙(PotBattle)의 EditMode 테스트. 코인플립 순서와 무관하게 통과하도록 헬퍼로 차례를 맞춘다.</summary>
    public sealed class PotBattleTests
    {
        private static PotBattle NewRabbitBattle(int seed = 46021)
        {
            return new PotBattle(
                BattlePresets.CreateStarterWheel(),
                BattlePresets.PlayerStartingChips,
                BattlePresets.CreateRabbitDealer(),
                seed);
        }

        /// <summary>코인플립 결과와 관계없이 원하는 쪽 차례가 올 때까지 상대 턴을 앤티 → CASH OUT으로 넘긴다.</summary>
        private static void AdvanceTo(PotBattle battle, Side side)
        {
            if (battle.Phase == BattlePhase.RoundOver)
            {
                battle.StartRound();
            }

            if (battle.Active != side)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.AreEqual(side, battle.Active);
            Assert.AreEqual(BattlePhase.AwaitingAnte, battle.Phase);
        }

        [Test]
        public void RaiseChain_SumsAdjacentRaisesIntoPot()
        {
            PotBattle battle = NewRabbitBattle();
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(2);

            LandingResult result = battle.Land(1);

            CollectionAssert.AreEqual(new[] { 0, 1 }, result.Group);
            Assert.AreEqual(4, result.Amount);
            Assert.AreEqual(6, battle.Player.Pot);
        }

        [Test]
        public void Multiplier_MultipliesCurrentPot()
        {
            PotBattle battle = NewRabbitBattle();
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(2);
            battle.Land(0);

            battle.Land(2);

            Assert.AreEqual(12, battle.Player.Pot);
        }

        [Test]
        public void ChainGroup_WrapsAroundLastAndFirstSlot()
        {
            List<Slot> wheel = new List<Slot>
            {
                new Slot("a", SlotKind.Raise, 1, "a"),
                new Slot("b", SlotKind.Insurance, 1, "b"),
                new Slot("c", SlotKind.Raise, 2, "c")
            };

            CollectionAssert.AreEqual(new[] { 2, 0 }, PotBattle.FindChainGroup(wheel, 0));
        }

        [Test]
        public void HouseCut_BurnsPotAndAnte_AndEndsTurn()
        {
            PotBattle battle = NewRabbitBattle();
            AdvanceTo(battle, Side.Player);
            int chipsBefore = battle.Player.Chips;
            battle.PlaceAnte(3);
            battle.Land(0);

            LandingResult result = battle.Land(5);

            Assert.IsTrue(result.EndedTurn);
            Assert.AreEqual(0, battle.Player.Pot);
            Assert.AreEqual(chipsBefore - 3, battle.Player.Chips);
            Assert.AreNotEqual(BattlePhase.Spinning, battle.Phase);
        }

        [Test]
        public void CashOut_IsZeroSum_DamageEqualsPotMinusInsurance()
        {
            PotBattle battle = NewRabbitBattle();
            AdvanceTo(battle, Side.Player);
            int playerBefore = battle.Player.Chips;
            int dealerBefore = battle.Dealer.Chips;
            battle.PlaceAnte(2);
            battle.Land(0); // 판돈 2 + 4 = 6

            CashOutResult result = battle.CashOut();

            Assert.AreEqual(6 - result.OpponentInsurance, result.Damage);
            Assert.AreEqual(dealerBefore - result.Damage, battle.Dealer.Chips);
            Assert.AreEqual(playerBefore + result.Damage, battle.Player.Chips);
        }

        [Test]
        public void Ante_IsClampedToTableLimit()
        {
            PotBattle battle = NewRabbitBattle();
            AdvanceTo(battle, Side.Player);

            Assert.AreEqual(3, battle.PlaceAnte(99));
        }

        [Test]
        public void DealerCashOutFullyCovered_GrantsHijackChance()
        {
            PotBattle battle = NewRabbitBattle();

            CashOutResult result = DealerCashesOutIntoInsurance(battle);

            Assert.IsTrue(result.FullCoverage);
            Assert.AreEqual(0, result.Damage);
            Assert.AreEqual(1, battle.HijackChances);
        }

        [Test]
        public void DealerCashOutPartlyCovered_GivesNoHijackChance()
        {
            PotBattle battle = NewRabbitBattle();
            PlayUntilDealerFacesInsurance(battle);
            battle.PlaceAnte(3);
            battle.Land(0); // 딜러 판돈 3 + 4 = 7 > 보험 3

            CashOutResult result = battle.CashOut();

            Assert.IsFalse(result.FullCoverage);
            Assert.AreEqual(4, result.Damage);
            Assert.AreEqual(0, battle.HijackChances);
        }

        [Test]
        public void Hijack_PermanentlyOverwrites_AndSealsDealerSlot()
        {
            PotBattle battle = GrantedHijackBattle();

            Assert.AreEqual(HijackError.None, battle.Hijack(5, 6));

            Assert.AreEqual("서비스", battle.Player.Wheel[6].Label);
            Assert.IsTrue(battle.Player.Wheel[6].IsStolen);
            Assert.IsTrue(battle.Player.Wheel[6].IsJackpot);
            Assert.AreEqual(SlotKind.Sealed, battle.Dealer.Wheel[5].Kind);
        }

        [Test]
        public void Hijack_CannotOverwriteHouseCut()
        {
            PotBattle battle = GrantedHijackBattle();

            Assert.AreEqual(HijackError.HouseCutProtected, battle.Hijack(0, 5));
        }

        [Test]
        public void Hijack_IsLimitedToOncePerRound()
        {
            PotBattle battle = GrantedHijackBattle();
            battle.Hijack(0, 0);

            Assert.AreEqual(HijackError.AlreadyUsedThisRound, battle.CanHijack(1, 1));
        }

        [Test]
        public void SealingEveryDealerSlot_WinsByCleanSweep()
        {
            DealerProfile tiny = new DealerProfile(
                "시험 딜러", 30, 3, 1, 99, HouseRule.FullCoverage, false, false,
                new List<Slot> { new Slot("x", SlotKind.Raise, 1, "레이즈 +1") });
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, tiny, 1);
            GrantHijack(battle);

            battle.Hijack(0, 0);

            Assert.AreEqual(BattleOutcome.PlayerWinsByCleanSweep, battle.Outcome);
            Assert.AreEqual(BattlePhase.Ended, battle.Phase);
        }

        [Test]
        public void StolenServiceSlot_PaysChipsAtRoundStart()
        {
            PotBattle battle = GrantedHijackBattle();
            battle.Hijack(5, 6);
            FinishRound(battle);
            int before = battle.Player.Chips;

            battle.StartRound();

            Assert.AreEqual(before + 1, battle.Player.Chips);
        }

        [Test]
        public void DealerBankrupt_WinsByBankrupt()
        {
            DealerProfile broke = new DealerProfile(
                "시험 딜러", 1, 3, 1, 99, HouseRule.FullCoverage, false, false,
                BattlePresets.CreateRabbitDealer().Wheel);
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, broke, 7);
            battle.StartRound();
            if (battle.Active == Side.Dealer)
            {
                battle.PlaceAnte(1);
                battle.Land(3); // 딜러 레이즈 +1·+3 연쇄
                battle.CashOut();
            }

            battle.PlaceAnte(3);
            battle.Land(0);
            battle.CashOut();

            Assert.AreEqual(BattleOutcome.PlayerWinsByBankrupt, battle.Outcome);
        }

        [Test]
        public void SameSeed_GivesSameInitiativeSequence()
        {
            PotBattle a = NewRabbitBattle(123);
            PotBattle b = NewRabbitBattle(123);
            for (int round = 0; round < 5; round++)
            {
                Assert.AreEqual(a.StartRound(), b.StartRound());
                FinishRound(a);
                FinishRound(b);
            }
        }

        [Test]
        public void RabbitScriptedRound_CashesOutWithAnteOnly()
        {
            PotBattle battle = NewRabbitBattle();
            while (true)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Round == 3) break;
                FinishRound(battle);
            }

            Assert.IsTrue(battle.IsScriptedInstantCashOutRound);
            AdvanceToDealerInRound(battle);
            battle.PlaceAnte(1);

            Assert.IsTrue(battle.DealerWantsToCashOut());
        }

        [Test]
        public void Comeback_IsOffWhenEven_AndBoostsGoodSlotsWhenBehind()
        {
            PotBattle battle = NewRabbitBattle();
            battle.StartRound();
            Assert.AreEqual(0f, battle.ComebackBoost);

            DealerProfile rich = new DealerProfile(
                "부자 딜러", 40, 3, 1, 6, HouseRule.FullCoverage, false, false,
                BattlePresets.CreateRabbitDealer().Wheel);
            PotBattle behind = new PotBattle(BattlePresets.CreateStarterWheel(), 20, rich, 3);
            behind.StartRound();

            Assert.Greater(behind.ComebackBoost, 0f);
            Assert.LessOrEqual(behind.ComebackBoost, PotBattle.ComebackMaxBoost);
            Slot raise = behind.Player.Wheel[0];
            Slot houseCut = behind.Player.Wheel[5];
            Assert.Greater(behind.LandingWeight(Side.Player, raise), 1f);
            Assert.AreEqual(1f, behind.LandingWeight(Side.Player, houseCut));
            Assert.AreEqual(1f, behind.LandingWeight(Side.Dealer, raise));
        }

        [Test]
        public void Comeback_TurnsOffAfterAGoodLanding()
        {
            DealerProfile rich = new DealerProfile(
                "부자 딜러", 40, 3, 1, 99, HouseRule.FullCoverage, false, false,
                BattlePresets.CreateRabbitDealer().Wheel);
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, rich, 3);
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(1);
            Assert.Greater(battle.ComebackBoost, 0f);

            battle.Land(0);

            Assert.AreEqual(0f, battle.ComebackBoost);
        }

        [Test]
        public void CounterHijack_SeizesStolenSlotFirst_AndReturnsItOnWin()
        {
            DealerProfile thief = new DealerProfile(
                "도둑 딜러", 30, 3, 1, 99, HouseRule.FullCoverage, false, true,
                new List<Slot> { new Slot("t1", SlotKind.Raise, 1, "레이즈 +1"), new Slot("t2", SlotKind.Raise, 5, "레이즈 +5") });
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, thief, 11);
            GrantHijack(battle);
            battle.Hijack(1, 0); // 레이즈 +5를 내 1번 칸에
            FinishRound(battle);
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(1);

            battle.Land(5); // 하우스 몫
            Assert.IsTrue(battle.CounterHijackPending);
            int seized = battle.ResolveCounterHijack();

            Assert.AreEqual(0, seized);
            Assert.AreEqual(SlotKind.Sealed, battle.Player.Wheel[0].Kind);
            Assert.IsFalse(battle.CounterHijackPending);
        }

        [Test]
        public void CounterHijack_SeizedSlotComesBackWhenPlayerWins()
        {
            DealerProfile thief = new DealerProfile(
                "도둑 딜러", 2, 3, 1, 99, HouseRule.FullCoverage, false, true,
                BattlePresets.CreateRabbitDealer().Wheel);
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, thief, 5);
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(1);
            battle.Land(5);
            int seized = battle.ResolveCounterHijack();
            Slot before = BattlePresets.CreateStarterWheel()[seized];
            FinishRound(battle);
            AdvanceTo(battle, Side.Player);
            battle.PlaceAnte(3);
            battle.Land(1);
            battle.CashOut(); // 판돈 7 ≥ 딜러 칩

            Assert.AreEqual(BattleOutcome.PlayerWinsByBankrupt, battle.Outcome);
            Assert.AreEqual(before.Id, battle.Player.Wheel[seized].Id);
        }

        private static void AdvanceToDealerInRound(PotBattle battle)
        {
            if (battle.Active == Side.Player)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.AreEqual(Side.Dealer, battle.Active);
        }

        private static PotBattle GrantedHijackBattle()
        {
            PotBattle battle = NewRabbitBattle();
            GrantHijack(battle);
            return battle;
        }

        private static void GrantHijack(PotBattle battle)
        {
            DealerCashesOutIntoInsurance(battle);
            Assert.AreEqual(1, battle.HijackChances);
        }

        /// <summary>플레이어가 보험 3을 쌓은 직후 딜러가 앤티 1로 곧장 CASH OUT한다(전액 보장).</summary>
        private static CashOutResult DealerCashesOutIntoInsurance(PotBattle battle)
        {
            PlayUntilDealerFacesInsurance(battle);
            battle.PlaceAnte(1);
            return battle.CashOut();
        }

        /// <summary>
        /// 플레이어가 보험 3을 쌓고 난 뒤 딜러가 앤티를 걸 차례가 될 때까지 진행한다.
        /// 보험은 자기 다음 앤티 때 사라지므로, 코인플립 순서와 관계없이 이 지점에서는 보험 3이 남아 있다.
        /// </summary>
        private static void PlayUntilDealerFacesInsurance(PotBattle battle)
        {
            bool playerInsured = false;
            for (int guard = 0; guard < 20; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver)
                {
                    battle.StartRound();
                }

                if (battle.Active == Side.Dealer && playerInsured)
                {
                    Assert.AreEqual(3, battle.Player.Insurance);
                    return;
                }

                battle.PlaceAnte(1);
                if (battle.Active == Side.Player)
                {
                    battle.Land(3);
                    playerInsured = true;
                }

                battle.CashOut();
            }

            Assert.Fail("딜러 차례에 도달하지 못했습니다.");
        }

        private static void FinishRound(PotBattle battle)
        {
            while (battle.Phase != BattlePhase.RoundOver && battle.Phase != BattlePhase.Ended)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }
        }
    }
}
