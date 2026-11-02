using System;
using System.Collections;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 캐릭터 그림의 코드 전투 모션(앞으로 나가기, 밀리기, 몰수 충격, HIJACK). 위치와 색만 잠깐 바꾸고 원래대로 돌린다.
    /// 연출 전용이다. 전투 규칙과 RNG를 쓰지 않으며 스프라이트 프레임은 NaturalBlinkAnimator가 맡는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class CharacterMotion : MonoBehaviour
    {
        /// <summary>도트 한 칸의 UI 크기(128px × 2배). 이 단위로 움직여 칸이 어긋나 보이지 않게 한다.</summary>
        private const float CellSize = 2f;

        private static readonly Color HitTint = new Color32(255, 120, 120, 255);
        private static readonly Color CutTint = new Color32(130, 130, 150, 255);
        private static readonly Color StrikeTint = new Color32(255, 235, 170, 255);

        [Tooltip("상대가 있는 쪽. 손님 +1(오른쪽), 딜러 -1(왼쪽)")]
        [SerializeField] private float facing = 1f;

        private RectTransform _rect;
        private UnityEngine.UI.Image _image;
        private Vector2 _origin;
        private Coroutine _routine;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _image = GetComponent<UnityEngine.UI.Image>();
            _origin = _rect.anchoredPosition;
        }

        private void OnDisable()
        {
            _routine = null;
            ResetPose();
        }

        /// <summary>CASH OUT을 지른 쪽: 상대 쪽으로 튀어 나갔다 돌아온다.</summary>
        public void Lunge(float speed = 1f) => Play(0.42f / speed, t => new Vector2(facing * 30f * Bump(t, 0.25f), 0f), Color.white);

        /// <summary>CASH OUT에 맞은 쪽: 피해가 클수록 멀리 밀리며 흔들린다.</summary>
        public void Recoil(int damage, float speed = 1f)
        {
            float k = Mathf.Clamp01(damage / 12f);
            float push = Mathf.Lerp(12f, 36f, k);
            float shake = Mathf.Lerp(3f, 8f, k);
            Play(0.5f / speed, t => new Vector2(-facing * push * Bump(t, 0.18f) + Shake(t, shake), 0f), HitTint);
        }

        /// <summary>보험으로 전부 막은 쪽: 살짝만 밀린다.</summary>
        public void Guard(float speed = 1f) => Play(0.3f / speed, t => new Vector2(-facing * 8f * Bump(t, 0.3f), 0f), Color.white);

        /// <summary>몰수에 걸린 쪽: 판돈이 증발하며 주저앉듯 내려갔다가 천천히 돌아온다.</summary>
        public void Slump(float speed = 1f) => Play(0.85f / speed, t => new Vector2(Shake(t, 5f), -16f * Bump(t, 0.2f)), CutTint);

        /// <summary>HIJACK을 하는 쪽: 크게 뛰어들어 칸을 낚아챈다.</summary>
        public void Strike(float speed = 1f) => Play(0.6f / speed, t => new Vector2(facing * 52f * Bump(t, 0.2f), 6f * Bump(t, 0.2f)), StrikeTint);

        /// <summary>HIJACK을 당한 쪽: 크게 밀리고 오래 흔들린다.</summary>
        public void Stagger(float speed = 1f) => Play(0.75f / speed, t => new Vector2(-facing * 40f * Bump(t, 0.15f) + Shake(t, 10f), 0f), HitTint);

        /// <summary>큰 연쇄에 움찔한다.</summary>
        public void Flinch(float speed = 1f) => Play(0.32f / speed, t => new Vector2(Shake(t, 9f), 0f), Color.white);

        private void Play(float duration, Func<float, Vector2> offset, Color tint)
        {
            if (!isActiveAndEnabled) return;
            if (_routine != null) StopCoroutine(_routine);
            ResetPose();
            _routine = StartCoroutine(Run(duration, offset, tint));
        }

        private IEnumerator Run(float duration, Func<float, Vector2> offset, Color tint)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                Vector2 o = offset(t);
                _rect.anchoredPosition = _origin + new Vector2(Mathf.Round(o.x / CellSize) * CellSize, Mathf.Round(o.y / CellSize) * CellSize);
                _image.color = Color.Lerp(tint, Color.white, t * t);
                yield return null;
            }

            ResetPose();
            _routine = null;
        }

        private void ResetPose()
        {
            if (_rect != null) _rect.anchoredPosition = _origin;
            if (_image != null) _image.color = Color.white;
        }

        /// <summary>0에서 peakAt까지 빠르게 1로 갔다가 끝에서 0으로 돌아오는 곡선.</summary>
        private static float Bump(float t, float peakAt)
        {
            if (t < peakAt) return 1f - (1f - t / peakAt) * (1f - t / peakAt);
            float back = (t - peakAt) / (1f - peakAt);
            return 1f - back * back;
        }

        /// <summary>끝으로 갈수록 잦아드는 좌우 흔들림. 난수를 쓰지 않아 항상 같게 보인다.</summary>
        private static float Shake(float t, float amplitude) => Mathf.Sin(t * 40f) * amplitude * (1f - t);
    }
}
