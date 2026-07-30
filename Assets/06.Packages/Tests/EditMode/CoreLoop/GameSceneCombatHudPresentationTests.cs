using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneCombatHudPresentationTests
    {
        private const string HudPrefabPath = "Assets/03. Prefabs/UI/HUD.prefab";
        private const string ManagerPrefabPath =
            "Assets/03. Prefabs/Manager/GameManager.prefab";

        [Test]
        public void GSH01_U01_PlayerTurnProjectsFourFixedActionsAndDynamicTooltips()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel model = GameSceneCombatHudPresenter.Create(
                core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false);

            Assert.That(model.Mode, Is.EqualTo(GameSceneCombatHudMode.Actions));
            Assert.That(model.PrimaryActions.Select(action => action.Command.Kind), Is.EqualTo(new[]
            {
                GameSceneCombatHudCommandKind.Hit,
                GameSceneCombatHudCommandKind.Stand,
                GameSceneCombatHudCommandKind.BeginChange,
                GameSceneCombatHudCommandKind.BeginContract
            }));
            Assert.That(model.PrimaryActions.Take(3).All(action => action.IsInteractable),
                Is.True);
            Assert.That(model.PrimaryActions[3].IsInteractable,
                Is.EqualTo(core.DemonContract.CanBegin));
            Assert.That(model.PrimaryActions[2].Tooltip, Does.Contain(core.ChangeActionText));
            Assert.That(model.PrimaryActions[3].Tooltip,
                Does.Contain(core.DemonContract.ActionText));
        }

        [Test]
        public void GSH01_U02_ChangeCandidatesKeepActualCandidateIndicesAndRespectInputLock()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);
            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel unlocked = GameSceneCombatHudPresenter.Create(
                core, false, false, false);
            GameSceneCombatHudViewModel locked = GameSceneCombatHudPresenter.Create(
                core, false, false, true);

            Assert.That(unlocked.Mode, Is.EqualTo(GameSceneCombatHudMode.Options));
            Assert.That(unlocked.OptionActions.Select(action => action.Command.OptionId),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(unlocked.OptionActions.All(action =>
                action.Command.Kind == GameSceneCombatHudCommandKind.SelectChangedCard &&
                action.IsInteractable), Is.True);
            Assert.That(locked.OptionActions.All(action => !action.IsInteractable), Is.True);
        }

        [Test]
        public void GSH01_U03_ContractCandidatesUseWorldCardModeAndKeepInteractionIds()
        {
            CoreLoopBattle battle = CreateStartedContractBattle(
                DemonContractKind.Mammon,
                DemonContractKind.Belphegor);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            CoreLoopViewModel resolving = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel candidates =
                GameSceneCombatHudPresenter.Create(
                    resolving,
                    false,
                    false,
                    false);
            GameSceneCombatHudViewModel locked =
                GameSceneCombatHudPresenter.Create(
                    resolving,
                    false,
                    false,
                    true);

            Assert.That(
                candidates.Mode,
                Is.EqualTo(GameSceneCombatHudMode.ContractCandidates));
            Assert.That(
                candidates.ContractCandidates.Select(
                    candidate => candidate.Command.OptionId),
                Is.EqualTo(resolving.DemonContract.Choices.Select(choice => choice.OptionId)));
            Assert.That(candidates.ContractCandidates.All(candidate =>
                candidate.Command.InteractionId ==
                    resolving.DemonContract.InteractionId), Is.True);
            Assert.That(candidates.ContractCandidates.All(candidate =>
                !string.IsNullOrEmpty(candidate.DefinitionKey) &&
                !string.IsNullOrEmpty(candidate.Ability) &&
                !string.IsNullOrEmpty(candidate.Cost)), Is.True);
            Assert.That(locked.ContractCandidates.All(
                candidate => !candidate.IsInteractable), Is.True);
        }

        [Test]
        public void GSH01_U04_AutomaticChoiceAndResultAreProjectedToHud()
        {
            CoreLoopSession session = new CoreLoopSession(() => new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0,
                    2,
                    3,
                    CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey),
                    4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 4, 5, 2)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy()));
            Assert.That(session.TryPlayerHit(), Is.True);

            CoreLoopViewModel pending = CoreLoopPresenter.Create(session.Battle);
            GameSceneCombatHudViewModel choices = GameSceneCombatHudPresenter.Create(
                pending, false, false, false);
            AutomaticCardInteractionViewModel interaction = pending.AutomaticCardInteraction;

            Assert.That(choices.Mode, Is.EqualTo(GameSceneCombatHudMode.Options));
            Assert.That(choices.OptionActions.All(action =>
                action.Command.Kind == GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice &&
                action.Command.InteractionId == interaction.InteractionId), Is.True);

            Assert.That(session.TryResolvePlayerAutomaticCardChoice(
                interaction.InteractionId,
                PoisonEffectHandler.PaySoulOptionId), Is.True);
            GameSceneCombatHudViewModel result = GameSceneCombatHudPresenter.Create(
                CoreLoopPresenter.Create(session.Battle), false, false, false);
            Assert.That(result.AutomaticCardResult, Does.StartWith("AUTOMATIC CARD"));
        }

        [Test]
        public void GSH01_U05_BattleEndDifferentiatesStageReturnAndStandaloneRestart()
        {
            CoreLoopSession session = new CoreLoopSession(() => new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 10, 1)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 10)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1,
                enemyPolicy: new StandPolicy()));
            Assert.That(session.TryPlayerStand(), Is.True);
            CoreLoopViewModel ended = CoreLoopPresenter.Create(session.Battle);

            GameSceneCombatHudViewModel standalone = GameSceneCombatHudPresenter.Create(
                ended, false, false, false);
            GameSceneCombatHudViewModel stage = GameSceneCombatHudPresenter.Create(
                ended, true, false, false);

            Assert.That(standalone.Mode, Is.EqualTo(GameSceneCombatHudMode.Restart));
            Assert.That(standalone.OptionActions.Single().Command.Kind,
                Is.EqualTo(GameSceneCombatHudCommandKind.Restart));
            Assert.That(stage.Mode, Is.EqualTo(GameSceneCombatHudMode.ReturningToRun));
            Assert.That(stage.OptionActions, Is.Empty);
        }

        [Test]
        public void CUM09_U01_HammerProjectsOnlyLegalEnemyCardsAsDiegeticTargets()
        {
            CoreLoopBattle battle = CreateStartedHammerBattle();
            BlackjackCard revealedHiddenCard = battle.Enemy.Hand.Cards[1];
            revealedHiddenCard.Reveal();
            battle.Enemy.Draw(faceUp: true);
            BlackjackCard hammer = battle.Player.Draw(faceUp: true);
            Assert.That(battle.TryBeginPlayerCardUse(hammer.Id), Is.True);

            GameSceneViewModel model = GameScenePresenter.Create(battle);
            PendingCardEffect pending = battle.PendingPlayerCardEffect;

            Assert.That(model.UsesDiegeticCardEffectSelection, Is.True);
            Assert.That(
                model.EnemyCards
                    .Where(card => card.CardEffectChoiceOptionId.HasValue)
                    .Select(card => card.CardId),
                Is.EqualTo(pending.Options.Select(option => option.CardId.Value)));
            foreach (CardEffectChoiceOption option in pending.Options)
            {
                GameSceneCardViewModel target = model.EnemyCards.Single(
                    card => card.CardId == option.CardId.Value);
                Assert.That(target.CardEffectChoiceOptionId, Is.EqualTo(option.Id));
            }

            GameSceneCardViewModel hiddenRoleCard = model.EnemyCards.Single(
                card => card.CardId == revealedHiddenCard.Id);
            Assert.That(hiddenRoleCard.RevealRank, Is.True);
            Assert.That(hiddenRoleCard.CardEffectChoiceOptionId, Is.Null);
        }

        [Test]
        public void CUM09_U02_HammerKeepsPromptButRemovesHudChoiceButtons()
        {
            CoreLoopBattle battle = CreateStartedHammerBattle();
            BlackjackCard hammer = battle.Player.Draw(faceUp: true);
            Assert.That(battle.TryBeginPlayerCardUse(hammer.Id), Is.True);
            GameSceneViewModel scene = GameScenePresenter.Create(battle);

            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(hud.Mode, Is.EqualTo(GameSceneCombatHudMode.Options));
            Assert.That(hud.Prompt, Is.EqualTo(scene.Core.CardEffectPrompt));
            Assert.That(hud.OptionActions, Is.Empty);
        }

        [Test]
        public void CUM09_U03_CardViewRetainsProjectedCardEffectChoiceOption()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/Card.prefab");
            Assert.That(cardPrefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(cardPrefab);

            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);
                view.Bind(new GameSceneCardViewModel(
                    cardId: 21,
                    rank: 10,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Target",
                    cardEffectChoiceOptionId: 42));

                Assert.That(view.CanUse, Is.False);
                Assert.That(view.CardEffectChoiceOptionId, Is.EqualTo(42));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CUM09_U04_NonHammerManualChoicesRemainHudButtons()
        {
            CardDefinition revolver = CardDefinitionCatalog.GetDefaultForRank(7);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 2, revolver, 3)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            BlackjackCard source = battle.Player.Hand.Cards.Single(
                card => card.Definition.Effect == CardEffectKind.AutoPistol);
            Assert.That(battle.TryBeginPlayerCardUse(source.Id), Is.True);
            GameSceneViewModel scene = GameScenePresenter.Create(battle);

            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(scene.UsesDiegeticCardEffectSelection, Is.False);
            Assert.That(hud.OptionActions, Has.Count.EqualTo(10));
            Assert.That(hud.OptionActions.All(action =>
                action.Command.Kind ==
                    GameSceneCombatHudCommandKind.ResolveCardEffectChoice), Is.True);
        }

        [Test]
        public void GSH01_U06_HudPrefabAuthorsFixedButtonsSlotsTooltipAndCandidateCatalog()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameHudView hud = prefab.GetComponent<GameHudView>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.CombatOptionSlotCount, Is.EqualTo(100));
            Assert.That(hud.CombatContractCandidateSlotCount, Is.EqualTo(2));
            Assert.That(hud.HasCombatTooltipReference, Is.True);
            Assert.That(hud.HasCombatCandidateContentReference, Is.True);
            Assert.That(prefab.GetComponentsInChildren<GameHudChoiceButton>(true).Length,
                Is.EqualTo(102));
            Assert.That(prefab.GetComponentInChildren<ScrollRect>(true), Is.Not.Null);

            GameObject managerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
            Assert.That(managerPrefab, Is.Not.Null);
            DemonContractSelectionView selection =
                managerPrefab.GetComponent<DemonContractSelectionView>();
            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.HasCandidatePrefab, Is.True);
            Assert.That(selection.Capacity, Is.EqualTo(2));

            Transform actionRow = prefab.transform.Find("CombatControls/ActionRow");
            Assert.That(GetBrushName(actionRow, "Hit"), Is.EqualTo("Brush_UI_1"));
            Assert.That(GetBrushName(actionRow, "Stand"), Is.EqualTo("Brush_UI_1"));
            Assert.That(GetBrushName(actionRow, "Change"), Is.EqualTo("Brush_UI_1"));
            Assert.That(GetBrushName(actionRow, "Contract"), Is.EqualTo("Brush_UI_1"));
        }

        private static CoreLoopBattle CreateStartedBattle(params int[] playerRanks)
        {
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateRankCards(0, playerRanks)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateStartedHammerBattle()
        {
            CardDefinition hammer = CardDefinitionCatalog.GetDefaultForRank(6);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 2, 3, hammer, 4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5, 4)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateStartedContractBattle(
            params DemonContractKind[] kinds)
        {
            var cards = new List<DemonContractCard>();
            for (int i = 0; i < kinds.Length; i++)
            {
                cards.Add(new DemonContractCard(
                    i,
                    DemonContractCatalog.Default.GetByKey(GetDefinitionKey(kinds[i]))));
            }

            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 5, 2, 3, 4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy(),
                playerDemonDeck: new DemonContractDeck(cards, seed: 73));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(
            int firstId,
            params object[] values)
        {
            var cards = new List<BlackjackCard>();
            for (int i = 0; i < values.Length; i++)
            {
                CardDefinition definition = values[i] as CardDefinition ??
                    CardDefinitionCatalog.GetDefaultForRank((int)values[i]);
                cards.Add(new BlackjackCard(firstId + i, definition));
            }

            return cards;
        }

        private static IReadOnlyList<BlackjackCard> CreateRankCards(
            int firstId,
            IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>();
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(firstId + i, ranks[i]));
            }

            return cards;
        }

        private static string GetBrushName(Transform actionRow, string actionName)
        {
            Image image = actionRow.Find(actionName).GetComponent<Image>();
            return image.sprite == null ? string.Empty : image.sprite.name;
        }

        private static string GetDefinitionKey(DemonContractKind kind)
        {
            switch (kind)
            {
                case DemonContractKind.Belphegor:
                    return DemonContractCatalog.BelphegorKey;
                case DemonContractKind.Mammon:
                    return DemonContractCatalog.MammonKey;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "gsh01-stand");
            }
        }
    }
}
