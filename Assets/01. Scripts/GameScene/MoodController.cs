using System.Collections.Generic;
using Border.Audio;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class MoodController : MonoBehaviour
    {
        private static readonly int WindowGlassGlowColorId =
            Shader.PropertyToID("_GlassGlowColor");
        private const float DefaultLightningDeltaTime = 1f / 60f;

        [SerializeField] private List<MoodProfileSO> moodProfiles =
            new List<MoodProfileSO>();
        [SerializeField] private Renderer[] windowGlassRenderers;
        [SerializeField] private Light volumetricLight;
        [SerializeField] private Light enemyLight;
        [SerializeField] private Light enteranceLight;
        [SerializeField] private MoodTransitionMode transitionMode =
            MoodTransitionMode.Fade;

        private MaterialPropertyBlock _windowProperties;
        private bool _isMoodBlendActive;
        private float _moodBlendDuration;
        private float _moodBlendElapsed;
        private float _moodBlendStartLightningBoost;
        private MoodProfileSO _pendingBlendProfile;
        private Color _blendWindowStart;
        private Color _blendWindowTarget;
        private Color _blendVolumetricStart;
        private Color _blendVolumetricTarget;
        private Color _blendEnemyStart;
        private Color _blendEnemyTarget;
        private Color _blendEnteranceStart;
        private Color _blendEnteranceTarget;
        private bool _hasWindowGlassGlowColor;
        private Color _currentWindowGlassGlowColor;
        private bool _audioReactiveLightningEnabled;
        private MoodProfileSO _audioReactiveLightningProfile;
        private SoundManager.SoundHandle _activeLightningSfx;
        private string _activeLightningSfxId;
        private float _lightningSfxPlayChance;
        private float _lightningSfxInterval;
        private float _lightningSfxRollTimer;
        private float _lightningSensitivity;
        private float _lightningThreshold;
        private float _lightningMaxBoost;
        private float _lightningAttackSpeed;
        private float _lightningReleaseSpeed;
        private float _currentLightningBoost;
        private float _currentLightningSfxRms;
        private bool _hasReactiveBaseline;
        private float _baseVolumetricLightIntensity;
        private float _baseEnemyLightIntensity;
        private float _baseEnteranceLightIntensity;

        public bool IsAudioReactiveLightningActive =>
            _audioReactiveLightningEnabled;

        public float CurrentAudioReactiveLightningRms =>
            _currentLightningSfxRms;

        public string CurrentAudioReactiveLightningSfxId =>
            _activeLightningSfxId;

        public float CurrentAudioReactiveLightningBoost =>
            _currentLightningBoost;

        public bool TryBlendToMood(string id, float duration = -1f)
        {
            MoodProfileSO profile = FindProfile(id);
            if (profile == null)
            {
                return false;
            }

            BlendToMood(profile, duration);
            return true;
        }

        public void BlendToMood(MoodProfileSO profile, float duration = -1f)
        {
            if (!CanApply(profile))
            {
                return;
            }

            float resolvedDuration = ResolveDuration(duration);
            if (resolvedDuration <= 0f)
            {
                SetMoodImmediate(profile);
                return;
            }

            KillMoodSequence();
            ApplyBgm(profile);

            switch (transitionMode)
            {
                case MoodTransitionMode.Fade:
                    BeginAudioReactiveLightningBlendOut(resolvedDuration);
                    FadeToMood(profile, resolvedDuration);
                    break;
                default:
                    SetMoodImmediate(profile);
                    break;
            }
        }

        public void SetMoodImmediate(MoodProfileSO profile)
        {
            if (!CanApply(profile))
            {
                return;
            }

            KillMoodSequence();
            DisableAudioReactiveLightning();
            ApplyColors(
                profile.WindowGlassGlowColor,
                profile.VolumetricLightColor,
                profile.EnemyLightColor,
                profile.EnteranceLightColor);
            ApplyBgm(profile);
            ConfigureAudioReactiveLightning(profile);
        }

        public MoodProfileSO FindProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || moodProfiles == null)
            {
                return null;
            }

            foreach (MoodProfileSO profile in moodProfiles)
            {
                if (profile != null &&
                    profile.HasValidId &&
                    string.Equals(profile.Id, id,
                        System.StringComparison.Ordinal))
                {
                    return profile;
                }
            }

            return null;
        }

        private void OnDisable()
        {
            KillMoodSequence();
            DisableAudioReactiveLightning();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime > 0f
                ? Time.deltaTime
                : DefaultLightningDeltaTime;

            if (_isMoodBlendActive)
            {
                UpdateMoodBlend(deltaTime);
            }

            if (!_audioReactiveLightningEnabled)
            {
                return;
            }

            UpdateLightningSfxPlayback(deltaTime);

            float targetBoost = ResolveAudioReactiveLightningTarget();
            float speed = targetBoost > _currentLightningBoost
                ? _lightningAttackSpeed
                : _lightningReleaseSpeed;
            _currentLightningBoost = Mathf.MoveTowards(
                _currentLightningBoost,
                targetBoost,
                speed * deltaTime);
            ApplyAudioReactiveLightningBoost();
        }

        public bool TriggerAudioReactiveLightningPulse(float strength = 1f)
        {
            if (!_audioReactiveLightningEnabled)
            {
                return false;
            }

            _currentLightningBoost = Mathf.Clamp(
                strength,
                0f,
                _lightningMaxBoost);
            ApplyAudioReactiveLightningBoost();
            return true;
        }

        public bool TriggerAudioReactiveLightningSfx()
        {
            if (!_audioReactiveLightningEnabled)
            {
                return false;
            }

            return TryPlayRandomLightningSfx();
        }

        private void FadeToMood(MoodProfileSO profile, float duration)
        {
            _pendingBlendProfile = profile;
            _moodBlendDuration = Mathf.Max(DefaultLightningDeltaTime, duration);
            _moodBlendElapsed = 0f;
            _moodBlendStartLightningBoost = _currentLightningBoost;
            _blendWindowStart = ResolveCurrentWindowGlassGlowColor();
            _blendWindowTarget = profile.WindowGlassGlowColor;
            _blendVolumetricStart = ResolveLightColor(volumetricLight);
            _blendVolumetricTarget = profile.VolumetricLightColor;
            _blendEnemyStart = ResolveLightColor(enemyLight);
            _blendEnemyTarget = profile.EnemyLightColor;
            _blendEnteranceStart = ResolveLightColor(enteranceLight);
            _blendEnteranceTarget = profile.EnteranceLightColor;
            _isMoodBlendActive = true;
            ApplyMoodBlend(0f);
        }

        private void ApplyColors(
            Color windowGlassGlowColor,
            Color volumetricLightColor,
            Color enemyLightColor,
            Color enteranceLightColor)
        {
            SetWindowGlassGlowColor(windowGlassGlowColor);
            SetLightColor(volumetricLight, volumetricLightColor);
            SetLightColor(enemyLight, enemyLightColor);
            SetLightColor(enteranceLight, enteranceLightColor);
        }

        private void SetWindowGlassGlowColor(Color color)
        {
            _currentWindowGlassGlowColor = color;
            _hasWindowGlassGlowColor = true;
            ApplyWindowGlassGlowColor(BoostColor(color));
        }

        private void ApplyWindowGlassGlowColor(Color color)
        {
            if (windowGlassRenderers == null)
            {
                return;
            }

            foreach (Renderer target in windowGlassRenderers)
            {
                if (target == null)
                {
                    continue;
                }

                _windowProperties ??= new MaterialPropertyBlock();
                target.GetPropertyBlock(_windowProperties);
                _windowProperties.SetColor(WindowGlassGlowColorId, color);
                target.SetPropertyBlock(_windowProperties);
            }
        }

        private Color ResolveCurrentWindowGlassGlowColor()
        {
            if (_hasWindowGlassGlowColor)
            {
                return _currentWindowGlassGlowColor;
            }

            if (windowGlassRenderers != null)
            {
                foreach (Renderer target in windowGlassRenderers)
                {
                    Material material = target == null
                        ? null
                        : target.sharedMaterial;
                    if (material != null &&
                        material.HasProperty(WindowGlassGlowColorId))
                    {
                        return material.GetColor(WindowGlassGlowColorId);
                    }
                }
            }

            return Color.white;
        }

        private void ConfigureAudioReactiveLightning(MoodProfileSO profile)
        {
            if (profile == null || !profile.EnableAudioReactiveLightning)
            {
                DisableAudioReactiveLightning();
                return;
            }

            _audioReactiveLightningProfile = profile;
            _lightningSfxPlayChance = Mathf.Clamp01(
                profile.LightningSfxPlayChance);
            _lightningSfxInterval = Mathf.Max(
                0.1f,
                profile.LightningSfxInterval);
            _lightningSfxRollTimer = _lightningSfxInterval;
            _lightningSensitivity = Mathf.Max(0f, profile.LightningSensitivity);
            _lightningThreshold = Mathf.Clamp01(profile.LightningThreshold);
            _lightningMaxBoost = Mathf.Max(0f, profile.LightningMaxBoost);
            _lightningAttackSpeed = Mathf.Max(0f, profile.LightningAttackSpeed);
            _lightningReleaseSpeed = Mathf.Max(0f, profile.LightningReleaseSpeed);
            _currentLightningSfxRms = 0f;
            _currentLightningBoost = 0f;
            _activeLightningSfx = default;
            _activeLightningSfxId = null;
            CaptureReactiveBaseline();
            _audioReactiveLightningEnabled = true;
        }

        private void DisableAudioReactiveLightning()
        {
            if (!_audioReactiveLightningEnabled && !_hasReactiveBaseline)
            {
                return;
            }

            _audioReactiveLightningEnabled = false;
            StopActiveLightningSfx();
            _audioReactiveLightningProfile = null;
            _lightningSfxRollTimer = 0f;
            _currentLightningSfxRms = 0f;
            _currentLightningBoost = 0f;
            ApplyAudioReactiveLightningBoost();
            _hasReactiveBaseline = false;
        }

        private void BeginAudioReactiveLightningBlendOut(float duration)
        {
            if (!_audioReactiveLightningEnabled && !_hasReactiveBaseline)
            {
                return;
            }

            _audioReactiveLightningEnabled = false;
            StopActiveLightningSfx(duration);
            _audioReactiveLightningProfile = null;
            _lightningSfxRollTimer = 0f;
            _currentLightningSfxRms = 0f;
        }

        private void UpdateMoodBlend(float deltaTime)
        {
            _moodBlendElapsed += deltaTime;
            float t = Mathf.Clamp01(_moodBlendElapsed / _moodBlendDuration);
            ApplyMoodBlend(t);

            if (t < 1f)
            {
                return;
            }

            MoodProfileSO completedProfile = _pendingBlendProfile;
            _isMoodBlendActive = false;
            _pendingBlendProfile = null;
            FinishAudioReactiveLightningBlendOut();
            ConfigureAudioReactiveLightning(completedProfile);
        }

        private void ApplyMoodBlend(float t)
        {
            ApplyColors(
                Color.Lerp(_blendWindowStart, _blendWindowTarget, t),
                Color.Lerp(_blendVolumetricStart, _blendVolumetricTarget, t),
                Color.Lerp(_blendEnemyStart, _blendEnemyTarget, t),
                Color.Lerp(_blendEnteranceStart, _blendEnteranceTarget, t));

            if (!_hasReactiveBaseline && _moodBlendStartLightningBoost <= 0f)
            {
                return;
            }

            _currentLightningBoost = Mathf.Lerp(
                _moodBlendStartLightningBoost,
                0f,
                t);
            ApplyAudioReactiveLightningBoost();
        }

        private void FinishAudioReactiveLightningBlendOut()
        {
            if (!_hasReactiveBaseline && _currentLightningBoost <= 0f)
            {
                return;
            }

            _currentLightningBoost = 0f;
            ApplyAudioReactiveLightningBoost();
            _hasReactiveBaseline = false;
        }

        private void CaptureReactiveBaseline()
        {
            _baseVolumetricLightIntensity = ResolveLightIntensity(volumetricLight);
            _baseEnemyLightIntensity = ResolveLightIntensity(enemyLight);
            _baseEnteranceLightIntensity = ResolveLightIntensity(enteranceLight);
            _hasReactiveBaseline = true;
        }

        private void UpdateLightningSfxPlayback(float deltaTime)
        {
            if (_audioReactiveLightningProfile == null ||
                !_audioReactiveLightningProfile.HasLightningSfxIds)
            {
                return;
            }

            _lightningSfxRollTimer -= deltaTime;
            if (_lightningSfxRollTimer > 0f)
            {
                return;
            }

            _lightningSfxRollTimer += _lightningSfxInterval;
            if (_lightningSfxRollTimer <= 0f)
            {
                _lightningSfxRollTimer = _lightningSfxInterval;
            }

            if (Random.value <= _lightningSfxPlayChance)
            {
                TryPlayRandomLightningSfx();
            }
        }

        private bool TryPlayRandomLightningSfx()
        {
            if (_audioReactiveLightningProfile == null ||
                !_audioReactiveLightningProfile.TryGetRandomLightningSfxId(
                    out string sfxId))
            {
                return false;
            }

            SoundManager manager = SoundManager.Current;
            if (manager == null)
            {
                return false;
            }

            SoundManager.SoundHandle handle = manager.PlaySfx(sfxId);
            if (!handle.IsValid)
            {
                return false;
            }

            _activeLightningSfx = handle;
            _activeLightningSfxId = sfxId;
            return true;
        }

        private void StopActiveLightningSfx(float fadeDuration = 0f)
        {
            if (_activeLightningSfx.IsValid)
            {
                SoundManager.Current?.StopSfx(_activeLightningSfx, fadeDuration);
            }

            _activeLightningSfx = default;
            _activeLightningSfxId = null;
        }

        private float ResolveAudioReactiveLightningTarget()
        {
            SoundManager manager = SoundManager.Current;
            if (manager == null ||
                !_activeLightningSfx.IsValid ||
                !manager.TryGetSfxRms(
                    _activeLightningSfx,
                    out _currentLightningSfxRms))
            {
                _currentLightningSfxRms = 0f;
                if (_activeLightningSfx.IsValid &&
                    manager != null &&
                    !manager.IsSfxPlaying(_activeLightningSfx))
                {
                    _activeLightningSfx = default;
                    _activeLightningSfxId = null;
                }

                return 0f;
            }

            float reactiveAmount =
                Mathf.Max(0f, _currentLightningSfxRms - _lightningThreshold) *
                _lightningSensitivity;
            return Mathf.Min(_lightningMaxBoost, reactiveAmount);
        }

        private void ApplyAudioReactiveLightningBoost()
        {
            float multiplier = 1f + _currentLightningBoost;
            ApplyWindowGlassGlowColor(BoostColor(_currentWindowGlassGlowColor));

            if (!_hasReactiveBaseline)
            {
                return;
            }

            SetLightIntensity(
                volumetricLight,
                _baseVolumetricLightIntensity * multiplier);
            SetLightIntensity(
                enemyLight,
                _baseEnemyLightIntensity * multiplier);
            SetLightIntensity(
                enteranceLight,
                _baseEnteranceLightIntensity * multiplier);
        }

        private Color BoostColor(Color color)
        {
            float multiplier = 1f + _currentLightningBoost;
            return new Color(
                color.r * multiplier,
                color.g * multiplier,
                color.b * multiplier,
                color.a);
        }

        private static Color ResolveLightColor(Light target)
        {
            return target == null ? Color.white : target.color;
        }

        private static float ResolveLightIntensity(Light target)
        {
            return target == null ? 0f : target.intensity;
        }

        private static void SetLightColor(Light target, Color color)
        {
            if (target != null)
            {
                target.color = color;
            }
        }

        private static void SetLightIntensity(Light target, float intensity)
        {
            if (target != null)
            {
                target.intensity = Mathf.Max(0f, intensity);
            }
        }

        private static void ApplyBgm(MoodProfileSO profile)
        {
            if (profile != null &&
                profile.TryGetRandomBgmId(out string bgmId))
            {
                SoundManager.Current?.PlayBgm(bgmId);
            }
        }

        private static float ResolveDuration(float duration)
        {
            return duration <= 0f ||
                float.IsNaN(duration) ||
                float.IsInfinity(duration)
                ? 0f
                : duration;
        }

        private static bool CanApply(MoodProfileSO profile)
        {
            return profile != null && profile.HasValidId;
        }

        private void KillMoodSequence()
        {
            _isMoodBlendActive = false;
            _pendingBlendProfile = null;
        }
    }
}
