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

        private readonly CandidateSlot[] _slots = new CandidateSlot[CandidateCount];
        private CardSelectionFanLayout _fanLayout;
        private CardView _candidatePrefab;
        private Sprite _brandSprite;
        private Camera _camera;
        private int _hoveredIndex = -1;
        private bool _isOpen;
        private bool _snapPose;

        public bool IsOpen => _isOpen;

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
            Camera worldCamera)
        {
            if (candidates == null || candidates.Count != CandidateCount ||
                _candidatePrefab == null)
            {
                Hide();
                return;
            }

            EnsureSlots();
            _camera = worldCamera != null ? worldCamera : Camera.main;
            _hoveredIndex = -1;
            _isOpen = true;
            for (int i = 0; i < CandidateCount; i++)
            {
                CandidateSlot slot = _slots[i];
                slot.Anchor.gameObject.SetActive(true);
                slot.Candidate = candidates[i];
                slot.Card.Bind(slot.Candidate);
                slot.Card.SetSortingOrder(BaseSortingOrder + i);
                slot.BrandRenderer.sprite = _brandSprite;
                slot.BrandRenderer.gameObject.SetActive(
                    _brandSprite != null && slot.Candidate.IsSatanBranded);
                slot.BrandRenderer.sortingOrder = BaseSortingOrder + i;
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
            return index < 0 ? null : _slots[index].Candidate;
        }

        public void SetHovered(CardView card)
        {
            int nextIndex = IndexOf(card);
            if (_hoveredIndex == nextIndex)
            {
                return;
            }

            _hoveredIndex = nextIndex;
            for (int i = 0; i < CandidateCount; i++)
            {
                bool hovered = i == _hoveredIndex;
                _slots[i].Card.SetHovered(hovered);
                _slots[i].Card.SetSortingOrder(
                    BaseSortingOrder + i + (hovered ? 20 : 0));
                _slots[i].BrandRenderer.sortingOrder =
                    BaseSortingOrder + i + (hovered ? 20 : 0);
            }
        }

        public void Hide()
        {
            SetHovered(null);
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
