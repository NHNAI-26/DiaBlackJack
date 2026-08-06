using System;
using DiaBlackJack.StageProgression;

namespace DiaBlackJack.GameScene
{
    public enum GameFlowScreen
    {
        Unavailable,
        StartingDemonReveal,
        OpponentSelection,
        FinalBossReveal,
        Combat,
        Shop,
        RunVictory,
        RunDefeat
    }

    public static class GameFlowScreenResolver
    {
        public static GameFlowScreen Resolve(FormalRunSession session)
        {
            if (session == null)
            {
                return GameFlowScreen.Unavailable;
            }

            session.SynchronizeExternalState();
            switch (session.Phase)
            {
                case FormalRunPhase.NotStarted:
                    return session.CombatSession.PendingStartingDemonGrant != null
                        ? GameFlowScreen.StartingDemonReveal
                        : GameFlowScreen.Unavailable;
                case FormalRunPhase.Combat:
                    return ResolveCombatScreen(session.CombatSession);
                case FormalRunPhase.Shop:
                    return GameFlowScreen.Shop;
                case FormalRunPhase.RunVictory:
                    return GameFlowScreen.RunVictory;
                case FormalRunPhase.RunDefeat:
                    return GameFlowScreen.RunDefeat;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static GameFlowScreen ResolveCombatScreen(
            StageProgressionSession session)
        {
            switch (session.Progress.State)
            {
                case StageProgressionState.OpponentSelection:
                    return GameFlowScreen.OpponentSelection;
                case StageProgressionState.InBattle:
                    return GameFlowScreen.Combat;
                case StageProgressionState.RunVictory:
                    return GameFlowScreen.RunVictory;
                case StageProgressionState.RunDefeat:
                    return GameFlowScreen.RunDefeat;
                default:
                    throw new InvalidOperationException(
                        $"Formal combat phase cannot display {session.Progress.State}.");
            }
        }
    }
}
