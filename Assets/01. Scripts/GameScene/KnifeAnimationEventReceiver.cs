using UnityEngine;
using System;
using System.Collections.Generic;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class KnifeAnimationEventReceiver :
        PresentationAnimationEventReceiver
    {
        public event Action KnifeImpact;

        [Serializable]
        private sealed class CameraShakeProfile
        {
            [SerializeField] internal string id;
            [SerializeField, Min(0.01f)] internal float duration = 0.5f;
            [SerializeField, Min(0f)] internal float amplitude = 0.005f;
        }

        [Header("Knife Camera Shake")]
        [SerializeField] private string defaultCameraShakeProfileId = "small";
        [SerializeField] private List<CameraShakeProfile> cameraShakeProfiles = new()
        {
            new CameraShakeProfile
            {
                id = "small",
                duration = 0.5f,
                amplitude = 0.005f
            },
            new CameraShakeProfile
            {
                id = "tap",
                duration = 0.2f,
                amplitude = 0.003f
            }
        };

        private readonly Dictionary<string, CameraShakeProfile> cameraShakeCatalog =
            new(StringComparer.Ordinal);

        protected override void Awake()
        {
            base.Awake();
            BuildCameraShakeCatalog();
        }

        public override void ShakeCamera(float duration) =>
            Presentation?.ShakeSmallCameraForDuration(duration);

        public void ShakeCameraProfile(string profileId)
        {
            string id = Key(profileId);
            if (string.IsNullOrEmpty(id))
                id = Key(defaultCameraShakeProfileId);

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[KnifeAnimationEventReceiver] Cannot shake camera with an empty profile ID.", this);
                return;
            }

            if (!cameraShakeCatalog.TryGetValue(id, out CameraShakeProfile profile))
            {
                Debug.LogWarning($"[KnifeAnimationEventReceiver] Camera shake profile '{id}' is not configured.", this);
                return;
            }

            Presentation?.ShakeSmallCameraForDuration(profile.duration, profile.amplitude);
        }

        public void ShakeCameraSmall() =>
            ShakeCameraProfile("small");

        public void ShakeCameraTap() =>
            ShakeCameraProfile("tap");

        public void NotifyKnifeImpact()
        {
            KnifeImpact?.Invoke();
        }

        private void BuildCameraShakeCatalog()
        {
            cameraShakeCatalog.Clear();

            for (int i = 0; i < cameraShakeProfiles.Count; i++)
            {
                CameraShakeProfile profile = cameraShakeProfiles[i];
                string id = profile == null ? string.Empty : Key(profile.id);
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[KnifeAnimationEventReceiver] Camera shake profile {i} has an empty ID and was ignored.", this);
                    continue;
                }

                if (profile.duration <= 0f ||
                    float.IsNaN(profile.duration) ||
                    float.IsInfinity(profile.duration))
                {
                    Debug.LogWarning($"[KnifeAnimationEventReceiver] Camera shake profile '{id}' has an invalid duration and was ignored.", this);
                    continue;
                }

                if (profile.amplitude <= 0f ||
                    float.IsNaN(profile.amplitude) ||
                    float.IsInfinity(profile.amplitude))
                {
                    Debug.LogWarning($"[KnifeAnimationEventReceiver] Camera shake profile '{id}' has an invalid amplitude and was ignored.", this);
                    continue;
                }

                if (cameraShakeCatalog.ContainsKey(id))
                {
                    Debug.LogWarning($"[KnifeAnimationEventReceiver] Duplicate camera shake profile '{id}' was ignored.", this);
                    continue;
                }

                profile.id = id;
                cameraShakeCatalog.Add(id, profile);
            }
        }
    }
}
