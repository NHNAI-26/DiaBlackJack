using System.Collections.Generic;
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

        [Test]
        public void GSV03_U03_DeckPreviewKeepsAllCardsInScrollableModelAndClearsOnClose()
        {
            GameObject previewObject = new GameObject("Deck Preview Test View");
            DeckPreviewView preview = previewObject.AddComponent<DeckPreviewView>();

            try
            {
                preview.Open(new GameSceneDeckViewModel(
                    DeckKind.Draw,
                    "뽑을 카드",
                    CreateCards(21)));

                Assert.That(preview.IsOpen, Is.True);
                Assert.That(preview.CardCount, Is.EqualTo(21));
                Assert.That(preview.CardSlotCount, Is.Zero);

                preview.Close();

                Assert.That(preview.IsOpen, Is.False);
                Assert.That(preview.CardCount, Is.Zero);
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
                    new List<GameSceneCardViewModel>().AsReadOnly()));

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
                prefab.GetComponentInChildren<ScrollRect>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<EventSystem>(true),
                Is.Null);
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreateCards(int count)
        {
            var cards = new List<GameSceneCardViewModel>(count);
            for (int i = 0; i < count; i++)
            {
                cards.Add(new GameSceneCardViewModel(
                    i,
                    2,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    "기본 카드",
                    showHoverBadgeWhenUnavailable: true,
                    definitionKey: "standard-plain-2"));
            }

            return cards.AsReadOnly();
        }
    }
}
