#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RouletteLike.Roulette.Editor
{
    /// <summary>
    /// 테스트 가능한 RouletteRoot 프리팹과 데모 Scene을 생성합니다.
    /// 메뉴를 다시 실행하면 데모 에셋을 최신 코드 구조로 갱신합니다.
    /// </summary>
    public static class RouletteDemoSceneBuilder
    {
        private const string RootFolder = "Assets/RouletteSystem";
        private const string ArtFolder = RootFolder + "/Art";
        private const string FontFolder = ArtFolder + "/Fonts";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string SpriteSheetPath = ArtFolder + "/roulette_ui_sheet.png";
        private const string PixelFontPath = FontFolder + "/neodgm.ttf";
        private const string PrefabPath = PrefabFolder + "/RouletteDemo.prefab";
        private const string ScenePath = SceneFolder + "/RouletteDemo.unity";

        // SpriteRect ID를 고정해 메뉴를 다시 실행해도 Prefab/Scene의 Sprite 참조가 유지됩니다.
        private const string FrameSpriteId = "89dc6cc804ca4e66bfe6521c52e2aa01";
        private const string PointerSpriteId = "02f249792c174d6f8d52a7f9a9495c02";
        private const string CenterCapSpriteId = "4ecb2cbfc92d46cb9744cf41bfe95c03";

        [MenuItem("Tools/HIJACKPOT/Create Roulette Demo Scene")]
        public static void CreateDemoScene()
        {
            EnsureAssetFolders();
            ConfigureSpriteSheet();

            Sprite frameSprite = LoadSprite("roulette_frame");
            Sprite pointerSprite = LoadSprite("pointer");
            Sprite centerCapSprite = LoadSprite("center_cap");
            Font pixelFont = ConfigureAndLoadPixelFont();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasObject = CreateCanvas();
            CreateEventSystem();

            RectTransform rouletteRoot = CreateRect(
                "RouletteRoot",
                canvasObject.transform,
                new Vector2(620f, 720f),
                Vector2.zero);

            RectTransform fixedFrame = CreateRect(
                "FixedFrame",
                rouletteRoot,
                new Vector2(512f, 512f),
                Vector2.zero);
            Image frameImage = fixedFrame.gameObject.AddComponent<Image>();
            ConfigurePixelArtImage(frameImage, frameSprite);
            frameImage.raycastTarget = false;

            RectTransform wheel = CreateRect(
                "Wheel",
                rouletteRoot,
                new Vector2(420f, 420f),
                Vector2.zero);

            RectTransform dynamicSegments = CreateStretchRect("DynamicSegments", wheel);
            RoulettePixelWheelRenderer pixelWheelRenderer =
                dynamicSegments.gameObject.AddComponent<RoulettePixelWheelRenderer>();

            // Wheel의 가장자리는 프레임 아래까지 넓게 채우고, 금속 프레임은 그 위에 그립니다.
            // 계층상 둘 다 RouletteRoot의 고정된 형제이며 실제 회전 대상은 Wheel뿐입니다.
            fixedFrame.SetAsLastSibling();

            RectTransform centerCap = CreateRect(
                "CenterCap",
                wheel,
                new Vector2(92f, 92f),
                Vector2.zero);
            Image centerCapImage = centerCap.gameObject.AddComponent<Image>();
            ConfigurePixelArtImage(centerCapImage, centerCapSprite);
            centerCapImage.raycastTarget = false;

            RectTransform pointer = CreateRect(
                "Pointer",
                rouletteRoot,
                new Vector2(128f, 128f),
                new Vector2(0f, 214f));
            Image pointerImage = pointer.gameObject.AddComponent<Image>();
            ConfigurePixelArtImage(pointerImage, pointerSprite);
            pointerImage.raycastTarget = false;

            RectTransform highlightFx = CreateRect(
                "HighlightFX",
                rouletteRoot,
                new Vector2(428f, 428f),
                Vector2.zero);
            highlightFx.gameObject.SetActive(false);

            RectTransform spinButtonRect = CreateRect(
                "SpinButton",
                rouletteRoot,
                new Vector2(156f, 48f),
                new Vector2(0f, -292f));
            Image spinButtonImage = spinButtonRect.gameObject.AddComponent<Image>();
            spinButtonImage.color = new Color32(217, 62, 116, 255);
            Button spinButton = spinButtonRect.gameObject.AddComponent<Button>();
            spinButton.targetGraphic = spinButtonImage;
            CreateText(
                "Label",
                spinButtonRect,
                "SPIN",
                pixelFont,
                22,
                Color.white,
                Vector2.zero,
                new Vector2(156f, 48f));

            RouletteController controller = rouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController spinController = rouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            RouletteTest rouletteTest = rouletteRoot.gameObject.AddComponent<RouletteTest>();

            AssignObjectReference(controller, "wheel", wheel);
            AssignObjectReference(controller, "dynamicSegmentRoot", dynamicSegments);
            AssignObjectReference(controller, "pixelWheelRenderer", pixelWheelRenderer);
            AssignObjectReference(controller, "labelFont", pixelFont);

            AssignObjectReference(spinController, "rouletteController", controller);
            AssignObjectReference(spinController, "wheel", wheel);
            AssignObjectReference(spinController, "spinButton", spinButton);
            AssignObjectReference(spinController, "centerCap", centerCap);

            AssignObjectReference(rouletteTest, "rouletteController", controller);
            AssignObjectReference(rouletteTest, "spinController", spinController);

            CreateText(
                "Title",
                canvasObject.transform,
                "HIJACKPOT - DYNAMIC ROULETTE",
                pixelFont,
                24,
                new Color32(255, 224, 76, 255),
                new Vector2(-350f, 310f),
                new Vector2(520f, 40f));
            CreateText(
                "TestGuide",
                canvasObject.transform,
                "SPACE Spin   A Add   R Remove   W Weight   M Merge",
                pixelFont,
                16,
                new Color32(224, 219, 236, 255),
                new Vector2(0f, -346f),
                new Vector2(620f, 32f));

            PrefabUtility.SaveAsPrefabAsset(rouletteRoot.gameObject, PrefabPath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = rouletteRoot.gameObject;
            Debug.Log($"Roulette demo created: {ScenePath} and {PrefabPath}");
        }

        /// <summary>
        /// Prefab을 임시 인스턴스화해 픽셀 텍스처 생성과 런타임 칸 변형을 검증합니다.
        /// 에셋 자체는 수정하지 않습니다.
        /// </summary>
        [MenuItem("Tools/HIJACKPOT/Validate Roulette Pixel Demo")]
        public static void ValidatePixelDemo()
        {
            Font expectedFont = ConfigureAndLoadPixelFont();
            Scene demoScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = GameObject.Find("RouletteRoot");
            RouletteController sceneController =
                sceneRoot != null ? sceneRoot.GetComponent<RouletteController>() : null;
            RoulettePixelWheelRenderer sceneRenderer = sceneRoot != null
                ? sceneRoot.GetComponentInChildren<RoulettePixelWheelRenderer>(true)
                : null;
            if (!demoScene.IsValid()
                || sceneController == null
                || sceneRenderer == null
                || sceneController.PixelWheelRenderer != sceneRenderer
                || sceneController.LabelFont != expectedFont)
            {
                throw new System.InvalidOperationException(
                    "RouletteDemo scene does not contain the connected pixel renderer and font.");
            }

            GameObject canvasObject = GameObject.Find("Canvas");
            Text[] sceneTexts = canvasObject != null
                ? canvasObject.GetComponentsInChildren<Text>(true)
                : new Text[0];
            for (int i = 0; i < sceneTexts.Length; i++)
            {
                if (sceneTexts[i].font != expectedFont)
                {
                    throw new System.InvalidOperationException(
                        $"Scene Text '{sceneTexts[i].name}' does not use neodgm.ttf.");
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new System.IO.FileNotFoundException($"Roulette prefab was not found: {PrefabPath}");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException("Could not instantiate RouletteDemo prefab.");
            }

            try
            {
                RouletteController controller = instance.GetComponent<RouletteController>();
                RoulettePixelWheelRenderer renderer =
                    instance.GetComponentInChildren<RoulettePixelWheelRenderer>(true);
                if (controller == null || renderer == null)
                {
                    throw new System.InvalidOperationException(
                        "RouletteDemo requires Controller and PixelWheelRenderer.");
                }

                if (!controller.SetSegments(RouletteTest.CreateDefaultSegments(), false))
                {
                    throw new System.InvalidOperationException("Default pixel wheel data could not be applied.");
                }

                if (renderer.RuntimeTexture == null
                    || renderer.RuntimeTexture.width != 210
                    || renderer.RuntimeTexture.height != 210
                    || renderer.RuntimeTexture.filterMode != FilterMode.Point)
                {
                    throw new System.InvalidOperationException(
                        "Pixel wheel must create a 210x210 Point-filtered runtime texture.");
                }

                RouletteSegmentGraphic[] contents =
                    instance.GetComponentsInChildren<RouletteSegmentGraphic>(true);
                if (contents.Length != 8)
                {
                    throw new System.InvalidOperationException(
                        $"Expected 8 segment content objects, but found {contents.Length}.");
                }

                for (int i = 0; i < contents.Length; i++)
                {
                    if (contents[i].RendersGeometry)
                    {
                        throw new System.InvalidOperationException(
                            "Segment mesh must be disabled when the pixel renderer is active.");
                    }
                }

                Text[] prefabTexts = instance.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < prefabTexts.Length; i++)
                {
                    if (prefabTexts[i].font != expectedFont)
                    {
                        throw new System.InvalidOperationException(
                            $"Prefab Text '{prefabTexts[i].name}' does not use neodgm.ttf.");
                    }
                }

                float start;
                float end;
                float center;
                float size;
                controller.TryGetSegmentAngles(0, out start, out end, out center, out size);
                Color32 beforeHighlight = SamplePixel(renderer.RuntimeTexture, center, 0.62f);
                string firstId = controller.GetSegment(0).id;
                renderer.SetHighlightedSegment(firstId, true);
                Color32 afterHighlight = SamplePixel(renderer.RuntimeTexture, center, 0.62f);
                renderer.ClearHighlight();

                if (beforeHighlight.Equals(afterHighlight))
                {
                    throw new System.InvalidOperationException(
                        "Pixel palette highlight did not change the selected segment.");
                }

                RouletteSegmentData added = RouletteTest.CreateDefaultSegments()[0].Clone();
                added.id = "pixel_validation_added";
                if (!controller.AddSegment(added)
                    || !controller.ChangeSegmentWeight(0, 2f)
                    || !controller.RemoveSegment(controller.Count - 1)
                    || !controller.MergeSegments(0, 1))
                {
                    throw new System.InvalidOperationException(
                        "Add, weight, remove, or merge failed during pixel wheel validation.");
                }

                Debug.Log(
                    "Roulette pixel demo validated: neodgm font, 210x210 Point texture, "
                    + "palette highlight, Add/Remove/Weight/Merge all passed.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Color32 SamplePixel(Texture2D texture, float clockwiseAngle, float radiusRatio)
        {
            float radians = clockwiseAngle * Mathf.Deg2Rad;
            float radius = texture.width * 0.5f * Mathf.Clamp01(radiusRatio);
            int x = Mathf.Clamp(
                Mathf.FloorToInt(texture.width * 0.5f + Mathf.Sin(radians) * radius),
                0,
                texture.width - 1);
            int y = Mathf.Clamp(
                Mathf.FloorToInt(texture.height * 0.5f + Mathf.Cos(radians) * radius),
                0,
                texture.height - 1);
            return texture.GetPixel(x, y);
        }

        private static GameObject CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvasObject;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static RectTransform CreateRect(
            string objectName,
            Transform parent,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreateStretchRect(string objectName, Transform parent)
        {
            RectTransform rect = CreateRect(objectName, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            string content,
            Font font,
            int fontSize,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreateRect(objectName, parent, size, anchoredPosition);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = font != null
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Font ConfigureAndLoadPixelFont()
        {
            if (!File.Exists(PixelFontPath))
            {
                throw new FileNotFoundException($"Pixel font was not found: {PixelFontPath}");
            }

            AssetDatabase.ImportAsset(PixelFontPath, ImportAssetOptions.ForceSynchronousImport);
            TrueTypeFontImporter importer = AssetImporter.GetAtPath(PixelFontPath) as TrueTypeFontImporter;
            if (importer == null)
            {
                throw new InvalidDataException($"Could not load pixel font importer: {PixelFontPath}");
            }

            bool changed = importer.fontRenderingMode != FontRenderingMode.HintedRaster
                           || importer.fontSize != 16
                           || importer.characterPadding != 1
                           || !importer.shouldRoundAdvanceValue;
            if (changed)
            {
                importer.fontRenderingMode = FontRenderingMode.HintedRaster;
                importer.fontSize = 16;
                importer.characterPadding = 1;
                importer.shouldRoundAdvanceValue = true;
                importer.SaveAndReimport();
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(PixelFontPath);
            if (font == null)
            {
                throw new InvalidDataException($"Could not load pixel font: {PixelFontPath}");
            }

            return font;
        }

        private static void AssignObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 통합 PNG를 3개의 UI Sprite로 자르고 픽셀 아트용 Import 옵션을 적용합니다.
        /// 시트 좌표는 Unity의 좌하단 원점을 사용합니다.
        /// </summary>
        private static void ConfigureSpriteSheet()
        {
            if (!File.Exists(SpriteSheetPath))
            {
                throw new FileNotFoundException(
                    $"Roulette UI sprite sheet was not found: {SpriteSheetPath}");
            }

            AssetDatabase.ImportAsset(
                SpriteSheetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(SpriteSheetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidDataException($"Could not load TextureImporter: {SpriteSheetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 1f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            SpriteRect[] spriteRects =
            {
                CreateSpriteRect(
                    "roulette_frame",
                    new Rect(0f, 0f, 256f, 256f),
                    FrameSpriteId),
                CreateSpriteRect(
                    "pointer",
                    new Rect(256f, 192f, 64f, 64f),
                    PointerSpriteId),
                CreateSpriteRect(
                    "center_cap",
                    new Rect(320f, 210f, 46f, 46f),
                    CenterCapSpriteId)
            };

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider dataProvider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects);

            ISpriteNameFileIdDataProvider nameProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProvider != null)
            {
                SpriteNameFileIdPair[] pairs = new SpriteNameFileIdPair[spriteRects.Length];
                for (int i = 0; i < spriteRects.Length; i++)
                {
                    pairs[i] = new SpriteNameFileIdPair(
                        spriteRects[i].name,
                        spriteRects[i].spriteID);
                }

                nameProvider.SetNameFileIdPairs(pairs);
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static SpriteRect CreateSpriteRect(string spriteName, Rect rect, string spriteId)
        {
            return new SpriteRect
            {
                name = spriteName,
                rect = rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = new GUID(spriteId)
            };
        }

        private static Sprite LoadSprite(string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i] as Sprite;
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            throw new InvalidDataException(
                $"Sprite '{spriteName}' was not found in {SpriteSheetPath}");
        }

        private static void ConfigurePixelArtImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void EnsureAssetFolders()
        {
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(FontFolder);
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(SceneFolder);
            AssetDatabase.Refresh();
        }
    }
}
#endif
