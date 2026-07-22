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

        public HandValue OpponentHandValue =>
            HandValueCalculator.Calculate(Opponent.Hand.Cards);

        public void ApplyOwnerSoulDamage(int amount)
        {
            Owner.Soul.ApplyDamage(amount);
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
                new BelphegorDemonContractHandler(),
                new MammonDemonContractHandler(
                    new DeterministicDemonDieRoller(seed: 20260722)),
                new LeviathanDemonContractHandler());
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
                if (activeContract.OwnerSide != CombatantSide.Player ||
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
            VisitPlayerTurnHandlers(
                battle,
                activeContracts,
                (handler, context) => handler.OnOwnerTurnStarted(context));
        }

        public bool TryGetPlayerTurnChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            out ActiveDemonContract choiceContract)
        {
            return TryGetPlayerChoiceContract<IDemonContractOwnerTurnChoiceHandler>(
                battle,
                activeContracts,
                (handler, context) => handler.RequiresOwnerTurnChoice(context),
                out choiceContract);
        }

        public DemonContractTurnChoiceResult ResolvePlayerTurnChoice(
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
            return TryGetPlayerChoiceContract<IDemonContractFinalChoiceHandler>(
                battle,
                activeContracts,
                (handler, context) => handler.RequiresFinalChoice(context),
                out choiceContract);
        }

        public int ResolvePlayerFinalChoice(
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
                if (activeContract.OwnerSide != CombatantSide.Player ||
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
            bool shouldStand = false;
            VisitPlayerTurnHandlers(
                battle,
                activeContracts,
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
            VisitPlayerTurnHandlers(
                battle,
                activeContracts,
                (handler, context) => handler.OnRoundEnded(context));
        }

        private void VisitPlayerTurnHandlers(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
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
                if (activeContract.OwnerSide != CombatantSide.Player ||
                    !_handlers.TryGetValue(activeContract.Kind, out IDemonContractHandler handler) ||
                    !(handler is IDemonContractOwnerTurnHandler turnHandler))
                {
                    continue;
                }

                visit(turnHandler, new DemonContractContext(battle, activeContract));
            }
        }

        private bool TryGetPlayerChoiceContract<THandler>(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
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
                if (activeContract.OwnerSide != CombatantSide.Player ||
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
