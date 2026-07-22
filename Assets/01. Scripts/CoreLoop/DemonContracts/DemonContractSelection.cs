using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public enum DemonContractFailureReason
    {
        None,
        BattleNotActive,
        NotPlayerTurn,
        PlayerStanding,
        PendingInteraction,
        BaseUseLimitReached,
        InsufficientSoul,
        InsufficientCandidates
    }

    public enum DemonContractInteractionKind
    {
        ChooseContract,
        BelphegorTopCard,
        MammonReroll,
        MammonApplyDie
    }

    public sealed class DemonContractAvailability
    {
        internal DemonContractAvailability(
            DemonContractFailureReason failureReason,
            int soulCost,
            int soulAfterCost,
            int remainingBaseUses)
        {
            if (!Enum.IsDefined(typeof(DemonContractFailureReason), failureReason))
            {
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            }

            if (soulCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(soulCost));
            }

            if (soulAfterCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(soulAfterCost));
            }

            if (remainingBaseUses < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingBaseUses));
            }

            FailureReason = failureReason;
            SoulCost = soulCost;
            SoulAfterCost = soulAfterCost;
            RemainingBaseUses = remainingBaseUses;
        }

        public bool CanBegin => FailureReason == DemonContractFailureReason.None;

        public DemonContractFailureReason FailureReason { get; }

        public int RemainingBaseUses { get; }

        public int SoulAfterCost { get; }

        public int SoulCost { get; }
    }

    public sealed class DemonContractOption
    {
        public DemonContractOption(
            int optionId,
            int? contractCardId,
            int? numericValue,
            string publicLabel)
        {
            if (optionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(optionId));
            }

            if (contractCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contractCardId));
            }

            if (string.IsNullOrWhiteSpace(publicLabel))
            {
                throw new ArgumentException(
                    "Demon contract option label cannot be empty.",
                    nameof(publicLabel));
            }

            OptionId = optionId;
            ContractCardId = contractCardId;
            NumericValue = numericValue;
            PublicLabel = publicLabel.Trim();
        }

        public int? ContractCardId { get; }

        public int? NumericValue { get; }

        public int OptionId { get; }

        public string PublicLabel { get; }
    }

    public sealed class PendingDemonContractInteraction
    {
        private readonly ReadOnlyCollection<DemonContractOption> _options;

        public PendingDemonContractInteraction(
            int interactionId,
            DemonContractInteractionKind kind,
            DemonContractKind? contractKind,
            IEnumerable<DemonContractOption> options,
            string publicPrompt,
            int? sourceContractCardId = null)
        {
            if (interactionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interactionId));
            }

            if (!Enum.IsDefined(typeof(DemonContractInteractionKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (contractKind.HasValue &&
                !Enum.IsDefined(typeof(DemonContractKind), contractKind.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(contractKind));
            }

            if (sourceContractCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceContractCardId));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(publicPrompt))
            {
                throw new ArgumentException(
                    "Demon contract prompt cannot be empty.",
                    nameof(publicPrompt));
            }

            var copiedOptions = new List<DemonContractOption>();
            var optionIds = new HashSet<int>();
            var contractCardIds = new HashSet<int>();
            foreach (DemonContractOption option in options)
            {
                if (option == null)
                {
                    throw new ArgumentException(
                        "Demon contract options cannot contain null.",
                        nameof(options));
                }

                if (!optionIds.Add(option.OptionId))
                {
                    throw new ArgumentException(
                        $"Demon contract option id {option.OptionId} is duplicated.",
                        nameof(options));
                }

                if (option.ContractCardId.HasValue &&
                    !contractCardIds.Add(option.ContractCardId.Value))
                {
                    throw new ArgumentException(
                        $"Demon contract card id {option.ContractCardId.Value} is duplicated.",
                        nameof(options));
                }

                copiedOptions.Add(option);
            }

            if (copiedOptions.Count == 0)
            {
                throw new ArgumentException(
                    "Pending demon contract interaction requires an option.",
                    nameof(options));
            }

            if (kind == DemonContractInteractionKind.ChooseContract)
            {
                if (contractKind.HasValue || sourceContractCardId.HasValue ||
                    copiedOptions.Count != DemonContractDeck.CandidateCount)
                {
                    throw new ArgumentException(
                        "Contract choice requires exactly three options without an active contract kind.",
                        nameof(options));
                }

                foreach (DemonContractOption option in copiedOptions)
                {
                    if (!option.ContractCardId.HasValue || option.NumericValue.HasValue)
                    {
                        throw new ArgumentException(
                            "Contract choice options require only a physical card id.",
                            nameof(options));
                    }
                }
            }
            else if (kind == DemonContractInteractionKind.BelphegorTopCard)
            {
                if (contractKind != DemonContractKind.Belphegor ||
                    copiedOptions.Count != 2)
                {
                    throw new ArgumentException(
                        "Belphegor top-card choice requires two options for Belphegor.",
                        nameof(options));
                }

                foreach (DemonContractOption option in copiedOptions)
                {
                    if (option.ContractCardId.HasValue || option.NumericValue.HasValue)
                    {
                        throw new ArgumentException(
                            "Belphegor public options cannot expose the previewed card.",
                            nameof(options));
                    }
                }
            }
            else if (kind == DemonContractInteractionKind.MammonReroll ||
                kind == DemonContractInteractionKind.MammonApplyDie)
            {
                if (contractKind != DemonContractKind.Mammon ||
                    !sourceContractCardId.HasValue ||
                    copiedOptions.Count != 2)
                {
                    throw new ArgumentException(
                        "Mammon choice requires two options and a physical source contract.",
                        nameof(options));
                }

                foreach (DemonContractOption option in copiedOptions)
                {
                    if (option.ContractCardId.HasValue || option.NumericValue.HasValue)
                    {
                        throw new ArgumentException(
                            "Mammon public options cannot contain card or future roll data.",
                            nameof(options));
                    }
                }
            }

            InteractionId = interactionId;
            Kind = kind;
            ContractKind = contractKind;
            SourceContractCardId = sourceContractCardId;
            _options = copiedOptions.AsReadOnly();
            PublicPrompt = publicPrompt.Trim();
        }

        public DemonContractKind? ContractKind { get; }

        public int InteractionId { get; }

        public DemonContractInteractionKind Kind { get; }

        public IReadOnlyList<DemonContractOption> Options => _options;

        public string PublicPrompt { get; }

        public int? SourceContractCardId { get; }

        internal bool TryGetOption(int optionId, out DemonContractOption option)
        {
            foreach (DemonContractOption candidate in _options)
            {
                if (candidate.OptionId == optionId)
                {
                    option = candidate;
                    return true;
                }
            }

            option = null;
            return false;
        }
    }

    public sealed class PlayerDemonContractPreview
    {
        internal PlayerDemonContractPreview(
            int interactionId,
            int sourceContractCardId,
            DemonContractKind contractKind,
            BlackjackCard card)
        {
            if (interactionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interactionId));
            }

            if (sourceContractCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceContractCardId));
            }

            if (!Enum.IsDefined(typeof(DemonContractKind), contractKind))
            {
                throw new ArgumentOutOfRangeException(nameof(contractKind));
            }

            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            InteractionId = interactionId;
            SourceContractCardId = sourceContractCardId;
            ContractKind = contractKind;
            CardId = card.Id;
            DefinitionKey = card.DefinitionKey;
            Rank = card.Rank;
        }

        public int CardId { get; }

        public DemonContractKind ContractKind { get; }

        public string DefinitionKey { get; }

        public int InteractionId { get; }

        public int Rank { get; }

        public int SourceContractCardId { get; }
    }

    public abstract class DemonContractRuntimeState
    {
        protected DemonContractRuntimeState()
        {
        }
    }

    internal sealed class EmptyDemonContractRuntimeState : DemonContractRuntimeState
    {
    }

    public sealed class ActiveDemonContract
    {
        internal ActiveDemonContract(
            DemonContractCard sourceCard,
            CombatantSide ownerSide,
            DemonContractRuntimeState runtimeState)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            SourceCard = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
            RuntimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            OwnerSide = ownerSide;
        }

        public DemonContractDefinition Definition => SourceCard.Definition;

        public DemonContractKind Kind => Definition.Kind;

        public CombatantSide OwnerSide { get; }

        public DemonContractRuntimeState RuntimeState { get; private set; }

        public int SourceCardId => SourceCard.Id;

        internal DemonContractCard SourceCard { get; }

        internal void SetRuntimeState(DemonContractRuntimeState runtimeState)
        {
            RuntimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        }
    }

    public sealed class DemonContractResult
    {
        internal DemonContractResult(
            int interactionId,
            ActiveDemonContract activeContract,
            int paidSoulCost,
            int soulAfterBaseCost,
            int ownerSoulAfterResolution,
            bool endedBattle)
        {
            if (interactionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interactionId));
            }

            if (paidSoulCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paidSoulCost));
            }

            if (soulAfterBaseCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(soulAfterBaseCost));
            }

            if (ownerSoulAfterResolution < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSoulAfterResolution));
            }

            InteractionId = interactionId;
            ActiveContract = activeContract ?? throw new ArgumentNullException(nameof(activeContract));
            PaidSoulCost = paidSoulCost;
            SoulAfterBaseCost = soulAfterBaseCost;
            OwnerSoulAfterResolution = ownerSoulAfterResolution;
            EndedBattle = endedBattle;
        }

        public ActiveDemonContract ActiveContract { get; }

        public bool EndedBattle { get; }

        public int InteractionId { get; }

        public int OwnerSoulAfterResolution { get; }

        public bool OwnerSoulDepleted => OwnerSoulAfterResolution == 0;

        public int PaidSoulCost { get; }

        public int SoulAfterBaseCost { get; }
    }

    public sealed class DemonContractEffectResult
    {
        internal DemonContractEffectResult(
            bool triggered,
            CombatantSide? bustedTarget,
            int paidSoulCost)
        {
            if (bustedTarget.HasValue &&
                !Enum.IsDefined(typeof(CombatantSide), bustedTarget.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(bustedTarget));
            }

            if (paidSoulCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paidSoulCost));
            }

            if (!triggered && (bustedTarget.HasValue || paidSoulCost != 0))
            {
                throw new ArgumentException(
                    "An inactive contract effect cannot bust a target or pay soul.");
            }

            Triggered = triggered;
            BustedTarget = bustedTarget;
            PaidSoulCost = paidSoulCost;
        }

        public CombatantSide? BustedTarget { get; }

        public int PaidSoulCost { get; }

        public bool Triggered { get; }
    }
}
