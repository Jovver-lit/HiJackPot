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
    public static class RabbitBattlePrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/RouletteSystem/Scenes/RabbitBattlePrototype.unity";
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
        private static readonly Color Muted = new Color32(148, 143, 162, 255);
        private static readonly Color Red = new Color32(202, 70, 75, 255);
        private static readonly Color Teal = new Color32(57, 180, 188, 255);

        [MenuItem("Tools/Roulette Like/Build Rabbit Battle Prototype")]
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
            scene.name = "RabbitBattlePrototype";

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

            RectTransform battleStage = AddStretchRect(
                "BattleStage", canvasObject.transform,
                new Vector2(0f, 0.69f), Vector2.one,
                new Vector2(40f, 8f), new Vector2(-40f, -30f));
            UnityEngine.UI.Image stagePlaceholder = AddImage("BackgroundPlaceholder", battleStage, StageBackground);
            Stretch(stagePlaceholder.rectTransform);
            AddFrame("Frame", battleStage, DarkGold);

            RectTransform playerArea = AddRect("PlayerArea", battleStage, new Vector2(-650f, 0f), new Vector2(500f, 300f));
            TMP_Text playerHpText;
            UnityEngine.UI.Image playerHpFill = AddHpWidget("PlayerHP", playerArea, new Vector2(0f, 112f), "손님", Teal, font, out playerHpText);
            RectTransform playerCharacter = AddRect("PlayerCharacter", playerArea, new Vector2(0f, -35f), new Vector2(230f, 230f));
            UnityEngine.UI.Image businessmanImage = AddSpriteImage("Sprite", playerCharacter, businessmanIdleFrames[0], new Vector2(220f, 220f));
            businessmanImage.raycastTarget = false;
            AddRect("EffectRoot", playerCharacter, Vector2.zero, new Vector2(230f, 230f));
            NaturalBlinkAnimator blinkAnimator = businessmanImage.gameObject.AddComponent<NaturalBlinkAnimator>();
            SerializedObject blinkSo = new SerializedObject(blinkAnimator);
            blinkSo.FindProperty("targetImage").objectReferenceValue = businessmanImage;
            SetObjectArray(blinkSo.FindProperty("idleFrames"), businessmanIdleFrames);
            SetObjectArray(blinkSo.FindProperty("blinkFrames"), businessmanBlinkFrames);
            blinkSo.ApplyModifiedPropertiesWithoutUndo();

            RectTransform centerStage = AddRect("CenterStage", battleStage, Vector2.zero, new Vector2(760f, 300f));
            RectTransform roundHeader = AddRect("RoundHeader", centerStage, new Vector2(0f, 112f), new Vector2(730f, 70f));
            TMP_Text roundText = AddText("RoundTitle", roundHeader, "TUTORIAL TABLE  ·  ROUND 1", font, 27, Gold, TextAlignmentOptions.Center, new Vector2(0f, 16f), new Vector2(720f, 36f));
            AddText("RoundSubtitle", roundHeader, "운이 없다면, 룰렛을 좋게 만든다.", font, 18, Ink, TextAlignmentOptions.Center, new Vector2(0f, -17f), new Vector2(720f, 28f));

            RectTransform phaseIndicator = AddRect("PhaseIndicator", centerStage, new Vector2(0f, 57f), new Vector2(690f, 34f));
            TMP_Text preparePhase = AddText("Prepare", phaseIndicator, "PREPARE", font, 15, Gold, TextAlignmentOptions.Center, new Vector2(-252f, 0f), new Vector2(116f, 28f));
            AddText("Arrow01", phaseIndicator, ">", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(-168f, 0f), new Vector2(32f, 28f));
            TMP_Text spinPhase = AddText("Spin", phaseIndicator, "SPIN", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(-85f, 0f), new Vector2(116f, 28f));
            AddText("Arrow02", phaseIndicator, ">", font, 15, Muted, TextAlignmentOptions.Center, Vector2.zero, new Vector2(32f, 28f));
            TMP_Text resolvePhase = AddText("Resolve", phaseIndicator, "RESOLVE", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(88f, 0f), new Vector2(116f, 28f));
            AddText("Arrow03", phaseIndicator, ">", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(174f, 0f), new Vector2(32f, 28f));
            TMP_Text dealerPhase = AddText("Dealer", phaseIndicator, "DEALER", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(260f, 0f), new Vector2(116f, 28f));

            RectTransform houseRuleCard = AddFramedPanel("HouseRuleCard", centerStage, new Vector2(0f, -64f), new Vector2(700f, 190f), Panel, DarkGold, out _, out UnityEngine.UI.Image houseRuleFrame);
            AddText("Title", houseRuleCard, "HOUSE RULE  ·  보험 처리", font, 21, Gold, TextAlignmentOptions.Center, new Vector2(0f, 65f), new Vector2(520f, 32f));
            UnityEngine.UI.Button houseRuleInfoButton = AddButton("InfoButton", houseRuleCard, new Vector2(292f, 65f), new Vector2(82f, 30f), PanelLight, font, "확인", 13, out _);
            AddText("Description", houseRuleCard, "공개된 공격을 방어로 전부 막으면", font, 16, Ink, TextAlignmentOptions.Center, new Vector2(0f, 29f), new Vector2(630f, 26f));
            AddText("RewardText", houseRuleCard, "토끼 룰렛 칸 1개를 영구 HIJACK", font, 17, Gold, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(630f, 26f));
            UnityEngine.UI.Image houseRuleTrack = AddImage("ProgressBar", houseRuleCard, new Color32(66, 59, 78, 255));
            houseRuleTrack.rectTransform.anchoredPosition = new Vector2(0f, -35f);
            houseRuleTrack.rectTransform.sizeDelta = new Vector2(560f, 12f);
            UnityEngine.UI.Image houseRuleFill = AddImage("ProgressFill", houseRuleTrack.transform, Gold);
            ConfigureFill(houseRuleFill, 0f);
            TMP_Text houseRuleProgressText = AddText("ProgressText", houseRuleCard, "0 / 1", font, 15, Ink, TextAlignmentOptions.Center, new Vector2(0f, -61f), new Vector2(620f, 24f));
            AddRect("EffectRoot", houseRuleCard, Vector2.zero, new Vector2(700f, 190f));

            RectTransform dealerArea = AddRect("DealerArea", battleStage, new Vector2(650f, 0f), new Vector2(500f, 300f));
            TMP_Text rabbitHpText;
            UnityEngine.UI.Image rabbitHpFill = AddHpWidget("DealerHP", dealerArea, new Vector2(0f, 112f), "토끼 딜러", Red, font, out rabbitHpText);
            RectTransform dealerCharacter = AddRect("DealerCharacter", dealerArea, new Vector2(112f, -42f), new Vector2(230f, 230f));
            UnityEngine.UI.Image rabbitImage = AddSpriteImage("Sprite", dealerCharacter, rabbitSprite, new Vector2(220f, 220f));
            rabbitImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rabbitImage.raycastTarget = false;
            AddRect("EffectRoot", dealerCharacter, Vector2.zero, new Vector2(230f, 230f));
            RectTransform openingSpeechBubble = AddFramedPanel("DialogueBubble", dealerArea, new Vector2(-105f, 15f), new Vector2(300f, 100f), new Color32(239, 235, 225, 255), DarkGold, out _, out _);
            AddText("Text", openingSpeechBubble, "손님, 공개된 공격을 전부 막으면\n제 룰렛 한 칸을 양도해 드립니다.", font, 14, Background, TextAlignmentOptions.Center, new Vector2(-5f, 0f), new Vector2(270f, 76f));
            UnityEngine.UI.Image speechTail = AddImage("TailPlaceholder", openingSpeechBubble, new Color32(239, 235, 225, 255));
            speechTail.rectTransform.anchoredPosition = new Vector2(136f, -37f);
            speechTail.rectTransform.sizeDelta = new Vector2(20f, 20f);
            speechTail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddRect("AnimationRoot", openingSpeechBubble, Vector2.zero, new Vector2(300f, 100f));

            RectTransform mainGameArea = AddStretchRect(
                "MainGameArea", canvasObject.transform,
                new Vector2(0f, 0.14f), new Vector2(1f, 0.69f),
                new Vector2(40f, 8f), new Vector2(-40f, -8f));

            RectTransform playerPanel = AddFramedPanel("PlayerRoulettePanel", mainGameArea, new Vector2(-630f, 0f), new Vector2(560f, 570f), Panel, DarkGold, out _, out _);
            AddText("Header", playerPanel, "◆  내 룰렛  ◆", font, 22, Teal, TextAlignmentOptions.Center, new Vector2(0f, 250f), new Vector2(500f, 38f));
            RectTransform playerRouletteContainer = AddRect("RouletteContainer", playerPanel, new Vector2(0f, -16f), new Vector2(470f, 470f));
            UnityEngine.UI.Image playerGlow = AddGlow("ActiveGlow", playerRouletteContainer, new Color32(57, 190, 198, 90), new Vector2(448f, 448f));
            RectTransform rouletteRoot = AddRect("ExistingPlayerRoulette", playerRouletteContainer, Vector2.zero, new Vector2(430f, 430f));
            RouletteController roulette = rouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController spin = rouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildRoulette(rouletteRoot, roulette, spin, legacyFont, frameSprite, pointerSprite, centerCapSprite, rouletteSpinSound, false);
            AddRect("EffectRoot", playerRouletteContainer, Vector2.zero, new Vector2(470f, 470f));
            TMP_Text chainPreviewText = AddText("ChainPreview", playerPanel, "연쇄 미리보기", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(0f, -258f), new Vector2(510f, 28f));

            RectTransform centerPanel = AddFramedPanel("CenterPanel", mainGameArea, Vector2.zero, new Vector2(600f, 570f), Panel, DarkGold, out _, out _);
            RectTransform currentInfo = AddFramedPanel("CurrentInfo", centerPanel, new Vector2(0f, 153f), new Vector2(548f, 220f), PanelInset, new Color32(74, 60, 82, 255), out _, out _);
            AddText("Title", currentInfo, "CURRENT INFO", font, 16, Gold, TextAlignmentOptions.Left, new Vector2(-184f, 82f), new Vector2(150f, 26f));
            TMP_Text resultText = AddText("MainInstruction", currentInfo, "첫 SPIN을 준비하세요", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 43f), new Vector2(510f, 36f));
            TMP_Text calculationText = AddText("Description", currentInfo, "인접한 같은 칸은 합산되고, 앞의 ×2는 연쇄를 배가합니다.", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(0f, 9f), new Vector2(510f, 46f));
            RectTransform statusRow = AddRect("StatusRow", currentInfo, new Vector2(0f, -58f), new Vector2(520f, 50f));
            TMP_Text guardText = AddBadge("DefenseStatus", statusRow, "방어 0", Teal, font, new Vector2(-132f, 0f));
            TMP_Text shardText = AddBadge("RuleStatus", statusRow, "규칙 조각 0", Gold, font, new Vector2(132f, 0f));

            RectTransform currentEvent = AddFramedPanel("CurrentEvent", centerPanel, new Vector2(0f, 14f), new Vector2(548f, 80f), PanelLight, new Color32(74, 60, 82, 255), out _, out _);
            AddText("Speaker", currentEvent, "토끼 딜러", font, 15, Gold, TextAlignmentOptions.Left, new Vector2(-205f, 0f), new Vector2(100f, 42f));
            TMP_Text dealerLineText = AddText("EventText", currentEvent, "하우스 룰이 공개되었습니다.", font, 15, Ink, TextAlignmentOptions.Left, new Vector2(55f, 0f), new Vector2(390f, 48f));

            RectTransform combatLog = AddFramedPanel("CombatLog", centerPanel, new Vector2(0f, -156f), new Vector2(548f, 238f), PanelInset, new Color32(74, 60, 82, 255), out _, out _);
            AddText("Title", combatLog, "COMBAT LOG", font, 16, Gold, TextAlignmentOptions.Left, new Vector2(-186f, 92f), new Vector2(150f, 26f));
            TMP_Text combatLogText = AddText("Entries", combatLog, "", font, 15, Muted, TextAlignmentOptions.TopLeft, new Vector2(0f, -18f), new Vector2(500f, 166f));
            combatLogText.lineSpacing = 8f;

            RectTransform dealerPanel = AddFramedPanel("DealerRoulettePanel", mainGameArea, new Vector2(630f, 0f), new Vector2(560f, 570f), Panel, DarkGold, out _, out _);
            AddText("Header", dealerPanel, "◆  상대 룰렛 · 토끼 딜러  ◆", font, 21, Red, TextAlignmentOptions.Center, new Vector2(0f, 250f), new Vector2(510f, 38f));
            RectTransform dealerIntent = AddFramedPanel("DealerIntent", dealerPanel, new Vector2(0f, 186f), new Vector2(470f, 76f), PanelLight, Red, out _, out _);
            AddText("IconPlaceholder", dealerIntent, "ATK", font, 17, Red, TextAlignmentOptions.Center, new Vector2(-170f, 0f), new Vector2(60f, 54f));
            AddText("NextText", dealerIntent, "NEXT", font, 13, Muted, TextAlignmentOptions.Center, new Vector2(-96f, 16f), new Vector2(90f, 22f));
            TMP_Text enemyNextIntentText = AddText("ValueText", dealerIntent, "공격 4", font, 21, Ink, TextAlignmentOptions.Left, new Vector2(66f, -5f), new Vector2(280f, 34f));
            RectTransform dealerRouletteContainer = AddRect("RouletteContainer", dealerPanel, new Vector2(0f, -66f), new Vector2(440f, 440f));
            UnityEngine.UI.Image dealerGlow = AddGlow("ActiveGlow", dealerRouletteContainer, new Color32(220, 72, 78, 90), new Vector2(418f, 418f));
            RectTransform enemyRouletteRoot = AddRect("ExistingDealerRoulette", dealerRouletteContainer, Vector2.zero, new Vector2(410f, 410f));
            RouletteController enemyRoulette = enemyRouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController enemySpin = enemyRouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            BuildRoulette(enemyRouletteRoot, enemyRoulette, enemySpin, legacyFont, frameSprite, pointerSprite, centerCapSprite, null, true);
            AddRect("EffectRoot", dealerRouletteContainer, Vector2.zero, new Vector2(440f, 440f));

            RectTransform bottomControl = AddStretchRect(
                "BottomControl", canvasObject.transform,
                Vector2.zero, new Vector2(1f, 0.14f),
                new Vector2(40f, 18f), new Vector2(-40f, -8f));
            RectTransform spinControl = AddFramedPanel("SpinControl", bottomControl, Vector2.zero, new Vector2(1840f, 125f), new Color32(24, 21, 36, 255), DarkGold, out _, out _);
            TMP_Text instructionText = AddText("GuideText", spinControl, "누르고 힘 모으기 · 놓으면 SPIN", font, 18, Ink, TextAlignmentOptions.Left, new Vector2(-560f, 30f), new Vector2(620f, 30f));
            TMP_Text powerPreviewText = AddText("InputGuide", spinControl, "강도는 착지 구역만 바꿉니다", font, 14, Muted, TextAlignmentOptions.Left, new Vector2(-560f, -7f), new Vector2(620f, 26f));
            RectTransform powerGauge = AddRect("PowerGauge", spinControl, new Vector2(160f, 6f), new Vector2(720f, 64f));
            UnityEngine.UI.Image powerTrack = AddImage("GaugeBackground", powerGauge, new Color32(54, 48, 68, 255));
            powerTrack.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            powerTrack.rectTransform.sizeDelta = new Vector2(700f, 18f);
            UnityEngine.UI.Image powerFill = AddImage("GaugeFill", powerTrack.transform, Gold);
            ConfigureFill(powerFill, 0.08f);
            TMP_Text powerText = AddText("PowerText", powerGauge, "회전 강도  8%", font, 15, Muted, TextAlignmentOptions.Center, new Vector2(0f, -17f), new Vector2(360f, 26f));
            UnityEngine.UI.Button throwButton = AddButton("SpinButton", spinControl, new Vector2(760f, 0f), new Vector2(104f, 104f), Gold, font, "SPIN", 21, out TMP_Text throwLabel);
            throwLabel.color = Background;
            UnityEngine.UI.Image throwImage = throwButton.GetComponent<UnityEngine.UI.Image>();
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (knobSprite != null)
            {
                throwImage.sprite = knobSprite;
                throwImage.preserveAspect = true;
            }
            AddRect("AnimationRoot", spinControl, Vector2.zero, new Vector2(1840f, 125f));

            GameObject battleObject = new GameObject("RabbitBattle", typeof(RectTransform));
            battleObject.transform.SetParent(canvasObject.transform, false);
            RabbitBattlePrototype battle = battleObject.AddComponent<RabbitBattlePrototype>();
            BattlePresentationUI presentation = battleObject.AddComponent<BattlePresentationUI>();
            HoldToSpinInput holdInput = throwButton.gameObject.AddComponent<HoldToSpinInput>();
            ConfigurePresentation(presentation, preparePhase, spinPhase, resolvePhase, dealerPhase, playerGlow, dealerGlow, houseRuleFrame);
            HijackTransferPresenter hijackTransferPresenter = BuildHijackTransferEffect(canvasObject.transform, font);

            RectTransform houseRuleOverlay = BuildHouseRuleOverlay(canvasObject.transform, font, out UnityEngine.UI.Button houseRuleInfoCloseButton, out TMP_Text houseRuleDetailProgressText);
            RectTransform hijackPanel = BuildHijackPanel(canvasObject.transform, font, out TMP_Text hijackInstruction, out UnityEngine.UI.Button[] hijackSourceButtons, out TMP_Text[] hijackSourceLabels, out UnityEngine.UI.Button[] hijackDestinationButtons, out TMP_Text[] hijackDestinationLabels);
            RectTransform endPanel = BuildEndPanel(canvasObject.transform, font, out TMP_Text endTitle, out TMP_Text endBody, out UnityEngine.UI.Button retryButton);

            BindBattle(
                battle, presentation, hijackTransferPresenter, roulette, enemyRoulette, spin, enemySpin, holdInput,
                playerHpText, rabbitHpText, guardText, shardText, playerHpFill, rabbitHpFill,
                roundText, dealerLineText, instructionText, powerPreviewText, resultText, calculationText,
                chainPreviewText, combatLogText, houseRuleProgressText, houseRuleFill,
                houseRuleOverlay.gameObject, houseRuleInfoButton, houseRuleInfoCloseButton, houseRuleDetailProgressText,
                openingSpeechBubble.gameObject, enemyNextIntentText,
                hijackPanel.gameObject, hijackInstruction, hijackSourceButtons, hijackSourceLabels,
                hijackDestinationButtons, hijackDestinationLabels, endPanel.gameObject, endTitle, endBody, retryButton);

            SerializedObject holdSo = new SerializedObject(holdInput);
            holdSo.FindProperty("battle").objectReferenceValue = battle;
            holdSo.FindProperty("fillImage").objectReferenceValue = powerFill;
            holdSo.FindProperty("powerText").objectReferenceValue = powerText;
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
            Debug.Log($"Rabbit battle tutorial UI built at {ScenePath}");
        }

        private static void BuildRoulette(RectTransform root, RouletteController controller, RouletteSpinController spin, Font legacyFont, Sprite frameSprite, Sprite pointerSprite, Sprite centerCapSprite, AudioClip spinSound, bool enemy)
        {
            float wheelSize = enemy ? 316f : 340f;
            float frameSize = enemy ? 390f : 418f;
            RectTransform wheel = AddRect("Wheel", root, Vector2.zero, new Vector2(wheelSize, wheelSize));
            RectTransform segments = AddRect("DynamicSegments", wheel, Vector2.zero, new Vector2(wheelSize, wheelSize));
            RoulettePixelWheelRenderer renderer = segments.gameObject.AddComponent<RoulettePixelWheelRenderer>();
            renderer.raycastTarget = false;
            UnityEngine.UI.Image centerCap = AddSpriteImage("CenterCap", wheel, centerCapSprite, new Vector2(74f, 74f));
            UnityEngine.UI.Image fixedFrame = AddSpriteImage("FixedFrame", root, frameSprite, new Vector2(frameSize, frameSize));
            fixedFrame.transform.SetAsLastSibling();
            UnityEngine.UI.Image pointer = AddSpriteImage("Pointer", root, pointerSprite, new Vector2(90f, 90f));
            pointer.rectTransform.anchoredPosition = new Vector2(0f, enemy ? 165f : 177f);

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("wheel").objectReferenceValue = wheel;
            controllerSo.FindProperty("dynamicSegmentRoot").objectReferenceValue = segments;
            controllerSo.FindProperty("pixelWheelRenderer").objectReferenceValue = renderer;
            controllerSo.FindProperty("labelFont").objectReferenceValue = legacyFont;
            controllerSo.FindProperty("labelFontSize").intValue = enemy ? 14 : 15;
            controllerSo.FindProperty("iconRadiusRatio").floatValue = 0.5f;
            controllerSo.FindProperty("labelRadiusRatio").floatValue = enemy ? 0.7f : 0.73f;
            controllerSo.FindProperty("labelSize").vector2Value = enemy ? new Vector2(82f, 32f) : new Vector2(78f, 34f);
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

        private static RectTransform BuildHouseRuleOverlay(Transform parent, TMP_FontAsset font, out UnityEngine.UI.Button closeButton, out TMP_Text detailProgress)
        {
            RectTransform overlay = AddPanel("HouseRuleInfoPanel", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color32(10, 8, 14, 220));
            overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            RectTransform dialog = AddFramedPanel("Dialog", overlay, Vector2.zero, new Vector2(840f, 500f), new Color32(35, 29, 45, 255), Gold, out _, out _);
            AddText("Title", dialog, "HOUSE RULE", font, 24, Gold, TextAlignmentOptions.Center, new Vector2(0f, 200f), new Vector2(720f, 38f));
            AddText("RuleName", dialog, "보험 처리", font, 36, Ink, TextAlignmentOptions.Center, new Vector2(0f, 150f), new Vector2(720f, 52f));
            AddText("ConditionLabel", dialog, "발동 조건", font, 18, Gold, TextAlignmentOptions.Left, new Vector2(-290f, 64f), new Vector2(150f, 30f));
            AddText("Condition", dialog, "토끼가 공개한 공격을 방어로 100% 막는다.", font, 20, Ink, TextAlignmentOptions.Left, new Vector2(70f, 64f), new Vector2(540f, 34f));
            AddText("RewardLabel", dialog, "보상", font, 18, Gold, TextAlignmentOptions.Left, new Vector2(-290f, 8f), new Vector2(150f, 30f));
            AddText("Reward", dialog, "토끼 룰렛의 칸 하나를 골라 내 칸과 교체한다.", font, 20, Ink, TextAlignmentOptions.Left, new Vector2(70f, 8f), new Vector2(540f, 34f));
            AddText("PenaltyLabel", dialog, "토끼 변화", font, 18, Gold, TextAlignmentOptions.Left, new Vector2(-290f, -48f), new Vector2(150f, 30f));
            AddText("Penalty", dialog, "빼앗긴 원래 칸은 봉인되어 이후 행동하지 않는다.", font, 20, Ink, TextAlignmentOptions.Left, new Vector2(70f, -48f), new Vector2(540f, 34f));
            AddText("Warning", dialog, "일부만 막거나 토끼가 회복한 턴에는 조건을 달성하지 못합니다.", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(0f, -112f), new Vector2(720f, 34f));
            detailProgress = AddText("Progress", dialog, "현재 진행도  0 / 1", font, 20, Gold, TextAlignmentOptions.Center, new Vector2(0f, -164f), new Vector2(720f, 36f));
            closeButton = AddButton("CloseButton", dialog, new Vector2(372f, 210f), new Vector2(54f, 54f), PanelLight, font, "×", 28, out _);
            AddRect("AnimationRoot", dialog, Vector2.zero, new Vector2(840f, 500f));
            return overlay;
        }

        private static RectTransform BuildHijackPanel(Transform parent, TMP_FontAsset font, out TMP_Text instruction, out UnityEngine.UI.Button[] sourceButtons, out TMP_Text[] sourceLabels, out UnityEngine.UI.Button[] destinationButtons, out TMP_Text[] destinationLabels)
        {
            RectTransform panel = AddFramedPanel("HijackPanel", parent, new Vector2(0f, -70f), new Vector2(1180f, 520f), new Color32(30, 25, 39, 252), Gold, out _, out _);
            AddText("Title", panel, "HOUSE RULE CLEAR · HIJACK", font, 32, Gold, TextAlignmentOptions.Center, new Vector2(0f, 210f), new Vector2(1080f, 48f));
            instruction = AddText("Instruction", panel, "1. 토끼의 칸 하나를 선택하세요", font, 20, Ink, TextAlignmentOptions.Center, new Vector2(0f, 165f), new Vector2(1080f, 38f));
            AddText("EnemySlotsTitle", panel, "토끼 룰렛 · 빼앗을 칸", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-390f, 125f), new Vector2(300f, 30f));
            sourceButtons = new UnityEngine.UI.Button[4];
            sourceLabels = new TMP_Text[4];
            for (int i = 0; i < sourceButtons.Length; i++)
            {
                float x = -330f + i * 220f;
                sourceButtons[i] = AddButton($"EnemySlot{i + 1}", panel, new Vector2(x, 73f), new Vector2(200f, 76f), new Color32(112, 50, 60, 255), font, $"{i + 1}\n-", 17, out sourceLabels[i]);
            }
            AddText("PlayerSlotsTitle", panel, "내 룰렛 · 교체할 위치", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-390f, 15f), new Vector2(300f, 30f));
            destinationButtons = new UnityEngine.UI.Button[8];
            destinationLabels = new TMP_Text[8];
            for (int i = 0; i < destinationButtons.Length; i++)
            {
                float x = -437.5f + i * 125f;
                destinationButtons[i] = AddButton($"PlayerSlot{i + 1}", panel, new Vector2(x, -43f), new Vector2(112f, 86f), new Color32(38, 91, 96, 255), font, $"{i + 1}\n-", 15, out destinationLabels[i]);
            }
            AddText("Note", panel, "선택한 토끼 칸은 봉인되고, 가져온 칸은 이번 전투 동안 내 룰렛에 남습니다.", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(0f, -125f), new Vector2(1060f, 36f));
            AddRect("AnimationRoot", panel, Vector2.zero, new Vector2(1180f, 520f));
            return panel;
        }

        private static RectTransform BuildEndPanel(Transform parent, TMP_FontAsset font, out TMP_Text title, out TMP_Text body, out UnityEngine.UI.Button retryButton)
        {
            RectTransform panel = AddFramedPanel("EndPanel", parent, Vector2.zero, new Vector2(900f, 480f), new Color32(35, 29, 45, 252), Gold, out _, out _);
            title = AddText("Title", panel, "", font, 42, Gold, TextAlignmentOptions.Center, new Vector2(0f, 130f), new Vector2(760f, 64f));
            body = AddText("Body", panel, "", font, 23, Ink, TextAlignmentOptions.Center, new Vector2(0f, 20f), new Vector2(760f, 120f));
            retryButton = AddButton("RetryButton", panel, new Vector2(0f, -145f), new Vector2(360f, 76f), Gold, font, "계약 되감기", 24, out TMP_Text label);
            label.color = Background;
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
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindBattle(
            RabbitBattlePrototype battle, BattlePresentationUI presentation,
            HijackTransferPresenter hijackTransferPresenter,
            RouletteController roulette, RouletteController enemyRoulette,
            RouletteSpinController spin, RouletteSpinController enemySpin, HoldToSpinInput holdInput,
            TMP_Text playerHpText, TMP_Text rabbitHpText, TMP_Text guardText, TMP_Text shardText,
            UnityEngine.UI.Image playerHpFill, UnityEngine.UI.Image rabbitHpFill,
            TMP_Text roundText, TMP_Text dealerLineText, TMP_Text instructionText, TMP_Text powerPreviewText,
            TMP_Text resultText, TMP_Text calculationText, TMP_Text chainPreviewText, TMP_Text combatLogText,
            TMP_Text houseRuleProgressText, UnityEngine.UI.Image houseRuleProgressFill,
            GameObject houseRuleInfoPanel, UnityEngine.UI.Button houseRuleInfoButton,
            UnityEngine.UI.Button houseRuleInfoCloseButton, TMP_Text houseRuleDetailProgressText,
            GameObject openingSpeechBubble, TMP_Text enemyNextIntentText,
            GameObject hijackPanel, TMP_Text hijackInstructionText,
            UnityEngine.UI.Button[] hijackSourceButtons, TMP_Text[] hijackSourceLabels,
            UnityEngine.UI.Button[] hijackDestinationButtons, TMP_Text[] hijackDestinationLabels,
            GameObject endPanel, TMP_Text endTitle, TMP_Text endBody, UnityEngine.UI.Button retryButton)
        {
            SerializedObject so = new SerializedObject(battle);
            SetObject(so, "presentationUi", presentation);
            SetObject(so, "hijackTransferPresenter", hijackTransferPresenter);
            SetObject(so, "roulette", roulette);
            SetObject(so, "enemyRoulette", enemyRoulette);
            SetObject(so, "spinController", spin);
            SetObject(so, "enemySpinController", enemySpin);
            SetObject(so, "spinInput", holdInput);
            SetObject(so, "playerHpText", playerHpText);
            SetObject(so, "rabbitHpText", rabbitHpText);
            SetObject(so, "guardText", guardText);
            SetObject(so, "shardText", shardText);
            SetObject(so, "playerHpFill", playerHpFill);
            SetObject(so, "rabbitHpFill", rabbitHpFill);
            SetObject(so, "roundText", roundText);
            SetObject(so, "dealerLineText", dealerLineText);
            SetObject(so, "instructionText", instructionText);
            SetObject(so, "powerPreviewText", powerPreviewText);
            SetObject(so, "resultText", resultText);
            SetObject(so, "calculationText", calculationText);
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
            SetObject(so, "retryButton", retryButton);
            so.FindProperty("enemyIntentFrames").arraySize = 0;
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

        private static UnityEngine.UI.Image AddHpWidget(string name, Transform parent, Vector2 position, string combatantName, Color fillColor, TMP_FontAsset font, out TMP_Text hpText)
        {
            RectTransform root = AddRect(name, parent, position, new Vector2(440f, 78f));
            AddText("NameText", root, combatantName, font, 18, Ink, TextAlignmentOptions.Left, new Vector2(-125f, 20f), new Vector2(170f, 28f));
            hpText = AddText("HPText", root, "24 / 24", font, 18, Ink, TextAlignmentOptions.Right, new Vector2(125f, 20f), new Vector2(170f, 28f));
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
