using System;
using System.Collections.Generic;
using System.Globalization;
using DiaBlackJack.StageProgression;

namespace Border.SaveLoad
{
    public enum RunSaveNotice
    {
        None,
        NoSave,
        ContinueAvailable,
        ReservationAvailable,
        BackupRecovered,
        TerminalSave,
        CorruptedSave,
        UnsupportedSave,
        IncompatibleSave,
        NewRunConfirmationRequired,
        ReservationWriteFailed,
        ReservationInvalid,
        ContinueFailed,
        CheckpointSaved,
        CheckpointWriteFailed
    }

    public sealed class RunSaveFlow
    {
        private readonly int _defaultRootSeed;
        private readonly Func<int> _rootSeedFactory;
        private readonly Func<string> _runIdFactory;
        private readonly RunReservationRepository _reservationRepository;
        private readonly RunSaveRepository _saveRepository;
        private readonly Func<int, StageProgressionSession> _sessionFactory;
        private readonly Func<int, IReadOnlyList<StageDefinition>> _stagePathFactory;
        private readonly Func<DateTimeOffset> _utcNowProvider;
        private readonly bool _usesBattleRewards;
        private bool _reservationBlocksSavedRun;

        public RunSaveFlow(
            RunSaveRepository saveRepository,
            RunReservationRepository reservationRepository,
            Func<int, IReadOnlyList<StageDefinition>> stagePathFactory,
            Func<int, StageProgressionSession> sessionFactory,
            int defaultRootSeed,
            bool usesBattleRewards = true)
            : this(
                saveRepository,
                reservationRepository,
                stagePathFactory,
                sessionFactory,
                defaultRootSeed,
                CreateRandomRootSeed,
                () => Guid.NewGuid().ToString("N"),
                () => DateTimeOffset.UtcNow,
                usesBattleRewards)
        {
        }

        internal RunSaveFlow(
            RunSaveRepository saveRepository,
            RunReservationRepository reservationRepository,
            Func<int, IReadOnlyList<StageDefinition>> stagePathFactory,
            Func<int, StageProgressionSession> sessionFactory,
            int defaultRootSeed,
            Func<string> runIdFactory,
            Func<DateTimeOffset> utcNowProvider,
            bool usesBattleRewards = true)
            : this(
                saveRepository,
                reservationRepository,
                stagePathFactory,
                sessionFactory,
                defaultRootSeed,
                () => defaultRootSeed,
                runIdFactory,
                utcNowProvider,
                usesBattleRewards)
        {
        }

        internal RunSaveFlow(
            RunSaveRepository saveRepository,
            RunReservationRepository reservationRepository,
            Func<int, IReadOnlyList<StageDefinition>> stagePathFactory,
            Func<int, StageProgressionSession> sessionFactory,
            int defaultRootSeed,
            Func<int> rootSeedFactory,
            Func<string> runIdFactory,
            Func<DateTimeOffset> utcNowProvider,
            bool usesBattleRewards = true)
        {
            _saveRepository = saveRepository ??
                throw new ArgumentNullException(nameof(saveRepository));
            _reservationRepository = reservationRepository ??
                throw new ArgumentNullException(nameof(reservationRepository));
            _stagePathFactory = stagePathFactory ??
                throw new ArgumentNullException(nameof(stagePathFactory));
            _sessionFactory = sessionFactory ??
                throw new ArgumentNullException(nameof(sessionFactory));
            _rootSeedFactory = rootSeedFactory ??
                throw new ArgumentNullException(nameof(rootSeedFactory));
            _runIdFactory = runIdFactory ??
                throw new ArgumentNullException(nameof(runIdFactory));
            _utcNowProvider = utcNowProvider ??
                throw new ArgumentNullException(nameof(utcNowProvider));
            _defaultRootSeed = defaultRootSeed;
            _usesBattleRewards = usesBattleRewards;
            ActiveRootSeed = defaultRootSeed;

            Session = _sessionFactory(defaultRootSeed) ??
                throw new InvalidOperationException(
                    "Session factory returned null.");
            IsMenuVisible = true;
            RefreshMenuState();
        }

        public bool CanContinueRun =>
            IsMenuVisible &&
            !_reservationBlocksSavedRun &&
            LastLoadResult != null &&
            LastLoadResult.CanContinue;

        public int ActiveRootSeed { get; private set; }

        public bool CanResumeReservation =>
            IsMenuVisible &&
            _reservationBlocksSavedRun &&
            LastReservationLoadResult != null &&
            LastReservationLoadResult.CanResume;

        public RunSaveCoordinator Coordinator { get; private set; }

        public bool HasPendingCheckpoint =>
            Coordinator != null && Coordinator.HasPendingCheckpoint;

        public bool IsMenuVisible { get; private set; }

        public RunSaveLoadResult LastLoadResult { get; private set; }

