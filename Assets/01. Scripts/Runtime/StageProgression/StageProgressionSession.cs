using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class StageProgressionSession
    {
        private readonly Func<StageDefinition, PlayerRunState, CoreLoopBattle> _battleFactory;
        private CoreLoopSession _battleSession;
        private CoreLoopBattle _processedBattle;

        public StageProgressionSession(
            RunProgress progress,
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory = null)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _battleFactory = battleFactory ?? StageBattleFactory.Create;
        }

        public CoreLoopBattle Battle => _battleSession?.Battle;

        public RunProgress Progress { get; }

        public bool TryStartRun()
        {
            if (!Progress.StartRun())
            {
                return false;
            }

            _battleSession = CreateBattleSession(Progress.CurrentStage);
            _processedBattle = null;
            return true;
        }

        public bool TryPlayerHit()
        {
            if (!CanForwardBattleAction() || !_battleSession.TryPlayerHit())
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryPlayerStand()
        {
            if (!CanForwardBattleAction() || !_battleSession.TryPlayerStand())
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryAdvanceToNextStage()
        {
            if (Progress.State != StageProgressionState.StageCleared)
            {
                return false;
            }

            int nextStageIndex = Progress.CurrentStageIndex + 1;
            if (nextStageIndex >= Progress.Stages.Count)
            {
                throw new InvalidOperationException("A cleared stage must have a following stage.");
            }

            CoreLoopSession nextBattleSession = CreateBattleSession(Progress.Stages[nextStageIndex]);
            if (!Progress.TryAdvanceToNextStage())
            {
                throw new InvalidOperationException("Run progress rejected a validated stage advance.");
            }

            _battleSession = nextBattleSession;
            _processedBattle = null;
            return true;
        }

        public bool TryRestartRun()
        {
            if (!Progress.TryRestartRun())
            {
                return false;
            }

            _battleSession = CreateBattleSession(Progress.CurrentStage);
            _processedBattle = null;
            return true;
        }

        private bool CanForwardBattleAction()
        {
            return Progress.State == StageProgressionState.InBattle && _battleSession != null;
        }

        private CoreLoopSession CreateBattleSession(StageDefinition stage)
        {
            return new CoreLoopSession(() => _battleFactory(stage, Progress.Player));
        }

        private void SynchronizeFinishedBattle()
        {
            CoreLoopBattle battle = Battle;
            if (battle == null ||
                battle.State != CoreLoopState.BattleEnded ||
                ReferenceEquals(battle, _processedBattle))
            {
                return;
            }

            Progress.Player.SetCurrentSoul(battle.Player.Soul.Current);

            bool resultApplied;
            switch (battle.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    resultApplied = Progress.TryCompleteCurrentStage();
                    break;
                case BattleOutcome.PlayerDefeat:
                    resultApplied = Progress.TryDefeatRun();
                    break;
                default:
                    throw new InvalidOperationException("An ended battle must have a final outcome.");
            }

            if (!resultApplied)
            {
                throw new InvalidOperationException("Run progress rejected a finished battle result.");
            }

            _processedBattle = battle;
        }
    }
}
