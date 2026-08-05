using System.Linq;
using System.Reflection;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CardSelectionFanLayoutTests
    {
        private const string CardPrefabPath =
            "Assets/03. Prefabs/Card/Card.prefab";
        private const string ManagerPrefabPath =
            "Assets/03. Prefabs/Manager/GameManager.prefab";
        private const string DemonCardPrefabPath =
            "Assets/03. Prefabs/Card/DemonCard.prefab";
        private const string SatanBrandSpritePath =
            "Assets/05. Arts/UI/DevilShape.png";

        [Test]
        public void GSH02_U01_OneCardUsesProfileCenterWithoutCurveOrRotation()
        {
            GameObject root = new GameObject("FanLayoutTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();

                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    0,
                    1,
                    hovered: false,
                    out CardSelectionFanPose pose), Is.True);
                Assert.That(pose.ViewportPosition.x, Is.EqualTo(0.5f));
                Assert.That(pose.ViewportPosition.y, Is.Zero);
                Assert.That(pose.Angle, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U02_TwoCardProfileUsesUpperArchRotation()
        {
            GameObject root = new GameObject("FanLayoutTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    0,
                    2,
                    hovered: false,
                    out CardSelectionFanPose left), Is.True);
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    1,
                    2,
                    hovered: false,
                    out CardSelectionFanPose right), Is.True);

                Assert.That(left.ViewportPosition.x, Is.EqualTo(0.425f));
                Assert.That(right.ViewportPosition.x, Is.EqualTo(0.575f));
                Assert.That(left.ViewportPosition.y, Is.EqualTo(-0.025f));
                Assert.That(right.ViewportPosition.y,
                    Is.EqualTo(left.ViewportPosition.y));
                Assert.That(left.Angle, Is.EqualTo(-10f));
                Assert.That(right.Angle, Is.EqualTo(-left.Angle));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U03_TenCardProfileUsesSameSymmetricFanModel()
        {
            GameObject root = new GameObject("FanLayoutTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TenCards,
                    0,
                    10,
                    hovered: false,
                    out CardSelectionFanPose left), Is.True);
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TenCards,
                    9,
                    10,
                    hovered: false,
                    out CardSelectionFanPose right), Is.True);
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TenCards,
                    4,
                    10,
                    hovered: false,
                    out CardSelectionFanPose inner), Is.True);

                Assert.That(left.ViewportPosition.x, Is.EqualTo(0.25f));
                Assert.That(right.ViewportPosition.x, Is.EqualTo(0.75f));
                Assert.That(left.ViewportPosition.y, Is.EqualTo(0.08f));
                Assert.That(right.ViewportPosition.y,
                    Is.EqualTo(left.ViewportPosition.y));
                Assert.That(inner.ViewportPosition.y,
                    Is.GreaterThan(left.ViewportPosition.y));
                Assert.That(left.Angle, Is.EqualTo(-16f));
                Assert.That(right.Angle, Is.EqualTo(-left.Angle));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U04_HoverLiftsPullsAndStraightensCard()
        {
            GameObject root = new GameObject("FanLayoutTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    0,
                    2,
                    hovered: false,
                    out CardSelectionFanPose resting), Is.True);
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    0,
                    2,
                    hovered: true,
                    out CardSelectionFanPose hovered), Is.True);

                Assert.That(
                    hovered.ViewportPosition.y - resting.ViewportPosition.y,
                    Is.EqualTo(0.18f).Within(0.0001f));
                Assert.That(
                    resting.CameraDistance - hovered.CameraDistance,
                    Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(hovered.Angle, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U05_InvalidPoseRequestLeavesNoPose()
        {
            GameObject root = new GameObject("FanLayoutTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();

                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    0,
                    0,
                    hovered: false,
                    out _), Is.False);
                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TenCards,
                    10,
                    10,
                    hovered: false,
                    out _), Is.False);
                Assert.That(layout.TryGetPose(
                    (CardSelectionFanPreset)999,
                    0,
                    1,
                    hovered: false,
                    out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U06_SelectionViewsShareOneLayoutComponent()
        {
            GameObject root = new GameObject("SharedFanLayoutTest");
            try
            {
                DemonContractSelectionView contract =
                    root.AddComponent<DemonContractSelectionView>();
                CrystalOrbSelectionView crystal =
                    root.AddComponent<CrystalOrbSelectionView>();
                SatanNumberSelectionView satan =
                    root.AddComponent<SatanNumberSelectionView>();
                CardSelectionFanLayout layout =
                    root.GetComponent<CardSelectionFanLayout>();

                Assert.That(
                    root.GetComponents<CardSelectionFanLayout>(),
                    Has.Length.EqualTo(1));
                Assert.That(contract.FanLayout, Is.SameAs(layout));
                Assert.That(crystal.FanLayout, Is.SameAs(layout));
                Assert.That(satan.FanLayout, Is.SameAs(layout));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U07_SatanViewRendersTenCardsWithSharedLayout()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            GameObject root = new GameObject("SatanFanLayoutTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();
                SatanNumberSelectionView view =
                    root.AddComponent<SatanNumberSelectionView>();
                Sprite brandSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    SatanBrandSpritePath);
                view.Initialize(cardPrefab.GetComponent<CardView>(), brandSprite);
                GameSceneCardViewModel[] candidates = Enumerable.Range(1, 10)
                    .Select(rank => new GameSceneCardViewModel(
                        cardId: 100 + rank,
                        rank: rank,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: rank != 1,
                        displayName: rank.ToString(),
                        isSatanBranded: rank == 1,
                        directSelectionCommand: rank == 1
                            ? null
                            : new GameSceneCombatHudCommand(
                                GameSceneCombatHudCommandKind
                                    .ResolveDemonContractChoice,
                                rank)))
                    .ToArray();

                view.Render(candidates, null);

                CardView[] cards = root.GetComponentsInChildren<CardView>(true);
                Assert.That(cards, Has.Length.EqualTo(10));
                Assert.That(cards.All(card => card.gameObject.activeInHierarchy),
                    Is.True);
                Assert.That(view.FanLayout, Is.SameAs(layout));
                Assert.That(cards.Single(card => card.CardId == 101).CanUse,
                    Is.False);
                CardView branded = cards.Single(card => card.CardId == 101);
                SpriteRenderer brand = branded
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "DevilShape");
                Assert.That(brand.sprite, Is.SameAs(brandSprite));
                Assert.That(brand.gameObject.activeSelf, Is.True);
                Assert.That(branded.IsUsedMarkVisible, Is.False);
                SpriteRenderer brandedFront = branded
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "Front");
                Assert.That(brand.sortingOrder,
                    Is.EqualTo(brandedFront.sortingOrder));
                CardView following = cards.Single(card => card.CardId == 102);
                SpriteRenderer followingFront = following
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "Front");
                Assert.That(followingFront.sortingOrder,
                    Is.GreaterThan(brand.sortingOrder));
                Assert.That(cards.Single(card => card.CardId == 101)
                    .DirectSelectionCommand.HasValue, Is.False);
                CardView selectable = cards.Single(card => card.CardId == 102);
                int restingSortingOrder = selectable
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Max(renderer => renderer.sortingOrder);
                view.SetHovered(selectable);
                Assert.That(view.HoveredCandidateIndex, Is.EqualTo(1));
                Assert.That(view.GetCandidate(selectable),
                    Is.SameAs(candidates[1]));
                Assert.That(
                    selectable.DirectSelectionCommand.Value.OptionId,
                    Is.EqualTo(2));
                Assert.That(
                    selectable.GetComponentsInChildren<SpriteRenderer>(true)
                        .Max(renderer => renderer.sortingOrder),
                    Is.EqualTo(restingSortingOrder + 40));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U08_ManagerPrefabAuthorsOneSharedFanLayout()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ManagerPrefabPath);
            CardSelectionFanLayout[] layouts =
                prefab.GetComponents<CardSelectionFanLayout>();
            DemonContractSelectionView contract =
                prefab.GetComponent<DemonContractSelectionView>();

            Assert.That(layouts, Has.Length.EqualTo(1));
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.FanLayout, Is.SameAs(layouts[0]));
            var serializedLayout = new SerializedObject(layouts[0]);
            Assert.That(serializedLayout.FindProperty(
                "twoCardProfile.depthStep").floatValue,
                Is.EqualTo(0.002f));
            Assert.That(serializedLayout.FindProperty(
                "tenCardProfile.depthStep").floatValue,
                Is.EqualTo(0.002f));
        }

        [Test]
        public void GSH02_U09_SatanHoverMovesVisualButKeepsColliderPose()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            GameObject root = new GameObject("SatanHoverColliderTest");
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Camera camera = CreateTestCamera(cameraObject);
                root.AddComponent<CardSelectionFanLayout>();
                SatanNumberSelectionView view =
                    root.AddComponent<SatanNumberSelectionView>();
                view.Initialize(cardPrefab.GetComponent<CardView>());
                GameSceneCardViewModel[] candidates = Enumerable.Range(1, 10)
                    .Select(rank => new GameSceneCardViewModel(
                        200 + rank,
                        rank,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: true,
                        displayName: rank.ToString(),
                        isSatanBranded: rank == 2))
                    .ToArray();
                view.Render(candidates, camera);

                const int hoveredIndex = 1;
                InvokeUpdateSlotPose(view, hoveredIndex);
                CardView card = root.GetComponentsInChildren<CardView>(true)
                    .Single(candidate => candidate.CardId == 202);
                BoxCollider collider = card.GetComponent<BoxCollider>();
                Vector3 rootPosition = card.transform.position;
                Quaternion rootRotation = card.transform.rotation;
                Bounds colliderBounds = collider.bounds;
                Vector3 visualPosition = card.HoverVisualTransform.position;

                view.SetHovered(card);
                InvokeUpdateSlotPose(view, hoveredIndex);

                AssertFixedColliderAndLiftedVisual(
                    card.transform,
                    card.HoverVisualTransform,
                    collider,
                    rootPosition,
                    rootRotation,
                    colliderBounds,
                    visualPosition);
                SpriteRenderer brand = card
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "DevilShape");
                Assert.That(brand.transform.IsChildOf(card.HoverVisualTransform),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void GSH02_U10_CrystalHoverMovesVisualButKeepsColliderPose()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            GameObject root = new GameObject("CrystalHoverColliderTest");
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Camera camera = CreateTestCamera(cameraObject);
                root.AddComponent<CardSelectionFanLayout>();
                CrystalOrbSelectionView view =
                    root.AddComponent<CrystalOrbSelectionView>();
                view.Initialize(cardPrefab.GetComponent<CardView>());
                var candidates = new[]
                {
                    new GameSceneCardViewModel(
                        301, 3, true, true, true, "3"),
                    new GameSceneCardViewModel(
                        302, 7, true, true, true, "7")
                };
                view.Render(candidates, camera);

                InvokeUpdateSlotPose(view, 0);
                CardView card = root.GetComponentsInChildren<CardView>(true)
                    .Single(candidate => candidate.CardId == 301);
                BoxCollider collider = card.GetComponent<BoxCollider>();
                Vector3 rootPosition = card.transform.position;
                Quaternion rootRotation = card.transform.rotation;
                Bounds colliderBounds = collider.bounds;
                Vector3 visualPosition = card.HoverVisualTransform.position;

                view.SetHovered(card);
                InvokeUpdateSlotPose(view, 0);

                AssertFixedColliderAndLiftedVisual(
                    card.transform,
                    card.HoverVisualTransform,
                    collider,
                    rootPosition,
                    rootRotation,
                    colliderBounds,
                    visualPosition);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void GSH02_U11_ContractHoverMovesVisualButKeepsColliderPose()
        {
            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardPrefabPath);
            GameObject root = new GameObject("ContractHoverColliderTest");
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Camera camera = CreateTestCamera(cameraObject);
                root.AddComponent<CardSelectionFanLayout>();
                DemonContractSelectionView view =
                    root.AddComponent<DemonContractSelectionView>();
                SetPrivateField(
                    view,
                    "candidatePrefab",
                    demonPrefab.GetComponent<DemonCardView>());
                InvokePrivateMethod(view, "EnsureSlots");
                var candidates = new[]
                {
                    CreateContractCandidate(1, "mammon"),
                    CreateContractCandidate(2, "satan")
                };
                view.Render(candidates, camera);

                InvokeUpdateSlotPose(view, 0);
                DemonCardView card = root
                    .GetComponentsInChildren<DemonCardView>(true)
                    .Single(candidate => candidate.CardId == 1);
                BoxCollider collider = card.GetComponent<BoxCollider>();
                Vector3 rootPosition = card.transform.position;
                Quaternion rootRotation = card.transform.rotation;
                Bounds colliderBounds = collider.bounds;
                Vector3 visualPosition = card.HoverVisualTransform.position;

                view.SetHovered(card);
                InvokeUpdateSlotPose(view, 0);

                AssertFixedColliderAndLiftedVisual(
                    card.transform,
                    card.HoverVisualTransform,
                    collider,
                    rootPosition,
                    rootRotation,
                    colliderBounds,
                    visualPosition);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [TestCase((int)CardSelectionFanPreset.TwoCards, 2, 1.5f)]
        [TestCase((int)CardSelectionFanPreset.TenCards, 10, 1.55f)]
        public void GSH02_U12_DepthIsUniqueCenteredAndMatchesSortingDirection(
            int presetValue,
            int count,
            float expectedCenterDistance)
        {
            GameObject root = new GameObject("FanDepthTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();
                CardSelectionFanPreset preset =
                    (CardSelectionFanPreset)presetValue;
                var distances = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Assert.That(layout.TryGetPose(
                        preset,
                        i,
                        count,
                        hovered: false,
                        out CardSelectionFanPose pose), Is.True);
                    distances[i] = pose.CameraDistance;
                    if (i > 0)
                    {
                        Assert.That(distances[i], Is.LessThan(distances[i - 1]));
                    }
                }

                Assert.That(distances.Distinct().Count(), Is.EqualTo(count));
                Assert.That(distances.Average(),
                    Is.EqualTo(expectedCenterDistance).Within(0.0001f));
                for (int i = 0; i < count / 2; i++)
                {
                    Assert.That(
                        (distances[i] + distances[count - 1 - i]) * 0.5f,
                        Is.EqualTo(expectedCenterDistance).Within(0.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U13_SatanSelectionUsesFifoToggleAndHoverSuppression()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            GameObject root = new GameObject("SatanMultiSelectionTest");
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Camera camera = CreateTestCamera(cameraObject);
                root.AddComponent<CardSelectionFanLayout>();
                SatanNumberSelectionView view =
                    root.AddComponent<SatanNumberSelectionView>();
                view.Initialize(cardPrefab.GetComponent<CardView>());
                GameSceneCardViewModel[] candidates = Enumerable.Range(1, 10)
                    .Select(rank => new GameSceneCardViewModel(
                        400 + rank,
                        rank,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: true,
                        displayName: rank.ToString(),
                        directSelectionCommand:
                            new GameSceneCombatHudCommand(
                                GameSceneCombatHudCommandKind
                                    .ResolveDemonContractChoice,
                                rank,
                                interactionId: 77)))
                    .ToArray();
                view.Render(candidates, camera, interactionId: 77);
                CardView[] cards = root.GetComponentsInChildren<CardView>(true);
                CardView two = cards.Single(card => card.CardId == 402);
                CardView four = cards.Single(card => card.CardId == 404);
                CardView six = cards.Single(card => card.CardId == 406);

                Assert.That(view.TryToggleSelection(two), Is.True);
                Assert.That(view.SelectedCount, Is.EqualTo(1));
                Assert.That(view.TryToggleSelection(four), Is.True);
                Assert.That(view.TryGetSelectedNumbers(
                    out int first,
                    out int second), Is.True);
                Assert.That((first, second), Is.EqualTo((2, 4)));

                Assert.That(view.TryToggleSelection(six), Is.True);
                Assert.That(view.TryGetSelectedNumbers(out first, out second),
                    Is.True);
                Assert.That((first, second), Is.EqualTo((4, 6)));

                view.SetHovered(four);
                Assert.That(view.TryToggleSelection(four), Is.True);
                InvokeUpdateSlotPose(view, 3);
                Vector3 suppressedPosition = four.HoverVisualTransform.position;
                view.SetHovered(four);
                InvokeUpdateSlotPose(view, 3);
                Assert.That(Vector3.Distance(
                    four.HoverVisualTransform.position,
                    suppressedPosition), Is.LessThan(0.0001f));

                view.Render(candidates, camera, interactionId: 77);
                view.SetHovered(four);
                InvokeUpdateSlotPose(view, 3);
                Assert.That(Vector3.Distance(
                    four.HoverVisualTransform.position,
                    suppressedPosition), Is.LessThan(0.0001f));

                view.SetHovered(null);
                view.SetHovered(four);
                InvokeUpdateSlotPose(view, 3);
                Assert.That(four.HoverVisualTransform.position.y,
                    Is.GreaterThan(suppressedPosition.y));
                Assert.That(view.SelectedCount, Is.EqualTo(1));

                view.Render(candidates, camera, interactionId: 78);
                Assert.That(view.SelectedCount, Is.Zero);
                Assert.That(view.TryGetSelectedNumbers(out _, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void GSH02_U14_SatanSelectedAndHoveredSortingStayAboveRestingCards()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            GameObject root = new GameObject("SatanSelectionSortingTest");
            try
            {
                root.AddComponent<CardSelectionFanLayout>();
                SatanNumberSelectionView view =
                    root.AddComponent<SatanNumberSelectionView>();
                view.Initialize(cardPrefab.GetComponent<CardView>());
                GameSceneCardViewModel[] candidates = Enumerable.Range(1, 10)
                    .Select(rank => new GameSceneCardViewModel(
                        500 + rank,
                        rank,
                        true,
                        true,
                        true,
                        rank.ToString(),
                        directSelectionCommand:
                            new GameSceneCombatHudCommand(
                                GameSceneCombatHudCommandKind
                                    .ResolveDemonContractChoice,
                                rank)))
                    .ToArray();
                view.Render(candidates, null, interactionId: 91);
                CardView[] cards = root.GetComponentsInChildren<CardView>(true);
                CardView selected = cards.Single(card => card.CardId == 502);
                CardView hovered = cards.Single(card => card.CardId == 504);
                int selectedRestingOrder = selected
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Max(renderer => renderer.sortingOrder);
                int hoveredRestingOrder = hovered
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Max(renderer => renderer.sortingOrder);

                Assert.That(view.TryToggleSelection(selected), Is.True);
                view.SetHovered(hovered);

                Assert.That(selected
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Max(renderer => renderer.sortingOrder),
                    Is.EqualTo(selectedRestingOrder + 20));
                Assert.That(hovered
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Max(renderer => renderer.sortingOrder),
                    Is.EqualTo(hoveredRestingOrder + 40));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GSH02_U15_InvalidCameraDistanceIsClampedAboveZero()
        {
            GameObject root = new GameObject("FanDistanceClampTest");
            try
            {
                CardSelectionFanLayout layout =
                    root.AddComponent<CardSelectionFanLayout>();
                var serializedLayout = new SerializedObject(layout);
                serializedLayout.FindProperty(
                    "twoCardProfile.cameraDistance").floatValue = -10f;
                serializedLayout.ApplyModifiedPropertiesWithoutUndo();
                InvokePrivateMethod(layout, "OnValidate");

                Assert.That(layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    0,
                    2,
                    hovered: true,
                    out CardSelectionFanPose pose), Is.True);
                Assert.That(pose.CameraDistance,
                    Is.GreaterThanOrEqualTo(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Category("GSV15")]
        public void GSV15_U01_SelectionCardsUseTextUiLayerAndUnlitMaterials()
        {
            int textUiLayer = LayerMask.NameToLayer("TextUI");
            Assert.That(textUiLayer, Is.GreaterThanOrEqualTo(0));

            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardPrefabPath);
            Material cardSourceMaterial = FindLightingMaterial(cardPrefab);
            Material demonSourceMaterial = FindLightingMaterial(demonPrefab);
            float cardSourceLightingMode =
                cardSourceMaterial.GetFloat("_LightingMode");
            float demonSourceLightingMode =
                demonSourceMaterial.GetFloat("_LightingMode");
            bool cardSourceWasUnlit =
                cardSourceMaterial.IsKeywordEnabled("_UNLIT_ON");
            bool demonSourceWasUnlit =
                demonSourceMaterial.IsKeywordEnabled("_UNLIT_ON");
            GameObject root = new GameObject("SelectionTextUILayerTest");
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Camera camera = CreateTestCamera(cameraObject);
                root.AddComponent<CardSelectionFanLayout>();

                CrystalOrbSelectionView crystal =
                    root.AddComponent<CrystalOrbSelectionView>();
                crystal.Initialize(cardPrefab.GetComponent<CardView>());

                SatanNumberSelectionView satan =
                    root.AddComponent<SatanNumberSelectionView>();
                satan.Initialize(cardPrefab.GetComponent<CardView>());

                DemonContractSelectionView contract =
                    root.AddComponent<DemonContractSelectionView>();
                SetPrivateField(
                    contract,
                    "candidatePrefab",
                    demonPrefab.GetComponent<DemonCardView>());
                InvokePrivateMethod(contract, "EnsureSlots");

                crystal.Render(
                    new[]
                    {
                        new GameSceneCardViewModel(
                            1001, 1, true, true, true, "1"),
                        new GameSceneCardViewModel(
                            1002, 2, true, true, true, "2")
                    },
                    camera);
                satan.Render(
                    Enumerable.Range(1, 10)
                        .Select(rank => new GameSceneCardViewModel(
                            2000 + rank,
                            rank,
                            true,
                            true,
                            true,
                            rank.ToString()))
                        .ToArray(),
                    camera);
                contract.Render(
                    new[]
                    {
                        CreateContractCandidate(3001, "mammon"),
                        CreateContractCandidate(3002, "satan")
                    },
                    camera);

                Transform[] selectionAnchors = root
                    .GetComponentsInChildren<Transform>(true)
                    .Where(item =>
                        item.name.StartsWith("CrystalOrbCandidate_") ||
                        item.name.StartsWith("SatanNumberCandidate_") ||
                        item.name.StartsWith("ContractCandidate_"))
                    .ToArray();

                Assert.That(selectionAnchors, Has.Length.EqualTo(14));
                foreach (Transform anchor in selectionAnchors)
                {
                    AssertLayerRecursively(anchor, textUiLayer);
                    AssertUnlitPresentation(anchor);
                }

                AssertSourceMaterialUnchanged(
                    cardSourceMaterial,
                    cardSourceLightingMode,
                    cardSourceWasUnlit);
                AssertSourceMaterialUnchanged(
                    demonSourceMaterial,
                    demonSourceLightingMode,
                    demonSourceWasUnlit);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        [Category("GSV15")]
        public void GSV15_U02_StartingDemonRevealUsesTextUiLayerAndUnlitMaterials()
        {
            int textUiLayer = LayerMask.NameToLayer("TextUI");
            Assert.That(textUiLayer, Is.GreaterThanOrEqualTo(0));

            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardPrefabPath);
            Material sourceMaterial = FindLightingMaterial(demonPrefab);
            float sourceLightingMode = sourceMaterial.GetFloat("_LightingMode");
            bool sourceWasUnlit =
                sourceMaterial.IsKeywordEnabled("_UNLIT_ON");
            GameObject root = new GameObject("StartingDemonTextUILayerTest");
            try
            {
                StartingDemonRevealView view =
                    root.AddComponent<StartingDemonRevealView>();
                SetPrivateField(view, "demonCardPrefab", demonPrefab);
                MethodInfo createCard = typeof(StartingDemonRevealView).GetMethod(
                    "CreateCard",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(createCard, Is.Not.Null);

                var model = new StartingDemonGrantCardViewModel(
                    "satan",
                    "Satan",
                    "Ability",
                    "Cost");
                GameObject card = (GameObject)createCard.Invoke(
                    view,
                    new object[]
                    {
                        model,
                        11000,
                        true,
                        Vector3.zero,
                        Quaternion.identity
                    });

                Assert.That(card, Is.Not.Null);
                AssertLayerRecursively(card.transform, textUiLayer);
                AssertUnlitPresentation(card.transform);
                AssertSourceMaterialUnchanged(
                    sourceMaterial,
                    sourceLightingMode,
                    sourceWasUnlit);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Camera CreateTestCamera(GameObject cameraObject)
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static GameSceneCombatHudContractCandidateViewModel
            CreateContractCandidate(int optionId, string definitionKey)
        {
            return new GameSceneCombatHudContractCandidateViewModel(
                new GameSceneCombatHudCommand(
                    GameSceneCombatHudCommandKind.ResolveDemonContractChoice,
                    optionId),
                definitionKey,
                definitionKey,
                "Ability",
                "Cost",
                isInteractable: true);
        }

        private static void AssertFixedColliderAndLiftedVisual(
            Transform cardRoot,
            Transform visual,
            BoxCollider collider,
            Vector3 rootPosition,
            Quaternion rootRotation,
            Bounds colliderBounds,
            Vector3 visualPosition)
        {
            Assert.That(visual, Is.Not.SameAs(cardRoot));
            Assert.That(Vector3.Distance(cardRoot.position, rootPosition),
                Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(cardRoot.rotation, rootRotation),
                Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(collider.bounds.center, colliderBounds.center),
                Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(collider.bounds.size, colliderBounds.size),
                Is.LessThan(0.0001f));
            Assert.That(visual.position.y, Is.GreaterThan(visualPosition.y));
            Assert.That(visual.position.z, Is.LessThan(visualPosition.z));
        }

        private static void AssertLayerRecursively(
            Transform root,
            int expectedLayer)
        {
            Transform[] hierarchy = root.GetComponentsInChildren<Transform>(true);
            Assert.That(hierarchy, Is.Not.Empty);
            Assert.That(
                hierarchy.All(item => item.gameObject.layer == expectedLayer),
                Is.True);
        }

        private static void AssertUnlitPresentation(Transform root)
        {
            SpriteRenderer[] renderers = root
                .GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer =>
                    renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.HasProperty("_LightingMode"))
                .ToArray();
            Assert.That(renderers, Has.Length.GreaterThanOrEqualTo(2));
            foreach (SpriteRenderer renderer in renderers)
            {
                Assert.That(
                    renderer.sharedMaterial.GetFloat("_LightingMode"),
                    Is.EqualTo(1f));
                Assert.That(
                    renderer.sharedMaterial.IsKeywordEnabled("_UNLIT_ON"),
                    Is.True);
                Assert.That(
                    renderer.shadowCastingMode,
                    Is.EqualTo(ShadowCastingMode.Off));
                Assert.That(renderer.receiveShadows, Is.False);
                Assert.That(
                    renderer.lightProbeUsage,
                    Is.EqualTo(LightProbeUsage.Off));
                Assert.That(
                    renderer.reflectionProbeUsage,
                    Is.EqualTo(ReflectionProbeUsage.Off));
            }
        }

        private static Material FindLightingMaterial(GameObject prefab)
        {
            Material material = prefab
                .GetComponentsInChildren<SpriteRenderer>(true)
                .Select(renderer => renderer.sharedMaterial)
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    candidate.HasProperty("_LightingMode"));
            Assert.That(material, Is.Not.Null);
            return material;
        }

        private static void AssertSourceMaterialUnchanged(
            Material material,
            float lightingMode,
            bool wasUnlit)
        {
            Assert.That(material.GetFloat("_LightingMode"), Is.EqualTo(lightingMode));
            Assert.That(material.IsKeywordEnabled("_UNLIT_ON"), Is.EqualTo(wasUnlit));
        }

        private static void InvokeUpdateSlotPose(MonoBehaviour view, int index)
        {
            MethodInfo method = view.GetType().GetMethod(
                "UpdateSlotPose",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(view, new object[] { index, true });
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

    }
}
