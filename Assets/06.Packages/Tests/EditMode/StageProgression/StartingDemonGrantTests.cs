using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class StartingDemonGrantTests
    {
        private const int RootSeed = 20260727;
        private const string SavedAtUtc =
            "2026-07-27T00:00:00.0000000+00:00";

        [Test]
        public void DCR01_I01_SameSeedCreatesTwoDistinctStartingDemons()
        {
            StartingDemonGrantGenerator first = CreateGenerator();
            StartingDemonGrantGenerator second = CreateGenerator();

            StartingDemonGrant firstGrant = first.Generate();
            StartingDemonGrant secondGrant = second.Generate();

            Assert.That(firstGrant.Cards.Count, Is.EqualTo(2));
            Assert.That(
                firstGrant.Cards
                    .Select(card => card.DefinitionKey)
                    .Distinct()
                    .Count(),
                Is.EqualTo(2));
            Assert.That(
                firstGrant.Cards.Select(card => card.DefinitionKey),
                Is.SubsetOf(
                    DemonContractCatalog.PlayerDefaultDemonDeckKeys));
            Assert.That(
                secondGrant.Cards.Select(card => card.DefinitionKey),
                Is.EqualTo(
                    firstGrant.Cards.Select(card => card.DefinitionKey)));
        }

        [Test]
        public void DCR01_I02_RunStartGrantsBothDemonsExactlyOnce()
        {
            StageProgressionSession session = CreateGrantSession();

            Assert.That(session.TryStartRun(), Is.True);

            StartingDemonGrant pending = session.PendingStartingDemonGrant;
            PlayerRunState player = session.Progress.Player;
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Cards.Count, Is.EqualTo(2));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(player.StartingDemonGrantCompleted, Is.True);
            Assert.That(
                player.DemonDeck.Select(card => card.DefinitionKey),
                Is.EqualTo(
                    pending.Cards.Select(card => card.DefinitionKey)));

            Assert.That(session.TryStartRun(), Is.False);
            Assert.That(player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(
                session.PendingStartingDemonGrant,
                Is.SameAs(pending));
        }

        [Test]
        public void DCR01_I03_InvalidGrantIsAtomic()
        {
            PlayerRunState player = CreateEmptyPlayer();

            Assert.That(
                player.TryGrantStartingDemons(
                    new[]
                    {
                        DemonContractCatalog.SatanKey,
                        DemonContractCatalog.SatanKey
                    }),
                Is.False);

            Assert.That(player.DemonDeck, Is.Empty);
            Assert.That(player.StartingDemonGrantCompleted, Is.False);
            Assert.That(player.LastIssuedDemonCardId, Is.EqualTo(-1));
        }

        [Test]
        public void DCR01_I04_RevealCompletionStartsRunWithoutSelection()
        {
            StageProgressionSession session = CreateGrantSession();
            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(
                session.TryCompleteStartingDemonReveal(),
                Is.True);
            Assert.That(session.PendingStartingDemonGrant, Is.Null);
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(session.TryCompleteStartingDemonReveal(), Is.False);
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
        }

        [Test]
        public void DCR01_I05_GrantedDemonsAndNextIdSurviveRunReset()
        {
            StageProgressionSession session = CreateGrantSession();
            Assert.That(session.TryStartRun(), Is.True);
            PlayerRunState player = session.Progress.Player;
            string[] grantedKeys = player.DemonDeck
                .Select(card => card.DefinitionKey)
                .ToArray();

            RunDemonDefinition added =
                player.AddDemonCard(DemonContractCatalog.AsmodeusKey);
            player.ResetForNewRun();

            Assert.That(added.Id, Is.EqualTo(2));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(
                player.DemonDeck.Select(card => card.DefinitionKey),
                Is.EqualTo(grantedKeys));
            Assert.That(
                player.AddDemonCard(DemonContractCatalog.AzazelKey).Id,
                Is.EqualTo(2));
        }

        [Test]
        public void DCR01_I06_StartingCheckpointRestoresBothGrantedDemons()
        {
            StageProgressionSession session = CreateGrantSession();
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(
                session.TryCompleteStartingDemonReveal(),
                Is.True);
            string[] grantedKeys = session.Progress.Player.DemonDeck
                .Select(card => card.DefinitionKey)
                .ToArray();

            Assert.That(
                RunSaveCapture.TryCapture(
                    session,
                    new RunSaveCaptureContext(
                        1,
                        "dcr01-start",
                        SavedAtUtc,
                        RunCheckpointKind.StartingDemonGranted,
                        RootSeed,
                        RunNextContentKind.Battle),
                    out RunSaveSnapshot snapshot,
                    out RunSaveValidationResult captureValidation),
                Is.True);
            Assert.That(captureValidation.IsValid, Is.True);
            Assert.That(
                snapshot.Player.StartingDemonGrantCompleted,
                Is.True);
            Assert.That(snapshot.Player.DemonCards.Count, Is.EqualTo(2));

            var factory = new RunRestoreFactory(CreateStages);
            Assert.That(
                factory.TryRestore(
                    snapshot,
                    out RunRestoreResult result,
                    out RunSaveValidationResult restoreValidation),
                Is.True);
            Assert.That(restoreValidation.IsValid, Is.True);
            Assert.That(
                result.Session.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(
                result.Session.Progress.Player.StartingDemonGrantCompleted,
                Is.True);
            Assert.That(
                result.Session.Progress.Player.DemonDeck
                    .Select(card => card.DefinitionKey),
                Is.EqualTo(grantedKeys));
            Assert.That(result.Session.TryStartRun(), Is.True);
        }

        private static StageProgressionSession CreateGrantSession()
        {
            return new StageProgressionSession(
                new RunProgress(
                    CreateStages(RootSeed),
                    CreateEmptyPlayer()),
                startingDemonGrantGenerator: CreateGenerator());
        }

        private static PlayerRunState CreateEmptyPlayer()
        {
            return new PlayerRunState(
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
        }

        private static StartingDemonGrantGenerator CreateGenerator()
        {
            return new StartingDemonGrantGenerator(
                DemonContractCatalog.Default,
                RootSeed);
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
    }
}
