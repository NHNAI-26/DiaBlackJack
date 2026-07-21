namespace DiaBlackJack.CoreLoop
{
    public enum CardUseUnavailableReason
    {
        None,
        EffectInProgress,
        NotPlayerTurn,
        CardNotInHand,
        CardIsNotManual,
        CardIsUnavailable,
        EffectNotImplemented,
        EffectRequirementsNotMet
    }

    public readonly struct CardUseAvailability
    {
        internal CardUseAvailability(
            int cardId,
            bool canUse,
            CardUseUnavailableReason reason)
        {
            CardId = cardId;
            CanUse = canUse;
            Reason = reason;
        }

        public int CardId { get; }

        public bool CanUse { get; }

        public CardUseUnavailableReason Reason { get; }
    }
}
