using System;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Editor-only debug entry point for forced enemy-card scenes.
    /// Attach it beside ShopDebugPanel and use its custom Inspector while in Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneEnemyAITestPanel : MonoBehaviour
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

        public bool DebugEnemyRevolverHit()
        {
            return StartEnemyCardScene(
                CardEffectKind.AutoPistol,
                pistolGuessNumber: 7);
        }

        public bool DebugEnemyRevolverMiss()
        {
            return StartEnemyCardScene(
                CardEffectKind.AutoPistol,
                pistolGuessNumber: 1);
        }

        public bool DebugEnemyHammer()
        {
            return StartEnemyCardScene(
                CardEffectKind.ThreatHammer,
                pistolGuessNumber: 7);
        }

        private bool StartEnemyCardScene(
            CardEffectKind effectKind,
            int pistolGuessNumber)
        {
            if (!CanRunTest)
            {
                return false;
            }

            var session = new CoreLoopSession(
                () => CreateBattle(effectKind, pistolGuessNumber));
            SetPrivateField(gameManager, "_session", session);
            SetPrivateField(gameManager, "_inputLocked", false);
            SetPrivateField(gameManager, "_choosingLighterRemoval", false);

            InvokePrivate(gameManager, "ResetRevolverAnimationState");
            HammerAnimationController hammer =
                FindFirstObjectByType<HammerAnimationController>(
                    FindObjectsInactive.Include);
            hammer?.Hide();

            InvokePrivate(gameManager, "RefreshView");
            InvokePrivate(
                gameManager,
                "ProcessInput",
                new object[] { new Func<bool>(session.TryPlayerStand) });
            return true;
        }

        private static CoreLoopBattle CreateBattle(
            CardEffectKind effectKind,
            int pistolGuessNumber)
        {
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    Card(0, 10, CardSuit.Spade),
                    Card(1, 7, CardSuit.Clover),
                    Card(2, 3, CardSuit.Spade),
                    Card(3, 4, CardSuit.Clover)
                });
            BlackjackDeck enemyDeck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    EnemyWeaponCard(100, effectKind),
                    Card(101, 2, CardSuit.Clover),
                    Card(102, 4, CardSuit.Spade),
                    Card(103, 5, CardSuit.Clover)
                });

            return new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                enemyMaximumSoul: 12,
                enemyPolicy: new ForcedEnemyCardPolicy(
                    effectKind,
                    pistolGuessNumber));
        }

        private static BlackjackCard EnemyWeaponCard(
            int id,
            CardEffectKind effectKind)
        {
            switch (effectKind)
            {
                case CardEffectKind.AutoPistol:
                    return new BlackjackCard(
                        id,
                        CardDefinitionCatalog.GetByKey("auto-pistol-7"),
                        suit: CardSuit.Spade);
                case CardEffectKind.ThreatHammer:
                    return new BlackjackCard(
                        id,
                        CardDefinitionCatalog.GetByKey("threat-hammer-6"),
                        suit: CardSuit.Spade);
                default:
                    throw new ArgumentOutOfRangeException(nameof(effectKind));
            }
        }

        private static BlackjackCard Card(int id, int rank, CardSuit suit)
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
                PrivateInstance);
            field?.SetValue(manager, value);
        }

        private static void InvokePrivate(
            GameManager manager,
            string methodName,
            object[] args = null)
        {
            MethodInfo method = typeof(GameManager).GetMethod(
                methodName,
                PrivateInstance);
            method?.Invoke(manager, args);
        }

        private sealed class ForcedEnemyCardPolicy : IEnemyBehaviorPolicy
        {
            private readonly CardEffectKind _effectKind;
            private readonly int _pistolGuessNumber;

            public ForcedEnemyCardPolicy(
                CardEffectKind effectKind,
                int pistolGuessNumber)
            {
                _effectKind = effectKind;
                _pistolGuessNumber = Mathf.Clamp(pistolGuessNumber, 1, 10);
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                if (observation == null)
                {
                    throw new ArgumentNullException(nameof(observation));
                }

                EnemyActionCandidate candidate =
                    SelectPendingCardEffectCandidate(observation) ??
                    SelectCardUseCandidate(observation) ??
                    SelectFallbackCandidate(observation);

                return new EnemyDecision(
                    candidate.ActionType,
                    candidate.CardId,
                    candidate.CardEffectOptionId,
                    "debug-force-enemy-" + _effectKind.ToString(),
                    Array.Empty<EnemyActionScore>(),
                    candidate.DemonContractOptionId,
                    candidate.DemonContractSourceCardId);
            }

            private EnemyActionCandidate SelectPendingCardEffectCandidate(
                EnemyObservation observation)
            {
                if (observation.PendingCardEffectKind == CardEffectKind.AutoPistol)
                {
                    return FindCandidate(
                        observation,
                        candidate =>
                            candidate.ActionType == EnemyActionType.UseCard &&
                            candidate.CardEffectOptionNumericValue ==
                                _pistolGuessNumber);
                }

                if (observation.PendingCardEffectKind == CardEffectKind.ThreatHammer)
                {
                    EnemyActionCandidate selected = null;
                    foreach (EnemyActionCandidate candidate in
                        observation.ActionCandidates)
                    {
                        if (candidate.ActionType != EnemyActionType.UseCard ||
                            !candidate.CardEffectOptionCardRank.HasValue)
                        {
                            continue;
                        }

                        if (selected == null ||
                            candidate.CardEffectOptionCardRank.Value >
                                selected.CardEffectOptionCardRank.Value)
                        {
                            selected = candidate;
                        }
                    }

                    return selected;
                }

                return null;
            }

            private EnemyActionCandidate SelectCardUseCandidate(
                EnemyObservation observation)
            {
                return FindCandidate(
                    observation,
                    candidate =>
                        candidate.ActionType == EnemyActionType.UseCard &&
                        !candidate.CardEffectOptionId.HasValue &&
                        CardDefinitionCatalog
                            .GetByKey(candidate.CardDefinitionKey)
                            .Effect == _effectKind);
            }

            private static EnemyActionCandidate SelectFallbackCandidate(
                EnemyObservation observation)
            {
                return FindCandidate(
                        observation,
                        candidate => candidate.ActionType == EnemyActionType.Stand) ??
                    FindCandidate(
                        observation,
                        candidate => candidate.ActionType == EnemyActionType.Hit) ??
                    throw new InvalidOperationException(
                        "Debug enemy policy has no executable candidate.");
            }

            private static EnemyActionCandidate FindCandidate(
                EnemyObservation observation,
                Func<EnemyActionCandidate, bool> predicate)
            {
                foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
                {
                    if (predicate(candidate))
                    {
                        return candidate;
                    }
                }

                return null;
            }
        }
#endif
    }
}
