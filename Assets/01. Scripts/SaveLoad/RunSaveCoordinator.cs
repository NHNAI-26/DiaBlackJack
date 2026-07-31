using System;
using System.Globalization;
using DiaBlackJack.StageProgression;

namespace Border.SaveLoad
{
    public sealed class RunSaveCoordinator
    {
        private readonly RunSaveRepository _repository;
        private readonly int _rootSeed;
        private readonly string _runId;
        private readonly StageProgressionSession _session;
        private readonly Func<DateTimeOffset> _utcNowProvider;
        private long _nextSaveSequence;
        private RunSaveSnapshot _pendingSnapshot;
        private bool _terminalCheckpointCommitted;

        public RunSaveCoordinator(
            StageProgressionSession session,
            RunSaveRepository repository,
            string runId,
            int rootSeed,
            long nextSaveSequence = 0)
            : this(
                session,
                repository,
                runId,
                rootSeed,
                nextSaveSequence,
                () => DateTimeOffset.UtcNow)
        {
        }

        internal RunSaveCoordinator(
            StageProgressionSession session,
            RunSaveRepository repository,
            string runId,
            int rootSeed,
            long nextSaveSequence,
            Func<DateTimeOffset> utcNowProvider)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _repository = repository ??
                throw new ArgumentNullException(nameof(repository));
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException(
                    "Run id cannot be empty.",
                    nameof(runId));
            }

