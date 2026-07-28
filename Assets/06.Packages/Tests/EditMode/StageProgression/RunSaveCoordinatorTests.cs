using System;
using System.Collections.Generic;
using System.Linq;
using Border.SaveLoad;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class RunSaveCoordinatorTests
    {
        private const int RootSeed = 20260728;
        private const string RunId = "sv04-run";

        private static readonly DateTimeOffset SavedAt =
            new DateTimeOffset(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void SV04_I01_StartingDemonSelectionWritesExactlyOneCheckpoint()
        {
            CoordinatorHarness harness = CreateStartingHarness();
            Assert.That(harness.Coordinator.TryStartRun(), Is.True);
            StartingDemonSelectionOffer offer =
                harness.Session.PendingStartingDemonSelection;

            bool selected = harness.Coordinator.TrySelectStartingDemon(
                offer.OfferId,
                offer.Options[0].OptionId);
            bool duplicate = harness.Coordinator.TrySelectStartingDemon(
                offer.OfferId,
                offer.Options[0].OptionId);
            RunSaveLoadResult loaded = harness.Repository.Load();

            Assert.That(selected, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(harness.Files.TemporaryWriteAttemptCount, Is.EqualTo(1));
            Assert.That(
                harness.Coordinator.HasPendingCheckpoint,
                Is.False,
                harness.Coordinator.LastWriteResult.Status + ":" +
                harness.Coordinator.LastWriteResult.ValidationError + ":" +
                harness.Coordinator.LastWriteResult.DiagnosticCode);
            Assert.That(harness.Coordinator.LastWriteResult.IsSuccess, Is.True);
            Assert.That(loaded.CanContinue, Is.True);
            Assert.That(
                loaded.Snapshot.CheckpointKind,
                Is.EqualTo(RunCheckpointKind.StartingDemonSelected));
            Assert.That(loaded.Snapshot.SaveSequence, Is.EqualTo(4));
            Assert.That(
                loaded.Snapshot.Player.StartingDemonDefinitionKey,
                Is.EqualTo(offer.Options[0].DefinitionKey));
        }

        [TestCase(false, RunNextContentKind.Shop)]
        [TestCase(true, RunNextContentKind.Event)]
        public void SV04_I02_BattleRewardCompletionWritesExactlyOneCheckpoint(
            bool selectReward,
            string nextContentKind)
        {
            CoordinatorHarness harness = CreateRewardHarness();
            BattleRewardOffer offer = BeginNormalReward(harness.Session.Progress);
            int originalDeckCount = harness.Session.Progress.Player.Deck.Count;

            bool completed = selectReward
                ? harness.Coordinator.TrySelectBattleReward(
                    offer.Options[0].OptionId,
                    nextContentKind)
                : harness.Coordinator.TrySkipBattleReward(nextContentKind);
            bool duplicate = harness.Coordinator.TrySkipBattleReward(nextContentKind);
            RunSaveLoadResult loaded = harness.Repository.Load();

            Assert.That(completed, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(harness.Files.TemporaryWriteAttemptCount, Is.EqualTo(1));
            Assert.That(
                harness.Session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(
                loaded.Snapshot.CheckpointKind,
                Is.EqualTo(RunCheckpointKind.CombatSettlementCompleted));
            Assert.That(loaded.Snapshot.NextContentKind, Is.EqualTo(nextContentKind));
            Assert.That(
                loaded.Snapshot.Player.Cards.Count,
                Is.EqualTo(originalDeckCount + (selectReward ? 1 : 0)));
        }

        [Test]
        public void SV04_I04_RejectedAndInvalidInputsDoNotWriteOrMutate()
        {
            CoordinatorHarness starting = CreateStartingHarness();
            Assert.That(starting.Coordinator.TryStartRun(), Is.True);
            StartingDemonSelectionOffer startingOffer =
                starting.Session.PendingStartingDemonSelection;

            Assert.That(
                starting.Coordinator.TrySelectStartingDemon(
                    startingOffer.OfferId + 1,
                    startingOffer.Options[0].OptionId),
                Is.False);
            Assert.That(starting.Files.TemporaryWriteAttemptCount, Is.Zero);
            Assert.That(starting.Session.Progress.Player.DemonDeck, Is.Empty);

            CoordinatorHarness reward = CreateRewardHarness();
            BeginNormalReward(reward.Session.Progress);

            Assert.That(
                reward.Coordinator.TrySkipBattleReward(
                    RunNextContentKind.Battle),
                Is.False);
            Assert.That(reward.Files.TemporaryWriteAttemptCount, Is.Zero);
            Assert.That(
                reward.Session.Progress.State,
                Is.EqualTo(StageProgressionState.RewardSelection));
        }

        [Test]
        public void SV04_I05_SaveFailureBlocksProgressUntilExactRetrySucceeds()
        {
            CoordinatorHarness harness = CreateStartingHarness();
            harness.Files.FailNextTemporaryWrite = true;
            Assert.That(harness.Coordinator.TryStartRun(), Is.True);
            StartingDemonSelectionOffer offer =
                harness.Session.PendingStartingDemonSelection;

            Assert.That(
                harness.Coordinator.TrySelectStartingDemon(
                    offer.OfferId,
                    offer.Options[1].OptionId),
                Is.True);
            Assert.That(harness.Coordinator.HasPendingCheckpoint, Is.True);
            Assert.That(
                harness.Coordinator.LastWriteResult.Status,
                Is.EqualTo(RunSaveWriteStatus.TemporaryWriteFailed));
            Assert.That(harness.Coordinator.TryStartRun(), Is.False);
            Assert.That(
                harness.Session.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));

            Assert.That(harness.Coordinator.TryRetryPendingCheckpoint(), Is.True);
            Assert.That(harness.Coordinator.HasPendingCheckpoint, Is.False);
            Assert.That(harness.Files.TemporaryWriteAttemptCount, Is.EqualTo(2));
            Assert.That(harness.Coordinator.TryStartRun(), Is.True);

            RunSaveLoadResult loaded = harness.Repository.Load();
            Assert.That(loaded.Snapshot.SaveSequence, Is.EqualTo(4));
            Assert.That(
                loaded.Snapshot.SavedAtUtc,
                Is.EqualTo(SavedAt.ToString("O")));
        }

        [Test]
        public void SV04_I06_RepeatedLoadReproducesSameNextOpponentOffer()
        {
            CoordinatorHarness harness = CreateStartingHarness();
            Assert.That(harness.Coordinator.TryStartRun(), Is.True);
            StartingDemonSelectionOffer offer =
                harness.Session.PendingStartingDemonSelection;
            Assert.That(
                harness.Coordinator.TrySelectStartingDemon(
                    offer.OfferId,
                    offer.Options[0].OptionId),
                Is.True);

            RunSaveSnapshot firstSnapshot = harness.Repository.Load().Snapshot;
            RunSaveSnapshot secondSnapshot = harness.Repository.Load().Snapshot;
            RunRestoreFactory factory = new RunRestoreFactory(CreateStages);
            Assert.That(
                factory.TryRestore(
                    firstSnapshot,
                    out RunRestoreResult first,
                    out RunSaveValidationResult firstValidation),
                Is.True);
            Assert.That(firstValidation.IsValid, Is.True);
            Assert.That(
                factory.TryRestore(
                    secondSnapshot,
                    out RunRestoreResult second,
                    out RunSaveValidationResult secondValidation),
                Is.True);
            Assert.That(secondValidation.IsValid, Is.True);

            Assert.That(first.Session.TryStartRun(), Is.True);
            Assert.That(second.Session.TryStartRun(), Is.True);
            Assert.That(
                first.Session.PendingOpponentSelection.Candidates
                    .Select(candidate => candidate.ProfileKey),
                Is.EqualTo(
                    second.Session.PendingOpponentSelection.Candidates
                        .Select(candidate => candidate.ProfileKey)));
        }

        [Test]
        public void SV04_I07_RunEndWritesOneTerminalCheckpoint()
        {
            CoordinatorHarness harness = CreateRewardHarness();
            Assert.That(harness.Session.Progress.StartRun(), Is.True);
            harness.Session.Progress.Player.SetCurrentSoul(0);
            Assert.That(harness.Session.Progress.TryDefeatRun(), Is.True);

            bool first = harness.Coordinator.TryCheckpointRunEnd();
            bool duplicate = harness.Coordinator.TryCheckpointRunEnd();
            RunSaveLoadResult loaded = harness.Repository.Load();

            Assert.That(first, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(harness.Files.TemporaryWriteAttemptCount, Is.EqualTo(1));
            Assert.That(
                loaded.Status,
                Is.EqualTo(RunSaveLoadStatus.NoContinueTerminalSave));
            Assert.That(
                loaded.Snapshot.CheckpointKind,
                Is.EqualTo(RunCheckpointKind.RunEnded));
            Assert.That(loaded.Snapshot.Status, Is.EqualTo(RunSaveStatus.Defeat));
        }

        [Test]
        public void SV04_I08_FinalBossRewardWritesTerminalCheckpoint()
        {
            CoordinatorHarness harness = CreateRewardHarness();
            RunProgress progress = harness.Session.Progress;
            Assert.That(progress.StartRun(), Is.True);
            BattleRewardOffer normalOffer = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                RootSeed).Generate(BattleRewardTier.Normal);
            Assert.That(
                progress.TryBeginBattleReward(
                    normalOffer,
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            Assert.That(progress.TrySkipBattleReward(), Is.True);
            Assert.That(progress.TryAdvanceToNextStage(), Is.True);
            BattleRewardOffer bossOffer = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                unchecked(RootSeed + 1)).Generate(BattleRewardTier.HighGrade);
            Assert.That(
                progress.TryBeginBattleReward(
                    bossOffer,
                    BattleRewardCompletionTarget.RunVictory),
                Is.True);

            Assert.That(
                harness.Coordinator.TrySkipBattleReward(
                    RunNextContentKind.Result),
                Is.True);
            Assert.That(
                harness.Session.Progress.State,
                Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(harness.Coordinator.TryCheckpointRunEnd(), Is.False);

            RunSaveLoadResult loaded = harness.Repository.Load();
            Assert.That(
                loaded.Status,
                Is.EqualTo(RunSaveLoadStatus.NoContinueTerminalSave));
            Assert.That(loaded.Snapshot.Status, Is.EqualTo(RunSaveStatus.Victory));
        }

        private static CoordinatorHarness CreateStartingHarness()
        {
            PlayerRunState player = new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 1),
                    new RunCardDefinition(1, 2),
                    new RunCardDefinition(2, 3),
                    new RunCardDefinition(3, 4)
                },
                new RunDemonDefinition[0]);
            StageProgressionSession session = new StageProgressionSession(
                new RunProgress(CreateStages(RootSeed), player),
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    RootSeed),
                startingDemonSelectionGenerator:
                    new StartingDemonSelectionGenerator(
                        DemonContractCatalog.Default,
                        RootSeed));
            return CreateHarness(session);
        }

        private static CoordinatorHarness CreateRewardHarness()
        {
            PlayerRunState player = new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 1),
                    new RunCardDefinition(1, 2),
                    new RunCardDefinition(2, 3),
                    new RunCardDefinition(3, 4)
                });
            return CreateHarness(
                new StageProgressionSession(
                    new RunProgress(CreateStages(RootSeed), player)));
        }

        private static CoordinatorHarness CreateHarness(
            StageProgressionSession session)
        {
            CountingRunSaveFileStore files = new CountingRunSaveFileStore();
            RunSaveRepository repository = new RunSaveRepository(
                files,
                CreateStages(RootSeed));
            RunSaveCoordinator coordinator = new RunSaveCoordinator(
                session,
                repository,
                RunId,
                RootSeed,
                4,
                () => SavedAt);
            return new CoordinatorHarness(session, repository, coordinator, files);
        }

        private static BattleRewardOffer BeginNormalReward(RunProgress progress)
        {
            Assert.That(progress.StartRun(), Is.True);
            BattleRewardOffer offer = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                RootSeed).Generate(BattleRewardTier.Normal);
            Assert.That(
                progress.TryBeginBattleReward(
                    offer,
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            return offer;
        }

        private static IReadOnlyList<StageDefinition> CreateStages(int seed)
        {
            return new[]
            {
                new StageDefinition(
                    "normal-1",
                    "Normal",
                    StageKind.NormalCombat,
                    3,
                    seed,
                    unchecked(seed + 1)),
                StageDefinition.CreateForEnemyProfile(
                    "boss",
                    "Boss",
                    StageKind.FinalBossCombat,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    unchecked(seed + 2),
                    unchecked(seed + 3))
            };
        }

        private sealed class CoordinatorHarness
        {
            internal CoordinatorHarness(
                StageProgressionSession session,
                RunSaveRepository repository,
                RunSaveCoordinator coordinator,
                CountingRunSaveFileStore files)
            {
                Session = session;
                Repository = repository;
                Coordinator = coordinator;
                Files = files;
            }

            internal StageProgressionSession Session { get; }

            internal RunSaveRepository Repository { get; }

            internal RunSaveCoordinator Coordinator { get; }

            internal CountingRunSaveFileStore Files { get; }
        }

        private sealed class CountingRunSaveFileStore : IRunSaveFileStore
        {
            private readonly Dictionary<string, string> _files =
                new Dictionary<string, string>(StringComparer.Ordinal);

            internal bool FailNextTemporaryWrite { get; set; }

            internal int TemporaryWriteAttemptCount { get; private set; }

            public bool Exists(string fileName)
            {
                return _files.ContainsKey(fileName);
            }

            public bool TryRead(string fileName, out string contents)
            {
                return _files.TryGetValue(fileName, out contents);
            }

            public bool TryWrite(string fileName, string contents)
            {
                if (string.Equals(
                    fileName,
                    RunSaveRepository.TemporaryFileName,
                    StringComparison.Ordinal))
                {
                    TemporaryWriteAttemptCount++;
                    if (FailNextTemporaryWrite)
                    {
                        FailNextTemporaryWrite = false;
                        return false;
                    }
                }

                _files[fileName] = contents;
                return true;
            }

            public bool TryDelete(string fileName)
            {
                _files.Remove(fileName);
                return true;
            }

            public bool TryMove(
                string sourceFileName,
                string destinationFileName,
                bool overwrite)
            {
                if (!_files.TryGetValue(sourceFileName, out string contents) ||
                    (!overwrite && _files.ContainsKey(destinationFileName)))
                {
                    return false;
                }

                _files.Remove(sourceFileName);
                _files[destinationFileName] = contents;
                return true;
            }
        }
    }
}
