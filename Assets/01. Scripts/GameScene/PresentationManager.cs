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
        private static readonly int ColorScreenBlendEnabledId = Shader.PropertyToID("_ColorScreenBlendEnabled");
        private static readonly int BlendStrengthId = Shader.PropertyToID("_BlendStrength");
        private const string ColorScreenBlendKeyword = "_COLOR_SCREEN_BLEND_ON";

        [Header("Mood")]
        [SerializeField] private Volume moodVolume;
        [SerializeField] private MoodController moodController;

        [Header("Camera Shake")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private BoolEventChannelSO changeCameraShakeEvent;
        [SerializeField] private bool shakeEnabled = true;

        [SerializeField, Min(0.01f)] private float chromaticReturnSpeed = 2f;

        [Header("Field Of View")]
        [SerializeField] private CinemachineCamera fieldOfViewCamera;
        [SerializeField, Range(1f, 179f)] private float fieldOfViewTarget = 90f;
        [SerializeField, Min(0.01f)] private float fieldOfViewReturnSpeed = 120f;

        [Header("Color Screen Blend")]
        [SerializeField] private Material colorScreenBlendMaterial;

        private Tween moodTween;
        private Tween shakeDurationRestoreTween;
        private float originalImpulseDuration = -1f;
        private Volume chromaticVolume;
        private VolumeProfile chromaticProfile;
        private ChromaticAberration chromaticAberration;
        private Tween chromaticTween;
        private CinemachineCamera activeFieldOfViewCamera;
        private float originalFieldOfView = -1f;
        private Tween fieldOfViewTween;
        private Tween colorScreenBlendTween;

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
            RestoreFieldOfViewImmediate();
            ResetColorScreenBlendImmediate();
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

        public bool TryBlendToMood(string id, float duration = -1f)
        {
            MoodController controller = ResolveMoodController();
            return controller != null && controller.TryBlendToMood(id, duration);
        }

        public void BlendToMood(MoodProfileSO profile, float duration = -1f)
        {
            ResolveMoodController()?.BlendToMood(profile, duration);
        }

        public void SetMoodImmediate(MoodProfileSO profile)
        {
            ResolveMoodController()?.SetMoodImmediate(profile);
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

        public void StopChromaticAberration(float returnSpeed = 0f)
        {
            if (chromaticVolume != null)
                TweenChromaticAberration(0f, ResolveReturnSpeed(returnSpeed, chromaticReturnSpeed));
        }

        public void StartFieldOfViewIncrease(float riseSpeed)
        {
            if (riseSpeed <= 0f || float.IsNaN(riseSpeed) || float.IsInfinity(riseSpeed))
                return;

            CinemachineCamera camera = ResolveFieldOfViewCamera();
            if (camera == null)
            {
                Log.W("[PresentationManager] Cannot animate FOV because no CinemachineCamera is available.", this);
                return;
            }

            if (activeFieldOfViewCamera != camera || originalFieldOfView < 0f)
            {
                activeFieldOfViewCamera = camera;
                originalFieldOfView = GetFieldOfView(camera);
            }

            TweenFieldOfView(fieldOfViewTarget, riseSpeed, clearOriginalOnComplete: false);
        }

        public void StopFieldOfViewIncrease(float returnSpeed = 0f)
        {
            if (activeFieldOfViewCamera == null || originalFieldOfView < 0f)
                return;

            TweenFieldOfView(
                originalFieldOfView,
                ResolveReturnSpeed(returnSpeed, fieldOfViewReturnSpeed),
                clearOriginalOnComplete: true);
        }

        public void StartColorScreenBlend(float fadeOutSpeed)
        {
            if (fadeOutSpeed <= 0f || float.IsNaN(fadeOutSpeed) || float.IsInfinity(fadeOutSpeed))
                return;

            if (!EnsureColorScreenBlendMaterial())
                return;

            KillColorScreenBlendTween();
            SetColorScreenBlendStrength(1f);
            colorScreenBlendTween = DOTween
                .To(() => colorScreenBlendMaterial.GetFloat(BlendStrengthId),
                    SetColorScreenBlendStrength,
                    0f,
                    1f / fadeOutSpeed)
                .SetEase(Ease.Linear)
                .OnComplete(() => colorScreenBlendTween = null);
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

        private MoodController ResolveMoodController()
        {
            if (moodController == null)
                moodController = GetComponentInChildren<MoodController>(true);

            return moodController;
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

        private CinemachineCamera ResolveFieldOfViewCamera()
        {
            if (fieldOfViewCamera != null)
                return fieldOfViewCamera;

            for (int i = 0; i < CinemachineBrain.ActiveBrainCount; i++)
            {
                if (CinemachineBrain.GetActiveBrain(i).ActiveVirtualCamera is CinemachineCamera camera)
                    return camera;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null &&
                mainCamera.TryGetComponent(out CinemachineBrain brain) &&
                brain.ActiveVirtualCamera is CinemachineCamera activeCamera)
                return activeCamera;

            return null;
        }

        private void TweenFieldOfView(float target, float speed, bool clearOriginalOnComplete)
        {
            if (activeFieldOfViewCamera == null)
                return;

            KillFieldOfViewTween();

            float clampedTarget = Mathf.Clamp(target, 1f, 179f);
            float current = GetFieldOfView(activeFieldOfViewCamera);
            if (Mathf.Approximately(current, clampedTarget))
            {
                SetFieldOfView(activeFieldOfViewCamera, clampedTarget);
                if (clearOriginalOnComplete)
                    ClearOriginalFieldOfView();
                return;
            }

            fieldOfViewTween = DOTween
                .To(() => GetFieldOfView(activeFieldOfViewCamera),
                    value => SetFieldOfView(activeFieldOfViewCamera, value),
                    clampedTarget,
                    Mathf.Abs(clampedTarget - current) / speed)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    fieldOfViewTween = null;
                    if (clearOriginalOnComplete)
                        ClearOriginalFieldOfView();
                });
        }

        private void RestoreFieldOfViewImmediate()
        {
            KillFieldOfViewTween();
            if (activeFieldOfViewCamera != null && originalFieldOfView >= 0f)
                SetFieldOfView(activeFieldOfViewCamera, originalFieldOfView);
            ClearOriginalFieldOfView();
        }

        private void KillFieldOfViewTween()
        {
            fieldOfViewTween?.Kill();
            fieldOfViewTween = null;
        }

        private void ClearOriginalFieldOfView()
        {
            activeFieldOfViewCamera = null;
            originalFieldOfView = -1f;
        }

        private static float GetFieldOfView(CinemachineCamera camera)
        {
            return camera.Lens.FieldOfView;
        }

        private static void SetFieldOfView(CinemachineCamera camera, float value)
        {
            camera.Lens.FieldOfView = Mathf.Clamp(value, 1f, 179f);
        }

        private static float ResolveReturnSpeed(float returnSpeed, float defaultSpeed)
        {
            if (returnSpeed > 0f && !float.IsNaN(returnSpeed) && !float.IsInfinity(returnSpeed))
                return returnSpeed;

            return defaultSpeed;
        }

        private bool EnsureColorScreenBlendMaterial()
        {
            if (colorScreenBlendMaterial == null)
            {
                Log.W("[PresentationManager] Cannot animate color screen blend because no material is assigned.", this);
                return false;
            }

            if (!colorScreenBlendMaterial.HasProperty(BlendStrengthId))
            {
                Log.W("[PresentationManager] Color screen blend material does not have _BlendStrength.", this);
                return false;
            }

            colorScreenBlendMaterial.EnableKeyword(ColorScreenBlendKeyword);
            if (colorScreenBlendMaterial.HasProperty(ColorScreenBlendEnabledId))
                colorScreenBlendMaterial.SetFloat(ColorScreenBlendEnabledId, 1f);
            return true;
        }

        private void SetColorScreenBlendStrength(float value)
        {
            colorScreenBlendMaterial.SetFloat(BlendStrengthId, Mathf.Clamp01(value));
        }

        private void ResetColorScreenBlendImmediate()
        {
            KillColorScreenBlendTween();
            if (colorScreenBlendMaterial != null && colorScreenBlendMaterial.HasProperty(BlendStrengthId))
                colorScreenBlendMaterial.SetFloat(BlendStrengthId, 0f);
        }

        private void KillColorScreenBlendTween()
        {
            colorScreenBlendTween?.Kill();
            colorScreenBlendTween = null;
        }

        private void SetCameraShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
        }
    }
}
