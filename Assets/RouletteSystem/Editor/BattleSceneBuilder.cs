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
        private const string RouletteTickSoundPath = "Assets/RouletteSystem/Music/hijackpot_roulette_sfx_3pack/hijackpot_roulette_tick.wav";
        private const string RouletteStopSoundPath = "Assets/RouletteSystem/Music/hijackpot_roulette_sfx_3pack/hijackpot_roulette_stop.wav";

        // ── UI 1단계 와이어프레임 팔레트(2026-10-02): 장식 없이 배치와 위계만 본다. 최종 색·재질은 ArtStyleBible(아르데코 카지노) 확정 뒤.
        // 회색 단계로 정보의 중요도를 나누고, 강조색(Accent)은 판돈·CASH OUT·SPIN·내 차례에만, 위험색(Danger)은 하우스 몫에만 쓴다.
        private static readonly Color Background = new Color32(24, 24, 26, 255);
        private static readonly Color Felt = new Color32(40, 41, 44, 255);
        private static readonly Color StageBackground = new Color32(0, 0, 0, 0);
        private static readonly Color Panel = new Color32(46, 47, 50, 250);
        private static readonly Color PanelInset = new Color32(36, 37, 40, 255);
        private static readonly Color PanelLight = new Color32(66, 67, 71, 255);
        private static readonly Color Gold = new Color32(214, 214, 210, 255);
        private static readonly Color DarkGold = new Color32(70, 71, 75, 255);
        private static readonly Color Ink = new Color32(236, 236, 232, 255);
        private static readonly Color Muted = new Color32(150, 150, 150, 255);
        private static readonly Color Red = new Color32(120, 120, 124, 255);
        private static readonly Color Teal = new Color32(176, 176, 180, 255);
        private static readonly Color Accent = new Color32(226, 170, 64, 255);
        private static readonly Color Danger = new Color32(206, 74, 78, 255);

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
            AudioClip rouletteTick = AssetDatabase.LoadAssetAtPath<AudioClip>(RouletteTickSoundPath);
            AudioClip rouletteStop = AssetDatabase.LoadAssetAtPath<AudioClip>(RouletteStopSoundPath);

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
            UnityEngine.UI.Image tableFelt = AddImage("TableFelt", backgroundLayer, Felt);
            tableFelt.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            tableFelt.rectTransform.anchoredPosition = new Vector2(0f, -90f);
            tableFelt.rectTransform.sizeDelta = new Vector2(1880f, 900f);

            // ── 상단 무대(화면 위 25%, 배치 A안 2026-10-02): 격투 게임처럼 양 끝에 큰 캐릭터가 마주 보고,
            // 칩 바가 가운데 VS를 향해 뻗는다. 하우스 룰 카드는 그 아래 가운데, 딜러 대사는 딜러 옆 말풍선.
            RectTransform battleStage = AddStretchRect(
                "BattleStage", canvasObject.transform,
                new Vector2(0f, 0.75f), Vector2.one,
                new Vector2(40f, 6f), new Vector2(-40f, -16f));
            UnityEngine.UI.Image stagePlaceholder = AddImage("BackgroundPlaceholder", battleStage, StageBackground);
            Stretch(stagePlaceholder.rectTransform);

            RectTransform playerArea = AddRect("PlayerArea", battleStage, new Vector2(-640f, 0f), new Vector2(540f, 190f));
            TMP_Text playerChipsText;
            UnityEngine.UI.Image playerChipsFill = AddChipsWidget("PlayerChips", playerArea, new Vector2(270f, 64f), "손님", Teal, font, out playerChipsText, out _, 600f, false);
            // 유물·NUDGE 띠: 칩 바 아래. 누르면 유물 설명이 펼쳐진다(전체 설명은 필요할 때만, CLAUDE.md 6장).
            // 유물 띠: 왼쪽 현금·NUDGE 숫자, 오른쪽 유물 아이콘 8칸. 아이콘에 올리면 설명 카드, 누르면 전체 설명(글은 필요할 때만).
            UnityEngine.UI.Button relicStripButton = AddButton("RelicStrip", playerArea, new Vector2(190f, 0f), new Vector2(440f, 56f), PanelLight, font, "", 14, out TMP_Text relicStripText);
            relicStripText.alignment = TextAlignmentOptions.Left;
            relicStripText.rectTransform.anchoredPosition = new Vector2(-142f, 0f);
            relicStripText.rectTransform.sizeDelta = new Vector2(140f, 50f);
            relicStripText.text = "NUDGE 1";
            relicStripText.enableAutoSizing = true;
            relicStripText.fontSizeMin = 10f;
            relicStripText.fontSizeMax = 14f;
            RelicIconHover[] relicIcons = new RelicIconHover[8];
            TMP_Text[] relicIconLabels = new TMP_Text[8];
            UnityEngine.UI.Image[] relicIconImages = new UnityEngine.UI.Image[8];
            for (int i = 0; i < relicIcons.Length; i++)
            {
                relicIconImages[i] = AddImage($"RelicIcon{i + 1}", relicStripButton.transform, new Color32(80, 70, 100, 255));
                relicIconImages[i].rectTransform.anchoredPosition = new Vector2(-50f + i * 37f, 0f);
                relicIconImages[i].rectTransform.sizeDelta = new Vector2(34f, 34f);
                relicIconImages[i].raycastTarget = true;
                relicIconLabels[i] = AddText("Glyph", relicIconImages[i].transform, "", font, 12, Ink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(34f, 34f));
                relicIconLabels[i].enableAutoSizing = true;
                relicIconLabels[i].fontSizeMin = 8f;
                relicIconLabels[i].fontSizeMax = 13f;
                relicIcons[i] = relicIconImages[i].gameObject.AddComponent<RelicIconHover>();
            }
            RectTransform playerCharacter = AddRect("PlayerCharacter", playerArea, new Vector2(-160f, -4f), new Vector2(240f, 240f));
            UnityEngine.UI.Image businessmanImage = AddSpriteImage("Sprite", playerCharacter, businessmanIdleFrames[0], new Vector2(236f, 236f));
            businessmanImage.raycastTarget = false;
            AddRect("EffectRoot", playerCharacter, Vector2.zero, new Vector2(170f, 170f));
            NaturalBlinkAnimator blinkAnimator = businessmanImage.gameObject.AddComponent<NaturalBlinkAnimator>();
            SerializedObject blinkSo = new SerializedObject(blinkAnimator);
            blinkSo.FindProperty("targetImage").objectReferenceValue = businessmanImage;
            SetObjectArray(blinkSo.FindProperty("idleFrames"), businessmanIdleFrames);
            SetObjectArray(blinkSo.FindProperty("blinkFrames"), businessmanBlinkFrames);
            blinkSo.ApplyModifiedPropertiesWithoutUndo();

            AddText("Versus", battleStage, "VS", font, 46, Accent, TextAlignmentOptions.Center, new Vector2(0f, 60f), new Vector2(120f, 56f));
            RectTransform centerStage = AddRect("CenterStage", battleStage, new Vector2(0f, -40f), new Vector2(760f, 190f));
            RectTransform roundHeader = AddRect("RoundHeader", centerStage, new Vector2(0f, 144f), new Vector2(730f, 34f));
            TMP_Text roundText = AddText("RoundTitle", roundHeader, "TUTORIAL TABLE  ·  ROUND 1", font, 16, Muted, TextAlignmentOptions.Center, Vector2.zero, new Vector2(720f, 34f));

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
            houseRuleCard.localScale = Vector3.one * 0.86f;

            RectTransform dealerArea = AddRect("DealerArea", battleStage, new Vector2(640f, 0f), new Vector2(540f, 190f));
            TMP_Text dealerChipsText;
            UnityEngine.UI.Image dealerChipsFill = AddChipsWidget("DealerChips", dealerArea, new Vector2(-270f, 64f), "토끼 딜러", Red, font, out dealerChipsText, out TMP_Text dealerNameText, 600f, true);
            RectTransform dealerCharacter = AddRect("DealerCharacter", dealerArea, new Vector2(160f, -4f), new Vector2(240f, 240f));
            UnityEngine.UI.Image rabbitImage = AddSpriteImage("Sprite", dealerCharacter, rabbitSprite, new Vector2(236f, 236f));
            rabbitImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rabbitImage.raycastTarget = false;
            AddRect("EffectRoot", dealerCharacter, Vector2.zero, new Vector2(170f, 170f));
            // 딜러 한마디 말풍선(가운데 판에서 옮겨 옴): 딜러 왼쪽, 하우스 룰 카드 오른쪽.
            RectTransform dealerLineBubble = AddPanel("DealerLineBubble", dealerArea, new Vector2(-160f, -62f), new Vector2(320f, 84f), new Color32(232, 232, 228, 255));
            TMP_Text dealerLineText = AddText("DealerLine", dealerLineBubble, "어서오세요, 첫 손님이시네요.", font, 16, Background, TextAlignmentOptions.Center, Vector2.zero, new Vector2(296f, 72f));
            dealerLineText.enableAutoSizing = true;
            dealerLineText.fontSizeMin = 11f;
            dealerLineText.fontSizeMax = 16f;
            RectTransform openingSpeechBubble = AddFramedPanel("DialogueBubble", dealerArea, new Vector2(-160f, -62f), new Vector2(320f, 84f), new Color32(239, 235, 225, 255), DarkGold, out _, out _);
            AddText("Text", openingSpeechBubble, "걸고, 돌리고, 적당할 때 터뜨리세요.\n보험으로 제 정산을 막으면 칸을 드려요.", font, 13, Background, TextAlignmentOptions.Center, Vector2.zero, new Vector2(300f, 56f));
            AddRect("AnimationRoot", openingSpeechBubble, Vector2.zero, new Vector2(320f, 70f));

            // ── 메인 영역(화면 아래 80%): 내 룰렛 | 테이블 | 딜러 룰렛, 맨 아래 사건 띠.
            RectTransform mainGameArea = AddStretchRect(
                "MainGameArea", canvasObject.transform,
                Vector2.zero, new Vector2(1f, 0.75f),
                new Vector2(40f, 20f), new Vector2(-40f, -6f));

            RectTransform playerPanel = AddRect("PlayerRoulettePanel", mainGameArea, new Vector2(-600f, 26f), new Vector2(640f, 760f));
            AddText("Header", playerPanel, "내 룰렛", font, 18, Muted, TextAlignmentOptions.Center, new Vector2(0f, 352f), new Vector2(560f, 36f)).gameObject.SetActive(false);
            RectTransform playerRouletteContainer = AddRect("RouletteContainer", playerPanel, new Vector2(0f, 10f), new Vector2(620f, 620f));
            UnityEngine.UI.Image playerGlow = AddGlow("ActiveGlow", playerRouletteContainer, new Color32(226, 170, 64, 70), new Vector2(600f, 600f));
            // 바깥 링: 안쪽 룰렛 뒤에 겹친 큰 룰렛. 바깥 링이 있으면 안쪽 룰렛이 줄어들어 바깥 띠만 보인다(같은 포인터 아래).
            RectTransform playerOuterRingRoot = AddRect("OuterRingWheel", playerRouletteContainer, Vector2.zero, new Vector2(580f, 580f));
            RouletteController playerOuterRoulette = playerOuterRingRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController playerOuterSpin = playerOuterRingRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildOuterRingWheel(playerOuterRingRoot, playerOuterRoulette, playerOuterSpin, legacyFont, pointerSprite, 580f);
            RectTransform rouletteRoot = AddRect("ExistingPlayerRoulette", playerRouletteContainer, Vector2.zero, new Vector2(580f, 580f));
            RouletteController roulette = rouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController spin = rouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildRoulette(rouletteRoot, roulette, spin, legacyFont, frameSprite, pointerSprite, centerCapSprite, rouletteTick, rouletteStop, false);
            RouletteClickArea playerClickArea = AddClickArea(rouletteRoot, roulette, 470f);

            // SPIN 버튼을 룰렛 중앙에 둔다. 회전하는 Wheel이 아니라 고정된 루트의 자식이라 함께 돌지 않는다.
            // 버튼 둘레의 원형 게이지가 누르고 있는 동안의 회전 강도를 보여준다.
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform spinCenter = AddRect("SpinCenter", rouletteRoot, Vector2.zero, new Vector2(150f, 150f));
            UnityEngine.UI.Image powerTrack = AddImage("PowerRingTrack", spinCenter, new Color32(40, 34, 52, 255));
            powerTrack.sprite = knobSprite;
            powerTrack.rectTransform.sizeDelta = new Vector2(150f, 150f);
            UnityEngine.UI.Image powerFill = AddImage("PowerRingFill", spinCenter, Accent);
            powerFill.sprite = knobSprite;
            powerFill.rectTransform.sizeDelta = new Vector2(150f, 150f);
            powerFill.type = UnityEngine.UI.Image.Type.Filled;
            powerFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            powerFill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            powerFill.fillClockwise = true;
            powerFill.fillAmount = 0.08f;
            UnityEngine.UI.Button throwButton = AddButton("SpinButton", spinCenter, Vector2.zero, new Vector2(124f, 124f), Accent, font, "길게 눌러\nSPIN", 18, out TMP_Text throwLabel);
            throwLabel.color = Background;
            UnityEngine.UI.Image throwImage = throwButton.GetComponent<UnityEngine.UI.Image>();
            if (knobSprite != null)
            {
                throwImage.sprite = knobSprite;
                throwImage.preserveAspect = true;
            }
            spinCenter.SetAsLastSibling();

            AddRect("EffectRoot", playerRouletteContainer, Vector2.zero, new Vector2(620f, 620f));
            // 룰렛 아래 안내: 특수 칸이 있으면 효과 설명, 없으면 칸 배치 안내(두 줄까지).
            TMP_Text chainPreviewText = AddText("ChainPreview", playerPanel, "칸은 하나씩 발동", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(0f, -347f), new Vector2(600f, 46f));

            // ── 가운데 "테이블": 판돈이 쌓이는 곳. 결과 → 계산식 → 칩 더미 → 판돈 → 보험 → 확률 → 앤티 → CASH OUT → 딜러 한마디.
            RectTransform centerPanel = AddRect("CenterPanel", mainGameArea, new Vector2(0f, 26f), new Vector2(360f, 760f));
            // 룰렛 칸 기호 범례(그레이박스 아이콘): 칸에는 기호 + 숫자만, 긴 이름은 착지 이름표·설명에서 보여 준다.
            AddText("SlotLegend", centerPanel, "▲레이즈 ×배율 ◆보험 ●배당 몫=하우스 몫", font, 13, Muted, TextAlignmentOptions.Center, new Vector2(0f, 356f), new Vector2(360f, 26f));
            // 테이블은 숫자 위주로: 한 줄 결과 → 판돈 흐름(이번 턴에 판돈이 어떻게 커졌는지) → 칩 더미·큰 판돈. 긴 계산식은 전체 기록에만 남긴다.
            TMP_Text instructionText = AddText("GuideText", centerPanel, "", font, 14, Ink, TextAlignmentOptions.Center, new Vector2(0f, -300f), new Vector2(360f, 60f));
            TMP_Text powerPreviewText = AddText("PowerPreview", centerPanel, "", font, 13, Muted, TextAlignmentOptions.Center, new Vector2(0f, 290f), new Vector2(440f, 22f));
            powerPreviewText.gameObject.SetActive(false);
            TMP_Text resultText = AddText("MainInstruction", centerPanel, "코인플립으로 선공을 정합니다", font, 24, Ink, TextAlignmentOptions.Center, new Vector2(0f, 306f), new Vector2(360f, 36f));
            resultText.enableAutoSizing = true;
            resultText.fontSizeMin = 16f;
            resultText.fontSizeMax = 24f;
            TMP_Text potTrailText = AddText("PotTrail", centerPanel, "", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 240f), new Vector2(360f, 84f));
            potTrailText.richText = true;
            potTrailText.enableAutoSizing = true;
            potTrailText.fontSizeMin = 15f;
            potTrailText.fontSizeMax = 22f;
            TMP_Text calculationText = AddText("Description", centerPanel, "", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(0f, 206f), new Vector2(450f, 50f));
            calculationText.gameObject.SetActive(false);
            UnityEngine.UI.Image[] potChips = BuildPotChipStack(centerPanel, new Vector2(0f, 150f));
            TMP_Text potText = AddText("PotValue", centerPanel, "판돈 0", font, 72, Accent, TextAlignmentOptions.Center, new Vector2(0f, 62f), new Vector2(360f, 84f));
            potText.enableAutoSizing = true;
            potText.fontSizeMin = 40f;
            potText.fontSizeMax = 72f;
            TMP_Text insuranceText = AddBadge("InsuranceStatus", centerPanel, "내 보험 0", Teal, font, new Vector2(0f, 6f), 340f);
            TMP_Text riskSummaryText = AddText("RiskSummary", centerPanel, "다음 SPIN 하우스 몫 확률 12.5%", font, 15, Gold, TextAlignmentOptions.Center, new Vector2(0f, -42f), new Vector2(360f, 44f));
            RectTransform betControls = AddRect("BetControls", centerPanel, new Vector2(0f, -100f), new Vector2(340f, 52f));
            UnityEngine.UI.Button anteDownButton = AddButton("AnteDown", betControls, new Vector2(-140f, 0f), new Vector2(52f, 48f), PanelLight, font, "−", 26, out _);
            TMP_Text anteText = AddText("AnteText", betControls, "앤티 1", font, 17, Ink, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(210f, 48f));
            UnityEngine.UI.Button anteUpButton = AddButton("AnteUp", betControls, new Vector2(140f, 0f), new Vector2(52f, 48f), PanelLight, font, "+", 26, out _);
            UnityEngine.UI.Button cashOutButton = AddButton("CashOutButton", centerPanel, new Vector2(0f, -186f), new Vector2(340f, 96f), Accent, font, "CASH OUT", 28, out TMP_Text cashOutLabel);
            cashOutLabel.color = Background;

            RectTransform dealerPanel = AddRect("DealerRoulettePanel", mainGameArea, new Vector2(600f, 26f), new Vector2(640f, 760f));
            TMP_Text dealerHeaderText = AddText("Header", dealerPanel, "상대 룰렛 · 토끼 딜러", font, 18, Muted, TextAlignmentOptions.Center, new Vector2(0f, 352f), new Vector2(580f, 36f));
            dealerHeaderText.gameObject.SetActive(false);
            RectTransform dealerIntent = AddFramedPanel("DealerIntent", dealerPanel, new Vector2(0f, 336f), new Vector2(580f, 50f), PanelLight, Red, out _, out _);
            AddText("NextText", dealerIntent, "성향", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(-245f, 0f), new Vector2(70f, 24f));
            TMP_Text enemyNextIntentText = AddText("ValueText", dealerIntent, "판돈 5+에서 CASH OUT", font, 17, Ink, TextAlignmentOptions.Left, new Vector2(35f, 0f), new Vector2(470f, 50f));
            RectTransform dealerRouletteContainer = AddRect("RouletteContainer", dealerPanel, new Vector2(0f, -10f), new Vector2(560f, 560f));
            UnityEngine.UI.Image dealerGlow = AddGlow("ActiveGlow", dealerRouletteContainer, new Color32(226, 170, 64, 70), new Vector2(540f, 540f));
            RectTransform dealerOuterRingRoot = AddRect("OuterRingWheel", dealerRouletteContainer, Vector2.zero, new Vector2(530f, 530f));
            RouletteController dealerOuterRoulette = dealerOuterRingRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController dealerOuterSpin = dealerOuterRingRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildOuterRingWheel(dealerOuterRingRoot, dealerOuterRoulette, dealerOuterSpin, legacyFont, pointerSprite, 530f);
            RectTransform enemyRouletteRoot = AddRect("ExistingDealerRoulette", dealerRouletteContainer, Vector2.zero, new Vector2(530f, 530f));
            RouletteController enemyRoulette = enemyRouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController enemySpin = enemyRouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildRoulette(enemyRouletteRoot, enemyRoulette, enemySpin, legacyFont, frameSprite, pointerSprite, centerCapSprite, rouletteTick, rouletteStop, true);
            RouletteClickArea dealerClickArea = AddClickArea(enemyRouletteRoot, enemyRoulette, 430f);
            AddRect("EffectRoot", dealerRouletteContainer, Vector2.zero, new Vector2(560f, 560f));

            // ── 사건 띠: 방금 일어난 일 3개를 한 줄로(전체 기록 대신).
            RectTransform eventStrip = AddRect("EventStrip", mainGameArea, new Vector2(0f, -385f), new Vector2(1840f, 44f));
            AddText("Icon", eventStrip, "LOG", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(-880f, 0f), new Vector2(60f, 30f));
            TMP_Text combatLogText = AddText("CombatLog", eventStrip, "", font, 15, Muted, TextAlignmentOptions.Left, new Vector2(-30f, 0f), new Vector2(1620f, 36f));
            // 사건 띠를 누르면 전체 전투 기록이 펼쳐진다(투명 버튼, 속도 버튼보다 아래).
            UnityEngine.UI.Image logButtonImage = AddImage("LogButton", eventStrip, new Color(0f, 0f, 0f, 0f));
            logButtonImage.rectTransform.sizeDelta = new Vector2(1700f, 44f);
            logButtonImage.rectTransform.anchoredPosition = new Vector2(-60f, 0f);
            logButtonImage.raycastTarget = true;
            UnityEngine.UI.Button logButton = logButtonImage.gameObject.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Button tempoButton = AddButton("TempoButton", eventStrip, new Vector2(860f, 0f), new Vector2(110f, 34f), PanelLight, font, "속도 ×1", 15, out TMP_Text tempoLabel);
            combatLogText.textWrappingMode = TextWrappingModes.NoWrap;
            combatLogText.overflowMode = TextOverflowModes.Ellipsis;

            // HIJACK 안내 띠: 룰렛 위에서 직접 칸을 고르는 동안 테이블 위쪽을 덮는다.
            RectTransform hijackBar = AddFramedPanel("HijackBar", centerPanel, new Vector2(0f, 250f), new Vector2(380f, 200f), new Color32(30, 25, 39, 255), Accent, out _, out _);
            TMP_Text hijackBarTitle = AddText("Title", hijackBar, "HOUSE RULE CLEAR · HIJACK", font, 20, Accent, TextAlignmentOptions.Center, new Vector2(0f, 72f), new Vector2(360f, 32f));
            TMP_Text hijackBarText = AddText("Instruction", hijackBar, "딜러 룰렛에서 빼앗을 칸을 클릭하세요", font, 16, Ink, TextAlignmentOptions.Center, new Vector2(0f, -12f), new Vector2(360f, 140f));

            // NUDGE 선택 띠: 하우스 몫에 걸렸을 때 테이블 위쪽을 덮는다(룰렛을 옆 칸으로 밀기 / 그대로).
            RectTransform nudgeBar = AddFramedPanel("NudgeBar", centerPanel, new Vector2(0f, 250f), new Vector2(380f, 200f), new Color32(48, 30, 32, 255), Danger, out _, out _);
            TMP_Text nudgeBarTitle = AddText("Title", nudgeBar, "하우스 몫!  NUDGE?", font, 24, Danger, TextAlignmentOptions.Center, new Vector2(0f, 70f), new Vector2(360f, 34f));
            TMP_Text nudgeBarText = AddText("Text", nudgeBar, "옆 칸으로 밀면 그 칸이 대신 발동합니다", font, 14, Ink, TextAlignmentOptions.Center, new Vector2(0f, 36f), new Vector2(360f, 26f));
            UnityEngine.UI.Button nudgeLeftButton = AddButton("NudgeLeft", nudgeBar, new Vector2(-122f, -20f), new Vector2(116f, 64f), PanelLight, font, "◀ 레이즈 +2", 14, out TMP_Text nudgeLeftLabel);
            UnityEngine.UI.Button nudgeStayButton = AddButton("NudgeStay", nudgeBar, new Vector2(0f, -20f), new Vector2(116f, 64f), new Color32(70, 40, 46, 255), font, "그대로\n(아껴 두기)", 13, out _);
            UnityEngine.UI.Button nudgeRightButton = AddButton("NudgeRight", nudgeBar, new Vector2(122f, -20f), new Vector2(116f, 64f), PanelLight, font, "보험 +2 ▶", 14, out TMP_Text nudgeRightLabel);
            TMP_Text nudgeCountText = AddText("Count", nudgeBar, "이번 전투 NUDGE 1회 남음", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(0f, -76f), new Vector2(360f, 22f));

            // 연출 층: CASH OUT 칩·피해 숫자가 패널 위로 날아다닌다. 큰 순간 배너는 그 위.
            RectTransform fxLayer = AddRect("FxLayer", canvasObject.transform, Vector2.zero, new Vector2(1920f, 1080f));
            RectTransform momentBanner = AddFramedPanel("MomentBanner", canvasObject.transform, new Vector2(0f, 60f), new Vector2(1920f, 150f), new Color32(30, 25, 39, 240), Gold, out UnityEngine.UI.Image momentBannerBackground, out _);
            TMP_Text momentBannerTitle = AddText("Title", momentBanner, "하우스 룰 달성!", font, 54, Gold, TextAlignmentOptions.Center, new Vector2(0f, 18f), new Vector2(1800f, 70f));
            TMP_Text momentBannerSubtitle = AddText("Subtitle", momentBanner, "HIJACK 기회 +1", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, -40f), new Vector2(1800f, 34f));
            foreach (UnityEngine.UI.Graphic graphic in momentBanner.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.raycastTarget = false;

            GameObject battleObject = new GameObject("DealerBattle", typeof(RectTransform));
            battleObject.transform.SetParent(canvasObject.transform, false);
            DealerBattleController battle = battleObject.AddComponent<DealerBattleController>();
            BattlePresentationUI presentation = battleObject.AddComponent<BattlePresentationUI>();
            HoldToSpinInput holdInput = throwButton.gameObject.AddComponent<HoldToSpinInput>();
            ConfigurePresentation(presentation, preparePhase, spinPhase, resolvePhase, dealerPhase, playerGlow, dealerGlow, houseRuleFrame);
            HijackTransferPresenter hijackTransferPresenter = BuildHijackTransferEffect(canvasObject.transform, font);

            RectTransform houseRuleOverlay = BuildHouseRuleOverlay(canvasObject.transform, font, out UnityEngine.UI.Button houseRuleInfoCloseButton, out TMP_Text houseRuleDetailProgressText, out TMP_Text[] houseRuleDetailTexts);
            // 딜러 룰렛 아래 안내: 역탈취 경고 + 딜러 특수 칸 효과.
            TMP_Text dealerNoteText = AddText("DealerNote", dealerPanel, "", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(0f, -347f), new Vector2(600f, 46f));
            RectTransform hijackPanel = BuildHijackPanel(canvasObject.transform, font, out TMP_Text hijackInstruction, out TMP_Text hijackSourceTitle, out UnityEngine.UI.Button[] hijackSourceButtons, out TMP_Text[] hijackSourceLabels, out UnityEngine.UI.Button[] hijackDestinationButtons, out TMP_Text[] hijackDestinationLabels);
            RectTransform endPanel = BuildEndPanel(canvasObject.transform, font, out TMP_Text endTitle, out TMP_Text endBody, out UnityEngine.UI.Button endContinueButton, out TMP_Text endContinueLabel);
            RectTransform doorPanel = BuildDoorPanel(canvasObject.transform, font, out TMP_Text doorFloorText, out UnityEngine.UI.Button[] doorButtons, out TMP_Text[] doorTitles, out TMP_Text[] doorBodies, out TMP_Text exchangeText, out UnityEngine.UI.Button[] exchangeButtons);

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
            SetObject(identitySo, "exchangeText", exchangeText);
            SetObjectArray(identitySo.FindProperty("exchangeButtons"), exchangeButtons);
            SetObject(identitySo, "playerOuterRoulette", playerOuterRoulette);
            SetObject(identitySo, "playerOuterSpin", playerOuterSpin);
            SetObject(identitySo, "dealerOuterRoulette", dealerOuterRoulette);
            SetObject(identitySo, "dealerOuterSpin", dealerOuterSpin);
            // 착지 이름표: 포인터 바로 위, 멈춘 칸의 이름과 실제로 일어난 효과. 다음 SPIN 전까지 남는다.
            RectTransform playerTag = BuildLandingTag(playerPanel, new Vector2(0f, 340f), font, out TMP_Text playerTagTitle, out TMP_Text playerTagEffect, out UnityEngine.UI.Image playerTagBackground);
            RectTransform dealerTag = BuildLandingTag(dealerPanel, new Vector2(0f, 336f), font, out TMP_Text dealerTagTitle, out TMP_Text dealerTagEffect, out UnityEngine.UI.Image dealerTagBackground);
            SetObject(identitySo, "playerLandingTag", playerTag);
            SetObject(identitySo, "playerLandingTitle", playerTagTitle);
            SetObject(identitySo, "playerLandingEffect", playerTagEffect);
            SetObject(identitySo, "playerLandingBackground", playerTagBackground);
            SetObject(identitySo, "dealerLandingTag", dealerTag);
            SetObject(identitySo, "dealerLandingTitle", dealerTagTitle);
            SetObject(identitySo, "dealerLandingEffect", dealerTagEffect);
            SetObject(identitySo, "dealerLandingBackground", dealerTagBackground);
            RectTransform relicOverlay = BuildRelicOverlay(canvasObject.transform, font, out TMP_Text relicOverlayTitle, out TMP_Text relicOverlayBody, out UnityEngine.UI.Button relicOverlayClose);
            RectTransform titlePanel = BuildTitlePanel(canvasObject.transform, font, out UnityEngine.UI.Button titleStartButton, out UnityEngine.UI.Button titleResetButton, out TMP_Text titleMetaText);
            SetObject(identitySo, "relicOverlayTitle", relicOverlayTitle);
            SetObject(identitySo, "logButton", logButton);
            SetObject(identitySo, "titlePanel", titlePanel.gameObject);
            SetObject(identitySo, "titleStartButton", titleStartButton);
            SetObject(identitySo, "titleResetButton", titleResetButton);
            SetObject(identitySo, "titleMetaText", titleMetaText);
            ShopView shop = BuildShopPanel(canvasObject.transform, font);
            SetObject(identitySo, "shopPanel", shop.Panel.gameObject);
            SetObject(identitySo, "shopStatusText", shop.Status);
            SetObject(identitySo, "shopMessageText", shop.Message);
            SetObjectArray(identitySo.FindProperty("shopServiceButtons"), shop.ServiceButtons);
            SetObjectArray(identitySo.FindProperty("shopServiceLabels"), shop.ServiceLabels);
            SetObjectArray(identitySo.FindProperty("shopSlotButtons"), shop.SlotButtons);
            SetObjectArray(identitySo.FindProperty("shopSlotLabels"), shop.SlotLabels);
            SetObjectArray(identitySo.FindProperty("slotReelTexts"), shop.Reels);
            SetObject(identitySo, "slotResultText", shop.SlotResult);
            SetObjectArray(identitySo.FindProperty("slotBetButtons"), shop.BetButtons);
            SetObject(identitySo, "slotPullButton", shop.PullButton);
            SetObject(identitySo, "slotPayTableText", shop.PayTable);
            SetObject(identitySo, "shopLeaveButton", shop.LeaveButton);
            SetObject(identitySo, "relicStripButton", relicStripButton);
            SetObject(identitySo, "potTrailText", potTrailText);
            SetObjectArray(identitySo.FindProperty("relicIcons"), relicIcons);
            SetObjectArray(identitySo.FindProperty("relicIconLabels"), relicIconLabels);
            SetObjectArray(identitySo.FindProperty("relicIconImages"), relicIconImages);
            RectTransform relicTooltip = AddFramedPanel("RelicTooltip", canvasObject.transform, new Vector2(-560f, 300f), new Vector2(460f, 96f), new Color32(30, 25, 39, 250), Gold, out _, out _);
            TMP_Text relicTooltipText = AddText("Text", relicTooltip, "", font, 16, Ink, TextAlignmentOptions.Left, Vector2.zero, new Vector2(436f, 84f));
            foreach (UnityEngine.UI.Graphic graphic in relicTooltip.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.raycastTarget = false;
            relicTooltip.gameObject.SetActive(false);
            SetObject(identitySo, "relicTooltip", relicTooltip.gameObject);
            SetObject(identitySo, "relicTooltipText", relicTooltipText);
            SetObject(identitySo, "relicStripText", relicStripText);
            SetObject(identitySo, "relicOverlay", relicOverlay.gameObject);
            SetObject(identitySo, "relicOverlayBody", relicOverlayBody);
            SetObject(identitySo, "relicOverlayClose", relicOverlayClose);
            SetObject(identitySo, "nudgeBar", nudgeBar.gameObject);
            SetObject(identitySo, "nudgeBarText", nudgeBarText);
            SetObject(identitySo, "nudgeLeftButton", nudgeLeftButton);
            SetObject(identitySo, "nudgeLeftLabel", nudgeLeftLabel);
            SetObject(identitySo, "nudgeStayButton", nudgeStayButton);
            SetObject(identitySo, "nudgeRightButton", nudgeRightButton);
            SetObject(identitySo, "nudgeRightLabel", nudgeRightLabel);
            SetObject(identitySo, "nudgeCountText", nudgeCountText);
            SetObject(identitySo, "tempoButton", tempoButton);
            SetObject(identitySo, "tempoLabel", tempoLabel);
            SetObject(identitySo, "playerWheelClick", playerClickArea);
            SetObject(identitySo, "dealerWheelClick", dealerClickArea);
            SetObject(identitySo, "hijackBar", hijackBar.gameObject);
            SetObject(identitySo, "hijackBarTitle", hijackBarTitle);
            SetObject(identitySo, "hijackBarText", hijackBarText);
            SetObject(identitySo, "fxLayer", fxLayer);
            SetObject(identitySo, "momentBanner", momentBanner);
            SetObject(identitySo, "momentBannerTitle", momentBannerTitle);
            SetObject(identitySo, "momentBannerSubtitle", momentBannerSubtitle);
            SetObject(identitySo, "momentBannerBackground", momentBannerBackground);
            identitySo.ApplyModifiedPropertiesWithoutUndo();
            hijackBar.gameObject.SetActive(false);
            nudgeBar.gameObject.SetActive(false);
            relicOverlay.gameObject.SetActive(false);
            shop.Panel.gameObject.SetActive(false);
            titlePanel.SetAsLastSibling();
            momentBanner.gameObject.SetActive(false);
            playerClickArea.gameObject.SetActive(false);
            dealerClickArea.gameObject.SetActive(false);
            playerTag.gameObject.SetActive(false);
            dealerTag.gameObject.SetActive(false);
            doorPanel.gameObject.SetActive(false);
            playerOuterRingRoot.gameObject.SetActive(false);
            dealerOuterRingRoot.gameObject.SetActive(false);

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

        private static void BuildRoulette(RectTransform root, RouletteController controller, RouletteSpinController spin, Font legacyFont, Sprite frameSprite, Sprite pointerSprite, Sprite centerCapSprite, AudioClip tickSound, AudioClip stopSound, bool enemy)
        {
            float wheelSize = enemy ? 430f : 470f;
            RectTransform wheel = AddRect("Wheel", root, Vector2.zero, new Vector2(wheelSize, wheelSize));
            RectTransform segments = AddRect("DynamicSegments", wheel, Vector2.zero, new Vector2(wheelSize, wheelSize));
            RoulettePixelWheelRenderer renderer = segments.gameObject.AddComponent<RoulettePixelWheelRenderer>();
            renderer.raycastTarget = false;
            // 와이어프레임: 룰렛 뒤에 단순한 테두리 원판을 깔고(장식 틀 없음), 12시에 강조색 마름모 포인터 하나.
            Sprite disc = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            UnityEngine.UI.Image rim = AddSpriteImage("FixedFrame", root, disc, new Vector2(wheelSize + 28f, wheelSize + 28f));
            rim.color = PanelLight;
            rim.transform.SetAsFirstSibling();
            UnityEngine.UI.Image centerCap = AddSpriteImage("CenterCap", wheel, disc, new Vector2(70f, 70f));
            centerCap.color = PanelInset;
            UnityEngine.UI.Image pointer = AddImage("Pointer", root, Accent);
            pointer.rectTransform.sizeDelta = new Vector2(30f, 30f);
            pointer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            pointer.rectTransform.anchoredPosition = new Vector2(0f, wheelSize * 0.5f + 10f);

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("wheel").objectReferenceValue = wheel;
            controllerSo.FindProperty("dynamicSegmentRoot").objectReferenceValue = segments;
            controllerSo.FindProperty("pixelWheelRenderer").objectReferenceValue = renderer;
            controllerSo.FindProperty("labelFont").objectReferenceValue = legacyFont;
            controllerSo.FindProperty("labelFontSize").intValue = enemy ? 26 : 30;
            controllerSo.FindProperty("iconRadiusRatio").floatValue = 0.5f;
            controllerSo.FindProperty("labelRadiusRatio").floatValue = 0.7f;
            controllerSo.FindProperty("labelSize").vector2Value = enemy ? new Vector2(96f, 36f) : new Vector2(104f, 40f);
            controllerSo.FindProperty("keepLabelsUpright").boolValue = true;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            // 회전음은 칸 경계마다 틱을 내므로 회전 속도·칸 수와 항상 맞는다. 딜러 룰렛은 조금 작게.
            AudioSource spinAudioSource = root.gameObject.AddComponent<AudioSource>();
            spinAudioSource.playOnAwake = false;
            spinAudioSource.loop = false;
            spinAudioSource.spatialBlend = 0f;
            spinAudioSource.volume = enemy ? 0.55f : 0.8f;

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
            spinSo.FindProperty("tickClip").objectReferenceValue = tickSound;
            spinSo.FindProperty("stopClip").objectReferenceValue = stopSound;
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
        /// <summary>상점 화면에서 컨트롤러에 넘길 참조 묶음.</summary>
        private sealed class ShopView
        {
            public RectTransform Panel;
            public TMP_Text Status, Message, SlotResult, PayTable;
            public UnityEngine.UI.Button[] ServiceButtons, SlotButtons, BetButtons;
            public TMP_Text[] ServiceLabels, SlotLabels, Reels;
            public UnityEngine.UI.Button PullButton, LeaveButton;
        }

        /// <summary>
        /// 상점층(ADR 0009): 왼쪽 서비스(위치 바꾸기·칸 강화·HIJACK 되돌리기·유물·칩 사기), 오른쪽 슬롯머신, 아래 내 룰렛 칸 고르기, 다음 층 버튼.
        /// 문구·가격·활성화는 컨트롤러가 채운다.
        /// </summary>
        private static ShopView BuildShopPanel(Transform parent, TMP_FontAsset font)
        {
            ShopView view = new ShopView();
            view.Panel = AddPanel("ShopPanel", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(18, 12, 24, 255));
            view.Panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            AddText("Title", view.Panel, "캐셔 라운지 · 상점", font, 40, Gold, TextAlignmentOptions.Center, new Vector2(0f, 470f), new Vector2(1400f, 56f));
            view.Status = AddText("Status", view.Panel, "현금 0 · 칩 0", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 420f), new Vector2(1600f, 34f));

            // 왼쪽: 서비스
            RectTransform services = AddFramedPanel("Services", view.Panel, new Vector2(-470f, 110f), new Vector2(880f, 560f), Panel, DarkGold, out _, out _);
            AddText("Header", services, "룰렛 손보기 · 새 칸은 팔지 않습니다(새 칸은 HIJACK으로)", font, 18, Gold, TextAlignmentOptions.Center, new Vector2(0f, 248f), new Vector2(840f, 30f));
            view.ServiceButtons = new UnityEngine.UI.Button[5];
            view.ServiceLabels = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                view.ServiceButtons[i] = AddButton($"Service{i + 1}", services, new Vector2(0f, 175f - i * 98f), new Vector2(820f, 86f), PanelLight, font, "", 17, out view.ServiceLabels[i]);
                view.ServiceLabels[i].alignment = TextAlignmentOptions.Left;
                view.ServiceLabels[i].rectTransform.sizeDelta = new Vector2(790f, 80f);
            }

            // 오른쪽: 슬롯머신
            RectTransform machine = AddFramedPanel("SlotMachine", view.Panel, new Vector2(480f, 110f), new Vector2(860f, 560f), new Color32(40, 22, 30, 255), Gold, out _, out _);
            AddText("Header", machine, "슬롯머신 · 현금을 걸고 당기기", font, 22, Gold, TextAlignmentOptions.Center, new Vector2(0f, 245f), new Vector2(820f, 34f));
            view.Reels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform reel = AddFramedPanel($"Reel{i + 1}", machine, new Vector2((i - 1) * 210f, 115f), new Vector2(190f, 170f), new Color32(245, 240, 226, 255), DarkGold, out _, out _);
                view.Reels[i] = AddText("Symbol", reel, "7", font, 80, Background, TextAlignmentOptions.Center, Vector2.zero, new Vector2(180f, 160f));
            }

            view.SlotResult = AddText("Result", machine, "얼마를 걸까요?", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(820f, 40f));
            view.BetButtons = new UnityEngine.UI.Button[3];
            string[] bets = { "1 걸기", "5 걸기", "10 걸기" };
            for (int i = 0; i < 3; i++)
            {
                view.BetButtons[i] = AddButton($"Bet{i + 1}", machine, new Vector2((i - 1) * 180f, -68f), new Vector2(160f, 56f), PanelLight, font, bets[i], 18, out _);
            }

            view.PullButton = AddButton("Pull", machine, new Vector2(0f, -145f), new Vector2(320f, 70f), Red, font, "당기기", 26, out TMP_Text pullLabel);
            pullLabel.color = Ink;
            view.PayTable = AddText("PayTable", machine, "", font, 14, Muted, TextAlignmentOptions.Center, new Vector2(0f, -232f), new Vector2(820f, 70f));

            // 아래: 내 룰렛 칸 고르기
            view.Message = AddText("Message", view.Panel, "서비스를 고르세요", font, 20, Ink, TextAlignmentOptions.Center, new Vector2(0f, -210f), new Vector2(1700f, 34f));
            view.SlotButtons = new UnityEngine.UI.Button[8];
            view.SlotLabels = new TMP_Text[8];
            for (int i = 0; i < 8; i++)
            {
                view.SlotButtons[i] = AddButton($"MySlot{i + 1}", view.Panel, new Vector2(-787.5f + i * 225f, -300f), new Vector2(210f, 100f), new Color32(38, 91, 96, 255), font, $"{i + 1}", 17, out view.SlotLabels[i]);
            }

            view.LeaveButton = AddButton("Leave", view.Panel, new Vector2(720f, -440f), new Vector2(380f, 70f), Accent, font, "다음 층으로 (2회차)", 22, out TMP_Text leaveLabel);
            leaveLabel.color = Background;
            AddText("Hint", view.Panel, "칸 번호는 룰렛을 시계 방향으로 돈 순서이고, 8번 다음은 다시 1번입니다. 같은 칸을 모아 두면 SPIN 강도로 그 구역을 노리기 쉽습니다.", font, 15, Muted, TextAlignmentOptions.Left, new Vector2(-300f, -440f), new Vector2(1200f, 30f));
            return view;
        }

        /// <summary>타이틀: 게임 이름·한 줄 소개·계약하기(시작)·해금 상태·해금 초기화. 게임을 켜면 가장 먼저 보인다.</summary>
        private static RectTransform BuildTitlePanel(Transform parent, TMP_FontAsset font, out UnityEngine.UI.Button startButton, out UnityEngine.UI.Button resetButton, out TMP_Text metaText)
        {
            RectTransform panel = AddPanel("TitlePanel", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(14, 10, 20, 255));
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            AddText("Logo", panel, "HIJACKPOT", font, 120, Accent, TextAlignmentOptions.Center, new Vector2(0f, 190f), new Vector2(1600f, 150f));
            AddText("Subtitle", panel, "Jack the Rules", font, 40, Ink, TextAlignmentOptions.Center, new Vector2(0f, 90f), new Vector2(1600f, 56f));
            AddText("Tagline", panel, "행운을 빼앗는 마법 카지노에서, 룰렛의 규칙을 훔쳐 탈출하라.", font, 24, Muted, TextAlignmentOptions.Center, new Vector2(0f, 30f), new Vector2(1600f, 40f));
            startButton = AddButton("StartButton", panel, new Vector2(0f, -90f), new Vector2(360f, 84f), Accent, font, "계약하기", 30, out TMP_Text startLabel);
            startLabel.color = Background;
            metaText = AddText("Meta", panel, "", font, 18, Muted, TextAlignmentOptions.Center, new Vector2(0f, -180f), new Vector2(1400f, 30f));
            resetButton = AddButton("ResetMetaButton", panel, new Vector2(0f, -232f), new Vector2(260f, 44f), PanelLight, font, "튜토리얼 다시 하기", 16, out _);
            AddText("Notice", panel, "실제 돈을 쓰지 않는 게임입니다. 칩은 손님의 행운이 굳은 것입니다.", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(0f, -470f), new Vector2(1600f, 26f));
            return panel;
        }

        /// <summary>유물 설명 펼침 패널: 가진 유물의 키워드·이름·효과와 NUDGE 규칙. 문구는 컨트롤러가 채운다.</summary>
        private static RectTransform BuildRelicOverlay(Transform parent, TMP_FontAsset font, out TMP_Text title, out TMP_Text body, out UnityEngine.UI.Button closeButton)
        {
            RectTransform overlay = AddPanel("RelicOverlay", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(10, 8, 14, 225));
            overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            RectTransform card = AddFramedPanel("Card", overlay, Vector2.zero, new Vector2(980f, 640f), new Color32(30, 25, 39, 255), Gold, out _, out _);
            title = AddText("Title", card, "유물 · 이전 탈출자들의 부정행위 도구", font, 28, Gold, TextAlignmentOptions.Center, new Vector2(0f, 270f), new Vector2(900f, 40f));
            body = AddText("Body", card, "", font, 18, Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, -10f), new Vector2(900f, 480f));
            closeButton = AddButton("CloseButton", card, new Vector2(0f, -280f), new Vector2(200f, 50f), PanelLight, font, "닫기", 18, out _);
            return overlay;
        }

        /// <summary>룰렛 위 클릭 영역(투명). HIJACK·JACKPOT 배치 때만 켜서 칸을 직접 고르게 한다. SPIN 버튼보다 아래에 둔다.</summary>
        private static RouletteClickArea AddClickArea(RectTransform rouletteRoot, RouletteController controller, float size)
        {
            UnityEngine.UI.Image area = AddImage("ClickArea", rouletteRoot, new Color(0f, 0f, 0f, 0f));
            area.rectTransform.sizeDelta = new Vector2(size, size);
            area.raycastTarget = true;
            Transform spinCenter = rouletteRoot.Find("SpinCenter");
            if (spinCenter != null) area.transform.SetSiblingIndex(spinCenter.GetSiblingIndex());
            RouletteClickArea click = area.gameObject.AddComponent<RouletteClickArea>();
            SerializedObject so = new SerializedObject(click);
            so.FindProperty("roulette").objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();
            return click;
        }

        /// <summary>착지 이름표: 칸 이름(크게)과 효과 한 줄. 배경색은 컨트롤러가 칸 종류에 맞춰 바꾼다.</summary>
        private static RectTransform BuildLandingTag(Transform parent, Vector2 position, TMP_FontAsset font, out TMP_Text title, out TMP_Text effect, out UnityEngine.UI.Image background)
        {
            RectTransform tag = AddFramedPanel("LandingTag", parent, position, new Vector2(470f, 54f), PanelLight, Gold, out background, out _);
            title = AddText("Title", tag, "레이즈 +2", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 11f), new Vector2(450f, 28f));
            effect = AddText("Effect", tag, "판돈 1 → 3", font, 15, Gold, TextAlignmentOptions.Center, new Vector2(0f, -13f), new Vector2(450f, 22f));
            return tag;
        }

        /// <summary>
        /// 바깥 링 룰렛: 안쪽 룰렛 뒤에 겹치는 큰 원판. 안쪽 룰렛(84%로 줄어듦)에 가려지지 않은 테두리 띠에 칸 기호가 보이고,
        /// 맨 위 작은 포인터가 안쪽 포인터와 같은 12시를 가리킨다. 결과는 코어가 정하고 이 룰렛은 그 칸으로 돌아가 멈춘다(소리 없음).
        /// </summary>
        private static void BuildOuterRingWheel(RectTransform root, RouletteController controller, RouletteSpinController spin, Font legacyFont, Sprite pointerSprite, float size)
        {
            RectTransform wheel = AddRect("Wheel", root, Vector2.zero, new Vector2(size, size));
            RectTransform segments = AddRect("DynamicSegments", wheel, Vector2.zero, new Vector2(size, size));
            RoulettePixelWheelRenderer renderer = segments.gameObject.AddComponent<RoulettePixelWheelRenderer>();
            renderer.raycastTarget = false;
            // 포인터는 링 바깥 가장자리에 걸쳐 두어 멈춘 칸의 기호를 가리지 않는다.
            UnityEngine.UI.Image pointer = AddImage("OuterPointer", root, Accent);
            pointer.rectTransform.sizeDelta = new Vector2(24f, 24f);
            pointer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            pointer.rectTransform.anchoredPosition = new Vector2(0f, size * 0.5f + 12f);

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("wheel").objectReferenceValue = wheel;
            controllerSo.FindProperty("dynamicSegmentRoot").objectReferenceValue = segments;
            controllerSo.FindProperty("pixelWheelRenderer").objectReferenceValue = renderer;
            controllerSo.FindProperty("labelFont").objectReferenceValue = legacyFont;
            controllerSo.FindProperty("labelFontSize").intValue = 22;
            controllerSo.FindProperty("iconRadiusRatio").floatValue = 0.915f;
            controllerSo.FindProperty("labelRadiusRatio").floatValue = 0.915f;
            controllerSo.FindProperty("labelSize").vector2Value = new Vector2(72f, 30f);
            controllerSo.FindProperty("keepLabelsUpright").boolValue = true;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            // 안쪽(최소 1.8초)보다 늘 먼저 멈추도록 짧게. 같은 DurationScale을 받는다.
            SerializedObject spinSo = new SerializedObject(spin);
            spinSo.FindProperty("rouletteController").objectReferenceValue = controller;
            spinSo.FindProperty("wheel").objectReferenceValue = wheel;
            spinSo.FindProperty("minSpinDuration").floatValue = 1.1f;
            spinSo.FindProperty("maxSpinDuration").floatValue = 1.3f;
            spinSo.FindProperty("startSpeed").floatValue = 900f;
            spinSo.FindProperty("deceleration").floatValue = 700f;
            spinSo.FindProperty("minimumFullRotations").intValue = 1;
            spinSo.FindProperty("selectedHighlightDuration").floatValue = 0.9f;
            spinSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>층 사이 문 선택 패널: 딜러 카드 두 장(이름·성향·하우스 룰·JACKPOT)과 층 안내.</summary>
        private static RectTransform BuildDoorPanel(Transform parent, TMP_FontAsset font, out TMP_Text floorText, out UnityEngine.UI.Button[] doorButtons, out TMP_Text[] doorTitles, out TMP_Text[] doorBodies,
            out TMP_Text exchangeText, out UnityEngine.UI.Button[] exchangeButtons)
        {
            RectTransform overlay = AddPanel("DoorPanel", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(10, 8, 14, 235));
            overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            AddText("Title", overlay, "다음 테이블", font, 40, Gold, TextAlignmentOptions.Center, new Vector2(0f, 380f), new Vector2(1200f, 60f));
            floorText = AddText("FloorText", overlay, "2층", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 315f), new Vector2(1400f, 64f));
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

            // 환전 창구: 딴 칩 중 현금으로 환전된 몫을 다시 칩(목숨)으로 바꾼다(ADR 0008). 현금이 없으면 컨트롤러가 숨긴다.
            RectTransform window = AddFramedPanel("ExchangeWindow", overlay, new Vector2(0f, -380f), new Vector2(1220f, 92f), new Color32(30, 25, 39, 255), Gold, out _, out _);
            exchangeText = AddText("Text", window, "환전 창구 · 현금 0", font, 19, Ink, TextAlignmentOptions.Left, new Vector2(-255f, 0f), new Vector2(680f, 80f));
            exchangeButtons = new UnityEngine.UI.Button[3];
            string[] labels = { "칩 +1", "칩 +5", "전부 환전" };
            for (int i = 0; i < exchangeButtons.Length; i++)
            {
                exchangeButtons[i] = AddButton($"Buy{i + 1}", window, new Vector2(200f + i * 160f, 0f), new Vector2(148f, 60f), Gold, font, labels[i], 18, out TMP_Text label);
                label.color = Background;
            }

            return overlay;
        }

        private static RectTransform BuildEndPanel(Transform parent, TMP_FontAsset font, out TMP_Text title, out TMP_Text body, out UnityEngine.UI.Button continueButton, out TMP_Text continueLabel)
        {
            RectTransform panel = AddFramedPanel("EndPanel", parent, Vector2.zero, new Vector2(900f, 480f), new Color32(35, 29, 45, 252), Gold, out _, out _);
            title = AddText("Title", panel, "", font, 42, Gold, TextAlignmentOptions.Center, new Vector2(0f, 130f), new Vector2(760f, 64f));
            body = AddText("Body", panel, "", font, 23, Ink, TextAlignmentOptions.Center, new Vector2(0f, 20f), new Vector2(760f, 120f));
            continueButton = AddButton("ContinueButton", panel, new Vector2(0f, -145f), new Vector2(360f, 76f), Accent, font, "계약 되감기", 24, out continueLabel);
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
            so.FindProperty("inactiveColor").colorValue = Muted;
            so.FindProperty("activeColor").colorValue = Accent;
            // 내 차례·딜러 차례 표시는 같은 강조색 테두리(누구 차례인지는 위치로 구분), 하우스 룰 카드는 달성 때만 강조색.
            so.FindProperty("playerGlowColor").colorValue = new Color32(226, 170, 64, 80);
            so.FindProperty("dealerGlowColor").colorValue = new Color32(226, 170, 64, 80);
            so.FindProperty("houseRuleNormalColor").colorValue = DarkGold;
            so.FindProperty("houseRuleHighlightColor").colorValue = Accent;
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

        /// <param name="mirrored">딜러 쪽: 이름은 오른쪽(바깥), 칩 수는 왼쪽(VS 쪽), 바는 오른쪽에서부터 채워져 VS 쪽부터 줄어든다.</param>
        private static UnityEngine.UI.Image AddChipsWidget(string name, Transform parent, Vector2 position, string combatantName, Color fillColor, TMP_FontAsset font, out TMP_Text chipsText, out TMP_Text nameText, float width = 440f, bool mirrored = false)
        {
            RectTransform root = AddRect(name, parent, position, new Vector2(width, 78f));
            float half = width * 0.5f - 90f;
            nameText = AddText("NameText", root, combatantName, font, 22, Ink, mirrored ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, new Vector2(mirrored ? half : -half, 22f), new Vector2(180f, 30f));
            chipsText = AddText("ChipsText", root, "칩 20", font, 22, Ink, mirrored ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, new Vector2(mirrored ? -half : half, 22f), new Vector2(180f, 30f));
            UnityEngine.UI.Image background = AddImage("BarBackground", root, new Color32(58, 59, 63, 255));
            background.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            background.rectTransform.sizeDelta = new Vector2(width - 20f, 22f);
            UnityEngine.UI.Image fill = AddImage("BarFill", background.transform, fillColor);
            ConfigureFill(fill, 1f);
            if (mirrored) fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Right;
            return fill;
        }

        private static TMP_Text AddBadge(string name, Transform parent, string label, Color accent, TMP_FontAsset font, Vector2 position, float width = 240f)
        {
            RectTransform badge = AddFramedPanel(name, parent, position, new Vector2(width, 44f), PanelLight, accent, out _, out _);
            TMP_Text text = AddText("Label", badge, label, font, 16, Ink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(width - 20f, 30f));
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 16f;
            return text;
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
