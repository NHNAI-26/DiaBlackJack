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
                CardDefinition definition = CardDefinitionCatalog.GetByKey(card.DefinitionKey);
                playerCards.Add(new BlackjackCard(card.Id, definition));
            }

            var playerDeck = new BlackjackDeck(playerCards, stage.PlayerDeckSeed);
            if (stage.BattleProfileKey == null)
            {
                return new CoreLoopBattle(
                    playerDeck,
                    BlackjackDeck.CreateStandard(stage.EnemyDeckSeed),
                    player.MaximumSoul,
                    player.CurrentSoul,
                    stage.EnemyMaximumSoul);
            }

            EnemyBattleConfiguration enemy = EnemyBattleConfigurationFactory.Create(
                stage.BattleProfileKey,
                stage.EnemyDeckSeed);
            if (enemy.EnemyMaximumSoul != stage.EnemyMaximumSoul)
            {
                throw new InvalidOperationException(
                    "The selected enemy profile no longer matches the stage soul configuration.");
            }

            return new CoreLoopBattle(
                playerDeck,
                enemy.CreateEnemyDeck(),
                player.MaximumSoul,
                player.CurrentSoul,
                enemy.EnemyMaximumSoul,
                enemy.BehaviorPolicy);
        }
    }
}
