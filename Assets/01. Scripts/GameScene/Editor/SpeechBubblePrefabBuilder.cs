#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    public static class SpeechBubblePrefabBuilder
    {
        public const string SpeechBubblePrefabPath =
            "Assets/03. Prefabs/UI/SpeechBubble.prefab";
        public const string EnemyCharacterPrefabPath =
            "Assets/03. Prefabs/Character/EnemyCharacter.prefab";
        public const string CameraPrefabPath =
            "Assets/03. Prefabs/Map/Camera.prefab";
        public const string PcRendererPath =
            "Assets/02. ScriptableObjects/Settings/PC_Renderer.asset";
        public const string PcPipelinePath =
            "Assets/02. ScriptableObjects/Settings/PC_RPAsset.asset";
        public const string TextUiRendererPath =
            "Assets/02. ScriptableObjects/Settings/TextUI_Renderer.asset";

        public const string TextUiLayerName = "TextUI";
        public const int TextUiLayerIndex = 7;
        public const int TextUiRendererIndex = 2;

        private const string BubbleAnchorName = "SpeechBubbleAnchor";
        private const string OverlayCameraName = "TextUIOverlayCamera";
        private const string TextCanvasName = "TextUICanvas";

        private static readonly Vector3 BubbleAnchorLocalPosition =
            new Vector3(-3.28f, 0.69f, 0f);

        [MenuItem("Tools/DiaBlackJack/Install TextUI Speech Bubble")]
        public static void Install()
        {
            EnsureTextUiLayer();
            EnsureTextUiRenderer();
            ConfigureSpeechBubblePrefab();
            ConfigureEnemyCharacterPrefab();
            ConfigureCameraPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureTextUiLayer()
        {
            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                throw new MissingReferenceException(
                    "ProjectSettings/TagManager.asset was not found.");
            }

            SerializedObject tagManager = new SerializedObject(
                tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || layers.arraySize <= TextUiLayerIndex)
            {
                throw new MissingReferenceException(
                    "TagManager does not expose the expected user layers.");
            }

            SerializedProperty targetLayer = layers.GetArrayElementAtIndex(
                TextUiLayerIndex);
            string existingName = targetLayer.stringValue;
            if (!string.IsNullOrEmpty(existingName) &&
                existingName != TextUiLayerName)
            {
                throw new System.InvalidOperationException(
                    $"Layer {TextUiLayerIndex} is already named {existingName}.");
            }

            targetLayer.stringValue = TextUiLayerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTextUiRenderer()
        {
            UniversalRendererData source =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                    PcRendererPath);
            if (source == null)
            {
                throw new MissingReferenceException(
                    $"PC renderer was not found at {PcRendererPath}.");
            }

            UniversalRendererData renderer =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                    TextUiRendererPath);
            if (renderer == null)
            {
                if (!AssetDatabase.CopyAsset(PcRendererPath, TextUiRendererPath))
                {
                    throw new System.InvalidOperationException(
                        "Could not create the TextUI renderer asset.");
                }

                AssetDatabase.ImportAsset(TextUiRendererPath);
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                    TextUiRendererPath);
            }

            renderer.name = "TextUI_Renderer";
            SerializedObject rendererSerialized = new SerializedObject(renderer);
            SerializedProperty features = rendererSerialized.FindProperty(
                "m_RendererFeatures");
            features?.ClearArray();
            SerializedProperty featureMap = rendererSerialized.FindProperty(
                "m_RendererFeatureMap");
            if (featureMap != null && featureMap.isArray)
            {
                featureMap.ClearArray();
            }

            SerializedProperty renderingMode = rendererSerialized.FindProperty(
                "m_RenderingMode");
            if (renderingMode != null)
            {
                renderingMode.intValue = 0;
            }

            rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
            Object[] copiedAssets = AssetDatabase.LoadAllAssetsAtPath(
                TextUiRendererPath);
            for (int i = 0; i < copiedAssets.Length; i++)
            {
                if (copiedAssets[i] != renderer &&
                    copiedAssets[i] is ScriptableRendererFeature)
                {
                    Object.DestroyImmediate(copiedAssets[i], true);
                }
            }

            EditorUtility.SetDirty(renderer);

            UniversalRenderPipelineAsset pipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                    PcPipelinePath);
            if (pipeline == null)
            {
                throw new MissingReferenceException(
                    $"PC pipeline was not found at {PcPipelinePath}.");
            }

            SerializedObject pipelineSerialized = new SerializedObject(pipeline);
            SerializedProperty rendererList = pipelineSerialized.FindProperty(
                "m_RendererDataList");
            if (rendererList == null)
            {
                throw new MissingReferenceException(
                    "PC pipeline does not expose a renderer data list.");
            }

            if (rendererList.arraySize <= TextUiRendererIndex)
            {
                rendererList.arraySize = TextUiRendererIndex + 1;
            }

            rendererList.GetArrayElementAtIndex(TextUiRendererIndex)
                .objectReferenceValue = renderer;
            pipelineSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSpeechBubblePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                SpeechBubblePrefabPath);
            try
            {
                SpeechBubbleView view = root.GetComponent<SpeechBubbleView>();
                Canvas backgroundCanvas = root.GetComponent<Canvas>();
                Transform body = root.transform.Find("SpeechTextBubble");
                TMP_Text messageText = root.GetComponentInChildren<TMP_Text>(true);
                if (view == null || backgroundCanvas == null ||
                    body == null || messageText == null)
                {
                    throw new MissingReferenceException(
                        "SpeechBubble requires its view, canvas, body, and TMP text.");
                }

                int defaultLayer = LayerMask.NameToLayer("Default");
                int textUiLayer = LayerMask.NameToLayer(TextUiLayerName);
                root.layer = defaultLayer;
                body.gameObject.layer = defaultLayer;
                backgroundCanvas.renderMode = RenderMode.WorldSpace;
                backgroundCanvas.overrideSorting = true;
                backgroundCanvas.sortingOrder = 200;

                Transform existingCanvas = body.Find(TextCanvasName);
                GameObject textCanvasObject;
                if (existingCanvas == null)
                {
                    textCanvasObject = new GameObject(
                        TextCanvasName,
                        typeof(RectTransform),
                        typeof(Canvas));
                    textCanvasObject.transform.SetParent(body, false);
                }
                else
                {
                    textCanvasObject = existingCanvas.gameObject;
                }

                RectTransform textCanvasRect =
                    (RectTransform)textCanvasObject.transform;
                textCanvasRect.anchorMin = Vector2.zero;
                textCanvasRect.anchorMax = Vector2.one;
                textCanvasRect.offsetMin = Vector2.zero;
                textCanvasRect.offsetMax = Vector2.zero;
                textCanvasRect.pivot = new Vector2(0.5f, 0.5f);
                textCanvasRect.localRotation = Quaternion.identity;
                textCanvasRect.localScale = Vector3.one;
                textCanvasObject.layer = textUiLayer;

                if (messageText.transform.parent != textCanvasRect)
                {
                    RectTransform textRect = (RectTransform)messageText.transform;
                    Vector2 anchorMin = textRect.anchorMin;
                    Vector2 anchorMax = textRect.anchorMax;
                    Vector2 anchoredPosition = textRect.anchoredPosition;
                    Vector2 sizeDelta = textRect.sizeDelta;
                    Vector2 pivot = textRect.pivot;
                    Quaternion localRotation = textRect.localRotation;
                    Vector3 localScale = textRect.localScale;

                    textRect.SetParent(textCanvasRect, false);
                    textRect.anchorMin = anchorMin;
                    textRect.anchorMax = anchorMax;
                    textRect.anchoredPosition = anchoredPosition;
                    textRect.sizeDelta = sizeDelta;
                    textRect.pivot = pivot;
                    textRect.localRotation = localRotation;
                    textRect.localScale = localScale;
                }

                Canvas textCanvas = textCanvasObject.GetComponent<Canvas>();
                textCanvas.overrideSorting = true;
                textCanvas.sortingOrder = 201;
                messageText.gameObject.layer = textUiLayer;

                Transform tail = body.Find("Image");
                if (tail != null)
                {
                    tail.gameObject.layer = defaultLayer;
                }

                Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    graphics[i].raycastTarget = false;
                }

                SerializedObject viewSerialized = new SerializedObject(view);
                viewSerialized.FindProperty("messageText").objectReferenceValue =
                    messageText;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SpeechBubblePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureEnemyCharacterPrefab()
        {
            GameObject speechPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SpeechBubblePrefabPath);
            if (speechPrefab == null)
            {
                throw new MissingReferenceException(
                    $"Speech bubble prefab was not found at {SpeechBubblePrefabPath}.");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(
                EnemyCharacterPrefabPath);
            try
            {
                Transform oldActionLabel = root.transform.Find("ActionLabel");
                if (oldActionLabel != null)
                {
                    Object.DestroyImmediate(oldActionLabel.gameObject);
                }

                Transform anchor = root.transform.Find(BubbleAnchorName);
                if (anchor == null)
                {
                    GameObject anchorObject = new GameObject(BubbleAnchorName);
                    anchor = anchorObject.transform;
                    anchor.SetParent(root.transform, false);
                }

                anchor.localPosition = BubbleAnchorLocalPosition;
                anchor.localRotation = Quaternion.identity;
                anchor.localScale = Vector3.one;

                SpeechBubbleView bubble =
                    root.GetComponentInChildren<SpeechBubbleView>(true);
                if (bubble == null)
                {
                    GameObject instance =
                        (GameObject)PrefabUtility.InstantiatePrefab(
                            speechPrefab,
                            anchor);
                    bubble = instance.GetComponent<SpeechBubbleView>();
                }
                else
                {
                    PrefabUtility.RevertObjectOverride(
                        bubble.gameObject,
                        InteractionMode.AutomatedAction);
                }

                if (bubble.transform.parent != anchor)
                {
                    bubble.transform.SetParent(anchor, false);
                }

                bubble.transform.localPosition = Vector3.zero;
                bubble.transform.localRotation = Quaternion.identity;
                bubble.gameObject.SetActive(true);

                CharacterView character = root.GetComponent<CharacterView>();
                if (character == null)
                {
                    throw new MissingReferenceException(
                        "Enemy character prefab requires CharacterView.");
                }

                SerializedObject serialized = new SerializedObject(character);
                serialized.FindProperty("actionLabel").objectReferenceValue = null;
                serialized.FindProperty("speechBubble").objectReferenceValue =
                    bubble;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, EnemyCharacterPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureCameraPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CameraPrefabPath);
            try
            {
                Camera sourceCamera = root.GetComponentInChildren<Camera>(true);
                if (sourceCamera == null)
                {
                    throw new MissingReferenceException(
                        "Camera prefab requires a source Camera.");
                }

                int textUiMask = 1 << LayerMask.NameToLayer(TextUiLayerName);
                sourceCamera.cullingMask &= ~textUiMask;

                Transform overlayTransform = sourceCamera.transform.Find(
                    OverlayCameraName);
                GameObject overlayObject;
                if (overlayTransform == null)
                {
                    overlayObject = new GameObject(OverlayCameraName);
                    overlayObject.transform.SetParent(sourceCamera.transform, false);
                }
                else
                {
                    overlayObject = overlayTransform.gameObject;
                }

                Camera overlayCamera = overlayObject.GetComponent<Camera>();
                if (overlayCamera == null)
                {
                    overlayCamera = overlayObject.AddComponent<Camera>();
                }

                overlayObject.transform.localPosition = Vector3.zero;
                overlayObject.transform.localRotation = Quaternion.identity;
                overlayObject.transform.localScale = Vector3.one;
                overlayCamera.CopyFrom(sourceCamera);
                overlayCamera.cullingMask = textUiMask;
                overlayCamera.enabled = true;

                UniversalAdditionalCameraData sourceData =
                    sourceCamera.GetComponent<UniversalAdditionalCameraData>();
                UniversalAdditionalCameraData overlayData =
                    overlayCamera.GetComponent<UniversalAdditionalCameraData>();
                if (sourceData == null)
                {
                    sourceData = sourceCamera.gameObject.AddComponent<
                        UniversalAdditionalCameraData>();
                }

                if (overlayData == null)
                {
                    overlayData = overlayObject.AddComponent<
                        UniversalAdditionalCameraData>();
                }

                overlayData.renderType = CameraRenderType.Overlay;
                overlayData.renderPostProcessing = false;
                overlayData.SetRenderer(TextUiRendererIndex);
                SerializedObject overlaySerialized = new SerializedObject(
                    overlayData);
                SerializedProperty clearDepth = overlaySerialized.FindProperty(
                    "m_ClearDepth");
                if (clearDepth != null)
                {
                    clearDepth.boolValue = true;
                }

                overlaySerialized.ApplyModifiedPropertiesWithoutUndo();

                DiaBlackJack.GameScene.TextUIOverlayCameraSync sync =
                    overlayObject.GetComponent<
                        DiaBlackJack.GameScene.TextUIOverlayCameraSync>();
                if (sync == null)
                {
                    sync = overlayObject.AddComponent<
                        DiaBlackJack.GameScene.TextUIOverlayCameraSync>();
                }

                SerializedObject syncSerialized = new SerializedObject(sync);
                syncSerialized.FindProperty("sourceCamera").objectReferenceValue =
                    sourceCamera;
                syncSerialized.ApplyModifiedPropertiesWithoutUndo();

                sourceData.cameraStack.Remove(overlayCamera);
                sourceData.cameraStack.Add(overlayCamera);
                EditorUtility.SetDirty(sourceCamera);
                EditorUtility.SetDirty(sourceData);
                EditorUtility.SetDirty(overlayCamera);
                EditorUtility.SetDirty(overlayData);

                PrefabUtility.SaveAsPrefabAsset(root, CameraPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
