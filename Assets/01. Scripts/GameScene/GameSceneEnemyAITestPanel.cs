using System;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Editor-only panel for forcing enemy AI card-use scenes in GameScene. It builds a dedicated
    /// deterministic battle and then routes a player STAND through GameManager's normal input path,
    /// so timeline snapshots, enemy policy validation, camera cuts, and weapon animations all run
    /// through the production presentation flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneEnemyAITestPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

#if UNITY_EDITOR
        private const BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForGameScene()
        {
            if (FindFirstObjectByType<GameSceneEnemyAITestPanel>(
                    FindObjectsInactive.Include) != null ||
                FindFirstObjectByType<GameManager>(
                    FindObjectsInactive.Include) == null)
            {
                return;
            }

            var panelObject = new GameObject("Enemy AI Test Panel");
            panelObject.AddComponent<GameSceneEnemyAITestPanel>();
        }

        private void OnGUI()
        {
            if (ResolveGameManager() == null)
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            const float w = 190f;
            const float h = 32f;
            const float gap = 6f;
            float x = 184f;
            float y = Screen.height * 0.5f - (4f * h + 3f * gap) * 0.5f;

            GUI.Label(new Rect(x, y - 22f, w, 20f), "ENEMY AI TEST", _labelStyle);

            if (DebugButton(ref y, x, w, h, gap, "Enemy Revolver Hit"))
            {
                StartEnemyCardScene(CardEffectKind.AutoPistol, pistolGuessNumber: 7);
            }

            if (DebugButton(ref y, x, w, h, gap, "Enemy Revolver Miss"))
            {
                StartEnemyCardScene(CardEffectKind.AutoPistol, pistolGuessNumber: 1);
            }

            if (DebugButton(ref y, x, w, h, gap, "Enemy Hammer"))
            {
                StartEnemyCardScene(CardEffectKind.ThreatHammer, pistolGuessNumber: 7);
            }
        }

        private bool DebugButton(
            ref float y,
            float x,
            float w,
            float h,
            float gap,
            string label)
        {
            bool clicked = GUI.Button(new Rect(x, y, w, h), label, _buttonStyle);
            y += h + gap;
            return clicked;
        }

        private void StartEnemyCardScene(
            CardEffectKind effectKind,
            int pistolGuessNumber)
        {
            GameManager manager = ResolveGameManager();
            if (manager == null)
            {
                return;
            }

            var session = new CoreLoopSession(
                () => CreateBattle(effectKind, pistolGuessNumber));
            SetPrivateField(manager, "_session", session);
            SetPrivateField(manager, "_inputLocked", false);
            SetPrivateField(manager, "_choosingLighterRemoval", false);

            InvokePrivate(manager, "ResetRevolverAnimationState");
            HammerAnimationController hammer =
                FindFirstObjectByType<HammerAnimationController>(
                    FindObjectsInactive.Include);
            hammer?.Hide();

            InvokePrivate(manager, "RefreshView");
            InvokePrivate(
                manager,
                "ProcessInput",
                new object[] { new Func<bool>(session.TryPlayerStand) });
        }

        private CoreLoopBattle CreateBattle(
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

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>(
                    FindObjectsInactive.Include);
            }

            return gameManager;
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
