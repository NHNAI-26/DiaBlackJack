using System;
using System.Collections.Generic;
using Border.Audio;
using Border.Core;
using UnityEngine;
using UnityEngine.VFX;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class RevolverAnimationEventReceiver : MonoBehaviour
    {
        [Serializable]
        private sealed class VfxBinding
        {
            [SerializeField] internal string id;
            [SerializeField] internal VisualEffect effect;
            [SerializeField] internal string eventName;
        }

        [SerializeField] private List<VfxBinding> vfxBindings = new();
        private readonly Dictionary<string, VfxBinding> vfxCatalog = new(StringComparer.Ordinal);

        private void Awake()
        {
            for (int i = 0; i < vfxBindings.Count; i++)
            {
                VfxBinding binding = vfxBindings[i];
                string id = binding == null ? string.Empty : Key(binding.id);

                if (string.IsNullOrEmpty(id))
                    Log.W($"[RevolverAnimationEventReceiver] VFX binding {i} has an empty ID and was ignored.", this);
                else if (binding.effect == null)
                    Log.W($"[RevolverAnimationEventReceiver] VFX binding '{id}' has no VisualEffect and was ignored.", this);
                else if (string.IsNullOrEmpty(Key(binding.eventName)))
                    Log.W($"[RevolverAnimationEventReceiver] VFX binding '{id}' has an empty event name and was ignored.", this);
                else if (vfxCatalog.ContainsKey(id))
                    Log.W($"[RevolverAnimationEventReceiver] Duplicate VFX binding ID '{id}' was ignored.", this);
                else
                {
                    binding.eventName = Key(binding.eventName);
                    vfxCatalog.Add(id, binding);
                }
            }
        }

        public void PlayVfx(string bindingId)
        {
            string id = Key(bindingId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W("[RevolverAnimationEventReceiver] Cannot play a VFX with an empty binding ID.", this);
                return;
            }
            if (!vfxCatalog.TryGetValue(id, out VfxBinding binding))
            {
                Log.W($"[RevolverAnimationEventReceiver] VFX binding ID '{id}' is not configured.", this);
                return;
            }
            if (binding.effect == null)
            {
                Log.W($"[RevolverAnimationEventReceiver] VFX binding '{id}' has no VisualEffect.", this);
                return;
            }
            binding.effect.SendEvent(binding.eventName);
        }

        public void PlaySfx(string soundId)
        {
            string id = Key(soundId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W("[RevolverAnimationEventReceiver] Cannot play an SFX with an empty sound ID.", this);
                return;
            }
            if (SoundManager.Current == null)
            {
                Log.W($"[RevolverAnimationEventReceiver] SoundManager is unavailable for SFX '{id}'.", this);
                return;
            }
            SoundManager.Current.PlaySfx(id);
        }
        private static string Key(string value) => value?.Trim() ?? string.Empty;
    }
}
