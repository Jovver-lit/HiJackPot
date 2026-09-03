using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 기본 8칸 생성과 키보드 런타임 변형을 빠르게 검증하는 개발용 컴포넌트입니다.
    /// 실제 빌드에서는 이 컴포넌트를 제거하거나 비활성화하십시오.
    /// </summary>
    [DisallowMultipleComponent]
    public class RouletteTest : MonoBehaviour
    {
        [SerializeField] private RouletteController rouletteController;
        [SerializeField] private RouletteSpinController spinController;
        [SerializeField] private bool initializeDefaultWheelWhenEmpty = true;
        [SerializeField, Min(1.01f)] private float randomWeightMultiplier = 1.5f;

        private int _generatedId;

        private void Reset()
        {
            rouletteController = GetComponent<RouletteController>();
            spinController = GetComponent<RouletteSpinController>();
        }

        private void Awake()
        {
            if (rouletteController == null)
            {
                rouletteController = GetComponent<RouletteController>();
            }

            if (spinController == null)
            {
                spinController = GetComponent<RouletteSpinController>();
            }
        }

        private void OnEnable()
        {
            if (spinController != null)
            {
                spinController.RouletteFinished += HandleRouletteFinished;
            }
        }

        private void Start()
        {
            if (rouletteController == null)
            {
                Debug.LogError("RouletteTest requires a RouletteController on the same GameObject.", this);
                enabled = false;
                return;
            }

            if (initializeDefaultWheelWhenEmpty && rouletteController.Count == 0)
            {
                rouletteController.SetSegments(CreateDefaultSegments(), false);
            }

            Debug.Log("Roulette Test: Space=Spin, A=Add, R=Remove, W=Increase Weight, M=Merge", this);
        }

        private void OnDisable()
        {
            if (spinController != null)
            {
                spinController.RouletteFinished -= HandleRouletteFinished;
            }
        }

        private void Update()
        {
            if (WasSpacePressed() && spinController != null)
            {
                spinController.Spin();
            }

            // 회전 중 데이터가 바뀌면 결과 설명과 시각 흐름이 혼란스러우므로 테스트 입력도 잠급니다.
            if (spinController != null && spinController.IsSpinning)
            {
                return;
            }

            if (WasLetterPressed(TestLetter.A))
            {
                AddRandomSegment();
            }

            if (WasLetterPressed(TestLetter.R))
            {
                RemoveRandomSegment();
            }

            if (WasLetterPressed(TestLetter.W))
            {
                IncreaseRandomWeight();
            }

            if (WasLetterPressed(TestLetter.M))
            {
                MergeRandomAdjacentSegments();
            }
        }

        public static List<RouletteSegmentData> CreateDefaultSegments()
        {
            return new List<RouletteSegmentData>
            {
                Create("damage_5", RouletteSegmentType.Damage, 5, 1f, new Color32(207, 72, 72, 255), "5"),
                Create("damage_10", RouletteSegmentType.Damage, 10, 1f, new Color32(185, 52, 67, 255), "10"),
                Create("damage_15", RouletteSegmentType.Damage, 15, 1f, new Color32(155, 42, 71, 255), "15"),
                Create("heal_8", RouletteSegmentType.Heal, 8, 1f, new Color32(67, 177, 116, 255), "8"),
                Create("critical", RouletteSegmentType.Critical, 0, 1f, new Color32(238, 165, 55, 255), "CRIT", true),
                Create("poison_5", RouletteSegmentType.Poison, 5, 1f, new Color32(114, 73, 153, 255), "POISON 5"),
                Create("multiplier_2", RouletteSegmentType.Multiplier, 2, 1f, new Color32(57, 139, 191, 255), "x2", true),
                Create("mystery", RouletteSegmentType.Mystery, 0, 1f, new Color32(78, 78, 104, 255), "?", true)
            };
        }

        private void AddRandomSegment()
        {
            RouletteSegmentType[] types =
            {
                RouletteSegmentType.Damage,
                RouletteSegmentType.Heal,
                RouletteSegmentType.Poison,
                RouletteSegmentType.Multiplier,
                RouletteSegmentType.Mystery,
                RouletteSegmentType.Jackpot,
                RouletteSegmentType.Hijack
            };

            RouletteSegmentType type = types[UnityEngine.Random.Range(0, types.Length)];
            int value = GetRandomValue(type);
            RouletteSegmentData segment = Create(
                "random_" + _generatedId++ + "_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                type,
                value,
                UnityEngine.Random.Range(0.75f, 1.75f),
                GetColor(type),
                GetDisplayText(type, value),
                type == RouletteSegmentType.Jackpot || type == RouletteSegmentType.Hijack);

            if (rouletteController.AddSegment(segment))
            {
                Debug.Log("Added Segment: " + segment.GetDebugLabel(), this);
            }
        }

        private void RemoveRandomSegment()
        {
            if (rouletteController.Count <= 2)
            {
                Debug.LogWarning("Remove skipped: the roulette must keep at least two segments.", this);
                return;
            }

            int index = UnityEngine.Random.Range(0, rouletteController.Count);
            RouletteSegmentData removed = rouletteController.GetSegment(index);
            string label = removed != null ? removed.GetDebugLabel() : index.ToString();

            if (rouletteController.RemoveSegment(index))
            {
                Debug.Log("Removed Segment: " + label, this);
            }
        }

        private void IncreaseRandomWeight()
        {
            if (rouletteController.Count == 0)
            {
                return;
            }

            int index = UnityEngine.Random.Range(0, rouletteController.Count);
            RouletteSegmentData segment = rouletteController.GetSegment(index);
            float previousWeight = segment.weight;
            float nextWeight = previousWeight * randomWeightMultiplier;

            if (rouletteController.ChangeSegmentWeight(segment.id, nextWeight))
            {
                Debug.Log(
                    $"Weight Increased: {segment.GetDebugLabel()} {previousWeight:0.##} -> {nextWeight:0.##}",
                    this);
            }
        }

        private void MergeRandomAdjacentSegments()
        {
            if (rouletteController.Count <= 2)
            {
                Debug.LogWarning("Merge skipped: the roulette must keep at least two segments.", this);
                return;
            }

            int indexA = UnityEngine.Random.Range(0, rouletteController.Count);
            int indexB = (indexA + 1) % rouletteController.Count;
            RouletteSegmentData first = rouletteController.GetSegment(indexA);
            RouletteSegmentData second = rouletteController.GetSegment(indexB);
            string firstLabel = first.GetDebugLabel();
            string secondLabel = second.GetDebugLabel();

            RouletteSegmentData merged;
            bool succeeded = rouletteController.MergeSegments(
                indexA,
                indexB,
                TestMergeRule,
                out merged);

            if (succeeded)
            {
                Debug.Log(
                    $"Merged: {firstLabel} + {secondLabel} -> {merged.GetDebugLabel()}",
                    this);
            }
        }

        /// <summary>
        /// 샘플 외부 합체 규칙입니다. 실제 게임에서는 조합표/ScriptableObject를 조회하도록 교체할 수 있습니다.
        /// </summary>
        private static RouletteSegmentData TestMergeRule(
            RouletteSegmentData first,
            RouletteSegmentData second)
        {
            return RouletteController.CreateDefaultMergedSegment(first, second);
        }

        private void HandleRouletteFinished(RouletteSegmentData result)
        {
            Debug.Log("Spin Result: " + result.GetDebugLabel(), this);
        }

        private static RouletteSegmentData Create(
            string id,
            RouletteSegmentType type,
            int value,
            float weight,
            Color color,
            string displayText,
            bool isSpecial = false)
        {
            return new RouletteSegmentData(
                id,
                type,
                value,
                weight,
                color,
                null,
                displayText,
                isSpecial,
                false);
        }

        private static int GetRandomValue(RouletteSegmentType type)
        {
            switch (type)
            {
                case RouletteSegmentType.Multiplier:
                    return UnityEngine.Random.Range(2, 4);
                case RouletteSegmentType.Mystery:
                    return 0;
                case RouletteSegmentType.Jackpot:
                    return UnityEngine.Random.Range(20, 51);
                default:
                    return UnityEngine.Random.Range(3, 16);
            }
        }

        private static string GetDisplayText(RouletteSegmentType type, int value)
        {
            switch (type)
            {
                case RouletteSegmentType.Multiplier:
                    return "x" + value;
                case RouletteSegmentType.Mystery:
                    return "?";
                case RouletteSegmentType.Jackpot:
                    return "JACKPOT";
                case RouletteSegmentType.Hijack:
                    return "HIJACK";
                default:
                    return value.ToString();
            }
        }

        private static Color GetColor(RouletteSegmentType type)
        {
            switch (type)
            {
                case RouletteSegmentType.Damage:
                    return new Color32(198, 63, 68, 255);
                case RouletteSegmentType.Heal:
                    return new Color32(64, 174, 111, 255);
                case RouletteSegmentType.Critical:
                    return new Color32(235, 162, 52, 255);
                case RouletteSegmentType.Poison:
                    return new Color32(112, 69, 151, 255);
                case RouletteSegmentType.Multiplier:
                    return new Color32(53, 136, 188, 255);
                case RouletteSegmentType.Jackpot:
                    return new Color32(244, 193, 54, 255);
                case RouletteSegmentType.Hijack:
                    return new Color32(210, 48, 143, 255);
                default:
                    return new Color32(76, 76, 101, 255);
            }
        }

        private enum TestLetter
        {
            A,
            R,
            W,
            M
        }

        private static bool WasSpacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private static bool WasLetterPressed(TestLetter letter)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            switch (letter)
            {
                case TestLetter.A:
                    return Keyboard.current.aKey.wasPressedThisFrame;
                case TestLetter.R:
                    return Keyboard.current.rKey.wasPressedThisFrame;
                case TestLetter.W:
                    return Keyboard.current.wKey.wasPressedThisFrame;
                case TestLetter.M:
                    return Keyboard.current.mKey.wasPressedThisFrame;
                default:
                    return false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (letter)
            {
                case TestLetter.A:
                    return Input.GetKeyDown(KeyCode.A);
                case TestLetter.R:
                    return Input.GetKeyDown(KeyCode.R);
                case TestLetter.W:
                    return Input.GetKeyDown(KeyCode.W);
                case TestLetter.M:
                    return Input.GetKeyDown(KeyCode.M);
                default:
                    return false;
            }
#else
            return false;
#endif
        }
    }
}
