using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>Which pile a deck object represents.</summary>
    public enum DeckKind
    {
        Draw,
        Discard,
    }

    /// <summary>
    /// Marker on a deck object (draw or discard). <see cref="GameManager"/>'s pointer raycast looks
    /// for this (via <c>GetComponentInParent</c>) and shows the matching hover panel based on
    /// <see cref="Kind"/>. Needs a collider on this object (or a child) so the raycast can hit it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeckClickable : MonoBehaviour
    {
        [SerializeField] private DeckKind kind = DeckKind.Draw;

        public DeckKind Kind => kind;
    }
}
