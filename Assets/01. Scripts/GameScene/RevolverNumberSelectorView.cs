using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>Authored 1-10 dial used for revolver predictions and lie-detector declarations.</summary>
    [DisallowMultipleComponent]
    public sealed class RevolverNumberSelectorView : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button confirmButton;

        private IReadOnlyList<GameSceneCombatHudActionViewModel> _options =
            Array.Empty<GameSceneCombatHudActionViewModel>();
        private int _selectedIndex;

        public event Action<GameSceneCombatHudCommand> CommandRequested;

        public bool IsOpen { get; private set; }

        internal bool HasRequiredReferences =>
            promptText != null &&
            numberText != null &&
            previousButton != null &&
            nextButton != null &&
            confirmButton != null;

        internal int SelectedNumber => _options.Count == 0
            ? 0
            : _options[_selectedIndex].Command.OptionId;

        internal Button PreviousButton => previousButton;
        internal Button NextButton => nextButton;
        internal Button ConfirmButton => confirmButton;

        private void Awake()
        {
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(SelectPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(SelectNext);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(Confirm);
            }
        }

        private void OnDestroy()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(SelectPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(SelectNext);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
            }
        }

        public void Render(
            string prompt,
            IReadOnlyList<GameSceneCombatHudActionViewModel> options)
        {
            if (options == null || options.Count == 0)
            {
                Hide();
                return;
            }

            bool wasOpen = IsOpen;
            _options = options;
            IsOpen = true;
            if (!wasOpen || _selectedIndex >= _options.Count)
            {
                _selectedIndex = 0;
            }

            gameObject.SetActive(true);
            if (promptText != null)
            {
                CurrencyIconText.Set(promptText, prompt ?? string.Empty);
            }

            RefreshSelection();
        }

        public void Hide()
        {
            IsOpen = false;
            _options = Array.Empty<GameSceneCombatHudActionViewModel>();
            _selectedIndex = 0;
            if (promptText != null)
            {
                promptText.text = string.Empty;
            }

            if (numberText != null)
            {
                numberText.text = string.Empty;
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame ||
                    keyboard.aKey.wasPressedThisFrame ||
                    keyboard.downArrowKey.wasPressedThisFrame)
                {
                    Move(-1);
                }
                else if (keyboard.rightArrowKey.wasPressedThisFrame ||
                    keyboard.dKey.wasPressedThisFrame ||
                    keyboard.upArrowKey.wasPressedThisFrame)
                {
                    Move(1);
                }

                if (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame)
                {
                    Confirm();
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.01f)
            {
                Move(1);
            }
            else if (scroll < -0.01f)
            {
                Move(-1);
            }
        }

        private void SelectPrevious()
        {
            Move(-1);
            ClearSelectedButton();
        }

        private void SelectNext()
        {
            Move(1);
            ClearSelectedButton();
        }

        private void Move(int direction)
        {
            if (_options.Count == 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + direction + _options.Count) %
                _options.Count;
            RefreshSelection();
        }

        private void Confirm()
        {
            ClearSelectedButton();
            if (_options.Count == 0 || !_options[_selectedIndex].IsInteractable)
            {
                return;
            }

            CommandRequested?.Invoke(_options[_selectedIndex].Command);
        }

        private void RefreshSelection()
        {
            if (numberText != null)
            {
                numberText.text = SelectedNumber.ToString();
            }

            bool canNavigate = _options.Count > 1;
            if (previousButton != null)
            {
                previousButton.interactable = canNavigate;
            }

            if (nextButton != null)
            {
                nextButton.interactable = canNavigate;
            }

            if (confirmButton != null)
            {
                confirmButton.interactable =
                    _options.Count > 0 && _options[_selectedIndex].IsInteractable;
            }
        }

        private static void ClearSelectedButton()
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
