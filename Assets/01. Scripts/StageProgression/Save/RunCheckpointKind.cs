namespace DiaBlackJack.StageProgression
{
    public enum RunCheckpointKind
    {
        StartingDemonGranted,
        CombatSettlementCompleted,
        ShopExited,
        EventResolved,
        RunEnded
    }

    public enum RunSaveStatus
    {
        InProgress,
        Victory,
        Defeat
    }

    public static class RunNextContentKind
    {
        public const string OpponentSelection = "opponent-selection";
        public const string Battle = "battle";
        public const string Shop = "shop";
        public const string Event = "event";
        public const string Boss = "boss";
        public const string Result = "result";
    }
}
