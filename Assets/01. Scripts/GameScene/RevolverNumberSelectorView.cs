using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    /// <summary>Focused 1-10 dial used for revolver predictions and lie-detector declarations.</summary>
    [DisallowMultipleComponent]
    public sealed class RevolverNumberSelectorView : MonoBehaviour
    {
        private IReadOnlyList<GameSceneCombatHudActionViewModel> _options =
            Array.Empty<GameSceneCombatHudActionViewModel>();
        private string _prompt = string.Empty;
        private int _selectedIndex;
        private GUIStyle _panelStyle;
        private GUIStyle _arrowStyle;
        private GUIStyle _numberStyle;
        private GUIStyle _promptStyle;
        private GUIStyle _confirmStyle;

        public event Action<GameSceneCombatHudCommand> CommandRequested;

        public bool IsOpen { get; private set; }

        internal int SelectedNumber => _options.Count == 0
            ? 0
            : _options[_selectedIndex].Command.OptionId;

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
            _prompt = prompt ?? string.Empty;
            _options = options;
            IsOpen = true;
            if (!wasOpen || _selectedIndex >= _options.Count)
            {
                _selectedIndex = 0;
            }
        }

        public void Hide()
        {
            IsOpen = false;
            _prompt = string.Empty;
            _options = Array.Empty<GameSceneCombatHudActionViewModel>();
            _selectedIndex = 0;
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

        private void OnGUI()
        {
            if (!IsOpen || _options.Count == 0)
            {
                return;
            }

            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 720f, 0.8f, 1.5f);
            float width = 420f * scale;
            float height = 260f * scale;
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.5f - height * 0.48f,
                width,
                height);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 34f * scale),
                _prompt,
                _promptStyle);

            float arrowSize = 72f * scale;
            float numberWidth = 130f * scale;
            float selectorY = panel.y + 67f * scale;
            if (GUI.Button(
                    new Rect(panel.x + 42f * scale, selectorY, arrowSize, arrowSize),
                    "◀",
                    _arrowStyle))
            {
                Move(-1);
            }

            GUI.Label(
                new Rect(
                    panel.center.x - numberWidth * 0.5f,
                    selectorY - 9f * scale,
                    numberWidth,
                    92f * scale),
                SelectedNumber.ToString(),
                _numberStyle);

            if (GUI.Button(
                    new Rect(
                        panel.xMax - (42f * scale) - arrowSize,
                        selectorY,
                        arrowSize,
                        arrowSize),
                    "▶",
                    _arrowStyle))
            {
                Move(1);
            }

            if (GUI.Button(
                    new Rect(
                        panel.center.x - 82f * scale,
                        panel.yMax - 62f * scale,
                        164f * scale,
                        38f * scale),
                    "확정",
                    _confirmStyle))
            {
                Confirm();
            }
        }

        private void Move(int direction)
        {
            if (_options.Count == 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + direction + _options.Count) %
                _options.Count;
        }

        private void Confirm()
        {
            if (_options.Count == 0 || !_options[_selectedIndex].IsInteractable)
            {
                return;
            }

            CommandRequested?.Invoke(_options[_selectedIndex].Command);
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            Texture2D panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.025f, 0.018f, 0.018f, 0.96f));
            panelTexture.Apply();
            panelTexture.hideFlags = HideFlags.HideAndDontSave;
            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture },
                border = new RectOffset(2, 2, 2, 2)
            };
            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = new Color(0.9f, 0.84f, 0.72f) }
            };
            _numberStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 72,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _arrowStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.16f, 0.12f) },
                hover = { textColor = Color.white }
            };
            _confirmStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                hover = { textColor = new Color(1f, 0.72f, 0.55f) }
            };
        }
    }
}
