using System;
using System.Collections.Generic;
using System.Globalization;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public enum RunSaveValidationError
    {
        None,
        SnapshotMissing,
        UnsupportedSchemaVersion,
        IncompatibleContentRevision,
        InvalidSaveMetadata,
        InvalidCheckpoint,
        InvalidRunStatus,
        InvalidStage,
        InvalidPlayerSoul,
        InvalidGold,
        EmptyDeck,
        InvalidCard,
        DuplicateCardId,
        InvalidDemonCard,
        DuplicateDemonCardId,
        InvalidLastIssuedCardId,
        InvalidLastIssuedDemonCardId,
        StartingDemonMissing,
        InvalidRandomState,
        InvalidCompletedContentId,
        DuplicateCompletedContentId,
        UnstableState
    }

    public sealed class RunSaveValidationResult
    {
        private RunSaveValidationResult(bool isValid, RunSaveValidationError error)
        {
            IsValid = isValid;
            Error = error;
        }

        public bool IsValid { get; }

        public RunSaveValidationError Error { get; }

        internal static RunSaveValidationResult Valid()
        {
            return new RunSaveValidationResult(true, RunSaveValidationError.None);
        }

        internal static RunSaveValidationResult Invalid(RunSaveValidationError error)
        {
            return new RunSaveValidationResult(false, error);
        }
    }

    public static class RunSaveValidator
    {
        public static RunSaveValidationResult Validate(
            RunSaveSnapshot snapshot,
            IReadOnlyList<StageDefinition> stages)
        {
            RunSaveValidationResult result = ValidateMetadata(snapshot);
            if (!result.IsValid)
            {
                return result;
            }

            result = ValidateStage(snapshot, stages);
            if (!result.IsValid)
            {
                return result;
            }

            result = ValidatePlayer(snapshot.Player, snapshot.Status);
            if (!result.IsValid)
            {
                return result;
            }

            result = ValidateRandom(snapshot.Random);
            if (!result.IsValid)
            {
                return result;
            }

            result = ValidateCompletedIds(snapshot.CompletedShopIds);
            if (!result.IsValid)
            {
                return result;
            }

            result = ValidateCompletedIds(snapshot.CompletedEventIds);
            if (!result.IsValid)
            {
                return result;
            }

            return ValidateCheckpoint(snapshot);
        }

        private static RunSaveValidationResult ValidateMetadata(RunSaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return Invalid(RunSaveValidationError.SnapshotMissing);
            }

            if (snapshot.SchemaVersion != RunSaveSnapshot.CurrentSchemaVersion)
            {
                return Invalid(RunSaveValidationError.UnsupportedSchemaVersion);
            }

            if (!string.Equals(
                    snapshot.ContentRevision,
                    RunSaveSnapshot.CurrentContentRevision,
                    StringComparison.Ordinal))
            {
                return Invalid(RunSaveValidationError.IncompatibleContentRevision);
            }

            DateTimeOffset savedAt;
            if (snapshot.SaveSequence < 0 ||
                string.IsNullOrWhiteSpace(snapshot.RunId) ||
                string.IsNullOrWhiteSpace(snapshot.SavedAtUtc) ||
                !DateTimeOffset.TryParseExact(
                    snapshot.SavedAtUtc,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out savedAt) ||
                string.IsNullOrWhiteSpace(snapshot.CurrentStageId) ||
                string.IsNullOrWhiteSpace(snapshot.NextContentKind) ||
                !Enum.IsDefined(typeof(RunCheckpointKind), snapshot.CheckpointKind) ||
                !Enum.IsDefined(typeof(RunSaveStatus), snapshot.Status))
            {
                return Invalid(RunSaveValidationError.InvalidSaveMetadata);
            }

            return RunSaveValidationResult.Valid();
        }

        private static RunSaveValidationResult ValidateStage(
            RunSaveSnapshot snapshot,
            IReadOnlyList<StageDefinition> stages)
        {
            if (stages == null ||
                stages.Count == 0 ||
                snapshot.CurrentStageIndex < 0 ||
                snapshot.CurrentStageIndex >= stages.Count)
            {
                return Invalid(RunSaveValidationError.InvalidStage);
            }

            StageDefinition stage = stages[snapshot.CurrentStageIndex];
            if (stage == null ||
                !string.Equals(stage.Id, snapshot.CurrentStageId, StringComparison.Ordinal))
            {
                return Invalid(RunSaveValidationError.InvalidStage);
            }

            if (snapshot.Status == RunSaveStatus.Victory &&
                snapshot.CurrentStageIndex != stages.Count - 1)
            {
                return Invalid(RunSaveValidationError.InvalidStage);
            }

            return RunSaveValidationResult.Valid();
        }

        private static RunSaveValidationResult ValidatePlayer(
            PlayerRunSaveSnapshot player,
            RunSaveStatus status)
        {
            if (player == null ||
                player.MaximumSoul <= 0 ||
                player.CurrentSoul < 0 ||
                player.CurrentSoul > player.MaximumSoul)
            {
                return Invalid(RunSaveValidationError.InvalidPlayerSoul);
            }

            if ((status == RunSaveStatus.InProgress && player.CurrentSoul == 0) ||
                (status == RunSaveStatus.Victory && player.CurrentSoul == 0) ||
                (status == RunSaveStatus.Defeat && player.CurrentSoul != 0))
            {
                return Invalid(RunSaveValidationError.InvalidPlayerSoul);
            }

            if (player.CurrentGold < 0)
            {
                return Invalid(RunSaveValidationError.InvalidGold);
            }

            RunSaveValidationResult result = ValidateCards(player);
            if (!result.IsValid)
            {
                return result;
            }

            return ValidateDemonCards(player);
        }

        private static RunSaveValidationResult ValidateCards(PlayerRunSaveSnapshot player)
        {
            if (player.Cards == null || player.Cards.Count == 0)
            {
                return Invalid(RunSaveValidationError.EmptyDeck);
            }

            HashSet<int> knownIds = new HashSet<int>();
            int maximumId = -1;
            for (int i = 0; i < player.Cards.Count; i++)
            {
                RunSaveCardSnapshot card = player.Cards[i];
                if (card == null ||
                    card.Id < 0 ||
                    string.IsNullOrWhiteSpace(card.DefinitionKey) ||
                    !ContainsCardDefinition(card.DefinitionKey) ||
                    !Enum.IsDefined(typeof(CardSuit), card.Suit))
                {
                    return Invalid(RunSaveValidationError.InvalidCard);
                }

                if (!knownIds.Add(card.Id))
                {
                    return Invalid(RunSaveValidationError.DuplicateCardId);
                }

                maximumId = Math.Max(maximumId, card.Id);
            }

            return player.LastIssuedCardId < maximumId
                ? Invalid(RunSaveValidationError.InvalidLastIssuedCardId)
                : RunSaveValidationResult.Valid();
        }

        private static RunSaveValidationResult ValidateDemonCards(
            PlayerRunSaveSnapshot player)
        {
            if (player.DemonCards == null)
            {
                return Invalid(RunSaveValidationError.InvalidDemonCard);
            }

            HashSet<int> knownIds = new HashSet<int>();
            HashSet<string> ownedDefinitionKeys =
                new HashSet<string>(StringComparer.Ordinal);
            int maximumId = -1;
            for (int i = 0; i < player.DemonCards.Count; i++)
            {
                RunSaveDemonSnapshot card = player.DemonCards[i];
                if (card == null ||
                    card.Id < 0 ||
                    string.IsNullOrWhiteSpace(card.DefinitionKey) ||
                    !ContainsDemonDefinition(card.DefinitionKey))
                {
                    return Invalid(RunSaveValidationError.InvalidDemonCard);
                }

                if (!knownIds.Add(card.Id))
                {
                    return Invalid(RunSaveValidationError.DuplicateDemonCardId);
                }

                ownedDefinitionKeys.Add(card.DefinitionKey);
                maximumId = Math.Max(maximumId, card.Id);
            }

            if (player.LastIssuedDemonCardId < maximumId ||
                player.LastIssuedDemonCardId < -1)
            {
                return Invalid(RunSaveValidationError.InvalidLastIssuedDemonCardId);
            }

            if (!string.IsNullOrEmpty(player.StartingDemonDefinitionKey) &&
                (!ContainsDemonDefinition(player.StartingDemonDefinitionKey) ||
                 !ownedDefinitionKeys.Contains(player.StartingDemonDefinitionKey)))
            {
                return Invalid(RunSaveValidationError.StartingDemonMissing);
            }

            return RunSaveValidationResult.Valid();
        }

        private static RunSaveValidationResult ValidateRandom(
            RunRandomSaveSnapshot random)
        {
            if (random == null ||
                random.OpponentOfferOrdinal < 0 ||
                random.BattleRewardOrdinal < 0 ||
                random.ShopOfferOrdinal < 0 ||
                random.EventOrdinal < 0 ||
                random.UtilityPriceLevel < 0 ||
                random.UtilityPriceLevel > random.ShopOfferOrdinal ||
                (random.ReservedNextOfferId != null &&
                 string.IsNullOrWhiteSpace(random.ReservedNextOfferId)))
            {
                return Invalid(RunSaveValidationError.InvalidRandomState);
            }

            return RunSaveValidationResult.Valid();
        }

        private static RunSaveValidationResult ValidateCompletedIds(
            IReadOnlyList<string> completedIds)
        {
            if (completedIds == null)
            {
                return Invalid(RunSaveValidationError.InvalidCompletedContentId);
            }

            HashSet<string> knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < completedIds.Count; i++)
            {
                string id = completedIds[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    return Invalid(RunSaveValidationError.InvalidCompletedContentId);
                }

                if (!knownIds.Add(id))
                {
                    return Invalid(RunSaveValidationError.DuplicateCompletedContentId);
                }
            }

            return RunSaveValidationResult.Valid();
        }

        private static RunSaveValidationResult ValidateCheckpoint(
            RunSaveSnapshot snapshot)
        {
            bool valid;
            switch (snapshot.CheckpointKind)
            {
                case RunCheckpointKind.StartingDemonSelected:
                    valid = snapshot.Status == RunSaveStatus.InProgress &&
                        snapshot.CurrentStageIndex == 0 &&
                        !string.IsNullOrEmpty(
                            snapshot.Player.StartingDemonDefinitionKey) &&
                        snapshot.Random.OpponentOfferOrdinal == 0 &&
                        snapshot.Random.BattleRewardOrdinal == 0 &&
                        snapshot.Random.ShopOfferOrdinal == 0 &&
                        snapshot.Random.UtilityPriceLevel == 0 &&
                        snapshot.Random.EventOrdinal == 0 &&
                        snapshot.CompletedShopIds.Count == 0 &&
                        snapshot.CompletedEventIds.Count == 0 &&
                        IsNext(
                            snapshot.NextContentKind,
                            RunNextContentKind.OpponentSelection,
                            RunNextContentKind.Battle);
                    break;
                case RunCheckpointKind.CombatSettlementCompleted:
                    valid = snapshot.Status == RunSaveStatus.InProgress &&
                        IsNext(
                            snapshot.NextContentKind,
                            RunNextContentKind.Shop,
                            RunNextContentKind.Event);
                    break;
                case RunCheckpointKind.ShopExited:
                    valid = snapshot.Status == RunSaveStatus.InProgress &&
                        snapshot.Random.ShopOfferOrdinal ==
                            snapshot.CurrentStageIndex + 1 &&
                        IsNext(
                            snapshot.NextContentKind,
                            RunNextContentKind.OpponentSelection,
                            RunNextContentKind.Battle,
                            RunNextContentKind.Boss);
                    break;
                case RunCheckpointKind.EventResolved:
                    valid = snapshot.Status == RunSaveStatus.InProgress &&
                        IsNext(
                            snapshot.NextContentKind,
                            RunNextContentKind.OpponentSelection,
                            RunNextContentKind.Battle,
                            RunNextContentKind.Boss);
                    break;
                case RunCheckpointKind.RunEnded:
                    valid = snapshot.Status != RunSaveStatus.InProgress &&
                        IsNext(snapshot.NextContentKind, RunNextContentKind.Result);
                    break;
                default:
                    valid = false;
                    break;
            }

            return valid
                ? RunSaveValidationResult.Valid()
                : Invalid(RunSaveValidationError.InvalidCheckpoint);
        }

        private static bool IsNext(string actual, params string[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                if (string.Equals(actual, expected[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCardDefinition(string definitionKey)
        {
            IReadOnlyList<CardDefinition> definitions = CardDefinitionCatalog.All;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(
                        definitions[i].Key,
                        definitionKey,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsDemonDefinition(string definitionKey)
        {
            IReadOnlyList<DemonContractDefinition> definitions =
                DemonContractCatalog.Default.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(
                        definitions[i].Key,
                        definitionKey,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static RunSaveValidationResult Invalid(RunSaveValidationError error)
        {
            return RunSaveValidationResult.Invalid(error);
        }
    }
}
