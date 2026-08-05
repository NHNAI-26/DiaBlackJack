using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal static class EnemyObservationFactory
    {
        public static EnemyObservation Create(CoreLoopBattle battle, int decisionSeed)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            IReadOnlyList<EnemyOwnedCardObservation> ownCards = CreateOwnCards(battle);
            IReadOnlyList<EnemyActionCandidate> actionCandidates =
                CreateActionCandidates(battle, ownCards);
            IReadOnlyList<PublicCardObservation> playerFaceUpCards =
                CreatePublicCards(battle.Player.Hand.GetPublicCards());
            int? knownPlayerHiddenCardRank = GetKnownPlayerHiddenCardRank(battle);
            IReadOnlyList<PublicCardObservation> playerDiscardedCards =
                CreatePublicCards(battle.Player.Deck.GetDiscardedCards());
            IReadOnlyList<EnemyNumberInference> numberInferences =
                CreateNumberInferences(
                    battle,
                    playerFaceUpCards,
                    playerDiscardedCards);

            return new EnemyObservation(
                battle.Enemy.HandValue,
                ownCards,
                playerFaceUpCards,
                battle.Player.Hand.HiddenCardCount,
                new SoulObservation(battle.Player.Soul.Current, battle.Player.Soul.Maximum),
                new SoulObservation(battle.Enemy.Soul.Current, battle.Enemy.Soul.Maximum),
                battle.RoundNumber,
                battle.Player.IsStanding,
                battle.Enemy.IsStanding,
                battle.Enemy.Deck.AvailableCardCount,
                battle.Player.Deck.AvailableCardCount,
                CreatePublicCards(battle.Enemy.Deck.GetDiscardedCards()),
                playerDiscardedCards,
                battle.PublicActionHistory,
                actionCandidates,
                numberInferences,
                battle.PendingEnemyCardEffect?.EffectKind,
                decisionSeed,
                battle.EnemyHiddenCardComparisonKnowledge,
                knownPlayerHiddenCardRank,
                battle.NextEnemyChangeSoulCost,
                battle.InjectedPoisonCardCount);
        }

        internal static IReadOnlyList<EnemyNumberInference> CreateNumberInferences(
            CoreLoopBattle battle)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            return CreateNumberInferences(
                battle,
                CreatePublicCards(battle.Player.Hand.GetPublicCards()),
                CreatePublicCards(battle.Player.Deck.GetDiscardedCards()));
        }

        private static IReadOnlyList<EnemyNumberInference> CreateNumberInferences(
            CoreLoopBattle battle,
            IReadOnlyList<PublicCardObservation> playerFaceUpCards,
            IReadOnlyList<PublicCardObservation> playerDiscardedCards)
        {
            return EnemyNumberInferenceCalculator.Calculate(
                battle.Player.Deck.GetKnownRankCounts(),
                playerFaceUpCards,
                playerDiscardedCards,
                battle.Player.Hand.HiddenCardCount,
                GetKnownPlayerHiddenCardRank(battle));
        }

        private static IReadOnlyList<EnemyOwnedCardObservation> CreateOwnCards(
            CoreLoopBattle battle)
        {
            var cards = new List<EnemyOwnedCardObservation>(battle.Enemy.Hand.Count);
            foreach (BlackjackCard card in battle.Enemy.Hand.Cards)
            {
                bool canUse = battle.EvaluateCardUse(CombatantSide.Enemy, card.Id).CanUse;
                cards.Add(new EnemyOwnedCardObservation(
                    card.Id,
                    card.DefinitionKey,
                    card.Rank,
                    card.IsFaceUp,
                    card.UseState,
                    canUse,
                    battle.Enemy.Hand.IsHiddenCard(card.Id)));
            }

            return cards.AsReadOnly();
        }

        private static int? GetKnownPlayerHiddenCardRank(CoreLoopBattle battle)
        {
            return battle.Player.Hand.TryGetSingleHiddenCard(
                    out BlackjackCard hiddenCard) &&
                hiddenCard.IsFaceUp
                    ? hiddenCard.Rank
                    : (int?)null;
        }

        private static IReadOnlyList<EnemyActionCandidate> CreateActionCandidates(
            CoreLoopBattle battle,
            IReadOnlyList<EnemyOwnedCardObservation> ownCards)
        {
            var candidates = new List<EnemyActionCandidate>();
            if (battle.State != CoreLoopState.EnemyTurn)
            {
                return candidates.AsReadOnly();
            }

            PendingCardEffect pendingEffect = battle.PendingEnemyCardEffect;
            if (pendingEffect != null)
            {
                if (!battle.Enemy.Hand.TryGetCard(
                    pendingEffect.SourceCardId,
                    out BlackjackCard sourceCard))
                {
                    throw new InvalidOperationException(
                        "Pending enemy card effect lost its source card.");
                }

                foreach (CardEffectChoiceOption option in pendingEffect.Options)
                {
                    BlackjackCard optionCard = FindOptionCard(
                        battle,
                        pendingEffect,
                        option.CardId);
                    candidates.Add(new EnemyActionCandidate(
                        EnemyActionType.UseCard,
                        sourceCard.Id,
                        sourceCard.DefinitionKey,
                        option.Id,
                        option.NumericValue,
                        optionCard?.Id,
                        optionCard?.Rank));
                }

                return candidates.AsReadOnly();
            }

            PendingDemonContractInteraction pendingContract =
                battle.PendingEnemyDemonContractInteraction;
            if (pendingContract != null)
            {
                AddDemonContractCandidates(battle, pendingContract, candidates);
                return candidates.AsReadOnly();
            }

            if (battle.Enemy.IsStanding)
            {
                return candidates.AsReadOnly();
            }

            foreach (ActiveDemonContract activeContract in
                battle.ActiveEnemyDemonContracts)
            {
                if (activeContract.Kind == DemonContractKind.Satan &&
                    activeContract.RuntimeState is SatanRuntimeState satanState)
                {
                    bool canUse = satanState.CurrentFace == SatanContractFace.Upper
                        ? battle.Player.Hand.HiddenCardCount == 1
                        : battle.Player.Deck.CanDraw(1);
                    if (canUse)
                    {
                        candidates.Add(new EnemyActionCandidate(
                            EnemyActionType.DemonContract,
                            demonContractKind: DemonContractKind.Satan,
                            demonContractDefinitionKey:
                                activeContract.Definition.Key,
                            demonContractSourceCardId:
                                activeContract.SourceCardId));
                    }
                }
            }

            if (battle.Enemy.Deck.CanDraw(1))
            {
                candidates.Add(new EnemyActionCandidate(EnemyActionType.Hit));
            }

            if (battle.CanEnemyStand)
            {
                candidates.Add(new EnemyActionCandidate(EnemyActionType.Stand));
            }

            if (battle.EnemyDemonContractAvailability.CanBegin)
            {
                candidates.Add(new EnemyActionCandidate(
                    EnemyActionType.DemonContract));
            }

            if (battle.CanBeginEnemyChange)
            {
                candidates.Add(new EnemyActionCandidate(
                    EnemyActionType.Change));
            }

            foreach (EnemyOwnedCardObservation card in ownCards)
            {
                if (!card.CanUse)
                {
                    continue;
                }

                candidates.Add(new EnemyActionCandidate(
                    EnemyActionType.UseCard,
                    card.CardId,
                    card.DefinitionKey));
            }

            return candidates.AsReadOnly();
        }

        private static void AddDemonContractCandidates(
            CoreLoopBattle battle,
            PendingDemonContractInteraction pending,
            ICollection<EnemyActionCandidate> candidates)
        {
            int? privateNumericValue = null;
            string definitionKey = null;

            if (pending.Kind == DemonContractInteractionKind.BelphegorTopCard)
            {
                PlayerDemonContractPreview preview = battle.EnemyDemonContractPreview;
                if (preview == null ||
                    preview.InteractionId != pending.InteractionId)
                {
                    throw new InvalidOperationException(
                        "Pending enemy Belphegor choice lost its private preview.");
                }

                privateNumericValue = preview.Rank;
                definitionKey = DemonContractCatalog.BelphegorKey;
            }
            else if (pending.Kind == DemonContractInteractionKind.MammonReroll ||
                pending.Kind == DemonContractInteractionKind.MammonApplyDie)
            {
                ActiveDemonContract activeContract = FindActiveEnemyContract(
                    battle,
                    pending);
                if (!(activeContract.RuntimeState is MammonRuntimeState mammonState))
                {
                    throw new InvalidOperationException(
                        "Pending enemy Mammon choice has an invalid runtime state.");
                }

                privateNumericValue = mammonState.CurrentDieValue;
                definitionKey = activeContract.Definition.Key;
            }
            else if (pending.Kind ==
                    DemonContractInteractionKind.SatanDeclareFirstNumber ||
                pending.Kind ==
                    DemonContractInteractionKind.SatanDeclareSecondNumber)
            {
                ActiveDemonContract activeContract = FindActiveEnemyContract(
                    battle,
                    pending);
                if (!(activeContract.RuntimeState is SatanRuntimeState))
                {
                    throw new InvalidOperationException(
                        "Pending enemy Satan choice has an invalid runtime state.");
                }

                definitionKey = activeContract.Definition.Key;
            }
            else if (pending.Kind ==
                    DemonContractInteractionKind.BeelzebubChooseOwnerCard ||
                pending.Kind ==
                    DemonContractInteractionKind.BeelzebubChooseOpponentCard)
            {
                definitionKey = DemonContractCatalog.BeelzebubKey;
            }
            else if (pending.Kind ==
                DemonContractInteractionKind.AsmodeusForceOpponentHit)
            {
                definitionKey = DemonContractCatalog.AsmodeusKey;
            }
            else if (pending.Kind ==
                    DemonContractInteractionKind.PaimonChooseDeck ||
                pending.Kind ==
                    DemonContractInteractionKind.PaimonChooseExileCard)
            {
                definitionKey = DemonContractCatalog.PaimonKey;
            }
            else if (pending.Kind ==
                DemonContractInteractionKind.BelialChooseOpponentCard)
            {
                definitionKey = DemonContractCatalog.BelialKey;
            }
            else if (pending.Kind == DemonContractInteractionKind
                .LuciferChooseAdditionalContract)
            {
                definitionKey = DemonContractCatalog.LuciferKey;
            }

            foreach (DemonContractOption option in pending.Options)
            {
                DemonContractKind? contractKind = pending.ContractKind;
                string optionDefinitionKey = definitionKey;
                if (pending.Kind == DemonContractInteractionKind.ChooseContract ||
                    (pending.Kind == DemonContractInteractionKind
                            .LuciferChooseAdditionalContract &&
                        option.ContractDefinitionKey != null))
                {
                    DemonContractDefinition definition = DemonContractCatalog.Default
                        .GetByKey(option.ContractDefinitionKey);
                    contractKind = definition.Kind;
                    optionDefinitionKey = definition.Key;
                }
                else if (pending.Kind == DemonContractInteractionKind
                    .LuciferChooseAdditionalContract)
                {
                    contractKind = null;
                    optionDefinitionKey = null;
                }

                int? optionNumericValue = privateNumericValue;
                if (pending.Kind ==
                        DemonContractInteractionKind.BeelzebubChooseOwnerCard ||
                    pending.Kind ==
                        DemonContractInteractionKind.BeelzebubChooseOpponentCard)
                {
                    optionNumericValue = GetBeelzebubOptionRank(
                        battle,
                        pending,
                        option);
                }
                else if (pending.Kind ==
                    DemonContractInteractionKind.PaimonChooseExileCard)
                {
                    optionNumericValue = option.NumericValue;
                    if (optionNumericValue.HasValue &&
                        battle.PendingEnemyPaimonSelectedOwnerDeck)
                    {
                        optionNumericValue = -optionNumericValue.Value;
                    }
                }
                else if (pending.Kind ==
                    DemonContractInteractionKind.BelialChooseOpponentCard)
                {
                    optionNumericValue = option.NumericValue;
                }

                candidates.Add(new EnemyActionCandidate(
                    EnemyActionType.DemonContract,
                    demonContractOptionId: option.OptionId,
                    demonContractInteractionKind: pending.Kind,
                    demonContractKind: contractKind,
                    demonContractDefinitionKey: optionDefinitionKey,
                    demonContractOptionNumericValue:
                        pending.Kind ==
                            DemonContractInteractionKind.SatanDeclareFirstNumber ||
                        pending.Kind ==
                            DemonContractInteractionKind.SatanDeclareSecondNumber
                            ? option.NumericValue
                            : optionNumericValue,
                    demonContractSourceCardId:
                        pending.Kind ==
                            DemonContractInteractionKind.SatanDeclareFirstNumber ||
                        pending.Kind ==
                            DemonContractInteractionKind.SatanDeclareSecondNumber
                            ? pending.SourceContractCardId
                            : null));
            }
        }

        private static int GetBeelzebubOptionRank(
            CoreLoopBattle battle,
            PendingDemonContractInteraction pending,
            DemonContractOption option)
        {
            if (!option.ContractCardId.HasValue)
            {
                throw new InvalidOperationException(
                    "Beelzebub discard option lost its public card id.");
            }

            BattleParticipant participant = pending.Kind ==
                DemonContractInteractionKind.BeelzebubChooseOwnerCard
                    ? battle.Enemy
                    : battle.Player;
            if (!participant.Hand.TryGetCard(
                    option.ContractCardId.Value,
                    out BlackjackCard card) ||
                !card.IsFaceUp ||
                participant.Hand.IsHiddenCard(option.ContractCardId.Value))
            {
                throw new InvalidOperationException(
                    "Beelzebub discard option no longer identifies a face-up card.");
            }

            return card.Rank;
        }

        private static ActiveDemonContract FindActiveEnemyContract(
            CoreLoopBattle battle,
            PendingDemonContractInteraction pending)
        {
            if (!pending.SourceContractCardId.HasValue)
            {
                throw new InvalidOperationException(
                    "Pending enemy contract choice has no active source contract.");
            }

            foreach (ActiveDemonContract activeContract in
                battle.ActiveEnemyDemonContracts)
            {
                if (activeContract.SourceCardId == pending.SourceContractCardId.Value &&
                    activeContract.OwnerSide == CombatantSide.Enemy)
                {
                    return activeContract;
                }
            }

            throw new InvalidOperationException(
                "Pending enemy contract choice lost its active contract.");
        }

        private static BlackjackCard FindOptionCard(
            CoreLoopBattle battle,
            PendingCardEffect pendingEffect,
            int? cardId)
        {
            if (!cardId.HasValue)
            {
                return null;
            }

            if (pendingEffect.ChoiceKind == CardEffectChoiceKind.TakePeekedCard)
            {
                foreach (BlackjackCard card in pendingEffect.TemporaryCards)
                {
                    if (card.Id == cardId.Value)
                    {
                        return card;
                    }
                }
            }

            if (pendingEffect.ChoiceKind == CardEffectChoiceKind.DiscardOpponentFaceUpCard &&
                battle.Player.Hand.TryGetCard(cardId.Value, out BlackjackCard opponentCard) &&
                opponentCard.IsFaceUp &&
                !battle.Player.Hand.IsHiddenCard(cardId.Value))
            {
                return opponentCard;
            }

            throw new InvalidOperationException(
                "Enemy card option references a missing temporary or public opponent card.");
        }

        private static IReadOnlyList<PublicCardObservation> CreatePublicCards(
            IReadOnlyList<BlackjackCard> cards)
        {
            var observations = new List<PublicCardObservation>(cards.Count);
            foreach (BlackjackCard card in cards)
            {
                observations.Add(new PublicCardObservation(
                    card.DefinitionKey,
                    card.Rank,
                    card.CanUse));
            }

            return observations.AsReadOnly();
        }
    }
}
