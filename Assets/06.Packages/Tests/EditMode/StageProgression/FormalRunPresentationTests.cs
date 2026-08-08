using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class FormalRunPresentationTests
    {
        [Test]
        public void RF04_P01_FormalOpponentCandidatesShowGoldInsteadOfCardRewardTier()
        {
            FormalRunSession run = CreateRun(enableOpponentSelection: true);
            Assert.That(run.TryStartRun(), Is.True);

            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);

            Assert.That(model.CanFocusOpponent, Is.True);
            Assert.That(model.OpponentCandidates.Count, Is.EqualTo(2));
            foreach (OpponentCandidateViewModel candidate in model.OpponentCandidates)
            {
                Assert.That(candidate.RewardTier, Does.StartWith("VICTORY GOLD "));
                Assert.That(candidate.RewardTier, Does.Not.Contain("REWARD"));
            }

            string focusedKey = model.OpponentCandidates[0].ProfileKey;
            StageProgressionViewModel focused =
                StageProgressionPresenter.Create(run, focusedKey);
            Assert.That(focused.FocusedOpponentProfileKey, Is.EqualTo(focusedKey));
            Assert.That(focused.OpponentCandidates[0].RewardTier,
                Does.StartWith("VICTORY GOLD "));
        }

        [Test]
        public void RF04_P02_FirstVictoryShowsGoldAndShopWithoutRewardControls()
        {
            FormalRunSession run = CreateRun();
            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);

            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);

            Assert.That(model.IsShop, Is.True);
            Assert.That(model.Message, Is.EqualTo("SHOP"));
            Assert.That(model.PlayerGold, Is.EqualTo("120 GOLD"));
            Assert.That(model.GoldResult, Is.EqualTo("VICTORY +120 GOLD"));
            Assert.That(model.ShopCardOptions.Count, Is.EqualTo(5));
            Assert.That(model.ShopOwnedCards.Count, Is.EqualTo(4));
            Assert.That(model.CanSelectReward, Is.False);
            Assert.That(model.CanSkipReward, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanLeaveShop, Is.True);
            Assert.That(
                model.WhiskeyRecoveryAmount,
                Is.EqualTo(run.ActiveShop.Offer.WhiskeyRecovery));
            Assert.That(
                model.IsPlayerSoulFull,
                Is.EqualTo(
                    run.CombatSession.Progress.Player.CurrentSoul >=
                    run.CombatSession.Progress.Player.MaximumSoul));
        }

        [Test]
        public void RF04_P03_PurchaseUpdatesSoldStateGoldAndTransactionText()
        {
            FormalRunSession run = OpenFirstShop();
            StageProgressionViewModel before = StageProgressionPresenter.Create(run);
            ShopCardOptionViewModel option = before.ShopCardOptions[0];

            Assert.That(run.TryBuyShopCard(
                before.ShopOfferId.Value,
                option.OptionId), Is.True);
            StageProgressionViewModel after = StageProgressionPresenter.Create(run);

            Assert.That(after.PlayerGold, Is.EqualTo("117 GOLD"));
            Assert.That(after.ShopCardOptions[0].IsSold, Is.True);
            Assert.That(after.ShopCardOptions[0].CanBuy, Is.False);
            Assert.That(after.ShopTransactionResult, Does.StartWith("PURCHASED"));
        }

        [Test]
        public void RF04_P04_ControllerRoutesRestAndRemovalToFormalSession()
        {
            FormalRunSession run = OpenFirstShop();
            PlayerRunState player = run.CombatSession.Progress.Player;
            player.SetCurrentSoul(10);
            player.AddGold(4);
            StageProgressionController controller = CreateController(
                run.CombatSession,
                out GameObject root);

            try
            {
                StageProgressionViewModel model = controller.CurrentViewModel;
                int offerId = model.ShopOfferId.Value;
                int cardId = model.ShopOwnedCards[0].CardId;

                controller.RequestRestAtShop(offerId);
                controller.RequestRemoveShopCard(offerId, cardId);

                Assert.That(player.CurrentSoul, Is.EqualTo(12));
                Assert.That(player.Deck.Count, Is.EqualTo(3));
                Assert.That(controller.CurrentViewModel.WhiskeyLabel, Does.Contain("USED"));
                Assert.That(controller.CurrentViewModel.LighterLabel, Does.Contain("USED"));
                Assert.That(controller.CurrentViewModel.ShopTransactionResult,
                    Does.StartWith("REMOVED CARD"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RF04_P05_UsedFirstShopRaisesOnlySecondLighterPriceLabel()
        {
            FormalRunSession run = OpenFirstShop();
            PlayerRunState player = run.CombatSession.Progress.Player;
            player.AddGold(4);
            int firstOfferId = run.ActiveShop.Offer.OfferId;
            Assert.That(run.TryRemoveShopCard(
                firstOfferId,
                player.Deck[0].Id), Is.True);
            Assert.That(run.TryLeaveShop(firstOfferId), Is.True);
            WinCurrentBattle(run.CombatSession);

            StageProgressionViewModel model = StageProgressionPresenter.Create(run);

            Assert.That(model.LighterLabel, Is.EqualTo("LIGHTER  70 GOLD"));
            Assert.That(model.WhiskeyLabel, Is.EqualTo("WHISKEY  50 GOLD"));
        }

        [Test]
        public void RF04_P06_BossVictoryShowsRunResultWithoutShopOrReward()
        {
            FormalRunSession run = OpenFirstShop();
            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
            WinCurrentBattle(run.CombatSession);
            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
            WinCurrentBattle(
                run.CombatSession,
                StageProgressionState.RunVictory);

            StageProgressionViewModel model = StageProgressionPresenter.Create(run);

            Assert.That(model.IsShop, Is.False);
            Assert.That(model.CanRestartRun, Is.True);
            Assert.That(model.CanSelectReward, Is.False);
            Assert.That(model.RewardOptions, Is.Empty);
            Assert.That(model.GoldResult, Is.Empty);
            Assert.That(model.Message, Is.EqualTo("RUN VICTORY"));
        }

        [Test]
        public void GF04_U01_FormalShopRendersThreeNormalAndTwoDemonOptions()
        {
            FormalRunSession run = OpenFirstShop();
            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);
            GameObject root = new GameObject("GF04 Formal Shop Test");
            try
            {
                ShopController shop = root.AddComponent<ShopController>();
                GameObject normalHolder = new GameObject("Normal Holder");
                normalHolder.transform.SetParent(root.transform);
                GameObject demonHolder = new GameObject("Demon Holder");
                demonHolder.transform.SetParent(root.transform);
                GameObject normalPrefabObject = new GameObject("Normal Prefab");
                normalPrefabObject.transform.SetParent(root.transform);
                CardView normalPrefab =
                    normalPrefabObject.AddComponent<CardView>();
                GameObject demonPrefabObject = new GameObject("Demon Prefab");
                demonPrefabObject.transform.SetParent(root.transform);
                DemonCardView demonPrefab =
                    demonPrefabObject.AddComponent<DemonCardView>();
                SetField(shop, "normalCardHolder", normalHolder.transform);
                SetField(shop, "demonCardHolder", demonHolder.transform);
                SetField(shop, "normalCardPrefab", normalPrefab);
                SetField(shop, "demonCardPrefab", demonPrefab);

                shop.OpenFormal(model);

                Assert.That(shop.IsOpen, Is.True);
                Assert.That(shop.IsFormal, Is.True);
                Assert.That(shop.FormalNormalOfferCount, Is.EqualTo(3));
                Assert.That(shop.FormalDemonOfferCount, Is.EqualTo(2));
                foreach (ShopCardOptionViewModel option in model.ShopCardOptions)
                {
                    Assert.That(option.DefinitionKey, Is.Not.Empty);
                }
                Assert.That(
                    normalHolder.GetComponentsInChildren<CardView>(true),
                    Has.Length.EqualTo(3));
                foreach (CardView card in
                         normalHolder.GetComponentsInChildren<CardView>(true))
                {
                    Assert.That(card.DefinitionKey, Is.Not.Empty);
                }
                DemonCardView[] demonCards =
                    demonHolder.GetComponentsInChildren<DemonCardView>(true);
                Assert.That(demonCards, Has.Length.EqualTo(2));
                foreach (DemonCardView card in demonCards)
                {
                    Assert.That(card.BoundCard, Is.Not.Null);
                    DemonContractDefinition definition =
                        DemonContractCatalog.Default.GetByKey(
                            card.BoundCard.DefinitionKey);
                    Assert.That(
                        card.BoundCard.DisplayName,
                        Is.EqualTo(definition.DisplayName));
                    Assert.That(
                        card.BoundCard.Summary,
                        Is.EqualTo(definition.Summary));
                    Assert.That(
                        card.BoundCard.CostSummary,
                        Is.EqualTo(definition.CostSummary));
                    Assert.That(
                        card.BoundCard.CostSummary,
                        Does.Not.Contain("GOLD"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSV06")]
        public void GSV06_U03_FormalRefreshKeepsFiveSlotsAndSoldOptionOrder()
        {
            FormalRunSession run = OpenFirstShop();
            StageProgressionViewModel before = StageProgressionPresenter.Create(run);
            ShopCardOptionViewModel purchasedOption = before.ShopCardOptions[0];
            int[] optionOrder = GetOptionOrder(before);
            Assert.That(purchasedOption.PriceAmount, Is.EqualTo(3));
            Assert.That(purchasedOption.Price, Is.EqualTo("3 GOLD"));

            GameObject root = new GameObject("GSV06 Formal Shop Test");
            try
            {
                ShopController shop = CreateFormalShopController(
                    root,
                    useAuthoredLayouts: true);
                shop.OpenFormal(before);
                Vector3[] firstPositions = GetActiveOfferPositions(root);
                CardView[] firstNormalCards = GetActiveComponents<CardView>(root);
                DemonCardView[] firstDemonCards =
                    GetActiveComponents<DemonCardView>(root);
                Assert.That(
                    AverageLocalX(firstNormalCards),
                    Is.EqualTo(0f).Within(0.0001f));
                AssertOfferBaseScales(firstNormalCards, firstDemonCards, 0.7f);

                Assert.That(run.TryBuyShopCard(
                    before.ShopOfferId.Value,
                    purchasedOption.OptionId), Is.True);
                StageProgressionViewModel after = StageProgressionPresenter.Create(run);
                shop.OpenFormal(after);

                Assert.That(GetOptionOrder(after), Is.EqualTo(optionOrder));
                Assert.That(after.ShopCardOptions, Has.Count.EqualTo(5));
                Assert.That(after.ShopCardOptions[0].IsSold, Is.True);
                Assert.That(GetActiveOfferPositions(root), Is.EqualTo(firstPositions));

                ShopCardOfferStatusView[] statuses =
                    GetActiveComponents<ShopCardOfferStatusView>(root);
                Assert.That(statuses, Has.Length.EqualTo(5));
                Assert.That(CountSoldStatuses(statuses), Is.EqualTo(1));
                Assert.That(shop.ActivePriceTargets, Has.Count.EqualTo(5));
                Assert.That(
                    shop.ActivePriceTargets.Count(target => target.IsSoldOut),
                    Is.EqualTo(1));
                Assert.That(
                    shop.ActivePriceTargets.Any(target => target.Price == 3),
                    Is.True);

                CardView[] normalCards = GetActiveComponents<CardView>(root);
                DemonCardView[] demonCards = GetActiveComponents<DemonCardView>(root);
                Assert.That(normalCards.Length + demonCards.Length, Is.EqualTo(5));
                AssertOfferBaseScales(normalCards, demonCards, 0.7f);
                Assert.That(FindOfferCanUse(
                    normalCards,
                    demonCards,
                    purchasedOption.OptionId), Is.False);
                Assert.That(run.TryBuyShopCard(
                    after.ShopOfferId.Value,
                    purchasedOption.OptionId), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSV06_U06_FormalShopKeepsAuthoredUtilityItemPositions()
        {
            FormalRunSession run = OpenFirstShop();
            StageProgressionViewModel model = StageProgressionPresenter.Create(run);
            GameObject root = new GameObject("GSV06 Utility Layout Test");
            try
            {
                ShopController shop = CreateFormalShopController(root);
                ShopUtilityItemView lighter = CreateChild(root, "Lighter")
                    .AddComponent<ShopUtilityItemView>();
                ShopUtilityItemView whiskey = CreateChild(root, "Whiskey")
                    .AddComponent<ShopUtilityItemView>();
                Vector3 lighterPosition = new Vector3(-1.25f, 2.5f, 3.75f);
                Vector3 whiskeyPosition = new Vector3(4.5f, 5.75f, -6.25f);
                lighter.transform.localPosition = lighterPosition;
                whiskey.transform.localPosition = whiskeyPosition;
                SetField(shop, "lighterItem", lighter);
                SetField(shop, "whiskeyItem", whiskey);

                shop.OpenFormal(model);

                Assert.That(lighter.transform.localPosition, Is.EqualTo(lighterPosition));
                Assert.That(whiskey.transform.localPosition, Is.EqualTo(whiskeyPosition));

                shop.CloseFormal();

                Assert.That(lighter.transform.localPosition, Is.EqualTo(lighterPosition));
                Assert.That(whiskey.transform.localPosition, Is.EqualTo(whiskeyPosition));
                Assert.That(shop.ActivePriceTargets, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSH02")]
        public void GSH02_U08_FormalShopUsesExactWhiskeyValuesAndSoulFullState()
        {
            FormalRunSession run = OpenFirstShop();
            PlayerRunState player = run.CombatSession.Progress.Player;
            player.SetCurrentSoul(player.MaximumSoul - 1);
            StageProgressionViewModel model = StageProgressionPresenter.Create(run);
            GameObject root = new GameObject("GSH02 Formal Utility Hover Test");
            GameObject lighter = null;
            GameObject whiskey = null;
            try
            {
                ShopController shop = CreateFormalShopController(root);
                lighter = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/Shop/ShopItem_Lighter.prefab"),
                    root.transform);
                whiskey = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/Shop/ShopItem_Whiskey.prefab"),
                    root.transform);
                SetField(
                    shop,
                    "lighterItem",
                    lighter.GetComponent<ShopUtilityItemView>());
                SetField(
                    shop,
                    "whiskeyItem",
                    whiskey.GetComponent<ShopUtilityItemView>());

                shop.OpenFormal(model);

                Assert.That(
                    lighter.GetComponent<HoverDescriptionTarget>()
                        .ResolvedDescription,
                    Does.Contain(model.LighterPriceAmount.ToString()));
                string availableWhiskey = whiskey
                    .GetComponent<HoverDescriptionTarget>()
                    .ResolvedDescription;
                Assert.That(
                    availableWhiskey,
                    Does.Contain(model.WhiskeyRecoveryAmount.ToString()));
                Assert.That(
                    availableWhiskey,
                    Does.Contain(model.WhiskeyPriceAmount.ToString()));
                Assert.That(
                    availableWhiskey,
                    Does.Not.Contain("이미 가득"));

                player.SetCurrentSoul(player.MaximumSoul);
                shop.OpenFormal(StageProgressionPresenter.Create(run));
                Assert.That(
                    whiskey.GetComponent<HoverDescriptionTarget>()
                        .ResolvedDescription,
                    Does.Contain("영혼이 이미 가득 찼습니다."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSH03")]
        [Category("GSV06")]
        public void GSH03_U02_FormalShopUsesCombatCardDescriptionsAndKeepsPricesSeparate()
        {
            FormalRunSession run = OpenFirstShop();
            StageProgressionViewModel model = StageProgressionPresenter.Create(run);
            GameObject root = new GameObject("GSH03 Formal Shop Tooltip Test");
            GameObject lighter = null;
            GameObject whiskey = null;
            try
            {
                foreach (ShopCardOptionViewModel option in model.ShopCardOptions)
                {
                    if (option.Category != "CARD")
                    {
                        continue;
                    }

                    CardDefinition definition = CardDefinitionCatalog.GetByKey(
                        option.DefinitionKey);
                    Assert.That(option.Summary, Is.EqualTo(definition.Description));
                    Assert.That(
                        option.Price,
                        Is.EqualTo($"{option.PriceAmount} GOLD"));
                }

                ShopController shop = CreateFormalShopController(root);
                lighter = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/Shop/ShopItem_Lighter.prefab"),
                    root.transform);
                whiskey = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/Shop/ShopItem_Whiskey.prefab"),
                    root.transform);
                SetField(
                    shop,
                    "lighterItem",
                    lighter.GetComponent<ShopUtilityItemView>());
                SetField(
                    shop,
                    "whiskeyItem",
                    whiskey.GetComponent<ShopUtilityItemView>());

                shop.OpenFormal(model);

                CardView[] normalCards = GetActiveComponents<CardView>(root);
                DemonCardView[] demonCards =
                    GetActiveComponents<DemonCardView>(root);
                ShopCardOfferStatusView[] statuses =
                    GetActiveComponents<ShopCardOfferStatusView>(root);
                Assert.That(normalCards, Has.Length.EqualTo(3));
                Assert.That(demonCards, Has.Length.EqualTo(2));
                Assert.That(statuses, Has.Length.EqualTo(5));
                Assert.That(shop.ActivePriceTargets, Has.Count.EqualTo(7));

                GameObject cameraObject = CreateChild(root, "Price Camera");
                cameraObject.transform.position = new Vector3(0f, 0f, -50f);
                Camera camera = cameraObject.AddComponent<Camera>();

                foreach (CardView card in normalCards)
                {
                    CardDefinition definition = CardDefinitionCatalog.GetByKey(
                        card.DefinitionKey);
                    Assert.That(
                        card.HoverBadgeDescription,
                        Is.EqualTo(definition.Description));
                    Assert.That(
                        card.HoverBadgeDescription,
                        Does.Not.Contain("GOLD"));
                    Assert.That(
                        card.HoverBadgeDescription,
                        Does.Not.Contain("PRICE"));
                }

                foreach (DemonCardView card in demonCards)
                {
                    DemonContractDefinition definition =
                        DemonContractCatalog.Default.GetByKey(
                            card.BoundCard.DefinitionKey);
                    Assert.That(
                        card.BoundCard.Summary,
                        Is.EqualTo(definition.Summary));
                    Assert.That(
                        card.BoundCard.CostSummary,
                        Is.EqualTo(definition.CostSummary));
                }

                foreach (ShopPriceTarget priceTarget in shop.ActivePriceTargets)
                {
                    Assert.That(priceTarget.HasRequiredReferences, Is.True);
                    Assert.That(priceTarget.ProductName, Is.Not.Empty);
                    Assert.That(priceTarget.Price, Is.GreaterThanOrEqualTo(0));
                    Assert.That(
                        priceTarget.TryCreateRequest(camera, out _),
                        Is.True);
                }

                Assert.That(
                    lighter.GetComponent<HoverDescriptionTarget>()
                        .ResolvedDescription,
                    Does.Contain(model.LighterPriceAmount.ToString()));
                Assert.That(
                    whiskey.GetComponent<HoverDescriptionTarget>()
                        .ResolvedDescription,
                    Does.Contain(model.WhiskeyRecoveryAmount.ToString()));
                Assert.That(
                    whiskey.GetComponent<HoverDescriptionTarget>()
                        .ResolvedDescription,
                    Does.Contain(model.WhiskeyPriceAmount.ToString()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ShopController CreateFormalShopController(
            GameObject root,
            bool useAuthoredLayouts = false)
        {
            ShopController shop = root.AddComponent<ShopController>();
            Transform normalHolder = CreateChild(root, "Normal Holder").transform;
            Transform demonHolder = CreateChild(root, "Demon Holder").transform;
            if (useAuthoredLayouts)
            {
                CreateAuthoredLayouts(normalHolder, "NormalCard", 3, 0.64f);
                CreateAuthoredLayouts(demonHolder, "DemonCard", 2, 0.32f);
            }

            GameObject normalPrefabObject = CreateChild(root, "Normal Prefab");
            CardView normalPrefab = normalPrefabObject.AddComponent<CardView>();
            GameObject demonPrefabObject = CreateChild(root, "Demon Prefab");
            DemonCardView demonPrefab = demonPrefabObject.AddComponent<DemonCardView>();
            CreateEmbeddedStatus(normalPrefabObject, normalPrefab);
            CreateEmbeddedStatus(demonPrefabObject, demonPrefab);
            CreatePriceTarget(normalPrefabObject);
            CreatePriceTarget(demonPrefabObject);
            SetField(shop, "normalCardHolder", normalHolder);
            SetField(shop, "demonCardHolder", demonHolder);
            SetField(shop, "normalCardPrefab", normalPrefab);
            SetField(shop, "demonCardPrefab", demonPrefab);
            return shop;
        }

        private static void CreateAuthoredLayouts(
            Transform holder,
            string label,
            int count,
            float spacing)
        {
            float offset = -(count - 1) * 0.5f * spacing;
            for (int i = 0; i < count; i++)
            {
                GameObject preview = new GameObject(
                    $"__TableLayoutPreview_{label}_{i}");
                preview.transform.SetParent(holder, false);
                preview.transform.localPosition = new Vector3(
                    offset + i * spacing,
                    0f,
                    i * 0.01f);
                preview.transform.localScale = Vector3.one * 0.7f;
            }
        }

        private static void AssertOfferBaseScales(
            IReadOnlyList<CardView> normalCards,
            IReadOnlyList<DemonCardView> demonCards,
            float expectedScale)
        {
            Vector3 expected = Vector3.one * expectedScale;
            foreach (CardView card in normalCards)
            {
                Assert.That(card.transform.localScale, Is.EqualTo(expected));
                Assert.That(GetField<Vector3>(card, "_baseScale"), Is.EqualTo(expected));
            }

            foreach (DemonCardView card in demonCards)
            {
                Assert.That(card.transform.localScale, Is.EqualTo(expected));
                Assert.That(GetField<Vector3>(card, "_baseScale"), Is.EqualTo(expected));
                Assert.That(GetField<Vector3>(card, "_targetScale"), Is.EqualTo(expected));
            }
        }

        private static void CreateEmbeddedStatus(
            GameObject cardObject,
            Component cardView)
        {
            GameObject statusObject = CreateChild(cardObject, "ShopCardOfferStatus");
            ShopCardOfferStatusView status =
                statusObject.AddComponent<ShopCardOfferStatusView>();
            Type textType = Type.GetType(
                "TMPro.TextMeshPro, Unity.TextMeshPro");
            Assert.That(textType, Is.Not.Null);
            Component sold = CreateChild(statusObject, "Sold")
                .AddComponent(textType);
            SetField(status, "soldOutText", sold);
            SetField(cardView, "shopOfferStatus", status);
            statusObject.SetActive(false);
        }

        private static void CreatePriceTarget(GameObject cardObject)
        {
            GameObject anchor = CreateChild(cardObject, "PriceAnchor");
            anchor.transform.localPosition = Vector3.up;
            ShopPriceTarget target = cardObject.AddComponent<ShopPriceTarget>();
            SetField(target, "priceAnchor", anchor.transform);
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child;
        }

        private static int[] GetOptionOrder(StageProgressionViewModel model)
        {
            var ids = new int[model.ShopCardOptions.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = model.ShopCardOptions[i].OptionId;
            }

            return ids;
        }

        private static Vector3[] GetActiveOfferPositions(GameObject root)
        {
            CardView[] normalCards = GetActiveComponents<CardView>(root);
            DemonCardView[] demonCards = GetActiveComponents<DemonCardView>(root);
            var positions = new Vector3[normalCards.Length + demonCards.Length];
            int index = 0;
            foreach (CardView card in normalCards)
            {
                positions[index++] = card.transform.localPosition;
            }

            foreach (DemonCardView card in demonCards)
            {
                positions[index++] = card.transform.localPosition;
            }

            return positions;
        }

        private static float AverageLocalX(CardView[] cards)
        {
            float sum = 0f;
            foreach (CardView card in cards)
            {
                sum += card.transform.localPosition.x;
            }

            return cards.Length == 0 ? 0f : sum / cards.Length;
        }

        private static T[] GetActiveComponents<T>(GameObject root)
            where T : Component
        {
            T[] all = root.GetComponentsInChildren<T>(true);
            var active = new List<T>();
            foreach (T component in all)
            {
                if (component.gameObject.activeInHierarchy &&
                    component.transform.parent != root.transform)
                {
                    active.Add(component);
                }
            }

            return active.ToArray();
        }

        private static int CountSoldStatuses(
            ShopCardOfferStatusView[] statuses)
        {
            int count = 0;
            foreach (ShopCardOfferStatusView status in statuses)
            {
                if (status.IsSoldOut)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool FindOfferCanUse(
            CardView[] normalCards,
            DemonCardView[] demonCards,
            int optionId)
        {
            foreach (CardView card in normalCards)
            {
                if (card.CardId == optionId)
                {
                    return card.CanUse;
                }
            }

            foreach (DemonCardView card in demonCards)
            {
                if (card.CardId == optionId)
                {
                    return card.CanUse;
                }
            }

            Assert.Fail($"Offer {optionId} was not rendered.");
            return false;
        }

        private static StageProgressionController CreateController(
            StageProgressionSession session,
            out GameObject root)
        {
            root = new GameObject("RF04 Controller Test");
            StageProgressionView view = root.AddComponent<StageProgressionView>();
            StageProgressionRuntime runtime = root.AddComponent<StageProgressionRuntime>();
            SetProperty(runtime, "SaveFlow", null);
            SetProperty(runtime, "Session", session);
            SetProperty(runtime, "Instance", runtime);

            StageProgressionController controller =
                root.AddComponent<StageProgressionController>();
            if (controller.CurrentViewModel == null)
            {
                SetField(controller, "_runtime", runtime);
                SetField(controller, "_view", view);
                Invoke(controller, "RefreshView");
            }

            return controller;
        }

        private static FormalRunSession OpenFirstShop()
        {
            FormalRunSession run = CreateRun();
            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);
            return run;
        }

        private static FormalRunSession CreateRun(bool enableOpponentSelection = false)
        {
            StageProgressionSession session = new StageProgressionSession(
                CreateProgress(),
                CreateVictoryBattle,
                opponentSelectionGenerator: enableOpponentSelection
                    ? CreateOpponentGenerator()
                    : null,
                usesBattleRewards: false);
            return new FormalRunSession(session, new ShopOfferGenerator(20260730));
        }

        private static RunProgress CreateProgress()
        {
            return new RunProgress(
                new[]
                {
                    StageDefinition.CreateForEnemyProfile(
                        "normal-1", "Ash Gate", StageKind.NormalCombat,
                        EnemyCombatProfileCatalog.GunslingerKey, 10, 11),
                    StageDefinition.CreateForEnemyProfile(
                        "normal-2", "Blood Hall", StageKind.NormalCombat,
                        EnemyCombatProfileCatalog.EnforcerKey, 20, 21),
                    StageDefinition.CreateForEnemyProfile(
                        "boss", "Black Throne", StageKind.FinalBossCombat,
                        EnemyCombatProfileCatalog.FinalBossKey, 30, 31)
                },
                new PlayerRunState(
                    12,
                    12,
                    new[]
                    {
                        new RunCardDefinition(0, 10),
                        new RunCardDefinition(1, 1),
                        new RunCardDefinition(2, 10),
                        new RunCardDefinition(3, 1)
                    }));
        }

        private static OpponentSelectionGenerator CreateOpponentGenerator()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;
            return new OpponentSelectionGenerator(
                new EnemyCombatProfileCatalog(new[]
                {
                    catalog.GetByKey(EnemyCombatProfileCatalog.GunslingerKey),
                    catalog.GetByKey(EnemyCombatProfileCatalog.CultistKey)
                }),
                20260730,
                0);
        }

        private static CoreLoopBattle CreateVictoryBattle(
            StageDefinition stage,
            PlayerRunState player)
        {
            return new CoreLoopBattle(
                CreateDeck(40, 10, 1),
                CreateDeck(40, 10, 10),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul,
                new SimpleEnemyPolicy());
        }

        private static BlackjackDeck CreateDeck(
            int count,
            int firstRank,
            int secondRank)
        {
            var cards = new List<BlackjackCard>(count);
            for (int index = 0; index < count; index++)
            {
                cards.Add(new BlackjackCard(
                    index,
                    index % 2 == 0 ? firstRank : secondRank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static void WinCurrentBattle(
            StageProgressionSession session,
            StageProgressionState expectedState = StageProgressionState.StageCleared)
        {
            for (int action = 0;
                action < 40 && session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(session.TryPlayerStand(), Is.True);
            }

            Assert.That(session.Progress.State, Is.EqualTo(expectedState));
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            Type type = target?.GetType() ?? typeof(StageProgressionRuntime);
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic);
            property.GetSetMethod(true).Invoke(target, new[] { value });
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }
    }
}
