using DiaBlackJack.GameScene;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(AutomaticCardDebugPanel))]
    public sealed class AutomaticCardDebugPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);

            AutomaticCardDebugPanel panel =
                (AutomaticCardDebugPanel)target;
            bool isPlaying = EditorApplication.isPlaying;

            if (!isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Debug actions are available only in Play Mode.",
                    MessageType.Info);
            }

            if (!panel.HasGameManager)
            {
                EditorGUILayout.HelpBox(
                    "GameManager reference is missing.",
                    MessageType.Error);
            }

            if (!panel.HasShop)
            {
                EditorGUILayout.HelpBox(
                    "ShopController reference is missing.",
                    MessageType.Error);
            }

            if (isPlaying && panel.IsShopOpen)
            {
                EditorGUILayout.HelpBox(
                    "Close the shop before running an automatic-card test.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(
                       !isPlaying || !panel.CanRunTest))
            {
                EditorGUILayout.LabelField(
                    "PLAYER AUTOMATIC CARD",
                    EditorStyles.boldLabel);
                DrawPlayerButtons(panel);

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    "ENEMY AUTOMATIC CARD",
                    EditorStyles.boldLabel);
                DrawEnemyButtons(panel);
            }
        }

        private static void DrawPlayerButtons(AutomaticCardDebugPanel panel)
        {
            if (GUILayout.Button("Player Poison"))
            {
                panel.DebugPlayerPoison();
            }

            if (GUILayout.Button("Player Resurrection Herb"))
            {
                panel.DebugPlayerResurrectionHerb();
            }

            if (GUILayout.Button("Player Lie Detector"))
            {
                panel.DebugPlayerLieDetector();
            }

            if (GUILayout.Button("Player Flamethrower"))
            {
                panel.DebugPlayerFlamethrower();
            }

            if (GUILayout.Button("Player Pocket Watch"))
            {
                panel.DebugPlayerPocketWatch();
            }
        }

        private static void DrawEnemyButtons(AutomaticCardDebugPanel panel)
        {
            if (GUILayout.Button("Enemy Poison"))
            {
                panel.DebugEnemyPoison();
            }

            if (GUILayout.Button("Enemy Resurrection Herb"))
            {
                panel.DebugEnemyResurrectionHerb();
            }

            if (GUILayout.Button("Enemy Lie Detector"))
            {
                panel.DebugEnemyLieDetector();
            }

            if (GUILayout.Button("Enemy Flamethrower"))
            {
                panel.DebugEnemyFlamethrower();
            }

            if (GUILayout.Button("Enemy Pocket Watch"))
            {
                panel.DebugEnemyPocketWatch();
            }
        }
    }
}
