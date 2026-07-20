using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyNormalPolicyTests
    {
        [Test]
        public void EP03_U01_DefaultNormalProfilesUseDedicatedPolicies()
        {
            Assert.That(
                EnemyBattleConfigurationFactory.Create(
                    EnemyCombatProfileCatalog.GunslingerKey,
                    1).BehaviorPolicy,
                Is.TypeOf<GunslingerEnemyPolicy>());
            Assert.That(
                EnemyBattleConfigurationFactory.Create(
                    EnemyCombatProfileCatalog.CultistKey,
                    1).BehaviorPolicy,
                Is.TypeOf<CultistEnemyPolicy>());
            Assert.That(
                EnemyBattleConfigurationFactory.Create(
                    EnemyCombatProfileCatalog.TricksterKey,
                    1).BehaviorPolicy,
                Is.TypeOf<TricksterEnemyPolicy>());
            Assert.That(
                EnemyBattleConfigurationFactory.Create(
                    EnemyCombatProfileCatalog.EnforcerKey,
                    1).BehaviorPolicy,
                Is.TypeOf<EnforcerEnemyPolicy>());
            Assert.That(
                EnemyBattleConfigurationFactory.Create(
                    EnemyCombatProfileCatalog.FinalBossKey,
                    1).BehaviorPolicy,
                Is.TypeOf<SimpleEnemyPolicy>());
        }

        [Test]
        public void EP03_U01_ObservationInfersHiddenNumberFromPublicCompositionOnly()
        {
            var policy = new CaptureThenStandPolicy();
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 7, 7, 7),
                CreateRankDeck(8, 7, 6),
                enemyPolicy: policy);
            battle.Start();

            battle.TryPlayerHit();

            EnemyObservation observation = policy.Observation;
            Assert.That(observation.PlayerFaceUpCards.Select(card => card.Rank),
                Is.EqualTo(new[] { 2, 7 }));
            Assert.That(observation.PlayerHiddenCardCount, Is.EqualTo(1));
            Assert.That(observation.NumberInferences.Count, Is.EqualTo(1));
            Assert.That(observation.NumberInferences[0].Number, Is.EqualTo(7));
            Assert.That(observation.NumberInferences[0].ProbabilityPercent, Is.EqualTo(100));
            Assert.That(
                typeof(EnemyNumberInference).GetProperties()
                    .Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    nameof(EnemyNumberInference.Number),
                    nameof(EnemyNumberInference.ProbabilityPercent)
                }));
        }

        [Test]
        public void EP03_U01_InferencePercentagesAreSortedAndSumToOneHundred()
        {
            var knownRankCounts = new int[11];
            knownRankCounts[7] = 2;
            knownRankCounts[8] = 2;

            IReadOnlyList<EnemyNumberInference> inferences =
                EnemyNumberInferenceCalculator.Calculate(
                    knownRankCounts,
                    new[] { new PublicCardObservation("auto-pistol-8", 8) },
                    Array.Empty<PublicCardObservation>(),
                    playerHiddenCardCount: 1);

            Assert.That(inferences.Select(value => value.Number), Is.EqualTo(new[] { 7, 8 }));
            Assert.That(
                inferences.Select(value => value.ProbabilityPercent),
                Is.EqualTo(new[] { 67, 33 }));
            Assert.That(inferences.Sum(value => value.ProbabilityPercent), Is.EqualTo(100));
        }

        [Test]
        public void EP03_U02_GunslingerUsesPistolAtHighConfidenceAndDeclaresTopRank()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 7, 7, 7),
                CreateDefinitionDeck(
                    "auto-pistol-7",
                    "standard-plain-2",
                    "standard-plain-3"),
                playerMaximumSoul: 2,
                enemyMaximumSoul: 3,
                enemyPolicy: new GunslingerEnemyPolicy());
            battle.Start();

            battle.TryPlayerHit();

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.True);
            Assert.That(battle.LastEnemyDecision.CardEffectOptionId, Is.EqualTo(7));
            Assert.That(
                typeof(CardEffectResult).GetProperties()
                    .Any(property => property.Name.Contains("Hidden") ||
                        property.Name.Contains("Number")),
                Is.False);
        }

        [Test]
        public void EP03_U02_GunslingerKeepsPistolBelowConfidenceThreshold()
        {
            EnemyActionCandidate pistol = CreateCardCandidate(1, "auto-pistol-7");
            EnemyObservation observation = CreateObservation(
                total: 17,
                candidates: new[]
                {
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand),
                    pistol
                },
                inferences: new[]
                {
                    new EnemyNumberInference(7, 20),
                    new EnemyNumberInference(8, 20)
                });

            EnemyDecision decision = new GunslingerEnemyPolicy().Decide(observation);

            Assert.That(decision.ActionType, Is.EqualTo(EnemyActionType.Stand));
            Assert.That(
                decision.CandidateScores.Single(score => score.Candidate == pistol).ReasonCode,
                Is.EqualTo("gunslinger-hold-pistol-at-low-confidence"));
        }

        [Test]
        public void EP03_U03_CultistAcceptsEighteenHitRiskButStandsAtNineteen()
        {
            EnemyActionCandidate[] candidates =
            {
                new EnemyActionCandidate(EnemyActionType.Hit),
                new EnemyActionCandidate(EnemyActionType.Stand),
                new EnemyActionCandidate(EnemyActionType.Fold)
            };
            var policy = new CultistEnemyPolicy();

            Assert.That(
                policy.Decide(CreateObservation(18, candidates)).ActionType,
                Is.EqualTo(EnemyActionType.Hit));
            Assert.That(
                policy.Decide(CreateObservation(19, candidates)).ActionType,
                Is.EqualTo(EnemyActionType.Stand));
        }

        [Test]
        public void EP03_U03_UnimplementedContractAndLieDetectorRemainAbsent()
        {
            EnemyCombatProfile cultist = EnemyCombatProfileCatalog.Default.GetByKey(
                EnemyCombatProfileCatalog.CultistKey);
            EnemyCombatProfile trickster = EnemyCombatProfileCatalog.Default.GetByKey(
                EnemyCombatProfileCatalog.TricksterKey);

            Assert.That(
                cultist.DeckDefinitionKeys.Any(key => key.Contains("contract")),
                Is.False);
            Assert.That(
                trickster.DeckDefinitionKeys.Any(key => key.Contains("lie")),
                Is.False);
            Assert.That(
                CardDefinitionCatalog.All.Any(definition =>
                    definition.Key.Contains("contract") || definition.Key.Contains("lie")),
                Is.False);
        }

        [Test]
        public void EP03_U04_TricksterUsesOrbAndKeepsHighestSafeCardInOwnHand()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 3, 4, 5),
                CreateDefinitionDeck(
                    "crystal-orb-5",
                    "standard-plain-4",
                    "standard-plain-4",
                    "standard-plain-2"),
                enemyPolicy: new TricksterEnemyPolicy());
            battle.Start();
            BlackjackCard orb = battle.Enemy.Hand.Cards[0];

            battle.TryPlayerHit();

            Assert.That(orb.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.Enemy.Hand.Cards.Count, Is.EqualTo(3));
            Assert.That(
                battle.Enemy.Hand.Cards.Count(card => card.Rank == 4),
                Is.EqualTo(2));
            Assert.That(battle.Enemy.Deck.Draw().Rank, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(
                battle.LastEnemyDecision.ReasonCode,
                Is.EqualTo("trickster-take-highest-safe-orb-card"));
        }

        [Test]
        public void EP03_U05_SameObservationProducesThreeDistinctNormalStrategies()
        {
            EnemyActionCandidate pistol = CreateCardCandidate(10, "auto-pistol-7");
            EnemyActionCandidate orb = CreateCardCandidate(11, "crystal-orb-5");
            EnemyObservation observation = CreateObservation(
                total: 17,
                candidates: new[]
                {
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand),
                    pistol,
                    orb
                },
                inferences: new[]
                {
                    new EnemyNumberInference(7, 80),
                    new EnemyNumberInference(8, 20)
                });

            EnemyDecision gunslinger = new GunslingerEnemyPolicy().Decide(observation);
            EnemyDecision cultist = new CultistEnemyPolicy().Decide(observation);
            EnemyDecision trickster = new TricksterEnemyPolicy().Decide(observation);

            Assert.That(gunslinger.ActionType, Is.EqualTo(EnemyActionType.UseCard));
            Assert.That(gunslinger.CardId, Is.EqualTo(pistol.CardId));
            Assert.That(cultist.ActionType, Is.EqualTo(EnemyActionType.Hit));
            Assert.That(trickster.ActionType, Is.EqualTo(EnemyActionType.UseCard));
            Assert.That(trickster.CardId, Is.EqualTo(orb.CardId));
        }

        [Test]
        public void EP03_U05_NormalPoliciesRepeatTheSameDecisionAndScores()
        {
            EnemyObservation observation = CreateObservation(
                total: 17,
                candidates: new[]
                {
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand),
                    CreateCardCandidate(10, "auto-pistol-7"),
                    CreateCardCandidate(11, "crystal-orb-5")
                },
                inferences: new[] { new EnemyNumberInference(7, 100) });
            IEnemyBehaviorPolicy[] policies =
            {
                new GunslingerEnemyPolicy(),
                new CultistEnemyPolicy(),
                new TricksterEnemyPolicy()
            };

            foreach (IEnemyBehaviorPolicy policy in policies)
            {
                EnemyDecision first = policy.Decide(observation);
                EnemyDecision second = policy.Decide(observation);

                Assert.That(second.ActionType, Is.EqualTo(first.ActionType));
                Assert.That(second.CardId, Is.EqualTo(first.CardId));
                Assert.That(second.ReasonCode, Is.EqualTo(first.ReasonCode));
                Assert.That(
                    second.CandidateScores.Select(score => score.Score),
                    Is.EqualTo(first.CandidateScores.Select(score => score.Score)));
            }
        }

        private static EnemyActionCandidate CreateCardCandidate(
            int cardId,
            string definitionKey)
        {
            return new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId,
                definitionKey);
        }

        private static EnemyObservation CreateObservation(
            int total,
            IReadOnlyList<EnemyActionCandidate> candidates,
            IReadOnlyList<EnemyNumberInference> inferences = null)
        {
            return new EnemyObservation(
                new HandValue(total),
                Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(3, 3),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 4,
                playerDeckAvailableCount: 4,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                candidates,
                inferences ?? Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed: 397);
        }

        private static BlackjackDeck CreateRankDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static BlackjackDeck CreateDefinitionDeck(params string[] definitionKeys)
        {
            var cards = new List<BlackjackCard>(definitionKeys.Length);
            for (int i = 0; i < definitionKeys.Length; i++)
            {
                cards.Add(new BlackjackCard(
                    i,
                    CardDefinitionCatalog.GetByKey(definitionKeys[i])));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private sealed class CaptureThenStandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyObservation Observation { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                Observation = observation;
                EnemyActionCandidate stand = observation.ActionCandidates.Single(
                    candidate => candidate.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(stand, "capture-stand");
            }
        }
    }
}
