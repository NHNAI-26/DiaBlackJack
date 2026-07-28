using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop.UI
{
    public sealed class DemonContractChoiceViewModel
    {
        public DemonContractChoiceViewModel(
            int optionId,
            string title,
            string ability,
            string cost,
            bool canSelect,
            string disabledReason)
        {
            OptionId = optionId;
            Title = title ?? string.Empty;
            Ability = ability ?? string.Empty;
            Cost = cost ?? string.Empty;
            CanSelect = canSelect;
            DisabledReason = disabledReason ?? string.Empty;
        }

        public int OptionId { get; }

        public string Title { get; }

        public string Ability { get; }

        public string Cost { get; }

        public bool CanSelect { get; }

        public string DisabledReason { get; }

        public string ButtonLabel
        {
            get
            {
                string label = Title;
                if (!string.IsNullOrEmpty(Ability))
                {
                    label += "\n능력 · " + Ability;
                }

                if (!string.IsNullOrEmpty(Cost))
                {
                    label += "\n대가 · " + Cost;
                }

                if (!CanSelect && !string.IsNullOrEmpty(DisabledReason))
                {
                    label += "\n" + DisabledReason;
                }

                return label;
            }
        }
    }

    public sealed class DemonContractPanelViewModel
    {
        public DemonContractPanelViewModel(
            bool canBegin,
            DemonContractFailureReason failureReason,
            int soulCost,
            int soulAfterCost,
            int remainingBaseUses,
            string actionText,
            bool isResolving,
            int? interactionId,
            DemonContractInteractionKind? interactionKind,
            string prompt,
            IReadOnlyList<DemonContractChoiceViewModel> choices,
            IReadOnlyList<string> activeContracts,
            string ownerPreview,
            string lastContractResult,
            string lastEffectResult)
        {
            CanBegin = canBegin;
            FailureReason = failureReason;
            SoulCost = soulCost;
            SoulAfterCost = soulAfterCost;
            RemainingBaseUses = remainingBaseUses;
            ActionText = actionText ?? string.Empty;
            IsResolving = isResolving;
            InteractionId = interactionId;
            InteractionKind = interactionKind;
            Prompt = prompt ?? string.Empty;
            Choices = choices ?? throw new ArgumentNullException(nameof(choices));
            ActiveContracts = activeContracts ??
                throw new ArgumentNullException(nameof(activeContracts));
            OwnerPreview = ownerPreview ?? string.Empty;
            LastContractResult = lastContractResult ?? string.Empty;
            LastEffectResult = lastEffectResult ?? string.Empty;
        }

        public bool CanBegin { get; }

        public DemonContractFailureReason FailureReason { get; }

        public int SoulCost { get; }

        public int SoulAfterCost { get; }

        public int RemainingBaseUses { get; }

        public string ActionText { get; }

        public bool IsResolving { get; }

        public int? InteractionId { get; }

        public DemonContractInteractionKind? InteractionKind { get; }

        public string Prompt { get; }

        public IReadOnlyList<DemonContractChoiceViewModel> Choices { get; }

        public IReadOnlyList<string> ActiveContracts { get; }

        public string OwnerPreview { get; }

        public string LastContractResult { get; }

        public string LastEffectResult { get; }
    }

    public static class DemonContractPresenter
    {
        public static DemonContractPanelViewModel Create(CoreLoopBattle battle)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            DemonContractAvailability availability =
                battle.PlayerDemonContractAvailability;
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            return new DemonContractPanelViewModel(
                availability.CanBegin,
                availability.FailureReason,
                availability.SoulCost,
                availability.SoulAfterCost,
                availability.RemainingBaseUses,
                FormatActionText(availability),
                battle.State == CoreLoopState.PlayerResolvingDemonContract,
                pending?.InteractionId,
                pending?.Kind,
                pending?.PublicPrompt,
                FormatChoices(pending),
                FormatActiveContracts(battle),
                FormatOwnerPreview(battle.PlayerDemonContractPreview),
                FormatLastContractResult(battle.LastDemonContractResult),
                FormatLastEffectResult(battle.LastDemonContractEffectResult));
        }

        private static string FormatActionText(DemonContractAvailability availability)
        {
            if (availability.CanBegin)
            {
                return $"CONTRACT (-{availability.SoulCost} SOUL | " +
                    $"{availability.SoulAfterCost} LEFT)";
            }

            switch (availability.FailureReason)
            {
                case DemonContractFailureReason.BattleNotActive:
                    return "CONTRACT (BATTLE INACTIVE)";
                case DemonContractFailureReason.NotPlayerTurn:
                    return "CONTRACT (WAIT FOR TURN)";
                case DemonContractFailureReason.PlayerStanding:
                    return "CONTRACT (ALREADY STOOD)";
                case DemonContractFailureReason.PendingInteraction:
                    return "CONTRACT (CHOICE IN PROGRESS)";
                case DemonContractFailureReason.BaseUseLimitReached:
                    return "CONTRACT (USED)";
                case DemonContractFailureReason.InsufficientSoul:
                    return $"CONTRACT (NEED {availability.SoulCost + 1}+ SOUL)";
                case DemonContractFailureReason.InsufficientCandidates:
                    return "CONTRACT (NOT ENOUGH CARDS)";
                case DemonContractFailureReason.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IReadOnlyList<DemonContractChoiceViewModel> FormatChoices(
            PendingDemonContractInteraction pending)
        {
            if (pending == null)
            {
                return Array.AsReadOnly(Array.Empty<DemonContractChoiceViewModel>());
            }

            var choices = new List<DemonContractChoiceViewModel>(pending.Options.Count);
            foreach (DemonContractOption option in pending.Options)
            {
                DemonContractDefinition definition = pending.Kind ==
                    DemonContractInteractionKind.ChooseContract
                        ? FindDefinition(option)
                        : null;
                choices.Add(new DemonContractChoiceViewModel(
                    option.OptionId,
                    definition?.DisplayName ?? option.PublicLabel,
                    definition?.Summary ?? string.Empty,
                    definition?.CostSummary ?? string.Empty,
                    canSelect: true,
                    disabledReason: string.Empty));
            }

            return choices.AsReadOnly();
        }

        private static DemonContractDefinition FindDefinition(DemonContractOption option)
        {
            if (!string.IsNullOrEmpty(option.ContractDefinitionKey))
            {
                return DemonContractCatalog.Default.GetByKey(
                    option.ContractDefinitionKey);
            }

            foreach (DemonContractDefinition definition in
                DemonContractCatalog.Default.Definitions)
            {
                if (StringComparer.Ordinal.Equals(
                    definition.DisplayName,
                    option.PublicLabel))
                {
                    return definition;
                }
            }

            return null;
        }

        private static IReadOnlyList<string> FormatActiveContracts(
            CoreLoopBattle battle)
        {
            var contracts = new List<ActiveDemonContract>(
                battle.ActivePlayerDemonContracts.Count +
                battle.ActiveEnemyDemonContracts.Count);
            contracts.AddRange(battle.ActivePlayerDemonContracts);
            contracts.AddRange(battle.ActiveEnemyDemonContracts);

            var labels = new List<string>(contracts.Count);
            foreach (ActiveDemonContract contract in contracts)
            {
                string status;
                if (contract.RuntimeState is MammonRuntimeState mammon)
                {
                    status = $"주사위 {mammon.CurrentDieValue}" +
                        (mammon.CanRerollThisTurn
                            ? " · 재굴림 행동 가능"
                            : string.Empty);
                }
                else if (contract.RuntimeState is SatanRuntimeState satan)
                {
                    string face = satan.CurrentFace == SatanContractFace.Upper
                        ? "윗면"
                        : "아랫면";
                    string penalty = satan.PenaltyApplied
                        ? " · 대가 적용 완료"
                        : string.Empty;
                    status =
                        $"종말 카운트 {satan.RemainingDoomCount} · {face}{penalty}";
                }
                else if (contract.RuntimeState is BelphegorRuntimeState belphegor)
                {
                    status = belphegor.AutoStandPending
                        ? "다음 행동 후 자동 스탠드"
                        : "덱 위 카드 확인 준비";
                }
                else if (contract.Kind == DemonContractKind.Leviathan)
                {
                    status = "리볼버 최대 2회 발동";
                }
                else
                {
                    status = "효과 구현 예정";
                }

                string ownerPrefix = contract.OwnerSide == CombatantSide.Enemy
                    ? "상대 · "
                    : string.Empty;
                labels.Add(
                    $"{ownerPrefix}{contract.Definition.DisplayName} · {status}");
            }

            return labels.AsReadOnly();
        }

        private static string FormatOwnerPreview(PlayerDemonContractPreview preview)
        {
            return preview == null
                ? string.Empty
                : $"PLAYER ONLY · 덱 위 카드 {preview.Rank}";
        }

        private static string FormatLastContractResult(DemonContractResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            string ownerPrefix = result.ActiveContract.OwnerSide == CombatantSide.Enemy
                ? "상대 계약 완료"
                : "계약 완료";
            return $"{ownerPrefix} · {result.ActiveContract.Definition.DisplayName} · " +
                $"영혼 -{result.PaidSoulCost} · 현재 {result.OwnerSoulAfterResolution}";
        }

        private static string FormatLastEffectResult(DemonContractEffectResult result)
        {
            if (result == null || !result.Triggered)
            {
                return string.Empty;
            }

            if (result.BustedTarget.HasValue)
            {
                return result.BustedTarget.Value == CombatantSide.Player
                    ? "계약 효과 · 플레이어 버스트"
                    : "계약 효과 · 상대 버스트";
            }

            return result.PaidSoulCost > 0
                ? $"계약 대가 · 영혼 -{result.PaidSoulCost}"
                : "계약 효과 발동";
        }
    }
}
