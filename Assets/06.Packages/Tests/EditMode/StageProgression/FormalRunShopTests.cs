using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class FormalRunShopTests
    {
        [Test]
        public void RF02_U01_OfferHasFixedSlotsDistinctDemonsAndConfiguredPrices()
        {
            var generator = new ShopOfferGenerator(12001);
            ShopOffer offer = generator.Generate(0, 0, false);

            Assert.That(offer.OfferId, Is.Zero);
            Assert.That(offer.VisitIndex, Is.Zero);
            Assert.That(offer.CardOptions.Count, Is.EqualTo(5));
            Assert.That(
                offer.CardOptions.Count(option => option.DeckKind == ShopCardDeckKind.Normal),
                Is.EqualTo(3));
            Assert.That(
                offer.CardOptions.Count(option => option.DeckKind == ShopCardDeckKind.Demon),
                Is.EqualTo(2));
            Assert.That(
                offer.CardOptions.Select(option => option.OptionId).Distinct().Count(),
                Is.EqualTo(5));
            Assert.That(
                offer.CardOptions
                    .Where(option => option.DeckKind == ShopCardDeckKind.Demon)
                    .Select(option => option.DefinitionKey)
                    .Distinct()
                    .Count(),
                Is.EqualTo(2));
            Assert.That(
                offer.CardOptions.All(option => option.Price == 3),
                Is.True);
            Assert.That(offer.LighterPrice, Is.EqualTo(2));
            Assert.That(offer.WhiskeyPrice, Is.EqualTo(2));
            Assert.That(offer.WhiskeyRecovery, Is.EqualTo(2));
        }

        [Test]
        public void RF02_U04_GoldAllowsEveryUnsoldCardSlotAndRejectsRepurchase()
        {
            PlayerRunState player = CreatePlayer(100);
            ShopOffer offer = new ShopOfferGenerator(12002).Generate(0, 0, false);
            var visit = new ShopVisit(offer);
            int initialNormalCount = player.Deck.Count;
            int initialDemonCount = player.DemonDeck.Count;

            foreach (ShopCardOption option in offer.CardOptions)
            {
                Assert.That(
                    visit.TryBuyCard(offer.OfferId, option.OptionId, player),
                    Is.True,
                    option.DefinitionKey);
            }

            Assert.That(player.CurrentGold, Is.EqualTo(85));
            Assert.That(player.Deck.Count, Is.EqualTo(initialNormalCount + 3));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(initialDemonCount + 2));
            Assert.That(visit.PurchasedOptionIds.Count, Is.EqualTo(5));
            Assert.That(
                visit.TryBuyCard(offer.OfferId, offer.CardOptions[0].OptionId, player),
                Is.False);
            Assert.That(player.CurrentGold, Is.EqualTo(85));
        }

        [Test]
        public void RF02_U05_LighterRemovesOneCardOnceAndDoesNotReuseItsId()
        {
            PlayerRunState player = CreatePlayer(20);
            ShopOffer offer = new ShopOfferGenerator(12003).Generate(0, 0, false);
            var visit = new ShopVisit(offer);
            int removedCardId = player.Deck[3].Id;

            Assert.That(visit.TryRemoveCard(offer.OfferId, removedCardId, player), Is.True);
            Assert.That(player.Deck.Any(card => card.Id == removedCardId), Is.False);
            Assert.That(player.CurrentGold, Is.EqualTo(18));
            Assert.That(visit.HasRemovedCard, Is.True);
            Assert.That(visit.TryRemoveCard(offer.OfferId, player.Deck[0].Id, player), Is.False);

            ShopCardOption normalOption = offer.CardOptions.First(
                option => option.DeckKind == ShopCardDeckKind.Normal);
            Assert.That(
                visit.TryBuyCard(offer.OfferId, normalOption.OptionId, player),
                Is.True);
            Assert.That(visit.LastTransaction.AffectedCardId, Is.EqualTo(4));
            Assert.That(visit.LastTransaction.AffectedCardId, Is.Not.EqualTo(removedCardId));
        }

        [Test]
        public void RF02_U05B_LighterCannotRemoveTheLastRunCard()
        {
            PlayerRunState player = CreatePlayer(20);
            for (int removal = 0; removal < 3; removal++)
            {
                ShopOffer offer = new ShopOfferGenerator(12100 + removal)
                    .Generate(0, 0, false);
                var visit = new ShopVisit(offer);
                Assert.That(
                    visit.TryRemoveCard(offer.OfferId, player.Deck[0].Id, player),
                    Is.True);
            }

            ShopOffer finalOffer = new ShopOfferGenerator(12103).Generate(0, 0, false);
            var finalVisit = new ShopVisit(finalOffer);
            int goldBeforeFailure = player.CurrentGold;

            Assert.That(player.Deck.Count, Is.EqualTo(1));
            Assert.That(
                finalVisit.TryRemoveCard(
                    finalOffer.OfferId,
                    player.Deck[0].Id,
                    player),
                Is.False);
            Assert.That(player.Deck.Count, Is.EqualTo(1));
            Assert.That(player.CurrentGold, Is.EqualTo(goldBeforeFailure));
            Assert.That(finalVisit.HasRemovedCard, Is.False);
        }

        [Test]
        public void RF02_U06_WhiskeyRecoversToMaximumAndCannotRepeat()
        {
            PlayerRunState player = CreatePlayer(20, currentSoul: 11);
            ShopOffer offer = new ShopOfferGenerator(12004).Generate(0, 0, false);
            var visit = new ShopVisit(offer);

            Assert.That(visit.TryRest(offer.OfferId, player), Is.True);
            Assert.That(player.CurrentSoul, Is.EqualTo(12));
            Assert.That(player.CurrentGold, Is.EqualTo(18));
            Assert.That(visit.LastTransaction.SoulRecovered, Is.EqualTo(1));
            Assert.That(visit.TryRest(offer.OfferId, player), Is.False);
            Assert.That(player.CurrentGold, Is.EqualTo(18));
        }

        [Test]
        public void RF02_U07_InvalidOrUnaffordableInputsLeaveAllStateUnchanged()
        {
            PlayerRunState player = CreatePlayer(1, currentSoul: 10);
            ShopOffer offer = new ShopOfferGenerator(12005).Generate(0, 0, false);
            var visit = new ShopVisit(offer);
            int normalCount = player.Deck.Count;
            int demonCount = player.DemonDeck.Count;

            Assert.That(visit.TryBuyCard(999, 0, player), Is.False);
            Assert.That(visit.TryBuyCard(offer.OfferId, 999, player), Is.False);
            Assert.That(visit.TryRemoveCard(offer.OfferId, 999, player), Is.False);
            Assert.That(visit.TryRest(offer.OfferId, player), Is.False);

            Assert.That(player.CurrentGold, Is.EqualTo(1));
            Assert.That(player.CurrentSoul, Is.EqualTo(10));
            Assert.That(player.Deck.Count, Is.EqualTo(normalCount));
            Assert.That(player.DemonDeck.Count, Is.EqualTo(demonCount));
            Assert.That(visit.PurchasedOptionIds, Is.Empty);
            Assert.That(visit.HasUsedAnyUtility, Is.False);
            Assert.That(visit.LastTransaction, Is.Null);
        }

        [Test]
        public void RF02_U08_LighterAndWhiskeyAreIndependentOncePerVisit()
        {
            PlayerRunState player = CreatePlayer(20, currentSoul: 8);
            ShopOffer offer = new ShopOfferGenerator(12006).Generate(0, 0, false);
            var visit = new ShopVisit(offer);

            Assert.That(visit.TryRemoveCard(offer.OfferId, player.Deck[0].Id, player), Is.True);
            Assert.That(visit.TryRest(offer.OfferId, player), Is.True);
            Assert.That(visit.HasRemovedCard, Is.True);
            Assert.That(visit.HasRested, Is.True);
            Assert.That(visit.HasUsedAnyUtility, Is.True);
            Assert.That(player.CurrentGold, Is.EqualTo(16));

            Assert.That(visit.TryRemoveCard(offer.OfferId, player.Deck[0].Id, player), Is.False);
            Assert.That(visit.TryRest(offer.OfferId, player), Is.False);
            Assert.That(player.CurrentGold, Is.EqualTo(16));
        }

        [Test]
        public void RF02_U09_GenerationIsDeterministicAndAutomaticCardsAreReachable()
        {
            ShopOffer first = new ShopOfferGenerator(12007).Generate(0, 0, false);
            ShopOffer repeated = new ShopOfferGenerator(12007).Generate(0, 0, false);
            Assert.That(
                repeated.CardOptions.Select(option => option.DefinitionKey),
                Is.EqualTo(first.CardOptions.Select(option => option.DefinitionKey)));

            var foundAutomaticKeys = new HashSet<string>(StringComparer.Ordinal);
            int normalHighGradeCount = 0;
            int eliteHighGradeCount = 0;
            for (int seed = 1; seed <= 500; seed++)
            {
                ShopOffer normal = new ShopOfferGenerator(seed).Generate(0, 0, false);
                ShopOffer elite = new ShopOfferGenerator(seed).Generate(0, 0, true);
                foreach (ShopCardOption option in normal.CardOptions)
                {
                    if (option.DeckKind != ShopCardDeckKind.Normal)
                    {
                        continue;
                    }

                    CardDefinition definition = CardDefinitionCatalog.GetByKey(option.DefinitionKey);
                    if (definition.Activation == CardActivationKind.Automatic)
                    {
                        foundAutomaticKeys.Add(definition.Key);
                    }

                    if (definition.Rank >= 5)
                    {
                        normalHighGradeCount++;
                    }
                }

                eliteHighGradeCount += elite.CardOptions.Count(option =>
                    option.DeckKind == ShopCardDeckKind.Normal &&
                    CardDefinitionCatalog.GetByKey(option.DefinitionKey).Rank >= 5);
            }

            Assert.That(foundAutomaticKeys, Is.EquivalentTo(new[]
            {
                CardDefinitionCatalog.PoisonKey,
                CardDefinitionCatalog.ResurrectionHerbKey,
                CardDefinitionCatalog.LieDetectorKey,
                CardDefinitionCatalog.FlamethrowerKey,
                CardDefinitionCatalog.PocketWatchKey
            }));
            Assert.That(eliteHighGradeCount, Is.GreaterThan(normalHighGradeCount));
        }

        [Test]
        public void RF02_U09B_PurchasedAutomaticCardReachesNextFactoryBattle()
        {
            ShopOffer offer = null;
            ShopCardOption automaticOption = null;
            for (int seed = 1; seed <= 100 && automaticOption == null; seed++)
            {
                offer = new ShopOfferGenerator(seed).Generate(0, 0, false);
                automaticOption = offer.CardOptions.FirstOrDefault(option =>
                    option.DeckKind == ShopCardDeckKind.Normal &&
                    CardDefinitionCatalog.GetByKey(option.DefinitionKey).Activation ==
                    CardActivationKind.Automatic);
            }

            Assert.That(automaticOption, Is.Not.Null);
            PlayerRunState player = CreatePlayer(20);
            int rank = CardDefinitionCatalog.GetByKey(automaticOption.DefinitionKey).Rank;
            int initialRankCount = player.Deck.Count(card => card.Rank == rank);
            var visit = new ShopVisit(offer);

            Assert.That(
                visit.TryBuyCard(offer.OfferId, automaticOption.OptionId, player),
                Is.True);
            Assert.That(
                player.Deck.Any(card => card.DefinitionKey == automaticOption.DefinitionKey),
                Is.True);

            CoreLoopBattle battle = StageBattleFactory.Create(
                new StageDefinition(
                    "shop-next-battle",
                    "Shop Next Battle",
                    StageKind.NormalCombat,
                    1,
                    500,
                    501),
                player);
            Assert.That(
                battle.Player.Deck.GetKnownRankCounts()[rank],
                Is.EqualTo(initialRankCount + 1));
        }

        [Test]
        public void GF04_U02_ShopDemonOffersUsePrototypePool()
        {
            var observedKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int seed = 1; seed <= 500; seed++)
            {
                ShopOffer offer = new ShopOfferGenerator(seed).Generate(
                    0,
                    0,
                    false);
                foreach (ShopCardOption option in offer.CardOptions)
                {
                    if (option.DeckKind == ShopCardDeckKind.Demon)
                    {
                        observedKeys.Add(option.DefinitionKey);
                    }
                }
            }

            Assert.That(
                observedKeys,
                Is.EquivalentTo(
                    DemonContractCatalog.PrototypeEnabledDemonKeys));
        }

        [Test]
        public void GSV07_U01_ShopExcludesOwnedDemonsAndKeepsDemonOffersDistinct()
        {
            IReadOnlyList<string> prototypeKeys =
                DemonContractCatalog.PrototypeEnabledDemonKeys;
            string[] ownedKeys = prototypeKeys.Take(3).ToArray();

            ShopOffer offer = new ShopOfferGenerator(20260802).Generate(
                0,
                0,
                false,
                ownedKeys);
            string[] offeredKeys = offer.CardOptions
                .Where(option => option.DeckKind == ShopCardDeckKind.Demon)
                .Select(option => option.DefinitionKey)
                .ToArray();

            Assert.That(offeredKeys, Has.Length.EqualTo(2));
            Assert.That(offeredKeys.Distinct().Count(), Is.EqualTo(2));
            Assert.That(offeredKeys.Intersect(ownedKeys), Is.Empty);
        }

        [Test]
        public void GSV07_U02_NormalShopCardsMayRepeat()
        {
            bool foundRepeatedNormalCard = false;
            for (int seed = 1; seed <= 100 && !foundRepeatedNormalCard; seed++)
            {
                ShopOffer offer = new ShopOfferGenerator(seed).Generate(0, 0, false);
                string[] normalKeys = offer.CardOptions
                    .Where(option => option.DeckKind == ShopCardDeckKind.Normal)
                    .Select(option => option.DefinitionKey)
                    .ToArray();
                foundRepeatedNormalCard = normalKeys.Distinct().Count() < normalKeys.Length;
            }

            Assert.That(foundRepeatedNormalCard, Is.True);
        }

        [Test]
        public void RF02_U10_UsedVisitRaisesBothNextPricesByOneLevelOnly()
        {
            var generator = new ShopOfferGenerator(12008);
            PlayerRunState player = CreatePlayer(20, currentSoul: 8);
            ShopOffer first = generator.Generate(0, 0, false);
            var visit = new ShopVisit(first);

            Assert.That(visit.TryRemoveCard(first.OfferId, player.Deck[0].Id, player), Is.True);
            Assert.That(visit.TryRest(first.OfferId, player), Is.True);
            Assert.That(visit.TryClose(first.OfferId), Is.True);
            int nextLevel = visit.HasUsedAnyUtility ? first.UtilityPriceLevel + 1 : first.UtilityPriceLevel;
            ShopOffer second = generator.Generate(1, nextLevel, false);

            Assert.That(second.UtilityPriceLevel, Is.EqualTo(1));
            Assert.That(second.LighterPrice, Is.EqualTo(3));
            Assert.That(second.WhiskeyPrice, Is.EqualTo(3));

            ShopOffer unused = new ShopOfferGenerator(12009).Generate(0, 0, false);
            var unusedVisit = new ShopVisit(unused);
            Assert.That(unusedVisit.TryClose(unused.OfferId), Is.True);
            Assert.That(unusedVisit.HasUsedAnyUtility, Is.False);
        }

        [Test]
        public void RF02_U11_NewRunRestoresGoldDeckAndOfferSequence()
        {
            const int seed = 12010;
            PlayerRunState player = CreatePlayer(0);
            player.AddGold(20);
            ShopOffer first = new ShopOfferGenerator(seed).Generate(0, 0, false);
            var visit = new ShopVisit(first);
            ShopCardOption option = first.CardOptions.First(
                candidate => candidate.DeckKind == ShopCardDeckKind.Normal);

            Assert.That(visit.TryBuyCard(first.OfferId, option.OptionId, player), Is.True);
            Assert.That(visit.TryRemoveCard(first.OfferId, player.Deck[0].Id, player), Is.True);
            player.ResetForNewRun();
            ShopOffer restarted = new ShopOfferGenerator(seed).Generate(0, 0, false);

            Assert.That(player.CurrentGold, Is.Zero);
            Assert.That(player.CurrentSoul, Is.EqualTo(player.MaximumSoul));
            Assert.That(player.Deck.Select(card => card.Id), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(restarted.OfferId, Is.Zero);
            Assert.That(restarted.UtilityPriceLevel, Is.Zero);
            Assert.That(
                restarted.CardOptions.Select(candidate => candidate.DefinitionKey),
                Is.EqualTo(first.CardOptions.Select(candidate => candidate.DefinitionKey)));
        }

        private static PlayerRunState CreatePlayer(int gold, int currentSoul = 12)
        {
            return new PlayerRunState(
                12,
                currentSoul,
                new[]
                {
                    new RunCardDefinition(0, 10),
                    new RunCardDefinition(1, 8),
                    new RunCardDefinition(2, 5),
                    new RunCardDefinition(3, 1)
                },
                gold);
        }
    }
}
