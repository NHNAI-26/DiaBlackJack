namespace DiaBlackJack.CoreLoop
{
    public static class EnemyDecisionValidator
    {
        public static bool CanExecute(
            EnemyObservation observation,
            EnemyDecision decision)
        {
            if (observation == null || decision == null)
            {
                return false;
            }

            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.Matches(decision))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
