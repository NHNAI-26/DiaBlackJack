using System;
using System.Collections.Generic;
using System.Text;

namespace DiaBlackJack.CoreLoop.UI
{
    public sealed class CoreLoopViewModel
    {
        public CoreLoopViewModel(
            CoreLoopState state,
            BattleOutcome outcome,
            int roundNumber,
            string playerSoul,
            string enemySoul,
            string playerCards,
            string enemyCards,
            int playerTotal,
            int enemyVisibleTotal,
            string playerDeck,
            string enemyDeck,
            string lastRound,
            string foldActionText,
            string changeActionText,
            IReadOnlyList<string> changeCandidates,
            bool canHit,
            bool canStand,
            bool canFold,
            bool canChange,
            bool isChoosingChangeCard,
            bool canRestart)
        {
            State = state;
            Outcome = outcome;
            RoundNumber = roundNumber;
            PlayerSoul = playerSoul;
            EnemySoul = enemySoul;
            PlayerCards = playerCards;
            EnemyCards = enemyCards;
            PlayerTotal = playerTotal;
            EnemyVisibleTotal = enemyVisibleTotal;
            PlayerDeck = playerDeck;
            EnemyDeck = enemyDeck;
            LastRound = lastRound;
            FoldActionText = foldActionText;
            ChangeActionText = changeActionText;
            ChangeCandidates = changeCandidates ??
                throw new ArgumentNullException(nameof(changeCandidates));
            CanHit = canHit;
            CanStand = canStand;
            CanFold = canFold;
            CanChange = canChange;
            IsChoosingChangeCard = isChoosingChangeCard;
            CanRestart = canRestart;
        }

        public CoreLoopState State { get; }

        public BattleOutcome Outcome { get; }

        public int RoundNumber { get; }

        public string PlayerSoul { get; }

        public string EnemySoul { get; }

        public string PlayerCards { get; }

        public string EnemyCards { get; }

        public int PlayerTotal { get; }

        public int EnemyVisibleTotal { get; }

        public string PlayerDeck { get; }

        public string EnemyDeck { get; }

        public string LastRound { get; }

        public string FoldActionText { get; }

        public string ChangeActionText { get; }

        public IReadOnlyList<string> ChangeCandidates { get; }

        public bool CanHit { get; }

        public bool CanStand { get; }

        public bool CanFold { get; }

        public bool CanChange { get; }

        public bool IsChoosingChangeCard { get; }

        public bool CanRestart { get; }
    }

    public static class CoreLoopPresenter
    {
        public static CoreLoopViewModel Create(CoreLoopBattle battle)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            bool canPlayerAct = battle.CanPlayerAct;
            return new CoreLoopViewModel(
                battle.State,
                battle.Outcome,
                battle.RoundNumber,
                FormatSoul(battle.Player.Soul),
                FormatSoul(battle.Enemy.Soul),
                FormatCards(battle.Player.Hand.Cards, revealAll: true),
                FormatCards(battle.Enemy.Hand.Cards, revealAll: false),
                battle.Player.HandValue.Total,
                battle.Enemy.VisibleHandValue.Total,
                FormatDeck(battle.Player.Deck),
                FormatDeck(battle.Enemy.Deck),
                FormatLastRound(battle.LastResolution),
                FormatFoldAction(battle),
                FormatChangeAction(battle),
                FormatChangeCandidates(battle.PlayerChangeCandidates),
                canPlayerAct,
                canPlayerAct,
                battle.CanPlayerFold,
                battle.CanBeginPlayerChange,
                battle.CanSelectChangedCard,
                battle.State == CoreLoopState.BattleEnded);
        }

        private static string FormatFoldAction(CoreLoopBattle battle)
        {
            return battle.Player.Soul.Current == 1
                ? "FOLD (-1 SOUL = DEFEAT)"
                : "FOLD (-1 SOUL)";
        }

        private static string FormatChangeAction(CoreLoopBattle battle)
        {
            return battle.HasPlayerChangedThisRound
                ? "CHANGE (USED)"
                : "CHANGE (1/ROUND)";
        }

        private static IReadOnlyList<string> FormatChangeCandidates(
            IReadOnlyList<BlackjackCard> candidates)
        {
            var labels = new string[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                labels[i] = candidates[i].Rank.ToString();
            }

            return Array.AsReadOnly(labels);
        }

        private static string FormatSoul(SoulPool soul)
        {
            return $"{soul.Current} / {soul.Maximum}";
        }

        private static string FormatDeck(BlackjackDeck deck)
        {
            return $"Draw {deck.DrawCount}  |  Discard {deck.DiscardCount}";
        }

        private static string FormatCards(IReadOnlyList<BlackjackCard> cards, bool revealAll)
        {
            if (cards.Count == 0)
            {
                return "-";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < cards.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  ");
                }

                BlackjackCard card = cards[i];
                builder.Append(revealAll || card.IsFaceUp ? card.Rank.ToString() : "?");
            }

            return builder.ToString();
        }

        private static string FormatLastRound(RoundResolution? resolution)
        {
            if (!resolution.HasValue)
            {
                return "No round result yet";
            }

            switch (resolution.Value.Outcome)
            {
                case RoundOutcome.PlayerBust:
                    return "Player bust  |  Player soul -2";
                case RoundOutcome.EnemyBust:
                    return "Enemy bust  |  Enemy soul -1";
                case RoundOutcome.PlayerTwentyOneWin:
                    return "Player 21  |  Enemy soul -2";
                case RoundOutcome.PlayerWin:
                    return "Player wins round  |  Enemy soul -1";
                case RoundOutcome.EnemyWin:
                    return "Enemy wins round  |  Player soul -1";
                case RoundOutcome.PlayerFold:
                    return "Player folds  |  Player soul -1";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
