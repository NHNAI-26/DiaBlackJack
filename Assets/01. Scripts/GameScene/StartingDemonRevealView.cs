using System;
using System.Collections;
using System.Collections.Generic;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class StartingDemonRevealView : MonoBehaviour
    {
        [SerializeField] private GameObject demonCardPrefab;

        [Header("World-space layout")]
        [SerializeField] private Vector3 deckPosition =
            new Vector3(1.5f, 3.58f, 16.44f);
        [SerializeField] private Vector3 firstCardPosition =
            new Vector3(-0.62f, 3.6f, 16.3f);
        [SerializeField] private Vector3 secondCardPosition =
            new Vector3(0.62f, 3.6f, 16.3f);
        [SerializeField] private Vector3 cardEulerAngles =
            new Vector3(270f, 0f, 0f);
        [SerializeField] private int deckVisualCardCount = 3;

        [Header("Pacing")]
        [SerializeField] private float dealDuration = 0.55f;
        [SerializeField] private float faceDownHoldDuration = 0.35f;
        [SerializeField] private float flipDuration = 0.3f;
        [SerializeField] private float revealedHoldDuration = 1.2f;

        private readonly List<GameObject> _instances = new List<GameObject>();
        private Coroutine _revealRoutine;
        private int? _activeGrantId;

        public event Action RevealCompleted;

        public bool IsVisible { get; private set; }

        public void Render(
            int grantId,
            IReadOnlyList<StartingDemonGrantCardViewModel> cards)
        {
            if (_activeGrantId == grantId && IsVisible)
            {
                return;
            }

            Hide();
            if (demonCardPrefab == null || cards == null || cards.Count != 2)
            {
                return;
            }

            _activeGrantId = grantId;
            IsVisible = true;
            _revealRoutine = StartCoroutine(Reveal(cards));
        }

        public void Hide()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i] != null)
                {
                    Destroy(_instances[i]);
                }
            }

            _instances.Clear();
            _activeGrantId = null;
            IsVisible = false;
        }

        private void OnDisable()
        {
            Hide();
        }

        private IEnumerator Reveal(
            IReadOnlyList<StartingDemonGrantCardViewModel> cards)
        {
            Quaternion rotation = Quaternion.Euler(cardEulerAngles);
            for (int i = 0; i < deckVisualCardCount; i++)
            {
                GameObject deckCard = CreateCard(
                    cards[0],
                    10000 + i,
                    false,
                    deckPosition + new Vector3(0f, i * 0.012f, 0f),
                    rotation);
                _instances.Add(deckCard);
            }

            GameObject first = CreateCard(
                cards[0],
                11000,
                false,
                deckPosition,
                rotation);
            GameObject second = CreateCard(
                cards[1],
                11001,
                false,
                deckPosition + new Vector3(0f, 0.015f, 0f),
                rotation);
            _instances.Add(first);
            _instances.Add(second);

            yield return MoveCards(
                first,
                second,
                firstCardPosition,
                secondCardPosition);
            yield return WaitUnscaled(faceDownHoldDuration);
            yield return FlipCards(first, second, cards);
            yield return WaitUnscaled(revealedHoldDuration);

            _revealRoutine = null;
            RevealCompleted?.Invoke();
        }

        private GameObject CreateCard(
            StartingDemonGrantCardViewModel card,
            int cardId,
            bool isFaceUp,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject instance = Instantiate(
                demonCardPrefab,
                position,
                rotation,
                transform);
            DemonCardView view = instance.GetComponent<DemonCardView>();
            if (view == null)
            {
                throw new MissingComponentException(
                    $"{nameof(DemonCardView)} is required on the demon card prefab.");
            }

            view.Bind(CreateCardViewModel(card, cardId, isFaceUp));
            return instance;
        }

        private IEnumerator MoveCards(
            GameObject first,
            GameObject second,
            Vector3 firstTarget,
            Vector3 secondTarget)
        {
            Vector3 firstStart = first.transform.position;
            Vector3 secondStart = second.transform.position;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, dealDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                first.transform.position =
                    Vector3.LerpUnclamped(firstStart, firstTarget, progress);
                second.transform.position =
                    Vector3.LerpUnclamped(secondStart, secondTarget, progress);
                yield return null;
            }

            first.transform.position = firstTarget;
            second.transform.position = secondTarget;
        }

        private IEnumerator FlipCards(
            GameObject first,
            GameObject second,
            IReadOnlyList<StartingDemonGrantCardViewModel> cards)
        {
            Vector3 firstScale = first.transform.localScale;
            Vector3 secondScale = second.transform.localScale;
            float halfDuration = Mathf.Max(0.01f, flipDuration * 0.5f);

            yield return ScaleCardWidths(
                first,
                second,
                firstScale,
                secondScale,
                1f,
                0f,
                halfDuration);

            first.GetComponent<DemonCardView>().Bind(
                CreateCardViewModel(cards[0], 11000, isFaceUp: true));
            second.GetComponent<DemonCardView>().Bind(
                CreateCardViewModel(cards[1], 11001, isFaceUp: true));
            first.transform.localScale = new Vector3(
                0f,
                firstScale.y,
                firstScale.z);
            second.transform.localScale = new Vector3(
                0f,
                secondScale.y,
                secondScale.z);

            yield return ScaleCardWidths(
                first,
                second,
                firstScale,
                secondScale,
                0f,
                1f,
                halfDuration);
        }

        private static IEnumerator ScaleCardWidths(
            GameObject first,
            GameObject second,
            Vector3 firstScale,
            Vector3 secondScale,
            float from,
            float to,
            float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float width = Mathf.Lerp(from, to, elapsed / duration);
                first.transform.localScale = new Vector3(
                    firstScale.x * width,
                    firstScale.y,
                    firstScale.z);
                second.transform.localScale = new Vector3(
                    secondScale.x * width,
                    secondScale.y,
                    secondScale.z);
                yield return null;
            }

            first.transform.localScale = new Vector3(
                firstScale.x * to,
                firstScale.y,
                firstScale.z);
            second.transform.localScale = new Vector3(
                secondScale.x * to,
                secondScale.y,
                secondScale.z);
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float remaining = Mathf.Max(0f, seconds);
            while (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static GameSceneDemonCardViewModel CreateCardViewModel(
            StartingDemonGrantCardViewModel card,
            int cardId,
            bool isFaceUp)
        {
            return new GameSceneDemonCardViewModel(
                cardId,
                card.DefinitionKey,
                isFaceUp,
                canUse: false,
                card.DisplayName,
                card.Summary,
                card.CostSummary);
        }
    }
}
