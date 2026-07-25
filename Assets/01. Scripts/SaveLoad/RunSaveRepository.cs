using System;
using System.Collections.Generic;
using DiaBlackJack.StageProgression;

namespace Border.SaveLoad
{
    public sealed class RunSaveRepository
    {
        public const string PrimaryFileName = "run-save.json";
        public const string BackupFileName = "run-save.bak";
        public const string TemporaryFileName = "run-save.tmp";
        public const string LegacyFileName = "save.game";

        private readonly IRunSaveFileStore _fileStore;
        private readonly IReadOnlyList<StageDefinition> _stages;

        public RunSaveRepository(
            IRunSaveFileStore fileStore,
            IReadOnlyList<StageDefinition> stages)
        {
            _fileStore = fileStore ??
                throw new ArgumentNullException(nameof(fileStore));
            if (stages == null)
            {
                throw new ArgumentNullException(nameof(stages));
            }

            _stages = new List<StageDefinition>(stages).AsReadOnly();
        }

        public RunSaveWriteResult TryWrite(RunSaveSnapshot snapshot)
        {
            RunSaveValidationResult validation =
                RunSaveValidator.Validate(snapshot, _stages);
            if (!validation.IsValid)
            {
                return WriteFailure(
                    RunSaveWriteStatus.ValidationFailed,
                    validation.Error,
                    "validation-failed");
            }

            if (!RunSaveSerializer.TrySerialize(snapshot, out string json))
            {
                return WriteFailure(
                    RunSaveWriteStatus.SerializationFailed,
                    RunSaveValidationError.None,
                    "serialization-failed");
            }

            if (_fileStore.Exists(TemporaryFileName) &&
                !_fileStore.TryDelete(TemporaryFileName))
            {
                return WriteFailure(
                    RunSaveWriteStatus.TemporaryWriteFailed,
                    RunSaveValidationError.None,
                    "temporary-cleanup-failed");
            }

            if (!_fileStore.TryWrite(TemporaryFileName, json))
            {
                return WriteFailure(
                    RunSaveWriteStatus.TemporaryWriteFailed,
                    RunSaveValidationError.None,
                    "temporary-write-failed");
            }

            if (!_fileStore.TryRead(TemporaryFileName, out string verifiedJson))
            {
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunSaveWriteStatus.VerificationFailed,
                    RunSaveValidationError.None,
                    "temporary-read-failed");
            }

            if (!RunSaveSerializer.TryDeserialize(
                    verifiedJson,
                    out RunSaveSnapshot verifiedSnapshot,
                    out _))
            {
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunSaveWriteStatus.VerificationFailed,
                    RunSaveValidationError.None,
                    "temporary-deserialization-failed");
            }

