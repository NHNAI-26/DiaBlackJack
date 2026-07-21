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

        [Header("Usable highlight (front tint)")]
        [SerializeField] private Color normalTint = new Color(0.96f, 0.95f, 0.9f);
        [SerializeField] private Color usableTint = new Color(1f, 0.85f, 0.3f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _propertyBlock;
        private Renderer _frontRenderer;

        /// <summary>Run card id of the bound card, for diegetic click routing. -1 when unbound.</summary>
        public int CardId { get; private set; } = -1;

        /// <summary>Whether clicking this card should trigger its manual effect (player, usable only).</summary>
        public bool CanUse { get; private set; }

        public void Bind(GameSceneCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            CardId = card.CardId;
            CanUse = card.CanUse;

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

            // "Lit" cue for the diegetic click: a usable, face-up card glows. Per-instance via a
            // property block so the shared front material is not mutated.
            ApplyHighlight(card.CanUse && card.IsFaceUp);
        }

        private void ApplyHighlight(bool usable)
        {
            if (front == null)
            {
                return;
            }

            if (_frontRenderer == null)
            {
                _frontRenderer = front.GetComponent<Renderer>();
            }

            if (_frontRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _frontRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, usable ? usableTint : normalTint);
            _frontRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
