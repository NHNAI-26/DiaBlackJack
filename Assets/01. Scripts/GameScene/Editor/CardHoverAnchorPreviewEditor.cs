#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    internal static class CardHoverAnchorPreviewDrawer
    {
        private static readonly Color TopColor = new Color(0.15f, 0.9f, 1f, 1f);
        private static readonly Color BottomColor = new Color(1f, 0.45f, 0.15f, 1f);
        private static readonly int PixelOutlineColorId =
            Shader.PropertyToID("_PixelOutlineColor");
        private static readonly int PixelOutlineWidthId =
            Shader.PropertyToID("_PixelOutlineWidth");
        private static readonly int PixelOutlineAlphaThresholdId =
            Shader.PropertyToID("_PixelOutlineAlphaThreshold");
        private static readonly int PixelOutlineVisibilityId =
            Shader.PropertyToID("_PixelOutlineVisibility");

        public static readonly string[] HoverOutlineStateLabels =
        {
            "Basic / Passive",
            "Manual Unavailable",
            "Manual Available",
            "Automatic",
            "Used",
        };

        public static void DrawInspectorHelp()
        {
            EditorGUILayout.HelpBox(
                "Select the card root in Prefab Mode to preview the exact tooltip anchor. " +
                "Cyan marks TOP; orange marks BOTTOM.",
                MessageType.Info);
        }

        public static void DrawSceneAnchors(
            SerializedObject serialized,
            Transform owner,
            bool includeBottom)
        {
            serialized.Update();
            DrawAnchor(
                serialized.FindProperty("topPosition"),
                owner,
                TopColor,
                "TOP TOOLTIP");

            if (includeBottom)
            {
                DrawAnchor(
                    serialized.FindProperty("bottomPosition"),
                    owner,
                    BottomColor,
                    "BOTTOM TOOLTIP");
            }
        }

        public static void ApplyHoverOutlinePreview(
            SerializedObject serialized,
            CardView view,
            GameSceneCardHoverOutlineState state,
            IDictionary<Renderer, MaterialPropertyBlock> originalBlocks)
        {
            if (serialized == null || view == null || originalBlocks == null)
            {
                return;
            }

            serialized.Update();
            Color color = ResolveHoverOutlineColor(serialized, state);
            ApplyHoverOutlinePreview(
                GetFaceRenderer(serialized, "front"),
                serialized,
                color,
                originalBlocks);
            ApplyHoverOutlinePreview(
                GetFaceRenderer(serialized, "back"),
                serialized,
                color,
                originalBlocks);
        }

        public static void RestoreHoverOutlinePreview(
            IDictionary<Renderer, MaterialPropertyBlock> originalBlocks)
        {
            if (originalBlocks == null)
            {
                return;
            }

            foreach (KeyValuePair<Renderer, MaterialPropertyBlock> entry in originalBlocks)
            {
                if (entry.Key != null)
                {
                    entry.Key.SetPropertyBlock(entry.Value);
                }
            }

            originalBlocks.Clear();
        }

        private static void ApplyHoverOutlinePreview(
            Renderer renderer,
            SerializedObject serialized,
            Color color,
            IDictionary<Renderer, MaterialPropertyBlock> originalBlocks)
        {
            if (renderer == null)
            {
                return;
            }

            if (!originalBlocks.ContainsKey(renderer))
            {
                var original = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original);
                originalBlocks.Add(renderer, original);
            }

            var preview = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(preview);
            preview.SetColor(PixelOutlineColorId, color);
            preview.SetFloat(
                PixelOutlineWidthId,
                ResolveOutlineFloat(
                    serialized,
                    renderer,
                    PixelOutlineWidthId,
                    "hoverOutlineWidth",
                    1f));
            preview.SetFloat(
                PixelOutlineAlphaThresholdId,
                ResolveOutlineFloat(
                    serialized,
                    renderer,
                    PixelOutlineAlphaThresholdId,
                    "hoverOutlineAlphaThreshold",
                    0.5f));
            preview.SetFloat(PixelOutlineVisibilityId, 1f);
            renderer.SetPropertyBlock(preview);
        }

        private static Renderer GetFaceRenderer(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            GameObject face = property?.objectReferenceValue as GameObject;
            return face == null ? null : face.GetComponent<SpriteRenderer>();
        }

        private static Color ResolveHoverOutlineColor(
            SerializedObject serialized,
            GameSceneCardHoverOutlineState state)
        {
            string propertyName;
            switch (state)
            {
                case GameSceneCardHoverOutlineState.ManualUnavailable:
                    propertyName = "unavailableHoverOutlineColor";
                    break;
                case GameSceneCardHoverOutlineState.ManualAvailable:
                    propertyName = "availableHoverOutlineColor";
                    break;
                case GameSceneCardHoverOutlineState.Automatic:
                    propertyName = "automaticHoverOutlineColor";
                    break;
                case GameSceneCardHoverOutlineState.Used:
                    propertyName = "usedHoverOutlineColor";
                    break;
                default:
                    propertyName = "basicHoverOutlineColor";
                    break;
            }

            SerializedProperty property = serialized.FindProperty(propertyName);
            return property == null ? Color.white : property.colorValue;
        }

        private static float ResolveOutlineFloat(
            SerializedObject serialized,
            Renderer renderer,
            int materialPropertyId,
            string serializedPropertyName,
            float fallback)
        {
            SerializedProperty useMaterial = serialized.FindProperty(
                "useMaterialHoverOutlineSettings");
            Material material = renderer.sharedMaterial;
            if ((useMaterial == null || useMaterial.boolValue) &&
                material != null &&
                material.HasProperty(materialPropertyId))
            {
                return material.GetFloat(materialPropertyId);
            }

            SerializedProperty property = serialized.FindProperty(
                serializedPropertyName);
            return property == null ? fallback : property.floatValue;
        }

        private static void DrawAnchor(
            SerializedProperty property,
            Transform owner,
            Color color,
            string label)
        {
            Transform anchor = property?.objectReferenceValue as Transform;
            if (anchor == null || owner == null)
            {
                return;
            }

            float size = HandleUtility.GetHandleSize(anchor.position) * 0.08f;
            Handles.color = color;
            Handles.DrawDottedLine(owner.position, anchor.position, 4f);
            Handles.SphereHandleCap(
                0,
                anchor.position,
                Quaternion.identity,
                size,
                EventType.Repaint);
            Handles.Label(anchor.position + owner.up * size, label);
        }
    }

    [CustomEditor(typeof(CardView))]
    internal sealed class CardViewHoverAnchorEditor : UnityEditor.Editor
    {
        private readonly Dictionary<Renderer, MaterialPropertyBlock>
            _originalHoverOutlineBlocks =
                new Dictionary<Renderer, MaterialPropertyBlock>();
        private bool _previewHoverOutline;
        private int _previewHoverOutlineState;

        public override void OnInspectorGUI()
        {
            bool propertiesChanged = DrawDefaultInspector();
            CardHoverAnchorPreviewDrawer.DrawInspectorHelp();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Hover Outline Preview",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scene View preview only. Uses the existing pixel-outline shader and does not save preview values to the prefab or material.",
                MessageType.Info);

            bool wasPreviewing = _previewHoverOutline;
            EditorGUI.BeginChangeCheck();
            _previewHoverOutline = EditorGUILayout.Toggle(
                "Preview Hover Outline",
                _previewHoverOutline);
            _previewHoverOutlineState = EditorGUILayout.Popup(
                "Preview State",
                _previewHoverOutlineState,
                CardHoverAnchorPreviewDrawer.HoverOutlineStateLabels);
            bool controlsChanged = EditorGUI.EndChangeCheck();

            if (_previewHoverOutline)
            {
                if (propertiesChanged ||
                    controlsChanged ||
                    _originalHoverOutlineBlocks.Count == 0)
                {
                    CardHoverAnchorPreviewDrawer.ApplyHoverOutlinePreview(
                        serializedObject,
                        target as CardView,
                        (GameSceneCardHoverOutlineState)_previewHoverOutlineState,
                        _originalHoverOutlineBlocks);
                    SceneView.RepaintAll();
                }
            }
            else if (wasPreviewing)
            {
                RestoreHoverOutlinePreview();
            }
        }

        private void OnDisable()
        {
            RestoreHoverOutlinePreview();
        }

        private void OnSceneGUI()
        {
            CardView view = target as CardView;
            CardHoverAnchorPreviewDrawer.DrawSceneAnchors(
                serializedObject,
                view == null ? null : view.transform,
                includeBottom: true);
        }

        private void RestoreHoverOutlinePreview()
        {
            CardHoverAnchorPreviewDrawer.RestoreHoverOutlinePreview(
                _originalHoverOutlineBlocks);
            SceneView.RepaintAll();
        }
    }

    [CustomEditor(typeof(DemonCardView))]
    internal sealed class DemonCardViewHoverAnchorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CardHoverAnchorPreviewDrawer.DrawInspectorHelp();
        }

        private void OnSceneGUI()
        {
            DemonCardView view = target as DemonCardView;
            CardHoverAnchorPreviewDrawer.DrawSceneAnchors(
                serializedObject,
                view == null ? null : view.transform,
                includeBottom: false);
        }
    }
}
#endif
