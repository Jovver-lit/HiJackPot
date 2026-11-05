using UnityEngine;
using UnityEngine.UI;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 룰렛의 황동·나무 테두리를 표시 크기에 맞는 해상도로 직접 그린다. 룰렛이 작아져도(WheelStage) 도트 크기가 그대로다.
    /// 회전하지 않는 고정 장식이며, 룰렛 칸과 착지는 맡지 않는다. 색은 ArtStyleBible 공용 팔레트다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PixelWheelRim : RawImage
    {
        private const float UiUnitsPerDot = 2f;
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Ink = new Color32(10, 14, 22, 255);
        private static readonly Color32 BrassLight = new Color32(255, 224, 138, 255);
        private static readonly Color32 Brass = new Color32(240, 180, 60, 255);
        private static readonly Color32 BrassMid = new Color32(196, 140, 40, 255);
        private static readonly Color32 BrassDark = new Color32(138, 92, 30, 255);
        private static readonly Color32 White = new Color32(251, 251, 252, 255);
        private static readonly Color32 Wood1 = new Color32(51, 36, 36, 255);
        private static readonly Color32 Wood2 = new Color32(74, 54, 50, 255);
        private static readonly Color32 Wood3 = new Color32(94, 69, 64, 255);

        [SerializeField, Range(0.5f, 0.99f)] private float innerRatio = 0.92f;

        private Texture2D _texture;
        private int _resolution;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            Redraw();
        }

        private void LateUpdate()
        {
            Redraw();
        }

        protected override void OnDestroy()
        {
            if (_texture != null)
            {
                if (Application.isPlaying) Destroy(_texture);
                else DestroyImmediate(_texture);
            }

            base.OnDestroy();
        }

        private void Redraw()
        {
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            if (root == null) return;
            float canvasScale = Mathf.Max(0.0001f, root.transform.lossyScale.x);
            float displayed = rectTransform.rect.width * Mathf.Abs(rectTransform.lossyScale.x) / canvasScale;
            int resolution = Mathf.Clamp(Mathf.RoundToInt(displayed / UiUnitsPerDot), 16, 512);
            if (resolution == _resolution && _texture != null) return;
            _resolution = resolution;

            if (_texture != null)
            {
                if (Application.isPlaying) Destroy(_texture);
                else DestroyImmediate(_texture);
            }

            _texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, false)
            {
                name = $"PixelWheelRim_{resolution}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            _texture.SetPixels32(Paint(resolution, innerRatio));
            _texture.Apply(false, false);
            texture = _texture;
            uvRect = new Rect(0f, 0f, 1f, 1f);
            color = Color.white;
        }

        private static Color32 Metal(float angleDegrees, float bias)
        {
            float a = (angleDegrees - 315f) * Mathf.Deg2Rad;
            float t = 0.5f + 0.34f * Mathf.Cos(2f * a) + 0.12f * Mathf.Cos(a) + bias;
            if (t > 0.86f) return White;
            if (t > 0.68f) return BrassLight;
            if (t > 0.46f) return Brass;
            if (t > 0.26f) return BrassMid;
            return BrassDark;
        }

        /// <summary>바깥부터: 윤곽, 넓은 황동 띠, 어두운 홈, 나무 띠(결과 징), 안쪽 황동 입술, 윤곽. 오른쪽 아래에 그림자 점묘.</summary>
        private static Color32[] Paint(int size, float innerRatio)
        {
            Color32[] pixels = new Color32[size * size];
            float center = size * 0.5f;
            float outer = center - 3f;
            float inner = outer * innerRatio;
            bool large = size >= 200;
            float brassWidth = large ? 5f : 2f;
            float studRadius = (outer - brassWidth - 1f + inner + 3f) * 0.5f;
            int studs = large ? 24 : 12;
            int largeStud = large ? 3 : 1;
            for (int y = 0; y < size; y++)
            {
                float dy = y + 0.5f - center;
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    int index = y * size + x;
                    if (distance > outer)
                    {
                        float shadow = Mathf.Sqrt((dx - 2f) * (dx - 2f) + (dy + 2f) * (dy + 2f));
                        pixels[index] = shadow <= outer + 0.5f && (x + y) % 2 == 0 ? Ink : Clear;
                        continue;
                    }

                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    float light = (dy - dx) * 0.7071f / Mathf.Max(1f, distance);
                    if (distance > outer - 1f) pixels[index] = Ink;
                    else if (distance > outer - brassWidth)
                    {
                        // 황동 띠 가운데 한 줄을 어둡게 눌러 두 겹으로 보이게 한다.
                        bool groove = large && Mathf.Abs(distance - (outer - brassWidth * 0.55f)) < 0.5f;
                        pixels[index] = groove ? Metal(angle + 180f, -0.2f) : Metal(angle, 0f);
                    }
                    else if (distance > outer - brassWidth - 1f) pixels[index] = Ink;
                    else if (distance > inner + 3f)
                    {
                        // 결이 도는 나무: 반지름마다 한 단계씩 어긋난다.
                        int grain = (Mathf.FloorToInt(distance) + Mathf.FloorToInt(angle / 9f)) % 3;
                        float tone = light + (grain == 0 ? 0.22f : grain == 1 ? 0f : -0.18f);
                        pixels[index] = tone > 0.5f ? Wood3 : tone > -0.4f ? Wood2 : Wood1;
                    }
                    else if (distance > inner + 2f) pixels[index] = Ink;
                    else if (distance > inner) pixels[index] = Metal(angle + 180f, 0f);
                    else pixels[index] = Ink;
                }
            }

            for (int i = 0; i < studs; i++)
            {
                float angle = i * Mathf.PI * 2f / studs;
                int cx = Mathf.FloorToInt(center + Mathf.Sin(angle) * studRadius);
                int cy = Mathf.FloorToInt(center + Mathf.Cos(angle) * studRadius);
                int radius = i % 2 == 0 ? largeStud : 1;
                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int manhattan = Mathf.Abs(ox) + Mathf.Abs(oy);
                        int px = cx + ox, py = cy + oy;
                        if (manhattan > radius || px < 0 || py < 0 || px >= size || py >= size) continue;
                        if (radius == 1) pixels[py * size + px] = manhattan == 0 ? BrassLight : BrassMid;
                        else pixels[py * size + px] = manhattan == radius ? Ink : ox - oy < 0 ? BrassLight : Brass;
                    }
                }
            }

            return pixels;
        }
    }
}
