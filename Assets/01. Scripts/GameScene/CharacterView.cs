using System;
using Border.Audio;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
        private const string EnemyEntranceSfxId = "enemyIn";
        private const string EnemyExitSfxId01 = "enemyOut01";
        private const string EnemyExitSfxId02 = "enemyOut02";
        private const string EnemyDamageSfxId = "enemyDamage";
        private const string EmissionKeyword = "_EMISSION";
        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionEnabledId =
            Shader.PropertyToID("_EmissionEnabled");
        private static readonly int EmissionIntensityId =
            Shader.PropertyToID("_EmissionIntensity");

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
        [Tooltip("Transform rotated by the appearance animation. Assign the sprite's child pivot so the character root and speech bubble stay upright.")]
        [SerializeField] private Transform appearanceTarget;
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

        [Header("Enemy appearance animation")]
        [FormerlySerializedAs("appearanceDuration")]
        [SerializeField, Min(0f)] private float appearanceEnterDuration = 0.8f;
        [SerializeField, Min(0f)] private float appearanceExitDuration = 0.8f;
        [SerializeField] private Vector3 appearanceRotationOffset =
            new Vector3(0f, 0f, 180f);
        [SerializeField] private Ease appearanceEnterEase = Ease.OutBack;
        [SerializeField] private Ease appearanceExitEase = Ease.InBack;
        [SerializeField] private Ease merchantExitEase = Ease.InCubic;

        [Header("Enemy appearance audio")]
        [SerializeField, Min(0f)] private float entranceSfxDelay;
        [SerializeField, Min(0f)] private float exitSfxDelay;

        [Header("Damage reaction")]
        [SerializeField] private Vector3 punchRotation =
            new Vector3(0f, 0f, 12f);
        [SerializeField] private Vector3 punchPos =
            new Vector3(0.06f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float punchDuration = 0.2f;
        [SerializeField, Min(1)] private int punchVibrato = 25;
        [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.7f;
        [SerializeField, Min(0.01f)] private float damageFlashDuration = 0.2f;
        [SerializeField, Min(0f)] private float damageFlashEmissionIntensity = 2f;

        private Vector3 _baseScale;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseLocalEulerAngles;
        private Vector3 _baseAppearanceLocalPosition;
        private Color _baseColor;
        private Sprite _defaultSprite;
        private EnemySpriteProfile _activeEnemySpriteProfile;
        private CharacterVisualState _lastVisualState;
        private Tween _appearanceTween;
        private Tween _appearanceSfxDelayTween;
        private Tween _damageReactionTween;
        private Tween _damageEmissionTween;
        private Material _spriteMaterial;
        private bool _damageEmissionStateCaptured;
        private Color _damageEmissionOriginalColor;
        private float _damageEmissionOriginalEnabled;
        private float _damageEmissionOriginalIntensity;
        private bool _damageEmissionOriginalKeywordEnabled;
        private bool _initialized;
        private bool _isMerchantMode;

        internal Vector3 SoulLossAnchorWorldPosition
        {
            get
            {
                EnsureInitialized();
                if (sprite == null)
                {
                    return transform.position;
                }

                Bounds bounds = sprite.bounds;
                return new Vector3(
                    bounds.center.x,
                    bounds.max.y,
                    bounds.center.z);
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            KillAppearanceAnimation();
            KillDamageReaction();
        }

        private void OnDestroy()
        {
            KillDamageReaction();
            if (_spriteMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_spriteMaterial);
            }
            else
            {
                DestroyImmediate(_spriteMaterial);
            }

            _spriteMaterial = null;
        }

        public void PlayEntranceAnimation(Action onComplete = null)
        {
            EnsureInitialized();
            KillAppearanceAnimation();
            ScheduleAppearanceSfx(EnemyEntranceSfxId, entranceSfxDelay);

            Vector3 startEulerAngles =
                _baseLocalEulerAngles + appearanceRotationOffset;
            appearanceTarget.localRotation = Quaternion.Euler(startEulerAngles);

            if (appearanceEnterDuration <= 0f)
            {
                CompleteEntranceAnimation(onComplete);
                return;
            }

            _appearanceTween = appearanceTarget.DOLocalRotate(
                    _baseLocalEulerAngles,
                    appearanceEnterDuration,
                    RotateMode.Fast)
                .SetEase(appearanceEnterEase)
                .OnComplete(() => CompleteEntranceAnimation(onComplete));
        }

        public void PlayExitAnimation(Action onComplete)
        {
            EnsureInitialized();
            KillAppearanceAnimation();
            ScheduleAppearanceSfx(
                UnityEngine.Random.Range(0, 2) == 0
                    ? EnemyExitSfxId01
                    : EnemyExitSfxId02,
                exitSfxDelay);
            appearanceTarget.localRotation = _baseLocalRotation;

            Vector3 endEulerAngles =
                _baseLocalEulerAngles + appearanceRotationOffset;
            if (appearanceExitDuration <= 0f)
            {
                appearanceTarget.localRotation = Quaternion.Euler(endEulerAngles);
                onComplete?.Invoke();
                return;
            }

            Ease exitEase = _isMerchantMode
                ? merchantExitEase
                : appearanceExitEase;
            _appearanceTween = appearanceTarget.DOLocalRotate(
                    endEulerAngles,
                    appearanceExitDuration,
                    RotateMode.Fast)
                .SetEase(exitEase)
                .OnComplete(() =>
                {
                    _appearanceTween = null;
                    onComplete?.Invoke();
                });
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

        public void PlayDamageReaction(bool playSound = true)
        {
            EnsureInitialized();
            if (sprite == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            KillDamageReaction();
            RenderVisual(CharacterVisualState.Attacked);
            if (playSound)
            {
                SoundManager.Current?.PlaySfx(EnemyDamageSfxId);
            }
            PlayDamageEmissionFlash();

            if (appearanceTarget == null)
            {
                return;
            }

            _damageReactionTween = DOTween.Sequence()
                .Append(appearanceTarget.DOPunchPosition(
                    punchPos,
                    Mathf.Max(punchDuration, 0.01f),
                    Mathf.Max(punchVibrato, 1),
                    Mathf.Clamp01(punchElasticity)))
                .Join(appearanceTarget.DOPunchRotation(
                    punchRotation,
                    Mathf.Max(punchDuration, 0.01f),
                    Mathf.Max(punchVibrato, 1),
                    Mathf.Clamp01(punchElasticity)))
                .SetTarget(this)
                .OnComplete(() => _damageReactionTween = null);
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
            _isMerchantMode = true;
            appearanceTarget.localRotation = _baseLocalRotation;

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
            _isMerchantMode = false;
            appearanceTarget.localRotation = _baseLocalRotation;

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

            if (appearanceTarget == null)
            {
                appearanceTarget = transform.Find("RotationAxis");
            }

            if (appearanceTarget == null)
            {
                appearanceTarget = transform;
            }

            _baseScale = transform.localScale;
            _baseLocalRotation = appearanceTarget.localRotation;
            _baseLocalEulerAngles = appearanceTarget.localEulerAngles;
            _baseAppearanceLocalPosition = appearanceTarget.localPosition;
            _baseColor = sprite != null ? sprite.color : Color.white;
            _defaultSprite = sprite != null ? sprite.sprite : null;
            _initialized = true;
        }

        private void CompleteEntranceAnimation(Action onComplete)
        {
            _appearanceTween = null;
            appearanceTarget.localRotation = _baseLocalRotation;
            onComplete?.Invoke();
        }

        private void KillAppearanceAnimation()
        {
            _appearanceTween?.Kill();
            _appearanceTween = null;
            _appearanceSfxDelayTween?.Kill();
            _appearanceSfxDelayTween = null;
        }

        private void KillDamageReaction()
        {
            _damageReactionTween?.Kill(complete: true);
            _damageReactionTween = null;
            if (appearanceTarget != null && _initialized)
            {
                appearanceTarget.localPosition = _baseAppearanceLocalPosition;
                appearanceTarget.localRotation = _baseLocalRotation;
            }

            RestoreDamageEmissionImmediate();
        }

        private void PlayDamageEmissionFlash()
        {
            Material material = ResolveSpriteMaterial();
            if (material == null ||
                !material.HasProperty(EmissionColorId) ||
                !material.HasProperty(EmissionIntensityId))
            {
                return;
            }

            RestoreDamageEmissionImmediate();
            _spriteMaterial = material;
            _damageEmissionOriginalColor = material.GetColor(EmissionColorId);
            _damageEmissionOriginalIntensity =
                material.GetFloat(EmissionIntensityId);
            _damageEmissionOriginalEnabled =
                material.HasProperty(EmissionEnabledId)
                    ? material.GetFloat(EmissionEnabledId)
                    : 0f;
            _damageEmissionOriginalKeywordEnabled =
                material.IsKeywordEnabled(EmissionKeyword);
            _damageEmissionStateCaptured = true;

            material.SetColor(EmissionColorId, Color.white);
            if (material.HasProperty(EmissionEnabledId))
            {
                material.SetFloat(EmissionEnabledId, 1f);
            }

            material.EnableKeyword(EmissionKeyword);
            float duration = Mathf.Max(damageFlashDuration, 0.01f);
            float targetIntensity = Mathf.Max(
                damageFlashEmissionIntensity,
                _damageEmissionOriginalIntensity);
            _damageEmissionTween = DOTween.Sequence()
                .Append(DOVirtual.Float(
                    _damageEmissionOriginalIntensity,
                    targetIntensity,
                    duration * 0.15f,
                    value => material.SetFloat(EmissionIntensityId, value)))
                .Append(DOVirtual.Float(
                    targetIntensity,
                    _damageEmissionOriginalIntensity,
                    duration * 0.2f,
                    value => material.SetFloat(EmissionIntensityId, value)))
                .Append(DOVirtual.Float(
                    _damageEmissionOriginalIntensity,
                    targetIntensity,
                    duration * 0.15f,
                    value => material.SetFloat(EmissionIntensityId, value)))
                .Append(DOVirtual.Float(
                    targetIntensity,
                    _damageEmissionOriginalIntensity,
                    duration * 0.5f,
                    value => material.SetFloat(EmissionIntensityId, value)))
                .SetTarget(this)
                .OnComplete(() =>
                {
                    if (_damageEmissionTween != null)
                    {
                        _damageEmissionTween = null;
                    }

                    RestoreDamageEmissionImmediate();
                });
        }

        private Material ResolveSpriteMaterial()
        {
            if (_spriteMaterial != null)
            {
                return _spriteMaterial;
            }

            if (sprite == null)
            {
                return null;
            }

            _spriteMaterial = sprite.material;
            return _spriteMaterial;
        }

        private void RestoreDamageEmissionImmediate()
        {
            _damageEmissionTween?.Kill();
            _damageEmissionTween = null;
            if (!_damageEmissionStateCaptured || _spriteMaterial == null)
            {
                _damageEmissionStateCaptured = false;
                return;
            }

            _spriteMaterial.SetColor(
                EmissionColorId,
                _damageEmissionOriginalColor);
            _spriteMaterial.SetFloat(
                EmissionIntensityId,
                _damageEmissionOriginalIntensity);
            if (_spriteMaterial.HasProperty(EmissionEnabledId))
            {
                _spriteMaterial.SetFloat(
                    EmissionEnabledId,
                    _damageEmissionOriginalEnabled);
            }

            if (_damageEmissionOriginalKeywordEnabled)
            {
                _spriteMaterial.EnableKeyword(EmissionKeyword);
            }
            else
            {
                _spriteMaterial.DisableKeyword(EmissionKeyword);
            }

            _damageEmissionStateCaptured = false;
        }

        private void ScheduleAppearanceSfx(string sfxId, float delay)
        {
            if (string.IsNullOrWhiteSpace(sfxId))
            {
                return;
            }

            if (delay <= 0f)
            {
                SoundManager.Current?.PlaySfx(sfxId);
                return;
            }

            _appearanceSfxDelayTween = DOVirtual.DelayedCall(
                delay,
                () =>
                {
                    _appearanceSfxDelayTween = null;
                    SoundManager.Current?.PlaySfx(sfxId);
                });
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