        public RunReservationLoadResult LastReservationLoadResult
        {
            get;
            private set;
        }

        public RunReservationWriteResult LastReservationWriteResult
        {
            get;
            private set;
        }

        public RunSaveNotice Notice { get; private set; }

        public int RestoredCompletedShopCount { get; private set; }

        public int RestoredUtilityPriceLevel { get; private set; }

        public bool RequiresNewRunConfirmation { get; private set; }

        public StageProgressionSession Session { get; private set; }

        public bool TryRequestNewRun()
        {
            if (!IsMenuVisible || RequiresNewRunConfirmation)
            {
                return false;
            }

            if (CanContinueRun || CanResumeReservation)
            {
                RequiresNewRunConfirmation = true;
                Notice = RunSaveNotice.NewRunConfirmationRequired;
                return true;
            }

            return TryCreateAndActivateReservation();
        }

        public bool TryConfirmNewRun()
        {
            if (!IsMenuVisible || !RequiresNewRunConfirmation)
            {
                return false;
            }

            RequiresNewRunConfirmation = false;
            return TryCreateAndActivateReservation();
        }

        public bool TryCancelNewRun()
        {
            if (!IsMenuVisible || !RequiresNewRunConfirmation)
            {
                return false;
            }

            RequiresNewRunConfirmation = false;
            SetMenuNotice();
            return true;
        }

        public bool TryResumeReservation()
        {
            if (!CanResumeReservation)
            {
                return false;
            }

            RunReservation reservation =
                LastReservationLoadResult.Reservation;
            if (!TryCreateReservedSession(
                    reservation,
                    out StageProgressionSession session))
            {
                Notice = RunSaveNotice.ReservationInvalid;
                return false;
            }

            ActivateReservation(reservation, session);
            Notice = RunSaveNotice.ReservationAvailable;
            return true;
        }

        public bool TryContinueRun()
        {
            if (!CanContinueRun ||
                LastLoadResult.Snapshot.SaveSequence == long.MaxValue)
            {
                return false;
            }

            RunRestoreFactory restoreFactory = new RunRestoreFactory(
                _stagePathFactory,
                usesBattleRewards: _usesBattleRewards);
            if (!restoreFactory.TryRestore(
                    LastLoadResult.Snapshot,
                    out RunRestoreResult restored,
                    out _))
            {
                Notice = RunSaveNotice.ContinueFailed;
                return false;
            }

            RunSaveSnapshot snapshot = LastLoadResult.Snapshot;
            RunSaveCoordinator coordinator = new RunSaveCoordinator(
                restored.Session,
                _saveRepository,
                snapshot.RunId,
                snapshot.RootSeed,
                snapshot.SaveSequence + 1,
                _utcNowProvider);
            if (snapshot.CheckpointKind ==
                    RunCheckpointKind.StartingDemonSelected &&
                !coordinator.TryStartRun())
            {
                Notice = RunSaveNotice.ContinueFailed;
                return false;
            }

            Session = restored.Session;
            Coordinator = coordinator;
            RestoredCompletedShopCount = restored.CompletedShopCount;
            RestoredUtilityPriceLevel = restored.UtilityPriceLevel;
            ActiveRootSeed = snapshot.RootSeed;
            IsMenuVisible = false;
            RequiresNewRunConfirmation = false;
            Notice = LastLoadResult.Status == RunSaveLoadStatus.SuccessBackup
                ? RunSaveNotice.BackupRecovered
                : RunSaveNotice.ContinueAvailable;
            return true;
        }

        public bool TryOpenRunMenu()
        {
            if (IsMenuVisible ||
                HasPendingCheckpoint ||
                (Session.Progress.State != StageProgressionState.RunVictory &&
                 Session.Progress.State != StageProgressionState.RunDefeat))
            {
                return false;
            }

            IsMenuVisible = true;
            RequiresNewRunConfirmation = false;
            RefreshMenuState();
            return true;
        }

        public bool TryStartRun()
        {
            return !IsMenuVisible &&
                Coordinator != null &&
                Coordinator.TryStartRun();
        }

        public bool TryAdvanceToNextStage()
        {
            return !IsMenuVisible &&
                Coordinator != null &&
                Coordinator.TryAdvanceToNextStage();
        }

        public bool TrySelectStartingDemon(int offerId, int optionId)
        {
            if (IsMenuVisible ||
                Coordinator == null ||
                !Coordinator.TrySelectStartingDemon(offerId, optionId))
            {
                return false;
            }

            UpdateCheckpointNotice();
            if (!HasPendingCheckpoint)
            {
                CompleteStartingCheckpoint();
            }

            return true;
        }

