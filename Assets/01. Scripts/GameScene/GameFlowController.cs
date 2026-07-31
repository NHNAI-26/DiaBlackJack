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
        [SerializeField] private StartingDemonRevealView startingDemonReveal;
        [SerializeField] private OpponentSelectionView opponentSelection;
        [SerializeField] private RunResultView resultView;
        [SerializeField] private CodexController codex;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameHudView hud;
        [SerializeField] private GameObject charactersRoot;
        [SerializeField] private CharacterView enemyCharacter;

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
            startingDemonReveal ??= GetComponent<StartingDemonRevealView>();
            opponentSelection ??= GetComponent<OpponentSelectionView>();
            resultView ??= GetComponent<RunResultView>();
            resultView ??= gameObject.AddComponent<RunResultView>();
            codex ??= GetComponent<CodexController>();
            ResolveSceneReferences();

            _runtime = StageProgressionRuntime.Instance;
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.FormalBattleCompleted += HandleBattleCompleted;
                gameManager.FormalShopCardPurchaseRequested +=
                    HandleFormalShopCardPurchaseRequested;
                gameManager.FormalShopCardRemovalRequested +=
                    HandleFormalShopCardRemovalRequested;
                gameManager.FormalShopRestRequested +=
                    HandleFormalShopRestRequested;
                gameManager.FormalShopLeaveRequested +=
                    HandleFormalShopLeaveRequested;
            }

            if (startingDemonReveal != null)
            {
                startingDemonReveal.ConfirmationRequested +=
                    HandleStartingDemonConfirmationRequested;
            }

            if (opponentSelection != null)
            {
                opponentSelection.OpponentFocused +=
                    HandleOpponentFocused;
                opponentSelection.OpponentConfirmed +=
                    HandleOpponentConfirmed;
            }

            if (resultView != null)
            {
                resultView.RestartRequested += HandleRestartRequested;
                resultView.MainMenuRequested += HandleMainMenuRequested;
                resultView.SaveRetryRequested += HandleSaveRetryRequested;
            }
        }

        private void Start()
        {
            ResolveSceneReferences();
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
                gameManager.FormalShopCardPurchaseRequested -=
                    HandleFormalShopCardPurchaseRequested;
                gameManager.FormalShopCardRemovalRequested -=
                    HandleFormalShopCardRemovalRequested;
                gameManager.FormalShopRestRequested -=
                    HandleFormalShopRestRequested;
                gameManager.FormalShopLeaveRequested -=
                    HandleFormalShopLeaveRequested;
            }

            if (startingDemonReveal != null)
            {
                startingDemonReveal.ConfirmationRequested -=
                    HandleStartingDemonConfirmationRequested;
            }

            if (opponentSelection != null)
            {
                opponentSelection.OpponentFocused -=
                    HandleOpponentFocused;
                opponentSelection.OpponentConfirmed -=
                    HandleOpponentConfirmed;
            }

            if (resultView != null)
            {
                resultView.RestartRequested -= HandleRestartRequested;
                resultView.MainMenuRequested -= HandleMainMenuRequested;
                resultView.SaveRetryRequested -= HandleSaveRetryRequested;
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

        public bool RequestRestartRun()
        {
            if (!IsTerminalScreen() || IsInputBlocked())
            {
                return false;
            }

            RunSaveFlow flow = _runtime.SaveFlow;
            if (!flow.TryOpenRunMenu() || !flow.TryRequestNewRun())
            {
                return false;
            }

            if (flow.RequiresNewRunConfirmation && !flow.TryConfirmNewRun())
            {
                return false;
            }

            _session = null;
            ClearFocusedOpponent();
            if (!TryAdoptFormalRun())
            {
                return false;
            }

            RefreshFlow();
            return true;
        }

        public bool RequestReturnToMainMenu()
        {
            if (!IsTerminalScreen() || IsInputBlocked() ||
                !_runtime.SaveFlow.TryOpenRunMenu())
            {
                return false;
            }

            _runtime.LoadMainMenuScene();
            return true;
        }

        public bool RequestRetrySave()
        {
            if (!IsTerminalScreen() ||
                _runtime == null ||
                _runtime.SaveFlow == null ||
                !_runtime.SaveFlow.HasPendingCheckpoint)
            {
                return false;
            }

            bool accepted = _runtime.SaveFlow.TryRetryPendingCheckpoint();
            RefreshFlow();
            return accepted;
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

        private void HandleStartingDemonConfirmationRequested()
        {
            RequestCompleteStartingDemonReveal();
        }

        private void HandleOpponentFocused(string profileKey)
        {
            RequestFocusOpponent(profileKey);
            opponentSelection?.Render(CurrentViewModel);
        }

        private void HandleOpponentConfirmed()
        {
            RequestConfirmOpponent();
        }

        private void HandleFormalShopCardPurchaseRequested(int optionId)
        {
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                RequestBuyShopCard(offerId, optionId);
            }
        }

        private void HandleFormalShopCardRemovalRequested(int cardId)
        {
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                RequestRemoveShopCard(offerId, cardId);
            }
        }

        private void HandleFormalShopRestRequested()
        {
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                RequestRestAtShop(offerId);
            }
        }

        private void HandleFormalShopLeaveRequested()
        {
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                RequestLeaveShop(offerId);
            }
        }

        private void HandleRestartRequested()
        {
            RequestRestartRun();
        }

        private void HandleMainMenuRequested()
        {
            RequestReturnToMainMenu();
        }

        private void HandleSaveRetryRequested()
        {
            RequestRetrySave();
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
                gameManager.UnbindFormalShop();
                gameManager.enabled = true;
                if (!gameManager.BindBattle(_session.CombatSession))
                {
                    throw new InvalidOperationException(
                        "The active formal battle could not be bound.");
                }
            }
            else if (nextScreen == GameFlowScreen.Shop)
            {
                gameManager.UnbindBattle();
                gameManager.enabled = true;
                if (!gameManager.BindFormalShop(
                        CurrentViewModel,
                        _session.CombatSession.Progress.Player.CurrentGold))
                {
                    throw new InvalidOperationException(
                        "The active formal shop could not be bound.");
                }
            }
            else
            {
                gameManager.UnbindFormalShop();
                gameManager.UnbindBattle();
                gameManager.enabled = false;
            }

            RenderFlowScreen();
            ScreenChanged?.Invoke(CurrentScreen, CurrentViewModel);
        }

        private void RenderFlowScreen()
        {
            ResolveSceneReferences();
            bool isStartingReveal =
                CurrentScreen == GameFlowScreen.StartingDemonReveal;
            bool isOpponentSelection =
                CurrentScreen == GameFlowScreen.OpponentSelection;
            bool isCombat = CurrentScreen == GameFlowScreen.Combat;
            bool isShop = CurrentScreen == GameFlowScreen.Shop;
            bool isResult =
                CurrentScreen == GameFlowScreen.RunVictory ||
                CurrentScreen == GameFlowScreen.RunDefeat;
            codex?.SetAvailable(isCombat || isShop);
            hud?.SetEnemyStatusVisible(isCombat);

            if (isStartingReveal &&
                CurrentViewModel.StartingDemonGrantId.HasValue)
            {
                startingDemonReveal?.Render(
                    CurrentViewModel.StartingDemonGrantId.Value,
                    CurrentViewModel.StartingDemonGrantCards);
            }
            else
            {
                startingDemonReveal?.Hide();
            }

            if (isOpponentSelection)
            {
                opponentSelection?.Render(CurrentViewModel);
            }
            else
            {
                opponentSelection?.Hide();
            }

            if (isResult)
            {
                resultView?.Render(RunResultPresenter.Create(
                    CurrentScreen,
                    CurrentViewModel,
                    RunSavePresenter.Create(_runtime.SaveFlow)));
            }
            else
            {
                resultView?.Hide();
            }

            if (hudRoot != null)
            {
                hudRoot.SetActive(isCombat || isShop);
            }

            if (charactersRoot != null)
            {
                charactersRoot.SetActive(
                    isCombat || isStartingReveal ||
                    CurrentScreen == GameFlowScreen.Shop);
            }

            if (enemyCharacter != null)
            {
                if (isStartingReveal ||
                    CurrentScreen == GameFlowScreen.Shop)
                {
                    enemyCharacter.EnterMerchant();
                }
                else if (isCombat)
                {
                    enemyCharacter.ExitMerchant();
                }
            }
        }

        private void ResolveSceneReferences()
        {
            if (hudRoot == null)
            {
                hudRoot = GameObject.Find("UIHUD");
            }

            if (hud == null && hudRoot != null)
            {
                hud = hudRoot.GetComponentInChildren<GameHudView>(true);
            }

            if (hudRoot == null && hud != null)
            {
                hudRoot = hud.gameObject;
            }

            hud?.SetEnemyStatusVisible(false);
            if (charactersRoot == null)
            {
                charactersRoot = GameObject.Find("Characters");
            }

            if (enemyCharacter == null && charactersRoot != null)
            {
                Transform enemy = charactersRoot.transform.Find(
                    "EnemyCharacter");
                enemyCharacter = enemy == null
                    ? null
                    : enemy.GetComponent<CharacterView>();
            }
        }

        private bool IsTerminalScreen()
        {
            return CurrentScreen == GameFlowScreen.RunVictory ||
                CurrentScreen == GameFlowScreen.RunDefeat;
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
