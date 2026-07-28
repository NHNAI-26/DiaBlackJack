using System;
using Border.Core;

namespace DiaBlackJack.CoreLoop
{
    internal interface IDemonDieRoller
    {
        int RollD6();
    }

    internal sealed class DeterministicDemonDieRoller : IDemonDieRoller
    {
        private readonly DeterministicRng _random = new DeterministicRng();

        public DeterministicDemonDieRoller(int seed)
        {
            _random.Reseed(seed);
        }

        public int RollD6()
        {
            return _random.Next(1, 7);
        }
    }

    public sealed class MammonRuntimeState : DemonContractRuntimeState
    {
        internal MammonRuntimeState(int currentDieValue)
        {
            SetDieValue(currentDieValue);
        }

        public int CurrentDieValue { get; private set; }

        public bool CanRerollThisTurn { get; private set; }

        public bool FinalChoiceResolved { get; private set; }

        internal void BeginOwnerTurn()
        {
            CanRerollThisTurn = true;
        }

        internal int Reroll(IDemonDieRoller dieRoller)
        {
            if (dieRoller == null)
            {
                throw new ArgumentNullException(nameof(dieRoller));
            }

            if (!CanRerollThisTurn)
            {
                throw new InvalidOperationException(
                    "Mammon die cannot be rerolled on this turn.");
            }

            SetDieValue(dieRoller.RollD6());
            CanRerollThisTurn = false;
            return CurrentDieValue;
        }

        internal void ResolveFinalChoice()
        {
            if (FinalChoiceResolved)
            {
                throw new InvalidOperationException(
                    "Mammon final die choice was already resolved this round.");
            }

            FinalChoiceResolved = true;
        }

        internal void ResetRound()
        {
            CanRerollThisTurn = false;
            FinalChoiceResolved = false;
        }

        private void SetDieValue(int value)
        {
            if (value < 1 || value > 6)
            {
                throw new InvalidOperationException(
                    "Mammon die roller must return a value from 1 through 6.");
            }

            CurrentDieValue = value;
        }
    }

    internal readonly struct MammonRerollResult
    {
        public MammonRerollResult(int currentDieValue, bool ownerBusted)
        {
            CurrentDieValue = currentDieValue;
            OwnerBusted = ownerBusted;
        }

        public int CurrentDieValue { get; }

        public bool OwnerBusted { get; }
    }

    internal interface IDemonContractMammonRerollHandler
    {
        bool CanReroll(DemonContractContext context);

        MammonRerollResult Reroll(DemonContractContext context);
    }

    internal interface IDemonContractFinalChoiceHandler
    {
        bool RequiresFinalChoice(DemonContractContext context);

        int ResolveFinalChoice(DemonContractContext context, int optionId);
    }

    internal sealed class MammonDemonContractHandler :
        IDemonContractHandler,
        IDemonContractOwnerTurnHandler,
        IDemonContractMammonRerollHandler,
        IDemonContractFinalChoiceHandler
    {
        internal const int DoNotApplyDieOptionId = 0;
        internal const int ApplyDieOptionId = 1;

        private readonly IDemonDieRoller _dieRoller;

        public MammonDemonContractHandler(IDemonDieRoller dieRoller)
        {
            _dieRoller = dieRoller ?? throw new ArgumentNullException(nameof(dieRoller));
        }

        public DemonContractKind Kind => DemonContractKind.Mammon;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new MammonRuntimeState(_dieRoller.RollD6());
        }

        public void OnOwnerTurnStarted(DemonContractContext context)
        {
            GetState(context).BeginOwnerTurn();
        }

        public bool TryConsumeAutoStandAfterOwnerAction(DemonContractContext context)
        {
            return false;
        }

        public void OnRoundEnded(DemonContractContext context)
        {
            GetState(context).ResetRound();
        }

        public bool CanReroll(DemonContractContext context)
        {
            return !context.OwnerIsStanding && GetState(context).CanRerollThisTurn;
        }

        public MammonRerollResult Reroll(DemonContractContext context)
        {
            if (!CanReroll(context))
            {
                throw new InvalidOperationException(
                    "Mammon cannot reroll outside an available owner turn.");
            }

            MammonRuntimeState state = GetState(context);
            state.Reroll(_dieRoller);

            return new MammonRerollResult(
                state.CurrentDieValue,
                ownerBusted: state.CurrentDieValue == 6);
        }

        public bool RequiresFinalChoice(DemonContractContext context)
        {
            return !GetState(context).FinalChoiceResolved;
        }

        public int ResolveFinalChoice(DemonContractContext context, int optionId)
        {
            MammonRuntimeState state = GetState(context);
            int bonus;
            switch (optionId)
            {
                case DoNotApplyDieOptionId:
                    bonus = 0;
                    break;
                case ApplyDieOptionId:
                    bonus = state.CurrentDieValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(optionId));
            }

            state.ResolveFinalChoice();
            return bonus;
        }

        private static MammonRuntimeState GetState(DemonContractContext context)
        {
            if (!(context.ActiveContract.RuntimeState is MammonRuntimeState runtimeState))
            {
                throw new InvalidOperationException(
                    "Mammon contract has an invalid runtime state.");
            }

            return runtimeState;
        }
    }
}
