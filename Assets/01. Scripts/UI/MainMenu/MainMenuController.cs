using Border.SaveLoad;
using Border.SaveLoad.UI;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.MainMenu.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MainMenuView))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private MainMenuView _view;
        private StageProgressionRuntime _runtime;

        private void Awake()
        {
            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException(
                    $"{nameof(MainMenuView)} is required.");
            }

            _runtime = StageProgressionRuntime.Instance;
            if (_runtime == null || _runtime.SaveFlow == null)
            {
                throw new MissingComponentException(
                    $"{nameof(StageProgressionRuntime)} is required.");
            }

            _view.NewRunRequested += RequestNewRun;
            _view.NewRunConfirmed += RequestConfirmNewRun;
            _view.CancelNewRunRequested += RequestCancelNewRun;
            _view.ContinueRunRequested += RequestContinueRun;
            _view.ResumeReservationRequested += RequestResumeReservation;
            _view.ExitRequested += RequestExit;
            _view.TutorialRequested += RequestTutorial;
            RefreshView();
        }

        private void OnDestroy()
        {
            if (_view == null)
            {
                return;
            }

            _view.NewRunRequested -= RequestNewRun;
            _view.NewRunConfirmed -= RequestConfirmNewRun;
            _view.CancelNewRunRequested -= RequestCancelNewRun;
            _view.ContinueRunRequested -= RequestContinueRun;
            _view.ResumeReservationRequested -= RequestResumeReservation;
            _view.ExitRequested -= RequestExit;
            _view.TutorialRequested -= RequestTutorial;
        }

        public void RequestNewRun()
        {
            ProcessMenuAction(_runtime.SaveFlow.TryRequestNewRun);
        }

        public void RequestConfirmNewRun()
        {
            ProcessMenuAction(_runtime.SaveFlow.TryConfirmNewRun);
        }

        public void RequestCancelNewRun()
        {
            ProcessMenuAction(_runtime.SaveFlow.TryCancelNewRun);
        }

        public void RequestContinueRun()
        {
            ProcessMenuAction(_runtime.SaveFlow.TryContinueRun);
        }

        public void RequestResumeReservation()
        {
            ProcessMenuAction(_runtime.SaveFlow.TryResumeReservation);
        }

        public void RequestExit()
        {
            Application.Quit();
        }

        public void RequestTutorial()
        {
            StageProgressionRuntime tutorialRuntime =
                StageProgressionRuntime.CreateTutorialInstance();
            RunSaveFlow flow = tutorialRuntime.SaveFlow;
            if (!flow.TryRequestNewRun())
            {
                return;
            }

            if (flow.RequiresNewRunConfirmation && !flow.TryConfirmNewRun())
            {
                return;
            }

            if (!flow.IsMenuVisible)
            {
                tutorialRuntime.LoadBattleScene();
            }
        }

        private void ProcessMenuAction(System.Func<bool> action)
        {
            if (!action())
            {
                RefreshView();
                return;
            }

            RunSaveFlow flow = _runtime.SaveFlow;
            if (!flow.IsMenuVisible)
            {
                _runtime.LoadBattleScene();
                return;
            }

            RefreshView();
        }

        private void RefreshView()
        {
            RunSaveViewModel model =
                RunSavePresenter.Create(_runtime.SaveFlow);
            _view.Render(model);
        }
    }
}
