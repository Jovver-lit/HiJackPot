using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>런 구조(Run)의 EditMode 테스트: 층·문, 칩·룰렛 이어가기, 상금, 탈출, JACKPOT 획득.</summary>
    public sealed class RunTests
    {
        private static DealerProfile Dealer(string name, int chips)
        {
            return new DealerProfile(
                name, chips, 3, 1, 99, HouseRule.FullCoverage, false, false,
                new List<Slot>
                {
                    new Slot(name + "_a", SlotKind.Raise, 1, "레이즈 +1"),
                    new Slot(name + "_j", SlotKind.Dividend, 1, name + " 잭팟", SlotTrigger.RoundStart, isJackpot: true)
                });
        }

        private static Run NewRun(int seed = 1)
        {
            return new Run(
                seed,
                20,
                BattlePresets.CreateStarterWheel(),
                BattlePresets.CreateRabbitDealer,
                new List<Func<DealerProfile>> { () => Dealer("A", 2), () => Dealer("B", 2), () => Dealer("C", 2) },
                () => Dealer("보스", 2));
        }

        /// <summary>플레이어가 매 턴 앤티 3 → 레이즈 연쇄 → CASH OUT, 딜러는 앤티 1 → CASH OUT으로 끝낸다.</summary>
        private static void WinByBankrupt(PotBattle battle)
        {
            for (int guard = 0; guard < 200 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Active == Side.Player)
                {
                    battle.PlaceAnte(3);
                    battle.Land(0);
                }
                else
                {
                    battle.PlaceAnte(1);
                }

                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            Assert.AreEqual(BattleOutcome.PlayerWinsByBankrupt, battle.Outcome);
        }

        [Test]
        public void FirstFloorIsTutorialRabbit_LaterFloorsOfferTwoDifferentDoors()
        {
            Run run = NewRun();
            Assert.AreEqual(1, run.Doors.Count);
            Assert.AreEqual("토끼 딜러", run.Doors[0].Name);

            PotBattle battle = run.EnterDoor(0);
            WinByBankrupt(battle);
            run.CompleteBattle();

            Assert.AreEqual(1, run.FloorIndex);
            Assert.AreEqual(2, run.Doors.Count);
            Assert.AreNotEqual(run.Doors[0].Name, run.Doors[1].Name);
        }

        [Test]
        public void ChipsAndHijackedSlotsCarryOver_WithWinnings()
        {
            Run run = NewRun();
            PotBattle battle = run.EnterDoor(0);
            // 첫 라운드에 HIJACK 기회를 강제로 만들 수 없으므로 룰렛 변화는 파산 승리 후 칩으로만 확인한다.
            WinByBankrupt(battle);
            int chipsAfterBattle = battle.Player.Chips;

            run.CompleteBattle();

            int winnings = (int)Math.Round(BattlePresets.CreateRabbitDealer().StartingChips * Run.WinningsRatio);
            Assert.AreEqual(chipsAfterBattle + winnings, run.Chips);
        }

        [Test]
        public void LosingABattle_EndsTheRunBankrupt()
        {
            Run run = new Run(
                3, 1, BattlePresets.CreateStarterWheel(),
                () => Dealer("강한 딜러", 99),
                new List<Func<DealerProfile>> { () => Dealer("A", 2) },
                () => Dealer("보스", 2));
            PotBattle battle = run.EnterDoor(0);
            for (int guard = 0; guard < 200 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                battle.PlaceAnte(1);
                if (battle.Active == Side.Player) battle.Land(5); // 하우스 몫으로 앤티를 잃는다
                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            run.CompleteBattle();

            Assert.AreEqual(RunOutcome.Bankrupt, run.Outcome);
        }

        [Test]
        public void BeatingTheBossOnTheLastFloor_Escapes()
        {
            Run run = NewRun();
            for (int floor = 0; floor < run.FloorCount; floor++)
            {
                if (floor == run.FloorCount - 1) Assert.AreEqual(FloorKind.Boss, run.CurrentFloorKind);
                WinByBankrupt(run.EnterDoor(0));
                run.CompleteBattle();
            }

            Assert.AreEqual(RunOutcome.Escaped, run.Outcome);
        }

        [Test]
        public void CleanSweep_GrantsJackpotSlotToPlace_NotOnHouseCut()
        {
            DealerProfile single = new DealerProfile(
                "외톨이", 50, 3, 1, 99, HouseRule.FullCoverage, false, false,
                new List<Slot> { new Slot("x_j", SlotKind.Raise, 4, "외톨이 잭팟", isJackpot: true) });
            Run run = new Run(
                5, 20, BattlePresets.CreateStarterWheel(), () => single,
                new List<Func<DealerProfile>> { () => Dealer("A", 2) }, () => Dealer("보스", 2));
            PotBattle battle = run.EnterDoor(0);

            // 플레이어 보험 3 → 딜러 앤티 1 CASH OUT(전액 보장) → HIJACK으로 유일한 칸 봉인 → 완전 강탈.
            for (int guard = 0; guard < 50 && battle.HijackChances == 0; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                battle.PlaceAnte(1);
                if (battle.Active == Side.Player) battle.Land(3);
                battle.CashOut();
            }

            Assert.AreEqual(HijackError.None, battle.Hijack(0, 7));
            Assert.AreEqual(BattleOutcome.PlayerWinsByCleanSweep, battle.Outcome);
            run.CompleteBattle();

            // 방금 HIJACK으로 이미 가져왔으므로 확정 획득분은 없다.
            Assert.IsNull(run.PendingJackpot);
        }
    }
}
