using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using UnityEngine;

namespace Border.SaveLoad
{
    internal static class RunSaveSerializer
    {
        private const string StartingDemonSelected = "starting-demon-selected";
        private const string CombatSettlementCompleted = "combat-settlement-completed";
        private const string ShopExited = "shop-exited";
        private const string EventResolved = "event-resolved";
        private const string RunEnded = "run-ended";
        private const string InProgress = "in-progress";
        private const string Victory = "victory";
        private const string Defeat = "defeat";
        private const string Spade = "spade";
        private const string Clover = "clover";

        internal static bool TrySerialize(
            RunSaveSnapshot snapshot,
            out string json)
        {
            json = null;
            if (snapshot == null)
            {
                return false;
            }

            try
            {
                RunSaveEnvelope envelope = ToEnvelope(snapshot);
                json = JsonUtility.ToJson(envelope);
                return !string.IsNullOrWhiteSpace(json);
            }
            catch (Exception)
            {
                json = null;
                return false;
            }
        }

        internal static bool TryDeserialize(
            string json,
            out RunSaveSnapshot snapshot,
            out RunSaveSerializationStatus status)
        {
            snapshot = null;
            status = RunSaveSerializationStatus.Corrupted;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            RunSaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<RunSaveEnvelope>(json);
            }
            catch (Exception)
            {
                return false;
            }

            if (envelope == null)
            {
                return false;
            }

            if (envelope.schemaVersion > RunSaveSnapshot.CurrentSchemaVersion)
            {
                status = RunSaveSerializationStatus.UnsupportedVersion;
                return false;
            }

            if (envelope.schemaVersion != RunSaveSnapshot.CurrentSchemaVersion)
            {
                return false;
            }

            if (!string.Equals(
                    envelope.contentRevision,
                    RunSaveSnapshot.CurrentContentRevision,
                    StringComparison.Ordinal))
            {
                status = RunSaveSerializationStatus.IncompatibleContent;
                return false;
            }

            if (!TryParseCheckpoint(envelope.checkpointKind, out RunCheckpointKind checkpoint) ||
                !TryParseStatus(envelope.status, out RunSaveStatus runStatus) ||
                !TryMapPlayer(envelope.player, out PlayerRunSaveSnapshot player))
            {
                return false;
            }

            RunRandomSaveSnapshot random = envelope.random == null
                ? null
                : new RunRandomSaveSnapshot(
                    envelope.random.opponentOfferOrdinal,
                    envelope.random.battleRewardOrdinal,
                    envelope.random.shopOfferOrdinal,
                    envelope.random.eventOrdinal,
                    string.IsNullOrEmpty(envelope.random.reservedNextOfferId)
                        ? null
                        : envelope.random.reservedNextOfferId);

            snapshot = new RunSaveSnapshot(
                envelope.schemaVersion,
                envelope.contentRevision,
                envelope.saveSequence,
                envelope.runId,
                envelope.savedAtUtc,
                checkpoint,
                runStatus,
                envelope.rootSeed,
                envelope.currentStageIndex,
                envelope.currentStageId,
                envelope.nextContentKind,
                player,
                random,
                envelope.completedShopIds,
                envelope.completedEventIds);
            status = RunSaveSerializationStatus.Success;
            return true;
        }

        private static RunSaveEnvelope ToEnvelope(RunSaveSnapshot snapshot)
        {
            return new RunSaveEnvelope
            {
                schemaVersion = snapshot.SchemaVersion,
                contentRevision = snapshot.ContentRevision,
                saveSequence = snapshot.SaveSequence,
                runId = snapshot.RunId,
                savedAtUtc = snapshot.SavedAtUtc,
                checkpointKind = FormatCheckpoint(snapshot.CheckpointKind),
                status = FormatStatus(snapshot.Status),
                rootSeed = snapshot.RootSeed,
                currentStageIndex = snapshot.CurrentStageIndex,
                currentStageId = snapshot.CurrentStageId,
                nextContentKind = snapshot.NextContentKind,
                player = ToEnvelope(snapshot.Player),
                random = ToEnvelope(snapshot.Random),
                completedShopIds = Copy(snapshot.CompletedShopIds),
                completedEventIds = Copy(snapshot.CompletedEventIds)
            };
        }

        private static RunSavePlayerEnvelope ToEnvelope(PlayerRunSaveSnapshot player)
        {
            if (player == null)
            {
                return null;
            }

            return new RunSavePlayerEnvelope
            {
                maximumSoul = player.MaximumSoul,
                currentSoul = player.CurrentSoul,
                currentGold = player.CurrentGold,
                lastIssuedCardId = player.LastIssuedCardId,
                lastIssuedDemonCardId = player.LastIssuedDemonCardId,
                startingDemonDefinitionKey = player.StartingDemonDefinitionKey,
                cards = ToCardEnvelopes(player.Cards),
                demonCards = ToDemonEnvelopes(player.DemonCards)
            };
        }

        private static RunSaveRandomEnvelope ToEnvelope(RunRandomSaveSnapshot random)
        {
            if (random == null)
            {
                return null;
            }

            return new RunSaveRandomEnvelope
            {
                opponentOfferOrdinal = random.OpponentOfferOrdinal,
                battleRewardOrdinal = random.BattleRewardOrdinal,
                shopOfferOrdinal = random.ShopOfferOrdinal,
                eventOrdinal = random.EventOrdinal,
                reservedNextOfferId = random.ReservedNextOfferId
            };
        }

