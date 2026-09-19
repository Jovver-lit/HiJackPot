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

        private static readonly Color Background = new Color32(22, 19, 29, 255);
        private static readonly Color Panel = new Color32(35, 30, 46, 255);
        private static readonly Color PanelLight = new Color32(52, 45, 66, 255);
        private static readonly Color Gold = new Color32(224, 168, 52, 255);
        private static readonly Color Ink = new Color32(245, 240, 226, 255);
        private static readonly Color Muted = new Color32(174, 165, 188, 255);
        private static readonly Color Red = new Color32(202, 70, 75, 255);
        private static readonly Color Green = new Color32(66, 168, 111, 255);
        private static readonly Color Teal = new Color32(57, 155, 163, 255);

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
                "Canvas",
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

            UnityEngine.UI.Image background = AddImage("Background", canvasObject.transform, Background);
            Stretch(background.rectTransform);

            RectTransform header = AddPanel("Header", canvasObject.transform, new Vector2(0f, 480f), new Vector2(1920f, 120f), new Color32(29, 24, 39, 255));
            TMP_Text roundText = AddText("Round", header, "TUTORIAL TABLE   ·   ROUND 1", font, 30, Gold, TextAlignmentOptions.Center, new Vector2(0f, 19f), new Vector2(700f, 42f));
            AddText("Motto", header, "운이 없다면, 룰렛을 좋게 만든다.", font, 23, Ink, TextAlignmentOptions.Center, new Vector2(0f, -27f), new Vector2(820f, 36f));

            TMP_Text playerHpText;
            UnityEngine.UI.Image playerHpFill = AddStatusBar("PlayerStatus", header, new Vector2(-700f, -5f), "손님", Green, font, out playerHpText);
            TMP_Text rabbitHpText;
            UnityEngine.UI.Image rabbitHpFill = AddStatusBar("RabbitStatus", header, new Vector2(700f, -5f), "토끼 딜러", Red, font, out rabbitHpText);

            RectTransform battleStage = AddPanel("BattleStage", canvasObject.transform, new Vector2(0f, 310f), new Vector2(1760f, 220f), new Color32(27, 22, 36, 255));
            UnityEngine.UI.Image stageFloor = AddImage("StageFloor", battleStage, new Color32(38, 31, 48, 255));
            stageFloor.rectTransform.anchorMin = new Vector2(0f, 0f);
            stageFloor.rectTransform.anchorMax = new Vector2(1f, 0f);
            stageFloor.rectTransform.pivot = new Vector2(0.5f, 0f);
            stageFloor.rectTransform.sizeDelta = new Vector2(0f, 66f);
            stageFloor.rectTransform.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image businessmanImage = AddSpriteImage("Businessman", battleStage, businessmanIdleFrames[0], new Vector2(210f, 210f));
            businessmanImage.rectTransform.anchoredPosition = new Vector2(-700f, 3f);
            businessmanImage.raycastTarget = false;
            NaturalBlinkAnimator blinkAnimator = businessmanImage.gameObject.AddComponent<NaturalBlinkAnimator>();
            SerializedObject blinkSo = new SerializedObject(blinkAnimator);
            blinkSo.FindProperty("targetImage").objectReferenceValue = businessmanImage;
            SetObjectArray(blinkSo.FindProperty("idleFrames"), businessmanIdleFrames);
            SetObjectArray(blinkSo.FindProperty("blinkFrames"), businessmanBlinkFrames);
            blinkSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEngine.UI.Image rabbitImage = AddSpriteImage("RabbitDealer", battleStage, rabbitSprite, new Vector2(210f, 210f));
            rabbitImage.rectTransform.anchoredPosition = new Vector2(700f, 3f);
            rabbitImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rabbitImage.raycastTarget = false;

            AddText("HouseRuleTitle", battleStage, "HOUSE RULE · 보험 처리", font, 24, Gold, TextAlignmentOptions.Center, new Vector2(0f, 43f), new Vector2(650f, 40f));
            UnityEngine.UI.Image houseRuleDivider = AddImage("HouseRuleDivider", battleStage, Gold);
            houseRuleDivider.rectTransform.anchoredPosition = new Vector2(0f, 15f);
            houseRuleDivider.rectTransform.sizeDelta = new Vector2(520f, 3f);
            AddText(
                "HouseRuleBody",
                battleStage,
                "공개된 공격을 방어로 전부 막으면 토끼 칸 1개를 영구 HIJACK",
                font,
                18,
                Ink,
                TextAlignmentOptions.Center,
                new Vector2(0f, -22f),
                new Vector2(720f, 36f));
            UnityEngine.UI.Image houseRuleTrack = AddImage("HouseRuleTrack", battleStage, new Color32(68, 59, 78, 255));
            houseRuleTrack.rectTransform.anchoredPosition = new Vector2(0f, -55f);
            houseRuleTrack.rectTransform.sizeDelta = new Vector2(520f, 12f);
            UnityEngine.UI.Image houseRuleFill = AddImage("HouseRuleFill", houseRuleTrack.transform, Gold);
            Stretch(houseRuleFill.rectTransform);
            houseRuleFill.sprite = GetFilledImageSprite();
            houseRuleFill.type = UnityEngine.UI.Image.Type.Filled;
            houseRuleFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            houseRuleFill.fillOrigin = 0;
            houseRuleFill.fillAmount = 0f;
            TMP_Text houseRuleProgressText = AddText("HouseRuleProgress", battleStage, "0 / 1  ·  공개 공격 완전 방어", font, 16, Muted, TextAlignmentOptions.Center, new Vector2(0f, -79f), new Vector2(620f, 28f));

            RectTransform wheelPanel = AddPanel("PlayerWheelPanel", canvasObject.transform, new Vector2(-610f, -90f), new Vector2(550f, 570f), new Color32(29, 25, 38, 255));
            AddText("WheelTitle", wheelPanel, "내 룰렛", font, 24, Gold, TextAlignmentOptions.Center, new Vector2(0f, 245f), new Vector2(500f, 40f));
            TMP_Text chainPreviewText = AddText("ChainPreview", wheelPanel, "연쇄 미리보기", font, 16, Muted, TextAlignmentOptions.Center, new Vector2(0f, -265f), new Vector2(510f, 28f));
            RectTransform rouletteRoot = AddRect("PlayerRoulette", wheelPanel, new Vector2(0f, -25f), new Vector2(440f, 440f));
            RouletteController roulette = rouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController spin = rouletteRoot.gameObject.AddComponent<RouletteSpinController>();

            RectTransform wheel = AddRect("Wheel", rouletteRoot, Vector2.zero, new Vector2(350f, 350f));
            RectTransform dynamicSegments = AddRect("DynamicSegments", wheel, Vector2.zero, new Vector2(350f, 350f));
            RoulettePixelWheelRenderer renderer = dynamicSegments.gameObject.AddComponent<RoulettePixelWheelRenderer>();
            renderer.raycastTarget = false;
            UnityEngine.UI.Image centerCap = AddSpriteImage("CenterCap", wheel, centerCapSprite, new Vector2(78f, 78f));
            UnityEngine.UI.Image fixedFrame = AddSpriteImage("FixedFrame", rouletteRoot, frameSprite, new Vector2(430f, 430f));
            fixedFrame.transform.SetAsLastSibling();
            UnityEngine.UI.Image pointer = AddSpriteImage("Pointer", rouletteRoot, pointerSprite, new Vector2(96f, 96f));
            pointer.rectTransform.anchoredPosition = new Vector2(0f, 184f);

            SerializedObject rouletteSo = new SerializedObject(roulette);
            rouletteSo.FindProperty("wheel").objectReferenceValue = wheel;
            rouletteSo.FindProperty("dynamicSegmentRoot").objectReferenceValue = dynamicSegments;
            rouletteSo.FindProperty("pixelWheelRenderer").objectReferenceValue = renderer;
            rouletteSo.FindProperty("labelFont").objectReferenceValue = legacyFont;
            rouletteSo.FindProperty("labelFontSize").intValue = 15;
            rouletteSo.FindProperty("iconRadiusRatio").floatValue = 0.5f;
            rouletteSo.FindProperty("labelRadiusRatio").floatValue = 0.73f;
            rouletteSo.FindProperty("labelSize").vector2Value = new Vector2(78f, 34f);
            rouletteSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject spinSo = new SerializedObject(spin);
            spinSo.FindProperty("rouletteController").objectReferenceValue = roulette;
            spinSo.FindProperty("wheel").objectReferenceValue = wheel;
            spinSo.FindProperty("centerCap").objectReferenceValue = centerCap.rectTransform;
            spinSo.FindProperty("minSpinDuration").floatValue = 1.8f;
            spinSo.FindProperty("maxSpinDuration").floatValue = 3.3f;
            spinSo.FindProperty("minimumFullRotations").intValue = 2;
            spinSo.FindProperty("selectedHighlightDuration").floatValue = 0.9f;
            spinSo.ApplyModifiedPropertiesWithoutUndo();

            RectTransform infoPanel = AddPanel("ExplanationLogPanel", canvasObject.transform, new Vector2(0f, -90f), new Vector2(520f, 570f), Panel);
            AddText("InfoTitle", infoPanel, "설명 · 전투 로그", font, 24, Gold, TextAlignmentOptions.Center, new Vector2(0f, 245f), new Vector2(470f, 40f));
            TMP_Text resultText = AddText("Result", infoPanel, "첫 SPIN을 준비하세요", font, 22, Ink, TextAlignmentOptions.Center, new Vector2(0f, 207f), new Vector2(470f, 38f));
            TMP_Text calculationText = AddText("Calculation", infoPanel, "인접 연쇄와 배율 계산", font, 16, Gold, TextAlignmentOptions.Center, new Vector2(0f, 174f), new Vector2(470f, 28f));

            RectTransform statRow = AddRect("StatRow", infoPanel, new Vector2(0f, 137f), new Vector2(500f, 52f));
            TMP_Text guardText = AddBadge("Guard", statRow, "방어 0", Teal, font, new Vector2(-116f, 0f));
            TMP_Text shardText = AddBadge("Shard", statRow, "규칙 조각 0", Gold, font, new Vector2(116f, 0f));

            RectTransform dialogue = AddPanel("DealerDialogue", infoPanel, new Vector2(0f, 69f), new Vector2(470f, 70f), new Color32(43, 35, 52, 255));
            AddText("Speaker", dialogue, "토끼 딜러", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-174f, 0f), new Vector2(92f, 46f));
            TMP_Text dealerLineText = AddText("Line", dialogue, "", font, 16, Ink, TextAlignmentOptions.Left, new Vector2(52f, 0f), new Vector2(340f, 52f));

            AddText("LogTitle", infoPanel, "전투 로그", font, 20, Gold, TextAlignmentOptions.Left, new Vector2(-85f, 16f), new Vector2(300f, 32f));
            RectTransform logPanel = AddPanel("LogPanel", infoPanel, new Vector2(0f, -121f), new Vector2(470f, 194f), new Color32(27, 23, 36, 255));
            TMP_Text combatLogText = AddText("CombatLog", logPanel, "", font, 16, Muted, TextAlignmentOptions.TopLeft, Vector2.zero, new Vector2(430f, 158f));
            combatLogText.lineSpacing = 8f;

            RectTransform enemyPanel = AddPanel("EnemyWheelPanel", canvasObject.transform, new Vector2(610f, -90f), new Vector2(550f, 570f), new Color32(29, 25, 38, 255));
            AddText("EnemyTitle", enemyPanel, "상대 룰렛 · 토끼 딜러", font, 24, Gold, TextAlignmentOptions.Center, new Vector2(0f, 245f), new Vector2(500f, 40f));
            TMP_Text enemyNextIntentText = AddText("NextIntent", enemyPanel, "NEXT  공격 4", font, 23, Ink, TextAlignmentOptions.Center, new Vector2(0f, 202f), new Vector2(460f, 38f));
            RectTransform enemyRouletteRoot = AddRect("EnemyRoulette", enemyPanel, new Vector2(0f, -45f), new Vector2(440f, 440f));
            RouletteController enemyRoulette = enemyRouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController enemySpin = enemyRouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            RectTransform enemyWheel = AddRect("Wheel", enemyRouletteRoot, Vector2.zero, new Vector2(350f, 350f));
            RectTransform enemySegments = AddRect("DynamicSegments", enemyWheel, Vector2.zero, new Vector2(350f, 350f));
            RoulettePixelWheelRenderer enemyRenderer = enemySegments.gameObject.AddComponent<RoulettePixelWheelRenderer>();
            enemyRenderer.raycastTarget = false;
            UnityEngine.UI.Image enemyCenterCap = AddSpriteImage("CenterCap", enemyWheel, centerCapSprite, new Vector2(78f, 78f));
            UnityEngine.UI.Image enemyFrame = AddSpriteImage("FixedFrame", enemyRouletteRoot, frameSprite, new Vector2(430f, 430f));
            enemyFrame.transform.SetAsLastSibling();
            UnityEngine.UI.Image enemyPointer = AddSpriteImage("Pointer", enemyRouletteRoot, pointerSprite, new Vector2(96f, 96f));
            enemyPointer.rectTransform.anchoredPosition = new Vector2(0f, 184f);

            SerializedObject enemyRouletteSo = new SerializedObject(enemyRoulette);
            enemyRouletteSo.FindProperty("wheel").objectReferenceValue = enemyWheel;
            enemyRouletteSo.FindProperty("dynamicSegmentRoot").objectReferenceValue = enemySegments;
            enemyRouletteSo.FindProperty("pixelWheelRenderer").objectReferenceValue = enemyRenderer;
            enemyRouletteSo.FindProperty("labelFont").objectReferenceValue = legacyFont;
            enemyRouletteSo.FindProperty("labelFontSize").intValue = 15;
            enemyRouletteSo.FindProperty("iconRadiusRatio").floatValue = 0.5f;
            enemyRouletteSo.FindProperty("labelRadiusRatio").floatValue = 0.7f;
            enemyRouletteSo.FindProperty("labelSize").vector2Value = new Vector2(86f, 34f);
            enemyRouletteSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject enemySpinSo = new SerializedObject(enemySpin);
            enemySpinSo.FindProperty("rouletteController").objectReferenceValue = enemyRoulette;
            enemySpinSo.FindProperty("wheel").objectReferenceValue = enemyWheel;
            enemySpinSo.FindProperty("centerCap").objectReferenceValue = enemyCenterCap.rectTransform;
            enemySpinSo.FindProperty("minSpinDuration").floatValue = 2.1f;
            enemySpinSo.FindProperty("maxSpinDuration").floatValue = 2.7f;
            enemySpinSo.FindProperty("startSpeed").floatValue = 960f;
            enemySpinSo.FindProperty("deceleration").floatValue = 680f;
            enemySpinSo.FindProperty("minimumFullRotations").intValue = 2;
            enemySpinSo.FindProperty("selectedHighlightDuration").floatValue = 0.9f;
            enemySpinSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEngine.UI.Image[] intentFrames = new UnityEngine.UI.Image[0];

            RectTransform controlPanel = AddPanel("Controls", canvasObject.transform, new Vector2(0f, -445f), new Vector2(1760f, 150f), new Color32(31, 26, 40, 255));
            TMP_Text instructionText = AddText("Instruction", controlPanel, "", font, 20, Ink, TextAlignmentOptions.Left, new Vector2(-560f, 40f), new Vector2(640f, 38f));
            TMP_Text powerPreviewText = AddText("PowerPreview", controlPanel, "", font, 17, Muted, TextAlignmentOptions.Left, new Vector2(-560f, -5f), new Vector2(640f, 34f));

            RectTransform nudgePanel = AddPanel("NudgePanel", controlPanel, new Vector2(-65f, 0f), new Vector2(300f, 104f), new Color32(43, 35, 52, 255));
            AddText("NudgeTitle", nudgePanel, "NUDGE · 턴당 1회", font, 15, Gold, TextAlignmentOptions.Center, new Vector2(0f, 34f), new Vector2(280f, 24f));
            UnityEngine.UI.Button nudgeLeft = AddButton("NudgeLeft", nudgePanel, new Vector2(-94f, -10f), new Vector2(82f, 48f), new Color32(54, 91, 107, 255), font, "◀", 23, out _);
            UnityEngine.UI.Button nudgeKeep = AddButton("NudgeKeep", nudgePanel, new Vector2(0f, -10f), new Vector2(82f, 48f), new Color32(74, 54, 93, 255), font, "유지", 16, out _);
            UnityEngine.UI.Button nudgeRight = AddButton("NudgeRight", nudgePanel, new Vector2(94f, -10f), new Vector2(82f, 48f), new Color32(54, 91, 107, 255), font, "▶", 23, out _);

            UnityEngine.UI.Button throwButton = AddButton("ThrowButton", controlPanel, new Vector2(490f, 0f), new Vector2(620f, 104f), new Color32(74, 54, 93, 255), font, "누르고 힘 모으기  ·  놓으면 SPIN", 24, out TMP_Text throwLabel);
            throwButton.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            UnityEngine.UI.Image powerTrack = AddImage("PowerTrack", throwButton.transform, new Color32(43, 35, 52, 255));
            powerTrack.rectTransform.anchorMin = new Vector2(0.04f, 0.12f);
            powerTrack.rectTransform.anchorMax = new Vector2(0.96f, 0.31f);
            powerTrack.rectTransform.offsetMin = Vector2.zero;
            powerTrack.rectTransform.offsetMax = Vector2.zero;
            UnityEngine.UI.Image powerFill = AddImage("PowerFill", powerTrack.transform, Gold);
            Stretch(powerFill.rectTransform);
            powerFill.sprite = GetFilledImageSprite();
            powerFill.type = UnityEngine.UI.Image.Type.Filled;
            powerFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            powerFill.fillAmount = 0.08f;
            TMP_Text powerText = AddText("PowerText", throwButton.transform, "회전 강도  8%", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(0f, -32f), new Vector2(300f, 28f));

            GameObject battleObject = new GameObject("RabbitBattle", typeof(RectTransform));
            battleObject.transform.SetParent(canvasObject.transform, false);
            RabbitBattlePrototype battle = battleObject.AddComponent<RabbitBattlePrototype>();
            HoldToSpinInput holdInput = throwButton.gameObject.AddComponent<HoldToSpinInput>();

            RectTransform hijackPanel = AddPanel("HijackPanel", canvasObject.transform, new Vector2(0f, -70f), new Vector2(1180f, 520f), new Color32(30, 25, 39, 252));
            AddText("HijackTitle", hijackPanel, "HOUSE RULE CLEAR · HIJACK", font, 32, Gold, TextAlignmentOptions.Center, new Vector2(0f, 210f), new Vector2(1080f, 48f));
            TMP_Text hijackInstruction = AddText("HijackInstruction", hijackPanel, "1. 토끼의 칸 하나를 선택하세요", font, 20, Ink, TextAlignmentOptions.Center, new Vector2(0f, 165f), new Vector2(1080f, 38f));
            AddText("EnemySlotsTitle", hijackPanel, "토끼 룰렛 · 빼앗을 칸", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-390f, 125f), new Vector2(300f, 30f));

            UnityEngine.UI.Button[] hijackSourceButtons = new UnityEngine.UI.Button[4];
            TMP_Text[] hijackSourceLabels = new TMP_Text[4];
            for (int i = 0; i < hijackSourceButtons.Length; i++)
            {
                float x = -330f + i * 220f;
                hijackSourceButtons[i] = AddButton($"EnemySlot{i + 1}", hijackPanel, new Vector2(x, 73f), new Vector2(200f, 76f), new Color32(112, 50, 60, 255), font, $"{i + 1}\n-", 17, out hijackSourceLabels[i]);
            }

            AddText("PlayerSlotsTitle", hijackPanel, "내 룰렛 · 교체할 위치", font, 17, Gold, TextAlignmentOptions.Left, new Vector2(-390f, 15f), new Vector2(300f, 30f));
            UnityEngine.UI.Button[] hijackDestinationButtons = new UnityEngine.UI.Button[8];
            TMP_Text[] hijackDestinationLabels = new TMP_Text[8];
            for (int i = 0; i < hijackDestinationButtons.Length; i++)
            {
                float x = -437.5f + i * 125f;
                hijackDestinationButtons[i] = AddButton($"PlayerSlot{i + 1}", hijackPanel, new Vector2(x, -43f), new Vector2(112f, 86f), new Color32(38, 91, 96, 255), font, $"{i + 1}\n-", 15, out hijackDestinationLabels[i]);
            }
            AddText("HijackNote", hijackPanel, "선택한 토끼 칸은 봉인되고, 가져온 칸은 이번 전투 동안 내 룰렛에 남습니다.", font, 17, Muted, TextAlignmentOptions.Center, new Vector2(0f, -125f), new Vector2(1060f, 36f));

            RectTransform endPanel = AddPanel("EndPanel", canvasObject.transform, Vector2.zero, new Vector2(900f, 480f), new Color32(35, 29, 45, 252));
            TMP_Text endTitle = AddText("EndTitle", endPanel, "", font, 42, Gold, TextAlignmentOptions.Center, new Vector2(0f, 130f), new Vector2(760f, 64f));
            TMP_Text endBody = AddText("EndBody", endPanel, "", font, 23, Ink, TextAlignmentOptions.Center, new Vector2(0f, 20f), new Vector2(760f, 120f));
            UnityEngine.UI.Button retryButton = AddButton("RetryButton", endPanel, new Vector2(0f, -145f), new Vector2(360f, 76f), Gold, font, "계약 되감기", 24, out TMP_Text retryLabel);
            retryLabel.color = Background;

            BindBattle(
                battle, roulette, enemyRoulette, spin, enemySpin, holdInput, playerHpText, rabbitHpText, guardText, shardText,
                playerHpFill, rabbitHpFill, roundText, dealerLineText, instructionText, powerPreviewText,
                resultText, calculationText, chainPreviewText, combatLogText, houseRuleProgressText, houseRuleFill,
                intentFrames, enemyNextIntentText, nudgePanel.gameObject, nudgeLeft, nudgeKeep, nudgeRight,
                hijackPanel.gameObject, hijackInstruction, hijackSourceButtons, hijackSourceLabels,
                hijackDestinationButtons, hijackDestinationLabels, endPanel.gameObject,
                endTitle, endBody, retryButton);

            SerializedObject holdSo = new SerializedObject(holdInput);
            holdSo.FindProperty("battle").objectReferenceValue = battle;
            holdSo.FindProperty("fillImage").objectReferenceValue = powerFill;
            holdSo.FindProperty("powerText").objectReferenceValue = powerText;
            holdSo.ApplyModifiedPropertiesWithoutUndo();

            nudgePanel.gameObject.SetActive(false);
            hijackPanel.gameObject.SetActive(false);
            endPanel.gameObject.SetActive(false);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = battleObject;
            Debug.Log($"Rabbit battle prototype built at {ScenePath}");
        }

        private static void BindBattle(
            RabbitBattlePrototype battle,
            RouletteController roulette,
            RouletteController enemyRoulette,
            RouletteSpinController spin,
            RouletteSpinController enemySpin,
            HoldToSpinInput holdInput,
            TMP_Text playerHpText,
            TMP_Text rabbitHpText,
            TMP_Text guardText,
            TMP_Text shardText,
            UnityEngine.UI.Image playerHpFill,
            UnityEngine.UI.Image rabbitHpFill,
            TMP_Text roundText,
            TMP_Text dealerLineText,
            TMP_Text instructionText,
            TMP_Text powerPreviewText,
            TMP_Text resultText,
            TMP_Text calculationText,
            TMP_Text chainPreviewText,
            TMP_Text combatLogText,
            TMP_Text houseRuleProgressText,
            UnityEngine.UI.Image houseRuleProgressFill,
            UnityEngine.UI.Image[] intentFrames,
            TMP_Text enemyNextIntentText,
            GameObject nudgePanel,
            UnityEngine.UI.Button nudgeLeftButton,
            UnityEngine.UI.Button nudgeKeepButton,
            UnityEngine.UI.Button nudgeRightButton,
            GameObject hijackPanel,
            TMP_Text hijackInstructionText,
            UnityEngine.UI.Button[] hijackSourceButtons,
            TMP_Text[] hijackSourceLabels,
            UnityEngine.UI.Button[] hijackDestinationButtons,
            TMP_Text[] hijackDestinationLabels,
            GameObject endPanel,
            TMP_Text endTitle,
            TMP_Text endBody,
            UnityEngine.UI.Button retryButton)
        {
            SerializedObject so = new SerializedObject(battle);
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
            SetObject(so, "enemyNextIntentText", enemyNextIntentText);
            SetObject(so, "nudgePanel", nudgePanel);
            SetObject(so, "nudgeLeftButton", nudgeLeftButton);
            SetObject(so, "nudgeKeepButton", nudgeKeepButton);
            SetObject(so, "nudgeRightButton", nudgeRightButton);
            SetObject(so, "hijackPanel", hijackPanel);
            SetObject(so, "hijackInstructionText", hijackInstructionText);
            SetObject(so, "endPanel", endPanel);
            SetObject(so, "endTitleText", endTitle);
            SetObject(so, "endBodyText", endBody);
            SetObject(so, "retryButton", retryButton);

            SerializedProperty frames = so.FindProperty("enemyIntentFrames");
            frames.arraySize = intentFrames.Length;
            for (int i = 0; i < intentFrames.Length; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue = intentFrames[i];
            }

            SetObjectArray(so.FindProperty("hijackSourceButtons"), hijackSourceButtons);
            SetObjectArray(so.FindProperty("hijackSourceLabels"), hijackSourceLabels);
            SetObjectArray(so.FindProperty("hijackDestinationButtons"), hijackDestinationButtons);
            SetObjectArray(so.FindProperty("hijackDestinationLabels"), hijackDestinationLabels);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_FontAsset GetOrCreateFontAsset()
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (asset != null)
            {
                return asset;
            }

            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/RouletteSystem/Art/Fonts/neodgm.ttf");
            asset = TMP_FontAsset.CreateFontAsset(
                source,
                64,
                8,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.ImportAsset(FontAssetPath);
            return asset;
        }

        private static UnityEngine.UI.Image AddStatusBar(string name, Transform parent, Vector2 position, string label, Color fillColor, TMP_FontAsset font, out TMP_Text valueText)
        {
            RectTransform root = AddRect(name, parent, position, new Vector2(420f, 76f));
            valueText = AddText("Value", root, label, font, 20, Ink, TextAlignmentOptions.Left, new Vector2(0f, 19f), new Vector2(400f, 30f));
            UnityEngine.UI.Image track = AddImage("Track", root, new Color32(68, 59, 78, 255));
            track.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            track.rectTransform.sizeDelta = new Vector2(400f, 18f);
            UnityEngine.UI.Image fill = AddImage("Fill", track.transform, fillColor);
            Stretch(fill.rectTransform);
            fill.sprite = GetFilledImageSprite();
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillClockwise = true;
            return fill;
        }

        private static Sprite GetFilledImageSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static TMP_Text AddBadge(string name, Transform parent, string label, Color accent, TMP_FontAsset font, Vector2 position)
        {
            RectTransform badge = AddPanel(name, parent, position, new Vector2(220f, 46f), new Color32(43, 37, 54, 255));
            UnityEngine.UI.Image stripe = AddImage("Stripe", badge, accent);
            stripe.rectTransform.anchorMin = new Vector2(0f, 0f);
            stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
            stripe.rectTransform.pivot = new Vector2(0f, 0.5f);
            stripe.rectTransform.sizeDelta = new Vector2(8f, 0f);
            return AddText("Label", badge, label, font, 18, Ink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(200f, 34f));
        }

        private static UnityEngine.UI.Button AddButton(string name, Transform parent, Vector2 position, Vector2 size, Color color, TMP_FontAsset font, string label, int fontSize, out TMP_Text labelText)
        {
            UnityEngine.UI.Image image = AddImage(name, parent, color);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            UnityEngine.UI.Button button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            image.raycastTarget = true;
            labelText = AddText("Label", image.transform, label, font, fontSize, Ink, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(28f, 20f));
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

        private static Sprite GetSprite(string spriteName)
        {
            return GetSprite(RouletteSpriteSheetPath, spriteName);
        }

        private static Sprite GetSprite(string assetPath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogError($"Missing sprite '{spriteName}' in {assetPath}");
            return null;
        }

        private static void SetObjectArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
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
    }
}
#endif
