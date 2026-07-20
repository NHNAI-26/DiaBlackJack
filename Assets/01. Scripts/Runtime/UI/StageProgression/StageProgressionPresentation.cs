using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression.UI
{
    public sealed class OpponentCandidateViewModel
    {
        public OpponentCandidateViewModel(
            string profileKey,
            string displayName,
            string grade,
            string maximumSoul,
            string summary,
            string rewardTier,
            bool isFocused)
        {
            ProfileKey = profileKey;
            DisplayName = displayName;
            Grade = grade;
            MaximumSoul = maximumSoul;
            Summary = summary;
            RewardTier = rewardTier;
            IsFocused = isFocused;
        }

        public string ProfileKey { get; }

        public string DisplayName { get; }

        public string Grade { get; }

        public string MaximumSoul { get; }

        public string Summary { get; }

        public string RewardTier { get; }

        public bool IsFocused { get; }
    }

    public sealed class BattleRewardOptionViewModel
    {
        public BattleRewardOptionViewModel(
            int optionId,
            string definitionKey,
            string displayName,
            int rank,
            string effectSummary)
        {
            OptionId = optionId;
            DefinitionKey = definitionKey;
            DisplayName = displayName;
            Rank = rank;
            EffectSummary = effectSummary;
        }

        public int OptionId { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public int Rank { get; }

        public string EffectSummary { get; }
    }

    public sealed class StageProgressionViewModel
    {
        private readonly ReadOnlyCollection<BattleRewardOptionViewModel> _rewardOptions;
        private readonly ReadOnlyCollection<OpponentCandidateViewModel> _opponentCandidates;

        public StageProgressionViewModel(
            string stageProgress,
            string stageName,
            string stageKind,
            string playerSoul,
            StageProgressionState state,
            string message,
            bool canStartRun,
            bool canAdvanceStage,
            bool canRestartRun,
            string rewardTier,
            IEnumerable<BattleRewardOptionViewModel> rewardOptions,
            bool canSelectReward,
            bool canSkipReward,
            string rewardCompletionMessage,
            string rewardResult,
            int deckCount,
            int? opponentOfferId,
            IEnumerable<OpponentCandidateViewModel> opponentCandidates,
            string focusedOpponentProfileKey,
            bool canFocusOpponent,
            bool canConfirmOpponent)
        {
            StageProgress = stageProgress;
            StageName = stageName;
            StageKind = stageKind;
            PlayerSoul = playerSoul;
            State = state;
            Message = message;
            CanStartRun = canStartRun;
            CanAdvanceStage = canAdvanceStage;
            CanRestartRun = canRestartRun;
            RewardTier = rewardTier;
            _rewardOptions = new List<BattleRewardOptionViewModel>(
                rewardOptions ?? throw new ArgumentNullException(nameof(rewardOptions)))
                .AsReadOnly();
            CanSelectReward = canSelectReward;
            CanSkipReward = canSkipReward;
            RewardCompletionMessage = rewardCompletionMessage;
            RewardResult = rewardResult;
            DeckCount = deckCount;
            OpponentOfferId = opponentOfferId;
            _opponentCandidates = new List<OpponentCandidateViewModel>(
                opponentCandidates ?? throw new ArgumentNullException(
                    nameof(opponentCandidates)))
                .AsReadOnly();
            FocusedOpponentProfileKey = focusedOpponentProfileKey;
            CanFocusOpponent = canFocusOpponent;
            CanConfirmOpponent = canConfirmOpponent;
        }

        public string StageProgress { get; }

        public string StageName { get; }

        public string StageKind { get; }

        public string PlayerSoul { get; }

        public StageProgressionState State { get; }

        public string Message { get; }

        public bool CanStartRun { get; }

        public bool CanAdvanceStage { get; }

        public bool CanRestartRun { get; }

        public string RewardTier { get; }

        public IReadOnlyList<BattleRewardOptionViewModel> RewardOptions => _rewardOptions;

        public bool CanSelectReward { get; }

        public bool CanSkipReward { get; }

        public string RewardCompletionMessage { get; }

        public string RewardResult { get; }

        public int DeckCount { get; }

        public int? OpponentOfferId { get; }

        public IReadOnlyList<OpponentCandidateViewModel> OpponentCandidates =>
            _opponentCandidates;

        public string FocusedOpponentProfileKey { get; }

        public bool CanFocusOpponent { get; }

        public bool CanConfirmOpponent { get; }
    }

    public static class StageProgressionPresenter
    {
        public static StageProgressionViewModel Create(RunProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return Create(progress, null, null);
        }

        public static StageProgressionViewModel Create(
            StageProgressionSession session,
            string focusedProfileKey = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return Create(session.Progress, session.PendingOpponentSelection, focusedProfileKey);
        }

        private static StageProgressionViewModel Create(
            RunProgress progress,
            OpponentSelectionOffer opponentOffer,
            string focusedProfileKey)
        {
            bool isOpponentSelection =
                progress.State == StageProgressionState.OpponentSelection;
            if (isOpponentSelection && opponentOffer == null)
            {
                throw new InvalidOperationException(
                    "Opponent selection state requires a pending opponent offer.");
            }

            string validatedFocusedProfileKey = isOpponentSelection &&
                ContainsProfileKey(opponentOffer, focusedProfileKey)
                    ? focusedProfileKey
                    : null;
            IReadOnlyList<OpponentCandidateViewModel> opponentCandidates =
                isOpponentSelection
                    ? CreateOpponentCandidates(
                        opponentOffer,
                        validatedFocusedProfileKey)
                    : Array.Empty<OpponentCandidateViewModel>();

            StageDefinition stage = progress.CurrentStage;
            bool canResolveReward = progress.State == StageProgressionState.RewardSelection;
            PendingBattleReward pendingReward = progress.PendingReward;
            if (canResolveReward && pendingReward == null)
            {
                throw new InvalidOperationException(
                    "Reward selection state requires a pending battle reward.");
            }

            return new StageProgressionViewModel(
                $"STAGE {progress.CurrentStageIndex + 1} / {progress.Stages.Count}",
                stage.DisplayName,
                stage.Kind == StageKind.FinalBossCombat ? "FINAL BOSS" : "NORMAL COMBAT",
                $"{progress.Player.CurrentSoul} / {progress.Player.MaximumSoul}",
                progress.State,
                GetMessage(progress.State),
                progress.State == StageProgressionState.NotStarted,
                progress.State == StageProgressionState.StageCleared,
                progress.State == StageProgressionState.RunVictory ||
                    progress.State == StageProgressionState.RunDefeat,
                canResolveReward ? GetRewardTier(pendingReward.Offer.Tier) : string.Empty,
                canResolveReward
                    ? CreateRewardOptions(pendingReward.Offer)
                    : Array.Empty<BattleRewardOptionViewModel>(),
                canResolveReward,
                canResolveReward,
                canResolveReward
                    ? GetRewardCompletionMessage(pendingReward.CompletionTarget)
                    : string.Empty,
                GetRewardResult(progress),
                progress.Player.Deck.Count,
                isOpponentSelection ? opponentOffer.OfferId : (int?)null,
                opponentCandidates,
                validatedFocusedProfileKey,
                isOpponentSelection,
                isOpponentSelection && validatedFocusedProfileKey != null);
        }

        private static IReadOnlyList<OpponentCandidateViewModel> CreateOpponentCandidates(
            OpponentSelectionOffer offer,
            string focusedProfileKey)
        {
            var candidates = new List<OpponentCandidateViewModel>(offer.Candidates.Count);
            foreach (OpponentSelectionCandidate candidate in offer.Candidates)
            {
                EnemyProfilePreview preview = candidate.Preview;
                candidates.Add(new OpponentCandidateViewModel(
                    candidate.ProfileKey,
                    preview.DisplayName,
                    preview.Grade.ToString().ToUpperInvariant(),
                    $"SOUL {preview.MaximumSoul}",
                    preview.Summary,
                    GetRewardTier(preview.ExpectedRewardTier),
                    StringComparer.Ordinal.Equals(
                        candidate.ProfileKey,
                        focusedProfileKey)));
            }

            return candidates;
        }

        private static bool ContainsProfileKey(
            OpponentSelectionOffer offer,
            string profileKey)
        {
            if (offer == null || string.IsNullOrEmpty(profileKey))
            {
                return false;
            }

            foreach (OpponentSelectionCandidate candidate in offer.Candidates)
            {
                if (StringComparer.Ordinal.Equals(candidate.ProfileKey, profileKey))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<BattleRewardOptionViewModel> CreateRewardOptions(
            BattleRewardOffer offer)
        {
            var options = new List<BattleRewardOptionViewModel>(offer.Options.Count);
            foreach (BattleRewardOption option in offer.Options)
            {
                CardDefinition definition = CardDefinitionCatalog.GetByKey(
                    option.DefinitionKey);
                options.Add(new BattleRewardOptionViewModel(
                    option.OptionId,
                    option.DefinitionKey,
                    definition.DisplayName,
                    definition.Rank,
                    GetEffectSummary(definition)));
            }

            return options;
        }

        private static string GetRewardTier(BattleRewardTier tier)
        {
            switch (tier)
            {
                case BattleRewardTier.Normal:
                    return "NORMAL REWARD";
                case BattleRewardTier.HighGrade:
                    return "HIGH-GRADE REWARD";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        private static string GetRewardCompletionMessage(
            BattleRewardCompletionTarget completionTarget)
        {
            switch (completionTarget)
            {
                case BattleRewardCompletionTarget.StageCleared:
                    return "REWARD COMPLETION WILL CLEAR THIS STAGE";
                case BattleRewardCompletionTarget.RunVictory:
                    return "REWARD COMPLETION WILL END THE RUN";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(completionTarget),
                        completionTarget,
                        null);
            }
        }

        private static string GetEffectSummary(CardDefinition definition)
        {
            switch (definition.Effect)
            {
                case CardEffectKind.None:
                    return definition.Activation == CardActivationKind.Passive
                        ? "PASSIVE VALUE CARD"
                        : "STANDARD VALUE CARD";
                case CardEffectKind.CrystalOrb:
                    return "PEEK AT 2 DECK CARDS";
                case CardEffectKind.ThreatHammer:
                    return "DISCARD 1 FACE-UP CARD";
                case CardEffectKind.AutoPistol:
                    return "GUESS 1 HIDDEN CARD";
                case CardEffectKind.MilitaryKnife:
                    return "FORCE A DRAW";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(definition),
                        definition.Effect,
                        null);
            }
        }

        private static string GetRewardResult(RunProgress progress)
        {
            BattleRewardResolution resolution = progress.LastRewardResolution;
            if (resolution == null)
            {
                return string.Empty;
            }

            if (resolution.WasSkipped)
            {
                return $"REWARD SKIPPED  |  DECK {progress.Player.Deck.Count}";
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(
                resolution.SelectedDefinitionKey);
            return $"ADDED  {definition.Rank} {definition.DisplayName}  |  " +
                $"DECK {progress.Player.Deck.Count}";
        }

        private static string GetMessage(StageProgressionState state)
        {
            switch (state)
            {
                case StageProgressionState.NotStarted:
                    return "READY TO START RUN";
                case StageProgressionState.OpponentSelection:
                    return "CHOOSE OPPONENT";
                case StageProgressionState.InBattle:
                    return "BATTLE IN PROGRESS";
                case StageProgressionState.RewardSelection:
                    return "SELECT BATTLE REWARD";
                case StageProgressionState.StageCleared:
                    return "STAGE CLEARED";
                case StageProgressionState.RunVictory:
                    return "RUN VICTORY";
                case StageProgressionState.RunDefeat:
                    return "RUN DEFEAT";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
