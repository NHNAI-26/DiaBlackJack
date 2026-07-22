using System;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.UI
{
    public sealed class CoreLoopView : MonoBehaviour
    {
        private CoreLoopViewModel _model;
        private GUIStyle _titleStyle;
        private GUIStyle _headingStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _resultStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _buttonStyle;
        private int _styleScreenHeight;
        private bool _inputLocked;
        private bool _showDemonContractConfirmation;

        public event Action HitRequested;

        public event Action StandRequested;

        public event Action ChangeRequested;

        public event Action<int> ChangeCandidateRequested;

        public event Action<int> CardUseRequested;

        public event Action<int> CardEffectChoiceRequested;

        public event Action DemonContractBeginRequested;

        public event Action<int, int> DemonContractChoiceRequested;

        public event Action RestartRequested;

        public void Render(CoreLoopViewModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (_model.DemonContract.IsResolving ||
                _model.State == CoreLoopState.BattleEnded)
            {
                _showDemonContractConfirmation = false;
            }
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

            float panelWidth = Mathf.Min(760f, Screen.width - 32f);
            float panelHeight = Mathf.Min(900f, Screen.height - 8f);
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Space(4f);
            GUILayout.Label("DEVIL BLACKJACK", _titleStyle);
            GUILayout.Label($"ROUND {_model.RoundNumber}  |  {_model.State}", _headingStyle);
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            DrawParticipant(
                "PLAYER",
                _model.PlayerSoul,
                _model.PlayerCards,
                _model.PlayerTotal.ToString(),
                _model.PlayerDeck);
            GUILayout.Space(12f);
            DrawParticipant(
                "ENEMY",
                _model.EnemySoul,
                _model.EnemyCards,
                _model.EnemyVisibleTotal.ToString(),
                _model.EnemyDeck);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            DrawEnemyInformation();
            GUILayout.Space(4f);
            GUILayout.Label(GetBattleMessage(_model), _resultStyle);
            GUILayout.Label(_model.LastRound, _bodyStyle);
            GUILayout.Label(_model.LastCardEffect, _bodyStyle);
            DrawDemonContractStatus();
            GUILayout.FlexibleSpace();
            DrawActions();
            GUILayout.Space(4f);
            GUILayout.EndArea();
        }

        private void DrawEnemyInformation()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label(
                $"{_model.EnemyDisplayName}  |  {_model.EnemyGrade}",
                _headingStyle);
            GUILayout.Label(_model.EnemySummary, _bodyStyle);
            GUILayout.Space(2f);

            if (_styleScreenHeight <= 720)
            {
                string compactInformation = _model.EnemyInformationTitle;
                foreach (string line in _model.EnemyInformationLines)
                {
                    compactInformation += " · " + line;
                }

                GUILayout.Label(compactInformation, _bodyStyle);
                if (!string.IsNullOrEmpty(_model.EnemyWarning))
                {
                    GUILayout.Label(_model.EnemyWarning, _warningStyle);
                }

                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                _model.EnemyInformationTitle,
                _headingStyle,
                GUILayout.Width(190f));
            foreach (string line in _model.EnemyInformationLines)
            {
                GUILayout.Label(line, _bodyStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_model.EnemyWarning))
            {
                GUILayout.Label(_model.EnemyWarning, _warningStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawParticipant(string name, string soul, string cards, string total, string deck)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label(name, _headingStyle);
            GUILayout.Label($"SOUL  {soul}", _bodyStyle);
            GUILayout.Space(_styleScreenHeight <= 720 ? 2f : 4f);
            GUILayout.Label($"CARDS  [ {cards} ]", _bodyStyle);
            GUILayout.Label($"TOTAL  {total}", _bodyStyle);
            GUILayout.Space(_styleScreenHeight <= 720 ? 2f : 4f);
            GUILayout.Label(deck, _bodyStyle);
            GUILayout.EndVertical();
        }

        private void DrawActions()
        {
            if (_model.CanRestart || _model.Outcome != BattleOutcome.InProgress)
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = _model.CanRestart && !_inputLocked;
                if (GUILayout.Button("RESTART", _buttonStyle, GUILayout.Height(40f)))
                {
                    RestartRequested?.Invoke();
                }

                GUI.enabled = previousEnabled;
                return;
            }

            if (_model.IsChoosingChangeCard)
            {
                DrawChangeCandidates();
                return;
            }

            if (_model.IsResolvingCardEffect)
            {
                DrawCardEffectChoices();
                return;
            }

            if (_model.DemonContract.IsResolving)
            {
                DrawDemonContractChoices();
                return;
            }

            if (_showDemonContractConfirmation)
            {
                DrawDemonContractConfirmation();
                return;
            }

            GUILayout.BeginHorizontal();
            bool wasEnabled = GUI.enabled;
            GUI.enabled = _model.CanHit && !_inputLocked;
            float primaryActionHeight = _styleScreenHeight <= 720 ? 34f : 40f;
            if (GUILayout.Button("HIT", _buttonStyle, GUILayout.Height(primaryActionHeight)))
            {
                HitRequested?.Invoke();
            }

            GUI.enabled = _model.CanStand && !_inputLocked;
            if (GUILayout.Button("STAND", _buttonStyle, GUILayout.Height(primaryActionHeight)))
            {
                StandRequested?.Invoke();
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label(
                _model.ChangeActionText + "  ·  " +
                _model.DemonContract.ActionText,
                _bodyStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = _model.CanChange && !_inputLocked;
            if (GUILayout.Button("CHANGE", _buttonStyle, GUILayout.Height(primaryActionHeight)))
            {
                ChangeRequested?.Invoke();
            }

            GUI.enabled = _model.DemonContract.CanBegin && !_inputLocked;
            if (GUILayout.Button("CONTRACT", _buttonStyle, GUILayout.Height(primaryActionHeight)))
            {
                _showDemonContractConfirmation = true;
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            DrawPlayerCardActions();
        }

        private void DrawChangeCandidates()
        {
            var candidates = _model.ChangeCandidates;
            GUILayout.Label("CHOOSE A NEW HIDDEN CARD", _headingStyle);
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = !_inputLocked;
            for (int i = 0; i < candidates.Count; i++)
            {
                int candidateIndex = i;
                string label = $"CARD {i + 1}  [ {candidates[i]} ]";
                if (GUILayout.Button(label, _buttonStyle, GUILayout.Height(60f)))
                {
                    ChangeCandidateRequested?.Invoke(candidateIndex);
                }
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawPlayerCardActions()
        {
            GUILayout.Label("PLAYER CARD EFFECTS", _headingStyle);
            GUILayout.Space(2f);
            GUILayout.BeginHorizontal();

            bool wasEnabled = GUI.enabled;
            foreach (PlayerCardViewModel card in _model.PlayerCardActions)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                GUI.enabled = card.CanUse && !_inputLocked;
                string label = $"USE  {card.Rank}\n{card.DisplayName}\n{card.UseState}";
                float buttonHeight = _styleScreenHeight <= 720 ? 60f : 72f;
                if (GUILayout.Button(label, _buttonStyle, GUILayout.Height(buttonHeight)))
                {
                    CardUseRequested?.Invoke(card.CardId);
                }

                GUI.enabled = wasEnabled;
                if (!string.IsNullOrEmpty(card.DisabledReason))
                {
                    GUILayout.Label(card.DisabledReason, _bodyStyle);
                }

                GUILayout.EndVertical();
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawCardEffectChoices()
        {
            var choices = _model.CardEffectChoices;
            GUILayout.Label(_model.CardEffectPrompt, _headingStyle);
            GUILayout.Space(8f);

            const int choicesPerRow = 5;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !_inputLocked;
            for (int rowStart = 0;
                rowStart < choices.Count;
                rowStart += choicesPerRow)
            {
                GUILayout.BeginHorizontal();
                int rowEnd = Mathf.Min(
                    rowStart + choicesPerRow,
                    choices.Count);
                for (int i = rowStart; i < rowEnd; i++)
                {
                    CardEffectChoiceViewModel choice = choices[i];
                    if (GUILayout.Button(
                        choice.Label,
                        _buttonStyle,
                        GUILayout.Height(52f)))
                    {
                        CardEffectChoiceRequested?.Invoke(choice.OptionId);
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }

            GUI.enabled = wasEnabled;
        }

        private void DrawDemonContractStatus()
        {
            DemonContractPanelViewModel contract = _model.DemonContract;
            if (contract.ActiveContracts.Count == 0 &&
                string.IsNullOrEmpty(contract.LastContractResult) &&
                string.IsNullOrEmpty(contract.LastEffectResult))
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label("DEMON CONTRACT", _headingStyle);
            foreach (string active in contract.ActiveContracts)
            {
                GUILayout.Label(active, _bodyStyle);
            }

            if (!string.IsNullOrEmpty(contract.LastContractResult))
            {
                GUILayout.Label(contract.LastContractResult, _bodyStyle);
            }

            if (!string.IsNullOrEmpty(contract.LastEffectResult))
            {
                GUILayout.Label(contract.LastEffectResult, _warningStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawDemonContractConfirmation()
        {
            DemonContractPanelViewModel contract = _model.DemonContract;
            GUILayout.Label("CONFIRM DEMON CONTRACT", _headingStyle);
            GUILayout.Label(
                $"영혼 {contract.SoulCost} 지불 · 계약 후 {contract.SoulAfterCost}",
                _bodyStyle);
            GUILayout.Label("비용 지불 뒤에는 후보 하나를 반드시 선택합니다.", _warningStyle);
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = contract.CanBegin && !_inputLocked;
            if (GUILayout.Button("CONFIRM", _buttonStyle, GUILayout.Height(48f)))
            {
                DemonContractBeginRequested?.Invoke();
            }

            GUI.enabled = !_inputLocked;
            if (GUILayout.Button("CANCEL", _buttonStyle, GUILayout.Height(48f)))
            {
                _showDemonContractConfirmation = false;
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawDemonContractChoices()
        {
            DemonContractPanelViewModel contract = _model.DemonContract;
            GUILayout.Label(contract.Prompt, _headingStyle);
            if (!string.IsNullOrEmpty(contract.OwnerPreview))
            {
                GUILayout.Label(contract.OwnerPreview, _warningStyle);
            }

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            bool wasEnabled = GUI.enabled;
            foreach (DemonContractChoiceViewModel choice in contract.Choices)
            {
                GUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.ExpandWidth(true),
                    GUILayout.MinHeight(contract.InteractionKind ==
                        DemonContractInteractionKind.ChooseContract ? 124f : 56f));
                GUILayout.Label(choice.Title, _headingStyle);
                if (!string.IsNullOrEmpty(choice.Ability))
                {
                    GUILayout.Label(choice.Ability, _bodyStyle);
                }

                if (!string.IsNullOrEmpty(choice.Cost))
                {
                    GUILayout.Label(choice.Cost, _warningStyle);
                }

                GUI.enabled = choice.CanSelect && !_inputLocked;
                if (GUILayout.Button(
                    choice.CanSelect ? "SELECT" : choice.DisabledReason,
                    _buttonStyle,
                    GUILayout.MinHeight(38f),
                    GUILayout.ExpandWidth(true)))
                {
                    if (contract.InteractionId.HasValue)
                    {
                        DemonContractChoiceRequested?.Invoke(
                            contract.InteractionId.Value,
                            choice.OptionId);
                    }
                }

                GUILayout.EndVertical();
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null && _styleScreenHeight == Screen.height)
            {
                return;
            }

            _styleScreenHeight = Screen.height;
            bool compact = Screen.height <= 720;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = compact ? 24 : 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.75f, 0.25f) }
            };
            _headingStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = compact ? 17 : 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = compact ? 14 : 17,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };
            _resultStyle = new GUIStyle(_headingStyle)
            {
                fontSize = compact ? 19 : 23,
                normal = { textColor = new Color(0.9f, 0.3f, 0.25f) }
            };
            _warningStyle = new GUIStyle(_headingStyle)
            {
                fontSize = compact ? 15 : 18,
                normal = { textColor = new Color(1f, 0.45f, 0.2f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = compact ? 17 : 20,
                fontStyle = FontStyle.Bold
            };
        }

        private static string GetBattleMessage(CoreLoopViewModel model)
        {
            switch (model.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "VICTORY";
                case BattleOutcome.PlayerDefeat:
                    return "DEFEAT";
                default:
                    if (model.IsChoosingChangeCard)
                    {
                        return "CHOOSE A CHANGE CARD";
                    }

                    if (model.IsResolvingCardEffect)
                    {
                        return "CHOOSE CARD EFFECT";
                    }

                    return model.State == CoreLoopState.PlayerTurn
                        ? "YOUR TURN"
                        : model.State.ToString();
            }
        }

        private static void DrawBackground()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.035f, 0.025f, 0.045f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
