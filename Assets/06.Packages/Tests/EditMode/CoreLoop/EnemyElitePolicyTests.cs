using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyElitePolicyTests
    {
        [Test]
        public void EP04_U01_EnforcerAndBossUseTheirDedicatedPolicies()
        {
            EnemyBattleConfiguration enforcer = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.EnforcerKey,
                enemyDeckSeed: 41);
            EnemyBattleConfiguration boss = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.FinalBossKey,
                enemyDeckSeed: 41);

            Assert.That(enforcer.Grade, Is.EqualTo(EnemyGrade.Elite));
            Assert.That(enforcer.EnemyMaximumSoul, Is.EqualTo(5));
            Assert.That(enforcer.BehaviorPolicy, Is.TypeOf<EnforcerEnemyPolicy>());
            Assert.That(enforcer.ExpectedRewardTier, Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(boss.BehaviorPolicy, Is.TypeOf<FinalBossEnemyPolicy>());
        }

        [Test]
        public void EP04_U02_EliteInferenceDisplayContainsTwoNumbersAndConfidenceOnly()
        {
            EnemyInferenceDisplayModel display = EnemyInferenceDisplayModel.CreateForElite(
                new[]
                {
                    new EnemyNumberInference(2, 20),
                    new EnemyNumberInference(7, 50),
                    new EnemyNumberInference(9, 30)
                });

            Assert.That(display.TopNumbers, Is.EqualTo(new[] { 7, 9 }));
            Assert.That(display.Confidence, Is.EqualTo(EnemyInferenceConfidence.Medium));
            Assert.That(
                typeof(EnemyInferenceDisplayModel).GetProperties()
                    .Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    nameof(EnemyInferenceDisplayModel.Confidence),
                    nameof(EnemyInferenceDisplayModel.TopNumbers)
                }));
        }

        [TestCase(34, EnemyInferenceConfidence.Low)]
        [TestCase(35, EnemyInferenceConfidence.Medium)]
        [TestCase(59, EnemyInferenceConfidence.Medium)]
        [TestCase(60, EnemyInferenceConfidence.High)]
        public void EP04_U02_EliteInferenceConfidenceUsesFixedBands(
            int probability,
            EnemyInferenceConfidence expected)
        {
            EnemyInferenceDisplayModel display = EnemyInferenceDisplayModel.CreateForElite(
                new[] { new EnemyNumberInference(7, probability) });

            Assert.That(display.Confidence, Is.EqualTo(expected));
        }

        [Test]
        public void EP04_U03_EnforcerPaysLowestPublicHammerCost()
        {
            var highCost = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 10,
                cardDefinitionKey: "threat-hammer-6",
                cardEffectOptionId: 10,
                cardEffectOptionCardId: 10,
                cardEffectOptionCardRank: 6);
            var lowCost = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 10,
                cardDefinitionKey: "threat-hammer-6",
                cardEffectOptionId: 12,
                cardEffectOptionCardId: 12,
                cardEffectOptionCardRank: 2);
            EnemyObservation observation = CreateObservation(
                ownTotal: 17,
                candidates: new[] { highCost, lowCost },
                playerIsStanding: true,
                pendingCardEffectKind: CardEffectKind.ThreatHammer);

            EnemyDecision decision = new EnforcerEnemyPolicy().Decide(observation);

            Assert.That(decision.CardEffectOptionId, Is.EqualTo(12));
            Assert.That(decision.ReasonCode, Is.EqualTo("enforcer-pay-lowest-hammer-cost"));
        }

        [Test]
        public void EP04_I01_EnemyHammerBreaksPlayerStandWithEnemyOwnedCost()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(10, 7, 3, 2),
                CreateDefinitionDeck(
                    "threat-hammer-6",
                    "standard-plain-4",
                    "standard-plain-3"),
                enemyMaximumSoul: 5,
                enemyPolicy: new EnforcerEnemyPolicy());
            battle.Start();
            BlackjackCard hammer = battle.Enemy.Hand.Cards[0];

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(hammer.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Enemy.Hand.Cards.Contains(hammer), Is.False);
            Assert.That(battle.Player.IsStanding, Is.False);
            Assert.That(
                battle.Player.Hand.Cards.Single(card => !card.IsFaceUp).Rank,
                Is.EqualTo(3));
            Assert.That(
                battle.Player.Deck.GetDiscardedCards().Select(card => card.Rank),
                Does.Contain(7));
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.ThreatHammer));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void EP04_I02_EnemyKnifeForcesPlayerDrawWithoutChangingCardOwner()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 3, 4, 10, 2),
                CreateDefinitionDeck(
                    "military-knife-9",
                    "standard-plain-4",
                    "standard-plain-3"),
                enemyMaximumSoul: 5,
                enemyPolicy: new EnforcerEnemyPolicy());
            battle.Start();
            BlackjackCard knife = battle.Enemy.Hand.Cards[0];

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Enemy.Hand.Cards.Contains(knife), Is.True);
            Assert.That(battle.Player.Hand.Cards.Count, Is.EqualTo(4));
            Assert.That(battle.Player.Hand.Cards[3].Rank, Is.EqualTo(10));
            Assert.That(battle.Player.Hand.Cards[3].IsFaceUp, Is.True);
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.MilitaryKnife));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void EP04_U04_KnifeScoreIncludesOneStepPublicBustEstimate()
        {
            EnemyActionCandidate knife = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 3,
                cardDefinitionKey: "military-knife-9");
            EnemyActionCandidate stand = new EnemyActionCandidate(EnemyActionType.Stand);
            var visibleCards = new[]
            {
                new PublicCardObservation("standard-plain-2", 2)
            };
            EnemyDecision highPressure = new EnforcerEnemyPolicy().Decide(
                CreateObservation(
                    ownTotal: 17,
                    candidates: new[] { stand, knife },
                    playerFaceUpCards: visibleCards,
                    inferences: new[] { new EnemyNumberInference(10, 100) }));
            EnemyDecision lowPressure = new EnforcerEnemyPolicy().Decide(
                CreateObservation(
                    ownTotal: 17,
                    candidates: new[] { stand, knife },
                    playerFaceUpCards: visibleCards,
                    inferences: new[] { new EnemyNumberInference(1, 100) }));

            int highScore = GetScore(highPressure, knife);
            int lowScore = GetScore(lowPressure, knife);
            Assert.That(highScore, Is.GreaterThan(lowScore));
            Assert.That(
                highPressure.CandidateScores.Single(score => score.Candidate == knife).ReasonCode,
                Is.EqualTo("enforcer-force-hit-and-evaluate-follow-up"));
        }

        [Test]
        public void EP04_I03_EnforcerVictoryOffersHighGradeThenClearsStage()
        {
            EnemyBattleConfiguration configuration = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.EnforcerKey,
                enemyDeckSeed: 71);
            var progress = new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "enforcer",
                        "집행관",
                        StageKind.NormalCombat,
                        configuration.EnemyMaximumSoul,
                        10,
                        11),
                    new StageDefinition(
                        "final-boss",
                        "최종 보스",
                        StageKind.FinalBossCombat,
                        1,
                        20,
                        21)
                },
                CreateRunPlayer());
            var session = new StageProgressionSession(
                progress,
                (stage, player) => new CoreLoopBattle(
                    CreateRepeatedRankDeck(5, 10, 1),
                    CreateRepeatedRankDeck(25, 4),
                    player.MaximumSoul,
                    player.CurrentSoul,
                    configuration.EnemyMaximumSoul,
                    configuration.BehaviorPolicy),
                rewardTierSelector: stage => configuration.ExpectedRewardTier);

            Assert.That(session.TryStartRun(), Is.True);
            int completedRounds = 0;
            while (session.Progress.State == StageProgressionState.InBattle)
            {
                Assert.That(session.TryPlayerStand(), Is.True);
                completedRounds++;
            }

            Assert.That(completedRounds, Is.EqualTo(3));
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(
                session.Progress.PendingReward.Offer.Tier,
                Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
            Assert.That(session.TrySkipBattleReward(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.StageCleared));
        }

        private static int GetScore(
            EnemyDecision decision,
            EnemyActionCandidate candidate)
        {
            return decision.CandidateScores.Single(
                score => score.Candidate == candidate).Score;
        }

        private static EnemyObservation CreateObservation(
            int ownTotal,
            IReadOnlyList<EnemyActionCandidate> candidates,
            bool playerIsStanding = false,
            IReadOnlyList<PublicCardObservation> playerFaceUpCards = null,
            IReadOnlyList<EnemyNumberInference> inferences = null,
            CardEffectKind? pendingCardEffectKind = null)
        {
            return new EnemyObservation(
                new HandValue(ownTotal),
                Array.Empty<EnemyOwnedCardObservation>(),
                playerFaceUpCards ?? Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(5, 5),
                roundNumber: 1,
                playerIsStanding,
                enemyIsStanding: false,
                ownDeckAvailableCount: 4,
                playerDeckAvailableCount: 4,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                candidates,
                inferences ?? Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind,
                decisionSeed: 397);
        }

        private static PlayerRunState CreateRunPlayer()
        {
            return new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 10),
                    new RunCardDefinition(1, 1)
                });
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

        private static BlackjackDeck CreateRepeatedRankDeck(
            int repeatCount,
            params int[] ranks)
        {
            var cards = new List<BlackjackCard>(repeatCount * ranks.Length);
            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                foreach (int rank in ranks)
                {
                    cards.Add(new BlackjackCard(cards.Count, rank));
                }
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
