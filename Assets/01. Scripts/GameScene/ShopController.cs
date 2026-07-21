using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// The GameScene-local shop for the MVP. After a battle win it strips the table down to the
    /// merchant's goods and holds the run gold. This type owns everything shop-specific — gold, the
    /// item sprites, the enemy-to-merchant swap, and which combat objects to hide — so that
    /// <see cref="GameManager"/> stays a thin coordinator and future shop growth (buying, card
    /// removal, soul recovery, prices, SOLD OUT) lands here rather than bloating the battle loop.
    ///
    /// Gold is a flat placeholder economy; RF-01 moves it to the pure run state
    /// (<c>PlayerRunState.CurrentGold</c>) keyed per enemy profile (3/3/4/6/10).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopController : MonoBehaviour
    {
        [Tooltip("The enemy character that becomes the merchant while the shop is open.")]
        [SerializeField] private CharacterView merchant;
        [Tooltip("Root of the shop-item sprites shown on the table; toggled active only while the shop is open.")]
        [SerializeField] private GameObject itemsRoot;
        [Tooltip("Combat-only table objects (deck piles, totals, hands, contract) hidden while the shop is open and restored on close.")]
        [SerializeField] private GameObject[] combatTableObjects;
        [Tooltip("Gold granted once per battle victory. Flat placeholder — RF-01 replaces it with per-profile 3/3/4/6/10 keyed by BattleProfileKey.")]
        [SerializeField] private int goldPerWin = 3;

        /// <summary>Whether the shop is currently open (table stripped to the merchant's goods).</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Run gold held so far. Accumulates across battles; reset only by <see cref="ResetGold"/>.</summary>
        public int Gold { get; private set; }

        /// <summary>
        /// Open the shop after a victory: grant the win gold once, swap the enemy to the merchant,
        /// hide the combat table objects and reveal the goods. A no-op if already open, so a caller
        /// may invoke it every re-present without double-granting.
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            Gold += goldPerWin;

            if (merchant != null)
            {
                merchant.EnterMerchant();
            }

            SetCombatTableActive(false);

            if (itemsRoot != null)
            {
                itemsRoot.SetActive(true);
            }
        }

        /// <summary>
        /// Close the shop: restore the enemy sprite, hide the goods and bring the combat table objects
        /// back. Gold is KEPT (it accumulates across the run). A no-op if not open, so it is safe to
        /// call on any restart path.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;

            if (merchant != null)
            {
                merchant.ExitMerchant();
            }

            if (itemsRoot != null)
            {
                itemsRoot.SetActive(false);
            }

            SetCombatTableActive(true);
        }

        /// <summary>Reset gold to 0 for a fresh run (called on a defeat restart).</summary>
        public void ResetGold()
        {
            Gold = 0;
        }

        // Hide/show the combat-only table objects (deck piles, totals, hands, contract) as a set, so
        // the table shows only the merchant's goods during the shop and returns to normal on close.
        private void SetCombatTableActive(bool active)
        {
            if (combatTableObjects == null)
            {
                return;
            }

            foreach (GameObject tableObject in combatTableObjects)
            {
                if (tableObject != null)
                {
                    tableObject.SetActive(active);
                }
            }
        }
    }
}
