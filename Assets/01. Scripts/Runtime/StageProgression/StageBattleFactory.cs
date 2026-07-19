using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public static class StageBattleFactory
    {
        public static CoreLoopBattle Create(StageDefinition stage, PlayerRunState player)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var playerCards = new List<BlackjackCard>(player.Deck.Count);
            foreach (RunCardDefinition card in player.Deck)
            {
                playerCards.Add(new BlackjackCard(card.Id, card.Rank));
            }

            return new CoreLoopBattle(
                new BlackjackDeck(playerCards, stage.PlayerDeckSeed),
                BlackjackDeck.CreateStandard(stage.EnemyDeckSeed),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul);
        }
    }
}
