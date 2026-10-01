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

        [Header("Landing Tag (포인터 위 착지 이름표)")]
        [SerializeField] private RectTransform playerLandingTag;
        [SerializeField] private TMP_Text playerLandingTitle;
        [SerializeField] private TMP_Text playerLandingEffect;
        [SerializeField] private UnityEngine.UI.Image playerLandingBackground;
        [SerializeField] private RectTransform dealerLandingTag;
        [SerializeField] private TMP_Text dealerLandingTitle;
        [SerializeField] private TMP_Text dealerLandingEffect;
        [SerializeField] private UnityEngine.UI.Image dealerLandingBackground;

        [Header("Relics · NUDGE")]
        [SerializeField] private UnityEngine.UI.Button relicStripButton;
        [SerializeField] private TMP_Text relicStripText;
        [SerializeField] private GameObject relicOverlay;
        [SerializeField] private TMP_Text relicOverlayBody;
        [SerializeField] private UnityEngine.UI.Button relicOverlayClose;
        [SerializeField] private GameObject nudgeBar;
        [SerializeField] private TMP_Text nudgeBarText;
        [SerializeField] private UnityEngine.UI.Button nudgeLeftButton;
        [SerializeField] private TMP_Text nudgeLeftLabel;
        [SerializeField] private UnityEngine.UI.Button nudgeStayButton;
        [SerializeField] private UnityEngine.UI.Button nudgeRightButton;
        [SerializeField] private TMP_Text nudgeRightLabel;
        [SerializeField] private TMP_Text nudgeCountText;

        [Header("Hijack On Wheels (룰렛 위에서 직접 고르기)")]
        [SerializeField] private RouletteClickArea playerWheelClick;
        [SerializeField] private RouletteClickArea dealerWheelClick;
        [SerializeField] private GameObject hijackBar;
        [SerializeField] private TMP_Text hijackBarTitle;
        [SerializeField] private TMP_Text hijackBarText;

        [Header("Moments (CASH OUT 칩·큰 순간 배너)")]
        [SerializeField] private RectTransform fxLayer;
        [SerializeField] private RectTransform momentBanner;
        [SerializeField] private TMP_Text momentBannerTitle;
        [SerializeField] private TMP_Text momentBannerSubtitle;
        [SerializeField] private UnityEngine.UI.Image momentBannerBackground;
        [SerializeField, Min(0.1f)] private float chipFlightDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float bannerDuration = 1.1f;

        [Header("Overlays · Title")]
        [SerializeField] private TMP_Text relicOverlayTitle;
        [SerializeField] private UnityEngine.UI.Button logButton;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private UnityEngine.UI.Button titleStartButton;
        [SerializeField] private UnityEngine.UI.Button titleResetButton;
        [SerializeField] private TMP_Text titleMetaText;

        [Header("Tempo Toggle")]
        [SerializeField] private UnityEngine.UI.Button tempoButton;
        [SerializeField] private TMP_Text tempoLabel;

        [Header("Run · Doors")]
        [SerializeField] private GameObject doorPanel;
        [SerializeField] private TMP_Text doorFloorText;
        [SerializeField] private UnityEngine.UI.Button[] doorButtons = new UnityEngine.UI.Button[0];
        [SerializeField] private TMP_Text[] doorTitleTexts = new TMP_Text[0];
        [SerializeField] private TMP_Text[] doorBodyTexts = new TMP_Text[0];

        [Header("Tuning")]
        [SerializeField] private int battleSeed = 46021;

        private readonly Queue<string> _combatLog = new Queue<string>();
        private readonly List<string> _fullLog = new List<string>();
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
        private int _pendingNudgeIndex = -1;

        /// <summary>템포 배율(1 또는 2). 결과 확인 시간·딜러 턴·회전·연출 대기를 이만큼 빨리 감는다. 기기에 저장한다.</summary>
        private float _tempo = 1f;
        private const string TempoKey = "hijackpot.settings.tempo";

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
            relicStripButton?.onClick.AddListener(OpenRelicOverlay);
            logButton?.onClick.AddListener(OpenLogOverlay);
            titleStartButton?.onClick.AddListener(OnTitleStart);
            titleResetButton?.onClick.AddListener(OnTitleReset);
            _tempo = PlayerPrefs.GetFloat(TempoKey, 1f) >= 2f ? 2f : 1f;
            tempoButton?.onClick.AddListener(ToggleTempo);
            RefreshTempoLabel();
            relicOverlayClose?.onClick.AddListener(() => relicOverlay?.SetActive(false));
            nudgeLeftButton?.onClick.AddListener(() => ChooseNudge(-1));
            nudgeStayButton?.onClick.AddListener(() => ChooseNudge(0));
            nudgeRightButton?.onClick.AddListener(() => ChooseNudge(1));
            if (playerWheelClick != null) playerWheelClick.SegmentClicked += OnPlayerWheelClicked;
            if (dealerWheelClick != null) dealerWheelClick.SegmentClicked += SelectHijackSource;
        }

        private void OnEnable()
        {
            if (spinController != null) spinController.RouletteFinished += HandlePlayerSpinFinished;
            if (enemySpinController != null) enemySpinController.RouletteFinished += HandleDealerSpinFinished;
        }

        private void Start()
        {
            if (titlePanel != null)
            {
                ShowTitle();
                return;
            }

            StartNewRun();
        }

        // ───────────── 타이틀 ─────────────

        private void ShowTitle()
        {
            titlePanel.SetActive(true);
            titlePanel.transform.SetAsLastSibling();
            spinInput?.ShowUnavailableState("대기");
            bool outer = PlayerPrefs.GetInt(OuterRingUnlockedKey, 0) == 1;
            if (titleMetaText != null)
            {
                titleMetaText.text = outer
                    ? "해금된 룰렛 형식: 바깥 링 (보스 「매니저」 격파)"
                    : "보스를 이기면 다음 계약부터 룰렛 형식을 얻습니다";
            }

            if (titleResetButton != null) titleResetButton.gameObject.SetActive(outer);
        }

        private void OnTitleStart()
        {
            titlePanel?.SetActive(false);
            StartNewRun();
        }

        /// <summary>해금 기록(바깥 링)을 지운다. 처음부터 다시 시험하고 싶을 때.</summary>
        private void OnTitleReset()
        {
            PlayerPrefs.DeleteKey(OuterRingUnlockedKey);
            PlayerPrefs.Save();
            ShowTitle();
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
                doorFloorText.text = (_run.CurrentFloorKind == FloorKind.Boss
                    ? $"{_run.FloorIndex + 1}층 · 보스 테이블  ·  칩 {_run.Chips}"
                    : $"{_run.FloorIndex + 1}층 · 어느 딜러의 룰렛을 털까?  ·  칩 {_run.Chips}")
                    + "\n" + DealerLines.FloorAnnouncement(_run.FloorIndex);
            }

            for (int i = 0; i < doorButtons.Length; i++)
            {
                bool exists = i < _run.Doors.Count;
                doorButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                DealerProfile dealer = _run.Doors[i];
                doorTitleTexts[i].text = dealer.Name;
                doorBodyTexts[i].text = DescribeDealer(dealer) + DescribeRelicPrize(i < _run.DoorRelics.Count ? _run.DoorRelics[i] : null);
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
            _fullLog.Clear();
            _loggedLines = 0;
            _chosenAnte = 1;
            _playerSpinning = false;
            _hijackTransferInProgress = false;
            _selectedHijackSourceIndex = -1;

            hijackPanel?.SetActive(false);
            houseRuleInfoPanel?.SetActive(false);
            HideLandingFeedback(Side.Player);
            HideLandingFeedback(Side.Dealer);
            SetWheelPicking(false);
            momentBanner?.gameObject.SetActive(false);
            nudgeBar?.SetActive(false);
            relicOverlay?.SetActive(false);
            _pendingNudgeIndex = -1;
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
            Say(Lines.Opening);
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
            HideLandingFeedback(Side.Player);
            HideLandingFeedback(Side.Dealer);
            RefreshAllUi();
            resultText.text = _battle.Active == Side.Player ? "코인플립: 손님 선공" : $"코인플립: {DealerShortName} 선공";
            calculationText.text = _battle.RoundStartEffects.Count > 0
                ? "[라운드 시작] " + string.Join("  ·  ", _battle.RoundStartEffects)
                : "선공은 매 라운드 동전으로 정합니다";
            yield return Wait(resultPause);
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
                yield return PlayBanner("하우스 룰 달성!", $"{houseRuleTitleText?.text}  →  HIJACK 기회", new Color32(224, 168, 52, 255));
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
            spinController.DurationScale = (_playerSpinsThisTurn == 1 ? 1f : followUpSpinScale) / _tempo;
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Spin);
            instructionText.text = "손을 떠났습니다. 이제 룰렛이 결정합니다.";
            resultText.text = "회전 중...";
            calculationText.text = $"판돈 {_battle.Player.Pot} · 착지 결과를 기다리는 중";
            HideLandingFeedback(Side.Player);
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

            if (CanOfferNudge(index))
            {
                OfferNudge(index);
                return;
            }

            ResolvePlayerLanding(index);
        }

        // ───────────── NUDGE ─────────────

        /// <summary>하우스 몫(안쪽)에 걸렸고, 보호막이 없고, NUDGE가 남아 있고, 밀 만한 옆 칸이 있을 때만 묻는다.</summary>
        private bool CanOfferNudge(int index)
        {
            IReadOnlyList<Slot> wheel = _battle.Player.Wheel;
            if (wheel[index].Kind != SlotKind.HouseCut || _battle.Player.CutShields > 0 || _battle.NudgesRemaining <= 0) return false;
            return wheel[Neighbor(index, -1)].Kind != SlotKind.HouseCut || wheel[Neighbor(index, 1)].Kind != SlotKind.HouseCut;
        }

        private int Neighbor(int index, int direction)
        {
            int count = _battle.Player.Wheel.Count;
            return ((index + direction) % count + count) % count;
        }

        private void OfferNudge(int index)
        {
            _state = ViewState.Busy;
            _pendingNudgeIndex = index;
            spinInput?.ShowUnavailableState("NUDGE?");
            Slot left = _battle.Player.Wheel[Neighbor(index, -1)];
            Slot right = _battle.Player.Wheel[Neighbor(index, 1)];
            nudgeLeftLabel.text = $"◀ {left.Label}";
            nudgeRightLabel.text = $"{right.Label} ▶";
            nudgeLeftButton.interactable = left.Kind != SlotKind.HouseCut;
            nudgeRightButton.interactable = right.Kind != SlotKind.HouseCut;
            nudgeBarText.text = $"증발 직전의 판돈 {_battle.Player.Pot} · 옆 칸으로 밀면 그 칸이 대신 발동";
            nudgeCountText.text = $"이번 전투 NUDGE {_battle.NudgesRemaining}회 남음";
            nudgeBar?.SetActive(true);
            resultText.text = "하우스 몫…?";
            calculationText.text = "NUDGE로 옆 칸으로 밀거나, 그대로 받아들이세요";
            roulette?.ShowLanding(_battle.Player.Wheel[index].Id, null);
        }

        private void ChooseNudge(int direction)
        {
            if (_pendingNudgeIndex < 0) return;
            int index = _pendingNudgeIndex;
            _pendingNudgeIndex = -1;
            nudgeBar?.SetActive(false);
            roulette?.ClearLanding();
            if (direction == 0 || !_battle.UseNudge())
            {
                ResolvePlayerLanding(index);
                return;
            }

            int target = Neighbor(index, direction);
            FlushCoreLog();
            Say(Lines.Nudged, true);
            if (spinController != null)
            {
                spinController.NudgeToSegment(target, () => ResolvePlayerLanding(target));
            }
            else
            {
                ResolvePlayerLanding(target);
            }
        }

        private void ResolvePlayerLanding(int index)
        {
            _state = ViewState.PlayerTurn;
            _lastPlayerOuter = _battle.RollOuterIndex(Side.Player);
            LandingResult landing = _battle.Land(index, _lastPlayerOuter);
            FlushCoreLog();
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Resolve);
            resultText.text = DescribeLanding(landing, SlotTitle(_battle.Player.Wheel[index]));
            calculationText.text = landing.Formula;
            ShowLandingFeedback(Side.Player, landing);
            if (!landing.EndedTurn && (landing.JackpotLine || landing.Group.Count >= 3 || landing.PotAfter - landing.PotBefore >= 6))
            {
                Say(Lines.BigCombo, true);
            }

            if (landing.EndedTurn)
            {
                Say(Lines.PlayerHouseCut);
                if (landing.HouseCutHit && landing.Amount > 0)
                {
                    StartCoroutine(PlayBanner("하우스 몫!", $"판돈 {landing.Amount} 증발", new Color32(230, 84, 84, 255)));
                }
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
            calculationText.text = result.RelicBonus > 0
                ? $"판돈 {result.Pot} − {DealerShortName} 보험 {result.OpponentInsurance} + 유물 {result.RelicBonus} = {result.Damage}"
                : $"판돈 {result.Pot} − {DealerShortName} 보험 {result.OpponentInsurance} = {result.Damage}";
            Say(result.Damage >= 8 ? Lines.BigCashOut : Lines.SmallCashOut, result.Damage >= 8);
            StartCoroutine(CashOutThenEndTurn(result));
        }

        private IEnumerator CashOutThenEndTurn(CashOutResult result)
        {
            _state = ViewState.Busy;
            spinInput?.ShowUnavailableState("정산 중");
            yield return PlayCashOutFlight(Side.Player, result);
            yield return AfterPlayerTurnEnded();
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
            yield return Wait(resultPause * 1.5f);
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
            yield return Wait(resultPause);
            yield return ContinueTurnFlow();
        }

        // ───────────── 딜러 턴 ─────────────

        private IEnumerator RunDealerTurn()
        {
            _state = ViewState.Busy;
            presentationUi?.SetPhase(BattlePresentationUI.Phase.Dealer);
            HideLandingFeedback(Side.Player);
            presentationUi?.SetPlayerRouletteActive(false);
            presentationUi?.SetDealerRouletteActive(true);
            spinInput?.ShowUnavailableState($"{DealerShortName} 차례");
            instructionText.text = $"{DealerShortName} 차례 · 판돈 {_battle.Profile.CashOutAt} 이상이면 CASH OUT합니다.";

            _battle.PlaceAnte(_battle.Profile.DealerAnte);
            FlushCoreLog();
            RefreshAllUi();
            if (enemySpinController != null) enemySpinController.DurationScale = dealerFastForwardScale / _tempo;
            float dealerPause = resultPause * dealerFastForwardScale;
            yield return Wait(dealerSpinDelay);

            int spins = 0;
            while (_battle.Phase == BattlePhase.Spinning && _battle.Active == Side.Dealer)
            {
                if (_battle.DealerWantsToCashOut() || spins >= dealerMaxSpinsPerTurn)
                {
                    CashOutResult cashOut = _battle.CashOut();
                    FlushCoreLog();
                    resultText.text = cashOut.FullCoverage ? "전액 보장!" : $"{DealerShortName} CASH OUT  피해 {cashOut.Damage}";
                    calculationText.text = $"판돈 {cashOut.Pot} − 내 보험 {cashOut.OpponentInsurance} = {cashOut.Damage}";
                    Say(cashOut.FullCoverage ? "보험이 전액 보장했군요. 규정상 칸 하나를 양도하겠습니다." : Lines.DealerCashOut);
                    presentationUi?.SetHouseRuleHighlighted(cashOut.FullCoverage);
                    yield return PlayCashOutFlight(Side.Dealer, cashOut);
                    break;
                }

                spins++;
                yield return SpinDealerWheel();
                _lastDealerOuter = _battle.RollOuterIndex(Side.Dealer);
                LandingResult landing = _battle.Land(_dealerLandingIndex, _lastDealerOuter);
                FlushCoreLog();
                ShowLandingFeedback(Side.Dealer, landing);
                resultText.text = landing.HouseCutHit
                    ? $"{DealerShortName}의 하우스 몫! 판돈 증발"
                    : DealerShortName + " " + DescribeLanding(landing, SlotTitle(_battle.Dealer.Wheel[_dealerLandingIndex]));
                if (landing.HouseCutHit)
                {
                    Say(Lines.DealerHouseCut);
                    break;
                }
                calculationText.text = landing.Formula;
                RefreshAllUi();
                yield return Wait(dealerPause);
            }

            RefreshAllUi();
            yield return Wait(resultPause);
            // 딜러 차례가 끝나면 이름표를 거둬 성향 칸을 다시 보이게 한다.
            HideLandingFeedback(Side.Dealer);
        }

        private IEnumerator SpinDealerWheel()
        {
            _dealerLandingIndex = _battle.RollLandingIndex(Side.Dealer);
            _dealerSpinFinished = enemySpinController == null;
            onDealerSpinRequested?.Invoke();
            if (dealerButtonPressLeadTime > 0f)
            {
                yield return Wait(dealerButtonPressLeadTime);
            }

            enemyRoulette?.PixelWheelRenderer?.ClearHighlight();
            HideLandingFeedback(Side.Dealer);
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
            // 룰렛 위에서 직접 고른다. 안내 띠가 없는 옛 씬이면 버튼 패널로 대신한다.
            bool onWheels = hijackBar != null && dealerWheelClick != null && playerWheelClick != null;
            hijackPanel?.SetActive(!onWheels);
            if (onWheels)
            {
                SetWheelPicking(true);
                hijackBarTitle.text = "HOUSE RULE CLEAR · HIJACK";
                hijackBarText.text = $"1. {DealerShortName} 룰렛에서 빼앗을 칸을 클릭하세요\n밝은 칸만 가능 · 하우스 몫·봉인 칸 불가";
                HideLandingFeedback(Side.Player);
                HideLandingFeedback(Side.Dealer);
                enemyRoulette?.ShowLanding(null, StealableDealerSlotIds(), false);
            }

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
            List<string> destinations = new List<string>();
            for (int i = 0; i < _battle.Player.Wheel.Count; i++)
            {
                bool valid = _battle.CanHijack(sourceIndex, i) == HijackError.None;
                if (valid) destinations.Add(_battle.Player.Wheel[i].Id);
                if (i < hijackDestinationButtons.Length) hijackDestinationButtons[i].interactable = valid;
            }

            if (hijackBarText != null)
            {
                hijackBarText.text = $"「{SlotTitle(chosen)}」\n{SlotDescriptions.Describe(chosen)}\n\n2. 내 룰렛에서 덮어쓸 칸을 클릭하세요\n(다른 딜러 칸을 눌러 바꿀 수 있음)";
            }

            enemyRoulette?.ShowLanding(chosen.Id, StealableDealerSlotIds(), false);
            roulette?.ShowLanding(null, destinations, false);
        }

        private void OnPlayerWheelClicked(int index)
        {
            if (_placingJackpot)
            {
                OnDestinationChosen(index);
                return;
            }

            PlaceHijackedSegment(index);
        }

        /// <summary>룰렛 위 클릭으로 칸 고르기를 켜고 끈다(안내 띠·클릭 영역).</summary>
        private void SetWheelPicking(bool on)
        {
            hijackBar?.SetActive(on);
            if (playerWheelClick != null) playerWheelClick.gameObject.SetActive(on);
            if (dealerWheelClick != null) dealerWheelClick.gameObject.SetActive(on);
            if (!on)
            {
                roulette?.ClearLanding();
                enemyRoulette?.ClearLanding();
            }
        }

        private List<string> StealableDealerSlotIds()
        {
            List<string> ids = new List<string>();
            foreach (Slot slot in _battle.Dealer.Wheel)
            {
                if (slot.Kind != SlotKind.Sealed && slot.Kind != SlotKind.HouseCut) ids.Add(slot.Id);
            }

            return ids;
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
            SetWheelPicking(false);

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
            Say(stolenSlot.IsJackpot ? Lines.JackpotHijacked : Lines.Hijacked, true);
            _state = ViewState.Busy;
            if (stolenSlot.IsJackpot)
            {
                yield return PlayBanner("JACKPOT HIJACK!", $"「{stolenSlot.Label}」 {SlotDescriptions.Describe(stolenSlot)}", new Color32(224, 168, 52, 255));
            }
            else
            {
                yield return Wait(resultPause);
            }

            yield return ContinueTurnFlow();
        }

        // ───────────── 템포 ─────────────

        private WaitForSecondsRealtime Wait(float seconds) => new WaitForSecondsRealtime(seconds / _tempo);

        private void ToggleTempo()
        {
            _tempo = _tempo >= 2f ? 1f : 2f;
            PlayerPrefs.SetFloat(TempoKey, _tempo);
            PlayerPrefs.Save();
            RefreshTempoLabel();
        }

        private void RefreshTempoLabel()
        {
            if (tempoLabel != null) tempoLabel.text = _tempo >= 2f ? "속도 ×2" : "속도 ×1";
        }

        // ───────────── 딜러 반응 ─────────────

        private DealerLines Lines => DealerLines.For(_battle != null ? _battle.Profile.Name : "");

        /// <summary>딜러 한마디. react면 딜러 그림이 움찔한다(큰 연쇄·HIJACK·큰 CASH OUT에 즉시 반응, CLAUDE.md 4장).</summary>
        private void Say(string line, bool react = false)
        {
            if (dealerLineText != null) dealerLineText.text = $"\"{line}\"";
            if (react && dealerSprite != null) StartCoroutine(Flinch(dealerSprite.rectTransform));
        }

        private static IEnumerator Flinch(RectTransform target)
        {
            Vector2 origin = target.anchoredPosition;
            const float duration = 0.32f;
            for (float t = 0f; t < duration && target != null; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / duration;
                target.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 70f) * 9f * k, 0f);
                yield return null;
            }

            if (target != null) target.anchoredPosition = origin;
        }

        // ───────────── 큰 순간 ─────────────

        /// <summary>화면 가운데 띠 배너(하우스 룰 달성·JACKPOT HIJACK·하우스 몫). 크게 튀어나왔다가 사라진다.</summary>
        private IEnumerator PlayBanner(string title, string subtitle, Color accent)
        {
            if (momentBanner == null) yield break;
            momentBannerTitle.text = title;
            momentBannerTitle.color = accent;
            momentBannerSubtitle.text = subtitle;
            if (momentBannerBackground != null)
            {
                momentBannerBackground.color = Color.Lerp(new Color32(30, 25, 39, 240), accent, 0.18f);
            }

            momentBanner.gameObject.SetActive(true);
            momentBanner.SetAsLastSibling();
            const float pop = 0.15f;
            for (float t = 0f; t < pop; t += Time.unscaledDeltaTime)
            {
                momentBanner.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, t / pop), 1f);
                momentBannerTitle.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t / pop);
                yield return null;
            }

            momentBanner.localScale = Vector3.one;
            momentBannerTitle.rectTransform.localScale = Vector3.one;
            yield return Wait(bannerDuration);
            momentBanner.gameObject.SetActive(false);
        }

        /// <summary>
        /// CASH OUT 연출: 판돈 칩 더미가 상대 칩 바로 날아가고, 보험만큼은 튕겨 나온다. 도착하면 칩 바가 줄고 피해 숫자가 터진다.
        /// 코어는 이미 정산을 끝낸 상태이며 이건 보여 주기만 한다.
        /// </summary>
        private IEnumerator PlayCashOutFlight(Side cashingSide, CashOutResult result)
        {
            RectTransform target = (cashingSide == Side.Player ? dealerChipsFill : playerChipsFill)?.rectTransform;
            if (fxLayer == null || target == null || potChipImages.Length == 0)
            {
                RefreshAllUi();
                yield break;
            }

            List<RectTransform> chips = new List<RectTransform>();
            foreach (UnityEngine.UI.Image chip in potChipImages)
            {
                if (!chip.gameObject.activeSelf) continue;
                GameObject copy = Instantiate(chip.gameObject, fxLayer);
                RectTransform rect = (RectTransform)copy.transform;
                rect.position = chip.rectTransform.position;
                chips.Add(rect);
                chip.gameObject.SetActive(false);
            }

            if (chips.Count == 0)
            {
                GameObject copy = Instantiate(potChipImages[0].gameObject, fxLayer);
                copy.SetActive(true);
                ((RectTransform)copy.transform).position = potChipImages[0].rectTransform.position;
                chips.Add((RectTransform)copy.transform);
            }

            int blocked = result.Pot <= 0 ? chips.Count : Mathf.RoundToInt(chips.Count * Mathf.Clamp01((float)(result.Pot - result.Damage) / result.Pot));
            if (result.Damage > 0) blocked = Mathf.Min(blocked, chips.Count - 1);
            Vector3[] starts = new Vector3[chips.Count];
            for (int i = 0; i < chips.Count; i++) starts[i] = chips[i].position;
            Vector3 end = target.position;
            const float stagger = 0.03f;
            float flight = chipFlightDuration / _tempo;
            float total = flight + stagger * chips.Count;
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < chips.Count; i++)
                {
                    float k = Mathf.Clamp01((t - i * stagger) / flight);
                    float eased = 1f - (1f - k) * (1f - k);
                    bool bounces = i >= chips.Count - blocked;
                    Vector3 position;
                    if (!bounces)
                    {
                        position = Vector3.Lerp(starts[i], end, eased) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 80f;
                    }
                    else
                    {
                        // 보험: 상대 앞 70% 지점에서 튕겨 아래로 떨어진다.
                        Vector3 wall = Vector3.Lerp(starts[i], end, 0.7f);
                        position = k < 0.6f
                            ? Vector3.Lerp(starts[i], wall, k / 0.6f) + Vector3.up * Mathf.Sin(k / 0.6f * Mathf.PI) * 60f
                            : wall + new Vector3(0f, -160f * ((k - 0.6f) / 0.4f), 0f);
                    }

                    chips[i].position = position;
                    chips[i].localScale = Vector3.one * (bounces && k > 0.6f ? Mathf.Lerp(1f, 0.4f, (k - 0.6f) / 0.4f) : 1f);
                }

                yield return null;
            }

            foreach (RectTransform chip in chips) Destroy(chip.gameObject);
            RefreshAllUi();
            if (result.Damage > 0)
            {
                StartCoroutine(FloatText($"−{result.Damage}", target.position + Vector3.up * 30f, new Color32(255, 96, 96, 255), 54));
            }

            if (result.OpponentInsurance > 0)
            {
                Vector3 wallPoint = Vector3.Lerp(starts[0], end, 0.7f);
                StartCoroutine(FloatText(result.Damage > 0 ? $"보험 −{result.OpponentInsurance}" : "전액 보장!", wallPoint, new Color32(86, 206, 214, 255), 30));
            }

            yield return Wait(0.35f);
        }

        /// <summary>떠오르며 사라지는 숫자(피해·보험). 판돈 숫자 글꼴을 복제해 쓴다.</summary>
        private IEnumerator FloatText(string text, Vector3 position, Color color, float size)
        {
            if (fxLayer == null || potText == null) yield break;
            TMP_Text label = Instantiate(potText, fxLayer);
            label.text = text;
            label.color = color;
            label.fontSize = size;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.sizeDelta = new Vector2(400f, 70f);
            rect.position = position;
            const float duration = 0.9f;
            for (float t = 0f; t < duration && label != null; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                rect.position = position + Vector3.up * (50f * k);
                rect.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(1.6f, 1f, k / 0.15f) : 1f);
                label.alpha = k < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.6f) / 0.4f);
                yield return null;
            }

            if (label != null) Destroy(label.gameObject);
        }

        // ───────────── 종료 ─────────────

        private void FinishBattle()
        {
            _state = ViewState.Ended;
            spinInput?.ShowUnavailableState("전투 종료");
            endPanel?.SetActive(true);
            bool lastFloor = _run.FloorIndex >= _run.FloorCount - 1;
            string prize = _run.CurrentPrize.HasValue && _battle.Outcome != BattleOutcome.DealerWins
                ? $"\n유물 획득: 「{RelicCatalog.Get(_run.CurrentPrize.Value).Name}」 {RelicCatalog.Get(_run.CurrentPrize.Value).Description}"
                : "";
            switch (_battle.Outcome)
            {
                case BattleOutcome.PlayerWinsByCleanSweep:
                    endTitleText.text = "완전 강탈";
                    endBodyText.text = $"{_battle.Profile.Name}의 룰렛을 전부 봉인했습니다.\n상금 대신 JACKPOT 칸을 가져갑니다.\n{DescribeChipsCarried()}" + prize;
                    Say(Lines.CleanSwept, true);
                    break;
                case BattleOutcome.PlayerWinsByBankrupt:
                    endTitleText.text = $"{_battle.Profile.Name} 파산";
                    endBodyText.text = $"{DescribeChipsCarried()}\n빼앗은 칸은 손님의 룰렛에 영구히 남습니다." + prize;
                    Say(Lines.DealerLoses, true);
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
            string dealerInsurance = $"{DealerShortName} 보험 {_battle.Dealer.Insurance}";
            insuranceText.text = _battle.HasRelic(RelicId.DopamineShot)
                ? $"내 보험 {_battle.Player.Insurance} · 도파민 {_battle.Dopamine} · {dealerInsurance}"
                : $"내 보험 {_battle.Player.Insurance}  ·  {dealerInsurance}";
            RefreshRelicStrip();
            Seat potSeat = _battle.Active == Side.Dealer && _battle.Phase == BattlePhase.Spinning ? _battle.Dealer : _battle.Player;
            potText.text = potSeat == _battle.Dealer ? $"{DealerShortName} 판돈 {potSeat.Pot}" : $"판돈 {potSeat.Pot}";
            int chipsShown = Mathf.Min(potChipImages.Length, (potSeat.Pot + 1) / 2);
            for (int i = 0; i < potChipImages.Length; i++)
            {
                potChipImages[i].gameObject.SetActive(i < chipsShown);
            }
            string closing = _battle.TableFee > 0
                ? $"   ·   마감 사용료 −{_battle.TableFee}"
                : _battle.Round >= PotBattle.TableFeeStartRound - 2 ? $"   ·   {PotBattle.TableFeeStartRound}R부터 마감" : "";
            roundText.text = _battle.Profile.Telegraphs
                ? $"{_run.FloorIndex + 1}F · TUTORIAL TABLE   ·   ROUND {_battle.Round}{closing}"
                : $"{_run.FloorIndex + 1}F · {_battle.Profile.Name}   ·   ROUND {_battle.Round}{closing}";
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
                (int low, int high) = NextSpinPotRange(_battle.Player.Wheel, _battle.Player.Pot);
                riskSummaryText.text =
                    $"지금 CASH OUT 피해 {_battle.PreviewCashOutDamage(Side.Player)}  ·  다음 SPIN 판돈 {low}~{high}  ·  하우스 몫 {houseCutChance:0.#}%";
                riskSummaryText.color = new Color32(242, 194, 110, 255);
            }
            else
            {
                int ante = _battle.Active == Side.Player && _battle.Phase == BattlePhase.AwaitingAnte ? _chosenAnte : 1;
                (int low, int high) = NextSpinPotRange(_battle.Player.Wheel, ante);
                riskSummaryText.text = $"첫 SPIN 판돈 {low}~{high}  ·  하우스 몫 {houseCutChance:0.#}%  ·  {DealerShortName} 판돈 {_battle.Dealer.Pot}";
                riskSummaryText.color = new Color32(202, 196, 212, 255);
            }
        }

        /// <summary>
        /// 확률판: 다음 SPIN 한 번 뒤 판돈의 범위(하우스 몫이면 0, 레이즈·배율 연쇄면 최대). 바깥 링·유물 보너스는 넣지 않은 어림값.
        /// </summary>
        private static (int low, int high) NextSpinPotRange(IReadOnlyList<Slot> wheel, int pot)
        {
            int low = int.MaxValue, high = 0;
            for (int i = 0; i < wheel.Count; i++)
            {
                Slot slot = wheel[i];
                int after = pot;
                if (slot.Kind == SlotKind.HouseCut)
                {
                    after = 0;
                }
                else if (slot.FiresOnLand && (slot.Kind == SlotKind.Raise || slot.Kind == SlotKind.Multiplier))
                {
                    int sum = 0, product = 1;
                    foreach (int index in PotBattle.FindChainGroup(wheel, i))
                    {
                        sum += wheel[index].Value;
                        product *= Mathf.Max(1, wheel[index].Value);
                    }

                    after = slot.Kind == SlotKind.Raise ? pot + sum : pot * product;
                }

                low = Mathf.Min(low, after);
                high = Mathf.Max(high, after);
            }

            return (low == int.MaxValue ? pot : low, high);
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

        /// <summary>
        /// 룰렛 칸 위 글자(그레이박스 아이콘): 일반 칸은 기호 + 숫자(▲레이즈 ×배율 ◆보험 ●배당, 몫=하우스 몫),
        /// 특수·JACKPOT 칸은 고유 이름. 긴 이름은 착지 이름표와 설명이 맡는다. 기호 범례는 가운데 테이블 맨 위.
        /// </summary>
        private static string WheelLabel(Slot slot)
        {
            if (SlotDescriptions.IsSpecial(slot)) return slot.Label;
            switch (slot.Kind)
            {
                case SlotKind.Raise: return $"▲{slot.Value}";
                case SlotKind.Multiplier: return $"×{slot.Value}";
                case SlotKind.Insurance: return $"◆{slot.Value}";
                case SlotKind.Dividend: return $"●{slot.Value}";
                case SlotKind.HouseCut: return "몫";
                case SlotKind.Sealed: return "봉인";
                default: return slot.Label;
            }
        }

        private static string SlotTitle(Slot slot)
        {
            return slot.Label + (slot.IsJackpot ? " [JP]" : "");
        }

        /// <summary>
        /// 착지 고정 표시: 멈춘 칸과 연쇄 묶음만 밝게 남기고, 포인터 위 이름표에 칸 이름과 실제로 일어난 효과를 띄운다.
        /// 다음 SPIN(딜러는 딜러 차례가 끝날 때)까지 유지된다.
        /// </summary>
        private void ShowLandingFeedback(Side side, LandingResult landing)
        {
            bool player = side == Side.Player;
            Seat seat = player ? _battle.Player : _battle.Dealer;
            RouletteController wheel = player ? roulette : enemyRoulette;
            if (landing.Index < 0 || landing.Index >= seat.Wheel.Count) return;

            Slot slot = seat.Wheel[landing.Index];
            List<string> groupIds = new List<string>();
            foreach (int i in landing.Group)
            {
                if (i >= 0 && i < seat.Wheel.Count) groupIds.Add(seat.Wheel[i].Id);
            }

            wheel?.ShowLanding(slot.Id, groupIds);

            RectTransform tag = player ? playerLandingTag : dealerLandingTag;
            if (tag == null) return;
            TMP_Text title = player ? playerLandingTitle : dealerLandingTitle;
            TMP_Text effect = player ? playerLandingEffect : dealerLandingEffect;
            UnityEngine.UI.Image background = player ? playerLandingBackground : dealerLandingBackground;

            bool bad = landing.HouseCutHit;
            string who = player ? "" : DealerShortName + " · ";
            if (title != null) title.text = who + (landing.JackpotLine ? "잭팟 라인!  " : "") + SlotTitle(slot);
            if (effect != null)
            {
                effect.text = SummarizeLandingEffect(landing, slot, seat);
                effect.color = bad ? new Color32(255, 120, 120, 255) : new Color32(224, 168, 52, 255);
            }

            if (background != null)
            {
                Color kindColor = ToSegment(slot).color;
                background.color = Color.Lerp(new Color32(43, 38, 58, 255), kindColor, bad ? 0.6f : 0.45f);
            }

            tag.gameObject.SetActive(true);
            tag.SetAsLastSibling();
            StartCoroutine(PopLandingTag(tag));
        }

        private void HideLandingFeedback(Side side)
        {
            bool player = side == Side.Player;
            (player ? roulette : enemyRoulette)?.ClearLanding();
            RectTransform tag = player ? playerLandingTag : dealerLandingTag;
            if (tag != null) tag.gameObject.SetActive(false);
        }

        private static IEnumerator PopLandingTag(RectTransform tag)
        {
            const float duration = 0.16f;
            for (float t = 0f; t < duration && tag != null; t += Time.unscaledDeltaTime)
            {
                tag.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t / duration);
                yield return null;
            }

            if (tag != null) tag.localScale = Vector3.one;
        }

        /// <summary>이름표 둘째 줄: 이 착지로 실제로 바뀐 것(판돈·보험·칩·특수 효과)과 연쇄·바깥 링.</summary>
        private static string SummarizeLandingEffect(LandingResult landing, Slot slot, Seat seat)
        {
            if (landing.HouseCutHit)
            {
                return landing.Kind == SlotKind.HouseCut
                    ? $"판돈 {landing.Amount} 증발 · 턴 종료"
                    : $"바깥 하우스 몫 · 판돈 {landing.Amount} 증발 · 턴 종료";
            }

            if (landing.CutShielded) return "보호막이 하우스 몫을 막음 · 판돈 유지";

            string main;
            if (landing.KeywordTriggered)
            {
                main = slot.Kind switch
                {
                    SlotKind.Dividend => $"특수 칸 발동 · 즉시 칩 +{slot.Value}",
                    SlotKind.Initiative => "특수 칸 발동 · 다음 라운드 선공 확정",
                    SlotKind.MinimumPayout => "특수 칸 발동 · 이번 턴 상대 보험 무시",
                    _ => "특수 칸 발동"
                };
            }
            else
            {
                main = slot.Kind switch
                {
                    SlotKind.Raise or SlotKind.Multiplier => $"판돈 {landing.PotBefore} → {landing.PotAfter}",
                    SlotKind.Insurance => $"보험 +{landing.Amount}",
                    SlotKind.Dividend => $"칩 +{landing.Amount}",
                    SlotKind.CutShield => "이번 턴 하우스 몫 1회 무효",
                    SlotKind.Sealed => "봉인된 칸 · 효과 없음",
                    _ => "효과 없음"
                };
            }

            if (landing.Group.Count > 1) main += $" · 연쇄 {landing.Group.Count}칸";
            if (landing.OuterIndex >= 0 && landing.OuterIndex < seat.OuterRing.Count)
            {
                main += $" · 바깥 {seat.OuterRing[landing.OuterIndex].Label}";
            }

            return main;
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
        // ───────────── 유물 ─────────────

        private IReadOnlyList<RelicId> OwnedRelics => _run != null ? _run.Relics : System.Array.Empty<RelicId>();

        private void RefreshRelicStrip()
        {
            if (relicStripText == null) return;
            int nudges = _battle != null ? _battle.NudgesRemaining : PotBattle.BaseNudgesPerBattle;
            List<string> names = new List<string>();
            foreach (RelicId id in OwnedRelics) names.Add(RelicCatalog.Get(id).Name);
            relicStripText.text = names.Count == 0
                ? $"NUDGE {nudges}  ·  유물 없음 (문 카드의 유물을 이겨서 얻기)"
                : $"NUDGE {nudges}  ·  유물 {names.Count}: {string.Join(" · ", names)}";
        }

        private void OpenRelicOverlay()
        {
            if (relicOverlay == null || relicOverlayBody == null) return;
            List<string> lines = new List<string>
            {
                $"NUDGE (전투당 {PotBattle.BaseNudgesPerBattle}회, 이번 전투 남은 {(_battle != null ? _battle.NudgesRemaining : PotBattle.BaseNudgesPerBattle)}회)",
                "   하우스 몫에 걸리면 룰렛을 옆 칸으로 밀 수 있다. 아껴 두고 큰 판돈을 지킬 수도 있다.",
                ""
            };
            if (OwnedRelics.Count == 0)
            {
                lines.Add("아직 유물이 없습니다. 층마다 문 카드에 걸린 유물은 그 딜러를 이기면 가져옵니다.");
            }

            foreach (RelicId id in OwnedRelics)
            {
                Relic relic = RelicCatalog.Get(id);
                lines.Add($"{relic.Keyword} 「{relic.Name}」");
                lines.Add($"   {relic.Description}");
            }

            if (relicOverlayTitle != null) relicOverlayTitle.text = "유물 · 이전 탈출자들의 부정행위 도구";
            relicOverlayBody.text = string.Join("\n", lines);
            relicOverlay.SetActive(true);
        }

        /// <summary>전체 전투 기록(사건 띠를 누르면). 최근 30줄, 최신이 아래.</summary>
        private void OpenLogOverlay()
        {
            if (relicOverlay == null || relicOverlayBody == null) return;
            int start = Mathf.Max(0, _fullLog.Count - 30);
            if (relicOverlayTitle != null) relicOverlayTitle.text = "전투 기록 · 발동 순서";
            relicOverlayBody.text = _fullLog.Count == 0 ? "아직 기록이 없습니다." : string.Join("\n", _fullLog.GetRange(start, _fullLog.Count - start));
            relicOverlay.SetActive(true);
        }

        /// <summary>종료 화면: 가지고 나가는 칩(입장 칩까지 + 상금)과 하우스가 회수하는 테이블 칩(ADR 0007).</summary>
        private string DescribeChipsCarried()
        {
            int entry = _run.ChipsAtEntry;
            int kept = Mathf.Min(_battle.Player.Chips, entry);
            int returned = Mathf.Max(0, _battle.Player.Chips - entry);
            int winnings = _battle.Outcome == BattleOutcome.PlayerWinsByBankrupt ? _run.WinningsFor(_battle.Profile) : 0;
            string line = winnings > 0
                ? $"가지고 나가는 칩: {kept} + 상금 {winnings} = {kept + winnings}"
                : $"가지고 나가는 칩: {kept}";
            return returned > 0 ? line + $"  (입장 칩 {entry}을 넘은 테이블 칩 {returned}은 하우스가 회수)" : line;
        }

        private static string DescribeRelicPrize(RelicId? prize)
        {
            if (!prize.HasValue) return "";
            Relic relic = RelicCatalog.Get(prize.Value);
            return $"\n이기면 유물: {relic.Keyword} 「{relic.Name}」 {relic.Description}";
        }

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

            string label = WheelLabel(slot);
            if (slot.IsStolen)
            {
                label = "H" + label;
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
            _fullLog.Add(message);
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
