using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.UI;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneDeckPreviewTests
    {
        private const string PreviewPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/DeckPreviewOverlay.prefab";
        private const string CardSlotPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/DeckPreviewCard.prefab";
        private const string CombatCardPrefabPath =
            "Assets/03. Prefabs/Card/Card.prefab";
        private const string CardCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
        private const string HoverOutlineMaterialPath =
            "Assets/05. Arts/Material/Card/UI_DeckCardHoverOutline.mat";
        private const string ConfirmButtonMaterialPath =
            "Assets/05. Arts/Material/Card/UI_Brush_Red_Confirm.mat";
        private const string HoverTooltipPrefabPath =
            "Assets/03. Prefabs/UI/CardHoverTooltip.prefab";
        private const string HudPrefabPath =
            "Assets/03. Prefabs/UI/HUD.prefab";
        private const string DefaultButtonPrefabPath =
            "Assets/03. Prefabs/UI/DefaultButton.prefab";
        private const string GameScenePath =
            "Assets/00. Scenes/GameScene.unity";

        [Test]
        public void GSV03_U03_DeckPreviewKeepsAllCardsInScrollableModelAndClearsOnClose()
        {
            GameObject previewObject = new GameObject("Deck Preview Test View");
            previewObject.SetActive(false);
            DeckPreviewView preview = previewObject.AddComponent<DeckPreviewView>();

            try
            {
                preview.Open(new GameSceneDeckViewModel(
                    DeckKind.Draw,
                    "뽑을 카드",
                    CreateCards(21)));

                Assert.That(preview.IsOpen, Is.True);
                Assert.That(previewObject.activeSelf, Is.True);
                Assert.That(preview.CardCount, Is.EqualTo(21));
                Assert.That(preview.GroupCount, Is.EqualTo(1));
                Assert.That(preview.CardSlotCount, Is.Zero);

                preview.Close();

                Assert.That(preview.IsOpen, Is.False);
                Assert.That(previewObject.activeSelf, Is.False);
                Assert.That(preview.CardCount, Is.Zero);
                Assert.That(preview.GroupCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(previewObject);
            }
        }

        [Test]
        public void GSV03_U04_DeckPreviewKeepsEmptyPileOpen()
        {
            GameObject previewObject = new GameObject("Empty Deck Preview Test View");
            DeckPreviewView preview = previewObject.AddComponent<DeckPreviewView>();

            try
            {
                preview.Open(new GameSceneDeckViewModel(
                    DeckKind.Discard,
                    "버린 카드",
                    new List<GameSceneDeckCardGroupViewModel>().AsReadOnly()));

                Assert.That(preview.IsOpen, Is.True);
                Assert.That(preview.CardCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(previewObject);
            }
        }

        [Test]
        public void GSV03_U05_DeckPreviewPrefabAuthorsScrollableHundredCardPool()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            DeckPreviewView preview = prefab.GetComponent<DeckPreviewView>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.CardSlotCount, Is.EqualTo(100));
            Assert.That(
                prefab.GetComponentsInChildren<DeckPreviewCardView>(true).Length,
                Is.EqualTo(100));
            Assert.That(
                prefab.GetComponentsInChildren<Transform>(true)
                    .Count(transform => transform.name == "Count"),
                Is.EqualTo(100));
            Assert.That(
                prefab.GetComponentInChildren<ScrollRect>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<EventSystem>(true),
                Is.Null);
        }

        [Test]
        public void GSV03_U06_SharedHoverTooltipSortsAboveOverlaysWithoutRaycasts()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HoverTooltipPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Canvas canvas = prefab.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.EqualTo(200));
            Assert.That(
                prefab.GetComponentInChildren<GraphicRaycaster>(true),
                Is.Null);
            Assert.That(
                prefab.GetComponentsInChildren<Graphic>(true)
                    .All(graphic => !graphic.raycastTarget),
                Is.True);
        }

        [Test]
        public void GSV03_U07_DeckHoverBadgeUsesRightEdgeAndDeckSpecificOffset()
        {
            GameObject cardObject = new GameObject(
                "Hover Badge Position Test",
                typeof(RectTransform));
            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 200f);

            try
            {
                rect.position = new Vector3(200f, 300f, 0f);
                Vector2 deckOffset = new Vector2(17f, -9f);
                CardHoverBadgeRequest request =
                    CardHoverBadgeRequest.CreateForDeckRect(
                        rect,
                        "Title",
                        "Body",
                        deckOffset);

                Assert.That(request.ShowBelow, Is.False);
                Assert.That(
                    request.ScreenPosition.x,
                    Is.EqualTo(267f).Within(0.001f));
                Assert.That(
                    request.ScreenPosition.y,
                    Is.EqualTo(291f).Within(0.001f));
                Assert.That(request.TooltipPivot, Is.EqualTo(new Vector2(0f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        [Category("GSV03")]
        public void GSV03_U08_DeckHoverBadgeUsesLeftEdgeAndMirroredOffset()
        {
            GameObject cardObject = new GameObject(
                "Left Hover Badge Position Test",
                typeof(RectTransform));
            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 200f);

            try
            {
                rect.position = new Vector3(200f, 300f, 0f);
                Vector2 deckOffset = new Vector2(17f, -9f);
                CardHoverBadgeRequest request =
                    CardHoverBadgeRequest.CreateForDeckRect(
                        rect,
                        "Title",
                        "Body",
                        deckOffset,
                        showOnLeft: true);

                Assert.That(request.ShowBelow, Is.False);
                Assert.That(
                    request.ScreenPosition.x,
                    Is.EqualTo(133f).Within(0.001f));
                Assert.That(
                    request.ScreenPosition.y,
                    Is.EqualTo(291f).Within(0.001f));
                Assert.That(
                    request.TooltipPivot,
                    Is.EqualTo(new Vector2(1f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        [Category("GSV03")]
        public void GSV03_U09_DeckColumnsThreeAndFourChooseOppositeTooltipSides()
        {
            GameObject instance = InstantiatePreviewPrefab();
            DeckPreviewView preview = instance.GetComponent<DeckPreviewView>();
            GameObject combatCardPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CombatCardPrefabPath);
            preview.Configure(combatCardPrefab.GetComponent<CardView>());
            CardHoverBadgeRequest latestRequest = null;
            preview.HoverBadgeRequested += request => latestRequest = request;

            try
            {
                preview.Open(CreateHoverBoundaryCards());
                DeckPreviewCardView third = FindSlot(instance, "CardSlot_03");
                DeckPreviewCardView fourth = FindSlot(instance, "CardSlot_04");

                third.OnPointerEnter(null);
                Assert.That(latestRequest, Is.Not.Null);
                Assert.That(
                    latestRequest.TooltipPivot,
                    Is.EqualTo(new Vector2(0f, 0.5f)));
                third.OnPointerExit(null);

                fourth.OnPointerEnter(null);
                Assert.That(latestRequest, Is.Not.Null);
                Assert.That(
                    latestRequest.TooltipPivot,
                    Is.EqualTo(new Vector2(1f, 0.5f)));
                fourth.OnPointerExit(null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV08")]
        public void GSV08_U01_SingleSelectionWaitsForConfirmAndSwitchesHighlight()
        {
            GameObject instance = InstantiatePreviewPrefab();
            DeckPreviewView preview = instance.GetComponent<DeckPreviewView>();
            GameObject combatCardPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CombatCardPrefabPath);
            preview.Configure(combatCardPrefab.GetComponent<CardView>());
            int confirmedCardId = -1;
            int confirmationCount = 0;
            preview.SelectionConfirmed += cardId =>
            {
                confirmedCardId = cardId;
                confirmationCount++;
            };

            try
            {
                preview.OpenForSingleSelection(CreateSelectionCards());
                DeckPreviewCardView first = FindSlot(instance, "CardSlot_01");
                DeckPreviewCardView second = FindSlot(instance, "CardSlot_02");
                Button confirm = instance.transform
                    .Find("Panel/SelectionFooter")
                    .GetComponent<Button>();

                Assert.That(preview.ConfirmButtonInteractable, Is.False);
                Assert.That(preview.ConfirmButtonAlpha, Is.EqualTo(0.5f));

                Vector3 firstRestingScale = first.transform.localScale;
                first.OnPointerEnter(null);
                Click(first);
                first.OnPointerExit(null);
                Assert.That(confirmationCount, Is.Zero);
                Assert.That(first.IsSelected, Is.True);
                Assert.That(first.IsVisuallyEmphasized, Is.True);
                Assert.That(first.transform.localScale.x,
                    Is.GreaterThan(firstRestingScale.x));
                Assert.That(first.CurrentHoverOutlineVisibility, Is.EqualTo(1f));
                Assert.That(second.IsSelected, Is.False);
                Assert.That(preview.ConfirmButtonInteractable, Is.True);
                Assert.That(preview.ConfirmButtonAlpha, Is.EqualTo(1f));

                Click(second);
                Assert.That(first.IsSelected, Is.False);
                Assert.That(first.IsVisuallyEmphasized, Is.False);
                Assert.That(first.transform.localScale, Is.EqualTo(firstRestingScale));
                Assert.That(first.CurrentHoverOutlineVisibility, Is.Zero);
                Assert.That(second.IsSelected, Is.True);
                Assert.That(second.IsVisuallyEmphasized, Is.True);
                Assert.That(second.CurrentHoverOutlineVisibility, Is.EqualTo(1f));
                Assert.That(confirmationCount, Is.Zero);

                confirm.onClick.Invoke();
                confirm.onClick.Invoke();

                Assert.That(confirmedCardId, Is.EqualTo(202));
                Assert.That(confirmationCount, Is.EqualTo(1));
                Assert.That(preview.ConfirmButtonInteractable, Is.False);
                Assert.That(preview.ConfirmButtonAlpha, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV08")]
        public void GSV08_U02_SingleSelectionCancelAndUnavailableCardDoNotConfirm()
        {
            GameObject instance = InstantiatePreviewPrefab();
            DeckPreviewView preview = instance.GetComponent<DeckPreviewView>();
            int confirmationCount = 0;
            int cancellationCount = 0;
            preview.SelectionConfirmed += _ => confirmationCount++;
            preview.SelectionCancelled += () => cancellationCount++;

            try
            {
                preview.OpenForSingleSelection(CreateSelectionCards());
                DeckPreviewCardView unavailable =
                    FindSlot(instance, "CardSlot_03");
                unavailable.OnPointerEnter(null);
                Click(unavailable);
                unavailable.OnPointerExit(null);

                Assert.That(unavailable.CanSelect, Is.False);
                Assert.That(preview.HasSelection, Is.False);
                Assert.That(unavailable.IsVisuallyEmphasized, Is.False);
                Assert.That(preview.ConfirmButtonInteractable, Is.False);

                Button close = instance.transform
                    .Find("Panel/CloseButton")
                    .GetComponent<Button>();
                close.onClick.Invoke();

                Assert.That(confirmationCount, Is.Zero);
                Assert.That(cancellationCount, Is.EqualTo(1));
                Assert.That(preview.IsOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV08")]
        public void GSV08_U03_PrefabsPreserveCameraBloomAndBrushShopLeaveControl()
        {
            GameObject previewPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
            GameObject cardSlotPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CardSlotPrefabPath);
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);

            Assert.That(previewPrefab, Is.Not.Null);
            Assert.That(
                previewPrefab.transform.Find("Panel/SelectionFooter"),
                Is.Not.Null);
            Assert.That(cardSlotPrefab, Is.Not.Null);
            Assert.That(cardSlotPrefab.transform.Find("SelectedFrame"), Is.Null);

            Assert.That(hudPrefab, Is.Not.Null);
            Canvas previewCanvas = previewPrefab.GetComponent<Canvas>();
            Canvas hudCanvas = hudPrefab.GetComponent<Canvas>();
            SerializedObject serializedPreviewCanvas =
                new SerializedObject(previewCanvas);
            Assert.That(
                serializedPreviewCanvas.FindProperty("m_RenderMode").intValue,
                Is.EqualTo((int)RenderMode.ScreenSpaceCamera));
            Assert.That(previewCanvas.sortingOrder, Is.EqualTo(100));
            Assert.That(hudCanvas.renderMode,
                Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(
                GameManager.ShouldShowShopPriceBadges(
                    shopOpen: true,
                    deckPreviewOpen: true,
                    shopUtilityAnimationPlaying: false),
                Is.False);
            Assert.That(
                GameManager.ShouldShowShopPriceBadges(
                    shopOpen: true,
                    deckPreviewOpen: false,
                    shopUtilityAnimationPlaying: true),
                Is.False);
            Assert.That(
                GameManager.ShouldShowShopPriceBadges(
                    shopOpen: true,
                    deckPreviewOpen: false,
                    shopUtilityAnimationPlaying: false),
                Is.True);
            Transform leaveRoot = hudPrefab.transform.Find("ShopLeaveRoot");
            Assert.That(leaveRoot, Is.Not.Null);
            Canvas leaveCanvas = leaveRoot.GetComponent<Canvas>();
            Assert.That(leaveCanvas.overrideSorting, Is.True);
            Assert.That(leaveCanvas.sortingOrder, Is.GreaterThan(100));
            Transform leaveButton = leaveRoot.Find("ShopLeaveButton");
            GameObject defaultButtonPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultButtonPrefabPath);
            Assert.That(
                leaveButton.GetComponent<Image>().sprite,
                Is.SameAs(defaultButtonPrefab.GetComponent<Image>().sprite));
            Assert.That(
                leaveButton.Find("Label"),
                Is.Not.Null);
        }

        [Test]
        [Category("GSV08")]
        public void GSV08_U04_GameSceneUsesCameraDeckPreviewForBloom()
        {
            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(
                    GameScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                DeckPreviewView preview = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<DeckPreviewView>(true))
                    .Single();
                Canvas previewCanvas = preview.GetComponent<Canvas>();

                Assert.That(previewCanvas.renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceCamera));
                Assert.That(previewCanvas.sortingOrder, Is.EqualTo(100));
                Assert.That(previewCanvas.worldCamera, Is.Not.Null);
            }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        [Category("GSV12")]
        public void GSV12_U01_ShopKeepsPlayerDeckAndHidesCombatCards()
        {
            GameManager manager = CreateBattleCardVisibilityFixture(
                out GameObject root,
                out GameObject[] battleCardObjects);

            try
            {
                manager.SetShopCardObjectsVisible();

                AssertShopCardVisibility(battleCardObjects);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSV12")]
        public void GSV12_U02_NextCombatRestoresBothHandsAndAllFourDeckPiles()
        {
            GameManager manager = CreateBattleCardVisibilityFixture(
                out GameObject root,
                out GameObject[] battleCardObjects);

            try
            {
                manager.SetShopCardObjectsVisible();
                manager.SetBattleCardObjectsVisible(true);

                Assert.That(
                    battleCardObjects.All(cardObject => cardObject.activeSelf),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSV12")]
        public void GSV12_U03_LighterSelectionAndShopOffersRemainVisible()
        {
            GameManager manager = CreateBattleCardVisibilityFixture(
                out GameObject root,
                out GameObject[] battleCardObjects);
            GameObject previewObject = InstantiatePreviewPrefab();
            previewObject.transform.SetParent(root.transform);
            DeckPreviewView preview = previewObject.GetComponent<DeckPreviewView>();
            GameObject shopOffers = new GameObject("Shop Offers");
            shopOffers.transform.SetParent(root.transform);
            SetReference(manager, "deckPreview", preview);

            try
            {
                preview.OpenForSingleSelection(CreateSelectionCards());
                manager.SetShopCardObjectsVisible();

                Assert.That(preview.IsOpen, Is.True);
                Assert.That(previewObject.activeSelf, Is.True);
                Assert.That(shopOffers.activeSelf, Is.True);
                AssertShopCardVisibility(battleCardObjects);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSV12")]
        public void GSV12_U04_ShopDebuggerUsesProductionVisibilityBoundary()
        {
            GameManager manager = CreateBattleCardVisibilityFixture(
                out GameObject root,
                out GameObject[] battleCardObjects);
            GameObject shopObject = new GameObject("Shop");
            shopObject.transform.SetParent(root.transform);
            ShopController shop = shopObject.AddComponent<ShopController>();
            SetReference(manager, "shop", shop);

            try
            {
                Assert.That(manager.DebugOpenStandaloneShop(), Is.True);
                Assert.That(shop.IsOpen, Is.True);
                AssertShopCardVisibility(battleCardObjects);

                Assert.That(manager.DebugCloseStandaloneShop(), Is.True);
                Assert.That(shop.IsOpen, Is.False);
                Assert.That(
                    battleCardObjects.All(cardObject => cardObject.activeSelf),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSV12")]
        public void GSV12_U05_ShopOwnedDeckPreviewContainsEveryOwnedCard()
        {
            var ownedCards = new List<ShopOwnedCardViewModel>
            {
                new ShopOwnedCardViewModel(
                    101,
                    "standard-plain-2",
                    2,
                    "Two",
                    "Ability A",
                    CardSuit.Spade,
                    canRemove: true),
                new ShopOwnedCardViewModel(
                    102,
                    "standard-plain-2",
                    2,
                    "Two",
                    "Ability B",
                    CardSuit.Clover,
                    canRemove: true)
            };

            GameSceneDeckViewModel model =
                GameManager.CreateShopOwnedDeckPreview(ownedCards);

            Assert.That(model.Kind, Is.EqualTo(DeckKind.Draw));
            Assert.That(model.Title, Is.EqualTo("MY DECK"));
            Assert.That(model.CardCount, Is.EqualTo(2));
            Assert.That(model.GroupCount, Is.EqualTo(2));
            Assert.That(model.CardGroups[0].Card.CardId, Is.EqualTo(101));
            Assert.That(model.CardGroups[1].Card.CardId, Is.EqualTo(102));
        }

        private static void AssertShopCardVisibility(
            GameObject[] battleCardObjects)
        {
            Assert.That(battleCardObjects[0].activeSelf, Is.False);
            Assert.That(battleCardObjects[1].activeSelf, Is.False);
            Assert.That(battleCardObjects[2].activeSelf, Is.True);
            Assert.That(battleCardObjects[3].activeSelf, Is.True);
            Assert.That(battleCardObjects[4].activeSelf, Is.False);
            Assert.That(battleCardObjects[5].activeSelf, Is.False);
        }

        [Test]
        public void GSV13_U01_DeckCardHoverOutlineMatchesCombatStateColors()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardSlotPrefabPath);
            GameObject combatCard = AssetDatabase.LoadAssetAtPath<GameObject>(
                CombatCardPrefabPath);
            CardContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(CardCatalogPath);
            SerializedObject combatView = new SerializedObject(
                combatCard.GetComponent<CardView>());
            GameObject instance = Object.Instantiate(source);
            DeckPreviewCardView slot = instance.GetComponent<DeckPreviewCardView>();

            try
            {
                AssertOutlineState(
                    slot,
                    catalog,
                    CreateCardModel("standard-plain-2", 2, false, false),
                    combatView,
                    "basicHoverOutlineColor");
                AssertOutlineState(
                    slot,
                    catalog,
                    CreateCardModel("crystal-orb-5", 5, false, false),
                    combatView,
                    "unavailableHoverOutlineColor");
                AssertOutlineState(
                    slot,
                    catalog,
                    CreateCardModel("crystal-orb-5", 5, true, false),
                    combatView,
                    "availableHoverOutlineColor");
                AssertOutlineState(
                    slot,
                    catalog,
                    CreateCardModel(CardDefinitionCatalog.PoisonKey, 1, false, false),
                    combatView,
                    "automaticHoverOutlineColor");
                AssertOutlineState(
                    slot,
                    catalog,
                    CreateCardModel("crystal-orb-5", 5, false, true),
                    combatView,
                    "usedHoverOutlineColor");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV13_U02_HoverOutlineVisibilityIsIsolatedPerCardSlot()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardSlotPrefabPath);
            CardContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(CardCatalogPath);
            Sprite sprite = catalog.GetNormalFaceSprite(
                "standard-plain-2",
                CardSuit.Spade);
            GameObject firstObject = Object.Instantiate(source);
            GameObject secondObject = Object.Instantiate(source);
            DeckPreviewCardView first =
                firstObject.GetComponent<DeckPreviewCardView>();
            DeckPreviewCardView second =
                secondObject.GetComponent<DeckPreviewCardView>();
            GameSceneDeckCardGroupViewModel group =
                new GameSceneDeckCardGroupViewModel(
                    CreateCardModel("standard-plain-2", 2, false, false),
                    1);

            try
            {
                first.Render(group, sprite);
                second.Render(group, sprite);

                first.OnPointerEnter(null);

                Assert.That(first.CurrentHoverOutlineVisibility, Is.EqualTo(1f));
                Assert.That(second.CurrentHoverOutlineVisibility, Is.Zero);
                Assert.That(
                    GetReference<Image>(first, "faceImage").material,
                    Is.Not.SameAs(GetReference<Image>(second, "faceImage").material));

                first.OnPointerExit(null);

                Assert.That(first.CurrentHoverOutlineVisibility, Is.Zero);
                Assert.That(second.CurrentHoverOutlineVisibility, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void GSV13_U03_DeckPreviewPrefabsSerializeOutlineAndConfirmVisuals()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardSlotPrefabPath);
            GameObject previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PreviewPrefabPath);
            Material outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                HoverOutlineMaterialPath);
            Material confirmMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                ConfirmButtonMaterialPath);
            DeckPreviewCardView card = cardPrefab.GetComponent<DeckPreviewCardView>();
            DeckPreviewView preview = previewPrefab.GetComponent<DeckPreviewView>();
            Transform footer = previewPrefab.transform.Find("Panel/SelectionFooter");
            Button confirm = footer.GetComponent<Button>();
            CanvasGroup confirmGroup = footer.GetComponent<CanvasGroup>();

            Assert.That(outlineMaterial, Is.Not.Null);
            Assert.That(outlineMaterial.IsKeywordEnabled("_PIXEL_OUTLINE_ON"), Is.True);
            Assert.That(outlineMaterial.GetFloat("_UseUIAlphaClip"), Is.Zero);
            Assert.That(
                GetReference<Material>(card, "hoverOutlineMaterial"),
                Is.SameAs(outlineMaterial));
            Assert.That(cardPrefab.transform.Find("HoverFrame"), Is.Null);

            Assert.That(confirmMaterial, Is.Not.Null);
            Assert.That(confirmMaterial.GetFloat("_UseUIAlphaClip"), Is.Zero);
            Assert.That(confirmMaterial.GetFloat("_RespectVertexRgbTint"), Is.EqualTo(1f));
            Assert.That(footer.GetComponent<Image>().material, Is.SameAs(confirmMaterial));
            Assert.That(confirmGroup, Is.Not.Null);
            Assert.That(confirmGroup.alpha, Is.EqualTo(0.5f));
            Assert.That(
                GetReference<CanvasGroup>(preview, "confirmButtonGroup"),
                Is.SameAs(confirmGroup));
            Assert.That(confirm.colors.normalColor, Is.EqualTo(Color.white));
            Assert.That(
                confirm.colors.highlightedColor,
                Is.EqualTo(new Color(0.78f, 0.78f, 0.78f, 1f)));
            Assert.That(
                confirm.colors.pressedColor,
                Is.EqualTo(new Color(0.65f, 0.65f, 0.65f, 1f)));
            Assert.That(confirm.colors.disabledColor, Is.EqualTo(Color.white));
        }

        [Test]
        public void GSV14_U01_DeckHoverMatchesCombatFeelAndKeepsGlowOutsideSprite()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardSlotPrefabPath);
            GameObject combatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CombatCardPrefabPath);
            Material outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                HoverOutlineMaterialPath);
            SerializedObject deckData = new SerializedObject(
                cardPrefab.GetComponent<DeckPreviewCardView>());
            SerializedObject combatData = new SerializedObject(
                combatPrefab.GetComponent<CardView>());

            Assert.That(
                deckData.FindProperty("hoverScale").floatValue,
                Is.EqualTo(combatData.FindProperty("hoverScale").floatValue));
            Assert.That(
                deckData.FindProperty("hoverScaleDuration").floatValue,
                Is.EqualTo(combatData.FindProperty("hoverScaleDuration").floatValue));
            Assert.That(
                deckData.FindProperty("hoverSfxId").stringValue,
                Is.EqualTo(combatData.FindProperty("hoverSfxId").stringValue));
            AnimationCurve deckCurve =
                deckData.FindProperty("hoverScaleCurve").animationCurveValue;
            AnimationCurve combatCurve =
                combatData.FindProperty("hoverScaleCurve").animationCurveValue;
            Assert.That(deckCurve.keys, Is.EqualTo(combatCurve.keys));
            Assert.That(outlineMaterial.GetFloat("_PixelOutlineGlowWidth"),
                Is.EqualTo(4f));
            Assert.That(outlineMaterial.GetFloat("_PixelOutlineGlowAlpha"),
                Is.EqualTo(0.35f));

            CardContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(CardCatalogPath);
            Sprite sprite = catalog.GetNormalFaceSprite(
                "standard-plain-2",
                CardSuit.Spade);
            GameObject instance = Object.Instantiate(cardPrefab);
            DeckPreviewCardView card = instance.GetComponent<DeckPreviewCardView>();
            Vector3 restingScale = instance.transform.localScale;
            try
            {
                card.Render(
                    new GameSceneDeckCardGroupViewModel(
                        CreateCardModel("standard-plain-2", 2, false, false),
                        1),
                    sprite);

                Vector4 padding = card.CurrentHoverOutlineMeshPadding;
                Assert.That(padding.x, Is.GreaterThan(0f));
                Assert.That(padding.y, Is.GreaterThan(0f));
                Assert.That(padding.z, Is.GreaterThan(0f));
                Assert.That(padding.w, Is.GreaterThan(0f));

                card.OnPointerEnter(null);
                Assert.That(instance.transform.localScale,
                    Is.EqualTo(restingScale * 1.02f));
                card.OnPointerExit(null);
                Assert.That(instance.transform.localScale,
                    Is.EqualTo(restingScale));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV09_U01_DefaultButtonPrefabAuthorsSharedVisualStyle()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultButtonPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            RectTransform rect = prefab.GetComponent<RectTransform>();
            Image image = prefab.GetComponent<Image>();
            Button button = prefab.GetComponent<Button>();
            Transform labelTransform = prefab.transform.Find("Label");
            Component label = labelTransform.GetComponents<Component>()
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            SerializedObject serializedLabel = new SerializedObject(label);

            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(234f, 66f)));
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(button.targetGraphic, Is.SameAs(image));
            // Hover/press feedback is an animated scale (UIButtonScaleFeedback), not a
            // ColorTint transition — see the "기본 버튼 호버 효과" rework.
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.None));
            Assert.That(
                prefab.GetComponent<UIButtonScaleFeedback>(),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponent<UISelectableSoundHook>(),
                Is.Not.Null);
            Assert.That(label, Is.Not.Null);
            Assert.That(
                serializedLabel.FindProperty("m_text").stringValue,
                Is.EqualTo("버튼"));
            Assert.That(
                serializedLabel.FindProperty("m_fontAsset")
                    .objectReferenceValue.name,
                Is.EqualTo("전주완판본 순R SDF"));
            Assert.That(
                serializedLabel.FindProperty("m_fontSize").floatValue,
                Is.EqualTo(28f));
            Assert.That(
                serializedLabel.FindProperty("m_enableAutoSizing").boolValue,
                Is.True);
            Assert.That(
                serializedLabel.FindProperty("m_fontStyle").intValue,
                Is.EqualTo(1));
        }

        [Test]
        public void UIFX01_U01_ButtonScaleFeedbackCentersPivotWithoutVisualMovement()
        {
            GameObject buttonObject = new GameObject(
                "Scale Feedback Position Test",
                typeof(RectTransform));
            try
            {
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(420f, 60f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(321f, -123f);
                Vector3[] originalCorners = new Vector3[4];
                rect.GetWorldCorners(originalCorners);
                UIButtonScaleFeedback.CenterPivotWithoutMovingVisuals(rect);

                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Vector3[] centeredCorners = new Vector3[4];
                rect.GetWorldCorners(centeredCorners);
                for (int i = 0; i < originalCorners.Length; i++)
                {
                    Assert.That(
                        Vector3.Distance(centeredCorners[i], originalCorners[i]),
                        Is.LessThan(0.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void UIFX01_U02_DynamicButtonLayoutResynchronizesCenteredPivot()
        {
            GameObject buttonObject = new GameObject(
                "Dynamic Scale Feedback Position Test",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(UIButtonScaleFeedback));
            try
            {
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                UIButtonScaleFeedback feedback =
                    buttonObject.GetComponent<UIButtonScaleFeedback>();
                rect.sizeDelta = new Vector2(380f, 64f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-48f, 48f);
                Vector3 worldCenter = rect.TransformPoint(rect.rect.center);

                feedback.SynchronizeRestingGeometry();

                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(
                    Vector3.Distance(
                        rect.TransformPoint(rect.rect.center),
                        worldCenter),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void UIFX01_U03_ActiveHudChoiceBlocksItsScreenRectangle()
        {
            GameObject buttonObject = new GameObject(
                "HUD Choice Pointer Priority Test",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(GameHudChoiceButton));
            try
            {
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, 80f);
                rect.position = new Vector3(320f, 180f, 0f);
                GameHudChoiceButton choice =
                    buttonObject.GetComponent<GameHudChoiceButton>();

                Assert.That(
                    choice.ContainsScreenPoint(new Vector2(320f, 180f), null),
                    Is.True);
                Assert.That(
                    choice.ContainsScreenPoint(new Vector2(600f, 400f), null),
                    Is.False);

                buttonObject.SetActive(false);
                Assert.That(
                    choice.ContainsScreenPoint(new Vector2(320f, 180f), null),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void UIFX01_U02_AllAuthoredButtonPrefabsUseSharedClickSound()
        {
            string[] prefabPaths =
            {
                DefaultButtonPrefabPath,
                "Assets/03. Prefabs/UI/PauseSettingsCanvas.prefab",
                "Assets/03. Prefabs/UI/GameScene/CodexOverlay.prefab",
                "Assets/03. Prefabs/UI/GameScene/RevolverNumberSelector.prefab",
                "Assets/03. Prefabs/UI/GameScene/DeckPreviewOverlay.prefab",
                "Assets/06.Packages/Demo/UI/Prefabs/GenericButton.prefab",
            };

            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
                Assert.That(buttons, Is.Not.Empty, prefabPath);
                foreach (Button button in buttons)
                {
                    Assert.That(
                        button.GetComponent<UISelectableSoundHook>(),
                        Is.Not.Null,
                        prefabPath + ": " + button.name);
                }
            }
        }

        [Test]
        public void GSV09_U02_ShopLeaveUsesNestedDefaultButtonPrefab()
        {
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Transform leaveRoot = hudPrefab.transform.Find("ShopLeaveRoot");
            Transform leaveButton = leaveRoot.Find("ShopLeaveButton");
            GameObject source =
                PrefabUtility.GetCorrespondingObjectFromSource(leaveButton.gameObject);
            Component label = leaveButton.Find("Label").GetComponents<Component>()
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            SerializedObject serializedLabel = new SerializedObject(label);
            Button button = leaveButton.GetComponent<Button>();
            RectTransform buttonRect = leaveButton.GetComponent<RectTransform>();
            Canvas leaveCanvas = leaveRoot.GetComponent<Canvas>();
            SerializedObject serializedHud =
                new SerializedObject(hudPrefab.GetComponent<GameHudView>());

            Assert.That(source, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(source),
                Is.EqualTo(DefaultButtonPrefabPath));
            Assert.That(
                serializedLabel.FindProperty("m_text").stringValue,
                Is.EqualTo("상점 나가기"));
            Assert.That(buttonRect.sizeDelta, Is.EqualTo(new Vector2(234f, 66f)));
            Assert.That(buttonRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 24f)));
            Assert.That(leaveCanvas.overrideSorting, Is.True);
            Assert.That(leaveCanvas.sortingOrder, Is.EqualTo(150));
            Assert.That(
                serializedHud.FindProperty("shopLeaveRoot").objectReferenceValue,
                Is.SameAs(leaveRoot.gameObject));
            Assert.That(
                serializedHud.FindProperty("shopLeaveButton").objectReferenceValue,
                Is.SameAs(button));
        }

        [Test]
        public void GSV09_U04_CombatOptionsUseNestedDefaultButtonPrefab()
        {
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameHudChoiceButton[] choices =
                hudPrefab.GetComponentsInChildren<GameHudChoiceButton>(true);

            Assert.That(choices, Has.Length.EqualTo(100));
            foreach (GameHudChoiceButton choice in choices)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(
                    choice.gameObject);
                Assert.That(source, Is.Not.Null, choice.name);
                Assert.That(
                    AssetDatabase.GetAssetPath(source),
                    Is.EqualTo(DefaultButtonPrefabPath),
                    choice.name);
            }
        }

        [Test]
        public void GSV09_U03_ShopLeaveClickRespectsInteractableAndHideState()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            GameHudView hud = instance.GetComponent<GameHudView>();
            Button button = instance.transform
                .Find("ShopLeaveRoot/ShopLeaveButton")
                .GetComponent<Button>();
            int requestedCount = 0;
            hud.ShopLeaveRequested += () => requestedCount++;
            MethodInfo bind = typeof(GameHudView).GetMethod(
                "BindShopLeaveControl",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bind, Is.Not.Null);
            bind.Invoke(hud, null);
            PointerEventData click = new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left,
            };
            hud.SetShopLeaveState(visible: false, interactable: false);

            try
            {
                Assert.That(hud.IsShopLeaveVisible, Is.False);
                Assert.That(hud.IsShopLeaveInteractable, Is.False);
                button.OnPointerClick(click);
                Assert.That(requestedCount, Is.Zero);

                hud.SetShopLeaveState(visible: true, interactable: false);
                button.OnPointerClick(click);
                Assert.That(hud.IsShopLeaveVisible, Is.True);
                Assert.That(hud.IsShopLeaveInteractable, Is.False);
                Assert.That(requestedCount, Is.Zero);

                hud.SetShopLeaveState(visible: true, interactable: true);
                button.OnPointerClick(click);
                Assert.That(requestedCount, Is.EqualTo(1));

                hud.SetShopLeaveState(visible: false, interactable: true);
                button.OnPointerClick(click);
                Assert.That(hud.IsShopLeaveVisible, Is.False);
                Assert.That(hud.IsShopLeaveInteractable, Is.False);
                Assert.That(requestedCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static GameObject InstantiatePreviewPrefab()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
            return Object.Instantiate(prefab);
        }

        private static GameManager CreateBattleCardVisibilityFixture(
            out GameObject root,
            out GameObject[] battleCardObjects)
        {
            root = new GameObject("Battle Card Visibility Test Root");
            GameManager manager = root.AddComponent<GameManager>();
            battleCardObjects = new[]
            {
                CreateCardObject<CardHand>(root.transform, "PlayerHand"),
                CreateCardObject<CardHand>(root.transform, "EnemyHand"),
                CreateCardObject<DeckStackView>(root.transform, "RemainingDeck"),
                CreateCardObject<DeckStackView>(root.transform, "DiscardDeck"),
                CreateCardObject<DeckStackView>(root.transform, "EnemyRemainingDeck"),
                CreateCardObject<DeckStackView>(root.transform, "EnemyDiscardDeck")
            };

            SetReference(
                manager,
                "playerHand",
                battleCardObjects[0].GetComponent<CardHand>());
            SetReference(
                manager,
                "enemyHand",
                battleCardObjects[1].GetComponent<CardHand>());
            SetReference(
                manager,
                "remainingDeck",
                battleCardObjects[2].GetComponent<DeckStackView>());
            SetReference(
                manager,
                "discardDeck",
                battleCardObjects[3].GetComponent<DeckStackView>());
            SetReference(
                manager,
                "enemyRemainingDeck",
                battleCardObjects[4].GetComponent<DeckStackView>());
            SetReference(
                manager,
                "enemyDiscardDeck",
                battleCardObjects[5].GetComponent<DeckStackView>());
            return manager;
        }

        private static GameObject CreateCardObject<T>(
            Transform parent,
            string name)
            where T : Component
        {
            GameObject cardObject = new GameObject(name);
            cardObject.transform.SetParent(parent);
            cardObject.AddComponent<T>();
            return cardObject;
        }

        private static DeckPreviewCardView FindSlot(
            GameObject root,
            string name)
        {
            return root.GetComponentsInChildren<DeckPreviewCardView>(true)
                .Single(slot => slot.name == name);
        }

        private static void Click(DeckPreviewCardView slot)
        {
            slot.OnPointerClick(new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left
            });
        }

        private static void AssertOutlineState(
            DeckPreviewCardView slot,
            CardContentCatalogSO catalog,
            GameSceneCardViewModel card,
            SerializedObject combatView,
            string colorPropertyName)
        {
            Sprite sprite = catalog.GetNormalFaceSprite(
                card.DefinitionKey,
                card.Suit);
            Assert.That(sprite, Is.Not.Null, card.DefinitionKey);
            slot.Render(
                new GameSceneDeckCardGroupViewModel(card, 1),
                sprite);

            Color expected = combatView.FindProperty(colorPropertyName).colorValue;
            Assert.That(slot.CurrentHoverOutlineColor, Is.EqualTo(expected));
            slot.OnPointerEnter(null);
            Assert.That(slot.CurrentHoverOutlineVisibility, Is.EqualTo(1f));
            slot.OnPointerExit(null);
            Assert.That(slot.CurrentHoverOutlineVisibility, Is.Zero);
        }

        private static GameSceneCardViewModel CreateCardModel(
            string definitionKey,
            int rank,
            bool canUse,
            bool isUsed)
        {
            return new GameSceneCardViewModel(
                cardId: rank,
                rank: rank,
                isFaceUp: true,
                revealRank: true,
                canUse: canUse,
                displayName: definitionKey,
                definitionKey: definitionKey,
                isUsed: isUsed);
        }

        private static T GetReference<T>(Object owner, string propertyName)
            where T : Object
        {
            SerializedObject serialized = new SerializedObject(owner);
            return serialized.FindProperty(propertyName).objectReferenceValue as T;
        }

        private static void SetReference(
            Object owner,
            string propertyName,
            Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameSceneDeckViewModel CreateSelectionCards()
        {
            var groups = new List<GameSceneDeckCardGroupViewModel>
            {
                CreateSelectionCard(101, 2, true),
                CreateSelectionCard(202, 3, true),
                CreateSelectionCard(303, 4, false)
            };
            return new GameSceneDeckViewModel(
                DeckKind.Draw,
                "제거할 카드 선택",
                groups.AsReadOnly());
        }

        private static GameSceneDeckViewModel CreateHoverBoundaryCards()
        {
            var groups = new List<GameSceneDeckCardGroupViewModel>
            {
                CreateSelectionCard(101, 1, true),
                CreateSelectionCard(202, 2, true),
                CreateSelectionCard(303, 3, true),
                CreateSelectionCard(404, 4, true)
            };
            return new GameSceneDeckViewModel(
                DeckKind.Draw,
                "Hover boundary cards",
                groups.AsReadOnly());
        }

        private static GameSceneDeckCardGroupViewModel CreateSelectionCard(
            int cardId,
            int rank,
            bool canUse)
        {
            return new GameSceneDeckCardGroupViewModel(
                new GameSceneCardViewModel(
                    cardId,
                    rank,
                    isFaceUp: true,
                    revealRank: true,
                    canUse,
                    "기본 카드",
                    definitionKey: $"standard-plain-{rank}"),
                1);
        }

        private static IReadOnlyList<GameSceneDeckCardGroupViewModel> CreateCards(
            int count)
        {
            var cards = new List<GameSceneDeckCardGroupViewModel>(1)
            {
                new GameSceneDeckCardGroupViewModel(
                    new GameSceneCardViewModel(
                        0,
                        2,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: false,
                        "기본 카드",
                        showHoverBadgeWhenUnavailable: true,
                        definitionKey: "standard-plain-2"),
                    count)
            };

            return cards.AsReadOnly();
        }
    }
}
