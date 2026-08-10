using System.Collections.Generic;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("GSV18")]
    public sealed class SoulLossPresentationTests
    {
        private const string ColorScreenMaterialPath =
            "Assets/05. Arts/Shader/NHNPostProcessing.mat";
        private const string DebugManagerPrefabPath =
            "Assets/03. Prefabs/Manager/DebugManager.prefab";
        private const string GameScenePath =
            "Assets/00. Scenes/GameScene.unity";

        [Test]
        public void GSV18_U01_ActualLossIsClampedAndZeroLossIsNotRecorded()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 9, 4, 5 },
                new[] { 10, 7, 4, 5 },
                playerMaximumSoul: 3,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            battle.ApplySoulDamage(
                CombatantSide.Player,
                9,
                SoulLossCause.AutomaticCardCost);
            battle.ApplySoulDamage(
                CombatantSide.Player,
                1,
                SoulLossCause.AutomaticCardCost);

            Assert.That(battle.SoulLossHistory.Count, Is.EqualTo(1));
            SoulLossRecord record = battle.SoulLossHistory[0];
            Assert.That(record.Id, Is.Zero);
            Assert.That(record.TargetSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(record.SoulBefore, Is.EqualTo(3));
            Assert.That(record.SoulAfter, Is.Zero);
            Assert.That(record.MaximumSoul, Is.EqualTo(3));
            Assert.That(record.LossAmount, Is.EqualTo(3));
            Assert.That(record.ResolutionId, Is.Null);
        }

        [Test]
        public void GSV18_U02_FreeThenPaidChangeRecordsOnlyActualCost()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 2, 4, 9, 6, 7, 8 },
                new[] { 10, 7, 5, 6 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            Assert.That(battle.TrySelectChangedCard(0), Is.True);
            Assert.That(battle.SoulLossHistory, Is.Empty);

            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            Assert.That(battle.SoulLossHistory.Count, Is.EqualTo(1));
            SoulLossRecord record = battle.SoulLossHistory[0];
            Assert.That(record.Cause, Is.EqualTo(SoulLossCause.ChangeCost));
            Assert.That(record.LossAmount, Is.EqualTo(1));
            Assert.That(record.SoulBefore, Is.EqualTo(12));
            Assert.That(record.SoulAfter, Is.EqualTo(11));
        }

        [Test]
        public void GSV18_U03_CommonCostPathUsesMonotonicIdsAndCauses()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 9, 4, 5 },
                new[] { 10, 7, 4, 5 },
                playerMaximumSoul: 5,
                enemyMaximumSoul: 5);
            Assert.That(battle.Start(), Is.True);

            battle.ApplySoulDamage(
                CombatantSide.Player,
                1,
                SoulLossCause.DemonContractCost);
            battle.ApplySoulDamage(
                CombatantSide.Enemy,
                2,
                SoulLossCause.AutomaticCardCost);

            Assert.That(
                new[]
                {
                    battle.SoulLossHistory[0].Id,
                    battle.SoulLossHistory[1].Id
                },
                Is.EqualTo(new long[] { 0, 1 }));
            Assert.That(
                battle.SoulLossHistory[0].Cause,
                Is.EqualTo(SoulLossCause.DemonContractCost));
            Assert.That(
                battle.SoulLossHistory[1].Cause,
                Is.EqualTo(SoulLossCause.AutomaticCardCost));
        }

        [Test]
        public void GSV18_U04_RoundDamageLinksResolutionAndReachesViewModel()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 9, 4, 5 },
                new[] { 10, 7, 4, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.SoulLossHistory.Count, Is.EqualTo(1));
            SoulLossRecord record = battle.SoulLossHistory[0];
            Assert.That(record.Cause, Is.EqualTo(SoulLossCause.RoundDamage));
            Assert.That(
                record.ResolutionId,
                Is.EqualTo(battle.LastResolution.Value.Id));
            Assert.That(record.TargetSide, Is.EqualTo(CombatantSide.Enemy));
            GameSceneViewModel model = GameScenePresenter.Create(battle);
            Assert.That(model.SoulLossHistory.Count, Is.EqualTo(1));
            Assert.That(model.SoulLossHistory[0].Id, Is.EqualTo(record.Id));
        }

        [Test]
        public void GSV18_U05_TokenCountAndReplayCursorUseActualRecords()
        {
            IReadOnlyList<SoulLossRecord> records = new[]
            {
                new SoulLossRecord(
                    4,
                    CombatantSide.Player,
                    5,
                    3,
                    5,
                    SoulLossCause.RoundDamage,
                    10),
                new SoulLossRecord(
                    5,
                    CombatantSide.Enemy,
                    3,
                    2,
                    3,
                    SoulLossCause.RoundDamage,
                    10)
            };

            Assert.That(SoulLossPresentation.CountTokenUnits(records), Is.EqualTo(3));
            Assert.That(GameManager.HasUnqueuedSoulLoss(3, records), Is.True);
            Assert.That(GameManager.HasUnqueuedSoulLoss(5, records), Is.False);
        }

        [TestCase(1, 2, 1, true)]
        [TestCase(1, 1, 1, false)]
        [TestCase(2, 1, 1, false)]
        [TestCase(1, 2, 0, false)]
        public void GSV18_U11_NewRoundFlushesCarriedRoundSoulLossBeforePresentation(
            int previousRoundNumber,
            int currentRoundNumber,
            int pendingRecordCount,
            bool expected)
        {
            Assert.That(
                GameManager.ShouldFlushPendingRoundSoulLossBeforeNewRound(
                    previousRoundNumber,
                    currentRoundNumber,
                    pendingRecordCount),
                Is.EqualTo(expected));
        }

        [Test]
        public void GSV18_U06_TokenInspectorSettingsAreNormalizedSafely()
        {
            var settings = new SoulLossTokenSettings(
                new Color(2f, 0.5f, 0.25f, 1f),
                fontScale: 0f,
                minimumFontSize: 0f,
                tokenSize: new Vector2(-20f, 0f),
                fallSeconds: 0f,
                staggerSeconds: -1f,
                impactSeconds: 5f,
                fadeSeconds: 5f,
                startRandomX: -3f,
                startYRange: new Vector2(12f, -8f),
                driftX: -4f,
                fallDistanceRange: new Vector2(185f, 145f),
                rotation: -9f,
                playerAnchor: new Vector2(-1f, 2f),
                enemyFallbackAnchor: new Vector2(2f, -1f));

            Assert.That(settings.FontScale, Is.EqualTo(0.1f));
            Assert.That(settings.MinimumFontSize, Is.EqualTo(1f));
            Assert.That(settings.TokenSize, Is.EqualTo(Vector2.one));
            Assert.That(settings.FallSeconds, Is.EqualTo(0.01f));
            Assert.That(settings.StaggerSeconds, Is.Zero);
            Assert.That(settings.ImpactSeconds, Is.EqualTo(0.01f));
            Assert.That(settings.FadeSeconds, Is.EqualTo(0.01f));
            Assert.That(settings.StartRandomX, Is.Zero);
            Assert.That(settings.StartYRange, Is.EqualTo(new Vector2(-8f, 12f)));
            Assert.That(settings.DriftX, Is.Zero);
            Assert.That(
                settings.FallDistanceRange,
                Is.EqualTo(new Vector2(145f, 185f)));
            Assert.That(settings.Rotation, Is.Zero);
            Assert.That(settings.PlayerAnchor, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(
                settings.EnemyFallbackAnchor,
                Is.EqualTo(new Vector2(1f, 0f)));
        }

        [Test]
        public void GSV18_U07_FirstPlayerAndEnemyTokensStartAndImpactTogether()
        {
            IReadOnlyList<SoulLossRecord> records = new[]
            {
                new SoulLossRecord(
                    10,
                    CombatantSide.Player,
                    3,
                    2,
                    3,
                    SoulLossCause.RoundDamage,
                    7),
                new SoulLossRecord(
                    11,
                    CombatantSide.Enemy,
                    3,
                    2,
                    3,
                    SoulLossCause.RoundDamage,
                    7)
            };

            IReadOnlyList<float> startDelays =
                SoulLossPresentation.CreateTokenStartDelays(records, 0.12f);
            const float impactSeconds = 0.4f;

            Assert.That(startDelays, Has.Count.EqualTo(2));
            Assert.That(startDelays[0], Is.Zero);
            Assert.That(startDelays[1], Is.EqualTo(startDelays[0]));
            Assert.That(
                startDelays[1] + impactSeconds,
                Is.EqualTo(startDelays[0] + impactSeconds));
        }

        [Test]
        public void GSV18_U08_PlayerDamageFallsBackToColorScreenAndRestores()
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(
                ColorScreenMaterialPath);
            Assert.That(source, Is.Not.Null);
            Material material = new Material(source);
            var root = new GameObject("PlayerDamageFallbackTest");
            root.SetActive(false);
            PresentationManager manager =
                root.AddComponent<PresentationManager>();
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("colorScreenBlendMaterial")
                .objectReferenceValue = material;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            int colorId = Shader.PropertyToID("_ColorScreen");
            int strengthId = Shader.PropertyToID("_BlendStrength");
            Color originalColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            const float originalStrength = 0.15f;
            material.SetColor(colorId, originalColor);
            material.SetFloat(strengthId, originalStrength);

            try
            {
                manager.PlayPlayerDamagePresentation();

                Assert.That(
                    material.GetColor(colorId),
                    Is.EqualTo(new Color(0.85f, 0.04f, 0.04f, 1f)));
                Assert.That(
                    material.GetFloat(strengthId),
                    Is.EqualTo(0.65f).Within(0.0001f));

                manager.ForceRestoreTransientCameraEffects();

                Color restoredColor = material.GetColor(colorId);
                Assert.That(
                    restoredColor.r,
                    Is.EqualTo(originalColor.r).Within(0.0001f));
                Assert.That(
                    restoredColor.g,
                    Is.EqualTo(originalColor.g).Within(0.0001f));
                Assert.That(
                    restoredColor.b,
                    Is.EqualTo(originalColor.b).Within(0.0001f));
                Assert.That(
                    restoredColor.a,
                    Is.EqualTo(originalColor.a).Within(0.0001f));
                Assert.That(
                    material.GetFloat(strengthId),
                    Is.EqualTo(originalStrength).Within(0.0001f));
            }
            finally
            {
                manager.ForceRestoreTransientCameraEffects();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GSV18_U09_DebugStandCreatesMutualLossAndBothSoulRecords()
        {
            CoreLoopSession session =
                SoulLossDebugPanel.CreatePreparedMutualBustSession();

            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(session.Battle.LastResolution.HasValue, Is.True);
            RoundResolution resolution = session.Battle.LastResolution.Value;
            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.MutualLoss));
            Assert.That(session.Battle.Player.Soul.Current, Is.EqualTo(2));
            Assert.That(session.Battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(session.Battle.SoulLossHistory, Has.Count.EqualTo(2));
            Assert.That(
                session.Battle.SoulLossHistory[0].TargetSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(
                session.Battle.SoulLossHistory[1].TargetSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                session.Battle.SoulLossHistory[0].ResolutionId,
                Is.EqualTo(resolution.Id));
            Assert.That(
                session.Battle.SoulLossHistory[1].ResolutionId,
                Is.EqualTo(resolution.Id));
            Assert.That(
                session.Battle.SoulLossHistory[0].Cause,
                Is.EqualTo(SoulLossCause.RoundDamage));
            Assert.That(
                session.Battle.SoulLossHistory[1].Cause,
                Is.EqualTo(SoulLossCause.RoundDamage));
        }

        [Test]
        public void GSV18_U10_DebugManagerPrefabAndGameSceneWireSoulLossPanel()
        {
            GameObject debugManagerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DebugManagerPrefabPath);
            Assert.That(debugManagerPrefab, Is.Not.Null);
            Assert.That(
                debugManagerPrefab.GetComponent<SoulLossDebugPanel>(),
                Is.Not.Null);

            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(
                    GameScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                SoulLossDebugPanel panel = FindComponentInScene<SoulLossDebugPanel>(
                    scene);
                Assert.That(panel, Is.Not.Null);
                var serializedPanel = new SerializedObject(panel);
                Assert.That(
                    serializedPanel.FindProperty("gameManager")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedPanel.FindProperty("shop").objectReferenceValue,
                    Is.Not.Null);
            }
            finally
            {
                if (openedForTest)
                {
                    EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerMaximumSoul,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul: playerMaximumSoul,
                enemyMaximumSoul: enemyMaximumSoul);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            List<BlackjackCard> cards = new List<BlackjackCard>(ranks.Count);
            for (int index = 0; index < ranks.Count; index++)
            {
                cards.Add(new BlackjackCard(index, ranks[index]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static T FindComponentInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
