using System.Linq;
using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
                Assert.That(hovered.CameraDistance, Is.EqualTo(1.4f));
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
                    Is.EqualTo(restingSortingOrder + 20));
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
