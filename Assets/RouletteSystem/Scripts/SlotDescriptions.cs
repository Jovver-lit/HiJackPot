using System.Collections.Generic;
using RouletteLike.Battle;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 칸 효과를 플레이어가 읽을 문장으로 바꾼다. 문 카드·HIJACK 선택·룰렛 아래 특수 칸 안내가 같은 설명을 쓴다.
    /// 규칙 자체는 Core가 계산하며, 여기는 표시 문구만 맡는다.
    /// </summary>
    public static class SlotDescriptions
    {
        /// <summary>칸 하나의 효과 한 줄. 예: "[라운드 시작] 매 라운드 칩 +1 · 착지하면 즉시 +1". 특수 칸은 착지해도 효과가 즉시 발동한다.</summary>
        public static string Describe(Slot slot)
        {
            switch (slot.Kind)
            {
                case SlotKind.Raise: return $"[착지] 판돈 +{slot.Value}, 이웃 레이즈와 합산";
                case SlotKind.Multiplier: return $"[착지] 판돈 ×{slot.Value}";
                case SlotKind.Insurance: return $"[착지] 보험 +{slot.Value}, 상대 CASH OUT 피해 −{slot.Value}";
                case SlotKind.Dividend:
                    return slot.Trigger == SlotTrigger.RoundStart
                        ? $"[라운드 시작] 매 라운드 칩 +{slot.Value} · 착지하면 즉시 +{slot.Value}"
                        : $"[착지] 판돈을 거치지 않고 칩 +{slot.Value}";
                case SlotKind.HouseCut: return "[착지] 판돈 전부 증발, 턴 종료. HIJACK 불가";
                case SlotKind.CutShield: return "[착지] 이번 턴 몰수 1회 무효";
                case SlotKind.MinimumPayout: return $"[CASH OUT] 피해 최소 {slot.Value} · 착지하면 이번 턴 보험 무시";
                case SlotKind.Initiative: return "[라운드 시작] 선공 확률 75% · 착지하면 다음 라운드 선공 확정";
                case SlotKind.Sealed: return "봉인된 칸. 아무 효과 없음";
                default: return slot.Label;
            }
        }

        /// <summary>일반 칸(레이즈·배율·보험·배당·몰수)이 아닌 특수 칸인지. 룰렛 아래 안내에 모아 보여 준다.</summary>
        public static bool IsSpecial(Slot slot)
        {
            return slot.IsJackpot
                   || slot.Trigger != SlotTrigger.Land
                   || slot.Kind == SlotKind.CutShield
                   || slot.Kind == SlotKind.MinimumPayout
                   || slot.Kind == SlotKind.Initiative;
        }

        /// <summary>룰렛의 특수 칸들을 "이름: 효과" 줄로 모은다. 없으면 빈 문자열.</summary>
        public static string DescribeSpecials(IReadOnlyList<Slot> wheel, string prefix)
        {
            List<string> lines = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            foreach (Slot slot in wheel)
            {
                if (!IsSpecial(slot) || !seen.Add(slot.Label)) continue;
                lines.Add($"{prefix}{slot.Label}{(slot.IsJackpot ? " [JP]" : "")}: {Describe(slot)}");
            }

            return string.Join("\n", lines);
        }
    }
}
