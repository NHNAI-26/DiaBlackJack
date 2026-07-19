using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class BattleRewardOption
    {
        public BattleRewardOption(int optionId, string definitionKey)
        {
            if (optionId < 0 || optionId > 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(optionId),
                    "Reward option id must be between zero and two.");
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(definitionKey);
            OptionId = optionId;
            DefinitionKey = definition.Key;
        }

        public int OptionId { get; }

        public string DefinitionKey { get; }
    }
}
