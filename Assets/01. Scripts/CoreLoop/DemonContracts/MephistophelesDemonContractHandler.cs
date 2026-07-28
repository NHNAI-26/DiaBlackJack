using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class MephistophelesRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class MephistophelesDemonContractHandler :
        IDemonContractHandler,
        IDemonContractAfterCardEffectHandler
    {
        public DemonContractKind Kind => DemonContractKind.Mephistopheles;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new MephistophelesRuntimeState();
        }

        public bool CanResolveAfterOwnerCardEffect(
            DemonContractContext context,
            CardEffectResult cardEffectResult)
        {
            return cardEffectResult.EffectKind == CardEffectKind.MilitaryKnife &&
                !cardEffectResult.EndedRound;
        }

        public DemonContractAfterCardEffectStep ResolveAfterOwnerCardEffect(
            DemonContractContext context,
            CardEffectResult cardEffectResult)
        {
            if (!CanResolveAfterOwnerCardEffect(context, cardEffectResult))
            {
                throw new InvalidOperationException(
                    "Mephistopheles can only resolve a military knife effect that did not end the round.");
            }

            context.TryRevealOwnerHiddenCard();
            return new DemonContractAfterCardEffectStep(
                new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    paidSoulCost: 0),
                roundResolution: null);
        }
    }
}
