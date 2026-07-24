namespace DiaBlackJack.CoreLoop
{
    public enum CoreLoopState
    {
        Initializing,
        StartingRound,
        PlayerTurn,
        PlayerChoosingChangeCard,
        PlayerResolvingCardEffect,
        ResolvingAutomaticCardEffect,
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
