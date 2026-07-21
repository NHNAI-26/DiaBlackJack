namespace DiaBlackJack.CoreLoop
{
    public interface IEnemyBehaviorPolicy
    {
        EnemyDecision Decide(EnemyObservation observation);
    }
}
