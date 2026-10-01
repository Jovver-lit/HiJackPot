using System;
using System.Collections.Generic;

namespace RouletteLike.Battle
{
    /// <summary>상점에서 하려다 안 된 이유.</summary>
    public enum ShopError
    {
        None,
        NotInShop,
        NotEnoughCash,
        InvalidSlot,
        CannotUpgrade,
        NotStolen,
        SoldOut
    }

    /// <summary>
    /// 상점 가격(현금). 새 칸은 팔지 않는다 — 새 칸은 HIJACK으로만 얻는다(ADR 0003·0009). 상점은 내 룰렛을 고치는 곳이다.
    /// </summary>
    public static class ShopPrices
    {
        public const int Swap = 12;
        public const int Upgrade = 18;
        public const int UpgradeMultiplier = 35;
        public const int Revert = 8;
        public const int Relic = 30;

        /// <summary>강화 상한: 레이즈·보험 6, 배당·허풍 4, 배율 ×4.</summary>
        public static int MaxValue(SlotKind kind)
        {
            switch (kind)
            {
                case SlotKind.Raise:
                case SlotKind.Insurance:
                    return 6;
                case SlotKind.Multiplier:
                    return 4;
                case SlotKind.Dividend:
                case SlotKind.MinimumPayout:
                    return 4;
                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// 런의 상점층 기능(Run의 나머지 절반): 칸 위치 바꾸기·칸 강화·HIJACK 되돌리기·유물 1개·칩 사기(환전)·슬롯머신.
    /// 상점층에서만 쓸 수 있고, LeaveShop으로 다음 층(2회차 딜러)에 간다.
    /// </summary>
    public sealed partial class Run
    {
        private readonly Random _shopRng;

        /// <summary>이번 상점에 걸린 유물(없거나 이미 샀으면 null).</summary>
        public RelicId? ShopRelic { get; private set; }

        /// <summary>마지막 슬롯머신 결과(없으면 null).</summary>
        public SlotMachineResult LastSlotMachine { get; private set; }

        public bool InShop => Outcome == RunOutcome.InProgress && CurrentFloorKind == FloorKind.Shop;

        private void StockShop()
        {
            List<RelicId> pool = new List<RelicId>();
            foreach (Relic relic in RelicCatalog.Relics)
            {
                if (!_relics.Contains(relic.Id)) pool.Add(relic.Id);
            }

            ShopRelic = pool.Count > 0 ? pool[_shopRng.Next(pool.Count)] : (RelicId?)null;
            LastSlotMachine = null;
        }

        /// <summary>칸 강화 가격(배율은 비싸다). 강화할 수 없으면 -1.</summary>
        public int UpgradePrice(int index)
        {
            if (index < 0 || index >= _wheel.Count) return -1;
            Slot slot = _wheel[index];
            if (slot.Value >= ShopPrices.MaxValue(slot.Kind)) return -1;
            return slot.Kind == SlotKind.Multiplier ? ShopPrices.UpgradeMultiplier : ShopPrices.Upgrade;
        }

        /// <summary>두 칸의 자리를 맞바꾼다(하우스 몫도 옮길 수 있다 — 연쇄를 직접 설계).</summary>
        public ShopError SwapSlots(int a, int b)
        {
            if (!InShop) return ShopError.NotInShop;
            if (a < 0 || b < 0 || a >= _wheel.Count || b >= _wheel.Count || a == b) return ShopError.InvalidSlot;
            if (Cash < ShopPrices.Swap) return ShopError.NotEnoughCash;
            Cash -= ShopPrices.Swap;
            Slot temp = _wheel[a];
            _wheel[a] = _wheel[b];
            _wheel[b] = temp;
            return ShopError.None;
        }

        /// <summary>칸 수치 +1(레이즈 +2 → +3, 배율 ×2 → ×3). 하우스 몫·봉인·보호막·선불은 강화할 수 없다.</summary>
        public ShopError UpgradeSlot(int index)
        {
            if (!InShop) return ShopError.NotInShop;
            if (index < 0 || index >= _wheel.Count) return ShopError.InvalidSlot;
            int price = UpgradePrice(index);
            if (price < 0) return ShopError.CannotUpgrade;
            if (Cash < price) return ShopError.NotEnoughCash;
            Cash -= price;
            Slot slot = _wheel[index];
            int value = slot.Value + 1;
            _wheel[index] = new Slot($"{slot.Id}_up{value}", slot.Kind, value, UpgradedLabel(slot, value), slot.Trigger, slot.IsJackpot, slot.IsStolen);
            return ShopError.None;
        }

        /// <summary>HIJACK으로 덮어쓴 칸을 시작 룰렛의 그 자리 칸으로 되돌린다(급하게 덮어쓴 것을 무르기).</summary>
        public ShopError RevertSlot(int index)
        {
            if (!InShop) return ShopError.NotInShop;
            if (index < 0 || index >= _wheel.Count || index >= _startingWheel.Count) return ShopError.InvalidSlot;
            if (!_wheel[index].IsStolen) return ShopError.NotStolen;
            if (Cash < ShopPrices.Revert) return ShopError.NotEnoughCash;
            Cash -= ShopPrices.Revert;
            _wheel[index] = _startingWheel[index];
            return ShopError.None;
        }

        public ShopError BuyShopRelic()
        {
            if (!InShop) return ShopError.NotInShop;
            if (!ShopRelic.HasValue) return ShopError.SoldOut;
            if (Cash < ShopPrices.Relic) return ShopError.NotEnoughCash;
            Cash -= ShopPrices.Relic;
            _relics.Add(ShopRelic.Value);
            ShopRelic = null;
            return ShopError.None;
        }

        /// <summary>슬롯머신: 현금 bet을 걸고 당긴다. 현금이 모자라면 null.</summary>
        public SlotMachineResult PlaySlotMachine(int bet)
        {
            if (!InShop || bet <= 0 || Cash < bet) return null;
            Cash -= bet;
            SlotMachineResult result = SlotMachine.Spin(bet, _shopRng);
            Cash += result.Payout;
            LastSlotMachine = result;
            return result;
        }

        /// <summary>상점을 나가 다음 층으로.</summary>
        public void LeaveShop()
        {
            if (!InShop) throw new InvalidOperationException("상점층이 아닙니다.");
            FloorIndex++;
            if (FloorIndex >= FloorCount)
            {
                Outcome = RunOutcome.Escaped;
                return;
            }

            RollDoors();
        }

        private static string UpgradedLabel(Slot slot, int value)
        {
            switch (slot.Kind)
            {
                case SlotKind.Raise when !slot.IsJackpot: return $"레이즈 +{value}";
                case SlotKind.Insurance when !slot.IsJackpot: return $"보험 +{value}";
                case SlotKind.Dividend when slot.Trigger == SlotTrigger.Land && !slot.IsJackpot: return $"배당 +{value}";
                case SlotKind.Multiplier when !slot.IsJackpot: return $"배율 ×{value}";
                default:
                    string name = slot.Label;
                    int cut = name.IndexOf(" +", StringComparison.Ordinal);
                    if (cut < 0) cut = name.IndexOf(" ×", StringComparison.Ordinal);
                    if (cut >= 0) name = name.Substring(0, cut);
                    return slot.Kind == SlotKind.Multiplier ? $"{name} ×{value}" : $"{name} +{value}";
            }
        }
    }
}
