using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace Border.SaveLoad
{
    public sealed class RunReservationRepository
    {
        public const string PrimaryFileName = "run-reservation.json";
        public const string TemporaryFileName = "run-reservation.tmp";

        private readonly DemonContractCatalog _catalog;
        private readonly IRunSaveFileStore _fileStore;

        public RunReservationRepository(
            IRunSaveFileStore fileStore,
            DemonContractCatalog catalog)
        {
            _fileStore = fileStore ??
                throw new ArgumentNullException(nameof(fileStore));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public RunReservationWriteResult TryWrite(RunReservation reservation)
        {
            if (!IsCompatible(reservation))
            {
                return WriteFailure(
                    RunReservationWriteStatus.ValidationFailed,
                    "reservation-validation-failed");
            }

            string json;
            try
            {
                json = JsonUtility.ToJson(ToEnvelope(reservation), true);
            }
            catch (Exception)
            {
                return WriteFailure(
                    RunReservationWriteStatus.SerializationFailed,
                    "reservation-serialization-failed");
            }

            if (_fileStore.Exists(TemporaryFileName) &&
                !_fileStore.TryDelete(TemporaryFileName))
            {
                return WriteFailure(
                    RunReservationWriteStatus.TemporaryWriteFailed,
                    "reservation-temporary-cleanup-failed");
            }

            if (!_fileStore.TryWrite(TemporaryFileName, json))
            {
                return WriteFailure(
                    RunReservationWriteStatus.TemporaryWriteFailed,
                    "reservation-temporary-write-failed");
            }

            if (!_fileStore.TryRead(TemporaryFileName, out string verifiedJson) ||
                !TryDeserialize(
                    verifiedJson,
                    out RunReservation verified,
                    out RunReservationLoadStatus verificationStatus) ||
                verificationStatus != RunReservationLoadStatus.Success ||
                !AreEqual(reservation, verified))
            {
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunReservationWriteStatus.VerificationFailed,
                    "reservation-temporary-verification-failed");
            }

            if (!_fileStore.TryMove(
                    TemporaryFileName,
                    PrimaryFileName,
                    true))
            {
                _fileStore.TryDelete(TemporaryFileName);
                return WriteFailure(
                    RunReservationWriteStatus.ReplaceFailed,
                    "reservation-replace-failed");
            }

            return new RunReservationWriteResult(
                RunReservationWriteStatus.Success,
                "success");
        }

        public RunReservationLoadResult Load()
        {
            if (!_fileStore.Exists(PrimaryFileName))
            {
                return new RunReservationLoadResult(
                    RunReservationLoadStatus.NoReservation,
                    null,
                    "no-reservation");
            }

            if (!_fileStore.TryRead(PrimaryFileName, out string json))
            {
                return new RunReservationLoadResult(
                    RunReservationLoadStatus.Corrupted,
                    null,
                    "reservation-load-failed");
            }

            if (!TryDeserialize(
                    json,
                    out RunReservation reservation,
                    out RunReservationLoadStatus status))
            {
                return new RunReservationLoadResult(
                    status,
                    null,
                    "reservation-load-failed");
            }

            return new RunReservationLoadResult(
                RunReservationLoadStatus.Success,
                reservation,
                "reservation");
        }

        public bool TryDelete()
        {
            bool temporaryDeleted = !_fileStore.Exists(TemporaryFileName) ||
                _fileStore.TryDelete(TemporaryFileName);
            bool primaryDeleted = !_fileStore.Exists(PrimaryFileName) ||
                _fileStore.TryDelete(PrimaryFileName);
            return temporaryDeleted && primaryDeleted;
        }

        private bool TryDeserialize(
            string json,
            out RunReservation reservation,
            out RunReservationLoadStatus status)
        {
            reservation = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                status = RunReservationLoadStatus.Corrupted;
                return false;
            }

            RunReservationEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<RunReservationEnvelope>(json);
            }
            catch (Exception)
            {
                status = RunReservationLoadStatus.Corrupted;
                return false;
            }

            if (envelope == null)
            {
                status = RunReservationLoadStatus.Corrupted;
                return false;
            }

            if (envelope.reservationVersion != RunReservation.CurrentVersion)
            {
                status = RunReservationLoadStatus.UnsupportedVersion;
                return false;
            }

            try
            {
                reservation = new RunReservation(
                    envelope.runId,
                    envelope.rootSeed,
                    envelope.startingDemonOfferId,
                    envelope.startingDemonDefinitionKeys,
                    envelope.createdAtUtc);
            }
            catch (ArgumentException)
            {
                status = RunReservationLoadStatus.Corrupted;
                return false;
            }

            if (!IsCompatible(reservation))
            {
                reservation = null;
                status = RunReservationLoadStatus.IncompatibleContent;
                return false;
            }

            status = RunReservationLoadStatus.Success;
            return true;
        }

        private bool IsCompatible(RunReservation reservation)
        {
            if (reservation == null)
            {
                return false;
            }

            try
            {
                for (int i = 0;
                     i < reservation.StartingDemonDefinitionKeys.Count;
                     i++)
                {
                    _catalog.GetByKey(
                        reservation.StartingDemonDefinitionKeys[i]);
                }

                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool AreEqual(
            RunReservation left,
            RunReservation right)
        {
            return left != null &&
                right != null &&
                left.Version == right.Version &&
                string.Equals(left.RunId, right.RunId, StringComparison.Ordinal) &&
                left.RootSeed == right.RootSeed &&
                left.StartingDemonOfferId == right.StartingDemonOfferId &&
                string.Equals(
                    left.StartingDemonDefinitionKeys[0],
                    right.StartingDemonDefinitionKeys[0],
                    StringComparison.Ordinal) &&
                string.Equals(
                    left.StartingDemonDefinitionKeys[1],
                    right.StartingDemonDefinitionKeys[1],
                    StringComparison.Ordinal) &&
                string.Equals(
                    left.CreatedAtUtc,
                    right.CreatedAtUtc,
                    StringComparison.Ordinal);
        }

        private static RunReservationEnvelope ToEnvelope(
            RunReservation reservation)
        {
            return new RunReservationEnvelope
            {
                reservationVersion = reservation.Version,
                runId = reservation.RunId,
                rootSeed = reservation.RootSeed,
                startingDemonOfferId = reservation.StartingDemonOfferId,
                startingDemonDefinitionKeys = new[]
                {
                    reservation.StartingDemonDefinitionKeys[0],
                    reservation.StartingDemonDefinitionKeys[1]
                },
                createdAtUtc = reservation.CreatedAtUtc
            };
        }

        private static RunReservationWriteResult WriteFailure(
            RunReservationWriteStatus status,
            string diagnosticCode)
        {
            return new RunReservationWriteResult(status, diagnosticCode);
        }

        [Serializable]
        private sealed class RunReservationEnvelope
        {
            public int reservationVersion;
            public string runId;
            public int rootSeed;
            public int startingDemonOfferId;
            public string[] startingDemonDefinitionKeys;
            public string createdAtUtc;
        }
    }
}
