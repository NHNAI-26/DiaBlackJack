using System;
using System.Collections.Generic;
using Border.Core;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal sealed class SpeechLineResolver
    {
        private readonly DeterministicRng _random = new DeterministicRng();
        private readonly HashSet<string> _warnedMissingKeys =
            new HashSet<string>(StringComparer.Ordinal);

        public SpeechLineResolver(int seed)
        {
            _random.Reseed(seed);
        }

        public string Resolve(SpeechProfileSO profile, string cueKey)
        {
            return Resolve(profile, cueKey, fallbackCueKey: null);
        }

        public string Resolve(
            SpeechProfileSO profile,
            string cueKey,
            string fallbackCueKey)
        {
            if (string.IsNullOrWhiteSpace(cueKey))
            {
                return string.Empty;
            }

            if (profile != null &&
                profile.TryGetLines(cueKey, out IReadOnlyList<string> lines) &&
                lines.Count > 0)
            {
                return lines[_random.Next(lines.Count)];
            }

            if (!string.IsNullOrWhiteSpace(fallbackCueKey) &&
                profile != null &&
                profile.TryGetLines(
                    fallbackCueKey,
                    out IReadOnlyList<string> fallbackLines) &&
                fallbackLines.Count > 0)
            {
                return fallbackLines[_random.Next(fallbackLines.Count)];
            }

            string speakerKey = profile == null ? "<missing>" : profile.SpeakerKey;
            string warningKey = speakerKey + "\n" + cueKey;
            if (_warnedMissingKeys.Add(warningKey))
            {
                Debug.LogWarning(
                    $"Speech cue '{cueKey}' is missing for speaker '{speakerKey}'.");
            }

            return cueKey;
        }
    }

    internal enum SpeechPlaybackMoment
    {
        Any,
        BeforeAnimation,
        AfterAnimation,
    }

    internal sealed class EnemySpeechPresentation
    {
        public EnemySpeechPresentation(
            string cueKey,
            string message,
            SpeechPriority priority)
        {
            CueKey = cueKey;
            Message = message;
            Priority = priority;
        }

        public string CueKey { get; }

        public bool IsTerminal => Priority == SpeechPriority.Terminal;

        public string Message { get; }

        public SpeechPriority Priority { get; }
    }

    internal sealed class EnemySpeechDirector
    {
        private readonly SpeechLineResolver _resolver;
        private readonly HashSet<string> _consumedActionBeats =
            new HashSet<string>(StringComparer.Ordinal);
        private CoreLoopBattle _battle;
        private bool _battleStartConsumed;
        private bool _lowSoulConsumed;
        private bool _terminalConsumed;
        private int _lastRoundNumber;
        private long _lastResolutionId = -1;

        public EnemySpeechDirector(int seed)
        {
            _resolver = new SpeechLineResolver(seed);
        }

        public void Reset()
        {
            _battle = null;
            _battleStartConsumed = false;
            _lowSoulConsumed = false;
            _terminalConsumed = false;
            _lastRoundNumber = 0;
            _consumedActionBeats.Clear();
            _lastResolutionId = -1;
        }

        public bool TryResolve(
            EnemySpeechObservation observation,
            SpeechProfileSO profile,
            out EnemySpeechPresentation presentation)
        {
            return TryResolve(
                observation,
                profile,
                SpeechPlaybackMoment.Any,
                out presentation);
        }

        public bool TryResolve(
            EnemySpeechObservation observation,
            SpeechProfileSO profile,
            SpeechPlaybackMoment playbackMoment,
            out EnemySpeechPresentation presentation)
        {
            if (observation == null || observation.Battle == null)
            {
                presentation = null;
                return false;
            }

            if (!ReferenceEquals(_battle, observation.Battle))
            {
                Reset();
                _battle = observation.Battle;
            }

            if (TryResolveOrderedActionCue(
                    observation,
                    profile,
                    playbackMoment,
                    out presentation))
            {
                return true;
            }

            if (playbackMoment == SpeechPlaybackMoment.AfterAnimation)
            {
                presentation = null;
                return false;
            }

            if (playbackMoment == SpeechPlaybackMoment.BeforeAnimation &&
                HasUnconsumedOrderedCue(
                    observation,
                    EnemySpeechBeat.AfterEffect))
            {
                presentation = null;
                return false;
            }

            string selectedKey = null;
            SpeechPriority selectedPriority = 0;

            if (!_terminalConsumed &&
                observation.Outcome != BattleOutcome.InProgress)
            {
                _terminalConsumed = true;
                Select(
                    observation.Outcome == BattleOutcome.PlayerDefeat
                        ? SpeechCueKeys.Victory
                        : SpeechCueKeys.Defeat,
                    SpeechPriority.Terminal,
                    ref selectedKey,
                    ref selectedPriority);
            }

            if (!_lowSoulConsumed &&
                observation.EnemySoulCurrent > 0 &&
                observation.EnemySoulCurrent * 3 <=
                    observation.EnemySoulMaximum)
            {
                _lowSoulConsumed = true;
                Select(
                    SpeechCueKeys.LowSoul,
                    SpeechPriority.LowSoul,
                    ref selectedKey,
                    ref selectedPriority);
            }

            RoundResolution? resolution = observation.LastResolution;
            if (resolution.HasValue &&
                resolution.Value.Id != _lastResolutionId)
            {
                _lastResolutionId = resolution.Value.Id;
                if (resolution.Value.EnemyDamage > 0)
                {
                    Select(
                        ResolveDamageCueKey(resolution.Value.Cause),
                        SpeechPriority.Damage,
                        ref selectedKey,
                        ref selectedPriority);
                }
            }

            foreach (EnemySpeechCue actionCue in observation.ActionCues)
            {
                if (actionCue.RequiresOrderedPlayback ||
                    !TryConsumeActionCue(actionCue))
                {
                    continue;
                }

                Select(
                    actionCue.CueKey,
                    SpeechPriority.Action,
                    ref selectedKey,
                    ref selectedPriority);
                break;
            }

            if (!_battleStartConsumed)
            {
                _battleStartConsumed = true;
                Select(
                    SpeechCueKeys.BattleStart,
                    SpeechPriority.BattleStart,
                    ref selectedKey,
                    ref selectedPriority);
            }

            // Round 1's start is already announced by BattleStart (which always wins
            // priority over RoundStart in the same call anyway) — but if the very
            // first post-bind snapshot is rendered before RoundNumber settles at 1,
            // a later refresh could otherwise see RoundNumber go 0 -> 1 and fire
            // RoundStart on its own, stomping the still-fresh appearance line before
            // the player has had a chance to read it. Gating on > 1 makes that
            // impossible regardless of exact call ordering: only round 2+ ever gets
            // its own RoundStart cue.
            if (observation.RoundNumber > 1 &&
                observation.RoundNumber != _lastRoundNumber)
            {
                _lastRoundNumber = observation.RoundNumber;
                Select(
                    SpeechCueKeys.RoundStart,
                    SpeechPriority.RoundStart,
                    ref selectedKey,
                    ref selectedPriority);
            }

            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                presentation = null;
                return false;
            }

            presentation = new EnemySpeechPresentation(
                selectedKey,
                _resolver.Resolve(profile, selectedKey),
                selectedPriority);
            return true;
        }

        private bool TryResolveOrderedActionCue(
            EnemySpeechObservation observation,
            SpeechProfileSO profile,
            SpeechPlaybackMoment playbackMoment,
            out EnemySpeechPresentation presentation)
        {
            foreach (EnemySpeechCue cue in observation.ActionCues)
            {
                if (!cue.RequiresOrderedPlayback ||
                    !MatchesPlaybackMoment(cue.Beat, playbackMoment) ||
                    !TryConsumeActionCue(cue))
                {
                    continue;
                }

                presentation = new EnemySpeechPresentation(
                    cue.CueKey,
                    _resolver.Resolve(
                        profile,
                        cue.CueKey,
                        cue.FallbackCueKey),
                    SpeechPriority.Action);
                return true;
            }

            presentation = null;
            return false;
        }

        private bool HasUnconsumedOrderedCue(
            EnemySpeechObservation observation,
            EnemySpeechBeat beat)
        {
            foreach (EnemySpeechCue cue in observation.ActionCues)
            {
                if (cue.RequiresOrderedPlayback &&
                    cue.Beat == beat &&
                    !_consumedActionBeats.Contains(CreateActionBeatKey(cue)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryConsumeActionCue(EnemySpeechCue cue)
        {
            return _consumedActionBeats.Add(CreateActionBeatKey(cue));
        }

        private static string CreateActionBeatKey(EnemySpeechCue cue)
        {
            return $"{cue.EventKind}:{cue.RoundNumber}:{cue.ActionOrdinal}:" +
                $"{cue.SequenceIndex}:{cue.Beat}";
        }

        private static bool MatchesPlaybackMoment(
            EnemySpeechBeat beat,
            SpeechPlaybackMoment playbackMoment)
        {
            switch (playbackMoment)
            {
                case SpeechPlaybackMoment.Any:
                    return true;
                case SpeechPlaybackMoment.BeforeAnimation:
                    return beat == EnemySpeechBeat.BeforeEffect;
                case SpeechPlaybackMoment.AfterAnimation:
                    return beat == EnemySpeechBeat.AfterEffect;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(playbackMoment));
            }
        }

        internal static string ResolveDamageCueKey(RoundEndCause cause)
        {
            switch (cause)
            {
                case RoundEndCause.CardEffectBust:
                    return SpeechCueKeys.DamageCard;
                case RoundEndCause.TotalComparison:
                case RoundEndCause.NumericBust:
                    return SpeechCueKeys.DamageRound;
                default:
                    return SpeechCueKeys.DamageOther;
            }
        }

        private static void Select(
            string cueKey,
            SpeechPriority priority,
            ref string selectedKey,
            ref SpeechPriority selectedPriority)
        {
            if (priority < selectedPriority)
            {
                return;
            }

            selectedKey = cueKey;
            selectedPriority = priority;
        }
    }
}
