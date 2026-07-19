namespace DiaBlackJack.StageProgression
{
    public sealed class BattleRewardResolution
    {
        private BattleRewardResolution(
            int offerId,
            bool wasSkipped,
            int? selectedOptionId,
            string selectedDefinitionKey,
            int? addedCardId,
            BattleRewardCompletionTarget completionTarget)
        {
            OfferId = offerId;
            WasSkipped = wasSkipped;
            SelectedOptionId = selectedOptionId;
            SelectedDefinitionKey = selectedDefinitionKey;
            AddedCardId = addedCardId;
            CompletionTarget = completionTarget;
        }

        public int OfferId { get; }

        public bool WasSkipped { get; }

        public int? SelectedOptionId { get; }

        public string SelectedDefinitionKey { get; }

        public int? AddedCardId { get; }

        public BattleRewardCompletionTarget CompletionTarget { get; }

        internal static BattleRewardResolution Selected(
            int offerId,
            BattleRewardOption selectedOption,
            RunCardDefinition addedCard,
            BattleRewardCompletionTarget completionTarget)
        {
            return new BattleRewardResolution(
                offerId,
                false,
                selectedOption.OptionId,
                selectedOption.DefinitionKey,
                addedCard.Id,
                completionTarget);
        }

        internal static BattleRewardResolution Skipped(
            int offerId,
            BattleRewardCompletionTarget completionTarget)
        {
            return new BattleRewardResolution(
                offerId,
                true,
                null,
                null,
                null,
                completionTarget);
        }
    }
}
