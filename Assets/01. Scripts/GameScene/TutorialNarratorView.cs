using System;
using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Tutorial-only narrator: a fixed, always-face-up Asmodeus card anchored to this
    /// transform's position (left of the contract paper stack in the scene) plus a
    /// typewriter speech line. Advancing is click-driven: while <see cref="IsActive"/>,
    /// <see cref="GameManager"/> routes every click here instead of the normal battle raycast
    /// chain. A click while the line is still typing completes it instantly; a click once the
    /// line is complete raises <see cref="LineAdvanceRequested"/> so a director (Layer D) can
    /// push the next line or close the narrator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialNarratorView : MonoBehaviour
    {
        // Never resolved through PlayerRunState (Bind only reads it back for CanUse-gated
        // click handling, and CanUse is always false here), so any non-negative sentinel is
        // safe — it will never collide with a real run card id in a way that matters.
        private const int NarratorCardId = int.MaxValue;

        [SerializeField] private DemonCardView cardPrefab;
        [SerializeField] private TutorialTypewriterTextView speechText;
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 40f;
        [SerializeField] private string narratorDefinitionKey =
            DemonContractCatalog.AsmodeusKey;
        [SerializeField] private Vector2 narratorSpeechScreenOffset =
            new Vector2(-56f, 72f);
        [SerializeField] private Vector2 contractedSpeechScreenOffset =
            new Vector2(56f, 72f);
        [SerializeField, Min(0f)] private float speechScreenMargin = 24f;

        private DemonCardView _card;
        private DemonCardView _externalSpeaker;
        private bool _cardBound;
        private bool _showRequested;

        public event Action LineAdvanceRequested;

        public bool HasCardPrefab => cardPrefab != null;

        public bool IsActive { get; private set; }

        public bool IsLineComplete => speechText == null || speechText.IsComplete;

        internal DemonCardView NarratorCard => _card;

        internal bool OwnsNarratorCard(DemonCardView card) =>
            card != null && card == _card;

        private void LateUpdate()
        {
            UpdateSpeechPosition();
        }

        private void Awake()
        {
            EnsureCard();
            IsActive = false;
            speechText?.Hide();
            // Awake runs for every GameScene load, tutorial or not — only Show() (the
            // tutorial actually starting) should make this object, and the card on it,
            // visible for the first time.
            if (!_showRequested)
            {
                gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            ShowCardOnly();
            IsActive = true;
        }

        internal void ShowCardOnly()
        {
            _showRequested = true;
            EnsureCard();
            if (!_cardBound && _card != null)
            {
                BindNarratorCard();
            }

            gameObject.SetActive(true);
            IsActive = false;
            speechText?.Hide();
        }

        public void Hide()
        {
            IsActive = false;
            speechText?.Hide();
            // Deliberately does NOT deactivate the root — once Show() has made the
            // Asmodeus card visible, it stays out on the table as a real object for the
            // rest of the tutorial; only the speech bubble text hides between lines/gates.
        }

        internal void ResetView()
        {
            _externalSpeaker = null;
            if (speechText != null)
            {
                speechText.SetBubbleMirrored(false);
            }

            _showRequested = false;
            IsActive = false;
            if (speechText != null)
            {
                speechText.Hide();
            }

            gameObject.SetActive(false);
        }

        public void ShowLine(string text)
        {
            Show();
            speechText?.Play(text, charactersPerSecond);
            UpdateSpeechPosition();
        }

        internal void UseNarratorCard()
        {
            _externalSpeaker = null;
            speechText?.SetBubbleMirrored(false);
            EnsureCard();
            if (_card != null)
            {
                _card.gameObject.SetActive(true);
            }
        }

        internal void UseExternalSpeaker(DemonCardView speaker)
        {
            _externalSpeaker = speaker;
            speechText?.SetBubbleMirrored(true);
            if (_card != null)
            {
                _card.gameObject.SetActive(false);
            }

            UpdateSpeechPosition();
        }

        public void HandleClick()
        {
            if (!IsActive || speechText == null)
            {
                return;
            }

            if (!speechText.IsComplete)
            {
                speechText.CompleteImmediately();
                return;
            }

            LineAdvanceRequested?.Invoke();
        }

        private void EnsureCard()
        {
            if (_card != null || cardPrefab == null)
            {
                return;
            }

            _card = Instantiate(cardPrefab, transform);
            _card.name = "NarratorCard";
            _card.EnableHoverVisualOnly();
            _card.SetUnlitPresentation();
            TMP_Text[] labels = _card.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextUIOverlayLayerUtility.ApplyRecursively(
                    labels[i].gameObject);
            }
        }

        private void UpdateSpeechPosition()
        {
            if (speechText == null || !speechText.IsVisible)
            {
                return;
            }

            DemonCardView speaker = _externalSpeaker != null
                ? _externalSpeaker
                : _card;
            if (speaker == null || !speaker.gameObject.activeInHierarchy)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null ||
                !TryGetSpeakerTopCenter(speaker, out Vector3 worldTop))
            {
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(worldTop);
            if (screen.z <= 0f)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            Vector2 screenOffset = ResolveSpeechScreenOffset(
                _externalSpeaker != null,
                narratorSpeechScreenOffset,
                contractedSpeechScreenOffset);
            screen.x = Mathf.Clamp(
                screen.x + screenOffset.x,
                safe.xMin + speechScreenMargin,
                safe.xMax - speechScreenMargin);
            screen.y = Mathf.Clamp(
                screen.y + screenOffset.y,
                safe.yMin + speechScreenMargin,
                safe.yMax - speechScreenMargin);
            speechText.transform.position = camera.ScreenToWorldPoint(screen);
        }

        internal static Vector2 ResolveSpeechScreenOffset(
            bool usesContractedSpeaker,
            Vector2 narratorOffset,
            Vector2 contractedOffset)
        {
            return usesContractedSpeaker ? contractedOffset : narratorOffset;
        }

        private static bool TryGetSpeakerTopCenter(
            DemonCardView speaker,
            out Vector3 worldTop)
        {
            SpriteRenderer[] renderers =
                speaker.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
            {
                worldTop = speaker.transform.position;
                return true;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            worldTop = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            return true;
        }

        private void BindNarratorCard()
        {
            _card.Bind(new GameSceneDemonCardViewModel(
                NarratorCardId,
                narratorDefinitionKey,
                isFaceUp: true,
                canUse: false,
                displayName: "아스모데우스",
                summary: "최고로 유능하고, 최고로 아름다운 악마지.",
                costSummary: "나랑 대화할 수 있는 걸 가문의 영광으로 알라고.",
                showHoverBadgeWhenUnavailable: true));
            _cardBound = true;
        }
    }
}
