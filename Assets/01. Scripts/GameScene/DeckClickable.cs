using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Marker on the remaining-deck object. <see cref="GameManager"/>'s pointer raycast looks for this
    /// (via <c>GetComponentInParent</c>) to toggle the "remaining cards" panel. Needs a collider on
    /// this object (or a child) so the raycast can hit it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeckClickable : MonoBehaviour
    {
    }
}
