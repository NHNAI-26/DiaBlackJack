using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class DemonContractFoundationTests
    {
        [TestCase(DemonContractCatalog.SatanKey, "사탄", DemonContractKind.Satan)]
        [TestCase(DemonContractCatalog.BelphegorKey, "벨페고르", DemonContractKind.Belphegor)]
        [TestCase(DemonContractCatalog.MammonKey, "마몬", DemonContractKind.Mammon)]
        [TestCase(DemonContractCatalog.LeviathanKey, "레비아탄", DemonContractKind.Leviathan)]
        public void DC_U01_DefaultCatalogProvidesStablePrototypeDefinitions(
            string key,
            string displayName,
            DemonContractKind kind)
        {
            DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(key);

            Assert.That(definition.Key, Is.EqualTo(key));
            Assert.That(definition.DisplayName, Is.EqualTo(displayName));
            Assert.That(definition.Kind, Is.EqualTo(kind));
            Assert.That(definition.BaseSoulCost, Is.EqualTo(1));
            Assert.That(definition.Summary, Is.Not.Empty);
            Assert.That(definition.CostSummary, Is.Not.Empty);
            Assert.That(DemonContractCatalog.Default.Definitions.Count, Is.EqualTo(4));
        }

        [Test]
        public void DC_U02_DefinitionAndCatalogRejectInvalidIdentity()
        {
            Assert.Throws<ArgumentException>(() => new DemonContractDefinition(
                " ",
                "악마",
                DemonContractKind.Satan,
                1,
                "효과",
                "대가"));
            Assert.Throws<ArgumentException>(() => new DemonContractDefinition(
                "demon",
                " ",
                DemonContractKind.Satan,
                1,
                "효과",
                "대가"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DemonContractDefinition(
                "demon",
                "악마",
                (DemonContractKind)99,
                1,
                "효과",
                "대가"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DemonContractDefinition(
                "demon",
                "악마",
                DemonContractKind.Satan,
                -1,
                "효과",
                "대가"));
            Assert.Throws<ArgumentException>(() => new DemonContractDefinition(
                "demon",
                "악마",
                DemonContractKind.Satan,
                1,
                "효과",
                " "));
            Assert.Throws<ArgumentException>(() => new DemonContractDefinition(
                "demon",
                "악마",
                DemonContractKind.Satan,
                1,
                " ",
                "대가"));
            Assert.Throws<KeyNotFoundException>(() =>
                DemonContractCatalog.Default.GetByKey("missing-demon"));
        }

        [Test]
        public void DC_U03_CatalogRejectsDuplicateKeysAndNullDefinitions()
        {
            var first = new DemonContractDefinition(
                "same",
                "첫 악마",
                DemonContractKind.Satan,
                1,
                "효과",
                "대가");
            var second = new DemonContractDefinition(
                "same",
                "둘째 악마",
                DemonContractKind.Belphegor,
                1,
                "효과",
                "대가");

            Assert.Throws<ArgumentException>(() =>
                new DemonContractCatalog(new[] { first, second }));
            Assert.Throws<ArgumentException>(() =>
                new DemonContractCatalog(new DemonContractDefinition[] { null }));
            Assert.Throws<ArgumentException>(() =>
                new DemonContractCatalog(Array.Empty<DemonContractDefinition>()));
        }

        [Test]
        public void DC_U04_PhysicalCardKeepsDefinitionAndRejectsInvalidId()
        {
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(DemonContractCatalog.MammonKey);
            var card = new DemonContractCard(17, definition);

            Assert.That(card.Id, Is.EqualTo(17));
            Assert.That(card.Definition, Is.SameAs(definition));
            Assert.That(card.DefinitionKey, Is.EqualTo(DemonContractCatalog.MammonKey));
            Assert.That(card.Kind, Is.EqualTo(DemonContractKind.Mammon));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DemonContractCard(-1, definition));
            Assert.Throws<ArgumentNullException>(() =>
                new DemonContractCard(0, null));
        }

        [Test]
        public void DC_U05_DeckRejectsDuplicatePhysicalIds()
        {
            DemonContractDefinition satan =
                DemonContractCatalog.Default.GetByKey(DemonContractCatalog.SatanKey);

            Assert.Throws<ArgumentException>(() => new DemonContractDeck(
                new[]
                {
                    new DemonContractCard(4, satan),
                    new DemonContractCard(4, satan)
                },
                seed: 1));
        }

        [Test]
        public void DC_U06_TakingCandidatesPreservesPhysicalCardOwnership()
        {
            DemonContractDeck deck = CreatePrototypeDeck(seed: 73);

            IReadOnlyList<DemonContractCard> candidates = deck.TakeCandidates();

            Assert.That(candidates.Count, Is.EqualTo(DemonContractDeck.CandidateCount));
            Assert.That(candidates.Select(card => card.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(deck.TotalCardCount, Is.EqualTo(4));
            Assert.That(deck.DrawCount, Is.EqualTo(1));
            Assert.That(deck.DiscardCount, Is.Zero);
            Assert.That(deck.AvailableCardCount, Is.EqualTo(1));
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(3));
        }

        [Test]
        public void DC_U07_DiscardRecycleExcludesActiveContractAndPreservesAllIds()
        {
            DemonContractDeck deck = CreatePrototypeDeck(seed: 101);
            IReadOnlyList<DemonContractCard> firstCandidates = deck.TakeCandidates();
            DemonContractCard activeContract = firstCandidates[0];
            deck.Discard(firstCandidates.Skip(1));

            IReadOnlyList<DemonContractCard> secondCandidates = deck.TakeCandidates();

            Assert.That(
                secondCandidates.Any(card => card.Id == activeContract.Id),
                Is.False);
            Assert.That(secondCandidates.Select(card => card.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(deck.TotalCardCount, Is.EqualTo(4));
            Assert.That(deck.AvailableCardCount, Is.Zero);
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(4));

            deck.Discard(secondCandidates);
            Assert.That(deck.AvailableCardCount, Is.EqualTo(3));
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(1));
        }

        [Test]
        public void DC_U08_InsufficientCandidateRequestIsAtomic()
        {
            DemonContractDefinition satan =
                DemonContractCatalog.Default.GetByKey(DemonContractCatalog.SatanKey);
            var deck = new DemonContractDeck(
                new[]
                {
                    new DemonContractCard(0, satan),
                    new DemonContractCard(1, satan)
                },
                seed: 1);

            Assert.That(deck.CanTakeCandidates, Is.False);
            Assert.Throws<InvalidOperationException>(() => deck.TakeCandidates());
            Assert.That(deck.DrawCount, Is.EqualTo(2));
            Assert.That(deck.DiscardCount, Is.Zero);
            Assert.That(deck.AvailableCardCount, Is.EqualTo(2));
            Assert.That(deck.CardsInPlayCount, Is.Zero);
        }

        [Test]
        public void DC_U09_DeckShuffleIsDeterministicAndLocallyOwned()
        {
            DemonContractDeck first = CreatePrototypeDeck(seed: 991);
            DemonContractDeck second = CreatePrototypeDeck(seed: 991);

            int[] firstOrder = first.TakeCandidates().Select(card => card.Id).ToArray();
            int[] secondOrder = second.TakeCandidates().Select(card => card.Id).ToArray();

            Assert.That(secondOrder, Is.EqualTo(firstOrder));
            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void DC_U10_IndependentBattleUsesAnEmptyNonSharedDemonDeck()
        {
            CoreLoopBattle first = CreateIndependentBattle();
            CoreLoopBattle second = CreateIndependentBattle();

            Assert.That(first.PlayerDemonDeck, Is.Not.Null);
            Assert.That(first.PlayerDemonDeck.TotalCardCount, Is.Zero);
            Assert.That(first.PlayerDemonDeck.CanTakeCandidates, Is.False);
            Assert.That(first.PlayerDemonDeck, Is.Not.SameAs(second.PlayerDemonDeck));
        }

        private static CoreLoopBattle CreateIndependentBattle()
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateStandard(seed: 1),
                BlackjackDeck.CreateStandard(seed: 2));
        }

        private static DemonContractDeck CreatePrototypeDeck(int seed)
        {
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            return new DemonContractDeck(
                new[]
                {
                    new DemonContractCard(0, catalog.GetByKey(DemonContractCatalog.SatanKey)),
                    new DemonContractCard(1, catalog.GetByKey(DemonContractCatalog.BelphegorKey)),
                    new DemonContractCard(2, catalog.GetByKey(DemonContractCatalog.MammonKey)),
                    new DemonContractCard(3, catalog.GetByKey(DemonContractCatalog.LeviathanKey))
                },
                seed);
        }
    }
}
