using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 첫 전투에서 강도 선택, 넓은 착지 오차, 보이는 적 행동, 룰렛 개조를 검증하는 수직 프로토타입입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RabbitBattlePrototype : MonoBehaviour
    {
        private enum BattleState
        {
            AwaitingThrow,
            Spinning,
            ChoosingNudge,
            Resolving,
            ChoosingHijack,
            Ended
        }

        [Header("Roulette")]
        [SerializeField] private RouletteController roulette;
        [SerializeField] private RouletteSpinController spinController;
        [SerializeField] private HoldToSpinInput spinInput;

        [Header("Status")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text rabbitHpText;
        [SerializeField] private TMP_Text guardText;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private UnityEngine.UI.Image playerHpFill;
        [SerializeField] private UnityEngine.UI.Image rabbitHpFill;

        [Header("Guidance")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text dealerLineText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text powerPreviewText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text calculationText;
        [SerializeField] private TMP_Text chainPreviewText;
        [SerializeField] private TMP_Text combatLogText;

        [Header("House Rule")]
        [SerializeField] private TMP_Text houseRuleProgressText;
        [SerializeField] private UnityEngine.UI.Image houseRuleProgressFill;

        [Header("Nudge")]
        [SerializeField] private GameObject nudgePanel;
        [SerializeField] private UnityEngine.UI.Button nudgeLeftButton;
        [SerializeField] private UnityEngine.UI.Button nudgeKeepButton;
        [SerializeField] private UnityEngine.UI.Button nudgeRightButton;

        [Header("Enemy Roulette")]
        [SerializeField] private RouletteController enemyRoulette;
        [SerializeField] private RouletteSpinController enemySpinController;
        [SerializeField] private UnityEngine.UI.Image[] enemyIntentFrames = new UnityEngine.UI.Image[0];
        [SerializeField] private TMP_Text enemyNextIntentText;
        [SerializeField, Min(0f)] private float rabbitSpinDelay = 0.35f;
        [SerializeField, Min(0f)] private float rabbitButtonPressLeadTime = 0.18f;
        [SerializeField] private UnityEvent onRabbitSpinRequested = new UnityEvent();

        [Header("Hijack")]
        [SerializeField] private GameObject hijackPanel;
        [SerializeField] private TMP_Text hijackInstructionText;
        [SerializeField] private UnityEngine.UI.Button[] hijackSourceButtons = new UnityEngine.UI.Button[0];
        [SerializeField] private TMP_Text[] hijackSourceLabels = new TMP_Text[0];
        [SerializeField] private UnityEngine.UI.Button[] hijackDestinationButtons = new UnityEngine.UI.Button[0];
        [SerializeField] private TMP_Text[] hijackDestinationLabels = new TMP_Text[0];

        [Header("Battle End")]
        [SerializeField] private GameObject endPanel;
        [SerializeField] private TMP_Text endTitleText;
        [SerializeField] private TMP_Text endBodyText;
        [SerializeField] private UnityEngine.UI.Button retryButton;

        [Header("Tuning")]
        [SerializeField, Min(1)] private int playerMaxHp = 24;
        [SerializeField, Min(1)] private int rabbitMaxHp = 24;
        [SerializeField] private int battleSeed = 46021;

        private readonly Queue<string> _combatLog = new Queue<string>();
        private BattleState _state;
        private int _playerHp;
        private int _rabbitHp;
        private int _guard;
        private int _ruleShards;
        private int _round;
        private int _rabbitIntentIndex;
        private bool _enemySpinFinished;
        private bool _hijackReady;
        private int _pendingResultIndex = -1;
        private int _selectedHijackSourceIndex = -1;

        public bool CanChooseSpinPower => _state == BattleState.AwaitingThrow;
        public UnityEvent OnRabbitSpinRequested => onRabbitSpinRequested;

        private void Awake()
        {
            nudgeLeftButton?.onClick.AddListener(() => ConfirmNudge(-1));
            nudgeKeepButton?.onClick.AddListener(() => ConfirmNudge(0));
            nudgeRightButton?.onClick.AddListener(() => ConfirmNudge(1));

            for (int i = 0; i < hijackSourceButtons.Length; i++)
            {
                int sourceIndex = i;
                hijackSourceButtons[i]?.onClick.AddListener(() => SelectHijackSource(sourceIndex));
            }

            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                int destinationIndex = i;
                hijackDestinationButtons[i]?.onClick.AddListener(() => PlaceHijackedSegment(destinationIndex));
            }

            retryButton?.onClick.AddListener(RestartBattle);
        }

        private void OnEnable()
        {
            if (spinController != null)
            {
                spinController.RouletteFinished += HandleSpinFinished;
            }

            if (enemySpinController != null)
            {
                enemySpinController.RouletteFinished += HandleEnemySpinFinished;
            }
        }

        private void Start()
        {
            RestartBattle();
        }

        private void OnDisable()
        {
            if (spinController != null)
            {
                spinController.RouletteFinished -= HandleSpinFinished;
            }

            if (enemySpinController != null)
            {
                enemySpinController.RouletteFinished -= HandleEnemySpinFinished;
            }
        }

        private void OnDestroy()
        {
            retryButton?.onClick.RemoveListener(RestartBattle);
        }

        public void PreviewSpinPower(float power)
        {
            if (!CanChooseSpinPower || powerPreviewText == null)
            {
                return;
            }

            string distance = power < 0.34f ? "가까운 구역" : power < 0.7f ? "중간 구역" : "먼 구역";
            powerPreviewText.text = $"{distance}을 향해 던집니다  |  마지막 2~3칸은 운";
        }

        public void ThrowRoulette(float power)
        {
            if (!CanChooseSpinPower || spinController == null)
            {
                return;
            }

            _state = BattleState.Spinning;
            instructionText.text = "손을 떠났습니다. 이제 룰렛이 결정합니다.";
            dealerLineText.text = "\"좋습니다, 손님. 힘은 정하셨고 운은 저희가 보관하겠습니다.\"";
            resultText.text = "회전 중...";
            calculationText.text = "착지 결과를 기다리는 중";
            nudgePanel?.SetActive(false);
            AddLog($"강도 {Mathf.RoundToInt(power * 100f)}%로 SPIN");
            _enemySpinFinished = enemySpinController == null;
            StartCoroutine(StartRabbitSpinAfterDelay(_rabbitIntentIndex % 4));
            spinController.Spin(power);
        }

        private IEnumerator StartRabbitSpinAfterDelay(int targetIntentIndex)
        {
            yield return new WaitForSecondsRealtime(rabbitSpinDelay);

            if (_state != BattleState.Spinning
                && _state != BattleState.ChoosingNudge
                && _state != BattleState.Resolving)
            {
                yield break;
            }

            onRabbitSpinRequested?.Invoke();
            if (rabbitButtonPressLeadTime > 0f)
            {
                yield return new WaitForSecondsRealtime(rabbitButtonPressLeadTime);
            }

            enemyRoulette?.PixelWheelRenderer?.ClearHighlight();
            enemyNextIntentText.text = "회전 중 · 결과 공개됨";
            enemySpinController?.SpinToSegment(targetIntentIndex);
        }

        private void RestartBattle()
        {
            StopAllCoroutines();
            _playerHp = playerMaxHp;
            _rabbitHp = rabbitMaxHp;
            _guard = 0;
            _ruleShards = 0;
            _round = 1;
            _rabbitIntentIndex = 0;
            _enemySpinFinished = true;
            _hijackReady = false;
            _pendingResultIndex = -1;
            _selectedHijackSourceIndex = -1;
            _combatLog.Clear();

            nudgePanel?.SetActive(false);
            hijackPanel?.SetActive(false);
            endPanel?.SetActive(false);
            spinController?.SetRandomSeed(battleSeed);
            enemySpinController?.SetRandomSeed(battleSeed + 1);
            roulette?.SetSegments(CreateStarterWheel(), false);
            enemyRoulette?.SetSegments(CreateRabbitWheel(), false);

            _state = BattleState.AwaitingThrow;
            dealerLineText.text = "\"무료 행운 1회입니다. 결과에 대한 책임은 손님께 있습니다.\"";
            instructionText.text = "버튼을 누르고 힘을 정한 뒤 놓으세요. 정확한 칸은 멈출 수 없습니다.";
            powerPreviewText.text = "강도는 착지 구역만 바꿉니다  |  마지막 2~3칸은 운";
            resultText.text = "첫 SPIN을 준비하세요";
            calculationText.text = "인접한 같은 칸은 합산되고, 앞의 ×2는 연쇄를 배가합니다.";
            AddLog("계약 체결: 토끼 딜러 전투 시작");
            spinInput?.ResetInput();
            RefreshAllUi();
        }

        private void HandleSpinFinished(RouletteSegmentData result)
        {
            if (_state != BattleState.Spinning || result == null)
            {
                return;
            }

            _pendingResultIndex = FindSegmentIndex(roulette, result);
            if (_pendingResultIndex < 0)
            {
                return;
            }

            _state = BattleState.ChoosingNudge;
            nudgePanel?.SetActive(true);
            instructionText.text = "착지했습니다. 이번 턴의 NUDGE를 사용하거나 결과를 유지하세요.";
            PreviewPendingResult("착지");
        }

        private void HandleEnemySpinFinished(RouletteSegmentData result)
        {
            _enemySpinFinished = true;
        }

        private IEnumerator ResolveRound(int resultIndex)
        {
            yield return new WaitUntil(() => _enemySpinFinished);

            ResolvePlayerResult(resultIndex);
            RefreshAllUi();
            yield return new WaitForSecondsRealtime(0.75f);

            if (_rabbitHp <= 0)
            {
                FinishBattle(true);
                yield break;
            }

            ResolveRabbitIntent();
            RefreshAllUi();
            yield return new WaitForSecondsRealtime(0.75f);

            if (_playerHp <= 0)
            {
                FinishBattle(false);
                yield break;
            }

            if (_hijackReady)
            {
                OpenHijackSelection();
                yield break;
            }

            BeginNextRound();
        }

        private void ConfirmNudge(int offset)
        {
            if (_state != BattleState.ChoosingNudge || roulette == null || roulette.Count == 0)
            {
                return;
            }

            _pendingResultIndex = (_pendingResultIndex + offset + roulette.Count) % roulette.Count;
            RouletteSegmentData chosen = roulette.GetSegment(_pendingResultIndex);
            roulette.HighlightSegment(chosen, 0.8f);
            nudgePanel?.SetActive(false);
            _state = BattleState.Resolving;

            string choice = offset < 0 ? "왼쪽" : offset > 0 ? "오른쪽" : "유지";
            AddLog($"NUDGE {choice}: {chosen.displayText}");
            PreviewPendingResult(choice);
            StartCoroutine(ResolveRound(_pendingResultIndex));
        }

        private void PreviewPendingResult(string prefix)
        {
            ResolvedEffect effect = EvaluateEffect(_pendingResultIndex);
            resultText.text = $"{prefix}  {effect.DisplayName} {effect.FinalValue}";
            calculationText.text = effect.Formula;
        }

        private void ResolvePlayerResult(int resultIndex)
        {
            ResolvedEffect effect = EvaluateEffect(resultIndex);
            calculationText.text = effect.Formula;

            switch (effect.Type)
            {
                case RouletteSegmentType.Damage:
                    _rabbitHp = Mathf.Max(0, _rabbitHp - effect.FinalValue);
                    resultText.text = $"최종 피해 {effect.FinalValue}";
                    AddLog($"플레이어: 연쇄 공격 {effect.FinalValue} 피해");
                    break;
                case RouletteSegmentType.Heal:
                    int healed = Mathf.Min(effect.FinalValue, playerMaxHp - _playerHp);
                    _playerHp += healed;
                    resultText.text = $"최종 회복 {healed}";
                    AddLog($"플레이어: HP {healed} 회복");
                    break;
                case RouletteSegmentType.Jackpot:
                    _rabbitHp = Mathf.Max(0, _rabbitHp - effect.FinalValue);
                    _ruleShards++;
                    resultText.text = $"JACKPOT! 피해 {effect.FinalValue}";
                    dealerLineText.text = "\"축하드립니다. 보안팀에도 즉시 공유하겠습니다.\"";
                    AddLog($"JACKPOT: {effect.FinalValue} 피해");
                    break;
                case RouletteSegmentType.Custom when effect.IsGuard:
                    _guard += effect.FinalValue;
                    resultText.text = $"최종 방어 {effect.FinalValue}";
                    AddLog($"플레이어: 방어 {effect.FinalValue} 획득");
                    break;
                default:
                    _ruleShards++;
                    resultText.text = "꽝... 규칙 조각 +1";
                    dealerLineText.text = "\"운이 없으시군요. 규칙을 고칠 재료는 드리겠습니다.\"";
                    AddLog("꽝: 규칙 조각 +1");
                    break;
            }
        }

        private void ResolveRabbitIntent()
        {
            int activeIndex = _rabbitIntentIndex % enemyRoulette.Count;
            RouletteSegmentData intent = enemyRoulette.GetSegment(activeIndex);
            if (intent.type == RouletteSegmentType.Damage)
            {
                ApplyRabbitDamage(intent.value);
            }
            else if (intent.type == RouletteSegmentType.Heal)
            {
                int healed = Mathf.Min(intent.value, rabbitMaxHp - _rabbitHp);
                _rabbitHp += healed;
                AddLog($"토끼: HP {healed} 회복");
                dealerLineText.text = "\"직원 복지는 계약서에 명시되어 있습니다.\"";
            }
            else
            {
                AddLog("토끼: 빼앗긴 칸이 비어 아무 일도 일어나지 않음");
                dealerLineText.text = "\"해당 규칙은 현재 손님 명의로 등록되어 있습니다.\"";
            }

            _rabbitIntentIndex++;
        }

        private void ApplyRabbitDamage(int damage)
        {
            int absorbed = Mathf.Min(_guard, damage);
            _guard -= absorbed;
            int healthDamage = damage - absorbed;
            _playerHp = Mathf.Max(0, _playerHp - healthDamage);
            AddLog($"토끼: 공격 {damage} (방어 흡수 {absorbed})");

            if (damage > 0 && absorbed == damage)
            {
                _hijackReady = true;
                AddLog("HOUSE RULE 달성: HIJACK 권한 획득");
                dealerLineText.text = "\"완전 방어라니요. 규정상 칸 하나를 양도하겠습니다.\"";
            }
            else
            {
                dealerLineText.text = "\"불편을 드려 죄송합니다. 다음 공격도 공개되어 있습니다.\"";
            }
        }

        private void OpenHijackSelection()
        {
            _state = BattleState.ChoosingHijack;
            _selectedHijackSourceIndex = -1;
            hijackPanel?.SetActive(true);
            hijackInstructionText.text = "1. 토끼의 칸 하나를 선택하세요";

            for (int i = 0; i < hijackSourceButtons.Length; i++)
            {
                bool valid = i < enemyRoulette.Count && !IsStolenSlot(enemyRoulette.GetSegment(i));
                hijackSourceButtons[i].interactable = valid;
                hijackSourceLabels[i].text = i < enemyRoulette.Count
                    ? $"{i + 1}\n{enemyRoulette.GetSegment(i).displayText}"
                    : "-";
            }

            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                hijackDestinationButtons[i].interactable = false;
                hijackDestinationLabels[i].text = i < roulette.Count
                    ? $"{i + 1}\n{roulette.GetSegment(i).displayText}"
                    : "-";
            }

            instructionText.text = "HOUSE RULE 달성. 토끼의 칸과 교체할 내 칸을 선택하세요.";
        }

        private void SelectHijackSource(int sourceIndex)
        {
            if (_state != BattleState.ChoosingHijack
                || sourceIndex < 0
                || sourceIndex >= enemyRoulette.Count
                || IsStolenSlot(enemyRoulette.GetSegment(sourceIndex)))
            {
                return;
            }

            _selectedHijackSourceIndex = sourceIndex;
            hijackInstructionText.text = $"2. {enemyRoulette.GetSegment(sourceIndex).displayText}과 교체할 내 칸 선택";
            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                hijackDestinationButtons[i].interactable = i < roulette.Count;
            }
        }

        private void PlaceHijackedSegment(int destinationIndex)
        {
            if (_state != BattleState.ChoosingHijack
                || _selectedHijackSourceIndex < 0
                || _selectedHijackSourceIndex >= enemyRoulette.Count
                || destinationIndex < 0
                || destinationIndex >= roulette.Count)
            {
                return;
            }

            RouletteSegmentData stolen = enemyRoulette.GetSegment(_selectedHijackSourceIndex).Clone();
            stolen.id = $"hijacked_{_round}_{stolen.id}";
            stolen.displayText = "H " + stolen.displayText;
            stolen.color = Color.Lerp(stolen.color, new Color32(224, 168, 52, 255), 0.35f);
            stolen.isSpecial = true;

            List<RouletteSegmentData> playerWheel = CloneWheel(roulette);
            playerWheel[destinationIndex] = stolen;
            roulette.SetSegments(playerWheel, false);

            List<RouletteSegmentData> rabbitWheel = CloneWheel(enemyRoulette);
            rabbitWheel[_selectedHijackSourceIndex] = CreateSegment(
                $"rabbit_stolen_{_round}_{_selectedHijackSourceIndex}",
                RouletteSegmentType.Custom,
                0,
                1f,
                new Color32(54, 48, 65, 255),
                "봉인",
                true);
            enemyRoulette.SetSegments(rabbitWheel, false);

            _hijackReady = false;
            hijackPanel?.SetActive(false);
            resultText.text = $"HIJACK  {stolen.displayText}";
            calculationText.text = $"토끼 { _selectedHijackSourceIndex + 1 }번 칸 → 내 {destinationIndex + 1}번 칸";
            AddLog($"HIJACK: {stolen.displayText}을 내 {destinationIndex + 1}번 칸에 배치");
            dealerLineText.text = "\"양도 처리가 완료됐습니다. 반환은 불가능합니다.\"";
            BeginNextRound();
        }

        private void BeginNextRound()
        {
            _round++;
            _state = BattleState.AwaitingThrow;
            _pendingResultIndex = -1;
            nudgePanel?.SetActive(false);
            instructionText.text = "바뀐 룰렛을 보고 다시 힘을 정하세요.";
            powerPreviewText.text = "강도는 착지 구역만 바꿉니다  |  마지막 2~3칸은 운";
            spinInput?.ResetInput();
            RefreshAllUi();
        }

        private void FinishBattle(bool victory)
        {
            _state = BattleState.Ended;
            endPanel?.SetActive(true);
            endTitleText.text = victory ? "규칙 탈취 성공" : "계약 갱신";
            endBodyText.text = victory
                ? "토끼 딜러의 초급 규칙을 훔쳤습니다.\n잘 멈춘 것이 아니라, 실패할 칸을 줄여 이겼습니다."
                : "이번 계약은 되감깁니다.\n다음에는 룰렛부터 더 유리하게 만드세요.";
            dealerLineText.text = victory
                ? "\"축하드립니다. 보안팀이 곧 안내해 드리겠습니다.\""
                : "\"재도전은 무료입니다. 미래의 행운은 별도 청구됩니다.\"";
        }

        private void RefreshAllUi()
        {
            playerHpText.text = $"손님  {_playerHp} / {playerMaxHp}";
            rabbitHpText.text = $"토끼 딜러  {_rabbitHp} / {rabbitMaxHp}";
            guardText.text = $"방어 {_guard}";
            shardText.text = $"규칙 조각 {_ruleShards}";
            playerHpFill.fillAmount = (float)_playerHp / playerMaxHp;
            rabbitHpFill.fillAmount = (float)_rabbitHp / rabbitMaxHp;
            houseRuleProgressFill.fillAmount = _hijackReady ? 1f : 0f;
            houseRuleProgressText.text = _hijackReady ? "1 / 1  ·  HIJACK 가능" : "0 / 1  ·  공개 공격 완전 방어";
            roundText.text = $"TUTORIAL TABLE   ·   ROUND {_round}";
            RefreshChainPreview();
            RefreshEnemyIntentUi();
        }

        private void RefreshEnemyIntentUi()
        {
            int activeIndex = _rabbitIntentIndex % enemyRoulette.Count;
            for (int i = 0; i < enemyIntentFrames.Length; i++)
            {
                enemyIntentFrames[i].color = i == activeIndex
                    ? new Color32(224, 168, 52, 255)
                    : new Color32(61, 53, 79, 255);
            }

            RouletteSegmentData intent = enemyRoulette.GetSegment(activeIndex);
            enemyNextIntentText.text = intent.type switch
            {
                RouletteSegmentType.Damage => $"NEXT  공격 {intent.value}",
                RouletteSegmentType.Heal => $"NEXT  회복 {intent.value}",
                _ => "NEXT  봉인 · 행동 없음"
            };

            if (enemyRoulette != null && activeIndex < enemyRoulette.Count)
            {
                RouletteSegmentData activeSegment = enemyRoulette.GetSegment(activeIndex);
                enemyRoulette.PixelWheelRenderer?.SetHighlightedSegment(activeSegment.id, true);

                if (enemyRoulette.TryGetSegmentAngles(activeIndex, out _, out _, out float centerAngle, out _))
                {
                    enemyRoulette.Wheel.localRotation = Quaternion.Euler(0f, 0f, centerAngle);
                }
            }
        }

        private void RefreshChainPreview()
        {
            ResolvedEffect best = null;
            for (int i = 0; i < roulette.Count; i++)
            {
                ResolvedEffect candidate = EvaluateEffect(i);
                if ((candidate.ChainCount > 1 || candidate.Multiplier > 1)
                    && (best == null || candidate.FinalValue > best.FinalValue))
                {
                    best = candidate;
                }
            }

            chainPreviewText.text = best == null
                ? "연쇄 미리보기 · 같은 효과를 인접시키세요"
                : "연쇄 미리보기 · " + best.Formula;
        }

        private ResolvedEffect EvaluateEffect(int selectedIndex)
        {
            ResolvedEffect effect = new ResolvedEffect();
            if (roulette == null || roulette.Count == 0 || selectedIndex < 0 || selectedIndex >= roulette.Count)
            {
                return effect;
            }

            int count = roulette.Count;
            int baseIndex = selectedIndex;
            int multiplierScan = 0;
            while (roulette.GetSegment(baseIndex).type == RouletteSegmentType.Multiplier && multiplierScan < count)
            {
                baseIndex = (baseIndex + 1) % count;
                multiplierScan++;
            }

            RouletteSegmentData baseSegment = roulette.GetSegment(baseIndex);
            int chainStart = baseIndex;
            if (IsChainable(baseSegment))
            {
                for (int i = 0; i < count - 1; i++)
                {
                    int previous = (chainStart - 1 + count) % count;
                    if (!HasSameEffect(baseSegment, roulette.GetSegment(previous)))
                    {
                        break;
                    }

                    chainStart = previous;
                }
            }

            List<int> values = new List<int>();
            int cursor = chainStart;
            do
            {
                RouletteSegmentData current = roulette.GetSegment(cursor);
                if (values.Count > 0 && !HasSameEffect(baseSegment, current))
                {
                    break;
                }

                values.Add(current.value);
                cursor = (cursor + 1) % count;
            }
            while (cursor != chainStart && IsChainable(baseSegment));

            int multiplier = 1;
            cursor = (chainStart - 1 + count) % count;
            int multiplierCount = 0;
            while (roulette.GetSegment(cursor).type == RouletteSegmentType.Multiplier
                   && multiplierCount < count - values.Count)
            {
                multiplier *= Mathf.Max(2, roulette.GetSegment(cursor).value);
                cursor = (cursor - 1 + count) % count;
                multiplierCount++;
            }

            int baseValue = 0;
            for (int i = 0; i < values.Count; i++)
            {
                baseValue += values[i];
            }

            effect.Type = baseSegment.type;
            effect.IsGuard = IsGuard(baseSegment);
            effect.ChainCount = values.Count;
            effect.Multiplier = multiplier;
            effect.FinalValue = baseValue * multiplier;
            effect.DisplayName = GetEffectName(baseSegment);

            string joinedValues = string.Join(" + ", values);
            effect.Formula = values.Count > 1
                ? $"{effect.DisplayName} {joinedValues} → {baseValue}"
                : $"{effect.DisplayName} {baseValue}";
            if (multiplier > 1)
            {
                effect.Formula += $"  ·  ×{multiplier} → 최종 {effect.FinalValue}";
            }
            else
            {
                effect.Formula += $" → 최종 {effect.FinalValue}";
            }

            return effect;
        }

        private static bool IsChainable(RouletteSegmentData segment)
        {
            return segment.type == RouletteSegmentType.Damage
                   || segment.type == RouletteSegmentType.Heal
                   || IsGuard(segment);
        }

        private static bool HasSameEffect(RouletteSegmentData first, RouletteSegmentData second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (IsGuard(first) || IsGuard(second))
            {
                return IsGuard(first) && IsGuard(second);
            }

            return first.type == second.type
                   && (first.type == RouletteSegmentType.Damage || first.type == RouletteSegmentType.Heal);
        }

        private static bool IsGuard(RouletteSegmentData segment)
        {
            return segment != null
                   && segment.type == RouletteSegmentType.Custom
                   && ((!string.IsNullOrEmpty(segment.id) && segment.id.Contains("guard"))
                       || (!string.IsNullOrEmpty(segment.displayText) && segment.displayText.Contains("방어")));
        }

        private static bool IsStolenSlot(RouletteSegmentData segment)
        {
            return segment == null
                   || (!string.IsNullOrEmpty(segment.id) && segment.id.StartsWith("rabbit_stolen"));
        }

        private static string GetEffectName(RouletteSegmentData segment)
        {
            if (IsGuard(segment))
            {
                return "방어";
            }

            return segment.type switch
            {
                RouletteSegmentType.Damage => "공격",
                RouletteSegmentType.Heal => "회복",
                RouletteSegmentType.Jackpot => "JACKPOT",
                RouletteSegmentType.Multiplier => "배율",
                _ => "꽝"
            };
        }

        private static int FindSegmentIndex(RouletteController controller, RouletteSegmentData segment)
        {
            for (int i = 0; i < controller.Count; i++)
            {
                RouletteSegmentData candidate = controller.GetSegment(i);
                if (ReferenceEquals(candidate, segment) || candidate.id == segment.id)
                {
                    return i;
                }
            }

            return -1;
        }

        private void AddLog(string message)
        {
            _combatLog.Enqueue(message);
            while (_combatLog.Count > 6)
            {
                _combatLog.Dequeue();
            }

            combatLogText.text = string.Join("\n", _combatLog);
        }

        private static List<RouletteSegmentData> CloneWheel(RouletteController controller)
        {
            List<RouletteSegmentData> copy = new List<RouletteSegmentData>(controller.Count);
            for (int i = 0; i < controller.Count; i++)
            {
                copy.Add(controller.GetSegment(i).Clone());
            }

            return copy;
        }

        private static List<RouletteSegmentData> CreateStarterWheel()
        {
            return new List<RouletteSegmentData>
            {
                CreateSegment("multiplier_2", RouletteSegmentType.Multiplier, 2, 1f, new Color32(57, 139, 191, 255), "×2", true),
                CreateSegment("damage_4_a", RouletteSegmentType.Damage, 4, 1f, new Color32(196, 67, 72, 255), "공격 4"),
                CreateSegment("damage_4_b", RouletteSegmentType.Damage, 4, 1f, new Color32(196, 67, 72, 255), "공격 4"),
                CreateSegment("guard_3", RouletteSegmentType.Custom, 3, 1f, new Color32(56, 153, 160, 255), "방어 3"),
                CreateSegment("heal_3", RouletteSegmentType.Heal, 3, 1f, new Color32(67, 166, 109, 255), "회복 3"),
                CreateSegment("blank_b", RouletteSegmentType.Custom, 0, 1f, new Color32(68, 62, 82, 255), "꽝"),
                CreateSegment("jackpot_8", RouletteSegmentType.Jackpot, 8, 1f, new Color32(222, 164, 48, 255), "JACK 8", true),
                CreateSegment("guard_4", RouletteSegmentType.Custom, 4, 1f, new Color32(56, 153, 160, 255), "방어 4")
            };
        }

        private static List<RouletteSegmentData> CreateRabbitWheel()
        {
            return new List<RouletteSegmentData>
            {
                CreateSegment("rabbit_attack_4_a", RouletteSegmentType.Damage, 4, 1f, new Color32(196, 67, 72, 255), "공격 4"),
                CreateSegment("rabbit_heal_3", RouletteSegmentType.Heal, 3, 1f, new Color32(67, 166, 109, 255), "회복 3"),
                CreateSegment("rabbit_attack_5", RouletteSegmentType.Damage, 5, 1f, new Color32(150, 46, 59, 255), "공격 5", true),
                CreateSegment("rabbit_attack_4_b", RouletteSegmentType.Damage, 4, 1f, new Color32(196, 67, 72, 255), "공격 4")
            };
        }

        private static RouletteSegmentData CreateSegment(
            string id,
            RouletteSegmentType type,
            int value,
            float weight,
            Color color,
            string label,
            bool special = false)
        {
            return new RouletteSegmentData(id, type, value, weight, color, null, label, special);
        }

        private sealed class ResolvedEffect
        {
            public RouletteSegmentType Type;
            public bool IsGuard;
            public int ChainCount;
            public int Multiplier = 1;
            public int FinalValue;
            public string DisplayName = "결과";
            public string Formula = "계산할 결과가 없습니다.";
        }
    }
}
