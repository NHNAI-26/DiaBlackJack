using System;
using DiaBlackJack.Content;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class OpponentSelectionView : MonoBehaviour
    {
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private EnemyContentCatalogSO enemyContentCatalog;
        [SerializeField] private OpponentWantedPosterView[] posterSlots =
            Array.Empty<OpponentWantedPosterView>();

        public event Action<string> OpponentSelected;

        public bool IsVisible { get; private set; }

        /// <summary>Exposed so other flow-driven posters (the final boss reveal) can resolve portraits without a duplicate serialized reference.</summary>
        internal EnemyContentCatalogSO ContentCatalog => enemyContentCatalog;

        private void OnEnable()
        {
            SetSlotSubscriptions(true);
        }

        private void OnDisable()
        {
            SetSlotSubscriptions(false);
        }

        public void Render(StageProgressionViewModel model)
        {
            if (model == null || model.OpponentCandidates.Count == 0)
            {
                Hide();
                return;
            }

            if (enemyContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "OpponentSelectionView requires EnemyContentCatalogSO.");
            }

            if (posterSlots == null ||
                model.OpponentCandidates.Count > posterSlots.Length)
            {
                throw new InvalidOperationException(
                    "OpponentSelectionView does not have enough poster slots.");
            }

            IsVisible = true;
            if (contentRoot != null)
            {
                contentRoot.SetActive(true);
            }

            for (int index = 0; index < posterSlots.Length; index++)
            {
                OpponentWantedPosterView slot = posterSlots[index];
                if (slot == null)
                {
                    throw new MissingReferenceException(
                        $"OpponentSelectionView poster slot {index} is missing.");
                }

                if (index >= model.OpponentCandidates.Count)
                {
                    slot.Hide();
                    continue;
                }

                OpponentCandidateViewModel candidate =
                    model.OpponentCandidates[index];
                slot.Render(
                    candidate,
                    enemyContentCatalog.GetPortrait(candidate.ProfileKey),
                    model.CanFocusOpponent);
            }
        }

        public void Hide()
        {
            IsVisible = false;
            if (posterSlots != null)
            {
                foreach (OpponentWantedPosterView slot in posterSlots)
                {
                    slot?.Hide();
                }
            }

            if (contentRoot != null)
            {
                contentRoot.SetActive(false);
            }
        }

        private void HandlePosterSelected(string profileKey)
        {
            if (!IsVisible)
            {
                return;
            }

            foreach (OpponentWantedPosterView slot in posterSlots)
            {
                slot?.SetInteractable(false);
            }

            OpponentSelected?.Invoke(profileKey);
        }

        private void SetSlotSubscriptions(bool subscribe)
        {
            if (posterSlots == null)
            {
                return;
            }

            foreach (OpponentWantedPosterView slot in posterSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.Selected -= HandlePosterSelected;
                if (subscribe)
                {
                    slot.Selected += HandlePosterSelected;
                }
            }
        }
    }
}
