using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ContractPaperClickable : MonoBehaviour
    {
        public bool IsInteractable { get; private set; }

        internal void SetInteractable(bool interactable)
        {
            IsInteractable = interactable;
        }
    }
}
