using DiaBlackJack.StageProgression;

namespace Border.SaveLoad
{
    public enum RunSaveWriteStatus
    {
        Success,
        ValidationFailed,
        SerializationFailed,
        TemporaryWriteFailed,
        VerificationFailed,
        ReplaceFailed
    }

    public enum RunSaveLoadStatus
    {
        SuccessPrimary,
        SuccessBackup,
        NoContinueTerminalSave,
        NoSave,
        ReservationOnly,
        Corrupted,
        UnsupportedVersion,
        IncompatibleContent
    }

    public sealed class RunSaveWriteResult
    {
        internal RunSaveWriteResult(
            RunSaveWriteStatus status,
            RunSaveValidationError validationError,
            string diagnosticCode)
        {
            Status = status;
            ValidationError = validationError;
            DiagnosticCode = diagnosticCode;
        }

        public RunSaveWriteStatus Status { get; }

        public RunSaveValidationError ValidationError { get; }

        public string DiagnosticCode { get; }

        public bool IsSuccess => Status == RunSaveWriteStatus.Success;
    }

    public sealed class RunSaveLoadResult
    {
        internal RunSaveLoadResult(
            RunSaveLoadStatus status,
            RunSaveSnapshot snapshot,
            RunSaveValidationError validationError,
            string diagnosticCode)
        {
            Status = status;
            Snapshot = snapshot;
            ValidationError = validationError;
            DiagnosticCode = diagnosticCode;
        }

        public RunSaveLoadStatus Status { get; }

        public RunSaveSnapshot Snapshot { get; }

        public RunSaveValidationError ValidationError { get; }

        public string DiagnosticCode { get; }

        public bool CanContinue =>
            Status == RunSaveLoadStatus.SuccessPrimary ||
            Status == RunSaveLoadStatus.SuccessBackup;
    }

    internal enum RunSaveSerializationStatus
    {
        Success,
        Corrupted,
        UnsupportedVersion,
        IncompatibleContent
    }
}
