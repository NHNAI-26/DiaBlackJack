using System;

namespace DiaBlackJack.StageProgression
{
    public sealed class PendingBattleReward
    {
        internal PendingBattleReward(
            BattleRewardOffer offer,
            BattleRewardCompletionTarget completionTarget)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));

            if (completionTarget != BattleRewardCompletionTarget.StageCleared &&
                completionTarget != BattleRewardCompletionTarget.RunVictory)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completionTarget),
                    completionTarget,
                    "Unknown battle reward completion target.");
            }

            CompletionTarget = completionTarget;
        }

        public BattleRewardOffer Offer { get; }

        public BattleRewardCompletionTarget CompletionTarget { get; }
    }
}
