using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyCommonActionTests
    {
        [Test]
        public void EP02_U01_ObservationContainsPublicPlayerCardsWithoutHiddenValueOrDeckOrder()
        {
            var policy = new CaptureThenStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 10, 3, 4 },
                enemyRanks: new[] { 8, 7, 6 },
                policy);
            battle.Start();

            battle.TryPlayerHit();

            EnemyObservation observation = policy.Observations.Single();
            Assert.That(
                observation.PlayerFaceUpCards.Select(card => card.Rank),
                Is.EqualTo(new[] { 2, 3 }));
            Assert.That(observation.PlayerHiddenCardCount, Is.EqualTo(1));
            Assert.That(observation.OwnCards.Select(card => card.Rank), Is.EqualTo(new[] { 8, 7 }));
            Assert.That(observation.PublicActionHistory.Count, Is.EqualTo(1));
            Assert.That(
                observation.PublicActionHistory[0].ActionType,
                Is.EqualTo(PublicCombatActionType.Hit));
            Assert.That(
                observation.PublicActionHistory[0].ActorSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(
                typeof(PublicCardObservation).GetProperty("CardId"),
                Is.Null);
            Assert.That(
                typeof(EnemyObservation).GetProperties()
                    .Any(property => ContainsBlackjackCard(property.PropertyType)),
                Is.False);
            Assert.That(
                typeof(EnemyObservation).GetProperties()
                    .Any(property => property.Name.Contains("Next") ||
                        property.Name.Contains("DrawPile")),
                Is.False);
        }

        [Test]
        public void EP02_U02_SameObservationAndSeedProduceSameDecisionAndScores()
        {
            var capturePolicy = new CaptureThenStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 3, 4, 5 },
                enemyRanks: new[] { 8, 7, 6 },
                capturePolicy);
            battle.Start();
            battle.TryPlayerHit();
            EnemyObservation observation = capturePolicy.Observations.Single();
            var policy = new SimpleEnemyPolicy();

            EnemyDecision first = policy.Decide(observation);
            EnemyDecision second = policy.Decide(observation);

            Assert.That(second.ActionType, Is.EqualTo(first.ActionType));
            Assert.That(second.CardId, Is.EqualTo(first.CardId));
            Assert.That(second.CardEffectOptionId, Is.EqualTo(first.CardEffectOptionId));
            Assert.That(second.ReasonCode, Is.EqualTo(first.ReasonCode));
            Assert.That(second.CandidateScores.Count, Is.EqualTo(first.CandidateScores.Count));
            for (int i = 0; i < first.CandidateScores.Count; i++)
            {
                Assert.That(
                    second.CandidateScores[i].Candidate.ActionType,
                    Is.EqualTo(first.CandidateScores[i].Candidate.ActionType));
                Assert.That(
                    second.CandidateScores[i].Score,
                    Is.EqualTo(first.CandidateScores[i].Score));
                Assert.That(
                    second.CandidateScores[i].ReasonCode,
                    Is.EqualTo(first.CandidateScores[i].ReasonCode));
            }
        }

        [Test]
        public void EP02_U03_UnavailableHitAndCardAreExcludedFromCandidates()
        {
            var policy = new CaptureThenStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 1, 8, 2 },
                enemyRanks: new[] { 9, 2 },
                policy);
            battle.Start();

            battle.TryPlayerHit();

            EnemyObservation observation = policy.Observations.Single();
            Assert.That(observation.PlayerFaceUpCards.Sum(card => card.Rank), Is.EqualTo(18));
            Assert.That(
                observation.ActionCandidates.Any(
                    candidate => candidate.ActionType == EnemyActionType.Hit),
                Is.False);
            Assert.That(
                observation.ActionCandidates.Any(
                    candidate => candidate.ActionType == EnemyActionType.UseCard),
                Is.False);
            Assert.That(
                observation.OwnCards.Single(card => card.Rank == 9).CanUse,
                Is.False);
            Assert.That(
                observation.ActionCandidates.Select(candidate => candidate.ActionType),
                Is.EqualTo(new[] { EnemyActionType.Stand, EnemyActionType.Fold }));
        }

        [Test]
        public void EP02_U04_InvalidDecisionIsRejectedBeforeAnyBattleDataChanges()
        {
            var policy = new InvalidThenStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 3, 4, 5 },
                enemyRanks: new[] { 8, 7, 6 },
                policy);
            battle.Start();

            battle.TryPlayerHit();

            Assert.That(policy.Observations.Count, Is.EqualTo(2));
            AssertEquivalentObservationState(
                policy.Observations[0],
                policy.Observations[1]);
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.IsStanding, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(
                battle.LastEnemyDecision.ActionType,
                Is.EqualTo(EnemyActionType.Stand));
        }

        [Test]
        public void EP02_U05_EnemyMilitaryKnifeUsesEnemyCardAndTargetsPlayer()
        {
            var policy = new UseEffectThenStandPolicy(CardEffectKind.MilitaryKnife);
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 3, 4, 5, 6 },
                enemyRanks: new[] { 9, 2, 3 },
                policy);
            battle.Start();
            BlackjackCard enemyKnife = battle.Enemy.Hand.Cards[0];

            battle.TryPlayerHit();

            Assert.That(enemyKnife.Definition.Effect, Is.EqualTo(CardEffectKind.MilitaryKnife));
            Assert.That(enemyKnife.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(4));
            Assert.That(battle.Player.Hand.Cards[3].Rank, Is.EqualTo(5));
            Assert.That(battle.Player.Hand.Cards[3].IsFaceUp, Is.True);
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.MilitaryKnife));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void EP02_U05_EnemyAutoPistolChoiceEndsRoundOnceWithCorrectTarget()
        {
            var policy = new UseAutoPistolWithGuessPolicy(guess: 7);
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 7, 4, 5 },
                enemyRanks: new[] { 7, 2, 3 },
                policy);
            battle.Start();

            battle.TryPlayerHit();

            Assert.That(policy.DecisionCount, Is.EqualTo(2));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(
                policy.PendingObservation.PlayerFaceUpCards.Any(card => card.Rank == 7),
                Is.False);
            Assert.That(policy.PendingObservation.PlayerHiddenCardCount, Is.EqualTo(1));
        }

        [Test]
        public void EP02_U05_EnemyFoldAppliesSymmetricSoulCostAndEndsRound()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 3, 4, 5 },
                enemyRanks: new[] { 8, 7, 6 },
                new SelectActionPolicy(EnemyActionType.Fold));
            battle.Start();

            battle.TryPlayerHit();

            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.EnemyFold));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.Fold));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void EnemyDecisionValidatorRequiresExactCardAndOptionCandidate()
        {
            var candidate = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 4,
                cardDefinitionKey: "auto-pistol-7",
                cardEffectOptionId: 7);
            var observation = CreateObservation(candidate);

            Assert.That(
                EnemyDecisionValidator.CanExecute(
                    observation,
                    EnemyDecision.FromCandidate(candidate, "valid")),
                Is.True);
            Assert.That(
                EnemyDecisionValidator.CanExecute(
                    observation,
                    new EnemyDecision(
                        EnemyActionType.UseCard,
                        cardId: 4,
                        cardEffectOptionId: 6,
                        reasonCode: "wrong-option",
                        candidateScores: Array.Empty<EnemyActionScore>())),
                Is.False);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            IEnemyBehaviorPolicy policy)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                enemyPolicy: policy);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Count);
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static EnemyObservation CreateObservation(EnemyActionCandidate candidate)
        {
            return new EnemyObservation(
                new HandValue(16),
                Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(3, 3),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 1,
                playerDeckAvailableCount: 1,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                new[] { candidate },
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: CardEffectKind.AutoPistol,
                decisionSeed: 1);
        }

        private static void AssertEquivalentObservationState(
            EnemyObservation expected,
            EnemyObservation actual)
        {
            Assert.That(actual.OwnHandValue.Total, Is.EqualTo(expected.OwnHandValue.Total));
            Assert.That(actual.OwnDeckAvailableCount, Is.EqualTo(expected.OwnDeckAvailableCount));
            Assert.That(actual.PlayerDeckAvailableCount, Is.EqualTo(expected.PlayerDeckAvailableCount));
            Assert.That(actual.PlayerSoul.Current, Is.EqualTo(expected.PlayerSoul.Current));
            Assert.That(actual.EnemySoul.Current, Is.EqualTo(expected.EnemySoul.Current));
            Assert.That(
                actual.OwnCards.Select(card => new { card.CardId, card.UseState, card.IsFaceUp }),
                Is.EqualTo(expected.OwnCards.Select(
                    card => new { card.CardId, card.UseState, card.IsFaceUp })));
            Assert.That(
                actual.PublicActionHistory.Select(action => action.ActionType),
                Is.EqualTo(expected.PublicActionHistory.Select(action => action.ActionType)));
        }

        private static bool ContainsBlackjackCard(Type type)
        {
            if (type == typeof(BlackjackCard))
            {
                return true;
            }

            return type.IsGenericType &&
                type.GetGenericArguments().Any(ContainsBlackjackCard);
        }

        private static EnemyDecision SelectCandidate(
            EnemyObservation observation,
            Func<EnemyActionCandidate, bool> predicate,
            string reason)
        {
            EnemyActionCandidate candidate = observation.ActionCandidates.First(predicate);
            return EnemyDecision.FromCandidate(candidate, reason);
        }

        private sealed class CaptureThenStandPolicy : IEnemyBehaviorPolicy
        {
            public List<EnemyObservation> Observations { get; } =
                new List<EnemyObservation>();

            public EnemyDecision Decide(EnemyObservation observation)
            {
                Observations.Add(observation);
                return SelectCandidate(
                    observation,
                    candidate => candidate.ActionType == EnemyActionType.Stand,
                    "capture-stand");
            }
        }

        private sealed class InvalidThenStandPolicy : IEnemyBehaviorPolicy
        {
            public List<EnemyObservation> Observations { get; } =
                new List<EnemyObservation>();

            public EnemyDecision Decide(EnemyObservation observation)
            {
                Observations.Add(observation);
                if (Observations.Count == 1)
                {
                    return new EnemyDecision(
                        EnemyActionType.UseCard,
                        cardId: 999,
                        cardEffectOptionId: null,
                        reasonCode: "invalid-card",
                        candidateScores: Array.Empty<EnemyActionScore>());
                }

                return SelectCandidate(
                    observation,
                    candidate => candidate.ActionType == EnemyActionType.Stand,
                    "retry-stand");
            }
        }

        private sealed class UseEffectThenStandPolicy : IEnemyBehaviorPolicy
        {
            private readonly CardEffectKind _effectKind;

            public UseEffectThenStandPolicy(CardEffectKind effectKind)
            {
                _effectKind = effectKind;
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate cardCandidate = observation.ActionCandidates
                    .FirstOrDefault(candidate =>
                        candidate.ActionType == EnemyActionType.UseCard &&
                        CardDefinitionCatalog.GetByKey(candidate.CardDefinitionKey).Effect ==
                            _effectKind);

                return cardCandidate != null
                    ? EnemyDecision.FromCandidate(cardCandidate, "use-enemy-card")
                    : SelectCandidate(
                        observation,
                        candidate => candidate.ActionType == EnemyActionType.Stand,
                        "stand-after-card");
            }
        }

        private sealed class UseAutoPistolWithGuessPolicy : IEnemyBehaviorPolicy
        {
            private readonly int _guess;

            public UseAutoPistolWithGuessPolicy(int guess)
            {
                _guess = guess;
            }

            public int DecisionCount { get; private set; }

            public EnemyObservation PendingObservation { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecisionCount++;
                if (observation.PendingCardEffectKind == CardEffectKind.AutoPistol)
                {
                    PendingObservation = observation;
                    return SelectCandidate(
                        observation,
                        candidate => candidate.CardEffectOptionId == _guess,
                        "declare-fixed-test-guess");
                }

                return SelectCandidate(
                    observation,
                    candidate => candidate.ActionType == EnemyActionType.UseCard &&
                        CardDefinitionCatalog.GetByKey(candidate.CardDefinitionKey).Effect ==
                            CardEffectKind.AutoPistol,
                    "begin-auto-pistol");
            }
        }

        private sealed class SelectActionPolicy : IEnemyBehaviorPolicy
        {
            private readonly EnemyActionType _actionType;

            public SelectActionPolicy(EnemyActionType actionType)
            {
                _actionType = actionType;
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                return SelectCandidate(
                    observation,
                    candidate => candidate.ActionType == _actionType,
                    "select-action");
            }
        }
    }
}
