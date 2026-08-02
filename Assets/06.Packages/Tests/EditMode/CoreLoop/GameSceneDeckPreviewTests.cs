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
