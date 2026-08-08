using System;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
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
                GameSceneQuickPlayButton.Id,
                EnemyBattleQuickPlayDropdown.Id)
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class EnemyBattleQuickPlayDropdown : EditorToolbarDropdown
    {
        public const string Id =
            "DiaBlackJack/QuickPlay/EnemyBattle";

        public EnemyBattleQuickPlayDropdown()
        {
            text = "Enemy Battle";
            tooltip = "Choose an enemy profile and enter GameScene Play Mode.";
            clicked += ShowEnemyMenu;
        }

        private static void ShowEnemyMenu()
        {
            var menu = new GenericMenu();
            foreach (EnemyProfilePreview preview in
                EnemyCombatProfileCatalog.Default.Previews)
            {
                EnemyProfilePreview captured = preview;
                string label =
                    $"{captured.Grade}/{captured.DisplayName}";
                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () => SceneQuickPlayLauncher.PlayEnemyBattle(
                        captured.ProfileKey));
            }

            menu.ShowAsContext();
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
        private const string PendingEnemyProfileKey =
            "DiaBlackJack.SceneQuickPlay.PendingEnemyProfile";

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
            ClearPendingEnemyBattle();
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
            ClearPendingEnemyBattle();
            PlayFromScene(GameScenePath);
        }

        internal static void PlayEnemyBattle(string profileKey)
        {
            if (!CanEnterPlayMode() || string.IsNullOrWhiteSpace(profileKey))
            {
                return;
            }

            try
            {
                EnemyCombatProfileCatalog.Default.GetByKey(profileKey);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ClearPendingEnemyBattle();
                return;
            }

            SessionState.SetString(PendingEnemyProfileKey, profileKey);
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
                ClearPendingEnemyBattle();
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
                ClearPendingEnemyBattle();
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
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RestorePreviousStartScene();
                if (!string.IsNullOrWhiteSpace(
                        SessionState.GetString(
                            PendingEnemyProfileKey,
                            string.Empty)))
                {
                    EditorApplication.delayCall += StartPendingEnemyBattle;
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestorePreviousStartScene();
                ClearPendingEnemyBattle();
            }
        }

        private static void StartPendingEnemyBattle()
        {
            string profileKey = SessionState.GetString(
                PendingEnemyProfileKey,
                string.Empty);
            ClearPendingEnemyBattle();
            if (string.IsNullOrWhiteSpace(profileKey))
            {
                return;
            }

            GameManager manager =
                UnityEngine.Object.FindFirstObjectByType<GameManager>(
                    FindObjectsInactive.Include);
            if (manager == null ||
                !manager.DebugStartStandaloneEnemyBattle(profileKey))
            {
                Debug.LogError(
                    $"Enemy Battle Quick Play could not start '{profileKey}'.");
            }
        }

        private static void ClearPendingEnemyBattle()
        {
            SessionState.EraseString(PendingEnemyProfileKey);
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
