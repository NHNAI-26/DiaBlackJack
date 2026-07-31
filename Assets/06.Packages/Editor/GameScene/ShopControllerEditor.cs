using System;
using System.Collections.Generic;
using DiaBlackJack.GameScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(ShopController))]
    public sealed class ShopControllerEditor : UnityEditor.Editor
    {
        internal const string PreviewObjectPrefix = "__TableLayoutPreview_";

        private SerializedProperty _itemsRoot;
        private SerializedProperty _demonCardHolder;
        private SerializedProperty _demonCardPrefab;
        private SerializedProperty _normalCardHolder;
        private SerializedProperty _normalCardPrefab;
        private SerializedProperty _combatTableObjects;
        private SerializedProperty _demonCardOfferCount;
        private SerializedProperty _demonCardSpacing;
        private SerializedProperty _normalCardOfferCount;
        private SerializedProperty _normalCardSpacing;
        private int _playerPreviewCardCount = 4;
        private int _enemyPreviewCardCount = 4;

        private void OnEnable()
        {
            if (target == null)
            {
                return;
            }

            _itemsRoot = serializedObject.FindProperty("itemsRoot");
            _demonCardHolder = serializedObject.FindProperty("demonCardHolder");
            _demonCardPrefab = serializedObject.FindProperty("demonCardPrefab");
            _normalCardHolder = serializedObject.FindProperty("normalCardHolder");
            _normalCardPrefab = serializedObject.FindProperty("normalCardPrefab");
            _combatTableObjects = serializedObject.FindProperty("combatTableObjects");
            _demonCardOfferCount = serializedObject.FindProperty("demonCardOfferCount");
            _demonCardSpacing = serializedObject.FindProperty("demonCardSpacing");
            _normalCardOfferCount = serializedObject.FindProperty("normalCardOfferCount");
            _normalCardSpacing = serializedObject.FindProperty("normalCardSpacing");
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                return;
            }

            DrawDefaultInspector();
            serializedObject.Update();
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "TABLE LAYOUT PREVIEW",
                EditorStyles.boldLabel);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "Layout preview is available only in Edit Mode.",
                    MessageType.Info);
                return;
            }

            DrawReferenceWarnings();
            EditorGUILayout.LabelField("Current", GetCurrentPreviewLabel());
            _playerPreviewCardCount = EditorGUILayout.IntSlider(
                "Player Preview Cards",
                _playerPreviewCardCount,
                1,
                10);
            _enemyPreviewCardCount = EditorGUILayout.IntSlider(
                "Enemy Preview Cards",
                _enemyPreviewCardCount,
                1,
                10);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Combat"))
                {
                    ApplyPreview(
                        shopActive: false,
                        combatActive: true,
                        createOfferPreviews: false,
                        createCombatCardPreviews: true,
                        undoName: "Preview Combat Table Layout");
                }

                if (GUILayout.Button("Shop"))
                {
                    ApplyPreview(
                        shopActive: true,
                        combatActive: false,
                        createOfferPreviews: true,
                        createCombatCardPreviews: false,
                        undoName: "Preview Shop Table Layout");
                }

                if (GUILayout.Button("Show All"))
                {
                    ApplyPreview(
                        shopActive: true,
                        combatActive: true,
                        createOfferPreviews: true,
                        createCombatCardPreviews: true,
                        undoName: "Preview All Table Layouts");
                }
            }

            EditorGUILayout.HelpBox(
                "Preview cards are temporary and are not saved to the scene. " +
                "Press a preview button again after changing counts or spacing.",
                MessageType.None);
        }

        internal static void ClearAllPreviewObjects()
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject gameObject in objects)
            {
                if (gameObject == null ||
                    !gameObject.name.StartsWith(
                        PreviewObjectPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private void DrawReferenceWarnings()
        {
            if (GetItemsRoot() == null)
            {
                EditorGUILayout.HelpBox(
                    "Items Root is missing.",
                    MessageType.Warning);
            }

            if (GetCombatTableObjects().Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Combat Table Objects is empty.",
                    MessageType.Warning);
            }

            if (GetCombatHands().Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No CardHand exists in Combat Table Objects.",
                    MessageType.Warning);
            }
        }

        private string GetCurrentPreviewLabel()
        {
            GameObject itemsRoot = GetItemsRoot();
            List<GameObject> combatObjects = GetCombatTableObjects();
            bool shopActive = itemsRoot != null && itemsRoot.activeSelf;
            bool anyCombatActive = false;
            bool allCombatActive = combatObjects.Count > 0;

            foreach (GameObject combatObject in combatObjects)
            {
                anyCombatActive |= combatObject.activeSelf;
                allCombatActive &= combatObject.activeSelf;
            }

            if (!shopActive && allCombatActive)
            {
                return "Combat";
            }

            if (shopActive && !anyCombatActive)
            {
                return "Shop";
            }

            if (shopActive && allCombatActive)
            {
                return "All";
            }

            return "Custom";
        }

        private void ApplyPreview(
            bool shopActive,
            bool combatActive,
            bool createOfferPreviews,
            bool createCombatCardPreviews,
            string undoName)
        {
            GameObject itemsRoot = GetItemsRoot();
            List<GameObject> combatObjects = GetCombatTableObjects();
            List<GameObject> changedObjects = new List<GameObject>();
            AddUnique(changedObjects, itemsRoot);
            foreach (GameObject combatObject in combatObjects)
            {
                AddUnique(changedObjects, combatObject);
            }

            foreach (GameObject gameObject in changedObjects)
            {
                Undo.RecordObject(gameObject, undoName);
            }

            if (itemsRoot != null)
            {
                itemsRoot.SetActive(shopActive);
            }

            foreach (GameObject combatObject in combatObjects)
            {
                combatObject.SetActive(combatActive);
            }

            ClearAllPreviewObjects();
            if (createOfferPreviews)
            {
                CreateOfferPreviews();
            }

            if (createCombatCardPreviews)
            {
                CreateCombatCardPreviews();
            }

            foreach (GameObject gameObject in changedObjects)
            {
                EditorUtility.SetDirty(gameObject);
            }

            ShopController controller = (ShopController)target;
            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void CreateOfferPreviews()
        {
            CreateOfferPreviews(
                _demonCardHolder.objectReferenceValue as Transform,
                _demonCardPrefab.objectReferenceValue as DemonCardView,
                Mathf.Max(0, _demonCardOfferCount.intValue),
                _demonCardSpacing.floatValue,
                "DemonCard");
            CreateOfferPreviews(
                _normalCardHolder.objectReferenceValue as Transform,
                _normalCardPrefab.objectReferenceValue as CardView,
                Mathf.Max(0, _normalCardOfferCount.intValue),
                _normalCardSpacing.floatValue,
                "NormalCard");
        }

        private static void CreateOfferPreviews<T>(
            Transform holder,
            T prefab,
            int count,
            float spacing,
            string label)
            where T : Component
        {
            if (holder == null || prefab == null || count == 0)
            {
                return;
            }

            float offset = -(count - 1) * 0.5f * spacing;
            for (int i = 0; i < count; i++)
            {
                GameObject preview = InstantiatePreview(
                    prefab.gameObject,
                    holder,
                    label + "_" + i);
                preview.transform.localPosition = new Vector3(
                    offset + i * spacing,
                    0f,
                    i * 0.01f);
                preview.transform.localRotation = Quaternion.identity;
                preview.SetActive(true);
            }
        }

        private void CreateCombatCardPreviews()
        {
            foreach (CardHand hand in GetCombatHands())
            {
                SerializedObject handObject = new SerializedObject(hand);
                CardView prefab = handObject
                    .FindProperty("cardPrefab")
                    .objectReferenceValue as CardView;
                if (prefab == null)
                {
                    continue;
                }

                bool isEnemy = hand.name.IndexOf(
                    "Enemy",
                    StringComparison.OrdinalIgnoreCase) >= 0;
                int count = isEnemy
                    ? _enemyPreviewCardCount
                    : _playerPreviewCardCount;
                float spacing = handObject.FindProperty("spacing").floatValue;
                float depthStagger = handObject
                    .FindProperty("depthStagger")
                    .floatValue;
                int sortingOrderBase = handObject
                    .FindProperty("sortingOrderBase")
                    .intValue;
                int sortingOrderStep = handObject
                    .FindProperty("sortingOrderStep")
                    .intValue;

                CreateHandCardPreviews(
                    hand.transform,
                    prefab,
                    count,
                    spacing,
                    depthStagger,
                    sortingOrderBase,
                    sortingOrderStep,
                    isEnemy ? "EnemyHandCard" : "PlayerHandCard");
            }
        }

        private static void CreateHandCardPreviews(
            Transform hand,
            CardView prefab,
            int count,
            float spacing,
            float depthStagger,
            int sortingOrderBase,
            int sortingOrderStep,
            string label)
        {
            float offset = -(count - 1) * 0.5f * spacing;
            for (int i = 0; i < count; i++)
            {
                GameObject preview = InstantiatePreview(
                    prefab.gameObject,
                    hand,
                    label + "_" + i);
                preview.transform.localPosition = new Vector3(
                    offset + i * spacing,
                    0f,
                    i * depthStagger);
                preview.transform.localRotation = Quaternion.identity;

                int sortingOrder =
                    sortingOrderBase + i * sortingOrderStep;
                SpriteRenderer[] renderers =
                    preview.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.sortingOrder = sortingOrder;
                }
            }
        }

        private static GameObject InstantiatePreview(
            GameObject prefab,
            Transform parent,
            string label)
        {
            GameObject preview = PrefabUtility.InstantiatePrefab(
                prefab,
                parent) as GameObject;
            if (preview == null)
            {
                preview = UnityEngine.Object.Instantiate(prefab, parent);
            }

            preview.name = PreviewObjectPrefix + label;
            preview.hideFlags =
                HideFlags.DontSaveInEditor |
                HideFlags.DontSaveInBuild;
            preview.SetActive(true);
            return preview;
        }

        private GameObject GetItemsRoot()
        {
            return _itemsRoot.objectReferenceValue as GameObject;
        }

        private List<GameObject> GetCombatTableObjects()
        {
            List<GameObject> objects = new List<GameObject>();
            for (int i = 0; i < _combatTableObjects.arraySize; i++)
            {
                GameObject gameObject = _combatTableObjects
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as GameObject;
                AddUnique(objects, gameObject);
            }

            return objects;
        }

        private List<CardHand> GetCombatHands()
        {
            List<CardHand> hands = new List<CardHand>();
            foreach (GameObject combatObject in GetCombatTableObjects())
            {
                CardHand hand = combatObject.GetComponent<CardHand>();
                if (hand != null && !hands.Contains(hand))
                {
                    hands.Add(hand);
                }
            }

            return hands;
        }

        private static void AddUnique(
            List<GameObject> objects,
            GameObject candidate)
        {
            if (candidate != null && !objects.Contains(candidate))
            {
                objects.Add(candidate);
            }
        }
    }

    [InitializeOnLoad]
    internal static class ShopControllerPreviewLifecycle
    {
        static ShopControllerPreviewLifecycle()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= ClearPreviewObjects;
            AssemblyReloadEvents.beforeAssemblyReload += ClearPreviewObjects;
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                ClearPreviewObjects();
            }
        }

        private static void ClearPreviewObjects()
        {
            ShopControllerEditor.ClearAllPreviewObjects();
        }
    }
}
