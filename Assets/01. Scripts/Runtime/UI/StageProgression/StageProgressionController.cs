using System;
using UnityEngine;

namespace DiaBlackJack.StageProgression.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StageProgressionView))]
    public sealed class StageProgressionController : MonoBehaviour
    {
        private StageProgressionRuntime _runtime;
        private StageProgressionView _view;
        private bool _inputLocked;

        public StageProgressionViewModel CurrentViewModel { get; private set; }

        private void Awake()
        {
            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException($"{nameof(StageProgressionView)} is required.");
            }

            _runtime = StageProgressionRuntime.Instance;
            if (_runtime == null || _runtime.Session == null)
            {
                throw new MissingComponentException($"{nameof(StageProgressionRuntime)} is required.");
            }

            _view.StartRunRequested += RequestStartRun;
            _view.NextStageRequested += RequestNextStage;
            _view.RestartRunRequested += RequestRestartRun;
            _view.BattleRewardSelected += RequestSelectBattleReward;
            _view.BattleRewardSkipped += RequestSkipBattleReward;
            RefreshView();
        }

        private void OnDestroy()
        {
            if (_view == null)
            {
                return;
            }

            _view.StartRunRequested -= RequestStartRun;
            _view.NextStageRequested -= RequestNextStage;
            _view.RestartRunRequested -= RequestRestartRun;
            _view.BattleRewardSelected -= RequestSelectBattleReward;
            _view.BattleRewardSkipped -= RequestSkipBattleReward;
        }

        public void RequestStartRun()
        {
            ProcessInput(_runtime.Session.TryStartRun);
        }

        public void RequestNextStage()
        {
            ProcessInput(_runtime.Session.TryAdvanceToNextStage);
        }

        public void RequestRestartRun()
        {
            ProcessInput(_runtime.Session.TryRestartRun);
        }

        public void RequestSelectBattleReward(int optionId)
        {
            ProcessRewardInput(() => _runtime.Session.TrySelectBattleReward(optionId));
        }

        public void RequestSkipBattleReward()
        {
            ProcessRewardInput(_runtime.Session.TrySkipBattleReward);
        }

        private void ProcessInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            _view.SetInputLocked(true);
            if (!action())
            {
                _inputLocked = false;
                _view.SetInputLocked(false);
                RefreshView();
                return;
            }

            _runtime.LoadBattleScene();
        }

        private void ProcessRewardInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            _view.SetInputLocked(true);
            try
            {
                action();
                RefreshView();
            }
            finally
            {
                _inputLocked = false;
                _view.SetInputLocked(false);
            }
        }

        private void RefreshView()
        {
            CurrentViewModel = StageProgressionPresenter.Create(_runtime.Session.Progress);
            _view.Render(CurrentViewModel);
        }
    }
}
