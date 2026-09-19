using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 누르는 동안 회전 강도를 왕복 충전하고, 놓는 순간 전투에 강도를 전달합니다.
    /// 강도는 착지 구역만 바꾸며 최종 칸은 RouletteSpinController의 오차가 결정합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoldToSpinInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RabbitBattlePrototype battle;
        [SerializeField] private UnityEngine.UI.Image fillImage;
        [SerializeField] private TMP_Text powerText;
        [SerializeField, Min(0.2f)] private float secondsToFullPower = 1.35f;
        [SerializeField, Range(0f, 1f)] private float minimumPower = 0.08f;

        private bool _isHolding;
        private float _holdTime;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (battle == null || !battle.CanChooseSpinPower)
            {
                return;
            }

            _isHolding = true;
            _holdTime = 0f;
            SetVisual(minimumPower);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isHolding && eventData.pointerPress == gameObject)
            {
                Release();
            }
        }

        private void Update()
        {
            if (!_isHolding)
            {
                return;
            }

            if (battle == null || !battle.CanChooseSpinPower)
            {
                CancelHold();
                return;
            }

            _holdTime += Time.unscaledDeltaTime;
            float phase = Mathf.PingPong(_holdTime / secondsToFullPower, 1f);
            float power = Mathf.Lerp(minimumPower, 1f, phase);
            SetVisual(power);
            battle.PreviewSpinPower(power);
        }

        public void ResetInput()
        {
            CancelHold();
            SetVisual(minimumPower);
        }

        private void Release()
        {
            if (!_isHolding)
            {
                return;
            }

            float power = GetCurrentPower();
            _isHolding = false;
            battle?.ThrowRoulette(power);
        }

        private void CancelHold()
        {
            _isHolding = false;
            _holdTime = 0f;
        }

        private float GetCurrentPower()
        {
            float phase = Mathf.PingPong(_holdTime / secondsToFullPower, 1f);
            return Mathf.Lerp(minimumPower, 1f, phase);
        }

        private void SetVisual(float power)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = power;
            }

            if (powerText != null)
            {
                powerText.text = $"회전 강도  {Mathf.RoundToInt(power * 100f)}%";
            }
        }
    }
}
