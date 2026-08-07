using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("CP01")]
    public sealed class CombatPromptTests
    {
        private const string CatalogPath =
            "Assets/02. ScriptableObjects/UI/CombatPromptCatalog.asset";
        private const string PromptPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/CombatPrompt.prefab";
        private const string HudPrefabPath =
            "Assets/03. Prefabs/UI/HUD.prefab";
        private const string RevolverPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/RevolverNumberSelector.prefab";
        private const string CoreLoopScenePath =
            "Assets/00. Scenes/CoreLoopTest.unity";

        [Test]
        public void CP01_U01_PromptIdsAreStableAndContainTwentyFourEntries()
        {
            CombatPromptId[] ids = Enum.GetValues(typeof(CombatPromptId))
                .Cast<CombatPromptId>()
                .Where(id => id != CombatPromptId.None)
                .ToArray();

            Assert.That(ids, Has.Length.EqualTo(24));
            Assert.That(ids.Select(id => (int)id),
                Is.EqualTo(Enumerable.Range(1, 24)));

            var stateIds = new HashSet<CombatPromptId>
            {
                CombatPromptId.ChangeCard,
                CombatPromptIdMap.ForManualCard(CardEffectKind.AutoPistol),
                CombatPromptIdMap.ForManualCard(CardEffectKind.CrystalOrb),
                CombatPromptIdMap.ForManualCard(CardEffectKind.ThreatHammer)
            };
            foreach (AutomaticCardChoiceKind choiceKind in
                Enum.GetValues(typeof(AutomaticCardChoiceKind)))
            {
                stateIds.Add(CombatPromptIdMap.ForAutomaticCard(choiceKind));
            }

            foreach (DemonContractInteractionKind interactionKind in
                Enum.GetValues(typeof(DemonContractInteractionKind)))
            {
                stateIds.Add(CombatPromptIdMap.ForDemonContract(interactionKind));
            }

            Assert.That(stateIds, Is.EquivalentTo(ids));
        }

        [Test]
        public void CP01_U02_CatalogCoversEveryIdAndResolvesDynamicTokens()
        {
            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.GetValidationErrors(), Is.Empty);
            Assert.That(
                catalog.Entries.Select(entry => entry.Id).Distinct().Count(),
                Is.EqualTo(24));

            var automatic = new CombatPromptRequest(
                CombatPromptId.AutomaticPoisonDecision,
                sourceDisplayName: "독극물");
            Assert.That(catalog.TryResolve(automatic, out string automaticText),
                Is.True);
            Assert.That(automaticText, Does.Contain("독극물"));

            var counted = new CombatPromptRequest(
                CombatPromptId.DemonSatanDeclareFirstNumber,
                currentCount: 1,
                requiredCount: 2);
            Assert.That(catalog.TryResolve(counted, out string countedText), Is.True);
            Assert.That(countedText, Does.Contain("1/2"));

            var contextual = new CombatPromptRequest(
                CombatPromptId.DemonBelphegorTopCard,
                contextText: "미리 본 카드: 7");
            Assert.That(catalog.TryResolve(contextual, out string contextText), Is.True);
            Assert.That(contextText, Does.Contain("미리 본 카드: 7"));
        }

        [Test]
        public void CP01_U03_CatalogValidationFindsDuplicateEmptyMissingAndUnknownToken()
        {
            CombatPromptCatalogSO catalog =
                ScriptableObject.CreateInstance<CombatPromptCatalogSO>();
            try
            {
                catalog.ReplaceEntriesForEditor(new[]
                {
                    new CombatPromptCatalogSO.Entry(
                        CombatPromptId.ChangeCard,
                        string.Empty),
                    new CombatPromptCatalogSO.Entry(
                        CombatPromptId.ChangeCard,
                        "{mystery}")
                });

                IReadOnlyList<string> errors = catalog.GetValidationErrors();
                Assert.That(errors.Any(error => error.Contains("Duplicate")), Is.True);
                Assert.That(errors.Any(error => error.Contains("empty")), Is.True);
                Assert.That(errors.Any(error => error.Contains("Unknown token")), Is.True);
                Assert.That(errors.Any(error => error.Contains("Missing")), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CP01_U04_NormalChangeAndLockedHudExposeExpectedPromptState()
        {
            CoreLoopBattle battle = CreateStartedBattle();
            CoreLoopViewModel normal = CoreLoopPresenter.Create(battle);
            Assert.That(normal.SelectionPrompt, Is.Null);

            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            CoreLoopViewModel changing = CoreLoopPresenter.Create(battle);
            Assert.That(
                changing.SelectionPrompt?.Id,
                Is.EqualTo(CombatPromptId.ChangeCard));

            GameSceneCombatHudViewModel unlocked =
                GameSceneCombatHudPresenter.Create(
                    changing,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: false);
            GameSceneCombatHudViewModel locked =
                GameSceneCombatHudPresenter.Create(
                    changing,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: true);
            GameSceneCombatHudViewModel shop =
                GameSceneCombatHudPresenter.Create(
                    changing,
                    isStageBattle: false,
                    isShopOpen: true,
                    inputLocked: false);

            Assert.That(unlocked.SelectionPrompt?.Id,
                Is.EqualTo(CombatPromptId.ChangeCard));
            Assert.That(locked.SelectionPrompt, Is.Null);
            Assert.That(shop.SelectionPrompt, Is.Null);
        }

        [Test]
        public void CP01_U05_PromptPrefabAndHudHaveSingleRequiredPromptView()
        {
            GameObject promptPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath);
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(promptPrefab, Is.Not.Null);
            Assert.That(hudPrefab, Is.Not.Null);

            CombatPromptView prefabView = promptPrefab.GetComponent<CombatPromptView>();
            CanvasGroup group = promptPrefab.GetComponent<CanvasGroup>();
            Graphic text = promptPrefab.transform.Find("Text").GetComponent<Graphic>();
            Image background = promptPrefab.GetComponent<Image>();
            RectTransform rect = promptPrefab.GetComponent<RectTransform>();
            Assert.That(prefabView.HasRequiredReferences, Is.True);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
            Assert.That(text.raycastTarget, Is.False);
            Assert.That(background.raycastTarget, Is.False);

            GameHudView hud = hudPrefab.GetComponent<GameHudView>();
            SerializedObject serialized = new SerializedObject(hud);
            CombatPromptView nested = serialized.FindProperty("combatPromptView")
                .objectReferenceValue as CombatPromptView;
            Component header = serialized.FindProperty("combatHeaderText")
                .objectReferenceValue as Component;
            Assert.That(nested, Is.Not.Null);
            Assert.That(nested.transform.IsChildOf(hudPrefab.transform), Is.True);
            Assert.That(header, Is.Not.Null);
            Assert.That(header.name, Is.EqualTo("HeaderText"));
            Assert.That(
                hudPrefab.GetComponentsInChildren<CombatPromptView>(true),
                Has.Length.EqualTo(1));

            RectTransform nestedRect = nested.GetComponent<RectTransform>();
            Assert.That(nestedRect.anchorMin, Is.EqualTo(rect.anchorMin));
            Assert.That(nestedRect.anchorMax, Is.EqualTo(rect.anchorMax));
            Assert.That(nestedRect.pivot, Is.EqualTo(rect.pivot));
            Assert.That(
                nestedRect.anchoredPosition,
                Is.EqualTo(rect.anchoredPosition));
            Assert.That(nestedRect.sizeDelta, Is.EqualTo(rect.sizeDelta));
        }

        [Test]
        public void CP01_U06_RevolverHasNoPromptAndCoreLoopSceneUsesCatalog()
        {
            GameObject revolver =
                AssetDatabase.LoadAssetAtPath<GameObject>(RevolverPrefabPath);
            Assert.That(revolver, Is.Not.Null);
            Assert.That(revolver.transform.Find("Prompt"), Is.Null);
            Assert.That(
                revolver.GetComponent<RevolverNumberSelectorView>()
                    .HasRequiredReferences,
                Is.True);

            Scene scene = SceneManager.GetSceneByPath(CoreLoopScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(
                    CoreLoopScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                CoreLoopView view = scene.GetRootGameObjects()
                    .Select(root => root.GetComponentInChildren<CoreLoopView>(true))
                    .First(candidate => candidate != null);
                SerializedObject serialized = new SerializedObject(view);
                Object catalog = serialized.FindProperty("combatPromptCatalog")
                    .objectReferenceValue;
                Assert.That(catalog, Is.EqualTo(
                    AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath)));
            }
            finally
            {
                if (opened && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
        }

        [Test]
        public void CP01_U07_PromptLayoutComesFromPrefabWithoutHudOverride()
        {
            GameObject promptPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath);
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            CombatPromptView nested = hudPrefab
                .GetComponent<GameHudView>()
                .GetComponentInChildren<CombatPromptView>(true);
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(
                nested.gameObject);

            Assert.That(source, Is.EqualTo(promptPrefab));
        }

        [Test]
        public void CP01_U08_PromptShowsAndHidesImmediately()
        {
            GameObject promptPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath);
            GameObject instance = Object.Instantiate(promptPrefab);
            try
            {
                CombatPromptView view = instance.GetComponent<CombatPromptView>();
                CanvasGroup group = instance.GetComponent<CanvasGroup>();

                view.Render(new CombatPromptRequest(CombatPromptId.ChangeCard));

                Assert.That(instance.activeSelf, Is.True);
                Assert.That(group.alpha, Is.EqualTo(1f));

                view.Hide();

                Assert.That(instance.activeSelf, Is.False);
                Assert.That(group.alpha, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static CoreLoopBattle CreateStartedBattle()
        {
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateStandard(seed: 7001),
                BlackjackDeck.CreateStandard(seed: 7002));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }
    }
}
