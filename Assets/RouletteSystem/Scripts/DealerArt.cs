using System;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 딜러 한 명의 스프라이트 묶음(대기·깜빡임). 딜러 이름에 nameKey가 들어 있으면 이 그림을 쓴다. 연출 전용 데이터이며 전투 규칙은 맡지 않는다.
    /// </summary>
    [Serializable]
    public sealed class DealerArt
    {
        public string nameKey;
        public Sprite[] idleFrames;
        public Sprite[] blinkFrames;
    }
}
