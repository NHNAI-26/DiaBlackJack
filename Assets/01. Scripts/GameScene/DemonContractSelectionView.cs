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
    [RequireComponent(typeof(CardSelectionFanLayout))]
    public sealed class DemonContractSelectionView : MonoBehaviour
    {
        private const int MaximumCandidateCount = 2;

        [SerializeField] private DemonCardView candidatePrefab;

        private readonly CandidateSlot[] _slots =
            new CandidateSlot[MaximumCandidateCount];
        private CardSelectionFanLayout _fanLayout;
        private int _candidateCount;
        private int _hoveredIndex = -1;
        private Camera _camera;
        private bool _snapPose;

        public bool IsOpen => _candidateCount > 0;

        public bool HasCandidatePrefab => candidatePrefab != null;

        public int Capacity => MaximumCandidateCount;

        internal CardSelectionFanLayout FanLayout => ResolveFanLayout();

        private void Awake()
        {
            ResolveFanLayout();
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
                slot.Card.SetUnlitPresentation();
                SetSortingOrder(slot, BaseSortingOrder + i);
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
                card.EnableHoverVisualOnly();
                TextUIOverlayLayerUtility.ApplyRecursively(anchorObject);
                Renderer[] renderers = card.GetComponentsInChildren<Renderer>(true);
                _slots[i] = new CandidateSlot(
                    anchor,
                    card.HoverVisualTransform,
                    card,
                    renderers);
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

            Quaternion restingRotation =
                _camera.transform.rotation *
                Quaternion.Euler(0f, 180f, restingPose.Angle);
            Quaternion visualRotation =
                _camera.transform.rotation *
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
                Transform visual,
                DemonCardView card,
                Renderer[] renderers)
            {
                Anchor = anchor;
                Visual = visual;
                Card = card;
                Renderers = renderers ?? Array.Empty<Renderer>();
            }

            public Transform Anchor { get; }

            public Transform Visual { get; }

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
