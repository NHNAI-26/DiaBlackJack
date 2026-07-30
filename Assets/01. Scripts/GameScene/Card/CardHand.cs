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
        private bool _hasRenderedLayout;

        internal CardView CardPrefab => cardPrefab;

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

        public void Render(IReadOnlyList<GameSceneCardViewModel> cards)
        {
            if (cardPrefab == null || cards == null)
            {
                ClearAll();
                return;
            }

            int previousCount = _spawned.Count;
            bool animateIncrease = Application.isPlaying &&
                _hasRenderedLayout &&
                cards.Count > previousCount;

            var retainedCardIds = new HashSet<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                retainedCardIds.Add(cards[i].CardId);
            }

            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                CardView spawnedCard = _spawned[i];
                if (spawnedCard != null &&
                    !retainedCardIds.Contains(spawnedCard.CardId))
                {
                    RemoveCardAt(i, animateDiscard: true);
                }
            }

            while (_spawned.Count < cards.Count)
            {
                _spawned.Add(Instantiate(cardPrefab, transform));
                _moveTweens.Add(null);
            }

            while (_spawned.Count > cards.Count)
            {
                RemoveCardAt(_spawned.Count - 1, animateDiscard: true);
            }

            float offset = -(cards.Count - 1) * 0.5f * spacing;
            for (int i = 0; i < cards.Count; i++)
            {
                CardView card = _spawned[i];
                Vector3 targetPosition = new Vector3(
                    offset + i * spacing,
                    0f,
                    i * depthStagger);
                card.transform.localRotation = Quaternion.identity;
                card.SetSortingOrder(sortingOrderBase + i * sortingOrderStep);
                card.Bind(cards[i]);
                MoveCardToLayoutPosition(card, i, targetPosition, animateIncrease, i >= previousCount);
            }

            _hasRenderedLayout = true;
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

            int selected = Random.Range(0, aliveCount);
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
                    Destroy(card.gameObject);
                }
            }

            _spawned.Clear();
            _moveTweens.Clear();
            _hasRenderedLayout = false;
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

        private Vector3 GetBodyEntryPosition(Vector3 targetPosition)
        {
            float direction = ResolveBodyEntryDirection();
            targetPosition.y += bodyEntryDistance * direction;
            return targetPosition;
        }

        private void AnimateDiscard(CardView card)
        {
            if (!Application.isPlaying || discardDuration <= 0f)
            {
                Destroy(card.gameObject);
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
                    Destroy(card.gameObject);
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
                Destroy(card.gameObject);
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
        }
    }
}
