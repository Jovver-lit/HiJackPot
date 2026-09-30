using System.Collections;
using System.Collections.Generic;
using RouletteLike.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 딜러와의 전투 화면과 런 진행(층 → 문 선택 → 다음 전투)을 맡는다.
    /// 규칙은 전부 코어(<see cref="Run"/>, <see cref="PotBattle"/>)가 계산하고,
    /// 이 컴포넌트는 입력·룰렛 회전·텍스트 표시만 한다.
    /// 한 턴: 앤티 → SPIN 반복(연쇄로 판돈 키우기) → CASH OUT 또는 하우스 몫.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DealerBattleController : MonoBehaviour
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
        [Tooltip("테이블 가운데 판돈 칩 더미(그레이박스). 판돈 2마다 칩 1개")]
        [SerializeField] private UnityEngine.UI.Image[] potChipImages = new UnityEngine.UI.Image[0];
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
        [SerializeField, Min(0f)] private float dealerButtonPressLeadTime = 0.18f;
        [SerializeField, Min(1)] private int dealerMaxSpinsPerTurn = 6;
        [SerializeField] private UnityEvent onDealerSpinRequested = new UnityEvent();

        [Header("Hijack")]
        [SerializeField] private GameObject hijackPanel;
        [SerializeField] private TMP_Text hijackInstructionText;
        [SerializeField] private UnityEngine.UI.Button[] hijackSourceButtons = new UnityEngine.UI.Button[0];
        [SerializeField] private TMP_Text[] hijackSourceLabels = new TMP_Text[0];
        [SerializeField] private UnityEngine.UI.Button[] hijackDestinationButtons = new UnityEngine.UI.Button[0];
        [SerializeField] private TMP_Text[] hijackDestinationLabels = new TMP_Text[0];

        [Header("Dealer Identity")]
        [SerializeField] private TMP_Text dealerNameText;
        [SerializeField] private TMP_Text dealerHeaderText;
        [SerializeField] private UnityEngine.UI.Image dealerSprite;
        [SerializeField] private TMP_Text houseRuleTitleText;
        [SerializeField] private TMP_Text houseRuleDescriptionText;
        [SerializeField] private TMP_Text hijackSourceTitleText;
        [Tooltip("딜러 룰렛 아래: 역탈취 경고와 딜러 특수 칸 설명")]
        [SerializeField] private TMP_Text dealerNoteText;

        [Header("House Rule Detail (딜러별)")]
        [SerializeField] private TMP_Text houseRuleDetailNameText;
        [SerializeField] private TMP_Text houseRuleDetailConditionText;
        [SerializeField] private TMP_Text houseRuleDetailRewardText;
        [SerializeField] private TMP_Text houseRuleDetailDealerText;
        [SerializeField] private TMP_Text houseRuleDetailWarningText;

        [Header("Battle End")]
        [SerializeField] private GameObject endPanel;
        [SerializeField] private TMP_Text endTitleText;
        [SerializeField] private TMP_Text endBodyText;
        [SerializeField] private UnityEngine.UI.Button endContinueButton;
        [SerializeField] private TMP_Text endContinueLabel;

        [Header("Outer Ring (그레이박스 띠)")]
        [SerializeField] private GameObject playerOuterRoot;
        [SerializeField] private UnityEngine.UI.Image[] playerOuterBoxes = new UnityEngine.UI.Image[0];
        [SerializeField] private TMP_Text[] playerOuterLabels = new TMP_Text[0];
        [SerializeField] private GameObject dealerOuterRoot;
        [SerializeField] private UnityEngine.UI.Image[] dealerOuterBoxes = new UnityEngine.UI.Image[0];
        [SerializeField] private TMP_Text[] dealerOuterLabels = new TMP_Text[0];

        [Header("Run · Doors")]
        [SerializeField] private GameObject doorPanel;
        [SerializeField] private TMP_Text doorFloorText;
        [SerializeField] private UnityEngine.UI.Button[] doorButtons = new UnityEngine.UI.Button[0];
        [SerializeField] private TMP_Text[] doorTitleTexts = new TMP_Text[0];
        [SerializeField] private TMP_Text[] doorBodyTexts = new TMP_Text[0];

        [Header("Tuning")]
        [SerializeField] private int battleSeed = 46021;

        private readonly Queue<string> _combatLog = new Queue<string>();
        private Run _run;
        private int _runCount;
        private bool _placingJackpot;
        private int _lastPlayerOuter = -1;
        private int _playerChipsBaseline = 1;
        private int _dealerChipsBaseline = 1;
        private int _lastDealerOuter = -1;

        /// <summary>보스를 이겨 해금한 바깥 링을 다음 런으로 넘기는 저장 키(메타 진행).</summary>
        private const string OuterRingUnlockedKey = "hijackpot.meta.outerRingUnlocked";
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
        public UnityEvent OnDealerSpinRequested => onDealerSpinRequested;

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
                hijackDestinationButtons[i]?.onClick.AddListener(() => OnDestinationChosen(destinationIndex));
            }

            for (int i = 0; i < doorButtons.Length; i++)
            {
                int doorIndex = i;
                doorButtons[i]?.onClick.AddListener(() => EnterDoor(doorIndex));
            }

            endContinueButton?.onClick.AddListener(OnEndContinue);
        }

        private void OnEnable()
        {
            if (spinController != null) spinController.RouletteFinished += HandlePlayerSpinFinished;
            if (enemySpinController != null) enemySpinController.RouletteFinished += HandleDealerSpinFinished;
        }

        private void Start()
        {
            StartNewRun();
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
            endContinueButton?.onClick.RemoveListener(OnEndContinue);
        }

        // ───────────── 런 흐름: 층 → 문 → 전투 ─────────────

        private void StartNewRun()
        {
            StopAllCoroutines();
            _runCount++;
            _run = new Run(
                battleSeed + _runCount * 1000,
                BattlePresets.PlayerStartingChips,
                BattlePresets.CreateStarterWheel(),
                BattlePresets.CreateRabbitDealer,
                BattlePresets.CreateDealerPool(),
                BattlePresets.CreateStageBoss,
                outerRing: PlayerPrefs.GetInt(OuterRingUnlockedKey, 0) == 1 ? BattlePresets.CreateOuterRing() : null);
            _placingJackpot = false;
            endPanel?.SetActive(false);
            ShowDoorsOrEnter();
        }

        /// <summary>1층(토끼)처럼 문이 하나뿐이면 바로 들어가고, 둘이면 문 선택 화면을 연다.</summary>
        private void ShowDoorsOrEnter()
        {
            if (_run.Doors.Count == 1)
            {
                EnterDoor(0);
                return;
            }

            doorPanel?.SetActive(true);
            hijackPanel?.SetActive(false);
            spinInput?.ShowUnavailableState("문 선택");
            if (doorFloorText != null)
            {
                doorFloorText.text = _run.CurrentFloorKind == FloorKind.Boss
                    ? $"{_run.FloorIndex + 1}층 · 보스 테이블  ·  칩 {_run.Chips}"
                    : $"{_run.FloorIndex + 1}층 · 어느 딜러의 룰렛을 털까?  ·  칩 {_run.Chips}";
            }

            for (int i = 0; i < doorButtons.Length; i++)
            {
                bool exists = i < _run.Doors.Count;
                doorButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                DealerProfile dealer = _run.Doors[i];
                doorTitleTexts[i].text = dealer.Name;
                doorBodyTexts[i].text = DescribeDealer(dealer);
            }
        }

        private void EnterDoor(int doorIndex)
        {
            if (_run == null || doorIndex >= _run.Doors.Count || _run.CurrentBattle != null)
            {
                return;
            }

            doorPanel?.SetActive(false);
            _battle = _run.EnterDoor(doorIndex);
            BeginBattle();
        }

        private void OnEndContinue()
        {
            if (_run == null || _run.Outcome != RunOutcome.InProgress || _battle == null || _battle.Outcome == BattleOutcome.DealerWins)
            {
                StartNewRun();
                return;
            }

            _run.CompleteBattle();
            endPanel?.SetActive(false);
            if (_run.Outcome == RunOutcome.Escaped)
            {
                ShowRunEscaped();
                return;
            }

            if (_run.PendingJackpot != null)
            {
                OpenJackpotPlacement();
                return;
            }

            ShowDoorsOrEnter();
        }

        private void ShowRunEscaped()
        {
            _state = ViewState.Ended;
            endPanel?.SetActive(true);
            endTitleText.text = "탈출 성공";
            endBodyText.text = $"남은 칩 {_run.Chips}.\n빼앗은 규칙을 들고 카지노 문을 나섰습니다.";
            if (_run.UnlockedOuterRingThisRun)
            {
                PlayerPrefs.SetInt(OuterRingUnlockedKey, 1);
                PlayerPrefs.Save();
                endBodyText.text += "\n\n룰렛 형식 해금: 다음 런부터 내 룰렛에 바깥 링이 붙습니다.";
            }
            if (endContinueLabel != null) endContinueLabel.text = "새 계약";
            dealerLineText.text = "\"다음에 또 오세요. 당첨 확률은 공개하지 않습니다.\"";
        }

        /// <summary>완전 강탈로 확정 획득한 JACKPOT 칸을 둘 자리를 고른다. HIJACK 패널의 내 칸 버튼을 재사용한다.</summary>
        private void OpenJackpotPlacement()
        {
            _placingJackpot = true;
            _state = ViewState.ChoosingHijack;
            hijackPanel?.SetActive(true);
            hijackInstructionText.text = $"완전 강탈 보상: {_run.PendingJackpot.Label}(JACKPOT)을 둘 내 칸을 고르세요 (하우스 몫 불가)";
            if (hijackSourceTitleText != null) hijackSourceTitleText.text = "";
            foreach (UnityEngine.UI.Button button in hijackSourceButtons) button.gameObject.SetActive(false);
            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                bool exists = i < _run.Wheel.Count;
                hijackDestinationButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                hijackDestinationButtons[i].interactable = _run.Wheel[i].Kind != SlotKind.HouseCut;
                hijackDestinationLabels[i].text = $"{i + 1}\n{_run.Wheel[i].Label}";
            }
        }

        private void OnDestinationChosen(int destinationIndex)
        {
            if (_placingJackpot)
            {
                if (_run.PlacePendingJackpot(destinationIndex))
                {
                    _placingJackpot = false;
                    hijackPanel?.SetActive(false);
                    ShowDoorsOrEnter();
                }

                return;
            }

            PlaceHijackedSegment(destinationIndex);
        }

        // ───────────── 전투 흐름 ─────────────

        private void BeginBattle()
        {
            StopAllCoroutines();
            _lastPlayerOuter = -1;
            _lastDealerOuter = -1;
            _playerChipsBaseline = Mathf.Max(1, _battle.Player.Chips);
            _dealerChipsBaseline = Mathf.Max(1, _battle.Dealer.Chips);
            _combatLog.Clear();
            _loggedLines = 0;
            _chosenAnte = 1;
            _playerSpinning = false;
            _hijackTransferInProgress = false;
            _selectedHijackSourceIndex = -1;

            hijackPanel?.SetActive(false);
            houseRuleInfoPanel?.SetActive(false);
            openingSpeechBubble?.SetActive(_battle.Profile.Telegraphs);
            endPanel?.SetActive(false);
            instructionText.text = $"{_battle.Profile.Name} 테이블에 앉았습니다. 코인플립으로 선공을 정합니다.";
            powerPreviewText.text = "";
            spinInput?.ShowUnavailableState("준비 중");
            spinController?.SetRandomSeed(battleSeed);
            if (spinController != null)
            {
                spinController.LandingBias = segment => LandingWeightFor(segment);
            }

            enemySpinController?.SetRandomSeed(battleSeed + 1);
            SyncWheels();

            presentationUi?.SetHouseRuleHighlighted(false);
            ApplyDealerIdentity();
            dealerLineText.text = _battle.Profile.Telegraphs
                ? "\"어서오세요, 첫 손님이시네요. 걸고, 돌리고, 적당할 때 터뜨리세요.\""
                : $"\"{_battle.Profile.Name}입니다. 오늘의 하우스 룰은 게시판에 붙어 있습니다.\"";
            AddLog($"{_run.FloorIndex + 1}층: {_battle.Profile.Name} 전투 시작");
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
            resultText.text = _battle.Active == Side.Player ? "코인플립: 손님 선공" : $"코인플립: {DealerShortName} 선공";
            calculationText.text = _battle.RoundStartEffects.Count > 0
                ? "[라운드 시작] " + string.Join("  ·  ", _battle.RoundStartEffects)
                : "선공은 매 라운드 동전으로 정합니다";
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
                ? $"{DealerShortName}가 이번에 앤티만으로 곧장 정산합니다. 보험 칸에 걸리면 전액 보장!"
                : AnteLocked
                    ? "SPIN을 길게 눌렀다 놓으세요. 앤티 1이 걸리고 판돈이 쌓입니다."
                    : "앤티를 고르고 SPIN. 판돈이 충분하면 CASH OUT.";
            powerPreviewText.text = "SPIN 밖으로 끌어내면 취소";
            spinInput?.ResetInput();
            RefreshAllUi();
        }

        // ───────────── 플레이어 입력 ─────────────

        /// <summary>튜토리얼(예고하는 딜러)에서만 앤티가 R4 전까지 1로 묶인다.</summary>
        private bool AnteLocked => _battle.Profile.Telegraphs && _battle.Round < AnteUnlockRound;

        private string DealerShortName => _battle.Profile.Name.Replace(" 딜러", "");

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

            _lastPlayerOuter = _battle.RollOuterIndex(Side.Player);
            LandingResult landing = _battle.Land(index, _lastPlayerOuter);
            FlushCoreLog();
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Resolve);
            resultText.text = DescribeLanding(landing, roulette.GetSegment(index).displayText);
            calculationText.text = landing.Formula;

            if (landing.EndedTurn)
            {
                dealerLineText.text = "\"하우스 몫입니다. 테이블 위의 칩은 저희가 정리하겠습니다.\"";
                if (_battle.CounterHijackPending)
                {
                    StartCoroutine(PlayCounterHijackThenContinue());
                    return;
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
            calculationText.text = $"판돈 {result.Pot} − {DealerShortName} 보험 {result.OpponentInsurance} = {result.Damage}";
            dealerLineText.text = result.Damage >= 8
                ? "\"크게 가져가시네요. 장부에 기록해 두겠습니다.\""
                : "\"정산 완료. 다음 판도 기대하겠습니다.\"";
            StartCoroutine(AfterPlayerTurnEnded());
        }

        /// <summary>
        /// 역탈취: 압수되는 내 칸이 딜러 쪽으로 끌려가는 연출(HIJACK의 반대 방향) 뒤에 봉인을 반영한다.
        /// 왜 당했는지(하우스 몫)와 되찾는 법(이기면 반환)을 함께 알린다.
        /// </summary>
        private IEnumerator PlayCounterHijackThenContinue()
        {
            _state = ViewState.Busy;
            spinInput?.ShowUnavailableState("역탈취");
            int preview = FindCounterHijackTarget();
            RouletteSegmentData token = preview >= 0 ? ToSegment(_battle.Player.Wheel[preview]) : null;
            resultText.text = "역탈취!";
            calculationText.text = $"하우스 몫에 걸려 {DealerShortName}가 내 칸 하나를 압수합니다 (이번 전투 {PotBattle.MaxCounterHijacksPerBattle}회, 이기면 반환)";

            bool applied = false;
            void Apply()
            {
                if (applied) return;
                applied = true;
                int seized = _battle.ResolveCounterHijack();
                FlushCoreLog();
                SyncWheels();
                if (seized >= 0)
                {
                    dealerLineText.text = $"\"하우스 몫에 이어 {seized + 1}번 칸도 압수하겠습니다. 이기시면 돌려드리죠.\"";
                }
            }

            if (hijackTransferPresenter != null && token != null && preview >= 0)
            {
                yield return hijackTransferPresenter.PlayHijack(roulette, preview, enemyRoulette, 0, token, Apply);
            }

            Apply();
            RefreshAllUi();
            yield return new WaitForSecondsRealtime(resultPause * 1.5f);
            yield return ContinueTurnFlow();
        }

        /// <summary>코어의 역탈취 대상 규칙(빼앗아 온 칸 우선, 없으면 값이 큰 칸)을 연출용으로 미리 계산한다.</summary>
        private int FindCounterHijackTarget()
        {
            int target = -1;
            IReadOnlyList<Slot> wheel = _battle.Player.Wheel;
            for (int i = 0; i < wheel.Count; i++)
            {
                if (wheel[i].Kind == SlotKind.HouseCut || wheel[i].Kind == SlotKind.Sealed) continue;
                if (target < 0) { target = i; continue; }
                bool better = wheel[i].IsStolen != wheel[target].IsStolen ? wheel[i].IsStolen : wheel[i].Value > wheel[target].Value;
                if (better) target = i;
            }

            return target;
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
            spinInput?.ShowUnavailableState($"{DealerShortName} 차례");
            instructionText.text = $"{DealerShortName} 차례 · 판돈 {_battle.Profile.CashOutAt} 이상이면 CASH OUT합니다.";

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
                    resultText.text = cashOut.FullCoverage ? "전액 보장!" : $"{DealerShortName} CASH OUT  피해 {cashOut.Damage}";
                    calculationText.text = $"판돈 {cashOut.Pot} − 내 보험 {cashOut.OpponentInsurance} = {cashOut.Damage}";
                    dealerLineText.text = cashOut.FullCoverage
                        ? "\"보험이 전액 보장했군요. 규정상 칸 하나를 양도하겠습니다.\""
                        : "\"정산하겠습니다. 손님 칩에서 받아 두었어요.\"";
                    presentationUi?.SetHouseRuleHighlighted(cashOut.FullCoverage);
                    break;
                }

                spins++;
                yield return SpinDealerWheel();
                _lastDealerOuter = _battle.RollOuterIndex(Side.Dealer);
                LandingResult landing = _battle.Land(_dealerLandingIndex, _lastDealerOuter);
                FlushCoreLog();
                resultText.text = landing.HouseCutHit
                    ? $"{DealerShortName}의 하우스 몫! 판돈 증발"
                    : DealerShortName + " " + DescribeLanding(landing, enemyRoulette.GetSegment(_dealerLandingIndex).displayText);
                if (landing.HouseCutHit)
                {
                    dealerLineText.text = "\"...하우스는 원래 저희 편인데요.\"";
                    break;
                }
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
            onDealerSpinRequested?.Invoke();
            if (dealerButtonPressLeadTime > 0f)
            {
                yield return new WaitForSecondsRealtime(dealerButtonPressLeadTime);
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
            hijackInstructionText.text = $"1. 빼앗을 {DealerShortName}의 칸을 고르세요 ([JP] = JACKPOT, 하우스 몫 불가)";
            if (hijackSourceTitleText != null) hijackSourceTitleText.text = $"{_battle.Profile.Name} 룰렛 · 빼앗을 칸";

            for (int i = 0; i < hijackSourceButtons.Length; i++)
            {
                bool exists = i < _battle.Dealer.Wheel.Count;
                Slot slot = exists ? _battle.Dealer.Wheel[i] : null;
                hijackSourceButtons[i].gameObject.SetActive(exists);
                hijackSourceButtons[i].interactable = exists && slot.Kind != SlotKind.Sealed && slot.Kind != SlotKind.HouseCut;
                hijackSourceLabels[i].text = exists
                    ? $"{i + 1}\n{slot.Label}{(slot.IsJackpot ? " [JP]" : "")}"
                    : "-";
            }

            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                bool exists = i < _battle.Player.Wheel.Count;
                hijackDestinationButtons[i].gameObject.SetActive(exists);
                hijackDestinationButtons[i].interactable = false;
                hijackDestinationLabels[i].text = exists ? $"{i + 1}\n{_battle.Player.Wheel[i].Label}" : "-";
            }

            instructionText.text = $"하우스 룰 달성. {DealerShortName}의 칸으로 내 칸 하나를 영구히 덮어씁니다.";
            RefreshAllUi();
        }

        private void SelectHijackSource(int sourceIndex)
        {
            if (_state != ViewState.ChoosingHijack
                || sourceIndex >= _battle.Dealer.Wheel.Count
                || _battle.Dealer.Wheel[sourceIndex].Kind == SlotKind.Sealed
                || _battle.Dealer.Wheel[sourceIndex].Kind == SlotKind.HouseCut)
            {
                return;
            }

            _selectedHijackSourceIndex = sourceIndex;
            Slot chosen = _battle.Dealer.Wheel[sourceIndex];
            hijackInstructionText.text = $"2. 「{chosen.Label}」 {SlotDescriptions.Describe(chosen)}\n   이 칸으로 덮어쓸 내 칸을 고르세요 (하우스 몫은 불가)";
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
            calculationText.text = $"{DealerShortName} {sourceIndex + 1}번 칸 → 내 {destinationIndex + 1}번 칸 (영구)";
            dealerLineText.text = stolenSlot.IsJackpot
                ? $"\"「{stolenSlot.Label}」까지요? 보안팀을 불러드리겠습니다.\""
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
            bool lastFloor = _run.FloorIndex >= _run.FloorCount - 1;
            int winnings = (int)System.Math.Round(_battle.Profile.StartingChips * Run.WinningsRatio);
            switch (_battle.Outcome)
            {
                case BattleOutcome.PlayerWinsByCleanSweep:
                    endTitleText.text = "완전 강탈";
                    endBodyText.text = $"{_battle.Profile.Name}의 룰렛을 전부 봉인했습니다.\n상금 대신 JACKPOT 칸을 가져갑니다.";
                    dealerLineText.text = "\"...제 룰렛이 텅 비었네요. 다음 테이블도 화이팅~\"";
                    break;
                case BattleOutcome.PlayerWinsByBankrupt:
                    endTitleText.text = $"{_battle.Profile.Name} 파산";
                    endBodyText.text = $"남은 칩 {_battle.Player.Chips} + 상금 {winnings}.\n빼앗은 칸은 손님의 룰렛에 영구히 남습니다.";
                    dealerLineText.text = "\"축하드립니다. 다음 테이블도 화이팅~\"";
                    break;
                default:
                    endTitleText.text = "계약 갱신";
                    endBodyText.text = $"{_run.FloorIndex + 1}층에서 칩이 바닥났습니다. 이번 계약은 되감깁니다.\n다음에는 언제 멈출지 다시 정해 보세요.";
                    dealerLineText.text = "\"재도전은 무료입니다. 미래의 행운은 별도 청구됩니다.\"";
                    break;
            }

            if (endContinueLabel != null)
            {
                endContinueLabel.text = _battle.Outcome == BattleOutcome.DealerWins
                    ? "계약 되감기"
                    : lastFloor ? "탈출하기" : $"{_run.FloorIndex + 2}층으로";
            }

            RefreshAllUi();
        }

        // ───────────── 표시 ─────────────

        private void RefreshAllUi()
        {
            if (_battle == null) return;

            // 칩 바는 이번 전투를 시작할 때의 칩을 가득 찬 기준으로 삼는다(상금으로 20을 넘어도 줄어드는 게 보이게).
            _playerChipsBaseline = Mathf.Max(_playerChipsBaseline, _battle.Player.Chips);
            _dealerChipsBaseline = Mathf.Max(_dealerChipsBaseline, _battle.Dealer.Chips);
            int playerStart = _playerChipsBaseline;
            int dealerStart = _dealerChipsBaseline;
            playerChipsText.text = $"칩 {_battle.Player.Chips}";
            dealerChipsText.text = $"칩 {_battle.Dealer.Chips}";
            playerChipsFill.fillAmount = Mathf.Clamp01((float)_battle.Player.Chips / playerStart);
            dealerChipsFill.fillAmount = Mathf.Clamp01((float)_battle.Dealer.Chips / dealerStart);
            insuranceText.text = $"내 보험 {_battle.Player.Insurance}";
            Seat potSeat = _battle.Active == Side.Dealer && _battle.Phase == BattlePhase.Spinning ? _battle.Dealer : _battle.Player;
            potText.text = potSeat == _battle.Dealer ? $"{DealerShortName} 판돈 {potSeat.Pot}" : $"판돈 {potSeat.Pot}";
            int chipsShown = Mathf.Min(potChipImages.Length, (potSeat.Pot + 1) / 2);
            for (int i = 0; i < potChipImages.Length; i++)
            {
                potChipImages[i].gameObject.SetActive(i < chipsShown);
            }
            roundText.text = _battle.Profile.Telegraphs
                ? $"{_run.FloorIndex + 1}F · TUTORIAL TABLE   ·   ROUND {_battle.Round}"
                : $"{_run.FloorIndex + 1}F · {_battle.Profile.Name}   ·   ROUND {_battle.Round}";
            RefreshBetControls();
            RefreshHouseRuleUi();
            RefreshChainPreview();
            RefreshDealerTelegraph();
            RefreshOddsBoard();
            RefreshSpecialNotes();
            RefreshHouseRuleDetail();
            RefreshOuterRing(_battle.Player, playerOuterRoot, playerOuterBoxes, playerOuterLabels, _lastPlayerOuter);
            RefreshOuterRing(_battle.Dealer, dealerOuterRoot, dealerOuterBoxes, dealerOuterLabels, _lastDealerOuter);
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
                    $"판돈 {_battle.Player.Pot} − {DealerShortName} 보험 {_battle.Dealer.Insurance} = 피해 {_battle.PreviewCashOutDamage(Side.Player)}  ·  하우스 몫 {houseCutChance:0.#}%";
                riskSummaryText.color = new Color32(242, 194, 110, 255);
            }
            else
            {
                riskSummaryText.text = $"다음 SPIN 하우스 몫 확률 {houseCutChance:0.#}%  ·  {DealerShortName} 판돈 {_battle.Dealer.Pot}";
                riskSummaryText.color = new Color32(202, 196, 212, 255);
            }
        }

        private void RefreshHouseRuleUi()
        {
            bool ready = _battle.HijackChances > 0;
            int goal = _battle.HouseRuleGoal;
            string progress = ready
                ? $"{goal} / {goal}  ·  HIJACK 가능"
                : $"{_battle.HouseRuleProgress} / {goal}  ·  {HouseRuleDescription(_battle.Profile)}";
            houseRuleProgressFill.fillAmount = ready ? 1f : (float)_battle.HouseRuleProgress / goal;
            houseRuleProgressText.text = progress;
            if (houseRuleDetailProgressText != null)
            {
                houseRuleDetailProgressText.text = "현재 진행도  " + progress;
            }
        }

        /// <summary>튜토리얼 딜러는 다음 행동을 예고하고(필요한 보험), 나머지 딜러는 공개된 성향만 보여준다.</summary>
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

            if (_battle.Active == Side.Dealer && _battle.Phase == BattlePhase.Spinning)
            {
                enemyNextIntentText.text = $"판돈 {_battle.Dealer.Pot} → {threshold} 이상이면 CASH OUT";
                return;
            }

            if (!_battle.Profile.Telegraphs)
            {
                enemyNextIntentText.text = $"판돈 {threshold}+에서 CASH OUT · 기본 보험 {_battle.Profile.BaseInsurance}";
                return;
            }

            enemyNextIntentText.text = needed > 0
                ? $"판돈 {threshold}+에서 CASH OUT · 보험 {needed} 부족"
                : $"판돈 {threshold}+에서 CASH OUT · 전액 보장 가능성";
        }

        /// <summary>룰렛 아래 안내: 내 특수 칸 효과, 딜러의 역탈취 경고와 특수 칸 효과.</summary>
        private void RefreshSpecialNotes()
        {
            if (dealerNoteText != null)
            {
                string warning = !_battle.Profile.CounterHijacks
                    ? "역탈취 없음 (튜토리얼)"
                    : _battle.CounterHijacksRemaining > 0
                        ? $"[!] 역탈취: 내가 하우스 몫에 걸리면 내 칸 1개 압수 (이번 전투 {_battle.CounterHijacksRemaining}회 남음, 이기면 반환)"
                        : "역탈취 소진: 이번 전투에는 더 압수하지 않음";
                string specials = SlotDescriptions.DescribeSpecials(_battle.Dealer.Wheel, "");
                dealerNoteText.text = string.IsNullOrEmpty(specials) ? warning : warning + "\n" + specials;
            }

            if (chainPreviewText != null)
            {
                string mine = SlotDescriptions.DescribeSpecials(_battle.Player.Wheel, "내 ");
                if (!string.IsNullOrEmpty(mine)) chainPreviewText.text = mine;
            }
        }

        /// <summary>하우스 룰 상세 페이지를 지금 딜러 기준으로 채운다.</summary>
        private void RefreshHouseRuleDetail()
        {
            DealerProfile dealer = _battle.Profile;
            if (houseRuleDetailNameText != null) houseRuleDetailNameText.text = $"{HouseRuleName(dealer.HouseRule)} · {dealer.Name}";
            if (houseRuleDetailConditionText != null) houseRuleDetailConditionText.text = $"{HouseRuleDescription(dealer)} ({_battle.HouseRuleProgress}/{_battle.HouseRuleGoal})";
            if (houseRuleDetailRewardText != null) houseRuleDetailRewardText.text = $"{dealer.Name}의 칸 하나로 내 칸 하나를 영구히 덮어쓴다 (라운드당 1회)";
            if (houseRuleDetailDealerText != null)
            {
                string specials = SlotDescriptions.DescribeSpecials(dealer.Wheel, "");
                houseRuleDetailDealerText.text = "빼앗긴 칸은 봉인. 하우스 몫 외 전부 봉인하면 완전 강탈.\n" + specials;
            }

            if (houseRuleDetailWarningText != null)
            {
                houseRuleDetailWarningText.text = dealer.CounterHijacks
                    ? $"역탈취: 내가 하우스 몫에 걸리면 {dealer.Name}가 내 칸 1개를 압수한다(전투당 {PotBattle.MaxCounterHijacksPerBattle}회, 이기면 반환)."
                    : "튜토리얼 딜러는 역탈취하지 않습니다.";
            }
        }

        private static string DescribeLanding(LandingResult landing, string innerLabel)
        {
            if (landing.CutShielded) return "보호막! 하우스 몫을 막았다";
            if (landing.HouseCutHit) return landing.Kind == SlotKind.HouseCut ? "하우스 몫!" : "바깥 하우스 몫!";
            if (landing.KeywordTriggered) return $"특수 칸 발동!  {innerLabel}";
            return landing.JackpotLine ? $"잭팟 라인!  {innerLabel}" : $"착지  {innerLabel}";
        }

        /// <summary>바깥 링 띠(그레이박스): 칸 이름을 보여주고 방금 멈춘 칸을 금색으로 강조한다.</summary>
        private static void RefreshOuterRing(Seat seat, GameObject root, UnityEngine.UI.Image[] boxes, TMP_Text[] labels, int highlighted)
        {
            if (root == null) return;
            root.SetActive(seat.HasOuterRing);
            if (!seat.HasOuterRing) return;
            int count = Mathf.Min(seat.OuterRing.Count, boxes.Length);
            for (int i = 0; i < boxes.Length; i++)
            {
                bool exists = i < count;
                boxes[i].gameObject.SetActive(exists);
                if (!exists) continue;
                float step = boxes[i].rectTransform.sizeDelta.x + 6f;
                boxes[i].rectTransform.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * step, 0f);
                labels[i].text = seat.OuterRing[i].Label;
                boxes[i].color = i == highlighted
                    ? new Color32(224, 168, 52, 255)
                    : new Color32(61, 53, 79, 255);
            }
        }

        // ───────────── 딜러 소개 ─────────────

        /// <summary>딜러 이름·하우스 룰을 화면 곳곳(상단, 룰렛 제목, 하우스 룰 카드)에 반영한다.</summary>
        private void ApplyDealerIdentity()
        {
            DealerProfile dealer = _battle.Profile;
            if (dealerNameText != null) dealerNameText.text = dealer.Name;
            if (dealerHeaderText != null) dealerHeaderText.text = $"◆  상대 룰렛 · {dealer.Name}  ◆";
            if (houseRuleTitleText != null) houseRuleTitleText.text = $"하우스 룰  ·  {HouseRuleName(dealer.HouseRule)}";
            if (houseRuleDescriptionText != null) houseRuleDescriptionText.text = $"{HouseRuleDescription(dealer)}  →  칸 1개 HIJACK";
            if (dealerSprite != null) dealerSprite.color = DealerTint(dealer.Name);
        }

        /// <summary>그레이박스용: 딜러 전용 아트가 나오기 전까지 토끼 스프라이트를 색으로 구분한다.</summary>
        private static Color DealerTint(string dealerName)
        {
            if (dealerName.StartsWith("여우")) return new Color32(255, 170, 110, 255);
            if (dealerName.StartsWith("고양이")) return new Color32(200, 190, 255, 255);
            if (dealerName.StartsWith("까마귀")) return new Color32(120, 120, 140, 255);
            if (dealerName.Contains("매니저")) return new Color32(255, 215, 120, 255);
            return Color.white;
        }

        private static string HouseRuleName(HouseRule rule)
        {
            return rule switch
            {
                HouseRule.FullCoverage => "전액 보장",
                HouseRule.SmallCashOuts => "소액 손님",
                HouseRule.MultipliedCashOut => "배율 정산",
                HouseRule.FirstStrike => "선제 정산",
                HouseRule.JackpotLines => "잭팟 라인",
                _ => rule.ToString()
            };
        }

        private static string HouseRuleDescription(DealerProfile dealer)
        {
            return dealer.HouseRule switch
            {
                HouseRule.FullCoverage => $"{dealer.Name}의 CASH OUT을 보험으로 전부 막기",
                HouseRule.SmallCashOuts => "판돈 4 이하로 CASH OUT 2번",
                HouseRule.MultipliedCashOut => "배율 칸으로 불린 판돈을 CASH OUT",
                HouseRule.FirstStrike => "내가 선공인 라운드에 CASH OUT 피해 5 이상",
                HouseRule.JackpotLines => "안쪽과 바깥 링이 같은 종류로 멈추기 2번",
                _ => dealer.HouseRule.ToString()
            };
        }

        /// <summary>문 선택 카드: 성향·보험·하우스 룰·JACKPOT 칸을 한눈에.</summary>
        private static string DescribeDealer(DealerProfile dealer)
        {
            string jackpot = "없음";
            string jackpotEffect = "";
            bool hasHouseCut = false;
            foreach (Slot slot in dealer.Wheel)
            {
                if (slot.IsJackpot)
                {
                    jackpot = slot.Label;
                    jackpotEffect = SlotDescriptions.Describe(slot);
                }
                if (slot.Kind == SlotKind.HouseCut) hasHouseCut = true;
            }

            return $"칩 {dealer.StartingChips} · 한도 {dealer.TableLimit} · 룰렛 {dealer.Wheel.Count}칸{(hasHouseCut ? "" : " (하우스 몫 없음)")}\n"
                   + $"성향: 판돈 {dealer.CashOutAt}+에서 CASH OUT · 보험 {dealer.BaseInsurance}\n"
                   + $"하우스 룰: {HouseRuleDescription(dealer)}\n"
                   + $"JACKPOT: 「{jackpot}」 {jackpotEffect}"
                   + (dealer.TableOuterRing.Count > 0 ? "\n테이블 규칙: 양쪽 룰렛에 바깥 링이 붙는다" : "");
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

        /// <summary>하단 사건 띠: 방금 일어난 일 3개를 한 줄로, 최신이 왼쪽.</summary>
        private void AddLog(string message)
        {
            _combatLog.Enqueue(message);
            while (_combatLog.Count > 3) _combatLog.Dequeue();
            string[] recent = _combatLog.ToArray();
            System.Array.Reverse(recent);
            combatLogText.text = string.Join("   ›   ", recent);
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
