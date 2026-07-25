using System;

namespace DiaBlackJack.CoreLoop
{
    public enum CardSuit
    {
        Spade,
        Clover,
    }

    public static class CardSuitUtility
    {
        public static void Validate(CardSuit suit, string parameterName)
        {
            if (!Enum.IsDefined(typeof(CardSuit), suit))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Card suit is not supported.");
            }
        }
    }
}
