using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneCardSpriteTests
    {
        private static readonly int CardBlendTextureId =
            Shader.PropertyToID("_CardBlendTex");
        private static readonly int CardBlendAmountId =
            Shader.PropertyToID("_CardBlendAmount");

        private const string CardPrefabPath = "Assets/03. Prefabs/Card/Card.prefab";
        private const string DemonCardPrefabPath =
            "Assets/03. Prefabs/Card/DemonCard.prefab";
        private const string AutomaticArtRoot =
            "Assets/05. Arts/Texture/CardSprite/AutoCard/AutoCard_";
        private const string DemonArtRoot =
            "Assets/05. Arts/Texture/CardSprite/DevilCard/DevilCard_";

        [TestCase(CardDefinitionCatalog.ResurrectionHerbKey, 1)]
        [TestCase(CardDefinitionCatalog.PoisonKey, 2)]
        [TestCase(CardDefinitionCatalog.PocketWatchKey, 3)]
        [TestCase(CardDefinitionCatalog.LieDetectorKey, 4)]
        [TestCase(CardDefinitionCatalog.FlamethrowerKey, 5)]
        public void GSV02_U01_AutomaticDefinitionsUseAuthoredArtOrder(
            string definitionKey,
            int expectedIndex)
        {
            Assert.That(
                GameSceneCardVisualCatalog.AutomaticCardSpriteIndexFor(definitionKey),
                Is.EqualTo(expectedIndex));
        }

        [TestCase(DemonContractCatalog.SatanKey, 1)]
        [TestCase(DemonContractCatalog.MammonKey, 2)]
        [TestCase(DemonContractCatalog.LeviathanKey, 3)]
        [TestCase(DemonContractCatalog.BelphegorKey, 4)]
        [TestCase(DemonContractCatalog.BeelzebubKey, 5)]
        [TestCase(DemonContractCatalog.LuciferKey, 6)]
        [TestCase(DemonContractCatalog.AsmodeusKey, 7)]
        [TestCase(DemonContractCatalog.PaimonKey, 8)]
        [TestCase(DemonContractCatalog.BelialKey, 9)]
        [TestCase(DemonContractCatalog.AzazelKey, 10)]
        [TestCase(DemonContractCatalog.BaphometKey, 11)]
        [TestCase(DemonContractCatalog.MephistophelesKey, 12)]
        public void GSV02_U02_DemonDefinitionsUseAuthoredArtOrder(
            string definitionKey,
            int expectedIndex)
        {
            Assert.That(
                GameSceneCardVisualCatalog.DemonCardSpriteIndexFor(definitionKey),
                Is.EqualTo(expectedIndex));
        }

        [Test]
        public void GSV02_U03_CardPrefabBindsAllAutomaticSprites()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);
                SpriteRenderer renderer = GetFrontRenderer(view);
                string[] keys =
                {
                    CardDefinitionCatalog.ResurrectionHerbKey,
                    CardDefinitionCatalog.PoisonKey,
                    CardDefinitionCatalog.PocketWatchKey,
                    CardDefinitionCatalog.LieDetectorKey,
                    CardDefinitionCatalog.FlamethrowerKey,
                };

                foreach (string key in keys)
                {
                    CardDefinition definition = CardDefinitionCatalog.GetByKey(key);
                    int artIndex =
                        GameSceneCardVisualCatalog.AutomaticCardSpriteIndexFor(key);
                    Sprite expected = LoadAutomaticSprite(artIndex);
                    Assert.That(expected, Is.Not.Null, key);

                    view.Bind(new GameSceneCardViewModel(
                        cardId: artIndex,
                        rank: definition.Rank,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: false,
                        displayName: definition.DisplayName,
                        definitionKey: key));

                    Assert.That(renderer.sprite, Is.SameAs(expected), key);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV02_U04_DemonPrefabResolvesAllDefinitionSprites()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemonCardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                DemonCardView view = instance.GetComponent<DemonCardView>();
                Assert.That(view, Is.Not.Null);
                foreach (DemonContractDefinition definition in
                    DemonContractCatalog.Default.Definitions)
                {
                    int artIndex = GameSceneCardVisualCatalog.DemonCardSpriteIndexFor(
                        definition.Key);
                    Sprite expected = LoadDemonSprite(artIndex);

                    Assert.That(artIndex, Is.InRange(1, 12), definition.Key);
                    Assert.That(expected, Is.Not.Null, definition.Key);
                    Assert.That(
                        view.GetFaceSprite(definition.Key),
                        Is.SameAs(expected),
                        definition.Key);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV02_U05_PlayerHiddenCardBlendsRealFaceOverBack()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);
                SpriteRenderer frontRenderer = GetFrontRenderer(view);
                SpriteRenderer backRenderer = GetBackRenderer(view);

                view.Bind(new GameSceneCardViewModel(
                    cardId: 1,
                    rank: 1,
                    isFaceUp: false,
                    revealRank: true,
                    canUse: false,
                    displayName: "Ace"));

                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                backRenderer.GetPropertyBlock(properties);

                Assert.That(frontRenderer.gameObject.activeSelf, Is.False);
                Assert.That(backRenderer.gameObject.activeSelf, Is.True);
                Assert.That(
                    properties.GetTexture(CardBlendTextureId),
                    Is.SameAs(frontRenderer.sprite.texture));
                Assert.That(properties.GetFloat(CardBlendAmountId), Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV02_U06_EnemyHiddenAndFaceUpCardsDoNotUsePlayerBlend()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);
                SpriteRenderer frontRenderer = GetFrontRenderer(view);
                SpriteRenderer backRenderer = GetBackRenderer(view);
                MaterialPropertyBlock properties = new MaterialPropertyBlock();

                view.Bind(new GameSceneCardViewModel(
                    cardId: 1,
                    rank: 1,
                    isFaceUp: false,
                    revealRank: true,
                    canUse: false,
                    displayName: "Ace"));
                view.Bind(new GameSceneCardViewModel(
                    cardId: 2,
                    rank: 0,
                    isFaceUp: false,
                    revealRank: false,
                    canUse: false,
                    displayName: ""));

                backRenderer.GetPropertyBlock(properties);
                Assert.That(frontRenderer.gameObject.activeSelf, Is.False);
                Assert.That(backRenderer.gameObject.activeSelf, Is.True);
                Assert.That(properties.GetFloat(CardBlendAmountId), Is.Zero);

                view.Bind(new GameSceneCardViewModel(
                    cardId: 3,
                    rank: 3,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Three"));

                backRenderer.GetPropertyBlock(properties);
                Assert.That(frontRenderer.gameObject.activeSelf, Is.True);
                Assert.That(backRenderer.gameObject.activeSelf, Is.False);
                Assert.That(properties.GetFloat(CardBlendAmountId), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static SpriteRenderer GetFrontRenderer(CardView view)
        {
            return GetRenderer(view, "front");
        }

        private static SpriteRenderer GetBackRenderer(CardView view)
        {
            return GetRenderer(view, "back");
        }

        private static SpriteRenderer GetRenderer(CardView view, string propertyName)
        {
            SerializedObject serialized = new SerializedObject(view);
            GameObject face = serialized.FindProperty(propertyName).objectReferenceValue as
                GameObject;
            Assert.That(face, Is.Not.Null);
            SpriteRenderer renderer = face.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            return renderer;
        }

        private static Sprite LoadAutomaticSprite(int index)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                AutomaticArtRoot + index.ToString("00") + ".png");
        }

        private static Sprite LoadDemonSprite(int index)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                DemonArtRoot + index.ToString("00") + ".png");
        }
    }
}
