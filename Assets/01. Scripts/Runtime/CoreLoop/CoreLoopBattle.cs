using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CoreLoopBattle
    {
        private readonly SimpleEnemyPolicy _enemyPolicy;
        private readonly RoundDamageApplier _damageApplier = new RoundDamageApplier();

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            SimpleEnemyPolicy enemyPolicy = null)
        {
            Player = new BattleParticipant(playerDeck, playerMaximumSoul);
            Enemy = new BattleParticipant(enemyDeck, enemyMaximumSoul);
            _enemyPolicy = enemyPolicy ?? new SimpleEnemyPolicy();
            State = CoreLoopState.Initializing;
        }

        public BattleParticipant Player { get; }

        public BattleParticipant Enemy { get; }

        public CoreLoopState State { get; private set; }

        public int RoundNumber { get; private set; }

        public RoundResolution? LastResolution { get; private set; }

        public bool Start()
        {
            if (State != CoreLoopState.Initializing)
            {
                return false;
            }

            StartRound();
            return true;
        }

        public bool TryPlayerHit()
        {
            if (!CanAcceptPlayerAction())
            {
                return false;
            }

            Player.Draw(faceUp: true);
            if (Player.HandValue.IsBust)
            {
                ResolveRound();
                return true;
            }

            RunEnemyTurn();
            return true;
        }

        public bool TryPlayerStand()
        {
            if (!CanAcceptPlayerAction())
            {
                return false;
            }

            Player.Stand();
            RunEnemyTurn();
            return true;
        }

        private bool CanAcceptPlayerAction()
        {
            return State == CoreLoopState.PlayerTurn && !Player.IsStanding;
        }

        private void StartRound()
        {
            State = CoreLoopState.StartingRound;
            RoundNumber++;

            Player.Draw(faceUp: true);
            Enemy.Draw(faceUp: true);
            Player.Draw(faceUp: false);
            Enemy.Draw(faceUp: false);

            State = CoreLoopState.PlayerTurn;
        }

        private void RunEnemyTurn()
        {
            State = CoreLoopState.EnemyTurn;

            while (true)
            {
                if (Enemy.IsStanding)
                {
                    if (Player.IsStanding)
                    {
                        ResolveRound();
                    }
                    else
                    {
                        State = CoreLoopState.PlayerTurn;
                    }

                    return;
                }

                EnemyAction action = _enemyPolicy.Decide(Enemy.HandValue);
                if (action == EnemyAction.Stand)
                {
                    Enemy.Stand();
                    if (Player.IsStanding)
                    {
                        ResolveRound();
                    }
                    else
                    {
                        State = CoreLoopState.PlayerTurn;
                    }

                    return;
                }

                Enemy.Draw(faceUp: true);
                if (Enemy.HandValue.IsBust)
                {
                    ResolveRound();
                    return;
                }

                if (!Player.IsStanding)
                {
                    State = CoreLoopState.PlayerTurn;
                    return;
                }
            }
        }

        private void ResolveRound()
        {
            State = CoreLoopState.ResolvingRound;

            RoundResolution resolution = RoundResolver.Resolve(
                RoundNumber,
                Player.Hand.Cards,
                Enemy.Hand.Cards);
            _damageApplier.TryApply(resolution, Player.Soul, Enemy.Soul);
            LastResolution = resolution;

            Player.ClearRound();
            Enemy.ClearRound();

            if (Player.Soul.IsDepleted || Enemy.Soul.IsDepleted)
            {
                State = CoreLoopState.BattleEnded;
                return;
            }

            StartRound();
        }
    }
}
