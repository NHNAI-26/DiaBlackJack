using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class StartingDemonSelectionTests
    {
        private const int RootSeed = 20260727;
        private const string SavedAtUtc = "2026-07-27T00:00:00.0000000+00:00";

        [Test]
        public void DCR01_I01_SameSeedCreatesTwoDistinctStartingCandidates()
        {
            StartingDemonSelectionGenerator first = CreateGenerator();
            StartingDemonSelectionGenerator second = CreateGenerator();

            StartingDemonSelectionOffer firstOffer = first.Generate();
            StartingDemonSelectionOffer secondOffer = second.Generate();

            Assert.That(firstOffer.Options.Count, Is.EqualTo(2));
            Assert.That(
                firstOffer.Options.Select(option => option.DefinitionKey).Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(
                secondOffer.Options.Select(option => option.DefinitionKey),
                Is.EqualTo(firstOffer.Options.Select(option => option.DefinitionKey)));
        }

        [Test]
        public void DCR01_I02_RunStartOpensSelectionWithoutAdvancingRun()
        {
            StageProgressionSession session = CreateSelectionSession();

            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(session.PendingStartingDemonSelection, Is.Not.Null);
            Assert.That(session.PendingStartingDemonSelection.Options.Count, Is.EqualTo(2));
            Assert.That(session.Progress.Player.DemonDeck, Is.Empty);
            Assert.That(session.TryStartRun(), Is.False);
        }

        [Test]
        public void DCR01_I03_StaleOrUnknownSelectionIsAtomic()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            StartingDemonSelectionOffer pending =
                session.PendingStartingDemonSelection;

            Assert.That(
                session.TrySelectStartingDemon(
                    pending.OfferId + 1,
                    pending.Options[0].OptionId),
                Is.False);
            Assert.That(
                session.TrySelectStartingDemon(pending.OfferId, 999),
                Is.False);

            Assert.That(session.PendingStartingDemonSelection, Is.SameAs(pending));
            Assert.That(session.Progress.Player.DemonDeck, Is.Empty);
            Assert.That(session.Progress.Player.StartingDemonDefinitionKey, Is.Null);
        }

        [Test]
        public void DCR01_I04_SelectionCreatesOneCardThenRunCanStart()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            StartingDemonSelectionOffer pending =
                session.PendingStartingDemonSelection;
            StartingDemonSelectionOption selected = pending.Options[1];

            Assert.That(
                session.TrySelectStartingDemon(pending.OfferId, selected.OptionId),
                Is.True);

            PlayerRunState player = session.Progress.Player;
            Assert.That(session.PendingStartingDemonSelection, Is.Null);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(1));
            Assert.That(player.DemonDeck[0].Id, Is.Zero);
            Assert.That(player.DemonDeck[0].DefinitionKey, Is.EqualTo(selected.DefinitionKey));
            Assert.That(player.StartingDemonDefinitionKey, Is.EqualTo(selected.DefinitionKey));
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
        }

        [Test]
        public void DCR01_I05_SelectedDemonAndNextIdSurviveRunReset()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            StartingDemonSelectionOffer pending =
                session.PendingStartingDemonSelection;
            Assert.That(
                session.TrySelectStartingDemon(
                    pending.OfferId,
                    pending.Options[0].OptionId),
                Is.True);
            PlayerRunState player = session.Progress.Player;

            RunDemonDefinition added = player.AddDemonCard(DemonContractCatalog.SatanKey);
            player.ResetForNewRun();

            Assert.That(added.Id, Is.EqualTo(1));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(1));
            Assert.That(
                player.DemonDeck[0].DefinitionKey,
                Is.EqualTo(player.StartingDemonDefinitionKey));
            Assert.That(
                player.AddDemonCard(DemonContractCatalog.MammonKey).Id,
                Is.EqualTo(1));
        }

        [Test]
        public void DCR01_I06_StartingCheckpointCapturesAndRestoresSelection()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            StartingDemonSelectionOffer pending =
                session.PendingStartingDemonSelection;
            StartingDemonSelectionOption selected = pending.Options[0];
            Assert.That(
                session.TrySelectStartingDemon(pending.OfferId, selected.OptionId),
                Is.True);

            Assert.That(
                RunSaveCapture.TryCapture(
                    session,
                    new RunSaveCaptureContext(
                        1,
                        "dcr01-start",
                        SavedAtUtc,
                        RunCheckpointKind.StartingDemonSelected,
                        RootSeed,
                        RunNextContentKind.Battle),
                    out RunSaveSnapshot snapshot,
                    out RunSaveValidationResult captureValidation),
                Is.True);
            Assert.That(captureValidation.IsValid, Is.True);
            Assert.That(
                snapshot.Player.StartingDemonDefinitionKey,
                Is.EqualTo(selected.DefinitionKey));

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
                result.Session.Progress.Player.StartingDemonDefinitionKey,
                Is.EqualTo(selected.DefinitionKey));
            Assert.That(result.Session.TryStartRun(), Is.True);
        }

        private static StageProgressionSession CreateSelectionSession()
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
                new RunProgress(CreateStages(RootSeed), player),
                startingDemonSelectionGenerator: CreateGenerator());
        }

        private static StartingDemonSelectionGenerator CreateGenerator()
        {
            return new StartingDemonSelectionGenerator(
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
