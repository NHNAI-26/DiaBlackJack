namespace DiaBlackJack.CoreLoop
{
    public interface IEnemyBehaviorPolicy
    {
        EnemyDecision Decide(EnemyObservation observation);
    }

    public interface IEnemyForcedActionPolicy
    {
        bool TryDecideForcedAction(
            EnemyObservation observation,
            out EnemyDecision decision);
    }
}
