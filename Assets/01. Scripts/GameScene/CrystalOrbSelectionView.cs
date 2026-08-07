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
    [RequireComponent(typeof(CardSelectionFanLayout))]
    public sealed class CrystalOrbSelectionView : MonoBehaviour
    {
        private const int MaximumCandidateCount = 2;

        private readonly CandidateSlot[] _slots =
            new CandidateSlot[MaximumCandidateCount];
        private CardSelectionFanLayout _fanLayout;
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

        internal CardSelectionFanLayout FanLayout => ResolveFanLayout();

        public void Initialize(CardView candidatePrefab)
        {
            if (candidatePrefab == null)
            {
                return;
            }

            ResolveFanLayout();
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
                slot.Card.ApplySelectionPresentation();
                slot.Card.SetUnlitPresentation();
                slot.Card.SetSortingOrder(BaseSortingOrder + i);
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
                    BaseSortingOrder + i + (hovered ? 20 : 0));
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
                card.EnableHoverVisualOnly();
                TextUIOverlayLayerUtility.ApplyRecursively(anchorObject);
                _slots[i] = new CandidateSlot(
                    anchor,
                    card.HoverVisualTransform,
                    card);
            }
        }

        private void UpdateSlotPose(int index, bool snap)
        {
            CandidateSlot slot = _slots[index];
            CardSelectionFanLayout layout = ResolveFanLayout();
            if (layout == null ||
                !layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    index,
                    _candidateCount,
                    hovered: false,
                    out CardSelectionFanPose restingPose) ||
                !layout.TryGetPose(
                    CardSelectionFanPreset.TwoCards,
                    index,
                    _candidateCount,
                    index == _hoveredIndex,
                    out CardSelectionFanPose visualPose))
            {
                return;
            }

            Vector3 restingPosition = _camera.ViewportToWorldPoint(
                new Vector3(
                    restingPose.ViewportPosition.x,
                    restingPose.ViewportPosition.y,
                    restingPose.CameraDistance));
            Vector3 visualPosition = _camera.ViewportToWorldPoint(
                new Vector3(
                    visualPose.ViewportPosition.x,
                    visualPose.ViewportPosition.y,
                    visualPose.CameraDistance));
            Quaternion restingRotation = _camera.transform.rotation *
                Quaternion.Euler(0f, 180f, restingPose.Angle);
            Quaternion visualRotation = _camera.transform.rotation *
                Quaternion.Euler(0f, 180f, visualPose.Angle);
            Vector3 restingScale = Vector3.one * restingPose.Scale;
            if (snap)
            {
                slot.Anchor.SetPositionAndRotation(restingPosition, restingRotation);
                slot.Anchor.localScale = restingScale;
                slot.Visual.SetPositionAndRotation(visualPosition, visualRotation);
                return;
            }

            float t = 1f - Mathf.Exp(-visualPose.PoseLerp * Time.deltaTime);
            slot.Anchor.position = Vector3.Lerp(
                slot.Anchor.position,
                restingPosition,
                t);
            slot.Anchor.rotation = Quaternion.Slerp(
                slot.Anchor.rotation,
                restingRotation,
                t);
            slot.Anchor.localScale = Vector3.Lerp(
                slot.Anchor.localScale,
                restingScale,
                t);
            slot.Visual.position = Vector3.Lerp(
                slot.Visual.position,
                visualPosition,
                t);
            slot.Visual.rotation = Quaternion.Slerp(
                slot.Visual.rotation,
                visualRotation,
                t);
        }

        private int BaseSortingOrder
        {
            get
            {
                CardSelectionFanLayout layout = ResolveFanLayout();
                return layout == null
                    ? 80
                    : layout.GetBaseSortingOrder(
                        CardSelectionFanPreset.TwoCards);
            }
        }

        private CardSelectionFanLayout ResolveFanLayout()
        {
            _fanLayout ??= GetComponent<CardSelectionFanLayout>();
            return _fanLayout;
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
            public CandidateSlot(
                Transform anchor,
                Transform visual,
                CardView card)
            {
                Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
                Visual = visual ?? throw new ArgumentNullException(nameof(visual));
                Card = card ?? throw new ArgumentNullException(nameof(card));
            }

            public Transform Anchor { get; }

            public Transform Visual { get; }

            public CardView Card { get; }

            public GameSceneCardViewModel Candidate { get; set; }
        }
    }
}
