using System;
using System.Collections.Generic;
using System.Linq;
using Border.SaveLoad;
using Border.SaveLoad.UI;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class RunSaveFlowTests
    {
        private const int RootSeed = 20260728;
        private static readonly DateTimeOffset CreatedAt =
            new DateTimeOffset(2026, 7, 28, 1, 2, 3, TimeSpan.Zero);

        [Test]
        public void SV05_I01_BattleExitRestoresPreviousStartingCheckpoint()
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            RunSaveFlow first = CreateFlow(files, "run-a");
            Assert.That(first.TryRequestNewRun(), Is.True);
            StartingDemonSelectionOffer starting =
                first.Session.PendingStartingDemonSelection;
            Assert.That(
                first.TrySelectStartingDemon(
                    starting.OfferId,
                    starting.Options[0].OptionId),
                Is.True);
            OpponentSelectionOffer opponent =
                first.Session.PendingOpponentSelection;
            Assert.That(
                first.Session.TrySelectOpponent(
                    opponent.OfferId,
                    opponent.Candidates[0].ProfileKey),
                Is.True);
            Assert.That(
                first.Session.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));

            RunSaveFlow reloaded = CreateFlow(files, "unused");
            Assert.That(reloaded.CanContinueRun, Is.True);
            Assert.That(reloaded.TryContinueRun(), Is.True);

            Assert.That(
                reloaded.Session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(reloaded.Session.Battle, Is.Null);
            Assert.That(
                reloaded.Session.PendingOpponentSelection.Candidates
                    .Select(candidate => candidate.ProfileKey),
                Is.EqualTo(
                    opponent.Candidates.Select(candidate => candidate.ProfileKey)));
        }

        [Test]
        public void SV05_I01_PostSettlementExitRestoresStageClearedCheckpoint()
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            RunSaveFlow first = CreateFlow(files, "run-b");
            Assert.That(first.TryRequestNewRun(), Is.True);
            StartingDemonSelectionOffer starting =
                first.Session.PendingStartingDemonSelection;
            Assert.That(
                first.TrySelectStartingDemon(
                    starting.OfferId,
                    starting.Options[0].OptionId),
                Is.True);
            OpponentSelectionOffer opponent =
                first.Session.PendingOpponentSelection;
            Assert.That(
                first.Session.TrySelectOpponent(
                    opponent.OfferId,
                    opponent.Candidates[0].ProfileKey),
                Is.True);
            BattleRewardOffer reward = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                RootSeed).Generate(BattleRewardTier.Normal);
            Assert.That(
                first.Session.Progress.TryBeginBattleReward(
                    reward,
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            Assert.That(first.TrySkipBattleReward(), Is.True);
            Assert.That(first.TryAdvanceToNextStage(), Is.True);

            RunSaveFlow reloaded = CreateFlow(files, "unused");
            Assert.That(reloaded.TryContinueRun(), Is.True);
            Assert.That(
                reloaded.Session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(reloaded.Session.Progress.CurrentStageIndex, Is.Zero);
        }

        [Test]
        public void SV05_I02_ReservationReconnectShowsSameStartingDemonOffer()
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            RunSaveFlow first = CreateFlow(files, "reserved-run");

            Assert.That(first.TryRequestNewRun(), Is.True);
            StartingDemonSelectionOffer original =
                first.Session.PendingStartingDemonSelection;
            Assert.That(original, Is.Not.Null);

            RunSaveFlow reconnected = CreateFlow(files, "unused");
            Assert.That(reconnected.CanResumeReservation, Is.True);
            Assert.That(reconnected.CanContinueRun, Is.False);
            Assert.That(reconnected.TryResumeReservation(), Is.True);
            StartingDemonSelectionOffer restored =
                reconnected.Session.PendingStartingDemonSelection;

            Assert.That(restored.OfferId, Is.EqualTo(original.OfferId));
            Assert.That(
                restored.Options.Select(option => option.DefinitionKey),
                Is.EqualTo(
                    original.Options.Select(option => option.DefinitionKey)));
        }

        [Test]
        public void SV05_I03_SaveFailureLocksProgressAndOffersExactRetry()
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            RunSaveFlow flow = CreateFlow(files, "retry-run");
            Assert.That(flow.TryRequestNewRun(), Is.True);
            StartingDemonSelectionOffer offer =
                flow.Session.PendingStartingDemonSelection;
            files.FailNextWriteFileName = RunSaveRepository.TemporaryFileName;

            Assert.That(
                flow.TrySelectStartingDemon(
                    offer.OfferId,
                    offer.Options[1].OptionId),
                Is.True);
            RunSaveViewModel failed = RunSavePresenter.Create(flow);

            Assert.That(flow.HasPendingCheckpoint, Is.True);
            Assert.That(failed.BlocksProgressionInput, Is.True);
            Assert.That(failed.CanRetrySave, Is.True);
            Assert.That(flow.TryStartRun(), Is.False);

            Assert.That(flow.TryRetryPendingCheckpoint(), Is.True);
            RunSaveViewModel recovered = RunSavePresenter.Create(flow);
            Assert.That(flow.HasPendingCheckpoint, Is.False);
            Assert.That(recovered.BlocksProgressionInput, Is.False);
            Assert.That(recovered.CanRetrySave, Is.False);
            Assert.That(
                flow.Session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
        }

        [Test]
        public void SV05_U01_NewRunCancelAndReservationFailurePreserveExistingSave()
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            WriteStartingCheckpoint(files, "old-run");
            string original = files.Get(RunSaveRepository.PrimaryFileName);
            RunSaveFlow flow = CreateFlow(files, "new-run");

            Assert.That(flow.TryRequestNewRun(), Is.True);
            Assert.That(flow.RequiresNewRunConfirmation, Is.True);
            Assert.That(flow.TryCancelNewRun(), Is.True);
            Assert.That(
                files.Get(RunSaveRepository.PrimaryFileName),
                Is.EqualTo(original));

            Assert.That(flow.TryRequestNewRun(), Is.True);
            files.FailNextWriteFileName =
                RunReservationRepository.TemporaryFileName;
            Assert.That(flow.TryConfirmNewRun(), Is.False);

            Assert.That(flow.IsMenuVisible, Is.True);
            Assert.That(flow.CanContinueRun, Is.True);
            Assert.That(
                files.Get(RunSaveRepository.PrimaryFileName),
                Is.EqualTo(original));
        }

        [Test]
        public void SV05_U02_BackupAndTerminalStatusesArePresentedSafely()
        {
            MemoryRunFileStore backupFiles = new MemoryRunFileStore();
            WriteStartingCheckpoint(backupFiles, "backup-run");
            backupFiles.MovePrimaryToBackup();
            RunSaveFlow backup = CreateFlow(backupFiles, "unused");
            RunSaveViewModel backupModel = RunSavePresenter.Create(backup);

            Assert.That(backup.CanContinueRun, Is.True);
            Assert.That(backupModel.StatusMessage, Does.Contain("BACKUP"));

            MemoryRunFileStore terminalFiles = new MemoryRunFileStore();
            WriteTerminalCheckpoint(terminalFiles);
            RunSaveFlow terminal = CreateFlow(terminalFiles, "unused");
            RunSaveViewModel terminalModel = RunSavePresenter.Create(terminal);

            Assert.That(terminal.CanContinueRun, Is.False);
            Assert.That(terminalModel.StatusMessage, Does.Contain("ENDED"));
        }

        [Test]
        public void SV05_U03_StartingDemonOfferHasTwoSelectableViewModels()
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            RunSaveFlow flow = CreateFlow(files, "view-run");
            Assert.That(flow.TryRequestNewRun(), Is.True);

            StageProgressionViewModel model =
                StageProgressionPresenter.Create(flow.Session);

            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanSelectStartingDemon, Is.True);
            Assert.That(model.StartingDemonOfferId, Is.Not.Null);
            Assert.That(model.StartingDemonOptions.Count, Is.EqualTo(2));
        }

        [TestCase("{invalid", "DAMAGED")]
        [TestCase("{\"reservationVersion\":999}", "NOT SUPPORTED")]
        public void SV05_U04_InvalidReservationStatusIsPresentedSafely(
            string reservationJson,
            string expectedMessage)
        {
            MemoryRunFileStore files = new MemoryRunFileStore();
            files.Put(
                RunReservationRepository.PrimaryFileName,
                reservationJson);

            RunSaveFlow flow = CreateFlow(files, "unused");
            RunSaveViewModel model = RunSavePresenter.Create(flow);

            Assert.That(flow.CanContinueRun, Is.False);
            Assert.That(flow.CanResumeReservation, Is.False);
            Assert.That(model.StatusMessage, Does.Contain(expectedMessage));
            Assert.That(model.CanStartNewRun, Is.True);
        }

        private static RunSaveFlow CreateFlow(
            MemoryRunFileStore files,
            string runId)
        {
            RunSaveRepository saveRepository = new RunSaveRepository(
                files,
                CreateStages(RootSeed));
            RunReservationRepository reservationRepository =
                new RunReservationRepository(files, DemonContractCatalog.Default);
            return new RunSaveFlow(
                saveRepository,
                reservationRepository,
                CreateStages,
                CreateSession,
                RootSeed,
                () => runId,
                () => CreatedAt);
        }

        private static StageProgressionSession CreateSession(int seed)
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
            return new StageProgressionSession(
                new RunProgress(CreateStages(seed), player),
                rewardGenerator: new BattleRewardGenerator(
                    BattleRewardCatalog.CreateDefault(),
                    unchecked(seed + 1)),
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    seed),
                startingDemonSelectionGenerator:
                    new StartingDemonSelectionGenerator(
                        DemonContractCatalog.Default,
                        unchecked(seed + 2)));
        }

        private static IReadOnlyList<StageDefinition> CreateStages(int seed)
        {
            return new[]
            {
                StageDefinition.CreateForEnemyProfile(
                    "normal-1",
                    "Normal",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.GunslingerKey,
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

        private static void WriteStartingCheckpoint(
            MemoryRunFileStore files,
            string runId)
        {
            StageProgressionSession session = CreateSession(RootSeed);
            RunSaveCoordinator coordinator = new RunSaveCoordinator(
                session,
                new RunSaveRepository(files, CreateStages(RootSeed)),
                runId,
                RootSeed,
                0,
                () => CreatedAt);
            Assert.That(coordinator.TryStartRun(), Is.True);
            StartingDemonSelectionOffer offer =
                session.PendingStartingDemonSelection;
            Assert.That(
                coordinator.TrySelectStartingDemon(
                    offer.OfferId,
                    offer.Options[0].OptionId),
                Is.True);
        }

        private static void WriteTerminalCheckpoint(MemoryRunFileStore files)
        {
            StageProgressionSession session = CreateSession(RootSeed);
            RunSaveCoordinator coordinator = new RunSaveCoordinator(
                session,
                new RunSaveRepository(files, CreateStages(RootSeed)),
                "terminal-run",
                RootSeed,
                0,
                () => CreatedAt);
            Assert.That(session.Progress.StartRun(), Is.True);
            session.Progress.Player.SetCurrentSoul(0);
            Assert.That(session.Progress.TryDefeatRun(), Is.True);
            Assert.That(coordinator.TryCheckpointRunEnd(), Is.True);
        }

        private sealed class MemoryRunFileStore : IRunSaveFileStore
        {
            private readonly Dictionary<string, string> _files =
                new Dictionary<string, string>(StringComparer.Ordinal);

            internal string FailNextWriteFileName { get; set; }

            internal string Get(string fileName)
            {
                return _files.TryGetValue(fileName, out string value)
                    ? value
                    : null;
            }

            internal void Put(string fileName, string contents)
            {
                _files[fileName] = contents;
            }

            internal void MovePrimaryToBackup()
            {
                _files[RunSaveRepository.BackupFileName] =
                    _files[RunSaveRepository.PrimaryFileName];
                _files.Remove(RunSaveRepository.PrimaryFileName);
            }

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
                        FailNextWriteFileName,
                        StringComparison.Ordinal))
                {
                    FailNextWriteFileName = null;
                    return false;
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
