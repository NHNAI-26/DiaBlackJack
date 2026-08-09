using System;
using System.Collections.Generic;
using Border.Audio;
using Border.Settings;
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
        private const string ButtonPressSfxId = "buttonPress";

        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button confirmButton;

        private IReadOnlyList<GameSceneCombatHudActionViewModel> _options =
            Array.Empty<GameSceneCombatHudActionViewModel>();
        private int _selectedIndex;
        private int? _tutorialTargetNumber;

        public event Action<GameSceneCombatHudCommand> CommandRequested;

        public bool IsOpen { get; private set; }

        internal bool HasRequiredReferences =>
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
            RefreshSelection();
        }

        public void Hide()
        {
            IsOpen = false;
            _options = Array.Empty<GameSceneCombatHudActionViewModel>();
            _selectedIndex = 0;
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

        /// <summary>
        /// Tutorial-only override: the dial still starts at its lowest option and the player
        /// can freely navigate it (buttons, arrow keys, scroll all stay live) — but Confirm
        /// stays disabled until the selection actually lands on <paramref name="number"/>. Not
        /// cleared automatically by <see cref="Hide"/>; the caller (the tutorial director) owns
        /// turning it back off once the scripted beat is past. Pass null to lift.
        /// </summary>
        internal void SetTutorialTargetNumber(int? number)
        {
            _tutorialTargetNumber = number;
            if (IsOpen)
            {
                RefreshSelection();
            }
        }

        private void Update()
        {
            if (!IsOpen || PauseSettingsController.IsGameplayInputBlocked)
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
                    Move(-1, playSound: true);
                }
                else if (keyboard.rightArrowKey.wasPressedThisFrame ||
                    keyboard.dKey.wasPressedThisFrame ||
                    keyboard.upArrowKey.wasPressedThisFrame)
                {
                    Move(1, playSound: true);
                }

                if (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame)
                {
                    Confirm(playSound: true);
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
                Move(1, playSound: true);
            }
            else if (scroll < -0.01f)
            {
                Move(-1, playSound: true);
            }
        }

        private void SelectPrevious()
        {
            Move(-1, playSound: false);
            ClearSelectedButton();
        }

        private void SelectNext()
        {
            Move(1, playSound: false);
            ClearSelectedButton();
        }

        private void Move(int direction, bool playSound)
        {
            if (_options.Count == 0)
            {
                return;
            }

            int previousIndex = _selectedIndex;
            _selectedIndex = (_selectedIndex + direction + _options.Count) %
                _options.Count;
            if (playSound && _selectedIndex != previousIndex)
            {
                SoundManager.Current?.PlaySfx(ButtonPressSfxId);
            }

            RefreshSelection();
        }

        private void Confirm()
        {
            Confirm(playSound: false);
        }

        private void Confirm(bool playSound)
        {
            ClearSelectedButton();
            if (_options.Count == 0 ||
                !_options[_selectedIndex].IsInteractable ||
                (_tutorialTargetNumber.HasValue &&
                    SelectedNumber != _tutorialTargetNumber.Value))
            {
                return;
            }

            if (playSound)
            {
                SoundManager.Current?.PlaySfx(ButtonPressSfxId);
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
                    _options.Count > 0 &&
                    _options[_selectedIndex].IsInteractable &&
                    (!_tutorialTargetNumber.HasValue ||
                        SelectedNumber == _tutorialTargetNumber.Value);
            }
        }

        private static void ClearSelectedButton()
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
