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
    /// MVP surface: hit, stand, restart, and a post-victory shop delegated to <see cref="ShopController"/>
    /// (gold reward + merchant + goods on the table + leave). The shop is GameScene-local (no
    /// StageProgression); leaving it restarts into the next battle with gold kept, while a defeat
    /// restart starts a fresh run with gold reset to 0.
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
        [SerializeField] private TableTotalsView totals;

        [Header("Shop (MVP)")]
        [SerializeField] private ShopController shop;

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
        private GUIStyle _panelStyle;
        private DeckClickable _hoveredDeck;
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

            bool hasHit = RaycastPointer(out RaycastHit hit);
            CardView pointed = hasHit ? hit.collider.GetComponentInParent<CardView>() : null;

            // Hover is visual-only, so it runs even while input is locked (during timeline playback).
            UpdateHover(pointed);

            // A deck's card-list panel shows while the pointer hovers it (draw or discard).
            _hoveredDeck = hasHit ? hit.collider.GetComponentInParent<DeckClickable>() : null;

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

        private bool RaycastPointer(out RaycastHit hit)
        {
            hit = default;
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Mouse mouse = Mouse.current;
            if (_camera == null || mouse == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            return Physics.Raycast(ray, out hit, 200f);
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

            if (_hoveredDeck != null)
            {
                DrawDeckPanel(_hoveredDeck.Kind);
            }

            if (_core.State == CoreLoopState.BattleEnded)
            {
                if (shop != null && shop.IsOpen)
                {
                    DrawHeading("SHOP — 상품을 둘러보고 나가세요");
                    DrawButtonRow(
                        new[] { "상점 나가기" },
                        new[] { true },
                        new Func<bool>[] { LeaveShop });
                }
                else
                {
                    DrawButtonRow(
                        new[] { "RESTART" },
                        new[] { _core.CanRestart },
                        new Func<bool>[] { RestartRun });
                }

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

        private void DrawDeckPanel(DeckKind kind)
        {
            _panelStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(18, 18, 18, 18),
                normal = { textColor = Color.white }
            };

            const float w = 430f;
            const float h = 200f;
            bool draw = kind == DeckKind.Draw;
            string content = draw
                ? GameScenePresenter.FormatDrawDeck(Battle)
                : GameScenePresenter.FormatDiscardDeck(Battle);
            // Draw-deck panel on the left, discard-deck panel on the right, so they never overlap.
            float x = draw ? 28f : Screen.width - w - 28f;
            var rect = new Rect(x, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(rect, content, _panelStyle);
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
            GameSceneViewModel vm = GameScenePresenter.Create(Battle);
            MaybeOpenShop(vm);
            ApplyView(vm);
        }

        private void ApplyView(GameSceneViewModel vm)
        {
            _core = vm.Core;

            if (hud != null)
            {
                hud.Render(vm.Core);
                hud.SetGold(shop != null ? shop.Gold : 0);
            }

            // While the shop is open its presentation (merchant, hidden combat objects, goods) is owned
            // by ShopController; skip the combat re-render so it doesn't repaint the enemy over the merchant.
            if (shop != null && shop.IsOpen)
            {
                return;
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

            if (totals != null)
            {
                totals.Render(vm.Core.PlayerTotal, vm.Core.EnemyVisibleTotal);
            }
        }

        // Open the shop the moment a battle is won. Called from RefreshView, which lands on the true
        // post-turn state (BattleEnded is not itself a Stepped beat). ShopController.Open guards against
        // repeat opens, so this fires the shop exactly once per victory; a defeat opens no shop.
        private void MaybeOpenShop(GameSceneViewModel vm)
        {
            if (shop == null || shop.IsOpen ||
                vm.Core.State != CoreLoopState.BattleEnded ||
                vm.Core.Outcome != BattleOutcome.PlayerVictory)
            {
                return;
            }

            shop.Open();
        }

        // Leave the shop and start the next battle. Gold is KEPT by ShopController — it accumulates
        // across the run's battles; only a defeat restart resets it. TryRestart swaps in a fresh battle
        // and emits no Stepped events, so ProcessInput re-presents immediately via RefreshView.
        private bool LeaveShop()
        {
            bool restarted = _session.TryRestart();
            if (restarted && shop != null)
            {
                shop.Close();
            }

            return restarted;
        }

        // Restart after a defeat: a fresh run, so the shop closes (a no-op if it was never open) and
        // gold returns to 0.
        private bool RestartRun()
        {
            bool restarted = _session.TryRestart();
            if (restarted && shop != null)
            {
                shop.Close();
                shop.ResetGold();
            }

            return restarted;
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
