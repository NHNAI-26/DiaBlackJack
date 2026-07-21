using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the scene-placed HUD text. The Canvas and the three <see cref="TMP_Text"/> labels are
    /// authored in the scene (player soul top-left, enemy soul top-right, round top-center); this
    /// only writes their <c>.text</c>. Serialized-text convention follows
    /// <c>Localization/UILocalizeText.cs</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerSoulText;
        [SerializeField] private TMP_Text enemySoulText;
        [SerializeField] private TMP_Text roundText;

        public void Render(CoreLoopViewModel core)
        {
            if (core == null)
            {
                return;
            }

            if (playerSoulText != null)
            {
                playerSoulText.text = $"YOU\n{core.PlayerSoul}";
            }

            if (enemySoulText != null)
            {
                enemySoulText.text = $"ENEMY\n{core.EnemySoul}";
            }

            if (roundText != null)
            {
                roundText.text = BuildRoundText(core);
            }
        }

        private static string BuildRoundText(CoreLoopViewModel core)
        {
            switch (core.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "VICTORY";
                case BattleOutcome.PlayerDefeat:
                    return "DEFEAT";
                default:
                    return $"ROUND {core.RoundNumber}";
            }
        }
    }
}
