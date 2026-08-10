using DiaBlackJack.GameScene;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(SoulLossDebugPanel))]
    public sealed class SoulLossDebugPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);

            SoulLossDebugPanel panel = (SoulLossDebugPanel)target;
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
                    "Close the shop before running the mutual-bust test.",
                    MessageType.Warning);
            }

            EditorGUILayout.LabelField(
                "SOUL LOSS DEBUG",
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!panel.CanRunTest))
            {
                if (GUILayout.Button("Mutual Bust / Both Soul -1"))
                {
                    panel.DebugMutualBust();
                }
            }
        }
    }
}
