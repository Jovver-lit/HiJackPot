using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>칩 경제 EditMode 테스트: 테이블 마감 사용료(교착 방지), 층별 딜러 강화(ADR 0007과 함께 정한 런 곡선).</summary>
    public sealed class EconomyTests
    {
        private static void PlayRoundQuietly(PotBattle battle)
        {
            // 양쪽 모두 베팅 1로 곧장 CASH OUT: 칩이 거의 움직이지 않는 라운드.
            for (int turn = 0; turn < 2 && battle.Phase != BattlePhase.Ended; turn++)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }
        }

        [Test]
        public void TableFee_StartsAtRoundTen_AndSurgesAtFifteen()
        {
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 60, BattlePresets.CreateFoxDealer().WithFloorScaling(60, 0), 2);
            for (int round = 1; round < PotBattle.TableFeeStartRound; round++)
            {
                battle.StartRound();
                Assert.AreEqual(0, battle.TableFee, $"round {round}");
                PlayRoundQuietly(battle);
            }

            int playerBefore = battle.Player.Chips;
            int dealerBefore = battle.Dealer.Chips;
            battle.StartRound();
            Assert.AreEqual(1, battle.TableFee);
            Assert.AreEqual(playerBefore - 1, battle.Player.Chips);
            Assert.AreEqual(dealerBefore - 1, battle.Dealer.Chips);
            CollectionAssert.Contains(new List<string>(battle.RoundStartEffects), "테이블 마감: 사용료 양쪽 칩 −1");

            while (battle.Round < PotBattle.TableFeeSurgeRound && battle.Phase != BattlePhase.Ended)
            {
                PlayRoundQuietly(battle);
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
            }

            Assert.AreEqual(2, battle.TableFee);
        }

        [Test]
        public void TableFee_CanBankruptAndEndTheBattle()
        {
            DealerProfile fox = BattlePresets.CreateFoxDealer();
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 40, fox.WithFloorScaling(40, 0), 2);
            for (int round = 1; round < PotBattle.TableFeeStartRound; round++)
            {
                battle.StartRound();
                PlayRoundQuietly(battle);
            }

            // 딜러 칩을 1로 만든 뒤 마감 라운드를 시작하면 사용료로 파산한다.
            battle.Dealer.Chips = 1;
            battle.StartRound();
            Assert.AreEqual(BattlePhase.Ended, battle.Phase);
            Assert.AreEqual(BattleOutcome.PlayerWinsByBankrupt, battle.Outcome);
        }

        private static int IndexOf(Seat seat, SlotKind kind)
        {
            for (int i = 0; i < seat.Wheel.Count; i++)
            {
                if (seat.Wheel[i].Kind == kind) return i;
            }

            Assert.Fail($"{kind} 칸이 없습니다.");
            return -1;
        }

        /// <summary>마지막 1칩을 걸어도 바로 죽지 않고, 불려서 CASH OUT하면 칩을 되찾는다(사용자 확인 2026-11-05).</summary>
        [Test]
        public void LastChip_CanBeBetAndSpun_AndCashOutRecovers()
        {
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateFoxDealer(), 3);
            battle.StartRound();
            if (battle.Active == Side.Dealer)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            battle.Player.Chips = 1;
            battle.PlaceAnte(3);
            Assert.AreEqual(0, battle.Player.Chips);
            Assert.AreEqual(BattlePhase.Spinning, battle.Phase);

            battle.Land(IndexOf(battle.Player, SlotKind.Raise));
            Assert.AreEqual(BattlePhase.Spinning, battle.Phase);
            CashOutResult result = battle.CashOut();
            Assert.AreEqual(1 + result.Damage, battle.Player.Chips);
            Assert.AreNotEqual(BattlePhase.Ended, battle.Phase);
        }

        /// <summary>마지막 칩을 걸고 몰수에 걸리면 파산한다. 그 칩으로 계속 돌릴지는 플레이어의 도박이다.</summary>
        [Test]
        public void LastChip_LostToHouseCut_IsBankrupt()
        {
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateFoxDealer(), 3);
            battle.StartRound();
            if (battle.Active == Side.Dealer)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            battle.Player.Chips = 1;
            battle.PlaceAnte(1);
            battle.Land(IndexOf(battle.Player, SlotKind.HouseCut));
            if (battle.CounterHijackPending) battle.ResolveCounterHijack();
            Assert.AreEqual(BattlePhase.Ended, battle.Phase);
            Assert.AreEqual(BattleOutcome.DealerWins, battle.Outcome);
        }

        /// <summary>치명적인 피해는 그대로 치명적이다. 칩 2에서 큰 피해를 맞으면 1칩으로 버티지 않는다.</summary>
        [Test]
        public void LethalCashOut_DoesNotLeaveOneChip()
        {
            PotBattle battle = new PotBattle(BattlePresets.CreateStarterWheel(), 20, BattlePresets.CreateFoxDealer(), 3);
            battle.StartRound();
            if (battle.Active == Side.Dealer)
            {
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            battle.Dealer.Chips = 2;
            battle.PlaceAnte(1);
            battle.Player.Pot = 100;
            battle.CashOut();
            Assert.AreEqual(BattlePhase.Ended, battle.Phase);
            Assert.AreEqual(BattleOutcome.PlayerWinsByBankrupt, battle.Outcome);
        }

        [Test]
        public void DealerFloors_GetMoreChips_AndFloorFourAddsAnte()
        {
            Func<DealerProfile> fox = BattlePresets.CreateFoxDealer;
            Run run = new Run(5, 999, BattlePresets.CreateStarterWheel(), fox, new List<Func<DealerProfile>> { fox }, BattlePresets.CreateStageBoss);
            int baseChips = fox().StartingChips;
            int baseAnte = fox().DealerAnte;
            int[] expected = { baseChips, baseChips, (int)Math.Round(baseChips * 1.3f), (int)Math.Round(baseChips * 1.6f) };
            for (int floor = 0; floor < 4; floor++)
            {
                Assert.AreEqual(expected[floor], run.Doors[0].StartingChips, $"floor {floor + 1}");
                Assert.AreEqual(floor >= Run.DealerAnteBonusFromPosition ? baseAnte + 1 : baseAnte, run.Doors[0].DealerAnte, $"floor {floor + 1}");
                PotBattle battle = run.EnterDoor(0);
                for (int guard = 0; guard < 200 && battle.Phase != BattlePhase.Ended; guard++)
                {
                    if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                    if (battle.Phase == BattlePhase.Ended) break; // 테이블 마감 사용료로 끝날 수 있다
                    battle.PlaceAnte(battle.Active == Side.Player ? 4 : 1);
                    if (battle.Active == Side.Player) battle.Land(0);
                    if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
                }

                run.CompleteBattle();
            }
        }
    }
}
