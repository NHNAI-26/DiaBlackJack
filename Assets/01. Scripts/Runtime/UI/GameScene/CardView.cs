using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on the card prefab root. <see cref="front"/> / <see cref="back"/> toggle by the
    /// card's physical orientation; <see cref="rankText"/> shows the number whenever the viewer is
    /// allowed to see it (<see cref="GameSceneCardViewModel.RevealRank"/>) — so the player's own
    /// face-down card still shows its rank over the back, while the enemy's face-down card shows none.
    /// The rank text must sit on the prefab root (not under <see cref="front"/>), so it stays visible
    /// when the back is shown. The designer authors the two child visuals freely.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private TMP_Text rankText;

        public void Bind(GameSceneCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            if (front != null)
            {
                front.SetActive(card.IsFaceUp);
            }

            if (back != null)
            {
                back.SetActive(!card.IsFaceUp);
            }

            if (rankText != null)
            {
                rankText.enabled = card.RevealRank;
                if (card.RevealRank)
                {
                    rankText.text = card.Rank.ToString();
                }
            }
        }
    }
}
