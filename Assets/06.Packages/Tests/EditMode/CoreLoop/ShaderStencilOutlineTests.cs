using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DiaBlackJack.GameScene;
using DiaBlackJack.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class ShaderStencilOutlineTests
    {
        private const string StencilOutlineKeyword = "_STENCIL_OUTLINE_ON";
        private const string StencilOutlinePass = "StencilOutline";
        private const string StencilOutlineOnlyShaderPath =
            "Assets/05. Arts/Shader/NHNStencilOutlineOnly.shader";
        private const string StencilOutlinePassIncludePath =
            "Assets/05. Arts/Shader/NHNStencilOutlinePass.hlsl";
        private const string MaskShaderPath =
            "Assets/05. Arts/Shader/NHNPostProcessOutlineMask.shader";
        private const string CompositeShaderPath =
            "Assets/05. Arts/Shader/NHNPostProcessOutlineComposite.shader";
        private const string PcRendererPath =
            "Assets/02. ScriptableObjects/Settings/PC_Renderer.asset";
        private const string StencilOutlineOnlyShaderName =
            "Hidden/NHN/Stencil Outline Only";
        private const string RemainingDeckPrefabPath =
            "Assets/03. Prefabs/Card/RemainingDeck.prefab";
        private const string DiscardDeckPrefabPath =
            "Assets/03. Prefabs/Card/DiscardDeck.prefab";
        private const string CodexBookPrefabPath =
            "Assets/03. Prefabs/Props/CodexBook.prefab";
        private const string ShopItemLighterPrefabPath =
            "Assets/03. Prefabs/Shop/ShopItem_Lighter.prefab";
        private const string ShopItemWhiskeyPrefabPath =
            "Assets/03. Prefabs/Shop/ShopItem_Whiskey.prefab";
        private const string LighterOnlyModelPrefabPath =
            "Assets/03. Prefabs/Item/Lighter_OnlyModel.prefab";
        private const string LighterAnimationPrefabPath =
            "Assets/03. Prefabs/Item/Lighter_Anim.prefab";
        private const string WhiskeyOnlyModelPrefabPath =
            "Assets/03. Prefabs/Item/whiskey_onlymodel.prefab";
        private const string CardDeckModelPath = "Assets/05. Arts/FBX/CardDeck.fbx";
        private const int StencilOutlineRenderQueue = 3000;
        private static readonly int StencilOutlineEnabledId =
            Shader.PropertyToID("_StencilOutlineEnabled");
        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");
        private static readonly int StencilOutlineWidthId =
            Shader.PropertyToID("_StencilOutlineWidth");

        [Test]
        public void GSV06_U01_LighterAnimationUsesSelectedCardSprite()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LighterAnimationPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            var texture = new Texture2D(2, 2);
            Sprite selectedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            try
            {
                LighterDragTriggerController controller =
                    instance.GetComponent<LighterDragTriggerController>();
                Assert.That(controller, Is.Not.Null);

                SpriteRenderer burnCardRenderer = null;
                SpriteRenderer[] renderers =
                    instance.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].name == "Square")
                    {
                        burnCardRenderer = renderers[i];
                        break;
                    }
                }

                Assert.That(burnCardRenderer, Is.Not.Null);
                Assert.That(controller.SetBurnCardSprite(selectedSprite), Is.True);
                Assert.That(burnCardRenderer.sprite, Is.SameAs(selectedSprite));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(selectedSprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void GSV06_U02_LighterAnimationResetsBurnAndPreservesAuthoredCardFootprint()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LighterAnimationPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            Texture2D texture = new Texture2D(40, 20);
            Sprite wideSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 40f, 20f),
                new Vector2(0.5f, 0.5f),
                10f);
            try
            {
                LighterDragTriggerController controller =
                    instance.GetComponent<LighterDragTriggerController>();
                SpriteRenderer burnCardRenderer =
                    FindSpriteRenderer(instance, "Square");
                Assert.That(controller, Is.Not.Null);
                Assert.That(burnCardRenderer, Is.Not.Null);

                Vector3 originalPosition = burnCardRenderer.transform.localPosition;
                Quaternion originalRotation = burnCardRenderer.transform.localRotation;
                Vector2 originalFootprint = GetLocalSpriteFootprint(burnCardRenderer);
                Material burnMaterial = burnCardRenderer.sharedMaterial;
                int dissolveAmountId = Shader.PropertyToID("_DissolveAmount");
                int dissolveEnabledId = Shader.PropertyToID("_DissolveEnabled");
                Assert.That(burnMaterial.HasProperty(dissolveAmountId), Is.True);
                burnMaterial.SetFloat(dissolveAmountId, 0.75f);
                burnCardRenderer.color = Color.gray;

                Assert.That(controller.SetBurnCardSprite(wideSprite), Is.True);

                Assert.That(burnCardRenderer.sprite, Is.SameAs(wideSprite));
                burnMaterial = burnCardRenderer.sharedMaterial;
                Assert.That(
                    burnMaterial.GetFloat(dissolveAmountId),
                    Is.EqualTo(0f).Within(0.0001f));
                Assert.That(
                    burnMaterial.GetFloat(dissolveEnabledId),
                    Is.EqualTo(0f).Within(0.0001f));
                Assert.That(burnMaterial.IsKeywordEnabled("_DISSOLVE_ON"), Is.False);
                Assert.That(burnCardRenderer.color, Is.EqualTo(Color.white));
                Assert.That(
                    GetLocalSpriteFootprint(burnCardRenderer).x,
                    Is.EqualTo(originalFootprint.x).Within(0.0001f));
                Assert.That(
                    GetLocalSpriteFootprint(burnCardRenderer).y,
                    Is.EqualTo(originalFootprint.y).Within(0.0001f));
                Assert.That(
                    burnCardRenderer.transform.localPosition,
                    Is.EqualTo(originalPosition));
                Assert.That(
                    burnCardRenderer.transform.localRotation,
                    Is.EqualTo(originalRotation));

                LighterAnimationEventReceiver animationEvents =
                    instance.GetComponent<LighterAnimationEventReceiver>();
                Assert.That(animationEvents, Is.Not.Null);
                animationEvents.SetAnimatorTrigger("CardFire");
                Assert.That(
                    burnMaterial.GetFloat(dissolveEnabledId),
                    Is.EqualTo(1f).Within(0.0001f));
                Assert.That(burnMaterial.IsKeywordEnabled("_DISSOLVE_ON"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(wideSprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void GSV06_U03_LighterAnimationHidesBurnCardBeforeCoverCloses()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LighterAnimationPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                SpriteRenderer burnCardRenderer =
                    FindSpriteRenderer(instance, "Square");
                LighterAnimationEventReceiver animationEvents =
                    instance.GetComponent<LighterAnimationEventReceiver>();
                Assert.That(burnCardRenderer, Is.Not.Null);
                Assert.That(animationEvents, Is.Not.Null);
                Assert.That(burnCardRenderer.enabled, Is.True);

                animationEvents.SetAnimatorTrigger("CoverClose");

                Assert.That(burnCardRenderer.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static SpriteRenderer FindSpriteRenderer(
            GameObject root,
            string objectName)
        {
            SpriteRenderer[] renderers =
                root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name == objectName)
                {
                    return renderers[i];
                }
            }

            return null;
        }

        private static Vector2 GetLocalSpriteFootprint(SpriteRenderer renderer)
        {
            Vector3 size = renderer.sprite.bounds.size;
            Vector3 scale = renderer.transform.localScale;
            return new Vector2(
                Mathf.Abs(size.x * scale.x),
                Mathf.Abs(size.y * scale.y));
        }

        [TestCase("Assets/05. Arts/Shader/NHNUberLit.shader", "Shader/Uber Lit")]
        [TestCase("Assets/03. Prefabs/Card/CardDeckStack.shader", "DiaBlackJack/CardDeckStack")]
        public void StencilOutlineShadersExposeMaterialControls(
            string shaderPath,
            string shaderName)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            Assert.That(shader, Is.Not.Null, shaderPath);
            Assert.That(Shader.Find(shaderName), Is.SameAs(shader));

            Material material = new Material(shader);
            try
            {
                Assert.That(material.FindPass(StencilOutlinePass),
                    Is.GreaterThanOrEqualTo(0));
                Assert.That(material.HasProperty(StencilOutlineEnabledId), Is.True);
                Assert.That(material.HasProperty(StencilOutlineColorId), Is.True);
                Assert.That(material.HasProperty(StencilOutlineWidthId), Is.True);

                Assert.That(material.IsKeywordEnabled(StencilOutlineKeyword), Is.False);
                material.SetFloat(StencilOutlineEnabledId, 1f);
                material.EnableKeyword(StencilOutlineKeyword);
                Assert.That(material.IsKeywordEnabled(StencilOutlineKeyword), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void StencilOutlineOnlyShaderDrawsAfterSceneDepth()
        {
            string source = File.ReadAllText(StencilOutlineOnlyShaderPath);
            Assert.That(source, Does.Contain("\"Queue\"=\"Transparent\""));
            Assert.That(source, Does.Contain("ZTest Always"));
        }

        [Test]
        public void StencilOutlinePassExpandsInScreenSpace()
        {
            string source = File.ReadAllText(StencilOutlinePassIncludePath);
            Assert.That(source, Does.Contain("_ScreenParams"));
            Assert.That(source, Does.Contain("positionCS.xy +="));
            Assert.That(source, Does.Not.Contain("positionWS += normalWS"));
        }

        [Test]
        public void PostProcessOutlineShadersExist()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Shader>(MaskShaderPath),
                Is.SameAs(Shader.Find("Hidden/NHN/Post Process Outline Mask")));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Shader>(CompositeShaderPath),
                Is.SameAs(Shader.Find("Hidden/NHN/Post Process Outline Composite")));

            string source = File.ReadAllText(CompositeShaderPath);
            Assert.That(source, Does.Contain("_NHNPostProcessOutlineMask"));
            Assert.That(source, Does.Contain("_NHNPostProcessOutlineWidthPixels"));
            Assert.That(source, Does.Contain("neighbor - center"));
        }

        [Test]
        public void PcRendererInstallsPostProcessOutlineFeature()
        {
            Object rendererData = AssetDatabase.LoadMainAssetAtPath(PcRendererPath);
            Assert.That(rendererData, Is.Not.Null);

            SerializedObject serialized = new SerializedObject(rendererData);
            SerializedProperty features =
                serialized.FindProperty("m_RendererFeatures");
            Assert.That(features, Is.Not.Null);

            bool found = false;
            for (int i = 0; i < features.arraySize; i++)
            {
                Object feature = features
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue;
                if (feature != null &&
                    feature.GetType().FullName ==
                    "DiaBlackJack.Rendering.PostProcessOutlineRendererFeature")
                {
                    found = true;
                    Assert.That(feature.name, Is.EqualTo("NHN Post Process Outline"));
                    break;
                }
            }

            Assert.That(found, Is.True);
        }

        [Test]
        public void DeckStackHoverRegistersPostProcessOutlineTarget()
        {
            PostProcessOutlineRegistry.Clear();

            Shader shader = Shader.Find("DiaBlackJack/CardDeckStack");
            Assert.That(shader, Is.Not.Null);

            GameObject root = new GameObject("Deck Stack Hover Test");
            Material sourceMaterial = new Material(shader);
            Mesh sourceMesh = CreateDuplicatePositionHardNormalMesh();
            try
            {
                MeshFilter filter = root.AddComponent<MeshFilter>();
                filter.sharedMesh = sourceMesh;
                MeshRenderer renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = sourceMaterial;
                DeckStackView view = root.AddComponent<DeckStackView>();

                view.SetHovered(true);

                var targets = new List<PostProcessOutlineRegistry.Target>();
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.EqualTo(1));
                Assert.That(targets[0].Renderer, Is.SameAs(renderer));
                Assert.That(targets[0].WidthPixels, Is.GreaterThan(0f));
                Assert.That(
                    root.GetComponentsInChildren<MeshRenderer>(true).Length,
                    Is.EqualTo(1));

                view.SetHovered(false);
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(sourceMaterial);
                Object.DestroyImmediate(sourceMesh);
                PostProcessOutlineRegistry.Clear();
            }
        }

        [Test]
        [Category("DXM11")]
        public void DXM11_U02_CodexHoverMatchesDeckPostProcessOutline()
        {
            PostProcessOutlineRegistry.Clear();

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CodexBookPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject root = Object.Instantiate(prefab);
            GameObject managerObject = new GameObject("Codex Hover Manager Test");
            try
            {
                CodexClickable clickable = root.GetComponent<CodexClickable>();
                MeshRenderer renderer =
                    root.GetComponentInChildren<MeshRenderer>(true);
                GameManager manager = managerObject.AddComponent<GameManager>();
                Assert.That(clickable, Is.Not.Null);
                Assert.That(renderer, Is.Not.Null);

                Vector3 baseScale = root.transform.localScale;
                MethodInfo update = typeof(GameManager).GetMethod(
                    "UpdateCodexHover",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(update, Is.Not.Null);

                update.Invoke(manager, new object[] { clickable });

                var targets = new List<PostProcessOutlineRegistry.Target>();
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.EqualTo(1));
                Assert.That(targets[0].Renderer, Is.SameAs(renderer));
                Assert.That(targets[0].WidthPixels, Is.EqualTo(4f));
                Assert.That(
                    targets[0].Color,
                    Is.EqualTo(renderer.sharedMaterial.GetColor(
                        StencilOutlineColorId)));
                AssertVectorApproximately(root.transform.localScale, baseScale);

                update.Invoke(manager, new object[] { null });
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(root);
                PostProcessOutlineRegistry.Clear();
            }
        }

        [TestCase(ShopItemLighterPrefabPath, LighterOnlyModelPrefabPath)]
        [TestCase(ShopItemWhiskeyPrefabPath, WhiskeyOnlyModelPrefabPath)]
        public void ShopUtilityOnlyModelHoverRegistersPostProcessOutlineWithoutScaling(
            string itemPrefabPath,
            string modelPrefabPath)
        {
            PostProcessOutlineRegistry.Clear();

            GameObject itemPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(itemPrefabPath);
            GameObject modelPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(modelPrefabPath);
            Assert.That(itemPrefab, Is.Not.Null, itemPrefabPath);
            Assert.That(modelPrefab, Is.Not.Null, modelPrefabPath);

            ShopUtilityItemView prefabView =
                itemPrefab.GetComponent<ShopUtilityItemView>();
            Assert.That(prefabView, Is.Not.Null);
            Assert.That(
                GetDisplayModelPrefab(prefabView),
                Is.SameAs(modelPrefab));

            GameObject root = Object.Instantiate(itemPrefab);
            try
            {
                ShopUtilityItemView view = root.GetComponent<ShopUtilityItemView>();
                Assert.That(view, Is.Not.Null);

                Vector3 baseScale = root.transform.localScale;
                EnsureShopUtilityAwake(view, modelPrefab.name);

                Transform modelRoot = root.transform.Find(modelPrefab.name);
                Assert.That(modelRoot, Is.Not.Null);
                Renderer[] modelRenderers =
                    modelRoot.GetComponentsInChildren<Renderer>(true);
                Assert.That(modelRenderers.Length, Is.GreaterThan(0));

                view.SetHovered(true);

                AssertVectorApproximately(root.transform.localScale, baseScale);

                var targets = new List<PostProcessOutlineRegistry.Target>();
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.EqualTo(modelRenderers.Length));
                for (int i = 0; i < targets.Count; i++)
                {
                    Assert.That(
                        ContainsRenderer(modelRenderers, targets[i].Renderer),
                        Is.True);
                    Assert.That(targets[i].WidthPixels, Is.GreaterThan(0f));
                }

                view.SetHovered(false);
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.Zero);
                AssertVectorApproximately(root.transform.localScale, baseScale);
            }
            finally
            {
                Object.DestroyImmediate(root);
                PostProcessOutlineRegistry.Clear();
            }
        }

        [TestCase(DeckKind.Draw, "remainingDeck", "RemainingDeck")]
        [TestCase(DeckKind.Discard, "discardDeck", "DiscardDeck")]
        public void GameManagerDeckStackHoverUpdatesPostProcessOutlineTarget(
            DeckKind kind,
            string managerFieldName,
            string deckName)
        {
            PostProcessOutlineRegistry.Clear();

            Shader shader = Shader.Find("DiaBlackJack/CardDeckStack");
            Assert.That(shader, Is.Not.Null);

            GameObject managerObject = new GameObject("Game Manager Hover Test");
            GameObject deckObject = new GameObject(deckName);
            Material material = new Material(shader);
            Mesh mesh = CreateQuadMesh();
            try
            {
                DeckClickable clickable = deckObject.AddComponent<DeckClickable>();
                SetDeckClickableKind(clickable, kind);
                MeshFilter filter = deckObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = deckObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                DeckStackView deckView = deckObject.AddComponent<DeckStackView>();

                GameManager manager = managerObject.AddComponent<GameManager>();
                SetGameManagerDeckField(manager, managerFieldName, deckView);

                MethodInfo resolve = typeof(GameManager).GetMethod(
                    "ResolvePointedDeckStack",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                DeckStackView resolved = (DeckStackView)resolve.Invoke(
                    manager,
                    new object[] { clickable });
                Assert.That(resolved, Is.SameAs(deckView));

                MethodInfo update = typeof(GameManager).GetMethod(
                    "UpdateDeckStackHover",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                update.Invoke(manager, new object[] { deckView });

                var targets = new List<PostProcessOutlineRegistry.Target>();
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.EqualTo(1));
                Assert.That(targets[0].Renderer, Is.SameAs(renderer));

                update.Invoke(manager, new object[] { null });
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(deckObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(mesh);
                PostProcessOutlineRegistry.Clear();
            }
        }

        [TestCase(RemainingDeckPrefabPath, DeckKind.Draw)]
        [TestCase(DiscardDeckPrefabPath, DeckKind.Discard)]
        public void DeckStackPrefabHoverUsesPostProcessOutlineWithoutMeshClone(
            string prefabPath,
            DeckKind expectedKind)
        {
            PostProcessOutlineRegistry.Clear();

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject root = Object.Instantiate(prefab);
            try
            {
                DeckClickable clickable = root.GetComponent<DeckClickable>();
                DeckStackView view = root.GetComponent<DeckStackView>();
                MeshFilter sourceFilter = root.GetComponent<MeshFilter>();
                MeshRenderer sourceRenderer = root.GetComponent<MeshRenderer>();
                Assert.That(clickable, Is.Not.Null);
                Assert.That(clickable.Kind, Is.EqualTo(expectedKind));
                Assert.That(view, Is.Not.Null);
                Assert.That(sourceFilter, Is.Not.Null);
                Assert.That(sourceRenderer, Is.Not.Null);

                Mesh sourceMesh = sourceFilter.sharedMesh;
                Assert.That(AssetDatabase.GetAssetPath(sourceMesh),
                    Is.EqualTo(CardDeckModelPath));
                Assert.That(sourceMesh.isReadable, Is.False);

                view.SetHovered(true);

                var targets = new List<PostProcessOutlineRegistry.Target>();
                PostProcessOutlineRegistry.FillTargets(targets);
                Assert.That(targets.Count, Is.EqualTo(1));
                Assert.That(targets[0].Renderer, Is.SameAs(sourceRenderer));
                Assert.That(
                    root.GetComponentsInChildren<MeshRenderer>(true).Length,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                PostProcessOutlineRegistry.Clear();
            }
        }

        private static MeshRenderer GetOutlineRenderer(
            GameObject root,
            MeshRenderer sourceRenderer)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != sourceRenderer)
                {
                    return renderers[i];
                }
            }

            return null;
        }

        private static void SetGameManagerDeckField(
            GameManager manager,
            string fieldName,
            DeckStackView deckView)
        {
            typeof(GameManager)
                .GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, deckView);
        }

        private static void SetDeckClickableKind(
            DeckClickable clickable,
            DeckKind kind)
        {
            typeof(DeckClickable)
                .GetField(
                    "kind",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(clickable, kind);
        }

        private static GameObject GetDisplayModelPrefab(ShopUtilityItemView view)
        {
            FieldInfo field = typeof(ShopUtilityItemView).GetField(
                "displayModelPrefab",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (GameObject)field.GetValue(view);
        }

        private static void EnsureShopUtilityAwake(
            ShopUtilityItemView view,
            string modelName)
        {
            if (view.transform.Find(modelName) != null)
            {
                return;
            }

            MethodInfo awake = typeof(ShopUtilityItemView).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(view, null);
        }

        private static bool ContainsRenderer(
            Renderer[] renderers,
            Renderer renderer)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == renderer)
                {
                    return true;
                }
            }

            return false;
        }

        private static Mesh CreateDuplicatePositionHardNormalMesh()
        {
            var mesh = new Mesh
            {
                name = "Duplicate Position Hard Normal Mesh",
                vertices = new[]
                {
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up,
                },
                normals = new[]
                {
                    Vector3.right,
                    Vector3.up,
                    Vector3.right,
                    Vector3.up,
                },
                triangles = new[]
                {
                    0, 2, 3,
                    1, 3, 2,
                }
            };
            return mesh;
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "Outline Registry Quad",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                triangles = new[]
                {
                    0, 1, 2,
                    2, 1, 3
                }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AssertVectorApproximately(Vector3 actual, Vector3 expected)
        {
            const float tolerance = 0.0001f;
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(tolerance));
        }
    }
}
