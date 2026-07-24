using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BlackjackCard
    {
        public BlackjackCard(int id, int rank, bool isFaceUp = false)
        {
            ValidateId(id);
            Id = id;
            Definition = CardDefinitionCatalog.GetDefaultForRank(rank);
            IsFaceUp = isFaceUp;
        }

        public BlackjackCard(int id, CardDefinition definition, bool isFaceUp = false)
        {
            ValidateId(id);
            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            IsFaceUp = isFaceUp;
        }

        public bool CanUse => UseState == CardUseState.Available;

        public CardDefinition Definition { get; private set; }

        public string DefinitionKey => Definition.Key;

        public int Id { get; }

        public int Rank => Definition.Rank;

        public CardUseState UseState { get; private set; }

        public bool IsFaceUp { get; private set; }

        public void Reveal()
        {
            IsFaceUp = true;
        }

        public void Conceal()
        {
            IsFaceUp = false;
        }

        internal void PrepareForHand()
        {
            UseState = Definition.Activation == CardActivationKind.Manual
                ? CardUseState.Available
                : CardUseState.Unavailable;
        }

        internal bool TryBeginUse()
        {
            if (UseState != CardUseState.Available)
            {
                return false;
            }

            UseState = CardUseState.Resolving;
            return true;
        }

        internal bool TryCompleteUse()
        {
            if (UseState != CardUseState.Resolving)
            {
                return false;
            }

            UseState = CardUseState.Used;
            return true;
        }

        internal bool TryReactivate()
        {
            if (Definition.Activation != CardActivationKind.Manual ||
                UseState != CardUseState.Used)
            {
                return false;
            }

            UseState = CardUseState.Available;
            return true;
        }

        internal void TransformTo(CardDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        private static void ValidateId(int id)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Card id cannot be negative.");
            }
        }
    }
}
