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
        private static readonly Vector3 NormalStatusOffset =
            new Vector3(0f, -0.25f, 0f);
        private static readonly Vector3 DemonStatusOffset =
            new Vector3(0f, -0.4f, 0f);

        private GameObject _root;
        private ShopController _shop;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Shop Controller Test Root");
            _shop = _root.AddComponent<ShopController>();
            SetPrivateField("lighterPrice", 2);
            SetPrivateField("whiskeyPrice", 3);
            SetPrivateField("lighterPriceIncreasePerUsedVisit", 1);
            SetPrivateField("whiskeySoulRestore", 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void RFM01_U01_LighterPriceIncreasesOncePerUsedShopWhiskeyPriceNeverChanges()
        {
            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);

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
            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);

            // Only whiskey was used in the previous shop — the lighter price is
            // unaffected by whiskey usage.
            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.True);
            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.False);
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out int secondRestore),
                Is.True);
            Assert.That(secondRestore, Is.EqualTo(2));

            _shop.Close();
            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);

            // The lighter was used in the previous shop, so its price rises by one
            // step — whiskey's price stays fixed no matter how it's used.
            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(3));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
        }

        [Test]
        public void RFM01_U02_UnusedShopDoesNotIncreaseUtilityPricesAndRunResetClearsThem()
        {
            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
            _shop.Close();
            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);

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
        public void GSV07_U01_PurchasedUtilityItemDisappearsImmediately()
        {
            GameObject lighterObject = new GameObject("Lighter");
            lighterObject.transform.SetParent(_root.transform);
            ShopUtilityItemView lighter =
                lighterObject.AddComponent<ShopUtilityItemView>();
            GameObject whiskeyObject = new GameObject("Whiskey");
            whiskeyObject.transform.SetParent(_root.transform);
            ShopUtilityItemView whiskey =
                whiskeyObject.AddComponent<ShopUtilityItemView>();
            SetPrivateField("lighterItem", lighter);
            SetPrivateField("whiskeyItem", whiskey);

            _shop.Open(EnemyCombatProfileCatalog.FinalBossKey);
            _shop.RefreshUtilityItems(2, 5, 12);
            Assert.That(lighterObject.activeSelf, Is.True);
            Assert.That(whiskeyObject.activeSelf, Is.True);

            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.True);
            Assert.That(lighterObject.activeSelf, Is.False);
            Assert.That(whiskeyObject.activeSelf, Is.True);

            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out int restoredSoul),
                Is.True);
            Assert.That(restoredSoul, Is.EqualTo(2));
            Assert.That(whiskeyObject.activeSelf, Is.False);
        }

        [Test]
        [Category("GSH02")]
        public void GSH02_U07_StandaloneShopInjectsPriceRecoveryAndSoulFullState()
        {
            GameObject lighter = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03. Prefabs/Shop/ShopItem_Lighter.prefab"),
                _root.transform);
            GameObject whiskey = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03. Prefabs/Shop/ShopItem_Whiskey.prefab"),
                _root.transform);
            SetPrivateField(
                "lighterItem",
                lighter.GetComponent<ShopUtilityItemView>());
            SetPrivateField(
                "whiskeyItem",
                whiskey.GetComponent<ShopUtilityItemView>());

            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
            _shop.RefreshUtilityItems(2, 5, 12);

            string lighterDescription = lighter
                .GetComponent<HoverDescriptionTarget>()
                .ResolvedDescription;
            string whiskeyDescription = whiskey
                .GetComponent<HoverDescriptionTarget>()
                .ResolvedDescription;
            Assert.That(
                lighterDescription,
                Does.Contain(_shop.CurrentLighterPrice.ToString()));
            Assert.That(whiskeyDescription, Does.Contain("영혼을 2 회복"));
            Assert.That(
                whiskeyDescription,
                Does.Contain(_shop.CurrentWhiskeyPrice.ToString()));
            Assert.That(whiskeyDescription, Does.Not.Contain("이미 가득"));
            Assert.That(lighterDescription, Does.Not.Contain("{"));
            Assert.That(whiskeyDescription, Does.Not.Contain("{"));

            _shop.RefreshUtilityItems(2, 12, 12);
            Assert.That(
                whiskey.GetComponent<HoverDescriptionTarget>()
                    .ResolvedDescription,
                Does.Contain("영혼이 이미 가득 찼습니다."));
        }

        [Test]
        [Category("GSH03")]
        public void GSH03_U01_StandaloneShopCardsUseCombatDescriptionsAndSeparatePrices()
        {
            Transform normalHolder = CreateHolder("Normal Card Holder");
            Transform demonHolder = CreateHolder("Demon Card Holder");
            SetPrivateField("normalCardHolder", normalHolder);
            SetPrivateField("demonCardHolder", demonHolder);
            SetPrivateField("normalCardPrefab", CreateNormalCardPrefab());
            SetPrivateField("demonCardPrefab", CreateDemonCardPrefab());
            SetPrivateField("normalCardOfferCount", 3);
            SetPrivateField("demonCardOfferCount", 2);

            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);

            CardView[] normalCards =
                normalHolder.GetComponentsInChildren<CardView>(true);
            DemonCardView[] demonCards =
                demonHolder.GetComponentsInChildren<DemonCardView>(true);
            ShopPriceTarget[] priceTargets = normalHolder
                .GetComponentsInChildren<ShopPriceTarget>(true)
                .Concat(demonHolder.GetComponentsInChildren<
                    ShopPriceTarget>(true))
                .ToArray();

            Assert.That(normalCards, Has.Length.EqualTo(3));
            Assert.That(demonCards, Has.Length.EqualTo(2));
            Assert.That(priceTargets, Has.Length.EqualTo(5));

            foreach (CardView card in normalCards)
            {
                CardDefinition definition = CardDefinitionCatalog.GetByKey(
                    card.DefinitionKey);
                Assert.That(
                    card.HoverBadgeDescription,
                    Is.EqualTo(definition.Description));
                Assert.That(card.HoverBadgeDescription, Does.Not.Contain("PRICE"));
                Assert.That(card.HoverBadgeDescription, Does.Not.Contain("GOLD"));
            }

            foreach (DemonCardView card in demonCards)
            {
                DemonContractDefinition definition =
                    DemonContractCatalog.Default.GetByKey(
                        card.BoundCard.DefinitionKey);
                Assert.That(card.BoundCard.Summary, Is.EqualTo(definition.Summary));
                Assert.That(
                    card.BoundCard.CostSummary,
                    Is.EqualTo(definition.CostSummary));
            }

            foreach (ShopPriceTarget priceTarget in priceTargets)
            {
                Assert.That(priceTarget.ProductName, Is.Not.Empty);
                Assert.That(priceTarget.Price, Is.GreaterThan(0));
                Assert.That(priceTarget.IsSoldOut, Is.False);
            }
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

            _shop.Open(EnemyCombatProfileCatalog.FinalBossKey);
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

            Assert.That(_shop.Gold, Is.EqualTo(6));
            Assert.That(
                _shop.TryPurchaseNormalCard(offers[0].CardId, out _, out _),
                Is.False);
            Assert.That(_shop.Gold, Is.EqualTo(6));
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

            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
            _shop.GrantGoldForTesting(1000);
            DemonCardView[] offers =
                holderObject.GetComponentsInChildren<DemonCardView>(true);
            var purchasedKeys = new List<string>();
            foreach (DemonCardView offer in offers)
            {
                Assert.That(offer.BoundCard, Is.Not.Null);
                DemonContractDefinition definition =
                    DemonContractCatalog.Default.GetByKey(
                        offer.BoundCard.DefinitionKey);
                Assert.That(
                    offer.BoundCard.DisplayName,
                    Is.EqualTo(definition.DisplayName));
                Assert.That(
                    offer.BoundCard.Summary,
                    Is.EqualTo(definition.Summary));
                Assert.That(
                    offer.BoundCard.CostSummary,
                    Is.EqualTo(definition.CostSummary));
                Assert.That(
                    offer.BoundCard.CostSummary,
                    Does.Not.Contain("PRICE"));

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
            SetPrivateField("normalCardHolder", normalHolder);
            SetPrivateField("demonCardHolder", demonHolder);
            SetPrivateField("normalCardPrefab", normalPrefab);
            SetPrivateField("demonCardPrefab", demonPrefab);
            SetPrivateField("normalCardOfferCount", 3);
            SetPrivateField("demonCardOfferCount", 2);

            _shop.Open(EnemyCombatProfileCatalog.FinalBossKey);
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
            Assert.That(
                normalCards.All(card =>
                    card.GetComponent<ShopPriceTarget>() != null),
                Is.True);
            Assert.That(
                demonCards.All(card =>
                    card.GetComponent<ShopPriceTarget>() != null),
                Is.True);
            Assert.That(
                normalPositions.Average(position => position.x),
                Is.EqualTo(0f).Within(0.0001f));
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
            Assert.That(
                FindStatusAt(
                    normalStatuses,
                    normalPositions[0] + NormalStatusOffset).IsSoldOut,
                Is.True);
            Assert.That(
                FindStatusAt(
                    demonStatuses,
                    demonPositions[0] + DemonStatusOffset).IsSoldOut,
                Is.True);
            Assert.That(
                normalCards[0].GetComponent<ShopPriceTarget>().IsSoldOut,
                Is.True);
            Assert.That(
                demonCards[0].GetComponent<ShopPriceTarget>().IsSoldOut,
                Is.True);
            Assert.That(normalStatuses.All(status => status.IsDetached), Is.True);
            Assert.That(demonStatuses.All(status => status.IsDetached), Is.True);
            Vector3 statusScale = normalStatuses[0].transform.localScale;
            normalCards[1].SetHovered(true);
            Assert.That(normalStatuses[0].transform.localScale,
                Is.EqualTo(statusScale));
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
            SetPrivateField("normalCardHolder", holder);
            SetPrivateField("normalCardPrefab", prefab);
            SetPrivateField("normalCardOfferCount", 1);

            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
            _shop.ResetGold();
            CardView card = holder.GetComponentInChildren<CardView>(true);
            ShopCardOfferStatusView status =
                holder.GetComponentInChildren<ShopCardOfferStatusView>(true);
            ShopPriceTarget priceTarget =
                card.GetComponent<ShopPriceTarget>();
            Vector3 position = card.transform.localPosition;

            Assert.That(card.CanUse, Is.False);
            Assert.That(card.IsShopSoldOut, Is.False);
            Assert.That(
                card.GetComponentInChildren<SpriteRenderer>().color,
                Is.EqualTo(Color.white));
            Assert.That(status.IsSoldOut, Is.False);
            Assert.That(priceTarget, Is.Not.Null);
            Assert.That(priceTarget.IsSoldOut, Is.False);
            Assert.That(
                _shop.TryPurchaseNormalCard(card.CardId, out _, out _),
                Is.False);
            Assert.That(_shop.Gold, Is.Zero);
            Assert.That(card.transform.localPosition, Is.EqualTo(position));
            Assert.That(card.IsShopSoldOut, Is.False);
            Assert.That(status.IsSoldOut, Is.False);
            Assert.That(priceTarget.IsSoldOut, Is.False);
        }

        [Test]
        public void GSV06_U04_StatusPrefabKeepsSoldOutAndDisablesWorldPrice()
        {
            const string prefabPath =
                "Assets/03. Prefabs/Shop/ShopCardOfferStatus.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                ShopCardOfferStatusView status =
                    instance.GetComponent<ShopCardOfferStatusView>();
                Assert.That(status, Is.Not.Null);
                status.Bind(isSoldOut: true);

                Assert.That(status.IsSoldOut, Is.True);
                Assert.That(
                    instance.GetComponentsInChildren<Collider>(true),
                    Is.Empty);
                Assert.That(
                    instance.transform.Find("Price"),
                    Is.Null);

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

                Assert.That(textCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV06_U05_ProductPrefabsAuthorIndependentShopPriceAnchors()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/Card.prefab");
            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/DemonCard.prefab");
            Assert.That(cardPrefab, Is.Not.Null);
            Assert.That(demonPrefab, Is.Not.Null);

            ShopPriceTarget cardPrice = cardPrefab.GetComponent<ShopPriceTarget>();
            ShopPriceTarget demonPrice = demonPrefab.GetComponent<ShopPriceTarget>();
            Assert.That(cardPrice, Is.Not.Null);
            Assert.That(demonPrice, Is.Not.Null);
            Assert.That(cardPrice.PriceAnchor.name, Is.EqualTo("ShopPriceAnchor"));
            Assert.That(demonPrice.PriceAnchor.name, Is.EqualTo("ShopPriceAnchor"));
            Assert.That(
                cardPrice.PriceAnchor,
                Is.Not.SameAs(cardPrefab.transform.Find("BottomPosition")));
            Assert.That(
                cardPrice.PriceAnchor,
                Is.Not.SameAs(cardPrefab.transform.Find("TopPosition")));
            Assert.That(
                demonPrice.PriceAnchor,
                Is.Not.SameAs(demonPrefab.transform.Find("BottomPosition")));
            Assert.That(
                demonPrice.PriceAnchor,
                Is.Not.SameAs(demonPrefab.transform.Find("TopPosition")));

            GameObject lighter = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Shop/ShopItem_Lighter.prefab");
            GameObject whiskey = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Shop/ShopItem_Whiskey.prefab");
            ShopPriceTarget lighterPrice = lighter.GetComponent<ShopPriceTarget>();
            ShopPriceTarget whiskeyPrice = whiskey.GetComponent<ShopPriceTarget>();
            Assert.That(lighterPrice.PriceAnchor.name, Is.EqualTo("PriceAnchor"));
            Assert.That(whiskeyPrice.PriceAnchor.name, Is.EqualTo("PriceAnchor"));
            Assert.That(
                lighterPrice.PriceAnchor.localPosition.y,
                Is.LessThan(0f));
            Assert.That(
                whiskeyPrice.PriceAnchor.localPosition.y,
                Is.LessThan(0f));
            Assert.That(
                lighterPrice.PriceAnchor.localPosition.y,
                Is.Not.EqualTo(whiskeyPrice.PriceAnchor.localPosition.y));

            GameObject cardInstance = Object.Instantiate(cardPrefab);
            GameObject demonInstance = Object.Instantiate(demonPrefab);
            try
            {
                ShopCardOfferStatusView cardInstanceStatus =
                    cardInstance.GetComponentInChildren<ShopCardOfferStatusView>(true);
                ShopCardOfferStatusView demonInstanceStatus =
                    demonInstance.GetComponentInChildren<ShopCardOfferStatusView>(true);
                cardInstanceStatus.gameObject.SetActive(true);
                demonInstanceStatus.gameObject.SetActive(true);

                InvokePrivate(cardInstance.GetComponent<CardView>(), "Awake");
                InvokePrivate(demonInstance.GetComponent<DemonCardView>(), "Awake");

                Assert.That(cardInstanceStatus.gameObject.activeSelf, Is.False);
                Assert.That(demonInstanceStatus.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cardInstance);
                Object.DestroyImmediate(demonInstance);
            }
        }

        [Test]
        [Category("GSV06")]
        public void GSV06_U07_ShopUsesTableControllerAuthoredProductTransforms()
        {
            Transform normalHolder = CreateHolder("Normal Card Holder");
            Transform demonHolder = CreateHolder("Demon Card Holder");
            Vector3[] normalPositions =
            {
                new Vector3(-0.64f, 0.04f, 0f),
                new Vector3(0.03f, 0.02f, 0.01f),
                new Vector3(0.71f, -0.01f, 0.02f),
            };
            Vector3[] demonPositions =
            {
                new Vector3(-0.32f, 0.08f, 0f),
                new Vector3(0.37f, 0.05f, 0.01f),
            };
            Transform[] normalLayouts = CreateAuthoredLayouts(
                normalHolder,
                "NormalCard",
                normalPositions);
            Transform[] demonLayouts = CreateAuthoredLayouts(
                demonHolder,
                "DemonCard",
                demonPositions);
            normalLayouts[1].localRotation = Quaternion.Euler(0f, 0f, 4f);
            normalLayouts[2].localScale = new Vector3(0.74f, 0.72f, 0.7f);
            demonLayouts[0].localRotation = Quaternion.Euler(0f, 0f, -3f);
            demonLayouts[1].localScale = new Vector3(0.68f, 0.7f, 0.72f);

            SetPrivateField("normalCardHolder", normalHolder);
            SetPrivateField("demonCardHolder", demonHolder);
            SetPrivateField("normalCardPrefab", CreateNormalCardPrefab());
            SetPrivateField("demonCardPrefab", CreateDemonCardPrefab());
            SetPrivateField("normalCardOfferCount", normalPositions.Length);
            SetPrivateField("demonCardOfferCount", demonPositions.Length);
            SetPrivateField("normalCardSpacing", 9f);
            SetPrivateField("demonCardSpacing", 9f);

            GameObject lighterObject = new GameObject("Lighter");
            lighterObject.transform.SetParent(_root.transform, false);
            lighterObject.transform.localPosition =
                new Vector3(0.44f, 0.12f, 1.3f);
            ShopUtilityItemView lighter =
                lighterObject.AddComponent<ShopUtilityItemView>();
            GameObject whiskeyObject = new GameObject("Whiskey");
            whiskeyObject.transform.SetParent(_root.transform, false);
            whiskeyObject.transform.localPosition =
                new Vector3(-0.51f, 0.09f, 1.24f);
            ShopUtilityItemView whiskey =
                whiskeyObject.AddComponent<ShopUtilityItemView>();
            SetPrivateField("lighterItem", lighter);
            SetPrivateField("whiskeyItem", whiskey);
            Vector3 lighterPosition = lighterObject.transform.localPosition;
            Vector3 whiskeyPosition = whiskeyObject.transform.localPosition;

            _shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
            _shop.RefreshUtilityItems(2, 5, 12);

            CardView[] normalCards = normalHolder
                .GetComponentsInChildren<CardView>(true)
                .OrderBy(card => card.transform.GetSiblingIndex())
                .ToArray();
            DemonCardView[] demonCards = demonHolder
                .GetComponentsInChildren<DemonCardView>(true)
                .OrderBy(card => card.transform.GetSiblingIndex())
                .ToArray();
            AssertLocalPositions(normalCards, normalPositions);
            AssertLocalPositions(demonCards, demonPositions);
            Assert.That(
                Quaternion.Angle(
                    normalCards[1].transform.localRotation,
                    normalLayouts[1].localRotation),
                Is.LessThan(0.001f));
            Assert.That(
                normalCards[2].transform.localScale,
                Is.EqualTo(normalLayouts[2].localScale));
            Assert.That(
                Quaternion.Angle(
                    demonCards[0].transform.localRotation,
                    demonLayouts[0].localRotation),
                Is.LessThan(0.001f));
            Assert.That(
                demonCards[1].transform.localScale,
                Is.EqualTo(demonLayouts[1].localScale));
            Assert.That(normalLayouts.All(layout => !layout.gameObject.activeSelf));
            Assert.That(demonLayouts.All(layout => !layout.gameObject.activeSelf));
            Assert.That(
                lighterObject.transform.localPosition,
                Is.EqualTo(lighterPosition));
            Assert.That(
                whiskeyObject.transform.localPosition,
                Is.EqualTo(whiskeyPosition));
        }

        private Transform CreateHolder(string name)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(_root.transform);
            return holder.transform;
        }

        private static Transform[] CreateAuthoredLayouts(
            Transform holder,
            string label,
            IReadOnlyList<Vector3> positions)
        {
            Transform[] layouts = new Transform[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                GameObject layout = new GameObject(
                    $"__TableLayoutPreview_{label}_{i}");
                layout.transform.SetParent(holder, false);
                layout.transform.localPosition = positions[i];
                layout.transform.localScale = Vector3.one * 0.7f;
                layouts[i] = layout.transform;
            }

            return layouts;
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
            ShopCardOfferStatusView status = CreateStatusPrefab(
                prefabObject.transform,
                NormalStatusOffset,
                Vector3.one * 1.4f);
            SetPrivateField(prefab, "shopOfferStatus", status);
            CreatePriceTarget(prefabObject);
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
            ShopCardOfferStatusView status = CreateStatusPrefab(
                prefabObject.transform,
                DemonStatusOffset,
                Vector3.one);
            SetPrivateField(prefab, "shopOfferStatus", status);
            CreatePriceTarget(prefabObject);
            return prefab;
        }

        private ShopCardOfferStatusView CreateStatusPrefab(
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject prefabObject = new GameObject("ShopCardOfferStatus");
            prefabObject.transform.SetParent(parent, false);
            prefabObject.transform.localPosition = localPosition;
            prefabObject.transform.localScale = localScale;
            ShopCardOfferStatusView prefab =
                prefabObject.AddComponent<ShopCardOfferStatusView>();
            Component soldOutText = CreateText("Sold Out", prefabObject.transform);
            SetPrivateField(prefab, "soldOutText", soldOutText);
            prefabObject.SetActive(false);
            return prefab;
        }

        private static ShopPriceTarget CreatePriceTarget(GameObject owner)
        {
            GameObject anchor = new GameObject("PriceAnchor");
            anchor.transform.SetParent(owner.transform, false);
            anchor.transform.localPosition = Vector3.up;
            ShopPriceTarget target = owner.AddComponent<ShopPriceTarget>();
            SetPrivateField(target, "priceAnchor", anchor.transform);
            return target;
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
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
