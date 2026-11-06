using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RouletteLike.Battle.Tests
{
    /// <summary>칸에 붙이는 유물(ADR 0017)의 발동·이득 기록과 런의 획득·배치·유지 EditMode 테스트.</summary>
    public sealed class RelicAttachmentTests
    {
        // 시작 룰렛: 0 레이즈+2, 1 레이즈+2, 2 배율×2, 3 보험+3, 4 레이즈+3, 5 몰수, 6 배당+2, 7 보험+2
        private const int RaiseA = 0, Raise3 = 4, HouseCut = 5;

        private static PotBattle BattleWith(int slotIndex, RelicId relic)
        {
            List<Slot> wheel = BattlePresets.CreateStarterWheel();
            wheel[slotIndex] = wheel[slotIndex].WithAttachment(relic);
            PotBattle battle = new PotBattle(wheel, 20, BattlePresets.CreateFoxDealer(), 4);
            for (int guard = 0; guard < 10; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                if (battle.Active == Side.Player && battle.Phase == BattlePhase.AwaitingAnte) return battle;
                battle.PlaceAnte(1);
                battle.CashOut();
            }

            Assert.Fail("플레이어 차례에 도달하지 못했습니다.");
            return null;
        }

        [Test]
        public void SkimmingHook_GivesChipsOnlyWhenItsSlotLands()
        {
            PotBattle battle = BattleWith(Raise3, RelicId.SkimmingHook);
            battle.PlaceAnte(1);
            int chips = battle.Player.Chips;

            Assert.AreEqual("", battle.Land(RaiseA).AttachmentNote);
            Assert.AreEqual(chips, battle.Player.Chips);

            StringAssert.Contains("갈고리", battle.Land(Raise3).AttachmentNote);
            Assert.AreEqual(chips + RelicCatalog.SkimmingHookChips, battle.Player.Chips);
            Assert.AreEqual(RelicCatalog.SkimmingHookChips, battle.AttachmentGains[RelicId.SkimmingHook]);
        }

        [Test]
        public void ForgedReceipt_AddsDamageThisTurn_AndCountsOnlyWhenCashedOut()
        {
            PotBattle battle = BattleWith(Raise3, RelicId.ForgedReceipt);
            battle.PlaceAnte(1);
            battle.Land(Raise3);
            int insurance = battle.Dealer.Insurance;

            Assert.IsFalse(battle.AttachmentGains.ContainsKey(RelicId.ForgedReceipt));
            CashOutResult result = battle.CashOut();

            Assert.AreEqual(Math.Max(0, 4 - insurance) + RelicCatalog.ForgedReceiptDamage, result.Damage);
            Assert.AreEqual(RelicCatalog.ForgedReceiptDamage, battle.AttachmentGains[RelicId.ForgedReceipt]);
        }

        [Test]
        public void RefundTicket_ReturnsAnteOnlyAfterItsSlotLandedThisTurn()
        {
            PotBattle battle = BattleWith(Raise3, RelicId.RefundTicket);
            battle.PlaceAnte(2);
            int chips = battle.Player.Chips;
            battle.Land(Raise3);

            LandingResult cut = battle.Land(HouseCut);

            Assert.IsTrue(cut.HouseCutHit);
            Assert.AreEqual(chips + 2, battle.Player.Chips);
            Assert.AreEqual(2, battle.AttachmentGains[RelicId.RefundTicket]);
        }

        [Test]
        public void MagnetStrip_NeedsASameKindNeighbor()
        {
            PotBattle battle = BattleWith(RaiseA, RelicId.MagnetStrip); // 옆 칸이 레이즈
            battle.PlaceAnte(1);
            battle.Land(RaiseA);
            Assert.AreEqual(1 + 2 + RelicCatalog.MagnetStripPot, battle.Player.Pot);

            PotBattle alone = BattleWith(Raise3, RelicId.MagnetStrip); // 옆 칸이 보험과 몰수
            alone.PlaceAnte(1);
            Assert.AreEqual("", alone.Land(Raise3).AttachmentNote);
            Assert.AreEqual(1 + 3, alone.Player.Pot);
        }

        [Test]
        public void Attachment_SurvivesSeizeAndRecapture()
        {
            Slot seized = new Slot("s", SlotKind.Raise, 2, "레이즈 +2", attachment: RelicId.SkimmingHook).AsSeized();

            Assert.AreEqual(RelicId.SkimmingHook, seized.Attachment);
            Assert.AreEqual(RelicId.SkimmingHook, seized.AsStolen("back").Attachment);
        }

        [Test]
        public void Run_OnlyOffersAttachments_AndRequiresPlacementBeforeNextDoor()
        {
            Func<DealerProfile> weak = () =>
            {
                DealerProfile fox = BattlePresets.CreateFoxDealer();
                return new DealerProfile(fox.Name, 6, fox.TableLimit, 1, 99, fox.HouseRule, false, false, fox.Wheel);
            };
            Run run = new Run(1, 20, BattlePresets.CreateStarterWheel(), weak, new List<Func<DealerProfile>> { weak }, weak, floorCount: 3);
            RelicId prize = run.DoorRelics[0].Value;
            Assert.IsTrue(RelicCatalog.Get(prize).IsAttachment);

            PotBattle battle = run.EnterDoor(0);
            for (int guard = 0; guard < 200 && battle.Phase != BattlePhase.Ended; guard++)
            {
                if (battle.Phase == BattlePhase.RoundOver) battle.StartRound();
                battle.PlaceAnte(1);
                if (battle.Active == Side.Player) battle.Land(Raise3);
                if (battle.Phase == BattlePhase.Spinning) battle.CashOut();
            }

            run.CompleteBattle();
            Assert.AreEqual(prize, run.PendingAttachment);
            Assert.Throws<InvalidOperationException>(() => run.EnterDoor(0));
            Assert.IsFalse(run.PlacePendingAttachment(HouseCut));

            Assert.IsTrue(run.PlacePendingAttachment(RaiseA));
            Assert.AreEqual(prize, run.Wheel[RaiseA].Attachment);
            Assert.IsNull(run.PendingAttachment);
            CollectionAssert.AreEqual(new[] { prize }, run.AttachedRelics);
        }
    }
}
