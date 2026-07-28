using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class DemonContractCardState
    {
        private readonly List<PaimonExileRecord> _paimonExiles =
            new List<PaimonExileRecord>();
        private readonly List<BelialTransferRecord> _belialTransfers =
            new List<BelialTransferRecord>();
        private int _nextTemporaryCardId = int.MaxValue;

        public int PaimonExileCount => _paimonExiles.Count;

        public int BelialTransferCount => _belialTransfers.Count;

        public void TrackPaimonExile(
            BattleParticipant originalOwner,
            BlackjackCard card)
        {
            if (originalOwner == null)
            {
                throw new ArgumentNullException(nameof(originalOwner));
            }

            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _paimonExiles.Add(new PaimonExileRecord(originalOwner, card));
        }

        public bool TryTransferFaceUpCard(
            BattleParticipant source,
            BattleParticipant target,
            int cardId,
            int sourceContractCardId,
            out BlackjackCard transferredCard)
        {
            transferredCard = null;
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!source.Hand.TryGetCard(cardId, out BlackjackCard sourceCard) ||
                !sourceCard.IsFaceUp)
            {
                return false;
            }

            int temporaryCardId = TakeTemporaryCardId(source.Deck, target.Deck);
            var proxyCard = new BlackjackCard(
                temporaryCardId,
                sourceCard.Definition,
                isFaceUp: true,
                suit: sourceCard.Suit);

            if (!source.Hand.TryTakeCard(cardId, out BlackjackCard removedCard) ||
                !ReferenceEquals(sourceCard, removedCard))
            {
                throw new InvalidOperationException(
                    "Validated Belial source card could not be removed.");
            }

            target.Deck.RegisterTemporaryCardInPlay(proxyCard);
            target.AddFaceUpCard(proxyCard);
            source.CancelStand();
            _belialTransfers.Add(new BelialTransferRecord(
                source,
                target,
                sourceCard,
                proxyCard,
                sourceContractCardId));
            transferredCard = proxyCard;
            return true;
        }

        public void RestoreAll()
        {
            foreach (PaimonExileRecord exile in _paimonExiles)
            {
                exile.OriginalOwner.Deck.Discard(exile.Card);
            }

            _paimonExiles.Clear();

            for (int i = _belialTransfers.Count - 1; i >= 0; i--)
            {
                BelialTransferRecord transfer = _belialTransfers[i];
                if (!transfer.CurrentOwner.TryRemoveTemporaryCard(
                        transfer.ProxyCard.Id))
                {
                    throw new InvalidOperationException(
                        "Belial proxy card could not be removed during battle cleanup.");
                }

                transfer.OriginalOwner.Deck.Discard(transfer.OriginalCard);
            }

            _belialTransfers.Clear();
        }

        private int TakeTemporaryCardId(
            BlackjackDeck sourceDeck,
            BlackjackDeck targetDeck)
        {
            while (_nextTemporaryCardId >= 0 &&
                (sourceDeck.ContainsKnownCardId(_nextTemporaryCardId) ||
                    targetDeck.ContainsKnownCardId(_nextTemporaryCardId)))
            {
                _nextTemporaryCardId--;
            }

            if (_nextTemporaryCardId < 0)
            {
                throw new InvalidOperationException(
                    "No temporary card id remains for a Belial transfer.");
            }

            return _nextTemporaryCardId--;
        }

        private sealed class PaimonExileRecord
        {
            public PaimonExileRecord(
                BattleParticipant originalOwner,
                BlackjackCard card)
            {
                OriginalOwner = originalOwner;
                Card = card;
            }

            public BlackjackCard Card { get; }

            public BattleParticipant OriginalOwner { get; }
        }

        private sealed class BelialTransferRecord
        {
            public BelialTransferRecord(
                BattleParticipant originalOwner,
                BattleParticipant currentOwner,
                BlackjackCard originalCard,
                BlackjackCard proxyCard,
                int sourceContractCardId)
            {
                OriginalOwner = originalOwner;
                CurrentOwner = currentOwner;
                OriginalCard = originalCard;
                ProxyCard = proxyCard;
                SourceContractCardId = sourceContractCardId;
            }

            public BattleParticipant CurrentOwner { get; }

            public BlackjackCard OriginalCard { get; }

            public BattleParticipant OriginalOwner { get; }

            public BlackjackCard ProxyCard { get; }

            public int SourceContractCardId { get; }
        }
    }
}
