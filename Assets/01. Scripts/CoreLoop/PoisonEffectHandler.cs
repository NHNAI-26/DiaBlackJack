using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class PoisonEffectHandler : IAutomaticCardEffectHandler
    {
        internal const int StandNowOptionId = 0;
        internal const int PaySoulOptionId = 1;
        internal const int SoulCost = 3;
        internal const int WinHealAmount = 5;

        public CardEffectKind EffectKind => CardEffectKind.Poison;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            var options = new List<AutomaticCardChoiceOption>(2);
            if (context.CanOwnerStand)
            {
                options.Add(new AutomaticCardChoiceOption(
                    StandNowOptionId,
                    "지금 즉시 스탠드"));
            }

            int soulLoss = Math.Min(SoulCost, context.OwnerCurrentSoul);
            options.Add(new AutomaticCardChoiceOption(
                PaySoulOptionId,
                $"영혼 {soulLoss}개 잃기"));

            return AutomaticCardEffectStep.AwaitChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.PoisonDecision,
                "독극물의 효과를 선택하세요.",
                options);
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            if (pendingInteraction.ChoiceKind !=
                AutomaticCardChoiceKind.PoisonDecision)
            {
                throw new InvalidOperationException(
                    "Poison received an invalid choice kind.");
            }

            switch (selectedOption.OptionId)
            {
                case StandNowOptionId:
                    if (!context.TryStandOwner())
                    {
                        throw new InvalidOperationException(
                            "Poison owner can no longer stand.");
                    }

                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.Discard);

                case PaySoulOptionId:
                    context.ApplyOwnerSoulDamage(SoulCost);
                    if (context.IsOwnerSoulDepleted)
                    {
                        return AutomaticCardEffectStep.Complete(
                            AutomaticCardSourceDisposition.Discard,
                            AutomaticCardCompletionFlow.EndBattle);
                    }

                    context.RegisterPoisonWinReward(WinHealAmount);
                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.Discard);

                default:
                    throw new InvalidOperationException(
                        "Poison received an unknown option.");
            }
        }
    }
}
