using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 유물 아이콘 한 칸의 마우스 반응: 올리면 설명 카드를 띄우고, 내리면 닫고, 누르면 전체 유물 설명을 연다.
    /// 무엇을 보여 줄지는 구독자(전투 컨트롤러)가 정한다. 아이콘 순서(Index)만 알린다.
    /// </summary>
    public sealed class RelicIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public int Index { get; set; }

        public event Action<int> Entered;
        public event Action<int> Exited;
        public event Action<int> Clicked;

        public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke(Index);
        public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke(Index);
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(Index);
    }
}
