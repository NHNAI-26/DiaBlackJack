using System.Collections.Generic;
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
        [SerializeField] private float surfaceLift = 0.06f;
        [SerializeField] private float depthStagger = 0.01f;

        private readonly List<CardView> _spawned = new List<CardView>();

        internal CardView CardPrefab => cardPrefab;

        public void Render(IReadOnlyList<GameSceneCardViewModel> cards)
        {
            if (cardPrefab == null || cards == null)
            {
                ClearAll();
                return;
            }

            while (_spawned.Count < cards.Count)
            {
                _spawned.Add(Instantiate(cardPrefab, transform));
            }

            while (_spawned.Count > cards.Count)
            {
                int last = _spawned.Count - 1;
                if (_spawned[last] != null)
                {
                    Destroy(_spawned[last].gameObject);
                }

                _spawned.RemoveAt(last);
            }

            float offset = -(cards.Count - 1) * 0.5f * spacing;
            for (int i = 0; i < cards.Count; i++)
            {
                CardView card = _spawned[i];
                card.transform.localPosition = new Vector3(
                    offset + i * spacing,
                    0f,
                    surfaceLift + i * depthStagger);
                card.transform.localRotation = Quaternion.identity;
                card.Bind(cards[i]);
            }
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
            foreach (CardView card in _spawned)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _spawned.Clear();
        }
    }
}
