using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>바깥 링(이중 룰렛)·잭팟 라인·바깥 몰수·보호막과 보스 매니저, 런의 바깥 링 해금 EditMode 테스트.</summary>
    public sealed class OuterRingTests
    {
        // 바깥 링 인덱스: 0 레이즈 +3, 1 배율 ×2, 2 보험 +2, 3 바깥 몰수
        private const int OuterRaise = 0, OuterMultiplier = 1, OuterInsurance = 2, OuterHouseCut = 3;

        private static PotBattle BossBattle(int seed = 4)
        {
            return new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateStageBoss(), seed);
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
        public void BossTable_GivesBothSeatsTheOuterRing()
        {
            PotBattle battle = BossBattle();

            Assert.IsTrue(battle.Player.HasOuterRing);
            Assert.IsTrue(battle.Dealer.HasOuterRing);
            Assert.AreEqual(4, battle.Player.OuterRing.Count);
        }

        [Test]
        public void OuterRaise_AddsAfterInnerLanding()
        {
            PotBattle battle = BossBattle();
            AdvanceToPlayer(battle);
            battle.PlaceAnte(2);

            LandingResult result = battle.Land(4, OuterRaise); // 안쪽 레이즈 +3 (옆 칸과 안 이어짐)

            Assert.IsTrue(result.JackpotLine); // 레이즈 + 레이즈
            Assert.AreEqual(2 + 3 + 3 + 3, battle.Player.Pot); // 베팅 + 안쪽 + 잭팟 라인 + 바깥 레이즈
        }

        [Test]
        public void OuterMultiplier_FiresInnerEffectTwice()
        {
            PotBattle battle = BossBattle();
            AdvanceToPlayer(battle);
            battle.PlaceAnte(2);

            battle.Land(3, OuterMultiplier); // 안쪽 보험 +3, 바깥 배율

            Assert.AreEqual(6, battle.Player.Insurance);
        }

        [Test]
        public void JackpotLine_CountsTowardBossHouseRule()
        {
            PotBattle battle = BossBattle();
            AdvanceToPlayer(battle);
            battle.PlaceAnte(1);
            battle.Land(3, OuterInsurance); // 보험 + 보험 = 잭팟 라인 1
            battle.Land(7, OuterInsurance); // 보험 + 보험 = 잭팟 라인 2

            Assert.AreEqual(1, battle.HijackChances);
        }

        [Test]
        public void OuterHouseCut_EvaporatesPotAfterInnerEffect()
        {
            PotBattle battle = BossBattle();
            AdvanceToPlayer(battle);
            battle.PlaceAnte(3);

            LandingResult result = battle.Land(0, OuterHouseCut);

            Assert.IsTrue(result.HouseCutHit);
            Assert.IsTrue(result.EndedTurn);
            Assert.AreEqual(0, battle.Player.Pot);
        }

        [Test]
        public void StolenVipShield_BlocksOuterHouseCutThisTurn()
        {
            List<Slot> wheel = BattlePresets.CreateStarterWheel();
            wheel[7] = new Slot("vip", SlotKind.CutShield, 1, "VIP 보호막", isJackpot: true, isStolen: true);
            PotBattle battle = new PotBattle(wheel, 20, BattlePresets.CreateStageBoss(), 4);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(3);
            battle.Land(7, OuterRaise); // 보호막 1회 + 바깥 레이즈
            int pot = battle.Player.Pot;

            LandingResult result = battle.Land(4, OuterHouseCut);

            Assert.IsTrue(result.CutShielded);
            Assert.IsFalse(result.EndedTurn);
            Assert.AreEqual(pot + 3, battle.Player.Pot); // 안쪽 레이즈 +3은 들어오고 판돈은 지켜진다
        }

        [Test]
        public void ShieldsExpireAtNextAnte()
        {
            List<Slot> wheel = BattlePresets.CreateStarterWheel();
            wheel[7] = new Slot("vip", SlotKind.CutShield, 1, "VIP 보호막", isJackpot: true, isStolen: true);
            PotBattle battle = new PotBattle(wheel, 20, BattlePresets.CreateStageBoss(), 4);
            AdvanceToPlayer(battle);
            battle.PlaceAnte(1);
            battle.Land(7, OuterRaise);
            battle.CashOut();
            AdvanceToPlayer(battle);

            battle.PlaceAnte(1);

            Assert.AreEqual(0, battle.Player.CutShields);
        }

        [Test]
        public void OrdinaryDealer_HasNoOuterRing()
        {
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateFoxDealer(), 1);

            Assert.IsFalse(battle.Player.HasOuterRing);
            Assert.AreEqual(-1, battle.RollOuterIndex(Side.Player));
        }

        [Test]
        public void BeatingTheBoss_UnlocksOuterRing_AndUnlockedRingFollowsTheRun()
        {
            Func<DealerProfile> weakBoss = () =>
            {
                DealerProfile boss = BattlePresets.CreateStageBoss();
                return new DealerProfile(boss.Name, 2, boss.TableLimit, 1, 99, boss.HouseRule, false, false,
                    boss.Wheel, null, 0, boss.TableOuterRing);
            };
            Run run = new Run(1, 20, BattlePresets.CreateStarterWheel(), weakBoss,
                new List<Func<DealerProfile>> { weakBoss }, weakBoss, floorCount: 1);
            PotBattle battle = run.EnterDoor(0);
            for (int guard = 0; guard < 100 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                battle.PlaceAnte(battle.Active == Side.Player ? 3 : 1);
                if (battle.Active == Side.Player) battle.Land(0);
                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            run.CompleteBattle();
            Assert.AreEqual(RunOutcome.Escaped, run.Outcome);
            Assert.IsTrue(run.UnlockedOuterRingThisRun);

            Run next = new Run(2, 20, BattlePresets.CreateStarterWheel(), BattlePresets.CreateRabbitDealer,
                BattlePresets.CreateDealerPool(), BattlePresets.CreateStageBoss, outerRing: BattlePresets.CreateOuterRing());
            Assert.IsTrue(next.EnterDoor(0).Player.HasOuterRing);
        }
    }
}
