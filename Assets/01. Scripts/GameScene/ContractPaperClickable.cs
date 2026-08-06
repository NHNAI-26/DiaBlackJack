using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ContractPaperClickable : MonoBehaviour
    {
        [SerializeField] private Collider paperCollider;

        public bool IsInteractable { get; private set; }

        internal void SetInteractable(bool interactable)
        {
            IsInteractable = interactable;

            // Disabling the collider — not just the flag — means a non-interactable
            // paper (the decorative one underneath) physically can't be raycast-hit at
            // all, so it can't trigger hover or click through any pointer-processing
            // path, present or future.
            EnsurePaperCollider();
            if (paperCollider != null)
            {
                paperCollider.enabled = interactable;
            }
        }

        private void Awake()
        {
            EnsurePaperCollider();
        }

        private void OnValidate()
        {
            EnsurePaperCollider();
        }

        private void EnsurePaperCollider()
        {
            paperCollider ??= GetComponent<Collider>();
        }
    }
}
