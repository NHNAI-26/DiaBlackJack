using System;
using System.Reflection;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CodexAssetTests
    {
        private const string CardCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
        private const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";
        private const string OverlayPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/CodexOverlay.prefab";
        private const string BookPrefabPath =
            "Assets/03. Prefabs/Props/CodexBook.prefab";

        [Test]
        public void DX02_U01_ContentCatalogCoversEveryEnemyAndDemon()
        {
            CardContentCatalogSO cardCatalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(
                    CardCatalogPath);
            EnemyContentCatalogSO enemyCatalog = LoadEnemyCatalog();

            Assert.That(cardCatalog, Is.Not.Null);
            Assert.That(enemyCatalog, Is.Not.Null);
            enemyCatalog.ValidateOrThrow();
            Assert.That(enemyCatalog.EnemyCount, Is.EqualTo(6));
            Assert.That(cardCatalog.DemonCardCount, Is.EqualTo(12));
            Assert.That(
                cardCatalog.BuildDemonLoreCatalog().Count,
                Is.EqualTo(12));

            EnemyCombatProfileCatalog authoredProfiles =
                enemyCatalog.BuildRuntimeCatalog();
            DiaBlackJack.StageProgression.GoldRewardCatalog authoredGold =
                enemyCatalog.BuildGoldRewardCatalog();
            foreach (EnemyCombatProfile expected in
                EnemyCombatProfileCatalog.Default.Profiles)
            {
                EnemyCombatProfile actual = authoredProfiles.GetByKey(
                    expected.Key);
                Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
                Assert.That(actual.Grade, Is.EqualTo(expected.Grade));
                Assert.That(actual.MaximumSoul, Is.EqualTo(expected.MaximumSoul));
                Assert.That(
                    actual.BehaviorPolicyKey,
                    Is.EqualTo(expected.BehaviorPolicyKey));
                Assert.That(
                    actual.DeckDefinitionKeys,
                    Is.EqualTo(expected.DeckDefinitionKeys));
                Assert.That(actual.Summary, Is.EqualTo(expected.Summary));
                Assert.That(
                    actual.PlayerInformationMode,
                    Is.EqualTo(expected.PlayerInformationMode));
                Assert.That(
                    actual.ChangeCostMode,
                    Is.EqualTo(expected.ChangeCostMode));
                Assert.That(
                    actual.DemonContractDefinitionKeys,
                    Is.EqualTo(expected.DemonContractDefinitionKeys));
                Assert.That(
                    actual.DemonContractCandidateCount,
                    Is.EqualTo(expected.DemonContractCandidateCount));
                Assert.That(
                    actual.InjectsPoisonIntoPlayerDeckEachRound,
                    Is.EqualTo(
                        expected.InjectsPoisonIntoPlayerDeckEachRound));
                Assert.That(
                    actual.FixedDemonContractPhases.Count,
                    Is.EqualTo(expected.FixedDemonContractPhases.Count));
                for (int index = 0;
                    index < actual.FixedDemonContractPhases.Count;
                    index++)
                {
                    FixedDemonContractPhaseDefinition actualPhase =
                        actual.FixedDemonContractPhases[index];
                    FixedDemonContractPhaseDefinition expectedPhase =
                        expected.FixedDemonContractPhases[index];
                    Assert.That(
                        actualPhase.ActivationSoulThreshold,
                        Is.EqualTo(expectedPhase.ActivationSoulThreshold));
                    Assert.That(
                        actualPhase.ActiveDefinitionKey,
                        Is.EqualTo(expectedPhase.ActiveDefinitionKey));
                    Assert.That(
                        actualPhase.DiscardedDefinitionKey,
                        Is.EqualTo(expectedPhase.DiscardedDefinitionKey));
                }

                Assert.That(
                    authoredGold.GetAmount(expected.Key),
                    Is.EqualTo(
                        DiaBlackJack.StageProgression.GoldRewardCatalog
                            .CreatePrototype()
                            .GetAmount(expected.Key)));
            }
        }

        [Test]
        public void DX02_U02_OverlayPrefabHasTabsCloseAndScrollableDeck()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<CodexOverlayView>(), Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<Button>(true).Length,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(
                prefab.GetComponentInChildren<ScrollRect>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<CodexCardThumbnailView>(true)
                    .Length,
                Is.EqualTo(2));
            Assert.That(
                prefab.GetComponentInChildren<EventSystem>(true),
                Is.Null);
        }

        [Test]
        public void DX02_U03_TableBookPrefabIsClickable()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(BookPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<CodexClickable>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Not.Null);
        }

        [Test]
        public void DX00_U01_ContentCatalogRejectsNullEnemy()
        {
            AssertInvalidEnemyCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "enemies");
                entries.GetArrayElementAtIndex(0).objectReferenceValue = null;
            });
        }

        [Test]
        public void DX00_U02_ContentCatalogRejectsDuplicateEnemyKey()
        {
            AssertInvalidEnemyCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "enemies");
                entries.GetArrayElementAtIndex(1).objectReferenceValue =
                    entries.GetArrayElementAtIndex(0).objectReferenceValue;
            });
        }

        [Test]
        public void DX00_U03_ContentCatalogRejectsEmptyLore()
        {
            CardContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadCardCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            DemonCardDefinitionSO demon = null;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                SerializedProperty entries = serialized.FindProperty(
                    "demonCards");
                demon = UnityEngine.Object.Instantiate(
                    entries.GetArrayElementAtIndex(0).objectReferenceValue as
                        DemonCardDefinitionSO);
                demon.hideFlags = HideFlags.HideAndDontSave;
                SerializedObject demonData = new SerializedObject(demon);
                demonData.FindProperty("codexLoreDescription").stringValue =
                    " ";
                demonData.ApplyModifiedPropertiesWithoutUndo();
                entries.GetArrayElementAtIndex(0).objectReferenceValue = demon;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<InvalidOperationException>(() =>
                    catalog.BuildDemonLoreCatalog());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(demon);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DX00_U04_ContentCatalogRejectsMissingPortrait()
        {
            EnemyContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadEnemyCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            EnemyCombatProfileDefinitionSO enemy = null;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                SerializedProperty entries = serialized.FindProperty(
                    "enemies");
                enemy = UnityEngine.Object.Instantiate(
                    entries.GetArrayElementAtIndex(0).objectReferenceValue as
                        EnemyCombatProfileDefinitionSO);
                enemy.hideFlags = HideFlags.HideAndDontSave;
                SerializedObject enemyData = new SerializedObject(enemy);
                enemyData.FindProperty("portrait").objectReferenceValue = null;
                enemyData.ApplyModifiedPropertiesWithoutUndo();
                entries.GetArrayElementAtIndex(0).objectReferenceValue = enemy;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(
                    () => catalog.ValidateOrThrow(),
                    Throws.Exception);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DX03_U01_ControllerOpensClosesAndCleansState()
        {
            GameObject overlayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(overlayPrefab);
            InvokeLifecycle(
                overlay.GetComponent<CodexOverlayView>(),
                "Awake");
            GameObject controllerObject = new GameObject("CodexControllerTest");
            controllerObject.SetActive(false);
            GameObject book = new GameObject("CodexBookTest");
            CodexController controller =
                controllerObject.AddComponent<CodexController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("view").objectReferenceValue =
                overlay.GetComponent<CodexOverlayView>();
            serialized.FindProperty("cardContentCatalog")
                .objectReferenceValue = LoadCardCatalog();
            serialized.FindProperty("enemyContentCatalog")
                .objectReferenceValue = LoadEnemyCatalog();
            serialized.FindProperty("tableBookRoot").objectReferenceValue =
                book;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            controllerObject.SetActive(true);
            InvokeLifecycle(controller, "Awake");
            InvokeLifecycle(controller, "OnEnable");

            Assert.That(controller.Open(), Is.True);
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(
                overlay.GetComponent<Canvas>().enabled,
                Is.True);
            Assert.That(book.activeSelf, Is.False);

            controller.Close();
            Assert.That(book.activeSelf, Is.True);
            Assert.That(controller.Open(), Is.True);

            controller.SetAvailable(false);
            Assert.That(controller.IsOpen, Is.False);
            Assert.That(controller.IsAvailable, Is.False);
            Assert.That(book.activeSelf, Is.False);

            UnityEngine.Object.DestroyImmediate(controllerObject);
            UnityEngine.Object.DestroyImmediate(overlay);
            UnityEngine.Object.DestroyImmediate(book);
        }

        [Test]
        public void DX03_U02_TransientCloseConsumesCodexBeforePause()
        {
            GameObject overlayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(overlayPrefab);
            InvokeLifecycle(
                overlay.GetComponent<CodexOverlayView>(),
                "Awake");
            GameObject controllerObject = new GameObject(
                "CodexControllerTest");
            controllerObject.SetActive(false);
            GameObject managerObject = new GameObject("GameManagerTest");
            managerObject.SetActive(false);
            GameObject book = new GameObject("CodexBookTest");
            CodexController controller =
                controllerObject.AddComponent<CodexController>();
            GameManager manager = managerObject.AddComponent<GameManager>();
            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("view").objectReferenceValue =
                overlay.GetComponent<CodexOverlayView>();
            controllerData.FindProperty("cardContentCatalog")
                .objectReferenceValue = LoadCardCatalog();
            controllerData.FindProperty("enemyContentCatalog")
                .objectReferenceValue = LoadEnemyCatalog();
            controllerData.FindProperty("tableBookRoot")
                .objectReferenceValue = book;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject managerData = new SerializedObject(manager);
            managerData.FindProperty("codex").objectReferenceValue =
                controller;
            managerData.ApplyModifiedPropertiesWithoutUndo();

            controllerObject.SetActive(true);
            InvokeLifecycle(controller, "Awake");
            InvokeLifecycle(controller, "OnEnable");
            Assert.That(controller.Open(), Is.True);
            Assert.That(manager.TryCloseTransientOverlay(), Is.True);
            Assert.That(controller.IsOpen, Is.False);

            UnityEngine.Object.DestroyImmediate(controllerObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
            UnityEngine.Object.DestroyImmediate(overlay);
            UnityEngine.Object.DestroyImmediate(book);
        }

        private static void AssertInvalidEnemyCatalog(
            Action<SerializedObject> mutate)
        {
            EnemyContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadEnemyCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                mutate(serialized);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(
                    () => catalog.ValidateOrThrow(),
                    Throws.Exception);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private static CardContentCatalogSO LoadCardCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(
                CardCatalogPath);
        }

        private static EnemyContentCatalogSO LoadEnemyCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                EnemyCatalogPath);
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}
