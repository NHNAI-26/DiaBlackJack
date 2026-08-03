using System.Collections.Generic;
using System.Linq;
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
        private const string HoverTooltipPrefabPath =
            "Assets/03. Prefabs/UI/CardHoverTooltip.prefab";
        private const string HudPrefabPath =
            "Assets/03. Prefabs/UI/HUD.prefab";

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

                Click(first);
                Assert.That(confirmationCount, Is.Zero);
                Assert.That(first.IsSelected, Is.True);
                Assert.That(second.IsSelected, Is.False);
                Assert.That(preview.ConfirmButtonInteractable, Is.True);

                Click(second);
                Assert.That(first.IsSelected, Is.False);
                Assert.That(second.IsSelected, Is.True);
                Assert.That(confirmationCount, Is.Zero);

                confirm.onClick.Invoke();
                confirm.onClick.Invoke();

                Assert.That(confirmedCardId, Is.EqualTo(202));
                Assert.That(confirmationCount, Is.EqualTo(1));
                Assert.That(preview.ConfirmButtonInteractable, Is.False);
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
            Assert.That(
                leaveButton.GetComponent<Image>().sprite.name,
                Is.EqualTo("Brush_UI_9"));
            Assert.That(
                leaveButton.Find("Label"),
                Is.Not.Null);
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
