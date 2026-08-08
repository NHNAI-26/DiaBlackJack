using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class RunSaveCardSnapshot
    {
        internal RunSaveCardSnapshot(int id, string definitionKey, CardSuit suit)
        {
            Id = id;
            DefinitionKey = definitionKey;
            Suit = suit;
        }

        public int Id { get; }

        public string DefinitionKey { get; }

        public CardSuit Suit { get; }
    }

    public sealed class RunSaveDemonSnapshot
    {
        internal RunSaveDemonSnapshot(int id, string definitionKey)
        {
            Id = id;
            DefinitionKey = definitionKey;
        }

        public int Id { get; }

        public string DefinitionKey { get; }
    }

    public sealed class PlayerRunSaveSnapshot
    {
        internal PlayerRunSaveSnapshot(
            int maximumSoul,
            int currentSoul,
            int currentGold,
            int lastIssuedCardId,
            int lastIssuedDemonCardId,
            bool startingDemonGrantCompleted,
            IEnumerable<RunSaveCardSnapshot> cards,
            IEnumerable<RunSaveDemonSnapshot> demonCards,
            bool hasMadeDemonContract = false)
        {
            MaximumSoul = maximumSoul;
            CurrentSoul = currentSoul;
            CurrentGold = currentGold;
            LastIssuedCardId = lastIssuedCardId;
            LastIssuedDemonCardId = lastIssuedDemonCardId;
            StartingDemonGrantCompleted = startingDemonGrantCompleted;
            HasMadeDemonContract = hasMadeDemonContract;
            Cards = Copy(cards);
            DemonCards = Copy(demonCards);
        }

        public int MaximumSoul { get; }

        public int CurrentSoul { get; }

        public int CurrentGold { get; }

        public int LastIssuedCardId { get; }

        public int LastIssuedDemonCardId { get; }

        public bool StartingDemonGrantCompleted { get; }

        public bool HasMadeDemonContract { get; }

        public IReadOnlyList<RunSaveCardSnapshot> Cards { get; }

        public IReadOnlyList<RunSaveDemonSnapshot> DemonCards { get; }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        {
            return source == null
                ? null
                : new ReadOnlyCollection<T>(new List<T>(source));
        }
    }

    public sealed class RunRandomSaveSnapshot
    {
        internal RunRandomSaveSnapshot(
            int opponentOfferOrdinal,
            int battleRewardOrdinal,
            int shopOfferOrdinal,
            int eventOrdinal,
            string reservedNextOfferId,
            int utilityPriceLevel = 0)
        {
            OpponentOfferOrdinal = opponentOfferOrdinal;
            BattleRewardOrdinal = battleRewardOrdinal;
            ShopOfferOrdinal = shopOfferOrdinal;
            EventOrdinal = eventOrdinal;
            ReservedNextOfferId = reservedNextOfferId;
            UtilityPriceLevel = utilityPriceLevel;
        }

        public int OpponentOfferOrdinal { get; }

        public int BattleRewardOrdinal { get; }

        public int ShopOfferOrdinal { get; }

        public int EventOrdinal { get; }

        public string ReservedNextOfferId { get; }

        public int UtilityPriceLevel { get; }
    }

    public sealed class RunSaveSnapshot
    {
        public const int CurrentSchemaVersion = 2;
        public const string CurrentContentRevision = "prototype-v3";

        internal RunSaveSnapshot(
            int schemaVersion,
            string contentRevision,
            long saveSequence,
            string runId,
            string savedAtUtc,
            RunCheckpointKind checkpointKind,
            RunSaveStatus status,
            int rootSeed,
            int currentStageIndex,
            string currentStageId,
            string nextContentKind,
            PlayerRunSaveSnapshot player,
            RunRandomSaveSnapshot random,
            IEnumerable<string> completedShopIds,
            IEnumerable<string> completedEventIds)
        {
            SchemaVersion = schemaVersion;
            ContentRevision = contentRevision;
            SaveSequence = saveSequence;
            RunId = runId;
            SavedAtUtc = savedAtUtc;
            CheckpointKind = checkpointKind;
            Status = status;
            RootSeed = rootSeed;
            CurrentStageIndex = currentStageIndex;
            CurrentStageId = currentStageId;
            NextContentKind = nextContentKind;
            Player = player;
            Random = random;
            CompletedShopIds = Copy(completedShopIds);
            CompletedEventIds = Copy(completedEventIds);
        }

        public int SchemaVersion { get; }

        public string ContentRevision { get; }

        public long SaveSequence { get; }

        public string RunId { get; }

        public string SavedAtUtc { get; }

        public RunCheckpointKind CheckpointKind { get; }

        public RunSaveStatus Status { get; }

        public int RootSeed { get; }

        public int CurrentStageIndex { get; }

        public string CurrentStageId { get; }

        public string NextContentKind { get; }

        public PlayerRunSaveSnapshot Player { get; }

        public RunRandomSaveSnapshot Random { get; }

        public IReadOnlyList<string> CompletedShopIds { get; }

        public IReadOnlyList<string> CompletedEventIds { get; }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        {
            return source == null
                ? null
                : new ReadOnlyCollection<T>(new List<T>(source));
        }
    }
}
