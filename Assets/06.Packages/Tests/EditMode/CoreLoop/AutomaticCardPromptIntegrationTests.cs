using System;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("CP04")]
    public sealed class AutomaticCardPromptIntegrationTests
    {
        private const string CatalogPath =
            "Assets/02. ScriptableObjects/UI/CombatPromptCatalog.asset";
        private const string HudPrefabPath =
            "Assets/03. Prefabs/UI/HUD.prefab";
        private const string DebugManagerPrefabPath =
            "Assets/03. Prefabs/Manager/DebugManager.prefab";
        private const string GameScenePath =
            "Assets/00. Scenes/GameScene.unity";

        [Test]
        public void CP04_U02_CatalogCoversFiveResultIdsAndResolvesEveryToken()
        {
            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath);
            AutomaticCardResultPromptId[] ids = Enum
                .GetValues(typeof(AutomaticCardResultPromptId))
                .Cast<AutomaticCardResultPromptId>()
                .Where(id => id != AutomaticCardResultPromptId.None)
                .ToArray();

            Assert.That(catalog, Is.Not.Null);
            Assert.That(ids.Select(id => (int)id),
                Is.EqualTo(Enumerable.Range(1, 5)));
            Assert.That(catalog.GetValidationErrors(), Is.Empty);
            Assert.That(
                catalog.AutomaticResultEntries.Select(entry => entry.Id),
                Is.EquivalentTo(ids));

            AssertResolved(
                catalog,
                new AutomaticCardResultPromptRequest(
                    AutomaticCardResultPromptId.Poison,
                    "Poison",
                    CombatantSide.Player,
                    AutomaticCardSourceDisposition.RetainFaceUp,
                    outcome: AutomaticCardResultOutcome.WinHealReserved),
                "광신도",
                "내 [독극물]",
                "이번 라운드에서 승리하면 영혼 5를 회복합니다");
            AssertResolved(
                catalog,
                new AutomaticCardResultPromptRequest(
                    AutomaticCardResultPromptId.ResurrectionHerb,
                    "Resurrection Herb",
                    CombatantSide.Enemy,
                    AutomaticCardSourceDisposition.Discard,
                    AutomaticCardDecisionOutcome.Accepted,
                    AutomaticCardDecisionOutcome.Declined),
                "광신도",
                "광신도의 [부활초]",
                "나는 영혼 1을 지불해 패를 다시 받았습니다",
                "광신도 측은 효과를 사용하지 않았습니다");
            AssertResolved(
                catalog,
                new AutomaticCardResultPromptRequest(
                    AutomaticCardResultPromptId.LieDetector,
                    "Lie Detector",
                    CombatantSide.Player,
                    AutomaticCardSourceDisposition.RetainFaceUp,
                    declaredNumber: 7,
                    comparison:
                        AutomaticCardHiddenComparison.AtLeastDeclared),
                "광신도",
                "내 [거짓말 탐지기]",
                "광신도의 비공개 카드는 7 이상입니다");
            AssertResolved(
                catalog,
                new AutomaticCardResultPromptRequest(
                    AutomaticCardResultPromptId.Flamethrower,
                    "Flamethrower",
                    CombatantSide.Player,
                    AutomaticCardSourceDisposition.RetainFaceUp,
                    AutomaticCardDecisionOutcome.Accepted,
                    AutomaticCardDecisionOutcome.Declined),
                "광신도",
                "나는 공개 카드 1장을 버렸습니다",
                "광신도 측은 공개 카드를 버리지 않았습니다");
            AssertResolved(
                catalog,
                new AutomaticCardResultPromptRequest(
                    AutomaticCardResultPromptId.PocketWatch,
                    "Pocket Watch",
                    CombatantSide.Enemy,
                    AutomaticCardSourceDisposition.Discard,
                    reactivatedCardDisplayName: "위협용 해머"),
                "광신도",
                "광신도의 [회중시계]",
                "선택한 [위협용 해머] 카드를 다시 사용할 수 있게 했습니다");
        }

        [Test]
        public void CP04_U03_ResultValidationFindsDuplicateEmptyMissingAndBadToken()
        {
            CombatPromptCatalogSO catalog =
                ScriptableObject.CreateInstance<CombatPromptCatalogSO>();
            try
            {
                var emptyEnemyFallback =
                    new CombatPromptCatalogSO.AutomaticResultEntry(
                        AutomaticCardResultPromptId.PocketWatch,
                        "{target}",
                        sourceLabel: "회중시계",
                        emptyTargetTemplate: "{enemy}");
                typeof(CombatPromptCatalogSO.AutomaticResultEntry)
                    .GetField(
                        "enemyOwnerLabel",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(emptyEnemyFallback, string.Empty);
                catalog.ReplaceAutomaticResultEntriesForEditor(new[]
                {
                    new CombatPromptCatalogSO.AutomaticResultEntry(
                        AutomaticCardResultPromptId.Poison,
                        string.Empty,
                        sourceLabel: string.Empty),
                    new CombatPromptCatalogSO.AutomaticResultEntry(
                        AutomaticCardResultPromptId.Poison,
                        "{mystery}",
                        sourceLabel: "독극물"),
                    emptyEnemyFallback
                });

                var errors = catalog.GetValidationErrors();
                Assert.That(errors.Any(error =>
                    error.Contains("Duplicate automatic result")), Is.True);
                Assert.That(errors.Any(error =>
                    error.Contains("empty")), Is.True);
                Assert.That(errors.Any(error =>
                    error.Contains("Unknown token")), Is.True);
                Assert.That(errors.Any(error =>
                    error.Contains("Missing automatic result")), Is.True);
                Assert.That(errors.Any(error =>
                    error.Contains("Automatic result label is empty")),
                    Is.True);
                Assert.That(errors.Any(error =>
                    error.Contains("source label is empty")), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CP04_U04_SelectionWinsThenResultUsesSamePromptView()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                HudPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                GameHudView hud = instance.GetComponent<GameHudView>();
                CombatPromptView promptView =
                    instance.GetComponentInChildren<CombatPromptView>(true);
                SerializedObject promptSerialized =
                    new SerializedObject(promptView);
                Component promptText = promptSerialized
                    .FindProperty("promptText")
                    .objectReferenceValue as Component;
                CoreLoopViewModel core = CoreLoopPresenter.Create(
                    CreateStartedBattle(),
                    EnemyCombatProfileCatalog.GunslingerKey);
                var resultRequest = new AutomaticCardResultPromptRequest(
                    AutomaticCardResultPromptId.Poison,
                    "독극물",
                    CombatantSide.Enemy,
                    AutomaticCardSourceDisposition.RetainFaceUp,
                    outcome: AutomaticCardResultOutcome.WinHealReserved);

                hud.Render(
                    core,
                    CreateHudModel(
                        new CombatPromptRequest(CombatPromptId.ChangeCard),
                        resultRequest));
                Assert.That(promptView.gameObject.activeSelf, Is.True);
                Assert.That(ReadText(promptText), Does.Contain("교체"));
                Assert.That(ReadText(promptText), Does.Not.Contain("독극물"));

                hud.Render(core, CreateHudModel(null, resultRequest));
                Assert.That(promptView.gameObject.activeSelf, Is.True);
                Assert.That(
                    ReadText(promptText),
                    Does.Contain("총잡이의 [독극물]"));

                hud.Render(core, CreateHudModel(null, null));
                Assert.That(promptView.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [TestCase(CombatantSide.Player, CardEffectKind.Poison)]
        [TestCase(CombatantSide.Player, CardEffectKind.ResurrectionHerb)]
        [TestCase(CombatantSide.Player, CardEffectKind.LieDetector)]
        [TestCase(CombatantSide.Player, CardEffectKind.Flamethrower)]
        [TestCase(CombatantSide.Player, CardEffectKind.PocketWatch)]
        [TestCase(CombatantSide.Enemy, CardEffectKind.Poison)]
        [TestCase(CombatantSide.Enemy, CardEffectKind.ResurrectionHerb)]
        [TestCase(CombatantSide.Enemy, CardEffectKind.LieDetector)]
        [TestCase(CombatantSide.Enemy, CardEffectKind.Flamethrower)]
        [TestCase(CombatantSide.Enemy, CardEffectKind.PocketWatch)]
        public void CP04_U05_DebugScenarioUsesRealAutomaticCardFlow(
            CombatantSide ownerSide,
            CardEffectKind effectKind)
        {
            CoreLoopBattle battle =
                AutomaticCardDebugPanel.CreateBattle(ownerSide, effectKind);
            Assert.That(battle.Start(), Is.True);
            if (effectKind == CardEffectKind.PocketWatch)
            {
                MarkManualCardUsed(battle, ownerSide);
            }

            AutomaticCardResultPromptRequest? presentedResult = null;
            battle.Stepped += () =>
            {
                if (battle.AutomaticCardResultPrompt.HasValue)
                {
                    presentedResult = battle.AutomaticCardResultPrompt;
                }
            };

            bool triggered = ownerSide == CombatantSide.Player
                ? battle.TryPlayerHit()
                : battle.TryPlayerStand();
            Assert.That(triggered, Is.True);

            int guard = 0;
            while (battle.PendingPlayerAutomaticInteraction != null &&
                guard++ < 4)
            {
                PendingAutomaticCardInteraction pending =
                    battle.PendingPlayerAutomaticInteraction;
                Assert.That(
                    battle.TryResolvePlayerAutomaticCardChoice(
                        pending.InteractionId,
                        pending.Options[0].OptionId),
                    Is.True);
            }

            Assert.That(guard, Is.LessThanOrEqualTo(4));
            Assert.That(battle.LastAutomaticCardResult.HasValue, Is.True);
            Assert.That(
                battle.LastAutomaticCardResult.Value.OwnerSide,
                Is.EqualTo(ownerSide));
            Assert.That(
                battle.LastAutomaticCardResult.Value.EffectKind,
                Is.EqualTo(effectKind));
            Assert.That(presentedResult.HasValue, Is.True);
            Assert.That(
                presentedResult.Value.Id,
                Is.EqualTo(AutomaticCardResultPromptIdMap.ForEffect(effectKind)));
            Assert.That(
                CoreLoopPresenter.Create(battle).AutomaticCardResult.HasValue,
                Is.True,
                "The prompt result must remain until the next accepted player input.");
        }

        [Test]
        public void CP04_U06_HudRemovesLegacyPanelAndDebuggerIsWired()
        {
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject debugPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    DebugManagerPrefabPath);
            Assert.That(hudPrefab, Is.Not.Null);
            Assert.That(debugPrefab, Is.Not.Null);
            Assert.That(
                hudPrefab.GetComponentsInChildren<CombatPromptView>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                hudPrefab.GetComponentsInChildren<Transform>(true)
                    .Any(transform =>
                        transform.name == "AutomaticCardResult"),
                Is.False);
            Assert.That(
                debugPrefab.GetComponent<AutomaticCardDebugPanel>(),
                Is.Not.Null);

            Scene scene = EditorSceneManager.OpenScene(
                GameScenePath,
                OpenSceneMode.Additive);
            try
            {
                AutomaticCardDebugPanel panel = scene.GetRootGameObjects()
                    .Select(root => root.GetComponentInChildren<
                        AutomaticCardDebugPanel>(true))
                    .First(candidate => candidate != null);
                SerializedObject serialized = new SerializedObject(panel);
                Assert.That(
                    serialized.FindProperty("gameManager").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serialized.FindProperty("shop").objectReferenceValue,
                    Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        [TestCase(EnemyCombatProfileCatalog.CowardlyGamblerKey, "겁쟁이 도박사")]
        [TestCase(EnemyCombatProfileCatalog.GunslingerKey, "총잡이")]
        [TestCase(EnemyCombatProfileCatalog.CultistKey, "광신도")]
        [TestCase(EnemyCombatProfileCatalog.TricksterKey, "사기꾼")]
        [TestCase(EnemyCombatProfileCatalog.EnforcerKey, "집행자")]
        [TestCase(EnemyCombatProfileCatalog.FinalBossKey, "최종 보스")]
        public void CP04_U07_ResultUsesCurrentEnemyProfileDisplayName(
            string profileKey,
            string expectedEnemyName)
        {
            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath);
            string enemyDisplayName = EnemyCombatProfileCatalog.Default
                .GetByKey(profileKey)
                .DisplayName;
            var request = new AutomaticCardResultPromptRequest(
                AutomaticCardResultPromptId.PocketWatch,
                "회중시계",
                CombatantSide.Enemy,
                AutomaticCardSourceDisposition.RetainFaceUp);

            Assert.That(enemyDisplayName, Is.EqualTo(expectedEnemyName));
            Assert.That(
                catalog.TryResolve(
                    request,
                    enemyDisplayName,
                    out string text),
                Is.True);
            Assert.That(
                text,
                Does.Contain(expectedEnemyName + "의 [회중시계]"));
            Assert.That(text, Does.Not.Contain("플레이어"));
            Assert.That(text, Does.Not.Contain("상대"));
        }

        [Test]
        public void CP04_U08_UnprofiledEnemyFallsBackToEnemyLabel()
        {
            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath);
            var request = new AutomaticCardResultPromptRequest(
                AutomaticCardResultPromptId.PocketWatch,
                "회중시계",
                CombatantSide.Enemy,
                AutomaticCardSourceDisposition.Discard);

            Assert.That(
                catalog.TryResolve(
                    request,
                    "UNPROFILED ENEMY",
                    out string text),
                Is.True);
            Assert.That(text, Does.Contain("적의 [회중시계]"));
            Assert.That(text, Does.Not.Contain("UNPROFILED ENEMY"));
            Assert.That(catalog.TryResolve(request, out string fallbackText),
                Is.True);
            Assert.That(fallbackText, Does.Contain("적의 [회중시계]"));
        }

        [Test]
        public void CP04_U09_PocketWatchResultNamesReactivatedCard()
        {
            CoreLoopBattle battle = AutomaticCardDebugPanel.CreateBattle(
                CombatantSide.Player,
                CardEffectKind.PocketWatch);
            Assert.That(battle.Start(), Is.True);
            MarkManualCardUsed(battle, CombatantSide.Player);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction targetChoice =
                battle.PendingPlayerAutomaticInteraction;
            AutomaticCardChoiceOption target = targetChoice.Options.First(
                option => option.CardId.HasValue);
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    targetChoice.InteractionId,
                    target.OptionId),
                Is.True);

            PendingAutomaticCardInteraction dispositionChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    dispositionChoice.InteractionId,
                    dispositionChoice.Options[0].OptionId),
                Is.True);

            AutomaticCardResultPromptRequest request = battle
                .AutomaticCardResultPrompt
                .Value;
            Assert.That(
                request.ReactivatedCardDisplayName,
                Is.EqualTo("위협용 해머"));

            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath);
            AssertResolved(
                catalog,
                request,
                "광신도",
                "내 [회중시계]",
                "선택한 [위협용 해머] 카드를 다시 사용할 수 있게 했습니다");
        }

        [Test]
        public void CP04_U10_ResultSurvivesRoundAndInvalidInputUntilAcceptedInput()
        {
            CoreLoopBattle battle = AutomaticCardDebugPanel.CreateBattle(
                CombatantSide.Enemy,
                CardEffectKind.LieDetector);
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerStand(), Is.True);
            Assert.That(battle.RoundNumber, Is.EqualTo(2));

            AutomaticCardResultPromptRequest result = battle
                .AutomaticCardResultPrompt
                .Value;
            Assert.That(result.Id, Is.EqualTo(
                AutomaticCardResultPromptId.LieDetector));
            Assert.That(battle.TryBeginPlayerCardUse(-1), Is.False);
            Assert.That(battle.AutomaticCardResultPrompt, Is.EqualTo(result));

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.AutomaticCardResultPrompt, Is.Null);
        }

        [Test]
        public void CP04_U11_PocketWatchWithoutTargetAndHiddenEnemyComparisonStayNatural()
        {
            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(CatalogPath);
            var pocketWatch = new AutomaticCardResultPromptRequest(
                AutomaticCardResultPromptId.PocketWatch,
                "Pocket Watch",
                CombatantSide.Player,
                AutomaticCardSourceDisposition.RetainFaceUp);
            AssertResolved(
                catalog,
                pocketWatch,
                "광신도",
                "내 [회중시계] 효과가 발동했지만 다시 사용된 카드는 없습니다");

            var enemyLieDetector = new AutomaticCardResultPromptRequest(
                AutomaticCardResultPromptId.LieDetector,
                "Lie Detector",
                CombatantSide.Enemy,
                AutomaticCardSourceDisposition.RetainFaceUp,
                declaredNumber: 7);
            Assert.That(
                catalog.TryResolve(
                    enemyLieDetector,
                    "광신도",
                    out string lieText),
                Is.True);
            Assert.That(
                lieText,
                Is.EqualTo(
                    "광신도의 [거짓말 탐지기] 효과로 숫자 7을 선언했습니다."));
        }

        private static void AssertResolved(
            CombatPromptCatalogSO catalog,
            AutomaticCardResultPromptRequest request,
            string enemyDisplayName,
            params string[] fragments)
        {
            Assert.That(
                catalog.TryResolve(request, enemyDisplayName, out string text),
                Is.True);
            Assert.That(text, Does.Not.Contain("플레이어"));
            Assert.That(text, Does.Not.Contain("상대"));
            Assert.That(text, Does.Not.Contain("사용 후 버려졌습니다"));
            Assert.That(text, Does.Not.Contain("공개 상태로 남았습니다"));
            Assert.That(text, Does.Not.Contain("확인 결과"));
            if (request.SourceDisplayName.Any(character =>
                    (character >= 'A' && character <= 'Z') ||
                    (character >= 'a' && character <= 'z')))
            {
                Assert.That(
                    text,
                    Does.Not.Contain(request.SourceDisplayName),
                    "Runtime automatic-card names must not leak into prompt copy.");
            }

            foreach (string fragment in fragments)
            {
                Assert.That(text, Does.Contain(fragment));
            }
        }

        private static GameSceneCombatHudViewModel CreateHudModel(
            CombatPromptRequest? selection,
            AutomaticCardResultPromptRequest? result)
        {
            return new GameSceneCombatHudViewModel(
                GameSceneCombatHudMode.Actions,
                selection,
                string.Empty,
                null,
                null,
                null,
                result);
        }

        private static CoreLoopBattle CreateStartedBattle()
        {
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateStandard(seed: 9101),
                BlackjackDeck.CreateStandard(seed: 9102));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void MarkManualCardUsed(
            CoreLoopBattle battle,
            CombatantSide ownerSide)
        {
            BattleParticipant owner = ownerSide == CombatantSide.Player
                ? battle.Player
                : battle.Enemy;
            BlackjackCard manual = owner.Hand.Cards.First(card =>
                card.Definition.Activation == CardActivationKind.Manual);
            Assert.That(manual.TryBeginUse(), Is.True);
            Assert.That(manual.TryCompleteUse(), Is.True);
        }

        private static string ReadText(Component textComponent)
        {
            Assert.That(textComponent, Is.Not.Null);
            return textComponent.GetType()
                .GetProperty("text")
                ?.GetValue(textComponent) as string ?? string.Empty;
        }
    }
}
