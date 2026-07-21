using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class OpponentSelectionCandidate
    {
        public OpponentSelectionCandidate(string profileKey, EnemyProfilePreview preview)
        {
            if (string.IsNullOrWhiteSpace(profileKey))
            {
                throw new ArgumentException(
                    "Opponent profile key cannot be empty.",
                    nameof(profileKey));
            }

            Preview = preview ?? throw new ArgumentNullException(nameof(preview));
            if (!StringComparer.Ordinal.Equals(profileKey, preview.ProfileKey))
            {
                throw new ArgumentException(
                    "Opponent profile key must match its preview.",
                    nameof(profileKey));
            }

            ProfileKey = profileKey;
        }

        public string ProfileKey { get; }

        public EnemyProfilePreview Preview { get; }
    }
}
