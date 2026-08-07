using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneSpeechBubbleTests
    {
        private const string SpeechBubblePrefabPath =
            "Assets/03. Prefabs/UI/SpeechBubble.prefab";
        private const string EnemyCharacterPrefabPath =
            "Assets/03. Prefabs/Character/EnemyCharacter.prefab";
        private const string CameraPrefabPath =
            "Assets/03. Prefabs/Map/Camera.prefab";
        private const string PcPipelinePath =
            "Assets/02. ScriptableObjects/Settings/PC_RPAsset.asset";
        private const string TextUiRendererPath =
            "Assets/02. ScriptableObjects/Settings/TextUI_Renderer.asset";
        private const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";
        private const string MerchantSpeechPath =
            "Assets/02. ScriptableObjects/Speech/merchant_speech.asset";

        [TestCase(PublicCombatActionType.Hit, SpeechCueKeys.ActionHit, null)]
        [TestCase(PublicCombatActionType.Stand, SpeechCueKeys.ActionStand, null)]
        [TestCase(PublicCombatActionType.Change, SpeechCueKeys.ActionChange, null)]
        [TestCase(PublicCombatActionType.UseCard, SpeechCueKeys.ActionUseCard,
            "card-revolver")]
        [TestCase(PublicCombatActionType.DemonContract,
            SpeechCueKeys.ActionDemonContract, "demon-lucifer")]
        public void GSB01_U01_EnemyPublicActionsCreateKeyedCues(
            PublicCombatActionType actionType,
            string expectedKey,
            string definitionKey)
        {
            PublicCombatAction action = new PublicCombatAction(
                CombatantSide.Enemy,
                actionType,
                definitionKey);

            EnemySpeechCue cue = GameScenePresenter.CreateEnemySpeechCue(
                roundNumber: 3,
                actionOrdinal: 7,
                action);

            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.RoundNumber, Is.EqualTo(3));
            Assert.That(cue.ActionOrdinal, Is.EqualTo(7));
            Assert.That(cue.CueKey, Is.EqualTo(expectedKey));
            Assert.That(cue.SourceDefinitionKey,
                Is.EqualTo(definitionKey ?? string.Empty));
        }

        [Test]
        public void GSB01_U02_PlayerAndMissingActionsDoNotCreateCues()
        {
            PublicCombatAction playerAction = new PublicCombatAction(
                CombatantSide.Player,
                PublicCombatActionType.Hit);

            Assert.That(
                GameScenePresenter.CreateEnemySpeechCue(1, 1, playerAction),
                Is.Null);
            Assert.That(
                GameScenePresenter.CreateEnemySpeechCue(1, 1, null),
                Is.Null);
            Assert.That(
                GameScenePresenter.CreateEnemySpeechCue(1, 0,
                    new PublicCombatAction(
                        CombatantSide.Enemy,
                        PublicCombatActionType.Stand)),
                Is.Null);
        }

        [Test]
        public void GSB01_U03_CueIdentityDeduplicatesFramesButNotRepeatedActions()
        {
            CoreLoopBattle battle = CreateBattle(11);
            CoreLoopBattle nextBattle = CreateBattle(21);
            PublicCombatAction action = new PublicCombatAction(
                CombatantSide.Enemy,
                PublicCombatActionType.Hit);
            EnemySpeechCue first = GameScenePresenter.CreateEnemySpeechCue(
                1, 2, action, battle);
            EnemySpeechCue sameFrame = GameScenePresenter.CreateEnemySpeechCue(
                1, 2, action, battle);
            EnemySpeechCue repeatedAction = GameScenePresenter.CreateEnemySpeechCue(
                1, 3, action, battle);
            EnemySpeechCue reboundBattle = GameScenePresenter.CreateEnemySpeechCue(
                1, 2, action, nextBattle);

            Assert.That(first.IsSameActionAs(sameFrame), Is.True);
            Assert.That(first.IsSameActionAs(repeatedAction), Is.False);
            Assert.That(first.IsSameActionAs(reboundBattle), Is.False);
        }

        [Test]
        public void GSB01_U04_SpeechBubbleShowsReplacesAndHidesKoreanText()
        {
            GameObject instance = InstantiatePrefab(SpeechBubblePrefabPath);
            try
            {
                SpeechBubbleView view = instance.GetComponent<SpeechBubbleView>();

                view.Show("한 장 더 뽑는다.");
                Assert.That(view.IsVisible, Is.True);
                Assert.That(view.DisplayedText, Is.EqualTo("한 장 더 뽑는다."));

                view.Show("스탠드.");
                Assert.That(view.DisplayedText, Is.EqualTo("스탠드."));

                view.Hide();
                Assert.That(view.IsVisible, Is.False);
                Assert.That(view.DisplayedText, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSB01_U05_SpeechBubbleFacesCameraAndKeepsWorldScale()
        {
            GameObject parent = new GameObject("Scaled Merchant");
            GameObject cameraObject = new GameObject("Speech Camera");
            GameObject instance = InstantiatePrefab(SpeechBubblePrefabPath);
            try
            {
                instance.transform.SetParent(parent.transform, false);
                SpeechBubbleView view = instance.GetComponent<SpeechBubbleView>();
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(12f, 34f, 5f);
                view.SetCameraForTesting(camera);
                view.Show("어서 오게.");
                Vector3 worldScale = instance.transform.lossyScale;

                parent.transform.localScale = Vector3.one * 0.6f;
                view.UpdateFacingAndScale();

                Assert.That(
                    Quaternion.Angle(instance.transform.rotation,
                        camera.transform.rotation),
                    Is.LessThan(0.001f));
                Assert.That(instance.transform.lossyScale.x,
                    Is.EqualTo(worldScale.x).Within(0.0001f));
                Assert.That(instance.transform.lossyScale.y,
                    Is.EqualTo(worldScale.y).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void GSB01_U06_ProfileSpeechResolvesWithoutChangingBubbleView()
        {
            GameObject enemy = InstantiatePrefab(EnemyCharacterPrefabPath);
            SpeechProfileSO profile = CreateSpeechProfile(
                "speech-test-profile",
                (SpeechCueKeys.ActionHit, new[] { "커스텀 히트." }));
            try
            {
                CharacterView character = enemy.GetComponent<CharacterView>();
                var resolver = new SpeechLineResolver(17);
                character.ShowSpeech(
                    resolver.Resolve(profile, SpeechCueKeys.ActionHit));

                SpeechBubbleView bubble =
                    enemy.GetComponentInChildren<SpeechBubbleView>(true);
                Assert.That(bubble.DisplayedText, Is.EqualTo("커스텀 히트."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void GSB01_U07_MerchantCuesUseConfiguredKoreanLines()
        {
            GameObject root = new GameObject("Merchant Speech Test");
            GameObject enemy = InstantiatePrefab(EnemyCharacterPrefabPath);
            SpeechProfileSO profile = CreateSpeechProfile(
                "merchant",
                (SpeechCueKeys.ShopGreeting, new[] { "어서 오게." }),
                (SpeechCueKeys.ShopPurchaseSuccess, new[] { "좋은 선택이군." }),
                (SpeechCueKeys.ShopInsufficientGold, new[] { "골드가 부족하군." }),
                (SpeechCueKeys.ShopSoldOut, new[] { "이미 팔린 물건일세." }),
                (SpeechCueKeys.ShopUnavailable, new[] { "지금은 팔 수 없네." }),
                (SpeechCueKeys.ShopLighterSuccess, new[] { "덱이 한결 가벼워졌군." }),
                (SpeechCueKeys.ShopWhiskeySuccess, new[] { "기운이 좀 돌아왔겠지." }),
                (SpeechCueKeys.ShopFarewell, new[] { "다음에 또 보지." }));
            try
            {
                ShopController shop = root.AddComponent<ShopController>();
                CharacterView merchant = enemy.GetComponent<CharacterView>();
                SetPrivateField(shop, "merchant", merchant);
                SetPrivateField(shop, "merchantSpeechProfile", profile);
                SpeechBubbleView bubble =
                    enemy.GetComponentInChildren<SpeechBubbleView>(true);

                shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
                shop.ResetGold();
                Assert.That(bubble.DisplayedText, Is.EqualTo("어서 오게."));

                AssertMerchantLine(shop, bubble,
                    SpeechCueKeys.ShopPurchaseSuccess, "좋은 선택이군.");
                AssertMerchantLine(shop, bubble,
                    SpeechCueKeys.ShopInsufficientGold, "골드가 부족하군.");
                AssertMerchantLine(shop, bubble,
                    SpeechCueKeys.ShopSoldOut, "이미 팔린 물건일세.");
                AssertMerchantLine(shop, bubble,
                    SpeechCueKeys.ShopUnavailable, "지금은 팔 수 없네.");
                AssertMerchantLine(shop, bubble,
                    SpeechCueKeys.ShopLighterSuccess, "덱이 한결 가벼워졌군.");
                AssertMerchantLine(shop, bubble,
                    SpeechCueKeys.ShopWhiskeySuccess, "기운이 좀 돌아왔겠지.");

                shop.Close();
                Assert.That(bubble.DisplayedText, Is.EqualTo("다음에 또 보지."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSB01_U08_UnavailableUtilitiesDoNotChangeShopState()
        {
            GameObject root = new GameObject("Merchant Atomicity Test");
            try
            {
                ShopController shop = root.AddComponent<ShopController>();
                SetPrivateField(shop, "lighterPrice", 2);
                SetPrivateField(shop, "whiskeyPrice", 3);
                shop.Open(EnemyCombatProfileCatalog.GunslingerKey);
                shop.ResetGold();

                Assert.That(shop.GetLighterAvailability(2),
                    Is.EqualTo(ShopPurchaseAvailability.InsufficientGold));
                Assert.That(shop.TryPurchaseLighterRemoval(2), Is.False);
                Assert.That(shop.GetWhiskeyAvailability(3, 10),
                    Is.EqualTo(ShopPurchaseAvailability.InsufficientGold));
                Assert.That(shop.TryPurchaseWhiskey(3, 10, out int restored),
                    Is.False);
                Assert.That(restored, Is.Zero);
                Assert.That(shop.Gold, Is.Zero);
                Assert.That(
                    ShopController.ResolveAvailabilitySpeech(
                        ShopPurchaseAvailability.SoldOut),
                    Is.EqualTo(SpeechCueKeys.ShopSoldOut));
                Assert.That(
                    GameManager.ResolveFormalUtilityAvailability(
                        false, false, 0, 3),
                    Is.EqualTo(ShopPurchaseAvailability.InsufficientGold));
                Assert.That(
                    GameManager.ResolveFormalUtilityAvailability(
                        false, true, 0, 3),
                    Is.EqualTo(ShopPurchaseAvailability.Unavailable));
                Assert.That(
                    GameManager.ResolveFormalUtilityAvailability(
                        true, false, 3, 3),
                    Is.EqualTo(ShopPurchaseAvailability.Available));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSB01_U09_EnemyAnchorPreservesSpeechBubblePrefabAppearance()
        {
            GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SpeechBubblePrefabPath);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyCharacterPrefabPath);

            Assert.That(bubblePrefab, Is.Not.Null);
            Assert.That(enemyPrefab, Is.Not.Null);
            Assert.That(bubblePrefab.GetComponent<Canvas>().renderMode,
                Is.EqualTo(RenderMode.WorldSpace));
            SpeechBubbleView prefabView =
                bubblePrefab.GetComponent<SpeechBubbleView>();
            Assert.That(prefabView, Is.Not.Null);
            SerializedObject bubbleSerialized = new SerializedObject(prefabView);
            UnityEngine.Object textReference = bubbleSerialized
                .FindProperty("messageText").objectReferenceValue;
            Assert.That(textReference, Is.Not.Null);
            Assert.That(textReference.GetType().Name,
                Does.Contain("TextMeshProUGUI"));
            Assert.That(
                bubblePrefab.GetComponentsInChildren<Graphic>(true)
                    .All(graphic => !graphic.raycastTarget),
                Is.True);

            Transform anchor = enemyPrefab.transform.Find("SpeechBubbleAnchor");
            Transform bubble = enemyPrefab.transform.Find(
                "SpeechBubbleAnchor/SpeechBubble");
            CharacterView character = enemyPrefab.GetComponent<CharacterView>();
            SerializedObject serialized = new SerializedObject(character);
            Assert.That(anchor, Is.Not.Null);
            Assert.That(anchor.localPosition,
                Is.EqualTo(new Vector3(-3.28f, 0.69f, 0f)));
            Assert.That(bubble, Is.Not.Null);
            Assert.That(bubble.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(bubble.localRotation, Is.EqualTo(Quaternion.identity));

            RectTransform sourceRect = (RectTransform)bubblePrefab.transform;
            RectTransform nestedRect = (RectTransform)bubble;
            Assert.That(nestedRect.sizeDelta, Is.EqualTo(sourceRect.sizeDelta));
            Assert.That(nestedRect.pivot, Is.EqualTo(sourceRect.pivot));
            Assert.That(nestedRect.localScale,
                Is.EqualTo(sourceRect.localScale));

            Component sourceText = FindTmpText(bubblePrefab);
            Component nestedText = FindTmpText(bubble.gameObject);
            SerializedObject sourceTextSerialized = new SerializedObject(
                sourceText);
            SerializedObject nestedTextSerialized = new SerializedObject(
                nestedText);
            Assert.That(
                nestedTextSerialized.FindProperty("m_fontSize").floatValue,
                Is.EqualTo(sourceTextSerialized.FindProperty("m_fontSize")
                    .floatValue));
            Assert.That(
                nestedTextSerialized.FindProperty("m_sharedMaterial")
                    .objectReferenceValue,
                Is.SameAs(sourceTextSerialized.FindProperty("m_sharedMaterial")
                    .objectReferenceValue));
            Assert.That(
                serialized.FindProperty("speechBubble").objectReferenceValue,
                Is.Not.Null);
            Assert.That(enemyPrefab.transform.Find("ActionLabel"), Is.Null);
        }

        [Test]
        public void GSB01_U10_TextUiLayerContainsOnlyIndependentTmpCanvas()
        {
            int textUiLayer = LayerMask.NameToLayer("TextUI");
            Assert.That(textUiLayer, Is.EqualTo(7));

            GameObject bubble = AssetDatabase.LoadAssetAtPath<GameObject>(
                SpeechBubblePrefabPath);
            Transform body = bubble.transform.Find("SpeechTextBubble");
            Transform textCanvasTransform = body.Find("TextUICanvas");
            Component text = FindTmpText(textCanvasTransform.gameObject);
            Canvas textCanvas = textCanvasTransform.GetComponent<Canvas>();

            Assert.That(bubble.layer, Is.EqualTo(0));
            Assert.That(body.gameObject.layer, Is.EqualTo(0));
            Assert.That(body.Find("Image").gameObject.layer, Is.EqualTo(0));
            Assert.That(textCanvasTransform.gameObject.layer,
                Is.EqualTo(textUiLayer));
            Assert.That(text.gameObject.layer, Is.EqualTo(textUiLayer));
            Assert.That(textCanvas.overrideSorting, Is.True);
            Assert.That(textCanvas.sortingOrder, Is.EqualTo(201));
            Assert.That(bubble.GetComponent<Canvas>().sortingOrder,
                Is.EqualTo(200));
            Assert.That(
                bubble.GetComponentsInChildren<Graphic>(true)
                    .All(graphic => !graphic.raycastTarget),
                Is.True);
        }

        [Test]
        public void GSB01_U11_CameraStackRendersTextUiWithoutPostProcessing()
        {
            GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CameraPrefabPath);
            Transform sourceTransform = cameraPrefab.transform.Find("Camera");
            Transform overlayTransform = sourceTransform.Find(
                "TextUIOverlayCamera");
            Camera source = sourceTransform.GetComponent<Camera>();
            Camera overlay = overlayTransform.GetComponent<Camera>();
            Component sourceData = FindAdditionalCameraData(
                sourceTransform.gameObject);
            Component overlayData = FindAdditionalCameraData(
                overlayTransform.gameObject);
            SerializedObject sourceDataSerialized = new SerializedObject(
                sourceData);
            SerializedObject overlayDataSerialized = new SerializedObject(
                overlayData);
            int textUiMask = 1 << LayerMask.NameToLayer("TextUI");

            Assert.That(source.cullingMask & textUiMask, Is.Zero);
            Assert.That(overlay.cullingMask, Is.EqualTo(textUiMask));
            Assert.That(
                overlayDataSerialized.FindProperty("m_CameraType").intValue,
                Is.EqualTo(1));
            Assert.That(
                overlayDataSerialized.FindProperty("m_RenderPostProcessing")
                    .boolValue,
                Is.False);
            Assert.That(
                overlayDataSerialized.FindProperty("m_ClearDepth").boolValue,
                Is.True);
            SerializedProperty cameraStack = sourceDataSerialized.FindProperty(
                "m_Cameras");
            Assert.That(cameraStack.arraySize, Is.GreaterThan(0));
            Assert.That(cameraStack.GetArrayElementAtIndex(
                    cameraStack.arraySize - 1).objectReferenceValue,
                Is.SameAs(overlay));
            Assert.That(
                overlayDataSerialized.FindProperty("m_RendererIndex").intValue,
                Is.EqualTo(2));

            ScriptableObject renderer =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    TextUiRendererPath);
            Assert.That(renderer, Is.Not.Null);
            SerializedObject rendererSerialized = new SerializedObject(renderer);
            Assert.That(
                rendererSerialized.FindProperty("m_RendererFeatures").arraySize,
                Is.Zero);

            ScriptableObject pipeline =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    PcPipelinePath);
            SerializedObject pipelineSerialized = new SerializedObject(pipeline);
            SerializedProperty rendererList = pipelineSerialized.FindProperty(
                "m_RendererDataList");
            Assert.That(rendererList.arraySize, Is.GreaterThan(2));
            Assert.That(
                rendererList.GetArrayElementAtIndex(2).objectReferenceValue,
                Is.SameAs(renderer));
        }

        [Test]
        public void GSB01_U12_OverlayCameraCopiesLiveSourceProjection()
        {
            GameObject sourceObject = new GameObject("Source Camera");
            GameObject overlayObject = new GameObject("Overlay Camera");
            try
            {
                Camera source = sourceObject.AddComponent<Camera>();
                Camera overlay = overlayObject.AddComponent<Camera>();
                TextUIOverlayCameraSync sync =
                    overlayObject.AddComponent<TextUIOverlayCameraSync>();
                source.transform.SetPositionAndRotation(
                    new Vector3(3f, 4f, 5f),
                    Quaternion.Euler(11f, 22f, 3f));
                source.fieldOfView = 47f;
                source.nearClipPlane = 0.2f;
                source.farClipPlane = 777f;
                source.rect = new Rect(0.1f, 0.2f, 0.7f, 0.6f);

                sync.SetSourceForTesting(source);
                sync.SynchronizeFromSource();

                Assert.That(overlay.transform.position,
                    Is.EqualTo(source.transform.position));
                Assert.That(overlay.transform.rotation,
                    Is.EqualTo(source.transform.rotation));
                Assert.That(overlay.fieldOfView,
                    Is.EqualTo(source.fieldOfView));
                Assert.That(overlay.nearClipPlane,
                    Is.EqualTo(source.nearClipPlane));
                Assert.That(overlay.farClipPlane,
                    Is.EqualTo(source.farClipPlane));
                Assert.That(overlay.rect, Is.EqualTo(source.rect));
                Assert.That(overlay.worldToCameraMatrix,
                    Is.EqualTo(source.worldToCameraMatrix));
                Assert.That(overlay.projectionMatrix,
                    Is.EqualTo(source.projectionMatrix));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(overlayObject);
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void GSB02_U01_SpeechProfileRejectsInvalidKeysAndLines()
        {
            SpeechProfileSO emptySpeaker = CreateSpeechProfile(
                string.Empty,
                ("valid", new[] { "line" }));
            SpeechProfileSO emptyKey = CreateSpeechProfile(
                "speaker",
                (string.Empty, new[] { "line" }));
            SpeechProfileSO duplicateKey = CreateSpeechProfile(
                "speaker",
                ("same", new[] { "first" }),
                ("same", new[] { "second" }));
            SpeechProfileSO emptyLine = CreateSpeechProfile(
                "speaker",
                ("valid", new[] { " " }));
            try
            {
                Assert.Throws<InvalidOperationException>(
                    emptySpeaker.ValidateOrThrow);
                Assert.Throws<InvalidOperationException>(
                    emptyKey.ValidateOrThrow);
                Assert.Throws<InvalidOperationException>(
                    duplicateKey.ValidateOrThrow);
                Assert.Throws<InvalidOperationException>(
                    emptyLine.ValidateOrThrow);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(emptyLine);
                UnityEngine.Object.DestroyImmediate(duplicateKey);
                UnityEngine.Object.DestroyImmediate(emptyKey);
                UnityEngine.Object.DestroyImmediate(emptySpeaker);
            }
        }

        [Test]
        [Category("GSB03")]
        public void GSB02_U02_EnemyAndMerchantAssetsContainTwoUniqueKoreanLinesPerCue()
        {
            EnemyContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            SpeechProfileSO merchant =
                AssetDatabase.LoadAssetAtPath<SpeechProfileSO>(
                    MerchantSpeechPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(merchant, Is.Not.Null);
            Assert.That(catalog.EnemyCount, Is.EqualTo(6));
            catalog.ValidateOrThrow();

            foreach (EnemyCombatProfileDefinitionSO enemy in catalog.Enemies)
            {
                SpeechProfileSO profile = enemy.SpeechProfile;
                EnemyCombatProfile runtimeProfile =
                    enemy.CreateRuntimeProfile();
                IReadOnlyList<string> requiredKeys =
                    SpeechCueKeys.GetRequiredEnemyKeys(runtimeProfile);
                Assert.That(profile, Is.Not.Null, enemy.Key);
                Assert.That(profile.SpeakerKey, Is.EqualTo(enemy.Key));
                Assert.That(profile.EntryCount,
                    Is.EqualTo(requiredKeys.Count));
                foreach (string cueKey in requiredKeys)
                {
                    Assert.That(profile.TryGetLines(
                        cueKey,
                        out IReadOnlyList<string> lines), Is.True,
                        enemy.Key + ":" + cueKey);
                    Assert.That(lines.Count,
                        Is.EqualTo(IsSingleLineEnemyCue(cueKey) ? 1 : 2));
                    Assert.That(lines.All(ContainsKorean), Is.True);
                    Assert.That(
                        lines.Distinct(StringComparer.Ordinal).Count(),
                        Is.EqualTo(lines.Count),
                        "Enemy cue lines must be unique: " +
                        enemy.Key + ":" + cueKey);
                }
            }

            Assert.That(merchant.SpeakerKey, Is.EqualTo("merchant"));
            Assert.That(merchant.EntryCount,
                Is.EqualTo(SpeechCueKeys.RequiredShopKeys.Count));
            foreach (string cueKey in SpeechCueKeys.RequiredShopKeys)
            {
                Assert.That(merchant.TryGetLines(
                    cueKey,
                    out IReadOnlyList<string> lines), Is.True, cueKey);
                Assert.That(lines.Count, Is.EqualTo(2));
                Assert.That(lines.All(ContainsKorean), Is.True);
            }
        }

        [Test]
        public void GSB02_U03_EnemyProfileRejectsMismatchedSpeakerKey()
        {
            EnemyContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            EnemyCombatProfileDefinitionSO enemy =
                UnityEngine.Object.Instantiate(catalog.Enemies[0]);
            enemy.hideFlags = HideFlags.DontSave;
            SpeechProfileSO mismatch = CreateSpeechProfile(
                "different-speaker",
                (SpeechCueKeys.BattleStart, new[] { "line" }));
            try
            {
                SetSerializedReference(enemy, "speechProfile", mismatch);
                InvalidOperationException exception =
                    Assert.Throws<InvalidOperationException>(
                        () => enemy.CreateRuntimeProfile());
                Assert.That(exception.Message, Does.Contain("must match"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mismatch);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void GSB02_U04_ResolverSelectsConfiguredLinesAndFallsBackToKeyOnce()
        {
            SpeechProfileSO profile = CreateSpeechProfile(
                "resolver",
                (SpeechCueKeys.ActionHit, new[] { "alpha", "beta" }));
            GameObject enemy = InstantiatePrefab(EnemyCharacterPrefabPath);
            try
            {
                var resolver = new SpeechLineResolver(91);
                for (int index = 0; index < 20; index++)
                {
                    Assert.That(
                        resolver.Resolve(profile, SpeechCueKeys.ActionHit),
                        Is.EqualTo("alpha").Or.EqualTo("beta"));
                }

                const string missingKey = "combat.custom.missing";
                LogAssert.Expect(
                    LogType.Warning,
                    "Speech cue 'combat.custom.missing' is missing for " +
                    "speaker 'resolver'.");
                string fallback = resolver.Resolve(profile, missingKey);
                Assert.That(resolver.Resolve(profile, missingKey),
                    Is.EqualTo(missingKey));

                const string missingProfileKey = "combat.custom.no_profile";
                LogAssert.Expect(
                    LogType.Warning,
                    "Speech cue 'combat.custom.no_profile' is missing for " +
                    "speaker '<missing>'.");
                Assert.That(resolver.Resolve(null, missingProfileKey),
                    Is.EqualTo(missingProfileKey));
                Assert.That(resolver.Resolve(null, missingProfileKey),
                    Is.EqualTo(missingProfileKey));

                CharacterView character = enemy.GetComponent<CharacterView>();
                character.ShowSpeech("previous");
                character.ShowSpeech(fallback);
                SpeechBubbleView bubble =
                    enemy.GetComponentInChildren<SpeechBubbleView>(true);
                Assert.That(bubble.DisplayedText, Is.EqualTo(missingKey));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GSB02_U05_DirectorDeduplicatesBattleRoundActionLowSoulAndTerminalCues()
        {
            CoreLoopBattle battle = CreateBattle(301);
            SpeechProfileSO profile = CreateCompleteSpeechProfile("director");
            try
            {
                var director = new EnemySpeechDirector(33);
                EnemySpeechObservation initial = CreateObservation(
                    battle, 1, 0, 9, 9, null,
                    BattleOutcome.InProgress, null);
                AssertResolved(director, profile, initial,
                    SpeechCueKeys.BattleStart);
                Assert.That(director.TryResolve(initial, profile, out _),
                    Is.False);

                EnemySpeechCue firstAction = new EnemySpeechCue(
                    battle, 1, 1, SpeechCueKeys.ActionHit, null);
                EnemySpeechObservation firstActionObservation =
                    CreateObservation(
                        battle, 1, 1, 9, 9, null,
                        BattleOutcome.InProgress, firstAction);
                AssertResolved(director, profile, firstActionObservation,
                    SpeechCueKeys.ActionHit);
                Assert.That(director.TryResolve(
                    firstActionObservation, profile, out _), Is.False);

                EnemySpeechCue repeatedAction = new EnemySpeechCue(
                    battle, 1, 2, SpeechCueKeys.ActionHit, null);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 2, 9, 9, null,
                        BattleOutcome.InProgress, repeatedAction),
                    SpeechCueKeys.ActionHit);

                EnemySpeechObservation lowSoul = CreateObservation(
                    battle, 1, 2, 3, 9, null,
                    BattleOutcome.InProgress, repeatedAction);
                AssertResolved(director, profile, lowSoul,
                    SpeechCueKeys.LowSoul);
                Assert.That(director.TryResolve(lowSoul, profile, out _),
                    Is.False);

                EnemySpeechObservation terminal = CreateObservation(
                    battle, 1, 2, 0, 9, null,
                    BattleOutcome.PlayerVictory, repeatedAction);
                AssertResolved(director, profile, terminal,
                    SpeechCueKeys.Defeat);
                Assert.That(director.TryResolve(terminal, profile, out _),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GSB02_U06_DirectorUsesDamageClassificationAndTransitionPriority()
        {
            CoreLoopBattle battle = CreateBattle(401);
            SpeechProfileSO profile = CreateCompleteSpeechProfile("priority");
            try
            {
                var director = new EnemySpeechDirector(44);
                EnemySpeechCue action = new EnemySpeechCue(
                    battle, 1, 1, SpeechCueKeys.ActionStand, null);
                RoundResolution cardDamage = new RoundResolution(
                    1, RoundOutcome.EnemyBust, 0, 2,
                    RoundEndCause.CardEffectBust, "card-revolver");
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 1, 8, 10, cardDamage,
                        BattleOutcome.InProgress, action),
                    SpeechCueKeys.DamageCard);

                RoundResolution roundDamage = new RoundResolution(
                    2, RoundOutcome.PlayerWin, 0, 2,
                    RoundEndCause.TotalComparison);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 1, 6, 10, roundDamage,
                        BattleOutcome.InProgress, action),
                    SpeechCueKeys.DamageRound);

                RoundResolution otherDamage = new RoundResolution(
                    3, RoundOutcome.EnemyBust, 0, 2,
                    RoundEndCause.ContractEffectBust);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 1, 4, 10, otherDamage,
                        BattleOutcome.InProgress, action),
                    SpeechCueKeys.DamageOther);

                RoundResolution finalDamage = new RoundResolution(
                    4, RoundOutcome.PlayerBust, 2, 0,
                    RoundEndCause.NumericBust);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 1, 1, 10, finalDamage,
                        BattleOutcome.PlayerDefeat, action),
                    SpeechCueKeys.Victory);

                Assert.That(EnemySpeechDirector.ResolveDamageCueKey(
                    RoundEndCause.NumericBust),
                    Is.EqualTo(SpeechCueKeys.DamageRound));
                Assert.That(EnemySpeechDirector.ResolveDamageCueKey(
                    RoundEndCause.ContractEffectBust),
                    Is.EqualTo(SpeechCueKeys.DamageOther));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GSB02_U07_RoundCueOccursOncePerNewRound()
        {
            CoreLoopBattle battle = CreateBattle(501);
            SpeechProfileSO profile = CreateCompleteSpeechProfile("rounds");
            try
            {
                var director = new EnemySpeechDirector(55);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 0, 10, 10, null,
                        BattleOutcome.InProgress, null),
                    SpeechCueKeys.BattleStart);
                EnemySpeechObservation secondRound = CreateObservation(
                    battle, 2, 0, 10, 10, null,
                    BattleOutcome.InProgress, null);
                AssertResolved(director, profile, secondRound,
                    SpeechCueKeys.RoundStart);
                Assert.That(director.TryResolve(secondRound, profile, out _),
                    Is.False);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 3, 0, 10, 10, null,
                        BattleOutcome.InProgress, null),
                    SpeechCueKeys.RoundStart);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GSB02_U08_TerminalHoldDefaultsToOnePointFiveSecondsAndBlocksExit()
        {
            GameObject root = new GameObject("Terminal Speech Hold Test");
            try
            {
                GameManager manager = root.AddComponent<GameManager>();
                SerializedObject serialized = new SerializedObject(manager);
                Assert.That(
                    serialized.FindProperty("terminalSpeechHoldSeconds")
                        .floatValue,
                    Is.EqualTo(1.5f));
                Assert.That(GameManager.DefaultTerminalSpeechHoldSeconds,
                    Is.EqualTo(1.5f));
                Assert.That(GameManager.IsTerminalSpeechHoldBlocking(
                    true, false), Is.True);
                Assert.That(GameManager.IsTerminalSpeechHoldBlocking(
                    true, true), Is.False);
                Assert.That(GameManager.IsTerminalSpeechHoldBlocking(
                    false, false), Is.False);

                MethodInfo method = typeof(GameManager).GetMethod(
                    "CompleteTerminalSpeechHold",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                IEnumerator routine = (IEnumerator)method.Invoke(
                    manager,
                    new object[] { CreateBattle(601) });
                Assert.That(routine.MoveNext(), Is.True);
                Assert.That(routine.Current,
                    Is.TypeOf<WaitForSecondsRealtime>());
                Assert.That(
                    ((WaitForSecondsRealtime)routine.Current).waitTime,
                    Is.EqualTo(1.5f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSB02")]
        public void GSB02_U09_NewRoundActionOrdinalRestartsWithoutBeingDeduplicated()
        {
            CoreLoopBattle battle = CreateBattle(701);
            SpeechProfileSO profile =
                CreateCompleteSpeechProfile("round-action-identity");
            try
            {
                var director = new EnemySpeechDirector(66);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 0, 10, 10, null,
                        BattleOutcome.InProgress, null),
                    SpeechCueKeys.BattleStart);

                EnemySpeechCue firstRoundAction = new EnemySpeechCue(
                    battle, 1, 3, SpeechCueKeys.ActionStand, null);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 3, 10, 10, null,
                        BattleOutcome.InProgress, firstRoundAction),
                    SpeechCueKeys.ActionStand);

                RoundResolution roundDamage = new RoundResolution(
                    1, RoundOutcome.PlayerWin, 0, 2,
                    RoundEndCause.TotalComparison);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 1, 3, 8, 10, roundDamage,
                        BattleOutcome.InProgress, firstRoundAction),
                    SpeechCueKeys.DamageRound);

                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 2, 0, 8, 10, roundDamage,
                        BattleOutcome.InProgress, null),
                    SpeechCueKeys.RoundStart);

                EnemySpeechCue secondRoundChange = new EnemySpeechCue(
                    battle, 2, 1, SpeechCueKeys.ActionChange, null);
                EnemySpeechObservation changeObservation = CreateObservation(
                    battle, 2, 1, 8, 10, roundDamage,
                    BattleOutcome.InProgress, secondRoundChange);
                AssertResolved(director, profile, changeObservation,
                    SpeechCueKeys.ActionChange);
                Assert.That(
                    director.TryResolve(changeObservation, profile, out _),
                    Is.False);

                EnemySpeechCue secondRoundHit = new EnemySpeechCue(
                    battle, 2, 2, SpeechCueKeys.ActionHit, null);
                AssertResolved(
                    director,
                    profile,
                    CreateObservation(
                        battle, 2, 2, 8, 10, roundDamage,
                        BattleOutcome.InProgress, secondRoundHit),
                    SpeechCueKeys.ActionHit);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        [Category("GSB03")]
        public void GSB03_U01_OrderedCardSpeechPlaysBeforeResultThenTerminal()
        {
            CoreLoopBattle battle = CreateBattle(801);
            SpeechProfileSO profile = CreateSpeechProfile(
                "ordered",
                (SpeechCueKeys.ActionRevolverBefore, new[] { "before" }),
                (SpeechCueKeys.ActionRevolverHit, new[] { "hit" }),
                (SpeechCueKeys.Victory, new[] { "victory" }));
            try
            {
                var director = new EnemySpeechDirector(77);
                var cues = Array.AsReadOnly(new[]
                {
                    new EnemySpeechCue(
                        battle,
                        1,
                        1,
                        SpeechCueKeys.ActionRevolverBefore,
                        "auto-pistol-7",
                        EnemySpeechEventKind.PublicAction,
                        EnemySpeechBeat.BeforeEffect,
                        SpeechCueKeys.ActionUseCard,
                        sequenceIndex: 1),
                    new EnemySpeechCue(
                        battle,
                        1,
                        1,
                        SpeechCueKeys.ActionRevolverHit,
                        "auto-pistol-7",
                        EnemySpeechEventKind.PublicAction,
                        EnemySpeechBeat.AfterEffect,
                        SpeechCueKeys.ActionUseCard,
                        sequenceIndex: 1),
                });
                EnemySpeechObservation observation = CreateObservationWithCues(
                    battle,
                    1,
                    1,
                    10,
                    10,
                    null,
                    BattleOutcome.PlayerDefeat,
                    cues);

                AssertResolved(
                    director,
                    profile,
                    observation,
                    SpeechPlaybackMoment.BeforeAnimation,
                    SpeechCueKeys.ActionRevolverBefore,
                    "before");
                Assert.That(director.TryResolve(
                    observation,
                    profile,
                    SpeechPlaybackMoment.BeforeAnimation,
                    out _), Is.False);
                AssertResolved(
                    director,
                    profile,
                    observation,
                    SpeechPlaybackMoment.AfterAnimation,
                    SpeechCueKeys.ActionRevolverHit,
                    "hit");
                AssertResolved(
                    director,
                    profile,
                    observation,
                    SpeechPlaybackMoment.BeforeAnimation,
                    SpeechCueKeys.Victory,
                    "victory");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        [Category("GSB03")]
        public void GSB03_U02_SpecializedCueFallsBackToLegacyCardLine()
        {
            CoreLoopBattle battle = CreateBattle(811);
            SpeechProfileSO profile = CreateSpeechProfile(
                "fallback",
                (SpeechCueKeys.ActionUseCard, new[] { "legacy" }));
            try
            {
                var director = new EnemySpeechDirector(78);
                EnemySpeechCue cue = new EnemySpeechCue(
                    battle,
                    1,
                    1,
                    SpeechCueKeys.ActionKnifeBefore,
                    "military-knife-9",
                    EnemySpeechEventKind.PublicAction,
                    EnemySpeechBeat.BeforeEffect,
                    SpeechCueKeys.ActionUseCard);
                EnemySpeechObservation observation = CreateObservationWithCues(
                    battle,
                    1,
                    1,
                    10,
                    10,
                    null,
                    BattleOutcome.InProgress,
                    Array.AsReadOnly(new[] { cue }));

                AssertResolved(
                    director,
                    profile,
                    observation,
                    SpeechPlaybackMoment.BeforeAnimation,
                    SpeechCueKeys.ActionKnifeBefore,
                    "legacy");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        [Category("GSB03")]
        public void GSB03_U03_CueIdentityIncludesBeatAndRevolverShotIndex()
        {
            CoreLoopBattle battle = CreateBattle(821);
            EnemySpeechCue firstBefore = new EnemySpeechCue(
                battle, 1, 1, "before", "auto-pistol-7",
                EnemySpeechEventKind.PublicAction,
                EnemySpeechBeat.BeforeEffect,
                sequenceIndex: 1);
            EnemySpeechCue firstResult = new EnemySpeechCue(
                battle, 1, 1, "result", "auto-pistol-7",
                EnemySpeechEventKind.PublicAction,
                EnemySpeechBeat.AfterEffect,
                sequenceIndex: 1);
            EnemySpeechCue secondBefore = new EnemySpeechCue(
                battle, 1, 1, "before", "auto-pistol-7",
                EnemySpeechEventKind.PublicAction,
                EnemySpeechBeat.BeforeEffect,
                sequenceIndex: 2);

            Assert.That(firstBefore.IsSameActionAs(firstResult), Is.True);
            Assert.That(firstBefore.IsSameBeatAs(firstResult), Is.False);
            Assert.That(firstBefore.IsSameBeatAs(secondBefore), Is.False);
        }

        [Test]
        [Category("GSB03")]
        public void GSB03_U04_LeviathanRetryBeforeCueUsesSecondShotIndex()
        {
            CoreLoopBattle battle = CreateBattle(831);
            FieldInfo activeSequenceField = typeof(CoreLoopBattle).GetField(
                "_activeLeviathanCardEffectSequence",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo pendingEffectField = typeof(CoreLoopBattle).GetField(
                "_pendingCardEffect",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo resolver = typeof(GameScenePresenter).GetMethod(
                "ResolveRevolverSpeechSequenceIndex",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(activeSequenceField, Is.Not.Null);
            Assert.That(pendingEffectField, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);

            BlackjackCard sourceCard = new BlackjackCard(
                831,
                CardDefinitionCatalog.GetByKey("auto-pistol-7"));
            activeSequenceField.SetValue(
                battle,
                new LeviathanCardEffectSequence(
                    CombatantSide.Enemy,
                    sourceContractCardId: 830,
                    sourceCard.Id,
                    firstActivationSucceeded: false));
            pendingEffectField.SetValue(
                battle,
                new PendingCardEffect(
                    sourceCard.Id,
                    sourceCard.Definition.Effect,
                    CombatPromptId.ManualAutoPistolDeclareNumber,
                    CardEffectChoiceKind.DeclareNumber,
                    new[]
                    {
                        new CardEffectChoiceOption(
                            id: 1,
                            label: "1",
                            numericValue: 1),
                    }));

            Assert.That(
                resolver.Invoke(null, new object[] { battle }),
                Is.EqualTo(2));
        }

        private static CoreLoopBattle CreateBattle(int seed)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateStandard(seed),
                BlackjackDeck.CreateStandard(seed + 1));
        }

        private static GameObject InstantiatePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return UnityEngine.Object.Instantiate(prefab);
        }

        private static Component FindTmpText(GameObject root)
        {
            Component text = root.GetComponentsInChildren<Component>(true)
                .FirstOrDefault(component =>
                    component.GetType().Name == "TextMeshProUGUI");
            Assert.That(text, Is.Not.Null);
            return text;
        }

        private static Component FindAdditionalCameraData(GameObject root)
        {
            Component data = root.GetComponents<Component>()
                .FirstOrDefault(component => component.GetType().Name ==
                    "UniversalAdditionalCameraData");
            Assert.That(data, Is.Not.Null);
            return data;
        }

        private static void AssertMerchantLine(
            ShopController shop,
            SpeechBubbleView bubble,
            string cueKey,
            string expected)
        {
            shop.ShowMerchantSpeech(cueKey);
            Assert.That(bubble.DisplayedText, Is.EqualTo(expected));
        }

        private static SpeechProfileSO CreateSpeechProfile(
            string speakerKey,
            params (string CueKey, string[] Lines)[] entries)
        {
            SpeechProfileSO profile =
                ScriptableObject.CreateInstance<SpeechProfileSO>();
            profile.hideFlags = HideFlags.DontSave;
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("speakerKey").stringValue = speakerKey;
            SerializedProperty serializedEntries =
                serialized.FindProperty("entries");
            serializedEntries.arraySize = entries.Length;
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                SerializedProperty entry =
                    serializedEntries.GetArrayElementAtIndex(entryIndex);
                entry.FindPropertyRelative("cueKey").stringValue =
                    entries[entryIndex].CueKey;
                SerializedProperty lines = entry.FindPropertyRelative("lines");
                lines.arraySize = entries[entryIndex].Lines.Length;
                for (int lineIndex = 0;
                     lineIndex < entries[entryIndex].Lines.Length;
                     lineIndex++)
                {
                    lines.GetArrayElementAtIndex(lineIndex).stringValue =
                        entries[entryIndex].Lines[lineIndex];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static SpeechProfileSO CreateCompleteSpeechProfile(
            string speakerKey)
        {
            (string CueKey, string[] Lines)[] entries =
                SpeechCueKeys.RequiredEnemyKeys
                    .Select(key => (key, new[] { key + ".line" }))
                    .ToArray();
            return CreateSpeechProfile(speakerKey, entries);
        }

        private static EnemySpeechObservation CreateObservation(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            int currentSoul,
            int maximumSoul,
            RoundResolution? resolution,
            BattleOutcome outcome,
            EnemySpeechCue actionCue)
        {
            return new EnemySpeechObservation(
                battle,
                roundNumber,
                actionOrdinal,
                currentSoul,
                maximumSoul,
                resolution,
                outcome,
                actionCue);
        }

        private static EnemySpeechObservation CreateObservationWithCues(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            int currentSoul,
            int maximumSoul,
            RoundResolution? resolution,
            BattleOutcome outcome,
            IReadOnlyList<EnemySpeechCue> actionCues)
        {
            return new EnemySpeechObservation(
                battle,
                roundNumber,
                actionOrdinal,
                currentSoul,
                maximumSoul,
                resolution,
                outcome,
                actionCues);
        }

        private static void AssertResolved(
            EnemySpeechDirector director,
            SpeechProfileSO profile,
            EnemySpeechObservation observation,
            string expectedCueKey)
        {
            Assert.That(director.TryResolve(
                observation,
                profile,
                out EnemySpeechPresentation presentation), Is.True);
            Assert.That(presentation.CueKey, Is.EqualTo(expectedCueKey));
            Assert.That(presentation.Message,
                Is.EqualTo(expectedCueKey + ".line"));
        }

        private static void AssertResolved(
            EnemySpeechDirector director,
            SpeechProfileSO profile,
            EnemySpeechObservation observation,
            SpeechPlaybackMoment playbackMoment,
            string expectedCueKey,
            string expectedMessage)
        {
            Assert.That(director.TryResolve(
                observation,
                profile,
                playbackMoment,
                out EnemySpeechPresentation presentation), Is.True);
            Assert.That(presentation.CueKey, Is.EqualTo(expectedCueKey));
            Assert.That(presentation.Message, Is.EqualTo(expectedMessage));
        }

        private static bool IsSingleLineEnemyCue(string cueKey)
        {
            return cueKey.StartsWith(
                    "combat.action.revolver.",
                    StringComparison.Ordinal) ||
                cueKey.StartsWith(
                    "combat.action.knife.",
                    StringComparison.Ordinal) ||
                cueKey.StartsWith(
                    "combat.action.hammer.",
                    StringComparison.Ordinal) ||
                cueKey.StartsWith(
                    "combat.action.automatic.",
                    StringComparison.Ordinal) ||
                cueKey.StartsWith(
                    "combat.action.demon_contract.",
                    StringComparison.Ordinal) ||
                cueKey.StartsWith(
                    "combat.reaction.player.",
                    StringComparison.Ordinal);
        }

        private static bool ContainsKorean(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.Any(character => character >= '\uAC00' &&
                    character <= '\uD7A3');
        }

        private static void SetSerializedReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
    }
}
