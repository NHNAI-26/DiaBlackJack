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

        public Color WindowGlassGlowColor => windowGlassGlowColor;

        public Color VolumetricLightColor => volumetricLightColor;

        public Color EnemyLightColor => enemyLightColor;

        public Color EnteranceLightColor => enteranceLightColor;

        public bool HasValidId => !string.IsNullOrWhiteSpace(id);

        public bool HasBgmIds => CountValidBgmIds() > 0;

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
    }
}
