using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public enum ShopCardDeckKind
    {
        Normal,
        Demon
    }

    public enum ShopTransactionKind
    {
        CardPurchase,
        CardRemoval,
        SoulRecovery,
        Leave
    }

    public sealed class ShopCardOption
    {
        public ShopCardOption(
            int optionId,
            ShopCardDeckKind deckKind,
            string definitionKey,
            int price)
        {
            if (optionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(optionId));
            }

            if (!Enum.IsDefined(typeof(ShopCardDeckKind), deckKind))
            {
                throw new ArgumentOutOfRangeException(nameof(deckKind));
            }

            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price));
            }

            DefinitionKey = ValidateDefinitionKey(deckKind, definitionKey);
            OptionId = optionId;
            DeckKind = deckKind;
            Price = price;
        }

        public ShopCardDeckKind DeckKind { get; }

        public string DefinitionKey { get; }

        public int OptionId { get; }

        public int Price { get; }

        private static string ValidateDefinitionKey(
            ShopCardDeckKind deckKind,
            string definitionKey)
        {
            switch (deckKind)
            {
                case ShopCardDeckKind.Normal:
                    return CardDefinitionCatalog.GetByKey(definitionKey).Key;
                case ShopCardDeckKind.Demon:
                    return DemonContractCatalog.Default.GetByKey(definitionKey).Key;
                default:
                    throw new ArgumentOutOfRangeException(nameof(deckKind));
            }
        }
    }

    public sealed class ShopOffer
    {
        private readonly Dictionary<int, ShopCardOption> _optionsById;

        public ShopOffer(
            int offerId,
            int visitIndex,
            int utilityPriceLevel,
            int lighterPrice,
            int whiskeyPrice,
            int whiskeyRecovery,
            IEnumerable<ShopCardOption> cardOptions)
        {
            if (offerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offerId));
            }

            if (visitIndex < 0 || visitIndex > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(visitIndex));
            }

            if (utilityPriceLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(utilityPriceLevel));
            }

            if (lighterPrice < 0 || whiskeyPrice < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lighterPrice),
                    "Shop utility prices cannot be negative.");
            }

            if (whiskeyRecovery <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(whiskeyRecovery));
            }

            if (cardOptions == null)
            {
                throw new ArgumentNullException(nameof(cardOptions));
            }

            var copiedOptions = new List<ShopCardOption>();
            var demonKeys = new HashSet<string>(StringComparer.Ordinal);
            _optionsById = new Dictionary<int, ShopCardOption>();
            int normalCount = 0;
            int demonCount = 0;
            foreach (ShopCardOption option in cardOptions)
            {
                if (option == null)
                {
                    throw new ArgumentException(
                        "Shop card options cannot contain null.",
                        nameof(cardOptions));
                }

                if (!_optionsById.TryAdd(option.OptionId, option))
                {
                    throw new ArgumentException(
                        $"Shop option id {option.OptionId} is duplicated.",
                        nameof(cardOptions));
                }

                if (option.DeckKind == ShopCardDeckKind.Demon &&
                    !demonKeys.Add(option.DefinitionKey))
                {
                    throw new ArgumentException(
                        $"Shop demon definition '{option.DefinitionKey}' is duplicated.",
                        nameof(cardOptions));
                }

                if (option.DeckKind == ShopCardDeckKind.Normal)
                {
                    normalCount++;
                }
                else
                {
                    demonCount++;
                }

                copiedOptions.Add(option);
            }

            if (normalCount != 3 || demonCount != 2 || copiedOptions.Count != 5)
            {
                throw new ArgumentException(
                    "A shop offer requires exactly three normal and two demon cards.",
                    nameof(cardOptions));
            }

            OfferId = offerId;
            VisitIndex = visitIndex;
            UtilityPriceLevel = utilityPriceLevel;
            LighterPrice = lighterPrice;
            WhiskeyPrice = whiskeyPrice;
            WhiskeyRecovery = whiskeyRecovery;
            CardOptions = new ReadOnlyCollection<ShopCardOption>(copiedOptions);
        }

        public IReadOnlyList<ShopCardOption> CardOptions { get; }

        public int LighterPrice { get; }

        public int OfferId { get; }

        public int UtilityPriceLevel { get; }

        public int VisitIndex { get; }

        public int WhiskeyPrice { get; }

        public int WhiskeyRecovery { get; }

        public bool TryGetOption(int optionId, out ShopCardOption option)
        {
            return _optionsById.TryGetValue(optionId, out option);
        }
    }

    public sealed class ShopTransaction
    {
        internal ShopTransaction(
            ShopTransactionKind kind,
            int goldSpent,
            int? affectedCardId,
            string definitionKey,
            int soulRecovered)
        {
            Kind = kind;
            GoldSpent = goldSpent;
            AffectedCardId = affectedCardId;
            DefinitionKey = definitionKey;
            SoulRecovered = soulRecovered;
        }

        public int? AffectedCardId { get; }

        public string DefinitionKey { get; }

        public int GoldSpent { get; }

        public ShopTransactionKind Kind { get; }

        public int SoulRecovered { get; }
    }
}
