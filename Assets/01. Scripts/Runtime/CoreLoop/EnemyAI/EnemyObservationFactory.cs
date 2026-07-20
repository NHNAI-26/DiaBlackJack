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
                CreatePublicCards(battle.Player.Hand.GetFaceUpCards());
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
                decisionSeed);
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
                CreatePublicCards(battle.Player.Hand.GetFaceUpCards()),
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
                battle.Player.Hand.HiddenCardCount);
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
                    canUse));
            }

            return cards.AsReadOnly();
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

            if (battle.Enemy.IsStanding)
            {
                return candidates.AsReadOnly();
            }

            if (battle.Enemy.Deck.CanDraw(1))
            {
                candidates.Add(new EnemyActionCandidate(EnemyActionType.Hit));
            }

            candidates.Add(new EnemyActionCandidate(EnemyActionType.Stand));

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

        private static BlackjackCard FindOptionCard(
            CoreLoopBattle battle,
            PendingCardEffect pendingEffect,
            int? cardId)
        {
            if (!cardId.HasValue)
            {
                return null;
            }

            foreach (BlackjackCard card in pendingEffect.TemporaryCards)
            {
                if (card.Id == cardId.Value)
                {
                    return card;
                }
            }

            if (battle.Enemy.Hand.TryGetCard(cardId.Value, out BlackjackCard ownedCard))
            {
                return ownedCard;
            }

            throw new InvalidOperationException(
                "Enemy card option references a missing temporary or owned card.");
        }

        private static IReadOnlyList<PublicCardObservation> CreatePublicCards(
            IReadOnlyList<BlackjackCard> cards)
        {
            var observations = new List<PublicCardObservation>(cards.Count);
            foreach (BlackjackCard card in cards)
            {
                observations.Add(new PublicCardObservation(
                    card.DefinitionKey,
                    card.Rank));
            }

            return observations.AsReadOnly();
        }
    }
}
