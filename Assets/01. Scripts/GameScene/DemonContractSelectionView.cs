using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Renders the two normal demon-contract candidates as screen-anchored world cards.
    /// The HUD owns description text; this view owns card binding, fan pose, and hover lift.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DemonContractSelectionView : MonoBehaviour
    {
        private const int MaximumCandidateCount = 2;

        [SerializeField] private DemonCardView candidatePrefab;
        [SerializeField] private float cameraDistance = 1.5f;
        [SerializeField] private float viewportCenterY = 0f;
        [SerializeField] private float viewportSpacing = 0.11f;
        [SerializeField] private float hoverViewportLift = 0.18f;
        [SerializeField] private float hoverCameraPull = 0.1f;
        [SerializeField] private float fanAngle = 7f;
        [SerializeField] private float cardScale = 1f;
        [SerializeField] private float poseLerp = 14f;
        [SerializeField] private int baseSortingOrder = 80;

        private readonly CandidateSlot[] _slots =
            new CandidateSlot[MaximumCandidateCount];
        private int _candidateCount;
        private int _hoveredIndex = -1;
        private Camera _camera;
        private bool _snapPose;

        public bool IsOpen => _candidateCount > 0;

        public bool HasCandidatePrefab => candidatePrefab != null;

        public int Capacity => MaximumCandidateCount;

        private void Awake()
        {
            EnsureSlots();
            Hide();
        }

        private void LateUpdate()
        {
            if (!IsOpen)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                return;
            }

            for (int i = 0; i < _candidateCount; i++)
            {
                UpdateSlotPose(i, _snapPose);
            }

            _snapPose = false;
        }

        public void Render(
            IReadOnlyList<GameSceneCombatHudContractCandidateViewModel> candidates,
            Camera worldCamera)
        {
            if (candidates == null ||
                candidates.Count == 0 ||
                candidates.Count > MaximumCandidateCount ||
                candidatePrefab == null)
            {
                Hide();
                return;
            }

            EnsureSlots();
            _camera = worldCamera != null ? worldCamera : Camera.main;
            _candidateCount = candidates.Count;
            _hoveredIndex = -1;
            for (int i = 0; i < _slots.Length; i++)
            {
                CandidateSlot slot = _slots[i];
                bool active = i < _candidateCount;
                slot.Anchor.gameObject.SetActive(active);
                if (!active)
                {
                    slot.Candidate = null;
                    continue;
                }

                GameSceneCombatHudContractCandidateViewModel candidate = candidates[i];
                slot.Candidate = candidate;
                slot.Card.Bind(new GameSceneDemonCardViewModel(
                    candidate.Command.OptionId,
                    candidate.DefinitionKey,
                    isFaceUp: true,
                    canUse: candidate.IsInteractable,
                    displayName: candidate.Title,
                    summary: candidate.Ability,
                    costSummary: candidate.Cost));
                SetSortingOrder(slot, baseSortingOrder + i);
            }

            _snapPose = true;
        }

        public bool Contains(DemonCardView card)
        {
            return IndexOf(card) >= 0;
        }

        public GameSceneCombatHudContractCandidateViewModel GetCandidate(
            DemonCardView card)
        {
            int index = IndexOf(card);
            return index >= 0 ? _slots[index].Candidate : null;
        }

        public void SetHovered(DemonCardView card)
        {
            int nextIndex = IndexOf(card);
            if (_hoveredIndex == nextIndex)
            {
                return;
            }

            _hoveredIndex = nextIndex;
            for (int i = 0; i < _candidateCount; i++)
            {
                bool hovered = i == _hoveredIndex;
                _slots[i].Card.SetHovered(hovered);
                SetSortingOrder(
                    _slots[i],
                    baseSortingOrder + i + (hovered ? 20 : 0));
            }
        }

        public void Hide()
        {
            SetHovered(null);
            _candidateCount = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                CandidateSlot slot = _slots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.Candidate = null;
                slot.Anchor.gameObject.SetActive(false);
            }
        }

        private void EnsureSlots()
        {
            if (candidatePrefab == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    continue;
                }

                var anchorObject = new GameObject($"ContractCandidate_{i + 1}");
                Transform anchor = anchorObject.transform;
                anchor.SetParent(transform, false);
                DemonCardView card = Instantiate(candidatePrefab, anchor);
                card.name = "DemonCard";
                Renderer[] renderers = card.GetComponentsInChildren<Renderer>(true);
                _slots[i] = new CandidateSlot(anchor, card, renderers);
            }
        }

        private void UpdateSlotPose(int index, bool snap)
        {
            CandidateSlot slot = _slots[index];
            bool hovered = index == _hoveredIndex;
            float centerOffset = _candidateCount == 1
                ? 0f
                : (index == 0 ? -viewportSpacing : viewportSpacing);
            float viewportY = viewportCenterY +
                (hovered ? hoverViewportLift : 0f);
            float distance = cameraDistance -
                (hovered ? hoverCameraPull : 0f);
            Vector3 targetPosition = _camera.ViewportToWorldPoint(
                new Vector3(0.5f + centerOffset, viewportY, distance));
            float angle = _candidateCount == 1
                ? 0f
                : index == 0 ? fanAngle : -fanAngle;
            if (hovered)
            {
                angle = 0f;
            }

            Quaternion targetRotation =
                _camera.transform.rotation *
                Quaternion.Euler(0f, 180f, angle);
            Vector3 targetScale = Vector3.one * cardScale;
            if (snap)
            {
                slot.Anchor.SetPositionAndRotation(targetPosition, targetRotation);
                slot.Anchor.localScale = targetScale;
                return;
            }

            float t = 1f - Mathf.Exp(-poseLerp * Time.deltaTime);
            slot.Anchor.position = Vector3.Lerp(
                slot.Anchor.position,
                targetPosition,
                t);
            slot.Anchor.rotation = Quaternion.Slerp(
                slot.Anchor.rotation,
                targetRotation,
                t);
            slot.Anchor.localScale = Vector3.Lerp(
                slot.Anchor.localScale,
                targetScale,
                t);
        }

        private int IndexOf(DemonCardView card)
        {
            if (card == null)
            {
                return -1;
            }

            for (int i = 0; i < _candidateCount; i++)
            {
                if (_slots[i].Card == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void SetSortingOrder(CandidateSlot slot, int order)
        {
            foreach (Renderer renderer in slot.Renderers)
            {
                if (renderer != null)
                {
                    renderer.sortingOrder = order;
                }
            }
        }

        private sealed class CandidateSlot
        {
            public CandidateSlot(
                Transform anchor,
                DemonCardView card,
                Renderer[] renderers)
            {
                Anchor = anchor;
                Card = card;
                Renderers = renderers ?? Array.Empty<Renderer>();
            }

            public Transform Anchor { get; }

            public DemonCardView Card { get; }

            public Renderer[] Renderers { get; }

            public GameSceneCombatHudContractCandidateViewModel Candidate
            {
                get;
                set;
            }
        }
    }
}
