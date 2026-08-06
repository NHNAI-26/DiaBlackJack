using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on a hand anchor (PlayerHand / EnemyHand). Owns the card prefab and layout settings,
    /// and lays spawned cards out center-aligned along the anchor's local X. The designer positions
    /// and rotates the anchor in the scene (over the table) and tunes <see cref="spacing"/> here;
    /// card size/art live on the prefab. Cards are pooled — reused across renders, not rebuilt each
    /// frame — so a hit adds one card without churning the rest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardHand : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private DemonCardView demonCardPrefab;
        [SerializeField] private float spacing = 1.1f;
        [SerializeField] private float depthStagger = 0.01f;
        [SerializeField] private int sortingOrderBase;
        [SerializeField] private int sortingOrderStep = 1;
        [SerializeField] private float moveDuration = 0.16f;
        [SerializeField] private AnimationCurve moveCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float enterDuration = 0.22f;
        [SerializeField] private AnimationCurve enterCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float bodyEntryDistance = 1.2f;
        [SerializeField] private bool invertBodyEntryDirection;
        [SerializeField] private float discardDuration = 0.22f;
        [SerializeField] private float discardExitDistance = 1.2f;
        [SerializeField] private AnimationCurve discardCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly List<CardView> _spawned = new List<CardView>();
        private readonly List<Tween> _moveTweens = new List<Tween>();
        private readonly List<DemonCardView> _spawnedDemonCards =
            new List<DemonCardView>();
        private readonly List<Tween> _demonMoveTweens = new List<Tween>();
        private bool _hasRenderedLayout;

        internal CardView CardPrefab => cardPrefab;

        internal DemonCardView DemonCardPrefab => demonCardPrefab;

        private void OnValidate()
        {
            spacing = Mathf.Max(0f, spacing);
            depthStagger = Mathf.Max(0f, depthStagger);
            sortingOrderStep = Mathf.Max(1, sortingOrderStep);
            moveDuration = Mathf.Max(0f, moveDuration);
            enterDuration = Mathf.Max(0f, enterDuration);
            bodyEntryDistance = Mathf.Max(0f, bodyEntryDistance);
            discardDuration = Mathf.Max(0f, discardDuration);
            discardExitDistance = Mathf.Max(0f, discardExitDistance);
            moveCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            enterCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            discardCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        private void OnDisable()
        {
            KillAllMoveTweens();
        }

        public bool Render(IReadOnlyList<GameSceneCardViewModel> cards)
        {
            return Render(cards, Array.Empty<GameSceneDemonCardViewModel>());
        }

        /// <returns>True if binding any card this call started its reveal-flip animation
        /// (back turning to face-up). Callers syncing other UI (e.g. a hand total) to the
        /// actual visual reveal should wait <see cref="CardView.RevealFaceSwapSeconds"/> before
        /// reacting to the new state when this returns true.</returns>
        public bool Render(
            IReadOnlyList<GameSceneCardViewModel> cards,
            IReadOnlyList<GameSceneDemonCardViewModel> demonCards)
        {
            return Render(
                cards,
                demonCards,
                showTransientEffectSources: true);
        }

        internal bool Render(
            IReadOnlyList<GameSceneCardViewModel> cards,
            IReadOnlyList<GameSceneDemonCardViewModel> demonCards,
            bool showTransientEffectSources)
        {
            if (cardPrefab == null ||
                cards == null ||
                demonCards == null ||
                (demonCards.Count > 0 && demonCardPrefab == null))
            {
                ClearAll();
                return false;
            }

            int previousDemonCount = _spawnedDemonCards.Count;

            HashSet<int> newCardIds = SynchronizeCards(cards);
            HashSet<int> newDemonCardIds = SynchronizeDemonCards(demonCards);
            bool animateLayout = Application.isPlaying &&
                _hasRenderedLayout &&
                (newCardIds.Count > 0 ||
                 demonCards.Count != previousDemonCount ||
                 newDemonCardIds.Count > 0);
            int totalCardCount = cards.Count + demonCards.Count;
            float offset = -(totalCardCount - 1) * 0.5f * spacing;
            bool anyRevealAnimated = false;
            for (int i = 0; i < cards.Count; i++)
            {
                CardView card = _spawned[i];
                Vector3 targetPosition = new Vector3(
                    offset + i * spacing,
                    0f,
                    i * depthStagger);
                card.transform.localRotation = Quaternion.identity;
                card.SetSortingOrder(sortingOrderBase + i * sortingOrderStep);
                anyRevealAnimated |= card.WillAnimateRevealFor(cards[i]);
                card.Bind(cards[i], showTransientEffectSources);
                MoveCardToLayoutPosition(
                    card,
                    i,
                    targetPosition,
                    animateLayout,
                    newCardIds.Contains(cards[i].CardId));
            }

            for (int i = 0; i < _spawnedDemonCards.Count; i++)
            {
                int combinedIndex = cards.Count + i;
                DemonCardView card = _spawnedDemonCards[i];
                GameSceneDemonCardViewModel model =
                    demonCards[demonCards.Count - 1 - i];
                Vector3 targetPosition = new Vector3(
                    offset + combinedIndex * spacing,
                    0f,
                    combinedIndex * depthStagger);
                card.SetSortingOrder(
                    sortingOrderBase + combinedIndex * sortingOrderStep);
                card.Bind(model, showTransientEffectSources);
                MoveDemonCardToLayoutPosition(
                    card,
                    i,
                    targetPosition,
                    animateLayout,
                    newDemonCardIds.Contains(model.CardId));
            }

            _hasRenderedLayout = true;
            return anyRevealAnimated;
        }

        public void ResetView()
        {
            ClearAll();
            _hasRenderedLayout = false;
        }

        public bool TryGetCardWorldPosition(int cardId, out Vector3 position)
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                CardView card = _spawned[i];
                if (card != null && card.CardId == cardId)
                {
                    position = card.transform.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// Returns the world position where the card is laid out, even while its move tween is
        /// still running. Presentation effects that lock on to a card should use this instead of
        /// the transient transform position.
        /// </summary>
        public bool TryGetCardLayoutWorldPosition(int cardId, out Vector3 position)
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                CardView card = _spawned[i];
                if (card != null && card.CardId == cardId)
                {
                    position = transform.TransformPoint(GetCardLayoutPosition(i));
                    return true;
                }
            }

            position = default;
            return false;
        }

        internal bool TryGetDemonCardWorldPosition(
            int cardId,
            out Vector3 position)
        {
            for (int i = 0; i < _spawnedDemonCards.Count; i++)
            {
                DemonCardView card = _spawnedDemonCards[i];
                if (card != null && card.CardId == cardId)
                {
                    position = card.transform.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public bool TryGetRandomCardWorldPosition(out Vector3 position)
        {
            int aliveCount = 0;
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    aliveCount++;
                }
            }

            if (aliveCount == 0)
            {
                position = default;
                return false;
            }

            int selected = UnityEngine.Random.Range(0, aliveCount);
            for (int i = 0; i < _spawned.Count; i++)
            {
                CardView card = _spawned[i];
                if (card == null)
                {
                    continue;
                }

                if (selected == 0)
                {
                    position = card.transform.position;
                    return true;
                }

                selected--;
            }

            position = default;
            return false;
        }

        private void ClearAll()
        {
            KillAllMoveTweens();

            foreach (CardView card in _spawned)
            {
                if (card != null)
                {
                    DestroyCardObject(card.gameObject);
                }
            }

            _spawned.Clear();
            _moveTweens.Clear();

            foreach (DemonCardView card in _spawnedDemonCards)
            {
                if (card != null)
                {
                    DestroyCardObject(card.gameObject);
                }
            }

            _spawnedDemonCards.Clear();
            _demonMoveTweens.Clear();
            _hasRenderedLayout = false;
        }

        private HashSet<int> SynchronizeCards(
            IReadOnlyList<GameSceneCardViewModel> cards)
        {
            var previousCards = new List<CardView>(_spawned);
            var previousTweens = new List<Tween>(_moveTweens);
            var newCardIds = new HashSet<int>();
            _spawned.Clear();
            _moveTweens.Clear();

            for (int modelIndex = 0; modelIndex < cards.Count; modelIndex++)
            {
                GameSceneCardViewModel model = cards[modelIndex];
                int previousIndex = FindCardIndex(previousCards, model.CardId);
                if (previousIndex >= 0)
                {
                    _spawned.Add(previousCards[previousIndex]);
                    _moveTweens.Add(previousTweens[previousIndex]);
                    previousCards[previousIndex] = null;
                    previousTweens[previousIndex] = null;
                    continue;
                }

                CardView card = Instantiate(cardPrefab, transform);
                card.EnableHandHoverVisualOnly();
                _spawned.Add(card);
                _moveTweens.Add(null);
                newCardIds.Add(model.CardId);
            }

            for (int i = 0; i < previousCards.Count; i++)
            {
                if (previousCards[i] == null)
                {
                    continue;
                }

                previousTweens[i]?.Kill();
                AnimateDiscard(previousCards[i]);
            }

            return newCardIds;
        }

        private HashSet<int> SynchronizeDemonCards(
            IReadOnlyList<GameSceneDemonCardViewModel> demonCards)
        {
            var previousCards = new List<DemonCardView>(_spawnedDemonCards);
            var previousTweens = new List<Tween>(_demonMoveTweens);
            var newCardIds = new HashSet<int>();
            _spawnedDemonCards.Clear();
            _demonMoveTweens.Clear();

            for (int modelIndex = demonCards.Count - 1;
                modelIndex >= 0;
                modelIndex--)
            {
                GameSceneDemonCardViewModel model = demonCards[modelIndex];
                int previousIndex = FindDemonCardIndex(previousCards, model.CardId);
                if (previousIndex >= 0)
                {
                    _spawnedDemonCards.Add(previousCards[previousIndex]);
                    _demonMoveTweens.Add(previousTweens[previousIndex]);
                    previousCards[previousIndex] = null;
                    previousTweens[previousIndex] = null;
                    continue;
                }

                DemonCardView card = Instantiate(demonCardPrefab, transform);
                card.EnableHandHoverVisualOnly();
                _spawnedDemonCards.Add(card);
                _demonMoveTweens.Add(null);
                newCardIds.Add(model.CardId);
            }

            for (int i = 0; i < previousCards.Count; i++)
            {
                if (previousCards[i] == null)
                {
                    continue;
                }

                previousTweens[i]?.Kill();
                AnimateDemonDiscard(previousCards[i]);
            }

            return newCardIds;
        }

        private static int FindCardIndex(
            IReadOnlyList<CardView> cards,
            int cardId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].CardId == cardId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindDemonCardIndex(
            IReadOnlyList<DemonCardView> cards,
            int cardId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].CardId == cardId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void MoveCardToLayoutPosition(
            CardView card,
            int index,
            Vector3 targetPosition,
            bool animate,
            bool isNewCard)
        {
            if (card == null)
            {
                return;
            }

            KillMoveTween(index);

            if (!animate)
            {
                card.transform.localPosition = targetPosition;
                return;
            }

            if (isNewCard)
            {
                card.transform.localPosition = GetBodyEntryPosition(targetPosition);
            }

            _moveTweens[index] = card.transform
                .DOLocalMove(targetPosition, isNewCard ? enterDuration : moveDuration)
                .SetEase(isNewCard ? enterCurve : moveCurve)
                .SetTarget(card);
        }

        private Vector3 GetCardLayoutPosition(int index)
        {
            int totalCardCount = _spawned.Count + _spawnedDemonCards.Count;
            float offset = -(totalCardCount - 1) * 0.5f * spacing;
            return new Vector3(
                offset + index * spacing,
                0f,
                index * depthStagger);
        }

        private Vector3 GetBodyEntryPosition(Vector3 targetPosition)
        {
            float direction = ResolveBodyEntryDirection();
            targetPosition.y += bodyEntryDistance * direction;
            return targetPosition;
        }

        private void MoveDemonCardToLayoutPosition(
            DemonCardView card,
            int index,
            Vector3 targetPosition,
            bool animate,
            bool isNewCard)
        {
            if (card == null)
            {
                return;
            }

            KillDemonMoveTween(index);

            if (!animate)
            {
                card.transform.localPosition = targetPosition;
                return;
            }

            if (isNewCard)
            {
                card.transform.localPosition = GetBodyEntryPosition(targetPosition);
            }

            _demonMoveTweens[index] = card.transform
                .DOLocalMove(targetPosition, isNewCard ? enterDuration : moveDuration)
                .SetEase(isNewCard ? enterCurve : moveCurve)
                .SetTarget(card);
        }

        private void AnimateDiscard(CardView card)
        {
            if (!Application.isPlaying || discardDuration <= 0f)
            {
                DestroyCardObject(card.gameObject);
                return;
            }

            Vector3 targetPosition = card.transform.localPosition;
            targetPosition.y += discardExitDistance * ResolveBodyEntryDirection();
            Sequence sequence = DOTween.Sequence()
                .SetTarget(card)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Join(
                card.transform
                    .DOLocalMove(targetPosition, discardDuration)
                    .SetEase(discardCurve));
            sequence.Join(
                card.transform.DOScale(Vector3.zero, discardDuration));
            sequence.OnComplete(() =>
            {
                if (card != null)
                {
                    DestroyCardObject(card.gameObject);
                }
            });
        }

        private void AnimateDemonDiscard(DemonCardView card)
        {
            if (!Application.isPlaying || discardDuration <= 0f)
            {
                DestroyCardObject(card.gameObject);
                return;
            }

            Vector3 targetPosition = card.transform.localPosition;
            targetPosition.y += discardExitDistance * ResolveBodyEntryDirection();
            Sequence sequence = DOTween.Sequence()
                .SetTarget(card)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Join(
                card.transform
                    .DOLocalMove(targetPosition, discardDuration)
                    .SetEase(discardCurve));
            sequence.Join(card.transform.DOScale(Vector3.zero, discardDuration));
            sequence.OnComplete(() =>
            {
                if (card != null)
                {
                    DestroyCardObject(card.gameObject);
                }
            });
        }

        private void RemoveCardAt(int index, bool animateDiscard)
        {
            KillMoveTween(index);
            CardView card = _spawned[index];
            _spawned.RemoveAt(index);
            _moveTweens.RemoveAt(index);
            if (card == null)
            {
                return;
            }

            if (animateDiscard)
            {
                AnimateDiscard(card);
            }
            else
            {
                DestroyCardObject(card.gameObject);
            }
        }

        private static void DestroyCardObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private float ResolveBodyEntryDirection()
        {
            float direction;
            if (name.Contains("Enemy"))
            {
                direction = 1f;
            }
            else if (name.Contains("Player"))
            {
                direction = -1f;
            }
            else
            {
                direction = transform.position.z < 16f ? 1f : -1f;
            }

            return invertBodyEntryDirection ? -direction : direction;
        }

        private void KillMoveTween(int index)
        {
            if (index < 0 || index >= _moveTweens.Count)
            {
                return;
            }

            if (_moveTweens[index] != null)
            {
                _moveTweens[index].Kill();
                _moveTweens[index] = null;
            }
        }

        private void KillAllMoveTweens()
        {
            for (int i = 0; i < _moveTweens.Count; i++)
            {
                KillMoveTween(i);
            }

            for (int i = 0; i < _demonMoveTweens.Count; i++)
            {
                KillDemonMoveTween(i);
            }
        }

        private void KillDemonMoveTween(int index)
        {
            if (index < 0 || index >= _demonMoveTweens.Count)
            {
                return;
            }

            if (_demonMoveTweens[index] != null)
            {
                _demonMoveTweens[index].Kill();
                _demonMoveTweens[index] = null;
            }
        }
    }
}
