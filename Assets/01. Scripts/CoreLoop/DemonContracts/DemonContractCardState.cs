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
        private readonly List<BaphometWaveRecord> _baphometWaves =
            new List<BaphometWaveRecord>();
        private int _nextTemporaryCardId = int.MaxValue;

        public int PaimonExileCount => _paimonExiles.Count;

        public int BelialTransferCount => _belialTransfers.Count;

        public int BaphometPentagramCount
        {
            get
            {
                int count = 0;
                foreach (BaphometWaveRecord wave in _baphometWaves)
                {
                    count += wave.CardIds.Count;
                }

                return count;
            }
        }

        public int BaphometWaveCount => _baphometWaves.Count;

        public void CreateBaphometWaves(
            BattleParticipant owner,
            CombatantSide ownerSide,
            BattleParticipant opponent,
            int sourceContractCardId)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (opponent == null)
            {
                throw new ArgumentNullException(nameof(opponent));
            }

            CreateBaphometWave(
                owner,
                ownerSide,
                sourceContractCardId,
                BaphometPentagramCatalog.OwnerRanks);
            CreateBaphometWave(
                opponent,
                ownerSide == CombatantSide.Player
                    ? CombatantSide.Enemy
                    : CombatantSide.Player,
                sourceContractCardId,
                BaphometPentagramCatalog.OpponentRanks);
        }

        public bool TryResetNextExhaustedBaphometWave(
            out BaphometExhaustion exhaustion)
        {
            foreach (BaphometWaveRecord wave in _baphometWaves)
            {
                bool hasPentagramInDrawPile = false;
                foreach (int cardId in wave.CardIds)
                {
                    if (wave.Target.Deck.IsInDrawPile(cardId))
                    {
                        hasPentagramInDrawPile = true;
                        break;
                    }
                }

                if (hasPentagramInDrawPile)
                {
                    continue;
                }

                RemoveBaphometWaveCards(wave);
                wave.Target.Deck.ResetAvailableCards();
                InsertBaphometCards(wave);
                exhaustion = new BaphometExhaustion(
                    wave.TargetSide,
                    wave.SourceContractCardId);
                return true;
            }

            exhaustion = null;
            return false;
        }

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
            foreach (BaphometWaveRecord wave in _baphometWaves)
            {
                RemoveBaphometWaveCards(wave);
            }

            _baphometWaves.Clear();

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

        private void CreateBaphometWave(
            BattleParticipant target,
            CombatantSide targetSide,
            int sourceContractCardId,
            IReadOnlyList<int> ranks)
        {
            var wave = new BaphometWaveRecord(
                target,
                targetSide,
                sourceContractCardId,
                ranks);
            InsertBaphometCards(wave);
            _baphometWaves.Add(wave);
        }

        private void InsertBaphometCards(BaphometWaveRecord wave)
        {
            wave.CardIds.Clear();
            foreach (int rank in wave.Ranks)
            {
                int cardId = TakeTemporaryCardId(wave.Target.Deck);
                var card = new BlackjackCard(
                    cardId,
                    BaphometPentagramCatalog.GetByRank(rank));
                if (!wave.Target.Deck.TryAddTemporaryAvailableCard(card))
                {
                    throw new InvalidOperationException(
                        "Validated Baphomet pentagram could not be inserted.");
                }

                wave.CardIds.Add(cardId);
            }
        }

        private void RemoveBaphometWaveCards(BaphometWaveRecord wave)
        {
            foreach (int cardId in wave.CardIds)
            {
                BlackjackCard detachedCard =
                    TakeBelialTransferredOriginal(wave.Target, cardId) ??
                    TakePaimonExile(wave.Target, cardId);
                bool removed = detachedCard != null
                    ? wave.Target.Deck.TryRemoveTemporaryCard(
                        cardId,
                        detachedCard)
                    : wave.Target.TryRemoveTemporaryCard(cardId);
                if (!removed)
                {
                    throw new InvalidOperationException(
                        "Baphomet pentagram could not be removed from the battle.");
                }
            }
        }

        private BlackjackCard TakeBelialTransferredOriginal(
            BattleParticipant originalOwner,
            int originalCardId)
        {
            for (int i = _belialTransfers.Count - 1; i >= 0; i--)
            {
                BelialTransferRecord transfer = _belialTransfers[i];
                if (!ReferenceEquals(transfer.OriginalOwner, originalOwner) ||
                    transfer.OriginalCard.Id != originalCardId)
                {
                    continue;
                }

                BlackjackCard detachedProxy = TakePaimonExile(
                    transfer.CurrentOwner,
                    transfer.ProxyCard.Id);
                bool proxyRemoved = detachedProxy != null
                    ? transfer.CurrentOwner.Deck.TryRemoveTemporaryCard(
                        transfer.ProxyCard.Id,
                        detachedProxy)
                    : transfer.CurrentOwner.TryRemoveTemporaryCard(
                        transfer.ProxyCard.Id);
                if (!proxyRemoved)
                {
                    throw new InvalidOperationException(
                        "Belial pentagram proxy could not be removed.");
                }

                _belialTransfers.RemoveAt(i);
                return transfer.OriginalCard;
            }

            return null;
        }

        private BlackjackCard TakePaimonExile(
            BattleParticipant originalOwner,
            int cardId)
        {
            for (int i = _paimonExiles.Count - 1; i >= 0; i--)
            {
                PaimonExileRecord exile = _paimonExiles[i];
                if (!ReferenceEquals(exile.OriginalOwner, originalOwner) ||
                    exile.Card.Id != cardId)
                {
                    continue;
                }

                _paimonExiles.RemoveAt(i);
                return exile.Card;
            }

            return null;
        }

        private int TakeTemporaryCardId(
            params BlackjackDeck[] decks)
        {
            while (_nextTemporaryCardId >= 0 &&
                ContainsCardId(decks, _nextTemporaryCardId))
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

        private static bool ContainsCardId(
            IReadOnlyList<BlackjackDeck> decks,
            int cardId)
        {
            foreach (BlackjackDeck deck in decks)
            {
                if (deck.ContainsKnownCardId(cardId))
                {
                    return true;
                }
            }

            return false;
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

        private sealed class BaphometWaveRecord
        {
            public BaphometWaveRecord(
                BattleParticipant target,
                CombatantSide targetSide,
                int sourceContractCardId,
                IReadOnlyList<int> ranks)
            {
                Target = target;
                TargetSide = targetSide;
                SourceContractCardId = sourceContractCardId;
                Ranks = ranks ?? throw new ArgumentNullException(nameof(ranks));
                CardIds = new List<int>(ranks.Count);
            }

            public List<int> CardIds { get; }

            public IReadOnlyList<int> Ranks { get; }

            public int SourceContractCardId { get; }

            public BattleParticipant Target { get; }

            public CombatantSide TargetSide { get; }
        }
    }

    internal sealed class BaphometExhaustion
    {
        public BaphometExhaustion(
            CombatantSide targetSide,
            int sourceContractCardId)
        {
            TargetSide = targetSide;
            SourceContractCardId = sourceContractCardId;
        }

        public int SourceContractCardId { get; }

        public CombatantSide TargetSide { get; }
    }
}
