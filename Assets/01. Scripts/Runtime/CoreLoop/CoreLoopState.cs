namespace DiaBlackJack.CoreLoop
{
    public enum CoreLoopState
    {
        Initializing,
        StartingRound,
        PlayerTurn,
        PlayerChoosingChangeCard,
        EnemyTurn,
        ResolvingRound,
        BattleEnded
    }

    public enum BattleOutcome
    {
        InProgress,
        PlayerVictory,
        PlayerDefeat
    }
}
