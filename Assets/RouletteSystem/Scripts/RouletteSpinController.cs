using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// Inspector에서도 결과 이벤트를 연결할 수 있는 직렬화 가능한 UnityEvent입니다.
    /// </summary>
    [Serializable]
    public class RouletteSegmentResultEvent : UnityEvent<RouletteSegmentData>
    {
    }

    /// <summary>
    /// Wheel 회전만 담당합니다. 결과 데이터와 UI 재생성은 RouletteController에 위임합니다.
    /// 회전은 빠른 유지 구간 뒤 속도가 0이 되는 포물선 감속 곡선을 사용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RouletteSpinController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RouletteController rouletteController;
        [SerializeField] private RectTransform wheel;
        [SerializeField] private Button spinButton;
        [SerializeField] private RectTransform centerCap;

        [Header("Spin Settings")]
        [SerializeField, Min(0.1f)] private float minSpinDuration = 2.5f;
        [SerializeField, Min(0.1f)] private float maxSpinDuration = 4f;
        [SerializeField, Min(1f)] private float startSpeed = 1080f;
        [SerializeField, Min(1f)] private float deceleration = 720f;
        [Tooltip("기본 회전 거리 뒤에 더할 최대 무작위 각도(도)입니다.")]
        [SerializeField, Min(0f)] private float randomExtraRotation = 720f;
        [SerializeField, Min(1)] private int minimumFullRotations = 3;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Spin Sound")]
        [Tooltip("틱·정지음을 재생할 소스. 녹음된 회전음 한 곡을 틀지 않고, 칸 경계가 포인터를 지날 때마다 틱을 낸다.")]
        [SerializeField] private AudioSource spinAudioSource;
        [Tooltip("칸 경계 하나가 포인터를 지날 때 나는 짧은 소리. 회전 속도·칸 수와 자동으로 맞는다.")]
        [SerializeField] private AudioClip tickClip;
        [Tooltip("룰렛이 멈춘 순간의 소리.")]
        [SerializeField] private AudioClip stopClip;
        [Tooltip("빠르게 돌 때 틱이 뭉개지지 않도록 두는 최소 간격(초).")]
        [SerializeField, Min(0f)] private float minimumTickInterval = 0.03f;
        [SerializeField, Range(0f, 1f)] private float tickVolume = 0.55f;

        [Header("Power Throw")]
        [Tooltip("강도 회전은 한 칸을 저격하지 못하도록 마지막 착지 각도에 이 범위의 오차를 더합니다.")]
        [SerializeField, Min(0f)] private float minimumLandingUncertainty = 42f;
        [SerializeField, Min(0f)] private float maximumLandingUncertainty = 60f;
        [Tooltip("강도 0~1이 가리키는 추가 이동 각도입니다. 한 바퀴 전체보다 조금 좁게 잡아 양 끝이 같은 지점이 되지 않게 합니다.")]
        [SerializeField] private Vector2 powerTravelArc = new Vector2(20f, 340f);
        [SerializeField, Min(0)] private int additionalFullRotationsAtMaxPower = 3;

        [Header("Replayable Random")]
        [Tooltip("전투 시드를 다시 주입하면 같은 순서의 정지 결과를 재현할 수 있습니다.")]
        [SerializeField] private bool useDeterministicRandom = true;
        [SerializeField] private int randomSeed = 12345;

        [Header("Result")]
        [Tooltip("12시=0도, 시계 방향 증가. 포인터가 12시라면 0을 유지합니다.")]
        [SerializeField] private float pointerAngle;
        [SerializeField, Min(0f)] private float selectedHighlightDuration = 0.75f;
        [SerializeField] private RouletteSegmentResultEvent onRouletteFinished =
            new RouletteSegmentResultEvent();

        [Header("Fixed Center Cap")]
        [Tooltip("CenterCap이 Wheel의 자식이어도 화면상 회전하지 않도록 역회전시킵니다.")]
        [SerializeField] private bool keepCenterCapVisuallyFixed = true;

        private Coroutine _spinRoutine;
        private bool _isSpinning;
        private bool _hasCapturedCenterCapRotation;
        private Quaternion _centerCapRotationRelativeToWheelParent;
        private System.Random _deterministicRandom;

        public bool IsSpinning => _isSpinning;
        public RouletteSegmentResultEvent OnRouletteFinished => onRouletteFinished;

        /// <summary>
        /// 코드 기반 전투 시스템은 이 C# 이벤트를 구독하면 됩니다.
        /// </summary>
        public event Action<RouletteSegmentData> RouletteFinished;

        /// <summary>
        /// 강도 회전의 착지 오차 안에서 칸별 상대 무게(기본 1)를 반영한다. null이면 균등.
        /// 강도로 정한 구역을 벗어나지 않으므로 조준감은 유지되고, 그 구역 안에서만 확률이 기운다.
        /// </summary>
        public Func<RouletteSegmentData, float> LandingBias { get; set; }

        private const int LandingBiasMaxAttempts = 8;

        /// <summary>
        /// 회전 시간 배율(기본 1). 한 턴의 두 번째 SPIN부터나 딜러 턴 빨리 감기에서 줄인다.
        /// 회전 거리는 그대로라 착지 판정과 시드 재현에는 영향이 없다.
        /// </summary>
        public float DurationScale { get; set; } = 1f;

        private void Reset()
        {
            rouletteController = GetComponent<RouletteController>();
            if (rouletteController != null)
            {
                wheel = rouletteController.Wheel;
            }

            Transform buttonTransform = transform.Find("SpinButton");
            if (buttonTransform != null)
            {
                spinButton = buttonTransform.GetComponent<Button>();
            }
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureFixedCenterCapRotation();
            SetRandomSeed(randomSeed);

            if (spinButton != null)
            {
                spinButton.onClick.AddListener(Spin);
            }
        }

        private void OnDestroy()
        {
            if (spinButton != null)
            {
                spinButton.onClick.RemoveListener(Spin);
            }
        }

        private void OnDisable()
        {
            if (_spinRoutine != null)
            {
                StopCoroutine(_spinRoutine);
                _spinRoutine = null;
            }

            _isSpinning = false;
            if (spinButton != null)
            {
                spinButton.interactable = true;
            }
        }

        private void OnValidate()
        {
            minSpinDuration = Mathf.Max(0.1f, minSpinDuration);
            maxSpinDuration = Mathf.Max(minSpinDuration, maxSpinDuration);
            startSpeed = Mathf.Max(1f, startSpeed);
            deceleration = Mathf.Max(1f, deceleration);
            minimumFullRotations = Mathf.Max(1, minimumFullRotations);
            minimumLandingUncertainty = Mathf.Max(0f, minimumLandingUncertainty);
            maximumLandingUncertainty = Mathf.Max(minimumLandingUncertainty, maximumLandingUncertainty);
            additionalFullRotationsAtMaxPower = Mathf.Max(0, additionalFullRotationsAtMaxPower);
            minimumTickInterval = Mathf.Max(0f, minimumTickInterval);
        }

        /// <summary>
        /// 버튼이나 전투 상태 머신에서 호출하는 공개 회전 진입점입니다.
        /// 중복 호출은 현재 회전이 끝날 때까지 무시합니다.
        /// </summary>
        public void Spin()
        {
            if (_isSpinning)
            {
                return;
            }

            ResolveReferences();
            CaptureFixedCenterCapRotation();
            if (rouletteController == null || wheel == null || rouletteController.Count < 2)
            {
                Debug.LogWarning("Roulette cannot spin until Controller, Wheel, and at least two segments exist.", this);
                return;
            }

            _spinRoutine = StartCoroutine(SpinRoutine(null, null));
        }

        /// <summary>
        /// 플레이어가 정한 회전 강도로 룰렛을 던집니다. 강도는 대략적인 이동 구역만 정하며,
        /// 마지막 2~3칸은 시드 기반 오차로 결정되어 한 칸 타이밍 저격을 방지합니다.
        /// </summary>
        public void Spin(float normalizedPower)
        {
            if (_isSpinning)
            {
                return;
            }

            ResolveReferences();
            CaptureFixedCenterCapRotation();
            if (rouletteController == null || wheel == null || rouletteController.Count < 2)
            {
                Debug.LogWarning("Roulette cannot spin until Controller, Wheel, and at least two segments exist.", this);
                return;
            }

            _spinRoutine = StartCoroutine(SpinRoutine(Mathf.Clamp01(normalizedPower), null));
        }

        /// <summary>
        /// 공개된 적 패턴처럼 결과가 이미 정해진 룰렛을 실제 회전시켜 해당 칸에 멈춥니다.
        /// </summary>
        public void SpinToSegment(int segmentIndex)
        {
            if (_isSpinning)
            {
                return;
            }

            ResolveReferences();
            CaptureFixedCenterCapRotation();
            if (rouletteController == null
                || wheel == null
                || segmentIndex < 0
                || segmentIndex >= rouletteController.Count)
            {
                Debug.LogWarning("Roulette cannot target the requested segment.", this);
                return;
            }

            _spinRoutine = StartCoroutine(SpinRoutine(null, segmentIndex));
        }

        /// <summary>
        /// NUDGE: 멈춘 룰렛을 가장 가까운 방향으로 살짝 돌려 지정한 칸의 가운데를 포인터 아래에 둔다(틱 한 번 + 멈춤 소리).
        /// 결과 이벤트는 다시 보내지 않는다. 끝나면 onDone을 부른다.
        /// </summary>
        public void NudgeToSegment(int segmentIndex, Action onDone)
        {
            ResolveReferences();
            if (_isSpinning || rouletteController == null || wheel == null
                || !rouletteController.TryGetSegmentAngles(segmentIndex, out _, out _, out float centerAngle, out _))
            {
                onDone?.Invoke();
                return;
            }

            _spinRoutine = StartCoroutine(NudgeRoutine(centerAngle, onDone));
        }

        private IEnumerator NudgeRoutine(float targetCenterAngle, Action onDone)
        {
            _isSpinning = true;
            float startAngle = wheel.localEulerAngles.z;
            float targetAngle = targetCenterAngle - pointerAngle;
            float delta = Mathf.DeltaAngle(startAngle, targetAngle);
            const float duration = 0.3f;
            PlayClip(tickClip, tickVolume);
            for (float t = 0f; t < duration; t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                float k = t / duration;
                float eased = 1f - (1f - k) * (1f - k) * (1f - k);
                SetWheelAngle(startAngle + delta * eased);
                yield return null;
            }

            SetWheelAngle(startAngle + delta);
            PlayClip(stopClip, 1f);
            _isSpinning = false;
            _spinRoutine = null;
            onDone?.Invoke();
        }

        /// <summary>
        /// 런 시작/전투 시작 시 저장된 RNG 시드를 주입하면 정지 결과를 재현할 수 있습니다.
        /// </summary>
        public void SetRandomSeed(int seed)
        {
            randomSeed = seed;
            _deterministicRandom = new System.Random(seed);
        }

        private IEnumerator SpinRoutine(float? normalizedPower, int? targetSegmentIndex)
        {
            _isSpinning = true;
            if (spinButton != null)
            {
                spinButton.interactable = false;
            }

            float duration = normalizedPower.HasValue
                ? Mathf.Lerp(minSpinDuration, maxSpinDuration, normalizedPower.Value)
                : NextRandomRange(minSpinDuration, maxSpinDuration);
            duration = Mathf.Max(0.1f, duration * Mathf.Clamp(DurationScale, 0.1f, 1f));

            // 설정한 감속도가 클수록 감속 구간이 짧아집니다.
            // 전체 시간의 25~80% 범위로 제한해 빠른 유지/감속 두 구간을 항상 확보합니다.
            float naturalDecelerationTime = startSpeed / Mathf.Max(1f, deceleration);
            float decelerationTime = Mathf.Clamp(
                naturalDecelerationTime,
                duration * 0.25f,
                duration * 0.8f);
            float holdTime = duration - decelerationTime;

            float holdDistance = startSpeed * holdTime;
            float decelerationDistance = startSpeed * decelerationTime * 0.5f;
            float profileDistance = Mathf.Max(1f, holdDistance + decelerationDistance);

            float startAngle = wheel.localEulerAngles.z;
            float totalDistance;
            if (targetSegmentIndex.HasValue
                && rouletteController.TryGetSegmentAngles(
                    targetSegmentIndex.Value,
                    out _,
                    out _,
                    out float targetCenterAngle,
                    out _))
            {
                float targetWheelAngle = targetCenterAngle - pointerAngle;
                float clockwiseDistance = Mathf.Repeat(startAngle - targetWheelAngle, 360f);
                totalDistance = minimumFullRotations * 360f + clockwiseDistance;
            }
            else if (normalizedPower.HasValue)
            {
                float power = normalizedPower.Value;
                int fullRotations = minimumFullRotations
                                    + Mathf.RoundToInt(additionalFullRotationsAtMaxPower * power);
                float landingUncertainty = Mathf.Lerp(
                    minimumLandingUncertainty,
                    maximumLandingUncertainty,
                    power);
                float intendedTravel = Mathf.Lerp(powerTravelArc.x, powerTravelArc.y, power);
                float baseDistance = fullRotations * 360f + intendedTravel;
                totalDistance = baseDistance + SampleLandingOffset(startAngle, baseDistance, landingUncertainty);
            }
            else
            {
                float minimumDistance = minimumFullRotations * 360f;
                totalDistance = Mathf.Max(minimumDistance, profileDistance)
                                + NextRandomRange(0f, randomExtraRotation);
            }
            float elapsed = 0f;
            RouletteSegmentData segmentUnderPointer = rouletteController.GetSelectedSegment(pointerAngle);
            float lastTickTime = float.NegativeInfinity;

            while (elapsed < duration)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed = Mathf.Min(duration, elapsed + deltaTime);

                float travelledProfileDistance;
                if (elapsed <= holdTime)
                {
                    // 빠른 속도 유지 구간.
                    travelledProfileDistance = startSpeed * elapsed;
                }
                else
                {
                    // v(t)가 startSpeed에서 0으로 선형 감소하도록 거리를 적분합니다.
                    float decelerationElapsed = elapsed - holdTime;
                    float effectiveDeceleration = startSpeed / Mathf.Max(0.0001f, decelerationTime);
                    travelledProfileDistance = holdDistance
                                               + startSpeed * decelerationElapsed
                                               - 0.5f * effectiveDeceleration
                                               * decelerationElapsed * decelerationElapsed;
                }

                float normalizedDistance = Mathf.Clamp01(travelledProfileDistance / profileDistance);
                // UI Z의 음수 방향이 화면상 시계 방향입니다.
                SetWheelAngle(startAngle - totalDistance * normalizedDistance);
                TickIfSegmentChanged(ref segmentUnderPointer, ref lastTickTime, elapsed);
                yield return null;
            }

            // 프레임 시간과 무관하게 마지막 각도를 정확히 고정합니다.
            SetWheelAngle(startAngle - totalDistance);
            TickIfSegmentChanged(ref segmentUnderPointer, ref lastTickTime, float.PositiveInfinity);
            PlayClip(stopClip, 1f);

            RouletteSegmentData result = rouletteController.GetSelectedSegment(pointerAngle);

            // Finished 콜백 안에서 다음 턴 Spin을 요청해도 정상 동작하도록 상태를 먼저 종료합니다.
            _isSpinning = false;
            _spinRoutine = null;
            if (spinButton != null)
            {
                spinButton.interactable = true;
            }

            if (result != null)
            {
                rouletteController.HighlightSegment(result, selectedHighlightDuration);
                RouletteFinished?.Invoke(result);
                onRouletteFinished?.Invoke(result);
            }
            else
            {
                Debug.LogWarning("Roulette stopped, but no segment result could be resolved.", this);
            }
        }

        /// <summary>포인터 아래 칸이 바뀌었으면 틱 한 번. 회전이 멈추면 소리도 함께 멈춘다.</summary>
        private void TickIfSegmentChanged(ref RouletteSegmentData segmentUnderPointer, ref float lastTickTime, float now)
        {
            RouletteSegmentData current = rouletteController.GetSelectedSegment(pointerAngle);
            if (current == segmentUnderPointer)
            {
                return;
            }

            segmentUnderPointer = current;
            if (now - lastTickTime < minimumTickInterval)
            {
                return;
            }

            lastTickTime = now;
            PlayClip(tickClip, tickVolume);
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (spinAudioSource != null && clip != null)
            {
                spinAudioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// 칸 폭을 바꾸는 동안(몰수가 SPIN마다 넓어질 때) 포인터 아래 칸이 밀리지 않게 휠 각도를 보정한다.
        /// relayout 안에서 RouletteController의 칸 무게를 바꾼다.
        /// </summary>
        public void RelayoutKeepingPointer(Action relayout)
        {
            if (rouletteController == null || wheel == null)
            {
                relayout?.Invoke();
                return;
            }

            float local = RouletteController.NormalizeAngle(pointerAngle + wheel.localEulerAngles.z);
            RouletteSegmentData under = rouletteController.GetSegmentAtLocalAngle(local);
            int index = -1;
            for (int i = 0; i < rouletteController.Count; i++)
            {
                if (rouletteController.GetSegment(i) == under) index = i;
            }

            float fraction = 0.5f;
            if (index >= 0 && rouletteController.TryGetSegmentAngles(index, out float start, out _, out _, out float size) && size > 0f)
            {
                fraction = Mathf.Clamp01(RouletteController.NormalizeAngle(local - start) / size);
            }

            relayout?.Invoke();
            if (index >= 0 && rouletteController.TryGetSegmentAngles(index, out float newStart, out _, out _, out float newSize))
            {
                SetWheelAngle(newStart + fraction * newSize - pointerAngle);
            }
        }

        private void SetWheelAngle(float zAngle)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            ApplyFixedCenterCapRotation();
        }

        /// <summary>
        /// 착지 오차를 고른다. LandingBias가 있으면 거절 샘플링으로 무거운 칸에 멈출 가능성을 높인다.
        /// </summary>
        private float SampleLandingOffset(float startAngle, float baseDistance, float uncertainty)
        {
            float offset = NextRandomRange(-uncertainty, uncertainty);
            if (LandingBias == null)
            {
                return offset;
            }

            float maxWeight = 1f;
            for (int i = 0; i < rouletteController.Count; i++)
            {
                maxWeight = Mathf.Max(maxWeight, LandingBias(rouletteController.GetSegment(i)));
            }

            for (int attempt = 0; attempt < LandingBiasMaxAttempts; attempt++)
            {
                float finalWheelAngle = startAngle - (baseDistance + offset);
                RouletteSegmentData landed = rouletteController.GetSegmentAtLocalAngle(
                    RouletteController.NormalizeAngle(pointerAngle + finalWheelAngle));
                float weight = landed == null ? 1f : Mathf.Max(0f, LandingBias(landed));
                if (NextRandomRange(0f, maxWeight) <= weight)
                {
                    break;
                }

                offset = NextRandomRange(-uncertainty, uncertainty);
            }

            return offset;
        }

        private float NextRandomRange(float minimum, float maximum)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            if (!useDeterministicRandom)
            {
                return UnityEngine.Random.Range(minimum, maximum);
            }

            if (_deterministicRandom == null)
            {
                _deterministicRandom = new System.Random(randomSeed);
            }

            return minimum + (float)_deterministicRandom.NextDouble() * (maximum - minimum);
        }

        private void ResolveReferences()
        {
            if (rouletteController == null)
            {
                rouletteController = GetComponent<RouletteController>();
            }

            if (wheel == null && rouletteController != null)
            {
                wheel = rouletteController.Wheel;
            }

            if (wheel == null)
            {
                wheel = transform.Find("Wheel") as RectTransform;
            }

            if (spinButton == null)
            {
                Transform buttonTransform = transform.Find("SpinButton");
                if (buttonTransform != null)
                {
                    spinButton = buttonTransform.GetComponent<Button>();
                }
            }

            if (centerCap == null && wheel != null)
            {
                centerCap = wheel.Find("CenterCap") as RectTransform;
            }

            if (!_hasCapturedCenterCapRotation)
            {
                CaptureFixedCenterCapRotation();
            }
        }

        private void CaptureFixedCenterCapRotation()
        {
            if (centerCap == null || wheel == null || wheel.parent == null)
            {
                return;
            }

            _centerCapRotationRelativeToWheelParent =
                Quaternion.Inverse(wheel.parent.rotation) * centerCap.rotation;
            _hasCapturedCenterCapRotation = true;
        }

        private void ApplyFixedCenterCapRotation()
        {
            if (!keepCenterCapVisuallyFixed
                || !_hasCapturedCenterCapRotation
                || centerCap == null
                || wheel == null
                || wheel.parent == null)
            {
                return;
            }

            centerCap.rotation = wheel.parent.rotation * _centerCapRotationRelativeToWheelParent;
        }
    }
}
