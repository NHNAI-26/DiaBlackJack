using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the two world-space totals on the table. The player's label receives both the full
    /// total and the public-card total; the enemy label receives only the public-card total so its
    /// hidden rank never crosses the information boundary. Formatting is supplied by the pure
    /// presentation model; this component only writes the prepared strings to the scene TMP labels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableTotalsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerTotalText;
        [SerializeField] private TMP_Text enemyTotalText;

        public void Render(string playerTotals, string enemyVisibleTotal)
        {
            if (playerTotalText != null)
            {
                playerTotalText.text = playerTotals ?? string.Empty;
            }

            if (enemyTotalText != null)
            {
                enemyTotalText.text = enemyVisibleTotal ?? string.Empty;
            }
        }
    }
}
