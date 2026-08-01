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
            if (button != null)
            {
                button.enabled = true;
            }

            _command = model == null ? default : model.Command;
            _isInteractable = model != null && model.IsInteractable;
            if (labelText != null)
            {
                CurrencyIconText.Set(
                    labelText,
                    model == null ? string.Empty : model.Label);
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
