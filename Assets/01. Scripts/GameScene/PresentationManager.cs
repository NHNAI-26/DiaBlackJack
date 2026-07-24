using Border.Core;
using Border.Events;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

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

        private Tween moodTween;

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
            if (!shakeEnabled || force <= 0f || float.IsNaN(force) || float.IsInfinity(force))
                return;

            if (impulseSource != null)
                impulseSource.GenerateImpulseWithForce(force);
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

        private void SetCameraShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
        }
    }
}
