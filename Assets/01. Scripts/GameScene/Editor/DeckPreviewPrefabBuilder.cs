#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    /// <summary>Builds the scene-authored deck-inspection prefabs with a fixed pool of 100 card slots.</summary>
    public static class DeckPreviewPrefabBuilder
    {
        public const string OverlayPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/DeckPreviewOverlay.prefab";
        public const string CardSlotPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/DeckPreviewCard.prefab";

        private const string CardHoverOutlineMaterialPath =
            "Assets/05. Arts/Material/Card/UI_DeckCardHoverOutline.mat";
        private const string CombatCardPrefabPath =
            "Assets/03. Prefabs/Card/Card.prefab";
        private const string ConfirmButtonMaterialPath =
            "Assets/05. Arts/Material/Card/UI_Brush_Red_Confirm.mat";

        private const string GameScenePath = "Assets/00. Scenes/GameScene.unity";
        private const int CardSlotCount = 100;

        [MenuItem("Tools/DiaBlackJack/Rebuild Deck Preview Prefabs")]
        public static void Build()
        {
            EnsureFolder("Assets/03. Prefabs/UI/GameScene");

            GameObject cardSlotRoot = CreateCardSlot();
            PrefabUtility.SaveAsPrefabAsset(cardSlotRoot, CardSlotPrefabPath);
            Object.DestroyImmediate(cardSlotRoot);

            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardSlotPrefabPath);
            GameObject overlayRoot = CreateOverlay(slotPrefab);
            PrefabUtility.SaveAsPrefabAsset(overlayRoot, OverlayPrefabPath);
            Object.DestroyImmediate(overlayRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/DiaBlackJack/Install Deck Preview In GameScene")]
        public static void InstallInGameScene()
        {
            GameObject overlayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            if (overlayPrefab == null)
            {
                Build();
                overlayPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            }

            if (overlayPrefab == null)
            {
                throw new MissingReferenceException(
                    $"Deck preview prefab was not created at {OverlayPrefabPath}.");
            }

            Scene targetScene = SceneManager.GetSceneByPath(GameScenePath);
            bool wasLoaded = targetScene.IsValid() && targetScene.isLoaded;
            if (!wasLoaded)
            {
                targetScene = EditorSceneManager.OpenScene(
                    GameScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                GameManager manager = FindInScene<GameManager>(targetScene);
                if (manager == null)
                {
                    throw new MissingReferenceException(
                        $"GameManager was not found in {GameScenePath}.");
                }

                DeckPreviewView preview = FindOverlayInScene(targetScene);
                if (preview == null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                        overlayPrefab,
                        targetScene);
                    instance.name = "UIDeckPreview";
                    preview = instance.GetComponent<DeckPreviewView>();
                }

                bool sceneChanged = false;
                if (preview.gameObject.activeSelf)
                {
                    preview.gameObject.SetActive(false);
                    sceneChanged = true;
                }

                EnsureEventSystem(targetScene, ref sceneChanged);
                SerializedObject managerData = new SerializedObject(manager);
                SerializedProperty previewProperty =
                    managerData.FindProperty("deckPreview");
                if (previewProperty.objectReferenceValue != preview)
                {
                    previewProperty.objectReferenceValue = preview;
                    managerData.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(manager);
                    sceneChanged = true;
                }

                DeckPreviewView[] legacyViews =
                    manager.GetComponents<DeckPreviewView>();
                for (int i = 0; i < legacyViews.Length; i++)
                {
                    if (legacyViews[i] != preview)
                    {
                        Object.DestroyImmediate(legacyViews[i], true);
                        sceneChanged = true;
                    }
                }

                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(targetScene);
                    EditorSceneManager.SaveScene(targetScene);
                }
            }
            finally
            {
                if (!wasLoaded && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        private static GameObject CreateOverlay(GameObject slotPrefab)
        {
            GameObject root = CreateUiObject("DeckPreviewOverlay", null);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            GraphicRaycaster raycaster = root.AddComponent<GraphicRaycaster>();
            DeckPreviewView view = root.AddComponent<DeckPreviewView>();

            GameObject background = CreateUiObject("Background", root.transform);
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.72f);
            Button backgroundButton = background.AddComponent<Button>();

            GameObject panel = CreateUiObject("Panel", root.transform);
            Stretch(
                panel.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.06f),
                new Vector2(0.94f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.075f, 0.075f, 0.09f, 0.98f);

            TMP_Text title = CreateText("Title", panel.transform, 38, TextAlignmentOptions.Left);
            Stretch(
                title.rectTransform,
                new Vector2(0.04f, 0.88f),
                new Vector2(0.72f, 0.98f),
                Vector2.zero,
                Vector2.zero);
            title.fontStyle = FontStyles.Bold;

            GameObject closeRoot = CreateUiObject("CloseButton", panel.transform);
            Stretch(
                closeRoot.GetComponent<RectTransform>(),
                new Vector2(0.83f, 0.89f),
                new Vector2(0.96f, 0.97f),
                Vector2.zero,
                Vector2.zero);
            Image closeImage = closeRoot.AddComponent<Image>();
            closeImage.color = new Color(0.38f, 0.09f, 0.09f, 1f);
            Button closeButton = closeRoot.AddComponent<Button>();
            TMP_Text closeLabel = CreateText("Label", closeRoot.transform, 26, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            closeLabel.text = "닫기";

            GameObject scrollRoot = CreateUiObject("CardScroll", panel.transform);
            Stretch(
                scrollRoot.GetComponent<RectTransform>(),
                new Vector2(0.04f, 0.14f),
                new Vector2(0.96f, 0.86f),
                Vector2.zero,
                Vector2.zero);
            ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 45f;

            GameObject viewport = CreateUiObject("Viewport", scrollRoot.transform);
            Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUiObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(22, 22, 22, 22);
            grid.cellSize = new Vector2(220f, 350f);
            grid.spacing = new Vector2(20f, 20f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;

            var slots = new List<DeckPreviewCardView>(CardSlotCount);
            for (int i = 0; i < CardSlotCount; i++)
            {
                GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, content.transform);
                slot.name = $"CardSlot_{i + 1:00}";
                slots.Add(slot.GetComponent<DeckPreviewCardView>());
            }

            GameObject selectionFooter = CreateUiObject(
                "SelectionFooter",
                panel.transform);
            Stretch(
                selectionFooter.GetComponent<RectTransform>(),
                new Vector2(0.42f, 0.035f),
                new Vector2(0.58f, 0.115f),
                Vector2.zero,
                Vector2.zero);
            Image confirmImage = selectionFooter.AddComponent<Image>();
            confirmImage.color = new Color(0.38f, 0.09f, 0.09f, 1f);
            confirmImage.material = AssetDatabase.LoadAssetAtPath<Material>(
                ConfirmButtonMaterialPath);
            Button confirmButton = selectionFooter.AddComponent<Button>();
            confirmButton.targetGraphic = confirmImage;
            ColorBlock confirmColors = confirmButton.colors;
            confirmColors.normalColor = Color.white;
            confirmColors.highlightedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            confirmColors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            confirmColors.selectedColor = Color.white;
            confirmColors.disabledColor = Color.white;
            confirmButton.colors = confirmColors;
            CanvasGroup confirmGroup = selectionFooter.AddComponent<CanvasGroup>();
            confirmGroup.alpha = 0.5f;
            TMP_Text confirmLabel = CreateText(
                "Label",
                selectionFooter.transform,
                27,
                TextAlignmentOptions.Center);
            Stretch(
                confirmLabel.rectTransform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            confirmLabel.text = "확인";
            selectionFooter.SetActive(false);

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("previewCanvas").objectReferenceValue = canvas;
            serializedView.FindProperty("previewRaycaster").objectReferenceValue = raycaster;
            serializedView.FindProperty("backgroundCloseButton").objectReferenceValue = backgroundButton;
            serializedView.FindProperty("closeButton").objectReferenceValue = closeButton;
            serializedView.FindProperty("selectionFooter").objectReferenceValue = selectionFooter;
            serializedView.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            serializedView.FindProperty("confirmButtonGroup").objectReferenceValue = confirmGroup;
            serializedView.FindProperty("cardScrollRect").objectReferenceValue = scrollRect;
            serializedView.FindProperty("titleText").objectReferenceValue = title;
            SerializedProperty slotsProperty = serializedView.FindProperty("cardSlots");
            slotsProperty.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject CreateCardSlot()
        {
            GameObject root = CreateUiObject("DeckPreviewCard", null);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 350f);
            Image frame = root.AddComponent<Image>();
            frame.color = new Color(0.18f, 0.18f, 0.2f, 1f);
            DeckPreviewCardView view = root.AddComponent<DeckPreviewCardView>();

            GameObject faceRoot = CreateUiObject("Face", root.transform);
            RectTransform faceRect = faceRoot.GetComponent<RectTransform>();
            Stretch(
                faceRect,
                Vector2.zero,
                Vector2.one,
                new Vector2(8f, 40f),
                new Vector2(-8f, -8f));
            Image face = faceRoot.AddComponent<Image>();
            face.preserveAspect = true;
            face.raycastTarget = true;

            TMP_Text fallback = CreateText("Fallback", root.transform, 28, TextAlignmentOptions.Center);
            Stretch(
                fallback.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 32f),
                Vector2.zero);
            fallback.textWrappingMode = TextWrappingModes.Normal;

            TMP_Text count = CreateText(
                "Count",
                root.transform,
                26,
                TextAlignmentOptions.Center);
            Stretch(
                count.rectTransform,
                Vector2.zero,
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 32f));
            count.fontStyle = FontStyles.Bold;
            count.text = "x1";

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("faceImage").objectReferenceValue = face;
            serializedView.FindProperty("hoverOutlineMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(CardHoverOutlineMaterialPath);
            serializedView.FindProperty("fallbackText").objectReferenceValue = fallback;
            serializedView.FindProperty("countText").objectReferenceValue = count;
            CopyCombatHoverFeel(serializedView);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void CopyCombatHoverFeel(SerializedObject target)
        {
            GameObject combatCardPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CombatCardPrefabPath);
            CardView combatCard = combatCardPrefab == null
                ? null
                : combatCardPrefab.GetComponent<CardView>();
            if (combatCard == null)
            {
                throw new MissingReferenceException(
                    $"Combat card prefab was not found at {CombatCardPrefabPath}.");
            }

            SerializedObject source = new SerializedObject(combatCard);
            target.FindProperty("hoverScale").floatValue =
                source.FindProperty("hoverScale").floatValue;
            target.FindProperty("hoverScaleDuration").floatValue =
                source.FindProperty("hoverScaleDuration").floatValue;
            target.FindProperty("hoverScaleCurve").animationCurveValue =
                source.FindProperty("hoverScaleCurve").animationCurveValue;
            target.FindProperty("hoverSfxId").stringValue =
                source.FindProperty("hoverSfxId").stringValue;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject root = CreateUiObject(name, parent);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void CreateBorder(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject border = CreateUiObject(name, parent);
            Stretch(
                border.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax,
                offsetMin,
                offsetMax);
            Image image = border.AddComponent<Image>();
            image.color = new Color(1f, 0.84f, 0.2f, 1f);
            image.raycastTarget = false;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static DeckPreviewView FindOverlayInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            DeckPreviewView fallback = null;
            for (int i = 0; i < roots.Length; i++)
            {
                DeckPreviewView preview =
                    roots[i].GetComponent<DeckPreviewView>();
                if (preview == null)
                {
                    continue;
                }

                if (string.Equals(
                        roots[i].name,
                        "UIDeckPreview",
                        System.StringComparison.Ordinal))
                {
                    return preview;
                }

                if (fallback == null &&
                    roots[i].name.StartsWith(
                        "DeckPreviewOverlay",
                        System.StringComparison.Ordinal))
                {
                    fallback = preview;
                }
            }

            return fallback;
        }

        private static void EnsureEventSystem(
            Scene scene,
            ref bool sceneChanged)
        {
            EventSystem eventSystem = FindInScene<EventSystem>(scene);
            if (eventSystem == null)
            {
                GameObject eventSystemRoot = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystemRoot, scene);
                eventSystem =
                    eventSystemRoot.GetComponent<EventSystem>();
                eventSystemRoot.GetComponent<InputSystemUIInputModule>()
                    .AssignDefaultActions();
                sceneChanged = true;
                return;
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                InputSystemUIInputModule inputModule =
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
                sceneChanged = true;
            }
        }
    }
}
#endif
