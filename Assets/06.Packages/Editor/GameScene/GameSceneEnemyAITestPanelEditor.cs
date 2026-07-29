using DiaBlackJack.GameScene;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(GameSceneEnemyAITestPanel))]
    public sealed class GameSceneEnemyAITestPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);

            GameSceneEnemyAITestPanel panel =
                (GameSceneEnemyAITestPanel)target;
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
                    "Close the shop before running an enemy AI test.",
                    MessageType.Warning);
            }

            EditorGUILayout.LabelField(
                "ENEMY AI TEST",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(
                       !isPlaying || !panel.CanRunTest))
            {
                if (GUILayout.Button("Enemy Revolver Hit"))
                {
                    panel.DebugEnemyRevolverHit();
                    Repaint();
                }

                if (GUILayout.Button("Enemy Revolver Miss"))
                {
                    panel.DebugEnemyRevolverMiss();
                    Repaint();
                }

                if (GUILayout.Button("Enemy Hammer"))
                {
                    panel.DebugEnemyHammer();
                    Repaint();
                }
            }
        }
    }
}
