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
            string nextContentKind,
            int completedShopCount = 0,
            int utilityPriceLevel = 0)
        {
            SaveSequence = saveSequence;
            RunId = runId;
            SavedAtUtc = savedAtUtc;
            CheckpointKind = checkpointKind;
            RootSeed = rootSeed;
            NextContentKind = nextContentKind;
            CompletedShopCount = completedShopCount;
            UtilityPriceLevel = utilityPriceLevel;
        }

        internal long SaveSequence { get; }

        internal string RunId { get; }

        internal string SavedAtUtc { get; }

        internal RunCheckpointKind CheckpointKind { get; }

        internal int RootSeed { get; }

        internal string NextContentKind { get; }

        internal int CompletedShopCount { get; }

        internal int UtilityPriceLevel { get; }
    }

    internal static class RunSaveCapture
    {
        internal static bool TryCapture(
            RunProgress progress,
            RunSaveCaptureContext context,
            out RunSaveSnapshot snapshot,
            out RunSaveValidationResult validation)
        {
            return TryCapture(
                progress,
                context,
                new RunRandomSaveSnapshot(0, 0, 0, 0, null),
                out snapshot,
                out validation);
        }

        internal static bool TryCapture(
            StageProgressionSession session,
            RunSaveCaptureContext context,
            out RunSaveSnapshot snapshot,
            out RunSaveValidationResult validation)
        {
            if (session == null)
            {
                snapshot = null;
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.InvalidSaveMetadata);
                return false;
            }

            return TryCapture(
                session.Progress,
                context,
                new RunRandomSaveSnapshot(
                    session.OpponentOfferOrdinal,
                    session.BattleRewardOrdinal,
                    context.CompletedShopCount,
                    0,
                    null,
                    context.UtilityPriceLevel),
                out snapshot,
                out validation);
        }

        private static bool TryCapture(
            RunProgress progress,
            RunSaveCaptureContext context,
            RunRandomSaveSnapshot random,
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

            if (context.CheckpointKind == RunCheckpointKind.StartingDemonGranted &&
                !progress.Player.StartingDemonGrantCompleted)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.StartingDemonGrantMissing);
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
                random,
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
                player.CurrentGold,
                player.LastIssuedCardId,
                player.LastIssuedDemonCardId,
                player.StartingDemonGrantCompleted,
                cards,
                demonCards,
                player.HasMadeDemonContract);
        }

        private static bool TryGetStableStatus(
            StageProgressionState state,
            RunCheckpointKind checkpointKind,
            out RunSaveStatus status)
        {
            if (state == StageProgressionState.NotStarted &&
                checkpointKind == RunCheckpointKind.StartingDemonGranted)
            {
                status = RunSaveStatus.InProgress;
                return true;
            }

            if (state == StageProgressionState.StageCleared &&
                (checkpointKind == RunCheckpointKind.CombatSettlementCompleted ||
                 checkpointKind == RunCheckpointKind.ShopExited))
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
