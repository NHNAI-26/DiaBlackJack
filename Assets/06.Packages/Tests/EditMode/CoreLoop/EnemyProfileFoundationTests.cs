using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyProfileFoundationTests
    {
        [Test]
        public void EP01_U01_DefaultCatalogContainsSixPlannedProfiles()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;

            Assert.That(catalog.Profiles.Count, Is.EqualTo(6));
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                EnemyGrade.Normal,
                3,
                EnemyBehaviorPolicyCatalog.CowardlyGambler);
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.GunslingerKey,
                EnemyGrade.Normal,
                3,
                EnemyBehaviorPolicyCatalog.Gunslinger);
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.CultistKey,
                EnemyGrade.Normal,
                5,
                EnemyBehaviorPolicyCatalog.Cultist);
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.TricksterKey,
                EnemyGrade.Normal,
                5,
                EnemyBehaviorPolicyCatalog.Trickster);
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.EnforcerKey,
                EnemyGrade.Elite,
                5,
                EnemyBehaviorPolicyCatalog.Enforcer);
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.FinalBossKey,
                EnemyGrade.Boss,
                8,
                EnemyBehaviorPolicyCatalog.FinalBoss);
        }

        [TestCase(EnemyCombatProfileCatalog.CowardlyGamblerKey, 3, 18, BattleRewardTier.Normal)]
        [TestCase(EnemyCombatProfileCatalog.GunslingerKey, 3, 14, BattleRewardTier.Normal)]
        [TestCase(EnemyCombatProfileCatalog.CultistKey, 5, 20, BattleRewardTier.Normal)]
        [TestCase(EnemyCombatProfileCatalog.TricksterKey, 5, 18, BattleRewardTier.Normal)]
        [TestCase(EnemyCombatProfileCatalog.EnforcerKey, 5, 19, BattleRewardTier.HighGrade)]
        [TestCase(EnemyCombatProfileCatalog.FinalBossKey, 8, 25, BattleRewardTier.HighGrade)]
        public void EPR04_U01_RevisedProfilesFlowThroughBattleConfiguration(
            string profileKey,
            int expectedMaximumSoul,
            int expectedDeckCount,
            BattleRewardTier expectedRewardTier)
        {
            EnemyCombatProfile profile = EnemyCombatProfileCatalog.Default.GetByKey(profileKey);
            EnemyBattleConfiguration configuration = EnemyBattleConfigurationFactory.Create(
                profileKey,
                enemyDeckSeed: 20260729);

            Assert.That(profile.MaximumSoul, Is.EqualTo(expectedMaximumSoul));
            Assert.That(profile.DeckDefinitionKeys.Count, Is.EqualTo(expectedDeckCount));
            Assert.That(configuration.EnemyMaximumSoul, Is.EqualTo(expectedMaximumSoul));
            Assert.That(configuration.EnemyDeckDefinitions.Count, Is.EqualTo(expectedDeckCount));
            Assert.That(configuration.ExpectedRewardTier, Is.EqualTo(expectedRewardTier));
            Assert.That(configuration.CreateEnemyDeck().TotalCardCount, Is.EqualTo(expectedDeckCount));
        }

        [Test]
        public void EPR04_U02_RevisedDecksMatchEveryAuthoredCardCount()
        {
            AssertDeckCounts(
                EnemyCombatProfileCatalog.GunslingerKey,
                ("standard-ace-1", 1), ("standard-plain-2", 1),
                ("standard-plain-3", 1), ("standard-plain-4", 1),
                ("crystal-orb-5", 1), ("threat-hammer-6", 1),
                ("auto-pistol-7", 4), ("auto-pistol-8", 4));
            AssertDeckCounts(
                EnemyCombatProfileCatalog.CultistKey,
                ("standard-ace-1", 2), ("standard-plain-2", 2),
                ("standard-plain-3", 2), ("standard-plain-4", 2),
                ("crystal-orb-5", 2), ("threat-hammer-6", 2),
                ("auto-pistol-7", 2), ("auto-pistol-8", 2),
                ("military-knife-9", 2), ("military-knife-10", 2));
            AssertDeckCounts(
                EnemyCombatProfileCatalog.TricksterKey,
                ("standard-ace-1", 3), ("standard-plain-2", 3),
                (CardDefinitionCatalog.LieDetectorKey, 3), ("standard-plain-4", 3),
                ("crystal-orb-5", 3), ("threat-hammer-6", 1),
                ("auto-pistol-7", 2));
            AssertDeckCounts(
                EnemyCombatProfileCatalog.EnforcerKey,
                (CardDefinitionCatalog.PoisonKey, 3), ("standard-ace-1", 3),
                ("standard-plain-2", 2), (CardDefinitionCatalog.LieDetectorKey, 2),
                (CardDefinitionCatalog.FlamethrowerKey, 2),
                (CardDefinitionCatalog.PocketWatchKey, 2),
                ("threat-hammer-6", 1), ("auto-pistol-7", 1),
                ("auto-pistol-8", 1), ("military-knife-9", 1),
                ("military-knife-10", 1));
            AssertDeckCounts(
                EnemyCombatProfileCatalog.FinalBossKey,
                ("standard-ace-1", 10), ("crystal-orb-5", 5),
                ("threat-hammer-6", 2), ("auto-pistol-7", 4),
                ("military-knife-9", 4));
        }

        [Test]
        public void EP01_U02_ProfileRejectsEmptyIdentityAndInvalidEnums()
        {
            Assert.Throws<ArgumentException>(() => CreateProfile(key: " "));
            Assert.Throws<ArgumentException>(() => CreateProfile(displayName: ""));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProfile(grade: (EnemyGrade)999));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProfile(playerInformationMode: (EnemyInformationMode)999));
            Assert.Throws<ArgumentException>(() => CreateProfile(summary: null));
        }

        [Test]
        public void EP01_U02_ProfileRejectsInvalidSoulDeckAndPolicy()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateProfile(maximumSoul: 0));
            Assert.Throws<ArgumentException>(() => CreateProfile(behaviorPolicyKey: ""));
            Assert.Throws<KeyNotFoundException>(
                () => CreateProfile(behaviorPolicyKey: "unknown-policy"));
            Assert.Throws<ArgumentNullException>(() => new EnemyCombatProfile(
                "test-enemy",
                "테스트 적",
                EnemyGrade.Normal,
                3,
                EnemyBehaviorPolicyCatalog.Simple,
                null,
                "테스트 성향",
                EnemyInformationMode.Standard));
            Assert.Throws<ArgumentException>(
                () => CreateProfile(deckDefinitionKeys: Array.Empty<string>()));
            Assert.Throws<ArgumentException>(
                () => CreateProfile(deckDefinitionKeys: new[] { "" }));
            Assert.Throws<KeyNotFoundException>(
                () => CreateProfile(deckDefinitionKeys: new[] { "unknown-card" }));
        }

        [Test]
        public void EP01_U02_CatalogRejectsNullEmptyAndDuplicateProfiles()
        {
            EnemyCombatProfile profile = CreateProfile();

            Assert.Throws<ArgumentNullException>(() => new EnemyCombatProfileCatalog(null));
            Assert.Throws<ArgumentException>(
                () => new EnemyCombatProfileCatalog(Array.Empty<EnemyCombatProfile>()));
            Assert.Throws<ArgumentException>(
                () => new EnemyCombatProfileCatalog(new EnemyCombatProfile[] { null }));
            Assert.Throws<ArgumentException>(
                () => new EnemyCombatProfileCatalog(new[] { profile, profile }));
        }

        [Test]
        public void EP01_U02_ProfileCopiesDeckDefinitionInput()
        {
            var input = new List<string> { "standard-ace-1" };
            EnemyCombatProfile profile = CreateProfile(deckDefinitionKeys: input);

            input[0] = "auto-pistol-7";

            Assert.That(profile.DeckDefinitionKeys.Count, Is.EqualTo(1));
            Assert.That(profile.DeckDefinitionKeys[0], Is.EqualTo("standard-ace-1"));
        }

        [Test]
        public void EP01_U03_PreviewContainsOnlySelectionSafeValues()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;
            EnemyProfilePreview preview = catalog.GetPreviewByKey(
                EnemyCombatProfileCatalog.EnforcerKey);

            Assert.That(preview.ProfileKey, Is.EqualTo(EnemyCombatProfileCatalog.EnforcerKey));
            Assert.That(preview.DisplayName, Is.EqualTo("집행자"));
            Assert.That(preview.Grade, Is.EqualTo(EnemyGrade.Elite));
            Assert.That(preview.MaximumSoul, Is.EqualTo(5));
            Assert.That(preview.Summary, Is.Not.Empty);
            Assert.That(preview.ExpectedRewardTier, Is.EqualTo(BattleRewardTier.HighGrade));

            string[] propertyNames = typeof(EnemyProfilePreview)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(propertyNames, Is.EqualTo(new[]
            {
                nameof(EnemyProfilePreview.DisplayName),
                nameof(EnemyProfilePreview.ExpectedRewardTier),
                nameof(EnemyProfilePreview.Grade),
                nameof(EnemyProfilePreview.MaximumSoul),
                nameof(EnemyProfilePreview.ProfileKey),
                nameof(EnemyProfilePreview.Summary)
            }));
        }

        [Test]
        public void EP01_U03_NormalAndBossPreviewsDeriveRewardTierFromGrade()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;

            Assert.That(
                catalog.GetPreviewByKey(EnemyCombatProfileCatalog.GunslingerKey)
                    .ExpectedRewardTier,
                Is.EqualTo(BattleRewardTier.Normal));
            Assert.That(
                catalog.GetPreviewByKey(EnemyCombatProfileCatalog.FinalBossKey)
                    .ExpectedRewardTier,
                Is.EqualTo(BattleRewardTier.HighGrade));
        }

        [Test]
        public void EP01_U04_SimplePolicyPreservesLegacyBoundaryThroughNewInterface()
        {
            var policy = new SimpleEnemyPolicy();

            Assert.That(policy.Decide(new HandValue(16)), Is.EqualTo(EnemyAction.Hit));
            Assert.That(policy.Decide(new HandValue(17)), Is.EqualTo(EnemyAction.Stand));

            IEnemyBehaviorPolicy replacementBoundary = policy;
            Assert.That(
                replacementBoundary.Decide(new EnemyObservation(new HandValue(16))).ActionType,
                Is.EqualTo(EnemyActionType.Hit));
            Assert.That(
                replacementBoundary.Decide(new EnemyObservation(new HandValue(17))).ActionType,
                Is.EqualTo(EnemyActionType.Stand));
        }

        [Test]
        public void EP01_U04_CoreLoopAcceptsReplacementPolicyWithoutChangingDefaultApi()
        {
            var battle = new CoreLoopBattle(
                CreateDeck(5, 5, 2),
                CreateDeck(8, 8, 1),
                enemyPolicy: new AlwaysStandPolicy());
            battle.Start();

            battle.TryPlayerHit();

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.IsStanding, Is.True);
        }

        [Test]
        public void EP01_U05_SameProfileAndSeedCreateSameBattleConfiguration()
        {
            EnemyBattleConfiguration first = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.FinalBossKey,
                enemyDeckSeed: 71);
            EnemyBattleConfiguration second = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.FinalBossKey,
                enemyDeckSeed: 71);

            Assert.That(second.ProfileKey, Is.EqualTo(first.ProfileKey));
            Assert.That(second.Grade, Is.EqualTo(first.Grade));
            Assert.That(second.EnemyMaximumSoul, Is.EqualTo(first.EnemyMaximumSoul));
            Assert.That(second.EnemyDeckSeed, Is.EqualTo(first.EnemyDeckSeed));
            Assert.That(second.ExpectedRewardTier, Is.EqualTo(first.ExpectedRewardTier));
            Assert.That(second.BehaviorPolicy.GetType(), Is.EqualTo(first.BehaviorPolicy.GetType()));
            Assert.That(
                second.EnemyDeckDefinitions.Select(definition => definition.Key),
                Is.EqualTo(first.EnemyDeckDefinitions.Select(definition => definition.Key)));
            Assert.That(
                DrawAllDefinitionKeys(second.CreateEnemyDeck(), second.EnemyDeckDefinitions.Count),
                Is.EqualTo(DrawAllDefinitionKeys(
                    first.CreateEnemyDeck(),
                    first.EnemyDeckDefinitions.Count)));
        }

        [Test]
        public void EP01_U05_UnknownProfileDoesNotFallBackToDefaultEnemy()
        {
            Assert.Throws<KeyNotFoundException>(
                () => EnemyBattleConfigurationFactory.Create("missing-profile", 1));
        }

        [Test]
        public void EnemyDecisionRejectsUnknownActionAndEmptyReason()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new EnemyDecision((EnemyActionType)999, "reason"));
            Assert.Throws<ArgumentException>(
                () => new EnemyDecision(EnemyActionType.Hit, " "));
        }

        private static void AssertProfile(
            EnemyCombatProfileCatalog catalog,
            string key,
            EnemyGrade expectedGrade,
            int expectedMaximumSoul,
            string expectedPolicyKey)
        {
            EnemyCombatProfile profile = catalog.GetByKey(key);

            Assert.That(profile.Key, Is.EqualTo(key));
            Assert.That(profile.Grade, Is.EqualTo(expectedGrade));
            Assert.That(profile.MaximumSoul, Is.EqualTo(expectedMaximumSoul));
            Assert.That(profile.BehaviorPolicyKey, Is.EqualTo(expectedPolicyKey));
            Assert.That(profile.DeckDefinitionKeys, Is.Not.Empty);
        }

        private static void AssertDeckCounts(
            string profileKey,
            params (string Key, int Count)[] expectedCounts)
        {
            EnemyCombatProfile profile = EnemyCombatProfileCatalog.Default.GetByKey(profileKey);
            Dictionary<string, int> actualCounts = profile.DeckDefinitionKeys
                .GroupBy(key => key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            Assert.That(actualCounts.Count, Is.EqualTo(expectedCounts.Length), profileKey);
            foreach ((string key, int expectedCount) in expectedCounts)
            {
                Assert.That(actualCounts.ContainsKey(key), Is.True, $"{profileKey}:{key}");
                Assert.That(actualCounts[key], Is.EqualTo(expectedCount), $"{profileKey}:{key}");
            }
        }

        private static EnemyCombatProfile CreateProfile(
            string key = "test-enemy",
            string displayName = "테스트 적",
            EnemyGrade grade = EnemyGrade.Normal,
            int maximumSoul = 3,
            string behaviorPolicyKey = EnemyBehaviorPolicyCatalog.Simple,
            IEnumerable<string> deckDefinitionKeys = null,
            string summary = "테스트 성향",
            EnemyInformationMode playerInformationMode = EnemyInformationMode.Standard)
        {
            return new EnemyCombatProfile(
                key,
                displayName,
                grade,
                maximumSoul,
                behaviorPolicyKey,
                deckDefinitionKeys ?? new[] { "standard-ace-1" },
                summary,
                playerInformationMode);
        }

        private static BlackjackDeck CreateDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static IReadOnlyList<string> DrawAllDefinitionKeys(
            BlackjackDeck deck,
            int count)
        {
            var keys = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                keys.Add(deck.Draw().DefinitionKey);
            }

            return keys;
        }

        private sealed class AlwaysStandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "test-stand");
            }
        }
    }
}
