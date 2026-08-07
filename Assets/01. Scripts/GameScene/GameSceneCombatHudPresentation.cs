using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;

namespace DiaBlackJack.GameScene
{
    public enum GameSceneCombatHudMode
    {
        Hidden,
        Actions,
        Options,
        DiegeticSelection,
        ContractCandidates,
        RevolverNumberSelection,
        SatanNumberSelection,
        ReturningToRun,
        Restart
    }

    public enum GameSceneCombatHudCommandKind
    {
        Hit,
        Stand,
        BeginChange,
        SelectChangedCard,
        BeginContract,
        ResolveCardEffectChoice,
        ResolveAutomaticCardChoice,
        ResolveDemonContractChoice,
        BeginActiveDemonContractAction,
        Restart,
        ConfirmSatanNumberSelection
    }

    public enum GameSceneCombatHudActionPlacement
    {
        Default,
        BottomRight,
        Center
    }

    /// <summary>Immutable input payload emitted by the scene-authored combat HUD.</summary>
    public readonly struct GameSceneCombatHudCommand
    {
        public GameSceneCombatHudCommand(
            GameSceneCombatHudCommandKind kind,
            int optionId = -1,
            int interactionId = -1)
        {
            Kind = kind;
            OptionId = optionId;
            InteractionId = interactionId;
        }

        public GameSceneCombatHudCommandKind Kind { get; }

        public int OptionId { get; }

        public int InteractionId { get; }
    }

    public sealed class GameSceneCombatHudActionViewModel
    {
        public GameSceneCombatHudActionViewModel(
            GameSceneCombatHudCommand command,
            string label,
            bool isInteractable,
            string tooltip = "",
            GameSceneCombatHudActionPlacement placement =
                GameSceneCombatHudActionPlacement.Default)
        {
            Command = command;
            Label = label ?? string.Empty;
            IsInteractable = isInteractable;
            Tooltip = tooltip ?? string.Empty;
            Placement = placement;
        }

        public GameSceneCombatHudCommand Command { get; }

        public string Label { get; }

        public bool IsInteractable { get; }

        public string Tooltip { get; }

        public GameSceneCombatHudActionPlacement Placement { get; }
    }

    public sealed class GameSceneCombatHudContractCandidateViewModel
    {
        public GameSceneCombatHudContractCandidateViewModel(
            GameSceneCombatHudCommand command,
            string definitionKey,
            string title,
            string ability,
            string cost,
            bool isInteractable)
        {
            Command = command;
            DefinitionKey = definitionKey ?? string.Empty;
            EnglishName = DefinitionKey.ToUpperInvariant();
            Title = title ?? string.Empty;
            Ability = ability ?? string.Empty;
            Cost = cost ?? string.Empty;
            IsInteractable = isInteractable;
        }

        public GameSceneCombatHudCommand Command { get; }

        public string DefinitionKey { get; }

        public string EnglishName { get; }

        public string Title { get; }

        public string Ability { get; }

        public string Cost { get; }

        public bool IsInteractable { get; }
    }

    /// <summary>Pure projection of current battle input into scene HUD controls.</summary>
    public sealed class GameSceneCombatHudViewModel
    {
        public GameSceneCombatHudViewModel(
            GameSceneCombatHudMode mode,
            CombatPromptRequest? selectionPrompt,
            string headerText,
            IReadOnlyList<GameSceneCombatHudActionViewModel> primaryActions,
            IReadOnlyList<GameSceneCombatHudActionViewModel> optionActions,
            IReadOnlyList<GameSceneCombatHudContractCandidateViewModel> contractCandidates,
            string automaticCardResult)
        {
            Mode = mode;
            SelectionPrompt = selectionPrompt;
            HeaderText = headerText ?? string.Empty;
            PrimaryActions = primaryActions ?? Array.Empty<GameSceneCombatHudActionViewModel>();
            OptionActions = optionActions ?? Array.Empty<GameSceneCombatHudActionViewModel>();
            ContractCandidates = contractCandidates ??
                Array.Empty<GameSceneCombatHudContractCandidateViewModel>();
            AutomaticCardResult = automaticCardResult ?? string.Empty;
        }

