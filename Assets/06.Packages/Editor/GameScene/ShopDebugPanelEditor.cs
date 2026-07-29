using DiaBlackJack.GameScene;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(ShopDebugPanel))]
    public sealed class ShopDebugPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);

            ShopDebugPanel panel = (ShopDebugPanel)target;
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

            DrawShopDebug(panel, isPlaying);
        }

        private void DrawShopDebug(ShopDebugPanel panel, bool isPlaying)
        {
            EditorGUILayout.LabelField("SHOP DEBUG", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Gold", panel.Gold);
                EditorGUILayout.Toggle("Shop Open", panel.IsShopOpen);
            }

            using (new EditorGUI.DisabledScope(
                       !isPlaying || !panel.CanWinNow))
            {
                if (GUILayout.Button("Win Now"))
                {
                    panel.DebugWinNow();
                    Repaint();
                }
            }

            using (new EditorGUI.DisabledScope(
                       !isPlaying || !panel.HasShop || panel.IsShopOpen))
            {
                if (GUILayout.Button("Open Shop"))
                {
                    panel.DebugOpenShop();
                    Repaint();
                }
            }

            using (new EditorGUI.DisabledScope(
                       !isPlaying || !panel.HasShop || !panel.IsShopOpen))
            {
                if (GUILayout.Button("Close Shop"))
                {
                    panel.DebugCloseShop();
                    Repaint();
                }
            }

            using (new EditorGUI.DisabledScope(
                       !isPlaying || !panel.HasShop))
            {
                if (GUILayout.Button("Reset Gold"))
                {
                    panel.DebugResetGold();
                    Repaint();
                }
            }

            if (isPlaying &&
                panel.HasGameManager &&
                panel.HasShop &&
                !panel.IsShopOpen &&
                !panel.CanWinNow)
            {
                EditorGUILayout.HelpBox(
                    "Win Now requires an active battle in PlayerTurn.",
                    MessageType.Info);
            }
        }

    }
}
