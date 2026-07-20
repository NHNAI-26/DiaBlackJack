using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyDecision
    {
        public EnemyDecision(EnemyActionType actionType, string reasonCode)
        {
            if (!Enum.IsDefined(typeof(EnemyActionType), actionType))
            {
                throw new ArgumentOutOfRangeException(nameof(actionType));
            }

            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                throw new ArgumentException("Enemy decision reason cannot be empty.", nameof(reasonCode));
            }

            ActionType = actionType;
            ReasonCode = reasonCode;
        }

        public EnemyActionType ActionType { get; }

        public string ReasonCode { get; }
    }
}
