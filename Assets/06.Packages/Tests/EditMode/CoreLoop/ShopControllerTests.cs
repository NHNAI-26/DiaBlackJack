using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
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

        [Test]
        public void GSV06_U01_PurchasedCardsKeepSlotsAndShowSoldOutStatus()
        {
            Transform normalHolder = CreateHolder("Normal Card Holder");
            Transform demonHolder = CreateHolder("Demon Card Holder");
            CardView normalPrefab = CreateNormalCardPrefab();
            DemonCardView demonPrefab = CreateDemonCardPrefab();
            ShopCardOfferStatusView statusPrefab = CreateStatusPrefab();
            SetPrivateField("normalCardHolder", normalHolder);
            SetPrivateField("demonCardHolder", demonHolder);
            SetPrivateField("normalCardPrefab", normalPrefab);
            SetPrivateField("demonCardPrefab", demonPrefab);
            SetPrivateField("cardOfferStatusPrefab", statusPrefab);
            SetPrivateField("normalCardOfferCount", 3);
            SetPrivateField("demonCardOfferCount", 2);

            _shop.Open();
            CardView[] normalCards =
                normalHolder.GetComponentsInChildren<CardView>(true);
            DemonCardView[] demonCards =
                demonHolder.GetComponentsInChildren<DemonCardView>(true);
            ShopCardOfferStatusView[] normalStatuses =
                normalHolder.GetComponentsInChildren<ShopCardOfferStatusView>(true);
            ShopCardOfferStatusView[] demonStatuses =
                demonHolder.GetComponentsInChildren<ShopCardOfferStatusView>(true);
            Vector3[] normalPositions = GetLocalPositions(normalCards);
            Vector3[] demonPositions = GetLocalPositions(demonCards);

            Assert.That(normalCards, Has.Length.EqualTo(3));
            Assert.That(demonCards, Has.Length.EqualTo(2));
            Assert.That(normalStatuses, Has.Length.EqualTo(3));
            Assert.That(demonStatuses, Has.Length.EqualTo(2));
            Assert.That(normalStatuses[0].PriceLabel, Does.StartWith("돈 : "));
            Assert.That(demonStatuses[0].PriceLabel, Does.StartWith("돈 : "));
            Assert.That(
                normalCards.All(card =>
                    !string.IsNullOrWhiteSpace(card.DefinitionKey)),
                Is.True);

            Assert.That(
                _shop.TryPurchaseNormalCard(normalCards[0].CardId, out _, out _),
                Is.True);
            Assert.That(
                _shop.TryPurchaseDemonCard(demonCards[0].CardId, out _),
                Is.True);

            Assert.That(
                normalHolder.GetComponentsInChildren<CardView>(true),
                Has.Length.EqualTo(3));
            Assert.That(
                demonHolder.GetComponentsInChildren<DemonCardView>(true),
                Has.Length.EqualTo(2));
            Assert.That(normalCards[0].gameObject.activeSelf, Is.True);
            Assert.That(demonCards[0].gameObject.activeSelf, Is.True);
            Assert.That(normalCards[0].IsShopSoldOut, Is.True);
            Assert.That(demonCards[0].IsShopSoldOut, Is.True);
            Assert.That(normalCards[0].CanUse, Is.False);
            Assert.That(demonCards[0].CanUse, Is.False);
            Assert.That(
                normalCards[0].GetComponentInChildren<SpriteRenderer>().color.r,
                Is.LessThan(0.5f));
            Assert.That(
                demonCards[0].GetComponentInChildren<SpriteRenderer>().color.r,
                Is.LessThan(0.5f));
            Assert.That(FindStatusAt(normalStatuses, normalPositions[0]).IsSoldOut,
                Is.True);
            Assert.That(FindStatusAt(demonStatuses, demonPositions[0]).IsSoldOut,
                Is.True);
            Assert.That(
                FindStatusAt(normalStatuses, normalPositions[0]).PriceColor,
                Is.EqualTo(new Color(0.42f, 0.42f, 0.42f, 1f)));
            AssertLocalPositions(normalCards, normalPositions);
            AssertLocalPositions(demonCards, demonPositions);

            int goldAfterPurchase = _shop.Gold;
            Assert.That(
                _shop.TryPurchaseNormalCard(normalCards[0].CardId, out _, out _),
                Is.False);
            Assert.That(_shop.Gold, Is.EqualTo(goldAfterPurchase));
        }

        [Test]
        public void GSV06_U02_InsufficientGoldDoesNotLookSoldOrChangeSlot()
        {
            Transform holder = CreateHolder("Normal Card Holder");
            CardView prefab = CreateNormalCardPrefab();
            ShopCardOfferStatusView statusPrefab = CreateStatusPrefab();
            SetPrivateField("normalCardHolder", holder);
            SetPrivateField("normalCardPrefab", prefab);
            SetPrivateField("cardOfferStatusPrefab", statusPrefab);
            SetPrivateField("normalCardOfferCount", 1);

            _shop.Open();
            _shop.ResetGold();
            CardView card = holder.GetComponentInChildren<CardView>(true);
            ShopCardOfferStatusView status =
                holder.GetComponentInChildren<ShopCardOfferStatusView>(true);
            Vector3 position = card.transform.localPosition;

            Assert.That(card.CanUse, Is.False);
            Assert.That(card.IsShopSoldOut, Is.False);
            Assert.That(
                card.GetComponentInChildren<SpriteRenderer>().color,
                Is.EqualTo(Color.white));
            Assert.That(status.IsSoldOut, Is.False);
            Assert.That(status.PriceLabel, Does.StartWith("돈 : "));
            Assert.That(
                status.PriceColor,
                Is.EqualTo(new Color(0.95f, 0.82f, 0.55f, 1f)));
            Assert.That(
                _shop.TryPurchaseNormalCard(card.CardId, out _, out _),
                Is.False);
            Assert.That(_shop.Gold, Is.Zero);
            Assert.That(card.transform.localPosition, Is.EqualTo(position));
            Assert.That(card.IsShopSoldOut, Is.False);
            Assert.That(status.IsSoldOut, Is.False);
        }

        [Test]
        public void GSV06_U04_StatusPrefabUsesKoreanFontAndDoesNotBlockInput()
        {
            const string prefabPath =
                "Assets/03. Prefabs/Shop/Resources/ShopCardOfferStatus.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                ShopCardOfferStatusView status =
                    instance.GetComponent<ShopCardOfferStatusView>();
                Assert.That(status, Is.Not.Null);
                status.Bind(3, isSoldOut: true);

                Assert.That(status.PriceLabel, Is.EqualTo("돈 : 3"));
                Assert.That(status.IsSoldOut, Is.True);
                Assert.That(
                    instance.GetComponentsInChildren<Collider>(true),
                    Is.Empty);
                Assert.That(
                    instance.transform.Find("Price").localScale.x,
                    Is.EqualTo(0.22f).Within(0.001f));

                int textCount = 0;
                foreach (Component component in
                         instance.GetComponentsInChildren<Component>(true))
                {
                    if (component.GetType().FullName != "TMPro.TextMeshPro")
                    {
                        continue;
                    }

                    textCount++;
                    object font = component.GetType().GetProperty("font")
                        .GetValue(component, null);
                    bool raycastTarget = (bool)component.GetType()
                        .GetProperty("raycastTarget")
                        .GetValue(component, null);
                    Assert.That(((Object)font).name,
                        Is.EqualTo("BMHANNAAir_ttf SDF"));
                    Assert.That(raycastTarget, Is.False);
                }

                Assert.That(textCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private Transform CreateHolder(string name)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(_root.transform);
            return holder.transform;
        }

        private CardView CreateNormalCardPrefab()
        {
            GameObject prefabObject = new GameObject("Normal Card Prefab");
            prefabObject.transform.SetParent(_root.transform);
            CardView prefab = prefabObject.AddComponent<CardView>();
            GameObject front = new GameObject("Front");
            front.transform.SetParent(prefabObject.transform);
            front.AddComponent<SpriteRenderer>();
            SetPrivateField(prefab, "front", front);
            return prefab;
        }

        private DemonCardView CreateDemonCardPrefab()
        {
            GameObject prefabObject = new GameObject("Demon Card Prefab");
            prefabObject.transform.SetParent(_root.transform);
            DemonCardView prefab = prefabObject.AddComponent<DemonCardView>();
            GameObject front = new GameObject("Front");
            front.transform.SetParent(prefabObject.transform);
            front.AddComponent<SpriteRenderer>();
            SetPrivateField(prefab, "front", front);
            return prefab;
        }

        private ShopCardOfferStatusView CreateStatusPrefab()
        {
            GameObject prefabObject = new GameObject("Status Prefab");
            prefabObject.transform.SetParent(_root.transform);
            ShopCardOfferStatusView prefab =
                prefabObject.AddComponent<ShopCardOfferStatusView>();
            Component priceText = CreateText("Price", prefabObject.transform);
            Component soldOutText = CreateText("Sold Out", prefabObject.transform);
            SetPrivateField(prefab, "priceText", priceText);
            SetPrivateField(prefab, "soldOutText", soldOutText);
            return prefab;
        }

        private static Component CreateText(string name, Transform parent)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
            System.Type textType = System.Type.GetType(
                "TMPro.TextMeshPro, Unity.TextMeshPro");
            Assert.That(textType, Is.Not.Null);
            return textObject.AddComponent(textType);
        }

        private static Vector3[] GetLocalPositions<T>(T[] views)
            where T : Component
        {
            var positions = new Vector3[views.Length];
            for (int i = 0; i < views.Length; i++)
            {
                positions[i] = views[i].transform.localPosition;
            }

            return positions;
        }

        private static void AssertLocalPositions<T>(
            T[] views,
            Vector3[] positions)
            where T : Component
        {
            Assert.That(views, Has.Length.EqualTo(positions.Length));
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].transform.localPosition,
                    Is.EqualTo(positions[i]));
            }
        }

        private static ShopCardOfferStatusView FindStatusAt(
            ShopCardOfferStatusView[] statuses,
            Vector3 localPosition)
        {
            foreach (ShopCardOfferStatusView status in statuses)
            {
                if (status.transform.localPosition == localPosition)
                {
                    return status;
                }
            }

            Assert.Fail($"Status not found at {localPosition}.");
            return null;
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
