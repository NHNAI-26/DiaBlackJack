using System;
using System.Linq;
using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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

        [TestCase(PublicCombatActionType.Hit, "Hit", null)]
        [TestCase(PublicCombatActionType.Stand, "Stand", null)]
        [TestCase(PublicCombatActionType.Change, "Change", null)]
        [TestCase(PublicCombatActionType.UseCard, "UseCard",
            "card-revolver")]
        [TestCase(PublicCombatActionType.DemonContract,
            "DemonContract", "demon-lucifer")]
        public void GSB01_U01_EnemyPublicActionsCreateTypedCues(
            PublicCombatActionType actionType,
            string expectedKind,
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
            Assert.That(cue.Kind.ToString(), Is.EqualTo(expectedKind));
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
        public void GSB01_U06_ProfileSpeechOverrideDoesNotChangeBubbleView()
        {
            GameObject enemy = InstantiatePrefab(EnemyCharacterPrefabPath);
            try
            {
                CharacterView character = enemy.GetComponent<CharacterView>();
                SerializedObject serialized = new SerializedObject(character);
                SerializedProperty profiles =
                    serialized.FindProperty("enemySpriteProfiles");
                SerializedProperty profile = profiles.GetArrayElementAtIndex(0);
                profile.FindPropertyRelative("profileKey").stringValue =
                    "speech-test-profile";
                profile.FindPropertyRelative("hitSpeech").stringValue =
                    "커스텀 히트.";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(character.TrySetEnemyProfile("speech-test-profile"),
                    Is.True);
                character.ShowEnemySpeech(EnemySpeechActionKind.Hit);

                SpeechBubbleView bubble =
                    enemy.GetComponentInChildren<SpeechBubbleView>(true);
                Assert.That(bubble.DisplayedText, Is.EqualTo("커스텀 히트."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void GSB01_U07_MerchantCuesUseConfiguredKoreanLines()
        {
            GameObject root = new GameObject("Merchant Speech Test");
            GameObject enemy = InstantiatePrefab(EnemyCharacterPrefabPath);
            try
            {
                ShopController shop = root.AddComponent<ShopController>();
                CharacterView merchant = enemy.GetComponent<CharacterView>();
                SetPrivateField(shop, "merchant", merchant);
                SetPrivateField(shop, "goldPerWin", 0);
                SpeechBubbleView bubble =
                    enemy.GetComponentInChildren<SpeechBubbleView>(true);

                shop.Open();
                Assert.That(bubble.DisplayedText, Is.EqualTo("어서 오게."));

                AssertMerchantLine(shop, bubble,
                    MerchantSpeechCue.PurchaseSuccess, "좋은 선택이군.");
                AssertMerchantLine(shop, bubble,
                    MerchantSpeechCue.InsufficientGold, "골드가 부족하군.");
                AssertMerchantLine(shop, bubble,
                    MerchantSpeechCue.SoldOut, "이미 팔린 물건일세.");
                AssertMerchantLine(shop, bubble,
                    MerchantSpeechCue.Unavailable, "지금은 팔 수 없네.");
                AssertMerchantLine(shop, bubble,
                    MerchantSpeechCue.LighterSuccess, "덱이 한결 가벼워졌군.");
                AssertMerchantLine(shop, bubble,
                    MerchantSpeechCue.WhiskeySuccess, "기운이 좀 돌아왔겠지.");

                shop.Close();
                Assert.That(bubble.DisplayedText, Is.EqualTo("다음에 또 보지."));
            }
            finally
            {
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
                SetPrivateField(shop, "goldPerWin", 0);
                SetPrivateField(shop, "lighterPrice", 2);
                SetPrivateField(shop, "whiskeyPrice", 3);
                shop.Open();

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
                    Is.EqualTo(MerchantSpeechCue.SoldOut));
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
            MerchantSpeechCue cue,
            string expected)
        {
            shop.ShowMerchantSpeech(cue);
            Assert.That(bubble.DisplayedText, Is.EqualTo(expected));
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
