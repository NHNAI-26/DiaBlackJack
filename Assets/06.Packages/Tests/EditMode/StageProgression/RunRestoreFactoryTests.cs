using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class RunRestoreFactoryTests
    {
        private const int RootSeed = 20260726;
        private const string SavedAtUtc = "2026-07-26T00:00:00.0000000+00:00";

        [Test]
        public void SV03_I01_RestorePreservesSoulDecksAndIssuedIds()
        {
            RunSaveSnapshot snapshot = CreateSnapshot(
                cards: new[]
                {
                    new RunSaveCardSnapshot(2, "standard-ace-1", CardSuit.Spade),
                    new RunSaveCardSnapshot(7, "military-knife-10", CardSuit.Clover)
                },
                demonCards: new[]
                {
                    new RunSaveDemonSnapshot(4, DemonContractCatalog.SatanKey),
                    new RunSaveDemonSnapshot(8, DemonContractCatalog.MammonKey)
                },
                lastIssuedCardId: 11,
                lastIssuedDemonCardId: 12,
                currentSoul: 7);
            RunRestoreFactory factory = CreateFactory();

            bool restored = factory.TryRestore(
                snapshot,
                out RunRestoreResult result,
                out RunSaveValidationResult validation);

            Assert.That(restored, Is.True);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(result, Is.Not.Null);
            PlayerRunState player = result.Session.Progress.Player;
            Assert.That(player.MaximumSoul, Is.EqualTo(12));
            Assert.That(player.CurrentSoul, Is.EqualTo(7));
            Assert.That(player.Deck.Count, Is.EqualTo(2));
            Assert.That(player.Deck[0].Id, Is.EqualTo(2));
            Assert.That(player.Deck[0].DefinitionKey, Is.EqualTo("standard-ace-1"));
            Assert.That(player.Deck[0].Suit, Is.EqualTo(CardSuit.Spade));
            Assert.That(player.Deck[1].Id, Is.EqualTo(7));
            Assert.That(player.Deck[1].DefinitionKey, Is.EqualTo("military-knife-10"));
            Assert.That(player.Deck[1].Suit, Is.EqualTo(CardSuit.Clover));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(player.DemonDeck[0].Id, Is.EqualTo(4));
            Assert.That(
                player.DemonDeck[0].DefinitionKey,
                Is.EqualTo(DemonContractCatalog.SatanKey));
            Assert.That(player.DemonDeck[1].Id, Is.EqualTo(8));
            Assert.That(
                player.DemonDeck[1].DefinitionKey,
                Is.EqualTo(DemonContractCatalog.MammonKey));
            Assert.That(player.LastIssuedCardId, Is.EqualTo(11));
            Assert.That(player.LastIssuedDemonCardId, Is.EqualTo(12));
            Assert.That(
                result.StartingDemonDefinitionKey,
                Is.EqualTo(DemonContractCatalog.SatanKey));
        }

        [Test]
        public void SV03_I02_RestoreDoesNotReuseRemovedHighestCardIds()
        {
            RunSaveSnapshot snapshot = CreateSnapshot(
                cards: new[]
                {
                    new RunSaveCardSnapshot(1, "standard-ace-1", CardSuit.Spade),
                    new RunSaveCardSnapshot(4, "military-knife-10", CardSuit.Clover)
                },
                demonCards: new[]
                {
                    new RunSaveDemonSnapshot(2, DemonContractCatalog.SatanKey)
                },
                lastIssuedCardId: 9,
                lastIssuedDemonCardId: 6);

            Assert.That(
                CreateFactory().TryRestore(
                    snapshot,
                    out RunRestoreResult result,
                    out RunSaveValidationResult validation),
                Is.True);

            RunCardDefinition addedCard =
                result.Session.Progress.Player.AddRewardCard("threat-hammer-6");
            RunDemonDefinition addedDemon =
                result.Session.Progress.Player.AddDemonCard(DemonContractCatalog.MammonKey);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(addedCard.Id, Is.EqualTo(10));
            Assert.That(addedDemon.Id, Is.EqualTo(7));
        }

        [Test]
        public void SV03_I03_RestorePreservesStageAndNextContent()
        {
            RunSaveSnapshot snapshot = CreateSnapshot(
                currentStageIndex: 1,
                currentStageId: "normal-2",
                nextContentKind: RunNextContentKind.Event,
                opponentOfferOrdinal: 2,
                battleRewardOrdinal: 2);

            Assert.That(
                CreateFactory().TryRestore(
                    snapshot,
                    out RunRestoreResult result,
                    out RunSaveValidationResult validation),
                Is.True);

            Assert.That(validation.IsValid, Is.True);
            Assert.That(
                result.Session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(result.Session.Progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(result.Session.Progress.CurrentStage.Id, Is.EqualTo("normal-2"));
            Assert.That(result.NextContentKind, Is.EqualTo(RunNextContentKind.Event));
            Assert.That(result.RootSeed, Is.EqualTo(RootSeed));
        }

        [Test]
        public void SV03_I04_FailedRestoreLeavesExistingSessionUntouched()
        {
            RunProgress existingProgress = new RunProgress(CreateStages(71), CreatePlayer());
            StageProgressionSession existingSession =
                new StageProgressionSession(existingProgress);
            RunSaveSnapshot invalid = CreateSnapshot(
                cards: new[]
                {
                    new RunSaveCardSnapshot(0, "unknown-card", CardSuit.Spade)
                },
                lastIssuedCardId: 0);

            bool restored = CreateFactory().TryRestore(
                invalid,
                out RunRestoreResult result,
                out RunSaveValidationResult validation);

            Assert.That(restored, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(validation.Error, Is.EqualTo(RunSaveValidationError.InvalidCard));
            Assert.That(existingSession.Progress, Is.SameAs(existingProgress));
            Assert.That(
                existingSession.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(existingSession.Progress.Player.CurrentSoul, Is.EqualTo(12));
        }

        [Test]
        public void SV03_I05_SameSeedAndOrdinalsReproduceNextOffers()
        {
            RunSaveSnapshot snapshot = CreateSnapshot(
                opponentOfferOrdinal: 1,
                battleRewardOrdinal: 1);
            Assert.That(
                CreateFactory().TryRestore(
                    snapshot,
                    out RunRestoreResult result,
                    out RunSaveValidationResult validation),
                Is.True);

            OpponentSelectionGenerator expectedOpponent =
                new OpponentSelectionGenerator(EnemyCombatProfileCatalog.Default, RootSeed);
            expectedOpponent.Generate(0);
            OpponentSelectionOffer expectedOpponentOffer = expectedOpponent.Generate(1);
            OpponentSelectionOffer restoredOpponentOffer =
                result.OpponentSelectionGenerator.Generate(1);

            BattleRewardGenerator expectedReward = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                unchecked(RootSeed + 1));
            expectedReward.Generate(BattleRewardTier.Normal);
            BattleRewardOffer expectedRewardOffer =
                expectedReward.Generate(BattleRewardTier.HighGrade);
            BattleRewardOffer restoredRewardOffer =
                result.BattleRewardGenerator.Generate(BattleRewardTier.HighGrade);

            Assert.That(validation.IsValid, Is.True);
            AssertOpponentOffersEqual(expectedOpponentOffer, restoredOpponentOffer);
            AssertRewardOffersEqual(expectedRewardOffer, restoredRewardOffer);
        }

        [Test]
        public void SV03_I06_SessionCaptureRecordsCurrentGeneratorOrdinals()
        {
            RunProgress progress = new RunProgress(CreateStages(RootSeed), CreatePlayer());
            Assert.That(progress.StartRun(), Is.True);
            Assert.That(
                progress.TryBeginBattleReward(
                    CreateRewardOffer(BattleRewardTier.Normal),
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            Assert.That(progress.TrySkipBattleReward(), Is.True);

            OpponentSelectionGenerator opponentGenerator =
                new OpponentSelectionGenerator(EnemyCombatProfileCatalog.Default, RootSeed);
            opponentGenerator.Generate(0);
            BattleRewardGenerator rewardGenerator = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                unchecked(RootSeed + 1));
            rewardGenerator.Generate(BattleRewardTier.Normal);
            StageProgressionSession session = new StageProgressionSession(
                progress,
                rewardGenerator: rewardGenerator,
                opponentSelectionGenerator: opponentGenerator);

            bool captured = RunSaveCapture.TryCapture(
                session,
                new RunSaveCaptureContext(
                    5,
                    "run-session-capture",
                    SavedAtUtc,
                    RunCheckpointKind.CombatSettlementCompleted,
                    RootSeed,
                    RunNextContentKind.Shop),
                out RunSaveSnapshot snapshot,
                out RunSaveValidationResult validation);

            Assert.That(captured, Is.True);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(snapshot.Random.OpponentOfferOrdinal, Is.EqualTo(1));
            Assert.That(snapshot.Random.BattleRewardOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void SV03_I07_RestoreRejectsGoldUntilRunStateOwnsIt()
        {
            RunSaveSnapshot snapshot = CreateSnapshot(currentGold: 1);

            bool restored = CreateFactory().TryRestore(
                snapshot,
                out RunRestoreResult result,
                out RunSaveValidationResult validation);

            Assert.That(restored, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(validation.Error, Is.EqualTo(RunSaveValidationError.InvalidGold));
        }

        private static RunRestoreFactory CreateFactory()
        {
            return new RunRestoreFactory(CreateStages);
        }

        private static RunSaveSnapshot CreateSnapshot(
            IEnumerable<RunSaveCardSnapshot> cards = null,
            IEnumerable<RunSaveDemonSnapshot> demonCards = null,
            int lastIssuedCardId = 7,
            int lastIssuedDemonCardId = 4,
            int currentSoul = 8,
            int currentGold = 0,
            int currentStageIndex = 0,
            string currentStageId = "normal-1",
            string nextContentKind = RunNextContentKind.Shop,
            int opponentOfferOrdinal = 1,
            int battleRewardOrdinal = 1)
        {
            PlayerRunSaveSnapshot player = new PlayerRunSaveSnapshot(
                12,
                currentSoul,
                currentGold,
                lastIssuedCardId,
                lastIssuedDemonCardId,
                DemonContractCatalog.SatanKey,
                cards ?? new[]
                {
                    new RunSaveCardSnapshot(2, "standard-ace-1", CardSuit.Spade),
                    new RunSaveCardSnapshot(7, "military-knife-10", CardSuit.Clover)
                },
                demonCards ?? new[]
                {
                    new RunSaveDemonSnapshot(4, DemonContractCatalog.SatanKey)
                });
            return new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                RunSaveSnapshot.CurrentContentRevision,
                3,
                "run-restore",
                SavedAtUtc,
                RunCheckpointKind.CombatSettlementCompleted,
                RunSaveStatus.InProgress,
                RootSeed,
                currentStageIndex,
                currentStageId,
                nextContentKind,
                player,
                new RunRandomSaveSnapshot(
                    opponentOfferOrdinal,
                    battleRewardOrdinal,
                    0,
                    0,
                    null),
                new string[0],
                new string[0]);
        }

        private static IReadOnlyList<StageDefinition> CreateStages(int seed)
        {
            return new[]
            {
                StageDefinition.CreateForEnemyProfile(
                    "normal-1",
                    "Normal 1",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.GunslingerKey,
                    seed,
                    unchecked(seed + 1)),
                StageDefinition.CreateForEnemyProfile(
                    "normal-2",
                    "Elite",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.EnforcerKey,
                    unchecked(seed + 2),
                    unchecked(seed + 3)),
                StageDefinition.CreateForEnemyProfile(
                    "final-boss",
                    "Final Boss",
                    StageKind.FinalBossCombat,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    unchecked(seed + 4),
                    unchecked(seed + 5))
            };
        }

        private static PlayerRunState CreatePlayer()
        {
            return new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, "standard-ace-1"),
                    new RunCardDefinition(1, "military-knife-10")
                },
                new[]
                {
                    new RunDemonDefinition(0, DemonContractCatalog.SatanKey)
                });
        }

        private static BattleRewardOffer CreateRewardOffer(BattleRewardTier tier)
        {
            return new BattleRewardGenerator(BattleRewardCatalog.CreateDefault(), 13)
                .Generate(tier);
        }

        private static void AssertOpponentOffersEqual(
            OpponentSelectionOffer expected,
            OpponentSelectionOffer actual)
        {
            Assert.That(actual.OfferId, Is.EqualTo(expected.OfferId));
            Assert.That(actual.StageIndex, Is.EqualTo(expected.StageIndex));
            Assert.That(actual.Candidates.Count, Is.EqualTo(expected.Candidates.Count));
            for (int i = 0; i < expected.Candidates.Count; i++)
            {
                Assert.That(
                    actual.Candidates[i].ProfileKey,
                    Is.EqualTo(expected.Candidates[i].ProfileKey));
            }
        }

        private static void AssertRewardOffersEqual(
            BattleRewardOffer expected,
            BattleRewardOffer actual)
        {
            Assert.That(actual.OfferId, Is.EqualTo(expected.OfferId));
            Assert.That(actual.Tier, Is.EqualTo(expected.Tier));
            Assert.That(actual.Options.Count, Is.EqualTo(expected.Options.Count));
            for (int i = 0; i < expected.Options.Count; i++)
            {
                Assert.That(
                    actual.Options[i].DefinitionKey,
                    Is.EqualTo(expected.Options[i].DefinitionKey));
            }
        }
    }
}
