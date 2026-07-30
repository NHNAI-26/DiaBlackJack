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
            if (titleText != null)
            {
                titleText.text = model == null ? string.Empty : model.Title;
            }

            if (abilityText != null)
            {
                abilityText.text = model == null
                    ? string.Empty
                    : "<color=#D34B3F><b>ACTIVE</b></color>\n" + model.Ability;
            }

            if (costText != null)
            {
                costText.text = model == null
                    ? string.Empty
                    : "<color=#D7A53B><b>COST</b></color>\n" + model.Cost;
            }

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }
        }
    }
}
