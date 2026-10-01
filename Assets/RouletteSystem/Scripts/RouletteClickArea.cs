using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 룰렛 위 클릭을 칸 순서로 바꿔 알린다(HIJACK·JACKPOT 배치 때 룰렛에서 직접 칸 고르기).
    /// 투명한 Image와 함께 룰렛 루트에 붙고, 고르는 동안에만 켠다. 무엇을 할지는 구독자(전투 컨트롤러)가 정한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RouletteClickArea : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RouletteController roulette;

        /// <summary>클릭된 칸의 순서(0부터). 칸이 아닌 곳은 알리지 않는다.</summary>
        public event Action<int> SegmentClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (roulette == null)
            {
                return;
            }

            int index = roulette.GetSegmentIndexAtScreenPoint(eventData.position, eventData.pressEventCamera);
            if (index >= 0)
            {
                SegmentClicked?.Invoke(index);
            }
        }
    }
}
