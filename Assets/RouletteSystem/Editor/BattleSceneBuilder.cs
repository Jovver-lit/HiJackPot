#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace RouletteLike.Roulette.Editor
{
    /// <summary>
    /// 기준 씬(SampleScene)을 코드로 처음부터 만든다: 캔버스·패널·룰렛·배팅 버튼·HIJACK 패널을 배치하고
    /// DealerBattleController의 참조를 연결한다. 문 선택·HIJACK·종료 패널도 여기서 만든다.
    /// 씬 구조를 바꾸려면 씬이 아니라 이 파일을 고친다.
    /// </summary>
    public static class BattleSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string FontAssetPath = "Assets/RouletteSystem/Art/Fonts/neodgm SDF.asset";
        private const string RouletteSpriteSheetPath = "Assets/RouletteSystem/Art/roulette_ui_sheet.png";
        private const string BusinessmanSpriteSheetPath = "Assets/RouletteSystem/Art/Characters/Businessman/businessman-70px-sheet.png";
        private const string RabbitSpritePath = "Assets/RouletteSystem/Art/Characters/Rabbit/bunny-dealer-70px.png";
        private const string RouletteSpinSoundPath = "Assets/RouletteSystem/Music/hijackpot_roulette_sfx_3pack/hijackpot_roulette_fast.wav";

        private static readonly Color Background = new Color32(13, 12, 23, 255);
        private static readonly Color StageBackground = new Color32(20, 18, 34, 255);
        private static readonly Color Panel = new Color32(27, 24, 41, 248);
        private static readonly Color PanelInset = new Color32(20, 18, 32, 255);
        private static readonly Color PanelLight = new Color32(43, 38, 58, 255);
        private static readonly Color Gold = new Color32(224, 168, 52, 255);
        private static readonly Color DarkGold = new Color32(125, 91, 38, 255);
        private static readonly Color Ink = new Color32(245, 240, 226, 255);
        private static readonly Color Muted = new Color32(196, 189, 209, 255);
        private static readonly Color Red = new Color32(202, 70, 75, 255);
        private static readonly Color Teal = new Color32(57, 180, 188, 255);

        [MenuItem("Tools/HIJACKPOT/Build Battle Scene (SampleScene)")]
        public static void Build()
        {
            TMP_FontAsset font = GetOrCreateFontAsset();
            Font legacyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/RouletteSystem/Art/Fonts/neodgm.ttf");
            Sprite frameSprite = GetSprite("roulette_frame");
            Sprite pointerSprite = GetSprite("pointer");
            Sprite centerCapSprite = GetSprite("center_cap");
            Sprite[] businessmanIdleFrames =
            {
                GetSprite(BusinessmanSpriteSheetPath, "businessman_idle_0"),
                GetSprite(BusinessmanSpriteSheetPath, "businessman_idle_1")
            };
            Sprite[] businessmanBlinkFrames =
            {
                GetSprite(BusinessmanSpriteSheetPath, "businessman_blink_0"),
                GetSprite(BusinessmanSpriteSheetPath, "businessman_blink_1"),
                GetSprite(BusinessmanSpriteSheetPath, "businessman_blink_2"),
                GetSprite(BusinessmanSpriteSheetPath, "businessman_blink_3")
            };
            Sprite rabbitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RabbitSpritePath);
            AudioClip rouletteSpinSound = AssetDatabase.LoadAssetAtPath<AudioClip>(RouletteSpinSoundPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SampleScene";

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera mainCamera = cameraObject.GetComponent<Camera>();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Background;
            mainCamera.cullingMask = 0;
            mainCamera.orthographic = true;
            mainCamera.depth = -100f;
            mainCamera.allowHDR = false;
            mainCamera.allowMSAA = false;

            GameObject canvasObject = new GameObject(
                "BattleCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backgroundLayer = AddStretchRect("BackgroundLayer", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UnityEngine.UI.Image casinoBackground = AddImage("CasinoBackgroundPlaceholder", backgroundLayer, Background);
            Stretch(casinoBackground.rectTransform);

            // ── 상단 무대(화면 위 20%): 캐릭터·칩 바·하우스 룰 카드. 룰렛에 자리를 내주기 위해 얇게 둔다.
            RectTransform battleStage = AddStretchRect(
                "BattleStage", canvasObject.transform,
                new Vector2(0f, 0.8f), Vector2.one,
                new Vector2(40f, 6f), new Vector2(-40f, -16f));
            UnityEngine.UI.Image stagePlaceholder = AddImage("BackgroundPlaceholder", battleStage, StageBackground);
            Stretch(stagePlaceholder.rectTransform);
            AddFrame("Frame", battleStage, DarkGold);

            RectTransform playerArea = AddRect("PlayerArea", battleStage, new Vector2(-640f, 0f), new Vector2(540f, 190f));
            TMP_Text playerChipsText;
            UnityEngine.UI.Image playerChipsFill = AddChipsWidget("PlayerChips", playerArea, new Vector2(70f, 30f), "손님", Teal, font, out playerChipsText, out _);
            RectTransform playerCharacter = AddRect("PlayerCharacter", playerArea, new Vector2(-200f, -8f), new Vector2(170f, 170f));
            UnityEngine.UI.Image businessmanImage = AddSpriteImage("Sprite", playerCharacter, businessmanIdleFrames[0], new Vector2(165f, 165f));
            businessmanImage.raycastTarget = false;
            AddRect("EffectRoot", playerCharacter, Vector2.zero, new Vector2(170f, 170f));
            NaturalBlinkAnimator blinkAnimator = businessmanImage.gameObject.AddComponent<NaturalBlinkAnimator>();
            SerializedObject blinkSo = new SerializedObject(blinkAnimator);
            blinkSo.FindProperty("targetImage").objectReferenceValue = businessmanImage;
            SetObjectArray(blinkSo.FindProperty("idleFrames"), businessmanIdleFrames);
            SetObjectArray(blinkSo.FindProperty("blinkFrames"), businessmanBlinkFrames);
            blinkSo.ApplyModifiedPropertiesWithoutUndo();

            RectTransform centerStage = AddRect("CenterStage", battleStage, Vector2.zero, new Vector2(760f, 190f));
            RectTransform roundHeader = AddRect("RoundHeader", centerStage, new Vector2(0f, 68f), new Vector2(730f, 40f));
            TMP_Text roundText = AddText("RoundTitle", roundHeader, "TUTORIAL TABLE  ·  ROUND 1", font, 24, Gold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(720f, 34f));

            // 단계 표시는 룰렛 글로우가 대신하므로 숨긴다. BattlePresentationUI 참조를 유지하기 위해 오브젝트는 남긴다.
            RectTransform phaseIndicator = AddRect("PhaseIndicator", centerStage, new Vector2(0f, 40f), new Vector2(690f, 30f));
            TMP_Text preparePhase = AddText("Prepare", phaseIndicator, "준비", font, 17, Gold, TextAlignmentOptions.Center, new Vector2(-252f, 0f), new Vector2(116f, 28f));
            TMP_Text spinPhase = AddText("Spin", phaseIndicator, "회전", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(-85f, 0f), new Vector2(116f, 28f));
            TMP_Text resolvePhase = AddText("Resolve", phaseIndicator, "결과", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(88f, 0f), new Vector2(116f, 28f));
            TMP_Text dealerPhase = AddText("Dealer", phaseIndicator, "딜러", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(260f, 0f), new Vector2(116f, 28f));
            phaseIndicator.gameObject.SetActive(false);

            RectTransform houseRuleCard = AddFramedPanel("HouseRuleCard", centerStage, new Vector2(0f, -22f), new Vector2(700f, 120f), Panel, DarkGold, out _, out UnityEngine.UI.Image houseRuleFrame);
            TMP_Text houseRuleTitleText = AddText("Title", houseRuleCard, "하우스 룰  ·  전액 보장", font, 20, Gold, TextAlignmentOptions.Center, new Vector2(0f, 36f), new Vector2(480f, 30f));
            UnityEngine.UI.Button houseRuleInfoButton = AddButton("InfoButton", houseRuleCard, new Vector2(294f, 36f), new Vector2(78f, 30f), PanelLight, font, "상세", 15, out _);
            TMP_Text houseRuleDescriptionText = AddText("Description", houseRuleCard, "딜러의 CASH OUT을 보험으로 전부 막기  →  칸 1개 HIJACK", font, 17, Ink, TextAlignmentOptions.Center, new Vector2(0f, 6f), new Vector2(660f, 28f));
            UnityEngine.UI.Image houseRuleTrack = AddImage("ProgressBar", houseRuleCard, new Color32(66, 59, 78, 255));
            houseRuleTrack.rectTransform.anchoredPosition = new Vector2(0f, -19f);
            houseRuleTrack.rectTransform.sizeDelta = new Vector2(580f, 10f);
            UnityEngine.UI.Image houseRuleFill = AddImage("ProgressFill", houseRuleTrack.transform, Gold);
            ConfigureFill(houseRuleFill, 0f);
            TMP_Text houseRuleProgressText = AddText("ProgressText", houseRuleCard, "0 / 1", font, 15, Ink, TextAlignmentOptions.Center, new Vector2(0f, -40f), new Vector2(660f, 24f));
            AddRect("EffectRoot", houseRuleCard, Vector2.zero, new Vector2(700f, 120f));

            RectTransform dealerArea = AddRect("DealerArea", battleStage, new Vector2(640f, 0f), new Vector2(540f, 190f));
            TMP_Text dealerChipsText;
            UnityEngine.UI.Image dealerChipsFill = AddChipsWidget("DealerChips", dealerArea, new Vector2(-70f, 30f), "토끼 딜러", Red, font, out dealerChipsText, out TMP_Text dealerNameText);
            RectTransform dealerCharacter = AddRect("DealerCharacter", dealerArea, new Vector2(200f, -8f), new Vector2(170f, 170f));
            UnityEngine.UI.Image rabbitImage = AddSpriteImage("Sprite", dealerCharacter, rabbitSprite, new Vector2(165f, 165f));
            rabbitImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rabbitImage.raycastTarget = false;
            AddRect("EffectRoot", dealerCharacter, Vector2.zero, new Vector2(170f, 170f));
            RectTransform openingSpeechBubble = AddFramedPanel("DialogueBubble", dealerArea, new Vector2(-80f, -45f), new Vector2(320f, 70f), new Color32(239, 235, 225, 255), DarkGold, out _, out _);
            AddText("Text", openingSpeechBubble, "걸고, 돌리고, 적당할 때 터뜨리세요.\n보험으로 제 정산을 막으면 칸을 드려요.", font, 13, Background, TextAlignmentOptions.Center, Vector2.zero, new Vector2(300f, 56f));
            AddRect("AnimationRoot", openingSpeechBubble, Vector2.zero, new Vector2(320f, 70f));

            // ── 메인 영역(화면 아래 80%): 내 룰렛 | 테이블 | 딜러 룰렛, 맨 아래 사건 띠.
            RectTransform mainGameArea = AddStretchRect(
                "MainGameArea", canvasObject.transform,
                Vector2.zero, new Vector2(1f, 0.8f),
                new Vector2(40f, 20f), new Vector2(-40f, -6f));

            RectTransform playerPanel = AddFramedPanel("PlayerRoulettePanel", mainGameArea, new Vector2(-600f, 26f), new Vector2(640f, 760f), Panel, DarkGold, out _, out _);
            AddText("Header", playerPanel, "◆  내 룰렛  ◆", font, 22, Teal, TextAlignmentOptions.Center, new Vector2(0f, 352f), new Vector2(560f, 36f));
            RectTransform playerRouletteContainer = AddRect("RouletteContainer", playerPanel, new Vector2(0f, 10f), new Vector2(620f, 620f));
            UnityEngine.UI.Image playerGlow = AddGlow("ActiveGlow", playerRouletteContainer, new Color32(57, 190, 198, 90), new Vector2(600f, 600f));
            RectTransform rouletteRoot = AddRect("ExistingPlayerRoulette", playerRouletteContainer, Vector2.zero, new Vector2(580f, 580f));
            RouletteController roulette = rouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController spin = rouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildRoulette(rouletteRoot, roulette, spin, legacyFont, frameSprite, pointerSprite, centerCapSprite, rouletteSpinSound, false);

            // SPIN 버튼을 룰렛 중앙에 둔다. 회전하는 Wheel이 아니라 고정된 루트의 자식이라 함께 돌지 않는다.
            // 버튼 둘레의 원형 게이지가 누르고 있는 동안의 회전 강도를 보여준다.
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform spinCenter = AddRect("SpinCenter", rouletteRoot, Vector2.zero, new Vector2(150f, 150f));
            UnityEngine.UI.Image powerTrack = AddImage("PowerRingTrack", spinCenter, new Color32(40, 34, 52, 255));
            powerTrack.sprite = knobSprite;
            powerTrack.rectTransform.sizeDelta = new Vector2(150f, 150f);
            UnityEngine.UI.Image powerFill = AddImage("PowerRingFill", spinCenter, Gold);
            powerFill.sprite = knobSprite;
            powerFill.rectTransform.sizeDelta = new Vector2(150f, 150f);
            powerFill.type = UnityEngine.UI.Image.Type.Filled;
            powerFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            powerFill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            powerFill.fillClockwise = true;
            powerFill.fillAmount = 0.08f;
            UnityEngine.UI.Button throwButton = AddButton("SpinButton", spinCenter, Vector2.zero, new Vector2(124f, 124f), Red, font, "길게 눌러\nSPIN", 18, out TMP_Text throwLabel);
            throwLabel.color = Ink;
            UnityEngine.UI.Image throwImage = throwButton.GetComponent<UnityEngine.UI.Image>();
            if (knobSprite != null)
            {
                throwImage.sprite = knobSprite;
                throwImage.preserveAspect = true;
            }
            spinCenter.SetAsLastSibling();

            AddRect("EffectRoot", playerRouletteContainer, Vector2.zero, new Vector2(620f, 620f));
            // 룰렛 아래 안내: 특수 칸이 있으면 효과 설명, 없으면 연쇄 안내(두 줄까지).
            TMP_Text chainPreviewText = AddText("ChainPreview", playerPanel, "이어진 같은 칸은 한 묶음", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(0f, -340f), new Vector2(600f, 60f));

            // ── 가운데 "테이블": 판돈이 쌓이는 곳. 결과 → 계산식 → 칩 더미 → 판돈 → 보험 → 확률 → 앤티 → CASH OUT → 딜러 한마디.
            RectTransform centerPanel = AddFramedPanel("CenterPanel", mainGameArea, new Vector2(0f, 26f), new Vector2(480f, 760f), Panel, DarkGold, out _, out _);
            AddText("TableTitle", centerPanel, "TABLE", font, 16, DarkGold, TextAlignmentOptions.Center, new Vector2(0f, 358f), new Vector2(440f, 24f));
            TMP_Text instructionText = AddText("GuideText", centerPanel, "SPIN을 길게 눌렀다 놓으세요. 앤티가 걸리고 판돈이 쌓입니다.", font, 15, Ink, TextAlignmentOptions.Center, new Vector2(0f, 322f), new Vector2(440f, 44f));
            TMP_Text powerPreviewText = AddText("PowerPreview", centerPanel, "SPIN 밖으로 끌어내면 취소", font, 13, Muted, TextAlignmentOptions.Center, new Vector2(0f, 290f), new Vector2(440f, 22f));
            TMP_Text resultText = AddText("MainInstruction", centerPanel, "코인플립으로 선공을 정합니다", font, 26, Ink, TextAlignmentOptions.Center, new Vector2(0f, 252f), new Vector2(450f, 40f));
            TMP_Text calculationText = AddText("Description", centerPanel, "레이즈는 판돈 +, 배율은 판돈 ×, 하우스 몫은 판돈 증발", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(0f, 206f), new Vector2(450f, 50f));
            UnityEngine.UI.Image[] potChips = BuildPotChipStack(centerPanel, new Vector2(0f, 130f));
            TMP_Text potText = AddText("PotValue", centerPanel, "판돈 0", font, 36, Gold, TextAlignmentOptions.Center, new Vector2(0f, 62f), new Vector2(440f, 48f));
            TMP_Text insuranceText = AddBadge("InsuranceStatus", centerPanel, "내 보험 0", Teal, font, new Vector2(0f, 14f));
            TMP_Text riskSummaryText = AddText("RiskSummary", centerPanel, "다음 SPIN 하우스 몫 확률 12.5%", font, 15, Gold, TextAlignmentOptions.Center, new Vector2(0f, -36f), new Vector2(450f, 44f));
            RectTransform betControls = AddRect("BetControls", centerPanel, new Vector2(0f, -94f), new Vector2(440f, 52f));
            UnityEngine.UI.Button anteDownButton = AddButton("AnteDown", betControls, new Vector2(-150f, 0f), new Vector2(52f, 48f), PanelLight, font, "−", 26, out _);
            TMP_Text anteText = AddText("AnteText", betControls, "앤티 1", font, 17, Ink, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(230f, 48f));
            UnityEngine.UI.Button anteUpButton = AddButton("AnteUp", betControls, new Vector2(150f, 0f), new Vector2(52f, 48f), PanelLight, font, "+", 26, out _);
            UnityEngine.UI.Button cashOutButton = AddButton("CashOutButton", centerPanel, new Vector2(0f, -170f), new Vector2(380f, 80f), Gold, font, "CASH OUT", 22, out TMP_Text cashOutLabel);
            cashOutLabel.color = Background;
            UnityEngine.UI.Image divider = AddImage("Divider", centerPanel, DarkGold);
            divider.rectTransform.anchoredPosition = new Vector2(0f, -232f);
            divider.rectTransform.sizeDelta = new Vector2(420f, 2f);
            TMP_Text dealerLineText = AddText("DealerLine", centerPanel, "어서오세요, 첫 손님이시네요.", font, 15, Ink, TextAlignmentOptions.Center, new Vector2(0f, -290f), new Vector2(440f, 90f));

            RectTransform dealerPanel = AddFramedPanel("DealerRoulettePanel", mainGameArea, new Vector2(600f, 26f), new Vector2(640f, 760f), Panel, DarkGold, out _, out _);
            TMP_Text dealerHeaderText = AddText("Header", dealerPanel, "◆  상대 룰렛 · 토끼 딜러  ◆", font, 21, Red, TextAlignmentOptions.Center, new Vector2(0f, 352f), new Vector2(580f, 36f));
            RectTransform dealerIntent = AddFramedPanel("DealerIntent", dealerPanel, new Vector2(0f, 300f), new Vector2(580f, 58f), PanelLight, Red, out _, out _);
            AddText("NextText", dealerIntent, "성향", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(-245f, 0f), new Vector2(70f, 24f));
            TMP_Text enemyNextIntentText = AddText("ValueText", dealerIntent, "판돈 5+에서 CASH OUT", font, 17, Ink, TextAlignmentOptions.Left, new Vector2(35f, 0f), new Vector2(470f, 50f));
            RectTransform dealerRouletteContainer = AddRect("RouletteContainer", dealerPanel, new Vector2(0f, -10f), new Vector2(560f, 560f));
            UnityEngine.UI.Image dealerGlow = AddGlow("ActiveGlow", dealerRouletteContainer, new Color32(220, 72, 78, 90), new Vector2(540f, 540f));
            RectTransform enemyRouletteRoot = AddRect("ExistingDealerRoulette", dealerRouletteContainer, Vector2.zero, new Vector2(530f, 530f));
            RouletteController enemyRoulette = enemyRouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController enemySpin = enemyRouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildRoulette(enemyRouletteRoot, enemyRoulette, enemySpin, legacyFont, frameSprite, pointerSprite, centerCapSprite, null, true);
            AddRect("EffectRoot", dealerRouletteContainer, Vector2.zero, new Vector2(560f, 560f));

            // ── 사건 띠: 방금 일어난 일 3개를 한 줄로(전체 기록 대신).
            RectTransform eventStrip = AddFramedPanel("EventStrip", mainGameArea, new Vector2(0f, -385f), new Vector2(1840f, 44f), Panel, DarkGold, out _, out _);
            AddText("Icon", eventStrip, "LOG", font, 14, Gold, TextAlignmentOptions.Center, new Vector2(-880f, 0f), new Vector2(60f, 30f));
            TMP_Text combatLogText = AddText("CombatLog", eventStrip, "", font, 15, Muted, TextAlignmentOptions.Left, new Vector2(30f, 0f), new Vector2(1740f, 36f));
            combatLogText.textWrappingMode = TextWrappingModes.NoWrap;
            combatLogText.overflowMode = TextOverflowModes.Ellipsis;

            GameObject battleObject = new GameObject("DealerBattle", typeof(RectTransform));
            battleObject.transform.SetParent(canvasObject.transform, false);
            DealerBattleController battle = battleObject.AddComponent<DealerBattleController>();
            BattlePresentationUI presentation = battleObject.AddComponent<BattlePresentationUI>();
            HoldToSpinInput holdInput = throwButton.gameObject.AddComponent<HoldToSpinInput>();
            ConfigurePresentation(presentation, preparePhase, spinPhase, resolvePhase, dealerPhase, playerGlow, dealerGlow, houseRuleFrame);
            HijackTransferPresenter hijackTransferPresenter = BuildHijackTransferEffect(canvasObject.transform, font);

            RectTransform houseRuleOverlay = BuildHouseRuleOverlay(canvasObject.transform, font, out UnityEngine.UI.Button houseRuleInfoCloseButton, out TMP_Text houseRuleDetailProgressText, out TMP_Text[] houseRuleDetailTexts);
            // 딜러 룰렛 아래 안내: 역탈취 경고 + 딜러 특수 칸 효과.
            TMP_Text dealerNoteText = AddText("DealerNote", dealerPanel, "", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(0f, -330f), new Vector2(600f, 80f));
            RectTransform hijackPanel = BuildHijackPanel(canvasObject.transform, font, out TMP_Text hijackInstruction, out TMP_Text hijackSourceTitle, out UnityEngine.UI.Button[] hijackSourceButtons, out TMP_Text[] hijackSourceLabels, out UnityEngine.UI.Button[] hijackDestinationButtons, out TMP_Text[] hijackDestinationLabels);
            RectTransform endPanel = BuildEndPanel(canvasObject.transform, font, out TMP_Text endTitle, out TMP_Text endBody, out UnityEngine.UI.Button endContinueButton, out TMP_Text endContinueLabel);
            RectTransform doorPanel = BuildDoorPanel(canvasObject.transform, font, out TMP_Text doorFloorText, out UnityEngine.UI.Button[] doorButtons, out TMP_Text[] doorTitles, out TMP_Text[] doorBodies);

            BindBattle(
                battle, presentation, hijackTransferPresenter, roulette, enemyRoulette, spin, enemySpin, holdInput,
                playerChipsText, dealerChipsText, insuranceText, potText, playerChipsFill, dealerChipsFill,
                anteText, anteDownButton, anteUpButton, cashOutButton, cashOutLabel,
                roundText, dealerLineText, instructionText, powerPreviewText, resultText, calculationText, riskSummaryText,
                chainPreviewText, combatLogText, houseRuleProgressText, houseRuleFill,
                houseRuleOverlay.gameObject, houseRuleInfoButton, houseRuleInfoCloseButton, houseRuleDetailProgressText,
                openingSpeechBubble.gameObject, enemyNextIntentText,
                hijackPanel.gameObject, hijackInstruction, hijackSourceButtons, hijackSourceLabels,
                hijackDestinationButtons, hijackDestinationLabels, endPanel.gameObject, endTitle, endBody, endContinueButton);

            SerializedObject identitySo = new SerializedObject(battle);
            SetObject(identitySo, "dealerNameText", dealerNameText);
            SetObject(identitySo, "dealerHeaderText", dealerHeaderText);
            SetObject(identitySo, "dealerSprite", rabbitImage);
            SetObject(identitySo, "houseRuleTitleText", houseRuleTitleText);
            SetObject(identitySo, "houseRuleDescriptionText", houseRuleDescriptionText);
            SetObject(identitySo, "hijackSourceTitleText", hijackSourceTitle);
            SetObject(identitySo, "endContinueLabel", endContinueLabel);
            SetObject(identitySo, "dealerNoteText", dealerNoteText);
            SetObjectArray(identitySo.FindProperty("potChipImages"), potChips);
            SetObject(identitySo, "houseRuleDetailNameText", houseRuleDetailTexts[0]);
            SetObject(identitySo, "houseRuleDetailConditionText", houseRuleDetailTexts[1]);
            SetObject(identitySo, "houseRuleDetailRewardText", houseRuleDetailTexts[2]);
            SetObject(identitySo, "houseRuleDetailDealerText", houseRuleDetailTexts[3]);
            SetObject(identitySo, "houseRuleDetailWarningText", houseRuleDetailTexts[4]);
            SetObject(identitySo, "doorPanel", doorPanel.gameObject);
            SetObject(identitySo, "doorFloorText", doorFloorText);
            SetObjectArray(identitySo.FindProperty("doorButtons"), doorButtons);
            SetObjectArray(identitySo.FindProperty("doorTitleTexts"), doorTitles);
            SetObjectArray(identitySo.FindProperty("doorBodyTexts"), doorBodies);
            RectTransform playerOuter = BuildOuterRingStrip("OuterRingStrip", playerPanel, new Vector2(0f, 318f), 96f, font, out UnityEngine.UI.Image[] playerOuterBoxes, out TMP_Text[] playerOuterLabels);
            RectTransform dealerOuter = BuildOuterRingStrip("OuterRingStrip", dealerPanel, new Vector2(0f, 258f), 92f, font, out UnityEngine.UI.Image[] dealerOuterBoxes, out TMP_Text[] dealerOuterLabels);
            SetObject(identitySo, "playerOuterRoot", playerOuter.gameObject);
            SetObjectArray(identitySo.FindProperty("playerOuterBoxes"), playerOuterBoxes);
            SetObjectArray(identitySo.FindProperty("playerOuterLabels"), playerOuterLabels);
            SetObject(identitySo, "dealerOuterRoot", dealerOuter.gameObject);
            SetObjectArray(identitySo.FindProperty("dealerOuterBoxes"), dealerOuterBoxes);
            SetObjectArray(identitySo.FindProperty("dealerOuterLabels"), dealerOuterLabels);
            identitySo.ApplyModifiedPropertiesWithoutUndo();
            doorPanel.gameObject.SetActive(false);
            playerOuter.gameObject.SetActive(false);
            dealerOuter.gameObject.SetActive(false);

            SerializedObject holdSo = new SerializedObject(holdInput);
            holdSo.FindProperty("battle").objectReferenceValue = battle;
            holdSo.FindProperty("fillImage").objectReferenceValue = powerFill;
            holdSo.FindProperty("powerText").objectReferenceValue = null;
            holdSo.FindProperty("buttonLabel").objectReferenceValue = throwLabel;
            holdSo.ApplyModifiedPropertiesWithoutUndo();

            houseRuleOverlay.gameObject.SetActive(false);
            hijackPanel.gameObject.SetActive(false);
            endPanel.gameObject.SetActive(false);
            dealerGlow.gameObject.SetActive(false);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = battleObject;
            Debug.Log($"Battle scene built at {ScenePath}");
        }

        private static void BuildRoulette(RectTransform root, RouletteController controller, RouletteSpinController spin, Font legacyFont, Sprite frameSprite, Sprite pointerSprite, Sprite centerCapSprite, AudioClip spinSound, bool enemy)
        {
            float wheelSize = enemy ? 430f : 470f;
            float frameSize = enemy ? 530f : 578f;
            RectTransform wheel = AddRect("Wheel", root, Vector2.zero, new Vector2(wheelSize, wheelSize));
            RectTransform segments = AddRect("DynamicSegments", wheel, Vector2.zero, new Vector2(wheelSize, wheelSize));
            RoulettePixelWheelRenderer renderer = segments.gameObject.AddComponent<RoulettePixelWheelRenderer>();
            renderer.raycastTarget = false;
            UnityEngine.UI.Image centerCap = AddSpriteImage("CenterCap", wheel, centerCapSprite, new Vector2(96f, 96f));
            UnityEngine.UI.Image fixedFrame = AddSpriteImage("FixedFrame", root, frameSprite, new Vector2(frameSize, frameSize));
            fixedFrame.transform.SetAsLastSibling();
            UnityEngine.UI.Image pointer = AddSpriteImage("Pointer", root, pointerSprite, new Vector2(110f, 110f));
            pointer.rectTransform.anchoredPosition = new Vector2(0f, enemy ? 222f : 244f);

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("wheel").objectReferenceValue = wheel;
            controllerSo.FindProperty("dynamicSegmentRoot").objectReferenceValue = segments;
            controllerSo.FindProperty("pixelWheelRenderer").objectReferenceValue = renderer;
            controllerSo.FindProperty("labelFont").objectReferenceValue = legacyFont;
            controllerSo.FindProperty("labelFontSize").intValue = enemy ? 19 : 21;
            controllerSo.FindProperty("iconRadiusRatio").floatValue = 0.5f;
            controllerSo.FindProperty("labelRadiusRatio").floatValue = enemy ? 0.7f : 0.73f;
            controllerSo.FindProperty("labelSize").vector2Value = enemy ? new Vector2(104f, 40f) : new Vector2(108f, 44f);
            controllerSo.FindProperty("keepLabelsUpright").boolValue = enemy;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            AudioSource spinAudioSource = null;
            if (spinSound != null)
            {
                spinAudioSource = root.gameObject.AddComponent<AudioSource>();
                spinAudioSource.clip = spinSound;
                spinAudioSource.playOnAwake = false;
                spinAudioSource.loop = false;
                spinAudioSource.spatialBlend = 0f;
                spinAudioSource.volume = 0.72f;
            }

            SerializedObject spinSo = new SerializedObject(spin);
            spinSo.FindProperty("rouletteController").objectReferenceValue = controller;
            spinSo.FindProperty("wheel").objectReferenceValue = wheel;
            spinSo.FindProperty("centerCap").objectReferenceValue = centerCap.rectTransform;
            spinSo.FindProperty("minSpinDuration").floatValue = enemy ? 2.1f : 1.8f;
            spinSo.FindProperty("maxSpinDuration").floatValue = enemy ? 2.7f : 3.3f;
            spinSo.FindProperty("startSpeed").floatValue = enemy ? 960f : 1080f;
            spinSo.FindProperty("deceleration").floatValue = enemy ? 680f : 720f;
            spinSo.FindProperty("minimumFullRotations").intValue = 2;
            spinSo.FindProperty("selectedHighlightDuration").floatValue = 0.9f;
            spinSo.FindProperty("spinAudioSource").objectReferenceValue = spinAudioSource;
            spinSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>하우스 룰 상세 페이지. 문구는 컨트롤러가 딜러에 맞게 채운다(detailTexts: 이름·조건·보상·딜러 변화·주의).</summary>
        private static RectTransform BuildHouseRuleOverlay(Transform parent, TMP_FontAsset font, out UnityEngine.UI.Button closeButton, out TMP_Text detailProgress, out TMP_Text[] detailTexts)
        {
            RectTransform overlay = AddPanel("HouseRuleInfoPanel", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(10, 8, 14, 220));
            overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            RectTransform dialog = AddFramedPanel("Dialog", overlay, Vector2.zero, new Vector2(840f, 500f), new Color32(35, 29, 45, 255), Gold, out _, out _);
            AddText("Title", dialog, "HOUSE RULE", font, 24, Gold, TextAlignmentOptions.Center, new Vector2(0f, 200f), new Vector2(720f, 38f));
            detailTexts = new TMP_Text[5];
            detailTexts[0] = AddText("RuleName", dialog, "전액 보장", font, 36, Ink, TextAlignmentOptions.Center, new Vector2(0f, 150f), new Vector2(720f, 52f));
            AddText("ConditionLabel", dialog, "발동 조건", font, 18, Gold, TextAlignmentOptions.Left, new Vector2(-290f, 64f), new Vector2(150f, 30f));
            detailTexts[1] = AddText("Condition", dialog, "", font, 19, Ink, TextAlignmentOptions.Left, new Vector2(70f, 64f), new Vector2(560f, 46f));
            AddText("RewardLabel", dialog, "보상", font, 18, Gold, TextAlignmentOptions.Left, new Vector2(-290f, 8f), new Vector2(150f, 30f));
            detailTexts[2] = AddText("Reward", dialog, "", font, 19, Ink, TextAlignmentOptions.Left, new Vector2(70f, 8f), new Vector2(560f, 46f));
            AddText("PenaltyLabel", dialog, "딜러 칸", font, 18, Gold, TextAlignmentOptions.Left, new Vector2(-290f, -48f), new Vector2(150f, 30f));
            detailTexts[3] = AddText("Penalty", dialog, "", font, 16, Ink, TextAlignmentOptions.TopLeft, new Vector2(70f, -70f), new Vector2(560f, 90f));
            detailTexts[4] = AddText("Warning", dialog, "", font, 16, Muted, TextAlignmentOptions.Center, new Vector2(0f, -135f), new Vector2(760f, 40f));
            detailProgress = AddText("Progress", dialog, "현재 진행도  0 / 1", font, 20, Gold, TextAlignmentOptions.Center, new Vector2(0f, -195f), new Vector2(720f, 36f));
            closeButton = AddButton("CloseButton", dialog, new Vector2(372f, 210f), new Vector2(54f, 54f), PanelLight, font, "×", 28, out _);
            AddRect("AnimationRoot", dialog, Vector2.zero, new Vector2(840f, 500f));
            return overlay;
        }

        /// <summary>딜러 칸(최대 14칸, 7칸씩 2줄)과 내 칸 8개를 고르는 HIJACK 패널. JACKPOT 칸 배치에도 재사용한다.</summary>
        private static RectTransform BuildHijackPanel(Transform parent, TMP_FontAsset font, out TMP_Text instruction, out TMP_Text sourceTitle, out UnityEngine.UI.Button[] sourceButtons, out TMP_Text[] sourceLabels, out UnityEngine.UI.Button[] destinationButtons, out TMP_Text[] destinationLabels)
        {
            RectTransform panel = AddFramedPanel("HijackPanel", parent, new Vector2(0f, -60f), new Vector2(1180f, 600f), new Color32(30, 25, 39, 255), Gold, out _, out _);
            // 뒤의 전투 화면을 어둡게 덮고 클릭을 막는다(문 선택 패널과 같은 방식).
            UnityEngine.UI.Image dim = AddImage("Dim", panel, new Color32(10, 8, 14, 225));
            dim.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            dim.rectTransform.sizeDelta = new Vector2(1920f, 1080f);
            dim.raycastTarget = true;
            dim.transform.SetAsFirstSibling();
            AddText("Title", panel, "HOUSE RULE CLEAR · HIJACK", font, 32, Gold, TextAlignmentOptions.Center, new Vector2(0f, 255f), new Vector2(1080f, 48f));
            instruction = AddText("Instruction", panel, "1. 빼앗을 딜러의 칸을 고르세요", font, 20, Ink, TextAlignmentOptions.Center, new Vector2(0f, 212f), new Vector2(1100f, 38f));
            sourceTitle = AddText("EnemySlotsTitle", panel, "딜러 룰렛 · 빼앗을 칸", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-390f, 175f), new Vector2(360f, 30f));
            sourceButtons = new UnityEngine.UI.Button[14];
            sourceLabels = new TMP_Text[14];
            for (int i = 0; i < sourceButtons.Length; i++)
            {
                float x = -480f + (i % 7) * 160f;
                float y = i < 7 ? 125f : 45f;
                sourceButtons[i] = AddButton($"EnemySlot{i + 1}", panel, new Vector2(x, y), new Vector2(148f, 70f), new Color32(112, 50, 60, 255), font, $"{i + 1}\n-", 15, out sourceLabels[i]);
            }
            AddText("PlayerSlotsTitle", panel, "내 룰렛 · 덮어쓸 위치 (하우스 몫 불가)", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-390f, -15f), new Vector2(360f, 30f));
            destinationButtons = new UnityEngine.UI.Button[8];
            destinationLabels = new TMP_Text[8];
            for (int i = 0; i < destinationButtons.Length; i++)
            {
                float x = -437.5f + i * 125f;
                destinationButtons[i] = AddButton($"PlayerSlot{i + 1}", panel, new Vector2(x, -80f), new Vector2(112f, 86f), new Color32(38, 91, 96, 255), font, $"{i + 1}\n-", 15, out destinationLabels[i]);
            }
            AddText("Note", panel, "빼앗은 딜러 칸은 봉인되고, 가져온 칸은 내 룰렛에 영구히 남습니다. [JP] = JACKPOT 칸", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(0f, -170f), new Vector2(1060f, 36f));
            AddRect("AnimationRoot", panel, Vector2.zero, new Vector2(1180f, 600f));
            return panel;
        }

        /// <summary>
        /// 테이블 가운데 판돈 칩 더미(그레이박스): 칩 12개 자리를 3줄 기둥으로 쌓아 두고, 컨트롤러가 판돈만큼 켠다.
        /// </summary>
        private static UnityEngine.UI.Image[] BuildPotChipStack(Transform parent, Vector2 position)
        {
            RectTransform stack = AddRect("PotChipStack", parent, position, new Vector2(240f, 90f));
            UnityEngine.UI.Image[] chips = new UnityEngine.UI.Image[12];
            for (int i = 0; i < chips.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                chips[i] = AddImage($"Chip{i + 1}", stack, column == 1 ? Gold : new Color32(196, 140, 40, 255));
                chips[i].rectTransform.anchoredPosition = new Vector2((column - 1) * 58f, -34f + row * 20f);
                chips[i].rectTransform.sizeDelta = new Vector2(50f, 16f);
            }

            return chips;
        }

        /// <summary>
        /// 바깥 링(이중 룰렛)을 보여주는 그레이박스 띠: 칸 6개 자리, 멈춘 칸은 컨트롤러가 금색으로 강조한다.
        /// 원형 바깥 링 아트는 ArtStyleBible 확정 뒤에 교체한다.
        /// </summary>
        private static RectTransform BuildOuterRingStrip(string name, Transform parent, Vector2 position, float boxWidth, TMP_FontAsset font, out UnityEngine.UI.Image[] boxes, out TMP_Text[] labels)
        {
            const int Count = 6;
            RectTransform strip = AddRect(name, parent, position, new Vector2(Count * (boxWidth + 6f), 30f));
            boxes = new UnityEngine.UI.Image[Count];
            labels = new TMP_Text[Count];
            for (int i = 0; i < Count; i++)
            {
                float x = (i - (Count - 1) * 0.5f) * (boxWidth + 6f);
                boxes[i] = AddImage($"OuterSlot{i + 1}", strip, new Color32(61, 53, 79, 255));
                boxes[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
                boxes[i].rectTransform.sizeDelta = new Vector2(boxWidth, 28f);
                labels[i] = AddText("Label", boxes[i].transform, "-", font, 14, Ink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(boxWidth - 6f, 24f));
            }

            return strip;
        }

        /// <summary>층 사이 문 선택 패널: 딜러 카드 두 장(이름·성향·하우스 룰·JACKPOT)과 층 안내.</summary>
        private static RectTransform BuildDoorPanel(Transform parent, TMP_FontAsset font, out TMP_Text floorText, out UnityEngine.UI.Button[] doorButtons, out TMP_Text[] doorTitles, out TMP_Text[] doorBodies)
        {
            RectTransform overlay = AddPanel("DoorPanel", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(10, 8, 14, 235));
            overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            AddText("Title", overlay, "다음 테이블", font, 40, Gold, TextAlignmentOptions.Center, new Vector2(0f, 380f), new Vector2(1200f, 60f));
            floorText = AddText("FloorText", overlay, "2층", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 325f), new Vector2(1200f, 36f));
            doorButtons = new UnityEngine.UI.Button[2];
            doorTitles = new TMP_Text[2];
            doorBodies = new TMP_Text[2];
            for (int i = 0; i < doorButtons.Length; i++)
            {
                float x = i == 0 ? -330f : 330f;
                doorButtons[i] = AddButton($"Door{i + 1}", overlay, new Vector2(x, -20f), new Vector2(560f, 520f), new Color32(43, 38, 58, 255), font, "", 16, out TMP_Text unused);
                unused.gameObject.SetActive(false);
                AddFrame("Frame", doorButtons[i].transform, DarkGold);
                doorTitles[i] = AddText("DealerName", doorButtons[i].transform, "딜러", font, 32, Gold, TextAlignmentOptions.Center, new Vector2(0f, 200f), new Vector2(500f, 48f));
                doorBodies[i] = AddText("DealerInfo", doorButtons[i].transform, "", font, 19, Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 10f), new Vector2(480f, 300f));
                doorBodies[i].lineSpacing = 18f;
                AddText("Enter", doorButtons[i].transform, "이 문으로 들어가기", font, 20, Teal, TextAlignmentOptions.Center, new Vector2(0f, -215f), new Vector2(500f, 36f));
            }

            return overlay;
        }

        private static RectTransform BuildEndPanel(Transform parent, TMP_FontAsset font, out TMP_Text title, out TMP_Text body, out UnityEngine.UI.Button continueButton, out TMP_Text continueLabel)
        {
            RectTransform panel = AddFramedPanel("EndPanel", parent, Vector2.zero, new Vector2(900f, 480f), new Color32(35, 29, 45, 252), Gold, out _, out _);
            title = AddText("Title", panel, "", font, 42, Gold, TextAlignmentOptions.Center, new Vector2(0f, 130f), new Vector2(760f, 64f));
            body = AddText("Body", panel, "", font, 23, Ink, TextAlignmentOptions.Center, new Vector2(0f, 20f), new Vector2(760f, 120f));
            continueButton = AddButton("ContinueButton", panel, new Vector2(0f, -145f), new Vector2(360f, 76f), Gold, font, "계약 되감기", 24, out continueLabel);
            continueLabel.color = Background;
            return panel;
        }

        private static HijackTransferPresenter BuildHijackTransferEffect(Transform parent, TMP_FontAsset font)
        {
            RectTransform layer = AddStretchRect(
                "HijackEffectLayer",
                parent,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            HijackTransferPresenter presenter = layer.gameObject.AddComponent<HijackTransferPresenter>();

            RectTransform token = AddFramedPanel(
                "HijackSlotToken",
                layer,
                Vector2.zero,
                new Vector2(168f, 104f),
                new Color32(73, 45, 74, 255),
                Gold,
                out UnityEngine.UI.Image tokenBackground,
                out UnityEngine.UI.Image tokenFrame);
            CanvasGroup tokenCanvasGroup = token.gameObject.AddComponent<CanvasGroup>();
            tokenCanvasGroup.interactable = false;
            tokenCanvasGroup.blocksRaycasts = false;

            UnityEngine.UI.Image iconPlaceholder = AddImage("IconPlaceholder", token, new Color32(224, 168, 52, 255));
            iconPlaceholder.sprite = GetFilledImageSprite();
            iconPlaceholder.type = UnityEngine.UI.Image.Type.Sliced;
            iconPlaceholder.rectTransform.anchoredPosition = new Vector2(-55f, 9f);
            iconPlaceholder.rectTransform.sizeDelta = new Vector2(42f, 42f);
            AddText("IconText", iconPlaceholder.transform, "SLOT", font, 10, Background, TextAlignmentOptions.Center, Vector2.zero, new Vector2(38f, 28f));
            TMP_Text valueText = AddText("ValueText", token, "공격 4", font, 19, Ink, TextAlignmentOptions.Center, new Vector2(30f, 11f), new Vector2(100f, 34f));
            TMP_Text ownerText = AddText("OwnerText", token, "DEALER  →  PLAYER", font, 10, Gold, TextAlignmentOptions.Center, new Vector2(0f, -34f), new Vector2(150f, 22f));

            UnityEngine.UI.Image impactFlashImage = AddImage("ImpactFlash", layer, Gold);
            impactFlashImage.sprite = GetFilledImageSprite();
            impactFlashImage.type = UnityEngine.UI.Image.Type.Sliced;
            impactFlashImage.fillCenter = false;
            impactFlashImage.rectTransform.sizeDelta = new Vector2(190f, 190f);

            SerializedObject so = new SerializedObject(presenter);
            SetObject(so, "effectLayer", layer);
            SetObject(so, "tokenRoot", token);
            SetObject(so, "tokenCanvasGroup", tokenCanvasGroup);
            SetObject(so, "tokenBackground", tokenBackground);
            SetObject(so, "tokenFrame", tokenFrame);
            SetObject(so, "tokenValueText", valueText);
            SetObject(so, "tokenOwnerText", ownerText);
            SetObject(so, "impactFlash", impactFlashImage.rectTransform);
            SetObject(so, "impactFlashImage", impactFlashImage);
            so.ApplyModifiedPropertiesWithoutUndo();

            token.gameObject.SetActive(false);
            impactFlashImage.gameObject.SetActive(false);
            return presenter;
        }

        private static void ConfigurePresentation(BattlePresentationUI presentation, TMP_Text prepare, TMP_Text spin, TMP_Text resolve, TMP_Text dealer, UnityEngine.UI.Image playerGlow, UnityEngine.UI.Image dealerGlow, UnityEngine.UI.Image houseRuleFrame)
        {
            SerializedObject so = new SerializedObject(presentation);
            SetObject(so, "prepareText", prepare);
            SetObject(so, "spinText", spin);
            SetObject(so, "resolveText", resolve);
            SetObject(so, "dealerText", dealer);
            SetObject(so, "playerRouletteGlow", playerGlow);
            SetObject(so, "dealerRouletteGlow", dealerGlow);
            SetObject(so, "houseRuleFrame", houseRuleFrame);
            so.FindProperty("inactiveColor").colorValue = new Color32(176, 169, 190, 255);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindBattle(
            DealerBattleController battle, BattlePresentationUI presentation,
            HijackTransferPresenter hijackTransferPresenter,
            RouletteController roulette, RouletteController enemyRoulette,
            RouletteSpinController spin, RouletteSpinController enemySpin, HoldToSpinInput holdInput,
            TMP_Text playerChipsText, TMP_Text dealerChipsText, TMP_Text insuranceText, TMP_Text potText,
            UnityEngine.UI.Image playerChipsFill, UnityEngine.UI.Image dealerChipsFill,
            TMP_Text anteText, UnityEngine.UI.Button anteDownButton, UnityEngine.UI.Button anteUpButton,
            UnityEngine.UI.Button cashOutButton, TMP_Text cashOutLabel,
            TMP_Text roundText, TMP_Text dealerLineText, TMP_Text instructionText, TMP_Text powerPreviewText,
            TMP_Text resultText, TMP_Text calculationText, TMP_Text riskSummaryText, TMP_Text chainPreviewText, TMP_Text combatLogText,
            TMP_Text houseRuleProgressText, UnityEngine.UI.Image houseRuleProgressFill,
            GameObject houseRuleInfoPanel, UnityEngine.UI.Button houseRuleInfoButton,
            UnityEngine.UI.Button houseRuleInfoCloseButton, TMP_Text houseRuleDetailProgressText,
            GameObject openingSpeechBubble, TMP_Text enemyNextIntentText,
            GameObject hijackPanel, TMP_Text hijackInstructionText,
            UnityEngine.UI.Button[] hijackSourceButtons, TMP_Text[] hijackSourceLabels,
            UnityEngine.UI.Button[] hijackDestinationButtons, TMP_Text[] hijackDestinationLabels,
            GameObject endPanel, TMP_Text endTitle, TMP_Text endBody, UnityEngine.UI.Button endContinueButton)
        {
            SerializedObject so = new SerializedObject(battle);
            SetObject(so, "presentationUi", presentation);
            SetObject(so, "hijackTransferPresenter", hijackTransferPresenter);
            SetObject(so, "roulette", roulette);
            SetObject(so, "enemyRoulette", enemyRoulette);
            SetObject(so, "spinController", spin);
            SetObject(so, "enemySpinController", enemySpin);
            SetObject(so, "spinInput", holdInput);
            SetObject(so, "playerChipsText", playerChipsText);
            SetObject(so, "dealerChipsText", dealerChipsText);
            SetObject(so, "insuranceText", insuranceText);
            SetObject(so, "potText", potText);
            SetObject(so, "playerChipsFill", playerChipsFill);
            SetObject(so, "dealerChipsFill", dealerChipsFill);
            SetObject(so, "anteText", anteText);
            SetObject(so, "anteDownButton", anteDownButton);
            SetObject(so, "anteUpButton", anteUpButton);
            SetObject(so, "cashOutButton", cashOutButton);
            SetObject(so, "cashOutLabel", cashOutLabel);
            SetObject(so, "roundText", roundText);
            SetObject(so, "dealerLineText", dealerLineText);
            SetObject(so, "instructionText", instructionText);
            SetObject(so, "powerPreviewText", powerPreviewText);
            SetObject(so, "resultText", resultText);
            SetObject(so, "calculationText", calculationText);
            SetObject(so, "riskSummaryText", riskSummaryText);
            SetObject(so, "chainPreviewText", chainPreviewText);
            SetObject(so, "combatLogText", combatLogText);
            SetObject(so, "houseRuleProgressText", houseRuleProgressText);
            SetObject(so, "houseRuleProgressFill", houseRuleProgressFill);
            SetObject(so, "houseRuleInfoPanel", houseRuleInfoPanel);
            SetObject(so, "houseRuleInfoButton", houseRuleInfoButton);
            SetObject(so, "houseRuleInfoCloseButton", houseRuleInfoCloseButton);
            SetObject(so, "houseRuleDetailProgressText", houseRuleDetailProgressText);
            SetObject(so, "openingSpeechBubble", openingSpeechBubble);
            SetObject(so, "enemyNextIntentText", enemyNextIntentText);
            SetObject(so, "hijackPanel", hijackPanel);
            SetObject(so, "hijackInstructionText", hijackInstructionText);
            SetObject(so, "endPanel", endPanel);
            SetObject(so, "endTitleText", endTitle);
            SetObject(so, "endBodyText", endBody);
            SetObject(so, "endContinueButton", endContinueButton);
            SetObjectArray(so.FindProperty("hijackSourceButtons"), hijackSourceButtons);
            SetObjectArray(so.FindProperty("hijackSourceLabels"), hijackSourceLabels);
            SetObjectArray(so.FindProperty("hijackDestinationButtons"), hijackDestinationButtons);
            SetObjectArray(so.FindProperty("hijackDestinationLabels"), hijackDestinationLabels);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_FontAsset GetOrCreateFontAsset()
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (asset != null) return asset;
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/RouletteSystem/Art/Fonts/neodgm.ttf");
            asset = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.ImportAsset(FontAssetPath);
            return asset;
        }

        private static UnityEngine.UI.Image AddChipsWidget(string name, Transform parent, Vector2 position, string combatantName, Color fillColor, TMP_FontAsset font, out TMP_Text chipsText, out TMP_Text nameText)
        {
            RectTransform root = AddRect(name, parent, position, new Vector2(440f, 78f));
            nameText = AddText("NameText", root, combatantName, font, 18, Ink, TextAlignmentOptions.Left, new Vector2(-125f, 20f), new Vector2(170f, 28f));
            chipsText = AddText("ChipsText", root, "칩 20", font, 18, Ink, TextAlignmentOptions.Right, new Vector2(125f, 20f), new Vector2(170f, 28f));
            UnityEngine.UI.Image background = AddImage("BarBackground", root, new Color32(58, 52, 70, 255));
            background.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            background.rectTransform.sizeDelta = new Vector2(420f, 18f);
            UnityEngine.UI.Image fill = AddImage("BarFill", background.transform, fillColor);
            ConfigureFill(fill, 1f);
            return fill;
        }

        private static TMP_Text AddBadge(string name, Transform parent, string label, Color accent, TMP_FontAsset font, Vector2 position)
        {
            RectTransform badge = AddFramedPanel(name, parent, position, new Vector2(240f, 44f), PanelLight, accent, out _, out _);
            return AddText("Label", badge, label, font, 16, Ink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(220f, 30f));
        }

        private static UnityEngine.UI.Image AddGlow(string name, Transform parent, Color color, Vector2 size)
        {
            UnityEngine.UI.Image glow = AddImage(name, parent, color);
            glow.sprite = GetFilledImageSprite();
            glow.type = UnityEngine.UI.Image.Type.Sliced;
            glow.fillCenter = false;
            glow.rectTransform.sizeDelta = size;
            return glow;
        }

        private static RectTransform AddFramedPanel(string name, Transform parent, Vector2 position, Vector2 size, Color backgroundColor, Color frameColor, out UnityEngine.UI.Image background, out UnityEngine.UI.Image frame)
        {
            RectTransform root = AddRect(name, parent, position, size);
            background = AddImage("Background", root, backgroundColor);
            Stretch(background.rectTransform);
            frame = AddFrame("Frame", root, frameColor);
            return root;
        }

        private static UnityEngine.UI.Image AddFrame(string name, Transform parent, Color color)
        {
            UnityEngine.UI.Image frame = AddImage(name, parent, color);
            frame.sprite = GetFilledImageSprite();
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.fillCenter = false;
            Stretch(frame.rectTransform);
            return frame;
        }

        private static UnityEngine.UI.Button AddButton(string name, Transform parent, Vector2 position, Vector2 size, Color color, TMP_FontAsset font, string label, int fontSize, out TMP_Text labelText)
        {
            UnityEngine.UI.Image image = AddImage(name, parent, color);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            UnityEngine.UI.Button button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            image.raycastTarget = true;
            labelText = AddText("Label", image.transform, label, font, fontSize, Ink, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(20f, 14f));
            return button;
        }

        private static RectTransform AddPanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            UnityEngine.UI.Image image = AddImage(name, parent, color);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image.rectTransform;
        }

        private static UnityEngine.UI.Image AddImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            gameObject.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = gameObject.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Image AddSpriteImage(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            UnityEngine.UI.Image image = AddImage(name, parent, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static TMP_Text AddText(string name, Transform parent, string text, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform AddRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform AddStretchRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = AddRect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static void ConfigureFill(UnityEngine.UI.Image fill, float amount)
        {
            Stretch(fill.rectTransform);
            fill.sprite = GetFilledImageSprite();
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillClockwise = true;
            fill.fillAmount = amount;
        }

        private static Sprite GetFilledImageSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static Sprite GetSprite(string spriteName)
        {
            return GetSprite(RouletteSpriteSheetPath, spriteName);
        }

        private static Sprite GetSprite(string assetPath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName) return sprite;
            }
            Debug.LogError($"Missing sprite '{spriteName}' in {assetPath}");
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            so.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
#endif
