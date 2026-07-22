using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal interface IDemonContractHandler
    {
        DemonContractKind Kind { get; }

        DemonContractRuntimeState Activate(DemonContractContext context);
    }

    internal interface IDemonContractPlayerHitPreviewHandler
    {
        bool RequiresOwnerHitPreview(DemonContractContext context);
    }

    internal interface IDemonContractOwnerTurnHandler
    {
        void OnOwnerTurnStarted(DemonContractContext context);

        bool TryConsumeAutoStandAfterOwnerAction(DemonContractContext context);

        void OnRoundEnded(DemonContractContext context);
    }

    internal interface IDemonContractNormalTurnHandler
    {
        bool OnNormalTurnStarted(
            DemonContractContext context,
            CombatantSide actorSide);
    }

    internal interface IDemonContractStandRestrictionHandler
    {
        bool PreventsOwnerStand(DemonContractContext context);
    }

    internal interface IDemonContractBustPreventionHandler
    {
        bool PreventsOwnerBust(DemonContractContext context);
    }

    internal interface IDemonContractBattleEndHandler
    {
        void OnBattleEnded(DemonContractContext context);
    }

    internal sealed class DemonContractContext
    {
        private readonly CoreLoopBattle _battle;

        public DemonContractContext(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            ActiveContract = activeContract ??
                throw new ArgumentNullException(nameof(activeContract));
        }

        public ActiveDemonContract ActiveContract { get; }

        public int OwnerSoul => Owner.Soul.Current;

        public bool OwnerSoulDepleted => Owner.Soul.IsDepleted;

        public bool OwnerIsStanding => Owner.IsStanding;

        public bool OpponentIsStanding => Opponent.IsStanding;

        public CombatantSide OpponentSide => ActiveContract.OwnerSide == CombatantSide.Player
            ? CombatantSide.Enemy
            : CombatantSide.Player;

        public HandValue OpponentHandValue => Opponent.VisibleHandValue;

        public HandValue OwnerVisibleHandValue => Owner.VisibleHandValue;

        public void ApplyOwnerSoulDamage(int amount)
        {
            Owner.Soul.ApplyDamage(amount);
        }

        public BlackjackCard AddOwnerTemporaryFaceUpCard(CardDefinition definition)
        {
            return _battle.AddTemporaryFaceUpCard(
                ActiveContract.OwnerSide,
                definition);
        }

        public bool TryRemoveOwnerTemporaryCard(int cardId)
        {
            return Owner.TryRemoveTemporaryCard(cardId);
        }

        public RoundResolution CreateOpponentContractEffectBustResolution()
        {
            return RoundResolver.ResolveContractEffectBust(
                _battle.RoundNumber,
                playerIsTarget: OpponentSide == CombatantSide.Player);
        }

        private BattleParticipant Owner =>
            _battle.GetParticipant(ActiveContract.OwnerSide);

        private BattleParticipant Opponent =>
            _battle.GetOpponent(ActiveContract.OwnerSide);
    }

    internal sealed class DemonContractResolver
    {
        private readonly Dictionary<DemonContractKind, IDemonContractHandler> _handlers =
            new Dictionary<DemonContractKind, IDemonContractHandler>();

        public DemonContractResolver(params IDemonContractHandler[] handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            foreach (IDemonContractHandler handler in handlers)
            {
                if (handler == null)
                {
                    throw new ArgumentException(
                        "Demon contract handlers cannot contain null.",
                        nameof(handlers));
                }

                if (!Enum.IsDefined(typeof(DemonContractKind), handler.Kind))
                {
                    throw new ArgumentOutOfRangeException(nameof(handlers));
                }

                if (_handlers.ContainsKey(handler.Kind))
                {
                    throw new ArgumentException(
                        $"Demon contract handler for {handler.Kind} is duplicated.",
                        nameof(handlers));
                }

                _handlers.Add(handler.Kind, handler);
            }
        }

        public static DemonContractResolver CreateDefault()
        {
            return new DemonContractResolver(
                new SatanDemonContractHandler(),
                new BelphegorDemonContractHandler(),
                new MammonDemonContractHandler(
                    new DeterministicDemonDieRoller(seed: 20260722)),
                new LeviathanDemonContractHandler());
        }

        public bool CanPlayerStand(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            return CanOwnerStand(
                battle,
                activeContracts,
                CombatantSide.Player);
        }

        public bool CanOwnerStand(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide)
        {
            return !HasOwnerRestriction<IDemonContractStandRestrictionHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) => handler.PreventsOwnerStand(context));
        }

        public bool PreventsPlayerBust(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            return PreventsOwnerBust(
                battle,
                activeContracts,
                CombatantSide.Player);
        }

        public bool PreventsOwnerBust(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide)
        {
            return HasOwnerRestriction<IDemonContractBustPreventionHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) => handler.PreventsOwnerBust(context));
        }

        public IReadOnlyList<ActiveDemonContract> NotifyNormalTurnStarted(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide actorSide)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            var endedContracts = new List<ActiveDemonContract>();
            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (!_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                    !(handler is IDemonContractNormalTurnHandler normalTurnHandler))
                {
                    continue;
                }

                if (normalTurnHandler.OnNormalTurnStarted(
                    new DemonContractContext(battle, activeContract),
                    actorSide))
                {
                    endedContracts.Add(activeContract);
                }
            }

            return endedContracts.AsReadOnly();
        }

        public void NotifyBattleEnded(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (_handlers.TryGetValue(
                        activeContract.Kind,
                        out IDemonContractHandler handler) &&
                    handler is IDemonContractBattleEndHandler battleEndHandler)
                {
                    battleEndHandler.OnBattleEnded(
                        new DemonContractContext(battle, activeContract));
                }
            }
        }

        public DemonContractRuntimeState Activate(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContract == null)
            {
                throw new ArgumentNullException(nameof(activeContract));
            }

            if (!_handlers.TryGetValue(
                activeContract.Kind,
                out IDemonContractHandler handler))
            {
                return new EmptyDemonContractRuntimeState();
            }

            DemonContractRuntimeState runtimeState = handler.Activate(
                new DemonContractContext(battle, activeContract));
            return runtimeState ?? throw new InvalidOperationException(
                $"Demon contract handler for {activeContract.Kind} returned no runtime state.");
        }

        public bool TryGetPlayerHitPreviewContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            out ActiveDemonContract previewContract)
        {
            return TryGetOwnerHitPreviewContract(
                battle,
                activeContracts,
                CombatantSide.Player,
                out previewContract);
        }

        public bool TryGetOwnerHitPreviewContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            out ActiveDemonContract previewContract)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.OwnerSide != ownerSide ||
                    !_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                    !(handler is IDemonContractPlayerHitPreviewHandler previewHandler))
                {
                    continue;
                }

                if (previewHandler.RequiresOwnerHitPreview(
                    new DemonContractContext(battle, activeContract)))
                {
                    previewContract = activeContract;
                    return true;
                }
            }

            previewContract = null;
            return false;
        }

        public void NotifyPlayerTurnStarted(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            NotifyOwnerTurnStarted(
                battle,
                activeContracts,
                CombatantSide.Player);
        }

        public void NotifyOwnerTurnStarted(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide)
        {
            VisitOwnerTurnHandlers(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) => handler.OnOwnerTurnStarted(context));
        }

        public bool TryGetPlayerTurnChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerTurnChoiceContract(
                battle,
                activeContracts,
                CombatantSide.Player,
                out choiceContract);
        }

        public bool TryGetOwnerTurnChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerChoiceContract<IDemonContractOwnerTurnChoiceHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) => handler.RequiresOwnerTurnChoice(context),
                out choiceContract);
        }

        public DemonContractTurnChoiceResult ResolvePlayerTurnChoice(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            int optionId)
        {
            return ResolveOwnerTurnChoice(battle, activeContract, optionId);
        }

        public DemonContractTurnChoiceResult ResolveOwnerTurnChoice(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            int optionId)
        {
            IDemonContractOwnerTurnChoiceHandler handler =
                GetSpecializedHandler<IDemonContractOwnerTurnChoiceHandler>(activeContract);
            return handler.ResolveOwnerTurnChoice(
                new DemonContractContext(battle, activeContract),
                optionId);
        }

        public bool TryGetPlayerFinalChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerFinalChoiceContract(
                battle,
                activeContracts,
                CombatantSide.Player,
                out choiceContract);
        }

        public bool TryGetOwnerFinalChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerChoiceContract<IDemonContractFinalChoiceHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) => handler.RequiresFinalChoice(context),
                out choiceContract);
        }

        public int ResolvePlayerFinalChoice(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            int optionId)
        {
            return ResolveOwnerFinalChoice(battle, activeContract, optionId);
        }

        public int ResolveOwnerFinalChoice(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            int optionId)
        {
            IDemonContractFinalChoiceHandler handler =
                GetSpecializedHandler<IDemonContractFinalChoiceHandler>(activeContract);
            return handler.ResolveFinalChoice(
                new DemonContractContext(battle, activeContract),
                optionId);
        }

        public bool TryResolvePlayerAfterCardEffect(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CardEffectResult cardEffectResult,
            out DemonContractAfterCardEffectStep step)
        {
            return TryResolveOwnerAfterCardEffect(
                battle,
                activeContracts,
                CombatantSide.Player,
                cardEffectResult,
                out step);
        }

        public bool TryResolveOwnerAfterCardEffect(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            CardEffectResult cardEffectResult,
            out DemonContractAfterCardEffectStep step)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.OwnerSide != ownerSide ||
                    !_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                    !(handler is IDemonContractAfterCardEffectHandler afterCardHandler))
                {
                    continue;
                }

                var context = new DemonContractContext(battle, activeContract);
                if (!afterCardHandler.CanResolveAfterOwnerCardEffect(
                    context,
                    cardEffectResult))
                {
                    continue;
                }

                step = afterCardHandler.ResolveAfterOwnerCardEffect(
                    context,
                    cardEffectResult);
                return true;
            }

            step = null;
            return false;
        }

        public bool TryConsumePlayerAutoStand(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            return TryConsumeOwnerAutoStand(
                battle,
                activeContracts,
                CombatantSide.Player);
        }

        public bool TryConsumeOwnerAutoStand(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide)
        {
            bool shouldStand = false;
            VisitOwnerTurnHandlers(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                {
                    if (handler.TryConsumeAutoStandAfterOwnerAction(context))
                    {
                        shouldStand = true;
                    }
                });
            return shouldStand;
        }

        public void NotifyRoundEnded(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            VisitOwnerTurnHandlers(
                battle,
                activeContracts,
                null,
                (handler, context) => handler.OnRoundEnded(context));
        }

        private void VisitOwnerTurnHandlers(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide? ownerSide,
            Action<IDemonContractOwnerTurnHandler, DemonContractContext> visit)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            if (visit == null)
            {
                throw new ArgumentNullException(nameof(visit));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if ((ownerSide.HasValue && activeContract.OwnerSide != ownerSide.Value) ||
                    !_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                    !(handler is IDemonContractOwnerTurnHandler turnHandler))
                {
                    continue;
                }

                visit(turnHandler, new DemonContractContext(battle, activeContract));
            }
        }

        private bool TryGetOwnerChoiceContract<THandler>(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            Func<THandler, DemonContractContext, bool> requiresChoice,
            out ActiveDemonContract choiceContract)
            where THandler : class
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            if (requiresChoice == null)
            {
                throw new ArgumentNullException(nameof(requiresChoice));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.OwnerSide != ownerSide ||
                    !_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                    !(handler is THandler choiceHandler))
                {
                    continue;
                }

                if (requiresChoice(
                    choiceHandler,
                    new DemonContractContext(battle, activeContract)))
                {
                    choiceContract = activeContract;
                    return true;
                }
            }

            choiceContract = null;
            return false;
        }

        private bool HasOwnerRestriction<THandler>(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            Func<THandler, DemonContractContext, bool> isRestricted)
            where THandler : class
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (activeContracts == null)
            {
                throw new ArgumentNullException(nameof(activeContracts));
            }

            if (isRestricted == null)
            {
                throw new ArgumentNullException(nameof(isRestricted));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.OwnerSide == ownerSide &&
                    _handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) &&
                    handler is THandler specializedHandler &&
                    isRestricted(
                        specializedHandler,
                        new DemonContractContext(battle, activeContract)))
                {
                    return true;
                }
            }

            return false;
        }

        private THandler GetSpecializedHandler<THandler>(
            ActiveDemonContract activeContract)
            where THandler : class
        {
            if (activeContract == null)
            {
                throw new ArgumentNullException(nameof(activeContract));
            }

            if (!_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                !(handler is THandler specializedHandler))
            {
                throw new InvalidOperationException(
                    $"Demon contract handler for {activeContract.Kind} does not support {typeof(THandler).Name}.");
            }

            return specializedHandler;
        }
    }
}
