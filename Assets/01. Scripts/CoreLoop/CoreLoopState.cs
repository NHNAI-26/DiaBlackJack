namespace DiaBlackJack.CoreLoop
{
    public enum CoreLoopState
    {
        Initializing,
        StartingRound,
        PlayerTurn,
        PlayerChoosingChangeCard,
        PlayerResolvingCardEffect,
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
