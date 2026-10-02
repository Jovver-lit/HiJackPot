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

        /// <summary>플레이어가 매 턴 베팅 3 → 레이즈 → CASH OUT, 딜러는 베팅 1 → CASH OUT으로 끝낸다.</summary>
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
        public void WonChips_SplitIntoChipsAndCash_PlusWinnings()
        {
            Run run = NewRun();
            int entry = run.Chips;
            PotBattle battle = run.EnterDoor(0);
            WinByBankrupt(battle);
            int chipsAfterBattle = battle.Player.Chips;
            int excess = Math.Max(0, chipsAfterBattle - entry);
            Assert.Greater(excess, 0, "이 테스트는 입장 칩보다 많이 딴 경우를 본다");

            run.CompleteBattle();

            // 딴 칩 정산(ADR 0008): 입장 칩까지 + 딴 칩의 일정 비율은 칩, 나머지는 현금(기본 상금 없음).
            int kept = (int)Math.Floor(excess * ChipExchangeRules.Default.KeepShare);
            int winnings = (int)Math.Round(BattlePresets.CreateRabbitDealer().StartingChips * ChipExchangeRules.Default.WinningsRatio);
            Assert.AreEqual(entry + kept + winnings, run.Chips);
            Assert.AreEqual(excess - kept, run.Cash);
            Assert.AreEqual(run.Chips, run.LastSettlement.ChipsAfter);
        }

        [Test]
        public void LosingChips_KeepsWhatIsLeft_AndNoCash()
        {
            Run run = NewRun();
            PotBattle battle = run.EnterDoor(0);
            battle.StartRound();
            if (battle.Active == Side.Dealer) { battle.PlaceAnte(1); battle.CashOut(); }
            battle.PlaceAnte(3);
            battle.Land(5); // 몰수: 베팅 3 증발
            int left = battle.Player.Chips;
            Assert.Less(left, 20);

            ChipSettlement settlement = run.PreviewSettlement(battle);
            Assert.AreEqual(left, settlement.BaseChips);
            Assert.AreEqual(0, settlement.KeptExcess);
            Assert.AreEqual(0, settlement.Cash);
        }

        [Test]
        public void ExchangeWindow_BuysChipsWithCash_AtTheRate()
        {
            ChipExchangeRules rules = new ChipExchangeRules(0f, 2, 0f, 0);
            Run run = new Run(5, 20, BattlePresets.CreateStarterWheel(), BattlePresets.CreateRabbitDealer,
                BattlePresets.CreateDealerPool(), BattlePresets.CreateStageBoss, exchange: rules);
            PotBattle battle = run.EnterDoor(0);
            WinByBankrupt(battle);
            int excess = Math.Max(0, battle.Player.Chips - 20);
            run.CompleteBattle();
            Assert.AreEqual(excess, run.Cash);

            int chips = run.Chips;
            int bought = run.BuyChips(int.MaxValue);
            Assert.AreEqual(excess / 2, bought);
            Assert.AreEqual(chips + bought, run.Chips);
            Assert.AreEqual(excess % 2, run.Cash);
        }

        [Test]
        public void ExchangeWindow_CapsChipsPerVisit()
        {
            Run run = new Run(5, 20, BattlePresets.CreateStarterWheel(), BattlePresets.CreateRabbitDealer,
                BattlePresets.CreateDealerPool(), BattlePresets.CreateStageBoss, exchange: new ChipExchangeRules(0f, 1, 0f, 3));
            PotBattle battle = run.EnterDoor(0);
            WinByBankrupt(battle);
            run.CompleteBattle();
            Assume.That(run.Cash, Is.GreaterThanOrEqualTo(4));

            Assert.AreEqual(3, run.BuyChips(int.MaxValue));
            Assert.AreEqual(0, run.BuyChips(1));
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
                if (battle.Active == Side.Player) battle.Land(5); // 몰수로 베팅을 잃는다
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
                if (run.CurrentFloorKind == FloorKind.Shop)
                {
                    run.LeaveShop();
                    continue;
                }

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

            // 플레이어 보험 3 → 딜러 베팅 1 CASH OUT(전액 보장) → HIJACK으로 유일한 칸 봉인 → 완전 강탈.
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
