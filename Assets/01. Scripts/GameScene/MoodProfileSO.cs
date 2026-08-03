using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [CreateAssetMenu(
        fileName = "MoodProfile",
        menuName = "DiaBlackJack/Presentation/Mood Profile")]
    public sealed class MoodProfileSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private List<string> bgmIds = new List<string>();
        [Header("Audio Reactive Lightning")]
        [SerializeField] private bool enableAudioReactiveLightning;
        [SerializeField] private List<string> lightningSfxIds =
            new List<string>();
        [SerializeField, Range(0f, 1f)] private float lightningSfxPlayChance =
            0.35f;
        [SerializeField, Min(0.1f)] private float lightningSfxInterval = 1f;
        [SerializeField, Min(0f)] private float lightningSensitivity = 35f;
        [SerializeField, Range(0f, 1f)] private float lightningThreshold = 0.08f;
        [SerializeField, Min(0f)] private float lightningMaxBoost = 0.8f;
        [SerializeField, Min(0f)] private float lightningAttackSpeed = 12f;
        [SerializeField, Min(0f)] private float lightningReleaseSpeed = 4f;
        [ColorUsage(false, true)]
        [SerializeField] private Color windowGlassGlowColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color volumetricLightColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color enemyLightColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color enteranceLightColor = Color.white;

        public string Id => id;

        public IReadOnlyList<string> BgmIds => bgmIds;

        public bool EnableAudioReactiveLightning => enableAudioReactiveLightning;

        public IReadOnlyList<string> LightningSfxIds => lightningSfxIds;

        public float LightningSfxPlayChance => lightningSfxPlayChance;

        public float LightningSfxInterval => lightningSfxInterval;

        public float LightningSensitivity => lightningSensitivity;

        public float LightningThreshold => lightningThreshold;

        public float LightningMaxBoost => lightningMaxBoost;

        public float LightningAttackSpeed => lightningAttackSpeed;

        public float LightningReleaseSpeed => lightningReleaseSpeed;

        public Color WindowGlassGlowColor => windowGlassGlowColor;

        public Color VolumetricLightColor => volumetricLightColor;

        public Color EnemyLightColor => enemyLightColor;

        public Color EnteranceLightColor => enteranceLightColor;

        public bool HasValidId => !string.IsNullOrWhiteSpace(id);

        public bool HasBgmIds => CountValidBgmIds() > 0;

        public bool HasLightningSfxIds => CountValidLightningSfxIds() > 0;

        public bool TryGetRandomBgmId(out string bgmId)
        {
            int validCount = CountValidBgmIds();
            if (validCount <= 0)
            {
                bgmId = null;
                return false;
            }

            int targetIndex = Random.Range(0, validCount);
            int currentIndex = 0;
            foreach (string candidate in bgmIds)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    bgmId = candidate;
                    return true;
                }

                currentIndex++;
            }

            bgmId = null;
            return false;
        }

        public bool TryGetRandomLightningSfxId(out string sfxId)
        {
            int validCount = CountValidLightningSfxIds();
            if (validCount <= 0)
            {
                sfxId = null;
                return false;
            }

            int targetIndex = Random.Range(0, validCount);
            int currentIndex = 0;
            foreach (string candidate in lightningSfxIds)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    sfxId = candidate;
                    return true;
                }

                currentIndex++;
            }

            sfxId = null;
            return false;
        }

        private void OnValidate()
        {
            if ((hideFlags & HideFlags.DontSave) != 0 || HasValidId)
            {
                return;
            }

            Debug.LogError(
                $"Mood profile asset '{name}' requires a non-empty id.",
                this);
        }

        private int CountValidBgmIds()
        {
            if (bgmIds == null)
            {
                return 0;
            }

            int validCount = 0;
            foreach (string candidate in bgmIds)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    validCount++;
                }
            }

            return validCount;
        }

        private int CountValidLightningSfxIds()
        {
            if (lightningSfxIds == null)
            {
                return 0;
            }

            int validCount = 0;
            foreach (string candidate in lightningSfxIds)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    validCount++;
                }
            }

            return validCount;
        }
    }
}
