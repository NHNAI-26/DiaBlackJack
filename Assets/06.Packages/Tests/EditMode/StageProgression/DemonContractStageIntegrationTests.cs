using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class DemonContractStageIntegrationTests
    {
        [Test]
        public void DC_I01_RunStartsWithFourPrototypeDemonCards()
        {
            PlayerRunState player = CreatePlayer();

            Assert.That(player.DemonDeck.Count, Is.EqualTo(4));
            Assert.That(player.DemonDeck.Select(card => card.Id),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(player.DemonDeck.Select(card => card.DefinitionKey),
                Is.EqualTo(new[]
                {
                    DemonContractCatalog.SatanKey,
                    DemonContractCatalog.BelphegorKey,
                    DemonContractCatalog.MammonKey,
                    DemonContractCatalog.LeviathanKey
                }));
        }

        [Test]
        public void DC_I02_RunDemonDeckCopiesInputAndRejectsDuplicatePhysicalIds()
        {
            var source = new List<RunDemonDefinition>
            {
                new RunDemonDefinition(8, DemonContractCatalog.SatanKey),
                new RunDemonDefinition(9, DemonContractCatalog.SatanKey)
            };
            PlayerRunState player = CreatePlayer(source);

            source.Clear();

            Assert.That(player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(player.DemonDeck[0].DefinitionKey,
                Is.EqualTo(DemonContractCatalog.SatanKey));
            Assert.That(player.DemonDeck[1].DefinitionKey,
                Is.EqualTo(DemonContractCatalog.SatanKey));
            Assert.Throws<ArgumentException>(() => CreatePlayer(new[]
            {
                new RunDemonDefinition(1, DemonContractCatalog.SatanKey),
                new RunDemonDefinition(1, DemonContractCatalog.BelphegorKey)
            }));
            Assert.Throws<KeyNotFoundException>(() =>
                new RunDemonDefinition(0, "missing-demon"));
        }

        [Test]
        public void DC_I03_RunResetRestoresInitialDemonDeckAndNextPhysicalId()
        {
            PlayerRunState player = CreatePlayer();
            RunDemonDefinition acquired = player.AddDemonCard(DemonContractCatalog.SatanKey);

            Assert.That(acquired.Id, Is.EqualTo(4));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(5));

            player.SetCurrentSoul(2);
            player.ResetForNewRun();

            Assert.That(player.CurrentSoul, Is.EqualTo(player.MaximumSoul));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(4));
            Assert.That(player.DemonDeck.Select(card => card.Id),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(player.AddDemonCard(DemonContractCatalog.LeviathanKey).Id,
                Is.EqualTo(4));
        }

        [Test]
        public void DC_I04_StageFactoryCreatesIndependentBattleDemonDecks()
        {
            PlayerRunState player = CreatePlayer();
            StageDefinition stage = CreateStage(playerDeckSeed: 41);

            CoreLoopBattle firstBattle = StageBattleFactory.Create(stage, player);
            CoreLoopBattle secondBattle = StageBattleFactory.Create(stage, player);

            Assert.That(firstBattle.PlayerDemonDeck, Is.Not.SameAs(secondBattle.PlayerDemonDeck));
            Assert.That(firstBattle.PlayerDemonDeck.TotalCardCount, Is.EqualTo(4));
            Assert.That(secondBattle.PlayerDemonDeck.TotalCardCount, Is.EqualTo(4));

            string[] firstCandidates = firstBattle.PlayerDemonDeck
                .TakeCandidates()
                .Select(card => $"{card.Id}:{card.DefinitionKey}")
                .ToArray();
            string[] secondCandidates = secondBattle.PlayerDemonDeck
                .TakeCandidates()
                .Select(card => $"{card.Id}:{card.DefinitionKey}")
                .ToArray();

            Assert.That(secondCandidates, Is.EqualTo(firstCandidates));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(4));
            Assert.That(player.DemonDeck.Select(card => card.Id),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void DC_I05_DemonShuffleDoesNotChangeNormalCardOrder()
        {
            const int playerDeckSeed = 817;
            PlayerRunState player = CreatePlayer();
            CoreLoopBattle battle = StageBattleFactory.Create(
                CreateStage(playerDeckSeed),
                player);

            BlackjackDeck expectedDeck = CreateExpectedPlayerDeck(player, playerDeckSeed);

            int[] actualOrder = DrawIds(battle.Player.Deck, player.Deck.Count);
            int[] expectedOrder = DrawIds(expectedDeck, player.Deck.Count);
            Assert.That(actualOrder, Is.EqualTo(expectedOrder));
        }

        [Test]
        public void DC_I06_StageConversionPreservesCustomPhysicalIdsAndDefinitions()
        {
            var runDemonDeck = new[]
            {
                new RunDemonDefinition(21, DemonContractCatalog.SatanKey),
                new RunDemonDefinition(34, DemonContractCatalog.BelphegorKey),
                new RunDemonDefinition(55, DemonContractCatalog.MammonKey),
                new RunDemonDefinition(89, DemonContractCatalog.LeviathanKey)
            };
            PlayerRunState player = CreatePlayer(runDemonDeck);

            CoreLoopBattle battle = StageBattleFactory.Create(CreateStage(7), player);
            IReadOnlyList<DemonContractCard> candidates =
                battle.PlayerDemonDeck.TakeCandidates();
            battle.PlayerDemonDeck.Discard(candidates);
            DemonContractCard[] converted = candidates
                .Concat(battle.PlayerDemonDeck.TakeCandidates())
                .GroupBy(card => card.Id)
                .Select(group => group.First())
                .ToArray();

            Assert.That(converted.Select(card => card.Id),
                Is.EquivalentTo(new[] { 21, 34, 55, 89 }));
            Assert.That(converted.Select(card => card.DefinitionKey),
                Is.EquivalentTo(runDemonDeck.Select(card => card.DefinitionKey)));
        }

        private static int[] DrawIds(BlackjackDeck deck, int count)
        {
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = deck.Draw().Id;
            }

            return ids;
        }

        private static BlackjackDeck CreateExpectedPlayerDeck(
            PlayerRunState player,
            int seed)
        {
            var cards = new List<BlackjackCard>(player.Deck.Count);
            foreach (RunCardDefinition runCard in player.Deck)
            {
                cards.Add(new BlackjackCard(
                    runCard.Id,
                    CardDefinitionCatalog.GetByKey(runCard.DefinitionKey)));
            }

            return new BlackjackDeck(cards, seed);
        }

        private static PlayerRunState CreatePlayer(
            IEnumerable<RunDemonDefinition> demonDeck = null)
        {
            var normalDeck = new[]
            {
                new RunCardDefinition(0, 1),
                new RunCardDefinition(1, 2),
                new RunCardDefinition(2, 3),
                new RunCardDefinition(3, 4),
                new RunCardDefinition(4, 5),
                new RunCardDefinition(5, 6)
            };

            return demonDeck == null
                ? new PlayerRunState(12, 12, normalDeck)
                : new PlayerRunState(12, 12, normalDeck, demonDeck);
        }

        private static StageDefinition CreateStage(int playerDeckSeed)
        {
            return new StageDefinition(
                "dc-test-stage",
                "계약 테스트",
                StageKind.NormalCombat,
                3,
                playerDeckSeed,
                31);
        }
    }
}
