using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 가변 세그먼트를 저해상도 Texture2D에 직접 래스터라이즈합니다.
    /// 생성된 텍스처를 Point 필터로 확대하므로 부채꼴 외곽, 경계선, 패턴이
    /// 프레임과 같은 실제 픽셀 단위로 표시됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoulettePixelWheelRenderer : RawImage
    {
        [Header("Pixel Output")]
        [Tooltip("420px Wheel을 2배 도트로 표시할 때 210을 사용합니다.")]
        [SerializeField, Range(32, 512)] private int textureResolution = 210;
        [SerializeField, Range(0, 6)] private int dividerThickness = 1;
        [SerializeField, Range(0, 8)] private int outerBorderThickness = 2;
        [SerializeField] private Color borderColor = new Color32(45, 35, 65, 255);
        [SerializeField] private Color outerBorderColor = new Color32(30, 23, 45, 255);

        [Header("Pixel Palette")]
        [SerializeField] private bool useTypePatterns = true;
        [SerializeField, Range(0f, 0.5f)] private float patternStrength = 0.16f;
        [SerializeField, Range(0f, 0.5f)] private float edgeShadeStrength = 0.16f;
        [SerializeField] private Color highlightColor = new Color32(255, 239, 92, 255);
        [SerializeField, Range(0f, 1f)] private float highlightBlend = 0.55f;

        private readonly List<RouletteSegmentData> _segments = new List<RouletteSegmentData>();
        private readonly List<float> _startAngles = new List<float>();
        private readonly List<float> _endAngles = new List<float>();

        private Texture2D _runtimeTexture;
        private Color32[] _pixels;
        private float _radiusRatio = 1f;
        private string _highlightedSegmentId;

        public int TextureResolution => textureResolution;
        public Texture2D RuntimeTexture => _runtimeTexture;

        protected override void Awake()
        {
            base.Awake();
            ConfigureRawImage();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ConfigureRawImage();

            if (_segments.Count > 0)
            {
                RenderPixels();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ClampSettings();
            ConfigureRawImage();

            if (Application.isPlaying && _segments.Count > 0)
            {
                RenderPixels();
            }
        }
#endif

        protected override void OnDestroy()
        {
            ReleaseRuntimeTexture();
            base.OnDestroy();
        }

        /// <summary>
        /// Controller가 계산한 세그먼트 각도를 픽셀 텍스처로 다시 만듭니다.
        /// radiusRatio는 DynamicSegments 반지름 대비 실제 색상 원의 비율입니다.
        /// </summary>
        public void Rebuild(
            IReadOnlyList<RouletteSegmentData> segments,
            IReadOnlyList<float> startAngles,
            IReadOnlyList<float> endAngles,
            float radiusRatio)
        {
            if (segments == null || startAngles == null || endAngles == null)
            {
                throw new ArgumentNullException("Pixel roulette input lists cannot be null.");
            }

            if (segments.Count != startAngles.Count || segments.Count != endAngles.Count)
            {
                throw new ArgumentException("Pixel roulette data and angle counts must match.");
            }

            _segments.Clear();
            _startAngles.Clear();
            _endAngles.Clear();

            for (int i = 0; i < segments.Count; i++)
            {
                _segments.Add(segments[i]);
                _startAngles.Add(startAngles[i]);
                _endAngles.Add(endAngles[i]);
            }

            _radiusRatio = Mathf.Clamp(radiusRatio, 0.05f, 1f);
            _highlightedSegmentId = null;

            if (_segments.Count == 0)
            {
                ClearWheel();
                return;
            }

            RenderPixels();
        }

        /// <summary>
        /// 선택된 칸의 팔레트를 바꾸어 픽셀 단위로 점멸시킵니다.
        /// </summary>
        public void SetHighlightedSegment(string segmentId, bool highlighted)
        {
            string nextId = highlighted ? segmentId : null;
            if (string.Equals(_highlightedSegmentId, nextId, StringComparison.Ordinal))
            {
                return;
            }

            _highlightedSegmentId = nextId;
            if (_segments.Count > 0)
            {
                RenderPixels();
            }
        }

        public void ClearHighlight()
        {
            SetHighlightedSegment(null, false);
        }

        public void ClearWheel()
        {
            _segments.Clear();
            _startAngles.Clear();
            _endAngles.Clear();
            _highlightedSegmentId = null;

            EnsureRuntimeTexture();
            Array.Clear(_pixels, 0, _pixels.Length);
            _runtimeTexture.SetPixels32(_pixels);
            _runtimeTexture.Apply(false, false);
        }

        private void RenderPixels()
        {
            ClampSettings();
            ConfigureRawImage();
            EnsureRuntimeTexture();

            float center = textureResolution * 0.5f;
            float radius = center * _radiusRatio;
            float radiusSquared = radius * radius;

            for (int y = 0; y < textureResolution; y++)
            {
                float localY = y + 0.5f - center;

                for (int x = 0; x < textureResolution; x++)
                {
                    int pixelIndex = y * textureResolution + x;
                    float localX = x + 0.5f - center;
                    float distanceSquared = localX * localX + localY * localY;

                    if (distanceSquared > radiusSquared)
                    {
                        _pixels[pixelIndex] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float distance = Mathf.Sqrt(distanceSquared);
                    float angle = Mathf.Atan2(localX, localY) * Mathf.Rad2Deg;
                    if (angle < 0f)
                    {
                        angle += 360f;
                    }

                    int segmentIndex = FindSegmentIndex(angle);
                    RouletteSegmentData data = _segments[segmentIndex];

                    bool isOuterBorder = outerBorderThickness > 0
                                         && distance >= radius - outerBorderThickness;
                    if (isOuterBorder)
                    {
                        _pixels[pixelIndex] = outerBorderColor;
                        continue;
                    }

                    bool highlighted = data != null
                                       && !string.IsNullOrEmpty(_highlightedSegmentId)
                                       && string.Equals(
                                           data.id,
                                           _highlightedSegmentId,
                                           StringComparison.Ordinal);

                    bool isDivider = IsDividerPixel(segmentIndex, angle, distance);
                    if (isDivider)
                    {
                        _pixels[pixelIndex] = highlighted ? highlightColor : borderColor;
                        continue;
                    }

                    Color fill = ResolveFillColor(data, highlighted);
                    fill = ApplyRadialShade(fill, distance / Mathf.Max(1f, radius));

                    if (useTypePatterns && data != null)
                    {
                        int pattern = GetPatternValue(data.type, x, y, segmentIndex);
                        if (pattern < 0)
                        {
                            fill = MultiplyRgb(fill, 1f - patternStrength);
                        }
                        else if (pattern > 0)
                        {
                            fill = Color.Lerp(fill, Color.white, patternStrength * 0.7f);
                        }
                    }

                    _pixels[pixelIndex] = fill;
                }
            }

            _runtimeTexture.SetPixels32(_pixels);
            _runtimeTexture.Apply(false, false);
        }

        private int FindSegmentIndex(float angle)
        {
            for (int i = 0; i < _endAngles.Count; i++)
            {
                if (angle >= _startAngles[i] && angle < _endAngles[i])
                {
                    return i;
                }
            }

            return Mathf.Max(0, _segments.Count - 1);
        }

        private bool IsDividerPixel(int segmentIndex, float angle, float distance)
        {
            if (dividerThickness <= 0 || distance < 1f)
            {
                return false;
            }

            // 호 길이 약 1px에 대응하는 각도를 계산해 가변 각도에서도 일정한 픽셀 두께를 유지합니다.
            float halfThicknessDegrees = Mathf.Rad2Deg
                                         * (dividerThickness * 0.5f)
                                         / Mathf.Max(1f, distance);
            float distanceFromStart = angle - _startAngles[segmentIndex];
            float distanceFromEnd = _endAngles[segmentIndex] - angle;
            return distanceFromStart <= halfThicknessDegrees
                   || distanceFromEnd <= halfThicknessDegrees;
        }

        private Color ResolveFillColor(RouletteSegmentData data, bool highlighted)
        {
            Color fill = data != null ? data.color : Color.magenta;

            if (data != null && data.isDisabled)
            {
                fill = Color.Lerp(fill, Color.gray, 0.7f);
                fill.a *= 0.55f;
            }

            if (highlighted)
            {
                fill = Color.Lerp(fill, highlightColor, highlightBlend);
            }

            return fill;
        }

        private Color ApplyRadialShade(Color source, float normalizedRadius)
        {
            float shade = 0f;

            // 중앙 캡 주변과 외곽에 한 단계 어두운 색을 넣어 평평한 단색 면을 피합니다.
            if (normalizedRadius < 0.24f)
            {
                shade = (1f - normalizedRadius / 0.24f) * edgeShadeStrength;
            }
            else if (normalizedRadius > 0.82f)
            {
                shade = ((normalizedRadius - 0.82f) / 0.18f) * edgeShadeStrength;
            }

            return MultiplyRgb(source, 1f - Mathf.Clamp01(shade));
        }

        /// <summary>
        /// -1은 어두운 도트, 0은 기본색, 1은 밝은 도트입니다.
        /// 별도 텍스처 없이도 결과 타입별 실루엣을 구분할 수 있는 작은 반복 패턴입니다.
        /// </summary>
        private static int GetPatternValue(
            RouletteSegmentType type,
            int x,
            int y,
            int segmentIndex)
        {
            int offset = segmentIndex * 3;
            int localX = PositiveMod(x + offset, 8);
            int localY = PositiveMod(y + offset, 8);

            switch (type)
            {
                case RouletteSegmentType.Damage:
                    return PositiveMod(localX + localY, 8) == 0 ? -1 : 0;

                case RouletteSegmentType.Heal:
                    return (localX == 3 && localY >= 1 && localY <= 5)
                           || (localY == 3 && localX >= 1 && localX <= 5)
                        ? 1
                        : 0;

                case RouletteSegmentType.Critical:
                case RouletteSegmentType.Jackpot:
                    if ((localX == 3 && (localY == 1 || localY == 5))
                        || (localY == 3 && (localX == 1 || localX == 5))
                        || (localX == 3 && localY == 3))
                    {
                        return 1;
                    }

                    return 0;

                case RouletteSegmentType.Poison:
                    return (localX == 2 && localY == 2)
                           || (localX == 6 && localY == 5)
                        ? -1
                        : 0;

                case RouletteSegmentType.Multiplier:
                    return localX == localY || localX + localY == 7 ? -1 : 0;

                case RouletteSegmentType.Mystery:
                    return ((localX / 2) + (localY / 2)) % 2 == 0 ? -1 : 0;

                case RouletteSegmentType.Hijack:
                    if (localY == 1 || localY == 6)
                    {
                        return PositiveMod(x + segmentIndex, 3) == 0 ? 1 : -1;
                    }

                    return 0;

                case RouletteSegmentType.Custom:
                    return (localX == 1 || localY == 1) ? -1 : 0;

                default:
                    return PositiveMod(localX + localY, 6) == 0 ? -1 : 0;
            }
        }

        private void EnsureRuntimeTexture()
        {
            int requiredPixelCount = textureResolution * textureResolution;
            bool needsTexture = _runtimeTexture == null
                                || _runtimeTexture.width != textureResolution
                                || _runtimeTexture.height != textureResolution;

            if (needsTexture)
            {
                ReleaseRuntimeTexture();
                _runtimeTexture = new Texture2D(
                    textureResolution,
                    textureResolution,
                    TextureFormat.RGBA32,
                    false,
                    false)
                {
                    name = $"RoulettePixelWheel_{textureResolution}",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    hideFlags = HideFlags.DontSave
                };
                texture = _runtimeTexture;
            }

            if (_pixels == null || _pixels.Length != requiredPixelCount)
            {
                _pixels = new Color32[requiredPixelCount];
            }
        }

        private void ReleaseRuntimeTexture()
        {
            if (_runtimeTexture == null)
            {
                return;
            }

            if (texture == _runtimeTexture)
            {
                texture = null;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeTexture);
            }
            else
            {
                DestroyImmediate(_runtimeTexture);
            }

            _runtimeTexture = null;
            _pixels = null;
        }

        private void ConfigureRawImage()
        {
            raycastTarget = false;
            color = Color.white;
            uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        private void ClampSettings()
        {
            textureResolution = Mathf.Clamp(textureResolution, 32, 512);
            dividerThickness = Mathf.Clamp(dividerThickness, 0, 6);
            outerBorderThickness = Mathf.Clamp(outerBorderThickness, 0, 8);
            patternStrength = Mathf.Clamp(patternStrength, 0f, 0.5f);
            edgeShadeStrength = Mathf.Clamp(edgeShadeStrength, 0f, 0.5f);
            highlightBlend = Mathf.Clamp01(highlightBlend);
        }

        private static Color MultiplyRgb(Color source, float multiplier)
        {
            source.r *= multiplier;
            source.g *= multiplier;
            source.b *= multiplier;
            return source;
        }

        private static int PositiveMod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
