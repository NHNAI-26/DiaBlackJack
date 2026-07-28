using System;

namespace Border.SaveLoad.UI
{
    public sealed class RunSaveViewModel
    {
        internal RunSaveViewModel(
            bool isMenuVisible,
            bool canStartNewRun,
            bool canContinueRun,
            bool canResumeReservation,
            bool requiresNewRunConfirmation,
            bool canRetrySave,
            bool blocksProgressionInput,
            string statusMessage,
            string saveIndicator)
        {
            IsMenuVisible = isMenuVisible;
            CanStartNewRun = canStartNewRun;
            CanContinueRun = canContinueRun;
            CanResumeReservation = canResumeReservation;
            RequiresNewRunConfirmation = requiresNewRunConfirmation;
            CanRetrySave = canRetrySave;
            BlocksProgressionInput = blocksProgressionInput;
            StatusMessage = statusMessage;
            SaveIndicator = saveIndicator;
        }

        public bool BlocksProgressionInput { get; }

        public bool CanContinueRun { get; }

        public bool CanResumeReservation { get; }

        public bool CanRetrySave { get; }

        public bool CanStartNewRun { get; }

        public bool IsMenuVisible { get; }

        public bool RequiresNewRunConfirmation { get; }

        public string SaveIndicator { get; }

        public string StatusMessage { get; }
    }

    public static class RunSavePresenter
    {
        public static RunSaveViewModel Create(RunSaveFlow flow)
        {
            if (flow == null)
            {
                throw new ArgumentNullException(nameof(flow));
            }

            return new RunSaveViewModel(
                flow.IsMenuVisible,
                flow.IsMenuVisible && !flow.RequiresNewRunConfirmation,
                flow.CanContinueRun && !flow.RequiresNewRunConfirmation,
                flow.CanResumeReservation &&
                    !flow.RequiresNewRunConfirmation,
                flow.RequiresNewRunConfirmation,
                !flow.IsMenuVisible && flow.HasPendingCheckpoint,
                flow.HasPendingCheckpoint,
                GetStatusMessage(flow.Notice),
                GetSaveIndicator(flow.Notice));
        }

        private static string GetStatusMessage(RunSaveNotice notice)
        {
            switch (notice)
            {
                case RunSaveNotice.NoSave:
                    return "NO RUN SAVE";
                case RunSaveNotice.ContinueAvailable:
                    return "RUN SAVE READY";
                case RunSaveNotice.ReservationAvailable:
                    return "STARTING DEMON SELECTION CAN BE RESUMED";
                case RunSaveNotice.BackupRecovered:
                    return "BACKUP RUN SAVE RECOVERED";
                case RunSaveNotice.TerminalSave:
                    return "PREVIOUS RUN HAS ENDED";
                case RunSaveNotice.CorruptedSave:
                    return "RUN SAVE IS DAMAGED";
                case RunSaveNotice.UnsupportedSave:
                    return "RUN SAVE VERSION IS NOT SUPPORTED";
                case RunSaveNotice.IncompatibleSave:
                    return "RUN SAVE CONTENT IS INCOMPATIBLE";
                case RunSaveNotice.NewRunConfirmationRequired:
                    return "STARTING A NEW RUN WILL REPLACE CURRENT PROGRESS";
                case RunSaveNotice.ReservationWriteFailed:
                    return "NEW RUN COULD NOT BE RESERVED";
                case RunSaveNotice.ReservationInvalid:
                    return "STARTING DEMON OFFER COULD NOT BE RESTORED";
                case RunSaveNotice.ContinueFailed:
                    return "RUN SAVE COULD NOT BE RESTORED";
                case RunSaveNotice.CheckpointSaved:
                    return "SAVE COMPLETE";
                case RunSaveNotice.CheckpointWriteFailed:
                    return "SAVE FAILED - CHECK STORAGE OR FILE ACCESS";
                default:
                    return string.Empty;
            }
        }

        private static string GetSaveIndicator(RunSaveNotice notice)
        {
            switch (notice)
            {
                case RunSaveNotice.CheckpointSaved:
                    return "SAVED";
                case RunSaveNotice.CheckpointWriteFailed:
                    return "SAVE FAILED";
                default:
                    return string.Empty;
            }
        }
    }
}
