using System.Collections;
using System.Collections.Generic;
using RouletteLike.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 1층 토끼 딜러 튜토리얼 전투. 규칙은 전부 <see cref="PotBattle"/>가 계산하고,
    /// 이 컴포넌트는 입력·룰렛 회전·텍스트 표시만 맡는다.
    /// 한 턴: 앤티 → SPIN 반복(연쇄로 판돈 키우기) → CASH OUT 또는 하우스 몫.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RabbitBattlePrototype : MonoBehaviour
    {
        private enum ViewState
        {
            Busy,
            PlayerTurn,
            ChoosingHijack,
            Ended
        }

        /// <summary>튜토리얼: 이 라운드 전까지는 앤티가 1로 고정된다(R4부터 앤티 선택 해금).</summary>
        private const int AnteUnlockRound = 4;

        [Header("Roulette")]
        [SerializeField] private RouletteController roulette;
        [SerializeField] private RouletteSpinController spinController;
        [SerializeField] private HoldToSpinInput spinInput;

        [Header("Status")]
        [SerializeField] private TMP_Text playerChipsText;
        [SerializeField] private TMP_Text dealerChipsText;
        [SerializeField] private TMP_Text insuranceText;
        [SerializeField] private TMP_Text potText;
        [SerializeField] private UnityEngine.UI.Image playerChipsFill;
        [SerializeField] private UnityEngine.UI.Image dealerChipsFill;

        [Header("Bet Controls")]
        [SerializeField] private TMP_Text anteText;
        [SerializeField] private UnityEngine.UI.Button anteDownButton;
        [SerializeField] private UnityEngine.UI.Button anteUpButton;
        [SerializeField] private UnityEngine.UI.Button cashOutButton;
        [SerializeField] private TMP_Text cashOutLabel;

        [Header("Guidance")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text dealerLineText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text powerPreviewText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text calculationText;
        [SerializeField] private TMP_Text riskSummaryText;
        [SerializeField] private TMP_Text chainPreviewText;
        [SerializeField] private TMP_Text combatLogText;

        [Header("House Rule")]
        [SerializeField] private TMP_Text houseRuleProgressText;
        [SerializeField] private UnityEngine.UI.Image houseRuleProgressFill;
        [SerializeField] private GameObject houseRuleInfoPanel;
        [SerializeField] private UnityEngine.UI.Button houseRuleInfoButton;
        [SerializeField] private UnityEngine.UI.Button houseRuleInfoCloseButton;
        [SerializeField] private TMP_Text houseRuleDetailProgressText;

        [Header("Opening Speech")]
        [SerializeField] private GameObject openingSpeechBubble;
        [SerializeField, Min(0.5f)] private float openingSpeechDuration = 4.5f;

        [Header("Presentation")]
        [SerializeField] private BattlePresentationUI presentationUi;
        [SerializeField] private HijackTransferPresenter hijackTransferPresenter;
        [SerializeField, Min(0f)] private float resultPause = 0.6f;

        [Header("Tempo")]
        [Tooltip("한 턴의 두 번째 SPIN부터 회전 시간 배율. 첫 SPIN만 길게 보여주고 이어지는 SPIN은 1초 안에 끝낸다.")]
        [SerializeField, Range(0.1f, 1f)] private float followUpSpinScale = 0.3f;
        [Tooltip("딜러 턴 빨리 감기: 딜러 회전 시간 배율과 결과 확인 시간 배율.")]
        [SerializeField, Range(0.1f, 1f)] private float dealerFastForwardScale = 0.4f;

        [Header("Dealer Roulette")]
        [SerializeField] private RouletteController enemyRoulette;
        [SerializeField] private RouletteSpinController enemySpinController;
        [SerializeField] private TMP_Text enemyNextIntentText;
        [SerializeField, Min(0f)] private float dealerSpinDelay = 0.35f;
        [SerializeField, Min(0f)] private float rabbitButtonPressLeadTime = 0.18f;
        [SerializeField, Min(1)] private int dealerMaxSpinsPerTurn = 6;
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
        [SerializeField] private int battleSeed = 46021;

        private readonly Queue<string> _combatLog = new Queue<string>();
        private PotBattle _battle;
        private ViewState _state;
        private int _chosenAnte = 1;
        private int _loggedLines;
        private bool _playerSpinning;
        private int _playerSpinsThisTurn;
        private bool _dealerSpinFinished;
        private int _dealerLandingIndex;
        private bool _hijackTransferInProgress;
        private int _selectedHijackSourceIndex = -1;

        public bool CanChooseSpinPower => _state == ViewState.PlayerTurn && !_playerSpinning;
        public UnityEvent OnRabbitSpinRequested => onRabbitSpinRequested;

        private void Awake()
        {
            houseRuleInfoButton?.onClick.AddListener(OpenHouseRuleInfo);
            houseRuleInfoCloseButton?.onClick.AddListener(CloseHouseRuleInfo);
            anteDownButton?.onClick.AddListener(() => ChangeAnte(-1));
            anteUpButton?.onClick.AddListener(() => ChangeAnte(1));
            cashOutButton?.onClick.AddListener(PlayerCashOut);

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
            if (spinController != null) spinController.RouletteFinished += HandlePlayerSpinFinished;
            if (enemySpinController != null) enemySpinController.RouletteFinished += HandleDealerSpinFinished;
        }

        private void Start()
        {
            RestartBattle();
        }

        private void OnDisable()
        {
            if (spinController != null) spinController.RouletteFinished -= HandlePlayerSpinFinished;
            if (enemySpinController != null) enemySpinController.RouletteFinished -= HandleDealerSpinFinished;
        }

        private void OnDestroy()
        {
            houseRuleInfoButton?.onClick.RemoveListener(OpenHouseRuleInfo);
            houseRuleInfoCloseButton?.onClick.RemoveListener(CloseHouseRuleInfo);
            cashOutButton?.onClick.RemoveListener(PlayerCashOut);
            retryButton?.onClick.RemoveListener(RestartBattle);
        }

        // ───────────── 전투 흐름 ─────────────

        private void RestartBattle()
        {
            StopAllCoroutines();
            _battle = new PotBattle(
                BattlePresets.CreateStarterWheel(),
                BattlePresets.PlayerStartingChips,
                BattlePresets.CreateRabbitDealer(),
                battleSeed);
            _combatLog.Clear();
            _loggedLines = 0;
            _chosenAnte = 1;
            _playerSpinning = false;
            _hijackTransferInProgress = false;
            _selectedHijackSourceIndex = -1;

            hijackPanel?.SetActive(false);
            houseRuleInfoPanel?.SetActive(false);
            openingSpeechBubble?.SetActive(true);
            endPanel?.SetActive(false);
            spinController?.SetRandomSeed(battleSeed);
            if (spinController != null)
            {
                spinController.LandingBias = segment => LandingWeightFor(segment);
            }

            enemySpinController?.SetRandomSeed(battleSeed + 1);
            SyncWheels();

            presentationUi?.SetHouseRuleHighlighted(false);
            dealerLineText.text = "\"어서오세요, 첫 손님이시네요. 걸고, 돌리고, 적당할 때 터뜨리세요.\"";
            AddLog("계약 체결: 토끼 딜러 전투 시작");
            StartCoroutine(HideOpeningSpeechBubbleAfterDelay());
            StartCoroutine(BeginRound());
        }

        private IEnumerator BeginRound()
        {
            _state = ViewState.Busy;
            _battle.StartRound();
            FlushCoreLog();
            SyncWheels();
            RefreshAllUi();
            resultText.text = _battle.Active == Side.Player ? "코인플립: 앞면 · 손님 선공" : "코인플립: 뒷면 · 토끼 선공";
            calculationText.text = "선공은 매 라운드 동전으로 정합니다";
            yield return new WaitForSecondsRealtime(resultPause);
            yield return ContinueTurnFlow();
        }

        /// <summary>현재 코어 상태에 맞춰 다음 행동(플레이어 입력 대기, 딜러 턴, HIJACK, 다음 라운드, 종료)으로 넘어간다.</summary>
        private IEnumerator ContinueTurnFlow()
        {
            RefreshAllUi();

            if (_battle.Phase == BattlePhase.Ended)
            {
                FinishBattle();
                yield break;
            }

            if (_battle.HijackChances > 0 && !_battle.HijackUsedThisRound)
            {
                OpenHijackSelection();
                yield break;
            }

            if (_battle.Phase == BattlePhase.RoundOver)
            {
                yield return BeginRound();
                yield break;
            }

            if (_battle.Active == Side.Dealer)
            {
                yield return RunDealerTurn();
                yield return ContinueTurnFlow();
                yield break;
            }

            BeginPlayerTurn();
        }

        private void BeginPlayerTurn()
        {
            _state = ViewState.PlayerTurn;
            _playerSpinsThisTurn = 0;
            _chosenAnte = Mathf.Clamp(_chosenAnte, 1, Mathf.Max(1, CurrentMaxAnte()));
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Prepare);
            presentationUi?.SetPlayerRouletteActive(true);
            presentationUi?.SetDealerRouletteActive(false);
            instructionText.text = _battle.IsScriptedInstantCashOutRound && _battle.FirstThisRound == Side.Player
                ? "토끼가 이번에 앤티만으로 곧장 정산합니다. 보험 칸에 걸리면 전액 보장!"
                : AnteLocked
                    ? "SPIN을 길게 눌렀다 놓으세요. 앤티 1이 걸리고 판돈이 쌓입니다."
                    : "앤티를 고르고 SPIN. 판돈이 충분하면 CASH OUT.";
            powerPreviewText.text = "SPIN 밖으로 끌어내면 취소";
            spinInput?.ResetInput();
            RefreshAllUi();
        }

        // ───────────── 플레이어 입력 ─────────────

        private bool AnteLocked => _battle.Round < AnteUnlockRound;

        private int CurrentMaxAnte() => AnteLocked ? 1 : _battle.MaxAnte(Side.Player);

        private void ChangeAnte(int delta)
        {
            if (_state != ViewState.PlayerTurn || _battle.Phase != BattlePhase.AwaitingAnte)
            {
                return;
            }

            _chosenAnte = Mathf.Clamp(_chosenAnte + delta, 1, Mathf.Max(1, CurrentMaxAnte()));
            RefreshBetControls();
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

            if (_battle.Phase == BattlePhase.AwaitingAnte)
            {
                _battle.PlaceAnte(_chosenAnte);
                FlushCoreLog();
            }

            _playerSpinning = true;
            _playerSpinsThisTurn++;
            spinController.DurationScale = _playerSpinsThisTurn == 1 ? 1f : followUpSpinScale;
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Spin);
            instructionText.text = "손을 떠났습니다. 이제 룰렛이 결정합니다.";
            resultText.text = "회전 중...";
            calculationText.text = $"판돈 {_battle.Player.Pot} · 착지 결과를 기다리는 중";
            RefreshAllUi();
            spinController.Spin(power);
        }

        private void HandlePlayerSpinFinished(RouletteSegmentData result)
        {
            if (!_playerSpinning || result == null)
            {
                return;
            }

            _playerSpinning = false;
            int index = FindSegmentIndex(roulette, result);
            if (index < 0)
            {
                return;
            }

            LandingResult landing = _battle.Land(index);
            FlushCoreLog();
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Resolve);
            resultText.text = landing.Kind == SlotKind.HouseCut ? "하우스 몫!" : $"착지  {roulette.GetSegment(index).displayText}";
            calculationText.text = landing.Formula;

            if (landing.EndedTurn)
            {
                dealerLineText.text = "\"하우스 몫입니다. 테이블 위의 칩은 저희가 정리하겠습니다.\"";
                if (_battle.CounterHijackPending && _battle.ResolveCounterHijack() >= 0)
                {
                    FlushCoreLog();
                    SyncWheels();
                    dealerLineText.text = "\"하우스 몫에 이어 칸 하나도 압수하겠습니다. 이기시면 돌려드리죠.\"";
                }

                StartCoroutine(AfterPlayerTurnEnded());
                return;
            }

            instructionText.text = $"판돈 {_battle.Player.Pot}. 한 번 더 SPIN, 아니면 CASH OUT.";
            spinInput?.ResetInput();
            RefreshAllUi();
        }

        private void PlayerCashOut()
        {
            if (!CanChooseSpinPower || _battle.Phase != BattlePhase.Spinning)
            {
                return;
            }

            CashOutResult result = _battle.CashOut();
            FlushCoreLog();
            resultText.text = $"CASH OUT  피해 {result.Damage}";
            calculationText.text = $"판돈 {result.Pot} − 토끼 보험 {result.OpponentInsurance} = {result.Damage}";
            dealerLineText.text = result.Damage >= 8
                ? "\"크게 가져가시네요. 장부에 기록해 두겠습니다.\""
                : "\"정산 완료. 다음 판도 기대하겠습니다.\"";
            StartCoroutine(AfterPlayerTurnEnded());
        }

        private IEnumerator AfterPlayerTurnEnded()
        {
            _state = ViewState.Busy;
            spinInput?.ShowUnavailableState("턴 종료");
            RefreshAllUi();
            yield return new WaitForSecondsRealtime(resultPause);
            yield return ContinueTurnFlow();
        }

        // ───────────── 딜러 턴 ─────────────

        private IEnumerator RunDealerTurn()
        {
            _state = ViewState.Busy;
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Dealer);
            presentationUi?.SetPlayerRouletteActive(false);
            presentationUi?.SetDealerRouletteActive(true);
            spinInput?.ShowUnavailableState("토끼 차례");
            instructionText.text = $"토끼 차례 · 판돈 {_battle.Profile.CashOutAt} 이상이면 CASH OUT합니다.";

            _battle.PlaceAnte(_battle.Profile.DealerAnte);
            FlushCoreLog();
            RefreshAllUi();
            if (enemySpinController != null) enemySpinController.DurationScale = dealerFastForwardScale;
            float dealerPause = resultPause * dealerFastForwardScale;
            yield return new WaitForSecondsRealtime(dealerSpinDelay);

            int spins = 0;
            while (_battle.Phase == BattlePhase.Spinning && _battle.Active == Side.Dealer)
            {
                if (_battle.DealerWantsToCashOut() || spins >= dealerMaxSpinsPerTurn)
                {
                    CashOutResult cashOut = _battle.CashOut();
                    FlushCoreLog();
                    resultText.text = cashOut.FullCoverage ? "전액 보장!" : $"토끼 CASH OUT  피해 {cashOut.Damage}";
                    calculationText.text = $"판돈 {cashOut.Pot} − 내 보험 {cashOut.OpponentInsurance} = {cashOut.Damage}";
                    dealerLineText.text = cashOut.FullCoverage
                        ? "\"보험이 전액 보장했군요. 규정상 칸 하나를 양도하겠습니다.\""
                        : "\"정산하겠습니다. 손님 칩에서 받아 두었어요.\"";
                    presentationUi?.SetHouseRuleHighlighted(cashOut.FullCoverage);
                    break;
                }

                spins++;
                yield return SpinDealerWheel();
                LandingResult landing = _battle.Land(_dealerLandingIndex);
                FlushCoreLog();
                resultText.text = $"토끼 착지  {enemyRoulette.GetSegment(_dealerLandingIndex).displayText}";
                calculationText.text = landing.Formula;
                RefreshAllUi();
                yield return new WaitForSecondsRealtime(dealerPause);
            }

            RefreshAllUi();
            yield return new WaitForSecondsRealtime(resultPause);
        }

        private IEnumerator SpinDealerWheel()
        {
            _dealerLandingIndex = _battle.RollLandingIndex(Side.Dealer);
            _dealerSpinFinished = enemySpinController == null;
            onRabbitSpinRequested?.Invoke();
            if (rabbitButtonPressLeadTime > 0f)
            {
                yield return new WaitForSecondsRealtime(rabbitButtonPressLeadTime);
            }

            enemyRoulette?.PixelWheelRenderer?.ClearHighlight();
            enemySpinController?.SpinToSegment(_dealerLandingIndex);
            yield return new WaitUntil(() => _dealerSpinFinished);
        }

        private void HandleDealerSpinFinished(RouletteSegmentData result)
        {
            _dealerSpinFinished = true;
        }

        // ───────────── HIJACK ─────────────

        private void OpenHijackSelection()
        {
            _state = ViewState.ChoosingHijack;
            spinInput?.ShowUnavailableState("칸 선택 중");
            presentationUi?.SetHouseRuleHighlighted(true);
            _selectedHijackSourceIndex = -1;
            hijackPanel?.SetActive(true);
            hijackInstructionText.text = "1. 빼앗을 토끼의 칸을 고르세요";

            for (int i = 0; i < hijackSourceButtons.Length; i++)
            {
                bool exists = i < _battle.Dealer.Wheel.Count;
                Slot slot = exists ? _battle.Dealer.Wheel[i] : null;
                hijackSourceButtons[i].gameObject.SetActive(exists);
                hijackSourceButtons[i].interactable = exists && slot.Kind != SlotKind.Sealed;
                hijackSourceLabels[i].text = exists
                    ? $"{i + 1}\n{slot.Label}{(slot.IsJackpot ? " ★" : "")}"
                    : "-";
            }

            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                bool exists = i < _battle.Player.Wheel.Count;
                hijackDestinationButtons[i].gameObject.SetActive(exists);
                hijackDestinationButtons[i].interactable = false;
                hijackDestinationLabels[i].text = exists ? $"{i + 1}\n{_battle.Player.Wheel[i].Label}" : "-";
            }

            instructionText.text = "하우스 룰 달성. 토끼의 칸으로 내 칸 하나를 영구히 덮어씁니다.";
            RefreshAllUi();
        }

        private void SelectHijackSource(int sourceIndex)
        {
            if (_state != ViewState.ChoosingHijack
                || sourceIndex >= _battle.Dealer.Wheel.Count
                || _battle.Dealer.Wheel[sourceIndex].Kind == SlotKind.Sealed)
            {
                return;
            }

            _selectedHijackSourceIndex = sourceIndex;
            hijackInstructionText.text = $"2. {_battle.Dealer.Wheel[sourceIndex].Label}로 덮어쓸 내 칸을 고르세요 (하우스 몫은 불가)";
            for (int i = 0; i < hijackDestinationButtons.Length && i < _battle.Player.Wheel.Count; i++)
            {
                hijackDestinationButtons[i].interactable =
                    _battle.CanHijack(sourceIndex, i) == HijackError.None;
            }
        }

        private void PlaceHijackedSegment(int destinationIndex)
        {
            if (_state != ViewState.ChoosingHijack
                || _hijackTransferInProgress
                || _selectedHijackSourceIndex < 0
                || _battle.CanHijack(_selectedHijackSourceIndex, destinationIndex) != HijackError.None)
            {
                return;
            }

            StartCoroutine(PlaceHijackedSegmentRoutine(_selectedHijackSourceIndex, destinationIndex));
        }

        private IEnumerator PlaceHijackedSegmentRoutine(int sourceIndex, int destinationIndex)
        {
            _hijackTransferInProgress = true;
            foreach (UnityEngine.UI.Button button in hijackSourceButtons) button.interactable = false;
            foreach (UnityEngine.UI.Button button in hijackDestinationButtons) button.interactable = false;

            Slot stolenSlot = _battle.Dealer.Wheel[sourceIndex];
            RouletteSegmentData stolenView = ToSegment(stolenSlot.AsStolen("preview"));
            hijackPanel?.SetActive(false);

            bool applied = false;
            void ApplyTransfer()
            {
                if (applied) return;
                applied = true;
                _battle.Hijack(sourceIndex, destinationIndex);
                FlushCoreLog();
                SyncWheels();
            }

            if (hijackTransferPresenter != null)
            {
                yield return hijackTransferPresenter.PlayHijack(
                    enemyRoulette, sourceIndex, roulette, destinationIndex, stolenView, ApplyTransfer);
            }

            ApplyTransfer();
            _hijackTransferInProgress = false;
            presentationUi?.SetHouseRuleHighlighted(false);
            resultText.text = stolenSlot.IsJackpot ? $"JACKPOT HIJACK  {stolenSlot.Label}" : $"HIJACK  {stolenSlot.Label}";
            calculationText.text = $"토끼 {sourceIndex + 1}번 칸 → 내 {destinationIndex + 1}번 칸 (영구)";
            dealerLineText.text = stolenSlot.IsJackpot
                ? "\"「서비스」까지요? 보안팀을 불러드리겠습니다.\""
                : "\"양도 처리가 완료됐습니다. 반환은 불가능합니다.\"";
            _state = ViewState.Busy;
            yield return new WaitForSecondsRealtime(resultPause);
            yield return ContinueTurnFlow();
        }

        // ───────────── 종료 ─────────────

        private void FinishBattle()
        {
            _state = ViewState.Ended;
            spinInput?.ShowUnavailableState("전투 종료");
            endPanel?.SetActive(true);
            switch (_battle.Outcome)
            {
                case BattleOutcome.PlayerWinsByCleanSweep:
                    endTitleText.text = "완전 강탈";
                    endBodyText.text = "토끼의 룰렛을 전부 봉인했습니다.\n빼앗은 칸은 모두 손님의 것입니다.";
                    dealerLineText.text = "\"...제 룰렛이 텅 비었네요. 다음 테이블도 화이팅~\"";
                    break;
                case BattleOutcome.PlayerWinsByBankrupt:
                    endTitleText.text = "토끼 딜러 파산";
                    endBodyText.text = $"남은 칩 {_battle.Player.Chips}.\n빼앗은 칸은 손님의 룰렛에 영구히 남습니다.";
                    dealerLineText.text = "\"축하드립니다. 다음 테이블도 화이팅~\"";
                    break;
                default:
                    endTitleText.text = "계약 갱신";
                    endBodyText.text = "칩이 바닥났습니다. 이번 계약은 되감깁니다.\n다음에는 언제 멈출지 다시 정해 보세요.";
                    dealerLineText.text = "\"재도전은 무료입니다. 미래의 행운은 별도 청구됩니다.\"";
                    break;
            }

            RefreshAllUi();
        }

        // ───────────── 표시 ─────────────

        private void RefreshAllUi()
        {
            if (_battle == null) return;

            int playerStart = BattlePresets.PlayerStartingChips;
            int dealerStart = _battle.Profile.StartingChips;
            playerChipsText.text = $"칩 {_battle.Player.Chips}";
            dealerChipsText.text = $"칩 {_battle.Dealer.Chips}";
            playerChipsFill.fillAmount = Mathf.Clamp01((float)_battle.Player.Chips / playerStart);
            dealerChipsFill.fillAmount = Mathf.Clamp01((float)_battle.Dealer.Chips / dealerStart);
            insuranceText.text = $"내 보험 {_battle.Player.Insurance}";
            potText.text = $"내 판돈 {_battle.Player.Pot}";
            roundText.text = $"TUTORIAL TABLE   ·   ROUND {_battle.Round}";
            RefreshBetControls();
            RefreshHouseRuleUi();
            RefreshChainPreview();
            RefreshDealerTelegraph();
            RefreshOddsBoard();
        }

        private void RefreshBetControls()
        {
            bool awaitingAnte = _state == ViewState.PlayerTurn && _battle.Phase == BattlePhase.AwaitingAnte;
            bool canCashOut = _state == ViewState.PlayerTurn && !_playerSpinning && _battle.Phase == BattlePhase.Spinning
                              && _battle.Active == Side.Player;

            if (anteText != null)
            {
                anteText.text = _battle.Phase == BattlePhase.Spinning && _battle.Active == Side.Player
                    ? $"앤티 {_battle.Player.Ante} 걸림"
                    : AnteLocked ? "앤티 1 (R4 해금)" : $"앤티 {_chosenAnte} / 한도 {_battle.Profile.TableLimit}";
            }

            if (anteDownButton != null) anteDownButton.interactable = awaitingAnte && !AnteLocked && _chosenAnte > 1;
            if (anteUpButton != null) anteUpButton.interactable = awaitingAnte && !AnteLocked && _chosenAnte < CurrentMaxAnte();
            if (cashOutButton != null) cashOutButton.interactable = canCashOut;
            if (cashOutLabel != null)
            {
                cashOutLabel.text = canCashOut
                    ? $"CASH OUT\n피해 {_battle.PreviewCashOutDamage(Side.Player)}"
                    : "CASH OUT";
            }
        }

        /// <summary>확률판: 다음 SPIN의 하우스 몫 확률과 CASH OUT 예상 피해.</summary>
        private void RefreshOddsBoard()
        {
            if (riskSummaryText == null) return;

            int houseCuts = 0;
            foreach (Slot slot in _battle.Player.Wheel)
            {
                if (slot.Kind == SlotKind.HouseCut) houseCuts++;
            }

            float houseCutChance = 100f * houseCuts / Mathf.Max(1, _battle.Player.Wheel.Count);
            if (_battle.Active == Side.Player && _battle.Phase == BattlePhase.Spinning)
            {
                riskSummaryText.text =
                    $"판돈 {_battle.Player.Pot} − 토끼 보험 {_battle.Dealer.Insurance} = 피해 {_battle.PreviewCashOutDamage(Side.Player)}  ·  하우스 몫 {houseCutChance:0.#}%";
                riskSummaryText.color = new Color32(242, 194, 110, 255);
            }
            else
            {
                riskSummaryText.text = $"다음 SPIN 하우스 몫 확률 {houseCutChance:0.#}%  ·  토끼 판돈 {_battle.Dealer.Pot}";
                riskSummaryText.color = new Color32(202, 196, 212, 255);
            }
        }

        private void RefreshHouseRuleUi()
        {
            bool ready = _battle.HijackChances > 0;
            string progress = ready ? "1 / 1  ·  HIJACK 가능" : "0 / 1  ·  토끼의 CASH OUT을 보험으로 전액 보장";
            houseRuleProgressFill.fillAmount = ready ? 1f : 0f;
            houseRuleProgressText.text = progress;
            if (houseRuleDetailProgressText != null)
            {
                houseRuleDetailProgressText.text = "현재 진행도  " + progress;
            }
        }

        /// <summary>토끼는 튜토리얼 딜러라 다음 행동을 예고한다(성향 + 필요한 보험).</summary>
        private void RefreshDealerTelegraph()
        {
            if (enemyNextIntentText == null) return;

            int threshold = _battle.Profile.CashOutAt;
            int needed = Mathf.Max(0, threshold - _battle.Player.Insurance);
            if (_battle.IsScriptedInstantCashOutRound)
            {
                enemyNextIntentText.text = _battle.Player.Insurance > 0
                    ? $"이번 라운드: 앤티만 걸고 곧장 CASH OUT · 전액 보장 가능!"
                    : $"이번 라운드: 앤티만 걸고 곧장 CASH OUT · 보험 칸을 노리세요";
                return;
            }

            enemyNextIntentText.text = _battle.Active == Side.Dealer && _battle.Phase == BattlePhase.Spinning
                ? $"판돈 {_battle.Dealer.Pot} → {threshold} 이상이면 CASH OUT"
                : needed > 0
                    ? $"판돈 {threshold}+에서 CASH OUT · 보험 {needed} 부족"
                    : $"판돈 {threshold}+에서 CASH OUT · 전액 보장 가능성";
        }

        private void RefreshChainPreview()
        {
            if (chainPreviewText == null) return;

            int bestRaise = 0;
            for (int i = 0; i < _battle.Player.Wheel.Count; i++)
            {
                if (_battle.Player.Wheel[i].Kind != SlotKind.Raise) continue;
                int sum = 0;
                foreach (int index in PotBattle.FindChainGroup(_battle.Player.Wheel, i)) sum += _battle.Player.Wheel[index].Value;
                bestRaise = Mathf.Max(bestRaise, sum);
            }

            chainPreviewText.text = $"이어진 같은 칸은 한 묶음 · 최대 레이즈 연쇄 +{bestRaise}";
        }

        private void SyncWheels()
        {
            roulette?.SetSegments(ToSegments(_battle.Player.Wheel), false);
            enemyRoulette?.SetSegments(ToSegments(_battle.Dealer.Wheel), false);
        }

        private static List<RouletteSegmentData> ToSegments(IReadOnlyList<Slot> wheel)
        {
            List<RouletteSegmentData> segments = new List<RouletteSegmentData>(wheel.Count);
            foreach (Slot slot in wheel) segments.Add(ToSegment(slot));
            return segments;
        }

        private static RouletteSegmentData ToSegment(Slot slot)
        {
            (RouletteSegmentType type, Color color) = slot.Kind switch
            {
                SlotKind.Raise => (RouletteSegmentType.Damage, (Color)new Color32(196, 67, 72, 255)),
                SlotKind.Multiplier => (RouletteSegmentType.Multiplier, (Color)new Color32(57, 139, 191, 255)),
                SlotKind.Insurance => (RouletteSegmentType.Custom, (Color)new Color32(56, 153, 160, 255)),
                SlotKind.Dividend => (RouletteSegmentType.Heal, (Color)new Color32(67, 166, 109, 255)),
                SlotKind.HouseCut => (RouletteSegmentType.Poison, (Color)new Color32(40, 30, 48, 255)),
                _ => (RouletteSegmentType.Custom, (Color)new Color32(54, 48, 65, 255))
            };

            if (slot.IsJackpot)
            {
                type = RouletteSegmentType.Jackpot;
                color = new Color32(222, 164, 48, 255);
            }

            string label = slot.Label;
            if (slot.IsStolen)
            {
                label = "H " + label;
                color = Color.Lerp(color, new Color32(224, 168, 52, 255), 0.35f);
            }

            return new RouletteSegmentData(
                slot.Id, type, slot.Value, 1f, color, null, label,
                slot.IsJackpot || slot.IsStolen || slot.Kind == SlotKind.HouseCut);
        }

        /// <summary>역전 보정 무게. 코어가 계산하고 회전은 그 무게를 착지 구역 안에서만 반영한다.</summary>
        private float LandingWeightFor(RouletteSegmentData segment)
        {
            if (_battle == null || segment == null) return 1f;
            foreach (Slot slot in _battle.Player.Wheel)
            {
                if (slot.Id == segment.id) return _battle.LandingWeight(Side.Player, slot);
            }

            return 1f;
        }

        private static int FindSegmentIndex(RouletteController controller, RouletteSegmentData segment)
        {
            for (int i = 0; i < controller.Count; i++)
            {
                RouletteSegmentData candidate = controller.GetSegment(i);
                if (ReferenceEquals(candidate, segment) || candidate.id == segment.id) return i;
            }

            return -1;
        }

        /// <summary>코어가 남긴 전투 로그 중 아직 표시하지 않은 줄을 전투 기록에 옮긴다.</summary>
        private void FlushCoreLog()
        {
            IReadOnlyList<string> log = _battle.Log;
            for (; _loggedLines < log.Count; _loggedLines++)
            {
                AddLog(log[_loggedLines]);
            }
        }

        private void AddLog(string message)
        {
            _combatLog.Enqueue(message);
            while (_combatLog.Count > 7) _combatLog.Dequeue();
            combatLogText.text = string.Join("\n", _combatLog);
        }

        private IEnumerator HideOpeningSpeechBubbleAfterDelay()
        {
            yield return new WaitForSecondsRealtime(openingSpeechDuration);
            openingSpeechBubble?.SetActive(false);
        }

        private void OpenHouseRuleInfo()
        {
            houseRuleInfoPanel?.SetActive(true);
            RefreshHouseRuleUi();
        }

        private void CloseHouseRuleInfo()
        {
            houseRuleInfoPanel?.SetActive(false);
        }
    }
}