            validation = RunSaveValidator.Validate(verifiedSnapshot, _stages);
            if (!validation.IsValid)
            {
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunSaveWriteStatus.VerificationFailed,
                    validation.Error,
                    "temporary-validation-failed");
            }

            bool hadPrimary = _fileStore.Exists(PrimaryFileName);
            if (hadPrimary &&
                !_fileStore.TryMove(
                    PrimaryFileName,
                    BackupFileName,
                    true))
            {
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunSaveWriteStatus.ReplaceFailed,
                    RunSaveValidationError.None,
                    "backup-replace-failed");
            }

            if (!_fileStore.TryMove(
                    TemporaryFileName,
                    PrimaryFileName,
                    false))
            {
                bool restored = !hadPrimary ||
                    _fileStore.TryMove(
                        BackupFileName,
                        PrimaryFileName,
                        true);
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunSaveWriteStatus.ReplaceFailed,
                    RunSaveValidationError.None,
                    restored
                        ? "primary-replace-failed-restored"
                        : "primary-replace-failed-restore-failed");
            }

            return new RunSaveWriteResult(
                RunSaveWriteStatus.Success,
                RunSaveValidationError.None,
                "success");
        }

        public RunSaveLoadResult Load()
        {
            FileLoadAttempt primary = TryLoadFile(PrimaryFileName);
            if (primary.IsValid)
            {
                return CreateSuccessfulLoad(
                    primary.Snapshot,
                    RunSaveLoadStatus.SuccessPrimary,
                    "primary");
            }

            FileLoadAttempt backup = TryLoadFile(BackupFileName);
            if (backup.IsValid)
            {
                return CreateSuccessfulLoad(
                    backup.Snapshot,
                    RunSaveLoadStatus.SuccessBackup,
                    "backup");
            }

            if (!primary.Exists && !backup.Exists)
            {
                return LoadLegacyFallback();
            }

            RunSaveLoadStatus failureStatus = ResolveFailureStatus(primary, backup);
            RunSaveValidationError validationError =
                primary.ValidationError != RunSaveValidationError.None
                    ? primary.ValidationError
                    : backup.ValidationError;
            return new RunSaveLoadResult(
                failureStatus,
                null,
                validationError,
                "no-valid-run-save");
        }

        private FileLoadAttempt TryLoadFile(string fileName)
        {
            if (!_fileStore.Exists(fileName))
            {
                return FileLoadAttempt.Missing();
            }

            if (!_fileStore.TryRead(fileName, out string json))
            {
                return FileLoadAttempt.Invalid(
                    RunSaveSerializationStatus.Corrupted,
                    RunSaveValidationError.None);
            }

            if (!RunSaveSerializer.TryDeserialize(
                    json,
                    out RunSaveSnapshot snapshot,
                    out RunSaveSerializationStatus serializationStatus))
            {
                return FileLoadAttempt.Invalid(
                    serializationStatus,
                    RunSaveValidationError.None);
            }

            RunSaveValidationResult validation =
                RunSaveValidator.Validate(snapshot, _stages);
            return validation.IsValid
                ? FileLoadAttempt.Valid(snapshot)
                : FileLoadAttempt.Invalid(
                    RunSaveSerializationStatus.Corrupted,
                    validation.Error);
        }

        private RunSaveLoadResult LoadLegacyFallback()
        {
            if (!_fileStore.Exists(LegacyFileName))
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.NoSave,
                    null,
                    RunSaveValidationError.None,
                    "no-save");
            }

            if (_fileStore.TryRead(LegacyFileName, out string legacyContents) &&
                string.IsNullOrWhiteSpace(legacyContents))
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.NoSave,
                    null,
                    RunSaveValidationError.None,
                    "empty-legacy-save");
            }

            return new RunSaveLoadResult(
                RunSaveLoadStatus.Corrupted,
                null,
                RunSaveValidationError.None,
                "legacy-save-not-supported");
        }

        private static RunSaveLoadResult CreateSuccessfulLoad(
            RunSaveSnapshot snapshot,
            RunSaveLoadStatus successStatus,
            string source)
        {
            if (snapshot.Status != RunSaveStatus.InProgress)
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.NoContinueTerminalSave,
                    snapshot,
                    RunSaveValidationError.None,
                    source + "-terminal");
            }

            return new RunSaveLoadResult(
                successStatus,
                snapshot,
                RunSaveValidationError.None,
                source);
        }

        private static RunSaveLoadStatus ResolveFailureStatus(
            FileLoadAttempt primary,
            FileLoadAttempt backup)
        {
            if (primary.SerializationStatus ==
                    RunSaveSerializationStatus.UnsupportedVersion ||
                backup.SerializationStatus ==
                    RunSaveSerializationStatus.UnsupportedVersion)
            {
                return RunSaveLoadStatus.UnsupportedVersion;
            }

            if (primary.SerializationStatus ==
                    RunSaveSerializationStatus.IncompatibleContent ||
                backup.SerializationStatus ==
                    RunSaveSerializationStatus.IncompatibleContent)
            {
                return RunSaveLoadStatus.IncompatibleContent;
            }

            return RunSaveLoadStatus.Corrupted;
        }

        private static RunSaveWriteResult WriteFailure(
            RunSaveWriteStatus status,
            RunSaveValidationError validationError,
            string diagnosticCode)
        {
            return new RunSaveWriteResult(status, validationError, diagnosticCode);
        }

        private sealed class FileLoadAttempt
        {
            private FileLoadAttempt(
                bool exists,
                RunSaveSnapshot snapshot,
                RunSaveSerializationStatus serializationStatus,
                RunSaveValidationError validationError)
            {
                Exists = exists;
                Snapshot = snapshot;
                SerializationStatus = serializationStatus;
                ValidationError = validationError;
            }

            internal bool Exists { get; }

            internal bool IsValid => Snapshot != null;

            internal RunSaveSnapshot Snapshot { get; }

            internal RunSaveSerializationStatus SerializationStatus { get; }

            internal RunSaveValidationError ValidationError { get; }

            internal static FileLoadAttempt Missing()
            {
                return new FileLoadAttempt(
                    false,
                    null,
                    RunSaveSerializationStatus.Corrupted,
                    RunSaveValidationError.None);
            }

            internal static FileLoadAttempt Valid(RunSaveSnapshot snapshot)
            {
                return new FileLoadAttempt(
                    true,
                    snapshot,
                    RunSaveSerializationStatus.Success,
                    RunSaveValidationError.None);
            }

            internal static FileLoadAttempt Invalid(
                RunSaveSerializationStatus serializationStatus,
                RunSaveValidationError validationError)
            {
                return new FileLoadAttempt(
                    true,
                    null,
                    serializationStatus,
                    validationError);
            }
        }
    }
}
