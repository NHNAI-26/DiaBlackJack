using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class RunResultView : MonoBehaviour
    {
        private const float PanelWidth = 520f;
        private const float ButtonHeight = 50f;

        private RunResultViewModel _model;
        private GUIStyle _buttonStyle;
        private GUIStyle _detailStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _summaryStyle;
        private GUIStyle _titleStyle;
        private Texture2D _backdropTexture;
        private Texture2D _panelTexture;

        public event Action MainMenuRequested;
        public event Action RestartRequested;
        public event Action SaveRetryRequested;

        public void Render(RunResultViewModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Hide()
        {
            _model = null;
        }

        private void OnDestroy()
        {
            DestroyTexture(_backdropTexture);
            DestroyTexture(_panelTexture);
        }

        private void OnGUI()
        {
            if (_model == null || !_model.IsVisible)
            {
                return;
            }

            EnsureStyles();
            _titleStyle.normal.textColor = _model.IsVictory
                ? new Color(0.88f, 0.72f, 0.42f)
                : new Color(0.72f, 0.15f, 0.12f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                _backdropTexture,
                ScaleMode.StretchToFill);
            float panelHeight = _model.CanRetrySave ? 460f : 500f;
            Rect panel = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                PanelWidth,
                panelHeight);
            GUI.DrawTexture(panel, _panelTexture, ScaleMode.StretchToFill);

            GUILayout.BeginArea(new Rect(
                panel.x + 42f,
                panel.y + 36f,
                panel.width - 84f,
                panel.height - 72f));
            GUILayout.Label(_model.Title, _titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label(
                CurrencyIconGui.Content(_model.Summary),
                _summaryStyle);
            GUILayout.Space(26f);
            GUILayout.Label(
                CurrencyIconGui.Soul(_model.PlayerSoul),
                _detailStyle);
            GUILayout.Label(
                CurrencyIconGui.Gold(_model.PlayerGold),
                _detailStyle);
            if (!string.IsNullOrEmpty(_model.GoldResult))
            {
                GUILayout.Label(
                    CurrencyIconGui.Content(_model.GoldResult),
                    _detailStyle);
            }

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_model.SaveStatus))
            {
                GUILayout.Label(_model.SaveStatus, _statusStyle);
                GUILayout.Space(12f);
            }

            if (_model.CanRetrySave)
            {
                if (GUILayout.Button(
                        "RETRY SAVE",
                        _buttonStyle,
                        GUILayout.Height(ButtonHeight)))
                {
                    SaveRetryRequested?.Invoke();
                }
            }
            else
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = _model.CanRestart;
                if (GUILayout.Button(
                        "NEW RUN",
                        _buttonStyle,
                        GUILayout.Height(ButtonHeight)))
                {
                    RestartRequested?.Invoke();
                }

                GUILayout.Space(10f);
                GUI.enabled = _model.CanReturnToMainMenu;
                if (GUILayout.Button(
                        "MAIN MENU",
                        _buttonStyle,
                        GUILayout.Height(ButtonHeight)))
                {
                    MainMenuRequested?.Invoke();
                }

                GUI.enabled = previousEnabled;
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _backdropTexture = CreateTexture(new Color(0f, 0f, 0f, 0.82f));
            _panelTexture = CreateTexture(new Color(0.075f, 0.045f, 0.04f, 0.98f));
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _summaryStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = new Color(0.68f, 0.56f, 0.48f) }
            };
            _detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.86f, 0.8f, 0.68f) }
            };
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.68f, 0.62f, 0.54f) }
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
            Texture2D texture = new Texture2D(1, 1)
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
