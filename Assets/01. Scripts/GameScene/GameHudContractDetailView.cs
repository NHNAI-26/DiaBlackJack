using DiaBlackJack.CoreLoop.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class GameHudContractDetailView : MonoBehaviour
    {
        [SerializeField] private Image faceImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text abilityText;
        [SerializeField] private TMP_Text costText;

        public bool HasRequiredReferences =>
            faceImage != null &&
            titleText != null &&
            abilityText != null &&
            costText != null;

        public void Render(
            GameSceneCombatHudContractCandidateViewModel model,
            Sprite faceSprite)
        {
            Render(
                model == null ? string.Empty : model.Title,
                model == null ? string.Empty : model.Ability,
                model == null ? string.Empty : model.Cost,
                faceSprite,
                model != null);
        }

        public void Render(
            GameSceneDemonCardViewModel model,
            Sprite faceSprite)
        {
            Render(
                model == null ? string.Empty : model.DisplayName,
                model == null ? string.Empty : model.Summary,
                model == null ? string.Empty : model.CostSummary,
                faceSprite,
                model != null);
        }

        private void Render(
            string title,
            string ability,
            string cost,
            Sprite faceSprite,
            bool hasModel)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (abilityText != null)
            {
                abilityText.text = hasModel
                    ? "<color=#D34B3F><b>ACTIVE</b></color>\n" + ability
                    : string.Empty;
            }

            if (costText != null)
            {
                costText.text = hasModel
                    ? "<color=#D7A53B><b>COST</b></color>\n" + cost
                    : string.Empty;
            }

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }
        }
    }
}
