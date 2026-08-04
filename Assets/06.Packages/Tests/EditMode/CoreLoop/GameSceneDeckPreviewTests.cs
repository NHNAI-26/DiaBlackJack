using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public void GSV08_U01_SingleSelectionWaitsForConfirmAndSwitchesHighlight()
        {
            GameObject instance = InstantiatePreviewPrefab();
            DeckPreviewView preview = instance.GetComponent<DeckPreviewView>();
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

                Click(first);
                Assert.That(confirmationCount, Is.Zero);
                Assert.That(first.IsSelected, Is.True);
                Assert.That(second.IsSelected, Is.False);
                Assert.That(preview.ConfirmButtonInteractable, Is.True);
                Assert.That(preview.ConfirmButtonAlpha, Is.EqualTo(1f));

                Click(second);
                Assert.That(first.IsSelected, Is.False);
                Assert.That(second.IsSelected, Is.True);
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
                Click(unavailable);

                Assert.That(unavailable.CanSelect, Is.False);
                Assert.That(preview.HasSelection, Is.False);
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
        public void GSV08_U03_PrefabsSerializeSelectionAndBrushShopLeaveControl()
        {
            GameObject previewPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);

            Assert.That(previewPrefab, Is.Not.Null);
            Assert.That(
                previewPrefab.transform.Find("Panel/SelectionFooter"),
                Is.Not.Null);
            Assert.That(
                previewPrefab.GetComponentsInChildren<DeckPreviewCardView>(true)
                    .All(slot => slot.transform.Find("SelectedFrame") != null),
                Is.True);

            Assert.That(hudPrefab, Is.Not.Null);
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
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
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
