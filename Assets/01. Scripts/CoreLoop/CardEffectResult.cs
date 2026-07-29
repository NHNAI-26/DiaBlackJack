using System;

namespace DiaBlackJack.CoreLoop
{
    public readonly struct CardEffectResult
    {
        public CardEffectResult(
            int sourceCardId,
            CardEffectKind effectKind,
            bool succeeded,
            bool endedRound,
            int? targetCardId = null)
        {
            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (targetCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCardId));
            }

            if (!Enum.IsDefined(typeof(CardEffectKind), effectKind) ||
                effectKind == CardEffectKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            SourceCardId = sourceCardId;
            EffectKind = effectKind;
            Succeeded = succeeded;
            EndedRound = endedRound;
            TargetCardId = targetCardId;
        }

        public bool EndedRound { get; }

        public CardEffectKind EffectKind { get; }

        public int SourceCardId { get; }

        public bool Succeeded { get; }

        public int? TargetCardId { get; }
    }
}
