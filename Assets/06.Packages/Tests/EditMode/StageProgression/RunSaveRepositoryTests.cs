using System;
using System.Collections.Generic;
using Border.SaveLoad;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class RunSaveRepositoryTests
    {
        private const string SavedAtUtc = "2026-07-26T00:00:00.0000000+00:00";

        [Test]
        public void SV02_U01_VersionOneJsonRoundTripPreservesEveryField()
        {
            RunSaveSnapshot source = CreateSnapshot();

            bool serialized = RunSaveSerializer.TrySerialize(source, out string json);
            bool deserialized = RunSaveSerializer.TryDeserialize(
                json,
                out RunSaveSnapshot result,
                out RunSaveSerializationStatus status);

            Assert.That(serialized, Is.True);
            Assert.That(deserialized, Is.True);
            Assert.That(status, Is.EqualTo(RunSaveSerializationStatus.Success));
            Assert.That(json, Does.Contain("\"checkpointKind\":\"combat-settlement-completed\""));
            Assert.That(json, Does.Contain("\"status\":\"in-progress\""));
            Assert.That(json, Does.Contain("\"suit\":\"spade\""));
            Assert.That(json, Does.Contain("\"suit\":\"clover\""));
            AssertSnapshot(result);
        }

        [Test]
        public void SV02_U02_SuccessfulWriteVerifiesTempBeforeReplacingPrimary()
        {
            MemoryRunSaveFileStore files = new MemoryRunSaveFileStore();
            files.Set(RunSaveRepository.PrimaryFileName, "previous-save");
            RunSaveRepository repository =
                new RunSaveRepository(files, CreateStages());

            RunSaveWriteResult result = repository.TryWrite(CreateSnapshot());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                files.Operations,
                Is.EqualTo(new[]
                {
                    "Exists:run-save.tmp",
                    "Write:run-save.tmp",
                    "Read:run-save.tmp",
                    "Exists:run-save.json",
                    "Move:run-save.json->run-save.bak:True",
                    "Move:run-save.tmp->run-save.json:False"
                }));
            Assert.That(files.Get(RunSaveRepository.PrimaryFileName), Does.Contain("\"runId\":\"run-001\""));
            Assert.That(files.Get(RunSaveRepository.BackupFileName), Is.EqualTo("previous-save"));
            Assert.That(files.ExistsWithoutLog(RunSaveRepository.TemporaryFileName), Is.False);
        }

        [Test]
        public void SV02_U03_TemporaryWriteFailurePreservesPrimary()
        {
            MemoryRunSaveFileStore files = new MemoryRunSaveFileStore();
            files.Set(RunSaveRepository.PrimaryFileName, "previous-save");
            files.FailOnce("Write:run-save.tmp");
            RunSaveRepository repository =
                new RunSaveRepository(files, CreateStages());

            RunSaveWriteResult result = repository.TryWrite(CreateSnapshot());

            Assert.That(
                result.Status,
                Is.EqualTo(RunSaveWriteStatus.TemporaryWriteFailed));
            Assert.That(files.Get(RunSaveRepository.PrimaryFileName), Is.EqualTo("previous-save"));
            Assert.That(files.ExistsWithoutLog(RunSaveRepository.BackupFileName), Is.False);
        }

        [Test]
        public void SV02_U04_FinalReplaceFailureRestoresPreviousPrimary()
        {
            MemoryRunSaveFileStore files = new MemoryRunSaveFileStore();
            files.Set(RunSaveRepository.PrimaryFileName, "previous-save");
            files.FailOnce("Move:run-save.tmp->run-save.json:False");
            RunSaveRepository repository =
                new RunSaveRepository(files, CreateStages());

            RunSaveWriteResult result = repository.TryWrite(CreateSnapshot());

            Assert.That(result.Status, Is.EqualTo(RunSaveWriteStatus.ReplaceFailed));
            Assert.That(
                result.DiagnosticCode,
                Is.EqualTo("primary-replace-failed-restored"));
            Assert.That(files.Get(RunSaveRepository.PrimaryFileName), Is.EqualTo("previous-save"));
            Assert.That(files.ExistsWithoutLog(RunSaveRepository.TemporaryFileName), Is.False);
        }

        [Test]
        public void SV02_U05_CorruptedPrimaryLoadsValidBackup()
        {
            MemoryRunSaveFileStore files = new MemoryRunSaveFileStore();
            Assert.That(
                RunSaveSerializer.TrySerialize(CreateSnapshot(), out string backupJson),
                Is.True);
            files.Set(RunSaveRepository.PrimaryFileName, "{broken");
            files.Set(RunSaveRepository.BackupFileName, backupJson);
            RunSaveRepository repository =
                new RunSaveRepository(files, CreateStages());

            RunSaveLoadResult result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.SuccessBackup));
            Assert.That(result.CanContinue, Is.True);
            Assert.That(result.Snapshot.RunId, Is.EqualTo("run-001"));
        }

        [Test]
        public void SV02_U06_BothCorruptedFilesReturnExplicitFailure()
        {
            MemoryRunSaveFileStore files = new MemoryRunSaveFileStore();
            files.Set(RunSaveRepository.PrimaryFileName, "{broken");
            files.Set(RunSaveRepository.BackupFileName, "not-json");
            RunSaveRepository repository =
                new RunSaveRepository(files, CreateStages());

            RunSaveLoadResult result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.Corrupted));
            Assert.That(result.CanContinue, Is.False);
            Assert.That(result.Snapshot, Is.Null);
        }

        [Test]
        public void SV02_U07_UnsupportedSchemaAndContentMismatchAreDistinguished()
        {
            Assert.That(
                RunSaveSerializer.TrySerialize(CreateSnapshot(), out string validJson),
                Is.True);
            MemoryRunSaveFileStore futureFiles = new MemoryRunSaveFileStore();
            futureFiles.Set(
                RunSaveRepository.PrimaryFileName,
                validJson.Replace(
                    "\"schemaVersion\":1",
                    "\"schemaVersion\":2"));
            MemoryRunSaveFileStore mismatchedFiles = new MemoryRunSaveFileStore();
            mismatchedFiles.Set(
                RunSaveRepository.PrimaryFileName,
                validJson.Replace(
                    RunSaveSnapshot.CurrentContentRevision,
                    "prototype-v1"));

            RunSaveLoadResult futureResult =
                new RunSaveRepository(futureFiles, CreateStages()).Load();
            RunSaveLoadResult mismatchedResult =
                new RunSaveRepository(mismatchedFiles, CreateStages()).Load();

            Assert.That(
                futureResult.Status,
                Is.EqualTo(RunSaveLoadStatus.UnsupportedVersion));
            Assert.That(
                mismatchedResult.Status,
                Is.EqualTo(RunSaveLoadStatus.IncompatibleContent));
        }

        [Test]
        public void SV02_U08_EmptyLegacySaveIsTreatedAsNoSave()
        {
            MemoryRunSaveFileStore files = new MemoryRunSaveFileStore();
            files.Set(RunSaveRepository.LegacyFileName, "  ");
            RunSaveRepository repository =
                new RunSaveRepository(files, CreateStages());

            RunSaveLoadResult result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.NoSave));
            Assert.That(result.CanContinue, Is.False);
            Assert.That(result.DiagnosticCode, Is.EqualTo("empty-legacy-save"));
        }

        private static RunSaveSnapshot CreateSnapshot()
        {
            PlayerRunSaveSnapshot player = new PlayerRunSaveSnapshot(
                12,
                8,
                5,
                7,
                4,
                DemonContractCatalog.SatanKey,
                new[]
                {
                    new RunSaveCardSnapshot(
                        2,
                        "standard-ace-1",
                        CardSuit.Spade),
                    new RunSaveCardSnapshot(
                        7,
                        "military-knife-10",
                        CardSuit.Clover)
                },
                new[]
                {
                    new RunSaveDemonSnapshot(
                        4,
                        DemonContractCatalog.SatanKey)
                });
            return new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                RunSaveSnapshot.CurrentContentRevision,
                9,
                "run-001",
                SavedAtUtc,
                RunCheckpointKind.CombatSettlementCompleted,
                RunSaveStatus.InProgress,
                20260726,
                0,
                "normal-1",
                RunNextContentKind.Shop,
                player,
                new RunRandomSaveSnapshot(1, 2, 3, 4, "offer-7"),
                new[] { "shop-1" },
                new[] { "event-1" });
        }

        private static void AssertSnapshot(RunSaveSnapshot snapshot)
        {
            Assert.That(snapshot.SchemaVersion, Is.EqualTo(1));
            Assert.That(snapshot.ContentRevision, Is.EqualTo("prototype-v2"));
            Assert.That(snapshot.SaveSequence, Is.EqualTo(9));
            Assert.That(snapshot.RunId, Is.EqualTo("run-001"));
            Assert.That(snapshot.SavedAtUtc, Is.EqualTo(SavedAtUtc));
            Assert.That(
                snapshot.CheckpointKind,
                Is.EqualTo(RunCheckpointKind.CombatSettlementCompleted));
            Assert.That(snapshot.Status, Is.EqualTo(RunSaveStatus.InProgress));
            Assert.That(snapshot.RootSeed, Is.EqualTo(20260726));
            Assert.That(snapshot.CurrentStageIndex, Is.Zero);
            Assert.That(snapshot.CurrentStageId, Is.EqualTo("normal-1"));
            Assert.That(snapshot.NextContentKind, Is.EqualTo(RunNextContentKind.Shop));
            Assert.That(snapshot.Player.MaximumSoul, Is.EqualTo(12));
            Assert.That(snapshot.Player.CurrentSoul, Is.EqualTo(8));
            Assert.That(snapshot.Player.CurrentGold, Is.EqualTo(5));
            Assert.That(snapshot.Player.LastIssuedCardId, Is.EqualTo(7));
            Assert.That(snapshot.Player.LastIssuedDemonCardId, Is.EqualTo(4));
            Assert.That(
                snapshot.Player.StartingDemonDefinitionKey,
                Is.EqualTo(DemonContractCatalog.SatanKey));
            Assert.That(snapshot.Player.Cards.Count, Is.EqualTo(2));
            Assert.That(snapshot.Player.Cards[0].Id, Is.EqualTo(2));
            Assert.That(snapshot.Player.Cards[0].Suit, Is.EqualTo(CardSuit.Spade));
            Assert.That(snapshot.Player.Cards[1].Id, Is.EqualTo(7));
            Assert.That(snapshot.Player.Cards[1].Suit, Is.EqualTo(CardSuit.Clover));
            Assert.That(snapshot.Player.DemonCards.Count, Is.EqualTo(1));
            Assert.That(snapshot.Player.DemonCards[0].Id, Is.EqualTo(4));
            Assert.That(snapshot.Random.OpponentOfferOrdinal, Is.EqualTo(1));
            Assert.That(snapshot.Random.BattleRewardOrdinal, Is.EqualTo(2));
            Assert.That(snapshot.Random.ShopOfferOrdinal, Is.EqualTo(3));
            Assert.That(snapshot.Random.EventOrdinal, Is.EqualTo(4));
            Assert.That(snapshot.Random.ReservedNextOfferId, Is.EqualTo("offer-7"));
            Assert.That(snapshot.CompletedShopIds, Is.EqualTo(new[] { "shop-1" }));
            Assert.That(snapshot.CompletedEventIds, Is.EqualTo(new[] { "event-1" }));
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

        private sealed class MemoryRunSaveFileStore : IRunSaveFileStore
        {
            private readonly Dictionary<string, string> _files =
                new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly HashSet<string> _failOnce =
                new HashSet<string>(StringComparer.Ordinal);

            internal MemoryRunSaveFileStore()
            {
                Operations = new List<string>();
            }

            internal List<string> Operations { get; }

            public bool Exists(string fileName)
            {
                Operations.Add("Exists:" + fileName);
                return _files.ContainsKey(fileName);
            }

            public bool TryRead(string fileName, out string contents)
            {
                string operation = "Read:" + fileName;
                Operations.Add(operation);
                if (ConsumeFailure(operation))
                {
                    contents = null;
                    return false;
                }

                return _files.TryGetValue(fileName, out contents);
            }

            public bool TryWrite(string fileName, string contents)
            {
                string operation = "Write:" + fileName;
                Operations.Add(operation);
                if (ConsumeFailure(operation))
                {
                    return false;
                }

                _files[fileName] = contents;
                return true;
            }

            public bool TryDelete(string fileName)
            {
                string operation = "Delete:" + fileName;
                Operations.Add(operation);
                if (ConsumeFailure(operation))
                {
                    return false;
                }

                _files.Remove(fileName);
                return true;
            }

            public bool TryMove(
                string sourceFileName,
                string destinationFileName,
                bool overwrite)
            {
                string operation = "Move:" + sourceFileName + "->" +
                    destinationFileName + ":" + overwrite;
                Operations.Add(operation);
                if (ConsumeFailure(operation) ||
                    !_files.TryGetValue(sourceFileName, out string contents) ||
                    (!overwrite && _files.ContainsKey(destinationFileName)))
                {
                    return false;
                }

                _files.Remove(sourceFileName);
                _files[destinationFileName] = contents;
                return true;
            }

            internal void Set(string fileName, string contents)
            {
                _files[fileName] = contents;
            }

            internal string Get(string fileName)
            {
                return _files[fileName];
            }

            internal bool ExistsWithoutLog(string fileName)
            {
                return _files.ContainsKey(fileName);
            }

            internal void FailOnce(string operation)
            {
                _failOnce.Add(operation);
            }

            private bool ConsumeFailure(string operation)
            {
                return _failOnce.Remove(operation);
            }
        }
    }
}
