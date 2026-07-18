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
        private GUIStyle _buttonStyle;
        private bool _inputLocked;

        public event Action HitRequested;

        public event Action StandRequested;

        public event Action RestartRequested;

        public void Render(CoreLoopViewModel model)
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

            float panelWidth = Mathf.Min(760f, Screen.width - 32f);
            float panelHeight = Mathf.Min(620f, Screen.height - 32f);
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Space(12f);
            GUILayout.Label("DEVIL BLACKJACK", _titleStyle);
            GUILayout.Label($"ROUND {_model.RoundNumber}  |  {_model.State}", _headingStyle);
            GUILayout.Space(12f);

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

            GUILayout.Space(14f);
            GUILayout.Label(GetBattleMessage(_model), _resultStyle);
            GUILayout.Label(_model.LastRound, _bodyStyle);
            GUILayout.FlexibleSpace();
            DrawActions();
            GUILayout.Space(12f);
            GUILayout.EndArea();
        }

        private void DrawParticipant(string name, string soul, string cards, string total, string deck)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label(name, _headingStyle);
            GUILayout.Label($"SOUL  {soul}", _bodyStyle);
            GUILayout.Space(8f);
            GUILayout.Label($"CARDS  [ {cards} ]", _bodyStyle);
            GUILayout.Label($"TOTAL  {total}", _bodyStyle);
            GUILayout.Space(8f);
            GUILayout.Label(deck, _bodyStyle);
            GUILayout.EndVertical();
        }

        private void DrawActions()
        {
            if (_model.CanRestart || _model.Outcome != BattleOutcome.InProgress)
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = _model.CanRestart && !_inputLocked;
                if (GUILayout.Button("RESTART", _buttonStyle, GUILayout.Height(52f)))
                {
                    RestartRequested?.Invoke();
                }

                GUI.enabled = previousEnabled;
                return;
            }

            GUILayout.BeginHorizontal();
            bool wasEnabled = GUI.enabled;
            GUI.enabled = _model.CanHit && !_inputLocked;
            if (GUILayout.Button("HIT", _buttonStyle, GUILayout.Height(52f)))
            {
                HitRequested?.Invoke();
            }

            GUI.enabled = _model.CanStand && !_inputLocked;
            if (GUILayout.Button("STAND", _buttonStyle, GUILayout.Height(52f)))
            {
                StandRequested?.Invoke();
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();
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
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };
            _resultStyle = new GUIStyle(_headingStyle)
            {
                fontSize = 23,
                normal = { textColor = new Color(0.9f, 0.3f, 0.25f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
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
