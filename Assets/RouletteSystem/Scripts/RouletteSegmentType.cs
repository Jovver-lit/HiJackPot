namespace RouletteLike.Roulette
{
    /// <summary>
    /// 룰렛 칸이 전투 시스템에 전달하는 결과의 기본 분류입니다.
    /// Custom은 프로젝트 고유 효과를 별도 효과 처리기에서 해석할 때 사용합니다.
    /// </summary>
    public enum RouletteSegmentType
    {
        Damage,
        Heal,
        Critical,
        Poison,
        Multiplier,
        Mystery,
        Jackpot,
        Hijack,
        Custom
    }
}
