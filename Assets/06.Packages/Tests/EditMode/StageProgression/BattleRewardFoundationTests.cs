using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class BattleRewardFoundationTests
    {
        [Test]
        public void RW01_U01_DefaultCatalogContainsOnlyRegisteredDefinitions()
        {
            BattleRewardCatalog catalog = BattleRewardCatalog.CreateDefault();

            Assert.That(
                catalog.GetDefinitionKeys(BattleRewardTier.Normal).Count,
                Is.EqualTo(10));
            Assert.That(
                catalog.GetDefinitionKeys(BattleRewardTier.HighGrade).Count,
                Is.EqualTo(6));

            foreach (BattleRewardTier tier in new[]
                     {
                         BattleRewardTier.Normal,
                         BattleRewardTier.HighGrade
                     })
            {
                foreach (string definitionKey in catalog.GetDefinitionKeys(tier))
                {
                    Assert.That(
                        CardDefinitionCatalog.GetByKey(definitionKey).Key,
                        Is.EqualTo(definitionKey));
                }
            }
        }

        [Test]
        public void RW01_U02_NormalRewardContainsThreeUniqueDefinitions()
        {
            BattleRewardCatalog catalog = BattleRewardCatalog.CreateDefault();
            var generator = new BattleRewardGenerator(catalog, 4102);

            BattleRewardOffer offer = generator.Generate(BattleRewardTier.Normal);

            Assert.That(offer.Tier, Is.EqualTo(BattleRewardTier.Normal));
            Assert.That(offer.Options.Count, Is.EqualTo(3));
            Assert.That(
                offer.Options.Select(option => option.DefinitionKey).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(
                offer.Options.All(option =>
                    catalog.Contains(BattleRewardTier.Normal, option.DefinitionKey)),
                Is.True);
        }

        [Test]
        public void RW01_U03_HighGradeRewardUsesOnlyHighGradePool()
        {
            BattleRewardCatalog catalog = BattleRewardCatalog.CreateDefault();
            var generator = new BattleRewardGenerator(catalog, 7811);

            BattleRewardOffer offer = generator.Generate(BattleRewardTier.HighGrade);

            Assert.That(offer.Tier, Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(offer.Options.Count, Is.EqualTo(3));
            Assert.That(
                offer.Options.Select(option => option.DefinitionKey).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(
                offer.Options.All(option =>
                    catalog.Contains(BattleRewardTier.HighGrade, option.DefinitionKey)),
                Is.True);
        }

        [Test]
        public void RW01_U04_SameSeedAndRequestOrderProduceSameOffers()
        {
            BattleRewardCatalog catalog = BattleRewardCatalog.CreateDefault();
            var first = new BattleRewardGenerator(catalog, 20260720);
            var second = new BattleRewardGenerator(catalog, 20260720);

            BattleRewardOffer firstNormal = first.Generate(BattleRewardTier.Normal);
            BattleRewardOffer secondNormal = second.Generate(BattleRewardTier.Normal);
            BattleRewardOffer firstHighGrade = first.Generate(BattleRewardTier.HighGrade);
            BattleRewardOffer secondHighGrade = second.Generate(BattleRewardTier.HighGrade);

            AssertOffersEqual(firstNormal, secondNormal);
            AssertOffersEqual(firstHighGrade, secondHighGrade);
            Assert.That(firstNormal.OfferId, Is.Zero);
            Assert.That(firstHighGrade.OfferId, Is.EqualTo(1));
        }

        [Test]
        public void RW01_U05_CatalogRejectsTooSmallDuplicateAndUnknownPools()
        {
            string[] validNormal = BattleRewardCatalog.CreateDefault()
                .GetDefinitionKeys(BattleRewardTier.Normal)
                .ToArray();
            string[] validHighGrade = BattleRewardCatalog.CreateDefault()
                .GetDefinitionKeys(BattleRewardTier.HighGrade)
                .ToArray();

            Assert.Throws<ArgumentException>(() => new BattleRewardCatalog(
                new[] { "standard-ace-1", "standard-plain-2" },
                validHighGrade));
            Assert.Throws<ArgumentException>(() => new BattleRewardCatalog(
                validNormal,
                new[] { "crystal-orb-5", "crystal-orb-5", "auto-pistol-7" }));
            Assert.Throws<KeyNotFoundException>(() => new BattleRewardCatalog(
                validNormal,
                new[] { "crystal-orb-5", "auto-pistol-7", "missing-card" }));
        }

        [Test]
        public void RW01_U06_AddRewardCardUsesNextUniqueRunCardId()
        {
            PlayerRunState player = CreatePlayerWithSparseIds();

            RunCardDefinition added = player.AddRewardCard("crystal-orb-5");

            Assert.That(added.Id, Is.EqualTo(10));
            Assert.That(added.DefinitionKey, Is.EqualTo("crystal-orb-5"));
            Assert.That(player.Deck.Count, Is.EqualTo(3));
            Assert.That(player.Deck[2], Is.SameAs(added));
        }

        [Test]
        public void RW01_U07_DuplicateDefinitionsReceiveDifferentPhysicalIds()
        {
            var player = new PlayerRunState(
                12,
                12,
                new[] { new RunCardDefinition(1, "crystal-orb-5") });

            RunCardDefinition first = player.AddRewardCard("crystal-orb-5");
            RunCardDefinition second = player.AddRewardCard("crystal-orb-5");

            Assert.That(first.DefinitionKey, Is.EqualTo("crystal-orb-5"));
            Assert.That(second.DefinitionKey, Is.EqualTo("crystal-orb-5"));
            Assert.That(first.Id, Is.EqualTo(2));
            Assert.That(second.Id, Is.EqualTo(3));
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        }

        [Test]
        public void RW01_U08_ResetRestoresInitialDeckSoulAndNextId()
        {
            PlayerRunState player = CreatePlayerWithSparseIds();
            player.SetCurrentSoul(3);
            player.AddRewardCard("crystal-orb-5");
            player.AddRewardCard("auto-pistol-7");

            player.ResetForNewRun();

            Assert.That(player.CurrentSoul, Is.EqualTo(12));
            Assert.That(player.Deck.Count, Is.EqualTo(2));
            Assert.That(player.Deck.Select(card => card.Id), Is.EqualTo(new[] { 7, 9 }));
            Assert.That(
                player.Deck.Select(card => card.DefinitionKey),
                Is.EqualTo(new[] { "standard-plain-3", "military-knife-10" }));

            RunCardDefinition addedAfterReset = player.AddRewardCard("threat-hammer-6");
            Assert.That(addedAfterReset.Id, Is.EqualTo(10));
        }

        private static PlayerRunState CreatePlayerWithSparseIds()
        {
            return new PlayerRunState(
                12,
                8,
                new[]
                {
                    new RunCardDefinition(7, "standard-plain-3"),
                    new RunCardDefinition(9, "military-knife-10")
                });
        }

        private static void AssertOffersEqual(
            BattleRewardOffer expected,
            BattleRewardOffer actual)
        {
            Assert.That(actual.OfferId, Is.EqualTo(expected.OfferId));
            Assert.That(actual.Tier, Is.EqualTo(expected.Tier));
            Assert.That(
                actual.Options.Select(option => option.OptionId),
                Is.EqualTo(expected.Options.Select(option => option.OptionId)));
            Assert.That(
                actual.Options.Select(option => option.DefinitionKey),
                Is.EqualTo(expected.Options.Select(option => option.DefinitionKey)));
        }
    }
}
