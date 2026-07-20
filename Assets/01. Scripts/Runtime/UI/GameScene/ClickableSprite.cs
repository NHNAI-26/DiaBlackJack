using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Raises <see cref="Clicked"/> when the object is clicked with the left mouse button.
    /// Relies on Unity's built-in <c>OnMouseDown</c>, so the GameObject needs a <see cref="Collider"/>
    /// and a camera in the scene. Used for diegetic input placeholders (deck pile, bell, restart)
    /// and for clickable cards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClickableSprite : MonoBehaviour
    {
        public event Action<ClickableSprite> Clicked;

        /// <summary>Optional integer payload — used to carry a card id for clickable cards.</summary>
        public int Payload { get; set; }

        public bool Interactable { get; set; } = true;

        private void OnMouseDown()
        {
            if (!Interactable)
            {
                return;
            }

            Clicked?.Invoke(this);
        }
    }
}
