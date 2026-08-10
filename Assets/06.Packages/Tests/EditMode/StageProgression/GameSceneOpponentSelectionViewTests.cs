using System;
using System.Linq;
using System.Reflection;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using DiaBlackJack.GameScene.Editor;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.StageProgression.Tests
{
    [Category("GSV10")]
    public sealed class GameSceneOpponentSelectionViewTests
    {
        private const string PosterPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/OpponentWantedPoster.prefab";
        private const string OverlayPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/OpponentSelectionOverlay.prefab";
        private const string WorldPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/OpponentSelectionWorld.prefab";
        private const string GameScenePath =
            "Assets/00. Scenes/GameScene.unity";
        private const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";
        private const string WantedSpritePath =
            "Assets/05. Arts/UI/wanted.png";

        [Test]
        public void GSV10_U01_OverlaySerializesTwoPostersWithoutButtons()
        {
            GameObject overlay = AssetDatabase.LoadAssetAtPath<GameObject>(
                OverlayPrefabPath);
            GameObject poster = AssetDatabase.LoadAssetAtPath<GameObject>(
                PosterPrefabPath);

            Assert.That(overlay, Is.Not.Null);
            Assert.That(poster, Is.Not.Null);
            Assert.That(overlay.GetComponent<Canvas>().sortingOrder,
                Is.EqualTo(110));

            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution,
                Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(overlay.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(overlay.GetComponentsInChildren<Button>(true), Is.Empty);

            OpponentSelectionView selection =
                overlay.GetComponent<OpponentSelectionView>();
            Assert.That(selection, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(selection);
            GameObject contentRoot = serialized.FindProperty("contentRoot")
                .objectReferenceValue as GameObject;
            Assert.That(contentRoot, Is.Not.Null);
            Assert.That(contentRoot.activeSelf, Is.False);
            Assert.That(
                serialized.FindProperty("enemyContentCatalog").objectReferenceValue,
                Is.Not.Null);
            Assert.That(serialized.FindProperty("posterSlots").arraySize,
                Is.EqualTo(2));

            Image background = poster.GetComponent<Image>();
            Assert.That(background, Is.Not.Null);
            Assert.That(background.sprite,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(
                    WantedSpritePath)));
            Assert.That(background.raycastTarget, Is.True);
            Assert.That(
                poster.GetComponentsInChildren<Graphic>(true)
                    .Where(graphic => graphic.gameObject != poster)
                    .All(graphic => !graphic.raycastTarget),
                Is.True);

            Component description = FindText(poster, "Description");
            Assert.That(
                description.GetType().GetProperty("overflowMode")
                    .GetValue(description).ToString(),
                Is.EqualTo("Ellipsis"));
        }

        [Test]
        public void GSV10_U02_PosterRendersPortraitAndAllWantedFields()
        {
            GameObject instance = CreatePosterInstance();
            try
            {
                OpponentWantedPosterView view =
                    instance.GetComponent<OpponentWantedPosterView>();
                Sprite portrait = LoadCatalog().GetPortrait("gunslinger");
                OpponentCandidateViewModel candidate = CreateCandidate();

                view.Render(candidate, portrait, true);

                Assert.That(view.DisplayedProfileKey, Is.EqualTo("gunslinger"));
                Assert.That(GetText(FindText(instance, "EnemyName")),
                    Is.EqualTo("검은 모자 총잡이"));
                Assert.That(GetText(FindText(instance, "SoulAmount")),
                    Is.EqualTo("×3"));
                Assert.That(GetText(FindText(instance, "Description")),
                    Is.EqualTo("공개 정보로 패를 추론하는 냉정한 총잡이."));
                Component description = FindText(instance, "Description");
                Assert.That(
                    description.GetType().GetProperty("maxVisibleLines")
                        .GetValue(description),
                    Is.EqualTo(3));
                Assert.That(GetText(FindText(instance, "DefeatGoldAmount")),
                    Is.EqualTo("×3"));
                Assert.That(FindImage(instance, "Portrait").sprite,
                    Is.SameAs(portrait));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV10_U03_HoverAppliesAndRestoresOutlineAndScale()
        {
            GameObject instance = CreatePosterInstance();
            try
            {
                OpponentWantedPosterView view =
                    instance.GetComponent<OpponentWantedPosterView>();
                view.Render(CreateCandidate(),
                    LoadCatalog().GetPortrait("gunslinger"), true);
                Vector3 restingScale = instance.transform.localScale;

                view.OnPointerEnter(null);

                Assert.That(view.IsHovered, Is.True);
                Assert.That(view.IsHoverOutlineVisible, Is.True);
                Assert.That(instance.transform.localScale.x,
                    Is.EqualTo(restingScale.x * 1.04f).Within(0.0001f));

                view.OnPointerExit(null);

                Assert.That(view.IsHovered, Is.False);
                Assert.That(view.IsHoverOutlineVisible, Is.False);
                Assert.That(instance.transform.localScale,
                    Is.EqualTo(restingScale));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV10_U04_OnlyFirstEnabledLeftClickSelects()
        {
            GameObject instance = CreatePosterInstance();
            try
            {
                OpponentWantedPosterView view =
                    instance.GetComponent<OpponentWantedPosterView>();
                view.Render(CreateCandidate(),
                    LoadCatalog().GetPortrait("gunslinger"), true);
                int selectionCount = 0;
                string selectedKey = null;
                view.Selected += key =>
                {
                    selectionCount++;
                    selectedKey = key;
                };

                view.OnPointerClick(new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Right
                });
                Assert.That(selectionCount, Is.Zero);

                PointerEventData leftClick = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };
                view.OnPointerClick(leftClick);
                view.OnPointerClick(leftClick);

                Assert.That(selectionCount, Is.EqualTo(1));
                Assert.That(selectedKey, Is.EqualTo("gunslinger"));

                view.Render(CreateCandidate(),
                    LoadCatalog().GetPortrait("gunslinger"), false);
                view.OnPointerClick(leftClick);
                Assert.That(selectionCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV10_U05_EditorPreviewUsesExistingSlotsAndRestores()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(prefab);
            OpponentSelectionView view =
                overlay.GetComponent<OpponentSelectionView>();
            SerializedObject serialized = new SerializedObject(view);
            GameObject contentRoot = serialized.FindProperty("contentRoot")
                .objectReferenceValue as GameObject;
            OpponentWantedPosterView[] slots = GetPosterSlots(serialized);
            Component firstName = FindText(slots[0].gameObject, "EnemyName");
            Image firstPortrait = FindImage(
                slots[0].gameObject,
                "Portrait");
            bool contentWasActive = contentRoot.activeSelf;
            string authoredName = GetText(firstName);
            Sprite authoredPortrait = firstPortrait.sprite;
            int posterCount = overlay.GetComponentsInChildren<
                OpponentWantedPosterView>(true).Length;

            try
            {
                Assert.That(
                    OpponentWantedPreviewSession.Show(view),
                    Is.Null);
                OpponentWantedPreviewSession session =
                    OpponentWantedPreviewSession.GetActive(view);
                Assert.That(session, Is.Not.Null);
                Assert.That(contentRoot.activeSelf, Is.True);
                Assert.That(
                    overlay.GetComponentsInChildren<
                        OpponentWantedPosterView>(true).Length,
                    Is.EqualTo(posterCount));

                EnemyContentCatalogSO catalog = LoadCatalog();
                EnemyCombatProfile firstProfile =
                    catalog.BuildRuntimeCatalog().Profiles[0];
                GoldRewardCatalog gold = catalog.BuildGoldRewardCatalog();
                Assert.That(slots[0].DisplayedProfileKey,
                    Is.EqualTo(firstProfile.Key));
                Assert.That(GetText(firstName),
                    Is.EqualTo(firstProfile.DisplayName));
                Assert.That(
                    GetText(FindText(slots[0].gameObject, "SoulAmount")),
                    Is.EqualTo($"×{firstProfile.MaximumSoul}"));
                Assert.That(
                    GetText(FindText(
                        slots[0].gameObject,
                        "DefeatGoldAmount")),
                    Is.EqualTo($"×{gold.GetAmount(firstProfile.Key)}"));
                Assert.That(firstPortrait.sprite,
                    Is.EqualTo(catalog.GetPortrait(firstProfile.Key)));

                Assert.That(
                    OpponentWantedPreviewSession.SetHover(view, 0),
                    Is.Null);
                Assert.That(slots[0].IsHovered, Is.True);
                Assert.That(slots[0].IsHoverOutlineVisible, Is.True);
                Assert.That(
                    OpponentWantedPreviewSession.MoveNext(view),
                    Is.Null);
                Assert.That(session.CurrentPageIndex, Is.EqualTo(1));

                OpponentWantedPreviewSession.StopActive();
                Assert.That(contentRoot.activeSelf,
                    Is.EqualTo(contentWasActive));
                Assert.That(GetText(firstName), Is.EqualTo(authoredName));
                Assert.That(firstPortrait.sprite, Is.EqualTo(authoredPortrait));
            }
            finally
            {
                OpponentWantedPreviewSession.StopActive();
                UnityEngine.Object.DestroyImmediate(overlay);
            }
        }

        [Test]
        public void GSV10_U06_EditorPreviewResumesAfterSaveAndKeepsLayoutEdit()
        {
            GameObject instance = CreatePosterInstance();
            OpponentWantedPosterView view =
                instance.GetComponent<OpponentWantedPosterView>();
            RectTransform rect = instance.transform as RectTransform;

            try
            {
                Assert.That(
                    OpponentWantedPreviewSession.Show(view),
                    Is.Null);
                Assert.That(
                    OpponentWantedPreviewSession.MoveNext(view),
                    Is.Null);
                OpponentWantedPreviewSession beforeSave =
                    OpponentWantedPreviewSession.GetActive(view);
                int pageIndex = beforeSave.CurrentPageIndex;
                Vector2 editedPosition =
                    rect.anchoredPosition + new Vector2(13f, -7f);
                rect.anchoredPosition = editedPosition;

                OpponentWantedPreviewLifecycle.SuspendForPrefabSave(instance);
                Assert.That(
                    OpponentWantedPreviewSession.GetActive(view),
                    Is.Null);
                Assert.That(rect.anchoredPosition,
                    Is.EqualTo(editedPosition));

                OpponentWantedPreviewLifecycle.ResumeAfterPrefabSave(instance);
                OpponentWantedPreviewSession afterSave =
                    OpponentWantedPreviewSession.GetActive(view);
                Assert.That(afterSave, Is.Not.Null);
                Assert.That(afterSave.CurrentPageIndex,
                    Is.EqualTo(pageIndex));
                Assert.That(rect.anchoredPosition,
                    Is.EqualTo(editedPosition));
            }
            finally
            {
                OpponentWantedPreviewSession.StopActive();
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV10_U07_InitialHidePreservesPrefabAuthoredPosterScale()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                OverlayPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);

            try
            {
                OpponentSelectionView selection =
                    instance.GetComponent<OpponentSelectionView>();
                OpponentWantedPosterView[] posters =
                    instance.GetComponentsInChildren<OpponentWantedPosterView>(true);
                Vector3[] authoredScales = posters
                    .Select(poster => poster.transform.localScale)
                    .ToArray();

                Assert.That(selection, Is.Not.Null);
                Assert.That(posters.Length, Is.EqualTo(2));

                selection.Hide();

                for (int index = 0; index < posters.Length; index++)
                {
                    Assert.That(
                        posters[index].transform.localScale,
                        Is.EqualTo(authoredScales[index]));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV20")]
        public void GSV20_U01_WorldPrefabKeepsLegacyAssetAndSerializesEntrance()
        {
            GameObject legacy = AssetDatabase.LoadAssetAtPath<GameObject>(
                OverlayPrefabPath);
            GameObject world = AssetDatabase.LoadAssetAtPath<GameObject>(
                WorldPrefabPath);

            Assert.That(legacy, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(legacy.GetComponent<Canvas>().renderMode,
                Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(world.GetComponent<Canvas>().renderMode,
                Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(world.transform.localScale,
                Is.EqualTo(Vector3.one * 0.003f));
            Assert.That(Vector3.Angle(world.transform.forward, Vector3.down),
                Is.LessThan(0.01f));

            OpponentSelectionView selection =
                world.GetComponent<OpponentSelectionView>();
            SerializedObject serialized = new SerializedObject(selection);
            Assert.That(serialized.FindProperty("playEntranceAnimation")
                .boolValue, Is.True);
            Assert.That(serialized.FindProperty("slideDuration").floatValue,
                Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(serialized.FindProperty("offTableAnchoredY").floatValue,
                Is.EqualTo(1100f));
            Assert.That(serialized.FindProperty("posterSlots").arraySize,
                Is.EqualTo(2));

            OpponentWantedPosterView[] posters =
                world.GetComponentsInChildren<OpponentWantedPosterView>(true);
            Assert.That(posters.Select(poster =>
                    ((RectTransform)poster.transform).anchoredPosition.x),
                Is.EquivalentTo(new[] { -325f, 325f }));
            Assert.That(posters.All(poster =>
                    Mathf.Approximately(
                        ((RectTransform)poster.transform).anchoredPosition.y,
                        0f)),
                Is.True);
            Assert.That(posters.All(poster =>
                    Vector3.Distance(
                        poster.transform.localScale,
                        Vector3.one * 1.105f) < 0.0001f),
                Is.True);
            Assert.That(posters.All(poster =>
                    Mathf.Abs(Mathf.DeltaAngle(
                        poster.transform.localEulerAngles.z,
                        0f)) < 0.01f),
                Is.True);
        }

        [Test]
        [Category("GSV20")]
        public void GSV20_U02_EntranceStateBlocksPosterUntilCompleted()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                WorldPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            GameObject cameraObject = new GameObject("CameraViewController");

            try
            {
                OpponentSelectionView selection =
                    instance.GetComponent<OpponentSelectionView>();
                GameSceneCameraViewController cameraController =
                    cameraObject.AddComponent<GameSceneCameraViewController>();
                SerializedObject serializedSelection =
                    new SerializedObject(selection);
                serializedSelection.FindProperty("cameraViewController")
                    .objectReferenceValue = cameraController;
                serializedSelection.ApplyModifiedPropertiesWithoutUndo();
                MethodInfo subscribeSlots = typeof(OpponentSelectionView)
                    .GetMethod(
                        "SetSlotSubscriptions",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(subscribeSlots, Is.Not.Null);
                subscribeSlots.Invoke(selection, new object[] { true });
                selection.Render(CreateSelectionModel(11));
                Assert.That(selection.IsReadyForSelection, Is.True);

                int selectionCount = 0;
                string selectedProfileKey = null;
                selection.OpponentSelected += profileKey =>
                {
                    selectionCount++;
                    selectedProfileKey = profileKey;
                };
                OpponentWantedPosterView poster =
                    instance.GetComponentsInChildren<OpponentWantedPosterView>(true)[0];
                PointerEventData click = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                selection.BeginEntranceState();
                poster.OnPointerClick(click);
                Assert.That(selection.IsReadyForSelection, Is.False);
                Assert.That(selectionCount, Is.Zero);

                selection.CompleteEntranceState();
                poster.OnPointerClick(click);
                Assert.That(selectionCount, Is.EqualTo(1));
                Assert.That(selection.IsReadyForSelection, Is.False);
                Assert.That(selectedProfileKey, Is.Not.Null.And.Not.Empty);
                Assert.That(selection.CanCommitSelection(
                        11,
                        selectedProfileKey),
                    Is.True);
                Assert.That(selection.CanCommitSelection(
                        12,
                        selectedProfileKey),
                    Is.False);
                Assert.That(cameraController.IsSwitchInputLocked, Is.True);
                CanvasGroup[] posterGroups = instance
                    .GetComponentsInChildren<CanvasGroup>(true)
                    .Where(group => group.GetComponent<
                        OpponentWantedPosterView>() != null)
                    .ToArray();
                Assert.That(posterGroups, Has.Length.EqualTo(2));
                Assert.That(posterGroups.All(group =>
                        Mathf.Approximately(group.alpha, 0f)),
                    Is.True);

                Assert.That(selection.RestoreSelectionAfterRejectedCommit(
                        selectedProfileKey),
                    Is.True);
                Assert.That(selection.IsReadyForSelection, Is.True);
                Assert.That(selection.CanCommitSelection(
                        11,
                        selectedProfileKey),
                    Is.False);
                Assert.That(cameraController.IsSwitchInputLocked, Is.True);
                Assert.That(posterGroups.All(group =>
                        Mathf.Approximately(group.alpha, 1f)),
                    Is.True);

                selection.Hide();
                Assert.That(cameraController.IsSwitchInputLocked, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        [Category("GSV20")]
        public void GSV20_U03_CameraSwitchInputUsesNestedLocks()
        {
            GameObject instance = new GameObject("CameraViewController");
            try
            {
                GameSceneCameraViewController controller =
                    instance.AddComponent<GameSceneCameraViewController>();
                Assert.That(controller.IsSwitchInputLocked, Is.False);

                controller.LockSwitchInput();
                controller.LockSwitchInput();
                Assert.That(controller.IsSwitchInputLocked, Is.True);
                Assert.That(controller.StepView(1), Is.False);

                controller.UnlockSwitchInput();
                Assert.That(controller.IsSwitchInputLocked, Is.True);
                controller.UnlockSwitchInput();
                Assert.That(controller.IsSwitchInputLocked, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV20")]
        public void GSV20_U04_FinalBossUsesCenteredWorldPosterAndRestoresPair()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                WorldPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);

            try
            {
                OpponentSelectionView selection =
                    instance.GetComponent<OpponentSelectionView>();
                SerializedObject serialized = new SerializedObject(selection);
                OpponentWantedPosterView[] slots = GetPosterSlots(serialized);

                selection.RenderFinalBossReveal(
                    CreateCandidate(),
                    "boss-stage");

                Assert.That(selection.IsVisible, Is.True);
                Assert.That(selection.IsReadyForSelection, Is.True);
                Assert.That(slots[0].gameObject.activeSelf, Is.True);
                Assert.That(slots[1].gameObject.activeSelf, Is.False);
                Assert.That(
                    ((RectTransform)slots[0].transform).anchoredPosition,
                    Is.EqualTo(Vector2.zero));

                selection.Render(CreateSelectionModel(11));

                Assert.That(slots.All(slot => slot.gameObject.activeSelf),
                    Is.True);
                Assert.That(slots.Select(slot =>
                        ((RectTransform)slot.transform).anchoredPosition.x),
                    Is.EquivalentTo(new[] { -325f, 325f }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV20")]
        public void GSV20_U05_FinalBossBlocksClickAndRestoresRejectedCommit()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                WorldPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);

            try
            {
                OpponentSelectionView selection =
                    instance.GetComponent<OpponentSelectionView>();
                SerializedObject serialized = new SerializedObject(selection);
                OpponentWantedPosterView poster =
                    GetPosterSlots(serialized)[0];
                int selectionCount = 0;
                string selectedProfileKey = null;
                selection.OpponentSelected += profileKey =>
                {
                    selectionCount++;
                    selectedProfileKey = profileKey;
                };
                PointerEventData click = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                selection.RenderFinalBossReveal(
                    CreateCandidate(),
                    "boss-stage");
                selection.BeginEntranceState();
                poster.OnPointerClick(click);
                Assert.That(selectionCount, Is.Zero);

                selection.CompleteEntranceState();
                poster.OnPointerClick(click);
                Assert.That(selectionCount, Is.EqualTo(1));
                Assert.That(selectedProfileKey, Is.EqualTo("gunslinger"));
                Assert.That(selection.CanCommitFinalBossReveal(
                        "boss-stage",
                        selectedProfileKey),
                    Is.True);
                Assert.That(selection.CanCommitFinalBossReveal(
                        "other-stage",
                        selectedProfileKey),
                    Is.False);

                Assert.That(selection
                        .RestoreFinalBossRevealAfterRejectedCommit(
                            "boss-stage",
                            selectedProfileKey),
                    Is.True);
                Assert.That(selection.IsReadyForSelection, Is.True);
                Assert.That(
                    ((RectTransform)poster.transform).anchoredPosition,
                    Is.EqualTo(Vector2.zero));
                Assert.That(
                    poster.GetComponent<CanvasGroup>().alpha,
                    Is.EqualTo(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSV20")]
        public void GSV20_U06_GameSceneUsesOneLoweredWorldPosterView()
        {
            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedForTest = !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(
                    GameScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                Transform[] transforms = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<Transform>(true))
                    .ToArray();
                Transform worldSelection = transforms.Single(transform =>
                    transform.name == "WorldOpponentSelection");
                MeshRenderer table = transforms
                    .Single(transform => transform.name == "Table")
                    .GetComponent<MeshRenderer>();
                GameFlowController flow = transforms
                    .Select(transform =>
                        transform.GetComponent<GameFlowController>())
                    .Single(controller => controller != null);
                SerializedObject serializedFlow =
                    new SerializedObject(flow);

                Assert.That(worldSelection.position.y,
                    Is.EqualTo(3.54f).Within(0.0001f));
                Assert.That(worldSelection.position.y,
                    Is.GreaterThan(table.bounds.max.y));
                Assert.That(transforms.Count(transform =>
                        transform.GetComponent<OpponentSelectionView>() != null),
                    Is.EqualTo(1));
                Assert.That(transforms.Any(transform =>
                        transform.name == "WantedPosterFinalBoss"),
                    Is.False);
                Assert.That(transforms.Any(transform =>
                        transform.name == "UIOpponentSelection_Legacy"),
                    Is.False);
                Assert.That(serializedFlow.FindProperty("opponentSelection")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(serializedFlow.FindProperty("finalBossReveal"),
                    Is.Null);
            }
            finally
            {
                if (openedForTest)
                {
                    EditorSceneManager.CloseScene(
                        scene,
                        removeScene: true);
                }
            }
        }

        private static GameObject CreatePosterInstance()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PosterPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            return UnityEngine.Object.Instantiate(prefab);
        }

        private static EnemyContentCatalogSO LoadCatalog()
        {
            EnemyContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog;
        }

        private static OpponentCandidateViewModel CreateCandidate()
        {
            return new OpponentCandidateViewModel(
                "gunslinger",
                "검은 모자 총잡이",
                "NORMAL",
                "SOUL 3",
                "공개 정보로 패를 추론하는 냉정한 총잡이.",
                "VICTORY GOLD 3",
                "×3",
                "×3",
                false);
        }

        private static StageProgressionViewModel CreateSelectionModel(
            int offerId)
        {
            return new StageProgressionViewModel(
                "1/3",
                "Opponent",
                "Normal",
                "12/12",
                StageProgressionState.OpponentSelection,
                string.Empty,
                false,
                false,
                false,
                string.Empty,
                Array.Empty<BattleRewardOptionViewModel>(),
                false,
                false,
                string.Empty,
                string.Empty,
                0,
                offerId,
                new[] { CreateCandidate(), new OpponentCandidateViewModel(
                    "cultist",
                    "Cultist",
                    "NORMAL",
                    "SOUL 4",
                    "Summary",
                    "VICTORY GOLD 4",
                    "4",
                    "4",
                    false) },
                null,
                true,
                false,
                null,
                Array.Empty<StartingDemonGrantCardViewModel>(),
                false,
                "0",
                string.Empty,
                false,
                null,
                Array.Empty<ShopCardOptionViewModel>(),
                Array.Empty<ShopOwnedCardViewModel>(),
                string.Empty,
                0,
                false,
                string.Empty,
                0,
                false,
                false,
                false,
                string.Empty);
        }

        private static Component FindText(GameObject root, string name)
        {
            Component label = root.GetComponentsInChildren<Component>(true)
                .Single(component =>
                    component.gameObject.name == name &&
                    component.GetType().FullName ==
                        "TMPro.TextMeshProUGUI");
            Assert.That(label, Is.Not.Null);
            return label;
        }

        private static string GetText(Component label)
        {
            return (string)label.GetType().GetProperty("text")
                .GetValue(label);
        }

        private static Image FindImage(GameObject root, string name)
        {
            Image image = root.GetComponentsInChildren<Image>(true)
                .Single(component => component.gameObject.name == name);
            Assert.That(image, Is.Not.Null);
            return image;
        }

        private static OpponentWantedPosterView[] GetPosterSlots(
            SerializedObject serialized)
        {
            SerializedProperty property =
                serialized.FindProperty("posterSlots");
            OpponentWantedPosterView[] slots =
                new OpponentWantedPosterView[property.arraySize];
            for (int index = 0; index < property.arraySize; index++)
            {
                slots[index] = property.GetArrayElementAtIndex(index)
                    .objectReferenceValue as OpponentWantedPosterView;
            }

            return slots;
        }
    }
}
