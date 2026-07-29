using System;
using Border.SaveLoad;
using Border.SaveLoad.UI;
using UnityEngine;

namespace DiaBlackJack.StageProgression.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StageProgressionView))]
    public sealed class StageProgressionController : MonoBehaviour
    {
        private StageProgressionRuntime _runtime;
        private FormalRunSession _formalSession;
        private StageProgressionView _view;
        private bool _inputLocked;
        private int? _focusedOpponentOfferId;
        private string _focusedOpponentProfileKey;

        public RunSaveViewModel CurrentSaveViewModel { get; private set; }

        public StageProgressionViewModel CurrentViewModel { get; private set; }

        private void Awake()
        {
            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException(
                    $"{nameof(StageProgressionView)} is required.");
            }

            _runtime = StageProgressionRuntime.Instance;
            if (_runtime == null ||
                _runtime.Session == null)
            {
                throw new MissingComponentException(
                    $"{nameof(StageProgressionRuntime)} is required.");
            }

            _view.StartRunRequested += RequestStartRun;
            _view.NextStageRequested += RequestNextStage;
            _view.RestartRunRequested += RequestOpenRunMenu;
            _view.BattleRewardSelected += RequestSelectBattleReward;
            _view.BattleRewardSkipped += RequestSkipBattleReward;
            _view.OpponentFocused += RequestFocusOpponent;
            _view.OpponentConfirmed += RequestConfirmOpponent;
            _view.NewRunRequested += RequestNewRun;
            _view.NewRunConfirmed += RequestConfirmNewRun;
            _view.NewRunCancelled += RequestCancelNewRun;
            _view.ContinueRunRequested += RequestContinueRun;
            _view.ResumeReservationRequested += RequestResumeReservation;
            _view.StartingDemonSelected += RequestSelectStartingDemon;
            _view.SaveRetryRequested += RequestRetrySave;
            _view.ShopCardPurchaseRequested += RequestBuyShopCard;
            _view.ShopCardRemovalRequested += RequestRemoveShopCard;
            _view.ShopRestRequested += RequestRestAtShop;
            _view.ShopLeaveRequested += RequestLeaveShop;

            _runtime.SaveFlow?.TryCheckpointRunEnd();
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
            _view.RestartRunRequested -= RequestOpenRunMenu;
            _view.BattleRewardSelected -= RequestSelectBattleReward;
            _view.BattleRewardSkipped -= RequestSkipBattleReward;
            _view.OpponentFocused -= RequestFocusOpponent;
            _view.OpponentConfirmed -= RequestConfirmOpponent;
            _view.NewRunRequested -= RequestNewRun;
            _view.NewRunConfirmed -= RequestConfirmNewRun;
            _view.NewRunCancelled -= RequestCancelNewRun;
            _view.ContinueRunRequested -= RequestContinueRun;
            _view.ResumeReservationRequested -= RequestResumeReservation;
            _view.StartingDemonSelected -= RequestSelectStartingDemon;
            _view.SaveRetryRequested -= RequestRetrySave;
            _view.ShopCardPurchaseRequested -= RequestBuyShopCard;
            _view.ShopCardRemovalRequested -= RequestRemoveShopCard;
            _view.ShopRestRequested -= RequestRestAtShop;
            _view.ShopLeaveRequested -= RequestLeaveShop;
        }

        public void RequestStartRun()
        {
            ProcessInput(TryStartRun);
        }

        public void RequestNextStage()
        {
            ProcessInput(TryAdvanceToNextStage);
        }

        public void RequestOpenRunMenu()
        {
            ProcessInput(TryOpenRunMenu);
        }

        public void RequestSelectBattleReward(int optionId)
        {
            ProcessRewardInput(
                () => TrySelectBattleReward(optionId));
        }

        public void RequestSkipBattleReward()
        {
            ProcessRewardInput(TrySkipBattleReward);
        }

        public void RequestNewRun()
        {
            ProcessMenuInput(() =>
                _runtime.SaveFlow != null &&
                _runtime.SaveFlow.TryRequestNewRun());
        }

        public void RequestConfirmNewRun()
        {
            ProcessMenuInput(() =>
                _runtime.SaveFlow != null &&
                _runtime.SaveFlow.TryConfirmNewRun());
        }

        public void RequestCancelNewRun()
        {
            ProcessMenuInput(() =>
                _runtime.SaveFlow != null &&
                _runtime.SaveFlow.TryCancelNewRun());
        }

        public void RequestContinueRun()
        {
            ProcessMenuInput(() =>
                _runtime.SaveFlow != null &&
                _runtime.SaveFlow.TryContinueRun());
        }

        public void RequestResumeReservation()
        {
            ProcessMenuInput(() =>
                _runtime.SaveFlow != null &&
                _runtime.SaveFlow.TryResumeReservation());
        }

        public void RequestSelectStartingDemon(int offerId, int optionId)
        {
            ProcessInput(
                () => TrySelectStartingDemon(offerId, optionId));
        }

        public void RequestRetrySave()
        {
            ProcessInput(() =>
                _runtime.SaveFlow != null &&
                _runtime.SaveFlow.TryRetryPendingCheckpoint());
        }

        public void RequestBuyShopCard(int offerId, int optionId)
        {
            ProcessShopInput(() =>
                _formalSession != null &&
                _formalSession.TryBuyShopCard(offerId, optionId));
        }

        public void RequestRemoveShopCard(int offerId, int cardId)
        {
            ProcessShopInput(() =>
                _formalSession != null &&
                _formalSession.TryRemoveShopCard(offerId, cardId));
        }

        public void RequestRestAtShop(int offerId)
        {
            ProcessShopInput(() =>
                _formalSession != null &&
                _formalSession.TryRestAtShop(offerId));
        }

        public void RequestLeaveShop(int offerId)
        {
            ProcessInput(() =>
                _formalSession != null &&
                _formalSession.TryLeaveShop(offerId));
        }

        public void RequestFocusOpponent(string profileKey)
        {
            if (_inputLocked ||
                CurrentSaveViewModel == null ||
                CurrentSaveViewModel.BlocksProgressionInput ||
                ActiveSession.Progress.State !=
                    StageProgressionState.OpponentSelection)
            {
                return;
            }

            StageProgressionViewModel requestedModel =
                _formalSession == null
                    ? StageProgressionPresenter.Create(
                        ActiveSession,
                        profileKey)
                    : StageProgressionPresenter.Create(
                        _formalSession,
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
            _view.Render(CurrentViewModel, CurrentSaveViewModel);
        }

        public void RequestConfirmOpponent()
        {
            if (_inputLocked ||
                CurrentSaveViewModel == null ||
                CurrentSaveViewModel.BlocksProgressionInput ||
                CurrentViewModel == null ||
                !CurrentViewModel.CanConfirmOpponent ||
                !CurrentViewModel.OpponentOfferId.HasValue)
            {
                return;
            }

            int offerId = CurrentViewModel.OpponentOfferId.Value;
            string profileKey = CurrentViewModel.FocusedOpponentProfileKey;
            ProcessInput(() =>
                _formalSession != null
                    ? _formalSession.TrySelectOpponent(offerId, profileKey)
                    : ActiveSession.TrySelectOpponent(offerId, profileKey));
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
                UnlockAndRefresh();
                return;
            }

            RouteAfterProgressionInput();
        }

        private void ProcessMenuInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            _view.SetInputLocked(true);
            action();
            UnlockAndRefresh();
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

        private void ProcessShopInput(Func<bool> action)
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
            _formalSession = _runtime.FormalSession;
            SynchronizeFocusedOpponent();
            CurrentViewModel = _formalSession == null
                ? StageProgressionPresenter.Create(
                    ActiveSession,
                    _focusedOpponentProfileKey)
                : StageProgressionPresenter.Create(
                    _formalSession,
                    _focusedOpponentProfileKey);
            CurrentSaveViewModel = _runtime.SaveFlow == null
                ? CreateStandaloneSaveViewModel()
                : RunSavePresenter.Create(_runtime.SaveFlow);
            _view.Render(CurrentViewModel, CurrentSaveViewModel);
        }

        private void RouteAfterProgressionInput()
        {
            if (ActiveSession.Progress.State ==
                    StageProgressionState.InBattle &&
                ActiveSession.Battle != null)
            {
                ClearFocusedOpponent();
                _runtime.LoadBattleScene();
                return;
            }

            UnlockAndRefresh();
        }

        private void UnlockAndRefresh()
        {
            _inputLocked = false;
            _view.SetInputLocked(false);
            RefreshView();
        }

        private void SynchronizeFocusedOpponent()
        {
            OpponentSelectionOffer offer =
                ActiveSession.PendingOpponentSelection;
            if ((_runtime.SaveFlow != null &&
                 _runtime.SaveFlow.IsMenuVisible) ||
                ActiveSession.Progress.State !=
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

        private bool TryStartRun()
        {
            if (_runtime.SaveFlow != null)
            {
                bool started = _runtime.SaveFlow.TryStartRun();
                _formalSession = _runtime.FormalSession;
                return started;
            }

            return _formalSession != null
                ? _formalSession.TryStartRun()
                : ActiveSession.TryStartRun();
        }

        private bool TryAdvanceToNextStage()
        {
            if (_formalSession != null)
            {
                return false;
            }

            return _runtime.SaveFlow == null
                ? ActiveSession.TryAdvanceToNextStage()
                : _runtime.SaveFlow.TryAdvanceToNextStage();
        }

        private bool TryOpenRunMenu()
        {
            return _runtime.SaveFlow == null
                ? _formalSession != null
                    ? _formalSession.TryRestartRun()
                    : ActiveSession.TryRestartRun()
                : _runtime.SaveFlow.TryOpenRunMenu();
        }

        private bool TrySelectBattleReward(int optionId)
        {
            return _runtime.SaveFlow == null
                ? ActiveSession.TrySelectBattleReward(optionId)
                : _runtime.SaveFlow.TrySelectBattleReward(optionId);
        }

        private bool TrySkipBattleReward()
        {
            return _runtime.SaveFlow == null
                ? ActiveSession.TrySkipBattleReward()
                : _runtime.SaveFlow.TrySkipBattleReward();
        }

        private bool TrySelectStartingDemon(int offerId, int optionId)
        {
            return _runtime.SaveFlow == null
                ? ActiveSession.TrySelectStartingDemon(offerId, optionId)
                : _runtime.SaveFlow.TrySelectStartingDemon(offerId, optionId);
        }

        private StageProgressionSession ActiveSession =>
            _formalSession?.CombatSession ?? _runtime.Session;

        private static RunSaveViewModel CreateStandaloneSaveViewModel()
        {
            return new RunSaveViewModel(
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                string.Empty,
                string.Empty);
        }
    }
}
