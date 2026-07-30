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
        ContractCandidates,
        ReturningToRun,
        Restart
    }

    public enum GameSceneCombatHudCommandKind
    {
        Hit,
        Stand,
        BeginChange,
        SelectChangedCard,
        BeginContractConfirmation,
        ConfirmContract,
        CancelContract,
        ResolveCardEffectChoice,
        ResolveAutomaticCardChoice,
        ResolveDemonContractChoice,
        BeginActiveDemonContractAction,
        Restart
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
            string tooltip = "")
        {
            Command = command;
            Label = label ?? string.Empty;
            IsInteractable = isInteractable;
            Tooltip = tooltip ?? string.Empty;
        }

        public GameSceneCombatHudCommand Command { get; }

        public string Label { get; }

        public bool IsInteractable { get; }

        public string Tooltip { get; }
    }

    public sealed class GameSceneCombatHudContractCandidateViewModel
    {
        public GameSceneCombatHudContractCandidateViewModel(
            GameSceneCombatHudCommand command,
            string definitionKey,
            string title,
            string ability,
            string cost,
            bool isInteractable,
            string buttonLabel)
        {
            Command = command;
            DefinitionKey = definitionKey ?? string.Empty;
            Title = title ?? string.Empty;
            Ability = ability ?? string.Empty;
            Cost = cost ?? string.Empty;
            IsInteractable = isInteractable;
            ButtonLabel = buttonLabel ?? string.Empty;
        }

        public GameSceneCombatHudCommand Command { get; }

        public string DefinitionKey { get; }

        public string Title { get; }

        public string Ability { get; }

        public string Cost { get; }

        public bool IsInteractable { get; }

        public string ButtonLabel { get; }
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
            bool showDemonContractConfirmation,
            bool isStageBattle,
            bool isShopOpen,
            bool inputLocked)
        {
            if (core == null || isShopOpen)
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
                    return CreateOptions(
                        "ENEMY AUTOMATIC DECISION",
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        automaticCardResult);
                }

                var options = new List<GameSceneCombatHudActionViewModel>();
                foreach (AutomaticCardChoiceViewModel choice in interaction.Choices)
                {
                    options.Add(new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice,
                            choice.OptionId,
                            interaction.InteractionId),
                        choice.Label,
                        !inputLocked));
                }

                return CreateOptions(
                    $"{interaction.SourceDisplayName}  |  {interaction.Prompt}",
                    options,
                    automaticCardResult);
            }

            if (core.IsResolvingCardEffect)
            {
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
                if (contract.UsesContractCandidateLayout &&
                    contract.Choices.Count <= 2)
                {
                    var candidates =
                        new List<GameSceneCombatHudContractCandidateViewModel>();
                    foreach (DemonContractChoiceViewModel choice in contract.Choices)
                    {
                        candidates.Add(new GameSceneCombatHudContractCandidateViewModel(
                            new GameSceneCombatHudCommand(
                                GameSceneCombatHudCommandKind.ResolveDemonContractChoice,
                                choice.OptionId,
                                contract.InteractionId ?? -1),
                            choice.DefinitionKey,
                            choice.Title,
                            choice.Ability,
                            choice.Cost,
                            choice.CanSelect && !inputLocked,
                            choice.CanSelect ? "SELECT" : choice.DisabledReason));
                    }

                    return new GameSceneCombatHudViewModel(
                        GameSceneCombatHudMode.ContractCandidates,
                        BuildContractPrompt(contract),
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        Array.Empty<GameSceneCombatHudActionViewModel>(),
                        candidates,
                        automaticCardResult);
                }

                var options = new List<GameSceneCombatHudActionViewModel>();
                foreach (DemonContractChoiceViewModel choice in contract.Choices)
                {
                    options.Add(new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveDemonContractChoice,
                            choice.OptionId,
                            contract.InteractionId ?? -1),
                        choice.CanSelect
                            ? choice.Title
                            : choice.Title + "\n" + choice.DisabledReason,
                        choice.CanSelect && !inputLocked));
                }

                return CreateOptions(
                    BuildContractPrompt(contract),
                    options,
                    automaticCardResult);
            }

            if (showDemonContractConfirmation)
            {
                return CreateOptions(
                    $"PAY {contract.SoulCost} SOUL  |  {contract.SoulAfterCost} LEFT\n" +
                    "REVEAL CANDIDATES AND CHOOSE ONE CONTRACT",
                    new[]
                    {
                        CreateAction(
                            GameSceneCombatHudCommandKind.ConfirmContract,
                            "CONFIRM CONTRACT",
                            contract.CanBegin && !inputLocked),
                        CreateAction(
                            GameSceneCombatHudCommandKind.CancelContract,
                            "CANCEL",
                            !inputLocked)
                    },
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
                    "CHANGE",
                    core.CanChange && !inputLocked,
                    "Reveal and discard hidden card, then choose one of two candidates.\n" +
                    core.ChangeActionText),
                CreateAction(
                    GameSceneCombatHudCommandKind.BeginContractConfirmation,
                    "CONTRACT",
                    contract.CanBegin && !inputLocked,
                    "Pay soul to reveal up to two candidates and choose one contract.\n" +
                    contract.ActionText)
            };

            var activeContractActions = new List<GameSceneCombatHudActionViewModel>();
            foreach (ActiveDemonContractActionViewModel action in contract.ActiveActions)
            {
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
            int optionId = -1)
        {
            return new GameSceneCombatHudActionViewModel(
                new GameSceneCombatHudCommand(kind, optionId),
                label,
                isInteractable,
                tooltip);
        }

        private static string BuildContractPrompt(DemonContractPanelViewModel contract)
        {
            return string.IsNullOrEmpty(contract.OwnerPreview)
                ? contract.Prompt
                : contract.Prompt + "  |  " + contract.OwnerPreview;
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
