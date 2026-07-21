using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Editor-only debug panel for exercising the GameScene shop without grinding a full battle to a
    /// win. It drives only public APIs — <see cref="ShopController.Open"/>/<see cref="ShopController.Close"/>/
    /// <see cref="ShopController.ResetGold"/>, <see cref="GameHudView.SetGold"/>, and the public battle
    /// soul via <see cref="GameManager.Battle"/> — so it touches no production logic. Drop it on a
    /// dev-only GameObject in GameScene (which is not in build settings); the panel is also compiled
    /// out of player builds via <c>UNITY_EDITOR</c>.
    ///
    /// Two ways to reach the shop:
    ///  - "Win Now" depletes the enemy's soul; the next normal STAND then ends the battle in victory,
    ///    so GameManager's own flow opens the shop for real (natural gold + the "상점 나가기" button +
    ///    leave-to-next-battle).
    ///  - "Open Shop" opens it instantly for isolated visual checks (the battle is not actually over,
    ///    so GameManager keeps drawing HIT/STAND; use "Close Shop" to leave).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopDebugPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ShopController shop;
        [SerializeField] private GameHudView hud;

#if UNITY_EDITOR
        private GUIStyle _buttonStyle;

        private void OnGUI()
        {
            if (shop == null)
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };

            const float w = 160f;
            const float h = 34f;
            const float gap = 6f;
            float x = 16f;
            float y = Screen.height * 0.5f - (4f * h + 3f * gap) * 0.5f;

            GUI.Label(new Rect(x, y - 22f, w, 20f), $"DEBUG  (gold {shop.Gold})");

            if (DebugButton(ref x, ref y, w, h, gap, "Win Now (적 즉사)"))
            {
                var battle = gameManager != null ? gameManager.Battle : null;
                if (battle != null)
                {
                    // Kill the enemy, then finish the round via STAND so the battle ends in a real
                    // PlayerVictory, then nudge GameManager to re-present so its own MaybeOpenShop opens
                    // the shop through the production path (natural gold + the "상점 나가기" button).
                    battle.Enemy.Soul.ApplyDamage(9999);
                    battle.TryPlayerStand();
                    Represent();
                }
            }

            if (DebugButton(ref x, ref y, w, h, gap, "Open Shop"))
            {
                shop.Open();
                RefreshGold();
            }

            if (DebugButton(ref x, ref y, w, h, gap, "Close Shop"))
            {
                shop.Close();
                RefreshGold();
            }

            if (DebugButton(ref x, ref y, w, h, gap, "Reset Gold"))
            {
                shop.ResetGold();
                RefreshGold();
            }
        }

        // Draws one button in the vertical stack and advances y. Returns true on the frame it is clicked.
        private bool DebugButton(ref float x, ref float y, float w, float h, float gap, string label)
        {
            bool clicked = GUI.Button(new Rect(x, y, w, h), label, _buttonStyle);
            y += h + gap;
            return clicked;
        }

        // The shop owns gold, but the HUD only re-reads it on a GameManager re-present (private). Push
        // the value straight to the HUD so the counter tracks these debug actions immediately.
        private void RefreshGold()
        {
            if (hud != null)
            {
                hud.SetGold(shop.Gold);
            }
        }

        // GameManager's re-present (RefreshView) is private; from this editor-only debug tool we invoke
        // it reflectively so "Win Now" reaches the same MaybeOpenShop path a real STAND would — without
        // widening any production API.
        private void Represent()
        {
            if (gameManager == null)
            {
                return;
            }

            System.Reflection.MethodInfo method = typeof(GameManager).GetMethod(
                "RefreshView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(gameManager, null);
        }
#endif
    }
}
