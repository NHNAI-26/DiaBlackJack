using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on a player/enemy character prefab root. MVP stand-in for the eventual per-action
    /// animations: <see cref="Render"/> maps a <see cref="CharacterVisualState"/> onto a small tint +
    /// scale change on the sprite, so a hit/stand/bust/win/loss is visible at a glance. The designer
    /// authors the sprite and base scale in the prefab; this only nudges color and scale. Wired from
    /// <c>GameManager</c> (serialized, null-guarded) — the scene reference is assigned later.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private TMP_Text actionLabel;

        [Header("State tints")]
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color activeColor = new Color(1f, 0.95f, 0.65f);
        [SerializeField] private Color standColor = new Color(0.70f, 0.80f, 1f);
        [SerializeField] private Color bustColor = new Color(1f, 0.42f, 0.38f);
        [SerializeField] private Color winColor = new Color(0.55f, 1f, 0.60f);
        [SerializeField] private Color loseColor = new Color(0.50f, 0.50f, 0.55f);
        [SerializeField] private Color useCardColor = new Color(0.65f, 0.90f, 1f);

        [Header("State scale multipliers (x the prefab's authored scale)")]
        [SerializeField] private float idleScale = 1f;
        [SerializeField] private float activeScale = 1.06f;
        [SerializeField] private float standScale = 0.97f;
        [SerializeField] private float bustScale = 0.85f;
        [SerializeField] private float winScale = 1.15f;
        [SerializeField] private float loseScale = 0.90f;
        [SerializeField] private float useCardScale = 1.08f;

        private Vector3 _baseScale;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>Apply the coarse visual for the given state. Instant (no tween) for the MVP.</summary>
        public void Render(CharacterVisualState state, string label)
        {
            EnsureInitialized();

            if (sprite != null)
            {
                sprite.color = ColorFor(state);
            }

            transform.localScale = _baseScale * ScaleFor(state);

            if (actionLabel != null)
            {
                bool hasLabel = !string.IsNullOrEmpty(label);
                actionLabel.enabled = hasLabel;
                if (hasLabel)
                {
                    actionLabel.text = label;
                }
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (sprite == null)
            {
                sprite = GetComponent<SpriteRenderer>();
            }

            if (actionLabel == null)
            {
                actionLabel = GetComponentInChildren<TMP_Text>(true);
            }

            _baseScale = transform.localScale;
            _initialized = true;
        }

        private Color ColorFor(CharacterVisualState state)
        {
            switch (state)
            {
                case CharacterVisualState.Active:
                    return activeColor;
                case CharacterVisualState.Stand:
                    return standColor;
                case CharacterVisualState.Bust:
                    return bustColor;
                case CharacterVisualState.Win:
                    return winColor;
                case CharacterVisualState.Lose:
                    return loseColor;
                case CharacterVisualState.UseCard:
                    return useCardColor;
                default:
                    return idleColor;
            }
        }

        private float ScaleFor(CharacterVisualState state)
        {
            switch (state)
            {
                case CharacterVisualState.Active:
                    return activeScale;
                case CharacterVisualState.Stand:
                    return standScale;
                case CharacterVisualState.Bust:
                    return bustScale;
                case CharacterVisualState.Win:
                    return winScale;
                case CharacterVisualState.Lose:
                    return loseScale;
                case CharacterVisualState.UseCard:
                    return useCardScale;
                default:
                    return idleScale;
            }
        }
    }
}
