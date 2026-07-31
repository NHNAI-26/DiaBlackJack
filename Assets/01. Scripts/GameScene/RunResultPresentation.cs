using System;
using Border.SaveLoad.UI;
using DiaBlackJack.StageProgression.UI;

namespace DiaBlackJack.GameScene
{
    public sealed class RunResultViewModel
    {
        internal RunResultViewModel(
            bool isVisible,
            bool isVictory,
            string title,
            string summary,
            string playerSoul,
            string playerGold,
            string goldResult,
            string saveStatus,
            bool canRestart,
            bool canReturnToMainMenu,
            bool canRetrySave)
        {
            IsVisible = isVisible;
            IsVictory = isVictory;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            PlayerSoul = playerSoul ?? string.Empty;
            PlayerGold = playerGold ?? string.Empty;
            GoldResult = goldResult ?? string.Empty;
            SaveStatus = saveStatus ?? string.Empty;
            CanRestart = canRestart;
            CanReturnToMainMenu = canReturnToMainMenu;
            CanRetrySave = canRetrySave;
        }

        public bool CanRestart { get; }
        public bool CanReturnToMainMenu { get; }
        public bool CanRetrySave { get; }
        public string GoldResult { get; }
        public bool IsVictory { get; }
        public bool IsVisible { get; }
        public string PlayerGold { get; }
        public string PlayerSoul { get; }
        public string SaveStatus { get; }
        public string Summary { get; }
        public string Title { get; }
    }

    public static class RunResultPresenter
    {
        public static RunResultViewModel Create(
            GameFlowScreen screen,
            StageProgressionViewModel progression,
            RunSaveViewModel save)
        {
            if (progression == null)
            {
                throw new ArgumentNullException(nameof(progression));
            }

            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            bool isVictory = screen == GameFlowScreen.RunVictory;
            bool isDefeat = screen == GameFlowScreen.RunDefeat;
            bool isVisible = isVictory || isDefeat;
            bool canLeaveResult =
                isVisible && progression.CanRestartRun && !save.BlocksProgressionInput;
            return new RunResultViewModel(
                isVisible,
                isVictory,
                isVictory ? "RUN VICTORY" : isDefeat ? "RUN DEFEAT" : string.Empty,
                isVictory
                    ? "THE BLACK THRONE IS EMPTY"
                    : isDefeat
                        ? "YOUR SOULS ARE SPENT"
                        : string.Empty,
                isVisible ? progression.PlayerSoul : string.Empty,
                isVisible ? progression.PlayerGold : string.Empty,
                isVisible ? progression.GoldResult : string.Empty,
                isVisible
                    ? string.IsNullOrEmpty(save.SaveIndicator)
                        ? save.StatusMessage
                        : save.SaveIndicator
                    : string.Empty,
                canLeaveResult,
                canLeaveResult,
                isVisible && save.CanRetrySave);
        }
    }
}