            if (nextSaveSequence < 0 || nextSaveSequence == long.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nextSaveSequence),
                    "Next save sequence must be non-negative and incrementable.");
            }

            _utcNowProvider = utcNowProvider ??
                throw new ArgumentNullException(nameof(utcNowProvider));
            _runId = runId;
            _rootSeed = rootSeed;
            _nextSaveSequence = nextSaveSequence;
        }

        public bool HasPendingCheckpoint => _pendingSnapshot != null;

        public RunSaveWriteResult LastWriteResult { get; private set; }

        public long NextSaveSequence => _nextSaveSequence;

        public bool TryStartRun()
        {
            return !HasPendingCheckpoint && _session.TryStartRun();
        }

        public bool TryAdvanceToNextStage()
        {
            return !HasPendingCheckpoint && _session.TryAdvanceToNextStage();
        }

        public bool TryCompleteStartingDemonReveal()
        {
            if (!CanCreateCheckpoint())
            {
                return false;
            }

            string savedAtUtc = GetSavedAtUtc();
            if (!_session.TryCompleteStartingDemonReveal())
            {
                return false;
            }

            SaveCheckpoint(
                RunCheckpointKind.StartingDemonGranted,
                ResolveStartingNextContent(),
                savedAtUtc);
            return true;
        }

        public bool TrySelectBattleReward(
            int optionId,
            string nextContentKind)
        {
            if (!TryResolveRewardCheckpoint(
                    nextContentKind,
                    out RunCheckpointKind checkpointKind,
                    out string resolvedNextContent))
            {
                return false;
            }

            string savedAtUtc = GetSavedAtUtc();
            if (!_session.TrySelectBattleReward(optionId))
            {
                return false;
            }

            SaveCheckpoint(checkpointKind, resolvedNextContent, savedAtUtc);
            return true;
        }

        public bool TrySkipBattleReward(string nextContentKind)
        {
            if (!TryResolveRewardCheckpoint(
                    nextContentKind,
                    out RunCheckpointKind checkpointKind,
                    out string resolvedNextContent))
            {
                return false;
            }

            string savedAtUtc = GetSavedAtUtc();
            if (!_session.TrySkipBattleReward())
            {
                return false;
            }

            SaveCheckpoint(checkpointKind, resolvedNextContent, savedAtUtc);
            return true;
        }

        public bool TryCheckpointRunEnd()
        {
            return TryCheckpointRunEnd(0, 0);
        }

        public bool TryCheckpointCombatSettlement(
            int completedShopCount,
            int utilityPriceLevel)
        {
            if (!CanCreateCheckpoint() ||
                _session.Progress.State != StageProgressionState.StageCleared ||
                completedShopCount < 0 ||
                utilityPriceLevel < 0)
            {
                return false;
            }

            SaveCheckpoint(
                RunCheckpointKind.CombatSettlementCompleted,
                RunNextContentKind.Shop,
                GetSavedAtUtc(),
                completedShopCount,
                utilityPriceLevel);
            return true;
        }

        public bool TryCheckpointShopExit(
            int completedShopCount,
            int utilityPriceLevel)
        {
            if (!CanCreateCheckpoint() ||
                _session.Progress.State != StageProgressionState.StageCleared ||
                completedShopCount <= 0 ||
                utilityPriceLevel < 0)
            {
                return false;
            }

            int nextStageIndex = _session.Progress.CurrentStageIndex + 1;
            if (nextStageIndex >= _session.Progress.Stages.Count)
            {
                return false;
            }

            StageDefinition nextStage = _session.Progress.Stages[nextStageIndex];
            string nextContentKind = nextStage.Kind == StageKind.FinalBossCombat
                ? RunNextContentKind.Boss
                : _session.IsOpponentSelectionEnabled
                    ? RunNextContentKind.OpponentSelection
                    : RunNextContentKind.Battle;
            SaveCheckpoint(
                RunCheckpointKind.ShopExited,
                nextContentKind,
                GetSavedAtUtc(),
                completedShopCount,
                utilityPriceLevel);
            return true;
        }

        public bool TryCheckpointRunEnd(
            int completedShopCount,
            int utilityPriceLevel)
        {
            if (!CanCreateCheckpoint() ||
                _terminalCheckpointCommitted ||
                completedShopCount < 0 ||
                utilityPriceLevel < 0 ||
                (_session.Progress.State != StageProgressionState.RunVictory &&
                 _session.Progress.State != StageProgressionState.RunDefeat))
            {
                return false;
            }

            SaveCheckpoint(
                RunCheckpointKind.RunEnded,
                RunNextContentKind.Result,
                GetSavedAtUtc(),
                completedShopCount,
                utilityPriceLevel);
            return true;
        }

        public bool TryRetryPendingCheckpoint()
        {
            if (_pendingSnapshot == null)
            {
                return false;
            }

            RunSaveSnapshot snapshot = _pendingSnapshot;
            LastWriteResult = _repository.TryWrite(snapshot);
            if (!LastWriteResult.IsSuccess)
            {
                return false;
            }

            CommitCheckpoint(snapshot);
            return true;
        }

        private string ResolveStartingNextContent()
        {
            return _session.IsOpponentSelectionEnabled &&
                _session.Progress.CurrentStage.Kind != StageKind.FinalBossCombat
                    ? RunNextContentKind.OpponentSelection
                    : RunNextContentKind.Battle;
        }

        private bool TryResolveRewardCheckpoint(
            string nextContentKind,
            out RunCheckpointKind checkpointKind,
            out string resolvedNextContent)
        {
            checkpointKind = RunCheckpointKind.CombatSettlementCompleted;
            resolvedNextContent = nextContentKind;
            if (!CanCreateCheckpoint())
            {
                return false;
            }

            PendingBattleReward pending = _session.Progress.PendingReward;
            if (pending == null)
            {
                return false;
            }

            if (pending.CompletionTarget == BattleRewardCompletionTarget.RunVictory)
            {
                if (!string.Equals(
                        nextContentKind,
                        RunNextContentKind.Result,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                checkpointKind = RunCheckpointKind.RunEnded;
                resolvedNextContent = RunNextContentKind.Result;
                return true;
            }

            return string.Equals(
                    nextContentKind,
                    RunNextContentKind.Shop,
                    StringComparison.Ordinal) ||
                string.Equals(
                    nextContentKind,
                    RunNextContentKind.Event,
                    StringComparison.Ordinal);
        }

        private bool CanCreateCheckpoint()
        {
            return !HasPendingCheckpoint && _nextSaveSequence < long.MaxValue;
        }

        private void SaveCheckpoint(
            RunCheckpointKind checkpointKind,
            string nextContentKind,
            string savedAtUtc,
            int completedShopCount = 0,
            int utilityPriceLevel = 0)
        {
            bool captured = RunSaveCapture.TryCapture(
                _session,
                new RunSaveCaptureContext(
                    _nextSaveSequence,
                    _runId,
                    savedAtUtc,
                    checkpointKind,
                    _rootSeed,
                    nextContentKind,
                    completedShopCount,
                    utilityPriceLevel),
                out RunSaveSnapshot snapshot,
                out RunSaveValidationResult validation);
            if (!captured)
            {
                throw new InvalidOperationException(
                    "A validated stable transition could not be captured: " +
                    validation.Error);
            }

            LastWriteResult = _repository.TryWrite(snapshot);
            if (LastWriteResult.IsSuccess)
            {
                CommitCheckpoint(snapshot);
                return;
            }

            _pendingSnapshot = snapshot;
        }

        private void CommitCheckpoint(RunSaveSnapshot snapshot)
        {
            _pendingSnapshot = null;
            _nextSaveSequence++;
            if (snapshot.CheckpointKind == RunCheckpointKind.RunEnded)
            {
                _terminalCheckpointCommitted = true;
            }
        }

        private string GetSavedAtUtc()
        {
            return _utcNowProvider()
                .ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
