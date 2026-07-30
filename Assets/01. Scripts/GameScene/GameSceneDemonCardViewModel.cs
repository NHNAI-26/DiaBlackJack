using System;

namespace DiaBlackJack.GameScene
{
    public sealed class GameSceneDemonCardViewModel
    {
        public GameSceneDemonCardViewModel(
            int cardId,
            string definitionKey,
            bool isFaceUp,
            bool canUse,
            string displayName,
            string summary = "",
            string costSummary = "",
            bool showHoverBadgeWhenUnavailable = false)
        {
            if (cardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardId), "Card id cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                throw new ArgumentException(
                    "Demon card definition key cannot be empty.",
                    nameof(definitionKey));
            }

            CardId = cardId;
            DefinitionKey = definitionKey;
            IsFaceUp = isFaceUp;
            CanUse = canUse;
            DisplayName = displayName ?? string.Empty;
            Summary = summary ?? string.Empty;
            CostSummary = costSummary ?? string.Empty;
            ShowHoverBadgeWhenUnavailable = showHoverBadgeWhenUnavailable;
        }

        public int CardId { get; }

        public bool CanUse { get; }

        public string CostSummary { get; }

        public string DisplayName { get; }

        public string DefinitionKey { get; }

        public bool IsFaceUp { get; }

        public bool ShowHoverBadgeWhenUnavailable { get; }

        public string Summary { get; }
    }
}
