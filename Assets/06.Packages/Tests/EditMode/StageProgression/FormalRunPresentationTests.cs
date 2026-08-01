using System;
using System.Collections.Generic;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
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
            Assert.That(model.PlayerGold, Is.EqualTo("4 GOLD"));
            Assert.That(model.GoldResult, Is.EqualTo("VICTORY +4 GOLD"));
            Assert.That(model.ShopCardOptions.Count, Is.EqualTo(5));
            Assert.That(model.ShopOwnedCards.Count, Is.EqualTo(4));
            Assert.That(model.CanSelectReward, Is.False);
            Assert.That(model.CanSkipReward, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanLeaveShop, Is.True);
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

            Assert.That(after.PlayerGold, Is.EqualTo("1 GOLD"));
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
        public void RF04_P05_UsedFirstShopRaisesBothSecondShopPriceLabels()
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

            Assert.That(model.LighterLabel, Is.EqualTo("LIGHTER  3 GOLD"));
            Assert.That(model.WhiskeyLabel, Is.EqualTo("WHISKEY  3 GOLD"));
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
            Assert.That(model.GoldResult, Is.EqualTo("VICTORY +15 GOLD"));
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
                Assert.That(
                    demonHolder.GetComponentsInChildren<DemonCardView>(true),
                    Has.Length.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
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
                ShopController shop = CreateFormalShopController(root);
                shop.OpenFormal(before);
                Vector3[] firstPositions = GetActiveOfferPositions(root);

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
                Assert.That(statuses[0].PriceLabel, Is.EqualTo("돈 : 3"));
                Assert.That(CountSoldStatuses(statuses), Is.EqualTo(1));

                CardView[] normalCards = GetActiveComponents<CardView>(root);
                DemonCardView[] demonCards = GetActiveComponents<DemonCardView>(root);
                Assert.That(normalCards.Length + demonCards.Length, Is.EqualTo(5));
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

        private static ShopController CreateFormalShopController(GameObject root)
        {
            ShopController shop = root.AddComponent<ShopController>();
            Transform normalHolder = CreateChild(root, "Normal Holder").transform;
            Transform demonHolder = CreateChild(root, "Demon Holder").transform;
            CardView normalPrefab = CreateChild(root, "Normal Prefab")
                .AddComponent<CardView>();
            DemonCardView demonPrefab = CreateChild(root, "Demon Prefab")
                .AddComponent<DemonCardView>();
            GameObject statusObject = CreateChild(root, "Status Prefab");
            ShopCardOfferStatusView statusPrefab =
                statusObject.AddComponent<ShopCardOfferStatusView>();
            Type textType = Type.GetType(
                "TMPro.TextMeshPro, Unity.TextMeshPro");
            Assert.That(textType, Is.Not.Null);
            Component price = CreateChild(statusObject, "Price")
                .AddComponent(textType);
            Component sold = CreateChild(statusObject, "Sold")
                .AddComponent(textType);
            SetField(statusPrefab, "priceText", price);
            SetField(statusPrefab, "soldOutText", sold);
            SetField(shop, "normalCardHolder", normalHolder);
            SetField(shop, "demonCardHolder", demonHolder);
            SetField(shop, "normalCardPrefab", normalPrefab);
            SetField(shop, "demonCardPrefab", demonPrefab);
            SetField(shop, "cardOfferStatusPrefab", statusPrefab);
            return shop;
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

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }
    }
}
