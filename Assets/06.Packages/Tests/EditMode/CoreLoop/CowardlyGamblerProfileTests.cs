using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CowardlyGamblerProfileTests
    {
        [Test]
        public void EPR01_U01_ProfileAddsFourthNormalOpponentWithoutReplacingExistingProfiles()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;
            EnemyCombatProfile profile = catalog.GetByKey(
                EnemyCombatProfileCatalog.CowardlyGamblerKey);

            Assert.That(catalog.Profiles.Count, Is.EqualTo(6));
            Assert.That(
                catalog.Profiles.Count(candidate => candidate.Grade == EnemyGrade.Normal),
                Is.EqualTo(4));
            Assert.That(
                catalog.Profiles.Select(candidate => candidate.Key),
                Does.Contain(EnemyCombatProfileCatalog.GunslingerKey)
                    .And.Contain(EnemyCombatProfileCatalog.CultistKey)
                    .And.Contain(EnemyCombatProfileCatalog.TricksterKey));
            Assert.That(profile.DisplayName, Is.EqualTo("겁쟁이 도박사"));
            Assert.That(profile.Grade, Is.EqualTo(EnemyGrade.Normal));
            Assert.That(profile.MaximumSoul, Is.EqualTo(3));
            Assert.That(
                profile.BehaviorPolicyKey,
                Is.EqualTo(EnemyBehaviorPolicyCatalog.CowardlyGambler));
            Assert.That(profile.PlayerInformationMode, Is.EqualTo(EnemyInformationMode.Standard));
        }

        [Test]
        public void EPR01_U02_ProfileUsesEveryAuthoredDeckEntryAndCorrectedEighteenCardTotal()
        {
            EnemyCombatProfile profile = EnemyCombatProfileCatalog.Default.GetByKey(
                EnemyCombatProfileCatalog.CowardlyGamblerKey);
            IReadOnlyDictionary<string, int> counts = profile.DeckDefinitionKeys
                .GroupBy(key => key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            Assert.That(profile.DeckDefinitionKeys.Count, Is.EqualTo(18));
            Assert.That(counts["standard-ace-1"], Is.EqualTo(1));
            Assert.That(counts["standard-plain-2"], Is.EqualTo(3));
            Assert.That(counts["standard-plain-3"], Is.EqualTo(3));
            Assert.That(counts["standard-plain-4"], Is.EqualTo(3));
            Assert.That(counts["crystal-orb-5"], Is.EqualTo(3));
            Assert.That(counts["threat-hammer-6"], Is.EqualTo(1));
            Assert.That(counts["auto-pistol-7"], Is.EqualTo(1));
            Assert.That(counts["auto-pistol-8"], Is.EqualTo(1));
            Assert.That(counts["military-knife-9"], Is.EqualTo(1));
            Assert.That(counts["military-knife-10"], Is.EqualTo(1));
            Assert.That(counts.Count, Is.EqualTo(10));
        }

        [TestCase(14, EnemyActionType.Hit)]
        [TestCase(15, EnemyActionType.Stand)]
        public void EPR04_U03_PolicyUsesFifteenAsLowRiskStandBoundary(
            int total,
            EnemyActionType expectedAction)
        {
            var policy = new CowardlyGamblerEnemyPolicy();

            EnemyDecision decision = policy.Decide(CreateObservation(
                total,
                playerHiddenCardCount: 1,
                new[]
                {
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand)
                }));

            Assert.That(decision.ActionType, Is.EqualTo(expectedAction));
        }

        [Test]
        public void EPR01_U04_PolicyKeepsManualCardsAndPrefersSafeStand()
        {
            EnemyActionCandidate manualCard = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 7,
                cardDefinitionKey: "crystal-orb-5");
            EnemyObservation observation = CreateObservation(
                total: 15,
                playerHiddenCardCount: 1,
                new[]
                {
                    manualCard,
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand)
                });

            EnemyDecision decision = new CowardlyGamblerEnemyPolicy().Decide(observation);

            Assert.That(decision.ActionType, Is.EqualTo(EnemyActionType.Stand));
            Assert.That(
                decision.CandidateScores.Single(score => score.Candidate == manualCard).ReasonCode,
                Is.EqualTo("cowardly-gambler-keep-manual-card"));
        }

        [Test]
        public void EPR01_U05_PlayerHiddenCountDoesNotChangeBasicDecision()
        {
            EnemyActionCandidate[] candidates =
            {
                new EnemyActionCandidate(EnemyActionType.Hit),
                new EnemyActionCandidate(EnemyActionType.Stand)
            };
            var policy = new CowardlyGamblerEnemyPolicy();

            EnemyDecision oneHidden = policy.Decide(CreateObservation(
                total: 13,
                playerHiddenCardCount: 1,
                candidates));
            EnemyDecision twoHidden = policy.Decide(CreateObservation(
                total: 13,
                playerHiddenCardCount: 2,
                candidates));

            Assert.That(oneHidden.ActionType, Is.EqualTo(EnemyActionType.Hit));
            Assert.That(twoHidden.ActionType, Is.EqualTo(oneHidden.ActionType));
            Assert.That(twoHidden.ReasonCode, Is.EqualTo(oneHidden.ReasonCode));
        }

        private static EnemyObservation CreateObservation(
            int total,
            int playerHiddenCardCount,
            IReadOnlyList<EnemyActionCandidate> candidates)
        {
            return new EnemyObservation(
                new HandValue(total + 10),
                CreateOwnCards(total),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount,
                new SoulObservation(12, 12),
                new SoulObservation(3, 3),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 18,
                playerDeckAvailableCount: 20,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                candidates,
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed: 20260728);
        }

        private static IReadOnlyList<EnemyOwnedCardObservation> CreateOwnCards(
            int faceUpTotal)
        {
            const int firstRank = 5;
            int secondRank = faceUpTotal - firstRank;
            CardDefinition firstDefinition = CardDefinitionCatalog.GetDefaultForRank(firstRank);
            CardDefinition secondDefinition = CardDefinitionCatalog.GetDefaultForRank(secondRank);
            return new[]
            {
                new EnemyOwnedCardObservation(
                    cardId: 0,
                    definitionKey: firstDefinition.Key,
                    rank: firstRank,
                    isFaceUp: true,
                    useState: CardUseState.Available,
                    canUse: false),
                new EnemyOwnedCardObservation(
                    cardId: 1,
                    definitionKey: secondDefinition.Key,
                    rank: secondRank,
                    isFaceUp: true,
                    useState: CardUseState.Available,
                    canUse: false),
                new EnemyOwnedCardObservation(
                    cardId: 2,
                    definitionKey: "military-knife-10",
                    rank: 10,
                    isFaceUp: false,
                    useState: CardUseState.Available,
                    canUse: false)
            };
        }
    }
}
