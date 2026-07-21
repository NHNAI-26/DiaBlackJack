using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.StageProgression
{
    public sealed class RunProgress
    {
        private readonly ReadOnlyCollection<StageDefinition> _stages;

        public RunProgress(IEnumerable<StageDefinition> stages, PlayerRunState player)
        {
            if (stages == null)
            {
                throw new ArgumentNullException(nameof(stages));
            }

            Player = player ?? throw new ArgumentNullException(nameof(player));
            _stages = ValidateAndCopyStages(stages);
            State = StageProgressionState.NotStarted;
            CurrentStageIndex = 0;
        }

        public StageProgressionState State { get; private set; }

        public int CurrentStageIndex { get; private set; }

        public StageDefinition CurrentStage => _stages[CurrentStageIndex];

        public IReadOnlyList<StageDefinition> Stages => _stages;

        public PlayerRunState Player { get; }

        public PendingBattleReward PendingReward { get; private set; }

        public BattleRewardResolution LastRewardResolution { get; private set; }

        public bool StartRun()
        {
            if (State != StageProgressionState.NotStarted)
            {
                return false;
            }

            ResetToFirstStage();
            return true;
        }

        internal bool TryBeginOpponentSelection()
        {
            if (State != StageProgressionState.InBattle ||
                CurrentStage.Kind == StageKind.FinalBossCombat)
            {
                return false;
            }

            State = StageProgressionState.OpponentSelection;
            return true;
        }

        internal bool TryBeginBattleFromOpponentSelection()
        {
            if (State != StageProgressionState.OpponentSelection)
            {
                return false;
            }

            State = StageProgressionState.InBattle;
            return true;
        }

        public bool TryBeginBattleReward(
            BattleRewardOffer offer,
            BattleRewardCompletionTarget completionTarget)
        {
            if (State != StageProgressionState.InBattle ||
                Player.IsDepleted ||
                offer == null ||
                !IsValidCompletionTarget(completionTarget) ||
                !RewardMatchesCurrentStage(offer, completionTarget))
            {
                return false;
            }

            PendingReward = new PendingBattleReward(offer, completionTarget);
            LastRewardResolution = null;
            State = StageProgressionState.RewardSelection;
            return true;
        }

        public bool TrySelectBattleReward(int optionId)
        {
            if (State != StageProgressionState.RewardSelection || PendingReward == null)
            {
                return false;
            }

            BattleRewardOption selectedOption = FindRewardOption(
                PendingReward.Offer,
                optionId);
            if (selectedOption == null)
            {
                return false;
            }

            PendingBattleReward pendingReward = PendingReward;
            RunCardDefinition addedCard = Player.AddRewardCard(selectedOption.DefinitionKey);
            BattleRewardResolution resolution = BattleRewardResolution.Selected(
                pendingReward.Offer.OfferId,
                selectedOption,
                addedCard,
                pendingReward.CompletionTarget);
            CompleteBattleReward(pendingReward.CompletionTarget, resolution);
            return true;
        }

        public bool TrySkipBattleReward()
        {
            if (State != StageProgressionState.RewardSelection || PendingReward == null)
            {
                return false;
            }

            PendingBattleReward pendingReward = PendingReward;
            BattleRewardResolution resolution = BattleRewardResolution.Skipped(
                pendingReward.Offer.OfferId,
                pendingReward.CompletionTarget);
            CompleteBattleReward(pendingReward.CompletionTarget, resolution);
            return true;
        }

        public bool TryAdvanceToNextStage()
        {
            if (State != StageProgressionState.StageCleared)
            {
                return false;
            }

            int nextStageIndex = CurrentStageIndex + 1;
            if (nextStageIndex >= _stages.Count)
            {
                throw new InvalidOperationException("No next stage exists after a cleared non-final stage.");
            }

            CurrentStageIndex = nextStageIndex;
            State = StageProgressionState.InBattle;
            return true;
        }

        public bool TryDefeatRun()
        {
            if (State != StageProgressionState.InBattle || !Player.IsDepleted)
            {
                return false;
            }

            State = StageProgressionState.RunDefeat;
            return true;
        }

        public bool TryRestartRun()
        {
            if (State != StageProgressionState.RunVictory &&
                State != StageProgressionState.RunDefeat)
            {
                return false;
            }

            ResetToFirstStage();
            return true;
        }

        private static ReadOnlyCollection<StageDefinition> ValidateAndCopyStages(
            IEnumerable<StageDefinition> stages)
        {
            var stageList = new List<StageDefinition>();
            var knownStageIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (StageDefinition stage in stages)
            {
                if (stage == null)
                {
                    throw new ArgumentException("Stage path cannot contain null.", nameof(stages));
                }

                if (!knownStageIds.Add(stage.Id))
                {
                    throw new ArgumentException($"Stage id '{stage.Id}' is duplicated.", nameof(stages));
                }

                stageList.Add(stage);
            }

            if (stageList.Count == 0)
            {
                throw new ArgumentException("Stage path must contain at least one stage.", nameof(stages));
            }

            for (int i = 0; i < stageList.Count - 1; i++)
            {
                if (stageList[i].Kind == StageKind.FinalBossCombat)
                {
                    throw new ArgumentException("Only the final stage can be the final boss.", nameof(stages));
                }
            }

            if (stageList[stageList.Count - 1].Kind != StageKind.FinalBossCombat)
            {
                throw new ArgumentException("Stage path must end with a final boss.", nameof(stages));
            }

            return stageList.AsReadOnly();
        }

        private static bool IsValidCompletionTarget(
            BattleRewardCompletionTarget completionTarget)
        {
            return completionTarget == BattleRewardCompletionTarget.StageCleared ||
                completionTarget == BattleRewardCompletionTarget.RunVictory;
        }

        private bool RewardMatchesCurrentStage(
            BattleRewardOffer offer,
            BattleRewardCompletionTarget completionTarget)
        {
            if (CurrentStage.Kind == StageKind.FinalBossCombat)
            {
                return completionTarget == BattleRewardCompletionTarget.RunVictory &&
                    offer.Tier == BattleRewardTier.HighGrade;
            }

            return completionTarget == BattleRewardCompletionTarget.StageCleared;
        }

        private static BattleRewardOption FindRewardOption(
            BattleRewardOffer offer,
            int optionId)
        {
            for (int i = 0; i < offer.Options.Count; i++)
            {
                BattleRewardOption option = offer.Options[i];
                if (option.OptionId == optionId)
                {
                    return option;
                }
            }

            return null;
        }

        private void CompleteBattleReward(
            BattleRewardCompletionTarget completionTarget,
            BattleRewardResolution resolution)
        {
            LastRewardResolution = resolution;
            PendingReward = null;
            State = completionTarget == BattleRewardCompletionTarget.RunVictory
                ? StageProgressionState.RunVictory
                : StageProgressionState.StageCleared;
        }

        private void ResetToFirstStage()
        {
            CurrentStageIndex = 0;
            Player.ResetForNewRun();
            PendingReward = null;
            LastRewardResolution = null;
            State = StageProgressionState.InBattle;
        }
    }
}
