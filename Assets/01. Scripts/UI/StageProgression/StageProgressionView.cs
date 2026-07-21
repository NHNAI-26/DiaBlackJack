using System;
using UnityEngine;

namespace DiaBlackJack.StageProgression.UI
{
    [DisallowMultipleComponent]
    public sealed class StageProgressionView : MonoBehaviour
    {
        private StageProgressionViewModel _model;
        private GUIStyle _titleStyle;
        private GUIStyle _headingStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _candidateBodyStyle;
        private GUIStyle _selectedStyle;
        private bool _inputLocked;

        public event Action StartRunRequested;

        public event Action NextStageRequested;

        public event Action RestartRunRequested;

        public event Action<int> BattleRewardSelected;

        public event Action BattleRewardSkipped;

        public event Action<string> OpponentFocused;

        public event Action OpponentConfirmed;

        public void Render(StageProgressionViewModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void SetInputLocked(bool inputLocked)
        {
            _inputLocked = inputLocked;
        }

        private void OnGUI()
        {
            if (_model == null)
            {
                return;
            }

            EnsureStyles();
            DrawBackground();

            float panelWidth = Mathf.Min(860f, Screen.width - 32f);
            float panelHeight = Mathf.Min(760f, Screen.height - 32f);
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Space(18f);
            GUILayout.Label("DEVIL BLACKJACK RUN", _titleStyle);
            GUILayout.Space(18f);
            GUILayout.Label(_model.StageProgress, _headingStyle);
            GUILayout.Label(_model.StageName, _messageStyle);
            GUILayout.Label(_model.StageKind, _bodyStyle);
            GUILayout.Space(24f);
            GUILayout.Label($"PLAYER SOUL  {_model.PlayerSoul}", _headingStyle);
            GUILayout.Label($"RUN DECK  {_model.DeckCount}", _bodyStyle);
            GUILayout.Space(18f);
            GUILayout.Label(_model.Message, _messageStyle);
            if (!string.IsNullOrEmpty(_model.RewardResult))
            {
                GUILayout.Space(8f);
                GUILayout.Label(_model.RewardResult, _headingStyle);
            }

            GUILayout.FlexibleSpace();
            DrawAction();
            GUILayout.Space(18f);
            GUILayout.EndArea();
        }

        private void DrawAction()
        {
            bool previousEnabled = GUI.enabled;
            if (_model.CanFocusOpponent)
            {
                DrawOpponentSelection();
                GUI.enabled = previousEnabled;
                return;
            }

            if (_model.CanSelectReward)
            {
                DrawBattleReward();
                GUI.enabled = previousEnabled;
                return;
            }

            GUI.enabled = !_inputLocked;

            if (_model.CanStartRun && GUILayout.Button("START RUN", _buttonStyle, GUILayout.Height(56f)))
            {
                StartRunRequested?.Invoke();
            }
            else if (_model.CanAdvanceStage && GUILayout.Button("NEXT STAGE", _buttonStyle, GUILayout.Height(56f)))
            {
                NextStageRequested?.Invoke();
            }
            else if (_model.CanRestartRun && GUILayout.Button("RESTART RUN", _buttonStyle, GUILayout.Height(56f)))
            {
                RestartRunRequested?.Invoke();
            }

            GUI.enabled = previousEnabled;
        }

        private void DrawOpponentSelection()
        {
            GUILayout.BeginHorizontal();
            foreach (OpponentCandidateViewModel candidate in _model.OpponentCandidates)
            {
                Color previousBackgroundColor = GUI.backgroundColor;
                if (candidate.IsFocused)
                {
                    GUI.backgroundColor = new Color(0.95f, 0.55f, 0.18f, 1f);
                }

                GUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.MinHeight(250f),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(candidate.DisplayName, _messageStyle);
                GUILayout.Label(
                    $"{candidate.Grade}  ·  {candidate.MaximumSoul}",
                    _headingStyle);
                GUILayout.Space(8f);
                GUILayout.Label(candidate.Summary, _candidateBodyStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(candidate.RewardTier, _bodyStyle);
                GUILayout.Space(8f);

                GUI.enabled = !_inputLocked && _model.CanFocusOpponent;
                if (GUILayout.Button(
                    candidate.IsFocused ? "SELECTED" : "SELECT",
                    _buttonStyle,
                    GUILayout.Height(46f)))
                {
                    OpponentFocused?.Invoke(candidate.ProfileKey);
                }

                GUILayout.EndVertical();
                GUI.backgroundColor = previousBackgroundColor;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12f);

            string selectedName = GetFocusedOpponentDisplayName();
            GUILayout.Label(
                selectedName == null
                    ? "SELECT AN OPPONENT"
                    : $"SELECTED: {selectedName}",
                _selectedStyle);
            GUILayout.Space(8f);

            GUI.enabled = !_inputLocked && _model.CanConfirmOpponent;
            if (GUILayout.Button(
                "CONFIRM OPPONENT",
                _buttonStyle,
                GUILayout.Height(52f)))
            {
                OpponentConfirmed?.Invoke();
            }
        }

        private string GetFocusedOpponentDisplayName()
        {
            foreach (OpponentCandidateViewModel candidate in _model.OpponentCandidates)
            {
                if (candidate.IsFocused)
                {
                    return candidate.DisplayName;
                }
            }

            return null;
        }

        private void DrawBattleReward()
        {
            GUILayout.Label(_model.RewardTier, _headingStyle);
            GUILayout.Label(_model.RewardCompletionMessage, _bodyStyle);
            GUILayout.Space(12f);

            GUILayout.BeginHorizontal();
            foreach (BattleRewardOptionViewModel option in _model.RewardOptions)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinHeight(190f));
                GUILayout.Label($"CARD {option.Rank}", _headingStyle);
                GUILayout.Label(option.DisplayName, _messageStyle);
                GUILayout.Space(6f);
                GUILayout.Label(option.EffectSummary, _bodyStyle);
                GUILayout.FlexibleSpace();

                GUI.enabled = !_inputLocked && _model.CanSelectReward;
                if (GUILayout.Button("SELECT", _buttonStyle, GUILayout.Height(46f)))
                {
                    BattleRewardSelected?.Invoke(option.OptionId);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12f);

            GUI.enabled = !_inputLocked && _model.CanSkipReward;
            if (GUILayout.Button("SKIP REWARD", _buttonStyle, GUILayout.Height(48f)))
            {
                BattleRewardSkipped?.Invoke();
            }
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
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.75f, 0.25f) }
            };
            _headingStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
            };
            _messageStyle = new GUIStyle(_headingStyle)
            {
                fontSize = 24,
                normal = { textColor = new Color(0.9f, 0.3f, 0.25f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            _candidateBodyStyle = new GUIStyle(_bodyStyle)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            _selectedStyle = new GUIStyle(_headingStyle)
            {
                normal = { textColor = new Color(0.95f, 0.75f, 0.25f) }
            };
        }

        private static void DrawBackground()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.025f, 0.018f, 0.035f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
