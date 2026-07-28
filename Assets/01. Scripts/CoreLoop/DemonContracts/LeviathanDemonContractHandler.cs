using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

    internal interface IDemonContractCardEffectRepeatHandler
    {
        bool SupportsOwnerCardEffect(
            DemonContractContext context,
            CardEffectKind effectKind);

        bool RequiresAdditionalActivation(
            DemonContractContext context,
            CardEffectResult cardEffectResult);
    }

    public sealed class LeviathanCardEffectResult
    {
        private readonly ReadOnlyCollection<bool> _activationSuccesses;

        internal LeviathanCardEffectResult(
            IEnumerable<bool> activationSuccesses,
            CombatantSide? bustedTarget,
            int paidSoulCost)
        {
            if (activationSuccesses == null)
            {
                throw new ArgumentNullException(nameof(activationSuccesses));
            }

            List<bool> copiedSuccesses = new List<bool>(activationSuccesses);
            if (copiedSuccesses.Count < 1 || copiedSuccesses.Count > 2)
            {
                throw new ArgumentException(
                    "Leviathan must resolve one or two auto-pistol activations.",
                    nameof(activationSuccesses));
            }

            if (bustedTarget.HasValue &&
                !Enum.IsDefined(typeof(CombatantSide), bustedTarget.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(bustedTarget));
            }

            bool twoFailures = copiedSuccesses.Count == 2 &&
                !copiedSuccesses[0] &&
                !copiedSuccesses[1];
            int expectedSoulCost = !bustedTarget.HasValue && twoFailures
                ? 1
                : 0;
            if (paidSoulCost != expectedSoulCost)
            {
                throw new ArgumentOutOfRangeException(nameof(paidSoulCost));
            }

            _activationSuccesses = copiedSuccesses.AsReadOnly();
            BustedTarget = bustedTarget;
            PaidSoulCost = paidSoulCost;
        }

        public IReadOnlyList<bool> ActivationSuccesses => _activationSuccesses;

        public CombatantSide? BustedTarget { get; }

        public int PaidSoulCost { get; }
    }

    internal sealed class LeviathanCardEffectSequence
    {
        public LeviathanCardEffectSequence(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int sourceCardId,
            bool firstActivationSucceeded)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            if (sourceContractCardId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceContractCardId));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            OwnerSide = ownerSide;
            SourceContractCardId = sourceContractCardId;
            SourceCardId = sourceCardId;
            FirstActivationSucceeded = firstActivationSucceeded;
        }

        public bool FirstActivationSucceeded { get; }

        public CombatantSide OwnerSide { get; }

        public int SourceCardId { get; }

        public int SourceContractCardId { get; }
    }

    public sealed class LeviathanRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class LeviathanDemonContractHandler :
        IDemonContractHandler,
        IDemonContractCardEffectRepeatHandler,
        IDemonContractAfterCardEffectHandler
    {
        public DemonContractKind Kind => DemonContractKind.Leviathan;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new LeviathanRuntimeState();
        }

        public bool SupportsOwnerCardEffect(
            DemonContractContext context,
            CardEffectKind effectKind)
        {
            return effectKind == CardEffectKind.AutoPistol;
        }

        public bool RequiresAdditionalActivation(
            DemonContractContext context,
            CardEffectResult cardEffectResult)
        {
            return SupportsOwnerCardEffect(context, cardEffectResult.EffectKind) &&
                !cardEffectResult.Succeeded &&
                !cardEffectResult.EndedRound;
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
                    "Leviathan can only charge for two failed auto-pistol activations.");
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
