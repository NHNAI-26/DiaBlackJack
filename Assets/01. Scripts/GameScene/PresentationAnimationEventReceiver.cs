using System;
using System.Collections.Generic;
using Border.Audio;
using Border.Core;
using UnityEngine;
using UnityEngine.VFX;

namespace DiaBlackJack.GameScene
{
    public abstract class PresentationAnimationEventReceiver : MonoBehaviour
    {
        [Serializable]
        private sealed class VfxBinding
        {
            [SerializeField] internal string id;
            [SerializeField] internal VisualEffect effect;
            [SerializeField] internal string eventName = "OnPlay";
        }

        [Header("VFX")]
        [SerializeField] private List<VfxBinding> vfxBindings = new();

        private readonly Dictionary<string, VfxBinding> vfxCatalog =
            new(StringComparer.Ordinal);

        protected virtual void Awake()
        {
            BuildVfxCatalog();
        }

        public void PlayVfx(string bindingId)
        {
            string id = Key(bindingId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W($"[{LogPrefix}] Cannot play a VFX with an empty binding ID.", this);
                return;
            }

            if (!vfxCatalog.TryGetValue(id, out VfxBinding binding))
            {
                Log.W($"[{LogPrefix}] VFX binding ID '{id}' is not configured.", this);
                return;
            }

            if (binding.effect == null)
            {
                Log.W($"[{LogPrefix}] VFX binding '{id}' has no VisualEffect.", this);
                return;
            }

            binding.effect.SendEvent(binding.eventName);
        }

        public void StopVfx(string bindingId)
        {
            string id = Key(bindingId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W($"[{LogPrefix}] Cannot stop a VFX with an empty binding ID.", this);
                return;
            }

            if (!vfxCatalog.TryGetValue(id, out VfxBinding binding))
            {
                Log.W($"[{LogPrefix}] VFX binding ID '{id}' is not configured.", this);
                return;
            }

            if (binding.effect == null)
            {
                Log.W($"[{LogPrefix}] VFX binding '{id}' has no VisualEffect.", this);
                return;
            }

            binding.effect.Stop();
        }

        public void PlaySfx(string soundId)
        {
            string id = Key(soundId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W($"[{LogPrefix}] Cannot play an SFX with an empty sound ID.", this);
                return;
            }

            if (SoundManager.Current == null)
            {
                Log.W($"[{LogPrefix}] SoundManager is unavailable for SFX '{id}'.", this);
                return;
            }

            SoundManager.Current.PlaySfx(id);
        }

        public void StopSfx(string soundId)
        {
            string id = Key(soundId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W($"[{LogPrefix}] Cannot stop an SFX with an empty sound ID.", this);
                return;
            }

            if (SoundManager.Current == null)
            {
                Log.W($"[{LogPrefix}] SoundManager is unavailable for SFX '{id}'.", this);
                return;
            }

            SoundManager.Current.StopSfx(id);
        }

        public void ShakeCamera(float duration) =>
            Presentation?.ShakeCameraForDuration(duration);

        public void StartChromaticAberration(float riseSpeed) =>
            Presentation?.StartChromaticAberration(riseSpeed);

        public void StopChromaticAberration() =>
            StopChromaticAberration(0f);

        public void StopChromaticAberration(float returnSpeed) =>
            Presentation?.StopChromaticAberration(returnSpeed);

        public void StartFieldOfViewIncrease(float riseSpeed) =>
            Presentation?.StartFieldOfViewIncrease(riseSpeed);

        public void StopFieldOfViewIncrease() =>
            StopFieldOfViewIncrease(0f);

        public void StopFieldOfViewIncrease(float returnSpeed) =>
            Presentation?.StopFieldOfViewIncrease(returnSpeed);

        public void StartColorScreenBlend(float fadeOutSpeed) =>
            Presentation?.StartColorScreenBlend(fadeOutSpeed);

        protected static string Key(string value) => value?.Trim() ?? string.Empty;

        private void BuildVfxCatalog()
        {
            for (int i = 0; i < vfxBindings.Count; i++)
            {
                VfxBinding binding = vfxBindings[i];
                string id = binding == null ? string.Empty : Key(binding.id);

                if (string.IsNullOrEmpty(id))
                    Log.W($"[{LogPrefix}] VFX binding {i} has an empty ID and was ignored.", this);
                else if (binding.effect == null)
                    Log.W($"[{LogPrefix}] VFX binding '{id}' has no VisualEffect and was ignored.", this);
                else if (string.IsNullOrEmpty(Key(binding.eventName)))
                    Log.W($"[{LogPrefix}] VFX binding '{id}' has an empty event name and was ignored.", this);
                else if (vfxCatalog.ContainsKey(id))
                    Log.W($"[{LogPrefix}] Duplicate VFX binding ID '{id}' was ignored.", this);
                else
                {
                    binding.eventName = Key(binding.eventName);
                    vfxCatalog.Add(id, binding);
                }
            }
        }

        private PresentationManager Presentation
        {
            get
            {
                if (PresentationManager.Current == null)
                    Log.W($"[{LogPrefix}] PresentationManager is unavailable.", this);

                return PresentationManager.Current;
            }
        }

        private string LogPrefix => GetType().Name;
    }
}
