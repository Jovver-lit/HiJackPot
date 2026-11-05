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
        private static readonly Color32 BrassDark = new Color32(138, 106, 96, 255);
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

        private static Color32[] Paint(int size, float innerRatio)
        {
            Color32[] pixels = new Color32[size * size];
            float center = size * 0.5f;
            float outer = center - 1f;
            float inner = outer * innerRatio;
            float studRadius = (outer - 4f + inner + 3f) * 0.5f;
            int studs = size >= 200 ? 24 : 12;
            int largeStud = size >= 200 ? 3 : 2;
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
                        pixels[index] = Clear;
                        continue;
                    }

                    float light = (dy - dx) * 0.7071f / Mathf.Max(1f, distance);
                    if (distance > outer - 1f) pixels[index] = Ink;
                    else if (distance > outer - 3f) pixels[index] = light > 0.55f ? BrassLight : light > -0.35f ? Brass : BrassDark;
                    else if (distance > outer - 4f) pixels[index] = Wood1;
                    else if (distance > inner + 3f) pixels[index] = light > 0.5f ? Wood3 : light > -0.5f ? Wood2 : Wood1;
                    else if (distance > inner + 1f) pixels[index] = light > -0.3f ? BrassDark : Wood2;
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
                        if (radius == 1) pixels[py * size + px] = BrassDark;
                        else pixels[py * size + px] = manhattan == radius ? Wood1 : ox - oy < 0 ? BrassLight : Brass;
                    }
                }
            }

            return pixels;
        }
    }
}
