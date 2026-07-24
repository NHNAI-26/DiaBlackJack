using System;
using System.Collections;
using System.Collections.Generic;
using Border.Core;
using Border.Events;
using UnityEngine;

namespace Border.Audio
{
    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour
    {
        private const float MinPitch = 0.01f;
        private const float MaxPitch = 3f;
        [Serializable]
        public sealed class SoundEntry
        {
            [SerializeField] internal string id;
            [SerializeField] internal AudioClip clip;
            [SerializeField, Range(0f, 1f)] internal float volume = 1f;
            [SerializeField, Range(MinPitch, MaxPitch)] internal float pitch = 1f;
            [SerializeField] internal bool loop;
        }
        public readonly struct SoundHandle
        {
            internal readonly SoundManager owner;
            internal readonly int index;
            internal readonly uint generation;

            internal SoundHandle(SoundManager owner, int index, uint generation)
            {
                this.owner = owner;
                this.index = index;
                this.generation = generation;
            }
        }
        private sealed class Voice
        {
            public AudioSource Source;
            public float BaseVolume, Fade = 1f;
            public ulong Started;
            public uint Generation;
        }
        [Header("Catalogs")]
        [SerializeField] private List<SoundEntry> bgmEntries = new();
        [SerializeField] private List<SoundEntry> sfxEntries = new();
        [Header("Playback")]
        [SerializeField, Min(0f)] private float crossfadeDuration = 1f;
        [SerializeField, Min(1)] private int maxSfxVoices = 16;
        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f, musicVolume = 1f, sfxVolume = 1f;
        [Header("Listening on")]
        [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent, changeMusicVolumeEvent, changeSfxVolumeEvent;
        private readonly Dictionary<string, SoundEntry> bgmCatalog = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SoundEntry> sfxCatalog = new(StringComparer.Ordinal);
        private Voice[] bgmVoices, sfxVoices;
        private Coroutine fadeRoutine;
        private string currentBgmId;
        private int activeBgm = -1;
        private ulong playOrder;
        public static SoundManager Current { get; private set; }
        private void Awake()
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            Register(bgmEntries, bgmCatalog, ids, "BGM");
            Register(sfxEntries, sfxCatalog, ids, "SFX");
            bgmVoices = new[] { CreateVoice(), CreateVoice() };
            sfxVoices = new Voice[Mathf.Max(1, maxSfxVoices)];
            for (int i = 0; i < sfxVoices.Length; i++)
                sfxVoices[i] = CreateVoice();
        }
        private void OnEnable()
        {
            if (Current != null && Current != this)
            {
                Log.W("[SoundManager] Another scene-local manager is active; this component was disabled.", this);
                enabled = false;
                return;
            }
            Current = this;
            if (changeMasterVolumeEvent != null) changeMasterVolumeEvent.OnEventRaised += SetMasterVolume;
            if (changeMusicVolumeEvent != null) changeMusicVolumeEvent.OnEventRaised += SetMusicVolume;
            if (changeSfxVolumeEvent != null) changeSfxVolumeEvent.OnEventRaised += SetSfxVolume;
        }
        private void OnDisable()
        {
            if (changeMasterVolumeEvent != null) changeMasterVolumeEvent.OnEventRaised -= SetMasterVolume;
            if (changeMusicVolumeEvent != null) changeMusicVolumeEvent.OnEventRaised -= SetMusicVolume;
            if (changeSfxVolumeEvent != null) changeSfxVolumeEvent.OnEventRaised -= SetSfxVolume;
            if (ReferenceEquals(Current, this))
                Current = null;
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            if (bgmVoices != null)
                foreach (Voice voice in bgmVoices) voice.Source.Stop();
            if (sfxVoices != null)
                foreach (Voice voice in sfxVoices) voice.Source.Stop();
            activeBgm = -1; currentBgmId = null;
        }
        public bool PlayBgm(string id, bool restart = false)
        {
            string key = Key(id);
            if (!bgmCatalog.TryGetValue(key, out SoundEntry entry))
            {
                WarnMissing("BGM", id);
                return false;
            }
            if (!restart && currentBgmId == key && activeBgm >= 0 &&
                bgmVoices[activeBgm].Source.isPlaying)
                return true;
            int target = activeBgm < 0 ? 0 : 1 - activeBgm;
            Configure(bgmVoices[target], entry, 0f, true);
            bgmVoices[target].Source.Play();
            activeBgm = target;
            currentBgmId = key;
            StartFade(target);
            return true;
        }
        public void StopBgm() { if (activeBgm >= 0) { currentBgmId = null; StartFade(-1); } }
        public SoundHandle PlaySfx(string id)
        {
            string key = Key(id);
            if (!sfxCatalog.TryGetValue(key, out SoundEntry entry))
            {
                WarnMissing("SFX", id);
                return default;
            }
            int index = FindSfxVoice();
            if (index < 0)
            {
                Log.W($"[SoundManager] SFX pool is full; active loops were preserved while playing '{key}'.", this);
                return default;
            }
            Voice voice = sfxVoices[index];
            voice.Source.Stop();
            voice.Generation++;
            voice.Started = ++playOrder;
            Configure(voice, entry, 1f, false);
            voice.Source.Play();
            return new SoundHandle(this, index, voice.Generation);
        }
        public bool StopSfx(SoundHandle handle)
        {
            if (handle.owner != this || handle.index < 0 || handle.index >= sfxVoices.Length)
                return false;
            Voice voice = sfxVoices[handle.index];
            if (voice.Generation != handle.generation || !voice.Source.isPlaying)
                return false;
            voice.Source.Stop();
            return true;
        }
        public void SetMasterVolume(float value) { masterVolume = Gain(value); RefreshVolumes(); }
        public void SetMusicVolume(float value) { musicVolume = Gain(value); RefreshVolumes(); }
        public void SetSfxVolume(float value) { sfxVolume = Gain(value); RefreshVolumes(); }
        private void Register(List<SoundEntry> entries, Dictionary<string, SoundEntry> catalog,
            HashSet<string> ids, string category)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SoundEntry entry = entries[i];
                string id = entry == null ? string.Empty : Key(entry.id);
                if (string.IsNullOrEmpty(id))
                    Log.W($"[SoundManager] {category} entry {i} has an empty ID and was ignored.", this);
                else if (!ids.Add(id))
                    Log.W($"[SoundManager] Duplicate sound ID '{id}' was ignored.", this);
                else if (entry.clip == null)
                    Log.W($"[SoundManager] Sound '{id}' has no AudioClip and was ignored.", this);
                else if (float.IsNaN(entry.pitch) || float.IsInfinity(entry.pitch) ||
                         entry.pitch < MinPitch || entry.pitch > MaxPitch)
                    Log.W($"[SoundManager] Sound '{id}' has invalid pitch {entry.pitch} and was ignored.", this);
                else
                    catalog.Add(id, entry);
            }
        }
        private Voice CreateVoice()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return new Voice { Source = source };
        }
        private void Configure(Voice voice, SoundEntry entry, float fade, bool music)
        {
            voice.Source.clip = entry.clip;
            voice.Source.pitch = entry.pitch;
            voice.Source.loop = entry.loop;
            voice.BaseVolume = Gain(entry.volume);
            voice.Fade = fade;
            ApplyVolume(voice, music);
        }
        private int FindSfxVoice()
        {
            int oldest = -1;
            for (int i = 0; i < sfxVoices.Length; i++)
            {
                Voice voice = sfxVoices[i];
                if (!voice.Source.isPlaying)
                    return i;
                if (!voice.Source.loop && (oldest < 0 || voice.Started < sfxVoices[oldest].Started))
                    oldest = i;
            }
            return oldest;
        }
        private void StartFade(int target)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeBgm(target));
        }
        private IEnumerator FadeBgm(int target)
        {
            float start0 = bgmVoices[0].Fade;
            float start1 = bgmVoices[1].Fade;
            float elapsed = 0f;
            float duration = Mathf.Max(0f, crossfadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                bgmVoices[0].Fade = Mathf.Lerp(start0, target == 0 ? 1f : 0f, t);
                bgmVoices[1].Fade = Mathf.Lerp(start1, target == 1 ? 1f : 0f, t);
                RefreshBgmVolumes();
                yield return null;
            }
            for (int i = 0; i < bgmVoices.Length; i++)
            {
                bgmVoices[i].Fade = i == target ? 1f : 0f;
                if (i != target)
                    bgmVoices[i].Source.Stop();
            }
            if (target < 0)
            {
                activeBgm = -1;
                currentBgmId = null;
            }
            RefreshBgmVolumes();
            fadeRoutine = null;
        }
        private void RefreshVolumes()
        {
            RefreshBgmVolumes();
            for (int i = 0; i < sfxVoices.Length; i++)
                ApplyVolume(sfxVoices[i], false);
        }
        private void RefreshBgmVolumes()
        {
            for (int i = 0; i < bgmVoices.Length; i++)
                ApplyVolume(bgmVoices[i], true);
        }
        private void ApplyVolume(Voice voice, bool music) =>
            voice.Source.volume = voice.BaseVolume * voice.Fade * masterVolume *
                                  (music ? musicVolume : sfxVolume);
        private static float Gain(float value) => float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        private static string Key(string id) => id?.Trim() ?? string.Empty;
        private void WarnMissing(string category, string id) =>
            Log.W($"[SoundManager] {category} ID '{id ?? "<null>"}' is not configured.", this);
    }
}
