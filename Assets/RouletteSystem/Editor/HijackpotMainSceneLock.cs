#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RouletteLike.Roulette.Editor
{
    /// <summary>
    /// HIJACKPOT의 작업·실행 기준 씬을 SampleScene으로 고정한다.
    /// - Play를 누르면 어떤 씬을 열어두었든 항상 SampleScene에서 시작한다.
    /// - Build Settings의 첫 번째 씬을 SampleScene으로 유지한다.
    /// </summary>
    [InitializeOnLoad]
    public static class HijackpotMainSceneLock
    {
        public const string MainScenePath = "Assets/Scenes/SampleScene.unity";

        static HijackpotMainSceneLock()
        {
            EditorApplication.delayCall += Apply;
        }

        [MenuItem("Tools/HIJACKPOT/Open Main Scene (SampleScene)")]
        private static void OpenMainScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }
        }

        private static void Apply()
        {
            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (mainScene == null)
            {
                Debug.LogWarning($"[HIJACKPOT] Main scene not found: {MainScenePath}");
                return;
            }

            EditorSceneManager.playModeStartScene = mainScene;

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0 || scenes[0].path != MainScenePath || !scenes[0].enabled)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>
                {
                    new EditorBuildSettingsScene(MainScenePath, true)
                };
                foreach (EditorBuildSettingsScene scene in scenes)
                {
                    if (scene.path != MainScenePath)
                    {
                        list.Add(scene);
                    }
                }
                EditorBuildSettings.scenes = list.ToArray();
            }
        }
    }
}
#endif
