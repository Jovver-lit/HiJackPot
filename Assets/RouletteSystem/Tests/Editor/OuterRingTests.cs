using System.Linq;
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

        /// <summary>바깥 링에 몰수가 있어도 안쪽과 합친 확률이 N × 5%다(ADR 0015).</summary>
        [Test]
        public void RingHouseCut_CombinedChanceFollowsNTimesFive()
        {
            const int trials = 6000;
            for (int spinsBefore = 0; spinsBefore < 3; spinsBefore++)
            {
                int cut = 0;
                for (int seed = 0; seed < trials; seed++)
                {
                    PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 30, BattlePresets.CreateStageBoss(), seed);
                    battle.StartRound();
                    Side side = battle.Active;
                    battle.PlaceAnte(1);
                    int raise = battle.ActiveSeat.Wheel.ToList().FindIndex(s => s.Kind == SlotKind.Raise);
                    for (int i = 0; i < spinsBefore; i++) battle.Land(raise);
                    Assert.AreEqual(0.05f * (spinsBefore + 1), battle.HouseCutChance(side), 1e-4f);
                    int inner = battle.RollLandingIndex(side);
                    int outer = battle.RollOuterIndex(side);
                    bool innerCut = battle.SeatOf(side).Wheel[inner].Kind == SlotKind.HouseCut;
                    bool outerCut = battle.SeatOf(side).OuterRing[outer].Kind == SlotKind.HouseCut;
                    if (innerCut || outerCut) cut++;
                }

                Assert.AreEqual(0.05f * (spinsBefore + 1), (float)cut / trials, 0.015f, $"{spinsBefore + 1}번째 SPIN");
            }
        }

        /// <summary>몰수 없는 바깥 링(카지노 링)은 안쪽 몰수 확률을 나눠 갖지 않는다.</summary>
        [Test]
        public void RingWithoutHouseCut_LeavesInnerChanceAlone()
        {
            PotBattle plain = new PotBattle(BattlePresets.CreateStarterWheel(), 30, BattlePresets.CreateFoxDealer(), 1);
            Assert.AreEqual(1f, plain.OuterHouseCutWeight(Side.Dealer));
            Assert.AreEqual(0.05f, plain.HouseCutChance(Side.Dealer), 1e-4f);
        }
    }
}
