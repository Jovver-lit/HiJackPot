using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>상점층(ADR 0009) EditMode 테스트: 10층 구성, 보스 뒤 런 계속·룰렛 형식 획득, 2회차 강화, 상점 기능, 슬롯머신 확률.</summary>
    public sealed class ShopTests
    {
        private static DealerProfile Weak(string name, IReadOnlyList<Slot> outerRing = null)
        {
            return new DealerProfile(name, 2, 3, 1, 99, HouseRule.FullCoverage, false, false,
                new List<Slot>
                {
                    new Slot(name + "_a", SlotKind.Raise, 1, "레이즈 +1"),
                    new Slot(name + "_j", SlotKind.Raise, 3, name + " 잭팟", isJackpot: true)
                }, null, 0, outerRing);
        }

        private static void Win(PotBattle battle)
        {
            for (int guard = 0; guard < 200 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                battle.PlaceAnte(battle.Active == Side.Player ? 3 : 1);
                if (battle.Active == Side.Player) battle.Land(0);
                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            Assert.AreEqual(BattlePhase.Ended, battle.Phase);
        }

        /// <summary>토끼 대신 약한 딜러로 상점층까지 간다. 현금을 넉넉히 주려고 딜러 칩을 40으로.</summary>
        private static Run RunToShop(int dealerChips = 40)
        {
            Func<DealerProfile> rich = () => Weak("부자").WithFloorScaling(dealerChips, 0);
            Run run = new Run(3, 20, BattlePresets.CreateStarterWheel(), rich,
                new List<Func<DealerProfile>> { rich, rich }, () => Weak("보스", BattlePresets.CreateOuterRing()).WithFloorScaling(dealerChips, 0));
            while (run.CurrentFloorKind != FloorKind.Shop)
            {
                if (run.PendingJackpot != null) run.PlacePendingJackpot(0);
                Win(run.EnterDoor(0));
                run.CompleteBattle();
                Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
            }

            return run;
        }

        [Test]
        public void StandardLayout_HasBossShopAndSecondStage()
        {
            Assert.AreEqual(10, Run.StandardLayout.Count);
            Assert.AreEqual(FloorKind.Boss, Run.StandardLayout[4]);
            Assert.AreEqual(FloorKind.Shop, Run.StandardLayout[5]);
            Assert.AreEqual(FloorKind.Boss, Run.StandardLayout[9]);
        }

        [Test]
        public void BeatingFirstBoss_ContinuesToShop_AndGrantsOuterRingInRun()
        {
            Run run = RunToShop();
            Assert.AreEqual(5, run.FloorIndex);
            Assert.AreEqual(1, run.Stage);
            Assert.AreEqual(4, run.OuterRing.Count);
            Assert.IsTrue(run.UnlockedOuterRingThisRun);
            Assert.AreEqual(0, run.Doors.Count);
            Assert.Throws<InvalidOperationException>(() => run.EnterDoor(0));
        }

        [Test]
        public void SecondStage_DealersAreStronger_AndBossIsRenamed()
        {
            Run run = RunToShop(10);
            run.LeaveShop();
            Assert.AreEqual(FloorKind.Dealer, run.CurrentFloorKind);
            Assert.AreEqual((int)Math.Round(10 * Run.SecondStageChipBase), run.Doors[0].StartingChips);
            Assert.AreEqual(1 + Run.SecondStageAnteBonus, run.Doors[0].DealerAnte);

            while (run.CurrentFloorKind != FloorKind.Boss)
            {
                Win(run.EnterDoor(0));
                run.CompleteBattle();
            }

            StringAssert.EndsWith(Run.SecondStageBossSuffix, run.Doors[0].Name);
            Assert.AreEqual((int)Math.Round(10 * Run.SecondStageBossChipScale), run.Doors[0].StartingChips);
        }

        [Test]
        public void Shop_SwapUpgradeRevert_CostCash()
        {
            Run run = RunToShop();
            Assume.That(run.Cash, Is.GreaterThanOrEqualTo(ShopPrices.Swap + ShopPrices.Upgrade));
            int cash = run.Cash;
            Slot first = run.Wheel[0], third = run.Wheel[2];

            Assert.AreEqual(ShopError.None, run.SwapSlots(0, 2));
            Assert.AreSame(third, run.Wheel[0]);
            Assert.AreSame(first, run.Wheel[2]);
            Assert.AreEqual(cash - ShopPrices.Swap, run.Cash);

            int raise = -1;
            for (int i = 0; i < run.Wheel.Count; i++) if (run.Wheel[i].Kind == SlotKind.Raise && !run.Wheel[i].IsJackpot) { raise = i; break; }
            int before = run.Wheel[raise].Value;
            Assert.AreEqual(ShopError.None, run.UpgradeSlot(raise));
            Assert.AreEqual(before + 1, run.Wheel[raise].Value);
            Assert.AreEqual($"레이즈 +{before + 1}", run.Wheel[raise].Label);

            int houseCut = -1;
            for (int i = 0; i < run.Wheel.Count; i++) if (run.Wheel[i].Kind == SlotKind.HouseCut) houseCut = i;
            Assert.AreEqual(ShopError.CannotUpgrade, run.UpgradeSlot(houseCut));
            Assert.AreEqual(ShopError.NotStolen, run.RevertSlot(houseCut));
        }

        [Test]
        public void Shop_NotEnoughCash_ChangesNothing()
        {
            Run run = RunToShop(2);
            // 슬롯머신으로 현금을 위치 바꾸기 가격 아래로 떨어뜨린다(기대 환수율 < 1이라 반드시 줄어든다).
            for (int guard = 0; guard < 2000 && run.Cash >= ShopPrices.Swap; guard++) run.PlaySlotMachine(1);
            Assert.Less(run.Cash, ShopPrices.Swap);
            Slot first = run.Wheel[0];
            Assert.AreEqual(ShopError.NotEnoughCash, run.SwapSlots(0, 1));
            Assert.AreSame(first, run.Wheel[0]);
        }

        [Test]
        public void SlotMachine_HouseHasASmallEdge()
        {
            double rtp = SlotMachine.ExpectedReturn();
            Assert.That(rtp, Is.InRange(0.88, 0.97), $"기대 환수율 {rtp:0.000}");
        }

        [Test]
        public void SlotMachine_PaysByTheTable_AndHouseCutLoses()
        {
            Assert.AreEqual(SlotMachine.JackpotTriplePayout, SlotMachine.PayoutMultiplier(new[] { ReelSymbol.Jackpot, ReelSymbol.Jackpot, ReelSymbol.Jackpot }, out _));
            Assert.AreEqual(SlotMachine.PairPayout, SlotMachine.PayoutMultiplier(new[] { ReelSymbol.Raise, ReelSymbol.Raise, ReelSymbol.Seven }, out _));
            Assert.AreEqual(0, SlotMachine.PayoutMultiplier(new[] { ReelSymbol.Seven, ReelSymbol.Seven, ReelSymbol.HouseCut }, out _));
            Assert.AreEqual(0, SlotMachine.PayoutMultiplier(new[] { ReelSymbol.Raise, ReelSymbol.Insurance, ReelSymbol.Dividend }, out _));
        }

        [Test]
        public void PlaySlotMachine_MovesCash_OnlyInShop()
        {
            Run run = RunToShop();
            Assume.That(run.Cash, Is.GreaterThanOrEqualTo(5));
            int cash = run.Cash;
            SlotMachineResult result = run.PlaySlotMachine(5);
            Assert.IsNotNull(result);
            Assert.AreEqual(cash - 5 + result.Payout, run.Cash);
            Assert.IsNull(run.PlaySlotMachine(run.Cash + 1));

            run.LeaveShop();
            Assert.IsNull(run.PlaySlotMachine(1));
        }
    }
}
