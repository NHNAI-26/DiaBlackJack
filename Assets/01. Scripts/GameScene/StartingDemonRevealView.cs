using System;
using System.Collections;
using System.Collections.Generic;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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
            new Vector3(-0.55f, 3.56f, 17f);
        [SerializeField] private Vector3 secondCardPosition =
            new Vector3(0.55f, 3.56f, 17f);
        [SerializeField] private Vector3 cardEulerAngles =
            new Vector3(270f, 0f, 0f);
        [SerializeField] private int deckVisualCardCount = 3;

        [Header("Pacing")]
        [SerializeField] private float dealDuration = 0.55f;
        [SerializeField] private float faceDownHoldDuration = 0.35f;
        [SerializeField] private float flipDuration = 0.3f;
        [SerializeField] private float revealedHoldDuration = 1.2f;

        private readonly List<GameObject> _instances = new List<GameObject>();
        private readonly List<DemonCardView> _revealedCards =
            new List<DemonCardView>(2);
        private GUIStyle _buttonStyle;
        private Coroutine _revealRoutine;
        private Camera _camera;
        private GameHudView _hud;
        private DemonCardView _hoveredCard;
        private int? _activeGrantId;
        private bool _canConfirm;

        public event Action ConfirmationRequested;

        public bool IsVisible { get; private set; }

        internal void BindHud(GameHudView hud)
        {
            _hud = hud;
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            UpdateHover(RaycastRevealedCard());
            if (_hoveredCard == null || !_hoveredCard.ShouldShowHoverBadge)
            {
                ResolveHud()?.HideDemonContractDetail();
                return;
            }

            ResolveHud()?.ShowDemonContractDetail(_hoveredCard.BoundCard);
        }

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
            _revealedCards.Clear();
            UpdateHover(null);
            ResolveHud()?.HideDemonContractDetail();
            _activeGrantId = null;
            _canConfirm = false;
            IsVisible = false;
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnGUI()
        {
            if (!IsVisible || !_canConfirm)
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Screen.height <= 720 ? 18 : 22,
                fontStyle = FontStyle.Bold
            };
            float width = Mathf.Min(420f, Screen.width * 0.4f);
            float height = Screen.height <= 720 ? 50f : 60f;
            Rect buttonRect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - height - 38f,
                width,
                height);
            if (GUI.Button(buttonRect, "CONFIRM DEMONS", _buttonStyle))
            {
                _canConfirm = false;
                ConfirmationRequested?.Invoke();
            }
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
            _canConfirm = true;
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

            TextUIOverlayLayerUtility.ApplyRecursively(instance);
            view.Bind(CreateCardViewModel(card, cardId, isFaceUp));
            view.SetUnlitPresentation();
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
            DemonCardView firstView = first.GetComponent<DemonCardView>();
            DemonCardView secondView = second.GetComponent<DemonCardView>();

            firstView.Bind(
                CreateCardViewModel(cards[0], 11000, isFaceUp: true));
            secondView.Bind(
                CreateCardViewModel(cards[1], 11001, isFaceUp: true));
            _revealedCards.Add(firstView);
            _revealedCards.Add(secondView);

            float duration = Mathf.Max(
                flipDuration,
                firstView.RevealFlipDuration,
                secondView.RevealFlipDuration);
            yield return WaitUnscaled(duration);
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
                card.CostSummary,
                showHoverBadgeWhenUnavailable: isFaceUp);
        }

        private DemonCardView RaycastRevealedCard()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Mouse mouse = Mouse.current;
            if (_camera == null || mouse == null)
            {
                return null;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                return null;
            }

            DemonCardView pointed = hit.collider.GetComponentInParent<DemonCardView>();
            return pointed != null && _revealedCards.Contains(pointed)
                ? pointed
                : null;
        }

        private void UpdateHover(DemonCardView pointed)
        {
            if (_hoveredCard == pointed)
            {
                return;
            }

            _hoveredCard?.SetHovered(false);
            _hoveredCard = pointed;
            _hoveredCard?.SetHovered(true);
        }

        private GameHudView ResolveHud()
        {
            if (_hud == null)
            {
                _hud = FindFirstObjectByType<GameHudView>();
            }

            return _hud;
        }
    }
}
