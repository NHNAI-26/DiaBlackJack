using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class AutomaticCardRevisionIntegrationTests
    {
        private const string SavedAtUtc = "2026-07-28T00:00:00.0000000+00:00";

        [Test]
        public void ACRV01_I01_NormalRewardsUseOnlyRevisedAutomaticKeys()
        {
            IReadOnlyList<string> normal = BattleRewardCatalog.CreateDefault()
                .GetDefinitionKeys(BattleRewardTier.Normal);
            string[] expectedKeys =
            {
                "poison-1",
                "resurrection-herb-2",
                "lie-detector-3",
                "flamethrower-4",
                "pocket-watch-5"
            };

            Assert.That(expectedKeys.All(normal.Contains), Is.True);
            Assert.That(normal.Contains("poison-2"), Is.False);
            Assert.That(normal.Contains("flamethrower-9"), Is.False);
            Assert.That(normal.Contains("pocket-watch-9"), Is.False);
        }

        [Test]
        public void ACRV01_I02_TricksterDeckKeepsRevisedLieDetectorReferences()
        {
            EnemyCombatProfile trickster = EnemyCombatProfileCatalog.Default.GetByKey(
                EnemyCombatProfileCatalog.TricksterKey);

            Assert.That(
                trickster.DeckDefinitionKeys.Count(
                    key => key == "lie-detector-3"),
                Is.EqualTo(3));
            Assert.That(
                trickster.DeckDefinitionKeys.Any(IsLegacyAutomaticKey),
                Is.False);
        }

        [Test]
        public void ACRV01_I03_RunCardsReachBattleWithRevisedRanks()
        {
            string[] keys =
            {
                "poison-1",
                "resurrection-herb-2",
                "lie-detector-3",
                "flamethrower-4",
                "pocket-watch-5"
            };
            PlayerRunState player = new PlayerRunState(
                12,
                12,
                keys.Select((key, id) => new RunCardDefinition(id, key)),
                new RunDemonDefinition[0]);
            StageDefinition stage = CreateStages()[0];

            CoreLoopBattle battle = StageBattleFactory.Create(stage, player);
            IReadOnlyList<int> counts = battle.Player.Deck.GetKnownRankCounts();

            for (int rank = 1; rank <= 5; rank++)
            {
                Assert.That(counts[rank], Is.EqualTo(1));
            }
        }

        [Test]
        public void ACRV01_I04_ContentRevisionRejectsLegacyAutomaticKeys()
        {
            Assert.That(
                RunSaveSnapshot.CurrentContentRevision,
                Is.EqualTo("prototype-v2"));
            RunSaveSnapshot legacy = new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                "prototype-v1",
                1,
                "acrv01-legacy",
                SavedAtUtc,
                RunCheckpointKind.CombatSettlementCompleted,
                RunSaveStatus.InProgress,
                28,
                0,
                "normal-1",
                RunNextContentKind.Shop,
                new PlayerRunSaveSnapshot(
                    12,
                    12,
                    0,
                    0,
                    0,
                    DemonContractCatalog.SatanKey,
                    new[]
                    {
                        new RunSaveCardSnapshot(
                            0,
                            "poison-2",
                            CardSuit.Spade)
                    },
                    new[]
                    {
                        new RunSaveDemonSnapshot(
                            0,
                            DemonContractCatalog.SatanKey)
                    }),
                new RunRandomSaveSnapshot(0, 0, 0, 0, null),
                new string[0],
                new string[0]);

            RunSaveValidationResult validation =
                RunSaveValidator.Validate(legacy, CreateStages());

            Assert.That(validation.IsValid, Is.False);
            Assert.That(
                validation.Error,
                Is.EqualTo(RunSaveValidationError.IncompatibleContentRevision));
        }

        private static bool IsLegacyAutomaticKey(string key)
        {
            return key == "poison-2" ||
                key == "flamethrower-9" ||
                key == "pocket-watch-9";
        }

        private static IReadOnlyList<StageDefinition> CreateStages()
        {
            return new[]
            {
                new StageDefinition(
                    "normal-1",
                    "Normal",
                    StageKind.NormalCombat,
                    3,
                    17,
                    19),
                StageDefinition.CreateForEnemyProfile(
                    "boss",
                    "Boss",
                    StageKind.FinalBossCombat,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    23,
                    29)
            };
        }
    }
}
