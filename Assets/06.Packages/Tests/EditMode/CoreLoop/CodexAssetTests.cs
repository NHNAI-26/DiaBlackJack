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
        private const string CodexCatalogPath =
            "Assets/02. ScriptableObjects/Codex/CodexContentCatalog.asset";
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
            CodexContentCatalogSO codexCatalog =
                AssetDatabase.LoadAssetAtPath<CodexContentCatalogSO>(
                    CodexCatalogPath);

            Assert.That(cardCatalog, Is.Not.Null);
            Assert.That(codexCatalog, Is.Not.Null);
            codexCatalog.ValidateOrThrow(cardCatalog.BuildRuntimeCatalog());
            Assert.That(codexCatalog.EnemyPortraitCount, Is.EqualTo(6));
            Assert.That(codexCatalog.DemonLoreCount, Is.EqualTo(12));
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
        public void DX00_U01_ContentCatalogRejectsMissingEnemyKey()
        {
            AssertInvalidCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "enemyPortraits");
                entries.DeleteArrayElementAtIndex(entries.arraySize - 1);
            });
        }

        [Test]
        public void DX00_U02_ContentCatalogRejectsDuplicateDemonKey()
        {
            AssertInvalidCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "demonLore");
                string duplicateKey = entries.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("definitionKey").stringValue;
                entries.GetArrayElementAtIndex(1)
                    .FindPropertyRelative("definitionKey").stringValue =
                    duplicateKey;
            });
        }

        [Test]
        public void DX00_U03_ContentCatalogRejectsEmptyLore()
        {
            AssertInvalidCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "demonLore");
                entries.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("loreDescription").stringValue =
                    " ";
            });
        }

        [Test]
        public void DX00_U04_ContentCatalogRejectsMissingPortrait()
        {
            AssertInvalidCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "enemyPortraits");
                entries.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("portrait").objectReferenceValue =
                    null;
            });
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
            serialized.FindProperty("codexContentCatalog")
                .objectReferenceValue = LoadCodexCatalog();
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
            controllerData.FindProperty("codexContentCatalog")
                .objectReferenceValue = LoadCodexCatalog();
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

        private static void AssertInvalidCatalog(
            Action<SerializedObject> mutate)
        {
            CodexContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadCodexCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                mutate(serialized);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() =>
                    catalog.ValidateOrThrow(
                        LoadCardCatalog().BuildRuntimeCatalog()));
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

        private static CodexContentCatalogSO LoadCodexCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<CodexContentCatalogSO>(
                CodexCatalogPath);
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
