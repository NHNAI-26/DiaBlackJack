using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    /// <summary>
    /// Scripted enemy behavior for the first-play tutorial battle — plays back one exact,
    /// hardcoded action per round rather than deciding anything, matching the tutorial script's
    /// sections 2-6 turn-by-turn (round 1: Hit, Hit, Stand; round 2: use the hidden manual card
    /// then take the first peeked card; round 3: Hit, then a second silent Hit). The script's
    /// own stage direction has the second round-3 action be a Change instead, but
    /// <c>CoreLoopBattle.ShouldEnemyChange()</c> only allows an AI-initiated Change when its
    /// hidden card is already revealed or it is already bust — neither is true at this exact
    /// scripted state, and forcing it would mean weakening a deliberate AI-fairness/realism
    /// gate for one flavor beat. That stage direction is bracketed (never shown as narrator
    /// text), so a quiet Hit in its place is mechanically invisible to the player — the only
    /// requirement is that it does *not* Stand, since Asmodeus's forced-hit turn-start choice
    /// (the round's actual finish) only offers itself while the opponent is still playable.
    /// Every step defensively searches <see cref="EnemyObservation.ActionCandidates"/> for a
    /// real match instead of constructing a candidate blind, since a rejected decision is asked
    /// again with the same observation rather than silently skipped.
    /// </summary>
    public sealed class TutorialEnemyPolicy : IEnemyBehaviorPolicy
    {
        private const string ReasonCode = "tutorial-scripted";

        private readonly Dictionary<int, Queue<Func<EnemyObservation, EnemyActionCandidate>>>
            _stepsByRound;

        public TutorialEnemyPolicy()
        {
            _stepsByRound = new Dictionary<
                int,
                Queue<Func<EnemyObservation, EnemyActionCandidate>>>
            {
                [1] = CreateQueue(FindHit, FindHit, FindStand),
                [2] = CreateQueue(FindBeginCardUse, o => FindCardEffectOption(o, 1)),
                [3] = CreateQueue(FindHit, FindHit)
            };
        }

        public EnemyDecision Decide(EnemyObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            EnemyActionCandidate candidate = null;
            if (_stepsByRound.TryGetValue(
                    observation.RoundNumber,
                    out Queue<Func<EnemyObservation, EnemyActionCandidate>> steps) &&
                steps.Count > 0)
            {
                candidate = steps.Peek()(observation);
                if (candidate != null)
                {
                    steps.Dequeue();
                }
            }

            candidate ??= FindStand(observation) ?? FindFirst(observation);
            if (candidate == null)
            {
                throw new InvalidOperationException(
                    "Tutorial enemy policy found no legal action to take.");
            }

            return candidate.ActionType == EnemyActionType.UseCard
                ? new EnemyDecision(
                    EnemyActionType.UseCard,
                    candidate.CardId,
                    candidate.CardEffectOptionId,
                    ReasonCode,
                    Array.Empty<EnemyActionScore>())
                : new EnemyDecision(candidate.ActionType, ReasonCode);
        }

        private static Queue<Func<EnemyObservation, EnemyActionCandidate>> CreateQueue(
            params Func<EnemyObservation, EnemyActionCandidate>[] steps)
        {
            return new Queue<Func<EnemyObservation, EnemyActionCandidate>>(steps);
        }

        private static EnemyActionCandidate FindHit(EnemyObservation observation)
        {
            return Find(observation, c => c.ActionType == EnemyActionType.Hit);
        }

        private static EnemyActionCandidate FindStand(EnemyObservation observation)
        {
            return Find(observation, c => c.ActionType == EnemyActionType.Stand);
        }

        private static EnemyActionCandidate FindBeginCardUse(EnemyObservation observation)
        {
            return Find(
                observation,
                c => c.ActionType == EnemyActionType.UseCard &&
                    !c.CardEffectOptionId.HasValue);
        }

        private static EnemyActionCandidate FindCardEffectOption(
            EnemyObservation observation,
            int optionId)
        {
            return Find(
                observation,
                c => c.ActionType == EnemyActionType.UseCard &&
                    c.CardEffectOptionId == optionId);
        }

        private static EnemyActionCandidate FindFirst(EnemyObservation observation)
        {
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                return candidate;
            }

            return null;
        }

        private static EnemyActionCandidate Find(
            EnemyObservation observation,
            Func<EnemyActionCandidate, bool> predicate)
        {
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (predicate(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }

    internal sealed class TutorialAutomaticCardDecisionPolicy :
        IAutomaticCardDecisionPolicy
    {
        private const int LieDetectorDeclaredNumber = 6;

        public static readonly TutorialAutomaticCardDecisionPolicy Instance =
            new TutorialAutomaticCardDecisionPolicy();

        private TutorialAutomaticCardDecisionPolicy()
        {
        }

        public AutomaticCardDecision Decide(
            AutomaticCardDecisionObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            if (observation.ChoiceKind ==
                AutomaticCardChoiceKind.LieDetectorNumber)
            {
                foreach (AutomaticCardOptionObservation option in
                    observation.Options)
                {
                    if (option.NumericValue == LieDetectorDeclaredNumber)
                    {
                        return new AutomaticCardDecision(
                            option.OptionId,
                            "tutorial-lie-detector-six");
                    }
                }
            }

            return DefaultAutomaticCardDecisionPolicy.Instance.Decide(
                observation);
        }
    }
}
