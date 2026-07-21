using System;
using System.Collections;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Owns and drives one standalone CoreLoop battle for the GameScene. The single coordinator: it
    /// holds the <see cref="CoreLoopSession"/>, takes input (temporary IMGUI buttons — the project is
    /// new-Input-System-only, so legacy OnMouseDown / Input.GetKey do not fire), and on every action
    /// re-presents through <see cref="GameScenePresenter"/> into the HUD and the two hands. Rendering
    /// lives in <see cref="GameHudView"/> and <see cref="CardHand"/>; this type only orchestrates.
    /// MVP surface: hit, stand, restart.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private int seed = 20260719;
        [SerializeField] private GameHudView hud;
        [SerializeField] private CardHand playerHand;
        [SerializeField] private CardHand enemyHand;
        [SerializeField] private CharacterView playerCharacter;
        [SerializeField] private CharacterView enemyCharacter;

        private CoreLoopSession _session;
        private CoreLoopViewModel _core;
        private bool _inputLocked;
        private int _battleIndex;
        private GUIStyle _buttonStyle;

        public CoreLoopBattle Battle => _session?.Battle;

        private void Awake()
        {
            _session = new CoreLoopSession(CreateBattle);
        }

        private void Start()
        {
            RefreshView();
        }

        private CoreLoopBattle CreateBattle()
        {
            int battleSeed = seed + (_battleIndex * 2);
            _battleIndex++;
            return new CoreLoopBattle(
                BlackjackDeck.CreateStandard(battleSeed),
                BlackjackDeck.CreateStandard(battleSeed + 1));
        }

        private void OnGUI()
        {
            if (_core == null)
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 20, fixedHeight = 46f };

            bool live = !_inputLocked;
            bool ended = _core.State == CoreLoopState.BattleEnded;

            const float w = 150f;
            const float h = 46f;
            float y = Screen.height - h - 24f;
            float cx = Screen.width * 0.5f;

            if (ended)
            {
                using (new GUIEnabledScope(live && _core.CanRestart))
                {
                    if (GUI.Button(new Rect(cx - w * 0.5f, y, w, h), "RESTART", _buttonStyle))
                    {
                        ProcessInput(_session.TryRestart);
                    }
                }

                return;
            }

            using (new GUIEnabledScope(live && _core.CanHit))
            {
                if (GUI.Button(new Rect(cx - w - 8f, y, w, h), "HIT", _buttonStyle))
                {
                    ProcessInput(_session.TryPlayerHit);
                }
            }

            using (new GUIEnabledScope(live && _core.CanStand))
            {
                if (GUI.Button(new Rect(cx + 8f, y, w, h), "STAND", _buttonStyle))
                {
                    ProcessInput(_session.TryPlayerStand);
                }
            }
        }

        private void ProcessInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            bool accepted = action();
            RefreshView();

            if (!accepted || !Application.isPlaying)
            {
                UnlockInput();
            }
            else
            {
                StartCoroutine(UnlockInputNextFrame());
            }
        }

        private void RefreshView()
        {
            GameSceneViewModel vm = GameScenePresenter.Create(Battle);
            _core = vm.Core;

            if (hud != null)
            {
                hud.Render(vm.Core);
            }

            if (playerHand != null)
            {
                playerHand.Render(vm.PlayerCards);
            }

            if (enemyHand != null)
            {
                enemyHand.Render(vm.EnemyCards);
            }

            if (playerCharacter != null)
            {
                playerCharacter.Render(vm.PlayerVisual);
            }

            if (enemyCharacter != null)
            {
                enemyCharacter.Render(vm.EnemyVisual);
            }
        }

        private void UnlockInput()
        {
            _inputLocked = false;
        }

        private IEnumerator UnlockInputNextFrame()
        {
            yield return null;
            UnlockInput();
        }

        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool _previous;

            public GUIEnabledScope(bool enabled)
            {
                _previous = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previous;
            }
        }
    }
}
