using System;
using System.Collections.Generic;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CardContentCatalogTests
    {
        [Test]
        public void CC_U01_RuntimeCatalogPreservesDescriptionPriceWeightAndDefaultRank()
        {
            CardContentCatalog catalog = CreateValidCatalog();

            CardDefinition normal = catalog.GetNormalByKey("normal-7");
            DemonContractDefinition demon = catalog.GetDemonByKey("satan");

            Assert.That(normal.Description, Is.EqualTo("효과 설명"));
            Assert.That(normal.BasePurchasePrice, Is.EqualTo(5));
            Assert.That(normal.ShopWeight, Is.EqualTo(2));
            Assert.That(catalog.GetStandardDeckDefault(7), Is.SameAs(normal));
            Assert.That(demon.Summary, Is.EqualTo("악마 효과"));
            Assert.That(demon.CostSummary, Is.EqualTo("영혼 대가"));
            Assert.That(demon.BasePurchasePrice, Is.EqualTo(8));
            Assert.That(demon.ShopWeight, Is.EqualTo(3));
        }

        [Test]
        public void CC_U02_RuntimeCatalogRejectsDuplicateNormalKey()
        {
            List<CardDefinition> normal = CreateNormalDefaults();
            normal.Add(new CardDefinition(
                "normal-1",
                "중복",
                1,
                CardActivationKind.None,
                CardEffectKind.None,
                "중복"));

            Assert.Throws<ArgumentException>(() => new CardContentCatalog(
                normal,
                new[] { CreateDemonDefinition() }));
        }

        [Test]
        public void CC_U03_RuntimeCatalogRejectsMissingStandardDeckRank()
        {
            List<CardDefinition> normal = CreateNormalDefaults();
            normal.RemoveAt(9);

            Assert.Throws<ArgumentException>(() => new CardContentCatalog(
                normal,
                new[] { CreateDemonDefinition() }));
        }

        [Test]
        public void CC_U04_ShopOfferUsesInjectedCatalogPurchasePrices()
        {
            var normal = new List<CardDefinition>();
            foreach (CardDefinition definition in CardDefinitionCatalog.All)
            {
                normal.Add(new CardDefinition(
                    definition.Key,
                    definition.DisplayName,
                    definition.Rank,
                    definition.Activation,
                    definition.Effect,
                    definition.Description,
                    basePurchasePrice: 7,
                    shopWeight: definition.ShopWeight,
                    isStandardDeckDefault: definition.IsStandardDeckDefault));
            }

            var demon = new List<DemonContractDefinition>();
            foreach (DemonContractDefinition definition in
                DemonContractCatalog.Default.Definitions)
            {
                demon.Add(new DemonContractDefinition(
                    definition.Key,
                    definition.DisplayName,
                    definition.Kind,
                    definition.BaseSoulCost,
                    definition.Summary,
                    definition.CostSummary,
                    basePurchasePrice: 9,
                    shopWeight: definition.ShopWeight));
            }

            ShopOffer offer = new ShopOfferGenerator(
                new CardContentCatalog(normal, demon),
                20260730).Generate(0, 0, false);

            foreach (ShopCardOption option in offer.CardOptions)
            {
                int expectedPrice = option.DeckKind == ShopCardDeckKind.Normal ? 7 : 9;
                Assert.That(option.Price, Is.EqualTo(expectedPrice), option.DefinitionKey);
            }
        }

        private static CardContentCatalog CreateValidCatalog()
        {
            List<CardDefinition> normal = CreateNormalDefaults();
            normal[6] = new CardDefinition(
                "normal-7",
                "카드 7",
                7,
                CardActivationKind.Manual,
                CardEffectKind.AutoPistol,
                "효과 설명",
                basePurchasePrice: 5,
                shopWeight: 2,
                isStandardDeckDefault: true);

            return new CardContentCatalog(normal, new[] { CreateDemonDefinition() });
        }

        private static List<CardDefinition> CreateNormalDefaults()
        {
            var definitions = new List<CardDefinition>();
            for (int rank = 1; rank <= 10; rank++)
            {
                definitions.Add(new CardDefinition(
                    "normal-" + rank,
                    "카드 " + rank,
                    rank,
                    CardActivationKind.None,
                    CardEffectKind.None,
                    "기본 카드",
                    isStandardDeckDefault: true));
            }

            return definitions;
        }

        private static DemonContractDefinition CreateDemonDefinition()
        {
            return new DemonContractDefinition(
                "satan",
                "사탄",
                DemonContractKind.Satan,
                1,
                "악마 효과",
                "영혼 대가",
                basePurchasePrice: 8,
                shopWeight: 3);
        }
    }
}
