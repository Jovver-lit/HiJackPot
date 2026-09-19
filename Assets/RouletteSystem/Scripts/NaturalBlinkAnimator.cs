using System.Collections;
using UnityEngine;

namespace RouletteLike.Roulette
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class NaturalBlinkAnimator : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image targetImage;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] blinkFrames;
        [SerializeField, Min(0.05f)] private float idleFrameDuration = 0.55f;
        [SerializeField] private Vector2 blinkIntervalRange = new Vector2(2.6f, 5.2f);
        [SerializeField, Min(0.02f)] private float blinkFrameDuration = 0.075f;
        [SerializeField, Range(0f, 1f)] private float doubleBlinkChance = 0.12f;
        [SerializeField] private bool useUnscaledTime = true;

        private Coroutine _animationRoutine;

        public string CurrentFrameName =>
            targetImage != null && targetImage.sprite != null ? targetImage.sprite.name : string.Empty;

        private void Reset()
        {
            targetImage = GetComponent<UnityEngine.UI.Image>();
        }

        private void OnEnable()
        {
            if (_animationRoutine == null)
            {
                _animationRoutine = StartCoroutine(Animate());
            }
        }

        private void OnDisable()
        {
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }
        }

        private IEnumerator Animate()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<UnityEngine.UI.Image>();
            }

            if (idleFrames == null || idleFrames.Length == 0)
            {
                yield break;
            }

            int idleIndex = 0;
            targetImage.sprite = idleFrames[idleIndex];

            while (true)
            {
                float blinkCountdown = Random.Range(
                    Mathf.Min(blinkIntervalRange.x, blinkIntervalRange.y),
                    Mathf.Max(blinkIntervalRange.x, blinkIntervalRange.y));
                float idleCountdown = idleFrameDuration;

                while (blinkCountdown > 0f)
                {
                    float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    blinkCountdown -= deltaTime;
                    idleCountdown -= deltaTime;

                    if (idleCountdown <= 0f)
                    {
                        idleIndex = (idleIndex + 1) % idleFrames.Length;
                        targetImage.sprite = idleFrames[idleIndex];
                        idleCountdown += idleFrameDuration;
                    }

                    yield return null;
                }

                yield return PlayBlink(idleIndex);
                if (Random.value < doubleBlinkChance)
                {
                    yield return Wait(0.14f);
                    yield return PlayBlink(idleIndex);
                }
            }
        }

        private IEnumerator PlayBlink(int idleIndex)
        {
            if (blinkFrames != null)
            {
                float matchedFrameDuration = blinkFrames.Length > 0
                    ? idleFrameDuration * idleFrames.Length / blinkFrames.Length
                    : blinkFrameDuration;

                for (int i = 0; i < blinkFrames.Length; i++)
                {
                    if (blinkFrames[i] != null)
                    {
                        targetImage.sprite = blinkFrames[i];
                    }

                    yield return Wait(Mathf.Max(0.02f, matchedFrameDuration));
                }
            }

            targetImage.sprite = idleFrames[Mathf.Clamp(idleIndex, 0, idleFrames.Length - 1)];
        }

        private IEnumerator Wait(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }
    }
}
