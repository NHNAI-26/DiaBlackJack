using System.Collections.Generic;
using Border.Audio;
using DG.Tweening;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class MoodController : MonoBehaviour
    {
        private static readonly int WindowGlassGlowColorId =
            Shader.PropertyToID("_GlassGlowColor");

        [SerializeField] private List<MoodProfileSO> moodProfiles =
            new List<MoodProfileSO>();
        [SerializeField] private Renderer[] windowGlassRenderers;
        [SerializeField] private Light volumetricLight;
        [SerializeField] private Light enemyLight;
        [SerializeField] private Light enteranceLight;
        [SerializeField] private MoodTransitionMode transitionMode =
            MoodTransitionMode.Fade;

        private MaterialPropertyBlock _windowProperties;
        private Sequence _moodSequence;
        private bool _hasWindowGlassGlowColor;
        private Color _currentWindowGlassGlowColor;

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
            ApplyColors(
                profile.WindowGlassGlowColor,
                profile.VolumetricLightColor,
                profile.EnemyLightColor,
                profile.EnteranceLightColor);
            ApplyBgm(profile);
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
        }

        private void FadeToMood(MoodProfileSO profile, float duration)
        {
            Color windowStart = ResolveCurrentWindowGlassGlowColor();
            Color volumetricStart = ResolveLightColor(volumetricLight);
            Color enemyStart = ResolveLightColor(enemyLight);
            Color enteranceStart = ResolveLightColor(enteranceLight);

            _moodSequence = DOTween.Sequence()
                .Join(DOVirtual.Color(
                    windowStart,
                    profile.WindowGlassGlowColor,
                    duration,
                    SetWindowGlassGlowColor))
                .Join(DOVirtual.Color(
                    volumetricStart,
                    profile.VolumetricLightColor,
                    duration,
                    value => SetLightColor(volumetricLight, value)))
                .Join(DOVirtual.Color(
                    enemyStart,
                    profile.EnemyLightColor,
                    duration,
                    value => SetLightColor(enemyLight, value)))
                .Join(DOVirtual.Color(
                    enteranceStart,
                    profile.EnteranceLightColor,
                    duration,
                    value => SetLightColor(enteranceLight, value)))
                .SetEase(Ease.Linear)
                .OnComplete(() => _moodSequence = null);
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

        private static Color ResolveLightColor(Light target)
        {
            return target == null ? Color.white : target.color;
        }

        private static void SetLightColor(Light target, Color color)
        {
            if (target != null)
            {
                target.color = color;
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
            _moodSequence?.Kill();
            _moodSequence = null;
        }
    }
}
