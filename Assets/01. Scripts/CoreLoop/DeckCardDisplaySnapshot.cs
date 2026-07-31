namespace DiaBlackJack.CoreLoop
{
    /// <summary>
    /// Immutable public-facing copy of one card currently available in a deck pile.
    /// It deliberately contains no mutable <see cref="BlackjackCard"/> reference and no pile order.
    /// </summary>
    public sealed class DeckCardDisplaySnapshot
    {
        internal DeckCardDisplaySnapshot(BlackjackCard card)
        {
            Id = card.Id;
            DefinitionKey = card.DefinitionKey;
            DisplayName = card.Definition.DisplayName;
            AbilityDescription = card.Definition.Description;
            Rank = card.Rank;
            Suit = card.Suit;
            Effect = card.Definition.Effect;
        }

        public string AbilityDescription { get; }

        public int Id { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public int Rank { get; }

        public CardSuit Suit { get; }

        public CardEffectKind Effect { get; }
    }
}
