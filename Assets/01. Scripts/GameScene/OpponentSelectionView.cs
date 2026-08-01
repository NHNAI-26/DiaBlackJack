using System;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class OpponentSelectionView : MonoBehaviour
    {
        [SerializeField] private Font font;

        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _headingStyle;
        private GUIStyle _posterStyle;
        private GUIStyle _titleStyle;
        private StageProgressionViewModel _model;

        public event Action<string> OpponentFocused;

        public event Action OpponentConfirmed;

        public bool IsVisible { get; private set; }

        public void Render(StageProgressionViewModel model)
        {
            _model = model;
            IsVisible = model != null &&
                model.OpponentCandidates.Count > 0;
        }

        public void Hide()
        {
            _model = null;
            IsVisible = false;
        }

        private void OnGUI()
        {
            if (!IsVisible || _model == null)
            {
                return;
            }

            EnsureStyles();
            float width = Mathf.Min(Screen.width * 0.78f, 1040f);
            float height = Mathf.Min(Screen.height * 0.84f, 760f);
            Rect area = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUILayout.BeginArea(area);
            GUILayout.Label("CHOOSE YOUR OPPONENT", _titleStyle);
            GUILayout.Space(Screen.height <= 720 ? 12f : 22f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int i = 0; i < _model.OpponentCandidates.Count; i++)
            {
                DrawPoster(_model.OpponentCandidates[i]);
                if (i + 1 < _model.OpponentCandidates.Count)
                {
                    GUILayout.Space(Screen.width <= 1280 ? 28f : 54f);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(18f);

            GUI.enabled = _model.CanConfirmOpponent;
            if (GUILayout.Button(
                    "CONFIRM OPPONENT",
                    _buttonStyle,
                    GUILayout.Width(Mathf.Min(420f, width * 0.46f)),
                    GUILayout.Height(Screen.height <= 720 ? 48f : 58f)))
            {
                OpponentConfirmed?.Invoke();
            }

            GUI.enabled = true;
            GUILayout.EndArea();
        }

        private void DrawPoster(OpponentCandidateViewModel candidate)
        {
            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = candidate.IsFocused
                ? new Color(0.72f, 0.28f, 0.16f, 1f)
                : new Color(0.35f, 0.29f, 0.22f, 1f);

            float posterWidth = Screen.width <= 1280 ? 310f : 370f;
            float posterHeight = Screen.height <= 720 ? 430f : 520f;
            GUILayout.BeginVertical(
                _posterStyle,
                GUILayout.Width(posterWidth),
                GUILayout.Height(posterHeight));
            GUILayout.Label("WANTED", _titleStyle);
            GUILayout.Space(8f);

            Color previousContent = GUI.contentColor;
            GUI.contentColor = ResolveGradeColor(candidate.Grade);
            GUILayout.Label(candidate.DisplayName, _headingStyle);
            GUILayout.Label(
                CurrencyIconGui.Soul(
                    $"{candidate.Grade}  ·  {candidate.MaximumSoul}"),
                _headingStyle);
            GUI.contentColor = previousContent;
            GUILayout.Space(16f);
            GUILayout.Label(candidate.Summary, _bodyStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                CurrencyIconGui.Content(candidate.RewardTier),
                _bodyStyle);
            GUILayout.Space(14f);

            GUI.enabled = _model.CanFocusOpponent;
            if (GUILayout.Button(
                    candidate.IsFocused ? "SELECTED" : "SELECT",
                    _buttonStyle,
                    GUILayout.Height(50f)))
            {
                OpponentFocused?.Invoke(candidate.ProfileKey);
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
            GUI.backgroundColor = previousBackground;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = font,
                fontSize = Screen.height <= 720 ? 28 : 38,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.84f, 0.68f) }
            };
            _headingStyle = new GUIStyle(_titleStyle)
            {
                fontSize = Screen.height <= 720 ? 20 : 26,
                wordWrap = true
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                font = font,
                fontSize = Screen.height <= 720 ? 16 : 20,
                wordWrap = true,
                normal = { textColor = new Color(0.13f, 0.09f, 0.06f) }
            };
            _posterStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(24, 24, 20, 22),
                normal = { background = Texture2D.whiteTexture }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                font = font,
                fontSize = Screen.height <= 720 ? 17 : 21,
                fontStyle = FontStyle.Bold
            };
        }

        private static Color ResolveGradeColor(string grade)
        {
            if (string.Equals(grade, "ELITE", StringComparison.Ordinal))
            {
                return new Color(0.45f, 0.15f, 0.55f);
            }

            if (string.Equals(grade, "BOSS", StringComparison.Ordinal))
            {
                return new Color(0.7f, 0.08f, 0.08f);
            }

            return new Color(0.08f, 0.06f, 0.04f);
        }
    }
}
