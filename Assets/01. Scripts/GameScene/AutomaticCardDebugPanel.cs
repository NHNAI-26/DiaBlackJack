using System;
using System.Collections.Generic;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Editor-only debug entry point for deterministic automatic-card scenes.
    /// Attach it to DebugManager and use its custom Inspector in Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AutomaticCardDebugPanel : MonoBehaviour
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

        public bool DebugPlayerPoison() =>
            StartAutomaticCardScene(CombatantSide.Player, CardEffectKind.Poison);

        public bool DebugPlayerResurrectionHerb() =>
            StartAutomaticCardScene(
                CombatantSide.Player,
                CardEffectKind.ResurrectionHerb);

        public bool DebugPlayerLieDetector() =>
            StartAutomaticCardScene(
                CombatantSide.Player,
                CardEffectKind.LieDetector);

        public bool DebugPlayerFlamethrower() =>
            StartAutomaticCardScene(
                CombatantSide.Player,
                CardEffectKind.Flamethrower);

        public bool DebugPlayerPocketWatch() =>
            StartAutomaticCardScene(
                CombatantSide.Player,
                CardEffectKind.PocketWatch);

        public bool DebugEnemyPoison() =>
            StartAutomaticCardScene(CombatantSide.Enemy, CardEffectKind.Poison);

        public bool DebugEnemyResurrectionHerb() =>
            StartAutomaticCardScene(
                CombatantSide.Enemy,
                CardEffectKind.ResurrectionHerb);

        public bool DebugEnemyLieDetector() =>
            StartAutomaticCardScene(
                CombatantSide.Enemy,
                CardEffectKind.LieDetector);

        public bool DebugEnemyFlamethrower() =>
            StartAutomaticCardScene(
                CombatantSide.Enemy,
                CardEffectKind.Flamethrower);

        public bool DebugEnemyPocketWatch() =>
            StartAutomaticCardScene(
                CombatantSide.Enemy,
                CardEffectKind.PocketWatch);

        internal static CoreLoopBattle CreateBattle(
            CombatantSide ownerSide,
            CardEffectKind effectKind)
        {
            if (!IsAutomaticEffect(effectKind))
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(
                CreateDeckCards(
                    0,
                    ownerSide == CombatantSide.Player,
                    effectKind));
            BlackjackDeck enemyDeck = BlackjackDeck.CreateInDrawOrder(
                CreateDeckCards(
                    100,
                    ownerSide == CombatantSide.Enemy,
                    effectKind));
            IEnemyBehaviorPolicy enemyPolicy = ownerSide == CombatantSide.Enemy
                ? (IEnemyBehaviorPolicy)new HitOnceThenStandPolicy()
                : new StandPolicy();

            return new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                enemyMaximumSoul: 12,
                enemyPolicy: enemyPolicy);
        }

        private bool StartAutomaticCardScene(
            CombatantSide ownerSide,
            CardEffectKind effectKind)
        {
            if (!CanRunTest)
            {
                return false;
            }

            var session = new CoreLoopSession(
                () => CreateBattle(ownerSide, effectKind));
            if (effectKind == CardEffectKind.PocketWatch)
            {
                MarkPocketWatchTargetUsed(session.Battle, ownerSide);
            }

            SetPrivateField(gameManager, "_session", session);
            SetPrivateField(gameManager, "_inputLocked", false);
            SetPrivateField(gameManager, "_choosingLighterRemoval", false);
            InvokePrivate(gameManager, "ResetRevolverAnimationState");

            HammerAnimationController hammer =
                FindFirstObjectByType<HammerAnimationController>(
                    FindObjectsInactive.Include);
            hammer?.Hide();

            InvokePrivate(gameManager, "RefreshView");
            Func<bool> trigger = ownerSide == CombatantSide.Player
                ? session.TryPlayerHit
                : session.TryPlayerStand;
            InvokePrivate(
                gameManager,
                "ProcessInput",
                new object[] { trigger });
            return true;
        }

        private static IReadOnlyList<BlackjackCard> CreateDeckCards(
            int firstId,
            bool ownsAutomaticCard,
            CardEffectKind effectKind)
        {
            var cards = new List<BlackjackCard>();
            if (ownsAutomaticCard && effectKind == CardEffectKind.PocketWatch)
            {
                cards.Add(new BlackjackCard(
                    firstId,
                    CardDefinitionCatalog.GetByKey("threat-hammer-6"),
                    suit: CardSuit.Spade));
                cards.Add(PlainCard(firstId + 1, 2, CardSuit.Clover));
            }
            else
            {
                cards.Add(PlainCard(firstId, 2, CardSuit.Spade));
                cards.Add(PlainCard(firstId + 1, 3, CardSuit.Clover));
            }

            cards.Add(ownsAutomaticCard
                ? AutomaticCard(firstId + 2, effectKind)
                : PlainCard(firstId + 2, 4, CardSuit.Spade));
            cards.Add(PlainCard(firstId + 3, 5, CardSuit.Clover));
            cards.Add(PlainCard(firstId + 4, 6, CardSuit.Spade));
            cards.Add(PlainCard(firstId + 5, 7, CardSuit.Clover));
            cards.Add(PlainCard(firstId + 6, 8, CardSuit.Spade));
            return cards.AsReadOnly();
        }

        private static BlackjackCard AutomaticCard(
            int id,
            CardEffectKind effectKind)
        {
            string definitionKey;
            switch (effectKind)
            {
                case CardEffectKind.Poison:
                    definitionKey = CardDefinitionCatalog.PoisonKey;
                    break;
                case CardEffectKind.ResurrectionHerb:
                    definitionKey = CardDefinitionCatalog.ResurrectionHerbKey;
                    break;
                case CardEffectKind.LieDetector:
                    definitionKey = CardDefinitionCatalog.LieDetectorKey;
                    break;
                case CardEffectKind.Flamethrower:
                    definitionKey = CardDefinitionCatalog.FlamethrowerKey;
                    break;
                case CardEffectKind.PocketWatch:
                    definitionKey = CardDefinitionCatalog.PocketWatchKey;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            return new BlackjackCard(
                id,
                CardDefinitionCatalog.GetByKey(definitionKey),
                suit: CardSuit.Spade);
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

        private static bool IsAutomaticEffect(CardEffectKind effectKind)
        {
            return effectKind == CardEffectKind.Poison ||
                effectKind == CardEffectKind.ResurrectionHerb ||
                effectKind == CardEffectKind.LieDetector ||
                effectKind == CardEffectKind.Flamethrower ||
                effectKind == CardEffectKind.PocketWatch;
        }

        private static void MarkPocketWatchTargetUsed(
            CoreLoopBattle battle,
            CombatantSide ownerSide)
        {
            BattleParticipant owner = ownerSide == CombatantSide.Player
                ? battle.Player
                : battle.Enemy;
            foreach (BlackjackCard card in owner.Hand.Cards)
            {
                if (card.Definition.Activation == CardActivationKind.Manual)
                {
                    if (!card.TryBeginUse() || !card.TryCompleteUse())
                    {
                        throw new InvalidOperationException(
                            "Pocket watch debug target could not be marked used.");
                    }

                    return;
                }
            }

            throw new InvalidOperationException(
                "Pocket watch debug scenario has no manual card target.");
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

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return SelectDecision(
                    observation,
                    EnemyActionType.Stand,
                    "debug-automatic-stand");
            }
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
                        "debug-automatic-hit");
                }

                return SelectDecision(
                    observation,
                    EnemyActionType.Stand,
                    "debug-automatic-stand");
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
                foreach (EnemyActionCandidate candidate in
                    observation.ActionCandidates)
                {
                    if (candidate.ActionType == EnemyActionType.Stand ||
                        candidate.ActionType == EnemyActionType.Hit)
                    {
                        selected = candidate;
                        break;
                    }
                }
            }

            if (selected == null)
            {
                throw new InvalidOperationException(
                    "Automatic card debug policy has no action candidate.");
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
