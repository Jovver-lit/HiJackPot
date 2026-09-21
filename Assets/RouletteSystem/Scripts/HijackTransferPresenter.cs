using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 확정된 HIJACK 결과를 화면 위에서 재생하는 연출 전용 컴포넌트입니다.
    /// 실제 룰렛 데이터 변경은 도착 프레임에 전달받은 콜백으로 실행합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HijackTransferPresenter : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private RectTransform effectLayer;
        [SerializeField] private RectTransform tokenRoot;
        [SerializeField] private CanvasGroup tokenCanvasGroup;
        [SerializeField] private UnityEngine.UI.Image tokenBackground;
        [SerializeField] private UnityEngine.UI.Image tokenFrame;
        [SerializeField] private TMP_Text tokenValueText;
        [SerializeField] private TMP_Text tokenOwnerText;
        [SerializeField] private RectTransform impactFlash;
        [SerializeField] private UnityEngine.UI.Image impactFlashImage;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float popDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float travelDuration = 0.48f;
        [SerializeField, Min(0.01f)] private float impactDuration = 0.24f;
        [SerializeField, Min(0f)] private float arcHeight = 180f;

        private static readonly Color Gold = new Color32(244, 190, 62, 255);

        public IEnumerator PlayHijack(
            RouletteController sourceRoulette,
            int sourceIndex,
            RouletteController destinationRoulette,
            int destinationIndex,
            RouletteSegmentData stolenSegment,
            Action applyAtImpact)
        {
            if (effectLayer == null
                || tokenRoot == null
                || tokenCanvasGroup == null
                || sourceRoulette == null
                || destinationRoulette == null
                || stolenSegment == null)
            {
                applyAtImpact?.Invoke();
                yield break;
            }

            Vector2 start = WorldToLayerPoint(GetSegmentWorldPosition(sourceRoulette, sourceIndex));
            Vector2 end = WorldToLayerPoint(GetSegmentWorldPosition(destinationRoulette, destinationIndex));
            Vector2 control = (start + end) * 0.5f + Vector2.up * arcHeight;

            tokenBackground.color = Color.Lerp(stolenSegment.color, Color.black, 0.18f);
            tokenFrame.color = Gold;
            tokenValueText.text = stolenSegment.displayText;
            tokenOwnerText.text = "DEALER  →  PLAYER";
            tokenRoot.anchoredPosition = start;
            tokenRoot.localRotation = Quaternion.identity;
            tokenRoot.localScale = Vector3.one * 0.45f;
            tokenCanvasGroup.alpha = 0f;
            tokenRoot.gameObject.SetActive(true);
            impactFlash?.gameObject.SetActive(false);

            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += GetAnimationDeltaTime();
                float t = Mathf.Clamp01(elapsed / popDuration);
                float eased = EaseOutBack(t);
                tokenRoot.anchoredPosition = Vector2.LerpUnclamped(start, start + Vector2.up * 42f, eased);
                tokenRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.45f, 1.08f, eased);
                tokenCanvasGroup.alpha = t;
                yield return null;
            }

            Vector2 travelStart = tokenRoot.anchoredPosition;
            elapsed = 0f;
            while (elapsed < travelDuration)
            {
                elapsed += GetAnimationDeltaTime();
                float t = Mathf.Clamp01(elapsed / travelDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                tokenRoot.anchoredPosition = QuadraticBezier(travelStart, control, end, eased);
                tokenRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -12f);
                tokenRoot.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.86f, eased);
                yield return null;
            }

            tokenRoot.anchoredPosition = end;
            applyAtImpact?.Invoke();

            RectTransform destinationWheel = destinationRoulette.Wheel;
            Vector3 destinationScale = destinationWheel != null ? destinationWheel.localScale : Vector3.one;
            if (impactFlash != null)
            {
                impactFlash.anchoredPosition = end;
                impactFlash.localScale = Vector3.one * 0.35f;
                impactFlash.gameObject.SetActive(true);
            }

            elapsed = 0f;
            while (elapsed < impactDuration)
            {
                elapsed += GetAnimationDeltaTime();
                float t = Mathf.Clamp01(elapsed / impactDuration);
                float punch = Mathf.Sin(t * Mathf.PI);
                tokenRoot.localScale = Vector3.one * Mathf.Lerp(1.18f, 0.55f, t);
                tokenCanvasGroup.alpha = 1f - t;

                if (impactFlash != null)
                {
                    impactFlash.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.35f, t);
                }

                if (impactFlashImage != null)
                {
                    Color flashColor = Gold;
                    flashColor.a = 1f - t;
                    impactFlashImage.color = flashColor;
                }

                if (destinationWheel != null)
                {
                    destinationWheel.localScale = destinationScale * (1f - punch * 0.08f);
                }

                yield return null;
            }

            if (destinationWheel != null)
            {
                destinationWheel.localScale = destinationScale;
            }

            tokenRoot.gameObject.SetActive(false);
            impactFlash?.gameObject.SetActive(false);
        }

        private Vector3 GetSegmentWorldPosition(RouletteController roulette, int segmentIndex)
        {
            RectTransform wheel = roulette.Wheel;
            if (wheel == null
                || !roulette.TryGetSegmentAngles(segmentIndex, out _, out _, out float centerAngle, out _))
            {
                return roulette.transform.position;
            }

            float radius = Mathf.Min(wheel.rect.width, wheel.rect.height) * 0.29f;
            float radians = centerAngle * Mathf.Deg2Rad;
            Vector3 localPoint = new Vector3(
                Mathf.Sin(radians) * radius,
                Mathf.Cos(radians) * radius,
                0f);
            return wheel.TransformPoint(localPoint);
        }

        private Vector2 WorldToLayerPoint(Vector3 worldPoint)
        {
            Vector3 local = effectLayer.InverseTransformPoint(worldPoint);
            return new Vector2(local.x, local.y);
        }

        private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start
                   + 2f * inverse * t * control
                   + t * t * end;
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted
                   + overshoot * shifted * shifted;
        }

        private static float GetAnimationDeltaTime()
        {
            // 에디터 포커스 전환이나 순간적인 프레임 지연으로 연출 전체가 한 프레임에
            // 건너뛰지 않도록 시각 연출에 사용할 최대 프레임 시간을 제한합니다.
            return Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        }

        private void OnDisable()
        {
            tokenRoot?.gameObject.SetActive(false);
            impactFlash?.gameObject.SetActive(false);
        }
    }
}