        private static RunSaveCardEnvelope[] ToCardEnvelopes(
            IReadOnlyList<RunSaveCardSnapshot> cards)
        {
            if (cards == null)
            {
                return null;
            }

            RunSaveCardEnvelope[] envelopes = new RunSaveCardEnvelope[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                RunSaveCardSnapshot card = cards[i];
                envelopes[i] = card == null
                    ? null
                    : new RunSaveCardEnvelope
                    {
                        id = card.Id,
                        definitionKey = card.DefinitionKey,
                        suit = FormatSuit(card.Suit)
                    };
            }

            return envelopes;
        }

        private static RunSaveDemonEnvelope[] ToDemonEnvelopes(
            IReadOnlyList<RunSaveDemonSnapshot> cards)
        {
            if (cards == null)
            {
                return null;
            }

            RunSaveDemonEnvelope[] envelopes = new RunSaveDemonEnvelope[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                RunSaveDemonSnapshot card = cards[i];
                envelopes[i] = card == null
                    ? null
                    : new RunSaveDemonEnvelope
                    {
                        id = card.Id,
                        definitionKey = card.DefinitionKey
                    };
            }

            return envelopes;
        }

        private static bool TryMapPlayer(
            RunSavePlayerEnvelope envelope,
            out PlayerRunSaveSnapshot player)
        {
            player = null;
            if (envelope == null)
            {
                return true;
            }

            if (!TryMapCards(envelope.cards, out RunSaveCardSnapshot[] cards))
            {
                return false;
            }

            RunSaveDemonSnapshot[] demonCards = null;
            if (envelope.demonCards != null)
            {
                demonCards = new RunSaveDemonSnapshot[envelope.demonCards.Length];
                for (int i = 0; i < envelope.demonCards.Length; i++)
                {
                    RunSaveDemonEnvelope card = envelope.demonCards[i];
                    demonCards[i] = card == null
                        ? null
                        : new RunSaveDemonSnapshot(card.id, card.definitionKey);
                }
            }

            player = new PlayerRunSaveSnapshot(
                envelope.maximumSoul,
                envelope.currentSoul,
                envelope.currentGold,
                envelope.lastIssuedCardId,
                envelope.lastIssuedDemonCardId,
                envelope.startingDemonDefinitionKey,
                cards,
                demonCards);
            return true;
        }

        private static bool TryMapCards(
            RunSaveCardEnvelope[] envelopes,
            out RunSaveCardSnapshot[] cards)
        {
            cards = null;
            if (envelopes == null)
            {
                return true;
            }

            cards = new RunSaveCardSnapshot[envelopes.Length];
            for (int i = 0; i < envelopes.Length; i++)
            {
                RunSaveCardEnvelope envelope = envelopes[i];
                if (envelope == null)
                {
                    cards[i] = null;
                    continue;
                }

                if (!TryParseSuit(envelope.suit, out CardSuit suit))
                {
                    cards = null;
                    return false;
                }

                cards[i] = new RunSaveCardSnapshot(
                    envelope.id,
                    envelope.definitionKey,
                    suit);
            }

            return true;
        }

        private static string[] Copy(IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return null;
            }

            string[] copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static string FormatCheckpoint(RunCheckpointKind checkpoint)
        {
            switch (checkpoint)
            {
                case RunCheckpointKind.StartingDemonSelected:
                    return StartingDemonSelected;
                case RunCheckpointKind.CombatSettlementCompleted:
                    return CombatSettlementCompleted;
                case RunCheckpointKind.ShopExited:
                    return ShopExited;
                case RunCheckpointKind.EventResolved:
                    return EventResolved;
                case RunCheckpointKind.RunEnded:
                    return RunEnded;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(checkpoint),
                        "Run checkpoint is not supported.");
            }
        }

        private static bool TryParseCheckpoint(
            string value,
            out RunCheckpointKind checkpoint)
        {
            switch (value)
            {
                case StartingDemonSelected:
                    checkpoint = RunCheckpointKind.StartingDemonSelected;
                    return true;
                case CombatSettlementCompleted:
                    checkpoint = RunCheckpointKind.CombatSettlementCompleted;
                    return true;
                case ShopExited:
                    checkpoint = RunCheckpointKind.ShopExited;
                    return true;
                case EventResolved:
                    checkpoint = RunCheckpointKind.EventResolved;
                    return true;
                case RunEnded:
                    checkpoint = RunCheckpointKind.RunEnded;
                    return true;
                default:
                    checkpoint = default;
                    return false;
            }
        }

        private static string FormatStatus(RunSaveStatus status)
        {
            switch (status)
            {
                case RunSaveStatus.InProgress:
                    return InProgress;
                case RunSaveStatus.Victory:
                    return Victory;
                case RunSaveStatus.Defeat:
                    return Defeat;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(status),
                        "Run save status is not supported.");
            }
        }

        private static bool TryParseStatus(string value, out RunSaveStatus status)
        {
            switch (value)
            {
                case InProgress:
                    status = RunSaveStatus.InProgress;
                    return true;
                case Victory:
                    status = RunSaveStatus.Victory;
                    return true;
                case Defeat:
                    status = RunSaveStatus.Defeat;
                    return true;
                default:
                    status = default;
                    return false;
            }
        }

        private static string FormatSuit(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Spade:
                    return Spade;
                case CardSuit.Clover:
                    return Clover;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(suit),
                        "Card suit is not supported.");
            }
        }

        private static bool TryParseSuit(string value, out CardSuit suit)
        {
            switch (value)
            {
                case Spade:
                    suit = CardSuit.Spade;
                    return true;
                case Clover:
                    suit = CardSuit.Clover;
                    return true;
                default:
                    suit = default;
                    return false;
            }
        }
    }
}
