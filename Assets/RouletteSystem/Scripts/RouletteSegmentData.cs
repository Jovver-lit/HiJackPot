using System;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 하나의 룰렛 칸을 나타내는 순수 데이터입니다.
    /// MonoBehaviour/ScriptableObject가 아니므로 플레이 중 자유롭게 복제하고 변경할 수 있습니다.
    /// 전투 효과 실행은 이 클래스가 아니라 결과를 구독하는 전투 시스템이 담당합니다.
    /// </summary>
    [Serializable]
    public class RouletteSegmentData
    {
        public string id;
        public RouletteSegmentType type;
        public int value;

        [Min(0.001f)]
        public float weight = 1f;

        public Color color = Color.white;
        public Sprite icon;
        public string displayText;
        public bool isSpecial;
        public bool isDisabled;

        public RouletteSegmentData()
        {
        }

        public RouletteSegmentData(
            string id,
            RouletteSegmentType type,
            int value,
            float weight,
            Color color,
            Sprite icon = null,
            string displayText = null,
            bool isSpecial = false,
            bool isDisabled = false)
        {
            this.id = id;
            this.type = type;
            this.value = value;
            this.weight = weight;
            this.color = color;
            this.icon = icon;
            this.displayText = displayText;
            this.isSpecial = isSpecial;
            this.isDisabled = isDisabled;
        }

        /// <summary>
        /// 런타임 룰렛이 원본 프리셋 데이터를 직접 수정하지 않게 할 때 사용합니다.
        /// Sprite는 Unity 에셋 참조이므로 얕은 복사가 올바른 동작입니다.
        /// </summary>
        public RouletteSegmentData Clone()
        {
            return new RouletteSegmentData(
                id,
                type,
                value,
                weight,
                color,
                icon,
                displayText,
                isSpecial,
                isDisabled);
        }

        /// <summary>
        /// 테스트 로그와 디버그 UI에서 읽기 쉬운 결과명을 반환합니다.
        /// 실제 칸에 표시되는 문자열은 displayText를 사용합니다.
        /// </summary>
        public string GetDebugLabel()
        {
            switch (type)
            {
                case RouletteSegmentType.Damage:
                    return "Damage " + value;
                case RouletteSegmentType.Heal:
                    return "Heal " + value;
                case RouletteSegmentType.Critical:
                    return value > 0 ? "Critical " + value : "Critical";
                case RouletteSegmentType.Poison:
                    return "Poison " + value;
                case RouletteSegmentType.Multiplier:
                    return "Multiplier x" + value;
                case RouletteSegmentType.Mystery:
                    return "Mystery";
                case RouletteSegmentType.Jackpot:
                    return value > 0 ? "Jackpot " + value : "Jackpot";
                case RouletteSegmentType.Hijack:
                    return value > 0 ? "Hijack " + value : "Hijack";
                case RouletteSegmentType.Custom:
                    return string.IsNullOrWhiteSpace(displayText) ? "Custom" : displayText;
                default:
                    return type.ToString();
            }
        }
    }
}
