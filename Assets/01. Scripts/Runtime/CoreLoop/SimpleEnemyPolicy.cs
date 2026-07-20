namespace DiaBlackJack.CoreLoop
{
    public enum EnemyAction
    {
        Hit,
        Stand
    }

    public sealed class SimpleEnemyPolicy : IEnemyBehaviorPolicy
    {
        public EnemyAction Decide(HandValue ownHand)
        {
            return ownHand.Total <= 16 ? EnemyAction.Hit : EnemyAction.Stand;
        }

        public EnemyDecision Decide(EnemyObservation observation)
        {
            if (observation == null)
            {
                throw new System.ArgumentNullException(nameof(observation));
            }

            EnemyAction legacyAction = Decide(observation.OwnHandValue);
            return legacyAction == EnemyAction.Hit
                ? new EnemyDecision(EnemyActionType.Hit, "simple-hit-at-sixteen-or-less")
                : new EnemyDecision(EnemyActionType.Stand, "simple-stand-at-seventeen-or-more");
        }
    }
}
