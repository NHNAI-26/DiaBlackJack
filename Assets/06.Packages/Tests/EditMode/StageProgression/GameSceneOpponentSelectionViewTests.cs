using System;
using System.Linq;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using DiaBlackJack.GameScene.Editor;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
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
