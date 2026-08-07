using System;
using System.Collections.Generic;
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
                    CoreLoopState.ResolvingAutomaticCardEffect,
                    null,
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
                    CoreLoopState.EnemyTurn,
                    demon,
                    53,
                    demonDecision,
                    null,
                    out CombatActionSkullRequest contract,
                    out _),
                Is.True);
            Assert.That(contract.CardId, Is.EqualTo(53));
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
        public void GSV19_U07_AllEnemySkullTintsMatchDeckTints()
        {
            EnemyContentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Enemies, Is.Not.Empty);
            foreach (EnemyCombatProfileDefinitionSO enemy in catalog.Enemies)
            {
                Assert.That(
                    enemy.SkullTint,
                    Is.EqualTo(enemy.DeckTopTint),
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
            Renderer renderer = skullPrefab.GetComponentInChildren<Renderer>(
                true);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(
                renderer.sharedMaterial.HasProperty("_DissolveAmount"),
                Is.True);
            Assert.That(
                renderer.sharedMaterial.shader.keywordSpace.FindKeyword(
                    "_DISSOLVE_ON").isValid,
                Is.True);

            GameObject managerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(GameManagerPrefabPath);
            GameManager manager = managerPrefab.GetComponent<GameManager>();
            Assert.That(manager, Is.Not.Null);
            var managerObject = new SerializedObject(manager);
            Assert.That(
                managerObject.FindProperty("actionSkullPrefab")
                    .objectReferenceValue,
                Is.EqualTo(skull));

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
    }
}
