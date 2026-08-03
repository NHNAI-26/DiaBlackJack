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
        [SerializeField] private TMP_Text englishNameText;
        [SerializeField] private TMP_Text abilityLabelText;
        [SerializeField] private TMP_Text abilityText;
        [SerializeField] private TMP_Text costLabelText;
        [SerializeField] private TMP_Text costText;

        public bool HasRequiredReferences =>
            faceImage != null &&
            titleText != null &&
            abilityLabelText != null &&
            abilityText != null &&
            costLabelText != null &&
            costText != null;

        public bool HasEnglishNameReference => englishNameText != null;

        public void Render(
            GameSceneCombatHudContractCandidateViewModel model,
            Sprite faceSprite)
        {
            Render(
                model == null ? string.Empty : model.Title,
                model == null ? string.Empty : model.EnglishName,
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
                model == null ? string.Empty : model.EnglishName,
                model == null ? string.Empty : model.Summary,
                model == null ? string.Empty : model.CostSummary,
                faceSprite,
                model != null);
        }

        private void Render(
            string title,
            string englishName,
            string ability,
            string cost,
            Sprite faceSprite,
            bool hasModel)
        {
            if (titleText != null)
            {
                CurrencyIconText.Set(titleText, title);
            }

            if (englishNameText != null)
            {
                CurrencyIconText.Set(englishNameText, englishName);
            }

            if (abilityLabelText != null)
            {
                CurrencyIconText.Set(
                    abilityLabelText,
                    hasModel ? "ACTIVE" : string.Empty);
            }

            if (abilityText != null)
            {
                CurrencyIconText.Set(
                    abilityText,
                    hasModel ? ability : string.Empty);
            }

            if (costLabelText != null)
            {
                CurrencyIconText.Set(
                    costLabelText,
                    hasModel ? "COST" : string.Empty);
            }

            if (costText != null)
            {
                CurrencyIconText.Set(
                    costText,
                    hasModel ? cost : string.Empty);
            }

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }
        }
    }
}
