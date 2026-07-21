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
        private int? _focusedOpponentOfferId;
        private string _focusedOpponentProfileKey;

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
            _view.OpponentFocused += RequestFocusOpponent;
            _view.OpponentConfirmed += RequestConfirmOpponent;
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
            _view.OpponentFocused -= RequestFocusOpponent;
            _view.OpponentConfirmed -= RequestConfirmOpponent;
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

        public void RequestFocusOpponent(string profileKey)
        {
            if (_inputLocked ||
                _runtime.Session.Progress.State !=
                    StageProgressionState.OpponentSelection)
            {
                return;
            }

            StageProgressionViewModel requestedModel = StageProgressionPresenter.Create(
                _runtime.Session,
                profileKey);
            if (!StringComparer.Ordinal.Equals(
                requestedModel.FocusedOpponentProfileKey,
                profileKey))
            {
                return;
            }

            _focusedOpponentOfferId = requestedModel.OpponentOfferId;
            _focusedOpponentProfileKey = profileKey;
            CurrentViewModel = requestedModel;
            _view.Render(CurrentViewModel);
        }

        public void RequestConfirmOpponent()
        {
            if (_inputLocked ||
                CurrentViewModel == null ||
                !CurrentViewModel.CanConfirmOpponent ||
                !CurrentViewModel.OpponentOfferId.HasValue)
            {
                return;
            }

            int offerId = CurrentViewModel.OpponentOfferId.Value;
            string profileKey = CurrentViewModel.FocusedOpponentProfileKey;
            ProcessInput(() =>
                _runtime.Session.TrySelectOpponent(offerId, profileKey));
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

            RouteAfterProgressionInput();
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
            SynchronizeFocusedOpponent();
            CurrentViewModel = StageProgressionPresenter.Create(
                _runtime.Session,
                _focusedOpponentProfileKey);
            _view.Render(CurrentViewModel);
        }

        private void RouteAfterProgressionInput()
        {
            if (_runtime.Session.Progress.State == StageProgressionState.InBattle &&
                _runtime.Session.Battle != null)
            {
                ClearFocusedOpponent();
                _runtime.LoadBattleScene();
                return;
            }

            _inputLocked = false;
            _view.SetInputLocked(false);
            RefreshView();
        }

        private void SynchronizeFocusedOpponent()
        {
            OpponentSelectionOffer offer = _runtime.Session.PendingOpponentSelection;
            if (_runtime.Session.Progress.State !=
                    StageProgressionState.OpponentSelection ||
                offer == null)
            {
                ClearFocusedOpponent();
                return;
            }

            if (_focusedOpponentOfferId != offer.OfferId)
            {
                _focusedOpponentOfferId = offer.OfferId;
                _focusedOpponentProfileKey = null;
            }
        }

        private void ClearFocusedOpponent()
        {
            _focusedOpponentOfferId = null;
            _focusedOpponentProfileKey = null;
        }
    }
}
