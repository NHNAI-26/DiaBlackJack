using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CoreLoopSession
    {
        private readonly Func<CoreLoopBattle> _battleFactory;

        public CoreLoopSession(Func<CoreLoopBattle> battleFactory)
        {
            _battleFactory = battleFactory ?? throw new ArgumentNullException(nameof(battleFactory));
            Battle = CreateStartedBattle();
        }

        public CoreLoopBattle Battle { get; private set; }

        public bool TryPlayerHit()
        {
            return Battle.TryPlayerHit();
        }

        public bool TryPlayerStand()
        {
            return Battle.TryPlayerStand();
        }

        public bool TryRestart()
        {
            if (Battle.State != CoreLoopState.BattleEnded)
            {
                return false;
            }

            Battle = CreateStartedBattle();
            return true;
        }

        private CoreLoopBattle CreateStartedBattle()
        {
            CoreLoopBattle battle = _battleFactory();
            if (battle == null)
            {
                throw new InvalidOperationException("Battle factory returned null.");
            }

            if (!battle.Start())
            {
                throw new InvalidOperationException("Battle factory must return an unstarted battle.");
            }

            return battle;
        }
    }
}
