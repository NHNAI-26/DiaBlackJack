using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop.UI
{
    public sealed class DemonContractChoiceViewModel
    {
        public DemonContractChoiceViewModel(
            int optionId,
            string definitionKey,
            string title,
            string ability,
            string cost,
            bool canSelect,
            string disabledReason,
            int? cardId = null)
        {
            OptionId = optionId;
            DefinitionKey = definitionKey ?? string.Empty;
            Title = title ?? string.Empty;
            Ability = ability ?? string.Empty;
            Cost = cost ?? string.Empty;
            CanSelect = canSelect;
            DisabledReason = disabledReason ?? string.Empty;
            CardId = cardId;
        }

        public int OptionId { get; }

        public string DefinitionKey { get; }

        public string Title { get; }

        public string Ability { get; }

        public string Cost { get; }

        public bool CanSelect { get; }

        public string DisabledReason { get; }

        public int? CardId { get; }
    }

    public sealed class ActiveDemonContractActionViewModel
    {
        public ActiveDemonContractActionViewModel(
            int sourceCardId,
            DemonContractKind kind,
            string label)
        {
            SourceCardId = sourceCardId;
            Kind = kind;
            Label = label ?? string.Empty;
        }

        public int SourceCardId { get; }

        public DemonContractKind Kind { get; }

        public string Label { get; }
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
            IReadOnlyList<ActiveDemonContractActionViewModel> activeActions,
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
            ActiveActions = activeActions ??
                throw new ArgumentNullException(nameof(activeActions));
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

        public IReadOnlyList<ActiveDemonContractActionViewModel> ActiveActions
        {
            get;
        }

        public bool UsesContractCandidateLayout =>
            InteractionKind == DemonContractInteractionKind.ChooseContract ||
            InteractionKind ==
                DemonContractInteractionKind.LuciferChooseAdditionalContract;

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
                FormatActiveActions(battle),
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
                bool isContractChoice = pending.Kind ==
                        DemonContractInteractionKind.ChooseContract ||
                    pending.Kind == DemonContractInteractionKind
                        .LuciferChooseAdditionalContract;
                DemonContractDefinition definition = isContractChoice &&
                    option.ContractDefinitionKey != null
                        ? FindDefinition(option)
                        : null;
                choices.Add(new DemonContractChoiceViewModel(
                    option.OptionId,
                    definition?.Key ?? string.Empty,
                    definition?.DisplayName ?? option.PublicLabel,
                    definition?.Summary ?? string.Empty,
                    definition?.CostSummary ?? string.Empty,
                    canSelect: true,
                    disabledReason: string.Empty,
                    cardId: option.ContractCardId));
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
                            ? " · 턴 시작 선택 대기"
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
                    status = "리볼버 첫 실패 시 재예측";
                }
                else if (contract.Kind == DemonContractKind.Baphomet)
                {
                    status = "오망성 덱 적용";
                }
                else if (contract.Kind == DemonContractKind.Beelzebub)
                {
                    status = "버스트 시 영혼 1 · 양측 공개 카드 선택 폐기";
                }
                else if (contract.Kind == DemonContractKind.Asmodeus)
                {
                    status = "숫자 7 이하 카드 제한 · 강제 히트 선택";
                }
                else if (contract.Kind == DemonContractKind.Azazel)
                {
                    status = "중복 공개 숫자 버스트 · 공개 효과 순차 사용";
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

        private static IReadOnlyList<ActiveDemonContractActionViewModel>
            FormatActiveActions(CoreLoopBattle battle)
        {
            List<ActiveDemonContractActionViewModel> actions =
                new List<ActiveDemonContractActionViewModel>();
            foreach (ActiveDemonContract contract in
                battle.ActivePlayerDemonContracts)
            {
                if (!battle.CanBeginPlayerActiveDemonContractAction(
                    contract.SourceCardId))
                {
                    continue;
                }

                string actionLabel;
                switch (contract.Kind)
                {
                    case DemonContractKind.Mammon:
                        actionLabel = "MAMMON REROLL";
                        break;
                    case DemonContractKind.Satan:
                        SatanRuntimeState satan =
                            (SatanRuntimeState)contract.RuntimeState;
                        actionLabel = satan.CurrentFace == SatanContractFace.Upper
                            ? "SATAN DECLARE"
                            : "SATAN FORCE HIT";
                        break;
                    default:
                        continue;
                }

                actions.Add(new ActiveDemonContractActionViewModel(
                    contract.SourceCardId,
                    contract.Kind,
                    actionLabel));
            }

            return actions.AsReadOnly();
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
