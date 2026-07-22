using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyDecision
    {
        public EnemyDecision(EnemyActionType actionType, string reasonCode)
            : this(
                actionType,
                cardId: null,
                cardEffectOptionId: null,
                reasonCode,
                Array.Empty<EnemyActionScore>(),
                demonContractOptionId: null)
        {
        }

        public EnemyDecision(
            EnemyActionType actionType,
            int? cardId,
            int? cardEffectOptionId,
            string reasonCode,
            IEnumerable<EnemyActionScore> candidateScores,
            int? demonContractOptionId = null)
        {
            if (!Enum.IsDefined(typeof(EnemyActionType), actionType))
            {
                throw new ArgumentOutOfRangeException(nameof(actionType));
            }

            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                throw new ArgumentException("Enemy decision reason cannot be empty.", nameof(reasonCode));
            }

            if (candidateScores == null)
            {
                throw new ArgumentNullException(nameof(candidateScores));
            }

            if (actionType == EnemyActionType.UseCard)
            {
                if (!cardId.HasValue || cardId.Value < 0)
                {
                    throw new ArgumentException(
                        "Card use decision requires a non-negative card id.",
                        nameof(cardId));
                }

                if (cardEffectOptionId < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(cardEffectOptionId));
                }

                if (demonContractOptionId.HasValue)
                {
                    throw new ArgumentException(
                        "Card decisions cannot contain a demon contract option.");
                }
            }
            else if (actionType == EnemyActionType.DemonContract)
            {
                if (cardId.HasValue || cardEffectOptionId.HasValue)
                {
                    throw new ArgumentException(
                        "Demon contract decisions cannot contain card selection values.");
                }

                if (demonContractOptionId < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(demonContractOptionId));
                }
            }
            else if (cardId.HasValue ||
                cardEffectOptionId.HasValue ||
                demonContractOptionId.HasValue)
            {
                throw new ArgumentException(
                    "Only card use decisions can contain card selection values.");
            }

            var copiedScores = new List<EnemyActionScore>();
            foreach (EnemyActionScore score in candidateScores)
            {
                if (score == null)
                {
                    throw new ArgumentException(
                        "Enemy decision scores cannot contain null.",
                        nameof(candidateScores));
                }

                copiedScores.Add(score);
            }

            ActionType = actionType;
            CardId = cardId;
            CardEffectOptionId = cardEffectOptionId;
            DemonContractOptionId = demonContractOptionId;
            ReasonCode = reasonCode;
            CandidateScores = new ReadOnlyCollection<EnemyActionScore>(copiedScores);
        }

        public EnemyActionType ActionType { get; }

        public int? CardEffectOptionId { get; }

        public int? CardId { get; }

        public int? DemonContractOptionId { get; }

        public IReadOnlyList<EnemyActionScore> CandidateScores { get; }

        public string ReasonCode { get; }

        internal static EnemyDecision FromCandidate(
            EnemyActionCandidate candidate,
            string reasonCode,
            IEnumerable<EnemyActionScore> scores = null)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            return new EnemyDecision(
                candidate.ActionType,
                candidate.CardId,
                candidate.CardEffectOptionId,
                reasonCode,
                scores ?? Array.Empty<EnemyActionScore>(),
                candidate.DemonContractOptionId);
        }
    }
}
