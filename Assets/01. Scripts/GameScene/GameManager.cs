using System;
using System.Collections;
using System.Collections.Generic;
using Border.Audio;
using DiaBlackJack.Bootstrap;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Owns and drives a CoreLoop battle for the GameScene. The single coordinator: it
    /// holds the <see cref="CoreLoopSession"/>, takes input (temporary IMGUI buttons — the project is
    /// new-Input-System-only, so legacy OnMouseDown / Input.GetKey do not fire), and on every action
    /// re-presents through <see cref="GameScenePresenter"/> into the HUD and the two hands. Rendering
    /// lives in <see cref="GameHudView"/> and <see cref="CardHand"/>; this type only orchestrates.
    /// During a formal run, actions are forwarded through <see cref="StageProgressionSession"/> so
    /// battle completion, gold, and the next shop remain authoritative there. When opened directly,
    /// the same scene hosts formal-run selection screens before reloading itself for battle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private int seed = 20260719;
        [SerializeField] private GameHudView hud;
        [SerializeField] private CardHand playerHand;
        [SerializeField] private CardHand enemyHand;
        [SerializeField] private CharacterView enemyCharacter;
        [SerializeField] private TableTotalsView totals;
        [SerializeField] private DeckStackView remainingDeck;
        [SerializeField] private DeckStackView discardDeck;
        [SerializeField] private DeckStackView enemyRemainingDeck;
        [SerializeField] private DeckStackView enemyDiscardDeck;
        [SerializeField] private DeckPreviewView deckPreview;
        [SerializeField] private CodexController codex;
        [SerializeField] private DemonContractSelectionView demonContractSelection;
        [SerializeField] private TutorialNarratorView tutorialNarrator;
        [SerializeField] private TutorialScriptSO tutorialScript;
        private GameSceneCombatHudCommandKind? _tutorialRestrictedPrimaryAction;
        private string _tutorialRestrictedContractDefinitionKey;
        private int? _tutorialRestrictedOptionId;
        private bool _tutorialContractPaperBlocked;
        private bool _tutorialCardUseBlocked;
        private TutorialDirector _tutorialDirector;
        private bool _tutorialIntroCompleted;
        private CrystalOrbSelectionView crystalOrbSelection;
        private SatanNumberSelectionView satanNumberSelection;
        [SerializeField] private Sprite satanBrandSprite;
        [SerializeField] private TableCombatCommandGroup tableCombatCommands;
        [SerializeField] private ContractPaperView contractPapers;
        [SerializeField] private MammonDieView mammonDie;

        [Header("Standalone enemy profile")]
        [SerializeField] private string enemyProfileKey =
            EnemyCombatProfileCatalog.GunslingerKey;

        [Header("Enemy speech")]
        [SerializeField] private int speechSeed = 20260804;
        [SerializeField] private float terminalSpeechHoldSeconds =
            DefaultTerminalSpeechHoldSeconds;

        [Header("Shop (MVP)")]
        [SerializeField] private ShopController shop;

        [Header("Presentation pacing")]
        [SerializeField] private float stepSeconds = 1.0f;
        [SerializeField] private float resolveHoldSeconds = 2.5f;
        [SerializeField, Min(0.01f)] private float comparisonCountSeconds = 0.28f;
        [SerializeField, Min(0f)] private float comparisonStepGapSeconds = 0.12f;

        internal const float MinimumRoundResultHoldSeconds = 2.5f;

        [Header("Revolver animation")]
        [SerializeField] private Animator revolverAnimator;
        [SerializeField] private GameObject revolverRoot;
        [SerializeField] private float revolverReadySeconds = 1.2666668f;
        [SerializeField] private float revolverCameraReturnSeconds = 0.45f;
        [SerializeField] private float revolverAnimationSeconds = 8.8f;
        [SerializeField] private string revolverBaseStateName = "Revolver_Base";
        [SerializeField] private string playerReadyTrigger = "PlayerTurnStart";
        [SerializeField] private string playerSuccessTrigger = "PlayerSuccess";
        [SerializeField] private string playerFailTrigger = "PlayerFail";
        [SerializeField] private string enemySuccessTrigger = "EnemySuccess";
        [SerializeField] private string enemyFailTrigger = "EnemyFail";

        [Header("Knife animation")]
        [SerializeField] private Animator knifeAnimator;
        [SerializeField] private GameObject knifeRoot;
        [SerializeField] private float knifeReadySeconds = 0.7f;
        [SerializeField] private float knifeSuspenseSeconds = 2.5f;
        [SerializeField] private float knifeResultSeconds = 3.1f;
        [SerializeField] private string knifeBaseStateName = "Knife_Empty";
        [SerializeField] private string playerKnifeStartTrigger = "PlayerStart";
        [SerializeField] private string enemyKnifeStartTrigger = "EnemyStart";
        [SerializeField] private string knifeSuccessTrigger = "Success";
        [SerializeField] private string knifeFailTrigger = "Fail";

        [Header("Hammer animation")]
        [SerializeField] private HammerAnimationController hammerAnimation;

        [Header("Satan number guess presentation")]
        [SerializeField, Min(0f)] private float satanNumberGuessCameraDelaySeconds = 0.8f;
        [SerializeField, Min(0f)] private float satanNumberGuessAfterResultHoldSeconds = 0.5f;
        [SerializeField, Min(0.01f)] private float satanNumberGuessFovRiseSpeed = 80f;
        [SerializeField, Min(0.01f)] private float satanNumberGuessFovReturnSpeed = 120f;
        [SerializeField, Min(0.01f)] private float satanNumberGuessChromaticRiseSpeed = 4f;
        [SerializeField, Min(0.01f)] private float satanNumberGuessChromaticReturnSpeed = 2f;
        [SerializeField, Min(0f)] private float cardSelectionHeartFadeOutSeconds = 0.35f;

        private bool _hasLastSatanAttackAnimationCue;
        private int _lastSatanAttackAnimationRoundNumber;
        private int _lastSatanAttackAnimationSourceCardId;
        private CombatantSide _lastSatanAttackAnimationActorSide;
        private int _lastSatanAttackAnimationActionOrdinal;
        private bool _hasLastSatanNumberGuessAnimationCue;
        private int _lastSatanNumberGuessAnimationRoundNumber;
        private int _lastSatanNumberGuessAnimationSourceCardId;
        private CombatantSide _lastSatanNumberGuessAnimationActorSide;
        private int _lastSatanNumberGuessAnimationTargetCardId;
        private bool _lastSatanNumberGuessAnimationSucceeded;
        private int _lastSatanNumberGuessAnimationActionOrdinal;
        private bool _satanNumberGuessSwitchInputLocked;
        private int _satanNumberGuessCardIdToSuppress = -1;
        private CombatantSide _satanNumberGuessSuppressedCardSide =
            CombatantSide.Player;

        [Header("Poison injection announcement")]
        [SerializeField] private PoisonInjectionAnnounceView poisonInjectionAnnounce;
        [SerializeField] private CardContentCatalogSO poisonInjectionCardCatalog;

        [Header("Shop utility animations")]
        [SerializeField] private GameObject lighterAnimationRoot;
        [SerializeField] private Animator lighterAnimator;
        [SerializeField] private GameObject whiskeyAnimationRoot;
        [SerializeField] private Animator whiskeyAnimator;
        [SerializeField] private float whiskeyAnimationSeconds = 5.6f;
        [SerializeField] private float whiskeyDrinkSfxDelaySeconds = 0.93333334f;
        [SerializeField] private string whiskeyDrinkSfxId = "drinkWhiskey";

        private const string WhiskeyAnimationStateName =
            "Base Layer.DrinkWhiskey";
        private const string CardSelectionHeartSlowSfxId =
            "heartSoundSlow";
        private const string CardSelectionHeartFastSfxId =
            "heartSoundFast";

        [Header("Cinematic camera")]
        [SerializeField] private GameSceneCameraViewController cameraViewController;

        private CoreLoopSession _session;
        private StageProgressionSession _stageSession;
        private StageProgressionRuntime _stageRuntime;
        private CoreLoopViewModel _core;
        private Camera _camera;
        private CardView _hoveredCard;
        private DemonCardView _hoveredDemonCard;
        private DeckStackView _hoveredDeckStack;
        private CodexClickable _hoveredCodex;
        private ContractPaperClickable _hoveredContractPaper;
        private ShopUtilityItemView _hoveredShopUtilityItem;
        private TableCombatCommandView _hoveredCombatCommand;
        private HoverDescriptionTarget _hoveredDescriptionTarget;
        private object _hoverBadgeOwner;
        private bool _inputLocked;
        private bool _suppressHandRenderUntilRoundOneStart;
        private bool _pauseInputBlocked;
        private bool _shopUtilityAnimationPlaying;
        private bool _choosingLighterRemoval;
        private GameSceneCardViewModel _pendingLighterBurnCard;
        private int _battleIndex;
        private string _activeEnemyProfileKey;
        private int? _enemyMammonDieValue;
        private bool _hasLastRevolverAnimationCue;
        private int _lastRevolverAnimationRoundNumber;
        private int _lastRevolverAnimationSourceCardId;
        private CombatantSide _lastRevolverAnimationActorSide;
        private GameSceneRevolverAnimationPhase _lastRevolverAnimationPhase;
        private bool _lastRevolverAnimationSucceeded;
        private bool _revolverReadyActive;
        private int _revolverReadyRoundNumber;
        private int _revolverReadySourceCardId;
        private CombatantSide _revolverReadyActorSide;
        private Coroutine _revolverHideRoutine;
        private Coroutine _revolverReadyCameraRoutine;
        private Coroutine _revolverShotRoutine;
        private bool _revolverSelectionReady;
        private bool _revolverSwitchInputLocked;
        private RevolverAnimationEventReceiver _revolverEventReceiver;
        private bool _revolverImpactPending;
        private CombatantSide _revolverImpactTargetSide;
        private bool _hasLastKnifeAnimationCue;
        private int _lastKnifeAnimationRoundNumber;
        private int _lastKnifeAnimationSourceCardId;
        private CombatantSide _lastKnifeAnimationActorSide;
        private GameSceneKnifeAnimationPhase _lastKnifeAnimationPhase;
        private bool _lastKnifeAnimationSucceeded;
        private Coroutine _knifeHideRoutine;
        private KnifeAnimationEventReceiver _knifeEventReceiver;
        private bool _knifeImpactPending;
        private CombatantSide _knifeImpactTargetSide;
        private bool _hasLastPoisonInjectionAnimationCue;
        private int _lastPoisonInjectionAnimationRoundNumber;
        private bool _hammerSwitchInputLocked;
        private bool _enemyCardSelectionSwitchInputLocked;
        private Coroutine _cardSelectionHeartRoutine;
        private SoundManager.SoundHandle _cardSelectionHeartSlowHandle;
        private SoundManager.SoundHandle _cardSelectionHeartFastHandle;
        private bool _satanNumberSelectionHeartSlowActive;
        private SoundManager.SoundHandle _satanNumberSelectionHeartSlowHandle;
        private bool _deckPreviewSwitchInputLocked;
        private bool _codexSwitchInputLocked;
        private bool _returnCameraToCurrentAfterHammer;
        private HammerAnimationController _hammerCameraLockController;
        private HammerAnimationController _playedHammerAnimationController;
        private readonly List<GameSceneViewModel> _timeline = new List<GameSceneViewModel>();
        private readonly List<PurchasedNormalCard> _purchasedNormalCards =
            new List<PurchasedNormalCard>();
        private readonly List<string> _purchasedDemonContractKeys = new List<string>();
        private readonly List<RemovedNormalCard> _removedNormalCards =
            new List<RemovedNormalCard>();
        private StageProgressionSession _completedStageSession;
        private StageProgressionViewModel _formalShopModel;
        private int _formalShopGold;
        private EnemySpeechDirector _enemySpeechDirector;
        private SpeechProfileSO _activeEnemySpeechProfile;
        private Coroutine _terminalSpeechHoldRoutine;
        private CoreLoopBattle _terminalSpeechBattle;
        private bool _terminalSpeechHoldActive;
        private bool _terminalSpeechHoldCompleted;
        private bool _roundComparisonActive;
        private PlayerMammonComparisonPlan _pendingPlayerMammonComparison;
        private CardView _comparisonHighlightedCard;
        private long _lastRoundComparisonResolutionId = -1;

        internal const float DefaultTerminalSpeechHoldSeconds = 1.5f;

        internal static bool IsTerminalSpeechHoldBlocking(
            bool isActive,
            bool isCompleted)
        {
            return isActive && !isCompleted;
        }

        public event Action FormalBattleCompleted;
        public event Action<int> FormalShopCardPurchaseRequested;
        public event Action<int> FormalShopCardRemovalRequested;
        public event Action FormalShopRestRequested;
        public event Action FormalShopLeaveRequested;

        internal string CurrentEnemyProfileKey =>
            string.IsNullOrWhiteSpace(_activeEnemyProfileKey)
                ? ResolveEnemyProfileKey()
                : _activeEnemyProfileKey;

        public CoreLoopBattle Battle => IsStageBattle
            ? _stageSession.Battle
            : _session?.Battle;

        private bool IsModalInputBlocked =>
            _pauseInputBlocked ||
            _shopUtilityAnimationPlaying ||
            (codex != null && codex.IsOpen);

        public bool BindBattle(StageProgressionSession session, bool unlockInput = true)
        {
            if (session == null ||
                session.Progress.State != StageProgressionState.InBattle ||
                session.Battle == null)
            {
                return false;
            }

            SetBattleCardObjectsVisible(true);

            if (ReferenceEquals(_stageSession, session) &&
                ReferenceEquals(Battle, session.Battle))
            {
                return true;
            }

            ResetBattlePresentation();
            _session = null;
            _stageSession = session;
            _completedStageSession = null;
            _activeEnemyProfileKey =
                session.ActiveStage?.BattleProfileKey ??
                ResolveEnemyProfileKey();
            _activeEnemySpeechProfile = ResolveActiveEnemySpeechProfile();
            enemyCharacter?.ExitMerchant();
            enemyCharacter?.TrySetEnemyProfile(_activeEnemyProfileKey);
            ApplyEnemyDeckTopTint();
            _inputLocked = !unlockInput;

            _tutorialDirector = null;
            _tutorialIntroCompleted = false;
            _tutorialContractPaperBlocked = false;
            _tutorialCardUseBlocked = false;
            if (session.IsTutorialRun &&
                tutorialNarrator != null &&
                tutorialScript != null &&
                session.ActiveStage?.Id == TutorialBattleFactory.TutorialStageId)
            {
                _tutorialDirector = new TutorialDirector(this, tutorialNarrator, tutorialScript);
                _tutorialDirector.IntroCompleted += HandleTutorialDirectorIntroCompleted;
                _tutorialDirector.RoundOneRecapCompleted +=
                    HandleTutorialRoundOneRecapCompleted;
                _tutorialDirector.Completed += HandleTutorialDirectorCompleted;
                // Only the contract-candidate gate explicitly lifts this — every other
                // gate (and every dialogue step, which already blocks all input via the
                // narrator) needs the contract paper to stay unclickable so the single
                // highlighted action is genuinely the only thing the player can do.
                _tutorialContractPaperBlocked = true;
                // Same default-deny reasoning as the contract paper above: a card dealt
                // earlier in the round (e.g. the round-3 face-up Bowie Knife, rank 5-10 and
                // therefore usable) can still sit in the player's hand during a later gate
                // that never meant to allow card use (e.g. the contract-candidate gate) —
                // SetTutorialActionRestriction only ever covered Hit/Stand/Change, never card
                // clicks. Only RevolverGate, which needs the player to click their own
                // revolver-ranked card, explicitly lifts this.
                _tutorialCardUseBlocked = true;
            }

            RefreshView();
            return true;
        }

        /// <summary>Fires once the tutorial's intro (sections 0-1) dialogue finishes.</summary>
        internal event Action TutorialIntroCompleted;

        /// <summary>
        /// True from the moment a tutorial battle binds until its intro dialogue finishes.
        /// <see cref="GameFlowController"/> uses this to hold the enemy-entrance animation and
        /// round-1 card-deal reveal off until the player has read through it.
        /// </summary>
        internal bool HasPendingTutorialIntro =>
            _tutorialDirector != null && !_tutorialIntroCompleted;

        internal void BeginTutorialIntroIfNeeded()
        {
            if (_tutorialDirector == null || _tutorialIntroCompleted)
            {
                return;
            }

            _tutorialDirector.BeginIntro();
        }

        private void HandleTutorialDirectorIntroCompleted()
        {
            _tutorialIntroCompleted = true;
            TutorialIntroCompleted?.Invoke();
        }

        // Mirrors GameFlowController's RoundOneCardRevealAnimationSeconds — round 2's deal
        // (held back since the Stand gate) needs the same brief wait after reveal before the
        // tutorial's next dialogue line is allowed to appear.
        private const float TutorialRoundTwoRevealAnimationSeconds = 0.3f;

        private void HandleTutorialRoundOneRecapCompleted()
        {
            StartCoroutine(RevealRoundTwoAfterRecapRoutine());
        }

        private IEnumerator RevealRoundTwoAfterRecapRoutine()
        {
            // Reuses the round-1 reveal machinery — it's just "stop suppressing hand
            // render and refresh," which is exactly what round 2's held-back deal needs too.
            RevealRoundOneHands();
            yield return new WaitForSeconds(TutorialRoundTwoRevealAnimationSeconds);
            _tutorialDirector?.NotifyRoundTwoRevealReady();
        }

        /// <summary>
        /// The tutorial is a single scripted battle with no progression scene of its own —
        /// once its final line is dismissed, tear down its throwaway (in-memory-backed)
        /// runtime singleton and return straight to the main menu, mirroring
        /// <c>StageProgressionRuntime.CreateTutorialInstance</c>'s outbound trip.
        /// </summary>
        private void HandleTutorialDirectorCompleted()
        {
            StageProgressionRuntime runtime = _stageRuntime;
            StageProgressionRuntime.DestroyInstanceForSceneTransition();
            runtime?.LoadMainMenuScene();
        }

        /// <summary>
        /// Sets the enemy character's visual appearance (sprite/merchant-mode exit)
        /// for the upcoming battle without touching table/card/HUD state — meant to
        /// run *before* the enemy's entrance animation plays, so the entrance shows
        /// the actual opponent instead of whatever the character was last displaying
        /// (e.g. a previous stage's enemy, or the merchant look), which would
        /// otherwise abruptly swap the instant the full <see cref="BindBattle"/>
        /// (deferred until after entrance) finally runs.
        /// </summary>
        internal void PrepareEnemyAppearance(StageProgressionSession session)
        {
            string profileKey = session?.ActiveStage?.BattleProfileKey ??
                ResolveEnemyProfileKey();
            enemyCharacter?.ExitMerchant();
            enemyCharacter?.TrySetEnemyProfile(profileKey);
            // ExitMerchant only resets sprite/scale/tint, not the speech bubble — without
            // this, the merchant's last line (e.g. the starting-demon-reveal greeting)
            // stays on screen through the whole entrance animation, reading as if the
            // newly-appearing enemy said it.
            enemyCharacter?.HideSpeech();
        }

        /// <summary>
        /// Tutorial-only override: when set, only <paramref name="allowedAction"/> among
        /// Hit/Stand/Change stays interactable — the presentation layer forces the other two
        /// off regardless of what CoreLoop would otherwise allow. Also forces the matching
        /// table button to show its hovered highlight so the restriction reads as an
        /// intentional prompt rather than two buttons randomly going dark. Pass null to lift
        /// the restriction. Not tied to any script trigger yet — the tutorial director (layer D)
        /// is what will actually call this at the right beat.
        /// </summary>
        internal void SetTutorialActionRestriction(
            GameSceneCombatHudCommandKind? allowedAction)
        {
            _tutorialRestrictedPrimaryAction = allowedAction;
            RefreshView();
        }

        /// <summary>
        /// Tutorial-only override: leaves the revolver/lie-detector number dial fully
        /// navigable, but keeps its Confirm button disabled until the player dials to
        /// <paramref name="number"/> themselves. Pass null to lift the restriction. See
        /// <see cref="SetTutorialActionRestriction"/> for the same "mechanism now, trigger
        /// later" split.
        /// </summary>
        internal void SetTutorialRevolverTargetNumber(int? number)
        {
            hud?.SetTutorialRevolverTargetNumber(number);
        }

        /// <summary>
        /// Tutorial-only override: when set, only the contract candidate with this
        /// <c>DefinitionKey</c> stays clickable among the (at most 2) demon-contract
        /// candidates — the world-space raycast click path is gated directly (see
        /// <see cref="HandleDemonContractSelectionInput"/>), not just the presentation layer,
        /// since that path does not otherwise consult <c>IsInteractable</c> before dispatching.
        /// Pass null to lift the restriction.
        /// </summary>
        internal void SetTutorialContractRestriction(string definitionKey)
        {
            _tutorialRestrictedContractDefinitionKey = definitionKey;
            RefreshView();
        }

        /// <summary>
        /// Tutorial-only override: when set, only the demon-contract option with this id stays
        /// clickable (e.g. Asmodeus's turn-start "능력 사용하기"). Pass null to lift.
        /// </summary>
        internal void SetTutorialContractOptionRestriction(int? optionId)
        {
            _tutorialRestrictedOptionId = optionId;
            RefreshView();
        }

        /// <summary>
        /// Tutorial-only override: when true, the contract paper renders non-interactable
        /// regardless of <c>PlayerDemonContractAvailability.CanBegin</c> — needed because,
        /// unlike Hit/Stand/Change, starting a demon contract is not gated by
        /// <see cref="SetTutorialActionRestriction"/> at all (it is a separate world object
        /// with its own always-on click path). Defaults to true for the whole tutorial the
        /// moment it binds; only the contract-candidate gate explicitly sets it false.
        /// </summary>
        internal void SetTutorialContractPaperBlocked(bool blocked)
        {
            _tutorialContractPaperBlocked = blocked;
            RefreshView();
        }

        /// <summary>
        /// Tutorial-only override: when true, clicking any player battle card to use it is
        /// ignored regardless of <c>CanUse</c> — the click-dispatch site consults this
        /// directly (see the <c>pointedBattleCard.CanUse</c> check), the same "gate at
        /// dispatch, not just presentation" approach already used for the contract-candidate
        /// restriction. Defaults to true for the whole tutorial the moment it binds; only the
        /// revolver gate, which needs the player to click their own revolver-ranked card,
        /// explicitly sets it false.
        /// </summary>
        internal void SetTutorialCardUseBlocked(bool blocked)
        {
            _tutorialCardUseBlocked = blocked;
        }

        /// <summary>
        /// Called once the round-1 entrance + card-deal reveal animation has fully finished
        /// playing — only then may the tutorial's post-intro dialogue actually appear, so it
        /// doesn't race that animation (see <see cref="TutorialDirector.NotifyRoundOneRevealReady"/>).
        /// </summary>
        internal void NotifyTutorialRoundOneRevealReady()
        {
            _tutorialDirector?.NotifyRoundOneRevealReady();
        }

        /// <summary>
        /// Suppresses player/enemy hand rendering — everything else BindBattle's
        /// first render produces (deck piles, table buttons, contract papers, HUD
        /// text) still renders normally. Meant to be called right before
        /// <see cref="BindBattle"/> at enemy-entrance-end, so round 1's
        /// already-dealt cards (CoreLoop deals them synchronously in
        /// <c>Start()</c>) don't visually pop in before the post-entrance hold
        /// finishes.
        /// </summary>
        internal void SuppressHandRenderUntilRoundOneStart()
        {
            _suppressHandRenderUntilRoundOneStart = true;
        }

        /// <summary>
        /// Ends the suppression begun by <see cref="SuppressHandRenderUntilRoundOneStart"/>
        /// and re-renders immediately so the now-dealt hands animate in for the
        /// first time (CardHand's entry tween fires on any card id it hasn't
        /// rendered before).
        /// </summary>
        internal void RevealRoundOneHands()
        {
            _suppressHandRenderUntilRoundOneStart = false;
            RefreshView();
        }

        public void UnbindBattle()
        {
            if (_stageSession == null && _session == null)
            {
                return;
            }

            ResetBattlePresentation();
            _stageSession = null;
            _session = null;
            _completedStageSession = null;
        }

        public bool BindFormalShop(
            StageProgressionViewModel model,
            int currentGold,
            bool unlockInput = true)
        {
            if (model == null || !model.IsShop || shop == null)
            {
                return false;
            }

            if (_stageSession != null || _session != null)
            {
                UnbindBattle();
            }

            bool keepLighterSelection = _choosingLighterRemoval &&
                deckPreview != null &&
                deckPreview.IsSingleSelection;
            _formalShopModel = model;
            _formalShopGold = currentGold;
            _inputLocked = !unlockInput;
            _choosingLighterRemoval = keepLighterSelection;
            shop.OpenFormal(model);
            SetBattleCardObjectsVisible(false);
            if (keepLighterSelection)
            {
                deckPreview.OpenForSingleSelection(
                    CreateFormalLighterRemovalPreview());
            }

            hud?.SetGold(currentGold);
            hud?.SetPlayerSoul(model.PlayerSoul);
            hud?.SetEnemyStatusVisible(false);
            UpdateShopLeaveControl();
            return true;
        }

        internal void SetPresentationInputLocked(bool locked)
        {
            _inputLocked = locked;
            UpdateShopLeaveControl();
            // The combat HUD's Hit/Stand/Change/contract buttons are driven by a
            // GameSceneCombatHudViewModel snapshot that only gets rebuilt on
            // specific battle events (ApplyView), not whenever _inputLocked
            // changes on its own — without this, unlocking here updates the flag
            // but the buttons stay stuck showing (and behaving as) disabled until
            // some unrelated event happens to re-render them.
            RefreshView();
        }

        internal void SetEnemyDeckVisible(bool visible)
        {
            SetComponentActive(enemyRemainingDeck, visible);
            SetComponentActive(enemyDiscardDeck, visible);
        }

        internal void SetBattleCardObjectsVisible(bool visible)
        {
            if (!visible)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateDeckStackHover(null);
            }

            SetComponentActive(playerHand, visible);
            SetComponentActive(enemyHand, visible);
            SetComponentActive(remainingDeck, visible);
            SetComponentActive(discardDeck, visible);
            SetComponentActive(enemyRemainingDeck, visible);
            SetComponentActive(enemyDiscardDeck, visible);
        }

        private static void SetComponentActive(Component component, bool active)
        {
            if (component != null)
            {
                component.gameObject.SetActive(active);
            }
        }

        public void UnbindFormalShop()
        {
            if (_formalShopModel == null && (shop == null || !shop.IsFormal))
            {
                return;
            }

            _formalShopModel = null;
            _formalShopGold = 0;
            _choosingLighterRemoval = false;
            UpdateHover(null);
            UpdateDemonCardHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            shop?.CloseFormal();
            UpdateShopLeaveControl();
        }

        public void SetPauseInputBlocked(bool blocked)
        {
            _pauseInputBlocked = blocked;
            UpdateShopLeaveControl();
            if (!blocked)
            {
                return;
            }

            UpdateHover(null);
            UpdateDemonCardHover(null);
            demonContractSelection?.SetHovered(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            hud?.HideDemonContractDetail();
        }

        internal void CompleteFormalLighterRemoval(bool succeeded)
        {
            if (succeeded)
            {
                _choosingLighterRemoval = false;
                CloseDeckPreview();
                shop?.ShowMerchantSpeech(SpeechCueKeys.ShopLighterSuccess);
            }
            else if (deckPreview != null && deckPreview.IsSingleSelection)
            {
                _choosingLighterRemoval = true;
                if (_formalShopModel != null)
                {
                    deckPreview.OpenForSingleSelection(
                        CreateFormalLighterRemovalPreview());
                }
            }

            if (!succeeded)
            {
                _pendingLighterBurnCard = null;
                shop?.ShowMerchantSpeech(SpeechCueKeys.ShopUnavailable);
            }

            RefreshShopUtilityItems();
            UpdateShopLeaveControl();
        }

        internal void CompleteFormalShopCardPurchase(bool succeeded)
        {
            shop?.ShowMerchantSpeech(succeeded
                ? SpeechCueKeys.ShopPurchaseSuccess
                : SpeechCueKeys.ShopUnavailable);
        }

        internal void CompleteFormalShopRest(bool succeeded)
        {
            shop?.ShowMerchantSpeech(succeeded
                ? SpeechCueKeys.ShopWhiskeySuccess
                : SpeechCueKeys.ShopUnavailable);
        }

        internal void CompleteFormalShopLeave(bool succeeded)
        {
            shop?.ShowMerchantSpeech(succeeded
                ? SpeechCueKeys.ShopFarewell
                : SpeechCueKeys.ShopUnavailable);
        }

        public bool TryCloseTransientOverlay()
        {
            if (codex != null && codex.IsOpen)
            {
                CloseCodex();
                return true;
            }

            if (deckPreview == null || !deckPreview.IsOpen)
            {
                return false;
            }

            if (_choosingLighterRemoval)
            {
                return CancelLighterRemoval();
            }

            CloseDeckPreview();
            return true;
        }

        private void Awake()
        {
            _enemySpeechDirector = new EnemySpeechDirector(speechSeed);
            HideRevolverAnimation();
            HideKnifeAnimation();
            ResolveHammerAnimation()?.ResetPresentationState();
            ResetSatanAttackAnimationState();
            ResetSatanNumberGuessAnimationState();
            _stageRuntime = StageProgressionRuntime.Instance;
            StageProgressionSession runtimeSession =
                _stageRuntime?.FormalSession?.CombatSession ??
                _stageRuntime?.Session;
            bool hasActiveFormalBattle = runtimeSession != null &&
                runtimeSession.Progress.State == StageProgressionState.InBattle &&
                runtimeSession.Battle != null;
            if (hasActiveFormalBattle)
            {
                _stageSession = runtimeSession;
                _activeEnemyProfileKey =
                    runtimeSession.ActiveStage?.BattleProfileKey ??
                    ResolveEnemyProfileKey();
            }
            else
            {
                _activeEnemyProfileKey = ResolveEnemyProfileKey();
                _session = new CoreLoopSession(CreateBattle);
            }

            _activeEnemySpeechProfile = ResolveActiveEnemySpeechProfile();

            ResolveDeckStackReferences();
            if (enemyCharacter != null)
            {
                enemyCharacter.TrySetEnemyProfile(_activeEnemyProfileKey);
            }
            ApplyEnemyDeckTopTint();
            EnsureDeckPreview();
            codex ??= GetComponent<CodexController>();
            demonContractSelection ??=
                GetComponent<DemonContractSelectionView>();
            crystalOrbSelection ??=
                GetComponent<CrystalOrbSelectionView>();
            crystalOrbSelection ??=
                gameObject.AddComponent<CrystalOrbSelectionView>();
            crystalOrbSelection.Initialize(playerHand?.CardPrefab);
            satanNumberSelection ??=
                GetComponent<SatanNumberSelectionView>();
            satanNumberSelection ??=
                gameObject.AddComponent<SatanNumberSelectionView>();
            satanNumberSelection.Initialize(
                playerHand?.CardPrefab,
                satanBrandSprite);
            tableCombatCommands ??= FindFirstObjectByType<TableCombatCommandGroup>(
                FindObjectsInactive.Include);
            contractPapers ??= FindFirstObjectByType<ContractPaperView>(
                FindObjectsInactive.Include);
            tutorialNarrator ??= FindFirstObjectByType<TutorialNarratorView>(
                FindObjectsInactive.Include);
            mammonDie ??= FindFirstObjectByType<MammonDieView>(
                FindObjectsInactive.Include);
            EnsureMammonDie();

            if (hud != null)
            {
                hud.CombatCommandRequested += HandleCombatCommand;
                hud.ShopLeaveRequested += HandleShopLeaveRequested;
            }
        }

        private void Start()
        {
            if (Battle != null)
            {
                RefreshView();
            }
        }

        private void OnEnable()
        {
            BindRevolverImpactEvent();
            BindKnifeImpactEvent();
            if (deckPreview != null)
            {
                deckPreview.HoverBadgeRequested +=
                    HandleDeckPreviewHoverBadgeRequested;
                deckPreview.HoverBadgeCleared +=
                    HandleDeckPreviewHoverBadgeCleared;
                deckPreview.SelectionConfirmed +=
                    HandleLighterSelectionConfirmed;
                deckPreview.SelectionCancelled +=
                    HandleLighterSelectionCancelled;
            }

            if (codex != null)
            {
                codex.OpenStateChanged += HandleCodexOpenStateChanged;
                codex.HoverBadgeRequested +=
                    HandleCodexHoverBadgeRequested;
                codex.HoverBadgeCleared +=
                    HandleCodexHoverBadgeCleared;
            }
        }

        private void OnDisable()
        {
            CancelRoundComparison(resetResolutionHistory: true);
            StopRevolverHideRoutine();
            StopRevolverReadyCameraRoutine();
            StopRevolverShotRoutine();
            EndRevolverSwitchInputLock();
            UnbindRevolverImpactEvent();
            ClearPendingRevolverImpact();
            UnbindKnifeImpactEvent();
            ClearPendingKnifeImpact();
            CloseDeckPreview();
            CloseCodex();
            EndEnemyCardSelectionCamera();
            EndSatanNumberGuessCameraSequence();
            PresentationManager.Current?.ForceRestoreTransientCameraEffects();
            ResetSatanAttackAnimationState();
            ResetSatanNumberGuessAnimationState();
            if (deckPreview != null)
            {
                deckPreview.HoverBadgeRequested -=
                    HandleDeckPreviewHoverBadgeRequested;
                deckPreview.HoverBadgeCleared -=
                    HandleDeckPreviewHoverBadgeCleared;
                deckPreview.SelectionConfirmed -=
                    HandleLighterSelectionConfirmed;
                deckPreview.SelectionCancelled -=
                    HandleLighterSelectionCancelled;
            }

            if (codex != null)
            {
                codex.OpenStateChanged -= HandleCodexOpenStateChanged;
                codex.HoverBadgeRequested -=
                    HandleCodexHoverBadgeRequested;
                codex.HoverBadgeCleared -=
                    HandleCodexHoverBadgeCleared;
            }
            demonContractSelection?.Hide();
            crystalOrbSelection?.Hide();
            satanNumberSelection?.Hide();
            tutorialNarrator?.Hide();
            hud?.HideDemonContractDetail();
            hud?.SetShopLeaveState(visible: false, interactable: false);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateCombatCommandHover(null);
            ResetShopUtilityAnimations();
        }

        private void OnDestroy()
        {
            EndEnemyCardSelectionCamera();
            EndSatanNumberGuessCameraSequence();
            PresentationManager.Current?.ForceRestoreTransientCameraEffects();
            if (hud != null)
            {
                hud.CombatCommandRequested -= HandleCombatCommand;
                hud.ShopLeaveRequested -= HandleShopLeaveRequested;
            }
        }

        private void ResetBattlePresentation()
        {
            StopAllCoroutines();
            CancelRoundComparison(resetResolutionHistory: true);
            EndSatanNumberGuessCameraSequence();
            PresentationManager.Current?.ForceRestoreTransientCameraEffects();
            ResetSatanNumberGuessAnimationState();
            CoreLoopBattle battle = Battle;
            if (battle != null)
            {
                battle.Stepped -= OnBattleStepped;
            }

            _timeline.Clear();
            _core = null;
            _inputLocked = true;
            _choosingLighterRemoval = false;
            UpdateHover(null);
            UpdateDemonCardHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            demonContractSelection?.SetHovered(null);
            demonContractSelection?.Hide();
            crystalOrbSelection?.Hide();
            satanNumberSelection?.Hide();
            tutorialNarrator?.Hide();
            contractPapers?.Render(null);
            hud?.HideCardHoverBadge();
            hud?.HideDemonContractDetail();
            hud?.Render(null);
            hud?.SetShopLeaveState(visible: false, interactable: false);
            tableCombatCommands?.ResetView();
            CloseDeckPreview();
            CloseCodex();
            EndEnemyCardSelectionCamera();
            EndHammerSwitchInputLock();
            ResolveHammerAnimation()?.ResetPresentationState();
            ResetSatanAttackAnimationState();
            ResetRevolverAnimationState();
            ResetKnifeAnimationState();
            ResetPoisonInjectionAnimationState();
            ResetShopUtilityAnimations();
            playerHand?.ResetView();
            enemyHand?.ResetView();
            remainingDeck?.ResetView();
            discardDeck?.ResetView();
            enemyRemainingDeck?.ResetView();
            enemyDiscardDeck?.ResetView();
            totals?.Render(string.Empty, string.Empty);
            enemyCharacter?.RenderVisual(CharacterVisualState.Idle);
            if (shop != null)
            {
                if (shop.IsFormal)
                {
                    shop.CloseFormal();
                }
                else
                {
                    shop.Close();
                }
            }

            ResetEnemySpeech();
        }

        // Diegetic input: hover any card to enlarge it (usable cards also show a HUD badge), click a
        // legal card-effect target to resolve that choice, or click a usable player card to activate
        // its effect. New Input System — legacy OnMouseDown does not fire, so we raycast the pointer
        // ourselves. Table commands and the contract share this same raycast path.
        private void Update()
        {
            UpdateShopPriceBadges();

            if (_deckPreviewSwitchInputLocked &&
                (deckPreview == null || !deckPreview.IsOpen))
            {
                EndDeckPreviewSwitchInputLock();
            }

            if (_codexSwitchInputLocked &&
                (codex == null || !codex.IsOpen))
            {
                EndCodexSwitchInputLock();
            }

            if (IsModalInputBlocked)
            {
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateCombatCommandHover(null);
                UpdateHoverDescriptionTarget(null);
                if (codex == null || !codex.IsOpen)
                {
                    hud?.HideCardHoverBadge();
                }
                return;
            }

            bool shopOpen = shop != null && shop.IsOpen;
            if (_core == null && !shopOpen)
            {
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateCombatCommandHover(null);
                UpdateHoverDescriptionTarget(null);
                hud?.HideCardHoverBadge();
                return;
            }

            if (_inputLocked)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateShopUtilityItemHover(null);
                UpdateCombatCommandHover(null);
                UpdateHoverDescriptionTarget(null);
                demonContractSelection?.SetHovered(null);
                crystalOrbSelection?.SetHovered(null);
                satanNumberSelection?.SetHovered(null);
                hud?.HideCardHoverBadge();
                hud?.HideDemonContractDetail();
                return;
            }

            if (LighterDragTriggerController.BlocksBackgroundInteraction)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateShopUtilityItemHover(null);
                UpdateHoverDescriptionTarget(null);
                demonContractSelection?.SetHovered(null);
                crystalOrbSelection?.SetHovered(null);
                satanNumberSelection?.SetHovered(null);
                hud?.HideCardHoverBadge();
                hud?.HideDemonContractDetail();
                return;
            }

            if (hud != null && hud.IsRevolverNumberSelectionOpen)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateShopUtilityItemHover(null);
                UpdateCombatCommandHover(null);
                UpdateHoverDescriptionTarget(null);
                demonContractSelection?.SetHovered(null);
                crystalOrbSelection?.SetHovered(null);
                satanNumberSelection?.SetHovered(null);
                hud.HideCardHoverBadge();
                hud.HideDemonContractDetail();
                return;
            }

            bool hasHit = RaycastPointer(out RaycastHit hit);
            if (demonContractSelection != null &&
                demonContractSelection.IsOpen)
            {
                HandleDemonContractSelectionInput(hasHit, hit);
                return;
            }

            if (crystalOrbSelection != null && crystalOrbSelection.IsOpen)
            {
                HandleCrystalOrbSelectionInput(hasHit, hit);
                return;
            }

            if (satanNumberSelection != null && satanNumberSelection.IsOpen)
            {
                HandleSatanNumberSelectionInput(hasHit, hit);
                return;
            }

            CardView pointedCard = hasHit
                ? hit.collider.GetComponentInParent<CardView>()
                : null;
            DemonCardView pointedDemonCard = hasHit
                ? hit.collider.GetComponentInParent<DemonCardView>()
                : null;
            CardView pointedBattleCard = shopOpen ? null : pointedCard;
            CardView pointedShopCard = shopOpen && pointedDemonCard == null
                ? pointedCard
                : null;
            ShopUtilityItemView pointedShopUtilityItem = shopOpen && hasHit
                ? hit.collider.GetComponentInParent<ShopUtilityItemView>()
                : null;
            TableCombatCommandView pointedCombatCommand = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<TableCombatCommandView>()
                : null;
            DeckClickable pointedDeck = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<DeckClickable>()
                : null;
            DeckStackView pointedDeckStack =
                ResolvePointedDeckStack(pointedDeck);
            CodexClickable pointedCodex = hasHit &&
                codex != null &&
                codex.IsAvailable
                    ? hit.collider.GetComponentInParent<CodexClickable>()
                    : null;
            // A non-interactable contract paper (the decorative one underneath) has its
            // collider disabled by ContractPaperClickable.SetInteractable, so the
            // raycast physically can't hit it — no extra gating needed here.
            ContractPaperClickable pointedContractPaper = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<ContractPaperClickable>()
                : null;
            HoverDescriptionTarget pointedHoverDescriptionTarget = hasHit
                ? hit.collider.GetComponentInParent<HoverDescriptionTarget>()
                : null;
            if (pointedCard != null || pointedDemonCard != null)
            {
                pointedHoverDescriptionTarget = null;
            }

            if (deckPreview != null && deckPreview.IsOpen)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateContractPaperHover(null);
                UpdateShopUtilityItemHover(null);
                UpdateCombatCommandHover(null);
                UpdateHoverDescriptionTarget(null);

                return;
            }

            UpdateHover(shopOpen ? pointedShopCard : pointedBattleCard);
            UpdateDemonCardHover(pointedDemonCard);
            UpdateDeckStackHover(pointedDeckStack);
            UpdateCodexHover(pointedCodex);
            UpdateContractPaperHover(pointedContractPaper);
            UpdateCardHoverBadge();
            UpdateShopUtilityItemHover(pointedShopUtilityItem);
            TableCombatCommandView effectiveCombatCommandHover =
                _tutorialRestrictedPrimaryAction.HasValue
                    ? tableCombatCommands?.GetView(
                        _tutorialRestrictedPrimaryAction.Value)
                    : pointedCombatCommand;
            UpdateCombatCommandHover(effectiveCombatCommandHover);
            UpdateHoverDescriptionTarget(pointedHoverDescriptionTarget);

            // The narrator still owns every click while active (advancing dialogue), but
            // hover updates above already ran normally — other objects (deck, codex,
            // contract paper, hover-description targets) still preview on hover during
            // dialogue, they just can't be clicked into their own actions.
            if (tutorialNarrator != null && tutorialNarrator.IsActive)
            {
                Mouse tutorialMouse = Mouse.current;
                if (tutorialMouse != null && tutorialMouse.leftButton.wasPressedThisFrame)
                {
                    tutorialNarrator.HandleClick();
                }

                return;
            }

            if (_inputLocked || _choosingLighterRemoval)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (pointedCodex != null && codex != null && codex.IsAvailable)
            {
                CloseDeckPreview();
                if (Battle == null)
                {
                    codex.Open();
                }
                else
                {
                    codex.Open(CurrentEnemyProfileKey);
                }
                return;
            }

            if (pointedContractPaper != null &&
                pointedContractPaper.IsInteractable)
            {
                CloseDeckPreview();
                ProcessInput(TryBeginPlayerDemonContract);
                return;
            }

            MammonDieView pointedMammonDie = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<MammonDieView>()
                : null;
            if (pointedMammonDie != null && pointedMammonDie.IsInteractable)
            {
                TryStartPlayerMammonPhysicalRoll();
                return;
            }

            if (pointedDeck != null)
            {
                OpenDeckPreview(pointedDeck.Kind);
                return;
            }

            if (pointedCombatCommand != null &&
                pointedCombatCommand.TryGetCommand(
                    out GameSceneCombatHudCommand combatCommand))
            {
                HandleCombatCommand(combatCommand);
                return;
            }

            if (pointedBattleCard != null &&
                pointedBattleCard.DirectSelectionCommand.HasValue)
            {
                HandleCombatCommand(
                    pointedBattleCard.DirectSelectionCommand.Value);
                return;
            }

            if (pointedBattleCard != null &&
                pointedBattleCard.CanUse &&
                !_tutorialCardUseBlocked)
            {
                int cardId = pointedBattleCard.CardId;
                ProcessInput(() => TryBeginPlayerCardUse(cardId));
                return;
            }

            if (!shopOpen &&
                pointedDemonCard != null &&
                pointedDemonCard.CanUse)
            {
                int cardId = pointedDemonCard.CardId;
                ProcessInput(() =>
                    TryBeginPlayerActiveDemonContractAction(cardId));
                return;
            }

            if (pointedShopCard != null)
            {
                PurchaseShopNormalCard(pointedShopCard);
                return;
            }

            if (shopOpen && pointedDemonCard != null)
            {
                PurchaseShopDemonCard(pointedDemonCard);
                return;
            }

            if (pointedShopUtilityItem != null)
            {
                UseShopUtilityItem(pointedShopUtilityItem);
            }
        }

        private bool RaycastPointer(out RaycastHit hit)
        {
            hit = default;
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Mouse mouse = Mouse.current;
            if (_camera == null || mouse == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            return Physics.Raycast(ray, out hit, 200f);
        }

        private void EnsureMammonDie()
        {
            if (mammonDie != null || discardDeck == null)
            {
                return;
            }

            MammonDieView prefab = Resources.Load<MammonDieView>(
                "Prefabs/MammonDie_Prototype");
            if (prefab == null)
            {
                return;
            }

            mammonDie = Instantiate(
                prefab,
                discardDeck.transform.parent);
            mammonDie.name = "MammonDie";
            mammonDie.transform.position = discardDeck.transform.position +
                new Vector3(0.85f, 0.2f, 0.08f);
            mammonDie.transform.rotation = Quaternion.identity;
        }

        private void HandleDemonContractSelectionInput(
            bool hasHit,
            RaycastHit hit)
        {
            DemonCardView pointed = hasHit
                ? hit.collider.GetComponentInParent<DemonCardView>()
                : null;
            if (pointed != null &&
                !demonContractSelection.Contains(pointed))
            {
                pointed = null;
            }

            UpdateHover(null);
            UpdateDemonCardHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            if (_tutorialRestrictedContractDefinitionKey != null)
            {
                demonContractSelection.SetHoveredByDefinitionKey(
                    _tutorialRestrictedContractDefinitionKey);
            }
            else
            {
                demonContractSelection.SetHovered(pointed);
            }

            GameSceneCombatHudContractCandidateViewModel candidate =
                demonContractSelection.GetCandidate(pointed);
            if (candidate == null)
            {
                hud?.HideDemonContractDetail();
            }
            else
            {
                hud?.ShowDemonContractDetail(candidate);
            }

            if (_inputLocked || candidate == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null ||
                !mouse.leftButton.wasPressedThisFrame ||
                !candidate.IsInteractable)
            {
                return;
            }

            ProcessInput(() => TryResolvePlayerDemonContract(
                candidate.Command.InteractionId,
                candidate.Command.OptionId));
        }

        private void HandleCrystalOrbSelectionInput(
            bool hasHit,
            RaycastHit hit)
        {
            CardView pointed = hasHit
                ? hit.collider.GetComponentInParent<CardView>()
                : null;
            if (pointed != null && !crystalOrbSelection.Contains(pointed))
            {
                pointed = null;
            }

            UpdateHover(pointed);
            UpdateDemonCardHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            demonContractSelection?.SetHovered(null);
            crystalOrbSelection.SetHovered(pointed);
            hud?.HideDemonContractDetail();
            UpdateCardHoverBadge();

            GameSceneCardViewModel candidate =
                crystalOrbSelection.GetCandidate(pointed);
            if (_inputLocked ||
                candidate == null ||
                !candidate.DirectSelectionCommand.HasValue)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            HandleCombatCommand(candidate.DirectSelectionCommand.Value);
        }

        private void HandleSatanNumberSelectionInput(
            bool hasHit,
            RaycastHit hit)
        {
            CardView pointed = hasHit
                ? hit.collider.GetComponentInParent<CardView>()
                : null;
            if (pointed != null && !satanNumberSelection.Contains(pointed))
            {
                pointed = null;
            }

            UpdateHover(pointed, updateCardVisual: false);
            UpdateDemonCardHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            demonContractSelection?.SetHovered(null);
            crystalOrbSelection?.SetHovered(null);
            satanNumberSelection.SetHovered(pointed);
            hud?.HideDemonContractDetail();
            UpdateCardHoverBadge();

            GameSceneCardViewModel candidate =
                satanNumberSelection.GetCandidate(pointed);
            if (_inputLocked || candidate == null ||
                !candidate.DirectSelectionCommand.HasValue)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            PendingDemonContractInteraction pending =
                Battle?.PendingPlayerDemonContractInteraction;
            if (pending == null)
            {
                return;
            }

            if (pending.Kind ==
                DemonContractInteractionKind.SatanDeclareSecondNumber)
            {
                HandleCombatCommand(candidate.DirectSelectionCommand.Value);
                return;
            }

            if (pending.Kind ==
                    DemonContractInteractionKind.SatanDeclareFirstNumber &&
                satanNumberSelection.TryToggleSelection(pointed))
            {
                RefreshSatanSelectionHud();
            }
        }

        private void UpdateHover(
            CardView pointed,
            bool updateCardVisual = true)
        {
            if (pointed == _hoveredCard)
            {
                return;
            }

            if (_hoveredCard != null)
            {
                bool previousIsSatanSelectionCard =
                    satanNumberSelection != null &&
                    satanNumberSelection.Contains(_hoveredCard);
                if (updateCardVisual || !previousIsSatanSelectionCard)
                {
                    _hoveredCard.SetHovered(false);
                }
            }

            _hoveredCard = pointed;
            if (_hoveredCard != null && updateCardVisual)
            {
                _hoveredCard.SetHovered(true);
            }
        }

        private DeckStackView ResolvePointedDeckStack(DeckClickable pointedDeck)
        {
            if (pointedDeck == null)
            {
                return null;
            }

            DeckStackView pointedStack =
                pointedDeck.GetComponentInParent<DeckStackView>();
            if (pointedStack == null)
            {
                return null;
            }

            if (pointedDeck.Kind == DeckKind.Draw && pointedStack == remainingDeck)
            {
                return pointedStack;
            }

            return pointedDeck.Kind == DeckKind.Discard && pointedStack == discardDeck
                ? pointedStack
                : null;
        }

        private void UpdateDeckStackHover(DeckStackView pointed)
        {
            if (pointed == _hoveredDeckStack)
            {
                return;
            }

            if (_hoveredDeckStack != null)
            {
                _hoveredDeckStack.SetHovered(false);
            }

            _hoveredDeckStack = pointed;
            if (_hoveredDeckStack != null)
            {
                _hoveredDeckStack.SetHovered(true);
            }
        }

        private void UpdateCombatCommandHover(TableCombatCommandView pointed)
        {
            if (_hoveredCombatCommand != pointed)
            {
                _hoveredCombatCommand?.SetHovered(false);
                _hoveredCombatCommand = pointed;
                _hoveredCombatCommand?.SetHovered(true);
            }

            hud?.HideCombatActionTooltip();
        }

        private void UpdateHoverDescriptionTarget(HoverDescriptionTarget pointed)
        {
            _hoveredDescriptionTarget = pointed;
            if (_hoveredDescriptionTarget == null)
            {
                // Only responsible for clearing our own badge. Card, demon-card, and
                // overlay-owned badges are managed by their respective hover paths.
                if (_hoveredCard == null &&
                    _hoveredDemonCard == null &&
                    _hoverBadgeOwner == null)
                {
                    hud?.HideCardHoverBadge();
                }

                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (hud == null ||
                !_hoveredDescriptionTarget.TryCreateRequest(
                    _camera,
                    out CardHoverBadgeRequest request))
            {
                hud?.HideCardHoverBadge();
                return;
            }

            hud.ShowCardHoverBadge(
                request.Title,
                request.Description,
                request.ScreenPosition,
                _camera,
                request.TooltipPivot);
        }

        private void UpdateShopPriceBadges()
        {
            if (hud == null)
            {
                return;
            }

            if (shop == null || !shop.IsOpen)
            {
                hud.HideShopPriceBadges();
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            hud.RenderShopPriceBadges(
                shop.ActivePriceTargets,
                _camera);
        }

        private void EnsureDeckPreview()
        {
            if (deckPreview == null)
            {
                Debug.LogError(
                    "GameManager requires the scene-authored UIDeckPreview reference.",
                    this);
                return;
            }

            deckPreview.Configure(playerHand == null ? null : playerHand.CardPrefab);
        }

        private void OpenDeckPreview(DeckKind kind)
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            GameSceneDeckViewModel model =
                GameScenePresenter.CreateDeckPreview(battle, kind);

            EnsureDeckPreview();
            if (deckPreview == null)
            {
                return;
            }

            UpdateHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateCombatCommandHover(null);
            deckPreview.Open(model);
            BeginDeckPreviewSwitchInputLock();
        }

        private void CloseDeckPreview()
        {
            if (deckPreview != null && deckPreview.IsOpen)
            {
                deckPreview.Close();
                UpdateHover(null);
                UpdateDeckStackHover(null);
                UpdateCodexHover(null);
                UpdateContractPaperHover(null);
                UpdateCombatCommandHover(null);
                hud?.HideCardHoverBadge();
            }

            EndDeckPreviewSwitchInputLock();
        }

        private void BeginDeckPreviewSwitchInputLock()
        {
            if (_deckPreviewSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.LockSwitchInput();
            _deckPreviewSwitchInputLocked = true;
        }

        private void EndDeckPreviewSwitchInputLock()
        {
            if (!_deckPreviewSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            controller?.UnlockSwitchInput();
            _deckPreviewSwitchInputLocked = false;
        }

        private void CloseCodex()
        {
            if (codex != null && codex.IsOpen)
            {
                codex.Close();
            }

            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            EndCodexSwitchInputLock();
        }

        private void HandleCodexOpenStateChanged(bool isOpen)
        {
            UpdateHover(null);
            UpdateDemonCardHover(null);
            UpdateDeckStackHover(null);
            UpdateCodexHover(null);
            UpdateContractPaperHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            hud?.HideDemonContractDetail();
            if (isOpen)
            {
                BeginCodexSwitchInputLock();
            }
            else
            {
                EndCodexSwitchInputLock();
            }
        }

        private void HandleDeckPreviewHoverBadgeRequested(
            CardHoverBadgeRequest request)
        {
            ShowOverlayHoverBadge(deckPreview, request);
        }

        private void HandleDeckPreviewHoverBadgeCleared()
        {
            ClearOverlayHoverBadge(deckPreview);
        }

        private void HandleCodexHoverBadgeRequested(
            CardHoverBadgeRequest request)
        {
            ShowOverlayHoverBadge(codex, request);
        }

        private void HandleCodexHoverBadgeCleared()
        {
            ClearOverlayHoverBadge(codex);
        }

        private void ShowOverlayHoverBadge(
            object owner,
            CardHoverBadgeRequest request)
        {
            if (hud == null || owner == null || request == null)
            {
                return;
            }

            _hoverBadgeOwner = owner;
            hud.ShowCardHoverBadge(
                request.Title,
                request.Description,
                request.ScreenPosition,
                _camera,
                request.TooltipPivot);
        }

        private void ClearOverlayHoverBadge(object owner)
        {
            if (!ReferenceEquals(_hoverBadgeOwner, owner))
            {
                return;
            }

            _hoverBadgeOwner = null;
            hud?.HideCardHoverBadge();
        }

        private void BeginCodexSwitchInputLock()
        {
            if (_codexSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.LockSwitchInput();
            _codexSwitchInputLocked = true;
        }

        private void EndCodexSwitchInputLock()
        {
            if (!_codexSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            controller?.UnlockSwitchInput();
            _codexSwitchInputLocked = false;
        }

        private void UpdateCardHoverBadge()
        {
            if (hud == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_hoveredCard == null ||
                !_hoveredCard.ShouldShowHoverBadge ||
                !_hoveredCard.TryGetHoverBadgeScreenPosition(
                    _camera,
                    _hoveredCard.ShowHoverBadgeBelow,
                    out Vector2 screenPosition))
            {
                UpdateDemonCardHoverBadge();
                return;
            }

            hud.ShowCardHoverBadge(
                _hoveredCard.HoverBadgeTitle,
                _hoveredCard.HoverBadgeDescription,
                screenPosition,
                _camera,
                _hoveredCard.ShowHoverBadgeBelow);
        }

        private void UpdateDemonCardHoverBadge()
        {
            if (hud == null)
            {
                return;
            }

            if (_hoveredDemonCard == null ||
                !_hoveredDemonCard.ShouldShowHoverBadge ||
                _hoveredDemonCard.BoundCard == null)
            {
                hud.HideCardHoverBadge();
                hud.HideDemonContractDetail();
                return;
            }

            hud.HideCardHoverBadge();
            hud.ShowDemonContractDetail(_hoveredDemonCard.BoundCard);
        }

        private void UpdateDemonCardHover(DemonCardView pointed)
        {
            if (pointed == _hoveredDemonCard)
            {
                return;
            }

            if (_hoveredDemonCard != null)
            {
                _hoveredDemonCard.SetHovered(false);
                if (pointed == null)
                {
                    hud?.HideDemonContractDetail();
                }
            }

            _hoveredDemonCard = pointed;
            if (_hoveredDemonCard != null)
            {
                _hoveredDemonCard.SetHovered(true);
            }
        }

        private void UpdateShopUtilityItemHover(ShopUtilityItemView pointed)
        {
            if (pointed == _hoveredShopUtilityItem)
            {
                return;
            }

            if (_hoveredShopUtilityItem != null)
            {
                _hoveredShopUtilityItem.SetHovered(false);
            }

            _hoveredShopUtilityItem = pointed;
            if (_hoveredShopUtilityItem != null)
            {
                _hoveredShopUtilityItem.SetHovered(true);
            }
        }

        private CoreLoopBattle CreateBattle()
        {
            ResetSatanAttackAnimationState();
            ResetRevolverAnimationState();
            ResetKnifeAnimationState();
            ResetPoisonInjectionAnimationState();
            ResolveHammerAnimation()?.ResetPresentationState();
            int battleSeed = seed + (_battleIndex * 2);
            _battleIndex++;
            int enemyDeckSeed = battleSeed + 1;
            EnemyBattleConfiguration enemy =
                EnemyBattleConfigurationFactory.Create(
                    _activeEnemyProfileKey,
                    enemyDeckSeed);
            return new CoreLoopBattle(
                CreatePlayerDeck(battleSeed),
                enemy.CreateEnemyDeck(),
                enemyMaximumSoul: enemy.EnemyMaximumSoul,
                enemyPolicy: enemy.BehaviorPolicy,
                playerDemonDeck: CreatePlayerDemonDeck(battleSeed + 1000),
                enemyDemonDeck: CreateEnemyDemonDeck(
                    enemy.DemonContractDefinitionKeys,
                    enemyDeckSeed ^ unchecked((int)0x4C957F2Du)),
                enemyChangeCostMode: enemy.ChangeCostMode,
                enemyDemonContractCandidateCount:
                    enemy.DemonContractCandidateCount,
                injectsPoisonIntoPlayerDeckEachRound:
                    enemy.InjectsPoisonIntoPlayerDeckEachRound,
                enablesEnemyChange: true,
                fixedEnemyDemonContractPhases:
                    enemy.FixedDemonContractPhases,
                demonContractSeed: battleSeed ^ unchecked((int)0xA511E9B3u));
        }

        private string ResolveEnemyProfileKey()
        {
            try
            {
                EnemyCombatProfileCatalog.Default.GetByKey(enemyProfileKey);
                return enemyProfileKey;
            }
            catch (Exception exception)
                when (exception is ArgumentException ||
                      exception is KeyNotFoundException)
            {
                Debug.LogWarning(
                    $"Enemy profile '{enemyProfileKey}' is invalid. " +
                    $"Falling back to '{EnemyCombatProfileCatalog.GunslingerKey}'.",
                    this);
                return EnemyCombatProfileCatalog.GunslingerKey;
            }
        }

        private SpeechProfileSO ResolveActiveEnemySpeechProfile()
        {
            EnemyContentCatalogSO catalog =
                CardContentBootstrap.Instance?.EnemyCatalog;
            if (catalog == null ||
                string.IsNullOrWhiteSpace(_activeEnemyProfileKey))
            {
                return null;
            }

            try
            {
                return catalog.GetSpeechProfile(_activeEnemyProfileKey);
            }
            catch (Exception exception)
                when (exception is ArgumentException ||
                      exception is KeyNotFoundException)
            {
                Debug.LogWarning(exception.Message, this);
                return null;
            }
        }

        private void UpdateCodexHover(CodexClickable pointed)
        {
            if (pointed == _hoveredCodex)
            {
                return;
            }

            _hoveredCodex?.SetHovered(false);
            _hoveredCodex = pointed;
            _hoveredCodex?.SetHovered(true);
        }

        private void UpdateContractPaperHover(ContractPaperClickable pointed)
        {
            if (pointed == _hoveredContractPaper)
            {
                return;
            }

            _hoveredContractPaper?.SetHovered(false);
            _hoveredContractPaper = pointed;
            _hoveredContractPaper?.SetHovered(true);
        }

        private void ApplyEnemyDeckTopTint()
        {
            EnemyContentCatalogSO catalog =
                CardContentBootstrap.Instance?.EnemyCatalog;
            if (catalog == null ||
                string.IsNullOrWhiteSpace(_activeEnemyProfileKey))
            {
                return;
            }

            try
            {
                Color tint = catalog.GetByKey(_activeEnemyProfileKey).DeckTopTint;
                enemyRemainingDeck?.SetTopTint(tint);
                enemyDiscardDeck?.SetTopTint(tint);
            }
            catch (Exception exception)
                when (exception is ArgumentException ||
                      exception is KeyNotFoundException)
            {
                Debug.LogWarning(exception.Message, this);
            }
        }

        private BlackjackDeck CreatePlayerDeck(int deckSeed)
        {
            var cards = new List<BlackjackCard>(20 + _purchasedNormalCards.Count);
            int id = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
                if (!IsBaseNormalCardRemoved(definition.Key, CardSuit.Spade))
                {
                    cards.Add(new BlackjackCard(id++, definition, suit: CardSuit.Spade));
                }

                if (!IsBaseNormalCardRemoved(definition.Key, CardSuit.Clover))
                {
                    cards.Add(new BlackjackCard(id++, definition, suit: CardSuit.Clover));
                }
            }

            foreach (PurchasedNormalCard purchasedCard in _purchasedNormalCards)
            {
                CardDefinition definition =
                    CardDefinitionCatalog.GetByKey(purchasedCard.DefinitionKey);
                cards.Add(new BlackjackCard(id++, definition, suit: purchasedCard.Suit));
            }

            return new BlackjackDeck(cards, deckSeed);
        }

        private DemonContractDeck CreatePlayerDemonDeck(int deckSeed)
        {
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            IReadOnlyList<string> defaultKeys =
                DemonContractCatalog.PlayerDefaultDemonDeckKeys;
            var cards = new List<DemonContractCard>(
                defaultKeys.Count + _purchasedDemonContractKeys.Count);
            int id = 0;
            foreach (string definitionKey in defaultKeys)
            {
                DemonContractDefinition definition = catalog.GetByKey(definitionKey);
                cards.Add(new DemonContractCard(id++, definition));
            }

            foreach (string definitionKey in _purchasedDemonContractKeys)
            {
                cards.Add(new DemonContractCard(id++, catalog.GetByKey(definitionKey)));
            }

            return new DemonContractDeck(cards, deckSeed);
        }

        private static DemonContractDeck CreateEnemyDemonDeck(
            IReadOnlyList<string> definitionKeys,
            int deckSeed)
        {
            List<DemonContractCard> cards =
                new List<DemonContractCard>(definitionKeys.Count);
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            for (int i = 0; i < definitionKeys.Count; i++)
            {
                cards.Add(
                    new DemonContractCard(
                        i,
                        catalog.GetByKey(definitionKeys[i])));
            }

            return new DemonContractDeck(cards, deckSeed);
        }

        private void PurchaseShopDemonCard(DemonCardView card)
        {
            if (_formalShopModel != null && card != null)
            {
                ShopPurchaseAvailability availability =
                    GetFormalCardAvailability(card.CardId);
                if (availability != ShopPurchaseAvailability.Available)
                {
                    ShowMerchantAvailability(availability);
                    UpdateDemonCardHover(null);
                    return;
                }

                FormalShopCardPurchaseRequested?.Invoke(card.CardId);
                UpdateDemonCardHover(null);
                return;
            }

            if (shop == null || card == null)
            {
                return;
            }

            ShopPurchaseAvailability standaloneAvailability =
                shop.GetDemonCardAvailability(card.CardId);
            if (standaloneAvailability != ShopPurchaseAvailability.Available)
            {
                ShowMerchantAvailability(standaloneAvailability);
                return;
            }

            if (!shop.TryPurchaseDemonCard(card.CardId, out string definitionKey))
            {
                shop.ShowMerchantSpeech(SpeechCueKeys.ShopUnavailable);
                return;
            }

            _purchasedDemonContractKeys.Add(definitionKey);
            AddPurchasedDemonContractToCurrentBattle(definitionKey);
            shop.ShowMerchantSpeech(SpeechCueKeys.ShopPurchaseSuccess);
            RefreshView();
            UpdateDemonCardHover(null);
        }

        private void PurchaseShopNormalCard(CardView card)
        {
            if (_formalShopModel != null && card != null)
            {
                ShopPurchaseAvailability availability =
                    GetFormalCardAvailability(card.CardId);
                if (availability != ShopPurchaseAvailability.Available)
                {
                    ShowMerchantAvailability(availability);
                    UpdateHover(null);
                    return;
                }

                FormalShopCardPurchaseRequested?.Invoke(card.CardId);
                UpdateHover(null);
                return;
            }

            if (shop == null || card == null)
            {
                return;
            }

            ShopPurchaseAvailability standaloneAvailability =
                shop.GetNormalCardAvailability(card.CardId);
            if (standaloneAvailability != ShopPurchaseAvailability.Available)
            {
                ShowMerchantAvailability(standaloneAvailability);
                return;
            }

            if (!shop.TryPurchaseNormalCard(
                    card.CardId,
                    out string definitionKey,
                    out CardSuit suit))
            {
                shop.ShowMerchantSpeech(SpeechCueKeys.ShopUnavailable);
                return;
            }

            _purchasedNormalCards.Add(new PurchasedNormalCard(definitionKey, suit));
            AddPurchasedNormalCardToCurrentBattle(definitionKey, suit);
            shop.ShowMerchantSpeech(SpeechCueKeys.ShopPurchaseSuccess);
            RefreshView();
            UpdateHover(null);
        }

        private void UseShopUtilityItem(ShopUtilityItemView item)
        {
            if (item == null)
            {
                return;
            }

            switch (item.Kind)
            {
                case ShopUtilityItemKind.Lighter:
                    BeginLighterRemoval();
                    break;
                case ShopUtilityItemKind.Whiskey:
                    PurchaseWhiskey();
                    break;
            }
        }

        private ShopPurchaseAvailability GetFormalCardAvailability(int optionId)
        {
            if (_formalShopModel == null)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            foreach (ShopCardOptionViewModel option in
                     _formalShopModel.ShopCardOptions)
            {
                if (option.OptionId != optionId)
                {
                    continue;
                }

                if (option.IsSold)
                {
                    return ShopPurchaseAvailability.SoldOut;
                }

                return _formalShopGold < option.PriceAmount
                    ? ShopPurchaseAvailability.InsufficientGold
                    : option.CanBuy
                        ? ShopPurchaseAvailability.Available
                        : ShopPurchaseAvailability.Unavailable;
            }

            return ShopPurchaseAvailability.Unavailable;
        }

        private void ShowMerchantAvailability(
            ShopPurchaseAvailability availability)
        {
            shop?.ShowMerchantSpeech(
                ShopController.ResolveAvailabilitySpeech(availability));
        }

        internal static ShopPurchaseAvailability ResolveFormalUtilityAvailability(
            bool canUse,
            bool wasUsed,
            int currentGold,
            int price,
            bool isSoulFull = false)
        {
            if (wasUsed)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            if (isSoulFull)
            {
                return ShopPurchaseAvailability.SoulFull;
            }

            if (currentGold < price)
            {
                return ShopPurchaseAvailability.InsufficientGold;
            }

            return canUse
                ? ShopPurchaseAvailability.Available
                : ShopPurchaseAvailability.Unavailable;
        }

        private bool HasFormalRemovableCard()
        {
            if (_formalShopModel == null)
            {
                return false;
            }

            foreach (ShopOwnedCardViewModel card in _formalShopModel.ShopOwnedCards)
            {
                if (card.CanRemove)
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginLighterRemoval()
        {
            int removableCount = _formalShopModel == null
                ? BuildRunDeckCardOptions().Count
                : CountFormalRemovableCards();
            if (shop == null || !shop.IsOpen || removableCount <= 0)
            {
                shop?.ShowMerchantSpeech(SpeechCueKeys.ShopUnavailable);
                return;
            }

            if (_formalShopModel == null)
            {
                ShopPurchaseAvailability availability =
                    shop.GetLighterAvailability(removableCount);
                if (availability != ShopPurchaseAvailability.Available)
                {
                    ShowMerchantAvailability(availability);
                    return;
                }
            }
            else if (!HasFormalRemovableCard())
            {
                ShowMerchantAvailability(
                    ResolveFormalUtilityAvailability(
                        false,
                        _formalShopModel.IsLighterUsed,
                        _formalShopGold,
                        _formalShopModel.LighterPriceAmount));
                return;
            }

            EnsureDeckPreview();
            if (deckPreview == null)
            {
                return;
            }

            _choosingLighterRemoval = true;
            UpdateShopUtilityItemHover(null);
            GameSceneDeckViewModel model = _formalShopModel == null
                ? CreateStandaloneLighterRemovalPreview()
                : CreateFormalLighterRemovalPreview();
            deckPreview.OpenForSingleSelection(model);
            BeginDeckPreviewSwitchInputLock();
            RefreshShopUtilityItems();
            UpdateShopLeaveControl();
        }

        private GameSceneDeckViewModel CreateStandaloneLighterRemovalPreview()
        {
            List<RunDeckCardOption> options = BuildRunDeckCardOptions();
            var groups = new List<GameSceneDeckCardGroupViewModel>(options.Count);
            bool hasMinimumDeck = options.Count > 1;
            for (int i = 0; i < options.Count; i++)
            {
                RunDeckCardOption option = options[i];
                CardDefinition definition =
                    CardDefinitionCatalog.GetByKey(option.DefinitionKey);
                var card = new GameSceneCardViewModel(
                    i,
                    definition.Rank,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: hasMinimumDeck && CanRemoveRunDeckCard(option),
                    definition.DisplayName,
                    definition.Description,
                    option.Suit,
                    showHoverBadgeWhenUnavailable: true,
                    definition.Key);
                groups.Add(new GameSceneDeckCardGroupViewModel(card, 1));
            }

            return new GameSceneDeckViewModel(
                DeckKind.Draw,
                "제거할 카드 선택",
                groups);
        }

        private GameSceneDeckViewModel CreateFormalLighterRemovalPreview()
        {
            IReadOnlyList<ShopOwnedCardViewModel> options =
                _formalShopModel.ShopOwnedCards;
            var groups = new List<GameSceneDeckCardGroupViewModel>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                ShopOwnedCardViewModel option = options[i];
                CardDefinition definition =
                    CardDefinitionCatalog.GetByKey(option.DefinitionKey);
                var card = new GameSceneCardViewModel(
                    option.CardId,
                    option.Rank,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: option.CanRemove,
                    definition.DisplayName,
                    option.AbilityDescription,
                    option.Suit,
                    showHoverBadgeWhenUnavailable: true,
                    option.DefinitionKey);
                groups.Add(new GameSceneDeckCardGroupViewModel(card, 1));
            }

            return new GameSceneDeckViewModel(
                DeckKind.Draw,
                "제거할 카드 선택",
                groups);
        }

        private bool RemoveCardWithLighter(int optionIndex)
        {
            if (shop == null || !shop.IsOpen)
            {
                return false;
            }

            List<RunDeckCardOption> options = BuildRunDeckCardOptions();
            if (optionIndex < 0 ||
                optionIndex >= options.Count ||
                options.Count <= 1)
            {
                return false;
            }

            RunDeckCardOption option = options[optionIndex];
            if (!CanRemoveRunDeckCard(option) ||
                !shop.TryPurchaseLighterRemoval(options.Count))
            {
                RefreshShopUtilityItems();
                return false;
            }

            _pendingLighterBurnCard = CreateLighterBurnCardModel(
                option.DefinitionKey,
                option.Suit);
            RemoveRunDeckCard(option);
            RemoveCurrentBattleAvailableCard(option);
            _choosingLighterRemoval = false;
            shop.ShowMerchantSpeech(SpeechCueKeys.ShopLighterSuccess);
            PlayLighterShopAnimation();
            RefreshView();
            UpdateShopLeaveControl();
            return true;
        }

        private bool CancelLighterRemoval()
        {
            bool wasChoosing = _choosingLighterRemoval;
            _choosingLighterRemoval = false;
            if (deckPreview != null && deckPreview.IsSingleSelection)
            {
                CloseDeckPreview();
            }

            RefreshShopUtilityItems();
            UpdateShopLeaveControl();
            return wasChoosing;
        }

        private void HandleLighterSelectionConfirmed(int selectionId)
        {
            if (!_choosingLighterRemoval || shop == null || !shop.IsOpen)
            {
                return;
            }

            if (_formalShopModel != null)
            {
                ShopOwnedCardViewModel selected = null;
                foreach (ShopOwnedCardViewModel card in
                         _formalShopModel.ShopOwnedCards)
                {
                    if (card.CardId == selectionId)
                    {
                        selected = card;
                        break;
                    }
                }

                if (selected == null || !selected.CanRemove)
                {
                    return;
                }

                _pendingLighterBurnCard = CreateLighterBurnCardModel(
                    selected.DefinitionKey,
                    selected.Suit);
                FormalShopCardRemovalRequested?.Invoke(selectionId);
                return;
            }

            if (RemoveCardWithLighter(selectionId))
            {
                CloseDeckPreview();
            }
        }

        private void HandleLighterSelectionCancelled()
        {
            if (!_choosingLighterRemoval)
            {
                return;
            }

            _choosingLighterRemoval = false;
            RefreshShopUtilityItems();
            UpdateShopLeaveControl();
        }

        private void HandleShopLeaveRequested()
        {
            if (shop == null || !shop.IsOpen)
            {
                return;
            }

            CancelLighterRemoval();
            if (_formalShopModel != null)
            {
                FormalShopLeaveRequested?.Invoke();
                return;
            }

            ProcessInput(LeaveShop);
        }

        private void UpdateShopLeaveControl()
        {
            bool visible = shop != null && shop.IsOpen;
            bool canLeave = visible &&
                (_formalShopModel == null || _formalShopModel.CanLeaveShop);
            bool interactable = canLeave &&
                !_inputLocked &&
                !_pauseInputBlocked &&
                !_shopUtilityAnimationPlaying;
            hud?.SetShopLeaveState(visible, interactable);
        }

        private void PurchaseWhiskey()
        {
            if (_formalShopModel != null)
            {
                if (!_formalShopModel.CanRestAtShop)
                {
                    ShowMerchantAvailability(
                        ResolveFormalUtilityAvailability(
                            false,
                            _formalShopModel.IsWhiskeyUsed,
                            _formalShopGold,
                            _formalShopModel.WhiskeyPriceAmount,
                            _formalShopModel.IsPlayerSoulFull));
                    UpdateShopUtilityItemHover(null);
                    return;
                }

                FormalShopRestRequested?.Invoke();
                UpdateShopUtilityItemHover(null);
                return;
            }

            CoreLoopBattle battle = Battle;
            if (shop == null || battle == null)
            {
                RefreshShopUtilityItems();
                return;
            }

            ShopPurchaseAvailability availability = shop.GetWhiskeyAvailability(
                battle.Player.Soul.Current,
                battle.Player.Soul.Maximum);
            if (availability != ShopPurchaseAvailability.Available)
            {
                ShowMerchantAvailability(availability);
                RefreshShopUtilityItems();
                return;
            }

            if (!shop.TryPurchaseWhiskey(
                    battle.Player.Soul.Current,
                    battle.Player.Soul.Maximum,
                    out int restoreAmount))
            {
                shop.ShowMerchantSpeech(SpeechCueKeys.ShopUnavailable);
                RefreshShopUtilityItems();
                return;
            }

            battle.Player.Soul.Restore(restoreAmount);
            shop.ShowMerchantSpeech(SpeechCueKeys.ShopWhiskeySuccess);
            PlayWhiskeyShopAnimation();
            PlayPlayerSoulRestoredFlourish();
            RefreshView();
            UpdateShopUtilityItemHover(null);
        }

        private void AddPurchasedNormalCardToCurrentBattle(
            string definitionKey,
            CardSuit suit)
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(definitionKey);
            int cardId = FindNextCardId(battle.Player.Deck);
            var card = new BlackjackCard(cardId, definition, suit: suit);
            if (!battle.Player.Deck.TryAddAvailableCard(card))
            {
                throw new InvalidOperationException(
                    "Purchased card could not be added to the battle deck.");
            }
        }

        private void AddPurchasedDemonContractToCurrentBattle(string definitionKey)
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(definitionKey);
            int cardId = battle.PlayerDemonDeck.TotalCardCount;
            var card = new DemonContractCard(cardId, definition);
            if (!battle.PlayerDemonDeck.TryAddAvailableCard(card))
            {
                throw new InvalidOperationException(
                    "Purchased demon contract could not be added to the battle deck.");
            }
        }

        private static int FindNextCardId(BlackjackDeck deck)
        {
            int cardId = deck.TotalCardCount;
            while (cardId < int.MaxValue && deck.ContainsKnownCardId(cardId))
            {
                cardId++;
            }

            if (deck.ContainsKnownCardId(cardId))
            {
                throw new InvalidOperationException("Player card ids are exhausted.");
            }

            return cardId;
        }

        private List<RunDeckCardOption> BuildRunDeckCardOptions()
        {
            var options = new List<RunDeckCardOption>(20 + _purchasedNormalCards.Count);
            for (int rank = 1; rank <= 10; rank++)
            {
                CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
                AddBaseRunDeckCardOption(options, definition, CardSuit.Spade);
                AddBaseRunDeckCardOption(options, definition, CardSuit.Clover);
            }

            for (int i = 0; i < _purchasedNormalCards.Count; i++)
            {
                PurchasedNormalCard card = _purchasedNormalCards[i];
                options.Add(new RunDeckCardOption(
                    card.DefinitionKey,
                    card.Suit,
                    isPurchased: true,
                    purchasedIndex: i));
            }

            return options;
        }

        private void AddBaseRunDeckCardOption(
            List<RunDeckCardOption> options,
            CardDefinition definition,
            CardSuit suit)
        {
            if (definition != null && !IsBaseNormalCardRemoved(definition.Key, suit))
            {
                options.Add(new RunDeckCardOption(
                    definition.Key,
                    suit,
                    isPurchased: false,
                    purchasedIndex: -1));
            }
        }

        private bool CanRemoveRunDeckCard(RunDeckCardOption option)
        {
            if (option.IsPurchased)
            {
                return option.PurchasedIndex >= 0 &&
                    option.PurchasedIndex < _purchasedNormalCards.Count &&
                    _purchasedNormalCards[option.PurchasedIndex].Matches(
                        option.DefinitionKey,
                        option.Suit);
            }

            return !IsBaseNormalCardRemoved(option.DefinitionKey, option.Suit);
        }

        private void RemoveRunDeckCard(RunDeckCardOption option)
        {
            if (option.IsPurchased)
            {
                _purchasedNormalCards.RemoveAt(option.PurchasedIndex);
                return;
            }

            _removedNormalCards.Add(new RemovedNormalCard(
                option.DefinitionKey,
                option.Suit));
        }

        private void RemoveCurrentBattleAvailableCard(RunDeckCardOption option)
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            battle.Player.Deck.TryRemoveAvailableCard(
                option.DefinitionKey,
                option.Suit);
        }

        private bool IsBaseNormalCardRemoved(string definitionKey, CardSuit suit)
        {
            foreach (RemovedNormalCard card in _removedNormalCards)
            {
                if (card.Matches(definitionKey, suit))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool PlayLighterShopAnimation()
        {
            if (_shopUtilityAnimationPlaying ||
                _pendingLighterBurnCard == null ||
                !TryResolveLighterAnimation())
            {
                return false;
            }

            _shopUtilityAnimationPlaying = true;
            StartCoroutine(PlayLighterShopAnimationRoutine());
            return true;
        }

        private static GameSceneCardViewModel CreateLighterBurnCardModel(
            string definitionKey,
            CardSuit suit)
        {
            CardDefinition definition =
                CardDefinitionCatalog.GetByKey(definitionKey);
            return new GameSceneCardViewModel(
                cardId: 0,
                rank: definition.Rank,
                isFaceUp: true,
                revealRank: true,
                canUse: false,
                displayName: definition.DisplayName,
                abilityDescription: definition.Description,
                suit: suit,
                showHoverBadgeWhenUnavailable: false,
                definitionKey: definition.Key);
        }

        internal bool PlayWhiskeyShopAnimation()
        {
            if (_shopUtilityAnimationPlaying || !TryResolveWhiskeyAnimation())
            {
                return false;
            }

            StartCoroutine(PlayWhiskeyShopAnimationRoutine());
            return true;
        }

        internal void PlayPlayerSoulRestoredFlourish()
        {
            hud?.PlaySoulRestoredFlourish();
        }

        private IEnumerator PlayLighterShopAnimationRoutine()
        {
            UpdateShopLeaveControl();
            lighterAnimationRoot.SetActive(true);
            lighterAnimator.Rebind();
            lighterAnimator.Update(0f);
            lighterAnimator.SetLayerWeight(0, 1f);
            LighterDragTriggerController interaction =
                lighterAnimationRoot.GetComponent<LighterDragTriggerController>();
            CardView cardVisualSource = playerHand == null
                ? null
                : playerHand.CardPrefab;
            Sprite selectedCardSprite = cardVisualSource == null
                ? null
                : cardVisualSource.GetFaceSprite(_pendingLighterBurnCard);
            bool prepared = interaction != null &&
                interaction.SetBurnCardSprite(selectedCardSprite);
            _pendingLighterBurnCard = null;
            if (!prepared)
            {
                Debug.LogError(
                    "Lighter animation could not resolve the selected card sprite.");
                lighterAnimationRoot.SetActive(false);
                _shopUtilityAnimationPlaying = false;
                UpdateShopLeaveControl();
                yield break;
            }

            lighterAnimator.SetTrigger("Start");

            if (interaction != null)
            {
                while (!interaction.HasCompletedInteraction)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(3f);
            }

            lighterAnimationRoot.SetActive(false);
            _shopUtilityAnimationPlaying = false;
            UpdateShopLeaveControl();
        }

        private IEnumerator PlayWhiskeyShopAnimationRoutine()
        {
            _shopUtilityAnimationPlaying = true;
            UpdateShopLeaveControl();
            whiskeyAnimationRoot.SetActive(true);
            whiskeyAnimator.Rebind();
            whiskeyAnimator.Update(0f);
            whiskeyAnimator.SetLayerWeight(0, 1f);
            whiskeyAnimator.Play(WhiskeyAnimationStateName, 0, 0f);
            whiskeyAnimator.Update(0f);
            float sfxDelay = Mathf.Clamp(
                whiskeyDrinkSfxDelaySeconds,
                0f,
                whiskeyAnimationSeconds);
            if (sfxDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(sfxDelay);
            }

            SoundManager.Current?.PlaySfx(whiskeyDrinkSfxId);

            float remainingSeconds = whiskeyAnimationSeconds - sfxDelay;
            if (remainingSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingSeconds);
            }

            whiskeyAnimationRoot.SetActive(false);
            _shopUtilityAnimationPlaying = false;
            UpdateShopLeaveControl();
        }

        private bool TryResolveLighterAnimation()
        {
            if (lighterAnimationRoot == null)
            {
                lighterAnimationRoot = FindInactiveSceneObject("Lighter_Anim");
            }

            if (lighterAnimator == null && lighterAnimationRoot != null)
            {
                lighterAnimator = lighterAnimationRoot.GetComponent<Animator>();
            }

            return lighterAnimationRoot != null && lighterAnimator != null;
        }

        private bool TryResolveWhiskeyAnimation()
        {
            if (whiskeyAnimationRoot == null)
            {
                whiskeyAnimationRoot = FindInactiveSceneObject("whiskey_Anim");
            }

            if (whiskeyAnimator == null && whiskeyAnimationRoot != null)
            {
                whiskeyAnimator = whiskeyAnimationRoot.GetComponent<Animator>();
            }

            return whiskeyAnimationRoot != null && whiskeyAnimator != null;
        }

        private static GameObject FindInactiveSceneObject(string objectName)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null &&
                    candidate.name == objectName &&
                    candidate.gameObject.scene.IsValid())
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private void ResolveDeckStackReferences()
        {
            remainingDeck ??= FindSceneDeckStack("RemainingDeck");
            discardDeck ??= FindSceneDeckStack("DiscardDeck");
            enemyRemainingDeck ??= FindSceneDeckStack("EnemyRemain");
            enemyDiscardDeck ??= FindSceneDeckStack("EnemyDiscard");
        }

        private static DeckStackView FindSceneDeckStack(string objectName)
        {
            GameObject sceneObject = FindInactiveSceneObject(objectName);
            return sceneObject == null
                ? null
                : sceneObject.GetComponent<DeckStackView>();
        }

        private void ResetShopUtilityAnimations()
        {
            if (lighterAnimationRoot != null)
            {
                lighterAnimationRoot.SetActive(false);
            }

            if (whiskeyAnimationRoot != null)
            {
                whiskeyAnimationRoot.SetActive(false);
            }

            _shopUtilityAnimationPlaying = false;
        }

        private void HandleCombatCommand(GameSceneCombatHudCommand command)
        {
            if (IsModalInputBlocked)
            {
                return;
            }

            if (_pendingPlayerMammonComparison != null &&
                (command.Kind !=
                    GameSceneCombatHudCommandKind.ResolveDemonContractChoice ||
                 command.InteractionId !=
                    _pendingPlayerMammonComparison.InteractionId))
            {
                return;
            }

            if (_revolverReadyActive &&
                !_revolverSelectionReady &&
                command.Kind ==
                    GameSceneCombatHudCommandKind.ResolveCardEffectChoice)
            {
                return;
            }

            switch (command.Kind)
            {
                case GameSceneCombatHudCommandKind.Hit:
                    ProcessInput(TryPlayerHit);
                    break;
                case GameSceneCombatHudCommandKind.Stand:
                    ProcessInput(TryPlayerStand);
                    break;
                case GameSceneCombatHudCommandKind.BeginChange:
                    ProcessInput(TryBeginPlayerChange);
                    break;
                case GameSceneCombatHudCommandKind.SelectChangedCard:
                    ProcessInput(() => TrySelectChangedCard(command.OptionId));
                    break;
                case GameSceneCombatHudCommandKind.BeginContract:
                    ProcessInput(TryBeginPlayerDemonContract);
                    break;
                case GameSceneCombatHudCommandKind.ResolveCardEffectChoice:
                    ProcessInput(() => TryResolvePlayerCardChoice(command.OptionId));
                    break;
                case GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice:
                    ProcessInput(() => TryResolvePlayerAutomaticCardChoice(
                        command.InteractionId,
                        command.OptionId));
                    break;
                case GameSceneCombatHudCommandKind.ResolveDemonContractChoice:
                    if (_tutorialRestrictedOptionId.HasValue &&
                        _tutorialRestrictedOptionId.Value != command.OptionId)
                    {
                        break;
                    }

                    bool animateBelphegorReinsert =
                        ShouldAnimateBelphegorReinsert(command);
                    Action onAccepted = null;
                    if (animateBelphegorReinsert && remainingDeck != null)
                    {
                        onAccepted = remainingDeck.PlayReinsertAnimation;
                    }
                    ProcessInput(() => TryResolvePlayerDemonContract(
                        command.InteractionId,
                        command.OptionId),
                        onAccepted);
                    break;
                case GameSceneCombatHudCommandKind.BeginActiveDemonContractAction:
                    ProcessInput(() => TryBeginPlayerActiveDemonContractAction(
                        command.OptionId));
                    break;
                case GameSceneCombatHudCommandKind.Restart:
                    ProcessInput(RestartRun);
                    break;
                case GameSceneCombatHudCommandKind.ConfirmSatanNumberSelection:
                    if (satanNumberSelection != null &&
                        satanNumberSelection.TryGetSelectedNumbers(
                            out int firstNumber,
                            out int secondNumber))
                    {
                        ProcessInput(() => TryResolvePlayerSatanNumbers(
                            command.InteractionId,
                            firstNumber,
                            secondNumber));
                    }
                    break;
            }
        }

        private void ProcessInput(Func<bool> action, Action onAccepted = null)
        {
            if (IsModalInputBlocked || _inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            UpdateShopLeaveControl();

            // The battle runs the whole turn synchronously; Stepped fires once per sub-step, so we
            // snapshot each into a timeline and then pace them out over PlayTimeline.
            CoreLoopBattle battle = Battle;
            GameSceneViewModel timelineBaseline = battle == null
                ? null
                : GameScenePresenter.Create(battle, _activeEnemyProfileKey);
            _timeline.Clear();
            if (battle != null)
            {
                battle.Stepped += OnBattleStepped;
            }

            bool accepted = action();
            if (accepted)
            {
                onAccepted?.Invoke();
            }

            if (battle != null)
            {
                battle.Stepped -= OnBattleStepped;
            }

            if (accepted && Application.isPlaying && battle != null &&
                _timeline.Count == 0)
            {
                GameSceneViewModel current =
                    GameScenePresenter.Create(battle, _activeEnemyProfileKey);
                if (ShouldQueuePlayerRevolverReadySnapshot(
                        _timeline.Count,
                        current.RevolverAnimationCue))
                {
                    _timeline.Add(current);
                }
            }

            if (accepted && Application.isPlaying && _timeline.Count > 0)
            {
                StartCoroutine(PlayTimeline(timelineBaseline));
            }
            else
            {
                UnlockInput();
                if (_pendingPlayerMammonComparison != null &&
                    timelineBaseline != null)
                {
                    ApplyView(
                        timelineBaseline,
                        preserveRoundComparisonCardsAndTotals: true);
                }
                else
                {
                    RefreshView();
                }
                ReturnToProgressionIfStageBattleEnded();
            }
        }

        private bool TryPlayerHit()
        {
            return IsStageBattle
                ? _stageSession.TryPlayerHit()
                : _session.TryPlayerHit();
        }

        private bool TryPlayerStand()
        {
            return IsStageBattle
                ? _stageSession.TryPlayerStand()
                : _session.TryPlayerStand();
        }

        private bool TryBeginPlayerChange()
        {
            return IsStageBattle
                ? _stageSession.TryBeginPlayerChange()
                : _session.TryBeginPlayerChange();
        }

        private bool TrySelectChangedCard(int candidateIndex)
        {
            return IsStageBattle
                ? _stageSession.TrySelectChangedCard(candidateIndex)
                : _session.TrySelectChangedCard(candidateIndex);
        }

        private bool TryBeginPlayerCardUse(int cardId)
        {
            return IsStageBattle
                ? _stageSession.TryBeginPlayerCardUse(cardId)
                : _session.TryBeginPlayerCardUse(cardId);
        }

        private bool TryResolvePlayerCardChoice(int optionId)
        {
            return IsStageBattle
                ? _stageSession.TryResolvePlayerCardChoice(optionId)
                : _session.TryResolvePlayerCardChoice(optionId);
        }

        private bool TryResolvePlayerAutomaticCardChoice(
            int interactionId,
            int optionId)
        {
            return IsStageBattle
                ? _stageSession.TryResolvePlayerAutomaticCardChoice(
                    interactionId,
                    optionId)
                : _session.TryResolvePlayerAutomaticCardChoice(
                    interactionId,
                    optionId);
        }

        private bool TryBeginPlayerDemonContract()
        {
            return IsStageBattle
                ? _stageSession.TryBeginPlayerDemonContract()
                : _session.TryBeginPlayerDemonContract();
        }

        private bool TryResolvePlayerDemonContract(
            int interactionId,
            int optionId)
        {
            return IsStageBattle
                ? _stageSession.TryResolvePlayerDemonContract(
                    interactionId,
                    optionId)
                : _session.TryResolvePlayerDemonContract(
                    interactionId,
                    optionId);
        }

        private bool TryResolvePlayerSatanNumbers(
            int interactionId,
            int firstNumber,
            int secondNumber)
        {
            return IsStageBattle
                ? _stageSession.TryResolvePlayerSatanNumbers(
                    interactionId,
                    firstNumber,
                    secondNumber)
                : _session.TryResolvePlayerSatanNumbers(
                    interactionId,
                    firstNumber,
                    secondNumber);
        }

        private bool ShouldAnimateBelphegorReinsert(
            GameSceneCombatHudCommand command)
        {
            PendingDemonContractInteraction pending =
                Battle?.PendingPlayerDemonContractInteraction;
            return pending != null &&
                pending.InteractionId == command.InteractionId &&
                pending.Kind == DemonContractInteractionKind.BelphegorTopCard &&
                command.OptionId ==
                    BelphegorDemonContractHandler.MoveTopCardToBottomOptionId;
        }

        private bool TryBeginPlayerActiveDemonContractAction(
            int sourceContractCardId)
        {
            return IsStageBattle
                ? _stageSession.TryBeginPlayerActiveDemonContractAction(
                    sourceContractCardId)
                : _session.TryBeginPlayerActiveDemonContractAction(
                    sourceContractCardId);
        }

        private void TryStartPlayerMammonPhysicalRoll()
        {
            if (IsModalInputBlocked || _inputLocked || mammonDie == null)
            {
                return;
            }

            GameSceneViewModel viewModel = Battle == null
                ? null
                : GameScenePresenter.Create(Battle, _activeEnemyProfileKey);
            if (viewModel?.PlayerMammonSourceCardId == null ||
                !viewModel.CanPlayerRerollMammon)
            {
                return;
            }

            int sourceContractCardId = viewModel.PlayerMammonSourceCardId.Value;
            _inputLocked = true;
            UpdateShopLeaveControl();
            mammonDie.Render(viewModel.PlayerMammonDieValue, false);
            mammonDie.PlayPhysicalRoll(
                landedValue => ResolvePlayerMammonPhysicalRoll(
                    sourceContractCardId,
                    landedValue));
        }

        private void ResolvePlayerMammonPhysicalRoll(
            int sourceContractCardId,
            int landedValue)
        {
            _inputLocked = false;
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryBeginPlayerMammonReroll(
                    sourceContractCardId,
                    landedValue)
                : _session.TryBeginPlayerMammonReroll(
                    sourceContractCardId,
                    landedValue));
        }

        // Fires synchronously for each sub-step while the battle resolves the turn. Snapshots the
        // public view state at that instant so PlayTimeline can reveal them one beat at a time.
        private void OnBattleStepped()
        {
            _timeline.Add(GameScenePresenter.Create(Battle, _activeEnemyProfileKey));
        }

        private IEnumerator PlayTimeline(GameSceneViewModel timelineBaseline)
        {
            List<GameSceneViewModel> timeline =
                new List<GameSceneViewModel>(_timeline);
            _timeline.Clear();
            GameSceneViewModel pendingKnifeReveal = null;

            for (int index = 0; index < timeline.Count; index++)
            {
                GameSceneViewModel vm = timeline[index];
                GameSceneViewModel previous = index == 0
                    ? timelineBaseline
                    : timeline[index - 1];
                RoundComparisonPlan comparisonPlan = vm.RoundComparisonPlan;
                if (_pendingPlayerMammonComparison != null &&
                    comparisonPlan == null)
                {
                    // The choice resolves synchronously and may publish an enemy Mammon decision
                    // before the final resolution. Keep the already-counted player prefix intact.
                    continue;
                }

                bool comparisonBeat = ShouldPlayRoundComparison(
                    _lastRoundComparisonResolutionId,
                    comparisonPlan);
                bool directResolutionBeat =
                    ShouldSkipRoundComparisonForDecisiveHiddenGuess(
                        _lastRoundComparisonResolutionId,
                        comparisonPlan);
                if (comparisonPlan != null &&
                    !comparisonBeat &&
                    !directResolutionBeat)
                {
                    continue;
                }

                PlayerMammonComparisonPlan mammonPrefixPlan =
                    vm.PlayerMammonComparisonPlan;
                bool beginsPlayerMammonComparison =
                    mammonPrefixPlan != null &&
                    _pendingPlayerMammonComparison == null;
                if (beginsPlayerMammonComparison)
                {
                    BeginPlayerMammonComparison(mammonPrefixPlan);
                }
                // Searches the rest of the timeline (not just index + 1) because an
                // automatic-activation card (e.g. poison) drawn mid-knife-effect inserts
                // its own beat(s) between the reveal and the knife's resolved beat —
                // those intervening beats still render/animate normally on their own
                // turn through this same loop, and the knife's reveal+throw combo still
                // finds and pairs with its resolved beat once it's reached.
                if (IsKnifeRevealBeat(previous, vm) &&
                    TryFindMatchingKnifeResolvedBeatIndex(
                        timeline,
                        index + 1,
                        vm,
                        out _))
                {
                    pendingKnifeReveal = vm;
                    continue;
                }

                bool revealKnifeCardWithThrow =
                    pendingKnifeReveal != null &&
                    IsMatchingKnifeResolvedBeat(pendingKnifeReveal, vm);
                DemonCardView satanAttackSource;
                bool playedSatanAttack = TryPlaySatanAttackAnimation(
                    vm.SatanAttackAnimationCue,
                    out satanAttackSource);
                if (playedSatanAttack)
                {
                    yield return WaitForSatanAttackAnimation(satanAttackSource);
                }

                AppliedAnimationResult playedAnimation = ApplyView(
                    vm,
                    scheduleRevolverRetry: false,
                    deferHammerSmashCardRender: true,
                    deferKnifeResultCardRender: revealKnifeCardWithThrow,
                    deferRevolverResultCardRender: true,
                    showTransientEffectSources: true,
                    preserveRoundComparisonCardsAndTotals:
                        beginsPlayerMammonComparison,
                    deferRoundResultPresentation: comparisonBeat);

                // The Satan source card can be instantiated by ApplyView when the timeline
                // starts from a stale/empty hand. Retry after rendering so the attack cannot be
                // dropped just because the source view was not available in the first snapshot.
                if (!playedSatanAttack)
                {
                    playedSatanAttack = TryPlaySatanAttackAnimation(
                        vm.SatanAttackAnimationCue,
                        out satanAttackSource);
                    if (playedSatanAttack)
                    {
                        yield return WaitForSatanAttackAnimation(
                            satanAttackSource);
                    }
                }

                if (revealKnifeCardWithThrow)
                {
                    yield return RenderHandsThenTotalsAfterRevealFlip(
                        pendingKnifeReveal,
                        showTransientEffectSources: true);
                }

                CardView satanNumberGuessTarget;
                bool playedSatanNumberGuess =
                    TryPlaySatanNumberGuessAnimation(
                        vm.SatanNumberGuessAnimationCue,
                        out satanNumberGuessTarget);
                if (playedSatanNumberGuess)
                {
                    yield return PlaySatanNumberGuessSequence(
                        vm.SatanNumberGuessAnimationCue,
                        satanNumberGuessTarget);

                    if (vm.SatanNumberGuessAnimationCue.Succeeded)
                    {
                        _satanNumberGuessCardIdToSuppress =
                            vm.SatanNumberGuessAnimationCue.TargetCardId;
                        _satanNumberGuessSuppressedCardSide =
                            vm.SatanNumberGuessAnimationCue.ActorSide ==
                                CombatantSide.Player
                                ? CombatantSide.Enemy
                                : CombatantSide.Player;
                    }
                }

                bool resolveBeat = vm.Core.State == CoreLoopState.ResolvingRound;
                float waitSeconds = comparisonBeat
                    ? 0f
                    : beginsPlayerMammonComparison
                    ? 0f
                    : resolveBeat
                    ? Mathf.Max(resolveHoldSeconds, MinimumRoundResultHoldSeconds)
                    : stepSeconds;
                if (IsKnifeConcealedCardBeat(previous, vm))
                {
                    waitSeconds = Mathf.Max(waitSeconds, knifeSuspenseSeconds);
                }

                if (playedAnimation.PlayedAny)
                {
                    waitSeconds = Mathf.Max(
                        waitSeconds,
                        playedAnimation.WaitSeconds);
                }

                yield return WaitForAnimationOrSeconds(
                    playedAnimation,
                    waitSeconds);

                if (comparisonBeat)
                {
                    yield return PlayRoundComparison(vm, comparisonPlan);
                    pendingKnifeReveal = null;
                    continue;
                }

                if (mammonPrefixPlan != null)
                {
                    yield return PlayPlayerMammonComparisonPrefix(
                        vm,
                        mammonPrefixPlan);
                    yield break;
                }

                bool playedResultSpeech = PresentEnemySpeech(
                    vm.EnemySpeechObservation,
                    SpeechPlaybackMoment.AfterAnimation);
                if (playedResultSpeech && stepSeconds > 0f)
                {
                    yield return new WaitForSeconds(stepSeconds);
                }

                if (playedAnimation.DeferredCardRender)
                {
                    yield return RenderHandsThenTotalsAfterRevealFlip(
                        playedAnimation.DeferredViewModel,
                        showTransientEffectSources: true);
                }

                if (directResolutionBeat)
                {
                    CompleteDirectRoundResolutionPresentation(comparisonPlan);
                }

                pendingKnifeReveal = null;

                GameSceneRevolverAnimationCue revolverCue =
                    vm.RevolverAnimationCue;
                if (playedAnimation.PlayedRevolver &&
                    revolverCue != null &&
                    revolverCue.Phase ==
                        GameSceneRevolverAnimationPhase.ResolvedWithRetry)
                {
                    PrepareRevolverRetry(revolverCue);
                    if (revolverCue.ActorSide == CombatantSide.Enemy &&
                        stepSeconds > 0f)
                    {
                        yield return new WaitForSeconds(stepSeconds);
                    }
                }
            }

            while (ShouldHoldInputForRevolverReady(
                       _revolverReadyActive,
                       _revolverSelectionReady,
                       _revolverReadyActorSide))
            {
                yield return null;
            }

            // Land on the true current state — e.g. BattleEnded, which is not itself a step.
            UnlockInput();
            RefreshView();
            ReturnToProgressionIfStageBattleEnded();
        }

        internal static bool ShouldPlayRoundComparison(
            long lastResolutionId,
            RoundComparisonPlan plan)
        {
            return plan != null &&
                plan.ResolutionId != lastResolutionId &&
                plan.PlaybackMode == RoundComparisonPlaybackMode.CountTotals;
        }

        internal static bool ShouldSkipRoundComparisonForDecisiveHiddenGuess(
            long lastResolutionId,
            RoundComparisonPlan plan)
        {
            return plan != null &&
                plan.ResolutionId != lastResolutionId &&
                plan.PlaybackMode ==
                    RoundComparisonPlaybackMode.SkipForDecisiveHiddenGuess;
        }

        internal static bool ShouldHideCombatHudForPresentation(
            bool inputLocked,
            bool roundComparisonActive,
            bool deferRoundResultPresentation,
            bool hasBlockingAnimationCue)
        {
            return inputLocked &&
                (roundComparisonActive ||
                 deferRoundResultPresentation ||
                 hasBlockingAnimationCue);
        }

        private void BeginPlayerMammonComparison(
            PlayerMammonComparisonPlan plan)
        {
            _roundComparisonActive = true;
            _pendingPlayerMammonComparison = plan;
            totals?.BeginComparison();
        }

        private IEnumerator PlayPlayerMammonComparisonPrefix(
            GameSceneViewModel vm,
            PlayerMammonComparisonPlan plan)
        {
            yield return PlayComparisonSteps(
                CombatantSide.Player,
                playerHand,
                plan.Player.PublicSteps);
            yield return RevealComparisonHand(
                playerHand,
                plan.RevealedPlayerCards,
                vm.PlayerDemonCards);
            yield return PlayComparisonStep(
                CombatantSide.Player,
                playerHand,
                plan.Player.HiddenStep);

            UnlockInput();
            ApplyView(
                vm,
                showTransientEffectSources: true,
                preserveRoundComparisonCardsAndTotals: true);
        }

        private IEnumerator PlayRoundComparison(
            GameSceneViewModel vm,
            RoundComparisonPlan plan)
        {
            bool resumesPlayerMammon = _pendingPlayerMammonComparison != null;
            if (!resumesPlayerMammon)
            {
                _roundComparisonActive = true;
                totals?.BeginComparison();
                yield return PlayComparisonSteps(
                    CombatantSide.Player,
                    playerHand,
                    plan.Player.PublicSteps);
                yield return PlayComparisonSteps(
                    CombatantSide.Enemy,
                    enemyHand,
                    plan.Enemy.PublicSteps);
                yield return RevealComparisonHand(
                    playerHand,
                    vm.PlayerCards,
                    vm.PlayerDemonCards);
                yield return PlayComparisonStep(
                    CombatantSide.Player,
                    playerHand,
                    plan.Player.HiddenStep);
            }
            else
            {
                // Keep the player side visually synchronized with the captured resolving model;
                // this does not reveal or render any enemy card.
                playerHand?.Render(
                    vm.PlayerCards,
                    vm.PlayerDemonCards,
                    showTransientEffectSources: true);
            }

            if (plan.Player.Bonus > 0)
            {
                yield return PlayComparisonBonus(
                    CombatantSide.Player,
                    plan.Player.FinalTotal);
            }

            if (resumesPlayerMammon)
            {
                yield return PlayComparisonSteps(
                    CombatantSide.Enemy,
                    enemyHand,
                    plan.Enemy.PublicSteps);
            }

            yield return RevealComparisonHand(
                enemyHand,
                vm.EnemyCards,
                vm.EnemyDemonCards);
            yield return PlayComparisonStep(
                CombatantSide.Enemy,
                enemyHand,
                plan.Enemy.HiddenStep);
            if (plan.Enemy.Bonus > 0)
            {
                yield return PlayComparisonBonus(
                    CombatantSide.Enemy,
                    plan.Enemy.FinalTotal);
            }

            ApplyView(
                vm,
                showTransientEffectSources: true,
                preserveRoundComparisonCardsAndTotals: true);
            PresentEnemySpeech(
                vm.EnemySpeechObservation,
                SpeechPlaybackMoment.AfterAnimation);

            yield return new WaitForSeconds(
                Mathf.Max(resolveHoldSeconds, MinimumRoundResultHoldSeconds));

            OnRoundResolutionResultHeld(plan);
            totals?.CompleteComparison(
                vm.PlayerTotalsText,
                vm.EnemyTotalsText);
            ClearComparisonHighlight();
            _lastRoundComparisonResolutionId = plan.ResolutionId;
            _pendingPlayerMammonComparison = null;
            _roundComparisonActive = false;
        }

        private void CompleteDirectRoundResolutionPresentation(
            RoundComparisonPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            OnRoundResolutionResultHeld(plan);
            _lastRoundComparisonResolutionId = plan.ResolutionId;
            _pendingPlayerMammonComparison = null;
            _roundComparisonActive = false;
        }

        private IEnumerator PlayComparisonSteps(
            CombatantSide side,
            CardHand hand,
            IReadOnlyList<RoundComparisonStep> steps)
        {
            if (steps == null)
            {
                yield break;
            }

            for (int index = 0; index < steps.Count; index++)
            {
                yield return PlayComparisonStep(side, hand, steps[index]);
            }
        }

        private IEnumerator PlayComparisonStep(
            CombatantSide side,
            CardHand hand,
            RoundComparisonStep step)
        {
            if (step == null)
            {
                yield break;
            }

            ClearComparisonHighlight();
            if (hand != null &&
                hand.TryGetCard(step.CardId, out CardView card))
            {
                _comparisonHighlightedCard = card;
                card.SetComparisonHighlighted(true);
            }

            totals?.AnimateComparisonTotal(
                side,
                step.Total,
                comparisonCountSeconds);
            if (comparisonCountSeconds > 0f)
            {
                yield return new WaitForSeconds(comparisonCountSeconds);
            }

            ClearComparisonHighlight();
            if (comparisonStepGapSeconds > 0f)
            {
                yield return new WaitForSeconds(comparisonStepGapSeconds);
            }
        }

        private IEnumerator PlayComparisonBonus(
            CombatantSide side,
            int finalTotal)
        {
            ClearComparisonHighlight();
            totals?.AnimateComparisonTotal(
                side,
                finalTotal,
                comparisonCountSeconds);
            if (comparisonCountSeconds > 0f)
            {
                yield return new WaitForSeconds(comparisonCountSeconds);
            }

            if (comparisonStepGapSeconds > 0f)
            {
                yield return new WaitForSeconds(comparisonStepGapSeconds);
            }
        }

        private IEnumerator RevealComparisonHand(
            CardHand hand,
            IReadOnlyList<GameSceneCardViewModel> cards,
            IReadOnlyList<GameSceneDemonCardViewModel> demonCards)
        {
            if (hand == null || cards == null)
            {
                yield break;
            }

            bool revealAnimated = hand.Render(
                cards,
                demonCards ?? Array.Empty<GameSceneDemonCardViewModel>(),
                showTransientEffectSources: true);
            if (revealAnimated)
            {
                yield return new WaitForSeconds(
                    ResolveCardRevealDurationSeconds());
            }
        }

        private float ResolveCardRevealDurationSeconds()
        {
            CardView prefab = playerHand != null ? playerHand.CardPrefab : null;
            prefab ??= enemyHand != null ? enemyHand.CardPrefab : null;
            return prefab != null ? prefab.RevealDurationSeconds : 0f;
        }

        private void ClearComparisonHighlight()
        {
            if (_comparisonHighlightedCard == null)
            {
                return;
            }

            _comparisonHighlightedCard.SetComparisonHighlighted(false);
            _comparisonHighlightedCard = null;
        }

        private void CancelRoundComparison(bool resetResolutionHistory)
        {
            ClearComparisonHighlight();
            if (totals != null && totals.IsComparisonActive)
            {
                totals.CancelComparison();
            }

            _roundComparisonActive = false;
            _pendingPlayerMammonComparison = null;
            if (resetResolutionHistory)
            {
                _lastRoundComparisonResolutionId = -1;
            }
        }

        /// <summary>
        /// Single extension seam for the follow-up shared soul-loss presentation. The immutable
        /// plan already carries both damage values and the resolution id at this exact beat.
        /// </summary>
        private static void OnRoundResolutionResultHeld(RoundComparisonPlan plan)
        {
            _ = plan;
        }

        private void RefreshView()
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            GameSceneViewModel vm =
                GameScenePresenter.Create(battle, _activeEnemyProfileKey);
            ApplyView(vm);
            MaybeOpenShop(vm);
            _tutorialDirector?.Observe();
        }

        private AppliedAnimationResult ApplyView(
            GameSceneViewModel vm,
            bool scheduleRevolverRetry = true,
            bool deferHammerSmashCardRender = false,
            bool deferKnifeResultCardRender = false,
            bool deferRevolverResultCardRender = false,
            bool showTransientEffectSources = false,
            bool preserveRoundComparisonCardsAndTotals = false,
            bool deferRoundResultPresentation = false)
        {
            _core = vm.Core;
            _enemyMammonDieValue = vm.EnemyMammonDieValue;
            bool isShopOpen = shop != null && shop.IsOpen;
            // Covers both first appearance (CurrentValue defaults to 0) and any later change the
            // die view hasn't shown yet (e.g. a silent round-start reroll) — not just null→value.
            bool playedMammonRoll =
                !isShopOpen &&
                vm.PlayerMammonDieValue.HasValue &&
                mammonDie != null &&
                mammonDie.CurrentValue != vm.PlayerMammonDieValue.Value;
            mammonDie?.Render(
                isShopOpen ? null : vm.PlayerMammonDieValue,
                !isShopOpen && !_inputLocked &&
                    vm.CanPlayerRerollMammon);
            if (playedMammonRoll)
            {
                mammonDie?.PlayRoll(vm.PlayerMammonDieValue.Value);
            }
            bool hasBlockingAnimationCue =
                vm.HammerAnimationCue != null ||
                vm.RevolverAnimationCue != null ||
                vm.KnifeAnimationCue != null ||
                vm.SatanAttackAnimationCue != null ||
                vm.SatanNumberGuessAnimationCue != null;
            bool hideCombatHudForPresentation =
                ShouldHideCombatHudForPresentation(
                    _inputLocked,
                    _roundComparisonActive,
                    deferRoundResultPresentation,
                    hasBlockingAnimationCue);
            GameSceneCombatHudViewModel combat =
                GameSceneCombatHudPresenter.Create(
                    vm.Core,
                    IsStageBattle,
                    isShopOpen,
                    _inputLocked,
                    vm.UsesDiegeticCardEffectSelection,
                    hideForPresentation: hideCombatHudForPresentation,
                    satanSelectedNumberCount:
                        satanNumberSelection?.SelectedCount ?? 0,
                    restrictedPrimaryAction: _tutorialRestrictedPrimaryAction,
                    restrictedContractDefinitionKey:
                        _tutorialRestrictedContractDefinitionKey,
                    restrictedOptionId: _tutorialRestrictedOptionId);

            if (hud != null)
            {
                hud.Render(vm.Core, combat);
                int gold = IsStageBattle
                    ? _stageSession.Progress.Player.CurrentGold
                    : shop != null ? shop.Gold : 0;
                hud.SetGold(gold);
            }

            UpdateShopLeaveControl();

            if (combat.Mode != GameSceneCombatHudMode.Actions)
            {
                UpdateCombatCommandHover(null);
            }

            tableCombatCommands?.Render(combat);

            RenderDemonContractSelection(combat);
            contractPapers?.Render(ContractPaperPresenter.Create(
                Battle,
                isCombatVisible: !isShopOpen,
                forceDisabled: _tutorialContractPaperBlocked));

            RefreshDeckStacks(vm);
            RefreshShopUtilityItems();

            bool playedRevolverAnimation =
                TryPlayRevolverAnimation(
                    vm.RevolverAnimationCue,
                    scheduleRevolverRetry);
            bool playedKnifeAnimation =
                TryPlayKnifeAnimation(vm.KnifeAnimationCue);
            _playedHammerAnimationController = null;
            bool playedHammerAnimation =
                TryPlayHammerAnimation(vm.HammerAnimationCue);
            bool playedPoisonInjectionAnimation =
                TryPlayPoisonInjectionAnimation(vm.PoisonInjectionAnimationCue);
            UpdateEnemyCardSelectionCamera(
                vm.FocusesEnemyCardsForSelection);
            bool deferredCardRender =
                (deferHammerSmashCardRender &&
                 playedHammerAnimation &&
                 IsHammerSmashCue(vm.HammerAnimationCue)) ||
                (deferKnifeResultCardRender &&
                 playedKnifeAnimation &&
                 vm.KnifeAnimationCue?.Phase ==
                    GameSceneKnifeAnimationPhase.Resolved) ||
                (deferRevolverResultCardRender &&
                 playedRevolverAnimation &&
                 IsRevolverResolvedCue(vm.RevolverAnimationCue));

            // While the shop is open its presentation (merchant, hidden combat objects, goods) is owned
            // by ShopController; skip the combat re-render so it doesn't repaint the enemy over the merchant.
            if (shop != null && shop.IsOpen)
            {
                return CreateAppliedAnimationResult(
                    playedRevolverAnimation,
                    playedKnifeAnimation,
                    playedHammerAnimation,
                    playedPoisonInjectionAnimation,
                    deferredCardRender: false,
                    deferredViewModel: null);
            }

            bool playedCardReveal = false;
            if (!deferredCardRender &&
                !preserveRoundComparisonCardsAndTotals &&
                !deferRoundResultPresentation)
            {
                playedCardReveal =
                    RenderHandsAndTotals(vm, showTransientEffectSources);
            }

            RenderCrystalOrbSelection(vm);
            RenderSatanNumberSelection(vm);

            bool rendersDeferredAttackVisual =
                deferRoundResultPresentation &&
                (playedRevolverAnimation ||
                 playedKnifeAnimation ||
                 playedHammerAnimation);
            if (enemyCharacter != null &&
                (!deferRoundResultPresentation || rendersDeferredAttackVisual))
            {
                enemyCharacter.RenderVisual(
                    ResolveKnifeTimedVisual(
                        CombatantSide.Enemy,
                        ResolveRevolverTimedVisual(
                            CombatantSide.Enemy,
                            vm.EnemyVisual)));
                if (!deferRoundResultPresentation)
                {
                    PresentEnemySpeech(
                        vm.EnemySpeechObservation,
                        SpeechPlaybackMoment.BeforeAnimation);
                }
            }

            return CreateAppliedAnimationResult(
                playedRevolverAnimation,
                playedKnifeAnimation,
                playedHammerAnimation,
                playedPoisonInjectionAnimation,
                deferredCardRender,
                deferredCardRender ? vm : null,
                playedMammonRoll,
                playedCardReveal);
        }

        private bool PresentEnemySpeech(
            EnemySpeechObservation observation,
            SpeechPlaybackMoment playbackMoment = SpeechPlaybackMoment.Any)
        {
            if (observation == null || enemyCharacter == null)
            {
                return false;
            }

            // The tutorial has its own single narrator (Asmodeus) — the enemy's ordinary
            // combat barks would talk over it and aren't part of the scripted dialogue.
            if (_tutorialDirector != null)
            {
                return false;
            }

            _enemySpeechDirector ??= new EnemySpeechDirector(speechSeed);
            if (!_enemySpeechDirector.TryResolve(
                observation,
                _activeEnemySpeechProfile,
                playbackMoment,
                out EnemySpeechPresentation presentation))
            {
                return false;
            }

            enemyCharacter.ShowSpeech(presentation.Message);
            if (presentation.IsTerminal)
            {
                BeginTerminalSpeechHold(observation.Battle);
            }

            return true;
        }

        private void ResetEnemySpeech()
        {
            if (_terminalSpeechHoldRoutine != null)
            {
                StopCoroutine(_terminalSpeechHoldRoutine);
                _terminalSpeechHoldRoutine = null;
            }

            _terminalSpeechBattle = null;
            _terminalSpeechHoldActive = false;
            _terminalSpeechHoldCompleted = false;
            _enemySpeechDirector?.Reset();
            enemyCharacter?.HideSpeech();
        }

        private void BeginTerminalSpeechHold(CoreLoopBattle battle)
        {
            if (battle == null ||
                (ReferenceEquals(_terminalSpeechBattle, battle) &&
                 (_terminalSpeechHoldActive || _terminalSpeechHoldCompleted)))
            {
                return;
            }

            _terminalSpeechBattle = battle;
            _terminalSpeechHoldActive = true;
            _terminalSpeechHoldCompleted = false;
            _inputLocked = true;
            _terminalSpeechHoldRoutine = StartCoroutine(
                CompleteTerminalSpeechHold(battle));
        }

        private IEnumerator CompleteTerminalSpeechHold(CoreLoopBattle battle)
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, terminalSpeechHoldSeconds));

            _terminalSpeechHoldRoutine = null;
            if (!ReferenceEquals(Battle, battle))
            {
                yield break;
            }

            _terminalSpeechHoldActive = false;
            _terminalSpeechHoldCompleted = true;
            if (IsStageBattle)
            {
                ReturnToProgressionIfStageBattleEnded();
                yield break;
            }

            GameSceneViewModel vm =
                GameScenePresenter.Create(battle, _activeEnemyProfileKey);
            MaybeOpenShop(vm);
            if (shop == null || !shop.IsOpen)
            {
                UnlockInput();
            }
        }

        /// <returns>True if any card started its reveal-flip animation this call (back
        /// turning to face-up) — see <see cref="CardView.WillAnimateRevealFor"/>.</returns>
        private bool RenderHands(
            GameSceneViewModel vm,
            bool showTransientEffectSources)
        {
            if (vm == null || _suppressHandRenderUntilRoundOneStart)
            {
                return false;
            }

            bool anyRevealAnimated = false;
            IReadOnlyList<GameSceneCardViewModel> playerCards =
                vm.PlayerCards;
            IReadOnlyList<GameSceneCardViewModel> enemyCards =
                vm.EnemyCards;
            if (_satanNumberGuessCardIdToSuppress >= 0)
            {
                if (_satanNumberGuessSuppressedCardSide ==
                    CombatantSide.Player)
                {
                    playerCards = WithoutCard(
                        playerCards,
                        _satanNumberGuessCardIdToSuppress);
                }
                else
                {
                    enemyCards = WithoutCard(
                        enemyCards,
                        _satanNumberGuessCardIdToSuppress);
                }
            }

            if (playerHand != null)
            {
                anyRevealAnimated |= playerHand.Render(
                    playerCards,
                    vm.PlayerDemonCards,
                    showTransientEffectSources);
            }

            if (enemyHand != null)
            {
                anyRevealAnimated |= enemyHand.Render(
                    enemyCards,
                    vm.EnemyDemonCards,
                    showTransientEffectSources);
            }

            ClearSatanNumberGuessSuppressionIfTargetIsGone(vm);
            return anyRevealAnimated;
        }

        private void ClearSatanNumberGuessSuppressionIfTargetIsGone(
            GameSceneViewModel vm)
        {
            if (_satanNumberGuessCardIdToSuppress < 0 || vm == null)
            {
                return;
            }

            IReadOnlyList<GameSceneCardViewModel> cards =
                _satanNumberGuessSuppressedCardSide == CombatantSide.Player
                    ? vm.PlayerCards
                    : vm.EnemyCards;
            if (ContainsCard(cards, _satanNumberGuessCardIdToSuppress))
            {
                return;
            }

            _satanNumberGuessCardIdToSuppress = -1;
            _satanNumberGuessSuppressedCardSide = CombatantSide.Player;
        }

        private static IReadOnlyList<GameSceneCardViewModel> WithoutCard(
            IReadOnlyList<GameSceneCardViewModel> cards,
            int cardId)
        {
            if (cards == null || !ContainsCard(cards, cardId))
            {
                return cards;
            }

            var filteredCards =
                new List<GameSceneCardViewModel>(cards.Count - 1);
            for (int index = 0; index < cards.Count; index++)
            {
                GameSceneCardViewModel card = cards[index];
                if (card != null && card.CardId == cardId)
                {
                    continue;
                }

                filteredCards.Add(card);
            }

            return filteredCards;
        }

        private static bool ContainsCard(
            IReadOnlyList<GameSceneCardViewModel> cards,
            int cardId)
        {
            if (cards == null)
            {
                return false;
            }

            for (int index = 0; index < cards.Count; index++)
            {
                if (cards[index] != null && cards[index].CardId == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderTotals(GameSceneViewModel vm)
        {
            if (totals == null || vm == null)
            {
                return;
            }

            totals.Render(vm.PlayerTotalsText, vm.EnemyTotalsText);
        }

        // A newly revealed card still plays its own flip animation in CardView; the hand total
        // must not count it until the flip has actually turned the sprite face-up, or the
        // number jumps while the card still looks face-down. Used from non-coroutine call
        // sites (e.g. the default ApplyView render), so the wait runs as its own coroutine
        // rather than blocking the caller.
        private bool RenderHandsAndTotals(
            GameSceneViewModel vm,
            bool showTransientEffectSources)
        {
            if (vm == null)
            {
                return false;
            }

            bool revealAnimated = RenderHands(vm, showTransientEffectSources);
            if (revealAnimated)
            {
                StartCoroutine(RenderTotalsAfterRevealFlip(vm));
            }
            else
            {
                RenderTotals(vm);
            }

            return revealAnimated;
        }

        // Same as RenderHandsAndTotals, but yieldable so a driving coroutine (PlayTimeline) can
        // wait for it directly instead of racing a fire-and-forget StartCoroutine.
        private IEnumerator RenderHandsThenTotalsAfterRevealFlip(
            GameSceneViewModel vm,
            bool showTransientEffectSources)
        {
            if (RenderHands(vm, showTransientEffectSources))
            {
                yield return new WaitForSeconds(ResolveCardRevealFaceSwapSeconds());
            }

            RenderTotals(vm);
        }

        private IEnumerator RenderTotalsAfterRevealFlip(GameSceneViewModel vm)
        {
            yield return new WaitForSeconds(ResolveCardRevealFaceSwapSeconds());
            RenderTotals(vm);
        }

        private float ResolveCardRevealFaceSwapSeconds()
        {
            CardView prefab = playerHand != null ? playerHand.CardPrefab : null;
            prefab ??= enemyHand != null ? enemyHand.CardPrefab : null;
            return prefab != null ? prefab.RevealFaceSwapSeconds : 0f;
        }

        internal static bool IsKnifeConcealedCardBeat(
            GameSceneViewModel previous,
            GameSceneViewModel current)
        {
            return TryFindKnifeCardTransition(
                previous,
                current,
                expectedPreviousFaceUp: null,
                expectedCurrentFaceUp: false);
        }

        internal static bool IsKnifeRevealBeat(
            GameSceneViewModel previous,
            GameSceneViewModel current)
        {
            return TryFindKnifeCardTransition(
                previous,
                current,
                expectedPreviousFaceUp: false,
                expectedCurrentFaceUp: true);
        }

        private static bool TryFindKnifeCardTransition(
            GameSceneViewModel previous,
            GameSceneViewModel current,
            bool? expectedPreviousFaceUp,
            bool expectedCurrentFaceUp)
        {
            GameSceneKnifeAnimationCue cue = current?.KnifeAnimationCue;
            if (previous == null || cue == null ||
                cue.Phase != GameSceneKnifeAnimationPhase.Ready)
            {
                return false;
            }

            IReadOnlyList<GameSceneCardViewModel> previousCards =
                cue.ActorSide == CombatantSide.Player
                    ? previous.EnemyCards
                    : previous.PlayerCards;
            IReadOnlyList<GameSceneCardViewModel> currentCards =
                cue.ActorSide == CombatantSide.Player
                    ? current.EnemyCards
                    : current.PlayerCards;
            for (int i = 0; i < currentCards.Count; i++)
            {
                GameSceneCardViewModel currentCard = currentCards[i];
                if (currentCard.IsFaceUp != expectedCurrentFaceUp)
                {
                    continue;
                }

                GameSceneCardViewModel previousCard = null;
                for (int j = 0; j < previousCards.Count; j++)
                {
                    if (previousCards[j].CardId == currentCard.CardId)
                    {
                        previousCard = previousCards[j];
                        break;
                    }
                }

                if (!expectedPreviousFaceUp.HasValue)
                {
                    if (previousCard == null)
                    {
                        return true;
                    }

                    continue;
                }

                if (previousCard != null &&
                    previousCard.IsFaceUp == expectedPreviousFaceUp.Value)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsMatchingKnifeResolvedBeat(
            GameSceneViewModel ready,
            GameSceneViewModel resolved)
        {
            GameSceneKnifeAnimationCue readyCue = ready?.KnifeAnimationCue;
            GameSceneKnifeAnimationCue resolvedCue = resolved?.KnifeAnimationCue;
            return readyCue != null &&
                resolvedCue != null &&
                readyCue.Phase == GameSceneKnifeAnimationPhase.Ready &&
                resolvedCue.Phase == GameSceneKnifeAnimationPhase.Resolved &&
                readyCue.RoundNumber == resolvedCue.RoundNumber &&
                readyCue.SourceCardId == resolvedCue.SourceCardId &&
                readyCue.ActorSide == resolvedCue.ActorSide;
        }

        // Scans forward from fromIndex (not just fromIndex itself) so intervening beats —
        // e.g. an automatic-activation card drawn mid-knife-effect — don't break the
        // reveal/resolved pairing; RoundNumber+SourceCardId+ActorSide in
        // IsMatchingKnifeResolvedBeat already scope the match to this exact knife use.
        internal static bool TryFindMatchingKnifeResolvedBeatIndex(
            IReadOnlyList<GameSceneViewModel> timeline,
            int fromIndex,
            GameSceneViewModel readyBeat,
            out int resolvedIndex)
        {
            for (int i = fromIndex; i < timeline.Count; i++)
            {
                if (IsMatchingKnifeResolvedBeat(readyBeat, timeline[i]))
                {
                    resolvedIndex = i;
                    return true;
                }
            }

            resolvedIndex = -1;
            return false;
        }

        private void RenderCrystalOrbSelection(GameSceneViewModel vm)
        {
            if (crystalOrbSelection == null)
            {
                return;
            }

            if (vm == null || vm.CrystalOrbCandidates.Count == 0)
            {
                crystalOrbSelection.Hide();
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            crystalOrbSelection.Render(vm.CrystalOrbCandidates, _camera);
        }

        private void RenderSatanNumberSelection(GameSceneViewModel vm)
        {
            if (satanNumberSelection == null)
            {
                return;
            }

            if (vm == null || vm.SatanNumberCandidates.Count == 0)
            {
                satanNumberSelection.Hide();
                StopSatanNumberSelectionHeartSlow();
                return;
            }

            StartSatanNumberSelectionHeartSlow();
            _camera ??= Camera.main;
            satanNumberSelection.Render(
                vm.SatanNumberCandidates,
                _camera,
                vm.Core?.DemonContract.InteractionId ?? -1);
        }

        private void RefreshSatanSelectionHud()
        {
            if (_core == null)
            {
                return;
            }

            bool isShopOpen = shop != null && shop.IsOpen;
            GameSceneCombatHudViewModel combat =
                GameSceneCombatHudPresenter.Create(
                    _core,
                    IsStageBattle,
                    isShopOpen,
                    _inputLocked,
                    satanSelectedNumberCount:
                        satanNumberSelection?.SelectedCount ?? 0,
                    restrictedPrimaryAction: _tutorialRestrictedPrimaryAction,
                    restrictedContractDefinitionKey:
                        _tutorialRestrictedContractDefinitionKey,
                    restrictedOptionId: _tutorialRestrictedOptionId);
            hud?.Render(_core, combat);
            tableCombatCommands?.Render(combat);
        }

        private void RefreshDeckStacks(GameSceneViewModel vm)
        {
            if (vm == null)
            {
                remainingDeck?.Render(0);
                discardDeck?.Render(0);
                enemyRemainingDeck?.Render(0);
                enemyDiscardDeck?.Render(0);
                return;
            }

            remainingDeck?.Render(vm.PlayerDrawPileCount);
            discardDeck?.Render(vm.PlayerDiscardPileCount);
            enemyRemainingDeck?.Render(vm.EnemyDrawPileCount);
            enemyDiscardDeck?.Render(vm.EnemyDiscardPileCount);
        }

        private void RefreshShopUtilityItems()
        {
            CoreLoopBattle battle = Battle;
            if (shop == null || !shop.IsOpen || battle == null)
            {
                return;
            }

            shop.RefreshUtilityItems(
                BuildRunDeckCardOptions().Count,
                battle.Player.Soul.Current,
                battle.Player.Soul.Maximum);
        }

        private bool TryPlayRevolverAnimation(
            GameSceneRevolverAnimationCue cue,
            bool scheduleRevolverRetry)
        {
            if (cue == null || revolverAnimator == null)
            {
                return false;
            }

            if (IsLastRevolverAnimationCue(cue))
            {
                return false;
            }

            RememberRevolverAnimationCue(cue);

            GameObject root = ResolveRevolverRoot();
            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            if (!revolverAnimator.gameObject.activeInHierarchy)
            {
                return false;
            }

            StopRevolverHideRoutine();
            StopRevolverShotRoutine();
            ResetRevolverTriggers();

            if (cue.Phase == GameSceneRevolverAnimationPhase.Ready)
            {
                ClearPendingRevolverImpact();
                ResetRevolverAnimatorToBase();
                revolverAnimator.SetTrigger(playerReadyTrigger);
                RememberActiveRevolverReady(cue);
                BeginRevolverReadyCameraSequence(cue.ActorSide);
                return false;
            }

            if (cue.ActorSide == CombatantSide.Player &&
                !IsMatchingActiveRevolverReady(cue))
            {
                ResetRevolverAnimatorToBase();
                revolverAnimator.SetTrigger(playerReadyTrigger);
            }

            if (cue.Succeeded)
            {
                _revolverImpactPending = true;
                _revolverImpactTargetSide = Opposite(cue.ActorSide);
                BindRevolverImpactEvent();
            }
            else
            {
                ClearPendingRevolverImpact();
            }

            StopRevolverReadyCameraRoutine();
            _revolverSelectionReady = false;
            _revolverReadyActive = false;
            if (cue.ActorSide == CombatantSide.Player)
            {
                // Only the player's own revolver sequence ever moves the camera to the
                // close-up table view, so only it needs to hand control back afterward. The
                // enemy's sequence never touches the camera, so the viewer's current view
                // (even one they picked manually) must be left alone here.
                ResolveCameraViewController()?.SetView(GameSceneCameraView.Current);
            }

            if (Application.isPlaying && revolverCameraReturnSeconds > 0f)
            {
                _revolverShotRoutine = StartCoroutine(
                    PlayRevolverShotAfterCameraReturn(cue));
            }
            else
            {
                PlayRevolverShot(cue);
            }

            if (Application.isPlaying &&
                cue.Phase == GameSceneRevolverAnimationPhase.ResolvedWithRetry &&
                scheduleRevolverRetry)
            {
                if (RevolverResolvedSequenceSeconds > 0f)
                {
                    _revolverHideRoutine =
                        StartCoroutine(PrepareRevolverRetryAfterDelay(cue));
                }
                else
                {
                    PrepareRevolverRetry(cue);
                }
            }
            else if (Application.isPlaying &&
                cue.Phase == GameSceneRevolverAnimationPhase.Resolved &&
                RevolverResolvedSequenceSeconds > 0f)
            {
                _revolverHideRoutine =
                    StartCoroutine(HideRevolverAnimationAfterDelay());
            }

            return true;
        }

        private bool TryPlayKnifeAnimation(GameSceneKnifeAnimationCue cue)
        {
            Animator animator = ResolveKnifeAnimator();
            if (cue == null || animator == null || IsLastKnifeAnimationCue(cue))
            {
                return false;
            }

            RememberKnifeAnimationCue(cue);
            GameObject root = ResolveKnifeRoot();
            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            if (!animator.gameObject.activeInHierarchy)
            {
                return false;
            }

            StopKnifeHideRoutine();
            ResetKnifeTriggers();

            if (cue.Phase == GameSceneKnifeAnimationPhase.Ready)
            {
                ClearPendingKnifeImpact();
                ResetKnifeAnimatorToBase();
                animator.SetTrigger(
                    cue.ActorSide == CombatantSide.Player
                        ? playerKnifeStartTrigger
                        : enemyKnifeStartTrigger);
                ApplyCinematicCamera(
                    ResolveKnifeCameraView(cue.ActorSide),
                    knifeReadySeconds);
                return true;
            }

            if (cue.Succeeded)
            {
                _knifeImpactPending = true;
                _knifeImpactTargetSide = Opposite(cue.ActorSide);
                BindKnifeImpactEvent();
            }
            else
            {
                ClearPendingKnifeImpact();
            }

            animator.SetTrigger(
                cue.Succeeded ? knifeSuccessTrigger : knifeFailTrigger);
            ApplyCinematicCamera(
                ResolveKnifeCameraView(cue.ActorSide),
                knifeResultSeconds);

            if (Application.isPlaying && knifeResultSeconds > 0f)
            {
                _knifeHideRoutine = StartCoroutine(HideKnifeAnimationAfterDelay());
            }

            return true;
        }

        internal static GameSceneCameraView ResolveKnifeCameraView(
            CombatantSide actorSide)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            return GameSceneCameraView.Current;
        }

        private Animator ResolveKnifeAnimator()
        {
            if (knifeAnimator != null)
            {
                return knifeAnimator;
            }

            KnifeAnimationEventReceiver receiver =
                FindFirstObjectByType<KnifeAnimationEventReceiver>(
                    FindObjectsInactive.Include);
            if (receiver != null)
            {
                _knifeEventReceiver = receiver;
                knifeAnimator = receiver.GetComponent<Animator>() ??
                    receiver.GetComponentInParent<Animator>(true) ??
                    receiver.GetComponentInChildren<Animator>(true);
            }

            if (knifeAnimator != null)
            {
                return knifeAnimator;
            }

            Animator[] animators = FindObjectsByType<Animator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                if (candidate != null && candidate.gameObject.name == "Knife_Anim")
                {
                    knifeAnimator = candidate;
                    break;
                }
            }

            return knifeAnimator;
        }

        private GameObject ResolveKnifeRoot()
        {
            if (knifeRoot != null)
            {
                return knifeRoot;
            }

            return knifeAnimator != null ? knifeAnimator.gameObject : null;
        }

        private void ResetKnifeTriggers()
        {
            ResetKnifeTrigger(playerKnifeStartTrigger);
            ResetKnifeTrigger(enemyKnifeStartTrigger);
            ResetKnifeTrigger(knifeSuccessTrigger);
            ResetKnifeTrigger(knifeFailTrigger);
        }

        private void ResetKnifeTrigger(string triggerName)
        {
            if (knifeAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                knifeAnimator.ResetTrigger(triggerName);
            }
        }

        private void ResetKnifeAnimatorToBase()
        {
            if (knifeAnimator == null ||
                !knifeAnimator.gameObject.activeInHierarchy ||
                string.IsNullOrWhiteSpace(knifeBaseStateName))
            {
                return;
            }

            knifeAnimator.Play(knifeBaseStateName, 0, 0f);
            knifeAnimator.Update(0f);
        }

        private bool IsLastKnifeAnimationCue(GameSceneKnifeAnimationCue cue)
        {
            return _hasLastKnifeAnimationCue &&
                _lastKnifeAnimationRoundNumber == cue.RoundNumber &&
                _lastKnifeAnimationSourceCardId == cue.SourceCardId &&
                _lastKnifeAnimationActorSide == cue.ActorSide &&
                _lastKnifeAnimationPhase == cue.Phase &&
                _lastKnifeAnimationSucceeded == cue.Succeeded;
        }

        private void RememberKnifeAnimationCue(GameSceneKnifeAnimationCue cue)
        {
            _hasLastKnifeAnimationCue = true;
            _lastKnifeAnimationRoundNumber = cue.RoundNumber;
            _lastKnifeAnimationSourceCardId = cue.SourceCardId;
            _lastKnifeAnimationActorSide = cue.ActorSide;
            _lastKnifeAnimationPhase = cue.Phase;
            _lastKnifeAnimationSucceeded = cue.Succeeded;
        }

        private bool TryPlayPoisonInjectionAnimation(
            GameScenePoisonInjectionAnimationCue cue)
        {
            if (cue == null ||
                poisonInjectionAnnounce == null ||
                poisonInjectionCardCatalog == null ||
                IsLastPoisonInjectionAnimationCue(cue))
            {
                return false;
            }

            RememberPoisonInjectionAnimationCue(cue);
            Sprite poisonSprite = poisonInjectionCardCatalog.GetNormalFaceSprite(
                CardDefinitionCatalog.PoisonKey,
                CardSuit.Spade);
            if (poisonSprite == null)
            {
                return false;
            }

            poisonInjectionAnnounce.Play(poisonSprite, onComplete: null);
            return true;
        }

        private bool IsLastPoisonInjectionAnimationCue(
            GameScenePoisonInjectionAnimationCue cue)
        {
            return _hasLastPoisonInjectionAnimationCue &&
                _lastPoisonInjectionAnimationRoundNumber == cue.RoundNumber;
        }

        private void RememberPoisonInjectionAnimationCue(
            GameScenePoisonInjectionAnimationCue cue)
        {
            _hasLastPoisonInjectionAnimationCue = true;
            _lastPoisonInjectionAnimationRoundNumber = cue.RoundNumber;
        }

        private void ResetPoisonInjectionAnimationState()
        {
            _hasLastPoisonInjectionAnimationCue = false;
            _lastPoisonInjectionAnimationRoundNumber = 0;
        }

        private void ResetKnifeAnimationState()
        {
            _hasLastKnifeAnimationCue = false;
            _lastKnifeAnimationRoundNumber = 0;
            _lastKnifeAnimationSourceCardId = 0;
            _lastKnifeAnimationActorSide = CombatantSide.Player;
            _lastKnifeAnimationPhase = GameSceneKnifeAnimationPhase.Ready;
            _lastKnifeAnimationSucceeded = false;
            ClearPendingKnifeImpact();
            HideKnifeAnimation();
        }

        private IEnumerator HideKnifeAnimationAfterDelay()
        {
            yield return new WaitForSeconds(knifeResultSeconds);
            _knifeHideRoutine = null;
            ResetKnifeAnimatorToBase();
            PresentationManager.Current?.ForceRestoreTransientCameraEffects();
            GameObject root = ResolveKnifeRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearPendingKnifeImpact();
        }

        private void StopKnifeHideRoutine()
        {
            if (_knifeHideRoutine == null)
            {
                return;
            }

            StopCoroutine(_knifeHideRoutine);
            _knifeHideRoutine = null;
        }

        private void HideKnifeAnimation()
        {
            StopKnifeHideRoutine();
            ClearPendingKnifeImpact();
            ResolveKnifeAnimator();
            ResetKnifeAnimatorToBase();
            PresentationManager.Current?.ForceRestoreTransientCameraEffects();
            GameObject root = ResolveKnifeRoot();
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private bool TryPlayHammerAnimation(GameSceneHammerAnimationCue cue)
        {
            HammerAnimationController controller = ResolveHammerAnimation();
            if (controller == null)
            {
                return false;
            }

            if (!controller.TryPlay(cue, playerHand, enemyHand))
            {
                // The same smash cue can appear in more than one timeline snapshot. It is still
                // the active presentation until the controller reports that the smash finished;
                // otherwise ApplyView would render the post-effect hand and discard the target
                // card while the hammer is still on screen.
                if (IsHammerSmashCue(cue) && controller.IsSmashAnimationPlaying)
                {
                    _playedHammerAnimationController = controller;
                    return true;
                }

                return false;
            }

            ApplyCinematicCamera(
                cue.ActorSide == CombatantSide.Player
                    ? GameSceneCameraView.EnemyFocus
                    : GameSceneCameraView.Current,
                controller,
                cue.ActorSide == CombatantSide.Player);
            _playedHammerAnimationController = controller;
            return true;
        }

        private bool TryPlaySatanAttackAnimation(
            GameSceneSatanAttackAnimationCue cue,
            out DemonCardView sourceCard)
        {
            sourceCard = null;
            if (cue == null || IsLastSatanAttackAnimationCue(cue))
            {
                return false;
            }

            CardHand sourceHand = cue.ActorSide == CombatantSide.Player
                ? playerHand
                : enemyHand;
            DeckStackView targetDeck = cue.ActorSide == CombatantSide.Player
                ? enemyRemainingDeck
                : remainingDeck;
            if (sourceHand == null ||
                targetDeck == null ||
                !sourceHand.TryGetDemonCard(cue.SourceCardId, out sourceCard) ||
                !sourceCard.PlaySatanAttackAnimation(targetDeck.transform.position))
            {
                sourceCard = null;
                return false;
            }

            RememberSatanAttackAnimationCue(cue);
            return true;
        }

        private IEnumerator WaitForSatanAttackAnimation(
            DemonCardView sourceCard)
        {
            while (sourceCard != null &&
                sourceCard.IsSatanAttackAnimationPlaying)
            {
                yield return null;
            }
        }

        private bool IsLastSatanAttackAnimationCue(
            GameSceneSatanAttackAnimationCue cue)
        {
            return _hasLastSatanAttackAnimationCue &&
                _lastSatanAttackAnimationRoundNumber == cue.RoundNumber &&
                _lastSatanAttackAnimationSourceCardId == cue.SourceCardId &&
                _lastSatanAttackAnimationActorSide == cue.ActorSide &&
                _lastSatanAttackAnimationActionOrdinal == cue.ActionOrdinal;
        }

        private void RememberSatanAttackAnimationCue(
            GameSceneSatanAttackAnimationCue cue)
        {
            _hasLastSatanAttackAnimationCue = true;
            _lastSatanAttackAnimationRoundNumber = cue.RoundNumber;
            _lastSatanAttackAnimationSourceCardId = cue.SourceCardId;
            _lastSatanAttackAnimationActorSide = cue.ActorSide;
            _lastSatanAttackAnimationActionOrdinal = cue.ActionOrdinal;
        }

        private void ResetSatanAttackAnimationState()
        {
            _hasLastSatanAttackAnimationCue = false;
            _lastSatanAttackAnimationRoundNumber = 0;
            _lastSatanAttackAnimationSourceCardId = 0;
            _lastSatanAttackAnimationActorSide = CombatantSide.Player;
            _lastSatanAttackAnimationActionOrdinal = 0;
        }

        private bool TryPlaySatanNumberGuessAnimation(
            GameSceneSatanNumberGuessAnimationCue cue,
            out CardView targetCard)
        {
            targetCard = null;
            if (cue == null || IsLastSatanNumberGuessAnimationCue(cue))
            {
                return false;
            }

            CardHand targetHand = cue.ActorSide == CombatantSide.Player
                ? enemyHand
                : playerHand;
            if (targetHand == null ||
                !targetHand.TryGetCard(cue.TargetCardId, out targetCard))
            {
                targetCard = null;
                return false;
            }

            if (!BeginSatanNumberGuessCameraSequence())
            {
                targetCard = null;
                return false;
            }

            RememberSatanNumberGuessAnimationCue(cue);
            return true;
        }

        private IEnumerator PlaySatanNumberGuessSequence(
            GameSceneSatanNumberGuessAnimationCue cue,
            CardView targetCard)
        {
            GameSceneCameraViewController cameraController =
                ResolveCameraViewController();
            while (cameraController != null && cameraController.IsTransitioning)
            {
                yield return null;
            }

            yield return new WaitForSeconds(
                Mathf.Max(satanNumberGuessCameraDelaySeconds, 0f));

            if (targetCard != null && targetCard.gameObject.activeInHierarchy)
            {
                targetCard.PlaySatanNumberGuessResult(
                    cue.Succeeded,
                    satanBrandSprite);
            }

            while (targetCard != null &&
                targetCard.IsSatanNumberGuessAnimationPlaying)
            {
                yield return null;
            }

            yield return new WaitForSeconds(
                Mathf.Max(satanNumberGuessAfterResultHoldSeconds, 0f));
            EndSatanNumberGuessCameraSequence();
        }

        private bool IsLastSatanNumberGuessAnimationCue(
            GameSceneSatanNumberGuessAnimationCue cue)
        {
            return _hasLastSatanNumberGuessAnimationCue &&
                _lastSatanNumberGuessAnimationRoundNumber == cue.RoundNumber &&
                _lastSatanNumberGuessAnimationSourceCardId == cue.SourceCardId &&
                _lastSatanNumberGuessAnimationActorSide == cue.ActorSide &&
                _lastSatanNumberGuessAnimationTargetCardId == cue.TargetCardId &&
                _lastSatanNumberGuessAnimationSucceeded == cue.Succeeded &&
                _lastSatanNumberGuessAnimationActionOrdinal == cue.ActionOrdinal;
        }

        private void RememberSatanNumberGuessAnimationCue(
            GameSceneSatanNumberGuessAnimationCue cue)
        {
            _hasLastSatanNumberGuessAnimationCue = true;
            _lastSatanNumberGuessAnimationRoundNumber = cue.RoundNumber;
            _lastSatanNumberGuessAnimationSourceCardId = cue.SourceCardId;
            _lastSatanNumberGuessAnimationActorSide = cue.ActorSide;
            _lastSatanNumberGuessAnimationTargetCardId = cue.TargetCardId;
            _lastSatanNumberGuessAnimationSucceeded = cue.Succeeded;
            _lastSatanNumberGuessAnimationActionOrdinal = cue.ActionOrdinal;
        }

        private void ResetSatanNumberGuessAnimationState()
        {
            _hasLastSatanNumberGuessAnimationCue = false;
            _lastSatanNumberGuessAnimationRoundNumber = 0;
            _lastSatanNumberGuessAnimationSourceCardId = 0;
            _lastSatanNumberGuessAnimationActorSide = CombatantSide.Player;
            _lastSatanNumberGuessAnimationTargetCardId = 0;
            _lastSatanNumberGuessAnimationSucceeded = false;
            _lastSatanNumberGuessAnimationActionOrdinal = 0;
            _satanNumberGuessCardIdToSuppress = -1;
            _satanNumberGuessSuppressedCardSide = CombatantSide.Player;
        }

        private bool BeginSatanNumberGuessCameraSequence()
        {
            if (_satanNumberGuessSwitchInputLocked)
            {
                return true;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null ||
                !controller.SetView(GameSceneCameraView.EnemyFocus))
            {
                return false;
            }

            controller.LockSwitchInput();
            _satanNumberGuessSwitchInputLocked = true;
            StopCardSelectionHeartSounds();
            StopSatanNumberSelectionHeartSlow();
            StartCardSelectionFastHeartAfterTransition(controller);
            PresentationManager.Current?.StartFieldOfViewIncrease(
                Mathf.Max(satanNumberGuessFovRiseSpeed, 0.01f));
            PresentationManager.Current?.StartChromaticAberration(
                Mathf.Max(satanNumberGuessChromaticRiseSpeed, 0.01f));
            return true;
        }

        private void EndSatanNumberGuessCameraSequence()
        {
            StopCardSelectionHeartSounds();
            StopSatanNumberSelectionHeartSlow();

            if (!_satanNumberGuessSwitchInputLocked)
            {
                return;
            }

            PresentationManager.Current?.StopFieldOfViewIncrease(
                Mathf.Max(satanNumberGuessFovReturnSpeed, 0.01f));
            PresentationManager.Current?.StopChromaticAberration(
                Mathf.Max(satanNumberGuessChromaticReturnSpeed, 0.01f));

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller != null)
            {
                controller.SetView(GameSceneCameraView.Current);
                controller.UnlockSwitchInput();
            }

            _satanNumberGuessSwitchInputLocked = false;
        }

        private HammerAnimationController ResolveHammerAnimation()
        {
            if (hammerAnimation != null)
            {
                return hammerAnimation;
            }

            hammerAnimation =
                FindFirstObjectByType<HammerAnimationController>(
                    FindObjectsInactive.Include);
            if (hammerAnimation != null)
            {
                return hammerAnimation;
            }

            Animator[] animators = FindObjectsByType<Animator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                if (candidate != null && candidate.gameObject.name == "Hammer_Anim")
                {
                    hammerAnimation =
                        candidate.GetComponent<HammerAnimationController>() ??
                        candidate.gameObject.AddComponent<HammerAnimationController>();
                    return hammerAnimation;
                }
            }

            return null;
        }

        private void ApplyCinematicCamera(
            GameSceneCameraView view,
            float lockSeconds)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.SetView(view);
            controller.LockSwitchInputForSeconds(lockSeconds);
        }

        private void UpdateEnemyCardSelectionCamera(bool focusesEnemyCards)
        {
            if (focusesEnemyCards == _enemyCardSelectionSwitchInputLocked)
            {
                return;
            }

            if (!focusesEnemyCards)
            {
                EndEnemyCardSelectionCamera();
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null ||
                !controller.SetView(GameSceneCameraView.EnemyFocus))
            {
                return;
            }

            controller.LockSwitchInput();
            _enemyCardSelectionSwitchInputLocked = true;
            StartCardSelectionHeartSounds(controller);
        }

        private void EndEnemyCardSelectionCamera()
        {
            StopCardSelectionHeartSounds();

            if (!_enemyCardSelectionSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller != null)
            {
                controller.SetView(GameSceneCameraView.Current);
                controller.UnlockSwitchInput();
            }

            _enemyCardSelectionSwitchInputLocked = false;
        }

        private void StartCardSelectionHeartSounds(
            GameSceneCameraViewController controller)
        {
            StopCardSelectionHeartSounds();
            SoundManager soundManager = SoundManager.Current;
            if (soundManager != null)
            {
                _cardSelectionHeartSlowHandle =
                    soundManager.PlaySfx(CardSelectionHeartSlowSfxId);
            }

            StartCardSelectionFastHeartAfterTransition(controller);
        }

        private void StartCardSelectionFastHeartAfterTransition(
            GameSceneCameraViewController controller)
        {
            StopCardSelectionHeartRoutine();

            if (!Application.isPlaying || controller == null)
            {
                SwitchToCardSelectionFastHeart();
                return;
            }

            _cardSelectionHeartRoutine = StartCoroutine(
                SwitchToCardSelectionFastHeartAfterTransition(controller));
        }

        private IEnumerator SwitchToCardSelectionFastHeartAfterTransition(
            GameSceneCameraViewController controller)
        {
            yield return null;

            while (controller != null && controller.IsTransitioning)
            {
                yield return null;
            }

            SwitchToCardSelectionFastHeart();
            _cardSelectionHeartRoutine = null;
        }

        private void SwitchToCardSelectionFastHeart()
        {
            FadeOutCardSelectionHeart(ref _cardSelectionHeartSlowHandle);

            SoundManager soundManager = SoundManager.Current;
            if (soundManager != null)
            {
                _cardSelectionHeartFastHandle =
                    soundManager.PlaySfx(CardSelectionHeartFastSfxId);
            }
        }

        private void StopCardSelectionHeartSounds()
        {
            StopCardSelectionHeartRoutine();
            FadeOutCardSelectionHeart(ref _cardSelectionHeartSlowHandle);
            FadeOutCardSelectionHeart(ref _cardSelectionHeartFastHandle);
            StopSatanNumberSelectionHeartSlow();
        }

        private void FadeOutCardSelectionHeart(
            ref SoundManager.SoundHandle handle)
        {
            SoundManager soundManager = SoundManager.Current;
            if (soundManager != null && handle.IsValid)
            {
                soundManager.StopSfx(
                    handle,
                    Mathf.Max(cardSelectionHeartFadeOutSeconds, 0f));
            }

            handle = default;
        }

        private void StopCardSelectionHeartRoutine()
        {
            if (_cardSelectionHeartRoutine == null)
            {
                return;
            }

            StopCoroutine(_cardSelectionHeartRoutine);
            _cardSelectionHeartRoutine = null;
        }

        private void StartSatanNumberSelectionHeartSlow()
        {
            SoundManager soundManager = SoundManager.Current;
            if (soundManager == null)
            {
                return;
            }

            if (_satanNumberSelectionHeartSlowActive &&
                soundManager.IsSfxPlaying(_satanNumberSelectionHeartSlowHandle))
            {
                return;
            }

            _satanNumberSelectionHeartSlowHandle =
                soundManager.PlaySfx(CardSelectionHeartSlowSfxId);
            _satanNumberSelectionHeartSlowActive =
                _satanNumberSelectionHeartSlowHandle.IsValid;
        }

        private void StopSatanNumberSelectionHeartSlow()
        {
            FadeOutCardSelectionHeart(
                ref _satanNumberSelectionHeartSlowHandle);
            _satanNumberSelectionHeartSlowHandle = default;
            _satanNumberSelectionHeartSlowActive = false;
        }

        private void ApplyCinematicCamera(
            GameSceneCameraView view,
            HammerAnimationController hammerController,
            bool returnToCurrentWhenFinished)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.SetView(view);
            _returnCameraToCurrentAfterHammer = returnToCurrentWhenFinished;
            BeginHammerSwitchInputLock(hammerController);
        }

        private void BeginHammerSwitchInputLock(
            HammerAnimationController hammerController)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            if (!_hammerSwitchInputLocked)
            {
                controller.LockSwitchInput();
                _hammerSwitchInputLocked = true;
            }

            if (_hammerCameraLockController == hammerController)
            {
                return;
            }

            if (_hammerCameraLockController != null)
            {
                _hammerCameraLockController.SmashAnimationFinished -=
                    HandleHammerSmashAnimationFinished;
            }

            _hammerCameraLockController = hammerController;
            if (_hammerCameraLockController != null)
            {
                _hammerCameraLockController.SmashAnimationFinished +=
                    HandleHammerSmashAnimationFinished;
            }
        }

        private void HandleHammerSmashAnimationFinished()
        {
            if (_returnCameraToCurrentAfterHammer)
            {
                GameSceneCameraViewController controller =
                    ResolveCameraViewController();
                controller?.SetView(GameSceneCameraView.Current);
            }

            _returnCameraToCurrentAfterHammer = false;
            EndHammerSwitchInputLock();
        }

        private void EndHammerSwitchInputLock()
        {
            if (_hammerCameraLockController != null)
            {
                _hammerCameraLockController.SmashAnimationFinished -=
                    HandleHammerSmashAnimationFinished;
                _hammerCameraLockController = null;
            }

            _returnCameraToCurrentAfterHammer = false;

            if (!_hammerSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller != null)
            {
                controller.UnlockSwitchInput();
            }

            _hammerSwitchInputLocked = false;
        }

        private GameSceneCameraViewController ResolveCameraViewController()
        {
            if (cameraViewController != null)
            {
                return cameraViewController;
            }

            cameraViewController =
                FindFirstObjectByType<GameSceneCameraViewController>(
                    FindObjectsInactive.Include);
            return cameraViewController;
        }

        private AppliedAnimationResult CreateAppliedAnimationResult(
            bool playedRevolver,
            bool playedKnife,
            bool playedHammer,
            bool playedPoisonInjection = false,
            bool deferredCardRender = false,
            GameSceneViewModel deferredViewModel = null,
            bool playedMammonRoll = false,
            bool playedCardReveal = false)
        {
            float waitSeconds = 0f;
            if (playedRevolver)
            {
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    RevolverResolvedSequenceSeconds);
            }

            if (playedKnife)
            {
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    _lastKnifeAnimationPhase == GameSceneKnifeAnimationPhase.Ready
                        ? knifeReadySeconds
                        : knifeResultSeconds);
            }

            if (playedHammer)
            {
                HammerAnimationController controller = ResolveHammerAnimation();
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    controller != null ? controller.AnimationSeconds : 0f);
            }

            if (playedMammonRoll && mammonDie != null)
            {
                waitSeconds = Mathf.Max(waitSeconds, mammonDie.RollDuration);
            }

            if (playedPoisonInjection && poisonInjectionAnnounce != null)
            {
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    poisonInjectionAnnounce.TotalDurationSeconds);
            }

            // A card that just revealed face-up (e.g. an enemy's automatic-effect
            // card, drawn face-down then flipped) is its own beat, separate from the
            // beat where its effect (poison injection, etc.) actually triggers. Without
            // this, that separate beat only ever waited the generic stepSeconds before
            // PlayTimeline advanced — if the flip takes longer than that, the next
            // beat's effect visual could start while the reveal was still mid-animation.
            if (playedCardReveal)
            {
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    ResolveCardRevealFaceSwapSeconds());
            }

            return new AppliedAnimationResult(
                playedRevolver,
                playedKnife,
                playedHammer,
                playedPoisonInjection,
                waitSeconds,
                playedHammer ? _playedHammerAnimationController : null,
                deferredCardRender,
                deferredViewModel,
                playedMammonRoll,
                playedCardReveal);
        }

        private void RenderDemonContractSelection(
            GameSceneCombatHudViewModel combat)
        {
            if (demonContractSelection == null)
            {
                return;
            }

            if (combat == null ||
                combat.Mode != GameSceneCombatHudMode.ContractCandidates)
            {
                demonContractSelection.Hide();
                hud?.HideDemonContractDetail();
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            demonContractSelection.Render(
                combat.ContractCandidates,
                _camera);
            hud?.HideDemonContractDetail();
        }

        private static bool IsHammerSmashCue(GameSceneHammerAnimationCue cue)
        {
            return cue != null &&
                cue.Phase == GameSceneHammerAnimationPhase.Smash;
        }

        private static bool IsRevolverResolvedCue(
            GameSceneRevolverAnimationCue cue)
        {
            return cue != null &&
                cue.Phase != GameSceneRevolverAnimationPhase.Ready;
        }

        private IEnumerator WaitForAnimationOrSeconds(
            AppliedAnimationResult animation,
            float waitSeconds)
        {
            bool waitsForHammer =
                animation.PlayedHammer &&
                animation.HammerController != null;
            bool waitsForPoisonInjection =
                animation.PlayedPoisonInjection &&
                poisonInjectionAnnounce != null;
            bool waitsForMammonRoll =
                animation.PlayedMammonRoll &&
                mammonDie != null;
            if (!waitsForHammer && !waitsForPoisonInjection && !waitsForMammonRoll)
            {
                yield return new WaitForSeconds(waitSeconds);
                yield break;
            }

            float elapsedSeconds = 0f;
            while (elapsedSeconds < waitSeconds ||
                (waitsForHammer &&
                    animation.HammerController.IsSmashAnimationPlaying) ||
                (waitsForPoisonInjection &&
                    poisonInjectionAnnounce.IsPlaying) ||
                (waitsForMammonRoll && mammonDie.IsRolling))
            {
                elapsedSeconds += Time.deltaTime;
                yield return null;
            }
        }

        private bool IsLastRevolverAnimationCue(
            GameSceneRevolverAnimationCue cue)
        {
            return _hasLastRevolverAnimationCue &&
                _lastRevolverAnimationRoundNumber == cue.RoundNumber &&
                _lastRevolverAnimationSourceCardId == cue.SourceCardId &&
                _lastRevolverAnimationActorSide == cue.ActorSide &&
                _lastRevolverAnimationPhase == cue.Phase &&
                _lastRevolverAnimationSucceeded == cue.Succeeded;
        }

        private void RememberRevolverAnimationCue(
            GameSceneRevolverAnimationCue cue)
        {
            _hasLastRevolverAnimationCue = true;
            _lastRevolverAnimationRoundNumber = cue.RoundNumber;
            _lastRevolverAnimationSourceCardId = cue.SourceCardId;
            _lastRevolverAnimationActorSide = cue.ActorSide;
            _lastRevolverAnimationPhase = cue.Phase;
            _lastRevolverAnimationSucceeded = cue.Succeeded;
        }

        private void RememberActiveRevolverReady(
            GameSceneRevolverAnimationCue cue)
        {
            _revolverReadyActive = true;
            _revolverReadyRoundNumber = cue.RoundNumber;
            _revolverReadySourceCardId = cue.SourceCardId;
            _revolverReadyActorSide = cue.ActorSide;
        }

        internal static bool ShouldHoldInputForRevolverReady(
            bool readyActive,
            bool selectionReady,
            CombatantSide actorSide)
        {
            return readyActive &&
                !selectionReady &&
                actorSide == CombatantSide.Player;
        }

        internal static bool ShouldQueuePlayerRevolverReadySnapshot(
            int timelineCount,
            GameSceneRevolverAnimationCue cue)
        {
            return timelineCount == 0 &&
                cue != null &&
                cue.Phase == GameSceneRevolverAnimationPhase.Ready &&
                cue.ActorSide == CombatantSide.Player;
        }

        private bool IsMatchingActiveRevolverReady(
            GameSceneRevolverAnimationCue cue)
        {
            return _revolverReadyActive &&
                _revolverReadyRoundNumber == cue.RoundNumber &&
                _revolverReadySourceCardId == cue.SourceCardId &&
                _revolverReadyActorSide == cue.ActorSide;
        }

        private string ResolveRevolverTrigger(GameSceneRevolverAnimationCue cue)
        {
            if (cue.ActorSide == CombatantSide.Player)
            {
                return cue.Succeeded ? playerSuccessTrigger : playerFailTrigger;
            }

            return cue.Succeeded ? enemySuccessTrigger : enemyFailTrigger;
        }

        private void ResetRevolverAnimationState()
        {
            _hasLastRevolverAnimationCue = false;
            _lastRevolverAnimationRoundNumber = 0;
            _lastRevolverAnimationSourceCardId = 0;
            _lastRevolverAnimationActorSide = CombatantSide.Player;
            _lastRevolverAnimationPhase = GameSceneRevolverAnimationPhase.Ready;
            _lastRevolverAnimationSucceeded = false;
            ClearPendingRevolverImpact();
            ClearActiveRevolverReady();
            HideRevolverAnimation();
        }

        private IEnumerator HideRevolverAnimationAfterDelay()
        {
            yield return new WaitForSeconds(RevolverResolvedSequenceSeconds);
            _revolverHideRoutine = null;
            ResetRevolverAnimatorToBase();

            GameObject root = ResolveRevolverRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearActiveRevolverReady();
            ClearPendingRevolverImpact();
            EndRevolverSwitchInputLock();
        }

        private IEnumerator PrepareRevolverRetryAfterDelay(
            GameSceneRevolverAnimationCue cue)
        {
            yield return new WaitForSeconds(RevolverResolvedSequenceSeconds);
            _revolverHideRoutine = null;
            PrepareRevolverRetry(cue);
        }

        private void PrepareRevolverRetry(GameSceneRevolverAnimationCue cue)
        {
            ClearPendingRevolverImpact();
            GameObject root = ResolveRevolverRoot();
            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            if (revolverAnimator == null ||
                !revolverAnimator.gameObject.activeInHierarchy)
            {
                ClearActiveRevolverReady();
                return;
            }

            ResetRevolverAnimatorToBase();
            ResetRevolverTriggers();
            if (cue.ActorSide == CombatantSide.Player)
            {
                revolverAnimator.SetTrigger(playerReadyTrigger);
            }

            RememberActiveRevolverReady(cue);
            BeginRevolverReadyCameraSequence(cue.ActorSide);
        }

        private void HideRevolverAnimation()
        {
            StopRevolverHideRoutine();
            StopRevolverReadyCameraRoutine();
            StopRevolverShotRoutine();
            ClearPendingRevolverImpact();
            ResetRevolverAnimatorToBase();

            GameObject root = ResolveRevolverRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearActiveRevolverReady();
            EndRevolverSwitchInputLock();
        }

        private float RevolverResolvedSequenceSeconds =>
            Mathf.Max(0f, revolverCameraReturnSeconds) +
            Mathf.Max(0f, revolverAnimationSeconds);

        private void BeginRevolverReadyCameraSequence(CombatantSide actorSide)
        {
            StopRevolverReadyCameraRoutine();
            _revolverSelectionReady = false;
            BeginRevolverSwitchInputLock();

            // Only the player's own number selection needs the close-up table view; the
            // camera stays on whatever the viewer already had while the enemy decides.
            if (actorSide != CombatantSide.Player)
            {
                return;
            }

            if (Application.isPlaying && revolverReadySeconds > 0f)
            {
                _revolverReadyCameraRoutine = StartCoroutine(
                    MoveRevolverSelectionCameraAfterReady());
                return;
            }

            MoveRevolverSelectionCameraToTableTop();
        }

        private IEnumerator MoveRevolverSelectionCameraAfterReady()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, revolverReadySeconds));
            _revolverReadyCameraRoutine = null;
            MoveRevolverSelectionCameraToTableTop();
        }

        private void MoveRevolverSelectionCameraToTableTop()
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            bool transitionRequested = controller != null &&
                controller.SetView(GameSceneCameraView.TableTop);
            if (!Application.isPlaying || !transitionRequested)
            {
                _revolverSelectionReady = true;
                return;
            }

            _revolverReadyCameraRoutine = StartCoroutine(
                CompleteRevolverSelectionAfterCameraTransition(controller));
        }

        private IEnumerator CompleteRevolverSelectionAfterCameraTransition(
            GameSceneCameraViewController controller)
        {
            // Cinemachine starts the blend during its next update after priorities change.
            yield return null;
            while (controller != null && controller.IsTransitioning)
            {
                yield return null;
            }

            _revolverReadyCameraRoutine = null;
            _revolverSelectionReady = true;
        }

        private IEnumerator PlayRevolverShotAfterCameraReturn(
            GameSceneRevolverAnimationCue cue)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0f, revolverCameraReturnSeconds));
            _revolverShotRoutine = null;
            PlayRevolverShot(cue);
        }

        private void PlayRevolverShot(GameSceneRevolverAnimationCue cue)
        {
            if (cue == null || revolverAnimator == null ||
                !revolverAnimator.gameObject.activeInHierarchy)
            {
                return;
            }

            revolverAnimator.SetTrigger(ResolveRevolverTrigger(cue));
        }

        internal static CharacterVisualState ResolveRevolverTimedVisual(
            CombatantSide side,
            CharacterVisualState visual,
            bool impactPending,
            CombatantSide impactTargetSide)
        {
            return impactPending &&
                side == impactTargetSide &&
                visual == CharacterVisualState.Attacked
                    ? CharacterVisualState.AttackThreatened
                    : visual;
        }

        private CharacterVisualState ResolveRevolverTimedVisual(
            CombatantSide side,
            CharacterVisualState visual)
        {
            return ResolveRevolverTimedVisual(
                side,
                visual,
                _revolverImpactPending,
                _revolverImpactTargetSide);
        }

        internal static CharacterVisualState ResolveKnifeTimedVisual(
            CombatantSide side,
            CharacterVisualState visual,
            bool impactPending,
            CombatantSide impactTargetSide)
        {
            return impactPending &&
                side == impactTargetSide &&
                visual == CharacterVisualState.Attacked
                    ? CharacterVisualState.AttackThreatened
                    : visual;
        }

        private CharacterVisualState ResolveKnifeTimedVisual(
            CombatantSide side,
            CharacterVisualState visual)
        {
            return ResolveKnifeTimedVisual(
                side,
                visual,
                _knifeImpactPending,
                _knifeImpactTargetSide);
        }

        private void BindRevolverImpactEvent()
        {
            RevolverAnimationEventReceiver receiver = ResolveRevolverEventReceiver();
            if (receiver == null)
            {
                return;
            }

            receiver.ShotImpact -= HandleRevolverShotImpact;
            receiver.ShotImpact += HandleRevolverShotImpact;
        }

        private void UnbindRevolverImpactEvent()
        {
            if (_revolverEventReceiver == null)
            {
                return;
            }

            _revolverEventReceiver.ShotImpact -= HandleRevolverShotImpact;
        }

        private RevolverAnimationEventReceiver ResolveRevolverEventReceiver()
        {
            if (_revolverEventReceiver != null)
            {
                return _revolverEventReceiver;
            }

            if (revolverAnimator == null)
            {
                return null;
            }

            _revolverEventReceiver =
                revolverAnimator.GetComponent<RevolverAnimationEventReceiver>() ??
                revolverAnimator.GetComponentInParent<RevolverAnimationEventReceiver>(true) ??
                revolverAnimator.GetComponentInChildren<RevolverAnimationEventReceiver>(true);
            return _revolverEventReceiver;
        }

        private void HandleRevolverShotImpact()
        {
            if (!_revolverImpactPending)
            {
                return;
            }

            if (_revolverImpactTargetSide == CombatantSide.Enemy)
            {
                enemyCharacter?.Render(CharacterVisualState.Attacked, "HIT!");
            }

            ClearPendingRevolverImpact();
        }

        private void ClearPendingRevolverImpact()
        {
            _revolverImpactPending = false;
            _revolverImpactTargetSide = CombatantSide.Player;
        }

        private void BindKnifeImpactEvent()
        {
            KnifeAnimationEventReceiver receiver = ResolveKnifeEventReceiver();
            if (receiver == null)
            {
                return;
            }

            receiver.KnifeImpact -= HandleKnifeImpact;
            receiver.KnifeImpact += HandleKnifeImpact;
        }

        private void UnbindKnifeImpactEvent()
        {
            if (_knifeEventReceiver != null)
            {
                _knifeEventReceiver.KnifeImpact -= HandleKnifeImpact;
            }
        }

        private KnifeAnimationEventReceiver ResolveKnifeEventReceiver()
        {
            if (_knifeEventReceiver != null)
            {
                return _knifeEventReceiver;
            }

            Animator animator = ResolveKnifeAnimator();
            if (animator == null)
            {
                return null;
            }

            _knifeEventReceiver =
                animator.GetComponent<KnifeAnimationEventReceiver>() ??
                animator.GetComponentInParent<KnifeAnimationEventReceiver>(true) ??
                animator.GetComponentInChildren<KnifeAnimationEventReceiver>(true);
            return _knifeEventReceiver;
        }

        private void HandleKnifeImpact()
        {
            if (!_knifeImpactPending)
            {
                return;
            }

            if (_knifeImpactTargetSide == CombatantSide.Enemy)
            {
                enemyCharacter?.Render(CharacterVisualState.Attacked, "HIT!");
            }

            ClearPendingKnifeImpact();
        }

        private void ClearPendingKnifeImpact()
        {
            _knifeImpactPending = false;
            _knifeImpactTargetSide = CombatantSide.Player;
        }

        private static CombatantSide Opposite(CombatantSide side)
        {
            return side == CombatantSide.Player
                ? CombatantSide.Enemy
                : CombatantSide.Player;
        }

        private void StopRevolverHideRoutine()
        {
            if (_revolverHideRoutine == null)
            {
                return;
            }

            StopCoroutine(_revolverHideRoutine);
            _revolverHideRoutine = null;
        }

        private void StopRevolverReadyCameraRoutine()
        {
            if (_revolverReadyCameraRoutine == null)
            {
                return;
            }

            StopCoroutine(_revolverReadyCameraRoutine);
            _revolverReadyCameraRoutine = null;
        }

        private void StopRevolverShotRoutine()
        {
            if (_revolverShotRoutine == null)
            {
                return;
            }

            StopCoroutine(_revolverShotRoutine);
            _revolverShotRoutine = null;
        }

        private void BeginRevolverSwitchInputLock()
        {
            if (_revolverSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.LockSwitchInput();
            _revolverSwitchInputLocked = true;
        }

        private void EndRevolverSwitchInputLock()
        {
            if (!_revolverSwitchInputLocked)
            {
                return;
            }

            ResolveCameraViewController()?.UnlockSwitchInput();
            _revolverSwitchInputLocked = false;
        }

        private void ResetRevolverAnimatorToBase()
        {
            if (revolverAnimator == null ||
                string.IsNullOrWhiteSpace(revolverBaseStateName) ||
                !revolverAnimator.gameObject.activeInHierarchy)
            {
                return;
            }

            revolverAnimator.Play(revolverBaseStateName, 0, 0f);
            revolverAnimator.Update(0f);
        }

        private void ResetRevolverTriggers()
        {
            ResetRevolverTrigger(playerReadyTrigger);
            ResetRevolverTrigger(playerSuccessTrigger);
            ResetRevolverTrigger(playerFailTrigger);
            ResetRevolverTrigger(enemySuccessTrigger);
            ResetRevolverTrigger(enemyFailTrigger);
        }

        private void ResetRevolverTrigger(string triggerName)
        {
            if (revolverAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                revolverAnimator.ResetTrigger(triggerName);
            }
        }

        private GameObject ResolveRevolverRoot()
        {
            if (revolverRoot != null)
            {
                return revolverRoot;
            }

            return revolverAnimator != null ? revolverAnimator.gameObject : null;
        }

        private void ClearActiveRevolverReady()
        {
            _revolverReadyActive = false;
            _revolverSelectionReady = false;
            _revolverReadyRoundNumber = 0;
            _revolverReadySourceCardId = 0;
            _revolverReadyActorSide = CombatantSide.Player;
        }

        private readonly struct AppliedAnimationResult
        {
            public AppliedAnimationResult(
                bool playedRevolver,
                bool playedKnife,
                bool playedHammer,
                bool playedPoisonInjection,
                float waitSeconds,
                HammerAnimationController hammerController,
                bool deferredCardRender,
                GameSceneViewModel deferredViewModel,
                bool playedMammonRoll = false,
                bool playedCardReveal = false)
            {
                PlayedRevolver = playedRevolver;
                PlayedKnife = playedKnife;
                PlayedHammer = playedHammer;
                PlayedPoisonInjection = playedPoisonInjection;
                WaitSeconds = waitSeconds;
                HammerController = hammerController;
                DeferredCardRender = deferredCardRender;
                DeferredViewModel = deferredViewModel;
                PlayedMammonRoll = playedMammonRoll;
                PlayedCardReveal = playedCardReveal;
            }

            public bool PlayedRevolver { get; }

            public bool PlayedKnife { get; }

            public bool PlayedHammer { get; }

            public bool PlayedPoisonInjection { get; }

            public bool PlayedMammonRoll { get; }

            public bool PlayedCardReveal { get; }

            public bool PlayedAny =>
                PlayedRevolver || PlayedKnife || PlayedHammer ||
                PlayedPoisonInjection || PlayedMammonRoll || PlayedCardReveal;

            public float WaitSeconds { get; }

            public HammerAnimationController HammerController { get; }

            public bool DeferredCardRender { get; }

            public GameSceneViewModel DeferredViewModel { get; }
        }

        // Open the shop the moment a battle is won. Called from RefreshView, which lands on the true
        // post-turn state (BattleEnded is not itself a Stepped beat). ShopController.Open guards against
        // repeat opens, so this fires the shop exactly once per victory; a defeat opens no shop.
        private void MaybeOpenShop(GameSceneViewModel vm)
        {
            if (IsStageBattle || shop == null || shop.IsOpen ||
                vm.Core.State != CoreLoopState.BattleEnded ||
                vm.Core.Outcome != BattleOutcome.PlayerVictory ||
                IsTerminalSpeechHoldBlocking(
                    _terminalSpeechHoldActive,
                    _terminalSpeechHoldCompleted))
            {
                return;
            }

            OpenStandaloneShop();
        }

        internal bool DebugOpenStandaloneShop()
        {
            if (shop == null || shop.IsOpen)
            {
                return false;
            }

            OpenStandaloneShop();
            RefreshView();
            return shop.IsOpen;
        }

        internal bool DebugCloseStandaloneShop()
        {
            if (shop == null || !shop.IsOpen)
            {
                return false;
            }

            bool leftVictoryShop = _session != null && LeaveShop();
            if (!leftVictoryShop)
            {
                CloseStandaloneShop();
            }

            RefreshView();
            return !shop.IsOpen;
        }

        internal void RefreshForDebug()
        {
            RefreshView();
        }

        private void OpenStandaloneShop()
        {
            CloseDeckPreview();
            shop.Open(CurrentEnemyProfileKey);
            SetBattleCardObjectsVisible(false);
        }

        private void CloseStandaloneShop()
        {
            CloseDeckPreview();
            _choosingLighterRemoval = false;
            UpdateHover(null);
            UpdateDemonCardHover(null);
            UpdateShopUtilityItemHover(null);
            shop.Close();
            SetBattleCardObjectsVisible(true);
        }

        // Leave the shop and start the next battle. Gold is KEPT by ShopController — it accumulates
        // across the run's battles; only a defeat restart resets it. TryRestart swaps in a fresh battle
        // and emits no Stepped events, so ProcessInput re-presents immediately via RefreshView.
        private bool LeaveShop()
        {
            CloseDeckPreview();
            if (IsStageBattle)
            {
                return false;
            }

            bool restarted = _session.TryRestart();
            if (restarted && shop != null)
            {
                CloseStandaloneShop();
            }

            return restarted;
        }

        // Restart after a defeat: a fresh run, so the shop closes (a no-op if it was never open) and
        // gold returns to 0.
        private bool RestartRun()
        {
            CloseDeckPreview();
            if (IsStageBattle)
            {
                return false;
            }

            bool restarted = _session.TryRestart();
            if (restarted && shop != null)
            {
                _purchasedNormalCards.Clear();
                _purchasedDemonContractKeys.Clear();
                _removedNormalCards.Clear();
                _choosingLighterRemoval = false;
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateShopUtilityItemHover(null);
                shop.Close();
                SetBattleCardObjectsVisible(true);
                shop.ResetRunEconomy();
                ResetEnemySpeech();
            }

            return restarted;
        }

        private bool IsStageBattle => _stageSession != null;

        private void ReturnToProgressionIfStageBattleEnded()
        {
            if (!IsStageBattle ||
                // The tutorial has no progression scene of its own — TutorialDirector's
                // own script (including its post-battle "축하해..." finale) keeps running
                // regardless of stage/battle state, and HandleTutorialDirectorCompleted
                // returns to the main menu once that script actually finishes.
                _stageSession.IsTutorialRun ||
                _stageSession.Progress.State == StageProgressionState.InBattle ||
                IsTerminalSpeechHoldBlocking(
                    _terminalSpeechHoldActive,
                    _terminalSpeechHoldCompleted))
            {
                return;
            }

            if (ReferenceEquals(_completedStageSession, _stageSession))
            {
                return;
            }

            _completedStageSession = _stageSession;
            _inputLocked = true;
            if (FormalBattleCompleted != null)
            {
                FormalBattleCompleted.Invoke();
                return;
            }

            _stageRuntime?.LoadProgressionScene();
        }

        private int CountFormalRemovableCards()
        {
            if (_formalShopModel == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ShopOwnedCardViewModel card in
                     _formalShopModel.ShopOwnedCards)
            {
                if (card.CanRemove)
                {
                    count++;
                }
            }

            return count;
        }

        private void UnlockInput()
        {
            _inputLocked = false;
            UpdateShopLeaveControl();
        }

        private readonly struct PurchasedNormalCard
        {
            public PurchasedNormalCard(string definitionKey, CardSuit suit)
            {
                DefinitionKey = definitionKey ?? string.Empty;
                Suit = suit;
            }

            public string DefinitionKey { get; }

            public CardSuit Suit { get; }

            public bool Matches(string definitionKey, CardSuit suit)
            {
                return StringComparer.Ordinal.Equals(DefinitionKey, definitionKey) &&
                    Suit == suit;
            }
        }

        private readonly struct RemovedNormalCard
        {
            public RemovedNormalCard(string definitionKey, CardSuit suit)
            {
                DefinitionKey = definitionKey ?? string.Empty;
                Suit = suit;
            }

            public string DefinitionKey { get; }

            public CardSuit Suit { get; }

            public bool Matches(string definitionKey, CardSuit suit)
            {
                return StringComparer.Ordinal.Equals(DefinitionKey, definitionKey) &&
                    Suit == suit;
            }
        }

        private readonly struct RunDeckCardOption
        {
            public RunDeckCardOption(
                string definitionKey,
                CardSuit suit,
                bool isPurchased,
                int purchasedIndex)
            {
                DefinitionKey = definitionKey ?? string.Empty;
                Suit = suit;
                IsPurchased = isPurchased;
                PurchasedIndex = purchasedIndex;
            }

            public string DefinitionKey { get; }

            public CardSuit Suit { get; }

            public bool IsPurchased { get; }

            public int PurchasedIndex { get; }
        }

    }
}
