using System;
using System.Collections;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Presentation pacing")]
        [SerializeField] private float stepSeconds = 1.0f;
        [SerializeField] private float resolveHoldSeconds = 1.1f;

        private CoreLoopSession _session;
        private CoreLoopViewModel _core;
        private Camera _camera;
        private CardView _hoveredCard;
        private bool _inputLocked;
        private int _battleIndex;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private readonly List<GameSceneViewModel> _timeline = new List<GameSceneViewModel>();

        public CoreLoopBattle Battle => _session?.Battle;

        private void Awake()
        {
            _session = new CoreLoopSession(CreateBattle);
        }

        private void Start()
        {
            RefreshView();
        }

        // Diegetic input: hover any card to enlarge it (usable cards also glow + show a badge), and
        // click a usable card to activate its effect. New Input System — legacy OnMouseDown does not
        // fire, so we raycast the pointer ourselves. Hit/Stand/Change and the choices stay as OnGUI.
        private void Update()
        {
            if (_core == null)
            {
                return;
            }

            CardView pointed = RaycastCard();

            // Hover is visual-only, so it runs even while input is locked (during timeline playback).
            UpdateHover(pointed);

            if (_inputLocked)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (pointed != null && pointed.CanUse)
            {
                int cardId = pointed.CardId;
                ProcessInput(() => _session.TryBeginPlayerCardUse(cardId));
            }
        }

        private CardView RaycastCard()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Mouse mouse = Mouse.current;
            if (_camera == null || mouse == null)
            {
                return null;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            return Physics.Raycast(ray, out RaycastHit hit, 200f)
                ? hit.collider.GetComponentInParent<CardView>()
                : null;
        }

        private void UpdateHover(CardView pointed)
        {
            if (pointed == _hoveredCard)
            {
                return;
            }

            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(false);
            }

            _hoveredCard = pointed;
            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(true);
            }
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

            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (_core.State == CoreLoopState.BattleEnded)
            {
                DrawButtonRow(
                    new[] { "RESTART" },
                    new[] { _core.CanRestart },
                    new Func<bool>[] { _session.TryRestart });
                return;
            }

            if (_core.IsChoosingChangeCard)
            {
                DrawChangeCandidates();
                return;
            }

            if (_core.IsResolvingCardEffect)
            {
                DrawCardEffectChoices();
                return;
            }

            DrawHeading("CLICK A LIT CARD TO USE ITS EFFECT");
            DrawButtonRow(
                new[] { "HIT", "STAND", _core.ChangeActionText },
                new[] { _core.CanHit, _core.CanStand, _core.CanChange },
                new Func<bool>[] { _session.TryPlayerHit, _session.TryPlayerStand, _session.TryBeginPlayerChange });
        }

        private void DrawChangeCandidates()
        {
            var candidates = _core.ChangeCandidates;
            int count = candidates.Count;
            var labels = new string[count];
            var enabled = new bool[count];
            var actions = new Func<bool>[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                labels[i] = $"[ {candidates[i]} ]";
                enabled[i] = true;
                actions[i] = () => _session.TrySelectChangedCard(index);
            }

            DrawHeading("CHOOSE A NEW HIDDEN CARD");
            DrawButtonRow(labels, enabled, actions);
        }

        private void DrawCardEffectChoices()
        {
            var choices = _core.CardEffectChoices;
            int count = choices.Count;
            var labels = new string[count];
            var enabled = new bool[count];
            var actions = new Func<bool>[count];
            for (int i = 0; i < count; i++)
            {
                CardEffectChoiceViewModel choice = choices[i];
                labels[i] = choice.Label;
                enabled[i] = true;
                actions[i] = () => _session.TryResolvePlayerCardChoice(choice.OptionId);
            }

            DrawHeading(_core.CardEffectPrompt);
            DrawButtonRow(labels, enabled, actions);
        }

        // Bottom-anchored, screen-centered row. Width shrinks to always fit one row on screen.
        private void DrawButtonRow(string[] labels, bool[] enabled, Func<bool>[] actions)
        {
            int n = labels.Length;
            if (n == 0)
            {
                return;
            }

            const float h = 48f;
            const float gap = 8f;
            float w = Mathf.Min(160f, (Screen.width - 40f - (n - 1) * gap) / n);
            float totalWidth = n * w + (n - 1) * gap;
            float x0 = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - h - 24f;

            for (int i = 0; i < n; i++)
            {
                using (new GUIEnabledScope(!_inputLocked && enabled[i]))
                {
                    if (GUI.Button(new Rect(x0 + i * (w + gap), y, w, h), labels[i], _buttonStyle))
                    {
                        ProcessInput(actions[i]);
                    }
                }
            }
        }

        private void DrawHeading(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            const float h = 30f;
            float y = Screen.height - 48f - 24f - h - 6f;
            GUI.Label(new Rect(0f, y, Screen.width, h), text, _labelStyle);
        }

        private void ProcessInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;

            // The battle runs the whole turn synchronously; Stepped fires once per sub-step, so we
            // snapshot each into a timeline and then pace them out over PlayTimeline.
            CoreLoopBattle battle = Battle;
            _timeline.Clear();
            if (battle != null)
            {
                battle.Stepped += OnBattleStepped;
            }

            bool accepted = action();

            if (battle != null)
            {
                battle.Stepped -= OnBattleStepped;
            }

            if (accepted && Application.isPlaying && _timeline.Count > 0)
            {
                StartCoroutine(PlayTimeline());
            }
            else
            {
                RefreshView();
                UnlockInput();
            }
        }

        // Fires synchronously for each sub-step while the battle resolves the turn. Snapshots the
        // public view state at that instant so PlayTimeline can reveal them one beat at a time.
        private void OnBattleStepped()
        {
            _timeline.Add(GameScenePresenter.Create(Battle));
        }

        private IEnumerator PlayTimeline()
        {
            foreach (GameSceneViewModel vm in _timeline)
            {
                ApplyView(vm);

                bool resolveBeat = vm.Core.State == CoreLoopState.ResolvingRound;
                yield return new WaitForSeconds(resolveBeat ? resolveHoldSeconds : stepSeconds);
            }

            // Land on the true current state — e.g. BattleEnded, which is not itself a step.
            RefreshView();
            UnlockInput();
        }

        private void RefreshView()
        {
            ApplyView(GameScenePresenter.Create(Battle));
        }

        private void ApplyView(GameSceneViewModel vm)
        {
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
                playerCharacter.Render(vm.PlayerVisual, vm.PlayerActionLabel);
            }

            if (enemyCharacter != null)
            {
                enemyCharacter.Render(vm.EnemyVisual, vm.EnemyActionLabel);
            }
        }

        private void UnlockInput()
        {
            _inputLocked = false;
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
