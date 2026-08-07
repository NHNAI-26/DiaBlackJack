using System;
using DiaBlackJack.CoreLoop;
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

        private DemonCardView _card;
        private bool _cardBound;

        public event Action LineAdvanceRequested;

        public bool HasCardPrefab => cardPrefab != null;

        public bool IsActive { get; private set; }

        public bool IsLineComplete => speechText == null || speechText.IsComplete;

        private void Awake()
        {
            EnsureCard();
            Hide();
        }

        public void Show()
        {
            EnsureCard();
            if (!_cardBound && _card != null)
            {
                BindNarratorCard();
            }

            gameObject.SetActive(true);
            IsActive = true;
        }

        public void Hide()
        {
            IsActive = false;
            speechText?.Hide();
            gameObject.SetActive(false);
        }

        public void ShowLine(string text)
        {
            Show();
            speechText?.Play(text, charactersPerSecond);
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
        }

        private void BindNarratorCard()
        {
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(narratorDefinitionKey);
            _card.Bind(new GameSceneDemonCardViewModel(
                NarratorCardId,
                narratorDefinitionKey,
                isFaceUp: true,
                canUse: false,
                displayName: definition.DisplayName,
                summary: definition.Summary,
                costSummary: definition.CostSummary));
            _cardBound = true;
        }
    }
}
