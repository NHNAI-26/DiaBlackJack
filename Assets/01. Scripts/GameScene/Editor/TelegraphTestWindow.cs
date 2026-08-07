#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiaBlackJack.GameScene.Editor
{
    internal sealed class TelegraphTestWindow : EditorWindow
    {
        private const string MenuPath = "Tools/DiaBlackJack/Telegraph Test";

        private Telegraph _telegraph;
        private Vector2 _scroll;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            TelegraphTestWindow window =
                GetWindow<TelegraphTestWindow>("Telegraph Test");
            window.FindSceneTelegraph();
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            Selection.selectionChanged += HandleSelectionChanged;
            if (_telegraph == null)
            {
                FindSceneTelegraph();
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("TELEGRAPH APPEARANCE", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _telegraph = EditorGUILayout.ObjectField(
                    "Telegraph",
                    _telegraph,
                    typeof(Telegraph),
                    true) as Telegraph;

                if (GUILayout.Button("Selection", GUILayout.Width(82f)))
                {
                    RefreshFromSelection();
                }

                if (GUILayout.Button("Find", GUILayout.Width(56f)))
                {
                    FindSceneTelegraph();
                }
            }

            if (_telegraph != null && EditorUtility.IsPersistent(_telegraph))
            {
                EditorGUILayout.HelpBox(
                    "The selected Telegraph is a prefab asset. Create a scene instance before testing.",
                    MessageType.Info);
                if (GUILayout.Button("Create Scene Test Instance"))
                {
                    CreateSceneTestInstance();
                }
            }

            if (_telegraph == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Telegraph from the scene to test its appearance animation.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            SerializedObject serialized = new SerializedObject(_telegraph);
            serialized.Update();
            DrawProperty(serialized, "playEntranceOnEnable");
            DrawProperty(serialized, "appearanceMoveDirection");
            DrawProperty(serialized, "appearanceMoveDistance");
            DrawProperty(serialized, "appearanceEnterDuration");
            DrawProperty(serialized, "appearanceExitDuration");
            DrawProperty(serialized, "appearanceEnterMoveEase");
            DrawProperty(serialized, "appearanceExitMoveEase");
            DrawProperty(serialized, "appearanceDitherAlphaCurve");
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField(
                "Animation",
                "Entrance: source direction to current / Exit: current to source with dither alpha");

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Test Entrance"))
                    {
                        TestEntrance();
                    }

                    if (GUILayout.Button("Test Exit"))
                    {
                        TestExit();
                    }
                }

                if (GUILayout.Button("Reset Telegraph Appearance"))
                {
                    ResetTelegraph();
                }
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode before running the Telegraph animation test.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawProperty(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void TestEntrance()
        {
            if (!TryPrepareTelegraph())
            {
                return;
            }

            _telegraph.PlayEntranceAnimation();
            SceneView.RepaintAll();
        }

        private void TestExit()
        {
            if (!TryPrepareTelegraph())
            {
                return;
            }

            _telegraph.PlayExitAnimation();
            SceneView.RepaintAll();
        }

        private void ResetTelegraph()
        {
            if (!TryPrepareTelegraph())
            {
                return;
            }

            _telegraph.ResetAppearanceVisualState();
            SceneView.RepaintAll();
        }

        private bool TryPrepareTelegraph()
        {
            if (_telegraph == null || EditorUtility.IsPersistent(_telegraph))
            {
                return false;
            }

            Transform current = _telegraph.transform;
            while (current != null)
            {
                current.gameObject.SetActive(true);
                current = current.parent;
            }

            return true;
        }

        private void CreateSceneTestInstance()
        {
            if (_telegraph == null ||
                !EditorUtility.IsPersistent(_telegraph) ||
                EditorApplication.isPlaying)
            {
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(
                _telegraph.gameObject,
                SceneManager.GetActiveScene()) as GameObject;
            if (instance == null)
            {
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create Telegraph Test Instance");
            _telegraph = instance.GetComponentInChildren<Telegraph>(true);
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Repaint();
        }

        private void RefreshFromSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                return;
            }

            _telegraph = selected.GetComponentInParent<Telegraph>();
            _telegraph ??= selected.GetComponentInChildren<Telegraph>(true);
            Repaint();
        }

        private void FindSceneTelegraph()
        {
            Telegraph[] telegraphs =
                Resources.FindObjectsOfTypeAll<Telegraph>();
            for (int i = 0; i < telegraphs.Length; i++)
            {
                Telegraph candidate = telegraphs[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                _telegraph = candidate;
                Repaint();
                return;
            }

            _telegraph = null;
            Repaint();
        }

        private void HandleSelectionChanged()
        {
            RefreshFromSelection();
        }
    }
}
#endif
