using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class RunSaveSnapshotTests
    {
        private const string SavedAtUtc = "2026-07-26T00:00:00.0000000+00:00";

        [Test]
        public void SV01_U01_SnapshotPreservesSoulDecksAndIssuedIds()
        {
            PlayerRunState player = new PlayerRunState(
                12,
                9,
                new[]
                {
                    new RunCardDefinition(2, "standard-ace-1", CardSuit.Spade),
                    new RunCardDefinition(7, "military-knife-10", CardSuit.Clover)
                },
                new[]
                {
                    new RunDemonDefinition(4, DemonContractCatalog.SatanKey),
                    new RunDemonDefinition(5, DemonContractCatalog.MammonKey)
                });
            Assert.That(player.StartingDemonGrantCompleted, Is.True);
            RunProgress progress = new RunProgress(CreateStages(), player);
            Assert.That(progress.StartRun(), Is.True);
            player.SetCurrentSoul(7);
            Assert.That(CompleteCurrentStage(progress), Is.True);

            bool captured = RunSaveCapture.TryCapture(
                progress,
                new RunSaveCaptureContext(
                    3,
                    "run-001",
                    SavedAtUtc,
                    RunCheckpointKind.CombatSettlementCompleted,
                    20260726,
                    RunNextContentKind.Shop),
                out RunSaveSnapshot snapshot,
                out RunSaveValidationResult validation);

            Assert.That(captured, Is.True);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(
                snapshot.SchemaVersion,
                Is.EqualTo(RunSaveSnapshot.CurrentSchemaVersion));
            Assert.That(snapshot.CurrentStageIndex, Is.Zero);
            Assert.That(snapshot.CurrentStageId, Is.EqualTo("normal-1"));
            Assert.That(snapshot.Player.MaximumSoul, Is.EqualTo(12));
            Assert.That(snapshot.Player.CurrentSoul, Is.EqualTo(7));
            Assert.That(snapshot.Player.CurrentGold, Is.Zero);
            Assert.That(snapshot.Player.LastIssuedCardId, Is.EqualTo(7));
            Assert.That(snapshot.Player.LastIssuedDemonCardId, Is.EqualTo(5));
            Assert.That(snapshot.Player.Cards.Count, Is.EqualTo(2));
            Assert.That(snapshot.Player.Cards[0].Id, Is.EqualTo(2));
            Assert.That(snapshot.Player.Cards[1].DefinitionKey, Is.EqualTo("military-knife-10"));
            Assert.That(snapshot.Player.Cards[1].Suit, Is.EqualTo(CardSuit.Clover));
            Assert.That(snapshot.Player.DemonCards.Count, Is.EqualTo(2));
            Assert.That(snapshot.Player.DemonCards[0].Id, Is.EqualTo(4));
            Assert.That(
                snapshot.Player.DemonCards[0].DefinitionKey,
                Is.EqualTo(DemonContractCatalog.SatanKey));
        }

        [Test]
        public void RF01A_I01_SnapshotPreservesCurrentGold()
        {
            PlayerRunState player = new PlayerRunState(
                12,
                12,
                new[] { new RunCardDefinition(0, 1) });
            RunProgress progress = new RunProgress(CreateStages(), player);
            Assert.That(progress.StartRun(), Is.True);
            player.AddGold(9);
            Assert.That(CompleteCurrentStage(progress), Is.True);
            Assert.That(player.CurrentGold, Is.EqualTo(9));

            bool captured = RunSaveCapture.TryCapture(
                progress,
                new RunSaveCaptureContext(
                    3,
                    "run-gold",
                    SavedAtUtc,
                    RunCheckpointKind.CombatSettlementCompleted,
                    20260730,
                    RunNextContentKind.Shop),
                out RunSaveSnapshot snapshot,
                out RunSaveValidationResult validation);

            Assert.That(captured, Is.True);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(snapshot.Player.CurrentGold, Is.EqualTo(9));
        }

        [Test]
        public void SV01_U02_ValidatorRejectsDuplicateCardIds()
        {
            RunSaveSnapshot duplicateCards = CreateSnapshot(
                cards: new[]
                {
                    new RunSaveCardSnapshot(0, "standard-ace-1", CardSuit.Spade),
                    new RunSaveCardSnapshot(0, "military-knife-10", CardSuit.Clover)
                });
            RunSaveSnapshot duplicateDemonCards = CreateSnapshot(
                demonCards: new[]
                {
                    new RunSaveDemonSnapshot(0, DemonContractCatalog.SatanKey),
                    new RunSaveDemonSnapshot(0, DemonContractCatalog.BelphegorKey)
                });

            RunSaveValidationResult cardResult =
                RunSaveValidator.Validate(duplicateCards, CreateStages());
            RunSaveValidationResult demonResult =
                RunSaveValidator.Validate(duplicateDemonCards, CreateStages());

            Assert.That(cardResult.IsValid, Is.False);
            Assert.That(
                cardResult.Error,
                Is.EqualTo(RunSaveValidationError.DuplicateCardId));
            Assert.That(demonResult.IsValid, Is.False);
            Assert.That(
                demonResult.Error,
                Is.EqualTo(RunSaveValidationError.DuplicateDemonCardId));
        }

        [Test]
        public void SV01_U03_ValidatorRejectsUnknownDefinitionsAndSuit()
        {
            RunSaveSnapshot unknownCard = CreateSnapshot(
                cards: new[]
                {
                    new RunSaveCardSnapshot(0, "unknown-card", CardSuit.Spade)
                },
                lastIssuedCardId: 0);
            RunSaveSnapshot invalidSuit = CreateSnapshot(
                cards: new[]
                {
                    new RunSaveCardSnapshot(0, "standard-ace-1", (CardSuit)99)
                },
                lastIssuedCardId: 0);
            RunSaveSnapshot unknownDemon = CreateSnapshot(
                demonCards: new[]
                {
                    new RunSaveDemonSnapshot(0, "unknown-demon"),
                    new RunSaveDemonSnapshot(1, DemonContractCatalog.MammonKey)
                },
                lastIssuedDemonCardId: 1,
                startingDemonGrantCompleted: true);

            Assert.That(
                RunSaveValidator.Validate(unknownCard, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidCard));
            Assert.That(
                RunSaveValidator.Validate(invalidSuit, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidCard));
            Assert.That(
                RunSaveValidator.Validate(unknownDemon, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidDemonCard));
        }

        [Test]
        public void SV01_U04_ValidatorRejectsIssuedIdsBelowCurrentMaximum()
        {
            RunSaveSnapshot invalidCardId = CreateSnapshot(lastIssuedCardId: 0);
            RunSaveSnapshot invalidDemonId =
                CreateSnapshot(lastIssuedDemonCardId: -1);

            Assert.That(
                RunSaveValidator.Validate(invalidCardId, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidLastIssuedCardId));
            Assert.That(
                RunSaveValidator.Validate(invalidDemonId, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidLastIssuedDemonCardId));
        }

        [Test]
        public void SV01_U05_ValidatorChecksCheckpointAndNextContentPair()
        {
            RunSaveSnapshot valid = CreateSnapshot();
            RunSaveSnapshot invalid = CreateSnapshot(
                nextContentKind: RunNextContentKind.Battle);
            RunSaveSnapshot invalidTerminal = CreateSnapshot(
                checkpointKind: RunCheckpointKind.RunEnded,
                status: RunSaveStatus.InProgress,
                nextContentKind: RunNextContentKind.Result);
            RunSaveSnapshot validDefeat = CreateSnapshot(
                currentSoul: 0,
                checkpointKind: RunCheckpointKind.RunEnded,
                status: RunSaveStatus.Defeat,
                nextContentKind: RunNextContentKind.Result);

            Assert.That(
                RunSaveValidator.Validate(valid, CreateStages()).IsValid,
                Is.True);
            Assert.That(
                RunSaveValidator.Validate(invalid, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidCheckpoint));
            Assert.That(
                RunSaveValidator.Validate(invalidTerminal, CreateStages()).Error,
                Is.EqualTo(RunSaveValidationError.InvalidCheckpoint));
            Assert.That(
                RunSaveValidator.Validate(validDefeat, CreateStages()).IsValid,
                Is.True);
        }

        [Test]
        public void SV01_U06_CaptureRejectsBattleAndPendingRewardStates()
        {
            RunProgress progress = CreateProgress();
            Assert.That(progress.StartRun(), Is.True);
            RunSaveCaptureContext context = new RunSaveCaptureContext(
                1,
                "run-unstable",
                SavedAtUtc,
                RunCheckpointKind.CombatSettlementCompleted,
                17,
                RunNextContentKind.Shop);

            bool battleCaptured = RunSaveCapture.TryCapture(
                progress,
                context,
                out RunSaveSnapshot battleSnapshot,
                out RunSaveValidationResult battleValidation);
            Assert.That(
                progress.TryBeginBattleReward(
                    CreateRewardOffer(),
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            bool rewardCaptured = RunSaveCapture.TryCapture(
                progress,
                context,
                out RunSaveSnapshot rewardSnapshot,
                out RunSaveValidationResult rewardValidation);

            Assert.That(battleCaptured, Is.False);
            Assert.That(battleSnapshot, Is.Null);
            Assert.That(
                battleValidation.Error,
                Is.EqualTo(RunSaveValidationError.UnstableState));
            Assert.That(rewardCaptured, Is.False);
            Assert.That(rewardSnapshot, Is.Null);
            Assert.That(
                rewardValidation.Error,
                Is.EqualTo(RunSaveValidationError.UnstableState));
        }

        [Test]
        public void SV01_U07_SnapshotDefensivelyCopiesSourceCollections()
        {
            List<RunSaveCardSnapshot> cards = new List<RunSaveCardSnapshot>
            {
                new RunSaveCardSnapshot(0, "standard-ace-1", CardSuit.Spade)
            };
            List<RunSaveDemonSnapshot> demonCards = new List<RunSaveDemonSnapshot>
            {
                new RunSaveDemonSnapshot(0, DemonContractCatalog.SatanKey),
                new RunSaveDemonSnapshot(1, DemonContractCatalog.MammonKey)
            };
            List<string> completedShopIds = new List<string> { "shop-1" };
            List<string> completedEventIds = new List<string> { "event-1" };
            PlayerRunSaveSnapshot player = new PlayerRunSaveSnapshot(
                12,
                8,
                0,
                0,
                1,
                true,
                cards,
                demonCards);
            RunSaveSnapshot snapshot = new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                RunSaveSnapshot.CurrentContentRevision,
                1,
                "run-copy",
                SavedAtUtc,
                RunCheckpointKind.CombatSettlementCompleted,
                RunSaveStatus.InProgress,
                23,
                0,
                "normal-1",
                RunNextContentKind.Shop,
                player,
                new RunRandomSaveSnapshot(1, 2, 3, 4, null),
                completedShopIds,
                completedEventIds);

            cards.Clear();
            demonCards.Clear();
            completedShopIds.Clear();
            completedEventIds.Clear();

            Assert.That(snapshot.Player.Cards.Count, Is.EqualTo(1));
            Assert.That(snapshot.Player.DemonCards.Count, Is.EqualTo(2));
            Assert.That(snapshot.CompletedShopIds, Is.EqualTo(new[] { "shop-1" }));
            Assert.That(snapshot.CompletedEventIds, Is.EqualTo(new[] { "event-1" }));
            Assert.That(
                RunSaveValidator.Validate(snapshot, CreateStages()).IsValid,
                Is.True);
        }

        private static RunSaveSnapshot CreateSnapshot(
            IEnumerable<RunSaveCardSnapshot> cards = null,
            IEnumerable<RunSaveDemonSnapshot> demonCards = null,
            int lastIssuedCardId = 1,
            int lastIssuedDemonCardId = 1,
            bool startingDemonGrantCompleted = true,
            int currentSoul = 8,
            RunCheckpointKind checkpointKind =
                RunCheckpointKind.CombatSettlementCompleted,
            RunSaveStatus status = RunSaveStatus.InProgress,
            string nextContentKind = RunNextContentKind.Shop)
        {
            PlayerRunSaveSnapshot player = new PlayerRunSaveSnapshot(
                12,
                currentSoul,
                0,
                lastIssuedCardId,
                lastIssuedDemonCardId,
                startingDemonGrantCompleted,
                cards ?? new[]
                {
                    new RunSaveCardSnapshot(0, "standard-ace-1", CardSuit.Spade),
                    new RunSaveCardSnapshot(1, "military-knife-10", CardSuit.Clover)
                },
                demonCards ?? new[]
                {
                    new RunSaveDemonSnapshot(0, DemonContractCatalog.SatanKey),
                    new RunSaveDemonSnapshot(1, DemonContractCatalog.MammonKey)
                });
            return new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                RunSaveSnapshot.CurrentContentRevision,
                1,
                "run-valid",
                SavedAtUtc,
                checkpointKind,
                status,
                31,
                0,
                "normal-1",
                nextContentKind,
                player,
                new RunRandomSaveSnapshot(1, 2, 0, 0, null),
                new string[0],
                new string[0]);
        }

        private static RunProgress CreateProgress()
        {
            return new RunProgress(
                CreateStages(),
                new PlayerRunState(
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
                    }));
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
                    10,
                    11),
                new StageDefinition(
                    "final-boss",
                    "Final Boss",
                    StageKind.FinalBossCombat,
                    7,
                    20,
                    21)
            };
        }

        private static bool CompleteCurrentStage(RunProgress progress)
        {
            if (!progress.TryBeginBattleReward(
                    CreateRewardOffer(),
                    BattleRewardCompletionTarget.StageCleared))
            {
                return false;
            }

            return progress.TrySkipBattleReward();
        }

        private static BattleRewardOffer CreateRewardOffer()
        {
            return new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                20260726).Generate(BattleRewardTier.Normal);
        }
    }
}
