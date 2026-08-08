using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public enum FormalRunPhase
    {
        NotStarted,
        Combat,
        Shop,
        RunVictory,
        RunDefeat
    }

    public sealed class FormalRunSession
    {
        private ShopOfferGenerator _shopOfferGenerator;
        private int _goldBeforeCurrentBattle;

        public FormalRunSession(
            StageProgressionSession combatSession,
            ShopOfferGenerator shopOfferGenerator)
            : this(combatSession, shopOfferGenerator, 0, 0)
        {
        }

        internal FormalRunSession(
            StageProgressionSession combatSession,
            ShopOfferGenerator shopOfferGenerator,
            int completedShopCount,
            int utilityPriceLevel)
        {
            CombatSession = combatSession ?? throw new ArgumentNullException(
                nameof(combatSession));
            if (combatSession.UsesBattleRewards)
            {
                throw new ArgumentException(
                    "A formal run combat session must bypass battle rewards.",
                    nameof(combatSession));
            }

            _shopOfferGenerator = shopOfferGenerator ?? throw new ArgumentNullException(
                nameof(shopOfferGenerator));
            if (completedShopCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completedShopCount));
            }

            if (utilityPriceLevel < 0 || utilityPriceLevel > completedShopCount)
            {
                throw new ArgumentOutOfRangeException(nameof(utilityPriceLevel));
            }

            CombatSession.BattleResultSynchronized += HandleBattleResultSynchronized;
            CompletedShopCount = completedShopCount;
            UtilityPriceLevel = utilityPriceLevel;
            Phase = FormalRunPhase.NotStarted;
        }

        public ShopVisit ActiveShop { get; private set; }

        public StageProgressionSession CombatSession { get; }

        public int CompletedShopCount { get; private set; }

        public int LastGoldReward { get; private set; }

        public FormalRunPhase Phase { get; private set; }

        public int UtilityPriceLevel { get; private set; }

        internal event Action CombatSettlementCompleted;

        internal event Action RunEnded;

        internal event Action ShopExited;

        public bool TryStartRun()
        {
            if (Phase != FormalRunPhase.NotStarted || !CombatSession.TryStartRun())
            {
                return false;
            }

            _goldBeforeCurrentBattle = CombatSession.Progress.Player.CurrentGold;
            if (CombatSession.Progress.State != StageProgressionState.NotStarted)
            {
                Phase = FormalRunPhase.Combat;
            }

            return true;
        }

        public bool TrySelectOpponent(int offerId, string profileKey)
        {
            if (Phase != FormalRunPhase.Combat ||
                !CombatSession.TrySelectOpponent(offerId, profileKey))
            {
                return false;
            }

            _goldBeforeCurrentBattle = CombatSession.Progress.Player.CurrentGold;
            return true;
        }

        public bool TrySelectBattleReward(int optionId)
        {
            return false;
        }

        public bool TrySkipBattleReward()
        {
            return false;
        }

        public bool TryBuyShopCard(int offerId, int optionId)
        {
            return Phase == FormalRunPhase.Shop &&
                ActiveShop != null &&
                ActiveShop.TryBuyCard(
                    offerId,
                    optionId,
                    CombatSession.Progress.Player);
        }

        public bool TryRemoveShopCard(int offerId, int cardId)
        {
            return Phase == FormalRunPhase.Shop &&
                ActiveShop != null &&
                ActiveShop.TryRemoveCard(
                    offerId,
                    cardId,
                    CombatSession.Progress.Player);
        }

        public bool TryRestAtShop(int offerId)
        {
            return Phase == FormalRunPhase.Shop &&
                ActiveShop != null &&
                ActiveShop.TryRest(offerId, CombatSession.Progress.Player);
        }

        public bool TryLeaveShop(int offerId)
        {
            ShopVisit shop = ActiveShop;
            if (Phase != FormalRunPhase.Shop ||
                shop == null ||
                !shop.CanClose(offerId))
            {
                return false;
            }

            if (!shop.TryClose(offerId))
            {
                throw new InvalidOperationException(
                    "A validated shop visit could not be closed.");
            }

            if (shop.HasRemovedCard)
            {
                UtilityPriceLevel++;
            }

            CompletedShopCount++;
            ShopExited?.Invoke();
            if (!CombatSession.TryAdvanceToNextStage())
            {
                throw new InvalidOperationException(
                    "A saved shop exit could not advance to the next stage.");
            }

            ActiveShop = null;
            LastGoldReward = 0;
            _goldBeforeCurrentBattle = CombatSession.Progress.Player.CurrentGold;
            Phase = FormalRunPhase.Combat;
            return true;
        }

        public bool TryRestartRun()
        {
            if ((Phase != FormalRunPhase.RunVictory &&
                 Phase != FormalRunPhase.RunDefeat) ||
                !CombatSession.TryRestartRun())
            {
                return false;
            }

            _shopOfferGenerator = _shopOfferGenerator.CreateFresh();
            ActiveShop = null;
            CompletedShopCount = 0;
            LastGoldReward = 0;
            UtilityPriceLevel = 0;
            _goldBeforeCurrentBattle = CombatSession.Progress.Player.CurrentGold;
            Phase = FormalRunPhase.Combat;
            return true;
        }

        internal void SynchronizeExternalState()
        {
            if (Phase != FormalRunPhase.NotStarted)
            {
                return;
            }

            StageProgressionState state = CombatSession.Progress.State;
            switch (state)
            {
                case StageProgressionState.NotStarted:
                    return;
                case StageProgressionState.OpponentSelection:
                case StageProgressionState.InBattle:
                    ReplayCompletedShopOffers();
                    _goldBeforeCurrentBattle =
                        CombatSession.Progress.Player.CurrentGold;
                    Phase = FormalRunPhase.Combat;
                    return;
                case StageProgressionState.StageCleared:
                    RestoreShopForStableProgress();
                    return;
                case StageProgressionState.RunVictory:
                    Phase = FormalRunPhase.RunVictory;
                    return;
                case StageProgressionState.RunDefeat:
                    Phase = FormalRunPhase.RunDefeat;
                    return;
                case StageProgressionState.RewardSelection:
                    throw new InvalidOperationException(
                        "A formal run cannot adopt a battle reward selection state.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private void HandleBattleResultSynchronized()
        {
            StageProgressionState state = CombatSession.Progress.State;
            int currentGold = CombatSession.Progress.Player.CurrentGold;
            LastGoldReward = currentGold - _goldBeforeCurrentBattle;
            if (LastGoldReward < 0)
            {
                throw new InvalidOperationException(
                    "A combat result cannot reduce the run gold balance.");
            }

            switch (state)
            {
                case StageProgressionState.StageCleared:
                    StageDefinition completedStage = CombatSession.ActiveStage ??
                        throw new InvalidOperationException(
                            "A completed combat must retain its active stage.");
                    bool followsEliteVictory = completedStage.BattleProfileKey != null &&
                        EnemyCombatProfileCatalog.Default
                            .GetPreviewByKey(completedStage.BattleProfileKey)
                            .Grade == EnemyGrade.Elite;
                    ActiveShop = new ShopVisit(_shopOfferGenerator.Generate(
                        CompletedShopCount,
                        UtilityPriceLevel,
                        followsEliteVictory,
                        GetOwnedDemonDefinitionKeys()));
                    Phase = FormalRunPhase.Shop;
                    CombatSettlementCompleted?.Invoke();
                    break;
                case StageProgressionState.RunVictory:
                    ActiveShop = null;
                    Phase = FormalRunPhase.RunVictory;
                    RunEnded?.Invoke();
                    break;
                case StageProgressionState.RunDefeat:
                    LastGoldReward = 0;
                    ActiveShop = null;
                    Phase = FormalRunPhase.RunDefeat;
                    RunEnded?.Invoke();
                    break;
                default:
                    throw new InvalidOperationException(
                        "A synchronized battle must produce a stable formal run result.");
            }
        }

        private void RestoreShopForStableProgress()
        {
            int visitIndex = CombatSession.Progress.CurrentStageIndex;
            if (visitIndex < 0 || visitIndex > 1)
            {
                throw new InvalidOperationException(
                    "Only the two normal stages can restore a formal shop.");
            }

            if (CompletedShopCount != visitIndex)
            {
                throw new InvalidOperationException(
                    "Restored shop count does not match the stable stage.");
            }

            ReplayCompletedShopOffers();

            StageDefinition completedStage = CombatSession.ActiveStage ??
                CombatSession.Progress.CurrentStage;
            bool followsEliteVictory = FollowsEliteVictory(completedStage);
            ActiveShop = new ShopVisit(_shopOfferGenerator.Generate(
                CompletedShopCount,
                UtilityPriceLevel,
                followsEliteVictory,
                GetOwnedDemonDefinitionKeys()));
            LastGoldReward = 0;
            _goldBeforeCurrentBattle = CombatSession.Progress.Player.CurrentGold;
            Phase = FormalRunPhase.Shop;
        }

        private static bool FollowsEliteVictory(StageDefinition stage)
        {
            return stage.BattleProfileKey != null &&
                EnemyCombatProfileCatalog.Default
                    .GetPreviewByKey(stage.BattleProfileKey)
                    .Grade == EnemyGrade.Elite;
        }

        private void ReplayCompletedShopOffers()
        {
            if (_shopOfferGenerator.NextOfferOrdinal != 0)
            {
                return;
            }

            for (int skippedVisit = 0;
                 skippedVisit < CompletedShopCount;
                 skippedVisit++)
            {
                _shopOfferGenerator.Generate(
                    skippedVisit,
                    0,
                    FollowsEliteVictory(
                        CombatSession.Progress.Stages[skippedVisit]),
                    GetOwnedDemonDefinitionKeys());
            }
        }

        private IReadOnlyList<string> GetOwnedDemonDefinitionKeys()
        {
            IReadOnlyList<RunDemonDefinition> demonDeck =
                CombatSession.Progress.Player.DemonDeck;
            var definitionKeys = new string[demonDeck.Count];
            for (int index = 0; index < demonDeck.Count; index++)
            {
                definitionKeys[index] = demonDeck[index].DefinitionKey;
            }

            return definitionKeys;
        }
    }
}
