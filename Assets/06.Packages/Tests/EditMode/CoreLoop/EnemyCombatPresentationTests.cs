using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyCombatPresentationTests
    {
        [Test]
        public void EUI04_U01_NormalSnapshotShowsSortedTopThreeProbabilities()
        {
            CoreLoopBattle battle = CreateInferenceBattle();

            EnemyCombatDisplaySnapshot snapshot =
                EnemyCombatDisplaySnapshotFactory.Create(
                    battle,
                    EnemyCombatProfileCatalog.GunslingerKey);

            Assert.That(snapshot.Grade, Is.EqualTo(EnemyGrade.Normal));
            Assert.That(snapshot.InferenceEntries.Count, Is.EqualTo(3));
            AssertEntry(snapshot.InferenceEntries[0], 4, 40);
            AssertEntry(snapshot.InferenceEntries[1], 3, 30);
            AssertEntry(snapshot.InferenceEntries[2], 2, 20);
            Assert.That(snapshot.Confidence, Is.Null);
        }

        [Test]
        public void EUI04_U02_NormalSnapshotMatchesSharedPolicyInference()
        {
            CoreLoopBattle battle = CreateInferenceBattle();
            IReadOnlyList<EnemyNumberInference> policyInference =
                EnemyObservationFactory.CreateNumberInferences(battle);

            EnemyCombatDisplaySnapshot snapshot =
                EnemyCombatDisplaySnapshotFactory.Create(
                    battle,
                    EnemyCombatProfileCatalog.CultistKey);

            for (int i = 0; i < snapshot.InferenceEntries.Count; i++)
            {
                Assert.That(
                    snapshot.InferenceEntries[i].Number,
                    Is.EqualTo(policyInference[i].Number));
                Assert.That(
                    snapshot.InferenceEntries[i].ProbabilityPercent,
                    Is.EqualTo(policyInference[i].ProbabilityPercent));
            }
        }

        [Test]
        public void EUI04_U03_EliteSnapshotHidesRawProbability()
        {
            CoreLoopBattle battle = CreateInferenceBattle(
                enemyMaximumSoul: 5);

            EnemyCombatDisplaySnapshot snapshot =
                EnemyCombatDisplaySnapshotFactory.Create(
                    battle,
                    EnemyCombatProfileCatalog.EnforcerKey);

            Assert.That(snapshot.Grade, Is.EqualTo(EnemyGrade.Elite));
            Assert.That(
                snapshot.InferenceEntries.Select(entry => entry.Number),
                Is.EqualTo(new[] { 4, 3 }));
            Assert.That(
                snapshot.InferenceEntries.All(
                    entry => !entry.ProbabilityPercent.HasValue),
                Is.True);
            Assert.That(
                snapshot.Confidence,
                Is.EqualTo(EnemyInferenceConfidence.Medium));
        }

        [Test]
        public void EUI04_U04_BossSnapshotExistsBeforeFirstPolicyDecision()
        {
            CoreLoopBattle battle = CreateInferenceBattle(
                new FinalBossEnemyPolicy(),
                enemyMaximumSoul: 7);

            EnemyCombatDisplaySnapshot snapshot =
                EnemyCombatDisplaySnapshotFactory.Create(
                    battle,
                    EnemyCombatProfileCatalog.FinalBossKey);

            Assert.That(snapshot.Grade, Is.EqualTo(EnemyGrade.Boss));
            Assert.That(snapshot.BossPhase, Is.EqualTo(FinalBossPhase.Survival));
            Assert.That(
                snapshot.BossInferenceDirection,
                Is.EqualTo(BossInferenceDirection.LowNumbers));
            Assert.That(
                snapshot.Confidence,
                Is.EqualTo(EnemyInferenceConfidence.Medium));
            Assert.That(
                snapshot.BossTelegraphedAction,
                Is.EqualTo(BossTelegraphedAction.None));
            Assert.That(snapshot.InferenceEntries, Is.Empty);
        }

        [Test]
        public void EUI04_U05_BossWarningTracksTelegraphAndExecution()
        {
            CoreLoopBattle battle = CreateBossTelegraphBattle();

            Assert.That(battle.TryPlayerHit(), Is.True);
            CoreLoopViewModel warned = CoreLoopPresenter.Create(
                battle,
                EnemyCombatProfileCatalog.FinalBossKey);

            Assert.That(warned.EnemyWarning, Is.EqualTo(
                "WARNING · FORCED DRAW PREPARED"));
            Assert.That(warned.EnemyInformationLines, Does.Contain("PHASE EXECUTION"));

            Assert.That(battle.TryPlayerHit(), Is.True);
            CoreLoopViewModel executed = CoreLoopPresenter.Create(
                battle,
                EnemyCombatProfileCatalog.FinalBossKey);

            Assert.That(executed.EnemyWarning, Is.Empty);
            Assert.That(
                battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.MilitaryKnife));
        }

        [Test]
        public void EUI04_U06_NoHiddenCardProducesSafeUnavailableInference()
        {
            CoreLoopBattle battle = CreateInferenceBattle();
            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            EnemyCombatDisplaySnapshot snapshot =
                EnemyCombatDisplaySnapshotFactory.Create(
                    battle,
                    EnemyCombatProfileCatalog.TricksterKey);
            CoreLoopViewModel model = CoreLoopPresenter.Create(
                battle,
                EnemyCombatProfileCatalog.TricksterKey);

            Assert.That(snapshot.InferenceEntries, Is.Empty);
            Assert.That(model.EnemyInformationLines, Is.EqualTo(
                new[] { "NO PUBLIC INFERENCE" }));
        }

        [Test]
        public void EUI04_U07_ProfilelessBattleUsesExplicitCompatibilityDisplay()
        {
            CoreLoopBattle battle = CreateInferenceBattle();

            EnemyCombatDisplaySnapshot snapshot =
                EnemyCombatDisplaySnapshotFactory.Create(battle, null);
            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(snapshot.HasProfile, Is.False);
            Assert.That(snapshot.ProfileKey, Is.Null);
            Assert.That(snapshot.InferenceEntries, Is.Empty);
            Assert.That(model.EnemyDisplayName, Is.EqualTo("UNPROFILED ENEMY"));
            Assert.That(model.EnemyGrade, Is.EqualTo("UNPROFILED"));
            Assert.That(model.EnemyInformationTitle, Is.EqualTo("ENEMY INFORMATION"));
            Assert.That(model.EnemyInformationLines, Is.EqualTo(
                new[] { "NO PROFILE INFORMATION" }));
        }

        [Test]
        public void EUI04_U08_NormalPresenterShowsProfileAndThreeProbabilities()
        {
            CoreLoopViewModel model = CoreLoopPresenter.Create(
                CreateInferenceBattle(),
                EnemyCombatProfileCatalog.GunslingerKey);

            Assert.That(model.EnemyDisplayName, Is.EqualTo("총잡이"));
            Assert.That(model.EnemyGrade, Is.EqualTo("NORMAL"));
            Assert.That(model.EnemySummary, Does.Contain("숫자를 추측"));
            Assert.That(model.EnemyInformationTitle, Is.EqualTo("INFERENCE"));
            Assert.That(model.EnemyInformationLines, Is.EqualTo(
                new[] { "4  40%", "3  30%", "2  20%" }));
            Assert.That(model.EnemyWarning, Is.Empty);
        }

        [Test]
        public void EUI04_U09_ElitePresenterShowsLikelyNumbersWithoutPercentages()
        {
            CoreLoopViewModel model = CoreLoopPresenter.Create(
                CreateInferenceBattle(enemyMaximumSoul: 5),
                EnemyCombatProfileCatalog.EnforcerKey);

            Assert.That(model.EnemyDisplayName, Is.EqualTo("집행관"));
            Assert.That(model.EnemyGrade, Is.EqualTo("ELITE"));
            Assert.That(model.EnemyInformationTitle, Is.EqualTo("ELITE INFERENCE"));
            Assert.That(model.EnemyInformationLines, Is.EqualTo(
                new[] { "LIKELY 4 · 3", "CONFIDENCE MEDIUM" }));
            Assert.That(
                model.EnemyInformationLines.All(line => !line.Contains("%")),
                Is.True);
        }

        [Test]
        public void EUI04_U10_BossPresenterShowsPhaseDirectionAndConfidence()
        {
            CoreLoopViewModel model = CoreLoopPresenter.Create(
                CreateInferenceBattle(
                    new FinalBossEnemyPolicy(),
                    enemyMaximumSoul: 7),
                EnemyCombatProfileCatalog.FinalBossKey);

            Assert.That(model.EnemyDisplayName, Is.EqualTo("최종 보스"));
            Assert.That(model.EnemyGrade, Is.EqualTo("BOSS"));
            Assert.That(model.EnemyInformationTitle, Is.EqualTo("BOSS PATTERN"));
            Assert.That(model.EnemyInformationLines, Is.EqualTo(new[]
            {
                "PHASE SURVIVAL",
                "DIRECTION LOW NUMBERS",
                "CONFIDENCE MEDIUM"
            }));
            Assert.That(model.EnemyInformationLines.All(
                line => !line.Contains("%")), Is.True);
        }

        [Test]
        public void EUI04_U11_PresentationNeverCallsEnemyPolicy()
        {
            var policy = new ThrowingCountingPolicy();
            CoreLoopBattle battle = CreateInferenceBattle(policy);

            EnemyCombatDisplaySnapshotFactory.Create(
                battle,
                EnemyCombatProfileCatalog.GunslingerKey);
            CoreLoopPresenter.Create(
                battle,
                EnemyCombatProfileCatalog.GunslingerKey);

            Assert.That(policy.DecisionCount, Is.Zero);
        }

        [Test]
        public void EUI04_I01_PlayerActionRefreshesPublicInference()
        {
            CoreLoopBattle battle = CreateInferenceUpdateBattle();
            CoreLoopViewModel before = CoreLoopPresenter.Create(
                battle,
                EnemyCombatProfileCatalog.GunslingerKey);

            Assert.That(battle.TryPlayerHit(), Is.True);
            CoreLoopViewModel after = CoreLoopPresenter.Create(
                battle,
                EnemyCombatProfileCatalog.GunslingerKey);

            Assert.That(after.EnemyInformationLines,
                Is.Not.EqualTo(before.EnemyInformationLines));
            Assert.That(after.EnemyInformationLines, Is.EqualTo(
                new[] { "5  50%", "4  37%", "1  13%" }));
        }

        [Test]
        public void EUI04_I02_SnapshotShapeContainsNoPrivateCombatObjects()
        {
            string[] snapshotProperties = typeof(EnemyCombatDisplaySnapshot)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] entryProperties = typeof(EnemyInferenceDisplayEntry)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(snapshotProperties, Is.EqualTo(new[]
            {
                nameof(EnemyCombatDisplaySnapshot.BossInferenceDirection),
                nameof(EnemyCombatDisplaySnapshot.BossPhase),
                nameof(EnemyCombatDisplaySnapshot.BossTelegraphedAction),
                nameof(EnemyCombatDisplaySnapshot.Confidence),
                nameof(EnemyCombatDisplaySnapshot.DisplayName),
                nameof(EnemyCombatDisplaySnapshot.Grade),
                nameof(EnemyCombatDisplaySnapshot.HasProfile),
                nameof(EnemyCombatDisplaySnapshot.InferenceEntries),
                nameof(EnemyCombatDisplaySnapshot.InformationMode),
                nameof(EnemyCombatDisplaySnapshot.ProfileKey),
                nameof(EnemyCombatDisplaySnapshot.Summary)
            }));
            Assert.That(entryProperties, Is.EqualTo(new[]
            {
                nameof(EnemyInferenceDisplayEntry.Number),
                nameof(EnemyInferenceDisplayEntry.ProbabilityPercent)
            }));
            Assert.That(
                typeof(EnemyCombatDisplaySnapshot).GetProperties()
                    .Any(property =>
                        property.PropertyType == typeof(BlackjackCard) ||
                        property.PropertyType == typeof(EnemyObservation) ||
                        property.PropertyType == typeof(EnemyDecision) ||
                        typeof(IEnemyBehaviorPolicy).IsAssignableFrom(
                            property.PropertyType)),
                Is.False);
        }

        [Test]
        public void EUI04_U12_UnknownProfileDoesNotInventFallbackInformation()
        {
            CoreLoopBattle battle = CreateInferenceBattle();

            Assert.Throws<KeyNotFoundException>(() =>
                EnemyCombatDisplaySnapshotFactory.Create(
                    battle,
                    "missing-profile"));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        private static CoreLoopBattle CreateInferenceBattle(
            IEnemyBehaviorPolicy policy = null,
            int enemyMaximumSoul = 3)
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(10, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4),
                CreateRankDeck(10, 7, 10, 7, 10, 7),
                playerMaximumSoul: 12,
                enemyMaximumSoul,
                policy);
            battle.Start();
            return battle;
        }

        private static CoreLoopBattle CreateInferenceUpdateBattle()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 1, 3, 4, 4, 4, 5, 5, 5, 5),
                CreateRankDeck(10, 7, 10, 7),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3,
                new SimpleEnemyPolicy());
            battle.Start();
            return battle;
        }

        private static CoreLoopBattle CreateBossTelegraphBattle()
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
            return battle;
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

        private static BlackjackDeck CreateDefinitionDeck(
            params string[] definitionKeys)
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

        private static void AssertEntry(
            EnemyInferenceDisplayEntry entry,
            int expectedNumber,
            int expectedProbability)
        {
            Assert.That(entry.Number, Is.EqualTo(expectedNumber));
            Assert.That(
                entry.ProbabilityPercent,
                Is.EqualTo(expectedProbability));
        }

        private sealed class ThrowingCountingPolicy : IEnemyBehaviorPolicy
        {
            public int DecisionCount { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecisionCount++;
                throw new InvalidOperationException(
                    "Presentation must not call the enemy policy.");
            }
        }
    }
}
