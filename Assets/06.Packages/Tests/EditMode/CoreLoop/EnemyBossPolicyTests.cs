using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyBossPolicyTests
    {
        [Test]
        public void EP05_U01_FinalBossUsesDedicatedPolicyAndFixedCombatProfile()
        {
            EnemyBattleConfiguration configuration =
                EnemyBattleConfigurationFactory.Create(
                    EnemyCombatProfileCatalog.FinalBossKey,
                    enemyDeckSeed: 53);

            Assert.That(configuration.Grade, Is.EqualTo(EnemyGrade.Boss));
            Assert.That(configuration.EnemyMaximumSoul, Is.EqualTo(7));
            Assert.That(
                configuration.BehaviorPolicy,
                Is.TypeOf<FinalBossEnemyPolicy>());
            Assert.That(
                configuration.ExpectedRewardTier,
                Is.EqualTo(BattleRewardTier.HighGrade));
        }

        [TestCase(7, FinalBossPhase.Survival)]
        [TestCase(6, FinalBossPhase.Survival)]
        [TestCase(5, FinalBossPhase.Survival)]
        [TestCase(4, FinalBossPhase.Pressure)]
        [TestCase(3, FinalBossPhase.Pressure)]
        [TestCase(2, FinalBossPhase.Execution)]
        [TestCase(1, FinalBossPhase.Execution)]
        public void EP05_U02_PhaseFollowsRemainingSoulBands(
            int currentSoul,
            FinalBossPhase expected)
        {
            Assert.That(
                FinalBossPhaseResolver.Resolve(new SoulObservation(currentSoul, 7)),
                Is.EqualTo(expected));
        }

        [Test]
        public void EP05_U03_ThreePhasesChooseDistinctStrategies()
        {
            EnemyActionCandidate hit = new EnemyActionCandidate(EnemyActionType.Hit);
            EnemyActionCandidate stand = new EnemyActionCandidate(EnemyActionType.Stand);
            EnemyActionCandidate orb = CreateCardCandidate(10, "crystal-orb-5");
            EnemyActionCandidate knife = CreateCardCandidate(11, "military-knife-9");
            EnemyActionCandidate[] candidates = { hit, stand, orb, knife };
            var inferences = new[] { new EnemyNumberInference(7, 50) };

            EnemyDecision survival = new FinalBossEnemyPolicy().Decide(
                CreateObservation(16, 7, candidates, inferences: inferences));
            EnemyDecision pressure = new FinalBossEnemyPolicy().Decide(
                CreateObservation(16, 4, candidates, inferences: inferences));
            var executionPolicy = new FinalBossEnemyPolicy();
            EnemyDecision execution = executionPolicy.Decide(
                CreateObservation(16, 2, candidates, inferences: inferences));

            Assert.That(survival.CardId, Is.EqualTo(orb.CardId));
            Assert.That(pressure.CardId, Is.EqualTo(knife.CardId));
            Assert.That(execution.ActionType, Is.EqualTo(EnemyActionType.Hit));
            Assert.That(
                executionPolicy.CurrentDisplay.TelegraphedAction,
                Is.EqualTo(BossTelegraphedAction.ForcedDraw));
        }

        [Test]
        public void EP05_U04_DisplayExposesOnlyDirectionConfidencePhaseAndTelegraph()
        {
            var policy = new FinalBossEnemyPolicy();
            policy.Decide(CreateObservation(
                ownTotal: 16,
                enemyCurrentSoul: 2,
                candidates: new[]
                {
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand),
                    CreateCardCandidate(11, "military-knife-9")
                },
                inferences: new[]
                {
                    new EnemyNumberInference(8, 60),
                    new EnemyNumberInference(2, 40)
                }));

            BossCombatDisplayModel display = policy.CurrentDisplay;
            Assert.That(display.Phase, Is.EqualTo(FinalBossPhase.Execution));
            Assert.That(
                display.InferenceDirection,
                Is.EqualTo(BossInferenceDirection.HighNumbers));
            Assert.That(display.Confidence, Is.EqualTo(EnemyInferenceConfidence.High));
            Assert.That(
                display.TelegraphedAction,
                Is.EqualTo(BossTelegraphedAction.ForcedDraw));
            Assert.That(
                typeof(BossCombatDisplayModel).GetProperties()
                    .Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    nameof(BossCombatDisplayModel.Confidence),
                    nameof(BossCombatDisplayModel.InferenceDirection),
                    nameof(BossCombatDisplayModel.Phase),
                    nameof(BossCombatDisplayModel.TelegraphedAction)
                }));
        }

        [Test]
        public void EP05_U05_OrbChoosesHighestSafeTemporaryCard()
        {
            EnemyActionCandidate low = CreateOptionCandidate(
                10,
                "crystal-orb-5",
                optionId: 1,
                optionCardId: 20,
                optionCardRank: 4);
            EnemyActionCandidate high = CreateOptionCandidate(
                10,
                "crystal-orb-5",
                optionId: 2,
                optionCardId: 21,
                optionCardRank: 9);
            EnemyDecision decision = new FinalBossEnemyPolicy().Decide(
                CreateObservation(
                    8,
                    7,
                    new[] { low, high },
                    pendingCardEffectKind: CardEffectKind.CrystalOrb));

            Assert.That(decision.CardEffectOptionId, Is.EqualTo(2));
        }

        [Test]
        public void EP05_U06_HammerDiscardsHighestPublicTarget()
        {
            EnemyActionCandidate highTarget = CreateOptionCandidate(
                10,
                "threat-hammer-6",
                optionId: 1,
                optionCardId: 20,
                optionCardRank: 6);
            EnemyActionCandidate lowTarget = CreateOptionCandidate(
                10,
                "threat-hammer-6",
                optionId: 2,
                optionCardId: 21,
                optionCardRank: 2);
            EnemyDecision decision = new FinalBossEnemyPolicy().Decide(
                CreateObservation(
                    16,
                    4,
                    new[] { highTarget, lowTarget },
                    pendingCardEffectKind: CardEffectKind.ThreatHammer));

            Assert.That(decision.CardEffectOptionId, Is.EqualTo(1));
            Assert.That(
                decision.ReasonCode,
                Is.EqualTo("boss-discard-highest-hammer-target"));
        }

        [Test]
        public void EP05_U07_PistolDeclaresMostLikelyNumber()
        {
            EnemyActionCandidate unlikely = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 10,
                cardDefinitionKey: "auto-pistol-7",
                cardEffectOptionId: 1,
                cardEffectOptionNumericValue: 8);
            EnemyActionCandidate likely = new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId: 10,
                cardDefinitionKey: "auto-pistol-7",
                cardEffectOptionId: 2,
                cardEffectOptionNumericValue: 7);
            EnemyDecision decision = new FinalBossEnemyPolicy().Decide(
                CreateObservation(
                    16,
                    2,
                    new[] { unlikely, likely },
                    inferences: new[]
                    {
                        new EnemyNumberInference(7, 80),
                        new EnemyNumberInference(8, 20)
                    },
                    pendingCardEffectKind: CardEffectKind.AutoPistol));

            Assert.That(decision.CardEffectOptionId, Is.EqualTo(2));
        }

        [Test]
        public void EP05_I01_ExecutionPhaseTelegraphsThenUsesStrongCard()
        {
            var policy = new FinalBossEnemyPolicy();
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 3, 4, 2, 10),
                CreateDefinitionDeck(
                    "military-knife-9",
                    "standard-plain-4",
                    "standard-plain-3"),
                enemyMaximumSoul: 7,
                enemyPolicy: policy);
            battle.Start();
            battle.Enemy.Soul.ApplyDamage(5);
            BlackjackCard knife = battle.Enemy.Hand.Cards[0];

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastEnemyDecision.ActionType, Is.EqualTo(EnemyActionType.Hit));
            Assert.That(
                policy.CurrentDisplay.TelegraphedAction,
                Is.EqualTo(BossTelegraphedAction.ForcedDraw));
            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Available));

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastEnemyDecision.ActionType, Is.EqualTo(EnemyActionType.UseCard));
            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Player.Hand.Cards.Last().Rank, Is.EqualTo(2));
            Assert.That(
                battle.Player.Deck.GetDiscardedCards().Select(card => card.Rank),
                Does.Contain(10));
            Assert.That(battle.LastCardEffectActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.MilitaryKnife));
            Assert.That(
                policy.CurrentDisplay.TelegraphedAction,
                Is.EqualTo(BossTelegraphedAction.None));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EP05_I02_FinalBossRewardCompletesRunAndRestartRestoresBoss(
            bool selectReward)
        {
            var progress = new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "final-boss",
                        "최종 보스",
                        StageKind.FinalBossCombat,
                        7,
                        20,
                        21)
                },
                CreateRunPlayer());
            var session = new StageProgressionSession(
                progress,
                (stage, player) => new CoreLoopBattle(
                    CreateRepeatedRankDeck(4, 10, 1),
                    CreateRepeatedRankDeck(18, 4),
                    player.MaximumSoul,
                    player.CurrentSoul,
                    enemyMaximumSoul: 7,
                    enemyPolicy: new FinalBossEnemyPolicy()));

            Assert.That(session.TryStartRun(), Is.True);
            int completedRounds = 0;
            while (session.Progress.State == StageProgressionState.InBattle)
            {
                Assert.That(session.TryPlayerStand(), Is.True);
                completedRounds++;
            }

            Assert.That(completedRounds, Is.EqualTo(4));
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(
                session.Progress.PendingReward.Offer.Tier,
                Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.RunVictory));

            bool rewardCompleted = selectReward
                ? session.TrySelectBattleReward(
                    session.Progress.PendingReward.Offer.Options[0].OptionId)
                : session.TrySkipBattleReward();

            Assert.That(rewardCompleted, Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(session.TryRestartRun(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Battle.Enemy.Soul.Maximum, Is.EqualTo(7));
            Assert.That(session.Battle.Enemy.Soul.Current, Is.EqualTo(7));
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

        private static EnemyActionCandidate CreateOptionCandidate(
            int cardId,
            string definitionKey,
            int optionId,
            int optionCardId,
            int optionCardRank)
        {
            return new EnemyActionCandidate(
                EnemyActionType.UseCard,
                cardId,
                definitionKey,
                optionId,
                cardEffectOptionCardId: optionCardId,
                cardEffectOptionCardRank: optionCardRank);
        }

        private static EnemyObservation CreateObservation(
            int ownTotal,
            int enemyCurrentSoul,
            IReadOnlyList<EnemyActionCandidate> candidates,
            IReadOnlyList<EnemyNumberInference> inferences = null,
            CardEffectKind? pendingCardEffectKind = null,
            IReadOnlyList<PublicCombatAction> publicActionHistory = null)
        {
            return new EnemyObservation(
                new HandValue(ownTotal),
                Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(enemyCurrentSoul, 7),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 8,
                playerDeckAvailableCount: 8,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                publicActionHistory ?? Array.Empty<PublicCombatAction>(),
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
                Enumerable.Range(0, 8)
                    .Select(index => new RunCardDefinition(
                        index,
                        index % 2 == 0 ? 10 : 1)));
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
