using System;

namespace DiaBlackJack.GameScene
{
    public sealed class GameSceneDemonCardViewModel
    {
        public GameSceneDemonCardViewModel(
            int cardId,
            int faceSpriteIndex,
            bool isFaceUp,
            bool canUse,
            string displayName,
            string summary = "",
            string costSummary = "")
        {
            if (cardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardId), "Card id cannot be negative.");
            }

            if (faceSpriteIndex < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(faceSpriteIndex),
                    "Demon card sprite index must be one or greater.");
            }

            CardId = cardId;
            FaceSpriteIndex = faceSpriteIndex;
            IsFaceUp = isFaceUp;
            CanUse = canUse;
            DisplayName = displayName ?? string.Empty;
            Summary = summary ?? string.Empty;
            CostSummary = costSummary ?? string.Empty;
        }

        public int CardId { get; }

        public bool CanUse { get; }

        public string CostSummary { get; }

        public string DisplayName { get; }

        public int FaceSpriteIndex { get; }

        public bool IsFaceUp { get; }

        public string Summary { get; }
    }
}