        public bool TrySelectBattleReward(int optionId)
        {
            PendingBattleReward pending = Session.Progress.PendingReward;
            if (IsMenuVisible || pending == null || Coordinator == null)
            {
                return false;
            }

            string nextContentKind = pending.CompletionTarget ==
                BattleRewardCompletionTarget.RunVictory
                    ? RunNextContentKind.Result
                    : RunNextContentKind.Shop;
            if (!Coordinator.TrySelectBattleReward(
                    optionId,
                    nextContentKind))
            {
                return false;
            }

            UpdateCheckpointNotice();
            return true;
        }

        public bool TrySkipBattleReward()
        {
            PendingBattleReward pending = Session.Progress.PendingReward;
            if (IsMenuVisible || pending == null || Coordinator == null)
            {
                return false;
            }

            string nextContentKind = pending.CompletionTarget ==
                BattleRewardCompletionTarget.RunVictory
                    ? RunNextContentKind.Result
                    : RunNextContentKind.Shop;
            if (!Coordinator.TrySkipBattleReward(nextContentKind))
            {
                return false;
            }

            UpdateCheckpointNotice();
            return true;
        }

        public bool TryCheckpointRunEnd()
        {
            if (IsMenuVisible ||
                Coordinator == null ||
                !Coordinator.TryCheckpointRunEnd())
            {
                return false;
            }

            UpdateCheckpointNotice();
            return true;
        }

        public bool TryCheckpointCombatSettlement(
            int completedShopCount,
            int utilityPriceLevel)
        {
            if (IsMenuVisible ||
                Coordinator == null ||
                !Coordinator.TryCheckpointCombatSettlement(
                    completedShopCount,
                    utilityPriceLevel))
            {
                return false;
            }

            UpdateCheckpointNotice();
            return true;
        }

        public bool TryCheckpointShopExit(
            int completedShopCount,
            int utilityPriceLevel)
        {
            if (IsMenuVisible ||
                Coordinator == null ||
                !Coordinator.TryCheckpointShopExit(
                    completedShopCount,
                    utilityPriceLevel))
            {
                return false;
            }

            UpdateCheckpointNotice();
            return true;
        }

        public bool TryCheckpointFormalRunEnd(
            int completedShopCount,
            int utilityPriceLevel)
        {
            if (IsMenuVisible ||
                Coordinator == null ||
                !Coordinator.TryCheckpointRunEnd(
                    completedShopCount,
                    utilityPriceLevel))
            {
                return false;
            }

            UpdateCheckpointNotice();
            return true;
        }

        public bool TryRetryPendingCheckpoint()
        {
            if (IsMenuVisible ||
                Coordinator == null ||
                !Coordinator.TryRetryPendingCheckpoint())
            {
                Notice = RunSaveNotice.CheckpointWriteFailed;
                return false;
            }

            Notice = RunSaveNotice.CheckpointSaved;
            if (Session.Progress.State == StageProgressionState.NotStarted &&
                Session.Progress.Player.StartingDemonDefinitionKey != null)
            {
                CompleteStartingCheckpoint();
            }

            return true;
        }

        private bool TryCreateAndActivateReservation()
        {
            int rootSeed = _rootSeedFactory();
            StageProgressionSession candidate =
                _sessionFactory(rootSeed);
            if (candidate == null || !candidate.TryStartRun())
            {
                Notice = RunSaveNotice.ReservationInvalid;
                return false;
            }

            StartingDemonSelectionOffer offer =
                candidate.PendingStartingDemonSelection;
            if (offer == null)
            {
                ActivateNewRun(candidate, rootSeed);
                Notice = RunSaveNotice.None;
                return true;
            }

            if (offer.Options.Count != 2)
            {
                Notice = RunSaveNotice.ReservationInvalid;
                return false;
            }

            RunReservation reservation;
            try
            {
                reservation = new RunReservation(
                    _runIdFactory(),
                    rootSeed,
                    offer.OfferId,
                    new[]
                    {
                        offer.Options[0].DefinitionKey,
                        offer.Options[1].DefinitionKey
                    },
                    GetCreatedAtUtc());
            }
            catch (ArgumentException)
            {
                Notice = RunSaveNotice.ReservationInvalid;
                return false;
            }

            LastReservationWriteResult =
                _reservationRepository.TryWrite(reservation);
            if (!LastReservationWriteResult.IsSuccess)
            {
                Notice = RunSaveNotice.ReservationWriteFailed;
                RefreshMenuFilesWithoutReplacingNotice();
                return false;
            }

            LastReservationLoadResult = _reservationRepository.Load();
            ActivateReservation(reservation, candidate);
            Notice = RunSaveNotice.ReservationAvailable;
            return true;
        }

