using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace DiaBlackJack.Editor
{
    [Overlay(typeof(SceneView), "Quick Play", defaultDisplay = true)]
    internal sealed class SceneQuickPlayOverlay : ToolbarOverlay
    {
        public SceneQuickPlayOverlay()
            : base(
                MainMenuQuickPlayButton.Id,
                GameSceneQuickPlayButton.Id)
        {
        }
    }

    internal abstract class SceneQuickPlayButton : EditorToolbarButton
    {
        protected SceneQuickPlayButton(
            string label,
            string buttonTooltip,
            Action playAction)
        {
            text = label;
            tooltip = buttonTooltip;
            clicked += playAction;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class MainMenuQuickPlayButton : SceneQuickPlayButton
    {
        public const string Id =
            "DiaBlackJack/QuickPlay/MainMenu";

        public MainMenuQuickPlayButton()
            : base(
                "Main Menu",
                "Enter Play Mode from MainMenuScene.",
                SceneQuickPlayLauncher.PlayMainMenu)
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class GameSceneQuickPlayButton : SceneQuickPlayButton
    {
        public const string Id =
            "DiaBlackJack/QuickPlay/GameScene";

        public GameSceneQuickPlayButton()
            : base(
                "Game Scene",
                "Enter Play Mode from GameScene.",
                SceneQuickPlayLauncher.PlayGameScene)
        {
        }
    }

    [InitializeOnLoad]
    internal static class SceneQuickPlayLauncher
    {
        internal const string MainMenuScenePath =
            "Assets/00. Scenes/MainMenuScene.unity";
        internal const string GameScenePath =
            "Assets/00. Scenes/GameScene.unity";

        private const string MenuRoot =
            "Tools/DiaBlackJack/Quick Play/";
        private const string RestorePendingKey =
            "DiaBlackJack.SceneQuickPlay.RestorePending";
        private const string PreviousStartScenePathKey =
            "DiaBlackJack.SceneQuickPlay.PreviousStartScenePath";

        static SceneQuickPlayLauncher()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        [MenuItem(MenuRoot + "Main Menu", false, 10)]
        internal static void PlayMainMenu()
        {
            PlayFromScene(MainMenuScenePath);
        }

        [MenuItem(MenuRoot + "Main Menu", true)]
        private static bool CanPlayMainMenu()
        {
            return CanEnterPlayMode();
        }

        [MenuItem(MenuRoot + "Game Scene", false, 11)]
        internal static void PlayGameScene()
        {
            PlayFromScene(GameScenePath);
        }

        [MenuItem(MenuRoot + "Game Scene", true)]
        private static bool CanPlayGameScene()
        {
            return CanEnterPlayMode();
        }

        private static bool CanEnterPlayMode()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void PlayFromScene(string scenePath)
        {
            if (!CanEnterPlayMode())
            {
                return;
            }

            SceneAsset scene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (scene == null)
            {
                string message =
                    $"Quick Play scene was not found:\n{scenePath}";
                Debug.LogError(message);
                EditorUtility.DisplayDialog(
                    "Quick Play",
                    message,
                    "OK");
                return;
            }

            CapturePreviousStartScene();

            try
            {
                EditorSceneManager.playModeStartScene = scene;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                RestorePreviousStartScene();
                Debug.LogException(exception);
            }
        }

        private static void CapturePreviousStartScene()
        {
            SceneAsset previous =
                EditorSceneManager.playModeStartScene;
            string previousPath = previous == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(previous);

            SessionState.SetString(
                PreviousStartScenePathKey,
                previousPath);
            SessionState.SetBool(RestorePendingKey, true);
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                RestorePreviousStartScene();
            }
        }

        private static void RestorePreviousStartScene()
        {
            if (!SessionState.GetBool(RestorePendingKey, false))
            {
                return;
            }

            string previousPath = SessionState.GetString(
                PreviousStartScenePathKey,
                string.Empty);
            SceneAsset previous = string.IsNullOrEmpty(previousPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);

            EditorSceneManager.playModeStartScene = previous;
            SessionState.EraseString(PreviousStartScenePathKey);
            SessionState.EraseBool(RestorePendingKey);
        }
    }
}
