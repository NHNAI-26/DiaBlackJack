using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Settings
{
    [DisallowMultipleComponent]
    public sealed class UISettingsArrowSelector : MonoBehaviour
    {
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private TMP_Text valueText;

        private readonly List<string> _options = new List<string>();
        private int _index;

        public int Index => _index;
        public event Action<int> ValueChanged;

        private void Awake()
        {
            if (leftButton != null)
            {
                leftButton.onClick.AddListener(SelectPrevious);
            }

            if (rightButton != null)
            {
                rightButton.onClick.AddListener(SelectNext);
            }
        }

        private void OnDestroy()
        {
            if (leftButton != null)
            {
                leftButton.onClick.RemoveListener(SelectPrevious);
            }

            if (rightButton != null)
            {
                rightButton.onClick.RemoveListener(SelectNext);
            }
        }

        public void SetOptions(
            IReadOnlyList<string> options,
            int selectedIndex)
        {
            _options.Clear();
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    _options.Add(options[i] ?? string.Empty);
                }
            }

            SetIndex(selectedIndex, false);
        }

        public void SetIndex(int index, bool notify)
        {
            _index = _options.Count == 0
                ? 0
                : WrapIndex(index, _options.Count);
            RefreshText();
            if (notify)
            {
                ValueChanged?.Invoke(_index);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (leftButton != null)
            {
                leftButton.interactable = interactable;
            }

            if (rightButton != null)
            {
                rightButton.interactable = interactable;
            }
        }

        public void SetDisplayText(string text)
        {
            if (valueText != null)
            {
                valueText.text = text ?? string.Empty;
            }
        }

        internal static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private void SelectPrevious()
        {
            SetIndex(_index - 1, true);
        }

        private void SelectNext()
        {
            SetIndex(_index + 1, true);
        }

        private void RefreshText()
        {
            if (valueText == null)
            {
                return;
            }

            valueText.text = _options.Count == 0
                ? string.Empty
                : _options[_index];
        }
    }
}
