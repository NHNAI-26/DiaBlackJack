using System;

namespace DiaBlackJack.StageProgression.UI
{
    public sealed class StageProgressionViewModel
    {
        public StageProgressionViewModel(
            string stageProgress,
            string stageName,
            string stageKind,
            string playerSoul,
            StageProgressionState state,
            string message,
            bool canStartRun,
            bool canAdvanceStage,
            bool canRestartRun)
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
    }

    public static class StageProgressionPresenter
    {
        public static StageProgressionViewModel Create(RunProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            StageDefinition stage = progress.CurrentStage;
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
                    progress.State == StageProgressionState.RunDefeat);
        }

        private static string GetMessage(StageProgressionState state)
        {
            switch (state)
            {
                case StageProgressionState.NotStarted:
                    return "READY TO START RUN";
                case StageProgressionState.InBattle:
                    return "BATTLE IN PROGRESS";
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
