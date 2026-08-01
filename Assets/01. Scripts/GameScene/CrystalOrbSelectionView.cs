using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Presents the two crystal-orb deck candidates as clickable world cards beneath the orb.
    /// Input remains routed by GameManager through each card's direct selection command.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalOrbSelectionView : MonoBehaviour
    {
        private const int MaximumCandidateCount = 2;

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
        private CardView _candidatePrefab;
        private int _candidateCount;
        private int _hoveredIndex = -1;
        private Camera _camera;
        private bool _snapPose;

        public bool IsOpen => _candidateCount > 0;

        public bool HasCandidatePrefab => _candidatePrefab != null;

        public int Capacity => MaximumCandidateCount;

        public int VisibleCandidateCount => _candidateCount;

        internal int HoveredCandidateIndex => _hoveredIndex;

        public void Initialize(CardView candidatePrefab)
        {
            if (candidatePrefab == null)
            {
                return;
            }

            _candidatePrefab = candidatePrefab;
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
            IReadOnlyList<GameSceneCardViewModel> candidates,
            Camera worldCamera)
        {
            if (candidates == null ||
                candidates.Count == 0 ||
                candidates.Count > MaximumCandidateCount ||
                _candidatePrefab == null)
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

                slot.Candidate = candidates[i];
                slot.Card.Bind(slot.Candidate);
                slot.Card.SetSortingOrder(baseSortingOrder + i);
            }

            _snapPose = true;
        }

        public bool Contains(CardView card)
        {
            return IndexOf(card) >= 0;
        }

        public GameSceneCardViewModel GetCandidate(CardView card)
        {
            int index = IndexOf(card);
            return index >= 0 ? _slots[index].Candidate : null;
        }

        public void SetHovered(CardView card)
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
                _slots[i].Card.SetSortingOrder(
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
                if (slot != null)
                {
                    slot.Candidate = null;
                    slot.Anchor.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureSlots()
        {
            if (_candidatePrefab == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    continue;
                }

                var anchorObject = new GameObject($"CrystalOrbCandidate_{i + 1}");
                Transform anchor = anchorObject.transform;
                anchor.SetParent(transform, false);
                CardView card = Instantiate(_candidatePrefab, anchor);
                card.name = "Card";
                _slots[i] = new CandidateSlot(anchor, card);
            }
        }

        private void UpdateSlotPose(int index, bool snap)
        {
            CandidateSlot slot = _slots[index];
            bool hovered = index == _hoveredIndex;
            float centerOffset = _candidateCount == 1
                ? 0f
                : index == 0 ? -viewportSpacing : viewportSpacing;
            float viewportY = viewportCenterY +
                (hovered ? hoverViewportLift : 0f);
            float distance = cameraDistance -
                (hovered ? hoverCameraPull : 0f);
            Vector3 targetPosition = _camera.ViewportToWorldPoint(
                new Vector3(
                    0.5f + centerOffset,
                    viewportY,
                    distance));
            float angle = _candidateCount == 1
                ? 0f
                : index == 0 ? fanAngle : -fanAngle;
            if (hovered)
            {
                angle = 0f;
            }
            Quaternion targetRotation = _camera.transform.rotation *
                Quaternion.Euler(0f, 180f, angle);
            Vector3 targetScale = Vector3.one * cardScale;
            if (snap)
            {
                slot.Anchor.SetPositionAndRotation(targetPosition, targetRotation);
                slot.Anchor.localScale = targetScale;
                return;
            }

            float t = 1f - Mathf.Exp(-poseLerp * Time.deltaTime);
            slot.Anchor.position = Vector3.Lerp(slot.Anchor.position, targetPosition, t);
            slot.Anchor.rotation = Quaternion.Slerp(slot.Anchor.rotation, targetRotation, t);
            slot.Anchor.localScale = Vector3.Lerp(slot.Anchor.localScale, targetScale, t);
        }

        private int IndexOf(CardView card)
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

        private sealed class CandidateSlot
        {
            public CandidateSlot(Transform anchor, CardView card)
            {
                Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
                Card = card ?? throw new ArgumentNullException(nameof(card));
            }

            public Transform Anchor { get; }

            public CardView Card { get; }

            public GameSceneCardViewModel Candidate { get; set; }
        }
    }
}
