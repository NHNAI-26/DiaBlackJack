using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// A single card projected for world-space rendering. <see cref="IsFaceUp"/> is the *physical*
    /// orientation (drives the card back visual). <see cref="RevealRank"/> is whether the rank may be
    /// shown to the viewer: true for all of the player's own cards (a player sees their own hidden
    /// card), but for the enemy only when the card is face-up. When <see cref="RevealRank"/> is false
    /// the <see cref="Rank"/> is forced to 0 — the hidden enemy rank never crosses into the view.
    /// </summary>
    public sealed class GameSceneCardViewModel
    {
        public GameSceneCardViewModel(int cardId, int rank, bool isFaceUp, bool revealRank, string displayName)
        {
            CardId = cardId;
            Rank = rank;
            IsFaceUp = isFaceUp;
            RevealRank = revealRank;
            DisplayName = displayName ?? string.Empty;
        }

        public int CardId { get; }

        public int Rank { get; }

        public bool IsFaceUp { get; }

        public bool RevealRank { get; }

        public string DisplayName { get; }
    }

    /// <summary>
    /// Read-only projection consumed by <c>GameSceneView</c>. Wraps the shared
    /// <see cref="CoreLoopViewModel"/> (souls, totals, state, player card actions) and adds
    /// world-render-friendly card lists for both sides.
    /// </summary>
    public sealed class GameSceneViewModel
    {
        public GameSceneViewModel(
            CoreLoopViewModel core,
            IReadOnlyList<GameSceneCardViewModel> playerCards,
            IReadOnlyList<GameSceneCardViewModel> enemyCards)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            PlayerCards = playerCards ?? throw new ArgumentNullException(nameof(playerCards));
            EnemyCards = enemyCards ?? throw new ArgumentNullException(nameof(enemyCards));
        }

        public CoreLoopViewModel Core { get; }

        public IReadOnlyList<GameSceneCardViewModel> PlayerCards { get; }

        public IReadOnlyList<GameSceneCardViewModel> EnemyCards { get; }
    }

    public static class GameScenePresenter
    {
        public static GameSceneViewModel Create(CoreLoopBattle battle, string profileKey = null)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle, profileKey);
            return new GameSceneViewModel(
                core,
                CreatePlayerCards(core),
                CreateEnemyCards(battle));
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreatePlayerCards(CoreLoopViewModel core)
        {
            var cards = new List<GameSceneCardViewModel>(core.PlayerCardActions.Count);
            foreach (PlayerCardViewModel card in core.PlayerCardActions)
            {
                // The player sees every one of their own cards, including the face-down one.
                cards.Add(new GameSceneCardViewModel(
                    card.CardId,
                    card.Rank,
                    card.IsFaceUp,
                    revealRank: true,
                    card.DisplayName));
            }

            return cards.AsReadOnly();
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreateEnemyCards(CoreLoopBattle battle)
        {
            IReadOnlyList<BlackjackCard> hand = battle.Enemy.Hand.Cards;
            var cards = new List<GameSceneCardViewModel>(hand.Count);
            foreach (BlackjackCard card in hand)
            {
                // Face-down enemy card: emit no rank. This is the information-hiding boundary.
                bool faceUp = card.IsFaceUp;
                cards.Add(new GameSceneCardViewModel(
                    card.Id,
                    faceUp ? card.Rank : 0,
                    faceUp,
                    revealRank: faceUp,
                    faceUp ? card.Definition.DisplayName : string.Empty));
            }

            return cards.AsReadOnly();
        }
    }
}
