using System.Collections.Generic;
using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class ShopControllerTests
    {
        private GameObject _root;
        private ShopController _shop;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Shop Controller Test Root");
            _shop = _root.AddComponent<ShopController>();
            SetPrivateField("goldPerWin", 20);
            SetPrivateField("lighterPrice", 2);
            SetPrivateField("whiskeyPrice", 3);
            SetPrivateField("utilityPriceIncreasePerUsedVisit", 1);
            SetPrivateField("whiskeySoulRestore", 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void RFM01_U01_UtilityPricesIncreaseOncePerUsedShop()
        {
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out int firstRestore),
                Is.True);
            Assert.That(firstRestore, Is.EqualTo(2));
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out _),
                Is.False);
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));

            _shop.Close();
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(3));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(4));
            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.True);
            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.False);
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out int secondRestore),
                Is.True);
            Assert.That(secondRestore, Is.EqualTo(2));

            _shop.Close();
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(4));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(5));
        }

        [Test]
        public void RFM01_U02_UnusedShopDoesNotIncreaseUtilityPricesAndRunResetClearsThem()
        {
            _shop.Open();
            _shop.Close();
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out _),
                Is.True);

            _shop.Close();
            _shop.ResetRunEconomy();

            Assert.That(_shop.Gold, Is.Zero);
            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
        }

        [Test]
        public void RFM01_U03_AllCardSlotsCanBePurchasedWithoutAVisitLimit()
        {
            GameObject holderObject = new GameObject("Normal Card Holder");
            holderObject.transform.SetParent(_root.transform);
            GameObject prefabObject = new GameObject("Normal Card Prefab");
            prefabObject.transform.SetParent(_root.transform);
            CardView prefab = prefabObject.AddComponent<CardView>();
            SetPrivateField("normalCardHolder", holderObject.transform);
            SetPrivateField("normalCardPrefab", prefab);
            SetPrivateField("normalCardOfferCount", 3);

            _shop.Open();
            CardView[] offers = holderObject.GetComponentsInChildren<CardView>(true);

            Assert.That(offers, Has.Length.EqualTo(3));
            foreach (CardView offer in offers)
            {
                Assert.That(
                    _shop.TryPurchaseNormalCard(
                        offer.CardId,
                        out string definitionKey,
                        out _),
                    Is.True);
                Assert.That(definitionKey, Is.Not.Empty);
            }

            Assert.That(_shop.Gold, Is.EqualTo(11));
            Assert.That(
                _shop.TryPurchaseNormalCard(offers[0].CardId, out _, out _),
                Is.False);
            Assert.That(_shop.Gold, Is.EqualTo(11));
        }

        [Test]
        public void RFM01_U04_CurrentBattleDeckRemovalUpdatesVisibleComposition()
        {
            string rankOneKey = CardDefinitionCatalog.GetDefaultForRank(1).Key;
            var drawDeck = BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(
                    0,
                    CardDefinitionCatalog.GetDefaultForRank(1),
                    suit: CardSuit.Spade),
                new BlackjackCard(
                    1,
                    CardDefinitionCatalog.GetDefaultForRank(1),
                    suit: CardSuit.Clover),
                new BlackjackCard(
                    2,
                    CardDefinitionCatalog.GetDefaultForRank(2),
                    suit: CardSuit.Spade),
            });

            Assert.That(
                drawDeck.TryRemoveAvailableCard(rankOneKey, CardSuit.Spade),
                Is.True);
            Assert.That(drawDeck.ContainsKnownCardId(0), Is.False);
            Assert.That(drawDeck.GetDrawPileRankCounts()[1], Is.EqualTo(1));
            Assert.That(drawDeck.DrawCount, Is.EqualTo(2));

            var discardDeck = BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(
                    10,
                    CardDefinitionCatalog.GetDefaultForRank(3),
                    suit: CardSuit.Spade),
                new BlackjackCard(
                    11,
                    CardDefinitionCatalog.GetDefaultForRank(4),
                    suit: CardSuit.Spade),
            });
            BlackjackCard discarded = discardDeck.Draw();
            discardDeck.Discard(discarded);

            Assert.That(
                discardDeck.TryRemoveAvailableCard(
                    discarded.DefinitionKey,
                    discarded.Suit),
                Is.True);
            Assert.That(discardDeck.ContainsKnownCardId(discarded.Id), Is.False);
            Assert.That(discardDeck.DiscardCount, Is.Zero);
        }

        [Test]
        public void RFM01_U05_GameSceneDemonDeckUsesDefaultFourAndCarriesPurchases()
        {
            GameObject managerObject = new GameObject("Game Manager Test Root");
            GameManager manager = managerObject.AddComponent<GameManager>();
            try
            {
                DemonContractDeck baseDeck =
                    InvokeCreatePlayerDemonDeck(manager, 17);
                Assert.That(baseDeck.TotalCardCount, Is.EqualTo(4));
                Assert.That(
                    DrainDemonDeckKeys(baseDeck),
                    Is.EquivalentTo(DemonContractCatalog.PlayerDefaultDemonDeckKeys));

                CoreLoopSession session = new CoreLoopSession(
                    () => new CoreLoopBattle(
                        BlackjackDeck.CreateStandard(1),
                        BlackjackDeck.CreateStandard(2),
                        playerDemonDeck:
                            InvokeCreatePlayerDemonDeck(manager, 19)));
                SetPrivateField(manager, "_session", session);

                InvokeAddPurchasedDemonContractToCurrentBattle(
                    manager,
                    DemonContractCatalog.LuciferKey);
                Assert.That(manager.Battle.PlayerDemonDeck.TotalCardCount,
                    Is.EqualTo(5));

                IList<string> purchasedKeys =
                    GetPurchasedDemonContractKeys(manager);
                purchasedKeys.Add(DemonContractCatalog.LuciferKey);
                DemonContractDeck nextDeck =
                    InvokeCreatePlayerDemonDeck(manager, 23);
                Assert.That(nextDeck.TotalCardCount, Is.EqualTo(5));
                Assert.That(
                    DrainDemonDeckKeys(nextDeck),
                    Has.Member(DemonContractCatalog.LuciferKey));
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void RFM01_U06_ShopDemonOffersUseFullCatalogPool()
        {
            GameObject holderObject = new GameObject("Demon Card Holder");
            holderObject.transform.SetParent(_root.transform);
            GameObject prefabObject = new GameObject("Demon Card Prefab");
            prefabObject.transform.SetParent(_root.transform);
            DemonCardView prefab = prefabObject.AddComponent<DemonCardView>();
            SetPrivateField("demonCardHolder", holderObject.transform);
            SetPrivateField("demonCardPrefab", prefab);
            SetPrivateField(
                "demonCardOfferCount",
                DemonContractCatalog.Default.Definitions.Count);
            SetPrivateField("goldPerWin", 40);

            _shop.Open();
            DemonCardView[] offers =
                holderObject.GetComponentsInChildren<DemonCardView>(true);
            var purchasedKeys = new List<string>();
            foreach (DemonCardView offer in offers)
            {
                Assert.That(
                    _shop.TryPurchaseDemonCard(
                        offer.CardId,
                        out string definitionKey),
                    Is.True);
                purchasedKeys.Add(definitionKey);
            }

            Assert.That(
                purchasedKeys.Count,
                Is.EqualTo(DemonContractCatalog.Default.Definitions.Count));
            Assert.That(
                purchasedKeys,
                Has.Member(DemonContractCatalog.LuciferKey));
            Assert.That(
                purchasedKeys,
                Has.Member(DemonContractCatalog.LeviathanKey));
        }

        private void SetPrivateField(string fieldName, object value)
        {
            SetPrivateField(_shop, fieldName, value);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static DemonContractDeck InvokeCreatePlayerDemonDeck(
            GameManager manager,
            int seed)
        {
            MethodInfo method = typeof(GameManager).GetMethod(
                "CreatePlayerDemonDeck",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (DemonContractDeck)method.Invoke(
                manager,
                new object[] { seed });
        }

        private static void InvokeAddPurchasedDemonContractToCurrentBattle(
            GameManager manager,
            string definitionKey)
        {
            MethodInfo method = typeof(GameManager).GetMethod(
                "AddPurchasedDemonContractToCurrentBattle",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(manager, new object[] { definitionKey });
        }

        private static IList<string> GetPurchasedDemonContractKeys(
            GameManager manager)
        {
            FieldInfo field = typeof(GameManager).GetField(
                "_purchasedDemonContractKeys",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (IList<string>)field.GetValue(manager);
        }

        private static string[] DrainDemonDeckKeys(DemonContractDeck deck)
        {
            var keys = new List<string>();
            while (deck.CanTakeCandidates)
            {
                IReadOnlyList<DemonContractCard> candidates =
                    deck.TakeCandidates();
                foreach (DemonContractCard candidate in candidates)
                {
                    keys.Add(candidate.DefinitionKey);
                }
            }

            return keys.ToArray();
        }
    }
}
