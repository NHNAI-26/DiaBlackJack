using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class GameHudChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image faceImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text abilityText;
        [SerializeField] private TMP_Text costText;

        private GameSceneCombatHudCommand _command;
        private bool _isInteractable;

        public event Action<GameSceneCombatHudCommand> CommandRequested;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Render(GameSceneCombatHudActionViewModel model)
        {
            _command = model == null ? default : model.Command;
            _isInteractable = model != null && model.IsInteractable;
            if (labelText != null)
            {
                labelText.text = model == null ? string.Empty : model.Label;
            }

            if (button != null)
            {
                button.interactable = _isInteractable;
            }
        }

        public void RenderContractCandidate(
            GameSceneCombatHudContractCandidateViewModel model,
            Sprite faceSprite)
        {
            _command = model == null ? default : model.Command;
            _isInteractable = model != null && model.IsInteractable;
            if (titleText != null)
            {
                titleText.text = model == null ? string.Empty : model.Title;
            }

            if (abilityText != null)
            {
                abilityText.text = model == null ? string.Empty : model.Ability;
            }

            if (costText != null)
            {
                costText.text = model == null ? string.Empty : model.Cost;
            }

            if (labelText != null)
            {
                labelText.text = model == null ? string.Empty : model.ButtonLabel;
            }

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }

            if (button != null)
            {
                button.interactable = _isInteractable;
            }
        }

        private void HandleClick()
        {
            if (_isInteractable)
            {
                CommandRequested?.Invoke(_command);
            }
        }
    }
}
