using System;
using System.Collections;
using Border.SaveLoad;
using Border.SaveLoad.UI;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
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
        [SerializeField] private MoodController moodController;
        [SerializeField] private float moodTransitionDuration = 1f;
        [SerializeField, Min(0f)] private float enemyAppearanceDelayAfterDoor = 1f;
        [Tooltip("Hold after the enemy entrance animation finishes, before round 1 begins (HUD activates, battle binds, initial cards deal).")]
        [SerializeField, Min(0f)] private float roundOneStartDelayAfterEntrance = 3f;

        [Header("Merchant speech")]
        [SerializeField] private SpeechProfileSO merchantSpeechProfile;
        [SerializeField] private int merchantSpeechSeed = 20260804;

        [Header("Run result dialogue")]
        [SerializeField] private RunResultDialogueSO runResultDialogue;

        private StageProgressionRuntime _runtime;
        private FormalRunSession _session;
        private SpeechLineResolver _speechResolver;
        private Coroutine _enemyAppearanceDelayRoutine;
        private int? _focusedOpponentOfferId;
        private string _focusedOpponentProfileKey;
        private string _acknowledgedFinalBossStageId;
        private bool _waitingForRoundOneReveal;
        private bool _isProcessingInput;
        private bool _charactersEntranceWaiting;
        private bool _pendingHideAfterDoorAnimation;
        private bool _hasPresentedCharacters;
        private bool _characterEntranceInProgress;
        private int _characterEntranceRequestId;
        private bool _playCharacterExitBeforeEntrance;
        private bool _characterExitWaitingForEntrance;
        private bool _merchantTransitionWaitingForEnemyExit;
        private bool _unlockInputAfterCharacterEntrance;
        private Coroutine _characterEntranceUnlockSafetyRoutine;
        private Coroutine _roundOneStartRoutine;
        private string _currentMoodId;
        private RunResultDialogueSequence _resultDialogueSequence;
        private float _resultDialogueCharactersPerSecond;
        private bool _resultDialoguePending;
        private RunResultTransitionView _resultTransition;
#if UNITY_EDITOR
        private bool _isResultDialoguePreview;
