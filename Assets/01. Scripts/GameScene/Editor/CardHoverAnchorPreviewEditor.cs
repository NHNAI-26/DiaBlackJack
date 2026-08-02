#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    internal static class CardHoverAnchorPreviewDrawer
    {
        private static readonly Color TopColor = new Color(0.15f, 0.9f, 1f, 1f);
        private static readonly Color BottomColor = new Color(1f, 0.45f, 0.15f, 1f);

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
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CardHoverAnchorPreviewDrawer.DrawInspectorHelp();
        }

        private void OnSceneGUI()
        {
            CardView view = target as CardView;
            CardHoverAnchorPreviewDrawer.DrawSceneAnchors(
                serializedObject,
                view == null ? null : view.transform,
                includeBottom: true);
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
