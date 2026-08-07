using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>Shows ranks 1-10 as clickable world cards for Satan's two declarations.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardSelectionFanLayout))]
    public sealed class SatanNumberSelectionView : MonoBehaviour
    {
        private const int CandidateCount = 10;
        private const int MaximumSelectionCount = 2;
        private const int SelectedSortingBoost = 20;
        private const int HoveredSortingBoost = 40;

        private readonly CandidateSlot[] _slots = new CandidateSlot[CandidateCount];
        private readonly List<int> _selectedIndices =
            new List<int>(MaximumSelectionCount);
        private CardSelectionFanLayout _fanLayout;
        private CardView _candidatePrefab;
        private Sprite _brandSprite;
        private Camera _camera;
        private int _hoveredIndex = -1;
        private int _suppressedHoverIndex = -1;
        private int _interactionId = -1;
        private bool _isOpen;
        private bool _snapPose;

        public bool IsOpen => _isOpen;

        public int SelectedCount => _selectedIndices.Count;

        internal int HoveredCandidateIndex => _hoveredIndex;

        internal CardSelectionFanLayout FanLayout => ResolveFanLayout();

        public void Initialize(
            CardView candidatePrefab,
            Sprite brandSprite = null)
        {
            if (candidatePrefab == null)
            {
                return;
            }

            ResolveFanLayout();
            _candidatePrefab = candidatePrefab;
            _brandSprite = brandSprite;
            EnsureSlots();
            Hide();
        }

        public void Render(
            IReadOnlyList<GameSceneCardViewModel> candidates,
            Camera worldCamera,
            int interactionId = -1)
        {
            if (candidates == null || candidates.Count != CandidateCount ||
                _candidatePrefab == null)
            {
                Hide();
                return;
            }

            EnsureSlots();
            if (_interactionId != interactionId)
            {
                _hoveredIndex = -1;
                _suppressedHoverIndex = -1;
                ClearSelectionState();
                _interactionId = interactionId;
            }

            _camera = worldCamera != null ? worldCamera : Camera.main;
            _hoveredIndex = -1;
            _isOpen = true;
            for (int i = 0; i < CandidateCount; i++)
            {
                CandidateSlot slot = _slots[i];
                slot.Anchor.gameObject.SetActive(true);
                slot.Candidate = candidates[i];
                slot.Card.Bind(slot.Candidate);
                slot.Card.ApplySelectionPresentation();
                slot.Card.SetUnlitPresentation();
                slot.Card.SetSortingOrder(BaseSortingOrder + i);
                slot.BrandRenderer.sprite = _brandSprite;
                slot.BrandRenderer.gameObject.SetActive(
                    _brandSprite != null && slot.Candidate.IsSatanBranded);
                slot.BrandRenderer.sortingOrder = BaseSortingOrder + i;
            }

            ApplyVisualStates();
            _snapPose = true;
        }

        public bool Contains(CardView card)
        {
            return IndexOf(card) >= 0;
        }

        public GameSceneCardViewModel GetCandidate(CardView card)
        {
            int index = IndexOf(card);
            return index < 0 ? null : _slots[index].Candidate;
        }

        public void SetHovered(CardView card)
        {
            int nextIndex = IndexOf(card);
            if (_hoveredIndex == nextIndex)
            {
                return;
            }

            if (_suppressedHoverIndex >= 0 &&
                nextIndex != _suppressedHoverIndex)
            {
                _suppressedHoverIndex = -1;
            }

            _hoveredIndex = nextIndex;
            ApplyVisualStates();
        }

        public bool TryToggleSelection(CardView card)
        {
            int index = IndexOf(card);
            if (!_isOpen ||
                index < 0 ||
                !_slots[index].Card.DirectSelectionCommand.HasValue)
            {
                return false;
            }

            int selectedPosition = _selectedIndices.IndexOf(index);
            if (selectedPosition >= 0)
            {
                _selectedIndices.RemoveAt(selectedPosition);
                _suppressedHoverIndex = index;
                ApplyVisualStates();
                return true;
            }

            if (_selectedIndices.Count == MaximumSelectionCount)
            {
                _selectedIndices.RemoveAt(0);
            }

            _selectedIndices.Add(index);
            _suppressedHoverIndex = -1;
            ApplyVisualStates();
            return true;
        }

        public bool TryGetSelectedNumbers(
            out int firstNumber,
            out int secondNumber)
        {
            if (_selectedIndices.Count != MaximumSelectionCount)
            {
                firstNumber = 0;
                secondNumber = 0;
                return false;
            }

            firstNumber = _slots[_selectedIndices[0]].Candidate.Rank;
            secondNumber = _slots[_selectedIndices[1]].Candidate.Rank;
            return true;
        }

        public void Hide()
        {
            _hoveredIndex = -1;
            _suppressedHoverIndex = -1;
            ClearSelectionState();
            _interactionId = -1;
            _isOpen = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                CandidateSlot slot = _slots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.Candidate = null;
                slot.BrandRenderer.gameObject.SetActive(false);
                slot.Anchor.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (!_isOpen)
            {
                return;
            }

            _camera ??= Camera.main;
            if (_camera == null)
            {
                return;
            }

            for (int i = 0; i < CandidateCount; i++)
            {
                UpdateSlotPose(i, _snapPose);
            }

            _snapPose = false;
        }

        private void EnsureSlots()
        {
            if (_candidatePrefab == null)
            {
                return;
            }

            for (int i = 0; i < CandidateCount; i++)
            {
                if (_slots[i] != null)
                {
                    continue;
                }

                var anchorObject = new GameObject($"SatanNumberCandidate_{i + 1}");
                Transform anchor = anchorObject.transform;
                anchor.SetParent(transform, false);
                CardView card = Instantiate(_candidatePrefab, anchor);
                card.name = "Card";
                card.EnableHoverVisualOnly();
                Transform visual = card.HoverVisualTransform;
                SpriteRenderer brandRenderer = CreateBrandRenderer(visual);
                TextUIOverlayLayerUtility.ApplyRecursively(anchorObject);
                _slots[i] = new CandidateSlot(
                    anchor,
                    visual,
                    card,
                    brandRenderer);
            }
        }

        private SpriteRenderer CreateBrandRenderer(Transform visual)
        {
            var brandObject = new GameObject("DevilShape");
            Transform brandTransform = brandObject.transform;
            brandTransform.SetParent(visual, false);
            brandTransform.localPosition = new Vector3(0f, 0f, 0.003f);
            brandTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            brandTransform.localScale = Vector3.one * 0.38f;

            SpriteRenderer renderer = brandObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _brandSprite;
            brandObject.SetActive(false);
            return renderer;
        }

        private void UpdateSlotPose(int index, bool snap)
        {
            CandidateSlot slot = _slots[index];
            bool raised = IsSelected(index) || IsPointerHovered(index);
            CardSelectionFanLayout layout = ResolveFanLayout();
            if (layout == null ||
                !layout.TryGetPose(
                    CardSelectionFanPreset.TenCards,
                    index,
                    CandidateCount,
                    hovered: false,
                    out CardSelectionFanPose restingPose) ||
                !layout.TryGetPose(
                    CardSelectionFanPreset.TenCards,
                    index,
                    CandidateCount,
                    raised,
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
                    ? 90
                    : layout.GetBaseSortingOrder(
                        CardSelectionFanPreset.TenCards);
            }
        }

        private CardSelectionFanLayout ResolveFanLayout()
        {
            _fanLayout ??= GetComponent<CardSelectionFanLayout>();
            return _fanLayout;
        }

        private int IndexOf(CardView card)
        {
            if (card == null || !_isOpen)
            {
                return -1;
            }

            for (int i = 0; i < CandidateCount; i++)
            {
                if (_slots[i].Card == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsSelected(int index)
        {
            return _selectedIndices.Contains(index);
        }

        private bool IsPointerHovered(int index)
        {
            return index == _hoveredIndex && index != _suppressedHoverIndex;
        }

        private void ApplyVisualStates()
        {
            for (int i = 0; i < CandidateCount; i++)
            {
                CandidateSlot slot = _slots[i];
                if (slot == null)
                {
                    continue;
                }

                bool selected = IsSelected(i);
                bool hovered = IsPointerHovered(i);
                bool raised = selected || hovered;
                bool branded = selected ||
                    (slot.Candidate != null && slot.Candidate.IsSatanBranded);
                int sortingBoost = hovered
                    ? HoveredSortingBoost
                    : selected
                        ? SelectedSortingBoost
                        : 0;
                slot.Card.SetHovered(raised);
                int sortingOrder = BaseSortingOrder + i + sortingBoost;
                slot.Card.SetSortingOrder(sortingOrder);
                slot.BrandRenderer.gameObject.SetActive(
                    _brandSprite != null && branded);
                slot.BrandRenderer.sortingOrder = sortingOrder;
            }
        }

        private void ClearSelectionState()
        {
            _selectedIndices.Clear();
            ApplyVisualStates();
        }

        private sealed class CandidateSlot
        {
            public CandidateSlot(
                Transform anchor,
                Transform visual,
                CardView card,
                SpriteRenderer brandRenderer)
            {
                Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
                Visual = visual ?? throw new ArgumentNullException(nameof(visual));
                Card = card ?? throw new ArgumentNullException(nameof(card));
                BrandRenderer = brandRenderer ??
                    throw new ArgumentNullException(nameof(brandRenderer));
            }

            public Transform Anchor { get; }

            public Transform Visual { get; }

            public CardView Card { get; }

            public SpriteRenderer BrandRenderer { get; }

            public GameSceneCardViewModel Candidate { get; set; }
        }
    }
}
