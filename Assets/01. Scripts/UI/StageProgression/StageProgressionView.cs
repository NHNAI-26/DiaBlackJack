using System;
using Border.SaveLoad.UI;
using UnityEngine;

namespace DiaBlackJack.StageProgression.UI
{
    [DisallowMultipleComponent]
    public sealed class StageProgressionView : MonoBehaviour
    {
        private StageProgressionViewModel _model;
        private RunSaveViewModel _saveModel;
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

        public event Action NewRunRequested;

        public event Action NewRunConfirmed;

        public event Action NewRunCancelled;

        public event Action ContinueRunRequested;

        public event Action ResumeReservationRequested;

        public event Action<int, int> StartingDemonSelected;

        public event Action SaveRetryRequested;

        public void Render(
            StageProgressionViewModel model,
            RunSaveViewModel saveModel)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _saveModel = saveModel ??
                throw new ArgumentNullException(nameof(saveModel));
        }

        public void SetInputLocked(bool inputLocked)
        {
            _inputLocked = inputLocked;
        }

        private void OnGUI()
        {
            if (_model == null || _saveModel == null)
            {
                return;
            }

            EnsureStyles();
            DrawBackground();

            float panelWidth = Mathf.Min(860f, Screen.width - 32f);
            float panelHeight = Mathf.Min(760f, Screen.height - 32f);
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Space(18f);
            GUILayout.Label("DEVIL BLACKJACK RUN", _titleStyle);
            GUILayout.Space(18f);

            if (_saveModel.IsMenuVisible)
            {
                DrawRunMenu();
            }
            else
            {
                DrawProgression();
            }

            GUILayout.Space(18f);
            GUILayout.EndArea();
        }

        private void DrawRunMenu()
        {
            GUILayout.Label("RUN MENU", _headingStyle);
            GUILayout.Space(18f);
            GUILayout.Label(_saveModel.StatusMessage, _messageStyle);
            GUILayout.FlexibleSpace();

            bool previousEnabled = GUI.enabled;
            GUI.enabled = !_inputLocked;
            if (_saveModel.RequiresNewRunConfirmation)
            {
                GUILayout.Label(
                    "CURRENT PROGRESS WILL REMAIN UNTIL THE NEW RUN IS RESERVED.",
                    _bodyStyle);
                GUILayout.Space(12f);
                if (GUILayout.Button(
                        "CONFIRM NEW RUN",
                        _buttonStyle,
                        GUILayout.Height(56f)))
                {
                    NewRunConfirmed?.Invoke();
                }

                if (GUILayout.Button(
                        "CANCEL",
                        _buttonStyle,
                        GUILayout.Height(48f)))
                {
                    NewRunCancelled?.Invoke();
                }

                GUI.enabled = previousEnabled;
                return;
            }

            GUI.enabled = !_inputLocked && _saveModel.CanContinueRun;
            if (GUILayout.Button(
                    "CONTINUE RUN",
                    _buttonStyle,
                    GUILayout.Height(56f)))
            {
                ContinueRunRequested?.Invoke();
            }

            GUI.enabled = !_inputLocked && _saveModel.CanResumeReservation;
            if (GUILayout.Button(
                    "RESUME STARTING DEMON SELECTION",
                    _buttonStyle,
                    GUILayout.Height(56f)))
            {
                ResumeReservationRequested?.Invoke();
            }

            GUI.enabled = !_inputLocked && _saveModel.CanStartNewRun;
            if (GUILayout.Button(
                    "NEW RUN",
                    _buttonStyle,
                    GUILayout.Height(56f)))
            {
                NewRunRequested?.Invoke();
            }

            GUI.enabled = previousEnabled;
        }

        private void DrawProgression()
        {
            GUILayout.Label(_model.StageProgress, _headingStyle);
            GUILayout.Label(_model.StageName, _messageStyle);
            GUILayout.Label(_model.StageKind, _bodyStyle);
            GUILayout.Space(Screen.height <= 720 ? 12f : 24f);
            GUILayout.Label($"PLAYER SOUL  {_model.PlayerSoul}", _headingStyle);
            GUILayout.Label($"RUN DECK  {_model.DeckCount}", _bodyStyle);
            GUILayout.Space(12f);
            GUILayout.Label(_model.Message, _messageStyle);
            if (!string.IsNullOrEmpty(_model.RewardResult))
            {
                GUILayout.Space(6f);
                GUILayout.Label(_model.RewardResult, _headingStyle);
            }

            if (!string.IsNullOrEmpty(_saveModel.SaveIndicator))
            {
                GUILayout.Space(6f);
                GUILayout.Label(_saveModel.SaveIndicator, _selectedStyle);
            }

            GUILayout.FlexibleSpace();
            DrawAction();
        }

