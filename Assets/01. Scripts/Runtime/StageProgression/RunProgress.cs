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

        public bool StartRun()
        {
            if (State != StageProgressionState.NotStarted)
            {
                return false;
            }

            ResetToFirstStage();
            return true;
        }

        public bool TryCompleteCurrentStage()
        {
            if (State != StageProgressionState.InBattle || Player.IsDepleted)
            {
                return false;
            }

            State = CurrentStage.Kind == StageKind.FinalBossCombat
                ? StageProgressionState.RunVictory
                : StageProgressionState.StageCleared;
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

        private void ResetToFirstStage()
        {
            CurrentStageIndex = 0;
            Player.ResetForNewRun();
            State = StageProgressionState.InBattle;
        }
    }
}
