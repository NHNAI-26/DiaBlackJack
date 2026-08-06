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

    internal interface IDemonContractNormalTurnEndHandler
    {
        void OnNormalTurnEnded(
            DemonContractContext context,
            CombatantSide actorSide);
    }

    internal interface IDemonContractStandRestrictionHandler
    {
        bool PreventsOwnerStand(DemonContractContext context);
    }

    internal interface IDemonContractCardUseRestrictionHandler
    {
        bool PreventsOwnerCardUse(
            DemonContractContext context,
            BlackjackCard card);
    }

    internal interface IDemonContractOwnerTurnStartChoiceHandler
    {
        bool CanOfferOwnerTurnStartChoice(DemonContractContext context);
    }

    internal interface IDemonContractOpponentBustChoiceHandler
    {
        bool CanChooseExileAfterOpponentBust(DemonContractContext context);
    }

    internal interface IDemonContractRoundEndBustHandler
    {
        bool BustsOwnerAtRoundEnd(DemonContractContext context);
    }

    internal interface IDemonContractRoundStartHandler
    {
        void OnRoundStarted(DemonContractContext context);
    }

    internal interface IDemonContractFaceUpCardAddedBustHandler
    {
        bool BustsOwnerAfterFaceUpCardAdded(
            DemonContractContext context,
            BlackjackCard addedCard);
    }

    internal interface IDemonContractBustPreventionHandler
    {
        bool PreventsOwnerBust(DemonContractContext context);
    }

    internal interface IDemonContractBustReplacementHandler
    {
        bool TryReplaceOwnerBust(
            DemonContractContext context,
            out DemonContractEffectResult result);
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

        public HandValue OwnerHandValue => Owner.HandValue;

        public HandValue OwnerVisibleHandValue => Owner.VisibleHandValue;

        public int OwnerFaceUpCardCount => Owner.Hand.GetPublicCards().Count;

        public bool OpponentCanDraw => Opponent.Deck.CanDraw(1);

        public bool OpponentHasSingleHiddenCard =>
            Opponent.Hand.TryGetSingleHiddenCard(out _);

        public bool OwnerDeckCanDraw(int count)
        {
            return Owner.Deck.CanDraw(count);
        }

        public bool OpponentDeckCanDraw(int count)
        {
            return Opponent.Deck.CanDraw(count);
        }

        public IReadOnlyList<BlackjackCard> OwnerFaceUpCards =>
            Owner.Hand.GetPublicCards();

        public IReadOnlyList<BlackjackCard> OpponentFaceUpCards =>
            Opponent.Hand.GetPublicCards();

        public bool CanChooseBeelzebubDiscardCards =>
            !Owner.Soul.IsDepleted &&
            (Owner.Hand.GetPublicCards().Count > 0 ||
                Opponent.Hand.GetPublicCards().Count > 0);

        public void ApplyOwnerSoulDamage(int amount)
        {
            Owner.Soul.ApplyDamage(amount);
        }

        public void DiscardAllOwnerFaceUpCards()
        {
            IReadOnlyList<BlackjackCard> faceUpCards =
                Owner.Hand.GetPublicCards();
            foreach (BlackjackCard card in faceUpCards)
            {
                if (!Owner.TryDiscardCard(card.Id))
                {
                    throw new InvalidOperationException(
                        "Validated Belial activation could not discard an owner card.");
                }
            }
        }

        public RoundResolution CreateOpponentContractEffectBustResolution()
        {
            return RoundResolver.ResolveContractEffectBust(
                _battle.RoundNumber,
                playerIsTarget: OpponentSide == CombatantSide.Player);
        }

        public bool TryDiscardChosenFaceUpCards(
            int? ownerCardId,
            int? opponentCardId)
        {
            if ((!ownerCardId.HasValue && !opponentCardId.HasValue) ||
                (ownerCardId.HasValue &&
                    (!Owner.Hand.TryGetCard(
                        ownerCardId.Value,
                        out BlackjackCard ownerCard) ||
                    !ownerCard.IsFaceUp ||
                    Owner.Hand.IsHiddenCard(ownerCardId.Value))) ||
                (opponentCardId.HasValue &&
                    (!Opponent.Hand.TryGetCard(
                        opponentCardId.Value,
                        out BlackjackCard opponentCard) ||
                    !opponentCard.IsFaceUp ||
                    Opponent.Hand.IsHiddenCard(opponentCardId.Value))))
            {
                return false;
            }

            if ((ownerCardId.HasValue &&
                    !Owner.TryDiscardCard(ownerCardId.Value)) ||
                (opponentCardId.HasValue &&
                    !Opponent.TryDiscardCard(opponentCardId.Value)))
            {
                throw new InvalidOperationException(
                    "Validated demon bust replacement could not discard its cards.");
            }

            return true;
        }

        public bool TryGetOwnerFaceUpCard(
            int cardId,
            out BlackjackCard card)
        {
            return TryGetFaceUpCard(Owner, cardId, out card);
        }

        public bool TryGetOpponentFaceUpCard(
            int cardId,
            out BlackjackCard card)
        {
            return TryGetFaceUpCard(Opponent, cardId, out card);
        }

        public bool TryRevealOwnerHiddenCard()
        {
            if (!Owner.Hand.TryGetSingleHiddenCard(out BlackjackCard hiddenCard))
            {
                return false;
            }

            hiddenCard.Reveal();
            return true;
        }

        public void CreateBaphometPentagrams()
        {
            _battle.CreateBaphometPentagrams(ActiveContract);
        }

        public bool OwnerHasAnotherFaceUpCardWithRank(BlackjackCard addedCard)
        {
            if (addedCard == null ||
                !addedCard.IsFaceUp ||
                Owner.Hand.IsHiddenCard(addedCard.Id))
            {
                return false;
            }

            foreach (BlackjackCard card in Owner.Hand.Cards)
            {
                if (!ReferenceEquals(card, addedCard) &&
                    card.IsFaceUp &&
                    !Owner.Hand.IsHiddenCard(card.Id) &&
                    card.Rank == addedCard.Rank)
                {
                    return true;
                }
            }

            return false;
        }

        private BattleParticipant Owner =>
            _battle.GetParticipant(ActiveContract.OwnerSide);

        private BattleParticipant Opponent =>
            _battle.GetOpponent(ActiveContract.OwnerSide);

        private static bool TryGetFaceUpCard(
            BattleParticipant participant,
            int cardId,
            out BlackjackCard card)
        {
            if (participant.Hand.TryGetCard(cardId, out card) &&
                card.IsFaceUp &&
                !participant.Hand.IsHiddenCard(cardId))
            {
                return true;
            }

            card = null;
            return false;
        }
    }

    internal sealed class DemonContractResolver
    {
        internal const int DefaultDemonDieSeed = 20260722;

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
            return CreateDefault(DefaultDemonDieSeed);
        }

        public static DemonContractResolver CreateDefault(int demonDieSeed)
        {
            return new DemonContractResolver(
                new SatanDemonContractHandler(),
                new BelphegorDemonContractHandler(),
                new MammonDemonContractHandler(
                    new DeterministicDemonDieRoller(demonDieSeed)),
                new LeviathanDemonContractHandler(),
                new BeelzebubDemonContractHandler(),
                new MephistophelesDemonContractHandler(),
                new AsmodeusDemonContractHandler(),
                new AzazelDemonContractHandler(),
                new PaimonDemonContractHandler(),
                new BelialDemonContractHandler(),
                new BaphometDemonContractHandler(),
                new LuciferDemonContractHandler());
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

        public bool CanOwnerUseCard(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            BlackjackCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            return !HasOwnerRestriction<IDemonContractCardUseRestrictionHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                    handler.PreventsOwnerCardUse(context, card));
        }

        public bool TryGetOwnerTurnStartChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerChoiceContract<
                IDemonContractOwnerTurnStartChoiceHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                    handler.CanOfferOwnerTurnStartChoice(context),
                out choiceContract);
        }

        public bool TryGetOwnerTurnStartChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            ISet<int> excludedSourceCardIds,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerChoiceContract<
                IDemonContractOwnerTurnStartChoiceHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                    handler.CanOfferOwnerTurnStartChoice(context),
                out choiceContract,
                excludedSourceCardIds);
        }

        public bool TryGetOwnerOpponentBustChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerOpponentBustChoiceContract(
                battle,
                activeContracts,
                ownerSide,
                excludedSourceCardIds: null,
                out choiceContract);
        }

        public bool TryGetOwnerOpponentBustChoiceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            ISet<int> excludedSourceCardIds,
            out ActiveDemonContract choiceContract)
        {
            return TryGetOwnerChoiceContract<
                IDemonContractOpponentBustChoiceHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                    handler.CanChooseExileAfterOpponentBust(context),
                out choiceContract,
                excludedSourceCardIds);
        }

        public bool BustsOwnerAtRoundEnd(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide)
        {
            return HasOwnerRestriction<IDemonContractRoundEndBustHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                    handler.BustsOwnerAtRoundEnd(context));
        }

        public void NotifyRoundStarted(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide)
        {
            VisitOwnerHandlers<IDemonContractRoundStartHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) => handler.OnRoundStarted(context));
        }

        public bool BustsOwnerAfterFaceUpCardAdded(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            BlackjackCard addedCard)
        {
            if (addedCard == null)
            {
                throw new ArgumentNullException(nameof(addedCard));
            }

            return HasOwnerRestriction<IDemonContractFaceUpCardAddedBustHandler>(
                battle,
                activeContracts,
                ownerSide,
                (handler, context) =>
                    handler.BustsOwnerAfterFaceUpCardAdded(
                        context,
                        addedCard));
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

        public bool TryReplaceOwnerBust(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            out DemonContractEffectResult result,
            out ActiveDemonContract replacementContract)
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
                    !_handlers.TryGetValue(
                        activeContract.Kind,
                        out IDemonContractHandler handler) ||
                    !(handler is IDemonContractBustReplacementHandler
                        replacementHandler))
                {
                    continue;
                }

                if (replacementHandler.TryReplaceOwnerBust(
                    new DemonContractContext(battle, activeContract),
                    out result))
                {
                    replacementContract = activeContract;
                    return true;
                }
            }

            result = null;
            replacementContract = null;
            return false;
        }

        public bool TryCompleteOwnerBustReplacement(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            int? ownerCardId,
            int? opponentCardId)
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
                    out IDemonContractHandler handler) ||
                !(handler is IDemonContractBustReplacementHandler))
            {
                return false;
            }

            return new DemonContractContext(battle, activeContract)
                .TryDiscardChosenFaceUpCards(ownerCardId, opponentCardId);
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

        public void NotifyNormalTurnEnded(
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

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.OwnerSide != actorSide ||
                    !_handlers.TryGetValue(
                        activeContract.Kind,
                        out IDemonContractHandler handler) ||
                    !(handler is IDemonContractNormalTurnEndHandler turnEndHandler))
                {
                    continue;
                }

                turnEndHandler.OnNormalTurnEnded(
                    new DemonContractContext(battle, activeContract),
                    actorSide);
            }
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

        public bool CanOwnerRerollMammon(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract)
        {
            IDemonContractMammonRerollHandler handler =
                GetSpecializedHandler<IDemonContractMammonRerollHandler>(
                    activeContract);
            return handler.CanReroll(
                new DemonContractContext(battle, activeContract));
        }

        public MammonRerollResult RerollMammon(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract)
        {
            IDemonContractMammonRerollHandler handler =
                GetSpecializedHandler<IDemonContractMammonRerollHandler>(
                    activeContract);
            return handler.Reroll(
                new DemonContractContext(battle, activeContract));
        }

        public MammonRerollResult RerollMammon(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            int dieValue)
        {
            IDemonContractMammonRerollHandler handler =
                GetSpecializedHandler<IDemonContractMammonRerollHandler>(
                    activeContract);
            return handler.Reroll(
                new DemonContractContext(battle, activeContract),
                dieValue);
        }

        public void KeepOwnerMammonDie(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract)
        {
            IDemonContractMammonRerollHandler handler =
                GetSpecializedHandler<IDemonContractMammonRerollHandler>(
                    activeContract);
            handler.KeepCurrentValue(
                new DemonContractContext(battle, activeContract));
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

        public bool TryGetOwnerCardEffectSequenceContract(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            CardEffectKind effectKind,
            out ActiveDemonContract sequenceContract)
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
                    !_handlers.TryGetValue(
                        activeContract.Kind,
                        out IDemonContractHandler handler) ||
                    !(handler is IDemonContractCardEffectRepeatHandler
                        repeatHandler))
                {
                    continue;
                }

                if (repeatHandler.SupportsOwnerCardEffect(
                    new DemonContractContext(battle, activeContract),
                    effectKind))
                {
                    sequenceContract = activeContract;
                    return true;
                }
            }

            sequenceContract = null;
            return false;
        }

        public bool RequiresAdditionalOwnerCardEffectActivation(
            CoreLoopBattle battle,
            ActiveDemonContract activeContract,
            CardEffectResult cardEffectResult)
        {
            IDemonContractCardEffectRepeatHandler handler =
                GetSpecializedHandler<IDemonContractCardEffectRepeatHandler>(
                    activeContract);
            return handler.RequiresAdditionalActivation(
                new DemonContractContext(battle, activeContract),
                cardEffectResult);
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

        private void VisitOwnerHandlers<THandler>(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            Action<THandler, DemonContractContext> visit)
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

            if (visit == null)
            {
                throw new ArgumentNullException(nameof(visit));
            }

            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.OwnerSide != ownerSide ||
                    !_handlers.TryGetValue(
                        activeContract.Kind,
                        out IDemonContractHandler handler) ||
                    !(handler is THandler specializedHandler))
                {
                    continue;
                }

                visit(
                    specializedHandler,
                    new DemonContractContext(battle, activeContract));
            }
        }

        private bool TryGetOwnerChoiceContract<THandler>(
            CoreLoopBattle battle,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            Func<THandler, DemonContractContext, bool> requiresChoice,
            out ActiveDemonContract choiceContract,
            ISet<int> excludedSourceCardIds = null)
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
                if ((excludedSourceCardIds != null &&
                        excludedSourceCardIds.Contains(
                            activeContract.SourceCardId)) ||
                    activeContract.OwnerSide != ownerSide ||
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
