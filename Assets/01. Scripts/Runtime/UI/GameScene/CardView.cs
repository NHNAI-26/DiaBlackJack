using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on the card prefab root. Toggles the face-up (<see cref="front"/>) and face-down
    /// (<see cref="back"/>) child objects and sets the rank text. The designer authors the two child
    /// visuals (art, size, colours) freely in the prefab; this only drives which side shows and the
    /// rank. A face-down card never receives a rank (the hidden value is stripped in presentation).
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

            if (card.IsFaceUp && rankText != null)
            {
                rankText.text = card.Rank.ToString();
            }
        }
    }
}
