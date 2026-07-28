using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Border.SaveLoad
{
    public sealed class RunReservation
    {
        public const int CurrentVersion = 1;

        private readonly ReadOnlyCollection<string> _startingDemonDefinitionKeys;

        public RunReservation(
            string runId,
            int rootSeed,
            int startingDemonOfferId,
            IEnumerable<string> startingDemonDefinitionKeys,
            string createdAtUtc)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException(
                    "Run id cannot be empty.",
                    nameof(runId));
            }

            if (startingDemonOfferId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingDemonOfferId));
            }

            if (startingDemonDefinitionKeys == null)
            {
                throw new ArgumentNullException(
                    nameof(startingDemonDefinitionKeys));
            }

            List<string> keys = new List<string>(startingDemonDefinitionKeys);
            if (keys.Count != 2 ||
                string.IsNullOrWhiteSpace(keys[0]) ||
                string.IsNullOrWhiteSpace(keys[1]) ||
                string.Equals(keys[0], keys[1], StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A run reservation requires two distinct demon keys.",
                    nameof(startingDemonDefinitionKeys));
            }

            if (!DateTimeOffset.TryParse(
                    createdAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                throw new ArgumentException(
                    "Reservation creation time must be ISO-8601.",
                    nameof(createdAtUtc));
            }

            Version = CurrentVersion;
            RunId = runId;
            RootSeed = rootSeed;
            StartingDemonOfferId = startingDemonOfferId;
            _startingDemonDefinitionKeys = keys.AsReadOnly();
            CreatedAtUtc = createdAtUtc;
        }

        public string CreatedAtUtc { get; }

        public int RootSeed { get; }

        public string RunId { get; }

        public IReadOnlyList<string> StartingDemonDefinitionKeys =>
            _startingDemonDefinitionKeys;

        public int StartingDemonOfferId { get; }

        public int Version { get; }
    }

    public enum RunReservationWriteStatus
    {
        Success,
        ValidationFailed,
        SerializationFailed,
        TemporaryWriteFailed,
        VerificationFailed,
        ReplaceFailed
    }

    public enum RunReservationLoadStatus
    {
        Success,
        NoReservation,
        Corrupted,
        UnsupportedVersion,
        IncompatibleContent
    }

    public sealed class RunReservationWriteResult
    {
        internal RunReservationWriteResult(
            RunReservationWriteStatus status,
            string diagnosticCode)
        {
            Status = status;
            DiagnosticCode = diagnosticCode;
        }

        public string DiagnosticCode { get; }

        public bool IsSuccess => Status == RunReservationWriteStatus.Success;

        public RunReservationWriteStatus Status { get; }
    }

    public sealed class RunReservationLoadResult
    {
        internal RunReservationLoadResult(
            RunReservationLoadStatus status,
            RunReservation reservation,
            string diagnosticCode)
        {
            Status = status;
            Reservation = reservation;
            DiagnosticCode = diagnosticCode;
        }

        public bool CanResume => Status == RunReservationLoadStatus.Success;

        public string DiagnosticCode { get; }

        public RunReservation Reservation { get; }

        public RunReservationLoadStatus Status { get; }
    }
}