        private void DrawAction()
        {
            bool previousEnabled = GUI.enabled;
            if (_saveModel.BlocksProgressionInput)
            {
                GUILayout.Label(_saveModel.StatusMessage, _messageStyle);
                GUILayout.Space(10f);
                GUI.enabled = !_inputLocked && _saveModel.CanRetrySave;
                if (GUILayout.Button(
                        "RETRY SAVE",
                        _buttonStyle,
                        GUILayout.Height(56f)))
                {
                    SaveRetryRequested?.Invoke();
                }

                GUI.enabled = previousEnabled;
                return;
            }

            if (_model.CanSelectStartingDemon)
            {
                DrawStartingDemonSelection();
                GUI.enabled = previousEnabled;
                return;
            }

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

            if (_model.CanStartRun && GUILayout.Button(
                    "START RUN",
                    _buttonStyle,
                    GUILayout.Height(56f)))
            {
                StartRunRequested?.Invoke();
            }
            else if (_model.CanAdvanceStage && GUILayout.Button(
                         "NEXT STAGE",
                         _buttonStyle,
                         GUILayout.Height(56f)))
            {
                NextStageRequested?.Invoke();
            }
            else if (_model.CanRestartRun && GUILayout.Button(
                         "RUN MENU",
                         _buttonStyle,
                         GUILayout.Height(56f)))
            {
                RestartRunRequested?.Invoke();
            }

            GUI.enabled = previousEnabled;
        }

        private void DrawStartingDemonSelection()
        {
            if (!_model.StartingDemonOfferId.HasValue)
            {
                return;
            }

            int offerId = _model.StartingDemonOfferId.Value;
            GUILayout.BeginHorizontal();
            foreach (StartingDemonOptionViewModel option in
                     _model.StartingDemonOptions)
            {
                GUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.MinHeight(Screen.height <= 720 ? 180f : 220f),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(option.DisplayName, _messageStyle);
                GUILayout.Space(6f);
                GUILayout.Label(option.Summary, _candidateBodyStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(option.CostSummary, _bodyStyle);
                GUILayout.Space(8f);

                GUI.enabled = !_inputLocked &&
                    _model.CanSelectStartingDemon;
                if (GUILayout.Button(
                        "SELECT DEMON",
                        _buttonStyle,
                        GUILayout.Height(46f)))
                {
                    StartingDemonSelected?.Invoke(offerId, option.OptionId);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawOpponentSelection()
        {
            GUILayout.BeginHorizontal();
            foreach (OpponentCandidateViewModel candidate in
                     _model.OpponentCandidates)
            {
                Color previousBackgroundColor = GUI.backgroundColor;
                if (candidate.IsFocused)
                {
                    GUI.backgroundColor = new Color(0.95f, 0.55f, 0.18f, 1f);
                }

                GUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.MinHeight(Screen.height <= 720 ? 210f : 250f),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(candidate.DisplayName, _messageStyle);
                GUILayout.Label(
                    $"{candidate.Grade}  |  {candidate.MaximumSoul}",
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
            foreach (OpponentCandidateViewModel candidate in
                     _model.OpponentCandidates)
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
                if (GUILayout.Button(
                        "SELECT",
                        _buttonStyle,
                        GUILayout.Height(46f)))
                {
                    BattleRewardSelected?.Invoke(option.OptionId);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12f);

            GUI.enabled = !_inputLocked && _model.CanSkipReward;
            if (GUILayout.Button(
                    "SKIP REWARD",
                    _buttonStyle,
                    GUILayout.Height(48f)))
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
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
            };
            _messageStyle = new GUIStyle(_headingStyle)
            {
                fontSize = 24,
                wordWrap = true,
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
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
