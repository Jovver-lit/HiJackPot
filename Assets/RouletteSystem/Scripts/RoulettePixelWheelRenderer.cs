using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 가변 세그먼트를 저해상도 Texture2D에 직접 그립니다. 텍스처 자체를 돌리거나 비정수로 늘리면 도트가 깨지므로,
    /// 룰렛의 회전각을 반영해 화면 격자에 매번 다시 그리고 해상도를 표시 크기에 맞춥니다.
    /// 칸 위치·착지 판정은 RouletteController가 맡고, 이 컴포넌트는 그리기만 합니다.
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
        private string _focusedSegmentId;
        private readonly HashSet<string> _focusGroupIds = new HashSet<string>(StringComparer.Ordinal);
        private bool _tintFocusGroup = true;

        /// <summary>한 도트의 UI 크기. 캐릭터 2배 표시와 같다(ArtStyleBible 1장).</summary>
        private const float UiUnitsPerDot = 2f;
        private const int ToneCount = 5;
        private const int ToneBase = 2;
        private const sbyte ToneOutside = -1;
        private const sbyte ToneBorder = -2;
        private const float LookupPerDegree = 2f;

        private float _renderedAngle;
        private float _cachedRadiusRatio = -1f;
        private sbyte[] _pixelTone;
        private float[] _pixelAngle;
        private float[] _pixelDistance;
        private int[] _segmentLookup;
        private Color32[] _segmentPalette;
        private Color32[] _segmentFret;

        [Header("Landing Focus")]
        [Tooltip("착지 고정 표시 중 착지 칸·연쇄 묶음이 아닌 칸의 밝기 배율.")]
        [SerializeField, Range(0f, 1f)] private float unfocusedBrightness = 0.38f;
        [Tooltip("착지 고정 표시 중 착지 칸이 강조색으로 섞이는 정도. 연쇄 묶음은 그 절반.")]
        [SerializeField, Range(0f, 1f)] private float focusBlend = 0.4f;

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
            _focusedSegmentId = null;
            _focusGroupIds.Clear();

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

        /// <summary>
        /// 착지 고정 표시: 착지 칸은 밝게, 연쇄 묶음은 조금 밝게, 나머지는 어둡게 그린다. 다음 회전 전까지 유지.
        /// HIJACK 선택처럼 고른 칸이 없을 때는 landedId 없이 groupIds만 밝게 남길 수 있다. 둘 다 비면 해제.
        /// </summary>
        public void SetLandingFocus(string landedId, IEnumerable<string> groupIds, bool tintGroup = true)
        {
            _focusedSegmentId = landedId;
            _tintFocusGroup = tintGroup;
            _focusGroupIds.Clear();
            if (groupIds != null)
            {
                foreach (string id in groupIds)
                {
                    if (id != null) _focusGroupIds.Add(id);
                }
            }

            if (_segments.Count > 0)
            {
                RenderPixels();
            }
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

        /// <summary>
        /// 룰렛이 돌아도 텍스처는 돌리지 않는다. 부모(Wheel)의 회전각만큼 칸 각도를 옮겨 화면 격자에 다시 그린다.
        /// 표시 크기가 바뀌면(WheelStage·바깥 링) 해상도를 맞춰 한 도트가 늘 같은 크기로 보이게 한다(ArtStyleBible 1장).
        /// </summary>
        private void LateUpdate()
        {
            if (_segments.Count == 0) return;
            RectTransform parent = rectTransform.parent as RectTransform;
            // 칸 글자를 자식으로 둔 옛 구조(RouletteDemo)는 함께 돌아야 하므로 그대로 둔다.
            if (parent == null || rectTransform.childCount > 0) return;

            rectTransform.localRotation = Quaternion.Inverse(parent.localRotation);
            bool dirty = false;
            float angle = parent.localEulerAngles.z;
            if (!Mathf.Approximately(angle, _renderedAngle))
            {
                _renderedAngle = angle;
                dirty = true;
            }

            int desired = DesiredResolution();
            if (desired != textureResolution)
            {
                textureResolution = desired;
                dirty = true;
            }

            if (dirty) RenderPixels();
        }

        private int DesiredResolution()
        {
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            if (root == null) return textureResolution;
            float canvasScale = Mathf.Max(0.0001f, root.transform.lossyScale.x);
            float displayed = rectTransform.rect.width * Mathf.Abs(rectTransform.lossyScale.x) / canvasScale;
            return Mathf.Clamp(Mathf.RoundToInt(displayed / UiUnitsPerDot), 32, 512);
        }

        private void RenderPixels()
        {
            ClampSettings();
            ConfigureRawImage();
            EnsureRuntimeTexture();
            EnsurePixelCache();
            BuildSegmentPalette();
            BuildSegmentLookup();

            float rotation = rectTransform.parent != null ? _renderedAngle : 0f;
            int segmentCount = _segments.Count;
            for (int i = 0; i < _pixels.Length; i++)
            {
                sbyte tone = _pixelTone[i];
                if (tone < 0)
                {
                    _pixels[i] = tone == ToneOutside ? new Color32(0, 0, 0, 0) : (Color32)outerBorderColor;
                    continue;
                }

                float angle = _pixelAngle[i] + rotation;
                angle -= Mathf.Floor(angle / 360f) * 360f;
                int segment = _segmentLookup[Mathf.Min(_segmentLookup.Length - 1, (int)(angle * LookupPerDegree))];
                while (segment < segmentCount - 1 && angle >= _endAngles[segment]) segment++;
                while (segment > 0 && angle < _startAngles[segment]) segment--;

                float toArc = Mathf.Deg2Rad * _pixelDistance[i];
                float fromStart = (angle - _startAngles[segment]) * toArc;
                float fromEnd = (_endAngles[segment] - angle) * toArc;
                // 칸막이는 어두운 선 하나로 두고, 양옆에 빛과 그림자 한 칸을 준다.
                if (fromStart < dividerThickness || fromEnd < dividerThickness)
                {
                    _pixels[i] = _segmentFret[segment];
                    continue;
                }

                // 칸 안은 한 색으로 칠한다. 음영 띠가 있으면 칸이 여러 갈래로 보여 어디에 멈췄는지 읽기 어렵다(사용자 피드백 2026-11-06).
                _pixels[i] = _segmentPalette[segment * ToneCount + tone];
            }

            _runtimeTexture.SetPixels32(_pixels);
            _runtimeTexture.Apply(false, false);
        }

        /// <summary>회전과 무관한 값(각도·거리·명암 단계)을 한 번만 계산해 둔다. 명암은 화면에 고정이라 돌아가는 판에 빛이 비치는 듯 보인다.</summary>
        private void EnsurePixelCache()
        {
            if (_pixelTone != null && _pixelTone.Length == _pixels.Length && Mathf.Approximately(_cachedRadiusRatio, _radiusRatio)) return;

            _cachedRadiusRatio = _radiusRatio;
            _pixelTone = new sbyte[_pixels.Length];
            _pixelAngle = new float[_pixels.Length];
            _pixelDistance = new float[_pixels.Length];
            float center = textureResolution * 0.5f;
            float radius = center * _radiusRatio;
            for (int y = 0; y < textureResolution; y++)
            {
                float localY = y + 0.5f - center;
                for (int x = 0; x < textureResolution; x++)
                {
                    int index = y * textureResolution + x;
                    float localX = x + 0.5f - center;
                    float distance = Mathf.Sqrt(localX * localX + localY * localY);
                    if (distance > radius)
                    {
                        _pixelTone[index] = ToneOutside;
                        continue;
                    }

                    if (distance >= radius - outerBorderThickness)
                    {
                        _pixelTone[index] = ToneBorder;
                        continue;
                    }

                    float angle = Mathf.Atan2(localX, localY) * Mathf.Rad2Deg;
                    _pixelAngle[index] = angle < 0f ? angle + 360f : angle;
                    _pixelDistance[index] = distance;

                    _pixelTone[index] = ToneBase;
                }
            }
        }

        /// <summary>칸마다 5단계 색을 미리 만든다. 강조·착지 표시는 이 색만 바꾸면 된다.</summary>
        private void BuildSegmentPalette()
        {
            int size = _segments.Count * ToneCount;
            if (_segmentPalette == null || _segmentPalette.Length != size) _segmentPalette = new Color32[size];
            if (_segmentFret == null || _segmentFret.Length != _segments.Count)
            {
                _segmentFret = new Color32[_segments.Count];
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                RouletteSegmentData data = _segments[i];
                bool highlighted = data != null && !string.IsNullOrEmpty(_highlightedSegmentId) && string.Equals(data.id, _highlightedSegmentId, StringComparison.Ordinal);
                Color fill = ResolveFillColor(data, highlighted);
                _segmentPalette[i * ToneCount + 0] = MultiplyRgb(fill, 1f - edgeShadeStrength * 2.6f);
                _segmentPalette[i * ToneCount + 1] = MultiplyRgb(fill, 1f - edgeShadeStrength * 1.4f);
                _segmentPalette[i * ToneCount + 2] = fill;
                _segmentPalette[i * ToneCount + 3] = Color.Lerp(fill, Color.white, patternStrength * 0.8f);
                _segmentPalette[i * ToneCount + 4] = Color.Lerp(fill, Color.white, patternStrength * 1.8f);
                _segmentFret[i] = highlighted ? highlightColor : new Color32(25, 18, 22, 255);
            }
        }

        private void BuildSegmentLookup()
        {
            int entries = (int)(360f * LookupPerDegree);
            if (_segmentLookup == null || _segmentLookup.Length != entries) _segmentLookup = new int[entries];
            int segment = 0;
            for (int i = 0; i < entries; i++)
            {
                float angle = i / LookupPerDegree;
                while (segment < _segments.Count - 1 && angle >= _endAngles[segment]) segment++;
                _segmentLookup[i] = segment;
            }
        }

        private Color ResolveFillColor(RouletteSegmentData data, bool highlighted)
        {
            Color fill = data != null ? data.color : Color.magenta;

            if (data != null && data.isDisabled)
            {
                fill = Color.Lerp(fill, Color.gray, 0.7f);
                fill.a = 1f;
            }

            if (highlighted)
            {
                fill = Color.Lerp(fill, highlightColor, highlightBlend);
            }

            if ((_focusedSegmentId != null || _focusGroupIds.Count > 0) && data != null)
            {
                if (string.Equals(data.id, _focusedSegmentId, StringComparison.Ordinal))
                {
                    fill = Color.Lerp(fill, highlightColor, focusBlend);
                }
                else if (_focusGroupIds.Contains(data.id))
                {
                    if (_tintFocusGroup) fill = Color.Lerp(fill, highlightColor, focusBlend * 0.5f);
                }
                else
                {
                    fill = MultiplyRgb(fill, unfocusedBrightness);
                }
            }

            return fill;
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
    }
}
