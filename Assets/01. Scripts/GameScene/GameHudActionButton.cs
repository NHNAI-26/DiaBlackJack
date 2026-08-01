using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class GameHudActionButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;

        private GameSceneCombatHudActionViewModel _model;

        public event Action<GameSceneCombatHudCommand> CommandRequested;
        public event Action<GameHudActionButton, bool> HoverChanged;

        public RectTransform RectTransform => transform as RectTransform;

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
            _model = model;
            if (labelText != null)
            {
                CurrencyIconText.Set(
                    labelText,
                    model == null ? string.Empty : model.Label);
            }

            if (button != null)
            {
                button.interactable = model != null && model.IsInteractable;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_model != null && !string.IsNullOrEmpty(_model.Tooltip))
            {
                HoverChanged?.Invoke(this, true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverChanged?.Invoke(this, false);
        }

        public string Tooltip => _model == null ? string.Empty : _model.Tooltip;

        private void HandleClick()
        {
            if (_model != null && _model.IsInteractable)
            {
                CommandRequested?.Invoke(_model.Command);
            }
        }
    }
}
