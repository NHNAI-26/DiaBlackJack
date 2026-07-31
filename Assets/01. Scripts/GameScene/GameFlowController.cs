using System;
using Border.SaveLoad;
using Border.SaveLoad.UI;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameManager))]
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private StageProgressionRuntime _runtime;
        private FormalRunSession _session;
        private int? _focusedOpponentOfferId;
        private string _focusedOpponentProfileKey;
        private bool _isProcessingInput;

        public event Action<GameFlowScreen, StageProgressionViewModel>
            ScreenChanged;

        public GameFlowScreen CurrentScreen { get; private set; } =
            GameFlowScreen.Unavailable;

        public StageProgressionViewModel CurrentViewModel { get; private set; }

        private void Awake()
        {
            gameManager ??= GetComponent<GameManager>();
            _runtime = StageProgressionRuntime.Instance;
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.FormalBattleCompleted += HandleBattleCompleted;
            }
        }

        private void Start()
        {
            if (!TryAdoptFormalRun())
            {
                return;
            }

            RefreshFlow();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.FormalBattleCompleted -= HandleBattleCompleted;
            }
        }

        public bool RequestCompleteStartingDemonReveal()
        {
            return ProcessInput(() =>
                _runtime.SaveFlow.TryCompleteStartingDemonReveal());
        }

        public bool RequestFocusOpponent(string profileKey)
        {
            if (IsInputBlocked() ||
                CurrentScreen != GameFlowScreen.OpponentSelection)
            {
                return false;
            }

            StageProgressionViewModel requested =
                StageProgressionPresenter.Create(_session, profileKey);
            if (!StringComparer.Ordinal.Equals(
                    requested.FocusedOpponentProfileKey,
                    profileKey))
            {
                return false;
            }

            _focusedOpponentOfferId = requested.OpponentOfferId;
            _focusedOpponentProfileKey = profileKey;
            CurrentViewModel = requested;
            ScreenChanged?.Invoke(CurrentScreen, CurrentViewModel);
            return true;
        }

        public bool RequestConfirmOpponent()
        {
            if (CurrentViewModel == null ||
                !CurrentViewModel.CanConfirmOpponent ||
                !CurrentViewModel.OpponentOfferId.HasValue)
            {
                return false;
            }

            int offerId = CurrentViewModel.OpponentOfferId.Value;
            string profileKey = CurrentViewModel.FocusedOpponentProfileKey;
            return ProcessInput(() =>
                _session.TrySelectOpponent(offerId, profileKey));
        }

        public bool RequestBuyShopCard(int offerId, int optionId)
        {
            return ProcessInput(
                () => _session.TryBuyShopCard(offerId, optionId));
        }

        public bool RequestRemoveShopCard(int offerId, int cardId)
        {
            return ProcessInput(
                () => _session.TryRemoveShopCard(offerId, cardId));
        }

        public bool RequestRestAtShop(int offerId)
        {
            return ProcessInput(() => _session.TryRestAtShop(offerId));
        }

        public bool RequestLeaveShop(int offerId)
        {
            return ProcessInput(() => _session.TryLeaveShop(offerId));
        }

        private bool TryAdoptFormalRun()
        {
            if (_runtime == null ||
                _runtime.SaveFlow == null ||
                _runtime.SaveFlow.IsMenuVisible)
            {
                return false;
            }

            _session = _runtime.FormalSession;
            return _session != null;
        }

        private bool ProcessInput(Func<bool> action)
        {
            if (action == null || IsInputBlocked())
            {
                return false;
            }

            _isProcessingInput = true;
            bool accepted;
            try
            {
                accepted = action();
            }
            finally
            {
                _isProcessingInput = false;
            }

            RefreshFlow();
            return accepted;
        }

        private bool IsInputBlocked()
        {
            if (_isProcessingInput ||
                _runtime == null ||
                _runtime.SaveFlow == null)
            {
                return true;
            }

            RunSaveViewModel save =
                RunSavePresenter.Create(_runtime.SaveFlow);
            return save.BlocksProgressionInput;
        }

        private void HandleBattleCompleted()
        {
            RefreshFlow();
        }

        private void RefreshFlow()
        {
            if (_session == null && !TryAdoptFormalRun())
            {
                return;
            }

            SynchronizeFocusedOpponent();
            GameFlowScreen nextScreen =
                GameFlowScreenResolver.Resolve(_session);
            CurrentViewModel = StageProgressionPresenter.Create(
                _session,
                _focusedOpponentProfileKey);
            CurrentScreen = nextScreen;

            if (nextScreen == GameFlowScreen.Combat)
            {
                gameManager.enabled = true;
                if (!gameManager.BindBattle(_session.CombatSession))
                {
                    throw new InvalidOperationException(
                        "The active formal battle could not be bound.");
                }
            }
            else
            {
                gameManager.UnbindBattle();
                gameManager.enabled = false;
            }

            ScreenChanged?.Invoke(CurrentScreen, CurrentViewModel);
        }

        private void SynchronizeFocusedOpponent()
        {
            OpponentSelectionOffer offer =
                _session.CombatSession.PendingOpponentSelection;
            if (offer == null)
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
