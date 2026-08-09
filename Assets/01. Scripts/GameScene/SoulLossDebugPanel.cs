using System;
using System.Collections.Generic;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Editor-only debug entry point for the mutual-bust soul-loss presentation.
    /// Attach it to DebugManager and use its custom Inspector in Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulLossDebugPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ShopController shop;

#if UNITY_EDITOR
        private const BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        public bool HasGameManager => gameManager != null;

        public bool HasShop => shop != null;

        public bool IsShopOpen => shop != null && shop.IsOpen;

        public bool CanRunTest =>
            Application.isPlaying &&
            gameManager != null &&
            shop != null &&
            !shop.IsOpen;

        public bool DebugMutualBust()
        {
            if (!CanRunTest)
            {
                return false;
            }

            CoreLoopSession session = CreatePreparedMutualBustSession();
            InvokePrivate(gameManager, "ResetBattlePresentation");
            SetPrivateField(gameManager, "_stageSession", null);
            SetPrivateField(gameManager, "_completedStageSession", null);
            SetPrivateField(gameManager, "_session", session);
            SetPrivateField(gameManager, "_inputLocked", false);
            SetPrivateField(gameManager, "_choosingLighterRemoval", false);
            SetPrivateField(gameManager, "_tutorialDirector", null);
            SetPrivateField(
                gameManager,
                "_suppressHandRenderUntilRoundOneStart",
                false);
            InvokePrivate(gameManager, "SynchronizeSoulLossCursor");
            InvokePrivate(gameManager, "RefreshView");
            InvokePrivate(
                gameManager,
                "ProcessInput",
                new object[]
                {
                    (Func<bool>)session.TryPlayerStand,
                    null,
                    null
                });
            return true;
        }

        internal static CoreLoopSession CreatePreparedMutualBustSession()
        {
            CoreLoopSession session = new CoreLoopSession(CreateBattle);
            if (!session.TryPlayerHit() ||
                session.Battle.State != CoreLoopState.PlayerTurn ||
                session.Battle.LastResolution != null)
            {
                throw new InvalidOperationException(
                    "Mutual-bust debug battle could not reach the final Stand.");
            }

            return session;
        }

        private static CoreLoopBattle CreateBattle()
        {
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(
                CreateDeckCards(0, 10, 8, 4));
            BlackjackDeck enemyDeck = BlackjackDeck.CreateInDrawOrder(
                CreateDeckCards(100, 10, 8, 5));
            return new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 3,
                enemyMaximumSoul: 3,
                enemyPolicy: new HitOnceThenStandPolicy());
        }

        private static IReadOnlyList<BlackjackCard> CreateDeckCards(
            int firstId,
            int firstRank,
            int secondRank,
            int hitRank)
        {
            var cards = new List<BlackjackCard>
            {
                PlainCard(firstId, firstRank, CardSuit.Spade),
                PlainCard(firstId + 1, secondRank, CardSuit.Clover),
                PlainCard(firstId + 2, hitRank, CardSuit.Spade),
                PlainCard(firstId + 3, 2, CardSuit.Clover),
                PlainCard(firstId + 4, 3, CardSuit.Spade),
                PlainCard(firstId + 5, 6, CardSuit.Clover)
            };
            return cards.AsReadOnly();
        }

        private static BlackjackCard PlainCard(
            int id,
            int rank,
            CardSuit suit)
        {
            return new BlackjackCard(
                id,
                CardDefinitionCatalog.GetDefaultForRank(rank),
                suit: suit);
        }

        private static void SetPrivateField(
            GameManager manager,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(GameManager).GetField(
                fieldName,
                PrivateInstance) ?? throw new MissingFieldException(
                    typeof(GameManager).FullName,
                    fieldName);
            field.SetValue(manager, value);
        }

        private static void InvokePrivate(
            GameManager manager,
            string methodName,
            object[] args = null)
        {
            MethodInfo method = typeof(GameManager).GetMethod(
                methodName,
                PrivateInstance) ?? throw new MissingMethodException(
                    typeof(GameManager).FullName,
                    methodName);
            method.Invoke(manager, args);
        }

        private sealed class HitOnceThenStandPolicy : IEnemyBehaviorPolicy
        {
            private bool _hasHit;

            public EnemyDecision Decide(EnemyObservation observation)
            {
                if (!_hasHit && HasCandidate(observation, EnemyActionType.Hit))
                {
                    _hasHit = true;
                    return SelectDecision(
                        observation,
                        EnemyActionType.Hit,
                        "debug-mutual-bust-hit");
                }

                return SelectDecision(
                    observation,
                    EnemyActionType.Stand,
                    "debug-mutual-bust-stand");
            }
        }

        private static EnemyDecision SelectDecision(
            EnemyObservation observation,
            EnemyActionType preferredAction,
            string reasonCode)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            EnemyActionCandidate selected = null;
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.ActionType == preferredAction)
                {
                    selected = candidate;
                    break;
                }
            }

            if (selected == null)
            {
                throw new InvalidOperationException(
                    "Mutual-bust debug policy has no matching action candidate.");
            }

            return new EnemyDecision(
                selected.ActionType,
                selected.CardId,
                selected.CardEffectOptionId,
                reasonCode,
                Array.Empty<EnemyActionScore>(),
                selected.DemonContractOptionId,
                selected.DemonContractSourceCardId);
        }

        private static bool HasCandidate(
            EnemyObservation observation,
            EnemyActionType actionType)
        {
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.ActionType == actionType)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
