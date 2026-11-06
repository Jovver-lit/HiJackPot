using UnityEngine;
using UnityEngine.UI;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 전투 띠 배경의 불빛(벽등·슬롯머신 전구·샹들리에)을 번갈아 켠다. 배경에 움직임을 주는 연출 전용이며 게임 규칙은 모른다.
    /// 전투 속도와 무관하게 실제 시간으로 돈다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class StageLights : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.05f)] private float frameSeconds = 0.45f;

        private Image _image;
        private int _frame;
        private float _nextAt;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Update()
        {
            if (frames.Length == 0 || Time.unscaledTime < _nextAt) return;
            _nextAt = Time.unscaledTime + frameSeconds;
            _frame = (_frame + 1) % frames.Length;
            _image.sprite = frames[_frame];
        }
    }
}