        public GameSceneCombatHudMode Mode { get; }

        public CombatPromptRequest? SelectionPrompt { get; }

        public string HeaderText { get; }

        public IReadOnlyList<GameSceneCombatHudActionViewModel> PrimaryActions { get; }

        public IReadOnlyList<GameSceneCombatHudActionViewModel> OptionActions { get; }

        public IReadOnlyList<GameSceneCombatHudContractCandidateViewModel> ContractCandidates { get; }

        public string AutomaticCardResult { get; }
    }

    public static class GameSceneCombatHudPresenter
    {
        public static GameSceneCombatHudViewModel Create(
            CoreLoopViewModel core,
            bool isStageBattle,
            bool isShopOpen,
            bool inputLocked,
            bool usesDiegeticCardEffectSelection = false,
            bool hideForPresentation = false,
            int satanSelectedNumberCount = 0,
            GameSceneCombatHudCommandKind? restrictedPrimaryAction = null,
            string restrictedContractDefinitionKey = null,
            int? restrictedOptionId = null)
        {
            if (core == null || isShopOpen || hideForPresentation)
            {
                return CreateHidden();
            }

            string automaticCardResult = FormatAutomaticCardResult(core.AutomaticCardResult);
            CombatPromptRequest? selectionPrompt = inputLocked
                ? null
                : core.SelectionPrompt;
            if (core.State == CoreLoopState.BattleEnded)
            {
                return new GameSceneCombatHudViewModel(
                    isStageBattle
                        ? GameSceneCombatHudMode.ReturningToRun
                        : GameSceneCombatHudMode.Restart,
                    selectionPrompt: null,
                    isStageBattle ? "RETURNING TO RUN" : "BATTLE ENDED",
                    Array.Empty<GameSceneCombatHudActionViewModel>(),
                    isStageBattle
                        ? Array.Empty<GameSceneCombatHudActionViewModel>()
                        : new[]
                        {
                            CreateAction(
                                GameSceneCombatHudCommandKind.Restart,
                                "RESTART",
                                core.CanRestart && !inputLocked)
                        },
                    Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                    automaticCardResult);
            }

            if (core.IsChoosingChangeCard)
            {
                return new GameSceneCombatHudViewModel(
                    GameSceneCombatHudMode.DiegeticSelection,
                    selectionPrompt,
                    string.Empty,
                    Array.Empty<GameSceneCombatHudActionViewModel>(),
                    Array.Empty<GameSceneCombatHudActionViewModel>(),
                    Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                    automaticCardResult);
            }

            if (core.IsResolvingAutomaticCardEffect)
            {
                AutomaticCardInteractionViewModel interaction = core.AutomaticCardInteraction;
                if (interaction == null)
                {
                    return CreateHidden();
                }

                var options = new List<GameSceneCombatHudActionViewModel>();
                foreach (AutomaticCardChoiceViewModel choice in interaction.Choices)
                {
                    if (choice.CardId.HasValue)
                    {
                        continue;
                    }

                    options.Add(new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice,
                            choice.OptionId,
                            interaction.InteractionId),
                        choice.Label,
                        !inputLocked,
                        placement: IsBottomRightAutomaticCardAction(
                            interaction.ChoiceKind,
                            choice.OptionId)
                                ? GameSceneCombatHudActionPlacement.BottomRight
                                : GameSceneCombatHudActionPlacement.Default));
                }

