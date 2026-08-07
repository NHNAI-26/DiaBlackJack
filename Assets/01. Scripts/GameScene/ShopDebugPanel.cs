using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Editor-only debug entry point for the GameScene shop.
    /// Select the scene's Shop Debug object and use its custom Inspector while in Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopDebugPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ShopController shop;
        [SerializeField] private GameHudView hud;

#if UNITY_EDITOR
        public bool HasGameManager => gameManager != null;

        public bool HasShop => shop != null;

        public bool IsShopOpen => shop != null && shop.IsOpen;

        public int Gold => shop != null ? shop.Gold : 0;

        public bool CanWinNow =>
            Application.isPlaying &&
            gameManager != null &&
            shop != null &&
            !shop.IsOpen &&
            gameManager.Battle != null &&
            gameManager.Battle.State == CoreLoopState.PlayerTurn;

        public bool DebugWinNow()
        {
            if (!CanWinNow)
            {
                return false;
            }

            CoreLoopBattle battle = gameManager.Battle;
            battle.Enemy.Soul.ApplyDamage(9999);
            if (!battle.TryPlayerStand())
            {
                return false;
            }

            gameManager.RefreshForDebug();
            return true;
        }

        public bool DebugOpenShop()
        {
            if (!Application.isPlaying ||
                gameManager == null ||
                shop == null ||
                shop.IsOpen)
            {
                return false;
            }

            return gameManager.DebugOpenStandaloneShop();
        }

        public bool DebugCloseShop()
        {
            if (!Application.isPlaying ||
                gameManager == null ||
                shop == null ||
                !shop.IsOpen)
            {
                return false;
            }

            return gameManager.DebugCloseStandaloneShop();
        }

        public bool DebugResetGold()
        {
            if (!Application.isPlaying || shop == null)
            {
                return false;
            }

            shop.ResetGold();
            if (hud != null)
            {
                hud.SetGold(shop.Gold);
            }

            return true;
        }

#endif
    }
}
