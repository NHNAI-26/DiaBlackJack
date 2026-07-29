using System;
using System.Collections.Generic;

namespace DiaBlackJack.StageProgression
{
    public sealed class ShopVisit
    {
        private readonly HashSet<int> _purchasedOptionIds = new HashSet<int>();

        public ShopVisit(ShopOffer offer)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
        }

        public bool HasRemovedCard { get; private set; }

        public bool HasRested { get; private set; }

        public bool HasUsedAnyUtility => HasRemovedCard || HasRested;

        public bool IsClosed { get; private set; }

        public ShopTransaction LastTransaction { get; private set; }

        public ShopOffer Offer { get; }

        public IReadOnlyCollection<int> PurchasedOptionIds =>
            new List<int>(_purchasedOptionIds).AsReadOnly();

        public bool TryBuyCard(int offerId, int optionId, PlayerRunState player)
        {
            if (!CanProcess(offerId, player) ||
                _purchasedOptionIds.Contains(optionId) ||
                !Offer.TryGetOption(optionId, out ShopCardOption option) ||
                player.CurrentGold < option.Price ||
                !CanAddOption(player, option))
            {
                return false;
            }

            if (!player.TrySpendGold(option.Price))
            {
                throw new InvalidOperationException("Validated shop card cost was rejected.");
            }

            int addedCardId;
            switch (option.DeckKind)
            {
                case ShopCardDeckKind.Normal:
                    addedCardId = player.AddRewardCard(option.DefinitionKey).Id;
                    break;
                case ShopCardDeckKind.Demon:
                    addedCardId = player.AddDemonCard(option.DefinitionKey).Id;
                    break;
                default:
                    throw new InvalidOperationException("Shop option has an unknown deck kind.");
            }

            _purchasedOptionIds.Add(optionId);
            LastTransaction = new ShopTransaction(
                ShopTransactionKind.CardPurchase,
                option.Price,
                addedCardId,
                option.DefinitionKey,
                0);
            return true;
        }

        public bool TryRemoveCard(int offerId, int cardId, PlayerRunState player)
        {
            if (!CanProcess(offerId, player) ||
                HasRemovedCard ||
                player.CurrentGold < Offer.LighterPrice ||
                !player.CanRemoveCard(cardId))
            {
                return false;
            }

            if (!player.TrySpendGold(Offer.LighterPrice) ||
                !player.TryRemoveCard(cardId))
            {
                throw new InvalidOperationException("Validated lighter transaction was rejected.");
            }

            HasRemovedCard = true;
            LastTransaction = new ShopTransaction(
                ShopTransactionKind.CardRemoval,
                Offer.LighterPrice,
                cardId,
                null,
                0);
            return true;
        }

        public bool TryRest(int offerId, PlayerRunState player)
        {
            if (!CanProcess(offerId, player) ||
                HasRested ||
                player.CurrentSoul >= player.MaximumSoul ||
                player.CurrentGold < Offer.WhiskeyPrice)
            {
                return false;
            }

            int recoveredSoul = Math.Min(
                Offer.WhiskeyRecovery,
                player.MaximumSoul - player.CurrentSoul);
            if (!player.TrySpendGold(Offer.WhiskeyPrice))
            {
                throw new InvalidOperationException("Validated whiskey cost was rejected.");
            }

            player.SetCurrentSoul(player.CurrentSoul + recoveredSoul);
            HasRested = true;
            LastTransaction = new ShopTransaction(
                ShopTransactionKind.SoulRecovery,
                Offer.WhiskeyPrice,
                null,
                null,
                recoveredSoul);
            return true;
        }

        public bool TryClose(int offerId)
        {
            if (!CanClose(offerId))
            {
                return false;
            }

            IsClosed = true;
            LastTransaction = new ShopTransaction(
                ShopTransactionKind.Leave,
                0,
                null,
                null,
                0);
            return true;
        }

        internal bool CanClose(int offerId)
        {
            return !IsClosed && offerId == Offer.OfferId;
        }

        private bool CanProcess(int offerId, PlayerRunState player)
        {
            return !IsClosed && offerId == Offer.OfferId && player != null;
        }

        private static bool CanAddOption(
            PlayerRunState player,
            ShopCardOption option)
        {
            return option.DeckKind == ShopCardDeckKind.Normal
                ? player.CanAddCard
                : player.CanAddDemonCard;
        }
    }
}
