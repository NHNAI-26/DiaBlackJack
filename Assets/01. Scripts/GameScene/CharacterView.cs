using System;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on the enemy character prefab root. <see cref="Render"/> keeps the action label and
    /// profile-specific attack sprites in sync with the presenter. General battle states never change
    /// the authored sprite color or transform scale.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterView : MonoBehaviour
    {
        private const int ActionLabelSortingOffset = 10;

        [Serializable]
        private sealed class EnemySpriteProfile
        {
            [SerializeField] private string profileKey;
            [SerializeField] private Sprite defaultState;
            [SerializeField] private Sprite attackThreatened;
            [SerializeField] private Sprite attacked;

            public string ProfileKey => profileKey;

            public Sprite Resolve(CharacterVisualState state)
            {
                switch (state)
                {
                    case CharacterVisualState.Attacked:
                        return attacked != null ? attacked : defaultState;
                    case CharacterVisualState.AttackThreatened:
                        return attackThreatened != null
                            ? attackThreatened
                            : defaultState;
                    default:
                        return defaultState;
                }
            }

        }

        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private SpeechBubbleView speechBubble;

        [Header("Enemy profile sprites")]
        [SerializeField] private EnemySpriteProfile[] enemySpriteProfiles =
            Array.Empty<EnemySpriteProfile>();

        [Header("Merchant (shop mode)")]
        [Tooltip("Optional. When assigned, the enemy swaps to this sprite in the shop; otherwise the dark tint + shrink alone reads as the merchant.")]
        [SerializeField] private Sprite merchantSprite;
        [SerializeField] private Color merchantTint = new Color(0.32f, 0.30f, 0.36f);
        [SerializeField] private float merchantScale = 0.8f;

        private Vector3 _baseScale;
        private Color _baseColor;
        private Sprite _defaultSprite;
        private EnemySpriteProfile _activeEnemySpriteProfile;
        private CharacterVisualState _lastVisualState;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>Apply the action label and profile-specific sprite for the given state.</summary>
        public void Render(CharacterVisualState state, string label)
        {
            RenderVisual(state);

            if (actionLabel != null)
            {
                KeepActionLabelInFront();
                bool hasLabel = !string.IsNullOrEmpty(label);
                actionLabel.enabled = hasLabel;
                if (hasLabel)
                {
                    actionLabel.text = label;
                }
            }
        }

        /// <summary>Apply only the profile-specific sprite state.</summary>
        public void RenderVisual(CharacterVisualState state)
        {
            EnsureInitialized();
            _lastVisualState = state;

            if (sprite != null)
            {
                ApplyEnemySprite(state);
            }
        }

        internal void ShowSpeech(string message)
        {
            EnsureInitialized();
            speechBubble?.Show(message);
        }

        public void HideSpeech()
        {
            EnsureInitialized();
            speechBubble?.Hide();
        }

        /// <summary>
        /// Selects the authored sprite set for an enemy combat profile. Invalid or unwired keys are
        /// rejected without changing the currently displayed profile.
        /// </summary>
        public bool TrySetEnemyProfile(string profileKey)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(profileKey) || enemySpriteProfiles == null)
            {
                return false;
            }

            for (int i = 0; i < enemySpriteProfiles.Length; i++)
            {
                EnemySpriteProfile candidate = enemySpriteProfiles[i];
                if (candidate == null ||
                    !string.Equals(candidate.ProfileKey, profileKey, StringComparison.Ordinal))
                {
                    continue;
                }

                Sprite resolved = candidate.Resolve(_lastVisualState);
                if (resolved == null)
                {
                    return false;
                }

                _activeEnemySpriteProfile = candidate;
                if (sprite != null)
                {
                    sprite.sprite = resolved;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Shop mode: turn this character into the small dark merchant that stands where the enemy was.
        /// Swaps to <see cref="merchantSprite"/> when one is assigned; otherwise the dark tint + shrink
        /// alone reads as the merchant, so this works before merchant art exists.
        /// </summary>
        public void EnterMerchant()
        {
            EnsureInitialized();

            if (sprite != null)
            {
                if (merchantSprite != null)
                {
                    sprite.sprite = merchantSprite;
                }

                sprite.color = merchantTint;
            }

            transform.localScale = _baseScale * merchantScale;

            if (actionLabel != null)
            {
                actionLabel.enabled = false;
            }
        }

        /// <summary>Leave shop mode and restore the authored combat appearance immediately.</summary>
        public void ExitMerchant()
        {
            EnsureInitialized();

            if (sprite != null)
            {
                ApplyEnemySprite(_lastVisualState);
                sprite.color = _baseColor;
            }

            transform.localScale = _baseScale;
        }

        private void ApplyEnemySprite(CharacterVisualState state)
        {
            if (sprite == null)
            {
                return;
            }

            Sprite resolved = _activeEnemySpriteProfile?.Resolve(state);
            sprite.sprite = resolved != null ? resolved : _defaultSprite;
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
                Transform label = transform.Find("ActionLabel");
                actionLabel = label == null
                    ? null
                    : label.GetComponent<TMP_Text>();
            }

            if (speechBubble == null)
            {
                speechBubble = GetComponentInChildren<SpeechBubbleView>(true);
            }

            _baseScale = transform.localScale;
            _baseColor = sprite != null ? sprite.color : Color.white;
            _defaultSprite = sprite != null ? sprite.sprite : null;
            _initialized = true;
        }

        private void KeepActionLabelInFront()
        {
            if (sprite == null || actionLabel == null)
            {
                return;
            }

            Renderer labelRenderer = actionLabel.GetComponent<Renderer>();
            if (labelRenderer == null)
            {
                return;
            }

            labelRenderer.sortingLayerID = sprite.sortingLayerID;
            labelRenderer.sortingOrder =
                sprite.sortingOrder + ActionLabelSortingOffset;
        }
    }
}
