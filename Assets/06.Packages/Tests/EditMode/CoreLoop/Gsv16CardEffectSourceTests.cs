using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("GSV16")]
    public sealed class Gsv16CardEffectSourceTests
    {
        private static readonly int PixelOutlineColorId =
            Shader.PropertyToID("_PixelOutlineColor");
        private static readonly int PixelOutlineVisibilityId =
            Shader.PropertyToID("_PixelOutlineVisibility");
        private const string CardPrefabPath =
            "Assets/03. Prefabs/Card/Card.prefab";
        private const string DemonCardPrefabPath =
            "Assets/03. Prefabs/Card/DemonCard.prefab";

        [Test]
        public void GSV16_U01_ManualCardSourceProjectsExactPlayerAndEnemyIds()
        {
            CoreLoopBattle playerBattle = CreatePlayerHammerBattle();
            BlackjackCard playerSource = playerBattle.Player.Draw(faceUp: true);
            Assert.That(
                playerBattle.TryBeginPlayerCardUse(playerSource.Id),
                Is.True);

            GameSceneViewModel playerModel =
                GameScenePresenter.Create(playerBattle);
            AssertSingleNormalSource(
                playerModel,
                CombatantSide.Player,
                playerSource.Id,
                isPersistent: true);

            CardDefinition enemyHammer =
                CardDefinitionCatalog.GetDefaultForRank(6);
            var enemyBattle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 10, 10, 2)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100,
                    enemyHammer,
                    2,
                    3)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(enemyBattle.Start(), Is.True);
            BlackjackCard enemySource = enemyBattle.Enemy.Hand.Cards
                .Single(card => card.Definition.Effect ==
                    CardEffectKind.ThreatHammer);
            BlackjackCard playerTarget = enemyBattle.Player.Hand.Cards
                .First(card => card.IsFaceUp);
            SetPrivateField(
                enemyBattle,
                "_activeCardEffectActorSide",
                (CombatantSide?)CombatantSide.Enemy);
            SetPrivateField(
                enemyBattle,
                "_pendingCardEffect",
                new PendingCardEffect(
                    enemySource.Id,
                    CardEffectKind.ThreatHammer,
                    "enemy hammer",
                    CardEffectChoiceKind.DiscardOpponentFaceUpCard,
                    new[]
                    {
                        new CardEffectChoiceOption(
                            0,
                            "target",
                            playerTarget.Id)
                    }));
            GameSceneViewModel enemyPendingModel =
                GameScenePresenter.Create(enemyBattle);
            AssertSingleNormalSource(
                enemyPendingModel,
                CombatantSide.Enemy,
                enemySource.Id,
                isPersistent: true);
        }

        [Test]
        public void GSV16_U02_AutomaticCardSourceProjectsBothOwners()
        {
            CardDefinition poison = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.PoisonKey);
            var playerBattle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0,
                    2,
                    3,
                    poison,
                    4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(playerBattle.Start(), Is.True);
            Assert.That(playerBattle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction playerPending =
                playerBattle.PendingAutomaticInteraction;
            Assert.That(playerPending, Is.Not.Null);
            AssertSingleNormalSource(
                GameScenePresenter.Create(playerBattle),
                CombatantSide.Player,
                playerPending.SourceCardId,
                isPersistent: true);

            CardDefinition lieDetector = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.LieDetectorKey);
            var enemyBattle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 10, 10, 2)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100,
                    4,
                    5,
                    lieDetector,
                    3)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new HitThenStandPolicy());
            GameSceneViewModel enemyPendingModel = null;
            int enemySourceId = -1;
            enemyBattle.Stepped += () =>
            {
                PendingAutomaticCardInteraction pending =
                    enemyBattle.PendingAutomaticInteraction;
                if (pending?.OwnerSide == CombatantSide.Enemy)
                {
                    enemySourceId = pending.SourceCardId;
                    enemyPendingModel = GameScenePresenter.Create(enemyBattle);
                }
            };

            Assert.That(enemyBattle.Start(), Is.True);
            Assert.That(enemyBattle.TryPlayerStand(), Is.True);
            Assert.That(enemyPendingModel, Is.Not.Null);
            AssertSingleNormalSource(
                enemyPendingModel,
                CombatantSide.Enemy,
                enemySourceId,
                isPersistent: true);
        }

        [Test]
        public void GSV16_U03_DemonSourceProjectsBothOwnersAndNestedAutomaticWins()
        {
            CoreLoopBattle playerBattle = CreatePlayerContractBattle();
            GameSceneViewModel playerModel = null;
            playerBattle.Stepped += () =>
            {
                if (playerBattle.LastPublicAction?.ActionType !=
                        PublicCombatActionType.DemonContract ||
                    playerBattle.ActivePlayerDemonContracts.Count == 0)
                {
                    return;
                }

                GameSceneViewModel candidate =
                    GameScenePresenter.Create(playerBattle);
                if (candidate.PlayerDemonCards.Any(card => card.IsEffectSource))
                {
                    playerModel = candidate;
                }
            };
            ActivatePlayerBelphegor(playerBattle);
            ActiveDemonContract playerContract =
                playerBattle.ActivePlayerDemonContracts.Single();
            Assert.That(playerModel, Is.Not.Null);
            AssertSingleDemonSource(
                playerModel,
                CombatantSide.Player,
                playerContract.SourceCardId);

            SetPrivateField(
                playerBattle,
                "_pendingPlayerDemonContractInteraction",
                new PendingDemonContractInteraction(
                    interactionId: 990,
                    kind: DemonContractInteractionKind.BelphegorTopCard,
                    contractKind: DemonContractKind.Belphegor,
                    options: new[]
                    {
                        new DemonContractOption(0, null, null, "keep"),
                        new DemonContractOption(1, null, null, "move")
                    },
                    publicPrompt: "contract",
                    sourceContractCardId: playerContract.SourceCardId));
            GameSceneViewModel contractPendingModel =
                GameScenePresenter.Create(playerBattle);
            AssertSingleDemonSource(
                contractPendingModel,
                CombatantSide.Player,
                playerContract.SourceCardId);

            BlackjackCard nestedSource = playerBattle.Player.Hand.Cards
                .First(card => card.IsFaceUp);
            SetPrivateField(
                playerBattle,
                "_pendingAutomaticCardInteraction",
                new PendingAutomaticCardInteraction(
                    interactionId: 991,
                    sourceCardId: nestedSource.Id,
                    effectKind: CardEffectKind.Poison,
                    ownerSide: CombatantSide.Player,
                    decisionSide: CombatantSide.Player,
                    choiceKind: AutomaticCardChoiceKind.PoisonDecision,
                    prompt: "nested",
                    options: new[]
                    {
                        new AutomaticCardChoiceOption(0, "continue")
                    }));

            GameSceneViewModel nestedModel =
                GameScenePresenter.Create(playerBattle);
            AssertSingleNormalSource(
                nestedModel,
                CombatantSide.Player,
                nestedSource.Id,
                isPersistent: true);
            Assert.That(
                nestedModel.PlayerDemonCards.All(card => !card.IsEffectSource),
                Is.True);

            CoreLoopBattle enemyBattle = CreateEnemyContractBattle();
            GameSceneViewModel enemyContractFrame = null;
            int enemyContractId = -1;
            enemyBattle.Stepped += () =>
            {
                if (enemyBattle.LastPublicAction?.ActionType !=
                        PublicCombatActionType.DemonContract ||
                    enemyBattle.ActiveEnemyDemonContracts.Count == 0)
                {
                    return;
                }

                enemyContractId = enemyBattle.ActiveEnemyDemonContracts[0]
                    .SourceCardId;
                GameSceneViewModel candidate =
                    GameScenePresenter.Create(enemyBattle);
                if (candidate.EnemyDemonCards.Any(card => card.IsEffectSource))
                {
                    enemyContractFrame = candidate;
                }
            };

            Assert.That(enemyBattle.Start(), Is.True);
            Assert.That(enemyBattle.TryPlayerHit(), Is.True);
            Assert.That(enemyContractFrame, Is.Not.Null);
            AssertSingleDemonSource(
                enemyContractFrame,
                CombatantSide.Enemy,
                enemyContractId);
        }

        [Test]
        public void GSV16_U04_NormalSourceKeepsScaleAndYellowOutlineWithoutTooltip()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                CardView view = instance.GetComponent<CardView>();
                view.EnableHandHoverVisualOnly();
                SpriteRenderer front = GetSerializedRenderer(view, "front");
                Color expected = front.sharedMaterial.GetColor(
                    PixelOutlineColorId);
                Vector3 restingScale = view.HoverVisualTransform.localScale;
                var model = new GameSceneCardViewModel(
                    71,
                    7,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: true,
                    displayName: "Revolver",
                    definitionKey:
                        CardDefinitionCatalog.GetDefaultForRank(7).Key,
                    isEffectSource: true,
                    isEffectSourcePersistent: true);

                view.Bind(model, showTransientEffectSource: false);
                Vector3 emphasizedScale = GetScaleTweenEndValue(view);
                var block = new MaterialPropertyBlock();
                front.GetPropertyBlock(block);

                Assert.That(
                    emphasizedScale.x,
                    Is.GreaterThan(restingScale.x));
                Assert.That(
                    block.GetFloat(PixelOutlineVisibilityId),
                    Is.EqualTo(1f));
                AssertColor(block.GetColor(PixelOutlineColorId), expected);
                Assert.That(view.ShouldShowHoverBadge, Is.False);
                Assert.That(GetPrivateField<bool>(view, "_hovered"), Is.False);

                var targetOnly = new GameSceneCardViewModel(
                    72,
                    6,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Target",
                    directSelectionCommand: new GameSceneCombatHudCommand(
                        GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                        optionId: 1,
                        interactionId: 1));
                view.Bind(targetOnly, showTransientEffectSource: false);
                Assert.That(GetPrivateField<bool>(view, "_isEffectSource"),
                    Is.False);
                Assert.That(GetPrivateField<object>(view, "_scaleTween"),
                    Is.Null);

                var pointerModel = new GameSceneCardViewModel(
                    73,
                    7,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: true,
                    displayName: "Pointer source",
                    definitionKey:
                        CardDefinitionCatalog.GetDefaultForRank(7).Key);
                view.Bind(pointerModel, showTransientEffectSource: false);
                view.SetHovered(true);
                Vector3 pointerScale = GetScaleTweenEndValue(view);
                view.HoverVisualTransform.localScale = pointerScale;
                view.SetHovered(false);
                var activatedSameCard = new GameSceneCardViewModel(
                    73,
                    7,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Pointer source",
                    definitionKey:
                        CardDefinitionCatalog.GetDefaultForRank(7).Key,
                    isEffectSource: true,
                    isEffectSourcePersistent: true);
                view.Bind(
                    activatedSameCard,
                    showTransientEffectSource: false);
                Assert.That(
                    view.HoverVisualTransform.localScale,
                    Is.EqualTo(pointerScale));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV16_U05_DemonSourceKeepsExistingScaleOnly()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                DemonCardView view = instance.GetComponent<DemonCardView>();
                view.EnableHandHoverVisualOnly();
                SpriteRenderer front = GetSerializedRenderer(view, "front");
                var before = new MaterialPropertyBlock();
                front.GetPropertyBlock(before);
                Vector3 restingScale = view.HoverVisualTransform.localScale;
                DemonContractDefinition definition = DemonContractCatalog.Default
                    .GetByKey(DemonContractCatalog.BelphegorKey);
                var model = new GameSceneDemonCardViewModel(
                    81,
                    definition.Key,
                    isFaceUp: true,
                    canUse: false,
                    definition.DisplayName,
                    definition.Summary,
                    definition.CostSummary,
                    showHoverBadgeWhenUnavailable: true,
                    isEffectSource: true,
                    isEffectSourcePersistent: true);

                view.Bind(model, showTransientEffectSource: false);
                Vector3 targetScale =
                    GetPrivateField<Vector3>(view, "_targetScale");
                var after = new MaterialPropertyBlock();
                front.GetPropertyBlock(after);

                Assert.That(targetScale.x, Is.GreaterThan(restingScale.x));
                Assert.That(
                    after.GetFloat(PixelOutlineVisibilityId),
                    Is.EqualTo(before.GetFloat(PixelOutlineVisibilityId)));
                Assert.That(view.ShouldShowHoverBadge, Is.False);
                Assert.That(GetPrivateField<bool>(view, "_hovered"), Is.False);

                var pointerModel = new GameSceneDemonCardViewModel(
                    82,
                    definition.Key,
                    isFaceUp: true,
                    canUse: true,
                    definition.DisplayName,
                    definition.Summary,
                    definition.CostSummary,
                    showHoverBadgeWhenUnavailable: true);
                view.Bind(pointerModel, showTransientEffectSource: false);
                view.SetHovered(true);
                Vector3 pointerScale =
                    GetPrivateField<Vector3>(view, "_targetScale");
                view.HoverVisualTransform.localScale = pointerScale;
                view.SetHovered(false);
                var activatedSameCard = new GameSceneDemonCardViewModel(
                    82,
                    definition.Key,
                    isFaceUp: true,
                    canUse: false,
                    definition.DisplayName,
                    definition.Summary,
                    definition.CostSummary,
                    showHoverBadgeWhenUnavailable: true,
                    isEffectSource: true,
                    isEffectSourcePersistent: true);
                view.Bind(
                    activatedSameCard,
                    showTransientEffectSource: false);
                Assert.That(
                    view.HoverVisualTransform.localScale,
                    Is.EqualTo(pointerScale));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSV16_U06_PendingPersistsButFinalRefreshSuppressesTransientSource()
        {
            CoreLoopBattle battle = CreatePlayerHammerBattle();
            BlackjackCard source = battle.Player.Draw(faceUp: true);
            Assert.That(battle.TryBeginPlayerCardUse(source.Id), Is.True);
            GameSceneCardViewModel pending = GameScenePresenter.Create(battle)
                .PlayerCards.Single(card => card.CardId == source.Id);
            Assert.That(pending.IsEffectSource, Is.True);
            Assert.That(pending.IsEffectSourcePersistent, Is.True);

            PendingCardEffect interaction = battle.PendingPlayerCardEffect;
            GameSceneCardViewModel completed = null;
            battle.Stepped += () =>
            {
                if (battle.PendingPlayerCardEffect != null ||
                    battle.LastPublicAction?.ActionType !=
                        PublicCombatActionType.UseCard)
                {
                    return;
                }

                GameSceneCardViewModel candidate = GameScenePresenter
                    .Create(battle)
                    .PlayerCards
                    .FirstOrDefault(card => card.CardId == source.Id);
                if (candidate?.IsEffectSource == true)
                {
                    completed = candidate;
                }
            };
            Assert.That(
                battle.TryResolvePlayerCardChoice(interaction.Options[0].Id),
                Is.True);
            Assert.That(completed, Is.Not.Null);
            Assert.That(completed.IsEffectSource, Is.True);
            Assert.That(completed.IsEffectSourcePersistent, Is.False);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                CardView view = instance.GetComponent<CardView>();
                view.Bind(pending, showTransientEffectSource: false);
                Assert.That(GetPrivateField<bool>(view, "_isEffectSource"),
                    Is.True);

                view.Bind(completed, showTransientEffectSource: false);
                Assert.That(GetPrivateField<bool>(view, "_isEffectSource"),
                    Is.False);

                view.Bind(completed, showTransientEffectSource: true);
                Assert.That(GetPrivateField<bool>(view, "_isEffectSource"),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            if (battle.State == CoreLoopState.PlayerTurn)
            {
                Assert.That(battle.TryPlayerStand(), Is.True);
            }

            GameSceneViewModel afterRound = GameScenePresenter.Create(battle);
            Assert.That(
                afterRound.PlayerCards.Concat(afterRound.EnemyCards)
                    .All(card => !card.IsEffectSource),
                Is.True);

            var session = new CoreLoopSession(() => new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 10, 10, 2)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 1, 1, 2)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1,
                enemyPolicy: new StandPolicy()));
            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(session.TryRestart(), Is.True);
            GameSceneViewModel restarted =
                GameScenePresenter.Create(session.Battle);
            Assert.That(
                restarted.PlayerCards.Concat(restarted.EnemyCards)
                    .All(card => !card.IsEffectSource),
                Is.True);
        }

        private static CoreLoopBattle CreatePlayerHammerBattle()
        {
            CoreLoopBattle battle = CreateUnstartedPlayerHammerBattle();
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateUnstartedPlayerHammerBattle()
        {
            CardDefinition hammer =
                CardDefinitionCatalog.GetDefaultForRank(6);
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0,
                    2,
                    3,
                    hammer,
                    4,
                    5)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100,
                    10,
                    7,
                    5,
                    4,
                    3)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
        }

        private static CoreLoopBattle CreatePlayerContractBattle()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.BelphegorKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 5, 2, 3, 4, 5)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100, 10, 7, 5, 4)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy(),
                playerDemonDeck: new DemonContractDeck(
                    new[] { new DemonContractCard(700, definition) },
                    seed: 73));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void ActivatePlayerBelphegor(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction offer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption option = offer.Options.Single();
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    offer.InteractionId,
                    option.OptionId),
                Is.True);
        }

        private static CoreLoopBattle CreateEnemyContractBattle()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.BelphegorKey);
            var contracts = new List<DemonContractCard>();
            for (int i = 0; i < 4; i++)
            {
                contracts.Add(new DemonContractCard(1000 + i, definition));
            }

            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 2, 2, 2, 2, 2, 2, 2)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100, 10, 2, 2, 2, 2, 2, 2)),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy: new CultistEnemyPolicy(),
                cardEffectResolver: CardEffectResolver.CreateDefault(),
                playerDemonDeck: new DemonContractDeck(
                    Array.Empty<DemonContractCard>(),
                    seed: 0),
                demonContractResolver: DemonContractResolver.CreateDefault(),
                enemyDemonDeck: new DemonContractDeck(contracts, seed: 17));
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

        private static void AssertSingleNormalSource(
            GameSceneViewModel model,
            CombatantSide side,
            int expectedCardId,
            bool isPersistent)
        {
            IEnumerable<GameSceneCardViewModel> owned =
                side == CombatantSide.Player
                    ? model.PlayerCards
                    : model.EnemyCards;
            IEnumerable<GameSceneCardViewModel> other =
                side == CombatantSide.Player
                    ? model.EnemyCards
                    : model.PlayerCards;
            GameSceneCardViewModel source =
                owned.Single(card => card.IsEffectSource);
            Assert.That(source.CardId, Is.EqualTo(expectedCardId));
            Assert.That(source.IsEffectSourcePersistent, Is.EqualTo(isPersistent));
            Assert.That(other.All(card => !card.IsEffectSource), Is.True);
            Assert.That(
                model.PlayerDemonCards.Concat(model.EnemyDemonCards)
                    .All(card => !card.IsEffectSource),
                Is.True);
        }

        private static void AssertSingleDemonSource(
            GameSceneViewModel model,
            CombatantSide side,
            int expectedCardId)
        {
            IEnumerable<GameSceneDemonCardViewModel> owned =
                side == CombatantSide.Player
                    ? model.PlayerDemonCards
                    : model.EnemyDemonCards;
            IEnumerable<GameSceneDemonCardViewModel> other =
                side == CombatantSide.Player
                    ? model.EnemyDemonCards
                    : model.PlayerDemonCards;
            GameSceneDemonCardViewModel source =
                owned.Single(card => card.IsEffectSource);
            Assert.That(source.CardId, Is.EqualTo(expectedCardId));
            Assert.That(other.All(card => !card.IsEffectSource), Is.True);
            Assert.That(
                model.PlayerCards.Concat(model.EnemyCards)
                    .All(card => !card.IsEffectSource),
                Is.True);
        }

        private static SpriteRenderer GetSerializedRenderer(
            UnityEngine.Object target,
            string propertyName)
        {
            SerializedProperty property = new SerializedObject(target)
                .FindProperty(propertyName);
            GameObject value = property.objectReferenceValue as GameObject;
            Assert.That(value, Is.Not.Null);
            return value.GetComponent<SpriteRenderer>();
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static Vector3 GetScaleTweenEndValue(CardView view)
        {
            object tween = GetPrivateField<object>(view, "_scaleTween");
            Assert.That(tween, Is.Not.Null);
            FieldInfo endValue = tween.GetType().GetField(
                "endValue",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(endValue, Is.Not.Null);
            return (Vector3)endValue.GetValue(tween);
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return SelectCandidate(
                    observation,
                    candidate => candidate.ActionType == EnemyActionType.Stand,
                    "gsv16-stand");
            }
        }

        private sealed class HitThenStandPolicy : IEnemyBehaviorPolicy
        {
            private bool _hasHit;

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionType action = _hasHit
                    ? EnemyActionType.Stand
                    : EnemyActionType.Hit;
                _hasHit = true;
                return SelectCandidate(
                    observation,
                    candidate => candidate.ActionType == action,
                    "gsv16-hit-then-stand");
            }
        }

        private static EnemyDecision SelectCandidate(
            EnemyObservation observation,
            Func<EnemyActionCandidate, bool> predicate,
            string reason)
        {
            EnemyActionCandidate candidate =
                observation.ActionCandidates.First(predicate);
            return EnemyDecision.FromCandidate(candidate, reason);
        }
    }
}
