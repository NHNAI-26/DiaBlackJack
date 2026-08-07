using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the first-play tutorial's scripted sequence: shows the 0-6 section dialogue
    /// (data — see <see cref="TutorialScriptSO"/>, not hardcoded here) one line at a time
    /// through <see cref="TutorialNarratorView"/>, and — between dialogue blocks — opens a
    /// single-action gate on <see cref="GameManager"/> (which specific
    /// Hit/Stand/Change/revolver-number/contract-candidate/contract-option is legal) and
    /// waits for the player to actually perform it before advancing.
    ///
    /// Not a MonoBehaviour — mirrors <see cref="EnemySpeechDirector"/>'s shape: <see
    /// cref="GameManager"/> owns and constructs it, and drives it by calling <see
    /// cref="Observe"/> once per render (same points that already drive EnemySpeechDirector),
    /// rather than this type independently subscribing to <c>CoreLoopBattle.Stepped</c> and
    /// racing GameManager's own consumption of that event.
    ///
    /// Two bracketed stage directions in the user's script could not be reproduced exactly as
    /// written without weakening real engine rules, and are handled invisibly instead (see
    /// <see cref="TutorialEnemyPolicy"/> for both): the enemy's round-2 "리볼버" card is
    /// mechanically a Crystal Orb (rank 5 is Crystal Orb, not AutoPistol, in the current
    /// catalog) — harmless, since AutoPistol's guess is a pure rank comparison and doesn't
    /// care what the target card's own effect is. And the enemy's round-3 second action is a
    /// second silent Hit instead of the scripted Change, because <c>ShouldEnemyChange()</c>
    /// only allows an AI-initiated Change when its hidden card is already revealed or it is
    /// already bust; neither holds here, and forcing it would weaken a deliberate AI-fairness
    /// gate for one flavor beat that is never shown as narrator text anyway.
    /// </summary>
    internal sealed class TutorialDirector
    {
        // Any value that is never Hit/Stand/BeginChange makes all three fail the restriction's
        // equality check — i.e. "no primary action is legal", without needing a dedicated
        // tri-state on top of GameSceneCombatHudCommandKind?.
        private const GameSceneCombatHudCommandKind BlockAllPrimaryActions =
            GameSceneCombatHudCommandKind.BeginContract;

        private const int RevolverTargetNumber = 5;

        private readonly GameManager _gameManager;
        private readonly TutorialNarratorView _narrator;
        private readonly List<Step> _steps;

        private bool _begun;
        private int _stepIndex = -1;
        private int _dialogueLineIndex;
        private bool _gateActive;
        private bool _awaitingRoundOneReveal;
        private bool _awaitingRoundTwoReveal;
        private BattleSnapshot _gateEntrySnapshot;

        public TutorialDirector(
            GameManager gameManager,
            TutorialNarratorView narrator,
            TutorialScriptSO script)
        {
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            _narrator = narrator ?? throw new ArgumentNullException(nameof(narrator));
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            _narrator.LineAdvanceRequested += HandleLineAdvanceRequested;
            _steps = BuildSteps(script);
        }

        /// <summary>Fires once the intro (sections 0-1) dialogue's last line is dismissed.</summary>
        public event Action IntroCompleted;

        /// <summary>
        /// Fires once the post-round-1 soul-loss recap dialogue's last line is dismissed —
        /// only then may round 2's deal (suppressed since the Stand gate) actually appear.
        /// </summary>
        public event Action RoundOneRecapCompleted;

        /// <summary>Fires once the entire scripted sequence's final line is dismissed.</summary>
        public event Action Completed;

        public bool IsFinished => _begun && _stepIndex >= _steps.Count;

        public void BeginIntro()
        {
            if (_begun)
            {
                return;
            }

            _begun = true;
            AdvanceStep();
        }

        /// <summary>
        /// Call once per render (GameManager already drives <see cref="EnemySpeechDirector"/>
        /// from the same points). No-ops outside an active gate step.
        /// </summary>
        public void Observe()
        {
            if (!_begun || !_gateActive || _stepIndex >= _steps.Count)
            {
                return;
            }

            CoreLoopBattle battle = _gameManager.Battle;
            if (battle == null)
            {
                return;
            }

            var current = new BattleSnapshot(battle);
            if (current.Equals(_gateEntrySnapshot))
            {
                return;
            }

            // Mark the gate closed before firing OnExit — OnExit (e.g.
            // SetTutorialActionRestriction) synchronously calls back into
            // RefreshView -> Observe, and without this order flip that reentrant call
            // still sees an active gate with the same "changed" snapshot and re-fires
            // OnExit forever (stack overflow).
            _gateActive = false;
            _steps[_stepIndex].OnExit?.Invoke(_gameManager);
            AdvanceStep();
        }

        private void HandleLineAdvanceRequested()
        {
            if (!_begun || _stepIndex < 0 || _stepIndex >= _steps.Count)
            {
                return;
            }

            Step step = _steps[_stepIndex];
            if (step.Kind != StepKind.Dialogue)
            {
                return;
            }

            bool wasIntroStep = step.IsIntro;
            bool wasRoundOneRecapStep = step.DefersRoundTwoReveal;
            _dialogueLineIndex++;
            if (_dialogueLineIndex < step.Lines.Length)
            {
                _narrator.ShowLine(step.Lines[_dialogueLineIndex]);
                return;
            }

            if (wasIntroStep)
            {
                // Don't show the next dialogue yet — it would race the enemy-entrance +
                // round-1 card-deal reveal that IntroCompleted is about to kick off.
                // NotifyRoundOneRevealReady() is what actually calls AdvanceStep() here,
                // once that animation has genuinely finished playing.
                _narrator.Hide();
                _awaitingRoundOneReveal = true;
                IntroCompleted?.Invoke();
                return;
            }

            if (wasRoundOneRecapStep)
            {
                // Same idea as the intro case — round 2's deal was suppressed since the
                // Stand gate specifically so it wouldn't appear before this recap dialogue
                // finished. NotifyRoundTwoRevealReady() calls AdvanceStep() once it's
                // actually been revealed.
                _narrator.Hide();
                _awaitingRoundTwoReveal = true;
                RoundOneRecapCompleted?.Invoke();
                return;
            }

            AdvanceStep();
        }

        /// <summary>
        /// Called once the round-1 entrance + card-deal reveal animation (kicked off by
        /// <see cref="IntroCompleted"/>) has fully finished playing — only then does the
        /// post-intro dialogue actually appear. No-ops if the intro hasn't just finished.
        /// </summary>
        internal void NotifyRoundOneRevealReady()
        {
            if (!_awaitingRoundOneReveal)
            {
                return;
            }

            _awaitingRoundOneReveal = false;
            AdvanceStep();
        }

        /// <summary>
        /// Called once round 2's deal (kicked off by <see cref="RoundOneRecapCompleted"/>)
        /// has fully finished animating in — only then does the "한참 모자라는군" dialogue
        /// actually appear. No-ops if the recap dialogue hasn't just finished.
        /// </summary>
        internal void NotifyRoundTwoRevealReady()
        {
            if (!_awaitingRoundTwoReveal)
            {
                return;
            }

            _awaitingRoundTwoReveal = false;
            AdvanceStep();
        }

        private void AdvanceStep()
        {
            _stepIndex++;
            if (_stepIndex >= _steps.Count)
            {
                _narrator.Hide();
                Completed?.Invoke();
                return;
            }

            Step step = _steps[_stepIndex];
            if (step.Kind == StepKind.Dialogue)
            {
                _dialogueLineIndex = 0;
                _narrator.ShowLine(step.Lines[0]);
                return;
            }

            _narrator.Hide();
            _gateEntrySnapshot = new BattleSnapshot(_gameManager.Battle);
            _gateActive = true;
            step.OnEnter?.Invoke(_gameManager);
        }

        /// <summary>
        /// Converts each data-only <see cref="TutorialStepEntry"/> in <paramref name="script"/>
        /// into a driven <see cref="Step"/> — dialogue entries carry their own text straight
        /// through, gate entries resolve to the matching behavior factory below (the actual
        /// GameManager wiring is not "content," so it stays in code, not data).
        /// </summary>
        private static List<Step> BuildSteps(TutorialScriptSO script)
        {
            IReadOnlyList<TutorialStepEntry> entries = script.Steps;
            var steps = new List<Step>(entries.Count);
            foreach (TutorialStepEntry entry in entries)
            {
                steps.Add(entry.kind == TutorialStepKind.Dialogue
                    ? BuildDialogueStep(entry)
                    : BuildGateStep(entry));
            }

            return steps;
        }

        private static Step BuildDialogueStep(TutorialStepEntry entry)
        {
            return new Step
            {
                Kind = StepKind.Dialogue,
                Lines = entry.lines,
                IsIntro = entry.isIntro,
                DefersRoundTwoReveal = entry.defersRoundTwoReveal
            };
        }

        private static Step BuildGateStep(TutorialStepEntry entry)
        {
            switch (entry.gateKind)
            {
                case TutorialGateKind.Hit:
                    return PrimaryActionGate(GameSceneCombatHudCommandKind.Hit);
                case TutorialGateKind.Stand:
                    // Round 1's Stand gate: round 2's deal stays visually suppressed until
                    // the following soul-loss recap dialogue finishes — see
                    // RoundOneRecapCompleted/NotifyRoundTwoRevealReady.
                    return PrimaryActionGate(
                        GameSceneCombatHudCommandKind.Stand,
                        suppressHandRenderOnEnter: true);
                case TutorialGateKind.BeginChange:
                    return PrimaryActionGate(GameSceneCombatHudCommandKind.BeginChange);
                case TutorialGateKind.Revolver:
                    return RevolverGate();
                case TutorialGateKind.ContractCandidate:
                    return ContractCandidateGate(entry.contractDefinitionKey);
                case TutorialGateKind.ContractOption:
                    return ContractOptionGate(entry.contractOptionId);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(entry),
                        entry.gateKind,
                        "Unknown tutorial gate kind.");
            }
        }

        private static Step PrimaryActionGate(
            GameSceneCombatHudCommandKind allowedAction,
            bool suppressHandRenderOnEnter = false)
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm =>
                {
                    gm.SetTutorialActionRestriction(allowedAction);
                    if (suppressHandRenderOnEnter)
                    {
                        // Reused from the round-1 entrance suppression — keeps round 2's
                        // deal invisible until RoundOneRecapDialogue finishes (see
                        // RoundOneRecapCompleted/NotifyRoundTwoRevealReady), so the soul-loss
                        // explanation reads before the next round's cards appear.
                        gm.SuppressHandRenderUntilRoundOneStart();
                    }
                },
                OnExit = gm => gm.SetTutorialActionRestriction(null)
            };
        }

        private static Step RevolverGate()
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm =>
                {
                    gm.SetTutorialActionRestriction(BlockAllPrimaryActions);
                    // Card use defaults to blocked for the whole tutorial — this is the one
                    // gate that needs it open, since the player must click their own
                    // revolver-ranked card to use it.
                    gm.SetTutorialCardUseBlocked(false);
                    gm.SetTutorialRevolverTargetNumber(RevolverTargetNumber);
                },
                OnExit = gm =>
                {
                    gm.SetTutorialActionRestriction(null);
                    gm.SetTutorialCardUseBlocked(true);
                    gm.SetTutorialRevolverTargetNumber(null);
                }
            };
        }

        private static Step ContractCandidateGate(string definitionKey)
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm =>
                {
                    gm.SetTutorialActionRestriction(BlockAllPrimaryActions);
                    gm.SetTutorialContractPaperBlocked(false);
                    gm.SetTutorialContractRestriction(definitionKey);
                },
                OnExit = gm =>
                {
                    gm.SetTutorialActionRestriction(null);
                    gm.SetTutorialContractPaperBlocked(true);
                    gm.SetTutorialContractRestriction(null);
                }
            };
        }

        private static Step ContractOptionGate(int optionId)
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm => gm.SetTutorialContractOptionRestriction(optionId),
                OnExit = gm => gm.SetTutorialContractOptionRestriction(null)
            };
        }

        private enum StepKind
        {
            Dialogue,
            Gate
        }

        private sealed class Step
        {
            public StepKind Kind;
            public string[] Lines;
            public bool IsIntro;
            public bool DefersRoundTwoReveal;
            public Action<GameManager> OnEnter;
            public Action<GameManager> OnExit;
        }

        private readonly struct BattleSnapshot : IEquatable<BattleSnapshot>
        {
            public BattleSnapshot(CoreLoopBattle battle)
            {
                TurnNumber = battle?.TurnNumber ?? 0;
                RoundNumber = battle?.RoundNumber ?? 0;
                ResolutionId = battle?.LastResolution?.Id ?? 0;
            }

            private int TurnNumber { get; }

            private int RoundNumber { get; }

            private long ResolutionId { get; }

            public bool Equals(BattleSnapshot other)
            {
                return TurnNumber == other.TurnNumber &&
                    RoundNumber == other.RoundNumber &&
                    ResolutionId == other.ResolutionId;
            }

            public override bool Equals(object obj)
            {
                return obj is BattleSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + TurnNumber;
                    hash = hash * 31 + RoundNumber;
                    hash = hash * 31 + ResolutionId.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
