using Border.Core;
using Border.Events;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class PresentationManager : MonoBehaviour
    {
        [Header("Mood")]
        [SerializeField] private Volume moodVolume;

        [Header("Camera Shake")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private BoolEventChannelSO changeCameraShakeEvent;
        [SerializeField] private bool shakeEnabled = true;

        [SerializeField, Min(0.01f)] private float chromaticReturnSpeed = 2f;
        private Tween moodTween;
        private Tween shakeDurationRestoreTween;
        private float originalImpulseDuration = -1f;
        private Volume chromaticVolume;
        private VolumeProfile chromaticProfile;
        private ChromaticAberration chromaticAberration;
        private Tween chromaticTween;

        public static PresentationManager Current { get; private set; }

        private void OnEnable()
        {
            if (Current != null && Current != this)
            {
                Log.W("[PresentationManager] Another scene-local manager is active; this component was disabled.", this);
                enabled = false;
                return;
            }

            Current = this;

            if (changeCameraShakeEvent != null)
                changeCameraShakeEvent.OnEventRaised += SetCameraShakeEnabled;
        }

        private void OnDisable()
        {
            if (changeCameraShakeEvent != null)
                changeCameraShakeEvent.OnEventRaised -= SetCameraShakeEnabled;

            if (ReferenceEquals(Current, this))
                Current = null;

            KillMoodTween();
            RestoreCameraShakeDuration();
            CleanupChromaticAberration();
        }

        public void BlendToMood(VolumeProfile profile, float duration)
        {
            if (profile == null)
            {
                RestoreMood(duration);
                return;
            }

            KillMoodTween();

            if (moodVolume == null)
                return;

            moodVolume.sharedProfile = profile;
            SetMoodWeight(1f, duration);
        }

        public void RestoreMood(float duration)
        {
            KillMoodTween();

            if (moodVolume == null)
                return;

            SetMoodWeight(0f, duration);
        }

        public void ShakeCamera(float force = 1f)
        {
            if (!shakeEnabled || impulseSource == null ||
                force <= 0f || float.IsNaN(force) || float.IsInfinity(force))
                return;

            RestoreCameraShakeDuration();
            impulseSource.GenerateImpulseWithForce(force);
        }

        public void ShakeCameraForDuration(float duration)
        {
            if (!shakeEnabled || impulseSource == null ||
                duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                return;
            if (originalImpulseDuration < 0f)
                originalImpulseDuration = impulseSource.ImpulseDefinition.ImpulseDuration;
            shakeDurationRestoreTween?.Kill();
            impulseSource.ImpulseDefinition.ImpulseDuration = duration;
            impulseSource.GenerateImpulseWithForce(1f);
            shakeDurationRestoreTween = DOVirtual.DelayedCall(duration, RestoreCameraShakeDuration, false);
        }
        public void StartChromaticAberration(float riseSpeed)
        {
            if (riseSpeed <= 0f || float.IsNaN(riseSpeed) || float.IsInfinity(riseSpeed))
                return;
            EnsureChromaticAberration();
            TweenChromaticAberration(1f, riseSpeed);
        }

        public void StopChromaticAberration()
        {
            if (chromaticVolume != null)
                TweenChromaticAberration(0f, chromaticReturnSpeed);
        }
        private void SetMoodWeight(float targetWeight, float duration)
        {
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                moodVolume.weight = targetWeight;
                return;
            }

            moodTween = DOTween
                .To(() => moodVolume.weight, value => moodVolume.weight = value, targetWeight, duration)
                .OnComplete(() => moodTween = null);
        }

        private void KillMoodTween()
        {
            if (moodTween == null)
                return;

            moodTween.Kill();
            moodTween = null;
        }

        private void RestoreCameraShakeDuration()
        {
            if (originalImpulseDuration < 0f)
                return;
            shakeDurationRestoreTween?.Kill();
            shakeDurationRestoreTween = null;
            if (impulseSource != null)
                impulseSource.ImpulseDefinition.ImpulseDuration = originalImpulseDuration;
            originalImpulseDuration = -1f;
        }
        private void EnsureChromaticAberration()
        {
            if (chromaticAberration != null)
                return;
            chromaticProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            chromaticAberration = chromaticProfile.Add<ChromaticAberration>(true);
            chromaticAberration.intensity.Override(1f);
            chromaticVolume = gameObject.AddComponent<Volume>();
            chromaticVolume.isGlobal = true;
            chromaticVolume.priority = 2f;
            chromaticVolume.weight = 0f;
            chromaticVolume.sharedProfile = chromaticProfile;
        }
        private void TweenChromaticAberration(float target, float speed)
        {
            KillChromaticTween();
            float current = chromaticVolume.weight;
            if (Mathf.Approximately(current, target))
            {
                chromaticVolume.weight = target;
                return;
            }
            chromaticTween = DOTween
                .To(() => chromaticVolume.weight,
                    value => chromaticVolume.weight = value,
                    target,
                    Mathf.Abs(target - current) / speed)
                .SetEase(Ease.Linear)
                .OnComplete(() => chromaticTween = null);
        }
        private void KillChromaticTween()
        {
            chromaticTween?.Kill();
            chromaticTween = null;
        }

        private void CleanupChromaticAberration()
        {
            KillChromaticTween();
            if (chromaticVolume != null)
            {
                chromaticVolume.sharedProfile = null;
                Destroy(chromaticVolume);
            }
            if (chromaticAberration != null)
                Destroy(chromaticAberration);
            if (chromaticProfile != null)
                Destroy(chromaticProfile);
            chromaticVolume = null;
            chromaticProfile = null;
            chromaticAberration = null;
        }
        private void SetCameraShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
        }
    }
}
