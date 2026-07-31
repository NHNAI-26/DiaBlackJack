using DiaBlackJack.GameScene;
using DiaBlackJack.Content;
using DiaBlackJack.Bootstrap;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneCardSpriteTests
    {
        private static readonly int CardBlendTextureId =
            Shader.PropertyToID("_CardBlendTex");
        private static readonly int CardBlendAmountId =
            Shader.PropertyToID("_CardBlendAmount");
        private static readonly int CardBlendUvRectId =
            Shader.PropertyToID("_CardBlendUVRect");
        private static readonly int PixelOutlineVisibilityId =
            Shader.PropertyToID("_PixelOutlineVisibility");

        private const string CardPrefabPath = "Assets/03. Prefabs/Card/Card.prefab";
        private const string DemonCardPrefabPath =
            "Assets/03. Prefabs/Card/DemonCard.prefab";
        private const string CatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
        private const string UsedMarkTexturePath =
            "Assets/05. Arts/Texture/CardSprite/Overlay/UsedCardPencilStroke.png";

        [Test]
        public void CC_U06_CardContentAssetBuildsAllDefinitions()
        {
            CardContentCatalog catalog = LoadCatalog().BuildRuntimeCatalog();

            Assert.That(catalog.NormalDefinitions, Has.Count.EqualTo(15));
            Assert.That(catalog.DemonDefinitions, Has.Count.EqualTo(12));
            foreach (CardDefinition definition in catalog.NormalDefinitions)
            {
                Assert.That(definition.DisplayName, Is.Not.Empty, definition.Key);
                Assert.That(definition.Description, Is.Not.Empty, definition.Key);
                Assert.That(definition.BasePurchasePrice, Is.EqualTo(3), definition.Key);
                Assert.That(definition.ShopWeight, Is.EqualTo(1), definition.Key);
            }

            for (int rank = 1; rank <= 10; rank++)
            {
                Assert.That(
                    catalog.GetStandardDeckDefault(rank).Rank,
                    Is.EqualTo(rank));
            }

            foreach (DemonContractDefinition definition in catalog.DemonDefinitions)
            {
                Assert.That(definition.DisplayName, Is.Not.Empty, definition.Key);
                Assert.That(definition.Summary, Is.Not.Empty, definition.Key);
                Assert.That(definition.CostSummary, Is.Not.Empty, definition.Key);
                Assert.That(definition.BasePurchasePrice, Is.EqualTo(3), definition.Key);
                Assert.That(definition.ShopWeight, Is.EqualTo(1), definition.Key);
            }
        }

        [Test]
        public void CC_U07_NormalCardContentRejectsInvalidFields()
        {
            System.Action<NormalCardDefinitionSO>[] mutations =
            {
                card => SetPrivateField(card, "displayName", string.Empty),
                card => SetPrivateField(card, "basePurchasePrice", -1),
                card => SetPrivateField(card, "rank", 0),
                card => SetPrivateField(card, "effect", (CardEffectKind)999),
                card => SetPrivateField(card, "spadeFaceSprite", null),
            };

            foreach (System.Action<NormalCardDefinitionSO> mutate in mutations)
            {
                CardContentCatalogSO catalog = CloneCatalog();
                NormalCardDefinitionSO card = ReplaceNormalCard(catalog, 0);
                try
                {
                    mutate(card);
                    Assert.That(
                        () => catalog.BuildRuntimeCatalog(),
                        Throws.TypeOf<System.InvalidOperationException>());
                }
                finally
                {
                    Object.DestroyImmediate(card);
                    Object.DestroyImmediate(catalog);
                }
            }
        }

        [Test]
        public void CC_U08_DemonCardContentRejectsInvalidFields()
        {
            System.Action<DemonCardDefinitionSO>[] mutations =
            {
                card => SetPrivateField(card, "displayName", string.Empty),
                card => SetPrivateField(card, "baseSoulCost", -1),
                card => SetPrivateField(card, "basePurchasePrice", -1),
                card => SetPrivateField(card, "kind", (DemonContractKind)999),
                card => SetPrivateField(card, "faceSprite", null),
            };

            foreach (System.Action<DemonCardDefinitionSO> mutate in mutations)
            {
                CardContentCatalogSO catalog = CloneCatalog();
                DemonCardDefinitionSO card = ReplaceDemonCard(catalog, 0);
                try
                {
                    mutate(card);
                    Assert.That(
                        () => catalog.BuildRuntimeCatalog(),
                        Throws.TypeOf<System.InvalidOperationException>());
                }
                finally
                {
                    Object.DestroyImmediate(card);
                    Object.DestroyImmediate(catalog);
                }
            }
        }

        [Test]
        public void CC_U09_CardContentRejectsDuplicateAndMissingStandardDefaults()
        {
            CardContentCatalogSO duplicateCatalog = CloneCatalog();
            NormalCardDefinitionSO duplicateCard = ReplaceNormalCard(duplicateCatalog, 0);
            try
            {
                SetPrivateField(duplicateCard, "key", "standard-plain-2");
                Assert.That(
                    () => duplicateCatalog.BuildRuntimeCatalog(),
                    Throws.TypeOf<System.ArgumentException>());
            }
            finally
            {
                Object.DestroyImmediate(duplicateCard);
                Object.DestroyImmediate(duplicateCatalog);
            }

            CardContentCatalogSO missingDefaultCatalog = CloneCatalog();
            NormalCardDefinitionSO missingDefaultCard = ReplaceNormalCard(missingDefaultCatalog, 0);
            try
            {
                SetPrivateField(missingDefaultCard, "isStandardDeckDefault", false);
                Assert.That(
                    () => missingDefaultCatalog.BuildRuntimeCatalog(),
                    Throws.TypeOf<System.ArgumentException>());
            }
            finally
            {
                Object.DestroyImmediate(missingDefaultCard);
                Object.DestroyImmediate(missingDefaultCatalog);
            }
        }

        [TestCase("Assets/00. Scenes/StageTest.unity")]
        [TestCase("Assets/00. Scenes/CoreLoopTest.unity")]
        [TestCase("Assets/00. Scenes/GameScene.unity")]
        public void CC_U10_EntrySceneContainsValidCardContentBootstrap(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                CardContentBootstrap bootstrap = FindCardContentBootstrap(scene);
                Assert.That(bootstrap, Is.Not.Null, scenePath);

                SerializedObject serialized = new SerializedObject(bootstrap);
                CardContentCatalogSO catalog = serialized.FindProperty("catalog")
                    .objectReferenceValue as CardContentCatalogSO;
                Assert.That(catalog, Is.Not.Null, scenePath);
                Assert.That(catalog.BuildRuntimeCatalog().NormalDefinitions, Has.Count.EqualTo(15));
                EnemyContentCatalogSO enemyCatalog = serialized
                    .FindProperty("enemyCatalog")
                    .objectReferenceValue as EnemyContentCatalogSO;
                Assert.That(enemyCatalog, Is.Not.Null, scenePath);
                Assert.That(
                    enemyCatalog.BuildRuntimeCatalog().Profiles,
                    Has.Count.EqualTo(6));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase(CardDefinitionCatalog.ResurrectionHerbKey, 1)]
        [TestCase(CardDefinitionCatalog.PoisonKey, 2)]
        [TestCase(CardDefinitionCatalog.PocketWatchKey, 3)]
        [TestCase(CardDefinitionCatalog.LieDetectorKey, 4)]
        [TestCase(CardDefinitionCatalog.FlamethrowerKey, 5)]
        public void CC_U04_AutomaticDefinitionsResolveSpriteFromContentCatalog(
            string definitionKey,
            int expectedIndex)
        {
            CardContentCatalogSO catalog = LoadCatalog();
            Assert.That(
                catalog.GetNormalFaceSprite(definitionKey, CardSuit.Spade),
                Is.Not.Null,
                definitionKey);
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
        public void CC_U05_DemonDefinitionsResolveSpriteFromContentCatalog(
            string definitionKey,
            int expectedIndex)
        {
            CardContentCatalogSO catalog = LoadCatalog();
            Assert.That(
                catalog.GetDemonFaceSprite(definitionKey),
                Is.Not.Null,
                definitionKey);
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
                    Sprite expected = LoadCatalog().GetNormalFaceSprite(
                        key,
                        CardSuit.Spade);
                    Assert.That(expected, Is.Not.Null, key);

                    view.Bind(new GameSceneCardViewModel(
                        cardId: definition.Rank,
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
                    Sprite expected = LoadCatalog().GetDemonFaceSprite(definition.Key);

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
        public void GSV02_U07_DemonPrefabBindsUppercaseKeyOnCardAndHoverBadge()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemonCardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                DemonCardView view = instance.GetComponent<DemonCardView>();
                Assert.That(view, Is.Not.Null);
                DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(
                    DemonContractCatalog.MephistophelesKey);
                string expectedName = definition.Key.ToUpperInvariant();

                view.Bind(new GameSceneDemonCardViewModel(
                    cardId: 1,
                    definitionKey: definition.Key,
                    isFaceUp: true,
                    canUse: true,
                    displayName: definition.DisplayName));

                SerializedObject serialized = new SerializedObject(view);
                Component englishNameText = serialized.FindProperty("englishNameText")
                    .objectReferenceValue as Component;
                Assert.That(englishNameText, Is.Not.Null);
                PropertyInfo textProperty = englishNameText.GetType().GetProperty("text");
                Assert.That(textProperty, Is.Not.Null);
                Assert.That(
                    textProperty.GetValue(englishNameText),
                    Is.EqualTo(expectedName));
                Assert.That(englishNameText.gameObject.activeSelf, Is.True);
                Assert.That(view.HoverBadgeTitle, Is.EqualTo(expectedName));

                view.Bind(new GameSceneDemonCardViewModel(
                    cardId: 1,
                    definitionKey: definition.Key,
                    isFaceUp: false,
                    canUse: false,
                    displayName: definition.DisplayName));

                Assert.That(englishNameText.gameObject.activeSelf, Is.False);
                Assert.That(view.HoverBadgeTitle, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV02_U05_PlayerHiddenCardBlendsRealFaceOverBackOnlyWhileHovered()
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
                    displayName: "Ace",
                    definitionKey: "standard-ace-1"));

                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                backRenderer.GetPropertyBlock(properties);

                Assert.That(frontRenderer.gameObject.activeSelf, Is.False);
                Assert.That(backRenderer.gameObject.activeSelf, Is.True);
                Assert.That(
                    properties.GetTexture(CardBlendTextureId),
                    Is.SameAs(frontRenderer.sprite.texture));
                Assert.That(properties.GetFloat(CardBlendAmountId), Is.Zero);
                Assert.That(
                    properties.GetVector(CardBlendUvRectId),
                    Is.EqualTo(GetSpriteUvRect(frontRenderer.sprite)));

                view.SetHovered(true);
                backRenderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(CardBlendAmountId), Is.EqualTo(0.5f));

                view.SetHovered(false);
                backRenderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(CardBlendAmountId), Is.Zero);
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
                    displayName: "Ace",
                    definitionKey: "standard-ace-1"));
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
                    displayName: "Three",
                    definitionKey: "standard-plain-3"));

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

        [Test]
        public void GSV03_U01_ShopNormalCardHoverMaterialIsIsolatedPerOffer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var instances = new GameObject[3];
            try
            {
                var views = new CardView[instances.Length];
                var renderers = new SpriteRenderer[instances.Length];
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i] = Object.Instantiate(prefab);
                    views[i] = instances[i].GetComponent<CardView>();
                    views[i].SetShopPresentation();
                    views[i].Bind(new GameSceneCardViewModel(
                        i,
                        rank: i + 1,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: true,
                        displayName: "SHOP",
                        definitionKey:
                            CardDefinitionCatalog.GetDefaultForRank(i + 1).Key));
                    renderers[i] = GetFrontRenderer(views[i]);
                }

                Assert.That(renderers[0].sharedMaterial,
                    Is.Not.SameAs(renderers[1].sharedMaterial));
                Assert.That(renderers[1].sharedMaterial,
                    Is.Not.SameAs(renderers[2].sharedMaterial));

                views[1].SetHovered(true);
                var properties = new MaterialPropertyBlock();
                renderers[0].GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(PixelOutlineVisibilityId), Is.Zero);
                renderers[1].GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(PixelOutlineVisibilityId), Is.EqualTo(1f));
                renderers[2].GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(PixelOutlineVisibilityId), Is.Zero);
            }
            finally
            {
                foreach (GameObject instance in instances)
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void GSV03_U02_ShopDemonCardUsesBrightUnlitMaterialInstance()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DemonCardPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                DemonCardView view = instance.GetComponent<DemonCardView>();
                SpriteRenderer renderer = GetDemonFrontRenderer(view);
                Material source = renderer.sharedMaterial;

                view.SetShopPresentation();

                Assert.That(renderer.sharedMaterial, Is.Not.SameAs(source));
                Assert.That(renderer.sharedMaterial.IsKeywordEnabled("_UNLIT_ON"),
                    Is.True);
                Assert.That(renderer.sharedMaterial.GetFloat("_LightingMode"),
                    Is.EqualTo(1f));
                Assert.That(renderer.sharedMaterial.GetFloat("_Brightness"),
                    Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV03_U03_InactiveDeckStackSkipsAnimationCoroutine()
        {
            var gameObject = new GameObject("Inactive Deck Stack");
            try
            {
                DeckStackView view = gameObject.AddComponent<DeckStackView>();
                SetPrivateField(view, "_displayedCardCount", 5);
                SetPrivateField(view, "_targetCardCount", 4);
                gameObject.SetActive(false);

                MethodInfo enqueue = typeof(DeckStackView).GetMethod(
                    "EnqueueAnimations",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(enqueue, Is.Not.Null);
                enqueue.Invoke(view, new object[] { -1 });

                Assert.That(
                    GetPrivateField<int>(view, "_displayedCardCount"),
                    Is.EqualTo(4));
                Assert.That(
                    GetPrivateField<object>(view, "_animationQueueRoutine"),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CUM10_U04_CardViewClearsUsedMarkAcrossRebindAndReactivation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);

                view.Bind(CreateUsedMarkCard(cardId: 1, isUsed: false));
                Assert.That(view.IsUsedMarkVisible, Is.False);

                view.Bind(CreateUsedMarkCard(cardId: 1, isUsed: true));
                Assert.That(view.IsUsedMarkVisible, Is.True);
                Assert.That(GetPrivateField<object>(view, "_usedMarkSequence"), Is.Null);

                view.Bind(CreateUsedMarkCard(cardId: 1, isUsed: false));
                Assert.That(view.IsUsedMarkVisible, Is.False);
                Assert.That(GetUsedMarkStroke(view, "usedMarkFirstStroke")
                    .transform.localScale.x, Is.Zero);
                Assert.That(GetUsedMarkStroke(view, "usedMarkSecondStroke")
                    .transform.localScale.x, Is.Zero);

                view.Bind(CreateUsedMarkCard(cardId: 2, isUsed: true));
                Assert.That(view.IsUsedMarkVisible, Is.True);
                Assert.That(GetPrivateField<object>(view, "_usedMarkSequence"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CUM10_U05_UsedMarkAssetAndPrefabAreAuthoredForDrawing()
        {
            TextureImporter importer = AssetImporter.GetAtPath(UsedMarkTexturePath) as
                TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(1024f));
            Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(0f, 0.5f)));

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(UsedMarkTexturePath);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(1024));
            Assert.That(texture.height, Is.EqualTo(128));

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(view);
                GameObject usedMark = serialized.FindProperty("usedMark")
                    .objectReferenceValue as GameObject;
                SpriteRenderer firstStroke = GetUsedMarkStroke(
                    view,
                    "usedMarkFirstStroke");
                SpriteRenderer secondStroke = GetUsedMarkStroke(
                    view,
                    "usedMarkSecondStroke");

                Assert.That(usedMark, Is.Not.Null);
                Assert.That(usedMark.transform.localPosition.z, Is.GreaterThan(0f));
                Assert.That(usedMark.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(firstStroke.sprite, Is.Not.Null);
                Assert.That(secondStroke.sprite, Is.SameAs(firstStroke.sprite));
                Assert.That(firstStroke.flipX, Is.False);
                Assert.That(secondStroke.flipX, Is.False);
                Assert.That(secondStroke.transform.localPosition.x,
                    Is.EqualTo(firstStroke.transform.localPosition.x));
                Assert.That(secondStroke.transform.localPosition.y,
                    Is.EqualTo(-firstStroke.transform.localPosition.y));
                Assert.That(firstStroke.color.r, Is.LessThanOrEqualTo(0.1f));
                Assert.That(secondStroke.color.r, Is.LessThanOrEqualTo(0.1f));
                Assert.That(firstStroke.transform.localScale.x,
                    Is.GreaterThanOrEqualTo(0.8f));
                Assert.That(firstStroke.transform.localScale.y,
                    Is.GreaterThanOrEqualTo(0.45f));
                Assert.That(secondStroke.transform.localScale,
                    Is.EqualTo(firstStroke.transform.localScale));
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(
                        firstStroke.transform.localEulerAngles.z,
                        secondStroke.transform.localEulerAngles.z)),
                    Is.GreaterThanOrEqualTo(100f));
                Assert.That(serialized.FindProperty("usedMarkStrokeDuration").floatValue,
                    Is.EqualTo(0.175f));

                view.SetSortingOrder(9);
                Assert.That(firstStroke.sortingOrder, Is.EqualTo(10));
                Assert.That(secondStroke.sortingOrder, Is.EqualTo(10));
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

        private static SpriteRenderer GetDemonFrontRenderer(DemonCardView view)
        {
            SerializedObject serialized = new SerializedObject(view);
            GameObject face = serialized.FindProperty("front")
                .objectReferenceValue as GameObject;
            Assert.That(face, Is.Not.Null);
            SpriteRenderer renderer = face.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            return renderer;
        }

        private static SpriteRenderer GetUsedMarkStroke(
            CardView view,
            string propertyName)
        {
            SerializedObject serialized = new SerializedObject(view);
            SpriteRenderer renderer = serialized.FindProperty(propertyName)
                .objectReferenceValue as SpriteRenderer;
            Assert.That(renderer, Is.Not.Null, propertyName);
            return renderer;
        }

        private static GameSceneCardViewModel CreateUsedMarkCard(int cardId, bool isUsed)
        {
            return new GameSceneCardViewModel(
                cardId,
                rank: 7,
                isFaceUp: true,
                revealRank: true,
                canUse: !isUsed,
                displayName: "Revolver",
                definitionKey: CardDefinitionCatalog.GetDefaultForRank(7).Key,
                isUsed: isUsed);
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

        private static CardContentCatalogSO LoadCatalog()
        {
            CardContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog;
        }

        private static CardContentBootstrap FindCardContentBootstrap(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                CardContentBootstrap bootstrap = root.GetComponentInChildren<
                    CardContentBootstrap>(true);
                if (bootstrap != null)
                {
                    return bootstrap;
                }
            }

            return null;
        }

        private static CardContentCatalogSO CloneCatalog()
        {
            return Object.Instantiate(LoadCatalog());
        }

        private static NormalCardDefinitionSO ReplaceNormalCard(
            CardContentCatalogSO catalog,
            int index)
        {
            List<NormalCardDefinitionSO> cards = GetPrivateField<
                List<NormalCardDefinitionSO>>(catalog, "normalCards");
            NormalCardDefinitionSO clone = Object.Instantiate(cards[index]);
            cards[index] = clone;
            return clone;
        }

        private static DemonCardDefinitionSO ReplaceDemonCard(
            CardContentCatalogSO catalog,
            int index)
        {
            List<DemonCardDefinitionSO> cards = GetPrivateField<
                List<DemonCardDefinitionSO>>(catalog, "demonCards");
            DemonCardDefinitionSO clone = Object.Instantiate(cards[index]);
            cards[index] = clone;
            return clone;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            Vector2[] uvs = sprite.uv;
            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                minimum = Vector2.Min(minimum, uvs[i]);
                maximum = Vector2.Max(maximum, uvs[i]);
            }

            Vector2 size = maximum - minimum;
            return new Vector4(minimum.x, minimum.y, size.x, size.y);
        }
    }
}
