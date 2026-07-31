using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [CreateAssetMenu(
        fileName = "MoodProfile",
        menuName = "DiaBlackJack/Presentation/Mood Profile")]
    public sealed class MoodProfileSO : ScriptableObject
    {
        [SerializeField] private string id;
        [ColorUsage(false, true)]
        [SerializeField] private Color windowGlassGlowColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color volumetricLightColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color enemyLightColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color enteranceLightColor = Color.white;

        public string Id => id;

        public Color WindowGlassGlowColor => windowGlassGlowColor;

        public Color VolumetricLightColor => volumetricLightColor;

        public Color EnemyLightColor => enemyLightColor;

        public Color EnteranceLightColor => enteranceLightColor;

        public bool HasValidId => !string.IsNullOrWhiteSpace(id);

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
    }
}
