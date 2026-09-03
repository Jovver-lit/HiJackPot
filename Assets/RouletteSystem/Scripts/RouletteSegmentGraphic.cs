using UnityEngine;
using UnityEngine.UI;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// Unity UI의 VertexHelper로 단일 부채꼴을 생성합니다.
    /// 텍스처 한 장에 결과를 굽지 않으므로 각도와 개수가 런타임에 바뀔 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RouletteSegmentGraphic : Graphic
    {
        private const float MinimumSpan = 0.0001f;

        private RouletteSegmentData _data;
        private float _startAngle;
        private float _endAngle;
        private float _radius;
        private float _borderThickness;
        private float _maximumArcStepDegrees;
        private Color _fillColor;
        private Color _borderColor;
        private Color _highlightColor;
        private bool _isHighlighted;
        private bool _snapVerticesToPixels;
        private int _labelFontSize;
        private float _iconRadiusRatio;
        private float _labelRadiusRatio;
        private Vector2 _iconSize;
        private Vector2 _labelSize;

        private Image _iconImage;
        private Text _labelText;

        public RouletteSegmentData Data => _data;
        public float StartAngle => _startAngle;
        public float EndAngle => _endAngle;
        public float CenterAngle => (_startAngle + _endAngle) * 0.5f;
        public float AngleSize => _endAngle - _startAngle;

        /// <summary>
        /// Controller가 계산한 각도와 표시 옵션을 이 Graphic에 적용합니다.
        /// 각도는 12시=0도, 시계 방향 증가 규칙을 사용합니다.
        /// </summary>
        public void Configure(
            RouletteSegmentData data,
            float startAngle,
            float endAngle,
            float radius,
            float borderThickness,
            Color borderColor,
            Color highlightColor,
            float maximumArcStepDegrees,
            bool snapVerticesToPixels,
            Font labelFont,
            int labelFontSize,
            float iconRadiusRatio,
            float labelRadiusRatio,
            Vector2 iconSize,
            Vector2 labelSize)
        {
            _data = data;
            _startAngle = startAngle;
            _endAngle = endAngle;
            _radius = Mathf.Max(0f, radius);
            _borderThickness = Mathf.Max(0f, borderThickness);
            _maximumArcStepDegrees = Mathf.Clamp(maximumArcStepDegrees, 0.5f, 30f);
            _fillColor = data != null ? data.color : Color.magenta;
            _borderColor = borderColor;
            _highlightColor = highlightColor;
            _snapVerticesToPixels = snapVerticesToPixels;
            _labelFontSize = labelFontSize;
            _iconRadiusRatio = iconRadiusRatio;
            _labelRadiusRatio = labelRadiusRatio;
            _iconSize = iconSize;
            _labelSize = labelSize;

            raycastTarget = false;
            color = Color.white;

            EnsureContentObjects(labelFont);
            ConfigureIcon(iconRadiusRatio, iconSize);
            ConfigureLabel(labelFontSize, labelRadiusRatio, labelSize);
            SetVerticesDirty();
        }

        /// <summary>
        /// 추가/삭제/합체 애니메이터가 매 프레임 각도와 반지름을 보간할 수 있는 확장 API입니다.
        /// </summary>
        public void SetGeometry(float startAngle, float endAngle, float radius)
        {
            _startAngle = startAngle;
            _endAngle = Mathf.Max(startAngle, endAngle);
            _radius = Mathf.Max(0f, radius);

            if (_iconImage != null)
            {
                ConfigureIcon(_iconRadiusRatio, _iconSize);
            }

            if (_labelText != null)
            {
                ConfigureLabel(_labelFontSize, _labelRadiusRatio, _labelSize);
            }

            SetVerticesDirty();
        }

        /// <summary>
        /// 합체 시 경계선 제거 같은 연출에 사용할 수 있습니다.
        /// </summary>
        public void SetBorderThickness(float thickness)
        {
            _borderThickness = Mathf.Max(0f, thickness);
            SetVerticesDirty();
        }

        /// <summary>
        /// 선택 강조는 메시 색상만 갱신하므로 룰렛 전체를 재생성하지 않습니다.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
            {
                return;
            }

            _isHighlighted = highlighted;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            float span = _endAngle - _startAngle;
            if (_radius <= 0f || span <= MinimumSpan)
            {
                return;
            }

            Vector2 center = rectTransform.rect.center;
            int subdivisions = Mathf.Clamp(
                Mathf.CeilToInt(span / _maximumArcStepDegrees),
                1,
                360);

            Color32 fill = GetCurrentFillColor();

            // 배경 부채꼴. 작은 삼각형 여러 개로 원호를 근사합니다.
            for (int i = 0; i < subdivisions; i++)
            {
                float t0 = i / (float)subdivisions;
                float t1 = (i + 1) / (float)subdivisions;
                float angle0 = Mathf.Lerp(_startAngle, _endAngle, t0);
                float angle1 = Mathf.Lerp(_startAngle, _endAngle, t1);

                Vector2 point0 = center + AngleToDirection(angle0) * _radius;
                Vector2 point1 = center + AngleToDirection(angle1) * _radius;
                AddTriangle(vertexHelper, Snap(center), Snap(point0), Snap(point1), fill);
            }

            if (_borderThickness <= 0f)
            {
                return;
            }

            Color32 border = _isHighlighted ? (Color32)_highlightColor : (Color32)_borderColor;

            // 두 방사형 경계선.
            AddLine(
                vertexHelper,
                center,
                center + AngleToDirection(_startAngle) * _radius,
                _borderThickness,
                border);
            AddLine(
                vertexHelper,
                center,
                center + AngleToDirection(_endAngle) * _radius,
                _borderThickness,
                border);

            // 외곽 원호 경계선.
            float innerRadius = Mathf.Max(0f, _radius - _borderThickness);
            for (int i = 0; i < subdivisions; i++)
            {
                float t0 = i / (float)subdivisions;
                float t1 = (i + 1) / (float)subdivisions;
                float angle0 = Mathf.Lerp(_startAngle, _endAngle, t0);
                float angle1 = Mathf.Lerp(_startAngle, _endAngle, t1);

                Vector2 outer0 = center + AngleToDirection(angle0) * _radius;
                Vector2 outer1 = center + AngleToDirection(angle1) * _radius;
                Vector2 inner1 = center + AngleToDirection(angle1) * innerRadius;
                Vector2 inner0 = center + AngleToDirection(angle0) * innerRadius;
                AddQuad(vertexHelper, Snap(outer0), Snap(outer1), Snap(inner1), Snap(inner0), border);
            }
        }

        private Color32 GetCurrentFillColor()
        {
            Color current = _fillColor;

            if (_data != null && _data.isDisabled)
            {
                current = Color.Lerp(current, Color.gray, 0.7f);
                current.a *= 0.55f;
            }

            if (_isHighlighted)
            {
                current = Color.Lerp(current, _highlightColor, 0.55f);
            }

            return current;
        }

        private void EnsureContentObjects(Font labelFont)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(transform, false);
                iconTransform = iconObject.transform;
            }

            _iconImage = iconTransform.GetComponent<Image>();
            _iconImage.raycastTarget = false;
            _iconImage.preserveAspect = true;

            Transform labelTransform = transform.Find("Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelObject.transform.SetParent(transform, false);
                labelTransform = labelObject.transform;
            }

            _labelText = labelTransform.GetComponent<Text>();
            _labelText.raycastTarget = false;
            _labelText.alignment = TextAnchor.MiddleCenter;
            _labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _labelText.verticalOverflow = VerticalWrapMode.Overflow;
            _labelText.color = Color.white;
            _labelText.font = labelFont != null ? labelFont : GetFallbackFont();
        }

        private void ConfigureIcon(float radiusRatio, Vector2 requestedSize)
        {
            Sprite sprite = _data != null ? _data.icon : null;
            _iconImage.sprite = sprite;
            _iconImage.gameObject.SetActive(sprite != null);

            if (sprite == null)
            {
                return;
            }

            // 런타임에서도 픽셀 스프라이트의 Point 필터를 보장합니다.
            if (sprite.texture != null)
            {
                sprite.texture.filterMode = FilterMode.Point;
            }

            float contentRadius = _radius * Mathf.Clamp01(radiusRatio);
            float availableWidth = GetAvailableChordWidth(contentRadius) * 0.7f;
            Vector2 actualSize = requestedSize;
            actualSize.x = Mathf.Min(actualSize.x, Mathf.Max(8f, availableWidth));
            actualSize.y = Mathf.Min(actualSize.y, Mathf.Max(8f, availableWidth));

            RectTransform iconRect = _iconImage.rectTransform;
            SetCenteredAnchors(iconRect);
            iconRect.anchoredPosition = AngleToDirection(CenterAngle) * contentRadius;
            iconRect.sizeDelta = actualSize;
            iconRect.localRotation = Quaternion.identity;
        }

        private void ConfigureLabel(int fontSize, float radiusRatio, Vector2 requestedSize)
        {
            string label = _data != null ? _data.displayText : string.Empty;
            _labelText.text = label ?? string.Empty;
            _labelText.fontSize = Mathf.Max(1, fontSize);
            _labelText.gameObject.SetActive(!string.IsNullOrEmpty(_labelText.text));

            float contentRadius = _radius * Mathf.Clamp01(radiusRatio);
            float availableWidth = GetAvailableChordWidth(contentRadius) * 0.85f;
            Vector2 actualSize = requestedSize;
            actualSize.x = Mathf.Min(actualSize.x, Mathf.Max(16f, availableWidth));

            RectTransform labelRect = _labelText.rectTransform;
            SetCenteredAnchors(labelRect);
            labelRect.anchoredPosition = AngleToDirection(CenterAngle) * contentRadius;
            labelRect.sizeDelta = actualSize;
            labelRect.localRotation = Quaternion.identity;
        }

        private float GetAvailableChordWidth(float contentRadius)
        {
            float halfSpanRadians = Mathf.Min(AngleSize * Mathf.Deg2Rad * 0.5f, Mathf.PI * 0.5f);
            return 2f * contentRadius * Mathf.Sin(halfSpanRadians);
        }

        private static void SetCenteredAnchors(RectTransform target)
        {
            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// 12시=0도, 시계 방향 증가 각도를 UI 좌표 방향으로 바꿉니다.
        /// </summary>
        private static Vector2 AngleToDirection(float clockwiseAngle)
        {
            float radians = clockwiseAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        private Vector2 Snap(Vector2 point)
        {
            if (!_snapVerticesToPixels)
            {
                return point;
            }

            return new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
        }

        private void AddLine(VertexHelper vertexHelper, Vector2 from, Vector2 to, float thickness, Color32 colorValue)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
            AddQuad(
                vertexHelper,
                Snap(from + perpendicular),
                Snap(to + perpendicular),
                Snap(to - perpendicular),
                Snap(from - perpendicular),
                colorValue);
        }

        private static void AddTriangle(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Color32 colorValue)
        {
            int startIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(a, colorValue, Vector2.zero);
            vertexHelper.AddVert(b, colorValue, Vector2.zero);
            vertexHelper.AddVert(c, colorValue, Vector2.zero);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        }

        private static void AddQuad(
            VertexHelper vertexHelper,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            Color32 colorValue)
        {
            int startIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(a, colorValue, Vector2.zero);
            vertexHelper.AddVert(b, colorValue, Vector2.zero);
            vertexHelper.AddVert(c, colorValue, Vector2.zero);
            vertexHelper.AddVert(d, colorValue, Vector2.zero);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        private static Font GetFallbackFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }
    }
}
