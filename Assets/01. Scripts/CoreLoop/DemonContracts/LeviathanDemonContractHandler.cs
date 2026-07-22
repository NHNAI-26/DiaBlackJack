using System;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class DemonContractAfterCardEffectStep
    {
        public DemonContractAfterCardEffectStep(
            DemonContractEffectResult result,
            RoundResolution? roundResolution)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            RoundResolution = roundResolution;
        }

        public DemonContractEffectResult Result { get; }

        public RoundResolution? RoundResolution { get; }
    }

    internal interface IDemonContractAfterCardEffectHandler
    {
        bool CanResolveAfterOwnerCardEffect(
            DemonContractContext context,
            CardEffectResult cardEffectResult);

        DemonContractAfterCardEffectStep ResolveAfterOwnerCardEffect(
            DemonContractContext context,
            CardEffectResult cardEffectResult);
    }

    public sealed class LeviathanRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class LeviathanDemonContractHandler :
        IDemonContractHandler,
        IDemonContractAfterCardEffectHandler
    {
        public DemonContractKind Kind => DemonContractKind.Leviathan;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new LeviathanRuntimeState();
        }

        public bool CanResolveAfterOwnerCardEffect(
            DemonContractContext context,
            CardEffectResult cardEffectResult)
        {
            return cardEffectResult.EffectKind == CardEffectKind.AutoPistol &&
                !cardEffectResult.Succeeded &&
                !cardEffectResult.EndedRound;
        }

        public DemonContractAfterCardEffectStep ResolveAfterOwnerCardEffect(
            DemonContractContext context,
            CardEffectResult cardEffectResult)
        {
            if (!CanResolveAfterOwnerCardEffect(context, cardEffectResult))
            {
                throw new InvalidOperationException(
                    "Leviathan can only follow a failed auto-pistol effect.");
            }

            if (context.OpponentHandValue.IsBust)
            {
                return new DemonContractAfterCardEffectStep(
                    new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: context.OpponentSide,
                        paidSoulCost: 0),
                    context.CreateOpponentContractEffectBustResolution());
            }

            context.ApplyOwnerSoulDamage(1);
            return new DemonContractAfterCardEffectStep(
                new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    paidSoulCost: 1),
                roundResolution: null);
        }
    }
}
