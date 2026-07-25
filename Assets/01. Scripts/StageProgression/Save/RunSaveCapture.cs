using System;
using System.Collections.Generic;

namespace DiaBlackJack.StageProgression
{
    internal sealed class RunSaveCaptureContext
    {
        internal RunSaveCaptureContext(
            long saveSequence,
            string runId,
            string savedAtUtc,
            RunCheckpointKind checkpointKind,
            int rootSeed,
            string nextContentKind)
        {
            SaveSequence = saveSequence;
            RunId = runId;
            SavedAtUtc = savedAtUtc;
            CheckpointKind = checkpointKind;
            RootSeed = rootSeed;
            NextContentKind = nextContentKind;
        }

        internal long SaveSequence { get; }

        internal string RunId { get; }

        internal string SavedAtUtc { get; }

        internal RunCheckpointKind CheckpointKind { get; }

        internal int RootSeed { get; }

        internal string NextContentKind { get; }
    }

    internal static class RunSaveCapture
    {
        internal static bool TryCapture(
            RunProgress progress,
            RunSaveCaptureContext context,
            out RunSaveSnapshot snapshot,
            out RunSaveValidationResult validation)
        {
            snapshot = null;
            if (progress == null || context == null)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.InvalidSaveMetadata);
                return false;
            }

            RunSaveStatus status;
            if (!TryGetStableStatus(
                    progress.State,
                    context.CheckpointKind,
                    out status))
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.UnstableState);
                return false;
            }

            PlayerRunSaveSnapshot player = CapturePlayer(progress.Player);
            snapshot = new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                RunSaveSnapshot.CurrentContentRevision,
                context.SaveSequence,
                context.RunId,
                context.SavedAtUtc,
                context.CheckpointKind,
                status,
                context.RootSeed,
                progress.CurrentStageIndex,
                progress.CurrentStage.Id,
                context.NextContentKind,
                player,
                new RunRandomSaveSnapshot(0, 0, 0, 0, null),
                Array.Empty<string>(),
                Array.Empty<string>());

            validation = RunSaveValidator.Validate(snapshot, progress.Stages);
            if (!validation.IsValid)
            {
                snapshot = null;
                return false;
            }

            return true;
        }

        private static PlayerRunSaveSnapshot CapturePlayer(PlayerRunState player)
        {
            List<RunSaveCardSnapshot> cards =
                new List<RunSaveCardSnapshot>(player.Deck.Count);
            for (int i = 0; i < player.Deck.Count; i++)
            {
                RunCardDefinition card = player.Deck[i];
                cards.Add(new RunSaveCardSnapshot(
                    card.Id,
                    card.DefinitionKey,
                    card.Suit));
            }

            List<RunSaveDemonSnapshot> demonCards =
                new List<RunSaveDemonSnapshot>(player.DemonDeck.Count);
            for (int i = 0; i < player.DemonDeck.Count; i++)
            {
                RunDemonDefinition card = player.DemonDeck[i];
                demonCards.Add(new RunSaveDemonSnapshot(
                    card.Id,
                    card.DefinitionKey));
            }

            return new PlayerRunSaveSnapshot(
                player.MaximumSoul,
                player.CurrentSoul,
                0,
                player.LastIssuedCardId,
                player.LastIssuedDemonCardId,
                null,
                cards,
                demonCards);
        }

        private static bool TryGetStableStatus(
            StageProgressionState state,
            RunCheckpointKind checkpointKind,
            out RunSaveStatus status)
        {
            if (state == StageProgressionState.StageCleared &&
                checkpointKind == RunCheckpointKind.CombatSettlementCompleted)
            {
                status = RunSaveStatus.InProgress;
                return true;
            }

            if (checkpointKind == RunCheckpointKind.RunEnded)
            {
                if (state == StageProgressionState.RunVictory)
                {
                    status = RunSaveStatus.Victory;
                    return true;
                }

                if (state == StageProgressionState.RunDefeat)
                {
                    status = RunSaveStatus.Defeat;
                    return true;
                }
            }

            status = RunSaveStatus.InProgress;
            return false;
        }
    }
}