        private bool TryCreateReservedSession(
            RunReservation reservation,
            out StageProgressionSession session)
        {
            session = null;
            if (reservation == null)
            {
                return false;
            }

            StageProgressionSession candidate =
                _sessionFactory(reservation.RootSeed);
            if (candidate == null || !candidate.TryStartRun())
            {
                return false;
            }

            StartingDemonSelectionOffer offer =
                candidate.PendingStartingDemonSelection;
            if (offer == null ||
                offer.OfferId != reservation.StartingDemonOfferId ||
                offer.Options.Count != 2 ||
                !string.Equals(
                    offer.Options[0].DefinitionKey,
                    reservation.StartingDemonDefinitionKeys[0],
                    StringComparison.Ordinal) ||
                !string.Equals(
                    offer.Options[1].DefinitionKey,
                    reservation.StartingDemonDefinitionKeys[1],
                    StringComparison.Ordinal))
            {
                return false;
            }

            session = candidate;
            return true;
        }

        private void ActivateReservation(
            RunReservation reservation,
            StageProgressionSession session)
        {
            Session = session;
            Coordinator = new RunSaveCoordinator(
                session,
                _saveRepository,
                reservation.RunId,
                reservation.RootSeed,
                0,
                _utcNowProvider);
            IsMenuVisible = false;
            RequiresNewRunConfirmation = false;
            RestoredCompletedShopCount = 0;
            RestoredUtilityPriceLevel = 0;
            ActiveRootSeed = reservation.RootSeed;
        }

        private void ActivateNewRun(
            StageProgressionSession session,
            int rootSeed)
        {
            _reservationRepository.TryDelete();
            Session = session;
            Coordinator = new RunSaveCoordinator(
                session,
                _saveRepository,
                _runIdFactory(),
                rootSeed,
                0,
                _utcNowProvider);
            IsMenuVisible = false;
            RequiresNewRunConfirmation = false;
            RestoredCompletedShopCount = 0;
            RestoredUtilityPriceLevel = 0;
            ActiveRootSeed = rootSeed;
        }

        private void CompleteStartingCheckpoint()
        {
            _reservationRepository.TryDelete();
            if (!Coordinator.TryStartRun())
            {
                throw new InvalidOperationException(
                    "A saved starting selection could not prepare the first stage.");
            }
        }

        private void UpdateCheckpointNotice()
        {
            Notice = HasPendingCheckpoint
                ? RunSaveNotice.CheckpointWriteFailed
                : RunSaveNotice.CheckpointSaved;
        }

        private void RefreshMenuState()
        {
            RefreshMenuFilesWithoutReplacingNotice();
            SetMenuNotice();
        }

        private void RefreshMenuFilesWithoutReplacingNotice()
        {
            LastReservationLoadResult = _reservationRepository.Load();
            LastLoadResult = _saveRepository.Load();
            bool sameRun = LastReservationLoadResult.CanResume &&
                LastLoadResult.Snapshot != null &&
                string.Equals(
                    LastReservationLoadResult.Reservation.RunId,
                    LastLoadResult.Snapshot.RunId,
                    StringComparison.Ordinal);
            if (sameRun)
            {
                _reservationRepository.TryDelete();
            }

            _reservationBlocksSavedRun =
                LastReservationLoadResult.Status !=
                    RunReservationLoadStatus.NoReservation &&
                !sameRun;
        }

        private void SetMenuNotice()
        {
            if (_reservationBlocksSavedRun)
            {
                switch (LastReservationLoadResult.Status)
                {
                    case RunReservationLoadStatus.Success:
                        Notice = RunSaveNotice.ReservationAvailable;
                        return;
                    case RunReservationLoadStatus.UnsupportedVersion:
                        Notice = RunSaveNotice.UnsupportedSave;
                        return;
                    case RunReservationLoadStatus.IncompatibleContent:
                        Notice = RunSaveNotice.IncompatibleSave;
                        return;
                    default:
                        Notice = RunSaveNotice.CorruptedSave;
                        return;
                }
            }

            switch (LastLoadResult.Status)
            {
                case RunSaveLoadStatus.SuccessPrimary:
                    Notice = RunSaveNotice.ContinueAvailable;
                    break;
                case RunSaveLoadStatus.SuccessBackup:
                    Notice = RunSaveNotice.BackupRecovered;
                    break;
                case RunSaveLoadStatus.NoContinueTerminalSave:
                    Notice = RunSaveNotice.TerminalSave;
                    break;
                case RunSaveLoadStatus.UnsupportedVersion:
                    Notice = RunSaveNotice.UnsupportedSave;
                    break;
                case RunSaveLoadStatus.IncompatibleContent:
                    Notice = RunSaveNotice.IncompatibleSave;
                    break;
                case RunSaveLoadStatus.Corrupted:
                    Notice = RunSaveNotice.CorruptedSave;
                    break;
                default:
                    Notice = RunSaveNotice.NoSave;
                    break;
            }
        }

        private string GetCreatedAtUtc()
        {
            return _utcNowProvider()
                .ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture);
        }

        private static int CreateRandomRootSeed()
        {
            return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        }
    }
}