                if (interaction.EffectKind == CardEffectKind.LieDetector &&
                    interaction.ChoiceKind ==
                        AutomaticCardChoiceKind.LieDetectorNumber)
                {
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.RevolverNumberSelection,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        options,
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                bool usesDirectCardSelection = HasCardChoice(interaction.Choices);
                if (usesDirectCardSelection)
                {
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.DiegeticSelection,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        options,
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                return CreateOptions(
                    selectionPrompt,
                    options,
                    automaticCardResult);
            }

            if (core.IsResolvingCardEffect)
            {
                if (core.PendingCardEffectKind == CardEffectKind.AutoPistol)
                {
                    if (inputLocked)
                    {
                        return CreateHidden();
                    }

                    var revolverOptions =
                        new List<GameSceneCombatHudActionViewModel>();
                    foreach (CardEffectChoiceViewModel choice in
                        core.CardEffectChoices)
                    {
                        revolverOptions.Add(new GameSceneCombatHudActionViewModel(
                            new GameSceneCombatHudCommand(
                                GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                                choice.OptionId),
                            choice.Label,
                            !inputLocked));
                    }

                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.RevolverNumberSelection,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        revolverOptions,
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                if (usesDiegeticCardEffectSelection)
                {
                    var directOptions =
                        new List<GameSceneCombatHudActionViewModel>();
                    foreach (CardEffectChoiceViewModel choice in core.CardEffectChoices)
                    {
                        if (choice.CardId.HasValue)
                        {
                            continue;
                        }

                        directOptions.Add(new GameSceneCombatHudActionViewModel(
                            new GameSceneCombatHudCommand(
                                GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                                choice.OptionId),
                            choice.Label,
                            !inputLocked,
                            placement: core.PendingCardEffectKind ==
                                CardEffectKind.CrystalOrb ||
                                core.PendingCardEffectKind ==
                                CardEffectKind.Flamethrower
                                    ? GameSceneCombatHudActionPlacement.BottomRight
                                    : GameSceneCombatHudActionPlacement.Default));
                    }

                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.DiegeticSelection,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        directOptions,
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                var options = new List<GameSceneCombatHudActionViewModel>();
                foreach (CardEffectChoiceViewModel choice in core.CardEffectChoices)
                {
                    options.Add(new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                            choice.OptionId),
                        choice.Label,
                        !inputLocked));
                }

                return CreateOptions(selectionPrompt, options, automaticCardResult);
            }

            DemonContractPanelViewModel contract = core.DemonContract;
            if (contract.IsResolving)
            {
                if (contract.InteractionKind ==
                    DemonContractInteractionKind.SatanDeclareFirstNumber)
                {
                    int selectedCount = Math.Max(
                        0,
                        Math.Min(2, satanSelectedNumberCount));
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.SatanNumberSelection,
                        selectionPrompt?.WithCounts(selectedCount, 2),
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        new[]
                        {
                            new GameSceneCombatHudActionViewModel(
                                new GameSceneCombatHudCommand(
                                    GameSceneCombatHudCommandKind
                                        .ConfirmSatanNumberSelection,
                                    interactionId:
                                        contract.InteractionId ?? -1),
                                "선택 완료",
                                selectedCount == 2 && !inputLocked,
                                // Satan's forward-facing ability (declare two numbers);
                                // its confirm button is centered on screen rather than
                                // the usual bottom-right corner.
                                placement:
                                    GameSceneCombatHudActionPlacement
                                        .Center)
                        },
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                if (contract.InteractionKind ==
                    DemonContractInteractionKind.SatanDeclareSecondNumber)
                {
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.SatanNumberSelection,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                if (contract.InteractionKind ==
                        DemonContractInteractionKind.BeelzebubChooseOwnerCard ||
                    contract.InteractionKind ==
                        DemonContractInteractionKind.BeelzebubChooseOpponentCard)
                {
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.DiegeticSelection,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                if (contract.InteractionKind ==
                        DemonContractInteractionKind.ChooseContract &&
                    contract.Choices.Count <= 2)
                {
                    var candidates =
                        new List<GameSceneCombatHudContractCandidateViewModel>();
                    foreach (DemonContractChoiceViewModel choice in contract.Choices)
                    {
                        candidates.Add(
                            new GameSceneCombatHudContractCandidateViewModel(
                                new GameSceneCombatHudCommand(
                                    GameSceneCombatHudCommandKind
                                        .ResolveDemonContractChoice,
                                    choice.OptionId,
                                    contract.InteractionId ?? -1),
                                choice.DefinitionKey,
                                choice.Title,
                                choice.Ability,
                                choice.Cost,
                                IsContractCandidateAllowed(
                                    choice.DefinitionKey,
                                    restrictedContractDefinitionKey) &&
                                    choice.CanSelect && !inputLocked));
                    }

                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.ContractCandidates,
                        selectionPrompt,
                        string.Empty,
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        candidates,
                        automaticCardResult);
                }

                var options = new List<GameSceneCombatHudActionViewModel>();
                foreach (DemonContractChoiceViewModel choice in contract.Choices)
                {
                    string label = choice.Title;
                    if (!string.IsNullOrEmpty(choice.Cost))
                    {
                        label += "\n" + choice.Cost;
                    }

                    if (!choice.CanSelect &&
                        !string.IsNullOrEmpty(choice.DisabledReason))
                    {
                        label += "\n" + choice.DisabledReason;
                    }

                    options.Add(new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveDemonContractChoice,
                            choice.OptionId,
                            contract.InteractionId ?? -1),
                        label,
                        IsOptionAllowed(choice.OptionId, restrictedOptionId) &&
                            choice.CanSelect && !inputLocked,
                        placement: IsBottomRightContractAction(
                            contract.InteractionKind,
                            choice.OptionId)
                                ? GameSceneCombatHudActionPlacement.BottomRight
                                : GameSceneCombatHudActionPlacement.Default));
                }

                return CreateOptions(
                    selectionPrompt,
                    options,
                    automaticCardResult);
            }

            var primaryActions = new List<GameSceneCombatHudActionViewModel>
            {
                CreateAction(
                    GameSceneCombatHudCommandKind.Hit,
                    "HIT",
                    IsPrimaryActionAllowed(
                        GameSceneCombatHudCommandKind.Hit,
                        restrictedPrimaryAction) &&
                        core.CanHit && !inputLocked),
                CreateAction(
                    GameSceneCombatHudCommandKind.Stand,
                    "STAND",
                    IsPrimaryActionAllowed(
                        GameSceneCombatHudCommandKind.Stand,
                        restrictedPrimaryAction) &&
                        core.CanStand && !inputLocked),
                CreateAction(
                    GameSceneCombatHudCommandKind.BeginChange,
                    FormatChangeLabel(core.ChangeActionText),
                    IsPrimaryActionAllowed(
                        GameSceneCombatHudCommandKind.BeginChange,
                        restrictedPrimaryAction) &&
                        core.CanChange && !inputLocked)
            };

            var activeContractActions = new List<GameSceneCombatHudActionViewModel>();
            foreach (ActiveDemonContractActionViewModel action in contract.ActiveActions)
            {
                if (action.Kind == DemonContractKind.Satan ||
                    action.Kind == DemonContractKind.Mammon)
                {
                    continue;
                }

                activeContractActions.Add(CreateAction(
                    GameSceneCombatHudCommandKind.BeginActiveDemonContractAction,
                    action.Label,
                    !inputLocked,
                    optionId: action.SourceCardId));
            }

            return new GameSceneCombatHudViewModel(
                GameSceneCombatHudMode.Actions,
                selectionPrompt: null,
                activeContractActions.Count == 0 ? string.Empty : "ACTIVE CONTRACTS",
                primaryActions,
                activeContractActions,
                Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                automaticCardResult);
        }

