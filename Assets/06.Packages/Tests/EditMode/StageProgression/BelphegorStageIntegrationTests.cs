using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class BelphegorStageIntegrationTests
    {
        [Test]
        public void DC03_I01_StageBattleActivatesBelphegorAndForwardsItsHitChoice()
        {
            var normalDeck = Enumerable.Range(0, 12)
                .Select(id => new RunCardDefinition(id, (id % 6) + 1))
                .ToArray();
            var demonDeck = Enumerable.Range(0, 4)
                .Select(id => new RunDemonDefinition(
                    id,
                    DemonContractCatalog.BelphegorKey))
                .ToArray();
            var player = new PlayerRunState(12, 12, normalDeck, demonDeck);
            var stage = new StageDefinition(
                "dc03-stage",
                "벨페고르 통합 테스트",
                StageKind.NormalCombat,
                enemyMaximumSoul: 12,
                playerDeckSeed: 41,
                enemyDeckSeed: 31);
            CoreLoopBattle battle = StageBattleFactory.Create(stage, player);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction contractChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    contractChoice.InteractionId,
                    contractChoice.Options[0].OptionId),
                Is.True);
            Assert.That(battle.ActivePlayerDemonContracts[0].RuntimeState,
                Is.TypeOf<BelphegorRuntimeState>());

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction previewChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(previewChoice.Kind,
                Is.EqualTo(DemonContractInteractionKind.BelphegorTopCard));
            Assert.That(battle.PlayerDemonContractPreview, Is.Not.Null);
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    previewChoice.InteractionId,
                    BelphegorDemonContractHandler.KeepTopCardOptionId),
                Is.True);
            Assert.That(battle.PlayerDemonContractPreview, Is.Null);
        }
    }
}