#endif

        // The entrance-animation completion callback that unlocks input
        // (CompleteCharacterEntrance) depends on a chain of several animation
        // events firing in order (mood door animation, entrance-delay wait,
        // exit-before-entrance, the entrance tween itself). If any link in that
        // chain fails to fire — e.g. an interrupted tween, a re-entrant
        // RefreshFlow call resetting the wait flag before the original entrance
        // completes — input would otherwise stay locked forever. This is a
        // generous upper bound on how long the whole chain should ever take.
        private const float CharacterEntranceUnlockSafetySeconds = 6f;

        public event Action<GameFlowScreen, StageProgressionViewModel>
            ScreenChanged;

        public GameFlowScreen CurrentScreen { get; private set; } =
            GameFlowScreen.Unavailable;

        public StageProgressionViewModel CurrentViewModel { get; private set; }

        private void Awake()
        {
            gameManager ??= GetComponent<GameManager>();
            startingDemonReveal ??= GetComponent<StartingDemonRevealView>();
            opponentSelection ??= FindFirstObjectByType<OpponentSelectionView>(
                FindObjectsInactive.Include);
            resultView ??= GetComponent<RunResultView>();
            resultView ??= gameObject.AddComponent<RunResultView>();
            codex ??= GetComponent<CodexController>();
            moodController ??= GetComponent<MoodController>();
            _resultTransition ??= GetComponent<RunResultTransitionView>();
            _resultTransition ??=
                gameObject.AddComponent<RunResultTransitionView>();
            ResolveSceneReferences();
            SubscribeToMoodController();

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
                opponentSelection.OpponentSelected +=
                    HandleOpponentSelected;
            }

            if (resultView != null)
            {
                resultView.RestartRequested += HandleRestartRequested;
                resultView.MainMenuRequested += HandleMainMenuRequested;
                resultView.SaveRetryRequested += HandleSaveRetryRequested;
            }

            moodController ??= GetComponent<MoodController>();
            SubscribeToMoodController();
        }

        private void Start()
        {
            ResolveSceneReferences();
            SubscribeToMoodController();
            if (!TryAdoptFormalRun())
            {
                ApplyMood(
                    GameFlowScreen.Combat,
                    gameManager?.CurrentEnemyProfileKey);
                return;
            }

            RefreshFlow();
        }

        /// <summary>
        /// A freshly code-spawned <see cref="StageProgressionRuntime"/> (e.g. the
        /// tutorial's throwaway, in-memory-backed instance created just before this scene
        /// loads) can occasionally still be settling relative to this object's own
        /// <see cref="Start"/> — if <see cref="TryAdoptFormalRun"/> loses that race, nothing
        /// else ever retries it, and the whole screen stays permanently stuck (no combat
        /// transition, no entrance animation, no dialogue). Retry every frame until it
        /// succeeds; a no-op once <c>_session</c> is set.
        /// </summary>
        private void Update()
        {
            if (_session == null && TryAdoptFormalRun())
            {
                RefreshFlow();
            }

            HandleResultDialogueInput();
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
                opponentSelection.OpponentSelected -=
                    HandleOpponentSelected;
            }

            if (resultView != null)
            {
                resultView.RestartRequested -= HandleRestartRequested;
                resultView.MainMenuRequested -= HandleMainMenuRequested;
                resultView.SaveRetryRequested -= HandleSaveRetryRequested;
            }

            UnsubscribeFromMoodController();
            CancelEnemyAppearanceDelay();
            StopCharacterEntranceUnlockSafety();
            StopRoundOneStartRoutine();
            _merchantTransitionWaitingForEnemyExit = false;
            _unlockInputAfterCharacterEntrance = false;
            _waitingForRoundOneReveal = false;
            ResetResultDialogue();
        }

        public bool RequestCompleteStartingDemonReveal()
        {
            return ProcessInput(() =>
                _runtime.SaveFlow.TryCompleteStartingDemonReveal());
        }

        public bool RequestFocusOpponent(string profileKey)
        {
            if (IsInputBlocked() ||
                CurrentScreen != GameFlowScreen.OpponentSelection ||
                !IsOpponentSelectionReady())
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
            if (IsInputBlocked() ||
                CurrentScreen != GameFlowScreen.OpponentSelection ||
                !IsOpponentSelectionReady() ||
                CurrentViewModel == null ||
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

        public bool RequestSelectOpponent(string profileKey)
        {
            return TrySelectOpponent(
                profileKey,
                requireReadyView: true);
        }

        private bool TrySelectOpponent(
            string profileKey,
            bool requireReadyView)
        {
            if (string.IsNullOrWhiteSpace(profileKey) ||
                IsInputBlocked() ||
                CurrentScreen != GameFlowScreen.OpponentSelection ||
                (requireReadyView && !IsOpponentSelectionReady()) ||
                CurrentViewModel == null ||
                !CurrentViewModel.OpponentOfferId.HasValue)
            {
                return false;
            }

            bool isOffered = false;
            foreach (OpponentCandidateViewModel candidate in
                CurrentViewModel.OpponentCandidates)
            {
                if (StringComparer.Ordinal.Equals(
                        candidate.ProfileKey,
                        profileKey))
                {
                    isOffered = true;
                    break;
                }
            }

            if (!isOffered)
            {
                return false;
            }

            int offerId = CurrentViewModel.OpponentOfferId.Value;
            return ProcessInput(() =>
                _session.TrySelectOpponent(offerId, profileKey));
        }

        private bool IsOpponentSelectionReady()
        {
            return opponentSelection == null ||
                opponentSelection.IsReadyForSelection;
        }

        public bool RequestAcknowledgeFinalBossReveal()
        {
            if (IsInputBlocked() ||
                CurrentScreen != GameFlowScreen.FinalBossReveal)
            {
                return false;
            }

            StageDefinition activeStage = _session?.CombatSession?.ActiveStage;
            if (activeStage == null)
            {
                return false;
            }

            return ProcessInput(() =>
            {
                _acknowledgedFinalBossStageId = activeStage.Id;
                return true;
            });
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
            _acknowledgedFinalBossStageId = null;
            StopRoundOneStartRoutine();
            _waitingForRoundOneReveal = false;
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
            if (ShouldWaitForEnemyExitBeforeMerchantScreen())
            {
                BeginEnemyExitBeforeMerchantScreen();
                return;
            }

            RefreshFlow();
        }

        private void HandleStartingDemonConfirmationRequested()
        {
            RequestCompleteStartingDemonReveal();
        }

        private void HandleOpponentSelected(string profileKey)
        {
            if (CurrentScreen == GameFlowScreen.FinalBossReveal)
            {
                StageDefinition activeStage =
                    _session?.CombatSession?.ActiveStage;
                if (opponentSelection == null ||
                    activeStage == null ||
                    !opponentSelection.CanCommitFinalBossReveal(
                        activeStage.Id,
                        profileKey))
                {
                    return;
                }

                bool bossAccepted = RequestAcknowledgeFinalBossReveal();
                if (!bossAccepted)
                {
                    opponentSelection
                        .RestoreFinalBossRevealAfterRejectedCommit(
                            activeStage.Id,
                            profileKey);
                }

                return;
            }

            if (opponentSelection == null ||
                CurrentViewModel == null ||
                !CurrentViewModel.OpponentOfferId.HasValue)
            {
                return;
            }

            int offerId = CurrentViewModel.OpponentOfferId.Value;
            if (!opponentSelection.CanCommitSelection(
                    offerId,
                    profileKey))
            {
                return;
            }

            bool accepted = TrySelectOpponent(
                profileKey,
                requireReadyView: false);
            if (!accepted)
            {
                opponentSelection.RestoreSelectionAfterRejectedCommit(
                    profileKey);
            }
        }

        private void HandleFormalShopCardPurchaseRequested(int optionId)
        {
            bool succeeded = false;
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                succeeded = RequestBuyShopCard(offerId, optionId);
            }

            gameManager?.CompleteFormalShopCardPurchase(succeeded);
        }

        private void HandleFormalShopCardRemovalRequested(int cardId)
        {
            bool succeeded = false;
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                succeeded = RequestRemoveShopCard(offerId, cardId);
                if (succeeded)
                {
                    gameManager?.PlayLighterShopAnimation();
                }
            }

            gameManager?.CompleteFormalLighterRemoval(succeeded);
        }

        private void HandleFormalShopRestRequested()
        {
            bool succeeded = false;
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                succeeded = RequestRestAtShop(offerId);
                if (succeeded)
                {
                    gameManager?.PlayWhiskeyShopAnimation();
                    gameManager?.PlayPlayerSoulRestoredFlourish();
                }
            }

            gameManager?.CompleteFormalShopRest(succeeded);
        }

        private void HandleFormalShopLeaveRequested()
        {
            bool succeeded = false;
            if (CurrentViewModel?.ShopOfferId is int offerId)
            {
                succeeded = RequestLeaveShop(offerId);
            }

            gameManager?.CompleteFormalShopLeave(succeeded);
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

        private void RefreshFlow(bool enemyExitAlreadyCompleted = false)
        {
            if (_session == null && !TryAdoptFormalRun())
            {
                return;
            }

            ResolveSceneReferences();
            SynchronizeFocusedOpponent();
            GameFlowScreen previousScreen = CurrentScreen;
            GameFlowScreen nextScreen =
                GameFlowScreenResolver.Resolve(_session);
            if (nextScreen == GameFlowScreen.Combat &&
                ShouldShowFinalBossReveal())
            {
                nextScreen = GameFlowScreen.FinalBossReveal;
            }

            bool isEnteringFinalBossCombat =
                IsFinalBossCombatEntrance(previousScreen, nextScreen);

            _playCharacterExitBeforeEntrance =
                IsCharacterModeTransition(previousScreen, nextScreen) &&
                !enemyExitAlreadyCompleted;
            // Deliberately not stopped here: a redundant RefreshFlow call for a
            // screen we are already on (so this resets straight back to false
            // below) must not cancel an in-flight entrance's safety timer, or a
            // still-pending CompleteCharacterEntrance would have nothing left to
            // fall back on. Only a genuinely new wait (below) restarts the timer.
            _unlockInputAfterCharacterEntrance = false;
            CurrentViewModel = StageProgressionPresenter.Create(
                _session,
                _focusedOpponentProfileKey);
            CurrentScreen = nextScreen;
            if (IsTerminalScreen(previousScreen) && !IsTerminalScreen(nextScreen))
            {
                ResetResultDialogue();
                enemyCharacter?.HideSpeech();
            }

            string combatProfileKey = ResolveCombatProfileKey();
            ApplyMood(nextScreen, combatProfileKey);
            if (isEnteringFinalBossCombat)
            {
                PrepareFinalBossCharacterEntrance();
                string moodId = GameSceneMoodResolver.Resolve(
                    nextScreen,
                    combatProfileKey);
                if (moodController != null &&
                    !moodController.TryQueuePendingBgm(moodId))
                {
                    Debug.LogWarning(
                        $"Final-boss BGM mood '{moodId}' is not registered.",
                        moodController);
                }

                moodController?.TryPlayEntranceDoorAnimation();
            }
            bool isEnteringStartingDemonReveal =
                nextScreen == GameFlowScreen.StartingDemonReveal &&
                previousScreen != GameFlowScreen.StartingDemonReveal;

            if (nextScreen == GameFlowScreen.Combat)
            {
                bool isEnteringCombat = previousScreen != GameFlowScreen.Combat;
                bool waitForCharacterEntrance = isEnteringCombat &&
                    charactersRoot != null &&
                    enemyCharacter != null;
                bool waitForTutorialIntro = ShouldDelayCombatForTutorialIntro(
                    isEnteringCombat,
                    _session.CombatSession.IsTutorialRun);

                gameManager.UnbindFormalShop();
                if (waitForCharacterEntrance || waitForTutorialIntro)
                {
                    // Sequenced as: enemy appearance is set immediately (so the
                    // entrance shows the actual opponent, not whatever the
                    // character was last displaying), then the battle binds right
                    // away too — table/deck piles/buttons/contract papers/codex/HUD
                    // all appear from this point on, before the entrance animation
                    // even starts, per the intro sequence's step 0. charactersRoot
                    // itself stays inactive until ShowCharactersWithEntrance, so
                    // binding this early does not reveal the enemy early — only the
                    // table. Hand rendering is suppressed until round 1's
                    // post-entrance hold elapses, so the already-dealt cards don't
                    // pop in before then either.
                    gameManager.PrepareEnemyAppearance(_session.CombatSession);
                    gameManager.enabled = true;
                    gameManager.SuppressHandRenderUntilRoundOneStart();
                    if (!gameManager.BindBattle(
                            _session.CombatSession,
                            unlockInput: false))
                    {
                        throw new InvalidOperationException(
                            "The active formal battle could not be bound.");
                    }

                    if (gameManager.HasPendingTutorialIntro)
                    {
                        _waitingForRoundOneReveal = true;
                        _hasPresentedCharacters = false;
                        BeginCharacterEntranceUnlockSafety();
                    }
                    else
                    {
                        _waitingForRoundOneReveal = true;
                        BeginCharacterEntranceUnlockSafety();
                    }
                }
                else
                {
                    _waitingForRoundOneReveal = false;
                    if (!gameManager.BindBattle(
                            _session.CombatSession,
                            unlockInput: true))
                    {
                        throw new InvalidOperationException(
                            "The active formal battle could not be bound.");
                    }

                    gameManager.enabled = true;
                }
            }
            else if (nextScreen == GameFlowScreen.Shop)
            {
                bool isEnteringShop = previousScreen != GameFlowScreen.Shop;
                bool waitForCharacterEntrance = isEnteringShop &&
                    charactersRoot != null &&
                    enemyCharacter != null;
                _unlockInputAfterCharacterEntrance = waitForCharacterEntrance;
                if (waitForCharacterEntrance)
                {
                    BeginCharacterEntranceUnlockSafety();
                }

                gameManager.UnbindBattle();
                if (!gameManager.BindFormalShop(
                        CurrentViewModel,
                        _session.CombatSession.Progress.Player.CurrentGold,
                        unlockInput: !waitForCharacterEntrance))
                {
                    throw new InvalidOperationException(
                        "The active formal shop could not be bound.");
                }

                gameManager.enabled = true;
            }
            else
            {
                gameManager.UnbindFormalShop();
                gameManager.UnbindBattle();
                gameManager.enabled = false;
            }

            RenderFlowScreen();
            // Fires after RenderFlowScreen, not before: "Characters" starts inactive in
            // the scene, so on the very first-ever transition (Unavailable ->
            // StartingDemonReveal) GameObject.Find("Characters") inside
            // ResolveSceneReferences can't find it yet — it's RenderFlowScreen's
            // UpdateCharactersVisibility that actually activates it. Firing before that
            // point left enemyCharacter null and this speech call silently no-opped.
            if (isEnteringStartingDemonReveal)
            {
                ShowMerchantSpeech(SpeechCueKeys.StartingDemonGreeting);
            }

            ScreenChanged?.Invoke(CurrentScreen, CurrentViewModel);
        }

        private void RenderFlowScreen()
        {
            ResolveSceneReferences();
            bool isStartingReveal =
                CurrentScreen == GameFlowScreen.StartingDemonReveal;
            bool isOpponentSelection =
                CurrentScreen == GameFlowScreen.OpponentSelection;
            bool isFinalBossReveal =
                CurrentScreen == GameFlowScreen.FinalBossReveal;
            bool isCombat = CurrentScreen == GameFlowScreen.Combat;
            bool isShop = CurrentScreen == GameFlowScreen.Shop;
            bool isResult =
                CurrentScreen == GameFlowScreen.RunVictory ||
                CurrentScreen == GameFlowScreen.RunDefeat;
            codex?.SetAvailable(isCombat || isShop);
            hud?.SetEnemyStatusVisible(isCombat);
            hud?.SetCoreStatsVisible(!isStartingReveal);
            // There is no opponent to draw against outside of combat, so the opponent's
            // deck piles should not be shown (or hoverable) during the starting-demon
            // grant or shop screens.
            gameManager?.SetEnemyDeckVisible(isCombat);

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
            else if (isFinalBossReveal)
            {
                RenderFinalBossReveal();
            }
            else
            {
                opponentSelection?.Hide();
            }

            RunSaveViewModel save = RunSavePresenter.Create(_runtime.SaveFlow);
            bool showSaveFallback = ShouldShowResultPanel(
                isResult,
                save.BlocksProgressionInput);
            if (showSaveFallback)
            {
                resultView?.Render(RunResultPresenter.Create(
                    CurrentScreen,
                    CurrentViewModel,
                    save));
            }
            else
            {
                resultView?.Hide();
                if (isResult)
                {
                    PrepareResultDialogue();
                }
            }

            if (hudRoot != null)
            {
                hudRoot.SetActive(ShouldShowHudRoot(CurrentScreen));
            }

            UpdateCharactersVisibility(
                isCombat || isStartingReveal ||
                CurrentScreen == GameFlowScreen.Shop || isResult);

            if (!_characterExitWaitingForEntrance &&
                (moodController == null ||
                    !moodController.IsEntranceDoorAnimationPlaying))
            {
                ApplyCharacterModeForCurrentScreen();
            }

            if (isResult && !showSaveFallback)
            {
                TryBeginResultDialogue();
            }
        }

        private void RenderFinalBossReveal()
        {
            OpponentCandidateViewModel bossCandidate =
                StageProgressionPresenter.CreateFinalBossRevealCandidate(_session);
            StageDefinition activeStage =
                _session?.CombatSession?.ActiveStage;
            if (bossCandidate == null || activeStage == null)
            {
                opponentSelection?.Hide();
                return;
            }

            opponentSelection?.RenderFinalBossReveal(
                bossCandidate,
                activeStage.Id);
        }

        private bool ShouldShowFinalBossReveal()
        {
            StageDefinition activeStage = _session?.CombatSession?.ActiveStage;
            return activeStage != null &&
                activeStage.Kind == StageKind.FinalBossCombat &&
                !StringComparer.Ordinal.Equals(
                    _acknowledgedFinalBossStageId,
                    activeStage.Id);
        }

        private bool ShouldWaitForEnemyExitBeforeMerchantScreen()
        {
            if (_merchantTransitionWaitingForEnemyExit ||
                _session == null ||
                CurrentScreen != GameFlowScreen.Combat)
            {
                return false;
            }

            GameFlowScreen nextScreen = GameFlowScreenResolver.Resolve(_session);
            if (nextScreen != GameFlowScreen.Shop &&
                !IsTerminalScreen(nextScreen))
            {
                return false;
            }

            ResolveSceneReferences();
            return charactersRoot != null &&
                charactersRoot.activeSelf &&
                enemyCharacter != null;
        }

        private void BeginEnemyExitBeforeMerchantScreen()
        {
            if (_merchantTransitionWaitingForEnemyExit)
            {
                return;
            }

            _merchantTransitionWaitingForEnemyExit = true;
            enemyCharacter.PlayExitAnimation(
                CompleteEnemyExitBeforeMerchantScreen);
        }

        private void CompleteEnemyExitBeforeMerchantScreen()
        {
            if (!_merchantTransitionWaitingForEnemyExit)
            {
                return;
            }

            _merchantTransitionWaitingForEnemyExit = false;
            if (charactersRoot != null)
            {
                charactersRoot.SetActive(false);
            }

            RefreshFlow(enemyExitAlreadyCompleted: true);
        }

        private void UpdateCharactersVisibility(
            bool shouldShow,
            bool skipExitAnimation = false)
        {
            if (charactersRoot == null)
            {
                return;
            }

            if (shouldShow)
            {
                if (moodController != null &&
                    moodController.IsEntranceDoorAnimationPlaying)
                {
                    BeginWaitingForDoorAnimation();
                    return;
                }

                if (_playCharacterExitBeforeEntrance)
                {
                    BeginWaitingForDoorAnimation();
                    return;
                }

                if (_charactersEntranceWaiting)
                {
                    return;
                }

                ShowCharactersWithEntrance();
                return;
            }

            if (_charactersEntranceWaiting &&
                moodController != null &&
                moodController.IsEntranceDoorAnimationPlaying)
            {
                // The very first characters-appear call is still waiting on the
                // one-shot door-opening animation. Cancelling _charactersEntranceWaiting
                // now (as the code below does) would make
                // HandleEntranceDoorAnimationCompleted() silently drop the entrance when
                // the door finishes later — permanently skipping it, since nothing else
                // ever retries a first-time entrance. Let the door keep playing and
                // resolve to hidden once it completes instead.
                _pendingHideAfterDoorAnimation = true;
                return;
            }

            _playCharacterExitBeforeEntrance = false;
            _characterExitWaitingForEntrance = false;
            _characterEntranceInProgress = false;
            _characterEntranceRequestId++;
            CancelEnemyAppearanceDelay();
            _charactersEntranceWaiting = false;

            if (enemyCharacter == null)
            {
                charactersRoot.SetActive(false);
                return;
            }

            if (!charactersRoot.activeSelf)
            {
                return;
            }

            if (skipExitAnimation)
            {
                charactersRoot.SetActive(false);
                return;
            }

            enemyCharacter.PlayExitAnimation(() =>
            {
                if (charactersRoot != null)
                {
                    charactersRoot.SetActive(false);
                }
            });
        }

        private void BeginWaitingForDoorAnimation()
        {
            StopEnemyAppearanceDelayRoutine();
            _charactersEntranceWaiting = true;
            _pendingHideAfterDoorAnimation = false;

            bool shouldPlayExit =
                _playCharacterExitBeforeEntrance &&
                enemyCharacter != null &&
                charactersRoot.activeSelf;
            _playCharacterExitBeforeEntrance = false;
            if (!shouldPlayExit)
            {
                charactersRoot.SetActive(false);
                return;
            }

            _characterExitWaitingForEntrance = true;
            enemyCharacter.PlayExitAnimation(
                CompleteCharacterExitBeforeEntrance);
        }

        private void HandleEntranceDoorAnimationCompleted()
        {
            if (!_charactersEntranceWaiting || !ShouldShowCharacters())
            {
                return;
            }

            if (_pendingHideAfterDoorAnimation)
            {
                _pendingHideAfterDoorAnimation = false;
                _charactersEntranceWaiting = false;
                return;
            }

            if (_characterExitWaitingForEntrance)
            {
                return;
            }

            StopEnemyAppearanceDelayRoutine();
            if (enemyAppearanceDelayAfterDoor <= 0f)
            {
                ShowCharactersWithEntrance();
                return;
            }

            _enemyAppearanceDelayRoutine = StartCoroutine(
                DelayEnemyAppearanceAfterDoor());
        }

        private IEnumerator DelayEnemyAppearanceAfterDoor()
        {
            yield return new WaitForSeconds(enemyAppearanceDelayAfterDoor);
            _enemyAppearanceDelayRoutine = null;

            if (_charactersEntranceWaiting && ShouldShowCharacters())
            {
                ShowCharactersWithEntrance();
            }
            else
            {
                _charactersEntranceWaiting = false;
            }
        }

        private void ShowCharactersWithEntrance()
        {
            if (charactersRoot == null || _characterExitWaitingForEntrance)
            {
                return;
            }

            if (_characterEntranceInProgress)
            {
                return;
            }

            bool wasVisible = charactersRoot.activeSelf;
            bool shouldAnimate = !wasVisible || !_hasPresentedCharacters;
            if (shouldAnimate && enemyCharacter != null)
            {
                enemyCharacter.PrepareEntranceAnimation();
            }
            charactersRoot.SetActive(true);
            if (shouldAnimate)
            {
                ApplyCharacterModeForCurrentScreen();
                int requestId = ++_characterEntranceRequestId;
                if (enemyCharacter != null)
                {
                    _characterEntranceInProgress = true;
                    enemyCharacter.PlayEntranceAnimation(
                        () => CompleteCharacterEntrance(requestId));
                }
                else
                {
                    CompleteCharacterEntrance(requestId);
                }
            }

            if (enemyCharacter != null)
            {
                moodController?.PlayPendingBgm();
            }

            _hasPresentedCharacters = true;
            _charactersEntranceWaiting = false;
        }

        private void CompleteCharacterEntrance(int requestId)
        {
            if (requestId != _characterEntranceRequestId)
            {
                return;
            }

            _characterEntranceInProgress = false;
            StopCharacterEntranceUnlockSafety();
            if (IsTerminalScreen())
            {
                TryBeginResultDialogue();
                return;
            }

            if (_waitingForRoundOneReveal)
            {
                if (gameManager != null && gameManager.HasPendingTutorialIntro)
                {
                    _waitingForRoundOneReveal = false;
                    gameManager.BeginTutorialIntroIfNeeded();
                    return;
                }

                // The battle already bound the instant the screen entered Combat
                // (see RefreshFlow) — this only starts the hold that gates the
                // card-deal reveal and input unlock.
                StopRoundOneStartRoutine();
                _roundOneStartRoutine = StartCoroutine(
                    BeginRoundOneAfterEntranceHold());
                return;
            }

            if (!_unlockInputAfterCharacterEntrance)
            {
                return;
            }

            _unlockInputAfterCharacterEntrance = false;
            gameManager?.SetPresentationInputLocked(false);
        }

        // Round 1's card deal (hidden + face-up card per side, animated the same
        // way a later hit is) and input unlock only happen once this hold elapses —
        // everything else (deck piles, table buttons, contract papers, codex, HUD
        // text, the enemy's appearance/battle-start line) is already showing from
        // the immediate BindBattle in RefreshFlow's combat transition.
        private IEnumerator BeginRoundOneAfterEntranceHold()
        {
            yield return new WaitForSeconds(roundOneStartDelayAfterEntrance);
            _roundOneStartRoutine = null;
            _waitingForRoundOneReveal = false;
            yield return gameManager.PresentRoundOneHands();
            gameManager.SetPresentationInputLocked(false);
            gameManager.NotifyTutorialRoundOneRevealReady();
        }

        private void StopRoundOneStartRoutine()
        {
            if (_roundOneStartRoutine == null)
            {
                return;
            }

            StopCoroutine(_roundOneStartRoutine);
            _roundOneStartRoutine = null;
        }

        private void BeginCharacterEntranceUnlockSafety()
        {
            StopCharacterEntranceUnlockSafety();
            _characterEntranceUnlockSafetyRoutine = StartCoroutine(
                CharacterEntranceUnlockSafetyRoutine());
        }

        private void StopCharacterEntranceUnlockSafety()
        {
            if (_characterEntranceUnlockSafetyRoutine == null)
            {
                return;
            }

            StopCoroutine(_characterEntranceUnlockSafetyRoutine);
            _characterEntranceUnlockSafetyRoutine = null;
        }

        private IEnumerator CharacterEntranceUnlockSafetyRoutine()
        {
            yield return new WaitForSeconds(CharacterEntranceUnlockSafetySeconds);
            _characterEntranceUnlockSafetyRoutine = null;
            if (!_waitingForRoundOneReveal)
            {
                _unlockInputAfterCharacterEntrance = false;
                gameManager?.SetPresentationInputLocked(false);
                yield break;
            }

            if (_session?.CombatSession == null ||
                !_session.CombatSession.IsTutorialRun)
            {
                _waitingForRoundOneReveal = false;
                yield return gameManager.PresentRoundOneHands();
                _unlockInputAfterCharacterEntrance = false;
                gameManager.SetPresentationInputLocked(false);
                yield break;
            }

            // Never skip the tutorial entrance by revealing the first hand from
            // this safety path. Re-resolve late scene references and issue a fresh
            // entrance request instead. CompleteCharacterEntrance remains the only
            // path that can reveal round one and unlock input.
            ResolveSceneReferences();
            if (charactersRoot != null && enemyCharacter != null)
            {
                _hasPresentedCharacters = false;
                RenderFlowScreen();
            }

            BeginCharacterEntranceUnlockSafety();
        }

        private void CompleteCharacterExitBeforeEntrance()
        {
            _characterExitWaitingForEntrance = false;
            if (charactersRoot != null)
            {
                charactersRoot.SetActive(false);
            }

            if (_charactersEntranceWaiting &&
                ShouldShowCharacters() &&
                (moodController == null ||
                    !moodController.IsEntranceDoorAnimationPlaying))
            {
                HandleEntranceDoorAnimationCompleted();
            }
        }

        private void ApplyCharacterModeForCurrentScreen()
        {
            if (enemyCharacter == null)
            {
                return;
            }

            if (CurrentScreen == GameFlowScreen.StartingDemonReveal ||
                CurrentScreen == GameFlowScreen.Shop ||
                IsTerminalScreen())
            {
                enemyCharacter.EnterMerchant();
            }
            else if (CurrentScreen == GameFlowScreen.Combat)
            {
                enemyCharacter.ExitMerchant();
            }
        }

        private void ShowMerchantSpeech(string cueKey)
        {
            if (enemyCharacter == null || string.IsNullOrWhiteSpace(cueKey))
            {
                return;
            }

            _speechResolver ??= new SpeechLineResolver(merchantSpeechSeed);
            enemyCharacter.ShowSpeech(
                _speechResolver.Resolve(merchantSpeechProfile, cueKey));
        }

#if UNITY_EDITOR
        internal bool DebugStartResultDialoguePreview(
            GameFlowScreen screen,
            bool hasMadeDemonContract,
            string opponentProfileKey)
        {
            if (!Application.isPlaying ||
                !IsTerminalScreen(screen) ||
                runResultDialogue == null ||
                string.IsNullOrWhiteSpace(opponentProfileKey))
            {
                return false;
            }

            EnemyProfilePreview opponent = null;
            foreach (EnemyProfilePreview preview in
                EnemyCombatProfileCatalog.Default.Previews)
            {
                if (string.Equals(
                    preview.ProfileKey,
                    opponentProfileKey,
                    StringComparison.Ordinal))
                {
                    opponent = preview;
                    break;
                }
            }

            if (opponent == null)
            {
                return false;
            }

            RunResultDialogueViewModel model =
                RunResultDialoguePresenter.CreateForPreview(
                    screen,
                    hasMadeDemonContract,
                    opponent.ProfileKey,
                    opponent.DisplayName,
                    runResultDialogue);

            ResolveSceneReferences();
            if (enemyCharacter == null || charactersRoot == null)
            {
                return false;
            }

            CancelEnemyAppearanceDelay();
            StopCharacterEntranceUnlockSafety();
            StopRoundOneStartRoutine();
            ResetResultDialogue();
            _isResultDialoguePreview = true;
            _waitingForRoundOneReveal = false;
            _unlockInputAfterCharacterEntrance = false;
            _charactersEntranceWaiting = false;
            _characterExitWaitingForEntrance = false;
            _characterEntranceInProgress = false;
            _characterEntranceRequestId++;
            _playCharacterExitBeforeEntrance = false;
            _merchantTransitionWaitingForEnemyExit = true;

            CurrentScreen = screen;
            CurrentViewModel = null;
            _resultDialogueSequence =
                new RunResultDialogueSequence(model.Lines);
            _resultDialogueCharactersPerSecond = model.CharactersPerSecond;
            _resultDialoguePending = true;

            startingDemonReveal?.Hide();
            opponentSelection?.Hide();
            resultView?.Hide();
            codex?.SetAvailable(false);
            hud?.SetEnemyStatusVisible(false);
            hud?.SetCoreStatsVisible(false);
            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }

            if (gameManager != null)
            {
                gameManager.UnbindFormalShop();
                gameManager.UnbindBattle();
                gameManager.SetBattleCardObjectsVisible(false);
                gameManager.SetEnemyDeckVisible(false);
                gameManager.enabled = false;
            }

            enemyCharacter.HideSpeech();
            enemyCharacter.ExitMerchant();
            enemyCharacter.TrySetEnemyProfile(opponent.ProfileKey);

            if (charactersRoot.activeInHierarchy &&
                enemyCharacter.gameObject.activeInHierarchy)
            {
                enemyCharacter.PlayExitAnimation(
                    CompleteDebugResultDialoguePreviewEnemyExit);
            }
            else
            {
                CompleteDebugResultDialoguePreviewEnemyExit();
            }

            return true;
        }

        private void CompleteDebugResultDialoguePreviewEnemyExit()
        {
            if (!_isResultDialoguePreview ||
                !_merchantTransitionWaitingForEnemyExit)
            {
                return;
            }

            _merchantTransitionWaitingForEnemyExit = false;
            charactersRoot.SetActive(false);
            ApplyMood(CurrentScreen, enemyProfileKey: null);
            UpdateCharactersVisibility(shouldShow: true);
            if (!_characterEntranceInProgress && !_charactersEntranceWaiting)
            {
                TryBeginResultDialogue();
            }
        }
