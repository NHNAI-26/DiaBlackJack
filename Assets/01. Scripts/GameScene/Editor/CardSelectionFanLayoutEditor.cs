#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(CardSelectionFanLayout))]
    internal sealed class CardSelectionFanLayoutEditor : UnityEditor.Editor
    {
        private const float PreviewCardWidth = 1f;
        private const float PreviewCardHeight = 1.4f;

        private CardSelectionFanPreset _previewPreset =
            CardSelectionFanPreset.TwoCards;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("twoCardProfile"),
                includeChildren: true);
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("tenCardProfile"),
                includeChildren: true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "SCENE HANDLE PREVIEW",
                EditorStyles.boldLabel);
            _previewPreset = (CardSelectionFanPreset)EditorGUILayout.EnumPopup(
                "Preview",
                _previewPreset);
            EditorGUILayout.HelpBox(
                "Open GameScene and select this component. Move the center to " +
                "change screen position/depth; move either edge to change " +
                "width/curve; rotate the left edge to change fan angle; use " +
                "the upper scale handle to resize cards. Both sides stay symmetric.",
                MessageType.Info);

            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                Camera.main == null)
            {
                EditorGUILayout.HelpBox(
                    "Scene handles require an active Main Camera.",
                    MessageType.Warning);
            }
        }

        private void OnSceneGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            CardSelectionFanLayout layout = target as CardSelectionFanLayout;
            Camera camera = Camera.main;
            if (layout == null || camera == null)
            {
                return;
            }

            serializedObject.Update();
            SerializedProperty profile = serializedObject.FindProperty(
                _previewPreset == CardSelectionFanPreset.TwoCards
                    ? "twoCardProfile"
                    : "tenCardProfile");
            if (profile == null)
            {
                return;
            }

            SerializedProperty centerProperty =
                profile.FindPropertyRelative("viewportCenter");
            SerializedProperty distanceProperty =
                profile.FindPropertyRelative("cameraDistance");
            SerializedProperty widthProperty =
                profile.FindPropertyRelative("halfViewportWidth");
            SerializedProperty liftProperty =
                profile.FindPropertyRelative("edgeLift");
            SerializedProperty angleProperty =
                profile.FindPropertyRelative("maximumFanAngle");
            SerializedProperty scaleProperty =
                profile.FindPropertyRelative("cardScale");

            Vector2 center = centerProperty.vector2Value;
            float distance = distanceProperty.floatValue;
            float halfWidth = widthProperty.floatValue;
            float edgeLift = liftProperty.floatValue;
            float maximumAngle = angleProperty.floatValue;
            float cardScale = scaleProperty.floatValue;
            bool changed = false;

            DrawPreview(layout, camera);

            Quaternion screenRotation = camera.transform.rotation;
            Vector3 centerWorld = ToWorld(camera, center, distance);
            EditorGUI.BeginChangeCheck();
            Vector3 movedCenter = Handles.PositionHandle(
                centerWorld,
                screenRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 viewport = camera.WorldToViewportPoint(movedCenter);
                center = new Vector2(viewport.x, viewport.y);
                distance = Mathf.Max(0.01f, viewport.z);
                changed = true;
            }

            Vector2 leftViewport = new Vector2(
                center.x - halfWidth,
                center.y + edgeLift);
            Vector2 rightViewport = new Vector2(
                center.x + halfWidth,
                center.y + edgeLift);
            Vector3 leftWorld = ToWorld(camera, leftViewport, distance);
            Vector3 rightWorld = ToWorld(camera, rightViewport, distance);

            EditorGUI.BeginChangeCheck();
            Vector3 movedLeft = Handles.PositionHandle(leftWorld, screenRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 viewport = camera.WorldToViewportPoint(movedLeft);
                halfWidth = Mathf.Max(0f, center.x - viewport.x);
                edgeLift = viewport.y - center.y;
                changed = true;
            }

            EditorGUI.BeginChangeCheck();
            Vector3 movedRight = Handles.PositionHandle(rightWorld, screenRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 viewport = camera.WorldToViewportPoint(movedRight);
                halfWidth = Mathf.Max(0f, viewport.x - center.x);
                edgeLift = viewport.y - center.y;
                changed = true;
            }

            Quaternion cardBaseRotation = screenRotation *
                Quaternion.Euler(0f, 180f, 0f);
            float signedLeftAngle = edgeLift < 0f
                ? -maximumAngle
                : maximumAngle;
            Quaternion leftRotation = cardBaseRotation *
                Quaternion.Euler(0f, 0f, signedLeftAngle);
            EditorGUI.BeginChangeCheck();
            Quaternion movedRotation = Handles.RotationHandle(
                leftRotation,
                leftWorld);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion relative =
                    Quaternion.Inverse(cardBaseRotation) * movedRotation;
                maximumAngle = Mathf.Clamp(
                    Mathf.Abs(Mathf.DeltaAngle(0f, relative.eulerAngles.z)),
                    0f,
                    180f);
                changed = true;
            }

            float handleSize = HandleUtility.GetHandleSize(centerWorld);
            Vector3 scaleHandlePosition = centerWorld +
                camera.transform.up * handleSize * 0.65f;
            EditorGUI.BeginChangeCheck();
            float movedScale = Handles.ScaleSlider(
                cardScale,
                scaleHandlePosition,
                camera.transform.up,
                screenRotation,
                handleSize * 0.8f,
                0.01f);
            if (EditorGUI.EndChangeCheck())
            {
                cardScale = Mathf.Max(0.01f, movedScale);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            Undo.RecordObject(layout, "Adjust card selection fan");
            centerProperty.vector2Value = center;
            distanceProperty.floatValue = distance;
            widthProperty.floatValue = halfWidth;
            liftProperty.floatValue = edgeLift;
            angleProperty.floatValue = maximumAngle;
            scaleProperty.floatValue = cardScale;
            serializedObject.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(layout);
            EditorUtility.SetDirty(layout);
            SceneView.RepaintAll();
        }

        private void DrawPreview(
            CardSelectionFanLayout layout,
            Camera camera)
        {
            int count = _previewPreset == CardSelectionFanPreset.TwoCards
                ? 2
                : 10;
            Color previousColor = Handles.color;
            Matrix4x4 previousMatrix = Handles.matrix;
            Handles.color = new Color(0.25f, 0.9f, 1f, 0.9f);

            Vector3 previous = default;
            for (int i = 0; i < count; i++)
            {
                if (!layout.TryGetPose(
                    _previewPreset,
                    i,
                    count,
                    hovered: false,
                    out CardSelectionFanPose pose))
                {
                    continue;
                }

                Vector3 position = ToWorld(
                    camera,
                    pose.ViewportPosition,
                    pose.CameraDistance);
                Quaternion rotation = camera.transform.rotation *
                    Quaternion.Euler(0f, 180f, pose.Angle);
                Handles.matrix = Matrix4x4.TRS(
                    position,
                    rotation,
                    Vector3.one * pose.Scale);
                Handles.DrawWireCube(
                    Vector3.zero,
                    new Vector3(
                        PreviewCardWidth,
                        PreviewCardHeight,
                        0.001f));
                Handles.matrix = previousMatrix;
                Handles.Label(position, (i + 1).ToString());
                if (i > 0)
                {
                    Handles.DrawDottedLine(previous, position, 4f);
                }

                previous = position;
            }

            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }

        private static Vector3 ToWorld(
            Camera camera,
            Vector2 viewport,
            float distance)
        {
            return camera.ViewportToWorldPoint(
                new Vector3(viewport.x, viewport.y, distance));
        }
    }
}
#endif
