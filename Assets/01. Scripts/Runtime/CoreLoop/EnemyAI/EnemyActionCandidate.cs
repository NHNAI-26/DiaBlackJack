using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyActionCandidate
    {
        public EnemyActionCandidate(
            EnemyActionType actionType,
            int? cardId = null,
            string cardDefinitionKey = null,
            int? cardEffectOptionId = null,
            int? cardEffectOptionNumericValue = null,
            int? cardEffectOptionCardId = null,
            int? cardEffectOptionCardRank = null)
        {
            if (!Enum.IsDefined(typeof(EnemyActionType), actionType))
            {
                throw new ArgumentOutOfRangeException(nameof(actionType));
            }

            if (actionType == EnemyActionType.UseCard)
            {
                if (!cardId.HasValue || cardId.Value < 0)
                {
                    throw new ArgumentException(
                        "Card use candidate requires a non-negative card id.",
                        nameof(cardId));
                }

                if (string.IsNullOrWhiteSpace(cardDefinitionKey))
                {
                    throw new ArgumentException(
                        "Card use candidate requires a definition key.",
                        nameof(cardDefinitionKey));
                }

                if (cardEffectOptionId < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(cardEffectOptionId));
                }

                if (!cardEffectOptionId.HasValue &&
                    (cardEffectOptionNumericValue.HasValue ||
                        cardEffectOptionCardId.HasValue ||
                        cardEffectOptionCardRank.HasValue))
                {
                    throw new ArgumentException(
                        "Card effect option details require an option id.");
                }

                if (cardEffectOptionCardId < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(cardEffectOptionCardId));
                }

                if (cardEffectOptionCardRank.HasValue &&
                    (cardEffectOptionCardRank.Value < 1 ||
                        cardEffectOptionCardRank.Value > 10))
                {
                    throw new ArgumentOutOfRangeException(nameof(cardEffectOptionCardRank));
                }

                if (cardEffectOptionCardId.HasValue != cardEffectOptionCardRank.HasValue)
                {
                    throw new ArgumentException(
                        "Card effect option card id and rank must be provided together.");
                }
            }
            else if (cardId.HasValue ||
                cardDefinitionKey != null ||
                cardEffectOptionId.HasValue ||
                cardEffectOptionNumericValue.HasValue ||
                cardEffectOptionCardId.HasValue ||
                cardEffectOptionCardRank.HasValue)
            {
                throw new ArgumentException(
                    "Only card use candidates can contain card selection values.");
            }

            ActionType = actionType;
            CardId = cardId;
            CardDefinitionKey = cardDefinitionKey;
            CardEffectOptionId = cardEffectOptionId;
            CardEffectOptionNumericValue = cardEffectOptionNumericValue;
            CardEffectOptionCardId = cardEffectOptionCardId;
            CardEffectOptionCardRank = cardEffectOptionCardRank;
        }

        public EnemyActionType ActionType { get; }

        public string CardDefinitionKey { get; }

        public int? CardEffectOptionId { get; }

        public int? CardEffectOptionNumericValue { get; }

        public int? CardEffectOptionCardId { get; }

        public int? CardEffectOptionCardRank { get; }

        public int? CardId { get; }

        internal bool Matches(EnemyDecision decision)
        {
            return decision != null &&
                ActionType == decision.ActionType &&
                CardId == decision.CardId &&
                CardEffectOptionId == decision.CardEffectOptionId;
        }
    }

    public sealed class EnemyActionScore
    {
        public EnemyActionScore(EnemyActionCandidate candidate, int score, string reasonCode)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                throw new ArgumentException("Enemy action score reason cannot be empty.", nameof(reasonCode));
            }

            Score = score;
            ReasonCode = reasonCode;
        }

        public EnemyActionCandidate Candidate { get; }

        public string ReasonCode { get; }

        public int Score { get; }
    }
}
