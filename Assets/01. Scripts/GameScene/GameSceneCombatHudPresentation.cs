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
        Restart
    }

    public enum GameSceneCombatHudActionPlacement
    {
        Default,
        BottomRight
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
            Title = title ?? string.Empty;
            Ability = ability ?? string.Empty;
            Cost = cost ?? string.Empty;
            IsInteractable = isInteractable;
        }

        public GameSceneCombatHudCommand Command { get; }

        public string DefinitionKey { get; }

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
            string prompt,
            IReadOnlyList<GameSceneCombatHudActionViewModel> primaryActions,
            IReadOnlyList<GameSceneCombatHudActionViewModel> optionActions,
            IReadOnlyList<GameSceneCombatHudContractCandidateViewModel> contractCandidates,
            string automaticCardResult)
        {
            Mode = mode;
            Prompt = prompt ?? string.Empty;
            PrimaryActions = primaryActions ?? Array.Empty<GameSceneCombatHudActionViewModel>();
            OptionActions = optionActions ?? Array.Empty<GameSceneCombatHudActionViewModel>();
            ContractCandidates = contractCandidates ??
                Array.Empty<GameSceneCombatHudContractCandidateViewModel>();
            AutomaticCardResult = automaticCardResult ?? string.Empty;
        }

        public GameSceneCombatHudMode Mode { get; }

        public string Prompt { get; }

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
            bool hideForPresentation = false)
        {
            if (core == null || isShopOpen || hideForPresentation)
            {
                return CreateHidden();
            }

            string automaticCardResult = FormatAutomaticCardResult(core.AutomaticCardResult);
            if (core.State == CoreLoopState.BattleEnded)
            {
                return new GameSceneCombatHudViewModel(
                    isStageBattle
                        ? GameSceneCombatHudMode.ReturningToRun
                        : GameSceneCombatHudMode.Restart,
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
                var options = new List<GameSceneCombatHudActionViewModel>();
                for (int i = 0; i < core.ChangeCandidates.Count; i++)
                {
                    options.Add(new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.SelectChangedCard,
                            optionId: i),
                        $"[ {core.ChangeCandidates[i]} ]",
                        !inputLocked));
                }

                return CreateOptions(
                    "CHOOSE A NEW HIDDEN CARD",
                    options,
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
                        placement: interaction.EffectKind ==
                            CardEffectKind.Flamethrower
                                ? GameSceneCombatHudActionPlacement.BottomRight
                                : GameSceneCombatHudActionPlacement.Default));
                }

                bool usesDirectCardSelection = HasCardChoice(interaction.Choices);
                if (usesDirectCardSelection)
                {
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.DiegeticSelection,
                        $"{interaction.SourceDisplayName}  |  {interaction.Prompt}",
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        options,
                        Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                        automaticCardResult);
                }

                return CreateOptions(
                    $"{interaction.SourceDisplayName}  |  {interaction.Prompt}",
                    options,
                    automaticCardResult);
            }

            if (core.IsResolvingCardEffect)
            {
                if (core.PendingCardEffectKind == CardEffectKind.AutoPistol)
                {
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
                        core.CardEffectPrompt,
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
                        core.CardEffectPrompt,
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

                return CreateOptions(core.CardEffectPrompt, options, automaticCardResult);
            }

            DemonContractPanelViewModel contract = core.DemonContract;
            if (contract.IsResolving)
            {
                if (contract.InteractionKind ==
                        DemonContractInteractionKind.SatanDeclareFirstNumber ||
                    contract.InteractionKind ==
                        DemonContractInteractionKind.SatanDeclareSecondNumber)
                {
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.SatanNumberSelection,
                        BuildContractPrompt(contract),
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
                    string progress = contract.InteractionKind ==
                            DemonContractInteractionKind.BeelzebubChooseOwnerCard
                        ? " (1/2)"
                        : " (2/2)";
                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.DiegeticSelection,
                        BuildContractPrompt(contract) + progress,
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
                                choice.CanSelect && !inputLocked));
                    }

                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.ContractCandidates,
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
                        choice.CanSelect && !inputLocked,
                        placement: IsBottomRightContractAction(
                            contract.InteractionKind,
                            choice.OptionId)
                                ? GameSceneCombatHudActionPlacement.BottomRight
                                : GameSceneCombatHudActionPlacement.Default));
                }

                return CreateOptions(
                    BuildContractPrompt(contract),
                    options,
                    automaticCardResult);
            }

            var primaryActions = new List<GameSceneCombatHudActionViewModel>
            {
                CreateAction(
                    GameSceneCombatHudCommandKind.Hit,
                    "HIT",
                    core.CanHit && !inputLocked,
                    "Draw one public card.\nBust if visible total exceeds 21."),
                CreateAction(
                    GameSceneCombatHudCommandKind.Stand,
                    "STAND",
                    core.CanStand && !inputLocked,
                    "End your actions for this round."),
                CreateAction(
                    GameSceneCombatHudCommandKind.BeginChange,
                    FormatChangeLabel(core.ChangeActionText),
                    core.CanChange && !inputLocked,
                    "Reveal and discard hidden card, then choose one of two candidates.\n" +
                    core.ChangeActionText)
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
                string.Empty,
                Array.Empty<GameSceneCombatHudActionViewModel>(),
                Array.Empty<GameSceneCombatHudActionViewModel>(),
                Array.Empty<GameSceneCombatHudContractCandidateViewModel>(),
                string.Empty);
        }

        private static GameSceneCombatHudViewModel CreateOptions(
            string prompt,
            IReadOnlyList<GameSceneCombatHudActionViewModel> options,
            string automaticCardResult)
        {
            return new GameSceneCombatHudViewModel(
                GameSceneCombatHudMode.Options,
                prompt,
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
                    return optionId ==
                        BelphegorDemonContractHandler.MoveTopCardToBottomOptionId;
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                    return optionId ==
                        AsmodeusDemonContractHandler.SkipForcedHitOptionId;
                case DemonContractInteractionKind.MammonReroll:
                    return optionId ==
                        MammonDemonContractHandler.KeepDieOptionId;
                default:
                    return false;
            }
        }

        private static string FormatChangeLabel(string changeActionText)
        {
            return CurrencyIconMarkup.FormatChangeActionLabel(changeActionText);
        }

        private static string BuildContractPrompt(DemonContractPanelViewModel contract)
        {
            return string.IsNullOrEmpty(contract.OwnerPreview)
                ? contract.Prompt
                : contract.Prompt + "  |  " + contract.OwnerPreview;
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
