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
                playerCards.Add(new BlackjackCard(card.Id, definition, suit: card.Suit));
            }

            var playerDeck = new BlackjackDeck(playerCards, stage.PlayerDeckSeed);
            DemonContractDeck playerDemonDeck = CreateDemonDeck(
                player.DemonDeck,
                DeriveDemonDeckSeed(stage.PlayerDeckSeed));
            if (stage.BattleProfileKey == null)
            {
                return new CoreLoopBattle(
                    playerDeck,
                    BlackjackDeck.CreateStandard(stage.EnemyDeckSeed),
                    player.MaximumSoul,
                    player.CurrentSoul,
                    stage.EnemyMaximumSoul,
                    playerDemonDeck: playerDemonDeck,
                    demonContractSeed: DeriveDemonContractSeed(
                        stage.PlayerDeckSeed,
                        stage.EnemyDeckSeed));
            }

            EnemyBattleConfiguration enemy = EnemyBattleConfigurationFactory.Create(
                stage.BattleProfileKey,
                stage.EnemyDeckSeed);
            if (enemy.EnemyMaximumSoul != stage.EnemyMaximumSoul)
            {
                throw new InvalidOperationException(
                    "The selected enemy profile no longer matches the stage soul configuration.");
            }

            DemonContractDeck enemyDemonDeck = CreateEnemyDemonDeck(
                enemy.DemonContractDefinitionKeys,
                DeriveEnemyDemonDeckSeed(stage.EnemyDeckSeed));

            return new CoreLoopBattle(
                playerDeck,
                enemy.CreateEnemyDeck(),
                player.MaximumSoul,
                player.CurrentSoul,
                enemy.EnemyMaximumSoul,
                enemyPolicy: enemy.BehaviorPolicy,
                playerDemonDeck: playerDemonDeck,
                enemyDemonDeck: enemyDemonDeck,
                enemyChangeCostMode: enemy.ChangeCostMode,
                enemyDemonContractCandidateCount:
                    enemy.DemonContractCandidateCount,
                injectsPoisonIntoPlayerDeckEachRound:
                    enemy.InjectsPoisonIntoPlayerDeckEachRound,
                enablesEnemyChange: true,
                fixedEnemyDemonContractPhases:
                    enemy.FixedDemonContractPhases,
                demonContractSeed: DeriveDemonContractSeed(
                    stage.PlayerDeckSeed,
                    stage.EnemyDeckSeed));
        }

        internal static DemonContractDeck CreateEnemyDemonDeck(
            IReadOnlyList<string> definitionKeys,
            int seed)
        {
            List<DemonContractCard> cards = new List<DemonContractCard>(definitionKeys.Count);
            for (int i = 0; i < definitionKeys.Count; i++)
            {
                DemonContractDefinition definition =
                    DemonContractCatalog.Default.GetByKey(definitionKeys[i]);
                cards.Add(new DemonContractCard(i, definition));
            }

            return new DemonContractDeck(cards, seed);
        }

        internal static DemonContractDeck CreateDemonDeck(
            IReadOnlyList<RunDemonDefinition> runCards,
            int seed)
        {
            var cards = new List<DemonContractCard>(runCards.Count);
            foreach (RunDemonDefinition runCard in runCards)
            {
                DemonContractDefinition definition =
                    DemonContractCatalog.Default.GetByKey(runCard.DefinitionKey);
                cards.Add(new DemonContractCard(runCard.Id, definition));
            }

            return new DemonContractDeck(cards, seed);
        }

        internal static int DeriveDemonDeckSeed(int playerDeckSeed)
        {
            unchecked
            {
                return playerDeckSeed ^ (int)0x6D2B79F5u;
            }
        }

        internal static int DeriveEnemyDemonDeckSeed(int enemyDeckSeed)
        {
            unchecked
            {
                return enemyDeckSeed ^ (int)0x4C957F2Du;
            }
        }

        internal static int DeriveDemonContractSeed(
            int playerDeckSeed,
            int enemyDeckSeed)
        {
            unchecked
            {
                return playerDeckSeed ^ (enemyDeckSeed * 397) ^
                    (int)0xA511E9B3u;
            }
        }
    }
}
