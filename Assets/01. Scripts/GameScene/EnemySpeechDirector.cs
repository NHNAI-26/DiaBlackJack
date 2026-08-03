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
        private CoreLoopBattle _battle;
        private bool _battleStartConsumed;
        private bool _lowSoulConsumed;
        private bool _terminalConsumed;
        private int _lastRoundNumber;
        private int _lastActionOrdinal;
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
            _lastActionOrdinal = 0;
            _lastResolutionId = -1;
        }

        public bool TryResolve(
            EnemySpeechObservation observation,
            SpeechProfileSO profile,
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

            EnemySpeechCue actionCue = observation.ActionCue;
            if (observation.ActionOrdinal > _lastActionOrdinal)
            {
                _lastActionOrdinal = observation.ActionOrdinal;
                if (actionCue != null)
                {
                    Select(
                        actionCue.CueKey,
                        SpeechPriority.Action,
                        ref selectedKey,
                        ref selectedPriority);
                }
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

            if (observation.RoundNumber > 0 &&
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
