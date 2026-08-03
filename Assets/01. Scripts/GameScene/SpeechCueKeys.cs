using System;
using System.Collections.Generic;

namespace DiaBlackJack.GameScene
{
    internal enum SpeechPriority
    {
        RoundStart = 100,
        BattleStart = 200,
        Action = 300,
        Damage = 400,
        LowSoul = 500,
        Terminal = 600,
    }

    internal static class SpeechCueKeys
    {
        public const string BattleStart = "combat.battle_start";
        public const string RoundStart = "combat.round_start";
        public const string ActionHit = "combat.action.hit";
        public const string ActionStand = "combat.action.stand";
        public const string ActionChange = "combat.action.change";
        public const string ActionUseCard = "combat.action.use_card";
        public const string ActionDemonContract =
            "combat.action.demon_contract";
        public const string DamageCard = "combat.damage.card";
        public const string DamageRound = "combat.damage.round";
        public const string DamageOther = "combat.damage.other";
        public const string LowSoul = "combat.low_soul";
        public const string Victory = "combat.victory";
        public const string Defeat = "combat.defeat";

        public const string ShopGreeting = "shop.greeting";
        public const string ShopPurchaseSuccess = "shop.purchase_success";
        public const string ShopInsufficientGold = "shop.insufficient_gold";
        public const string ShopSoldOut = "shop.sold_out";
        public const string ShopUnavailable = "shop.unavailable";
        public const string ShopLighterSuccess = "shop.lighter_success";
        public const string ShopWhiskeySuccess = "shop.whiskey_success";
        public const string ShopFarewell = "shop.farewell";

        private static readonly IReadOnlyList<string> EnemyKeys =
            Array.AsReadOnly(new[]
            {
                BattleStart,
                RoundStart,
                ActionHit,
                ActionStand,
                ActionChange,
                ActionUseCard,
                ActionDemonContract,
                DamageCard,
                DamageRound,
                DamageOther,
                LowSoul,
                Victory,
                Defeat,
            });

        private static readonly IReadOnlyList<string> ShopKeys =
            Array.AsReadOnly(new[]
            {
                ShopGreeting,
                ShopPurchaseSuccess,
                ShopInsufficientGold,
                ShopSoldOut,
                ShopUnavailable,
                ShopLighterSuccess,
                ShopWhiskeySuccess,
                ShopFarewell,
            });

        public static IReadOnlyList<string> RequiredEnemyKeys => EnemyKeys;

        public static IReadOnlyList<string> RequiredShopKeys => ShopKeys;
    }
}