        private static GameSceneCombatHudViewModel CreateHidden()
        {
            return new GameSceneCombatHudViewModel(
                GameSceneCombatHudMode.Hidden,
                selectionPrompt: null,
                string.Empty,
                Array.Empty<GameSceneCombatHudActionViewModel>(),
                Array.Empty<GameSceneCombatHudActionViewModel>(),
                Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                string.Empty);
        }

        private static GameSceneCombatHudViewModel CreateOptions(
            CombatPromptRequest? selectionPrompt,
            IReadOnlyList<GameSceneCombatHudActionViewModel> options,
            string automaticCardResult)
        {
            return new GameSceneCombatHudViewModel(
                GameSceneCombatHudMode.Options,
                selectionPrompt,
                string.Empty,
                Array.Empty<GameSceneCombatHudActionViewModel>(),
                options,
                Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                automaticCardResult);
        }

        private static GameSceneCombatHudActionViewModel CreateAction(
            GameSceneCombatHudCommandKind kind,
            string label,
            bool isInteractable,
            string tooltip = "",
            int optionId = -1,
            GameSceneCombatHudActionPlacement placement =
                GameSceneCombatHudActionPlacement.Default)
        {
            return new GameSceneCombatHudActionViewModel(
                new GameSceneCombatHudCommand(kind, optionId),
                label,
                isInteractable,
                tooltip,
                placement);
        }

