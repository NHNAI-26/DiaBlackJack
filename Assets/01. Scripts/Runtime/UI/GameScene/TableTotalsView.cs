using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the two world-space "합 : N" totals on the table — the player's current hand total and
    /// the enemy's *visible* total (the hidden enemy card is never counted here, matching the
    /// information-hiding rule). The TMP labels are placed in the scene near each hand; this only
    /// writes their text. "합" is Korean, so the labels need a Korean-capable TMP font.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableTotalsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerTotalText;
        [SerializeField] private TMP_Text enemyTotalText;

        public void Render(int playerTotal, int enemyVisibleTotal)
        {
            if (playerTotalText != null)
            {
                playerTotalText.text = "합 : " + playerTotal;
            }

            if (enemyTotalText != null)
            {
                enemyTotalText.text = "합 : " + enemyVisibleTotal;
            }
        }
    }
}
