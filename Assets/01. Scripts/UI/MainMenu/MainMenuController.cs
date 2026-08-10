using System;
using Border.SaveLoad;
using Border.SaveLoad.UI;
using Border.Settings;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.MainMenu.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MainMenuView))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string MainMenuMoodId = "mainMenu";
        private const string MainMenuCanvasName = "MainMenuCanvas";

        [SerializeField] private MoodController moodController;
        [SerializeField] private PauseSettingsController settingsController;

        private MainMenuView _view;
        private StageProgressionRuntime _runtime;
        private bool _startupNoticePrepared;
        private bool _transitioning;

        private void Awake()
        {
            Canvas mainMenuCanvas =
                UIOverlayCanvasCameraUtility.FindCanvasInScene(
                    gameObject.scene,
                    MainMenuCanvasName);
            UIOverlayCanvasCameraUtility.TryConfigure(mainMenuCanvas);

            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException(
                    $"{nameof(MainMenuView)} is required.");
            }

            _startupNoticePrepared = _view.PrepareStartupNotice(
                !MainMenuStartupNoticeGate.HasShown);
            if (_startupNoticePrepared)
            {
                _view.SetInputEnabled(false);
            }

            _runtime = StageProgressionRuntime.Instance;
            if (_runtime == null || _runtime.SaveFlow == null)
            {
                throw new MissingComponentException(
                    $"{nameof(StageProgressionRuntime)} is required.");
            }

            moodController ??= FindFirstObjectByType<MoodController>(
                FindObjectsInactive.Include);
            settingsController ??=
                FindFirstObjectByType<PauseSettingsController>(
                    FindObjectsInactive.Include);

            _view.NewRunRequested += RequestNewRun;
            _view.SettingsRequested += RequestSettings;
            _view.TutorialRequested += RequestTutorial;
            if (settingsController != null)
            {
                settingsController.SettingsPanelClosed +=
                    HandleSettingsPanelClosed;
            }

            RefreshView();
        }

        private void Start()
        {
            if (_startupNoticePrepared &&
                MainMenuStartupNoticeGate.TryClaim() &&
                _view.TryPlayStartupNotice(
                    StartMainMenuPresentation))
            {
                return;
            }

            _view.HideStartupNotice();
            StartMainMenuPresentation();
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.NewRunRequested -= RequestNewRun;
                _view.SettingsRequested -= RequestSettings;
                _view.TutorialRequested -= RequestTutorial;
            }

            if (settingsController != null)
            {
                settingsController.SettingsPanelClosed -=
                    HandleSettingsPanelClosed;
            }
        }

        public void RequestNewRun()
        {
            if (_transitioning)
            {
                return;
            }

            RunSaveFlow flow = _runtime.SaveFlow;
            if (!TryStartNewRunImmediately(flow) || flow.IsMenuVisible)
            {
                RefreshView(showRuntimeStatus: true);
                return;
            }

            BeginSceneTransition(_runtime.LoadBattleScene);
        }

        public void RequestTutorial()
        {
            if (_transitioning)
            {
                return;
            }

            StageProgressionRuntime tutorialRuntime =
                StageProgressionRuntime.CreateTutorialInstance();
            RunSaveFlow flow = tutorialRuntime.SaveFlow;
            if (!TryStartNewRunImmediately(flow) || flow.IsMenuVisible)
            {
                RefreshView(showRuntimeStatus: true);
                return;
            }

            BeginSceneTransition(tutorialRuntime.LoadBattleScene);
        }

        public void RequestSettings()
        {
            if (_transitioning || settingsController == null)
            {
                return;
            }

            _view.SetInputEnabled(false);
            if (!settingsController.OpenSettingsPanel())
            {
                _view.SetInputEnabled(true);
            }
        }

        internal static bool TryStartNewRunImmediately(RunSaveFlow flow)
        {
            if (flow == null || !flow.TryRequestNewRun())
            {
                return false;
            }

            return !flow.RequiresNewRunConfirmation ||
                flow.TryConfirmNewRun();
        }

        private void BeginSceneTransition(Action loadScene)
        {
            _transitioning = true;
            _view.PlayExitAnimation(loadScene);
        }

        private void HandleSettingsPanelClosed()
        {
            if (!_transitioning)
            {
                _view.SetInputEnabled(true);
            }
        }

        private void StartMainMenuPresentation()
        {
            bool moodPrepared = moodController != null &&
                moodController.TryBlendToMoodWithoutDoorOpenSfx(
                    MainMenuMoodId,
                    0f);
            if (moodPrepared)
            {
                moodController.PlayPendingBgm();
            }

            _view.PlayEntranceAnimation();
            if (!_transitioning)
            {
                _view.SetInputEnabled(true);
            }
        }

        private void RefreshView(bool showRuntimeStatus = false)
        {
            RunSaveViewModel model =
                RunSavePresenter.Create(_runtime.SaveFlow);
            _view.Render(model, showRuntimeStatus);
        }
    }

    internal static class MainMenuStartupNoticeGate
    {
        private static bool _shown;

        internal static bool HasShown => _shown;

        internal static bool TryClaim()
        {
            if (_shown)
            {
                return false;
            }

            _shown = true;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            _shown = false;
        }

        internal static void ResetForTests()
        {
            _shown = false;
        }
    }
}