        internal static bool IsBottomRightContractAction(
            DemonContractInteractionKind? interactionKind,
            int optionId)
        {
            switch (interactionKind)
            {
                case DemonContractInteractionKind.BelphegorTopCard:
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                case DemonContractInteractionKind.SatanTurnStartChoice:
                case DemonContractInteractionKind.MammonApplyDie:
                    return true;
                case DemonContractInteractionKind.MammonReroll:
                    return optionId ==
                        MammonDemonContractHandler.KeepDieOptionId;
                default:
                    return false;
            }
        }

        internal static bool IsBottomRightAutomaticCardAction(
            AutomaticCardChoiceKind choiceKind,
            int optionId)
        {
            switch (choiceKind)
            {
                case AutomaticCardChoiceKind.PoisonDecision:
                case AutomaticCardChoiceKind.ResurrectionHerbDecision:
                case AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision:
                case AutomaticCardChoiceKind.PocketWatchSourceDisposition:
                    return true;
                case AutomaticCardChoiceKind.FlamethrowerOwnerDiscard:
                case AutomaticCardChoiceKind.FlamethrowerOpponentDiscard:
                    return optionId == FlamethrowerEffectHandler.SkipOptionId;
                case AutomaticCardChoiceKind.PocketWatchManualCard:
                    return optionId ==
                        PocketWatchEffectHandler.SkipManualCardOptionId;
                default:
                    return false;
            }
        }

        private static bool IsPrimaryActionAllowed(
            GameSceneCombatHudCommandKind kind,
            GameSceneCombatHudCommandKind? restrictedPrimaryAction)
        {
            return !restrictedPrimaryAction.HasValue ||
                restrictedPrimaryAction.Value == kind;
        }

        private static bool IsContractCandidateAllowed(
            string definitionKey,
            string restrictedContractDefinitionKey)
        {
            return string.IsNullOrEmpty(restrictedContractDefinitionKey) ||
                string.Equals(
                    definitionKey,
                    restrictedContractDefinitionKey,
                    StringComparison.Ordinal);
        }

        private static bool IsOptionAllowed(
            int optionId,
            int? restrictedOptionId)
        {
            return !restrictedOptionId.HasValue ||
                restrictedOptionId.Value == optionId;
        }

        private static string FormatChangeLabel(string changeActionText)
        {
            return CurrencyIconMarkup.FormatChangeActionLabel(changeActionText);
        }

        private static bool HasCardChoice(
            IReadOnlyList<AutomaticCardChoiceViewModel> choices)
        {
            foreach (AutomaticCardChoiceViewModel choice in choices)
            {
                if (choice.CardId.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatAutomaticCardResult(
            AutomaticCardResultViewModel result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            string text = "AUTOMATIC CARD\n" + result.PublicSummary;
            return string.IsNullOrEmpty(result.PrivateSummary)
                ? text
                : text + "\n" + result.PrivateSummary;
        }
    }
}
