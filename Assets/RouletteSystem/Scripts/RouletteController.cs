using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 룰렛 데이터, 각도 계산, 동적 UI 생성, 런타임 칸 변형을 관리합니다.
    /// 플레이어/적 룰렛 모두 이 컴포넌트를 그대로 재사용할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RouletteController : MonoBehaviour
    {
        public delegate RouletteSegmentData SegmentMergeRule(
            RouletteSegmentData first,
            RouletteSegmentData second);

        [Header("Hierarchy References")]
        [SerializeField] private RectTransform wheel;
        [SerializeField] private RectTransform dynamicSegmentRoot;
        [SerializeField] private RoulettePixelWheelRenderer pixelWheelRenderer;

        [Header("Runtime Segment Data")]
        [SerializeField] private List<RouletteSegmentData> segments = new List<RouletteSegmentData>();
        [SerializeField, Min(2)] private int minimumSegmentCount = 2;
        [SerializeField, Min(2)] private int maximumSegmentCount = 20;
        [SerializeField] private bool rebuildOnAwake = true;

        [Header("Wheel Geometry")]
        [Tooltip("0이면 DynamicSegments RectTransform의 짧은 변 길이를 기준으로 자동 계산합니다.")]
        [SerializeField, Min(0f)] private float radiusOverride;
        [SerializeField, Min(0f)] private float radiusPadding = 2f;
        [SerializeField, Range(0.5f, 20f)] private float maximumArcStepDegrees = 4f;
        [SerializeField] private bool snapMeshVerticesToPixels = true;

        [Header("Segment Visuals")]
        [SerializeField, Min(0f)] private float borderThickness = 2f;
        [SerializeField] private Color borderColor = new Color32(45, 35, 65, 255);
        [SerializeField] private Color highlightColor = new Color32(255, 239, 92, 255);
        [SerializeField] private Font labelFont;
        [SerializeField, Min(1)] private int labelFontSize = 16;
        [SerializeField, Range(0f, 1f)] private float iconRadiusRatio = 0.56f;
        [SerializeField, Range(0f, 1f)] private float labelRadiusRatio = 0.79f;
        [SerializeField] private Vector2 iconSize = new Vector2(32f, 32f);
        [SerializeField] private Vector2 labelSize = new Vector2(64f, 24f);

        private readonly List<float> _startAngles = new List<float>();
        private readonly List<float> _endAngles = new List<float>();
        private readonly List<RouletteSegmentGraphic> _graphics = new List<RouletteSegmentGraphic>();
        private readonly Dictionary<string, RouletteSegmentGraphic> _graphicById =
            new Dictionary<string, RouletteSegmentGraphic>(StringComparer.Ordinal);

        private Coroutine _highlightRoutine;
        private bool _isRebuilding;

        public IReadOnlyList<RouletteSegmentData> Segments => segments;
        public RectTransform Wheel => wheel;
        public RoulettePixelWheelRenderer PixelWheelRenderer => pixelWheelRenderer;
        public Font LabelFont => labelFont;
        public int Count => segments.Count;
        public bool HasValidWheel => wheel != null && dynamicSegmentRoot != null && segments.Count >= minimumSegmentCount;

        // 이 이벤트들은 애니메이션 코디네이터나 저장/전투 로그 시스템의 확장 지점입니다.
        public event Action<RouletteSegmentData> SegmentAdded;
        public event Action<RouletteSegmentData> SegmentRemoving;
        public event Action<RouletteSegmentData, float, float> SegmentWeightChanged;
        public event Action<RouletteSegmentData, RouletteSegmentData> SegmentsMerging;
        public event Action<RouletteSegmentData> SegmentsMerged;
        public event Action WheelRebuilt;

        private void Reset()
        {
            ResolveHierarchyReferences();
        }

        private void Awake()
        {
            ResolveHierarchyReferences();

            if (rebuildOnAwake)
            {
                RebuildWheel();
            }
        }

        private void OnValidate()
        {
            minimumSegmentCount = Mathf.Max(2, minimumSegmentCount);
            maximumSegmentCount = Mathf.Max(minimumSegmentCount, maximumSegmentCount);
            maximumArcStepDegrees = Mathf.Clamp(maximumArcStepDegrees, 0.5f, 20f);
            borderThickness = Mathf.Max(0f, borderThickness);
        }

        /// <summary>
        /// 전체 데이터를 교체합니다. 적 룰렛 프리셋을 주입할 때도 사용할 수 있습니다.
        /// cloneData가 true면 호출자의 원본 데이터가 플레이 중 변형되지 않습니다.
        /// </summary>
        public bool SetSegments(IList<RouletteSegmentData> newSegments, bool cloneData = true)
        {
            if (newSegments == null || newSegments.Count < minimumSegmentCount || newSegments.Count > maximumSegmentCount)
            {
                Debug.LogWarning(
                    $"Roulette requires {minimumSegmentCount} to {maximumSegmentCount} segments.",
                    this);
                return false;
            }

            List<RouletteSegmentData> replacement = new List<RouletteSegmentData>(newSegments.Count);
            for (int i = 0; i < newSegments.Count; i++)
            {
                if (newSegments[i] == null)
                {
                    Debug.LogWarning("Roulette segment data cannot be null.", this);
                    return false;
                }

                replacement.Add(cloneData ? newSegments[i].Clone() : newSegments[i]);
            }

            segments = replacement;
            RebuildWheel();
            return true;
        }

        /// <summary>
        /// weight 비율에 따라 모든 각도를 다시 계산하고 UI 메시를 재생성합니다.
        /// </summary>
        public void RebuildWheel()
        {
            if (_isRebuilding)
            {
                return;
            }

            ResolveHierarchyReferences();
            if (wheel == null || dynamicSegmentRoot == null)
            {
                Debug.LogError("RouletteController requires Wheel and DynamicSegments RectTransforms.", this);
                return;
            }

            _isRebuilding = true;
            EnsureValidRuntimeData();
            ClearGeneratedGraphics();
            _startAngles.Clear();
            _endAngles.Clear();

            if (segments.Count == 0)
            {
                if (pixelWheelRenderer != null)
                {
                    pixelWheelRenderer.ClearWheel();
                }

                _isRebuilding = false;
                WheelRebuilt?.Invoke();
                return;
            }

            float totalWeight = 0f;
            for (int i = 0; i < segments.Count; i++)
            {
                totalWeight += segments[i].weight;
            }

            if (totalWeight <= Mathf.Epsilon)
            {
                Debug.LogError("Roulette total weight must be greater than zero.", this);
                _isRebuilding = false;
                return;
            }

            // LayoutGroup/CanvasScaler가 있는 Scene에서도 실제 Rect 크기를 기준으로 반지름을 얻습니다.
            Canvas.ForceUpdateCanvases();
            float radius = GetResolvedRadius();
            float currentAngle = 0f;

            for (int i = 0; i < segments.Count; i++)
            {
                RouletteSegmentData data = segments[i];
                float angleSize = 360f * (data.weight / totalWeight);
                float startAngle = currentAngle;
                float endAngle = i == segments.Count - 1 ? 360f : currentAngle + angleSize;

                _startAngles.Add(startAngle);
                _endAngles.Add(endAngle);
                currentAngle = endAngle;
            }

            bool usePixelRenderer = pixelWheelRenderer != null && pixelWheelRenderer.enabled;
            if (usePixelRenderer)
            {
                pixelWheelRenderer.Rebuild(
                    segments,
                    _startAngles,
                    _endAngles,
                    GetPixelRadiusRatio(radius));
            }

            // 픽셀 렌더러 사용 시에도 칸별 Graphic은 아이콘/텍스트 배치 컨테이너로 유지합니다.
            // 렌더러가 없는 기존 Prefab은 자동으로 메시 부채꼴 폴백을 사용합니다.
            for (int i = 0; i < segments.Count; i++)
            {
                CreateSegmentGraphic(
                    i,
                    segments[i],
                    _startAngles[i],
                    _endAngles[i],
                    radius,
                    !usePixelRenderer);
            }

            _isRebuilding = false;
            WheelRebuilt?.Invoke();
        }

        /// <summary>
        /// 리스트 끝에 새 칸을 추가합니다.
        /// </summary>
        public bool AddSegment(RouletteSegmentData newSegment)
        {
            return InsertSegment(segments.Count, newSegment);
        }

        /// <summary>
        /// 지정한 인덱스에 새 칸을 삽입합니다.
        /// </summary>
        public bool InsertSegment(int index, RouletteSegmentData newSegment)
        {
            if (newSegment == null || newSegment.weight <= 0f)
            {
                Debug.LogWarning("A new roulette segment must be non-null and have a positive weight.", this);
                return false;
            }

            if (segments.Count >= maximumSegmentCount)
            {
                Debug.LogWarning($"Roulette already has the maximum of {maximumSegmentCount} segments.", this);
                return false;
            }

            index = Mathf.Clamp(index, 0, segments.Count);
            EnsureUniqueId(newSegment);
            segments.Insert(index, newSegment);
            RebuildWheel();
            SegmentAdded?.Invoke(newSegment);
            return true;
        }

        public bool RemoveSegment(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            int index = segments.FindIndex(segment => segment != null && segment.id == id);
            return RemoveSegment(index);
        }

        public bool RemoveSegment(int index)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            if (segments.Count <= minimumSegmentCount)
            {
                Debug.LogWarning($"Roulette must keep at least {minimumSegmentCount} segments.", this);
                return false;
            }

            RouletteSegmentData removed = segments[index];
            SegmentRemoving?.Invoke(removed);
            segments.RemoveAt(index);
            RebuildWheel();
            return true;
        }

        public bool ChangeSegmentWeight(string id, float newWeight)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            int index = segments.FindIndex(segment => segment != null && segment.id == id);
            return ChangeSegmentWeight(index, newWeight);
        }

        public bool ChangeSegmentWeight(int index, float newWeight)
        {
            if (!IsValidIndex(index) || newWeight <= 0f)
            {
                Debug.LogWarning("Segment weight must be greater than zero.", this);
                return false;
            }

            RouletteSegmentData data = segments[index];
            float previousWeight = data.weight;
            data.weight = newWeight;
            RebuildWheel();
            SegmentWeightChanged?.Invoke(data, previousWeight, newWeight);
            return true;
        }

        /// <summary>
        /// 기본 합체 규칙으로 두 인접 칸을 합칩니다.
        /// 첫 칸과 마지막 칸도 원형 룰렛에서는 인접한 것으로 취급합니다.
        /// </summary>
        public bool MergeSegments(int indexA, int indexB)
        {
            RouletteSegmentData ignored;
            return MergeSegments(indexA, indexB, null, out ignored);
        }

        public bool MergeSegments(int indexA, int indexB, SegmentMergeRule mergeRule)
        {
            RouletteSegmentData ignored;
            return MergeSegments(indexA, indexB, mergeRule, out ignored);
        }

        /// <summary>
        /// 외부 합체 규칙을 받아 두 인접 칸을 하나로 바꿉니다.
        /// 규칙이 반환한 데이터와 무관하게 최종 weight는 두 원본 weight의 합으로 보장됩니다.
        /// </summary>
        public bool MergeSegments(
            int indexA,
            int indexB,
            SegmentMergeRule mergeRule,
            out RouletteSegmentData mergedResult)
        {
            mergedResult = null;

            if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
            {
                return false;
            }

            if (segments.Count <= minimumSegmentCount)
            {
                Debug.LogWarning("Merging would leave fewer than the minimum segment count.", this);
                return false;
            }

            int low = Mathf.Min(indexA, indexB);
            int high = Mathf.Max(indexA, indexB);
            bool wrapsAcrossZero = low == 0 && high == segments.Count - 1;
            bool directlyAdjacent = high - low == 1;
            if (!directlyAdjacent && !wrapsAcrossZero)
            {
                Debug.LogWarning("Only adjacent roulette segments can be merged.", this);
                return false;
            }

            // 원형 순서를 보존하기 위해 0/N-1 조합은 마지막 칸 -> 첫 칸 순서로 규칙에 전달합니다.
            RouletteSegmentData first = wrapsAcrossZero ? segments[high] : segments[low];
            RouletteSegmentData second = wrapsAcrossZero ? segments[low] : segments[high];
            SegmentsMerging?.Invoke(first, second);

            SegmentMergeRule resolvedRule = mergeRule;
            if (resolvedRule == null)
            {
                resolvedRule = CreateDefaultMergedSegment;
            }
            RouletteSegmentData merged = resolvedRule(first, second);
            if (merged == null)
            {
                Debug.LogWarning("The segment merge rule returned null. Merge was cancelled.", this);
                return false;
            }

            merged.weight = first.weight + second.weight;

            // 높은 인덱스를 먼저 지워야 낮은 인덱스가 변하지 않습니다.
            segments.RemoveAt(high);
            segments.RemoveAt(low);
            EnsureUniqueId(merged);
            segments.Insert(low, merged);

            mergedResult = merged;
            RebuildWheel();
            SegmentsMerged?.Invoke(merged);
            return true;
        }

        /// <summary>
        /// 같은 타입은 값을 더하고, 다른 타입은 Custom으로 만드는 안전한 기본 규칙입니다.
        /// 게임별 조합표가 생기면 MergeSegments의 delegate 인자로 대체하십시오.
        /// </summary>
        public static RouletteSegmentData CreateDefaultMergedSegment(
            RouletteSegmentData first,
            RouletteSegmentData second)
        {
            bool sameType = first.type == second.type;
            RouletteSegmentType mergedType = sameType ? first.type : RouletteSegmentType.Custom;
            int mergedValue = first.value + second.value;
            Color mergedColor = Color.Lerp(first.color, second.color, 0.5f);
            string mergedText;

            if (sameType)
            {
                switch (mergedType)
                {
                    case RouletteSegmentType.Multiplier:
                        mergedText = "x" + mergedValue;
                        break;
                    case RouletteSegmentType.Critical:
                        mergedText = "CRIT";
                        break;
                    case RouletteSegmentType.Mystery:
                        mergedText = "?";
                        break;
                    default:
                        mergedText = mergedValue.ToString();
                        break;
                }
            }
            else
            {
                mergedText = first.type + "+" + second.type;
            }

            return new RouletteSegmentData(
                Guid.NewGuid().ToString("N"),
                mergedType,
                mergedValue,
                first.weight + second.weight,
                mergedColor,
                first.icon != null ? first.icon : second.icon,
                mergedText,
                first.isSpecial || second.isSpecial,
                first.isDisabled && second.isDisabled);
        }

        /// <summary>
        /// 현재 Wheel 회전과 12시 포인터를 기준으로 선택된 칸을 반환합니다.
        /// </summary>
        public RouletteSegmentData GetSelectedSegment()
        {
            return GetSelectedSegment(0f);
        }

        /// <summary>
        /// pointerAngle은 12시=0도, 시계 방향 증가입니다.
        /// 포인터 위치를 바꾸는 적 룰렛/상태이상에서도 같은 판정 코드를 재사용할 수 있습니다.
        /// </summary>
        public RouletteSegmentData GetSelectedSegment(float pointerAngle)
        {
            if (wheel == null || segments.Count == 0 || _startAngles.Count != segments.Count)
            {
                return null;
            }

            // Wheel의 +Z는 반시계 회전입니다. 시계 방향 로컬 각도계에서는
            // 포인터 아래에 온 로컬 각도 = pointerAngle + wheel Z 입니다.
            float wheelZ = wheel.localEulerAngles.z;
            float wheelLocalPointerAngle = NormalizeAngle(pointerAngle + wheelZ);
            return GetSegmentAtLocalAngle(wheelLocalPointerAngle);
        }

        public RouletteSegmentData GetSegmentAtLocalAngle(float angle)
        {
            if (segments.Count == 0 || _startAngles.Count != segments.Count)
            {
                return null;
            }

            float normalized = NormalizeAngle(angle);
            for (int i = 0; i < segments.Count; i++)
            {
                // 시작 포함, 끝 미포함으로 경계 판정을 결정론적으로 처리합니다.
                if (normalized >= _startAngles[i] && normalized < _endAngles[i])
                {
                    return segments[i];
                }
            }

            // 부동소수점 누적 오차로 360도 직전 값이 빠질 경우의 안전망입니다.
            return segments[segments.Count - 1];
        }

        public RouletteSegmentData GetSegment(int index)
        {
            return IsValidIndex(index) ? segments[index] : null;
        }

        /// <summary>
        /// 계산된 시작/끝/중앙/크기 각도를 외부 애니메이션과 디버그 UI에 제공합니다.
        /// </summary>
        public bool TryGetSegmentAngles(
            int index,
            out float startAngle,
            out float endAngle,
            out float centerAngle,
            out float angleSize)
        {
            startAngle = 0f;
            endAngle = 0f;
            centerAngle = 0f;
            angleSize = 0f;

            if (!IsValidIndex(index) || _startAngles.Count != segments.Count)
            {
                return false;
            }

            startAngle = _startAngles[index];
            endAngle = _endAngles[index];
            centerAngle = (startAngle + endAngle) * 0.5f;
            angleSize = endAngle - startAngle;
            return true;
        }

        public bool TryGetSegmentGraphic(string id, out RouletteSegmentGraphic graphic)
        {
            if (string.IsNullOrEmpty(id))
            {
                graphic = null;
                return false;
            }

            return _graphicById.TryGetValue(id, out graphic) && graphic != null;
        }

        /// <summary>
        /// 선택된 칸만 간단히 깜빡입니다. 향후 별도 애니메이터로 교체하기 쉬운 진입점입니다.
        /// </summary>
        public void HighlightSegment(RouletteSegmentData data, float duration = 0.7f)
        {
            if (data == null)
            {
                return;
            }

            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                ClearHighlights();
            }

            _highlightRoutine = StartCoroutine(HighlightRoutine(data.id, Mathf.Max(0f, duration)));
        }

        public void ClearHighlights()
        {
            if (pixelWheelRenderer != null)
            {
                pixelWheelRenderer.ClearHighlight();
            }

            for (int i = 0; i < _graphics.Count; i++)
            {
                if (_graphics[i] != null)
                {
                    _graphics[i].SetHighlighted(false);
                }
            }
        }

        public static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }

        private IEnumerator HighlightRoutine(string id, float duration)
        {
            RouletteSegmentGraphic graphic;
            if (!TryGetSegmentGraphic(id, out graphic))
            {
                _highlightRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            const float blinkInterval = 0.11f;
            bool highlighted = true;
            SetSegmentHighlighted(id, graphic, true);

            while (elapsed < duration && graphic != null)
            {
                yield return new WaitForSecondsRealtime(blinkInterval);
                elapsed += blinkInterval;
                highlighted = !highlighted;

                if (graphic != null)
                {
                    SetSegmentHighlighted(id, graphic, highlighted);
                }
            }

            if (graphic != null)
            {
                SetSegmentHighlighted(id, graphic, false);
            }

            _highlightRoutine = null;
        }

        private void ResolveHierarchyReferences()
        {
            if (wheel == null)
            {
                Transform wheelTransform = transform.Find("Wheel");
                wheel = wheelTransform as RectTransform;
            }

            if (dynamicSegmentRoot == null && wheel != null)
            {
                Transform segmentTransform = wheel.Find("DynamicSegments");
                dynamicSegmentRoot = segmentTransform as RectTransform;
            }

            if (pixelWheelRenderer == null && dynamicSegmentRoot != null)
            {
                pixelWheelRenderer = dynamicSegmentRoot.GetComponent<RoulettePixelWheelRenderer>();
            }
        }

        private void EnsureValidRuntimeData()
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i] == null)
                {
                    segments.RemoveAt(i);
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                RouletteSegmentData data = segments[i];
                data.weight = Mathf.Max(0.001f, data.weight);

                if (string.IsNullOrWhiteSpace(data.id) || ids.Contains(data.id))
                {
                    data.id = CreateUniqueId(data.type.ToString(), ids);
                }

                ids.Add(data.id);
            }

            if (segments.Count > maximumSegmentCount)
            {
                Debug.LogWarning(
                    $"Roulette has {segments.Count} segments; the recommended maximum is {maximumSegmentCount}.",
                    this);
            }
        }

        private void EnsureUniqueId(RouletteSegmentData data)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i] != null && !ReferenceEquals(segments[i], data) && !string.IsNullOrEmpty(segments[i].id))
                {
                    ids.Add(segments[i].id);
                }
            }

            if (string.IsNullOrWhiteSpace(data.id) || ids.Contains(data.id))
            {
                data.id = CreateUniqueId(data.type.ToString(), ids);
            }
        }

        private static string CreateUniqueId(string prefix, HashSet<string> existingIds)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "segment" : prefix.ToLowerInvariant();
            string candidate;
            do
            {
                candidate = safePrefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            }
            while (existingIds.Contains(candidate));

            return candidate;
        }

        private float GetResolvedRadius()
        {
            if (radiusOverride > 0f)
            {
                return Mathf.Max(1f, radiusOverride - radiusPadding);
            }

            Rect rect = dynamicSegmentRoot.rect;
            float shortSide = Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
            if (shortSide <= 0f)
            {
                Debug.LogWarning("DynamicSegments has no size yet. Using a temporary 200px radius.", this);
                return 200f;
            }

            return Mathf.Max(1f, shortSide * 0.5f - radiusPadding);
        }

        private float GetPixelRadiusRatio(float radius)
        {
            Rect rect = dynamicSegmentRoot.rect;
            float shortSide = Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
            if (shortSide <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(radius / (shortSide * 0.5f));
        }

        private void CreateSegmentGraphic(
            int index,
            RouletteSegmentData data,
            float startAngle,
            float endAngle,
            float radius,
            bool renderGeometry)
        {
            string objectName = $"Segment_{index:00}_{data.type}";
            GameObject segmentObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RouletteSegmentGraphic));
            segmentObject.transform.SetParent(dynamicSegmentRoot, false);

            RectTransform segmentRect = segmentObject.GetComponent<RectTransform>();
            segmentRect.anchorMin = Vector2.zero;
            segmentRect.anchorMax = Vector2.one;
            segmentRect.offsetMin = Vector2.zero;
            segmentRect.offsetMax = Vector2.zero;
            segmentRect.pivot = new Vector2(0.5f, 0.5f);
            segmentRect.localScale = Vector3.one;
            segmentRect.localRotation = Quaternion.identity;

            RouletteSegmentGraphic graphic = segmentObject.GetComponent<RouletteSegmentGraphic>();
            graphic.Configure(
                data,
                startAngle,
                endAngle,
                radius,
                borderThickness,
                borderColor,
                highlightColor,
                maximumArcStepDegrees,
                snapMeshVerticesToPixels,
                labelFont,
                labelFontSize,
                iconRadiusRatio,
                labelRadiusRatio,
                iconSize,
                labelSize,
                renderGeometry);

            _graphics.Add(graphic);
            _graphicById[data.id] = graphic;
        }

        private void SetSegmentHighlighted(
            string id,
            RouletteSegmentGraphic graphic,
            bool highlighted)
        {
            if (graphic != null)
            {
                graphic.SetHighlighted(highlighted);
            }

            if (pixelWheelRenderer != null)
            {
                pixelWheelRenderer.SetHighlightedSegment(id, highlighted);
            }
        }

        private void ClearGeneratedGraphics()
        {
            _graphics.Clear();
            _graphicById.Clear();

            if (dynamicSegmentRoot == null)
            {
                return;
            }

            // 전용 루트 안에 디자이너가 배치한 장식물이 있어도 Graphic이 없는 오브젝트는 보존합니다.
            RouletteSegmentGraphic[] generatedGraphics =
                dynamicSegmentRoot.GetComponentsInChildren<RouletteSegmentGraphic>(true);

            for (int i = 0; i < generatedGraphics.Length; i++)
            {
                GameObject target = generatedGraphics[i].gameObject;
                if (Application.isPlaying)
                {
                    Destroy(target);
                }
                else
                {
                    DestroyImmediate(target);
                }
            }
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < segments.Count;
        }
    }
}
