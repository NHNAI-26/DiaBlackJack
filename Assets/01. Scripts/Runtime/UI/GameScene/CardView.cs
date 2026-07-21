using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on the card prefab root. <see cref="front"/> / <see cref="back"/> toggle by the
    /// card's physical orientation; <see cref="rankText"/> shows the number whenever the viewer is
    /// allowed to see it. Hover feedback is driven by <see cref="SetHovered"/> (called from
    /// <c>GameManager</c>'s pointer raycast): any hovered card scales up, and a hovered *usable* card
    /// also glows and pops a <see cref="badge"/> above it naming the ability. Usability is
    /// orientation-independent — a face-down card can be usable — so the badge/glow are gated on
    /// <see cref="CanUse"/> only, and the glow tints whichever face (front or back) is showing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private TMP_Text rankText;

        [Header("Usable badge (hover)")]
        [SerializeField] private GameObject badge;
        [SerializeField] private TMP_Text badgeText;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float scaleLerp = 12f;
        [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.3f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _propertyBlock;
        private Renderer _frontRenderer;
        private Renderer _backRenderer;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private bool _isFaceUp = true;
        private bool _hovered;

        /// <summary>Run card id of the bound card, for pointer routing. -1 when unbound.</summary>
        public int CardId { get; private set; } = -1;

        /// <summary>Whether this card's manual effect can be activated right now (player, usable only).</summary>
        public bool CanUse { get; private set; }

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
            if (badge != null)
            {
                badge.SetActive(false);
            }
        }

        private void Update()
        {
            Vector3 current = transform.localScale;
            if ((current - _targetScale).sqrMagnitude > 0.0000001f)
            {
                transform.localScale = Vector3.Lerp(current, _targetScale, Time.deltaTime * scaleLerp);
            }
        }

        public void Bind(GameSceneCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            CardId = card.CardId;
            CanUse = card.CanUse;
            _isFaceUp = card.IsFaceUp;

            if (front != null)
            {
                front.SetActive(card.IsFaceUp);
            }

            if (back != null)
            {
                back.SetActive(!card.IsFaceUp);
            }

            if (rankText != null)
            {
                rankText.enabled = card.RevealRank;
                if (card.RevealRank)
                {
                    rankText.text = card.Rank.ToString();
                }
            }

            if (badgeText != null)
            {
                badgeText.text = string.IsNullOrEmpty(card.AbilityDescription)
                    ? $"{card.Rank} {card.DisplayName}"
                    : $"{card.Rank} {card.DisplayName}\n{card.AbilityDescription}";
            }

            // Pooled cards are reused; clear any prior hover state and snap to base size.
            _hovered = false;
            transform.localScale = _baseScale;
            _targetScale = _baseScale;
            ApplyHoverVisuals();
        }

        /// <summary>Called by the pointer raycast when this card gains/loses hover.</summary>
        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            _targetScale = hovered ? _baseScale * hoverScale : _baseScale;
            ApplyHoverVisuals();
        }

        private void ApplyHoverVisuals()
        {
            bool lit = _hovered && CanUse;

            if (badge != null)
            {
                badge.SetActive(lit);
            }

            // Clear both faces first so a stale tint never lingers on the hidden side, then glow the
            // face that is actually showing.
            ClearGlow(FrontRenderer());
            ClearGlow(BackRenderer());
            if (lit)
            {
                Renderer active = _isFaceUp ? FrontRenderer() : BackRenderer();
                if (active != null)
                {
                    _propertyBlock ??= new MaterialPropertyBlock();
                    active.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor(BaseColorId, glowColor);
                    active.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        private static void ClearGlow(Renderer renderer)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }

        private Renderer FrontRenderer()
        {
            if (_frontRenderer == null && front != null)
            {
                _frontRenderer = front.GetComponent<Renderer>();
            }

            return _frontRenderer;
        }

        private Renderer BackRenderer()
        {
            if (_backRenderer == null && back != null)
            {
                _backRenderer = back.GetComponent<Renderer>();
            }

            return _backRenderer;
        }
    }
}
