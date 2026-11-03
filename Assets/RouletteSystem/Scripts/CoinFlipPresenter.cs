using System.Collections;
using TMPro;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 라운드 시작의 선공 동전을 보여 주는 연출 전용 컴포넌트. 동전이 떠서 돌다 선공 쪽 면으로 떨어지고, 그쪽 룰렛으로 날아간다.
    /// 누가 선공인지는 코어(PotBattle.StartRound)가 이미 정한 뒤에 받기만 하며, 전투 RNG를 쓰지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoinFlipPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform coin;
        [SerializeField] private UnityEngine.UI.Image face;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color playerColor = new Color32(57, 190, 198, 255);
        [SerializeField] private Color dealerColor = new Color32(220, 72, 78, 255);
        [SerializeField, Min(0f)] private float holdDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float travelDuration = 0.2f;
        [SerializeField, Min(0f)] private float tossHeight = 60f;

        /// <summary>동전이 떠 있는 시간. 결과 글자를 내려앉는 순간에 맞추는 데 쓴다.</summary>
        public const float DefaultTossDuration = 0.6f;

        /// <summary>반 바퀴 횟수. 짝수여야 시작 면과 같은 면으로 떨어진다.</summary>
        private const int HalfTurns = 6;

        /// <param name="tossed">false면 던지지 않고 선공 면만 보여 준다(선불 착지로 선공이 확정된 라운드).</param>
        /// <param name="target">선공 쪽 룰렛. 동전이 마지막에 날아가는 곳.</param>
        /// <param name="speed">속도 설정 배율.</param>
        public IEnumerator Play(bool playerFirst, string dealerFaceText, bool tossed, Transform target, float speed)
        {
            gameObject.SetActive(true);
            coin.localScale = Vector3.one;
            ShowFace(playerFirst, dealerFaceText);

            if (tossed)
            {
                float duration = DefaultTossDuration / speed;
                for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = elapsed / duration;
                    float angle = HalfTurns * Mathf.PI * (1f - (1f - t) * (1f - t));
                    bool flipped = Mathf.FloorToInt((angle + Mathf.PI * 0.5f) / Mathf.PI) % 2 == 1;
                    ShowFace(flipped ? !playerFirst : playerFirst, dealerFaceText);
                    coin.localScale = new Vector3(1f, Mathf.Abs(Mathf.Cos(angle)), 1f);
                    coin.anchoredPosition = new Vector2(0f, tossHeight * 4f * t * (1f - t));
                    yield return null;
                }

                ShowFace(playerFirst, dealerFaceText);
                coin.localScale = Vector3.one;
                coin.anchoredPosition = Vector2.zero;
            }

            yield return WaitUnscaled(holdDuration / speed);

            Vector3 from = coin.position;
            float travel = travelDuration / speed;
            for (float elapsed = 0f; elapsed < travel; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / travel;
                coin.position = Vector3.Lerp(from, target.position, t * t);
                coin.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, t);
                yield return null;
            }

            coin.anchoredPosition = Vector2.zero;
            coin.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void ShowFace(bool playerFace, string dealerFaceText)
        {
            face.color = playerFace ? playerColor : dealerColor;
            label.text = playerFace ? "손님" : dealerFaceText;
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }
    }
}
