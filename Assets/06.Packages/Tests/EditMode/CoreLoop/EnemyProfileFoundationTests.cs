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
        public void EP01_U01_DefaultCatalogContainsFivePlannedProfiles()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;

            Assert.That(catalog.Profiles.Count, Is.EqualTo(5));
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
                3,
                EnemyBehaviorPolicyCatalog.Cultist);
            AssertProfile(
                catalog,
                EnemyCombatProfileCatalog.TricksterKey,
                EnemyGrade.Normal,
                4,
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
                7,
                EnemyBehaviorPolicyCatalog.FinalBoss);
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
            Assert.That(preview.DisplayName, Is.EqualTo("집행관"));
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
