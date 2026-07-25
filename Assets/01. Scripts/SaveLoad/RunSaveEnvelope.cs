using System;

namespace Border.SaveLoad
{
    [Serializable]
    internal sealed class RunSaveEnvelope
    {
        public int schemaVersion;
        public string contentRevision;
        public long saveSequence;
        public string runId;
        public string savedAtUtc;
        public string checkpointKind;
        public string status;
        public int rootSeed;
        public int currentStageIndex;
        public string currentStageId;
        public string nextContentKind;
        public RunSavePlayerEnvelope player;
        public RunSaveRandomEnvelope random;
        public string[] completedShopIds;
        public string[] completedEventIds;
    }

    [Serializable]
    internal sealed class RunSavePlayerEnvelope
    {
        public int maximumSoul;
        public int currentSoul;
        public int currentGold;
        public int lastIssuedCardId;
        public int lastIssuedDemonCardId;
        public string startingDemonDefinitionKey;
        public RunSaveCardEnvelope[] cards;
        public RunSaveDemonEnvelope[] demonCards;
    }

    [Serializable]
    internal sealed class RunSaveCardEnvelope
    {
        public int id;
        public string definitionKey;
        public string suit;
    }

    [Serializable]
    internal sealed class RunSaveDemonEnvelope
    {
        public int id;
        public string definitionKey;
    }

    [Serializable]
    internal sealed class RunSaveRandomEnvelope
    {
        public int opponentOfferOrdinal;
        public int battleRewardOrdinal;
        public int shopOfferOrdinal;
        public int eventOrdinal;
        public string reservedNextOfferId;
    }
}
