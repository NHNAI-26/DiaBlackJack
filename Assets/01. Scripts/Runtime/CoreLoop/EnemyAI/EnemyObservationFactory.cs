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

            return new EnemyObservation(
                battle.Enemy.HandValue,
                ownCards,
                CreatePublicCards(battle.Player.Hand.GetFaceUpCards()),
                battle.Player.Hand.HiddenCardCount,
                new SoulObservation(battle.Player.Soul.Current, battle.Player.Soul.Maximum),
                new SoulObservation(battle.Enemy.Soul.Current, battle.Enemy.Soul.Maximum),
                battle.RoundNumber,
                battle.Player.IsStanding,
                battle.Enemy.IsStanding,
                battle.Enemy.Deck.AvailableCardCount,
                battle.Player.Deck.AvailableCardCount,
                CreatePublicCards(battle.Enemy.Deck.GetDiscardedCards()),
                CreatePublicCards(battle.Player.Deck.GetDiscardedCards()),
                battle.PublicActionHistory,
                actionCandidates,
                Array.Empty<EnemyNumberInference>(),
                battle.PendingEnemyCardEffect?.EffectKind,
                decisionSeed);
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
                    candidates.Add(new EnemyActionCandidate(
                        EnemyActionType.UseCard,
                        sourceCard.Id,
                        sourceCard.DefinitionKey,
                        option.Id));
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
            candidates.Add(new EnemyActionCandidate(EnemyActionType.Fold));

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
