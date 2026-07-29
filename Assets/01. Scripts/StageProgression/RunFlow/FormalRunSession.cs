using System;
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
            CombatSession.BattleResultSynchronized += HandleBattleResultSynchronized;
            Phase = FormalRunPhase.NotStarted;
        }

        public ShopVisit ActiveShop { get; private set; }

        public StageProgressionSession CombatSession { get; }

        public int CompletedShopCount { get; private set; }

        public int LastGoldReward { get; private set; }

        public FormalRunPhase Phase { get; private set; }

        public int UtilityPriceLevel { get; private set; }

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
                !shop.CanClose(offerId) ||
                !CombatSession.TryAdvanceToNextStage())
            {
                return false;
            }

            if (!shop.TryClose(offerId))
            {
                throw new InvalidOperationException(
                    "A validated shop visit could not be closed.");
            }

            if (shop.HasUsedAnyUtility)
            {
                UtilityPriceLevel++;
            }

            CompletedShopCount++;
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
                        followsEliteVictory));
                    Phase = FormalRunPhase.Shop;
                    break;
                case StageProgressionState.RunVictory:
                    ActiveShop = null;
                    Phase = FormalRunPhase.RunVictory;
                    break;
                case StageProgressionState.RunDefeat:
                    LastGoldReward = 0;
                    ActiveShop = null;
                    Phase = FormalRunPhase.RunDefeat;
                    break;
                default:
                    throw new InvalidOperationException(
                        "A synchronized battle must produce a stable formal run result.");
            }
        }
    }
}
