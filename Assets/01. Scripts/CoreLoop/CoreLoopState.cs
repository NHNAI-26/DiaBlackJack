namespace DiaBlackJack.CoreLoop
{
    public enum CoreLoopState
    {
        Initializing,
        StartingRound,
        PlayerTurn,
        PlayerChoosingChangeCard,
        PlayerResolvingCardEffect,
        PlayerResolvingDemonContract,
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
