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
                EnemyBattleQuickPlayDropdown.Id,
                RunResultQuickPlayDropdown.Id)
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class RunResultQuickPlayDropdown : EditorToolbarDropdown
    {
        public const string Id =
            "DiaBlackJack/QuickPlay/RunResult";

        public RunResultQuickPlayDropdown()
        {
            text = "Run Result";
            tooltip = "Preview victory or opponent-specific defeat dialogue.";
            clicked += ShowResultMenu;
        }

        private static void ShowResultMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Victory/No Contract"),
                false,
                () => SceneQuickPlayLauncher.PlayRunVictory(
                    hasMadeDemonContract: false));
            menu.AddItem(
                new GUIContent("Victory/Contracted"),
                false,
                () => SceneQuickPlayLauncher.PlayRunVictory(
                    hasMadeDemonContract: true));
            menu.AddSeparator(string.Empty);

            foreach (EnemyProfilePreview preview in
                EnemyCombatProfileCatalog.Default.Previews)
            {
                EnemyProfilePreview captured = preview;
                menu.AddItem(
                    new GUIContent(
                        $"Defeat/{captured.Grade}/{captured.DisplayName}"),
                    false,
                    () => SceneQuickPlayLauncher.PlayRunDefeat(
                        captured.ProfileKey));
            }

            menu.ShowAsContext();
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
        private const string RunResultMenuRoot =
            MenuRoot + "Run Result/";
        private const string RestorePendingKey =
            "DiaBlackJack.SceneQuickPlay.RestorePending";
        private const string PreviousStartScenePathKey =
            "DiaBlackJack.SceneQuickPlay.PreviousStartScenePath";
        private const string PendingEnemyProfileKey =
            "DiaBlackJack.SceneQuickPlay.PendingEnemyProfile";
        private const string PendingRunResultKey =
            "DiaBlackJack.SceneQuickPlay.PendingRunResult";
        private const string PendingRunResultScreenKey =
            "DiaBlackJack.SceneQuickPlay.PendingRunResultScreen";
        private const string PendingRunResultContractKey =
            "DiaBlackJack.SceneQuickPlay.PendingRunResultContract";
        private const string PendingRunResultOpponentKey =
            "DiaBlackJack.SceneQuickPlay.PendingRunResultOpponent";

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
            ClearPendingPreview();
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
            ClearPendingPreview();
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

            ClearPendingRunResult();
            SessionState.SetString(PendingEnemyProfileKey, profileKey);
            PlayFromScene(GameScenePath);
        }

        internal static void PlayRunVictory(bool hasMadeDemonContract)
        {
            PlayRunResult(
                GameFlowScreen.RunVictory,
                hasMadeDemonContract,
                EnemyCombatProfileCatalog.FinalBossKey);
        }

        internal static void PlayRunDefeat(string opponentProfileKey)
        {
            PlayRunResult(
                GameFlowScreen.RunDefeat,
                hasMadeDemonContract: false,
                opponentProfileKey);
        }

        [MenuItem(
            RunResultMenuRoot + "Victory/No Contract",
            false,
            20)]
        private static void PlayRunVictoryWithoutContract()
        {
            PlayRunVictory(hasMadeDemonContract: false);
        }

        [MenuItem(
            RunResultMenuRoot + "Victory/Contracted",
            false,
            21)]
        private static void PlayRunVictoryWithContract()
        {
            PlayRunVictory(hasMadeDemonContract: true);
        }

        [MenuItem(
            RunResultMenuRoot + "Defeat/Cowardly Gambler",
            false,
            30)]
        private static void PlayCowardlyGamblerDefeat()
        {
            PlayRunDefeat(EnemyCombatProfileCatalog.CowardlyGamblerKey);
        }

        [MenuItem(
            RunResultMenuRoot + "Defeat/Gunslinger",
            false,
            31)]
        private static void PlayGunslingerDefeat()
        {
            PlayRunDefeat(EnemyCombatProfileCatalog.GunslingerKey);
        }

        [MenuItem(
            RunResultMenuRoot + "Defeat/Cultist",
            false,
            32)]
        private static void PlayCultistDefeat()
        {
            PlayRunDefeat(EnemyCombatProfileCatalog.CultistKey);
        }

        [MenuItem(
            RunResultMenuRoot + "Defeat/Trickster",
            false,
            33)]
        private static void PlayTricksterDefeat()
        {
            PlayRunDefeat(EnemyCombatProfileCatalog.TricksterKey);
        }

        [MenuItem(
            RunResultMenuRoot + "Defeat/Enforcer",
            false,
            34)]
        private static void PlayEnforcerDefeat()
        {
            PlayRunDefeat(EnemyCombatProfileCatalog.EnforcerKey);
        }

        [MenuItem(
            RunResultMenuRoot + "Defeat/Final Boss",
            false,
            35)]
        private static void PlayFinalBossDefeat()
        {
            PlayRunDefeat(EnemyCombatProfileCatalog.FinalBossKey);
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
                ClearPendingPreview();
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
                ClearPendingPreview();
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
                if (SessionState.GetBool(PendingRunResultKey, false))
                {
                    EditorApplication.delayCall += StartPendingRunResult;
                }
                else if (!string.IsNullOrWhiteSpace(
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
                ClearPendingPreview();
            }
        }

        private static void PlayRunResult(
            GameFlowScreen screen,
            bool hasMadeDemonContract,
            string opponentProfileKey)
        {
            if (!CanEnterPlayMode() ||
                (screen != GameFlowScreen.RunVictory &&
                 screen != GameFlowScreen.RunDefeat) ||
                string.IsNullOrWhiteSpace(opponentProfileKey))
            {
                return;
            }

            try
            {
                EnemyCombatProfileCatalog.Default.GetByKey(opponentProfileKey);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ClearPendingPreview();
                return;
            }

            ClearPendingPreview();
            SessionState.SetBool(PendingRunResultKey, true);
            SessionState.SetInt(PendingRunResultScreenKey, (int)screen);
            SessionState.SetBool(
                PendingRunResultContractKey,
                hasMadeDemonContract);
            SessionState.SetString(
                PendingRunResultOpponentKey,
                opponentProfileKey);
            PlayFromScene(GameScenePath);
        }

        private static void StartPendingRunResult()
        {
            GameFlowScreen screen = (GameFlowScreen)SessionState.GetInt(
                PendingRunResultScreenKey,
                (int)GameFlowScreen.Unavailable);
            bool hasMadeDemonContract = SessionState.GetBool(
                PendingRunResultContractKey,
                false);
            string opponentProfileKey = SessionState.GetString(
                PendingRunResultOpponentKey,
                string.Empty);
            ClearPendingRunResult();

            GameFlowController controller =
                UnityEngine.Object.FindFirstObjectByType<GameFlowController>(
                    FindObjectsInactive.Include);
            if (controller == null ||
                !controller.DebugStartResultDialoguePreview(
                    screen,
                    hasMadeDemonContract,
                    opponentProfileKey))
            {
                Debug.LogError(
                    "Run Result Quick Play could not start " +
                    $"'{screen}' for '{opponentProfileKey}'.");
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

        private static void ClearPendingRunResult()
        {
            SessionState.EraseBool(PendingRunResultKey);
            SessionState.EraseInt(PendingRunResultScreenKey);
            SessionState.EraseBool(PendingRunResultContractKey);
            SessionState.EraseString(PendingRunResultOpponentKey);
        }

        private static void ClearPendingPreview()
        {
            ClearPendingEnemyBattle();
            ClearPendingRunResult();
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
