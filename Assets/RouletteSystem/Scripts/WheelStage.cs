using System.Collections;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 두 룰렛의 자리를 옮기는 연출 전용 컴포넌트. 차례인 룰렛은 아래 보드에 크게, 상대 룰렛은 주인 곁에 작게 두고,
    /// 칸을 고르거나 빼앗길 때는 둘을 나란히 펼친다. 언제 어느 자리로 갈지는 전투 컨트롤러가 정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelStage : MonoBehaviour
    {
        /// <summary>자리를 옮기는 시간(속도 1배 기준).</summary>
        public const float MoveDuration = 0.25f;

        [SerializeField] private RectTransform playerWheel;
        [SerializeField] private RectTransform dealerWheel;
        [SerializeField] private Vector2 boardPosition = new Vector2(0f, -175f);
        [SerializeField] private Vector2 playerSidePosition = new Vector2(-790f, 360f);
        [SerializeField] private Vector2 dealerSidePosition = new Vector2(790f, 360f);
        [SerializeField, Min(0.01f)] private float sideScale = 0.34f;
        [SerializeField] private float spreadOffset = 270f;
        [SerializeField, Min(0.01f)] private float spreadScale = 0.77f;

        private Coroutine _move;

        /// <summary>차례인 쪽을 보드로, 상대를 곁으로. speed가 0이면 바로 옮긴다.</summary>
        public void ShowActive(bool playerActive, float speed)
        {
            MoveTo(
                playerActive ? boardPosition : playerSidePosition, playerActive ? 1f : sideScale,
                playerActive ? dealerSidePosition : boardPosition, playerActive ? sideScale : 1f,
                speed);
        }

        /// <summary>칸을 고르거나 빼앗길 때 두 룰렛을 보드에 나란히 펼친다.</summary>
        public void ShowSideBySide(float speed)
        {
            MoveTo(
                boardPosition + Vector2.left * spreadOffset, spreadScale,
                boardPosition + Vector2.right * spreadOffset, spreadScale,
                speed);
        }

        private void MoveTo(Vector2 playerPosition, float playerScale, Vector2 dealerPosition, float dealerScale, float speed)
        {
            if (_move != null)
            {
                StopCoroutine(_move);
                _move = null;
            }

            if (speed <= 0f || !isActiveAndEnabled)
            {
                Place(playerWheel, playerPosition, playerScale);
                Place(dealerWheel, dealerPosition, dealerScale);
                return;
            }

            _move = StartCoroutine(Move(playerPosition, playerScale, dealerPosition, dealerScale, MoveDuration / speed));
        }

        private IEnumerator Move(Vector2 playerPosition, float playerScale, Vector2 dealerPosition, float dealerScale, float duration)
        {
            Vector2 playerFrom = playerWheel.anchoredPosition;
            Vector2 dealerFrom = dealerWheel.anchoredPosition;
            float playerScaleFrom = playerWheel.localScale.x;
            float dealerScaleFrom = dealerWheel.localScale.x;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                t = 1f - (1f - t) * (1f - t);
                Place(playerWheel, Vector2.Lerp(playerFrom, playerPosition, t), Mathf.Lerp(playerScaleFrom, playerScale, t));
                Place(dealerWheel, Vector2.Lerp(dealerFrom, dealerPosition, t), Mathf.Lerp(dealerScaleFrom, dealerScale, t));
                yield return null;
            }

            Place(playerWheel, playerPosition, playerScale);
            Place(dealerWheel, dealerPosition, dealerScale);
            _move = null;
        }

        private static void Place(RectTransform wheel, Vector2 position, float scale)
        {
            wheel.anchoredPosition = position;
            wheel.localScale = Vector3.one * scale;
        }
    }
}
