namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyObservation
    {
        public EnemyObservation(HandValue ownHandValue)
        {
            OwnHandValue = ownHandValue;
        }

        public HandValue OwnHandValue { get; }
    }
}