#endif

        private void PrepareResultDialogue()
        {
            if (_resultDialogueSequence != null ||
                runResultDialogue == null ||
                _session?.CombatSession?.Progress?.Player == null)
            {
                return;
            }

            StageDefinition activeStage = _session.CombatSession.ActiveStage;
            if (activeStage == null)
            {
                Debug.LogError(
                    "Run result dialogue requires the completed active stage.",
                    this);
                return;
            }

            RunResultDialogueViewModel model = RunResultDialoguePresenter.Create(
                CurrentScreen,
                _session.CombatSession.Progress.Player,
                activeStage,
                runResultDialogue);
            _resultDialogueSequence =
                new RunResultDialogueSequence(model.Lines);
            _resultDialogueCharactersPerSecond = model.CharactersPerSecond;
            _resultDialoguePending = true;
        }

        private void TryBeginResultDialogue()
        {
            if (!_resultDialoguePending ||
                _resultDialogueSequence == null ||
                enemyCharacter == null ||
                !enemyCharacter.gameObject.activeInHierarchy ||
                _characterEntranceInProgress ||
                _charactersEntranceWaiting)
            {
                return;
            }

            _resultDialoguePending = false;
            enemyCharacter.PlaySpeech(
                _resultDialogueSequence.CurrentLine,
                _resultDialogueCharactersPerSecond);
        }

        private void HandleResultDialogueInput()
        {
            if (!IsTerminalScreen() ||
                _resultDialoguePending ||
                _resultDialogueSequence == null ||
                enemyCharacter == null ||
                (_resultTransition != null && _resultTransition.IsPlaying))
            {
                return;
            }

            if (!DialogueAdvanceInput.WasPressedThisFrame())
            {
                return;
            }

            RunResultDialogueClickResult clickResult =
                _resultDialogueSequence.HandleClick(
                    enemyCharacter.IsSpeechComplete);
            if (clickResult ==
                RunResultDialogueClickResult.CompleteCurrentLine)
            {
                enemyCharacter.CompleteSpeechImmediately();
                return;
            }

            if (clickResult == RunResultDialogueClickResult.ShowNextLine)
            {
                enemyCharacter.PlaySpeech(
                    _resultDialogueSequence.CurrentLine,
                    _resultDialogueCharactersPerSecond);
                return;
            }

            BeginResultExitTransition();
        }

        private void BeginResultExitTransition()
        {
            _resultTransition ??= GetComponent<RunResultTransitionView>();
            _resultTransition ??=
                gameObject.AddComponent<RunResultTransitionView>();
            if (_resultTransition.IsPlaying)
            {
                return;
            }
#if UNITY_EDITOR
            bool isPreview = _isResultDialoguePreview;
#else
            const bool isPreview = false;
#endif
            if (!_resultTransition.TryPlay(
                    CurrentScreen,
                    hud,
                    () => CompleteResultExitTransition(isPreview)))
            {
                CompleteResultExitTransition(isPreview);
            }
        }

        private void CompleteResultExitTransition(bool isPreview)
        {
            try
            {
#if UNITY_EDITOR
                if (isPreview)
                {
                    StageProgressionRuntime
                        .ReturnToMainMenuAndDestroyInstance();
                    return;
                }
#endif
                if (RequestReturnToMainMenu())
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            _resultTransition?.CancelAndRestore();
        }

        private void ResetResultDialogue()
        {
            _resultTransition?.CancelAndRestore();
            _resultDialogueSequence = null;
            _resultDialogueCharactersPerSecond = 0f;
            _resultDialoguePending = false;
#if UNITY_EDITOR
            _isResultDialoguePreview = false;
#endif
        }

        private static bool IsCharacterModeTransition(
            GameFlowScreen previousScreen,
            GameFlowScreen nextScreen)
        {
            return (previousScreen == GameFlowScreen.Combat &&
                    (nextScreen == GameFlowScreen.Shop ||
                     IsTerminalScreen(nextScreen))) ||
                (previousScreen == GameFlowScreen.Shop &&
                    nextScreen == GameFlowScreen.Combat);
        }

        internal static bool IsFinalBossCombatEntrance(
            GameFlowScreen previousScreen,
            GameFlowScreen nextScreen)
        {
            return previousScreen == GameFlowScreen.FinalBossReveal &&
                nextScreen == GameFlowScreen.Combat;
        }

        private void PrepareFinalBossCharacterEntrance()
        {
            StopEnemyAppearanceDelayRoutine();
            StopCharacterEntranceUnlockSafety();
            _charactersEntranceWaiting = false;
            _pendingHideAfterDoorAnimation = false;
            _characterExitWaitingForEntrance = false;
            _characterEntranceInProgress = false;
            _characterEntranceRequestId++;
            _hasPresentedCharacters = false;
            if (charactersRoot != null)
            {
                charactersRoot.SetActive(false);
            }
        }

        private bool ShouldShowCharacters()
        {
            return CurrentScreen == GameFlowScreen.StartingDemonReveal ||
                CurrentScreen == GameFlowScreen.Combat ||
                CurrentScreen == GameFlowScreen.Shop ||
                IsTerminalScreen();
        }

        private void StopEnemyAppearanceDelayRoutine()
        {
            if (_enemyAppearanceDelayRoutine == null)
            {
                return;
            }

            StopCoroutine(_enemyAppearanceDelayRoutine);
            _enemyAppearanceDelayRoutine = null;
        }

        private void CancelEnemyAppearanceDelay()
        {
            StopEnemyAppearanceDelayRoutine();
            _charactersEntranceWaiting = false;
            moodController?.CancelPendingBgm();
        }

        private void SubscribeToMoodController()
        {
            if (moodController == null)
            {
                return;
            }

            moodController.EntranceDoorAnimationCompleted -=
                HandleEntranceDoorAnimationCompleted;
            moodController.EntranceDoorAnimationCompleted +=
                HandleEntranceDoorAnimationCompleted;
        }

        private void UnsubscribeFromMoodController()
        {
            if (moodController == null)
            {
                return;
            }

            moodController.EntranceDoorAnimationCompleted -=
                HandleEntranceDoorAnimationCompleted;
        }

        private void ResolveSceneReferences()
        {
            moodController ??= GetComponent<MoodController>();
            opponentSelection ??= FindFirstObjectByType<OpponentSelectionView>(
                FindObjectsInactive.Include);

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

            // Formal screens decide visibility in RenderFlowScreen; standalone combat keeps
            // the authored HUD state so the enemy soul counter remains visible.
            startingDemonReveal?.BindHud(hud);
            ResolveCharacterReferencesIncludingInactive();
        }

        private void ResolveCharacterReferencesIncludingInactive()
        {
            if (charactersRoot == null)
            {
                Transform[] transforms = FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                Transform bestCandidate = null;
                for (int index = 0; index < transforms.Length; index++)
                {
                    Transform candidate = transforms[index];
                    if (candidate != null &&
                        candidate.gameObject.scene == gameObject.scene &&
                        candidate.name == "Characters")
                    {
                        bool candidatePreferred = bestCandidate == null ||
                            (!candidate.gameObject.activeInHierarchy &&
                                bestCandidate.gameObject.activeInHierarchy) ||
                            (candidate.gameObject.activeInHierarchy ==
                                bestCandidate.gameObject.activeInHierarchy &&
                                candidate.GetInstanceID() >
                                    bestCandidate.GetInstanceID());
                        if (candidatePreferred)
                        {
                            bestCandidate = candidate;
                        }
                    }
                }

                charactersRoot = bestCandidate == null
                    ? null
                    : bestCandidate.gameObject;
            }

            if (enemyCharacter == null && charactersRoot != null)
            {
                Transform enemy = charactersRoot.transform.Find(
                    "EnemyCharacter");
                enemyCharacter = enemy == null
                    ? null
                    : enemy.GetComponent<CharacterView>();
            }

            if (enemyCharacter != null)
            {
                return;
            }

            CharacterView[] characters = FindObjectsByType<CharacterView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < characters.Length; index++)
            {
                CharacterView candidate = characters[index];
                if (candidate == null ||
                    candidate.gameObject.scene != gameObject.scene ||
                    candidate.name != "EnemyCharacter")
                {
                    continue;
                }

                enemyCharacter = candidate;
                Transform ancestor = candidate.transform.parent;
                while (charactersRoot == null && ancestor != null)
                {
                    if (ancestor.name == "Characters")
                    {
                        charactersRoot = ancestor.gameObject;
                        break;
                    }

                    ancestor = ancestor.parent;
                }

                break;
            }
        }

        private string ResolveCombatProfileKey()
        {
            return _session?.CombatSession?.ActiveStage?.BattleProfileKey ??
                gameManager?.CurrentEnemyProfileKey;
        }

        private void ApplyMood(
            GameFlowScreen screen,
            string enemyProfileKey)
        {
            string moodId = GameSceneMoodResolver.Resolve(
                screen,
                enemyProfileKey);
            if (string.IsNullOrWhiteSpace(moodId) ||
                string.Equals(
                    moodId,
                    _currentMoodId,
                    StringComparison.Ordinal))
            {
                return;
            }

            moodController ??= GetComponent<MoodController>();
            if (moodController == null)
            {
                Debug.LogWarning(
                    $"MoodController is missing for mood '{moodId}'.",
                    this);
                return;
            }

            float duration = Mathf.Max(0f, moodTransitionDuration);
            bool applied = screen == GameFlowScreen.FinalBossReveal
                ? moodController.TryBlendToMoodWithoutEntrance(
                    moodId,
                    duration)
                : moodController.TryBlendToMood(moodId, duration);
            if (!applied)
            {
                Debug.LogWarning(
                    $"Mood profile '{moodId}' is not registered.",
                    moodController);
                return;
            }

            _currentMoodId = moodId;
        }

        internal static bool ShouldShowHudRoot(GameFlowScreen screen)
        {
            return screen == GameFlowScreen.StartingDemonReveal ||
                screen == GameFlowScreen.Combat ||
                screen == GameFlowScreen.Shop;
        }

        internal static bool ShouldDelayCombatForTutorialIntro(
            bool isEnteringCombat,
            bool isTutorialRun)
        {
            return isEnteringCombat && isTutorialRun;
        }

        private bool IsTerminalScreen()
        {
            return IsTerminalScreen(CurrentScreen);
        }

        internal static bool ShouldShowResultPanel(
            bool isResult,
            bool saveBlocksProgression)
        {
            return isResult && saveBlocksProgression;
        }

        private static bool IsTerminalScreen(GameFlowScreen screen)
        {
            return screen == GameFlowScreen.RunVictory ||
                screen == GameFlowScreen.RunDefeat;
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
