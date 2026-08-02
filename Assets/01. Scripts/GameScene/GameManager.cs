using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private DeckPreviewView deckPreview;
        [SerializeField] private CodexController codex;
        [SerializeField] private DemonContractSelectionView demonContractSelection;
        private CrystalOrbSelectionView crystalOrbSelection;
        private SatanNumberSelectionView satanNumberSelection;
        [SerializeField] private TableCombatCommandGroup tableCombatCommands;
        [SerializeField] private ContractPaperView contractPapers;

        [Header("Standalone enemy profile")]
        [SerializeField] private string enemyProfileKey =
            EnemyCombatProfileCatalog.GunslingerKey;

        [Header("Shop (MVP)")]
        [SerializeField] private ShopController shop;

        [Tooltip("Font for the remaining shop/lighter IMGUI panels. Leave empty to use Unity's default.")]
        [SerializeField] private Font uiFont;

        [Header("Presentation pacing")]
        [SerializeField] private float stepSeconds = 1.0f;
        [SerializeField] private float resolveHoldSeconds = 2.5f;

        internal const float MinimumRoundResultHoldSeconds = 2.5f;

        [Header("Revolver animation")]
        [SerializeField] private Animator revolverAnimator;
        [SerializeField] private GameObject revolverRoot;
        [SerializeField] private float revolverAnimationSeconds = 8.8f;
        [SerializeField] private string revolverBaseStateName = "Revolver_Base";
        [SerializeField] private string playerReadyTrigger = "PlayerTurnStart";
        [SerializeField] private string playerSuccessTrigger = "PlayerSuccess";
        [SerializeField] private string playerFailTrigger = "PlayerFail";
        [SerializeField] private string enemySuccessTrigger = "EnemySuccess";
        [SerializeField] private string enemyFailTrigger = "EnemyFail";

        [Header("Hammer animation")]
        [SerializeField] private HammerAnimationController hammerAnimation;

        [Header("Cinematic camera")]
        [SerializeField] private GameSceneCameraViewController cameraViewController;

        private CoreLoopSession _session;
        private StageProgressionSession _stageSession;
        private StageProgressionRuntime _stageRuntime;
        private CoreLoopViewModel _core;
        private Camera _camera;
        private CardView _hoveredCard;
        private DemonCardView _hoveredDemonCard;
        private ShopUtilityItemView _hoveredShopUtilityItem;
        private TableCombatCommandView _hoveredCombatCommand;
        private bool _inputLocked;
        private bool _pauseInputBlocked;
        private bool _choosingLighterRemoval;
        private int _battleIndex;
        private string _activeEnemyProfileKey;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _shopPanelStyle;
        private GUIStyle _shopCardButtonStyle;
        private Vector2 _lighterRemovalScroll;
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
        private RevolverAnimationEventReceiver _revolverEventReceiver;
        private bool _revolverImpactPending;
        private CombatantSide _revolverImpactTargetSide;
        private bool _hammerSwitchInputLocked;
        private bool _enemyCardSelectionSwitchInputLocked;
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

        public event Action FormalBattleCompleted;
        public event Action<int> FormalShopCardPurchaseRequested;
        public event Action<int> FormalShopCardRemovalRequested;
        public event Action FormalShopRestRequested;
        public event Action FormalShopLeaveRequested;

        public CoreLoopBattle Battle => IsStageBattle
            ? _stageSession.Battle
            : _session?.Battle;

        private bool IsModalInputBlocked =>
            _pauseInputBlocked || (codex != null && codex.IsOpen);

        public bool BindBattle(StageProgressionSession session)
        {
            if (session == null ||
                session.Progress.State != StageProgressionState.InBattle ||
                session.Battle == null)
            {
                return false;
            }

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
            enemyCharacter?.ExitMerchant();
            enemyCharacter?.TrySetEnemyProfile(_activeEnemyProfileKey);
            _inputLocked = false;
            RefreshView();
            return true;
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
            int currentGold)
        {
            if (model == null || !model.IsShop || shop == null)
            {
                return false;
            }

            if (_stageSession != null || _session != null)
            {
                UnbindBattle();
            }

            _formalShopModel = model;
            _inputLocked = false;
            _choosingLighterRemoval = false;
            shop.OpenFormal(model);
            hud?.SetGold(currentGold);
            hud?.SetEnemyStatusVisible(false);
            return true;
        }

        public void UnbindFormalShop()
        {
            if (_formalShopModel == null && (shop == null || !shop.IsFormal))
            {
                return;
            }

            _formalShopModel = null;
            _choosingLighterRemoval = false;
            UpdateHover(null);
            UpdateDemonCardHover(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            shop?.CloseFormal();
        }

        public void SetPauseInputBlocked(bool blocked)
        {
            _pauseInputBlocked = blocked;
            if (!blocked)
            {
                return;
            }

            UpdateHover(null);
            UpdateDemonCardHover(null);
            demonContractSelection?.SetHovered(null);
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            hud?.HideDemonContractDetail();
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

            CloseDeckPreview();
            return true;
        }

        private void Awake()
        {
            HideRevolverAnimation();
            ResolveHammerAnimation()?.Hide();
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

            if (enemyCharacter != null)
            {
                enemyCharacter.TrySetEnemyProfile(_activeEnemyProfileKey);
            }
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
            satanNumberSelection.Initialize(playerHand?.CardPrefab);
            tableCombatCommands ??= FindFirstObjectByType<TableCombatCommandGroup>(
                FindObjectsInactive.Include);
            contractPapers ??= FindFirstObjectByType<ContractPaperView>(
                FindObjectsInactive.Include);

            if (hud != null)
            {
                hud.CombatCommandRequested += HandleCombatCommand;
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
            if (codex != null)
            {
                codex.OpenStateChanged += HandleCodexOpenStateChanged;
            }
        }

        private void OnDisable()
        {
            UnbindRevolverImpactEvent();
            ClearPendingRevolverImpact();
            CloseDeckPreview();
            CloseCodex();
            EndEnemyCardSelectionCamera();
            if (codex != null)
            {
                codex.OpenStateChanged -= HandleCodexOpenStateChanged;
            }
            demonContractSelection?.Hide();
            crystalOrbSelection?.Hide();
            satanNumberSelection?.Hide();
            hud?.HideDemonContractDetail();
            UpdateCombatCommandHover(null);
        }

        private void OnDestroy()
        {
            EndEnemyCardSelectionCamera();
            if (hud != null)
            {
                hud.CombatCommandRequested -= HandleCombatCommand;
            }
        }

        private void ResetBattlePresentation()
        {
            StopAllCoroutines();
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
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            demonContractSelection?.SetHovered(null);
            demonContractSelection?.Hide();
            crystalOrbSelection?.Hide();
            satanNumberSelection?.Hide();
            contractPapers?.Render(null);
            hud?.HideCardHoverBadge();
            hud?.HideDemonContractDetail();
            hud?.Render(null);
            tableCombatCommands?.ResetView();
            CloseDeckPreview();
            CloseCodex();
            EndEnemyCardSelectionCamera();
            EndHammerSwitchInputLock();
            ResolveHammerAnimation()?.Hide();
            ResetRevolverAnimationState();
            playerHand?.ResetView();
            enemyHand?.ResetView();
            remainingDeck?.ResetView();
            discardDeck?.ResetView();
            totals?.Render(string.Empty, string.Empty);
            enemyCharacter?.Render(
                CharacterVisualState.Idle,
                string.Empty);
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
        }

        // Diegetic input: hover any card to enlarge it (usable cards also show a HUD badge), click a
        // legal card-effect target to resolve that choice, or click a usable player card to activate
        // its effect. New Input System — legacy OnMouseDown does not fire, so we raycast the pointer
        // ourselves. Table commands and the contract share this same raycast path.
        private void Update()
        {
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
                UpdateCombatCommandHover(null);
                return;
            }

            bool shopOpen = shop != null && shop.IsOpen;
            if (_core == null && !shopOpen)
            {
                UpdateCombatCommandHover(null);
                return;
            }

            if (_inputLocked)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateShopUtilityItemHover(null);
                UpdateCombatCommandHover(null);
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
                UpdateShopUtilityItemHover(null);
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
                UpdateShopUtilityItemHover(null);
                UpdateCombatCommandHover(null);
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
            DemonCardView pointedDemonCard = shopOpen && hasHit
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

            if (deckPreview != null && deckPreview.IsOpen)
            {
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateShopUtilityItemHover(null);
                UpdateCombatCommandHover(null);
                hud?.HideCardHoverBadge();

                return;
            }

            UpdateHover(shopOpen ? pointedShopCard : pointedBattleCard);
            UpdateDemonCardHover(pointedDemonCard);
            UpdateCardHoverBadge();
            UpdateShopUtilityItemHover(pointedShopUtilityItem);
            UpdateCombatCommandHover(pointedCombatCommand);

            if (_inputLocked || _choosingLighterRemoval)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            CodexClickable pointedCodex = hasHit
                ? hit.collider.GetComponentInParent<CodexClickable>()
                : null;
            if (pointedCodex != null && codex != null && codex.IsAvailable)
            {
                CloseDeckPreview();
                codex.Open();
                return;
            }

            ContractPaperClickable pointedContractPaper = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<ContractPaperClickable>()
                : null;
            if (pointedContractPaper != null &&
                pointedContractPaper.IsInteractable)
            {
                CloseDeckPreview();
                ProcessInput(TryBeginPlayerDemonContract);
                return;
            }

            DeckClickable pointedDeck = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<DeckClickable>()
                : null;
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

            if (pointedBattleCard != null && pointedBattleCard.CanUse)
            {
                int cardId = pointedBattleCard.CardId;
                ProcessInput(() => TryBeginPlayerCardUse(cardId));
                return;
            }

            if (pointedShopCard != null && pointedShopCard.CanUse)
            {
                PurchaseShopNormalCard(pointedShopCard);
                return;
            }

            if (pointedDemonCard != null && pointedDemonCard.CanUse)
            {
                PurchaseShopDemonCard(pointedDemonCard);
                return;
            }

            if (pointedShopUtilityItem != null && pointedShopUtilityItem.CanUse)
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
            UpdateShopUtilityItemHover(null);
            UpdateCombatCommandHover(null);
            hud?.HideCardHoverBadge();
            demonContractSelection.SetHovered(pointed);

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
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
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

            UpdateHover(pointed);
            UpdateDemonCardHover(null);
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

            HandleCombatCommand(candidate.DirectSelectionCommand.Value);
        }

        private void UpdateHover(CardView pointed)
        {
            if (pointed == _hoveredCard)
            {
                return;
            }

            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(false);
            }

            _hoveredCard = pointed;
            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(true);
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

            if (_hoveredCombatCommand == null ||
                !_hoveredCombatCommand.IsInteractable ||
                string.IsNullOrEmpty(_hoveredCombatCommand.Tooltip))
            {
                hud?.HideCombatActionTooltip();
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                hud?.HideCombatActionTooltip();
                return;
            }

            Vector3 screenPosition = _camera.WorldToScreenPoint(
                _hoveredCombatCommand.TooltipWorldPosition);
            if (screenPosition.z <= 0f)
            {
                hud?.HideCombatActionTooltip();
                return;
            }

            hud?.ShowCombatActionTooltip(
                _hoveredCombatCommand.Tooltip,
                new Vector2(screenPosition.x, screenPosition.y),
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

            EnsureDeckPreview();
            if (deckPreview == null)
            {
                return;
            }

            UpdateHover(null);
            UpdateCombatCommandHover(null);
            deckPreview.Open(GameScenePresenter.CreateDeckPreview(battle, kind));
            BeginDeckPreviewSwitchInputLock();
        }

        private void CloseDeckPreview()
        {
            if (deckPreview != null && deckPreview.IsOpen)
            {
                deckPreview.Close();
                UpdateHover(null);
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

            EndCodexSwitchInputLock();
        }

        private void HandleCodexOpenStateChanged(bool isOpen)
        {
            UpdateHover(null);
            UpdateDemonCardHover(null);
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
            ResetRevolverAnimationState();
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
                FormalShopCardPurchaseRequested?.Invoke(card.CardId);
                UpdateDemonCardHover(null);
                return;
            }

            if (shop == null || card == null ||
                !shop.TryPurchaseDemonCard(card.CardId, out string definitionKey))
            {
                return;
            }

            _purchasedDemonContractKeys.Add(definitionKey);
            AddPurchasedDemonContractToCurrentBattle(definitionKey);
            RefreshView();
            UpdateDemonCardHover(null);
        }

        private void PurchaseShopNormalCard(CardView card)
        {
            if (_formalShopModel != null && card != null)
            {
                FormalShopCardPurchaseRequested?.Invoke(card.CardId);
                UpdateHover(null);
                return;
            }

            if (shop == null || card == null ||
                !shop.TryPurchaseNormalCard(
                    card.CardId,
                    out string definitionKey,
                    out CardSuit suit))
            {
                return;
            }

            _purchasedNormalCards.Add(new PurchasedNormalCard(definitionKey, suit));
            AddPurchasedNormalCardToCurrentBattle(definitionKey, suit);
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

        private void BeginLighterRemoval()
        {
            int removableCount = _formalShopModel == null
                ? BuildRunDeckCardOptions().Count
                : CountFormalRemovableCards();
            if (shop == null || !shop.IsOpen || removableCount <= 0)
            {
                return;
            }

            _choosingLighterRemoval = true;
            UpdateShopUtilityItemHover(null);
            RefreshShopUtilityItems();
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

            RemoveRunDeckCard(option);
            RemoveCurrentBattleAvailableCard(option);
            _choosingLighterRemoval = false;
            RefreshView();
            return true;
        }

        private bool CancelLighterRemoval()
        {
            _choosingLighterRemoval = false;
            RefreshShopUtilityItems();
            return true;
        }

        private void PurchaseWhiskey()
        {
            if (_formalShopModel != null)
            {
                FormalShopRestRequested?.Invoke();
                UpdateShopUtilityItemHover(null);
                return;
            }

            CoreLoopBattle battle = Battle;
            if (shop == null ||
                battle == null ||
                !shop.TryPurchaseWhiskey(
                    battle.Player.Soul.Current,
                    battle.Player.Soul.Maximum,
                    out int restoreAmount))
            {
                RefreshShopUtilityItems();
                return;
            }

            battle.Player.Soul.Restore(restoreAmount);
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

        private string BuildRunDeckCardLabel(RunDeckCardOption option)
        {
            CardDefinition definition = CardDefinitionCatalog.GetByKey(option.DefinitionKey);
            string source = option.IsPurchased ? "BOUGHT" : "BASE";
            return definition.Rank + " " + FormatSuit(option.Suit) +
                "\n" + definition.DisplayName +
                "\n" + source;
        }

        private static string FormatSuit(CardSuit suit)
        {
            return suit == CardSuit.Clover ? "CLOVER" : "SPADE";
        }

        private void OnGUI()
        {
            if (IsModalInputBlocked ||
                shop == null ||
                !shop.IsOpen ||
                (_core == null && _formalShopModel == null))
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button)
            {
                font = uiFont,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (_choosingLighterRemoval)
            {
                DrawLighterRemovalPanel();
                return;
            }

            if (_formalShopModel != null ||
                _core.State == CoreLoopState.BattleEnded)
            {
                DrawShopControls();
            }
        }

        private void DrawShopControls()
        {
            if (_choosingLighterRemoval)
            {
                DrawLighterRemovalPanel();
                return;
            }

            DrawHeading("SHOP - hover goods and click to buy");
            if (_formalShopModel != null)
            {
                const float width = 160f;
                const float height = 48f;
                using (new GUIEnabledScope(
                           !_inputLocked &&
                           _formalShopModel.CanLeaveShop))
                {
                    if (GUI.Button(
                            new Rect(
                                (Screen.width - width) * 0.5f,
                                Screen.height - height - 24f,
                                width,
                                height),
                            "LEAVE",
                            _buttonStyle))
                    {
                        FormalShopLeaveRequested?.Invoke();
                    }
                }

                return;
            }

            DrawButtonRow(
                new[] { "나가기" },
                new[] { true },
                new Func<bool>[] { LeaveShop });
        }

        private void DrawLighterRemovalPanel()
        {
            if (_formalShopModel != null)
            {
                DrawFormalLighterRemovalPanel();
                return;
            }

            List<RunDeckCardOption> options = BuildRunDeckCardOptions();
            EnsureShopStyles();

            float width = Mathf.Min(760f, Screen.width - 40f);
            float height = Mathf.Min(520f, Screen.height - 120f);
            var panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                70f,
                width,
                height);
            GUI.Box(panelRect, string.Empty, _shopPanelStyle);

            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 14f, width - 36f, 30f),
                "LIGHTER - CHOOSE 1 CARD TO REMOVE",
                _labelStyle);

            int columns = Mathf.Clamp(Mathf.FloorToInt((width - 36f) / 132f), 3, 5);
            const float gap = 8f;
            float cardWidth = (width - 36f - (columns - 1) * gap) / columns;
            const float cardHeight = 74f;
            int rows = Mathf.CeilToInt(options.Count / (float)columns);
            var scrollRect = new Rect(
                panelRect.x + 18f,
                panelRect.y + 58f,
                width - 36f,
                height - 122f);
            var contentRect = new Rect(
                0f,
                0f,
                scrollRect.width - 18f,
                Mathf.Max(scrollRect.height, rows * (cardHeight + gap)));

            _lighterRemovalScroll = GUI.BeginScrollView(
                scrollRect,
                _lighterRemovalScroll,
                contentRect);
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                int row = i / columns;
                int column = i % columns;
                var cardRect = new Rect(
                    column * (cardWidth + gap),
                    row * (cardHeight + gap),
                    cardWidth,
                    cardHeight);

                using (new GUIEnabledScope(!_inputLocked && options.Count > 1))
                {
                    if (GUI.Button(
                        cardRect,
                        BuildRunDeckCardLabel(options[i]),
                        _shopCardButtonStyle))
                    {
                        RemoveCardWithLighter(index);
                    }
                }
            }

            GUI.EndScrollView();

            using (new GUIEnabledScope(!_inputLocked))
            {
                const float footerButtonWidth = 160f;
                const float footerGap = 12f;
                float footerX = panelRect.x +
                    (width - footerButtonWidth * 2f - footerGap) * 0.5f;
                if (GUI.Button(
                    new Rect(
                        footerX,
                        panelRect.yMax - 52f,
                        footerButtonWidth,
                        38f),
                    "CANCEL",
                    _buttonStyle))
                {
                    CancelLighterRemoval();
                }

                if (GUI.Button(
                    new Rect(
                        footerX + footerButtonWidth + footerGap,
                        panelRect.yMax - 52f,
                        footerButtonWidth,
                        38f),
                    "나가기",
                    _buttonStyle))
                {
                    ProcessInput(LeaveShop);
                }
            }
        }

        private void EnsureShopStyles()
        {
            _shopPanelStyle ??= new GUIStyle(GUI.skin.box)
            {
                font = uiFont,
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(14, 14, 14, 14),
                normal = { textColor = Color.white }
            };
            _shopCardButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                font = uiFont,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        // Bottom-anchored, screen-centered row. Width shrinks to always fit one row on screen.
        private void DrawButtonRow(
            string[] labels,
            bool[] enabled,
            Func<bool>[] actions,
            float height = 48f,
            float maxWidth = 160f)
        {
            int n = labels.Length;
            if (n == 0)
            {
                return;
            }

            const float gap = 8f;
            float w = Mathf.Min(
                maxWidth,
                (Screen.width - 40f - (n - 1) * gap) / n);
            float totalWidth = n * w + (n - 1) * gap;
            float x0 = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - height - 24f;

            for (int i = 0; i < n; i++)
            {
                using (new GUIEnabledScope(!_inputLocked && enabled[i]))
                {
                    if (GUI.Button(
                        new Rect(x0 + i * (w + gap), y, w, height),
                        labels[i],
                        _buttonStyle))
                    {
                        ProcessInput(actions[i]);
                    }
                }
            }
        }

        private void DrawHeading(string text, float rowHeight = 48f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            const float h = 30f;
            float y = Screen.height - rowHeight - 24f - h - 6f;
            GUI.Label(new Rect(0f, y, Screen.width, h), text, _labelStyle);
        }

        private void HandleCombatCommand(GameSceneCombatHudCommand command)
        {
            if (IsModalInputBlocked)
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
                    ProcessInput(() => TryResolvePlayerDemonContract(
                        command.InteractionId,
                        command.OptionId));
                    break;
                case GameSceneCombatHudCommandKind.BeginActiveDemonContractAction:
                    ProcessInput(() => TryBeginPlayerActiveDemonContractAction(
                        command.OptionId));
                    break;
                case GameSceneCombatHudCommandKind.Restart:
                    ProcessInput(RestartRun);
                    break;
            }
        }

        private void ProcessInput(Func<bool> action)
        {
            if (IsModalInputBlocked || _inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;

            // The battle runs the whole turn synchronously; Stepped fires once per sub-step, so we
            // snapshot each into a timeline and then pace them out over PlayTimeline.
            CoreLoopBattle battle = Battle;
            _timeline.Clear();
            if (battle != null)
            {
                battle.Stepped += OnBattleStepped;
            }

            bool accepted = action();

            if (battle != null)
            {
                battle.Stepped -= OnBattleStepped;
            }

            if (accepted && Application.isPlaying && _timeline.Count > 0)
            {
                StartCoroutine(PlayTimeline());
            }
            else
            {
                UnlockInput();
                RefreshView();
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

        private bool TryBeginPlayerActiveDemonContractAction(
            int sourceContractCardId)
        {
            return IsStageBattle
                ? _stageSession.TryBeginPlayerActiveDemonContractAction(
                    sourceContractCardId)
                : _session.TryBeginPlayerActiveDemonContractAction(
                    sourceContractCardId);
        }

        // Fires synchronously for each sub-step while the battle resolves the turn. Snapshots the
        // public view state at that instant so PlayTimeline can reveal them one beat at a time.
        private void OnBattleStepped()
        {
            _timeline.Add(GameScenePresenter.Create(Battle, _activeEnemyProfileKey));
        }

        private IEnumerator PlayTimeline()
        {
            List<GameSceneViewModel> timeline =
                new List<GameSceneViewModel>(_timeline);
            _timeline.Clear();

            foreach (GameSceneViewModel vm in timeline)
            {
                AppliedAnimationResult playedAnimation = ApplyView(
                    vm,
                    scheduleRevolverRetry: false,
                    deferHammerSmashCardRender: true);

                bool resolveBeat = vm.Core.State == CoreLoopState.ResolvingRound;
                float waitSeconds = resolveBeat
                    ? Mathf.Max(resolveHoldSeconds, MinimumRoundResultHoldSeconds)
                    : stepSeconds;
                if (playedAnimation.PlayedAny)
                {
                    waitSeconds = Mathf.Max(
                        waitSeconds,
                        playedAnimation.WaitSeconds);
                }

                yield return WaitForAnimationOrSeconds(
                    playedAnimation,
                    waitSeconds);

                if (playedAnimation.DeferredCardRender)
                {
                    RenderHands(playedAnimation.DeferredViewModel);
                }

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

            // Land on the true current state — e.g. BattleEnded, which is not itself a step.
            UnlockInput();
            RefreshView();
            ReturnToProgressionIfStageBattleEnded();
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
            MaybeOpenShop(vm);
            ApplyView(vm);
        }

        private AppliedAnimationResult ApplyView(
            GameSceneViewModel vm,
            bool scheduleRevolverRetry = true,
            bool deferHammerSmashCardRender = false)
        {
            _core = vm.Core;
            bool isShopOpen = shop != null && shop.IsOpen;
            bool hideCombatHudForPresentation =
                _inputLocked &&
                (vm.HammerAnimationCue != null ||
                 vm.RevolverAnimationCue != null);
            GameSceneCombatHudViewModel combat =
                GameSceneCombatHudPresenter.Create(
                    vm.Core,
                    IsStageBattle,
                    isShopOpen,
                    _inputLocked,
                    vm.UsesDiegeticCardEffectSelection,
                    hideForPresentation: hideCombatHudForPresentation);

            if (hud != null)
            {
                hud.Render(vm.Core, combat);
                int gold = IsStageBattle
                    ? _stageSession.Progress.Player.CurrentGold
                    : shop != null ? shop.Gold : 0;
                hud.SetGold(gold);
            }

            if (combat.Mode != GameSceneCombatHudMode.Actions)
            {
                UpdateCombatCommandHover(null);
            }

            tableCombatCommands?.Render(combat);

            RenderDemonContractSelection(combat);
            contractPapers?.Render(ContractPaperPresenter.Create(
                Battle,
                isCombatVisible: !isShopOpen));

            RefreshDeckStacks();
            RefreshShopUtilityItems();

            bool playedRevolverAnimation =
                TryPlayRevolverAnimation(
                    vm.RevolverAnimationCue,
                    scheduleRevolverRetry);
            _playedHammerAnimationController = null;
            bool playedHammerAnimation =
                TryPlayHammerAnimation(vm.HammerAnimationCue);
            UpdateEnemyCardSelectionCamera(
                vm.FocusesEnemyCardsForSelection);
            bool deferredCardRender =
                deferHammerSmashCardRender &&
                playedHammerAnimation &&
                IsHammerSmashCue(vm.HammerAnimationCue);

            // While the shop is open its presentation (merchant, hidden combat objects, goods) is owned
            // by ShopController; skip the combat re-render so it doesn't repaint the enemy over the merchant.
            if (shop != null && shop.IsOpen)
            {
                return CreateAppliedAnimationResult(
                    playedRevolverAnimation,
                    playedHammerAnimation,
                    deferredCardRender: false,
                    deferredViewModel: null);
            }

            if (!deferredCardRender)
            {
                RenderHands(vm);
            }

            RenderCrystalOrbSelection(vm);
            RenderSatanNumberSelection(vm);

            if (enemyCharacter != null)
            {
                enemyCharacter.Render(
                    ResolveRevolverTimedVisual(CombatantSide.Enemy, vm.EnemyVisual),
                    vm.EnemyActionLabel);
            }

            if (totals != null)
            {
                totals.Render(
                    vm.PlayerTotalsText,
                    vm.EnemyTotalsText);
            }

            return CreateAppliedAnimationResult(
                playedRevolverAnimation,
                playedHammerAnimation,
                deferredCardRender,
                deferredCardRender ? vm : null);
        }

        private void RenderHands(GameSceneViewModel vm)
        {
            if (vm == null)
            {
                return;
            }

            if (playerHand != null)
            {
                playerHand.Render(vm.PlayerCards);
            }

            if (enemyHand != null)
            {
                enemyHand.Render(vm.EnemyCards);
            }
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
                return;
            }

            _camera ??= Camera.main;
            satanNumberSelection.Render(vm.SatanNumberCandidates, _camera);
        }

        private void RefreshDeckStacks()
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                remainingDeck?.Render(0);
                discardDeck?.Render(0);
                return;
            }

            remainingDeck?.Render(SumRankCounts(battle.Player.Deck.GetDrawPileRankCounts()));
            discardDeck?.Render(SumRankCounts(battle.Player.Deck.GetDiscardPileRankCounts()));
        }

        private static int SumRankCounts(IReadOnlyList<int> counts)
        {
            if (counts == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < counts.Count; i++)
            {
                total += counts[i];
            }

            return total;
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
            ResetRevolverTriggers();

            if (cue.Phase == GameSceneRevolverAnimationPhase.Ready)
            {
                ClearPendingRevolverImpact();
                ResetRevolverAnimatorToBase();
                revolverAnimator.SetTrigger(playerReadyTrigger);
                RememberActiveRevolverReady(cue);
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

            revolverAnimator.SetTrigger(ResolveRevolverTrigger(cue));
            _revolverReadyActive = false;
            ApplyCinematicCamera(
                GameSceneCameraView.Current,
                revolverAnimationSeconds);

            if (Application.isPlaying &&
                cue.Phase == GameSceneRevolverAnimationPhase.ResolvedWithRetry &&
                scheduleRevolverRetry)
            {
                if (revolverAnimationSeconds > 0f)
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
                revolverAnimationSeconds > 0f)
            {
                _revolverHideRoutine =
                    StartCoroutine(HideRevolverAnimationAfterDelay());
            }

            return true;
        }

        private bool TryPlayHammerAnimation(GameSceneHammerAnimationCue cue)
        {
            HammerAnimationController controller = ResolveHammerAnimation();
            if (controller == null || !controller.TryPlay(cue, playerHand, enemyHand))
            {
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
        }

        private void EndEnemyCardSelectionCamera()
        {
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
            bool playedHammer,
            bool deferredCardRender = false,
            GameSceneViewModel deferredViewModel = null)
        {
            float waitSeconds = 0f;
            if (playedRevolver)
            {
                waitSeconds = Mathf.Max(waitSeconds, revolverAnimationSeconds);
            }

            if (playedHammer)
            {
                HammerAnimationController controller = ResolveHammerAnimation();
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    controller != null ? controller.AnimationSeconds : 0f);
            }

            return new AppliedAnimationResult(
                playedRevolver,
                playedHammer,
                waitSeconds,
                playedHammer ? _playedHammerAnimationController : null,
                deferredCardRender,
                deferredViewModel);
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

        private IEnumerator WaitForAnimationOrSeconds(
            AppliedAnimationResult animation,
            float waitSeconds)
        {
            if (!animation.PlayedHammer ||
                animation.HammerController == null ||
                !animation.HammerController.IsSmashAnimationPlaying)
            {
                yield return new WaitForSeconds(waitSeconds);
                yield break;
            }

            float elapsedSeconds = 0f;
            while (elapsedSeconds < waitSeconds ||
                animation.HammerController.IsSmashAnimationPlaying)
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
            yield return new WaitForSeconds(revolverAnimationSeconds);
            _revolverHideRoutine = null;
            ResetRevolverAnimatorToBase();

            GameObject root = ResolveRevolverRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearActiveRevolverReady();
            ClearPendingRevolverImpact();
        }

        private IEnumerator PrepareRevolverRetryAfterDelay(
            GameSceneRevolverAnimationCue cue)
        {
            yield return new WaitForSeconds(revolverAnimationSeconds);
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
        }

        private void HideRevolverAnimation()
        {
            StopRevolverHideRoutine();
            ClearPendingRevolverImpact();
            ResetRevolverAnimatorToBase();

            GameObject root = ResolveRevolverRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearActiveRevolverReady();
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
            _revolverReadyRoundNumber = 0;
            _revolverReadySourceCardId = 0;
            _revolverReadyActorSide = CombatantSide.Player;
        }

        private readonly struct AppliedAnimationResult
        {
            public AppliedAnimationResult(
                bool playedRevolver,
                bool playedHammer,
                float waitSeconds,
                HammerAnimationController hammerController,
                bool deferredCardRender,
                GameSceneViewModel deferredViewModel)
            {
                PlayedRevolver = playedRevolver;
                PlayedHammer = playedHammer;
                WaitSeconds = waitSeconds;
                HammerController = hammerController;
                DeferredCardRender = deferredCardRender;
                DeferredViewModel = deferredViewModel;
            }

            public bool PlayedRevolver { get; }

            public bool PlayedHammer { get; }

            public bool PlayedAny => PlayedRevolver || PlayedHammer;

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
                vm.Core.Outcome != BattleOutcome.PlayerVictory)
            {
                return;
            }

            CloseDeckPreview();
            shop.Open();
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
                _choosingLighterRemoval = false;
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateShopUtilityItemHover(null);
                shop.Close();
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
                shop.ResetRunEconomy();
            }

            return restarted;
        }

        private bool IsStageBattle => _stageSession != null;

        private void ReturnToProgressionIfStageBattleEnded()
        {
            if (!IsStageBattle ||
                _stageSession.Progress.State == StageProgressionState.InBattle)
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

        private void DrawFormalLighterRemovalPanel()
        {
            EnsureShopStyles();
            IReadOnlyList<ShopOwnedCardViewModel> options =
                _formalShopModel.ShopOwnedCards;
            float width = Mathf.Min(760f, Screen.width - 40f);
            float height = Mathf.Min(520f, Screen.height - 120f);
            var panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                70f,
                width,
                height);
            GUI.Box(panelRect, string.Empty, _shopPanelStyle);
            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 14f, width - 36f, 30f),
                "LIGHTER - CHOOSE 1 CARD TO REMOVE",
                _labelStyle);

            int columns = Mathf.Clamp(
                Mathf.FloorToInt((width - 36f) / 132f),
                3,
                5);
            const float gap = 8f;
            float cardWidth =
                (width - 36f - (columns - 1) * gap) / columns;
            const float cardHeight = 74f;
            int rows = Mathf.CeilToInt(options.Count / (float)columns);
            var scrollRect = new Rect(
                panelRect.x + 18f,
                panelRect.y + 58f,
                width - 36f,
                height - 122f);
            var contentRect = new Rect(
                0f,
                0f,
                scrollRect.width - 18f,
                Mathf.Max(scrollRect.height, rows * (cardHeight + gap)));
            _lighterRemovalScroll = GUI.BeginScrollView(
                scrollRect,
                _lighterRemovalScroll,
                contentRect);
            for (int i = 0; i < options.Count; i++)
            {
                ShopOwnedCardViewModel option = options[i];
                int row = i / columns;
                int column = i % columns;
                var cardRect = new Rect(
                    column * (cardWidth + gap),
                    row * (cardHeight + gap),
                    cardWidth,
                    cardHeight);
                using (new GUIEnabledScope(
                           !_inputLocked && option.CanRemove))
                {
                    if (GUI.Button(
                            cardRect,
                            option.DisplayName,
                            _shopCardButtonStyle))
                    {
                        _choosingLighterRemoval = false;
                        FormalShopCardRemovalRequested?.Invoke(option.CardId);
                    }
                }
            }

            GUI.EndScrollView();
            using (new GUIEnabledScope(!_inputLocked))
            {
                const float buttonWidth = 160f;
                if (GUI.Button(
                        new Rect(
                            panelRect.center.x - buttonWidth * 0.5f,
                            panelRect.yMax - 52f,
                            buttonWidth,
                            38f),
                        "CANCEL",
                        _buttonStyle))
                {
                    CancelLighterRemoval();
                }
            }
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

        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool _previous;

            public GUIEnabledScope(bool enabled)
            {
                _previous = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previous;
            }
        }
    }
}
