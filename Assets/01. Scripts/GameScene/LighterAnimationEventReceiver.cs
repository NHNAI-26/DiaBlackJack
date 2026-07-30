using System;
using System.Collections.Generic;
using Border.Audio;
using Border.Core;
using UnityEngine;
using UnityEngine.VFX;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class LighterAnimationEventReceiver : MonoBehaviour
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

        [Header("Fire Shader")]
        [SerializeField] private Renderer fireRenderer;

        private static readonly int IgnitionRevealId = Shader.PropertyToID("_IgnitionReveal");
        private static readonly int IgnitionRevealHeightId = Shader.PropertyToID("_IgnitionRevealHeight");

        private readonly Dictionary<string, VfxBinding> vfxCatalog =
            new(StringComparer.Ordinal);

        private MaterialPropertyBlock firePropertyBlock;

        private void Awake()
        {
            firePropertyBlock = new MaterialPropertyBlock();
            fireRenderer ??= transform.Find("fire")?.GetComponent<Renderer>();
            BuildVfxCatalog();
        }

        public void PlayVfx(string bindingId)
        {
            string id = Key(bindingId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W("[LighterAnimationEventReceiver] Cannot play a VFX with an empty binding ID.", this);
                return;
            }

            if (!vfxCatalog.TryGetValue(id, out VfxBinding binding))
            {
                Log.W($"[LighterAnimationEventReceiver] VFX binding ID '{id}' is not configured.", this);
                return;
            }

            if (binding.effect == null)
            {
                Log.W($"[LighterAnimationEventReceiver] VFX binding '{id}' has no VisualEffect.", this);
                return;
            }

            binding.effect.SendEvent(binding.eventName);
        }

        public void StopVfx(string bindingId)
        {
            string id = Key(bindingId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W("[LighterAnimationEventReceiver] Cannot stop a VFX with an empty binding ID.", this);
                return;
            }

            if (!vfxCatalog.TryGetValue(id, out VfxBinding binding))
            {
                Log.W($"[LighterAnimationEventReceiver] VFX binding ID '{id}' is not configured.", this);
                return;
            }

            if (binding.effect == null)
            {
                Log.W($"[LighterAnimationEventReceiver] VFX binding '{id}' has no VisualEffect.", this);
                return;
            }

            binding.effect.Stop();
        }

        public void PlaySfx(string soundId)
        {
            string id = Key(soundId);
            if (string.IsNullOrEmpty(id))
            {
                Log.W("[LighterAnimationEventReceiver] Cannot play an SFX with an empty sound ID.", this);
                return;
            }

            if (SoundManager.Current == null)
            {
                Log.W($"[LighterAnimationEventReceiver] SoundManager is unavailable for SFX '{id}'.", this);
                return;
            }

            SoundManager.Current.PlaySfx(id);
        }

        public void SetFireRevealHeight(float height)
        {
            SetFireReveal(1f, height);
        }

        public void EnableFireReveal()
        {
            SetFireReveal(1f, 0f);
        }

        public void DisableFireReveal()
        {
            SetFireReveal(0f, 1f);
        }

        public void HideFire()
        {
            if (ResolveFireRenderer() != null)
                fireRenderer.enabled = false;
        }

        public void ShowFire()
        {
            if (ResolveFireRenderer() != null)
                fireRenderer.enabled = true;
        }

        private void SetFireReveal(float enabled, float height)
        {
            if (ResolveFireRenderer() == null)
                return;

            firePropertyBlock ??= new MaterialPropertyBlock();
            fireRenderer.GetPropertyBlock(firePropertyBlock);
            firePropertyBlock.SetFloat(IgnitionRevealId, Mathf.Clamp01(enabled));
            firePropertyBlock.SetFloat(IgnitionRevealHeightId, Mathf.Clamp01(height));
            fireRenderer.SetPropertyBlock(firePropertyBlock);
        }

        private Renderer ResolveFireRenderer()
        {
            if (fireRenderer != null)
                return fireRenderer;

            fireRenderer = transform.Find("fire")?.GetComponent<Renderer>();
            if (fireRenderer == null)
                Log.W("[LighterAnimationEventReceiver] Fire renderer is not assigned.", this);

            return fireRenderer;
        }

        private void BuildVfxCatalog()
        {
            for (int i = 0; i < vfxBindings.Count; i++)
            {
                VfxBinding binding = vfxBindings[i];
                string id = binding == null ? string.Empty : Key(binding.id);

                if (string.IsNullOrEmpty(id))
                    Log.W($"[LighterAnimationEventReceiver] VFX binding {i} has an empty ID and was ignored.", this);
                else if (binding.effect == null)
                    Log.W($"[LighterAnimationEventReceiver] VFX binding '{id}' has no VisualEffect and was ignored.", this);
                else if (string.IsNullOrEmpty(Key(binding.eventName)))
                    Log.W($"[LighterAnimationEventReceiver] VFX binding '{id}' has an empty event name and was ignored.", this);
                else if (vfxCatalog.ContainsKey(id))
                    Log.W($"[LighterAnimationEventReceiver] Duplicate VFX binding ID '{id}' was ignored.", this);
                else
                {
                    binding.eventName = Key(binding.eventName);
                    vfxCatalog.Add(id, binding);
                }
            }
        }

        private static string Key(string value) => value?.Trim() ?? string.Empty;
    }
}
