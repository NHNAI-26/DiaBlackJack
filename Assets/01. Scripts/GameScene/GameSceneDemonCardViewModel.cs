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
            bool showHoverBadgeWhenUnavailable = false,
            bool isUpsideDown = false,
            int? satanDoomCount = null)
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

            if (satanDoomCount.HasValue && satanDoomCount.Value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(satanDoomCount),
                    "Satan doom count cannot be negative.");
            }

            CardId = cardId;
            DefinitionKey = definitionKey;
            EnglishName = definitionKey.ToUpperInvariant();
            IsFaceUp = isFaceUp;
            CanUse = canUse;
            DisplayName = displayName ?? string.Empty;
            Summary = summary ?? string.Empty;
            CostSummary = costSummary ?? string.Empty;
            ShowHoverBadgeWhenUnavailable = showHoverBadgeWhenUnavailable;
            IsUpsideDown = isUpsideDown;
            SatanDoomCount = satanDoomCount;
        }

        public int CardId { get; }

        public bool CanUse { get; }

        public string CostSummary { get; }

        public string DisplayName { get; }

        public string EnglishName { get; }

        public string DefinitionKey { get; }

        public bool IsFaceUp { get; }

        public bool IsUpsideDown { get; }

        public int? SatanDoomCount { get; }

        public bool ShowHoverBadgeWhenUnavailable { get; }

        public string Summary { get; }
    }
}
