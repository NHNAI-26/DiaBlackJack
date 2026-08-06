using System;
using System.Collections;
using Border.SaveLoad;
using Border.SaveLoad.UI;
using DiaBlackJack.Content;
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
        [SerializeField] private OpponentWantedPosterView finalBossReveal;
        [SerializeField] private RunResultView resultView;
        [SerializeField] private CodexController codex;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameHudView hud;
        [SerializeField] private GameObject charactersRoot;
        [SerializeField] private CharacterView enemyCharacter;
        [SerializeField] private MoodController moodController;
        [SerializeField] private float moodTransitionDuration = 1f;
        [SerializeField, Min(0f)] private float enemyAppearanceDelayAfterDoor = 1f;

        [Header("Merchant speech")]
        [SerializeField] private SpeechProfileSO merchantSpeechProfile;
        [SerializeField] private int merchantSpeechSeed = 20260804;

        private StageProgressionRuntime _runtime;
        private FormalRunSession _session;
        private SpeechLineResolver _speechResolver;
        private Coroutine _enemyAppearanceDelayRoutine;
        private int? _focusedOpponentOfferId;
        private string _focusedOpponentProfileKey;
        private string _acknowledgedFinalBossStageId;
        private StageProgressionSession _pendingCombatBindSession;
        private bool _isProcessingInput;
        private bool _charactersEntranceWaiting;
        private bool _hasPresentedCharacters;
        private bool _playCharacterExitBeforeEntrance;
        private bool _characterExitWaitingForEntrance;
        private bool _shopTransitionWaitingForEnemyExit;
        private bool _unlockInputAfterCharacterEntrance;
        private Coroutine _characterEntranceUnlockSafetyRoutine;
        private string _currentMoodId;

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

            if (finalBossReveal != null)
            {
                finalBossReveal.Selected += HandleFinalBossRevealSelected;
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

            if (finalBossReveal != null)
            {
                finalBossReveal.Selected -= HandleFinalBossRevealSelected;
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
            _shopTransitionWaitingForEnemyExit = false;
            _unlockInputAfterCharacterEntrance = false;
            _pendingCombatBindSession = null;
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

        public bool RequestSelectOpponent(string profileKey)
        {
            if (string.IsNullOrWhiteSpace(profileKey) ||
                IsInputBlocked() ||
                CurrentScreen != GameFlowScreen.OpponentSelection ||
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
            _pendingCombatBindSession = null;
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
            if (ShouldWaitForEnemyExitBeforeShop())
            {
                BeginEnemyExitBeforeShop();
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
            RequestSelectOpponent(profileKey);
        }

        private void HandleFinalBossRevealSelected(string profileKey)
        {
            RequestAcknowledgeFinalBossReveal();
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
            ApplyMood(nextScreen, ResolveCombatProfileKey());
            bool isEnteringStartingDemonReveal =
                nextScreen == GameFlowScreen.StartingDemonReveal &&
                previousScreen != GameFlowScreen.StartingDemonReveal;

            if (nextScreen == GameFlowScreen.Combat)
            {
                bool isEnteringCombat = previousScreen != GameFlowScreen.Combat;
                bool waitForCharacterEntrance = isEnteringCombat &&
                    charactersRoot != null &&
                    enemyCharacter != null;

                gameManager.UnbindFormalShop();
                if (waitForCharacterEntrance)
                {
                    // Sequenced as: enemy appearance is set immediately (so the
                    // entrance shows the actual opponent, not whatever the
                    // character was last displaying), then the entrance plays
                    // with nothing else bound yet, then CompleteCharacterEntrance
                    // binds the battle (table/cards/HUD appear) and only then
                    // unlocks input — not all three at once the moment the screen
                    // switches. gameManager stays disabled meanwhile so nothing
                    // renders off unbound state while entrance plays.
                    gameManager.PrepareEnemyAppearance(_session.CombatSession);
                    gameManager.enabled = false;
                    _pendingCombatBindSession = _session.CombatSession;
                    BeginCharacterEntranceUnlockSafety();
                }
                else
                {
                    _pendingCombatBindSession = null;
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
            else
            {
                opponentSelection?.Hide();
            }

            if (isFinalBossReveal)
            {
                RenderFinalBossReveal();
            }
            else
            {
                finalBossReveal?.Hide();
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
                hudRoot.SetActive(ShouldShowHudRoot(CurrentScreen));
            }

            UpdateCharactersVisibility(
                isCombat || isStartingReveal ||
                CurrentScreen == GameFlowScreen.Shop);

            if (!_characterExitWaitingForEntrance &&
                (moodController == null ||
                    !moodController.IsEntranceDoorAnimationPlaying))
            {
                ApplyCharacterModeForCurrentScreen();
            }
        }

        private void RenderFinalBossReveal()
        {
            OpponentCandidateViewModel bossCandidate =
                StageProgressionPresenter.CreateFinalBossRevealCandidate(_session);
            if (bossCandidate == null || finalBossReveal == null)
            {
                finalBossReveal?.Hide();
                return;
            }

            finalBossReveal.Render(
                bossCandidate,
                ResolveBossPortrait(bossCandidate.ProfileKey),
                interactable: true);
        }

        private Sprite ResolveBossPortrait(string profileKey)
        {
            return opponentSelection != null && opponentSelection.ContentCatalog != null
                ? opponentSelection.ContentCatalog.GetPortrait(profileKey)
                : null;
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

        private bool ShouldWaitForEnemyExitBeforeShop()
        {
            if (_shopTransitionWaitingForEnemyExit ||
                _session == null ||
                CurrentScreen != GameFlowScreen.Combat ||
                GameFlowScreenResolver.Resolve(_session) != GameFlowScreen.Shop)
            {
                return false;
            }

            ResolveSceneReferences();
            return charactersRoot != null &&
                charactersRoot.activeSelf &&
                enemyCharacter != null;
        }

        private void BeginEnemyExitBeforeShop()
        {
            if (_shopTransitionWaitingForEnemyExit)
            {
                return;
            }

            _shopTransitionWaitingForEnemyExit = true;
            enemyCharacter.PlayExitAnimation(CompleteEnemyExitBeforeShop);
        }

        private void CompleteEnemyExitBeforeShop()
        {
            if (!_shopTransitionWaitingForEnemyExit)
            {
                return;
            }

            _shopTransitionWaitingForEnemyExit = false;
            if (charactersRoot != null)
            {
                charactersRoot.SetActive(false);
            }

            RefreshFlow(enemyExitAlreadyCompleted: true);
        }

        private void UpdateCharactersVisibility(bool shouldShow)
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

            _playCharacterExitBeforeEntrance = false;
            _characterExitWaitingForEntrance = false;
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

            bool wasVisible = charactersRoot.activeSelf;
            charactersRoot.SetActive(true);
            if (!wasVisible || !_hasPresentedCharacters)
            {
                ApplyCharacterModeForCurrentScreen();
                if (enemyCharacter != null)
                {
                    enemyCharacter.PlayEntranceAnimation(
                        CompleteCharacterEntrance);
                }
                else
                {
                    CompleteCharacterEntrance();
                }
                if (enemyCharacter != null)
                {
                    moodController?.PlayPendingBgm();
                }
            }

            _hasPresentedCharacters = true;
            _charactersEntranceWaiting = false;
        }

        private void CompleteCharacterEntrance()
        {
            StopCharacterEntranceUnlockSafety();
            if (_pendingCombatBindSession != null)
            {
                StageProgressionSession session = _pendingCombatBindSession;
                _pendingCombatBindSession = null;
                gameManager.enabled = true;
                // Object setup (BindBattle, unlocked: false) and button
                // activation (SetPresentationInputLocked(false)) stay as two
                // separate calls/renders — even though nothing currently
                // animates between them — so the sequence is explicit rather
                // than baked into a single BindBattle(unlockInput: true) call.
                gameManager.BindBattle(session, unlockInput: false);
                gameManager.SetPresentationInputLocked(false);
                return;
            }

            if (!_unlockInputAfterCharacterEntrance)
            {
                return;
            }

            _unlockInputAfterCharacterEntrance = false;
            gameManager?.SetPresentationInputLocked(false);
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
            if (_pendingCombatBindSession != null)
            {
                StageProgressionSession session = _pendingCombatBindSession;
                _pendingCombatBindSession = null;
                gameManager.enabled = true;
                gameManager.BindBattle(session, unlockInput: false);
            }

            _unlockInputAfterCharacterEntrance = false;
            gameManager?.SetPresentationInputLocked(false);
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
                CurrentScreen == GameFlowScreen.Shop)
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

        private static bool IsCharacterModeTransition(
            GameFlowScreen previousScreen,
            GameFlowScreen nextScreen)
        {
            return (previousScreen == GameFlowScreen.Combat &&
                    nextScreen == GameFlowScreen.Shop) ||
                (previousScreen == GameFlowScreen.Shop &&
                    nextScreen == GameFlowScreen.Combat);
        }

        private bool ShouldShowCharacters()
        {
            return CurrentScreen == GameFlowScreen.StartingDemonReveal ||
                CurrentScreen == GameFlowScreen.Combat ||
                CurrentScreen == GameFlowScreen.Shop;
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
            if (finalBossReveal == null)
            {
                GameObject finalBossPosterObject =
                    GameObject.Find("WantedPosterFinalBoss");
                finalBossReveal = finalBossPosterObject == null
                    ? null
                    : finalBossPosterObject.GetComponent<OpponentWantedPosterView>();
            }

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

            if (!moodController.TryBlendToMood(
                    moodId,
                    Mathf.Max(0f, moodTransitionDuration)))
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
