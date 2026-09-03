#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string PrefabPath = PrefabFolder + "/RouletteDemo.prefab";
        private const string ScenePath = SceneFolder + "/RouletteDemo.unity";

        [MenuItem("Tools/HIJACKPOT/Create Roulette Demo Scene")]
        public static void CreateDemoScene()
        {
            EnsureAssetFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasObject = CreateCanvas();
            CreateEventSystem();

            RectTransform rouletteRoot = CreateRect(
                "RouletteRoot",
                canvasObject.transform,
                new Vector2(480f, 560f),
                Vector2.zero);

            RectTransform fixedFrame = CreateRect(
                "FixedFrame",
                rouletteRoot,
                new Vector2(424f, 424f),
                new Vector2(0f, 20f));
            Image frameImage = fixedFrame.gameObject.AddComponent<Image>();
            frameImage.color = new Color32(38, 30, 56, 255);
            frameImage.raycastTarget = false;

            RectTransform wheel = CreateRect(
                "Wheel",
                rouletteRoot,
                new Vector2(400f, 400f),
                new Vector2(0f, 20f));

            RectTransform dynamicSegments = CreateStretchRect("DynamicSegments", wheel);

            RectTransform centerCap = CreateRect(
                "CenterCap",
                wheel,
                new Vector2(76f, 76f),
                Vector2.zero);
            Image centerCapImage = centerCap.gameObject.AddComponent<Image>();
            centerCapImage.color = new Color32(32, 25, 48, 255);
            centerCapImage.raycastTarget = false;

            RectTransform pointer = CreateRect(
                "Pointer",
                rouletteRoot,
                new Vector2(22f, 46f),
                new Vector2(0f, 229f));
            Image pointerImage = pointer.gameObject.AddComponent<Image>();
            pointerImage.color = new Color32(255, 224, 76, 255);
            pointerImage.raycastTarget = false;

            RectTransform highlightFx = CreateRect(
                "HighlightFX",
                rouletteRoot,
                new Vector2(420f, 420f),
                new Vector2(0f, 20f));
            highlightFx.gameObject.SetActive(false);

            RectTransform spinButtonRect = CreateRect(
                "SpinButton",
                rouletteRoot,
                new Vector2(156f, 48f),
                new Vector2(0f, -225f));
            Image spinButtonImage = spinButtonRect.gameObject.AddComponent<Image>();
            spinButtonImage.color = new Color32(217, 62, 116, 255);
            Button spinButton = spinButtonRect.gameObject.AddComponent<Button>();
            spinButton.targetGraphic = spinButtonImage;
            CreateText(
                "Label",
                spinButtonRect,
                "SPIN",
                22,
                Color.white,
                Vector2.zero,
                new Vector2(156f, 48f));

            RouletteController controller = rouletteRoot.gameObject.AddComponent<RouletteController>();
            RouletteSpinController spinController = rouletteRoot.gameObject.AddComponent<RouletteSpinController>();
            RouletteTest rouletteTest = rouletteRoot.gameObject.AddComponent<RouletteTest>();

            AssignObjectReference(controller, "wheel", wheel);
            AssignObjectReference(controller, "dynamicSegmentRoot", dynamicSegments);

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
                24,
                new Color32(255, 224, 76, 255),
                new Vector2(0f, 298f),
                new Vector2(520f, 40f));
            CreateText(
                "TestGuide",
                canvasObject.transform,
                "SPACE Spin   A Add   R Remove   W Weight   M Merge",
                15,
                new Color32(224, 219, 236, 255),
                new Vector2(0f, -302f),
                new Vector2(620f, 32f));

            PrefabUtility.SaveAsPrefabAsset(rouletteRoot.gameObject, PrefabPath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = rouletteRoot.gameObject;
            Debug.Log($"Roulette demo created: {ScenePath} and {PrefabPath}");
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
            scaler.referenceResolution = new Vector2(960f, 640f);
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
            int fontSize,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreateRect(objectName, parent, size, anchoredPosition);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void AssignObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureAssetFolders()
        {
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(SceneFolder);
            AssetDatabase.Refresh();
        }
    }
}
#endif
