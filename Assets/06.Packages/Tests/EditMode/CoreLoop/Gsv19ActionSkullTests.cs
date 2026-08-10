using System;
using System.Collections.Generic;
using System.Reflection;
using Border.Audio;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("GSV19")]
    public sealed class Gsv19ActionSkullTests
    {
        private const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";
        private const string SkullPrefabPath =
            "Assets/03. Prefabs/Item/Skull.prefab";
        private const string GameManagerPrefabPath =
            "Assets/03. Prefabs/Manager/GameManager.prefab";
        private const string SoundManagerPrefabPath =
            "Assets/03. Prefabs/Manager/SoundManager.prefab";
        private const string HammerPrefabPath =
            "Assets/03. Prefabs/Weapon/Hammer_Anim.prefab";
        private const string KnifePrefabPath =
            "Assets/03. Prefabs/Weapon/Knife_Anim.prefab";

        [Test]
        public void GSV19_U01_PlayerActionsMapToExactTargets()
        {
            AssertRequest(
                new PublicCombatAction(
                    CombatantSide.Player,
                    PublicCombatActionType.Hit),
                null,
                CombatActionSkullTargetKind.Hit,
                null);
            AssertRequest(
                new PublicCombatAction(
                    CombatantSide.Player,
                    PublicCombatActionType.Stand),
                null,
                CombatActionSkullTargetKind.Stand,
                null);
            AssertRequest(
                new PublicCombatAction(
                    CombatantSide.Player,
                    PublicCombatActionType.Change),
                null,
                CombatActionSkullTargetKind.Change,
                null);
            AssertRequest(
                new PublicCombatAction(
                    CombatantSide.Player,
                    PublicCombatActionType.UseCard,
                    "normal"),
                42,
                CombatActionSkullTargetKind.NormalCard,
                42);
            AssertRequest(
                new PublicCombatAction(
                    CombatantSide.Player,
                    PublicCombatActionType.DemonContract,
                    "demon"),
                77,
                CombatActionSkullTargetKind.DemonCard,
                77);
        }

        [Test]
        public void GSV19_U02_RejectedAutomaticAndFollowupInputsDoNotCreateCue()
        {
            Assert.That(
                CombatActionSkullPresenter.TryCreateRequest(
                    CombatantSide.Player,
                    null,
                    null,
                    out _),
                Is.False);
            Assert.That(
                CombatActionSkullPresenter.TryCreateRequest(
                    CombatantSide.Player,
                    new PublicCombatAction(
                        CombatantSide.Enemy,
                        PublicCombatActionType.Hit),
                    null,
                    out _),
                Is.False);

            PublicCombatAction enemyHit = new PublicCombatAction(
                CombatantSide.Enemy,
                PublicCombatActionType.Hit);
            EnemyDecision decision = new EnemyDecision(
                EnemyActionType.Hit,
                "test");
            var consumed = new CombatActionSkullCueKey(1, 2);
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    1,
                    2,
                    1,
                    0,
                    CoreLoopState.EnemyTurn,
                    enemyHit,
                    null,
                    decision,
                    consumed,
                    out _,
                    out _),
                Is.False);
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    1,
                    3,
                    2,
                    1,
                    CoreLoopState.ResolvingAutomaticCardEffect,
                    null,
                    null,
                    decision,
                    null,
                    out _,
                    out _),
                Is.False);
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    1,
                    3,
                    2,
                    2,
                    CoreLoopState.EnemyTurn,
                    enemyHit,
                    null,
                    decision,
                    null,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void GSV19_U03_EnemyCuesIncreaseAndRepeatedHitReplays()
        {
            PublicCombatAction action = new PublicCombatAction(
                CombatantSide.Enemy,
                PublicCombatActionType.Hit);
            EnemyDecision decision = new EnemyDecision(
                EnemyActionType.Hit,
                "test");
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    2,
                    1,
                    7,
                    6,
                    CoreLoopState.EnemyTurn,
                    action,
                    null,
                    decision,
                    null,
                    out CombatActionSkullRequest first,
                    out CombatActionSkullCueKey firstCue),
                Is.True);
            Assert.That(first.TargetKind, Is.EqualTo(
                CombatActionSkullTargetKind.Hit));
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    2,
                    2,
                    8,
                    7,
                    CoreLoopState.EnemyTurn,
                    action,
                    null,
                    decision,
                    firstCue,
                    out CombatActionSkullRequest second,
                    out CombatActionSkullCueKey secondCue),
                Is.True);
            Assert.That(second.TargetKind, Is.EqualTo(
                CombatActionSkullTargetKind.Hit));
            Assert.That(secondCue.ActionOrdinal, Is.EqualTo(2));
        }

        [Test]
        public void GSV19_U04_EnemyCardCuesPreservePhysicalIds()
        {
            var useCard = new PublicCombatAction(
                CombatantSide.Enemy,
                PublicCombatActionType.UseCard,
                "normal");
            var useCardDecision = new EnemyDecision(
                EnemyActionType.UseCard,
                31,
                null,
                "test",
                Array.Empty<EnemyActionScore>());
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    3,
                    4,
                    1,
                    0,
                    CoreLoopState.EnemyTurn,
                    useCard,
                    31,
                    useCardDecision,
                    null,
                    out CombatActionSkullRequest normal,
                    out _),
                Is.True);
            Assert.That(normal.CardId, Is.EqualTo(31));

            var demon = new PublicCombatAction(
                CombatantSide.Enemy,
                PublicCombatActionType.DemonContract,
                "demon");
            var demonDecision = new EnemyDecision(
                EnemyActionType.DemonContract,
                null,
                null,
                "test",
                Array.Empty<EnemyActionScore>(),
                demonContractSourceCardId: 53);
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    3,
                    5,
                    1,
                    0,
                    CoreLoopState.EnemyTurn,
                    demon,
                    53,
                    demonDecision,
                    null,
                    out CombatActionSkullRequest contract,
                    out _),
                Is.True);
            Assert.That(contract.CardId, Is.EqualTo(53));

            var initialContractCandidate = new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId: 0,
                demonContractInteractionKind:
                    DemonContractInteractionKind.ChooseContract,
                demonContractKind: DemonContractKind.Belphegor,
                demonContractDefinitionKey: DemonContractCatalog.BelphegorKey);
            EnemyDecision initialContractDecision = EnemyDecision.FromCandidate(
                initialContractCandidate,
                "initial-contract");
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    3,
                    5,
                    2,
                    1,
                    CoreLoopState.EnemyTurn,
                    demon,
                    53,
                    initialContractDecision,
                    null,
                    out CombatActionSkullRequest initialContract,
                    out _),
                Is.True);
            Assert.That(initialContract.CardId, Is.EqualTo(53));

            var followupCandidate = new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId: 0,
                demonContractInteractionKind:
                    DemonContractInteractionKind.BelphegorTopCard,
                demonContractKind: DemonContractKind.Belphegor);
            EnemyDecision followup = EnemyDecision.FromCandidate(
                followupCandidate,
                "followup");
            Assert.That(
                CombatActionSkullPresenter.TryCreateEnemyRequest(
                    3,
                    6,
                    3,
                    2,
                    CoreLoopState.EnemyTurn,
                    demon,
                    53,
                    followup,
                    null,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void GSV19_U05_ButtonOffsetsSeparateSidesAndRoundResetHidesView()
        {
            Vector3 player = CombatActionSkullPresenter
                .ResolveSharedButtonOffset(CombatantSide.Player);
            Vector3 enemy = CombatActionSkullPresenter
                .ResolveSharedButtonOffset(CombatantSide.Enemy);
            Assert.That(player.x, Is.LessThan(0f));
            Assert.That(enemy.x, Is.GreaterThan(0f));
            Assert.That(player.y, Is.EqualTo(enemy.y));
            Assert.That(player.y, Is.GreaterThan(0f));
            Assert.That(player.z, Is.GreaterThan(0f));
            Assert.That(enemy.z, Is.EqualTo(player.z));

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SkullPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                CombatActionSkullView view =
                    instance.GetComponent<CombatActionSkullView>();
                Assert.That(view, Is.Not.Null);
                view.Initialize(Color.white, Vector3.one);
                Assert.That(view.IsVisible, Is.False);
                view.EnsureVisibleAtHome();
                Assert.That(view.IsVisible, Is.True);
                view.ResetView(Vector3.zero);
                Assert.That(view.IsVisible, Is.False);
                Assert.That(view.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV19_U16_GameManagerRoundStartKeepsBothSkullsHidden()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                GameManagerPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                GameManager manager = instance.GetComponent<GameManager>();
                MethodInfo ensureRound = typeof(GameManager).GetMethod(
                    "EnsureActionSkullRound",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(ensureRound, Is.Not.Null);
                ensureRound.Invoke(manager, new object[] { 1 });

                var playerField = typeof(GameManager).GetField(
                    "_playerActionSkull",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var enemyField = typeof(GameManager).GetField(
                    "_enemyActionSkull",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(playerField, Is.Not.Null);
                Assert.That(enemyField, Is.Not.Null);
                var player = (CombatActionSkullView)playerField.GetValue(manager);
                var enemy = (CombatActionSkullView)enemyField.GetValue(manager);

                Assert.That(player.IsVisible, Is.False);
                Assert.That(enemy.IsVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [TestCase(-1, 1, true, true)]
        [TestCase(1, 1, false, false)]
        [TestCase(1, 2, false, true)]
        [TestCase(1, 2, true, false)]
        public void GSV19_U17_RoundResetWaitsForHeldTutorialTransition(
            int currentRound,
            int nextRound,
            bool holdTransition,
            bool expected)
        {
            Assert.That(
                GameManager.ShouldResetActionSkullsForRound(
                    currentRound,
                    nextRound,
                    holdTransition),
                Is.EqualTo(expected));
        }

        [Test]
        public void GSV19_U06_OnlyLosingSideDissolvesOnce()
        {
            Assert.That(
                CombatActionSkullPresenter.TryResolveLosingSide(
                    BattleOutcome.PlayerVictory,
                    out CombatantSide victoryLoser),
                Is.True);
            Assert.That(victoryLoser, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                CombatActionSkullPresenter.TryResolveLosingSide(
                    BattleOutcome.PlayerDefeat,
                    out CombatantSide defeatLoser),
                Is.True);
            Assert.That(defeatLoser, Is.EqualTo(CombatantSide.Player));
            Assert.That(
                CombatActionSkullPresenter.ShouldStartTerminalDissolve(
                    BattleOutcome.PlayerVictory,
                    isRunning: false,
                    isCompleted: false),
                Is.True);
            Assert.That(
                CombatActionSkullPresenter.ShouldStartTerminalDissolve(
                    BattleOutcome.PlayerVictory,
                    isRunning: true,
                    isCompleted: false),
                Is.False);
            Assert.That(
                CombatActionSkullPresenter.ShouldStartTerminalDissolve(
                    BattleOutcome.PlayerDefeat,
                    isRunning: false,
                    isCompleted: true),
                Is.False);
        }

        [Test]
        public void GSV19_U07_AllEnemySkullBaseColorsNormalizeDeckTints()
        {
            EnemyContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Enemies, Is.Not.Empty);
            foreach (EnemyCombatProfileDefinitionSO enemy in catalog.Enemies)
            {
                Color deckTint = enemy.DeckTopTint;
                float maximumChannel = Mathf.Max(
                    deckTint.r,
                    Mathf.Max(deckTint.g, deckTint.b));
                Color expected = new Color(
                    deckTint.r / maximumChannel,
                    deckTint.g / maximumChannel,
                    deckTint.b / maximumChannel,
                    deckTint.a);
                Color actual = enemy.SkullBaseColor;

                Assert.That(
                    actual.r,
                    Is.InRange(0f, 1f),
                    enemy.Key);
                Assert.That(actual.g, Is.InRange(0f, 1f), enemy.Key);
                Assert.That(actual.b, Is.InRange(0f, 1f), enemy.Key);
                Assert.That(actual.a, Is.InRange(0f, 1f), enemy.Key);
                Assert.That(
                    actual.r,
                    Is.EqualTo(expected.r).Within(0.000001f),
                    enemy.Key);
                Assert.That(
                    actual.g,
                    Is.EqualTo(expected.g).Within(0.000001f),
                    enemy.Key);
                Assert.That(
                    actual.b,
                    Is.EqualTo(expected.b).Within(0.000001f),
                    enemy.Key);
                Assert.That(
                    actual.a,
                    Is.EqualTo(expected.a).Within(0.000001f),
                    enemy.Key);
            }
        }

        [Test]
        public void GSV19_U08_PrefabsShaderAndSoundCatalogAreWired()
        {
            GameObject skullPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SkullPrefabPath);
            CombatActionSkullView skull =
                skullPrefab.GetComponent<CombatActionSkullView>();
            Assert.That(skull, Is.Not.Null);
            Transform model = skullPrefab.transform.Find("Model");
            Assert.That(model, Is.Not.Null);
            Renderer renderer = model.GetComponentInChildren<Renderer>(true);
            Assert.That(renderer, Is.Not.Null);
            MeshRenderer rootRenderer = skullPrefab.GetComponent<MeshRenderer>();
            Assert.That(rootRenderer, Is.Not.Null);
            Assert.That(rootRenderer.enabled, Is.False);
            Assert.That(
                renderer.sharedMaterial.HasProperty("_DissolveAmount"),
                Is.True);
            Assert.That(
                renderer.sharedMaterial.shader.keywordSpace.FindKeyword(
                    "_DISSOLVE_ON").isValid,
                Is.True);
            var skullObject = new SerializedObject(skull);
            Assert.That(
                skullObject.FindProperty("useArrivalPunchRotation").boolValue,
                Is.True);
            Assert.That(
                skullObject.FindProperty("punchRotation").vector3Value.sqrMagnitude,
                Is.GreaterThan(0f));
            Assert.That(
                skullObject.FindProperty("punchDuration").floatValue,
                Is.GreaterThan(0f));
            Assert.That(
                skullObject.FindProperty("punchVibrato").intValue,
                Is.GreaterThanOrEqualTo(1));
            Assert.That(
                skullObject.FindProperty("punchElasticity").floatValue,
                Is.InRange(0f, 1f));
            Assert.That(
                skullObject.FindProperty("useRandomYRotation").boolValue,
                Is.True);
            Vector2 randomYRotationRange =
                skullObject.FindProperty("randomYRotationRange").vector2Value;
            Assert.That(randomYRotationRange.x, Is.LessThanOrEqualTo(
                randomYRotationRange.y));
            float configuredMoveDuration =
                skullObject.FindProperty("moveDuration").floatValue;
            float configuredPunchDuration =
                skullObject.FindProperty("punchDuration").floatValue;
            Assert.That(
                skull.MoveDuration,
                Is.EqualTo(configuredMoveDuration + configuredPunchDuration)
                    .Within(0.000001f));

            GameObject managerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(GameManagerPrefabPath);
            GameManager manager = managerPrefab.GetComponent<GameManager>();
            Assert.That(manager, Is.Not.Null);
            var managerObject = new SerializedObject(manager);
            Assert.That(
                managerObject.FindProperty("actionSkullPrefab")
                    .objectReferenceValue,
                Is.EqualTo(skull));
            Assert.That(
                managerObject.FindProperty("knifeBaseStateName").stringValue,
                Is.EqualTo("Base Layer.Empty"));
            Assert.That(
                managerObject.FindProperty("playerKnifeReadyStateName").stringValue,
                Is.EqualTo("Base Layer.Knife_Start"));
            Assert.That(
                managerObject.FindProperty("enemyKnifeReadyStateName").stringValue,
                Is.EqualTo("Base Layer.Knife_Start_Enemy"));
            Assert.That(
                managerObject.FindProperty("playerKnifeSuccessStateName").stringValue,
                Is.EqualTo("Base Layer.Knife_Attack"));
            Assert.That(
                managerObject.FindProperty("enemyKnifeSuccessStateName").stringValue,
                Is.EqualTo("Base Layer.Knife_Attack_Enemy"));

            GameObject soundPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SoundManagerPrefabPath);
            SoundManager soundManager = soundPrefab.GetComponent<SoundManager>();
            Assert.That(soundManager, Is.Not.Null);
            var soundObject = new SerializedObject(soundManager);
            SerializedProperty entries = soundObject.FindProperty("sfxEntries");
            var ids = new HashSet<string>();
            for (int index = 0; index < entries.arraySize; index++)
            {
                ids.Add(entries.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("id").stringValue);
            }

            Assert.That(ids, Does.Contain("skullLay01"));
            Assert.That(ids, Does.Contain("skullLay02"));
            Assert.That(ids, Does.Contain("skullLay03"));
            Assert.That(ids, Does.Contain("skullLay04"));
            Assert.That(ids, Does.Contain("dissolve"));
        }

        [TestCase(CombatantSide.Player)]
        [TestCase(CombatantSide.Enemy)]
        public void GSV19_U09_ActualStandTransitionCreatesOneStandCue(
            CombatantSide side)
        {
            PublicCombatAction stand = new PublicCombatAction(
                side,
                PublicCombatActionType.Stand);
            Assert.That(
                CombatActionSkullPresenter.TryCreateImplicitStandRequest(
                    side,
                    4,
                    9,
                    wasStanding: false,
                    isStanding: true,
                    stand,
                    null,
                    out CombatActionSkullRequest request,
                    out CombatActionSkullCueKey cue),
                Is.True);
            Assert.That(request.Side, Is.EqualTo(side));
            Assert.That(request.TargetKind, Is.EqualTo(
                CombatActionSkullTargetKind.Stand));
            Assert.That(
                CombatActionSkullPresenter.TryCreateImplicitStandRequest(
                    side,
                    4,
                    9,
                    wasStanding: false,
                    isStanding: true,
                    stand,
                    cue,
                    out _,
                    out _),
                Is.False);
            Assert.That(
                CombatActionSkullPresenter.TryCreateImplicitStandRequest(
                    side,
                    4,
                    10,
                    wasStanding: true,
                    isStanding: true,
                    stand,
                    null,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void GSV19_U10_InitialContractUsesExactCardAndFollowupIsIgnored()
        {
            var initial = new PendingDemonContractInteraction(
                12,
                DemonContractInteractionKind.ChooseContract,
                null,
                new[]
                {
                    new DemonContractOption(3, 81, null, "contract"),
                },
                CombatPromptId.DemonChooseContract);
            Assert.That(
                CombatActionSkullPresenter.TryCreatePlayerInitialContractRequest(
                    initial,
                    12,
                    3,
                    out CombatActionSkullRequest request),
                Is.True);
            Assert.That(request.TargetKind, Is.EqualTo(
                CombatActionSkullTargetKind.DemonCard));
            Assert.That(request.CardId, Is.EqualTo(81));

            var followup = new PendingDemonContractInteraction(
                13,
                DemonContractInteractionKind.BelphegorTopCard,
                DemonContractKind.Belphegor,
                new[]
                {
                    new DemonContractOption(0, null, null, "keep"),
                    new DemonContractOption(1, null, null, "move"),
                },
                CombatPromptId.DemonBelphegorTopCard,
                sourceContractCardId: 81);
            Assert.That(
                CombatActionSkullPresenter.TryCreatePlayerInitialContractRequest(
                    followup,
                    13,
                    0,
                    out _),
                Is.False);
        }

        [Test]
        public void GSV19_U11_OnlyAcceptedPlayerPoisonStandTargetsStand()
        {
            var pending = new PendingAutomaticCardInteraction(
                9,
                42,
                CardEffectKind.Poison,
                CombatantSide.Player,
                CombatantSide.Player,
                AutomaticCardChoiceKind.PoisonDecision,
                CombatPromptId.AutomaticPoisonDecision,
                new[]
                {
                    new AutomaticCardChoiceOption(
                        PoisonEffectHandler.StandNowOptionId,
                        "stand"),
                    new AutomaticCardChoiceOption(
                        PoisonEffectHandler.PaySoulOptionId,
                        "pay")
                });

            Assert.That(
                CombatActionSkullPresenter.TryCreatePlayerAutomaticChoiceRequest(
                    pending,
                    pending.InteractionId,
                    PoisonEffectHandler.StandNowOptionId,
                    out CombatActionSkullRequest request),
                Is.True);
            Assert.That(request.Side, Is.EqualTo(CombatantSide.Player));
            Assert.That(request.TargetKind,
                Is.EqualTo(CombatActionSkullTargetKind.Stand));
            Assert.That(
                CombatActionSkullPresenter.TryCreatePlayerAutomaticChoiceRequest(
                    pending,
                    pending.InteractionId,
                    PoisonEffectHandler.PaySoulOptionId,
                    out _),
                Is.False);
            Assert.That(
                CombatActionSkullPresenter.TryCreatePlayerAutomaticChoiceRequest(
                    pending,
                    pending.InteractionId + 1,
                    PoisonEffectHandler.StandNowOptionId,
                    out _),
                Is.False);
        }

        [Test]
        public void GSV19_U12_EnemyDecisionOrdinalIsVisibleOnEveryDecisionBeat()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(10, 7, 2, 3),
                CreateRankDeck(2, 3, 4, 5, 10));
            Assert.That(battle.Start(), Is.True);
            var decisionOrdinals = new List<int>();
            int lastPublicActionOrdinal = battle.PublicActionHistory.Count;
            battle.Stepped += () =>
            {
                int publicActionOrdinal = battle.PublicActionHistory.Count;
                PublicCombatAction action = battle.LastPublicAction;
                if (publicActionOrdinal == lastPublicActionOrdinal ||
                    action == null ||
                    action.ActorSide != CombatantSide.Enemy)
                {
                    return;
                }

                lastPublicActionOrdinal = publicActionOrdinal;
                decisionOrdinals.Add(battle.EnemyDecisionOrdinal);
            };

            Assert.That(battle.TryPlayerStand(), Is.True);
            Assert.That(decisionOrdinals.Count, Is.GreaterThanOrEqualTo(2));
            for (int index = 1; index < decisionOrdinals.Count; index++)
            {
                Assert.That(
                    decisionOrdinals[index],
                    Is.GreaterThan(decisionOrdinals[index - 1]));
            }
        }

        [Test]
        public void GSV19_U13_WeaponAnimatorsRequireValidStartupState()
        {
            GameObject hammerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HammerPrefabPath);
            GameObject hammerInstance = UnityEngine.Object.Instantiate(
                hammerPrefab);
            try
            {
                HammerAnimationController hammer =
                    hammerInstance.GetComponent<HammerAnimationController>();
                Assert.That(hammer, Is.Not.Null);
                Animator hammerAnimator = hammerInstance.GetComponent<Animator>();
                Assert.That(hammerAnimator, Is.Not.Null);
                Assert.That(
                    hammerAnimator.HasState(
                        0,
                        Animator.StringToHash(
                            "Base Layer.Hammer_ReadyAttack")),
                    Is.True);
                Assert.That(
                    hammerAnimator.HasState(
                        0,
                        Animator.StringToHash("Base Layer.Hammer_Smash")),
                    Is.True);
                Assert.That(
                    hammerAnimator.HasState(
                        0,
                        Animator.StringToHash(
                            "Base Layer.Hammer_EnemySmash")),
                    Is.True);
                var cue = new GameSceneHammerAnimationCue(
                    1,
                    7,
                    CombatantSide.Player,
                    GameSceneHammerAnimationPhase.Smash,
                    1,
                    targetCardId: 11);

                Assert.That(hammer.TryPlay(cue, null, null), Is.False);
                Assert.That(hammer.IsSmashAnimationPlaying, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hammerInstance);
            }

            GameObject knifePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(KnifePrefabPath);
            GameObject knifeInstance = UnityEngine.Object.Instantiate(
                knifePrefab);
            try
            {
                Animator knifeAnimator = knifeInstance.GetComponent<Animator>();
                Assert.That(knifeAnimator, Is.Not.Null);
                knifeAnimator.Rebind();
                knifeAnimator.Update(0f);
                Assert.That(
                    knifeAnimator.GetCurrentAnimatorStateInfo(0).IsName(
                        "Base Layer.Empty"),
                    Is.True);
                string[] requiredStates =
                {
                    "Base Layer.Knife_Start",
                    "Base Layer.Knife_Start_Enemy",
                    "Base Layer.Knife_Attack",
                    "Base Layer.Knife_Attack_Enemy",
                    "Base Layer.Knife_Disappear",
                    "Base Layer.Knife_Disappear_Enemy",
                };
                foreach (string stateName in requiredStates)
                {
                    Assert.That(
                        knifeAnimator.HasState(
                            0,
                            Animator.StringToHash(stateName)),
                        Is.True,
                        stateName);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(knifeInstance);
            }
        }

        [Test]
        public void GSV19_U14_HammerRootAlignmentSurvivesAnimatorTargetReset()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HammerPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Animator animator = instance.GetComponent<Animator>();
                Transform animatedTarget =
                    instance.transform.Find("Hammer_Rigging/Target");
                Assert.That(animator, Is.Not.Null);
                Assert.That(animatedTarget, Is.Not.Null);

                animator.Rebind();
                animator.Update(0f);
                Vector3 desiredPosition = new Vector3(100f, 80f, 60f);
                animator.Play("Base Layer.Hammer_ReadyAttack", 0, 0f);
                animator.Update(0f);

                Assert.That(
                    HammerAnimationController.TryAlignRootToTarget(
                        instance.transform,
                        animatedTarget,
                        desiredPosition),
                    Is.True);
                Assert.That(
                    Vector3.Distance(animatedTarget.position, desiredPosition),
                    Is.LessThan(0.001f));
                animator.Update(0.25f);
                Assert.That(
                    Vector3.Distance(animatedTarget.position, desiredPosition),
                    Is.LessThan(0.001f));

                animator.Play("Base Layer.Hammer_Smash", 0, 0f);
                animator.Update(0f);
                Assert.That(
                    HammerAnimationController.TryAlignRootToTarget(
                        instance.transform,
                        animatedTarget,
                        desiredPosition),
                    Is.True);
                Assert.That(
                    Vector3.Distance(animatedTarget.position, desiredPosition),
                    Is.LessThan(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV19_U15_ViewAppliesOnlyInstanceBaseAndFresnelColors()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SkullPrefabPath);
            Transform prefabModel = prefab.transform.Find("Model");
            Assert.That(prefabModel, Is.Not.Null);
            Renderer prefabRenderer = prefabModel.GetComponentInChildren<Renderer>(
                true);
            Material sharedMaterial = prefabRenderer.sharedMaterial;
            Color sharedBaseColor = sharedMaterial.GetColor("_BaseColor");
            Color sharedFresnelColor = sharedMaterial.GetColor("_RimColor");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                CombatActionSkullView view =
                    instance.GetComponent<CombatActionSkullView>();
                Transform instanceModel = instance.transform.Find("Model");
                Assert.That(instanceModel, Is.Not.Null);
                Renderer instanceRenderer =
                    instanceModel.GetComponentInChildren<Renderer>(true);
                Color expected = new Color(0.25f, 0.5f, 0.75f, 1f);
                Color expectedFresnel = new Color(0.9f, 0.55f, 0.3f, 1f);

                view.Initialize(expected, Vector3.zero);
                view.SetFresnelColor(expectedFresnel);

                Assert.That(
                    instanceRenderer.sharedMaterial,
                    Is.Not.SameAs(sharedMaterial));
                Color actual =
                    instanceRenderer.sharedMaterial.GetColor("_BaseColor");
                Assert.That(
                    actual.r,
                    Is.EqualTo(expected.r).Within(0.000001f));
                Assert.That(
                    actual.g,
                    Is.EqualTo(expected.g).Within(0.000001f));
                Assert.That(
                    actual.b,
                    Is.EqualTo(expected.b).Within(0.000001f));
                Assert.That(
                    actual.a,
                    Is.EqualTo(expected.a).Within(0.000001f));
                Color actualFresnel =
                    instanceRenderer.sharedMaterial.GetColor("_RimColor");
                Assert.That(
                    actualFresnel.r,
                    Is.EqualTo(expectedFresnel.r).Within(0.000001f));
                Assert.That(
                    actualFresnel.g,
                    Is.EqualTo(expectedFresnel.g).Within(0.000001f));
                Assert.That(
                    actualFresnel.b,
                    Is.EqualTo(expectedFresnel.b).Within(0.000001f));
                Assert.That(
                    actualFresnel.a,
                    Is.EqualTo(expectedFresnel.a).Within(0.000001f));
                Color currentSharedBaseColor =
                    sharedMaterial.GetColor("_BaseColor");
                Assert.That(
                    currentSharedBaseColor.r,
                    Is.EqualTo(sharedBaseColor.r).Within(0.000001f));
                Assert.That(
                    currentSharedBaseColor.g,
                    Is.EqualTo(sharedBaseColor.g).Within(0.000001f));
                Assert.That(
                    currentSharedBaseColor.b,
                    Is.EqualTo(sharedBaseColor.b).Within(0.000001f));
                Assert.That(
                    currentSharedBaseColor.a,
                    Is.EqualTo(sharedBaseColor.a).Within(0.000001f));
                Color currentSharedFresnelColor =
                    sharedMaterial.GetColor("_RimColor");
                Assert.That(
                    currentSharedFresnelColor.r,
                    Is.EqualTo(sharedFresnelColor.r).Within(0.000001f));
                Assert.That(
                    currentSharedFresnelColor.g,
                    Is.EqualTo(sharedFresnelColor.g).Within(0.000001f));
                Assert.That(
                    currentSharedFresnelColor.b,
                    Is.EqualTo(sharedFresnelColor.b).Within(0.000001f));
                Assert.That(
                    currentSharedFresnelColor.a,
                    Is.EqualTo(sharedFresnelColor.a).Within(0.000001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV19_U18_RandomYRotationAppliesToModelOnly()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SkullPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            GameObject target = new GameObject("Gsv19RandomYRotationTarget");
            try
            {
                CombatActionSkullView view =
                    instance.GetComponent<CombatActionSkullView>();
                Transform model = instance.transform.Find("Model");
                Assert.That(model, Is.Not.Null);
                view.Initialize(Color.white, new Vector3(1f, 2f, 3f));
                Vector3 modelBasePosition = model.localPosition;
                Quaternion modelBaseRotation = model.localRotation;
                Vector3 worldOffset = new Vector3(0.4f, -0.25f, 0.6f);
                UnityEngine.Random.InitState(19081926);
                MethodInfo moveToMethod =
                    typeof(CombatActionSkullView).GetMethod("MoveTo");
                Assert.That(moveToMethod, Is.Not.Null);
                moveToMethod.Invoke(
                    view,
                    new object[] { target.transform, worldOffset });

                FieldInfo followOffsetField =
                    typeof(CombatActionSkullView).GetField(
                        "_followOffset",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(followOffsetField, Is.Not.Null);
                Vector3 followOffset =
                    (Vector3)followOffsetField.GetValue(view);
                Quaternion relativeRotation =
                    Quaternion.Inverse(modelBaseRotation) * model.localRotation;
                float modelYRotation = Mathf.DeltaAngle(
                    0f,
                    relativeRotation.eulerAngles.y);
                Vector2 configuredRange = new SerializedObject(view)
                    .FindProperty("randomYRotationRange")
                    .vector2Value;

                Assert.That(followOffset.x, Is.EqualTo(worldOffset.x));
                Assert.That(followOffset.y, Is.EqualTo(worldOffset.y));
                Assert.That(followOffset.z, Is.EqualTo(worldOffset.z));
                Assert.That(model.localPosition,
                    Is.EqualTo(modelBasePosition));
                Assert.That(modelYRotation,
                    Is.InRange(configuredRange.x, configuredRange.y));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssertRequest(
            PublicCombatAction action,
            int? sourceCardId,
            CombatActionSkullTargetKind expectedKind,
            int? expectedCardId)
        {
            Assert.That(
                CombatActionSkullPresenter.TryCreateRequest(
                    CombatantSide.Player,
                    action,
                    sourceCardId,
                    out CombatActionSkullRequest request),
                Is.True);
            Assert.That(request.TargetKind, Is.EqualTo(expectedKind));
            Assert.That(request.CardId, Is.EqualTo(expectedCardId));
        }

        private static BlackjackDeck CreateRankDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int index = 0; index < ranks.Length; index++)
            {
                cards.Add(new BlackjackCard(index, ranks[index]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
