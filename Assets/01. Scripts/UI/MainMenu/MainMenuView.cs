using System;
using Border.SaveLoad.UI;
using UnityEngine;

namespace DiaBlackJack.MainMenu.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        private const float PanelWidth = 460f;
        private const float ButtonHeight = 52f;

        private RunSaveViewModel _model;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _titleStyle;
        private Texture2D _backgroundTexture;
        private Texture2D _panelTexture;

        public event Action CancelNewRunRequested;

        public event Action ContinueRunRequested;

        public event Action ExitRequested;

        public event Action NewRunConfirmed;

        public event Action NewRunRequested;

        public event Action ResumeReservationRequested;

        public void Render(RunSaveViewModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        private void OnDestroy()
        {
            DestroyTexture(_backgroundTexture);
            DestroyTexture(_panelTexture);
        }

        private void OnGUI()
        {
            if (_model == null)
            {
                return;
            }

            EnsureStyles();
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                _backgroundTexture,
                ScaleMode.StretchToFill);

            float panelHeight = _model.RequiresNewRunConfirmation
                ? 430f
                : 510f;
            Rect panel = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                PanelWidth,
                panelHeight);
            GUI.DrawTexture(panel, _panelTexture, ScaleMode.StretchToFill);

            GUILayout.BeginArea(new Rect(
                panel.x + 36f,
                panel.y + 32f,
                panel.width - 72f,
                panel.height - 64f));
            GUILayout.Label("DIA BLACKJACK", _titleStyle);
            GUILayout.Label("SOULS ON THE TABLE", _subtitleStyle);
            GUILayout.Space(30f);

            if (_model.RequiresNewRunConfirmation)
            {
                DrawConfirmation();
            }
            else
            {
                DrawMainActions();
            }

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_model.StatusMessage))
            {
                GUILayout.Label(_model.StatusMessage, _statusStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawMainActions()
        {
            if (GUILayout.Button(
                    "NEW RUN",
                    _buttonStyle,
                    GUILayout.Height(ButtonHeight)))
            {
                NewRunRequested?.Invoke();
            }

            GUILayout.Space(10f);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = _model.CanContinueRun;
            if (GUILayout.Button(
                    "CONTINUE",
                    _buttonStyle,
                    GUILayout.Height(ButtonHeight)))
            {
                ContinueRunRequested?.Invoke();
            }

            GUI.enabled = previousEnabled;

            if (_model.CanResumeReservation)
            {
                GUILayout.Space(10f);
                if (GUILayout.Button(
                        "RESUME START",
                        _buttonStyle,
                        GUILayout.Height(ButtonHeight)))
                {
                    ResumeReservationRequested?.Invoke();
                }
            }

            GUILayout.Space(10f);
            previousEnabled = GUI.enabled;
            GUI.enabled = false;
            GUILayout.Button(
                "SETTINGS (LATER)",
                _buttonStyle,
                GUILayout.Height(ButtonHeight));
            GUI.enabled = previousEnabled;

            GUILayout.Space(10f);
            if (GUILayout.Button(
                    "EXIT",
                    _buttonStyle,
                    GUILayout.Height(ButtonHeight)))
            {
                ExitRequested?.Invoke();
            }
        }

        private void DrawConfirmation()
        {
            GUILayout.Label(
                "A saved run already exists.\nStarting a new run will replace it.",
                _statusStyle);
            GUILayout.Space(24f);
            if (GUILayout.Button(
                    "START NEW RUN",
                    _buttonStyle,
                    GUILayout.Height(ButtonHeight)))
            {
                NewRunConfirmed?.Invoke();
            }

            GUILayout.Space(10f);
            if (GUILayout.Button(
                    "CANCEL",
                    _buttonStyle,
                    GUILayout.Height(ButtonHeight)))
            {
                CancelNewRunRequested?.Invoke();
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _backgroundTexture = CreateTexture(
                new Color(0.018f, 0.012f, 0.012f, 1f));
            _panelTexture = CreateTexture(
                new Color(0.08f, 0.055f, 0.05f, 0.97f));
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.86f, 0.72f, 0.48f) }
            };
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.55f, 0.42f, 0.34f) }
            };
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.76f, 0.68f, 0.6f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
