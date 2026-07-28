using System.Collections.Generic;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class AutomaticCardRevisionTests
    {
        [TestCase("poison-1", 1, CardEffectKind.Poison)]
        [TestCase("resurrection-herb-2", 2, CardEffectKind.ResurrectionHerb)]
        [TestCase("lie-detector-3", 3, CardEffectKind.LieDetector)]
        [TestCase("flamethrower-4", 4, CardEffectKind.Flamethrower)]
        [TestCase("pocket-watch-5", 5, CardEffectKind.PocketWatch)]
        public void ACRV01_U01_AutomaticDefinitionsUseConsecutiveRanks(
            string expectedKey,
            int expectedRank,
            CardEffectKind expectedEffect)
        {
            CardDefinition definition = CardDefinitionCatalog.GetByKey(expectedKey);

            Assert.That(definition.Key, Is.EqualTo(expectedKey));
            Assert.That(definition.Rank, Is.EqualTo(expectedRank));
            Assert.That(definition.Activation, Is.EqualTo(CardActivationKind.Automatic));
            Assert.That(definition.Effect, Is.EqualTo(expectedEffect));
        }

        [Test]
        public void ACRV01_U02_LegacyAutomaticDefinitionKeysAreRejected()
        {
            string[] legacyKeys =
            {
                "poison-2",
                "flamethrower-9",
                "pocket-watch-9"
            };

            foreach (string legacyKey in legacyKeys)
            {
                Assert.Throws<KeyNotFoundException>(
                    () => CardDefinitionCatalog.GetByKey(legacyKey));
            }
        }

        [Test]
        public void ACRV01_U03_DefaultRankDefinitionsRemainUnchanged()
        {
            Assert.That(
                CardDefinitionCatalog.GetDefaultForRank(1).Key,
                Is.EqualTo("standard-ace-1"));
            Assert.That(
                CardDefinitionCatalog.GetDefaultForRank(2).Key,
                Is.EqualTo("standard-plain-2"));
            Assert.That(
                CardDefinitionCatalog.GetDefaultForRank(3).Key,
                Is.EqualTo("standard-plain-3"));
            Assert.That(
                CardDefinitionCatalog.GetDefaultForRank(4).Key,
                Is.EqualTo("standard-plain-4"));
            Assert.That(
                CardDefinitionCatalog.GetDefaultForRank(5).Key,
                Is.EqualTo("crystal-orb-5"));
        }
    }
}
